﻿using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Inedo.Diagnostics;
using Inedo.ExecutionEngine.Executer;
using Inedo.Extensibility.CIServers;
using Inedo.Extensions.TeamCity.Credentials;

namespace Inedo.Extensions.TeamCity;

internal sealed class TeamCityClient
{
    public static readonly string[] BuiltInTypes = new[] { "lastSuccessful", "lastPinned", "lastFinished" };
    private readonly HttpClient httpClient;
    private readonly ILogSink? log;

    public TeamCityClient(TeamCityCredentials credentials, ILogSink? log = null)
    {
        if (string.IsNullOrEmpty(credentials.ServiceUrl))
            throw new ArgumentException($"{nameof(credentials.ServiceUrl)} is missing from TeamCity credentials.");

        this.log = log;

        var url = credentials.ServiceUrl;
        if (!url.EndsWith('/'))
            url += "/";

        this.httpClient = SDK.CreateHttpClient();
        this.httpClient.BaseAddress = new Uri(url);

        string auth;
        if (credentials.Password != null)
        {
            this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AH.Unprotect(credentials.Password));
            auth = "token-authenticated";
        }
        else
        {
            auth = "anonymous";
        }

        this.log?.LogDebug($"Initiating {auth} connection to {url}");
    }

    public async IAsyncEnumerable<CIProjectInfo> GetProjectsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var projElement in this.GetPaginatedResultsAsync("app/rest/projects", "project", cancellationToken).ConfigureAwait(false))
        {
            if (projElement.Attribute("parentProjectId") != null)
            {
                var id = (string?)projElement.Attribute("id");
                var name = (string?)projElement.Attribute("name");

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                    yield return new CIProjectInfo(id, name);
            }
        }
    }

    public async IAsyncEnumerable<CIBuildInfo> GetBuildsAsync(string project, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var url = $"app/rest/builds?locator=defaultFilter:false,project:{Uri.EscapeDataString(project)}&fields=build(id,number,status,state,webUrl,startDate,buildTypeId)";

        var buildTypeNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await foreach (var bt in this.GetProjectBuildTypesAsync(project, cancellationToken).ConfigureAwait(false))
        {
            buildTypeNames[bt.Id] = bt.Name;
        }

        await foreach (var buildElement in this.GetPaginatedResultsAsync(url, "build", cancellationToken).ConfigureAwait(false))
        {
            var id = (string?)buildElement.Attribute("id");
            var number = (string?)buildElement.Attribute("number");
            var status = (string?)buildElement.Attribute("status");
            var webUrl = (string?)buildElement.Attribute("webUrl");
            var date = TryParseTeamCityDate(buildElement.Element("startDate")?.Value);
            var buildTypeId = (string?)buildElement.Attribute("buildTypeId");

            if (id == null || number == null || status == null || webUrl == null || date == null || buildTypeId == null)
                continue;
            if (!buildTypeNames.TryGetValue(buildTypeId, out var buildTypeName))
                continue;

            yield return new CIBuildInfo(id, buildTypeName, number, date.Value.UtcDateTime, status, webUrl);
        }
    }
    public async Task<TeamCityBuildInfo> GetBuildAsync(string buildId, CancellationToken cancellationToken = default)
    {
        var url = $"app/rest/builds/id:{Uri.EscapeDataString(buildId)}?fields=file(name),properties(property(name,value)),buildType";
        var xdoc = await this.GetXDocumentAsync(url, cancellationToken).ConfigureAwait(false);

        var buildTypes = xdoc
            .Descendants("buildType")
            .Select(TeamCityBuildType.FromXElement)
            .ToList();

        var properties = xdoc
            .Descendants("property")
            .Select(p => new KeyValuePair<string, string>((string?)p.Attribute("name") ?? string.Empty, (string?)p.Attribute("value") ?? string.Empty))
            .ToList();

        var artifacts = xdoc
            .Descendants("file")
            .Select(f => (string?)f.Attribute("name") ?? string.Empty)
            .ToList();

        return new TeamCityBuildInfo(artifacts, properties, buildTypes);
    }
    public async Task<TeamCityBuildType> GetBuildTypeByIdAsync(string buildTypeId, CancellationToken cancellationToken = default)
    {
        var url = $"app/rest/buildTypes/id:{Uri.EscapeDataString(buildTypeId)}";
        var xdoc = await this.GetXDocumentAsync(url, cancellationToken).ConfigureAwait(false);

        return TeamCityBuildType.FromXElement(xdoc.Root!);
    }
    public async IAsyncEnumerable<TeamCityBuildType> GetProjectBuildTypesAsync(string project, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var url = $"app/rest/projects/{Uri.EscapeDataString(project)}?fields=buildTypes(buildType)";
        var xdoc = await this.GetXDocumentAsync(url, cancellationToken).ConfigureAwait(false);

        var types = xdoc
            .Descendants("buildType")
            .Select(TeamCityBuildType.FromXElement);

        foreach (var t in types)
            yield return t;
    }
    public async Task<TeamCityBuildStatus> QueueBuildAsync(string buildConfigId, string? branchName = null, IEnumerable<KeyValuePair<string, string>>? additionalProperties = null, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        using (var writer = XmlWriter.Create(buffer, new XmlWriterSettings { OmitXmlDeclaration = true }))
        {
            writer.WriteStartElement("build");
            if (!string.IsNullOrEmpty(branchName))
                writer.WriteAttributeString("branchName", branchName);

            writer.WriteStartElement("buildType");
            writer.WriteAttributeString("id", buildConfigId);
            writer.WriteEndElement(); // buildType

            var props = additionalProperties?.ToList();
            if (props?.Count > 0)
            {
                writer.WriteStartElement("properties");
                foreach (var p in props)
                {
                    writer.WriteStartElement("property");
                    writer.WriteAttributeString("name", p.Key);
                    writer.WriteAttributeString("value", p.Value);
                    writer.WriteEndElement(); // property
                }

                writer.WriteEndElement(); // properties
            }

            writer.WriteEndElement(); // build
        }

        buffer.Position = 0;
        using var content = new StreamContent(buffer);
        content.Headers.ContentType = new ("application/xml");
        using var res = await this.httpClient.PostAsync("app/rest/buildQueue", content, cancellationToken).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();

        using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
        var xdoc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken)
            ?? throw new ExecutionFailureException("Invalid response: expected XML.");

        return TeamCityBuildStatus.FromXElement(xdoc.Root!);
    }
    public async Task<Stream> DownloadArtifactsAsync(string buildConfigId, string buildId, CancellationToken cancellationToken = default)
    {
        var url = $"repository/downloadAll/{Uri.EscapeDataString(buildConfigId)}/{Uri.EscapeDataString(buildId)}/artifacts.zip";
        using var stream = await this.httpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
        var temp = new MemoryStream();
        await stream.CopyToAsync(temp, cancellationToken).ConfigureAwait(false);
        temp.Position = 0;
        return temp;
    }
    public async Task<TeamCityBuildStatus> GetBuildStatusAsync(TeamCityBuildStatus status, CancellationToken cancellationToken = default)
    {
        using var stream = await this.httpClient.GetStreamAsync(status.Href, cancellationToken);
        var xdoc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken)
            ?? throw new ExecutionFailureException($"Invalid response from {status.Href}: expected XML.");

        return TeamCityBuildStatus.FromXElement(xdoc.Root!);
    }
    public static void ParseBuildId(string buildId, out string? branchName, out int buildNumber)
    {
        int hyphenIndex = buildId.LastIndexOf('-');
        int? myMaybeBuildNumber;
        if (hyphenIndex < 0)
        {
            branchName = null;
            myMaybeBuildNumber = AH.ParseInt(buildId);
        }
        else
        {
            branchName = buildId[..hyphenIndex];
            myMaybeBuildNumber = AH.ParseInt(buildId[(hyphenIndex + 1)..]);
        }

        if (myMaybeBuildNumber == null)
            throw new FormatException($"{nameof(buildId)} has an unexpected format (\"{buildId}\").");

        buildNumber = myMaybeBuildNumber.Value;
    }

    private async Task<XDocument> GetXDocumentAsync(string url, CancellationToken cancellationToken)
    {
        using var stream = await this.httpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
        return await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
    }
    private async IAsyncEnumerable<XElement> GetPaginatedResultsAsync(string? url, string elementName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!string.IsNullOrWhiteSpace(url))
        {
            var xdoc = await this.GetXDocumentAsync(url, cancellationToken).ConfigureAwait(false);
            foreach (var element in xdoc.Root!.Elements(elementName))
                yield return element;

            url = (string?)xdoc.Root?.Attribute("nextHref");
        }
    }
    static private DateTimeOffset? TryParseTeamCityDate(string? date)
    {
        if (string.IsNullOrEmpty(date))
            return null;

        if (DateTime.TryParse(date, out var result))
            return result;

        //20220928T021935+0000
        var match = Regex.Match(date, "^(?<year>20\\d\\d)(?<month>\\d\\d)(?<day>\\d\\d)T(?<hour>\\d\\d)(?<minute>\\d\\d)(?<second>\\d\\d)\\+(?<offset>\\d\\d\\d\\d)$");
        if (match.Success)
        {
            return new DateTime(
                int.Parse(match.Groups["year"].Value),
                int.Parse(match.Groups["month"].Value),
                int.Parse(match.Groups["day"].Value),
                int.Parse(match.Groups["hour"].Value),
                int.Parse(match.Groups["minute"].Value),
                int.Parse(match.Groups["second"].Value)
            );
        }

        return null;
    }
}

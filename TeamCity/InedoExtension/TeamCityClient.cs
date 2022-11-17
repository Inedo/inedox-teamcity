﻿using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Inedo.Diagnostics;
using Inedo.Extensibility.CIServers;
using Inedo.Extensions.TeamCity.Credentials;

#nullable enable

namespace Inedo.Extensions.TeamCity;

internal sealed record TeamCityBuildType(string Id, string Name);

internal sealed record TeamCityBuildInfo(List<string> Artifacts, List<KeyValuePair<string, string>> Properties, List<TeamCityBuildType> BuildTypes);

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
        if (!string.IsNullOrEmpty(credentials.UserName) && credentials.Password != null)
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
        await foreach (var buildElement in this.GetPaginatedResultsAsync(url, "build", cancellationToken).ConfigureAwait(false))
        {
            var id = (string?)buildElement.Attribute("id");
            var number = (string?)buildElement.Attribute("number");
            var status = (string?)buildElement.Attribute("status");
            var webUrl = (string?)buildElement.Attribute("webUrl");
            var date = (DateTimeOffset?)buildElement.Element("startDate");
            var buildType = (string?)buildElement.Attribute("buildTypeId");

            if (id == null || number == null || status == null || webUrl == null || date == null || buildType == null)
                continue;

            yield return new CIBuildInfo(id, buildType, number, date.Value.UtcDateTime, status, webUrl);
        }
    }
    public async Task<TeamCityBuildInfo> GetBuildAsync(string buildId, CancellationToken cancellationToken = default)
    {
        var url = $"app/rest/builds/id:{Uri.EscapeDataString(buildId)}?fields=file(name),properties(property(name,value)),buildType";
        var xdoc = await this.GetXDocumentAsync(url, cancellationToken).ConfigureAwait(false);

        var buildTypes = xdoc
            .Descendants("buildType")
            .Select(t => new TeamCityBuildType((string?)t.Attribute("id") ?? string.Empty, (string?)t.Attribute("name") ?? string.Empty))
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
    public async IAsyncEnumerable<TeamCityBuildType> GetProjectBuildTypesAsync(string project, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var url = $"app/rest/projects/{Uri.EscapeDataString(project)}?fields=buildTypes";
        var xdoc = await this.GetXDocumentAsync(url, cancellationToken).ConfigureAwait(false);

        var types = xdoc
            .Descendants("buildType")
            .Select(t => new TeamCityBuildType((string?)t.Attribute("id") ?? string.Empty, (string?)t.Attribute("name") ?? string.Empty));

        foreach (var t in types)
            yield return t;
    }
    public async Task QueueBuildAsync(string buildConfigId, string? branchName = null, CancellationToken cancellationToken = default)
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

            writer.WriteEndElement(); // build
        }

        buffer.Position = 0;
        using var content = new StreamContent(buffer);
        using var res = await this.httpClient.PostAsync("app/rest/buildQueue", content, cancellationToken).ConfigureAwait(false);
        res.EnsureSuccessStatusCode();
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
            foreach (var element in xdoc.Elements(elementName))
                yield return element;

            url = (string?)xdoc.Root?.Attribute("nextHref");
        }
    }
}

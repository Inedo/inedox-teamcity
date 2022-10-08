using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility.CIServers;
using Inedo.Extensibility.Credentials;
using Inedo.Extensions.TeamCity.SuggestionProviders;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.Credentials;

[DisplayName("TeamCity Project")]
[Description("Connect to a TeamCity project for importing artifacts and queueing builds")]
[PersistFrom("Inedo.Extensions.TeamCity.Credentials.TeamCitySecureResource,TeamCity")]
public sealed class TeamCityProject : CIProject<TeamCityCredentials>, IMissingPersistentPropertyHandler
{
    [Required]
    [Persistent]
    [DisplayName("[Obsolete] TeamCity server URL")]
    [PlaceholderText("use the credential's URL")]
    [Description("In earlier versions, the TeamCity server URL was specified on the project. This should not be used going forward.")]
    public string? LegacyServerUrl { get; set; }

#warning deal with ProjectName
    [Persistent]
    [SuggestableValue(typeof(ProjectNameSuggestionProvider))]
    [DisplayName("[Obsolete] Project name")]
    [PlaceholderText("use the project ID & Display name")]
    public string? LegacyProjectName { get; set; }

    private TeamCityBuildInfo? buildInfo;

    public override async IAsyncEnumerable<string> GetBuildArtifactsAsync(string buildId, ICredentialResolutionContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(buildId))
            throw new ArgumentNullException(nameof(buildId));

        var build = await this.GetBuildInfoAsync(buildId, context, cancellationToken).ConfigureAwait(false);
        foreach (var a in build.Artifacts)
            yield return a;
    }

    public override IAsyncEnumerable<CIBuildInfo> GetBuildsAsync(ICredentialResolutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(this.ProjectId))
            throw new InvalidOperationException($"{nameof(ProjectId)} is required.");

        var creds = this.GetCredentials(context);
        if (creds is not TeamCityCredentials teamCityCreds)
            throw new InvalidOperationException("Unsupported credentials type.");

        var client = new TeamCityClient(teamCityCreds);
        return client.GetBuildsAsync(this.ProjectId, cancellationToken);
    }
    public override async IAsyncEnumerable<KeyValuePair<string, string>> GetBuildVariablesAsync(string buildId, ICredentialResolutionContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(buildId))
            throw new ArgumentNullException(nameof(buildId));

        var build = await this.GetBuildInfoAsync(buildId, context, cancellationToken).ConfigureAwait(false);
        foreach (var p in build.Properties)
            yield return p;
    }
    private async ValueTask<TeamCityBuildInfo> GetBuildInfoAsync(string buildId, ICredentialResolutionContext context, CancellationToken cancellationToken)
    {
        if (this.buildInfo != null)
            return this.buildInfo;

        var creds = this.GetCredentials(context);
        if (creds is not TeamCityCredentials teamCityCreds)
            throw new InvalidOperationException("Unsupported credentials type.");

        var client = new TeamCityClient(teamCityCreds);
        return this.buildInfo = await client.GetBuildAsync(buildId, cancellationToken).ConfigureAwait(false);
    }

    public override RichDescription GetDescription() => new(this.ProjectDisplayName);

    void IMissingPersistentPropertyHandler.OnDeserializedMissingProperties(IReadOnlyDictionary<string, string> missingProperties)
    {
        if (missingProperties.ContainsKey("ServerUrl"))
            this.LegacyServerUrl = missingProperties["ServerUrl"];
        if (missingProperties.ContainsKey("ProjectName"))
        {
            this.LegacyProjectName = missingProperties["ProjectName"];
            this.ProjectDisplayName ??= this.LegacyProjectName;
        }
    }
}

using System.ComponentModel;
using System.IO.Compression;
using System.Reflection;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.ExecutionEngine.Executer;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.TeamCity;
using Inedo.Extensions.TeamCity.Credentials;
using Inedo.Extensions.TeamCity.Operations;
using Inedo.Extensions.TeamCity.SuggestionProviders;
using Inedo.IO;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.TeamCity.Operations;

[DisplayName("Import Artifact from TeamCity")]
[Description("Downloads an artifact from the specified TeamCity server and saves it to the artifact library.")]
[ScriptAlias("Import-Artifact")]
[Tag("artifacts")]
[Tag("teamcity")]
public sealed class ImportTeamCityArtifactOperation : TeamCityOperation, IImportCIArtifactsOperation
{
    [ScriptAlias("From")]
    [DisplayName("TeamCity resource")]
    [DefaultValue("$CIProject")]
    [SuggestableValue(typeof(SecureResourceSuggestionProvider<TeamCityProject>))]
    public override string? ResourceName { get; set; }
    [ScriptAlias("Project"), ScriptAlias("Job", Obsolete = true)]
    [DisplayName("Project name")]
    [DefaultValue("$CIProject")]
    [SuggestableValue(typeof(ProjectNameSuggestionProvider))]
    public override string? ProjectName { get; set; }
    [ScriptAlias("BuildConfiguration")]
    [DisplayName("Build configuration")]
    [DefaultValue("$CIProjectScope")]
    [SuggestableValue(typeof(BuildConfigurationNameSuggestionProvider))]
    public override string? BuildConfigurationName { get; set; }
    [ScriptAlias("BuildNumber")]
    [DisplayName("Build number")]
    [Description("The build number may be a specific build number, or a special value such as \"lastSuccessful\", \"lastFinished\", or \"lastPinned\". "
        + "To specify a build ID instead, append ':id' as a suffix, e.g. 1234:id")]
    [SuggestableValue(typeof(BuildNumberSuggestionProvider))]
    [DefaultValue("$CIBuildNumber")]
    public string? BuildNumber { get; set; }

    [Category("Advanced")]
    [ScriptAlias("Artifact")]
    [DisplayName("BuildMaster artifact name")]
    [DefaultValue("Default")]
    [Description("The name of the artifact in BuildMaster to create after artifacts are downloaded from TeamCity.")]
    public string? ArtifactName { get; set; }
    [Category("Advanced")]
    [ScriptAlias("Include")]
    [DisplayName("Include files")]
    [DefaultValue("**")]
    [Description(CommonDescriptions.MaskingHelp)]
    public IEnumerable<string>? Includes { get; set; }
    [Category("Advanced")]
    [ScriptAlias("Exclude")]
    [DisplayName("Exclude files")]
    [Description(CommonDescriptions.MaskingHelp)]
    public IEnumerable<string>? Excludes { get; set; }
    [Category("Advanced")]
    [ScriptAlias("Branch")]
    [DisplayName("Branch filter")]
    [Description("When the build number is a special value such as \"lastSuccesful\", this will be used to find that build")]
    public string? BranchName { get; set; }
    [Output]
    [Category("Advanced")]
    [ScriptAlias("TeamCityBuildNumber")]
    [DisplayName("Actual build number (output)")]
    [PlaceholderText("e.g. $ActualBuildNumber")]
    [Description("When you specify a Build Number like \"lastSuccessful\", this will output the real TeamCity BuildNumber into a runtime variable.")]
    public string? TeamCityBuildNumber { get; set; }

    [Category("Advanced")]
    [ScriptAlias("BuildConfigurationId")]
    [DisplayName("Build configuration ID")]
    [Description("TeamCity identifier that targets a single build configuration. May be specified instead of the Project name and Build configuration name.")]
    public string? BuildConfigurationId { get; set; }

    string? IImportCIArtifactsOperation.BuildId 
    {
        get => AH.NullIf(this.BranchName + "-", "-") + this.BuildNumber;
        set
        {
            if (value == null)
            {
                this.BranchName = null;
                this.BuildNumber = null;
            }
            else
            {
                TeamCityClient.ParseBuildId(value, out var branch, out var number);
                this.BranchName = branch;
                this.BuildNumber = number.ToString();
            }

        }
    }

    public async override Task ExecuteAsync(IOperationExecutionContext context)
    {
        if (string.IsNullOrEmpty(this.ArtifactName))
            throw new ExecutionFailureException("ArtifactName was not specified.");

        if (!this.TryCreateClient(context, out var client))
            throw new ExecutionFailureException($"Could not create a connection to TeamCity resource \"{AH.CoalesceString(this.ResourceName, this.ServerUrl)}\".");

        if (!string.IsNullOrEmpty(this.BuildConfigurationId))
        {
            if (!string.IsNullOrEmpty(this.ProjectName) || !string.IsNullOrEmpty(this.BuildConfigurationName))
                this.LogWarning($"Project (\"{this.ProjectName}\") and BuildConfiguration  ($\"{this.BuildConfigurationName}\") will be ignored when BuildConfigurationId is used.");

            this.LogDebug($"Querying TeamCity for Build Configuration {this.BuildConfigurationId}...");
            var config = await client.GetBuildTypeByIdAsync(this.BuildConfigurationId, context.CancellationToken);

            this.ProjectName = config.ProjectName;
            this.BuildConfigurationName = config.Name;
        }

        if (this.ProjectName == null)
            throw new ExecutionFailureException("No TeamCity project was specified, and there is no CI build associated with this execution.");
        if (this.BuildNumber == null)
            throw new ExecutionFailureException("No TeamCity build was specified, and there is no CI build associated with this execution.");

        var configName = this.BuildConfigurationName;
        if (string.IsNullOrEmpty(configName))
        {
            this.LogDebug("No build configuration specified, querying TeamCity...");
            await foreach (var t in client.GetProjectBuildTypesAsync(this.ProjectName, context.CancellationToken))
            {
                if (!string.IsNullOrEmpty(configName))
                    throw new ExecutionFailureException("Multiple build configurations were found for this project; specify which one to import artifacts from.");

                configName = t.Name;
            }
        }
        if (string.IsNullOrEmpty(configName))
            throw new ExecutionFailureException("No Build Configuration was specified, and there is no CI build associated with this execution.");

        using var zipStream = await client.DownloadArtifactsAsync(configName, this.BuildNumber, context.CancellationToken);
        var mask = new MaskingContext(this.Includes, this.Excludes);
        if (!mask.MatchAll)
        {
            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Update, true))
            {
                var deletes = new List<ZipArchiveEntry>();

                foreach (var entry in zip.Entries)
                {
                    if (!mask.IsMatch(entry.FullName))
                        deletes.Add(entry);
                }

                foreach (var entry in deletes)
                    entry.Delete();
            }

            zipStream.Position = 0;
        }

        await context.CreateBuildMasterArtifactAsync(this.ArtifactName, zipStream, false, context.CancellationToken);
    }

    protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
    {
        string? val(string name) => AH.NullIf(config[name], this.GetType().GetProperty(name)?.GetCustomAttribute<DefaultValueAttribute>()?.Value?.ToString());

        var projectName = val(nameof(this.ProjectName));
        var configurationName = val(nameof(this.BuildConfigurationName));
        var buildNum = val(nameof(this.BuildNumber));

        if (!string.IsNullOrEmpty(configurationName))
            projectName += $" (${configurationName}";


        return new ExtendedRichDescription(
            new RichDescription("Import TeamCity Artifacts"),
            string.IsNullOrEmpty(this.BuildConfigurationId)
                ? string.IsNullOrEmpty(projectName)
                    ? new RichDescription("from the associated TeamCity build")
                    : new RichDescription("from build ", new Hilite(buildNum), " in project ", new Hilite(projectName))
                : new RichDescription("from build ", new Hilite(buildNum), " using build configuration ", new Hilite(this.BuildConfigurationId))
        );
    }
}

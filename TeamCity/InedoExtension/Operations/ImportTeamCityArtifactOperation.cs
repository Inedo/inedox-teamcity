using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.ExecutionEngine.Executer;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.TeamCity.Credentials;
using Inedo.Extensions.TeamCity.Operations;
using Inedo.Extensions.TeamCity.SuggestionProviders;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.TeamCity.Operations;

[DisplayName("Import Artifact from TeamCity")]
[Description("Downloads an artifact from the specified TeamCity server and saves it to the artifact library.")]
[ScriptAlias("Import-Artifact")]
[Tag("artifacts")]
[Tag("teamcity")]
public sealed class ImportTeamCityArtifactOperation : TeamCityOperation
{
    [ScriptAlias("From")]
    [DisplayName("TeamCity resource")]
    [DefaultValue("$CIProject")]
    [SuggestableValue(typeof(SecureResourceSuggestionProvider<TeamCityProject>))]
    public override string? ResourceName { get; set; }
    [ScriptAlias("Project"), ScriptAlias("Job", Obsolete = true)]
    [DisplayName("Project name")]
    [DefaultValue("$TeamCityProjectName($CIProject)")]
    [SuggestableValue(typeof(ProjectNameSuggestionProvider))]
    public override string? ProjectName { get; set; }
    [ScriptAlias("BuildConfiguration")]
    [DisplayName("Build configuration")]
    [DefaultValue("$TeamCityBuildConfigurationName($CIBuild)")]
    [SuggestableValue(typeof(BuildConfigurationNameSuggestionProvider))]
    public override string? BuildConfigurationName { get; set; }
    [ScriptAlias("BuildNumber")]
    [DisplayName("Build number")]
    [DefaultValue("$TeamCityBuildNumber($CIBuild)")]
    [Description("The build number may be a specific build number, or a special value such as \"lastSuccessful\", \"lastFinished\", or \"lastPinned\". "
        + "To specify a build ID instead, append ':id' as a suffix, e.g. 1234:id")]
    [SuggestableValue(typeof(BuildNumberSuggestionProvider))]
    public string? BuildNumber { get; set; }

    [Category("Advanced")]
    [ScriptAlias("Artifact")]
    [DisplayName("BuildMaster artifact name")]
    [DefaultValue("Default"), NotNull]
    [Description("The name of the artifact in BuildMaster to create after artifacts are downloaded from Jenkins.")]
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


    [Undisclosed]
    [ScriptAlias("BuildConfigurationId", Obsolete = true)]
    public string? BuildConfigurationId { get; set; }

    public async override Task ExecuteAsync(IOperationExecutionContext context)
    {
        if (this.BuildConfigurationId == null)
            this.LogWarning($"Specifying BuildConfigurationId is no longer supported, and the property value of \"{this.BuildConfigurationId}\" will be ignored. Use BuildConfigurationName instead.");

        if (this.ProjectName == null)
            throw new ExecutionFailureException($"No TeamCity project was specified, and there is no CI build associated with this execution.");
        if (this.BuildNumber == null)
            throw new ExecutionFailureException($"No TeamCity build was specified, and there is no CI build associated with this execution.");
        if (!this.TryCreateClient(context, out var client))
            throw new ExecutionFailureException($"Could not create a connection to Jenkins resource \"{AH.CoalesceString(this.ResourceName, this.ServerUrl)}\".");

#warning Implement Importing
        throw new NotImplementedException();
    }

    protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
    {
        string buildNumber = config[nameof(this.BuildNumber)];
        return new ExtendedRichDescription(
            new RichDescription("Import TeamCity ", new Hilite(config[nameof(this.ArtifactName)]), " Artifact "),
            new RichDescription("of build ",
                AH.ParseInt(buildNumber) != null ? "#" : "",
                new Hilite(buildNumber),
                " of project ", 
                new Hilite(config[nameof(this.ProjectName)]),
                " using configuration \"",
                config[nameof(this.BuildConfigurationName)]
            )
        );
    }
}

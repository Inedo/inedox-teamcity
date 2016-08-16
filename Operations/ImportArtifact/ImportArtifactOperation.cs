using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.Documentation;
using System.Collections.Generic;
using Inedo.BuildMaster.Web.Controls.Plans;
using System.Reflection;
using Inedo.Web.Controls;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.TeamCity.Operations
{

    
    [DisplayName("Import Artifact from TeamCity")]
    [Description("Downloads an artifact from the specified TeamCity server and saves it to the artifact library.")]
    [ScriptAlias("Import-Artifact")]
    // [CustomEditor(typeof(ImportArtifactOperationEditor))] // Waiting for fix #EDO-1645
    [Tag(Tags.Artifacts)]
    public sealed class ImportArtifactOperation : Operation
    {

        [Required]
        [ScriptAlias("BuildConfigurationId")]
        [DisplayName("Build configuration")]
        [CustomEditor(typeof(BuildConfigurationArgumentEditor))] 
        public string BuildConfigurationId { get; set; }

        [ScriptAlias("BuildNumber")]
        [DisplayName("Build number")]
        [DefaultValue("lastSuccessful")]
        [PlaceholderText("lastSuccessful")]
        [Description("The build number may be a specific build number, or a special value such as \"lastSuccessful\", \"lastFinished\", or \"lastPinned\".")]
        public string BuildNumber { get; set; }

        [ScriptAlias("Branch")]
        [DisplayName("Branch name")]
        [PlaceholderText("Default")]
        public string BranchName { get; set; }

        [ScriptAlias("Artifact")]
        [DisplayName("Artifact name")]
        [PlaceholderText("All")]
        public string ArtifactName { get; set; }

        public async override Task ExecuteAsync(IOperationExecutionContext context)
        {
            var manager = new ImportArtifactOperationManager(this, this, context)
            {
                BuildConfigurationId = this.BuildConfigurationId,
                BuildNumber = this.BuildNumber,
                BranchName = this.BranchName,
                ArtifactName = this.ArtifactName
            };

            string teamCityBuildNumber = await manager.ImportAsync().ConfigureAwait(false);

            this.LogDebug("TeamCity build number resolved to {0}, creating $TeamCityBuildNumber variable...", teamCityBuildNumber);

            await new DB.Context(false).Variables_CreateOrUpdateVariableDefinitionAsync(
                "TeamCityBuildNumber",
                Application_Id: context.ApplicationId,
                Release_Number: context.ReleaseNumber,
                Build_Number: context.BuildNumber,
                Value_Text: teamCityBuildNumber,
                Sensitive_Indicator: false,
                Environment_Id: null,
                ServerRole_Id: null,
                Server_Id: null,
                ApplicationGroup_Id: null,
                Execution_Id: null,
                Promotion_Id: null,
                Deployable_Id: null
            ).ConfigureAwait(false);
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            string buildNumber = config[nameof(this.BuildNumber)];
            string branchName = (string) config[nameof(this.BranchName)] ?? "default";
            string buildConfigurationId = config[nameof(this.BuildConfigurationId)];

            return new ExtendedRichDescription(
                new RichDescription("Import TeamCity ", new Hilite(config[nameof(this.ArtifactName)]), " Artifact "),

                new RichDescription(
                    "of build ", AH.ParseInt(buildNumber) != null ? "#" : "", new Hilite(buildNumber),
                    " on branch ", new Hilite(branchName),
                    " of build configuration ", new Hilite(buildConfigurationId)
                )
            );
        }
    }
}

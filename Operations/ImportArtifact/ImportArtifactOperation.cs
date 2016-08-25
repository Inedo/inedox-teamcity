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
using System;

namespace Inedo.BuildMasterExtensions.TeamCity.Operations
{

    /// <summary>
    /// This class defines a Plan operation which imports an artifact from TeamCity.
    /// It uses the Resource Credentials via its base class <see cref="Operation"/>.
    /// The work logic is performed in <see cref="ImportArtifactOperationManager"/>.
    /// </summary>
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

        [ScriptAlias("BranchName")]
        [DisplayName("Branch name")]
        [PlaceholderText("Default")]
        public string BranchName { get; set; }

        [ScriptAlias("Artifact")]
        [DisplayName("Artifact name")]
        [PlaceholderText("All")]
        public string ArtifactName { get; set; }

        public async override Task ExecuteAsync(IOperationExecutionContext context)
        {
            // Note: here we are passing the connectionInfo data from the base class's Credentials (hence we are not using legacy Configuration Profiles)
            var manager = new ImportArtifactOperationManager(this, context);

            await manager.ImportAsync().ConfigureAwait(false);

        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            string buildNumber = config[nameof(this.BuildNumber)];

            string branchName = config[nameof(this.BranchName)];
            branchName = string.IsNullOrEmpty(branchName) ? "default" : branchName;

            string buildConfigurationId = config[nameof(this.BuildConfigurationId)];

            string artifactName = config[nameof(this.ArtifactName)];
            artifactName = string.IsNullOrEmpty(artifactName) ? "" : $"'{artifactName}'";

            return new ExtendedRichDescription(
                new RichDescription("Import Artifact from TeamCity ", new Hilite(artifactName)),

                new RichDescription(
                    " of build ", AH.ParseInt(buildNumber) != null ? "#" : "", new Hilite(buildNumber),
                    " on branch ", new Hilite(branchName),
                    " of build configuration ", new Hilite(buildConfigurationId)
                )
            );
        }
    }

}

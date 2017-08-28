using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web.Controls;
using Inedo.Diagnostics;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.TeamCity.Operations
{
    [DisplayName("Queue TeamCity Build")]
    [Description("Queues a build in TeamCity, optionally waiting for its completion.")]
    [ScriptAlias("Queue-Build")]
    [Tag(Tags.Builds)]
    [Tag("teamcity")]
    public sealed class QueueTeamCityBuildOperation : TeamCityOperation
    {
        private TeamCityBuildQueuer buildQueuer;

        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }

        [Required]
        [ScriptAlias("Project")]
        [DisplayName("Project name")]
        [SuggestibleValue(typeof(ProjectNameSuggestionProvider))]
        public string ProjectName { get; set; }
        [Required]
        [ScriptAlias("BuildConfiguration")]
        [DisplayName("Build configuration")]
        [SuggestibleValue(typeof(BuildConfigurationNameSuggestionProvider))]
        public string BuildConfigurationName { get; set; }
        [ScriptAlias("Branch")]
        [DisplayName("Branch name")]
        [PlaceholderText("Default")]
        public string BranchName { get; set; }

        [ScriptAlias("BuildConfigurationId")]
        [DisplayName("Build configuration ID")]
        [Description("TeamCity identifier that targets a single build configuration. May be specified instead of the Project name and Build configuration name.")]
        public string BuildConfigurationId { get; set; }

        [Category("Advanced")]
        [ScriptAlias("AdditionalParameters")]
        [DisplayName("Additional parameters")]
        [Description("Optionally enter any additional parameters accepted by the TeamCity API in query string format, for example:<br/> "
            + "&amp;name=agent&amp;value=&lt;agentnamevalue&gt;&amp;name=system.name&amp;value=&lt;systemnamevalue&gt;..")]
        public string AdditionalParameters { get; set; }
        [Category("Advanced")]
        [ScriptAlias("WaitForCompletion")]
        [DisplayName("Wait for completion")]
        [DefaultValue(true)]
        [PlaceholderText("true")]
        public bool WaitForCompletion { get; set; } = true;

        public override Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.buildQueuer = new TeamCityBuildQueuer((ITeamCityConnectionInfo)this, (ILogger)this, context)
            {
                ProjectName = this.ProjectName,
                BuildConfigurationId = this.BuildConfigurationId,
                BuildConfigurationName = this.BuildConfigurationName,
                AdditionalParameters = this.AdditionalParameters,
                WaitForCompletion = this.WaitForCompletion,
                BranchName = this.BranchName
            };

            return this.buildQueuer.QueueBuildAsync(context.CancellationToken, logProgressToExecutionLog: false);
        }

        public override OperationProgress GetProgress()
        {
            return this.buildQueuer.GetProgress();
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Queue TeamCity Build"),
                new RichDescription(
                    "for project ", 
                    new Hilite(config[nameof(this.ProjectName)]), 
                    " configuration ", 
                    new Hilite(config[nameof(this.BuildConfigurationName)]), 
                    !string.IsNullOrEmpty(this.BranchName) ? " using branch " + this.BranchName : ""
                )
            );
        }
    }
}

using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.TeamCity.Operations
{
    [DisplayName("Queue TeamCity Build")]
    [Description("Queues a build in TeamCity, optionally waiting for its completion.")]
    [ScriptAlias("Queue-Build")]
    [Tag(Tags.Builds)]
    public sealed class QueueBuildOperation :  Operation
    {
        private QueueBuildOperationManager manager;

        [Required]
        [ScriptAlias("BuildConfigurationId")]
        [DisplayName("Build configuration")]
        [CustomEditor(typeof(BuildConfigurationArgumentEditor))]
        public string BuildConfigurationId { get; set; }

        [ScriptAlias("Branch")]
        [DisplayName("Branch name")]
        [PlaceholderText("Default")]
        public string BranchName { get; set; }

        //[Category("Advanced")]
        //[ScriptAlias("AdditionalParameters")]
        //[DisplayName("Additional parameters")]
        //[Description("Optionally enter any additional parameters accepted by the TeamCity API in query string format, for example:<br/> "
        //    + "&amp;name=agent&amp;value=&lt;agentnamevalue&gt;&amp;name=system.name&amp;value=&lt;systemnamevalue&gt;..")]
        //public string AdditionalParameters { get; set; }

        [Category("Advanced")]
        [ScriptAlias("WaitForCompletion")]
        [DisplayName("Wait for completion")]
        [DefaultValue(true)]
        [PlaceholderText("true")]
        public bool WaitForCompletion { get; set; } = true;

        public override Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.manager = new QueueBuildOperationManager(this, this, context)
            {
                BuildConfigurationId = this.BuildConfigurationId,
                //AdditionalParameters = this.AdditionalParameters,
                WaitForCompletion = this.WaitForCompletion,
                BranchName = this.BranchName
            };

            return this.manager.QueueBuildAsync(context.CancellationToken, logProgressToExecutionLog: true);
        }

        public override OperationProgress GetProgress()
        {
            return this.manager.GetProgress();
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            string branchName = (string)config[nameof(this.BranchName)] ?? "default";
            string buildConfigurationId = config[nameof(this.BuildConfigurationId)];

            return new ExtendedRichDescription(
                new RichDescription("Queue TeamCity Build"),
                new RichDescription(
                    " on branch ", new Hilite(branchName),
                    " of build configuration ", new Hilite(buildConfigurationId)
                )
            );
        }
    }
}

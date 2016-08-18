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
    /// <summary>
    /// This class defines a Plan operation which triggers a teamcity build.
    /// It uses the Resource Credentials via its base class <see cref="Operation"/>.
    /// The work logic is performed in <see cref="QueueBuildOperationManager"/>.
    /// </summary>
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

        [ScriptAlias("BranchName")]
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
            // Note: here we are passing the connectionInfo data from the base class's Credentials (hence we are not using legacy Configuration Profiles)
            this.manager = new QueueBuildOperationManager(this, context);
           
            return this.manager.QueueBuildAsync(context.CancellationToken, logProgressToExecutionLog: true);
        }

        public override OperationProgress GetProgress()
        {
            return this.manager.GetProgress();
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            string branchName = config[nameof(this.BranchName)];
            branchName = string.IsNullOrEmpty(branchName) ? "default" : branchName;

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

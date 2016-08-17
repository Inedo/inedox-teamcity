using System.ComponentModel;
using System.Threading;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Web;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    /// <summary>
    /// This class implements ??? performs similar function as the QueueBuildOperation available in Plans.
    /// It uses the same manager class to implement this logic (<see cref="QueueBuildOperationManager"/>) but does rely on LEGACY
    /// Configuration profile (<see cref="Configurer"/>) to retrieve credentials for the API, it does *NOT* use Resource Credentials (<see cref="Credentials.Credentials"/>)
    /// </summary>
    [DisplayName("Trigger TeamCity Build")]
    [Description("Triggers a build in TeamCity using the specified build configuration ID.")]
    [CustomEditor(typeof(TriggerBuildActionEditor))]
    [Tag(Tags.ContinuousIntegration)]
    public sealed class TriggerBuildAction : DefaultActionBase
    {
        [Persistent]
        public string BuildConfigurationId { get; set; }
        //[Persistent]
        //public string AdditionalParameters { get; set; }
        [Persistent]
        public bool WaitForCompletion { get; set; }
        [Persistent]
        public string BranchName { get; set; }

        public override string ToString()
        {
            return string.Format(
                "Triggers a build of the configuration \"{0}\" in TeamCity{1}{2}.",
                this.BuildConfigurationId,
                //Util.ConcatNE(" with the additional parameters \"", this.AdditionalParameters, "\""),
                !string.IsNullOrEmpty(this.BranchName) ? " using branch " + this.BranchName : ""
            );
        }

        protected override void Execute()
        {
            // Grabs the default configuration profile (legacy)
            var configurer = this.GetExtensionConfigurer();

            // Builds an operation object as required by the manager (the legacy code abides by the modern code rules)
            var op = new Operations.QueueBuildOperation()
            {
                ServerUrl = configurer.ServerUrl,
                UserName = configurer.UserName,
                Password = configurer.Password,

                BranchName = this.GetBranchName(configurer),
                BuildConfigurationId = this.BuildConfigurationId,
                WaitForCompletion = this.WaitForCompletion
            };

            // use the modern code to perform the task
            var manager = new QueueBuildOperationManager(op, (IGenericBuildMasterContext)this.Context); ;

            manager.QueueBuildAsync(CancellationToken.None, logProgressToExecutionLog: true).WaitAndUnwrapExceptions();
        }

        private string GetBranchName(Configurer configurer)
        {
            if (!string.IsNullOrEmpty(this.BranchName))
                return this.BranchName;

            if (!string.IsNullOrEmpty(configurer.DefaultBranchName))
                return configurer.DefaultBranchName;

            return null;
        }
    }
}

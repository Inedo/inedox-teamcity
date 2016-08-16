using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.ExecutionEngine.Executer;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class QueueBuildOperationManager
    {
        public string BuildConfigurationId { get; set; }
        //public string ProjectName { get; set; }
        //public string BuildConfigurationName { get; set; }
        public string BuildNumber { get; set; } // version
        public string BranchName { get; set; }

        private int progressPercent;
        private string progressMessage;

        public bool WaitForCompletion { get; set; }

        public IConnectionInfo ConnectionInfo { get; }
        public ILogger Logger { get; }
        public IGenericBuildMasterContext Context { get; }

        private TeamCityAPI teamCityAPI;

        public QueueBuildOperationManager(IConnectionInfo connectionInfo, ILogger logger, IGenericBuildMasterContext context)
        {
            if (connectionInfo == null)
                throw new ArgumentNullException(nameof(connectionInfo));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.ApplicationId == null)
                throw new InvalidOperationException("context requires a valid application ID");

            this.ConnectionInfo = connectionInfo;
            this.Logger = logger;
            this.Context = context;

            this.teamCityAPI = new TeamCityAPI(connectionInfo, logger, context);

        }

        public OperationProgress GetProgress()
        {
            return new OperationProgress(this.progressPercent, this.progressMessage);
        }

        public async Task QueueBuildAsync(CancellationToken cancellationToken, bool logProgressToExecutionLog = true)
        {
            this.Logger.LogInformation($"Queueing build in TeamCity...");
            TeamCityBuildType buildType = null;

            //buildType = this.teamCityAPI.GetBuildTypeByName(this.ProjectName, this.BuildConfigurationName); // will raise an error if not found
            buildType = this.teamCityAPI.GetBuildType(this.BuildConfigurationId); // will raise an error if not found

            TeamCityBuild build = new TeamCityBuild();
            string xml;

            using (var client = new WebClient(this.ConnectionInfo))
            {
                this.Logger.LogDebug("Triggering TeamCity build configuration id {0}", buildType.id);

                client.Headers[System.Net.HttpRequestHeader.ContentType] = "application/xml";
                xml = client.UploadString("app/rest/buildQueue", $"<build><buildType id=\"{buildType.id}\" /></build>"); // We WANT synchronous call for this one !
                build.Update(xml);

                // record build details

                this.Logger.LogInformation($"Building of build configuration {buildType.id} was triggered successfully. For reference Build ID is {build.id}.");
                //<a href=\"{2}\" target=\"_blank\">Click here</a> for more details.", this.BuildConfigurationId, build.id, build.href);

                if (!this.WaitForCompletion)
                    return;

                // Loop until the build is complete

                do
                {
                    await Task.Delay(2 * 1000, cancellationToken).ConfigureAwait(false); // check every 2 seconds

                    xml = await client.DownloadStringTaskAsync($"app/rest/buildQueue/id:{build.id}").ConfigureAwait(false);
                    build.Update(xml);

                    this.progressPercent = build.percentageComplete;
                    this.progressMessage = $"Building {build.projectName} Build #{build.number} ({build.percentageComplete}% Complete)\n {build.statusText}";

                    if (logProgressToExecutionLog)
                    {
                        this.Logger.LogInformation(this.progressMessage);

                        if (build.running)
                            this.Logger.LogInformation(build.statusText);

                        if (build.waitReason.Length != 0)
                            this.Logger.LogInformation(build.waitReason);
                    }



                } while (build.running || build.status == TeamCityBuild.BuildStatuses.Unknown);

                this.Logger.LogInformation($"{build.projectName} build #{build.number} : {build.statusText}");

            }
        }

        
        
    }
}

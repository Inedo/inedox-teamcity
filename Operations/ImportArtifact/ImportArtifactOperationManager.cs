using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using Inedo.Agents;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Artifacts;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Files;
using Inedo.Diagnostics;
using Inedo.ExecutionEngine.Executer;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class ImportArtifactOperationManager
    {
        public string BuildConfigurationId { get; set; }

        public string BuildNumber { get; set; } // version

        public string BranchName { get; set; }

        public string ArtifactName { get; set; }


        public IConnectionInfo ConnectionInfo { get; }
        public ILogger Logger { get; }
        public IGenericBuildMasterContext Context { get; }

        private TeamCityAPI teamCityAPI;

        public ImportArtifactOperationManager(IConnectionInfo connectionInfo, ILogger logger, IGenericBuildMasterContext context)
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

        public async Task<string> ImportAsync()
        {
            this.Logger.LogInformation($"Importing artifact \"{this.ArtifactName}\" from TeamCity...");

            TeamCityBuildType buildType = null;
            
            buildType = this.teamCityAPI.GetBuildType(this.BuildConfigurationId); // will raise an error if not found

            if (string.IsNullOrEmpty(this.BuildNumber))
            {
                this.Logger.LogDebug("BuildNumber was not specified in plan configuration, downloading the last successful build...");
                this.BuildNumber = "lastSuccessful";
            }

            var build = this.teamCityAPI.FindBuild(buildType.id, null, null, this.BranchName);

            var artifacts = this.teamCityAPI.GetBuildArtifacts(build.id); // the first isthe latest successful

            // remove artifacts not ending with .zip AND (if defined only keep the one specificied by this.ArtifactName (if not empty)
            foreach (var key in artifacts.Keys)
            {
                if (!string.IsNullOrEmpty(this.ArtifactName) && (string.Compare(key, this.ArtifactName, true) == 0))
                    artifacts.Remove(key);

                if (! key.ToLower().EndsWith(".zip")) // BM does not understand non-zip artifacts...sigh...
                    artifacts.Remove(key);

            }

            if (artifacts.Count == 0)
                throw new Exception($"Could not find artifact named '{this.ArtifactName}' in the list of available artifacts.");

            this.Logger.LogDebug($"Importing {artifacts.Count} TeamCity artifacts...");
            foreach (var artifact in artifacts)
            {
                this.Logger.LogDebug($"Importing artifact : {artifact.Key} ");

                string tempFile = null;
                try
                {
                    using (var client = new WebClient(this.ConnectionInfo))
                    {
                        tempFile = Path.GetTempFileName();
                        this.Logger.LogDebug($"Downloading temp file to \"{tempFile}\"...");
                        try
                        {
                            await client.DownloadFileTaskAsync(artifact.Value.Replace("/httpAuth/", ""), tempFile).ConfigureAwait(false);
                        }
                        catch (WebException wex)
                        {
                            var response = wex.Response as HttpWebResponse;
                            if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                                this.Logger.LogWarning("The TeamCity request returned a 404 - this could mean that the branch name, build number, or build configuration is invalid.");

                            throw;
                        }
                    }

                    this.Logger.LogInformation("Importing artifact into BuildMaster...");

                    ArtifactBuilder.ImportZip(
                        new ArtifactIdentifier(
                            (int)this.Context.ApplicationId,
                            this.Context.ReleaseNumber,
                            this.Context.BuildNumber,
                            this.Context.DeployableId,
                            TrimWhitespaceAndZipExtension(artifact.Key)
                        ),
                        Util.Agents.CreateLocalAgent().GetService<IFileOperationsExecuter>(),
                        new FileEntryInfo(artifact.Key, tempFile)
                    );

                    this.Logger.LogInformation("Artifact imported.");
                }
                finally
                {
                    if (tempFile != null)
                    {
                        this.Logger.LogDebug("Removing temp file...");
                        FileEx.Delete(tempFile);
                    }
                }

               
            }
            
            return build.number;
        }

     

        private static string TrimWhitespaceAndZipExtension(string artifactName)
        {
            string file = PathEx.GetFileName(artifactName).Trim();
            if (file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                return file.Substring(0, file.Length - ".zip".Length);
            else
                return file;
        }
    }
}

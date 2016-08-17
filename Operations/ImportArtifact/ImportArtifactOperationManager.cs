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
using Inedo.BuildMasterExtensions.TeamCity.Operations;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    /// <summary>
    /// Implements the work logic to retreive artifacts from a successful TeamCity build.
    /// Connection details for the API are passed by the calling class.
    /// </summary>
    internal sealed class ImportArtifactOperationManager
    {
        private ImportArtifactOperation Operation { get; }
        private IGenericBuildMasterContext Context { get; }

        public ImportArtifactOperationManager(ImportArtifactOperation operation, IGenericBuildMasterContext context)
        {
            if (context.ApplicationId == null)
                throw new InvalidOperationException("context requires a valid application ID");

            this.Operation = operation;
            this.Context = context;
        }

        public async Task<string> ImportAsync()
        {
            this.Operation.LogInformation($"Importing artifact \"{this.Operation.ArtifactName}\" from TeamCity...");

            TeamCityBuildType buildType = null;
            
            buildType = this.Operation.api.GetBuildType(this.Operation.BuildConfigurationId); // will raise an error if not found

            if (string.IsNullOrEmpty(this.Operation.BuildNumber))
            {
                this.Operation.LogDebug("BuildNumber was not specified in plan configuration, downloading the last successful build...");
                this.Operation.BuildNumber = "lastSuccessful";
            }

            var build = this.Operation.api.FindBuild(buildType.id, null, null, this.Operation.BranchName);

            var artifacts = this.Operation.api.GetBuildArtifacts(build.id); // the first isthe latest successful

            // remove artifacts not ending with .zip AND (if defined only keep the one specificied by this.ArtifactName (if not empty)
            foreach (var key in artifacts.Keys)
            {
                if (!string.IsNullOrEmpty(this.Operation.ArtifactName) && (string.Compare(key, this.Operation.ArtifactName, true) == 0))
                    artifacts.Remove(key);

                if (! key.ToLower().EndsWith(".zip")) // BM does not understand non-zip artifacts...sigh...
                    artifacts.Remove(key);

            }

            if (artifacts.Count == 0)
                throw new Exception($"Could not find artifact named '{this.Operation.ArtifactName}' in the list of available artifacts.");

            this.Operation.LogInformation($"Importing {artifacts.Count} TeamCity artifacts...");
            foreach (var artifact in artifacts)
            {
                this.Operation.LogInformation($"Importing artifact : {artifact.Key} ");

                string tempFile = null;
                try
                {
                    this.Operation.LogDebug("Downloading artifact from TeamCity...");
                    using (var client = new WebClient(this.Operation))
                    {
                        tempFile = Path.GetTempFileName();
                        this.Operation.LogDebug($"Saving temp file to \"{tempFile}\"...");
                        try
                        {
                            await client.DownloadFileTaskAsync(artifact.Value.Replace("/httpAuth/", ""), tempFile).ConfigureAwait(false);
                        }
                        catch (WebException wex)
                        {
                            var response = wex.Response as HttpWebResponse;
                            if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                                this.Operation.LogWarning("The TeamCity request returned a 404 - this could mean that the branch name, build number, or build configuration is invalid.");

                            throw;
                        }
                    }

                    this.Operation.LogDebug("Importing saved temp file into BuildMaster...");

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

                    this.Operation.LogInformation("Artifact imported.");
                }
                finally
                {
                    if (tempFile != null)
                    {
                        this.Operation.LogDebug("Removing temp file...");
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

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Inedo.Diagnostics;
using Inedo.ExecutionEngine.Executer;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.TeamCity;
using Inedo.Extensions.TeamCity.Credentials;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class TeamCityArtifactImporter
    {
        public string BuildConfigurationId { get; set; }
        public string ProjectName { get; set; }
        public string BuildConfigurationName { get; set; }
        public string ArtifactName { get; set; }
        public string BuildNumber { get; set; }
        public string BranchName { get; set; }

        private readonly TeamCitySecureResource resource;
        private readonly SecureCredentials credentials;
        public ILogSink Logger { get; }

        private readonly BuildMasterContextShim context;

        public TeamCityArtifactImporter(TeamCitySecureResource resource, SecureCredentials credentials, ILogSink logger, IOperationExecutionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            this.resource = resource ?? throw new ArgumentNullException(nameof(resource));
            this.credentials = credentials;
            this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.context = new BuildMasterContextShim(context);
        }

        public async Task<string> ImportAsync()
        {
            this.Logger.LogInformation($"Importing artifact \"{this.ArtifactName}\" from TeamCity...");

            if (this.BuildConfigurationId == null)
            {
                if (this.BuildConfigurationName != null && this.ProjectName != null)
                {
                    await SetBuildConfigurationIdFromName().ConfigureAwait(false);
                }
                else
                {
                    throw new ExecutionFailureException("If BuildConfigurationId is not specified directly, a project name and configuration name are required.");
                }
            }

            if (string.IsNullOrEmpty(this.BuildNumber))
            {
                this.Logger.LogDebug("BuildNumber was not specified, using lastSuccessful...");
                this.BuildNumber = "lastSuccessful";
            }

            string relativeUrl = $"repository/download/{this.BuildConfigurationId}/{this.BuildNumber}/{this.ArtifactName}";

            
            if (!string.IsNullOrEmpty(this.BranchName))
            {
                this.Logger.LogDebug("Branch name was specified: " + this.BranchName);
                relativeUrl += "?branch=" + Uri.EscapeDataString(this.BranchName);
            }

            string tempFile = null;
            try
            {
                using (var client = new TeamCityWebClient(this.resource, this.credentials))
                {
                    this.Logger.LogDebug($"Importing TeamCity artifact \"{this.ArtifactName}\" from {client.BaseAddress + relativeUrl}...");

                    tempFile = Path.GetTempFileName();
                    this.Logger.LogDebug($"Downloading temp file to \"{tempFile}\"...");
                    try
                    {
                        await client.DownloadFileTaskAsync(relativeUrl, tempFile).ConfigureAwait(false);
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
                using (var file = File.OpenRead(tempFile))
                {
                    await SDK.CreateArtifactAsync(
                        applicationId: (int)this.context.ApplicationId,
                        releaseNumber: this.context.ReleaseNumber,
                        buildNumber: this.context.BuildNumber,
                        deployableId: this.context.DeployableId,
                        executionId: this.context.ExecutionId,
                        artifactName: TrimWhitespaceAndZipExtension(this.ArtifactName),
                        artifactData: file,
                        overwrite: true
                    );
                }
            }
            finally
            {
                if (tempFile != null)
                {
                    this.Logger.LogDebug("Removing temp file...");
                    FileEx.Delete(tempFile);
                }
            }

            this.Logger.LogInformation(this.ArtifactName + " artifact imported.");
            
            return await this.GetActualBuildNumber().ConfigureAwait(false);
        }

        private async Task<string> GetActualBuildNumber()
        {
            this.Logger.LogDebug("Resolving actual build number...");

            string apiUrl = this.TryGetPredefinedConstantBuildNumberApiUrl(this.BuildNumber);
            if (apiUrl == null)
            {
                this.Logger.LogDebug("Using explicit build number: {0}", this.BuildNumber);
                return this.BuildNumber;
            }

            this.Logger.LogDebug("Build number is the predefined constant \"{0}\", resolving...", this.BuildNumber);

            try
            {
                if (this.BranchName != null)
                    apiUrl += ",branch:" + Uri.EscapeDataString(this.BranchName);

                using (var client = new TeamCityWebClient(this.resource, this.credentials))
                {
                    string xml = await client.DownloadStringTaskAsync(apiUrl).ConfigureAwait(false);
                    var doc = XDocument.Parse(xml);
                    return doc.Element("builds").Element("build").Attribute("number").Value;
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError("Could not parse actual build number from TeamCity. Exception details: " + ex);
                return null;
            }
        }

        private string TryGetPredefinedConstantBuildNumberApiUrl(string buildNumber)
        {
            if (string.Equals(buildNumber, "lastSuccessful", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/?locator=buildType:{0},running:false,status:success,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            if (string.Equals(buildNumber, "lastPinned", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/?locator=buildType:{0},running:false,pinned:true,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            if (string.Equals(buildNumber, "lastFinished", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/?locator=buildType:{0},running:false,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            return null;
        }

        private async Task SetBuildConfigurationIdFromName()
        {
            this.Logger.LogDebug("Attempting to resolve build configuration ID from project and name...");
            using (var client = new TeamCityWebClient(this.resource, this.credentials))
            {
                this.Logger.LogDebug("Downloading build types...");
                string result = await client.DownloadStringTaskAsync("app/rest/buildTypes").ConfigureAwait(false);
                var doc = XDocument.Parse(result);
                var buildConfigurations = from e in doc.Element("buildTypes").Elements("buildType")
                                          let buildType = new BuildType(e)
                                          where string.Equals(buildType.BuildConfigurationName, this.BuildConfigurationName, StringComparison.OrdinalIgnoreCase)
                                          let match = new
                                          {
                                              BuildType = buildType,
                                              Index = Array.FindIndex(buildType.ProjectNameParts, p => string.Equals(p, this.ProjectName, StringComparison.OrdinalIgnoreCase))
                                          }
                                          where match.Index > -1 || string.Equals(match.BuildType.ProjectName, this.ProjectName, StringComparison.OrdinalIgnoreCase)
                                          orderby match.Index
                                          select match.BuildType.BuildConfigurationId;
                
                this.BuildConfigurationId = buildConfigurations.FirstOrDefault();
                if (this.BuildConfigurationId == null)
                    throw new ExecutionFailureException($"Build configuration ID could not be found for project \"{this.ProjectName}\" and build configuration \"{this.BuildConfigurationName}\".");

                this.Logger.LogDebug("Build configuration ID resolved to: " + this.BuildConfigurationId);
            }
        }

        private static string TrimWhitespaceAndZipExtension(string artifactName)
        {
            string file = PathEx.GetFileName(artifactName).Trim();
            if (file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                return file.Substring(0, file.Length - ".zip".Length);
            else
                return file;
        }

        private sealed class BuildMasterContextShim
        {
            private readonly IOperationExecutionContext context;
            private readonly PropertyInfo[] properties;
            public BuildMasterContextShim(IOperationExecutionContext context)
            {
                // this is copied from Jenkins, and similarly is absolutely horrid, but works for backwards compatibility since this can only be used in BuildMaster
                this.context = context;
                this.properties = context.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            public int? ApplicationId => AH.ParseInt(this.GetValue());
            public int? DeployableId => AH.ParseInt(this.GetValue());
            public string ReleaseNumber => this.GetValue();
            public string BuildNumber => this.GetValue();
            public int ExecutionId => this.context.ExecutionId;
            private string GetValue([CallerMemberName] string name = null)
            {
                var prop = this.properties.FirstOrDefault(p => string.Equals(name, p.Name, StringComparison.OrdinalIgnoreCase));
                return prop?.GetValue(this.context)?.ToString();
            }
        }
    }
}

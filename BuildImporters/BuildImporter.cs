using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Xml.Linq;
using Inedo.Agents;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Artifacts;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Inedo.Diagnostics;
using Inedo.IO;
using Inedo.Serialization;
using System.Collections.Generic;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    [DisplayName("TeamCity")]
    [Description("Imports artifacts from a build in TeamCity.")]
    [BuildImporterTemplate(typeof(BuildImporterTemplate))]
    [CustomEditor(typeof(BuildImporterEditor))]
    public sealed class BuildImporter : BuildImporterBase, ICustomBuildNumberProvider
    {
        [Persistent]
        public string ArtifactName { get; set; }
        [Persistent]
        public string BuildConfigurationId { get; set; }
        [Persistent]
        public string BuildConfigurationDisplayName { get; set; }
        [Persistent]
        public string BuildNumber { get; set; }
        [Persistent]
        public string BranchName { get; set; }
        
        public IEnumerable<string> BranchNames2 { get; set; }

        string ICustomBuildNumberProvider.BuildNumber => GetActualBuildNumber(this.BuildNumber);

        public new Configurer GetExtensionConfigurer() => (Configurer)base.GetExtensionConfigurer();

        public override void Import(IBuildImporterContext context)
        {
            var configurer = this.GetExtensionConfigurer();
            var importer = new ImportArtifactOperationManager(configurer, this, context)
            {
                ArtifactName = this.ArtifactName,
                BranchName = this.GetBranchName(configurer),
                BuildConfigurationId = this.BuildConfigurationId,
                BuildNumber = this.BuildNumber
            };
            string teamCityBuildNumber = importer.ImportAsync().Result();
            
            this.LogDebug("TeamCity build number resolved to {0}, creating $TeamCityBuildNumber variable...", teamCityBuildNumber);

            DB.Variables_CreateOrUpdateVariableDefinition(
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
            );
        }

        private string GetActualBuildNumber(string buildNumber)
        {
            string apiUrl = this.TryGetPredefinedConstantBuildNumberApiUrl(buildNumber);
            if (apiUrl == null)
            {
                this.LogDebug("Using explicit build number: {0}", buildNumber);
                return buildNumber;
            }

            this.LogDebug("Build number is the predefined constant \"{0}\", resolving...", buildNumber);

            try
            {
                var configurer = this.GetExtensionConfigurer();
                string branch = this.GetBranchName(configurer);
                if (branch != null)
                    apiUrl += ",branch:" + Uri.EscapeDataString(branch);

                using (var client = new WebClient(configurer))
                {
                    string xml = client.DownloadString(apiUrl);
                    var doc = XDocument.Parse(xml);
                    return doc.Element("build").Attribute("number").Value;
                }
            }
            catch (Exception ex)
            {
                this.LogError("Could not parse actual build number from TeamCity. Exception details: {0}", ex);
                return null;
            }
        }

        private string TryGetPredefinedConstantBuildNumberApiUrl(string buildNumber)
        {
            if (string.Equals(buildNumber, "lastSuccessful", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/buildType:{0},running:false,status:success,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            if (string.Equals(buildNumber, "lastPinned", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/buildType:{0},running:false,pinned:true,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            if (string.Equals(buildNumber, "lastFinished", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/buildType:{0},running:false,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            return null;
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
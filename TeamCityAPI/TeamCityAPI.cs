using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.BuildMaster.Extensibility;
using Inedo.ExecutionEngine.Executer;
using System.Xml.Linq;
using System.Xml;

namespace Inedo.BuildMasterExtensions.TeamCity
{

    //public class TeamCityAPIContext
    //{
    //    public TeamCityBuildType BuildType { get; set; }
    //    public TeamCityBuild Build { get; set; }

    //    public string BuildConfigurationId { get; set; }
    //    public string ProjectName { get; set; }
    //    public string BuildConfigurationName { get; set; }
    //    public string BuildNumber { get; set; } // version
    //    public string BranchName { get; set; }
    //}

    internal class TeamCityAPI
    {

        public IConnectionInfo ConnectionInfo { get; }
        public ILogger Logger { get; }
        public IGenericBuildMasterContext BuildMasterContext { get; }

        public TeamCityAPI(IConnectionInfo connectionInfo, ILogger logger = null, IGenericBuildMasterContext context = null) {
            Logger = logger ?? Logger; // fallback on static class if null
            ConnectionInfo = connectionInfo;
            BuildMasterContext = context;
        }

        public List<TeamCityBuildType> GetBuildTypes()
        {
            var buildTypes = new List<TeamCity.TeamCityBuildType>();
            using (var client = new WebClient(this.ConnectionInfo))
            {

                string xml = client.DownloadString("app/rest/buildTypes");

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                foreach (XmlNode node in doc.SelectNodes("buildTypes/buildType"))
                {
                    buildTypes.Add(new TeamCityBuildType(node.OuterXml));
                }

            }
            return buildTypes;
        }

        public TeamCityBuildType GetBuildType(string buildTypeId)
        {
            var buildType = new TeamCityBuildType();
            using (var client = new WebClient(this.ConnectionInfo))
            {
                string getBuildTypeUrl = $"app/rest/buildTypes?locator=id:{buildTypeId}";

                string xml = client.DownloadString(getBuildTypeUrl);
                buildType.Update(xml, "/buildTypes/"); 
            }
            return buildType;
        }

        public TeamCityBuildType GetBuildTypeByName(string projectName, string buildTypeName)
        {
            var buildType = new TeamCityBuildType();
            this.Logger.LogDebug("Attempting to resolve build configuration ID from project and name...");
            using (var client = new WebClient(this.ConnectionInfo))
            {
                this.Logger.LogDebug("Downloading build types...");
                string getBuildTypeUrl = $"app/rest/buildTypes?locator=affectedProject:(name:{projectName}),name:{buildTypeName}";
                string xml = client.DownloadString(getBuildTypeUrl);
                
                buildType.Update(xml, "/buildTypes/"); // we suppose there is 1 only.... BAD

                if (buildType.id.Length == 0)
                    throw new ExecutionFailureException($"Build configuration ID could not be found for project \"{projectName}\" and build configuration \"{buildTypeName}\".");

                this.Logger.LogDebug("Build configuration ID resolved to: " + buildType.id);

            }
            return buildType;
        }

        public IEnumerable<string> GetBranches(string buildTypeId)
        {
            var builds = GetBuilds(buildTypeId);

            return builds.Distinct(b => b.branchName).Select(b => b.branchName);
        }

        public IEnumerable<TeamCityBuild> GetBuilds(string buildTypeId = null, string buildId = null, string buildNumber = null, string branchName = null)
        {
            var builds = new List<TeamCityBuild>();

            using (var client = new WebClient(this.ConnectionInfo))
            {
                string locator = "status:success,state:finished"; // pointless to retreive unsuccessful/unfinished build's artifacts...

                if (!string.IsNullOrEmpty(buildTypeId))
                    locator += $",buildType:{buildTypeId}";

                if (!string.IsNullOrEmpty(buildId))
                    locator += $",id:{buildId}";

                if (!string.IsNullOrEmpty(buildNumber))
                    locator += $",number:{buildNumber}";

                if (string.IsNullOrEmpty(branchName))
                    branchName = "default:any";
                locator += $",branch:{branchName}";

                string xml = client.DownloadString($"app/rest/builds/?locator={locator}");

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                foreach (XmlNode node in doc.SelectNodes("builds/build"))
                {
                    builds.Add(new TeamCityBuild(node.OuterXml));
                }

            }

            return builds;
        }

        public TeamCityBuild FindBuild(string buildTypeId = null, string buildId = null, string buildNumber = null, string branchName = null)
        {
            if (buildId == null && buildTypeId == null)
                throw new ExecutionFailureException("Failed to retreive artifacts : Missing build configuration ID or build ID");

            var builds = GetBuilds(buildTypeId, buildId, buildNumber, branchName);

            if (builds.Count() == 0)
                throw new ExecutionFailureException("No build found to retreive artifacts from.");

            return builds.First();
        }

        public Dictionary<string, string> GetBuildArtifacts(string buildId)
        {

            Dictionary<string, string> artifacts = new Dictionary<string, string>();

            using (var client = new WebClient(this.ConnectionInfo))
            {

                string xml = client.DownloadString($"app/rest/builds/id:{buildId}/artifacts/children/");
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                foreach(XmlNode node in doc.SelectNodes("/files/file"))
                {
                    artifacts.Add(node.Attributes["name"].Value, node.SelectSingleNode("content").Attributes["href"].Value);
                }
                
            }
            return artifacts;
        }

        internal string TryGetApiUrl(string tag, string buildTypeId)
        {
            if (tag ==  "lastSuccessful")
                return $"app/rest/builds/buildType:{buildTypeId},running:false,status:success,count:1";

            if (tag == "lastPinned")
                return $"app/rest/builds/buildType:{buildTypeId},running:false,pinned:true,count:1";

            if (tag == "lastFinished")
                return $"app/rest/builds/buildType:{buildTypeId},running:false,count:1";

            return null;
        }

    }
}

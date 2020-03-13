using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using System.Xml.Linq;
using Inedo.Extensibility.Credentials;
using Inedo.Extensions.TeamCity.Credentials;

namespace Inedo.Extensions.TeamCity
{
    internal sealed class TeamCityWebClient : WebClient
    {
        static TeamCityWebClient()
        {
            // ensure TLS v1.2 is supported
            ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | SecurityProtocolType.Tls12;
        }

        public TeamCityWebClient(TeamCitySecureResource resource, SecureCredentials secureCredentials)
        {
            string userName = null, password = null;
            if (secureCredentials is TeamCityAccountSecureCredentials asc)
            {
                userName = asc.UserName;
                password = AH.Unprotect(asc.Password);
            }
            else if (secureCredentials is TeamCityLegacyResourceCredentials lrc)
            {
                userName = lrc.UserName;
                password = AH.Unprotect(lrc.Password);
            }
            else if (secureCredentials is TeamCityTokenSecureCredentials tct)
            {
                this.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + AH.Unprotect(tct.Token));
            }
            else
                throw new ArgumentException(nameof(secureCredentials));

            this.BaseAddress = $"{resource.ServerUrl.TrimEnd('/')}/{(string.IsNullOrEmpty(userName) ? "guestAuth" : "httpAuth")}/";

            if (!string.IsNullOrEmpty(userName))
            {
                // Using a CredentialCache because API URLs with TeamCity variables in them will issue redirects
                // to the actual URLs, and unlike the NetworkCredential class, CredentialCache will ensure that the
                // credentials will be sent to the redirected URL as well
                var credentials = new CredentialCache();
                credentials.Add(new Uri(this.BaseAddress), "Basic", new NetworkCredential(userName, password));
                this.Credentials = credentials;
            }
        }

        public async Task<XDocument> DownloadXDocumentAsync(string url)
        {
            var xml = await this.DownloadStringTaskAsync(url).ConfigureAwait(false);
            return XDocument.Parse(xml);
        }

        public async Task<IList<string>> GetProjectNamesAsync()
        {
            var xdoc = await this.DownloadXDocumentAsync("app/rest/projects").ConfigureAwait(false);

            return xdoc
                .Element("projects")
                .Elements("project")
                .Select(e => (string)e.Attribute("name"))
                .ToList();
        }
        public async Task<IList<string>> GetQualifiedProjectNamesAsync()
        {
            var xdoc = await this.DownloadXDocumentAsync("app/rest/projects?fields=project(id,name,parentProjectId,buildTypes(buildType(projectName)))").ConfigureAwait(false);

            var projects = xdoc.Element("projects").Elements("project");

            // Assume double colons if any project names have them; otherwise, use slashes.
            var separator = projects.SelectMany(p => p.Element("buildTypes").Elements("buildType"))
                .All(bt => !bt.Attribute("projectName").Value.Contains(" :: ")) ? " :: " : " / ";

            return projects.Select(p => p.Element("buildTypes").Elements("buildType").Select(bt => bt.Attribute("projectName").Value).FirstOrDefault()
                ?? GetQualifiedProjectName(projects, p.Attribute("parentProjectId"), (string)p.Attribute("name"), separator)).ToList();
        }
        private static string GetQualifiedProjectName(IEnumerable<XElement> projects, XAttribute parentId, string name, string separator = " :: ")
        {
            if (parentId == null)
                return name;

            var parent = projects.FirstOrDefault(p => p.Attribute("id")?.Value == parentId.Value);
            if (parent == null)
                return name;

            var parentParentId = parent.Attribute("parentProjectId");
            if (parentParentId == null) // Don't put <Root project> before all project names.
                return name;

            return GetQualifiedProjectName(projects, parentParentId, parent.Attribute("name").Value + separator + name);
        }
        public async Task<IList<string>> GetBuildTypeNamesAsync(string projectName)
        {
            var xdoc = await this.DownloadXDocumentAsync("app/rest/buildTypes").ConfigureAwait(false);

            return xdoc
                .Element("buildTypes")
                .Elements("buildType")
                .Where(e => string.Equals(projectName, (string)e.Attribute("projectName"), StringComparison.OrdinalIgnoreCase))
                .Select(e => (string)e.Attribute("name"))
                .ToList();
        }

        public async Task<IList<TeamCityBuildConfiguration>> GetBuildTypesAsync()
        {
            var xdoc = await this.DownloadXDocumentAsync("app/rest/buildTypes").ConfigureAwait(false);

            return xdoc
                .Element("buildTypes")
                .Elements("buildType")
                .Select(e => new TeamCityBuildConfiguration
                {
                    Id = (string)e.Attribute("id"),
                    Project = (string)e.Attribute("projectName"),
                    Name = (string)e.Attribute("name")
                })
                .ToList();
        }
        public async Task<IList<string>> GetBuildNumbersAsync(string projectName, string buildConfigurationName)
        {
            var builtInTypes = new[] { "lastSuccessful", "lastPinned", "lastFinished" };

            var buildTypes = await this.GetBuildTypesAsync().ConfigureAwait(false);
            var buildTypeId = buildTypes
                .FirstOrDefault(c => string.Equals(projectName, c.Project, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(buildConfigurationName, c.Name, StringComparison.OrdinalIgnoreCase))
                ?.Id;
            if (string.IsNullOrEmpty(buildTypeId))
                return builtInTypes;

            var xdoc = await this.DownloadXDocumentAsync($"app/rest/buildTypes/id:{Uri.EscapeDataString(buildTypeId)}/builds").ConfigureAwait(false);
            var buildNumbers = xdoc
                .Element("builds")
                .Elements("build")
                .Select(e => (string)e.Attribute("number"));

            return builtInTypes.Concat(buildNumbers).ToList();
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            
            if (request.Method == "POST")
                request.ContentType = "application/xml";

            return request;
        }
    }
}

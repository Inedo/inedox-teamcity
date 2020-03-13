using System;
using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.TeamCity.SuggestionProviders;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.Credentials
{
    [DisplayName("TeamCity Project")]
    [Description("Connect to a TeamCity project for importing artifacts and queueing builds")]
    public sealed class TeamCitySecureResource : SecureResource<TeamCityAccountSecureCredentials,TeamCityTokenSecureCredentials>
    {
        [Required]
        [Persistent]
        [DisplayName("TeamCity server URL")]
        public string ServerUrl { get; set; }

        [Persistent]
        [SuggestableValue(typeof(ProjectNameSuggestionProvider))]
        [DisplayName("Project name")]
        public string ProjectName { get; set; }

        public override RichDescription GetDescription()
        {
            var host = AH.CoalesceString(this.ServerUrl, "(unknown)");
            if (!string.IsNullOrWhiteSpace(this.ServerUrl) && Uri.TryCreate(this.ServerUrl, UriKind.Absolute, out var uri))
                host = uri.Host;

            return string.IsNullOrEmpty(this.ProjectName)
                ? new RichDescription(host)
                : new RichDescription($"{ this.ProjectName } @ { host }");
        }
    }
}

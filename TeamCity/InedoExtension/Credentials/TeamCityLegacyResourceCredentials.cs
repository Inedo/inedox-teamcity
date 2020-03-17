using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.ListVariableSources;
using Inedo.Extensibility.SecureResources;
using Inedo.Serialization;
using Inedo.Web;
using Inedo.Web.Plans;

namespace Inedo.Extensions.TeamCity.Credentials
{
    [ScriptAlias(TeamCityLegacyResourceCredentials.TypeName)]
    [DisplayName("TeamCity")]
    [Description("Credentials for TeamCity.")]
    [PersistFrom("Inedo.BuildMasterExtensions.TeamCity.Credentials.TeamCityCredentials,TeamCity")]
    [PersistFrom("Inedo.Extensions.TeamCity.Credentials.TeamCityCredentials,TeamCity")]
    public sealed class TeamCityLegacyResourceCredentials : ResourceCredentials
    {
        public const string TypeName = "TeamCity";

        [Required]
        [Persistent]
        [DisplayName("TeamCity server URL")]
        public string ServerUrl { get; set; }

        [Persistent]
        [DisplayName("User name")]
        [PlaceholderText("Use guest authentication")]
        public string UserName { get; set; }

        [Persistent(Encrypted = true)]
        [DisplayName("Password")]
        [FieldEditMode(FieldEditMode.Password)]
        public SecureString Password { get; set; }

        public override RichDescription GetDescription() => new RichDescription(string.IsNullOrEmpty(this.UserName) ? "Guest" : this.UserName);

        public override SecureCredentials ToSecureCredentials() => 
            string.IsNullOrEmpty(this.UserName) 
                ? null 
            : new TeamCityAccountSecureCredentials { UserName = this.UserName, Password = this.Password };

        public override SecureResource ToSecureResource() => new TeamCitySecureResource { ServerUrl = this.ServerUrl };
    }
}

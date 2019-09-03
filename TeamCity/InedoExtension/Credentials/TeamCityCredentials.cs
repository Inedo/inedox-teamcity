using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.ListVariableSources;
using Inedo.Serialization;
using Inedo.Web;
using Inedo.Web.Plans;

namespace Inedo.Extensions.TeamCity.Credentials
{
    [ScriptAlias(TeamCityCredentials.TypeName)]
    [DisplayName("TeamCity")]
    [Description("Credentials for TeamCity.")]
    [PersistFrom("Inedo.BuildMasterExtensions.TeamCity.Credentials.TeamCityCredentials,TeamCity")]
    public sealed class TeamCityCredentials : ResourceCredentials, ITeamCityConnectionInfo
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

        string ITeamCityConnectionInfo.Password
        {
            get
            {
                var ptr = Marshal.SecureStringToGlobalAllocUnicode(this.Password);
                try
                {
                    return Marshal.PtrToStringUni(ptr);
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }
            }
        }

        public override RichDescription GetDescription() => new RichDescription(string.IsNullOrEmpty(this.UserName) ? "Guest" : this.UserName);

        internal static TeamCityCredentials TryCreate(string name, ValueEnumerationContext context)
        {
            return (TeamCityCredentials)ResourceCredentials.TryCreate(TeamCityCredentials.TypeName, name, environmentId: null, applicationId: context.ProjectId, inheritFromParent: false);
        }

        internal static TeamCityCredentials TryCreate(string name, IComponentConfiguration config)
        {
            int? projectId = (config.EditorContext as IOperationEditorContext)?.ProjectId ?? AH.ParseInt(AH.CoalesceString(config["ProjectId"], config["ApplicationId"]));
            int? environmentId = AH.ParseInt(config["EnvironmentId"]);

            return (TeamCityCredentials)ResourceCredentials.TryCreate(TeamCityCredentials.TypeName, name, environmentId: environmentId, applicationId: projectId, inheritFromParent: false);
        }
    }
}

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.Credentials
{
    [ScriptAlias("TeamCity")]
    [DisplayName("TeamCity")]
    [Description("Credentials for TeamCity.")]
    [PersistFrom("Inedo.BuildMasterExtensions.TeamCity.Credentials.TeamCityCredentials,TeamCity")]
    public sealed class TeamCityCredentials : ResourceCredentials, ITeamCityConnectionInfo
    {
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
    }
}

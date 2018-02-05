using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.TeamCity.Credentials
{
    [ScriptAlias("TeamCity")]
    [DisplayName("TeamCity")]
    [Description("Credentials for TeamCity.")]
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.TeamCity.Credentials
{
    /// <summary>
    /// This class provides the UI building logic to enter connection details as Resource Credential 
    /// in replacement for legacy configuration profiles.
    /// Web : /administration/credentials
    /// </summary>
    [ScriptAlias("TeamCity")]
    [DisplayName("TeamCity")]
    [Description("Credentials for TeamCity.")]
    public sealed class Credentials : ResourceCredentials
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

        public override RichDescription GetDescription()
        {
            return new RichDescription(string.IsNullOrEmpty(this.UserName) ? "Guest" : this.UserName);
        }

    }
}

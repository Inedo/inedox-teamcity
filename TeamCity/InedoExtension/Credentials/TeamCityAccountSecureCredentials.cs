using System.ComponentModel;
using System.Security;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.Credentials
{
    [DisplayName("TeamCity Account")]
    [Description("Use a username/password to connect a TeamCity server")]
    public sealed class TeamCityAccountSecureCredentials : SecureCredentials
    {
        [Persistent]
        [DisplayName("User name")]
        [Required]
        public string UserName { get; set; }

        [Persistent(Encrypted = true)]
        [DisplayName("Password or Personal access token")]
        [FieldEditMode(FieldEditMode.Password)]
        [Required]
        public SecureString Password { get; set; }

        public override RichDescription GetDescription() => new RichDescription(this.UserName);
    }
}

using System.ComponentModel;
using System.Security;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.Credentials
{
    [DisplayName("TeamCity Personal Access Token")]
    [Description("Use a personal access token to connect a TeamCity server")]
    public sealed class TeamCityTokenSecureCredentials : SecureCredentials
    {
        [Persistent(Encrypted = true)]
        [DisplayName("Personal access token")]
        [FieldEditMode(FieldEditMode.Password)]
        [Required]
        public SecureString Token { get; set; }

        public override RichDescription GetDescription() => new RichDescription("(secret)");
    }
}

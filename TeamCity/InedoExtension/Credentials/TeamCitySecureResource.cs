using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility.SecureResources;
using Inedo.Serialization;

namespace Inedo.Extensions.TeamCity.Credentials
{
    [DisplayName("TeamCity Project")]
    [Description("Connect to a TeamCity project to queue or import builds.")]
    public sealed class TeamCitySecureResource : SecureResource<Extensions.Credentials.UsernamePasswordCredentials>
    {
        [Required]
        [Persistent]
        [DisplayName("TeamCity server URL")]
        public string ServerUrl { get; set; }

        public override RichDescription GetDescription() => new RichDescription(this.ServerUrl);
    }
}

using System.ComponentModel;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.TeamCity.Credentials;
using Inedo.Documentation;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.TeamCity.Operations
{
    /// <summary>
    /// Defines a basic plan operation which holds TeamCity API httpAuth credentials information
    /// </summary>
    public abstract class Operation : ExecuteOperation, IHasCredentials<Credentials.Credentials>, IConnectionInfo
    {
        [Category("Connection/Identity")]
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        [PlaceholderText("Select a resource credential or enter details below")]
        public string CredentialName { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Server")]
        [DisplayName("TeamCity server URL")]
        [MappedCredential(nameof(Credentials.Credentials.ServerUrl))]
        [PlaceholderText("http://teamcity.mydomain.com")]
        public string ServerUrl { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("UserName")]
        [DisplayName("User name")]
        [PlaceholderText("If left blank, will use guest authentication")]
        [MappedCredential(nameof(Credentials.Credentials.UserName))]
        public string UserName { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Password")]
        [DisplayName("Password")]
        [MappedCredential(nameof(Credentials.Credentials.Password))]
        [PlaceholderText("If left blank, will use guest authentication")]
        [CustomEditor(typeof(PasswordArgumentEditor))]
        public string Password { get; set; }

        internal TeamCityAPI api => new TeamCityAPI(this, this);
    }
}

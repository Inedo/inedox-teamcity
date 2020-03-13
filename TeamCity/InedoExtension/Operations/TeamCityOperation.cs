using System.ComponentModel;
using Inedo.Extensions.TeamCity.Credentials;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Web;
using System.Security;
using Inedo.Extensibility.SecureResources;

namespace Inedo.Extensions.TeamCity.Operations
{
    public abstract class TeamCityOperation : ExecuteOperation
    {
        [DisplayName("From resource")]
        [ScriptAlias("From")]
        [ScriptAlias("Credentials")]
        [SuggestableValue(typeof(SecureResourceSuggestionProvider<TeamCitySecureResource>))]
        public string ResourceName { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Server")]
        [DisplayName("TeamCity server URL")]
        public string ServerUrl { get; set; }

        [Category("Connection/Identity")]
        [DisplayName("Credentials")]
        [ScriptAlias("CredentialName")]
        [PlaceholderText("Use credential from resource")]
        public string CredentialName { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("UserName")]
        [DisplayName("User name")]
        [PlaceholderText("Use user name from credential")]
        public string UserName { get; set; }

        [Category("Connection/Identity")]
        [ScriptAlias("Password")]
        [DisplayName("Password")]
        [PlaceholderText("Use password from credential")]
        public string Password { get; set; }

        protected (TeamCitySecureResource resource, SecureCredentials secureCredentials) GetConnectionInfo(IOperationExecutionContext context)
        {
            TeamCitySecureResource resource = null;  
            SecureCredentials credentials = null;
            
            if (!string.IsNullOrEmpty(this.ResourceName))
            {
                resource = SecureResource.TryCreate(this.ResourceName, context as IResourceResolutionContext) as TeamCitySecureResource;
                if (resource == null)
                {
                    var legacy = ResourceCredentials.TryCreate<TeamCityLegacyResourceCredentials>(this.ResourceName);
                    resource = legacy?.ToSecureResource() as TeamCitySecureResource;
                    credentials = legacy?.ToSecureCredentials();
                }
            }
            if (!string.IsNullOrEmpty(this.CredentialName))
            {
                credentials = SecureCredentials.TryCreate(this.CredentialName, context as ICredentialResolutionContext) as SecureCredentials;
            }
            if (!string.IsNullOrEmpty(this.UserName) && !string.IsNullOrEmpty(this.Password))
            {
                credentials = new TeamCityAccountSecureCredentials
                {
                    UserName = this.UserName,
                    Password = AH.CreateSecureString(this.Password)
                };
            }

            return (new TeamCitySecureResource()
            {
                ServerUrl = AH.CoalesceString(this.ServerUrl, resource?.ServerUrl)
            }, credentials);
        }
    }
}

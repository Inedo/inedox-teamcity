using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.TeamCity.Credentials;

namespace Inedo.Extensions.TeamCity.Operations;

public abstract class TeamCityOperation : ExecuteOperation
{
    public abstract string? ResourceName { get; set; }
    public abstract string? ProjectName { get; set; }
    public abstract string? BuildConfigurationName { get; set; }

    [Category("Connection/Identity")]
    [DisplayName("Credentials")]
    [ScriptAlias("CredentialName")]
    [PlaceholderText("Use credential from resource")]
    public string? CredentialName { get; set; }
    [Category("Connection/Identity")]
    [ScriptAlias("Server")]
    [PlaceholderText("Use server URL from credential")]
    [DisplayName("TeamCity server URL")]
    public string? ServerUrl { get; set; }
    [Category("Connection/Identity")]
    [ScriptAlias("Token"), ScriptAlias("Password", Obsolete = true)]
    [DisplayName("Token")]
    [PlaceholderText("Use token from credential")]
    public SecureString? Password { get; set; }

    [ScriptAlias("UserName", Obsolete = true)]
    public string? UserName { get; set; }

    internal bool TryCreateClient(IOperationExecutionContext context, [NotNullWhen(true)] out TeamCityClient? client)
    {
        var project = SecureResource.TryCreate(SecureResourceType.CIProject, this.ResourceName, context) as TeamCityProject;

        var credentialName = this.CredentialName ?? project?.CredentialName;

        if (!TeamCityCredentials.TryCreateFromCredentialName(credentialName, context, out var credentials))
            credentials = new();

        if (!string.IsNullOrEmpty(this.UserName))
        {
            client = null;
            this.LogWarning("A UserName was specified for TeamCity, which is no longer supported; you'll need to switch to API Tokens.");
            return false;
        }

        credentials.Password = this.Password ?? credentials.Password;
        credentials.ServiceUrl = this.ServerUrl ?? credentials.ServiceUrl ?? project?.LegacyServerUrl;
        if (string.IsNullOrEmpty(credentials.ServiceUrl))
        {
            client = null;
            this.LogWarning("A ServiceUrl was not specified for TeamCity.");
            return false;
        }

        client = new TeamCityClient(credentials, this);
        return true;
    }
}

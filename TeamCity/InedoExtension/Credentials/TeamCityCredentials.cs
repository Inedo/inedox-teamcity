using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility.CIServers;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.Credentials;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.Credentials;

[DisplayName("TeamCity Server")]
[Description("Credentials for TeamCity.")]
[PersistFrom("Inedo.BuildMasterExtensions.TeamCity.Credentials.TeamCityCredentials,TeamCity")]
[PersistFrom("Inedo.Extensions.TeamCity.Credentials.TeamCityLegacyResourceCredentials,TeamCity")]
[PersistFrom("Inedo.Extensions.TeamCity.Credentials.TeamCityAccountSecureCredentials,TeamCity")]
[PersistFrom("Inedo.Extensions.TeamCity.Credentials.TeamCityTokenSecureCredentials,TeamCity")]
public sealed class TeamCityCredentials : CIServiceCredentials<TeamCityService>, IMissingPersistentPropertyHandler
{
    public string? LegacyUserName;

    [Persistent(Encrypted = true)]
    [Required]
    [DisplayName("Personal access token")]
    [FieldEditMode(FieldEditMode.Password)]
    public override SecureString? Password { get; set; }

    public override RichDescription GetCredentialDescription() => new("(secret)");
    public override RichDescription GetServiceDescription() => new(this.TryGetServiceUrlHostName(out var hostName) ? hostName : this.ServiceUrl);

    internal static bool TryCreateFromCredentialName(string? credentialName, ICredentialResolutionContext? context, [NotNullWhen(true)] out TeamCityCredentials? credentials)
    {
        credentials =
            TryCreate(credentialName, context) switch
            {
                TeamCityCredentials j => j,
                TokenCredentials t => new() { Password = t.Token },
                UsernamePasswordCredentials u => new() { UserName = u.UserName, Password = u.Password },
                _ => new()
            };
        return credentials != null;
    }
    internal static bool TryCreateFromResourceName(string? resourceName, ICredentialResolutionContext? context, [NotNullWhen(true)] out TeamCityCredentials? credentials)
        => TryCreateFromCredentialName(SecureResource.TryCreate(resourceName, context)?.CredentialName, context, out credentials);

    void IMissingPersistentPropertyHandler.OnDeserializedMissingProperties(IReadOnlyDictionary<string, string> missingProperties)
    {
        if (missingProperties.ContainsKey("ServerUrl"))
            this.ServiceUrl = missingProperties["ServerUrl"];
        if (missingProperties.ContainsKey("UserName"))
            this.LegacyUserName = missingProperties["UserName"];
        if (missingProperties.ContainsKey("Token"))
            this.Password = AH.CreateSecureString(missingProperties["Token"]);
    }
}
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.ListVariableSources;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.TeamCity.Credentials;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.ListVariableSources
{
    [DisplayName("TeamCity Project Name")]
    [Description("Project names from a specified TeamCity instance.")]
    public sealed class TeamCityProjectNameVariableSource : ListVariableSource, IMissingPersistentPropertyHandler
    {
        [Persistent]
        [DisplayName("From resource")]
        [SuggestableValue(typeof(SecureResourceSuggestionProvider<TeamCitySecureResource>))]
        [Required]
        public string ResourceName { get; set; }
        
        public override async Task<IEnumerable<string>> EnumerateValuesAsync(ValueEnumerationContext context)
        {
            var rrContext = new CredentialResolutionContext(context.ProjectId, null);
            var resource = SecureResource.TryCreate(this.ResourceName, rrContext) as TeamCitySecureResource;
            if (resource == null)
                return Enumerable.Empty<string>();

            using (var client = new TeamCityWebClient(resource, resource.GetCredentials(rrContext)))
            {
                return await client.GetProjectNamesAsync().ConfigureAwait(false);
            }
        }

        public override RichDescription GetDescription() =>
            new RichDescription("TeamCity (", new Hilite(this.ResourceName), ") ", " project names.");

        void IMissingPersistentPropertyHandler.OnDeserializedMissingProperties(IReadOnlyDictionary<string, string> missingProperties)
        {
            if (missingProperties.ContainsKey("CredentialName"))
                this.ResourceName = missingProperties["CredentialName"];
        }
    }
}

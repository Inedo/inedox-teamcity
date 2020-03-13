using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.TeamCity.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.SuggestionProviders
{
    internal class ProjectNameSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var resourceName = config["ResourceName"];
            if (string.IsNullOrEmpty(resourceName))
                return Enumerable.Empty<string>();

            var rrContext = config.EditorContext as ICredentialResolutionContext;
            var resource = SecureResource.TryCreate(resourceName, rrContext) as TeamCitySecureResource;
            if (resource == null)
                return Enumerable.Empty<string>();

            using (var client = new TeamCityWebClient(resource, resource.GetCredentials(rrContext)))
            {
                return await client.GetQualifiedProjectNamesAsync().ConfigureAwait(false);
            }
        }
    }
}
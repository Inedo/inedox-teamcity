using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.TeamCity.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.SuggestionProviders;

internal class BuildConfigurationNameSuggestionProvider : ISuggestionProvider
{
    public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
    {
        var resourceName = config["ResourceName"];
        if (string.IsNullOrEmpty(resourceName))
            return Enumerable.Empty<string>();

        var projectName = config["ProjectName"];
        if (string.IsNullOrEmpty(projectName))
            return Enumerable.Empty<string>();

        if (!TeamCityCredentials.TryCreateFromResourceName(resourceName, config.EditorContext as ICredentialResolutionContext, out var credentials))
            return Enumerable.Empty<string>();
#warning Look-up ProjectId?
        var list = await new TeamCityClient(credentials).GetProjectBuildTypesAsync(projectName).Select(b => b.Name).ToListAsync().ConfigureAwait(false);
        return list.AsEnumerable();
    }
}
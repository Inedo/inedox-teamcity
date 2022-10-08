using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.TeamCity.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.SuggestionProviders;

internal class BuildNumberSuggestionProvider : ISuggestionProvider
{
    public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
    {
        var resourceName = config["ResourceName"];
        if (string.IsNullOrEmpty(resourceName))
            return TeamCityClient.builtInTypes.AsEnumerable();

        var projectName = config["ProjectName"];
        if (string.IsNullOrEmpty(projectName))
            return TeamCityClient.builtInTypes.AsEnumerable();

        var buildConfigurationName = config["BuildConfigurationName"];
        if (string.IsNullOrEmpty(projectName))
            return TeamCityClient.builtInTypes.AsEnumerable();

        if (!TeamCityCredentials.TryCreateFromResourceName(resourceName, config.EditorContext as ICredentialResolutionContext, out var credentials))
            return TeamCityClient.builtInTypes.AsEnumerable();

        try
        {
#warning Look-up ProjectId?
            var list = await new TeamCityClient(credentials).GetBuildsAsync(projectName).Select(s => s.Number).ToListAsync().ConfigureAwait(false);
            return TeamCityClient.builtInTypes.Concat(list.AsEnumerable());
        }
        catch
        {
            return TeamCityClient.builtInTypes.AsEnumerable();
        }
    }
}
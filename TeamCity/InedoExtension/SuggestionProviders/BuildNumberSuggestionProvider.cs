using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensions.TeamCity.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.SuggestionProviders
{
    internal class BuildNumberSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var credentialName = config["CredentialName"];
            if (string.IsNullOrEmpty(credentialName))
                return Enumerable.Empty<string>();

            var projectName = config["ProjectName"];
            if (string.IsNullOrEmpty(projectName))
                return Enumerable.Empty<string>();

            var buildConfigurationName = config["BuildConfigurationName"];
            if (string.IsNullOrEmpty(projectName))
                return Enumerable.Empty<string>();

            var credentials = TeamCityCredentials.TryCreate(credentialName, config);
            if (credentials == null)
                return Enumerable.Empty<string>();

            using (var client = new TeamCityWebClient(credentials))
            {
                return await client.GetBuildNumbersAsync(projectName, buildConfigurationName).ConfigureAwait(false);
            }
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.TeamCity.Credentials;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal class ProjectNameSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var credentialName = config["CredentialName"];
            if (string.IsNullOrEmpty(credentialName))
                return Enumerable.Empty<string>();

            var credentials = ResourceCredentials.Create<TeamCityCredentials>(credentialName);
            using (var client = new TeamCityWebClient(credentials))
            {
                return await client.GetQualifiedProjectNamesAsync().ConfigureAwait(false);
            }
        }
    }
}
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Extensibility;

namespace Inedo.Extensions.TeamCity.SuggestionProviders;

internal class ProjectNameSuggestionProvider : TeamCitySuggestionProvider
{
    protected override async IAsyncEnumerable<string> GetSuggestionsAsync(TeamCityClient client, IComponentConfiguration config, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var p in client.GetProjectsAsync(cancellationToken).ConfigureAwait(false))
        {
            if (p.DisplayName != null)
                yield return p.DisplayName;
        }
    }
}
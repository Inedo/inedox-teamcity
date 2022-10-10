using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Extensibility;

namespace Inedo.Extensions.TeamCity.SuggestionProviders;

internal sealed class BuildConfigurationNameSuggestionProvider : TeamCitySuggestionProvider
{
    protected override async IAsyncEnumerable<string> GetSuggestionsAsync(TeamCityClient client, IComponentConfiguration config, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var projectName = config["ProjectName"];
        if (string.IsNullOrEmpty(projectName))
            yield break;

        await foreach (var p in client.GetProjectBuildTypesAsync(projectName, cancellationToken).ConfigureAwait(false))
            yield return p.Name;
    }
}
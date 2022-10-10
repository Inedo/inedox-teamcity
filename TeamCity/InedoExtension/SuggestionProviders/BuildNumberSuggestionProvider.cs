using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Extensibility;

namespace Inedo.Extensions.TeamCity.SuggestionProviders;

internal class BuildNumberSuggestionProvider : TeamCitySuggestionProvider
{
    protected override IEnumerable<string> DefaultResults => TeamCityClient.BuiltInTypes;

    protected override async IAsyncEnumerable<string> GetSuggestionsAsync(TeamCityClient client, IComponentConfiguration config, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var projectName = config["ProjectName"];
        if (string.IsNullOrEmpty(projectName))
            yield break;

        await foreach (var b in client.GetBuildsAsync(projectName, cancellationToken).ConfigureAwait(false))
            yield return b.Number;
    }
}
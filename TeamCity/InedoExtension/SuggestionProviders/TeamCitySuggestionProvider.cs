using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensions.TeamCity.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.SuggestionProviders;

internal abstract class TeamCitySuggestionProvider : ISuggestionProvider
{
    protected TeamCitySuggestionProvider()
    {
    }

    protected virtual IEnumerable<string> DefaultResults => Enumerable.Empty<string>();

    protected abstract IAsyncEnumerable<string> GetSuggestionsAsync(TeamCityClient client, IComponentConfiguration config, CancellationToken cancellationToken);

    private async IAsyncEnumerable<string> GetSuggestionsInternalAsync(string startsWith, IComponentConfiguration config, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var r in this.DefaultResults)
        {
            if (string.IsNullOrEmpty(startsWith) || r.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
                yield return r;
        }

        var resourceName = config["ResourceName"];
        if (string.IsNullOrEmpty(resourceName))
            yield break;

        if (!TeamCityCredentials.TryCreateFromResourceName(resourceName, config.EditorContext as ICredentialResolutionContext, out var credentials))
            yield break;

        await foreach (var s in this.GetSuggestionsAsync(new TeamCityClient(credentials), config, cancellationToken).ConfigureAwait(false))
        {
            if (s == null)
                continue;

            if (string.IsNullOrEmpty(startsWith) || s.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
                yield return s;
        }
    }

    IAsyncEnumerable<string> ISuggestionProvider.GetSuggestionsAsync(string startsWith, IComponentConfiguration config, CancellationToken cancellationToken)
    {
        return this.GetSuggestionsInternalAsync(startsWith, config, cancellationToken);
    }
    async Task<IEnumerable<string>> ISuggestionProvider.GetSuggestionsAsync(IComponentConfiguration config)
    {
        var list = new List<string>();
        await foreach (var s in this.GetSuggestionsInternalAsync(string.Empty, config, default).ConfigureAwait(false))
            list.Add(s);
        return list;
    }
}

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility.VariableTemplates;
using Inedo.Extensions.TeamCity.Credentials;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.ListVariableSources;

[DisplayName("TeamCity Project Name")]
[Description("Project names from a specified TeamCity instance.")]
public sealed class TeamCityProjectNameVariableSource : DynamicListVariableType, IMissingPersistentPropertyHandler
{
    [Persistent]
    [DisplayName("From resource")]
    [SuggestableValue(typeof(SecureResourceSuggestionProvider<TeamCityProject>))]
    [Required]
    public string? ResourceName { get; set; }

    public override async Task<IEnumerable<string>> EnumerateListValuesAsync(VariableTemplateContext context)
    {
        if (!TeamCityCredentials.TryCreateFromResourceName(this.ResourceName, context, out var credentials))
            return Enumerable.Empty<string>();

        var list = new List<string>();
        await foreach (var p in new TeamCityClient(credentials).GetProjectsAsync().ConfigureAwait(false))
        {
            if (!string.IsNullOrEmpty(p.DisplayName))
                list.Add(p.DisplayName);
        }

        return list;
    }

    public override RichDescription GetDescription()
    {
        return new RichDescription("TeamCity (", new Hilite(this.ResourceName), ") ", " project names.");
    }

    void IMissingPersistentPropertyHandler.OnDeserializedMissingProperties(IReadOnlyDictionary<string, string> missingProperties)
    {
        if (missingProperties.ContainsKey("CredentialName"))
            this.ResourceName = missingProperties["CredentialName"];
    }
}

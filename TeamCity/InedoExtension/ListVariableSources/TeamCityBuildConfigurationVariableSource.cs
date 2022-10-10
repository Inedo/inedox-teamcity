using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility.VariableTemplates;
using Inedo.Extensions.TeamCity.Credentials;
using Inedo.Extensions.TeamCity.SuggestionProviders;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.ListVariableSources;

[DisplayName("TeamCity Build Configuration")]
[Description("Build configurations in a specified project in a TeamCity instance.")]
public sealed class TeamCityBuildConfigurationVariableSource : DynamicListVariableType
{
    [Persistent]
    [DisplayName("From resource")]
    [SuggestableValue(typeof(SecureResourceSuggestionProvider<TeamCityProject>))]
    [Required]
    public string? ResourceName { get; set; }

    [Persistent]
    [DisplayName("Project name")]
    [SuggestableValue(typeof(ProjectNameSuggestionProvider))]
    [Required]
    public string? ProjectName { get; set; }

    public override async Task<IEnumerable<string>> EnumerateListValuesAsync(VariableTemplateContext context)
    {
        if (!TeamCityCredentials.TryCreateFromResourceName(this.ResourceName, context, out var credentials))
            return Enumerable.Empty<string>();

        if (string.IsNullOrEmpty(this.ProjectName))
            return Enumerable.Empty<string>();

        var list = new List<string>();
        await foreach (var p in new TeamCityClient(credentials).GetProjectBuildTypesAsync(this.ProjectName).ConfigureAwait(false))
            list.Add(p.Name);

        return list;
    }

    public override RichDescription GetDescription()
    {
        return new RichDescription("TeamCity (", new Hilite(this.ResourceName), ") ", " build configurations in ", new Hilite(this.ProjectName), ".");
    }
}

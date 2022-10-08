using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.SecureResources;
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

#warning Look-up ProjectId?
        var list = await new TeamCityClient(credentials).GetProjectBuildTypesAsync(this.ProjectName!).Select(b => b.Name).ToListAsync().ConfigureAwait(false);
        return list.AsEnumerable();
    }

    public override RichDescription GetDescription() =>
        new RichDescription("TeamCity (", new Hilite(this.ResourceName), ") ", " build configurations in ", new Hilite(this.ProjectName), ".");
}

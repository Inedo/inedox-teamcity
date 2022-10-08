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

[DisplayName("TeamCity Build Number")]
[Description("Build numbers from a specified build configuration in a TeamCity instance.")]
public sealed class TeamCityBuildNumberVariableSource : DynamicListVariableType
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

    [Persistent]
    [DisplayName("Build configuration")]
    [SuggestableValue(typeof(BuildConfigurationNameSuggestionProvider))]
    [Required]
    public string? BuildConfigurationName { get; set; }


    public override async Task<IEnumerable<string>> EnumerateListValuesAsync(VariableTemplateContext context)
    {
        if (!TeamCityCredentials.TryCreateFromResourceName(this.ResourceName, context, out var credentials))
            return TeamCityClient.builtInTypes.AsEnumerable();

        try
        {
#warning Look-up ProjectId?
            var list = await new TeamCityClient(credentials).GetBuildsAsync(this.ProjectName!).Select(s => s.Number).ToListAsync().ConfigureAwait(false);
            return TeamCityClient.builtInTypes.Concat(list.AsEnumerable());
        }
        catch
        {
            return TeamCityClient.builtInTypes.AsEnumerable();
        }
    }

    public override RichDescription GetDescription() =>
        new RichDescription("TeamCity (", new Hilite(this.ResourceName), ") ", " builds for ", new Hilite(this.BuildConfigurationName), " in ", new Hilite(this.ProjectName), ".");
}

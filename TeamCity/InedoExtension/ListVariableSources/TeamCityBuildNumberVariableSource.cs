using System.ComponentModel;
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

    public override async IAsyncEnumerable<string> EnumerateListValuesAsync(VariableTemplateContext context)
    {
        if (!string.IsNullOrEmpty(this.ProjectName) && TeamCityCredentials.TryCreateFromResourceName(this.ResourceName, context, out var credentials))
        {
            await foreach (var p in new TeamCityClient(credentials).GetBuildsAsync(this.ProjectName).ConfigureAwait(false))
                yield return p.Number;
        }
    }

    public override RichDescription GetDescription()
    {
        return new RichDescription("TeamCity (", new Hilite(this.ResourceName), ") ", " builds for ", new Hilite(this.BuildConfigurationName), " in ", new Hilite(this.ProjectName), ".");
    }
}

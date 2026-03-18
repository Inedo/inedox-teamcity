using System.ComponentModel;
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

    public override IAsyncEnumerable<string> EnumerateListValuesAsync(VariableTemplateContext context)
    {
        if (!TeamCityCredentials.TryCreateFromResourceName(this.ResourceName, context, out var credentials))
            return AsyncEnumerable.Empty<string>();

        if (string.IsNullOrEmpty(this.ProjectName))
            return AsyncEnumerable.Empty<string>();

        return new TeamCityClient(credentials).GetProjectBuildTypesAsync(this.ProjectName)
            .Select(p => p.Name);
    }

    public override RichDescription GetDescription()
    {
        return new RichDescription("TeamCity (", new Hilite(this.ResourceName), ") ", " build configurations in ", new Hilite(this.ProjectName), ".");
    }
}

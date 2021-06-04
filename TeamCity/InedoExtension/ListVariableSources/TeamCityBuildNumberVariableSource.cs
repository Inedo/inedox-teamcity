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

namespace Inedo.Extensions.TeamCity.ListVariableSources
{
    [DisplayName("TeamCity Build Number")]
    [Description("Build numbers from a specified build configuration in a TeamCity instance.")]
    public sealed class TeamCityBuildNumberVariableSource : DynamicListVariableType
    {
        [Persistent]
        [DisplayName("From resource")]
        [SuggestableValue(typeof(SecureResourceSuggestionProvider<TeamCitySecureResource>))]
        [Required]
        public string ResourceName { get; set; }

        [Persistent]
        [DisplayName("Project name")]
        [SuggestableValue(typeof(ProjectNameSuggestionProvider))]
        [Required]
        public string ProjectName { get; set; }

        [Persistent]
        [DisplayName("Build configuration")]
        [SuggestableValue(typeof(BuildConfigurationNameSuggestionProvider))]
        [Required]
        public string BuildConfigurationName { get; set; }


        public override async Task<IEnumerable<string>> EnumerateListValuesAsync(VariableTemplateContext context)
        {
            var rrContext = new CredentialResolutionContext(context.ProjectId, null);
            var resource = SecureResource.TryCreate(this.ResourceName, rrContext) as TeamCitySecureResource;
            if (resource == null)
                return Enumerable.Empty<string>();

            using (var client = new TeamCityWebClient(resource, resource.GetCredentials(rrContext)))
            {
                return await client.GetBuildNumbersAsync(this.ProjectName, this.BuildConfigurationName).ConfigureAwait(false);
            }
        }

        public override RichDescription GetDescription() =>
            new RichDescription("TeamCity (", new Hilite(this.ResourceName), ") ", " builds for ", new Hilite(this.BuildConfigurationName), " in ", new Hilite(this.ProjectName), ".");
    }
}

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMasterExtensions.TeamCity.Credentials;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.ListVariableSources;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.TeamCity.ListVariableSources
{
    [DisplayName("TeamCity Build Number")]
    [Description("Build numbers from a specified build configuration in a TeamCity instance.")]
    public sealed class TeamCityBuildNumberVariableSource : ListVariableSource, IHasCredentials<TeamCityCredentials>
    {
        [Persistent]
        [DisplayName("Credentials")]
        [TriggerPostBackOnChange]
        [Required]
        public string CredentialName { get; set; }

        [Persistent]
        [DisplayName("Project name")]
        [SuggestableValue(typeof(ProjectNameSuggestionProvider))]
        [TriggerPostBackOnChange]
        [Required]
        public string ProjectName { get; set; }

        [Persistent]
        [DisplayName("Build configuration")]
        [SuggestableValue(typeof(BuildConfigurationNameSuggestionProvider))]
        [Required]
        public string BuildConfigurationName { get; set; }

        public override async Task<IEnumerable<string>> EnumerateValuesAsync(ValueEnumerationContext context)
        {
            var credentials = ResourceCredentials.Create<TeamCityCredentials>(this.CredentialName);

            using (var client = new TeamCityWebClient(credentials))
            {
                return await client.GetBuildNumbersAsync(this.ProjectName, this.BuildConfigurationName).ConfigureAwait(false);
            }
        }

        public override RichDescription GetDescription() =>
            new RichDescription("TeamCity (", new Hilite(this.CredentialName), ") ", " builds for ", new Hilite(this.BuildConfigurationName), " in ", new Hilite(this.ProjectName), ".");
    }
}

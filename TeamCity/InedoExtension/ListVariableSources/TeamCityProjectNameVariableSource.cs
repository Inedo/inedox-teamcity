using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.ListVariableSources;
using Inedo.Extensions.TeamCity.Credentials;
using Inedo.Serialization;

namespace Inedo.Extensions.TeamCity.ListVariableSources
{
    [DisplayName("TeamCity Project Name")]
    [Description("Project names from a specified TeamCity instance.")]
    public sealed class TeamCityProjectNameVariableSource : ListVariableSource, IHasCredentials<TeamCityCredentials>
    {
        [Persistent]
        [DisplayName("Credentials")]
        [Required]
        public string CredentialName { get; set; }

        public override async Task<IEnumerable<string>> EnumerateValuesAsync(ValueEnumerationContext context)
        {
            var credentials = TeamCityCredentials.TryCreate(this.CredentialName, context);
            if (credentials == null)
                return Enumerable.Empty<string>();

            using (var client = new TeamCityWebClient(credentials))
            {
                return await client.GetProjectNamesAsync().ConfigureAwait(false);
            }
        }

        public override RichDescription GetDescription() =>
            new RichDescription("TeamCity (", new Hilite(this.CredentialName), ") ", " project names.");
    }
}

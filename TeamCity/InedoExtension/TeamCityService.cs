using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inedo.BuildMasterExtensions.TeamCity.Operations;
using Inedo.Extensibility.CIServers;
using Inedo.Extensions.TeamCity.Credentials;

namespace Inedo.Extensions.TeamCity
{
    public sealed class TeamCityService : CIService<TeamCityProject, TeamCityCredentials, ImportTeamCityArtifactOperation>
    {
        public override string ServiceName => "TeamCity";
        public override string VariablesDisplayName => "Parameters";
        public override string? ScopeDisplayName => "Build configuration";
        public override string ApiUrlDisplayName => "TeamCity Server URL";
        public override string PasswordDisplayName => "Personal access token";
        public override string? UsernameDisplayName => null;

        public override IAsyncEnumerable<CIProjectInfo> GetProjectsAsync(TeamCityCredentials credentials, CancellationToken cancellationToken = default)
        {
            var client = new TeamCityClient(credentials);
            return client.GetProjectsAsync(cancellationToken);
        }
    }
}

namespace Inedo.Extensions.TeamCity;

internal sealed record TeamCityBuildInfo(List<string> Artifacts, List<KeyValuePair<string, string>> Properties, List<TeamCityBuildType> BuildTypes);

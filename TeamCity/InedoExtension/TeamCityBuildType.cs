using System.Xml.Linq;

namespace Inedo.Extensions.TeamCity;

internal sealed record TeamCityBuildType(string Id, string Name, string ProjectName)
{
    public static TeamCityBuildType FromXElement(XElement t) => new((string?)t.Attribute("id") ?? string.Empty, (string?)t.Attribute("name") ?? string.Empty, (string?)t.Attribute("projectName") ?? string.Empty);
};

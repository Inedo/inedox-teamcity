using System.Xml.Linq;

namespace Inedo.Extensions.TeamCity;

internal sealed record TeamCityBuildStatus(int Id, string State, string? BuildNumber, string? StatusText, string? Href, int PercentageComplete)
{

    public static TeamCityBuildStatus FromXElement(XElement status) =>
        new((int?)status.Attribute("id") ?? 0,
            (string?)status.Attribute("state") ?? "unknown",
            (string?)status.Attribute("number"),
            (string?)status.Attribute("status"),
            (string?)status.Attribute("href"),
            (int?)status.Attribute("percentageComplete") ?? 0);

    public bool Success => string.Equals(this.StatusText, "success", StringComparison.OrdinalIgnoreCase);
    public bool Finished => string.Equals(this.State, "finished", StringComparison.OrdinalIgnoreCase);
}

using System.Xml.Linq;

namespace Inedo.Extensions.TeamCity;

internal sealed class TeamCityBuildStatus
{
    public TeamCityBuildStatus(XElement status)
    {
        this.State = (string?)status.Attribute("state") ?? string.Empty;
        this.StatusText = (string?)status.Attribute("status") ?? string.Empty;
        this.Href = (string?)status.Attribute("href") ?? string.Empty;
        this.PercentComplete = this.Finished ? 100 : ((int?)status.Attribute("percentageComplete")).GetValueOrDefault();
    }

    public string State { get; }
    public string StatusText { get; }
    public string Href { get; }
    public int PercentComplete { get; }

    public bool Success => string.Equals(this.StatusText, "success", StringComparison.OrdinalIgnoreCase);
    public bool Finished => string.Equals(this.State, "finished", StringComparison.OrdinalIgnoreCase);
}

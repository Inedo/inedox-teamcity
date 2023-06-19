using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.ExecutionEngine.Executer;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.TeamCity.Credentials;
using Inedo.Extensions.TeamCity.SuggestionProviders;
using Inedo.Web;

namespace Inedo.Extensions.TeamCity.Operations;

[DisplayName("Queue TeamCity Build")]
[Description("Queues a build in TeamCity, optionally waiting for its completion.")]
[ScriptAlias("Queue-Build")]
public sealed class QueueTeamCityBuildOperation : TeamCityOperation
{
    [ScriptAlias("From")]
    [DisplayName("TeamCity resource")]
    [DefaultValue("$CIProject")]
    [SuggestableValue(typeof(SecureResourceSuggestionProvider<TeamCityProject>))]
    public override string? ResourceName { get; set; }
    [ScriptAlias("Project"), ScriptAlias("Job", Obsolete = true)]
    [DisplayName("Project name")]
    [SuggestableValue(typeof(ProjectNameSuggestionProvider))]
    public override string? ProjectName { get; set; }
    [ScriptAlias("BuildConfiguration")]
    [DisplayName("Build configuration")]
    [SuggestableValue(typeof(BuildConfigurationNameSuggestionProvider))]
    public override string? BuildConfigurationName { get; set; }

    [Category("Advanced")]
    [ScriptAlias("Artifact")]
    [DisplayName("BuildMaster artifact name")]
    [DefaultValue("Default"), NotNull]
    [Description("The name of the artifact in BuildMaster to create after artifacts are downloaded from TeamCity.")]
    public string? ArtifactName { get; set; }
    [Category("Advanced")]
    [ScriptAlias("Include")]
    [DisplayName("Include files")]
    [DefaultValue("**")]
    [Description(CommonDescriptions.MaskingHelp)]
    public IEnumerable<string>? Includes { get; set; }
    [Category("Advanced")]
    [ScriptAlias("Exclude")]
    [DisplayName("Exclude files")]
    [Description(CommonDescriptions.MaskingHelp)]
    public IEnumerable<string>? Excludes { get; set; }
    [Category("Advanced")]
    [ScriptAlias("Branch")]
    [DisplayName("Branch")]
    [Description("Use the specified branch when creating the build")]
    public string? BranchName { get; set; }
    [Output]
    [Category("Advanced")]
    [ScriptAlias("TeamCityBuildNumber")]
    [DisplayName("Actual build number (output)")]
    [PlaceholderText("e.g. $ActualBuildNumber")]
    [Description("When you specify a Build Number like \"lastSuccessful\", this will output the real TeamCity BuildNumber into a runtime variable.")]
    public string? TeamCityBuildNumber { get; set; }
    [Category("Advanced")]
    [ScriptAlias("AdditionalParameters")]
    [DisplayName("Additional parameters")]
    [Description("Optionally enter any additional parameters accepted by the TeamCity API in query string format, for example:<br/> "
        + "&amp;name=agent&amp;value=&lt;agentnamevalue&gt;&amp;name=system.name&amp;value=&lt;systemnamevalue&gt;..")]
    public string? AdditionalParameters { get; set; }
    [Category("Advanced")]
    [ScriptAlias("WaitForCompletion")]
    [DisplayName("Wait for completion")]
    [DefaultValue(true)]
    [PlaceholderText("true")]
    public bool? WaitForCompletion { get; set; } = true;

    [Category("Advanced")]
    [ScriptAlias("BuildConfigurationId")]
    [DisplayName("Build configuration ID")]
    [Description("TeamCity identifier that targets a single build configuration. May be specified instead of the Project name and Build configuration name.")]
    public string? BuildConfigurationId { get; set; }

    public override async Task ExecuteAsync(IOperationExecutionContext context)
    {
        if (!this.TryCreateClient(context, out var client))
            throw new ExecutionFailureException($"Could not create a connection to TeamCity resource \"{AH.CoalesceString(this.ResourceName, this.ServerUrl)}\".");

        if (string.IsNullOrEmpty(this.BuildConfigurationId))
        {
            if (string.IsNullOrEmpty(this.ProjectName))
                throw new ExecutionFailureException("No TeamCity project was specified, and there is no CI build associated with this execution.");
            
            await foreach (var t in client.GetProjectBuildTypesAsync(this.ProjectName, context.CancellationToken))
            {
                if (!string.IsNullOrEmpty(this.BuildConfigurationName) && !this.BuildConfigurationName.Equals(t.Name, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(this.BuildConfigurationId))
                    throw new ExecutionFailureException("Multiple build configurations were found for this project; specify which one to queue a build for.");

                this.BuildConfigurationId = t.Id;
            }

            if (string.IsNullOrEmpty(this.BuildConfigurationId))
                throw new ExecutionFailureException($"BuildConfiguration \"{this.BuildConfigurationName}\" not found on Project \"{this.ProjectName}\".");
        }
        else
        {
            if (!string.IsNullOrEmpty(this.ProjectName) || !string.IsNullOrEmpty(this.BuildConfigurationName))
                this.LogWarning($"Project (\"{this.ProjectName}\") and BuildConfiguration  ($\"{this.BuildConfigurationName}\") will be ignored when BuildConfigurationId is used.");
        }

        this.LogInformation($"Queueing \"{this.BuildConfigurationId}\" {(!string.IsNullOrEmpty(this.BranchName) ? " using branch " + this.BranchName : "")}...");

        var additionalProperties = this.AdditionalParameters?.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(p => p.Length == 2)
            .Select(p => new KeyValuePair<string, string>(Uri.UnescapeDataString(p[0]), Uri.UnescapeDataString(p[1])));

        var status = await client.QueueBuildAsync(this.BuildConfigurationId!, this.BranchName, additionalProperties, context.CancellationToken);

        if (this.WaitForCompletion == true)
        {
            this.LogDebug("Waiting for build to complete...");

            while (!status.Finished)
            {
                await Task.Delay(2000, context.CancellationToken);
                context.CancellationToken.ThrowIfCancellationRequested();
                status = await client.GetBuildStatusAsync(status, context.CancellationToken);
            }

            this.LogDebug("Build completed.");
        }
    }

    protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
    {
        return new ExtendedRichDescription(
            new RichDescription("Queue TeamCity Build"),
            string.IsNullOrEmpty(config[nameof(this.BuildConfigurationId)])
            ?   new RichDescription(
                    "for project ", 
                    new Hilite(config[nameof(this.ProjectName)]), 
                    " configuration ", 
                    new Hilite(config[nameof(this.BuildConfigurationName)]), 
                    !string.IsNullOrEmpty(this.BranchName) ? " using branch " + this.BranchName : ""
                )
            : new RichDescription(
                    "for build configuration ",
                    new Hilite(config[nameof(this.BuildConfigurationId)]),
                    !string.IsNullOrEmpty(this.BranchName) ? " using branch " + this.BranchName : ""
                )
        );
    }
}

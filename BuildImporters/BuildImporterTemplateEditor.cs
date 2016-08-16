using System.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.BuildMaster.Web.Controls.Extensions.BuildImporters;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class BuildImporterTemplateEditor : BuildImporterTemplateEditorBase
    {
        private ValidatingTextBox txtArtifactName;
        private ValidatingTextBox txtBranchName;
        private CheckBox chkArtifactNameLocked;
        private CheckBox chkBranchNameLocked;
        private BuildConfigurationSelectList ddlBuildConfigurationId;
        private SelectList ddlBuildNumber;

        public BuildImporterTemplateEditor()
        {
            this.ValidateBeforeSave += TeamCityBuildImporterTemplateEditor_ValidateBeforeSave;
        }

        private void TeamCityBuildImporterTemplateEditor_ValidateBeforeSave(object sender, ValidationEventArgs<BuildImporterTemplateBase> e)
        {
            var template = (BuildImporterTemplate)e.Extension;
            if (string.IsNullOrWhiteSpace(template.BuildConfigurationId))
            {
                e.Message = "A build configuration is required";
                e.ValidLevel = ValidationLevel.Error;
            }
        }

        public override void BindToForm(BuildImporterTemplateBase extension)
        {
            var template = (BuildImporterTemplate)extension;

            this.txtArtifactName.Text = template.ArtifactName;
            this.ddlBuildConfigurationId.SelectedValue = template.BuildConfigurationId;
            this.chkArtifactNameLocked.Checked = !template.ArtifactNameLocked;
            this.ddlBuildNumber.SelectedValue = template.BuildNumber;
            this.txtBranchName.Text = template.BranchName;
            this.chkBranchNameLocked.Checked = !template.BranchNameLocked;
        }

        public override BuildImporterTemplateBase CreateFromForm()
        {
            var selected = this.ddlBuildConfigurationId.Items.FirstOrDefault(i => i.Selected);

            return new BuildImporterTemplate
            {
                ArtifactName = this.txtArtifactName.Text,
                ArtifactNameLocked = !this.chkArtifactNameLocked.Checked,
                BuildConfigurationId = this.ddlBuildConfigurationId.SelectedValue,
                BuildConfigurationDisplayName = selected?.Text ?? string.Empty,
                BuildNumber = this.ddlBuildNumber.SelectedValue,
                BranchName = this.txtBranchName.Text,
                BranchNameLocked = !this.chkBranchNameLocked.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.ddlBuildConfigurationId = new BuildConfigurationSelectList() { ID = "ddlBuildConfigurationId" };
            this.ddlBuildConfigurationId.ExternalInit =
                () =>
                {
                    int? configurerId = this.TryGetConfigurerId();
                    var configurer = Configurer.GetConfigurer(configurerId: configurerId);
                    if (configurer != null)
                        this.ddlBuildConfigurationId.FillItems(configurer);
                };

            this.txtBranchName = new ValidatingTextBox() { DefaultText = "Default" };
            this.chkBranchNameLocked = new CheckBox { Text = "Allow selection at build time" };

            this.ddlBuildNumber = new SelectList();
            this.ddlBuildNumber.Items.Add(new SelectListItem("Select at build import time", ""));
            this.ddlBuildNumber.Items.Add(new SelectListItem("Always use last successful build", "lastSuccessful"));
            this.ddlBuildNumber.Items.Add(new SelectListItem("Always use last finished build", "lastFinished"));
            this.ddlBuildNumber.Items.Add(new SelectListItem("Always use last pinned build", "lastPinned"));

            this.txtArtifactName = new ValidatingTextBox { Required = false, DefaultText = "Default (all)" };
            this.chkArtifactNameLocked = new CheckBox { Text = "Allow selection at build time" };

            this.Controls.Add(
                new SlimFormField("Build configuration:", this.ddlBuildConfigurationId)
                {
                    HelpText = "Automatic population of build configurations will only works if a default configuration profile is defined."
                },
                new SlimFormField("Branch name:", new Div(this.txtBranchName), new Div(this.chkBranchNameLocked))
                {
                    HelpText = "The branch used to get the artifact, typically used in conjunction with predefined constant build numbers."
                },
                new SlimFormField("TeamCity build number:", this.ddlBuildNumber),
                new SlimFormField("Artifact name:", new Div(this.txtArtifactName), new Div(this.chkArtifactNameLocked))
                {
                    HelpText = "The name of artifact, for example: \"ideaIC-118.SNAPSHOT.win.zip\"."
                }
            );
        }

        /// <summary>
        /// This is a hack to find the selected configurer ID since it is not exposed via the SDK at the moment...
        /// </summary>
        private int? TryGetConfigurerId()
        {
            try
            {
                var ddlExtensionConfigurer = this.Page.FindControl("ddlExtensionConfigurer") as DropDownList;
                if (ddlExtensionConfigurer != null)
                    return AH.ParseInt(ddlExtensionConfigurer.SelectedValue);
            }
            catch
            {
            }
            return null;
        }
    }
}

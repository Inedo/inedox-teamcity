using Inedo.BuildMaster.Web;
using Inedo.BuildMaster.Web.Controls.Plans;
using Inedo.Web.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace Inedo.BuildMasterExtensions.TeamCity.Operations
{
    /// <summary>
    /// Defines a custom UI editor for the BuildConfiguration property which renders a dropdown list filled with available BuildTypes from TeamCity.
    /// </summary>
    /// <remarks>Connection to TeamCity uses the default Configuration profile, *NOT* Resource Credentials, hence **There MUST BE a default configuration profile set for this editor to work**.</remarks>
    public sealed class BuildConfigurationArgumentEditor : Inedo.BuildMaster.Web.Controls.Plans.OperationArgumentEditor
    {
        public BuildConfigurationArgumentEditor(PropertyInfo prop) : base(prop) { }

        protected override ISimpleControl BuildEditorHtml()
        {
            var ddlConfiguration = new BuildConfigurationSelectList();
            var configurer = Configurer.GetConfigurer(); // Uses default one...horrible...

            // TODO: Use Resource Credentials if possible or implement client-side population of the dropdown using KO            
            ddlConfiguration.FillItems(configurer);

            ddlConfiguration.Attributes["data-bind"] = string.Format("planargvalue: {0}", base.Property.Name, "BuildConfigurationId");

            return ddlConfiguration;

        }
        
    }
}

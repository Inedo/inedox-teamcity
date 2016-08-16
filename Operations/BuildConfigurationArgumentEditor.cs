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

    public sealed class BuildConfigurationArgumentEditor : Inedo.BuildMaster.Web.Controls.Plans.OperationArgumentEditor
    {
        public BuildConfigurationArgumentEditor(PropertyInfo prop) : base(prop) { }

        protected override ISimpleControl BuildEditorHtml()
        {
            var ddlConfiguration = new BuildConfigurationSelectList();
            var configurer = Configurer.GetConfigurer();
            // if configurer not defined, read the credentials

            ddlConfiguration.FillItems(configurer);

            ddlConfiguration.Attributes["data-bind"] = string.Format("planargvalue: {0}", base.Property.Name, "BuildConfigurationId");

            return ddlConfiguration;
        }
        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inedo.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using System.Net;
using System.Xml.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    /// <summary>
    /// Builds a SELECT html control filled with buildTypes
    /// </summary>
    /// <remarks>Uses the default Configuration Profile, not Resource Credentials</remarks>
    public class BuildConfigurationSelectList : SelectList
    {
        internal Action ExternalInit;

        public BuildConfigurationSelectList()
        {
            this.IsIdRequired = true;
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (this.ExternalInit != null)
                this.ExternalInit();
        }

        internal void FillItems(Configurer configurer)
        {
            // TODO: Use Resource Credentials instead of Configuration Profile
            if (configurer == null)
                return;

            var teamCityAPI = new TeamCityAPI(configurer);

            var buildTypes = teamCityAPI.GetBuildTypes();

            this.Items.AddRange(buildTypes
                .Select(bt => new SelectListItem(bt.projectName + ": " + bt.name, bt.id))
                .ToArray());

        }

    }
}

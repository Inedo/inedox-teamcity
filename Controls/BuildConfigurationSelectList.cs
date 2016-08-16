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
            if (configurer == null)
                return;

            var teamCityAPI = configurer.GetAPI();

            var buildTypes = teamCityAPI.GetBuildTypes();

            this.Items.AddRange(buildTypes
                .Select(bt => new SelectListItem(bt.projectName + ": " + bt.name, bt.id))
                .ToArray());

        }

    }
}

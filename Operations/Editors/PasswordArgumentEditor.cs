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
    public sealed class PasswordArgumentEditor : Inedo.BuildMaster.Web.Controls.Plans.OperationArgumentEditor
    {
        public PasswordArgumentEditor(PropertyInfo prop) : base(prop) { }

        protected override ISimpleControl BuildEditorHtml()
        {
            Element element = new Element("input", new ElementAttribute("type", "password"));
            element.Attributes["data-bind"] = string.Format("planargvalue: {0}", base.Property.Name, "Password");
            return element;
            // return new Inedo.Web.Controls.PasswordTextBox();
        }
        
    }
}

using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Web;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.ExecutionEngine;
using Inedo.Web.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Inedo.BuildMasterExtensions.TeamCity.Operations
{
    /// <summary>
    /// Defines a custom editor for the ImportArtifactOperation
    /// Not in use.
    /// </summary>
    public class ImportArtifactOperationEditor : Inedo.BuildMaster.Web.Controls.Plans.OperationEditor
    {
        public ImportArtifactOperationEditor(Type operationType) : base(operationType) { }

        public override ISimpleControl CreateView(ActionStatement statement) 
        {
            SimpleVirtualCompositeControl simpleVirtualCompositeControl = (SimpleVirtualCompositeControl) base.CreateView(statement);

            foreach(ISimpleControl control in simpleVirtualCompositeControl.Controls)
            {

            }

            return simpleVirtualCompositeControl;
            //SimpleVirtualCompositeControl simpleVirtualCompositeControl = new SimpleVirtualCompositeControl();
            //var propertyInfos = ((IEnumerable<PropertyInfo>)this.GetPropertyContainer().GetProperties());
            //return simpleVirtualCompositeControl;
        }

        //private Type GetCustomEditorAttributeForPropertyInfo(PropertyInfo propertyInfo)
        //{
        //    if (propertyInfo == null)
        //    {
        //        return null;
        //    }
        //    CustomEditorAttribute customAttribute = propertyInfo.GetCustomAttribute<CustomEditorAttribute>();
        //    if (customAttribute != null)
        //    {
        //        return customAttribute.ControlType;
        //    }
        //    return null;
        //}
    }
}

using System;
using Inedo.BuildMaster.Web.Controls.Extensions;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    public sealed class TeamCityBuildConfigurationVariableSetter : BuildConfigurationSelectList, IVariableSetter<TeamCityBuildConfigurationVariable>
    {
        public TeamCityBuildConfigurationVariableSetter()
        {
        }

        string IVariableSetter.VariableValue
        {
            get
            {
                return this.SelectedValue;
            }
            set
            {
                this.SelectedValue = value;
            }
        }

        void IVariableSetter<TeamCityBuildConfigurationVariable>.BindToVariable(TeamCityBuildConfigurationVariable variable, string defaultValue)
        {
            if (variable == null) throw new ArgumentNullException("variable");

            this.FillItems(Configurer.GetConfigurer(variable.ConfigurationProfileName));

            this.SelectedValue = AH.CoalesceString(variable.Value, defaultValue);
        }
    }
}

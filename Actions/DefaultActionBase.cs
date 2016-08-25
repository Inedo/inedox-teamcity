using Inedo.BuildMaster.Extensibility.Actions;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    public abstract class DefaultActionBase : AgentBasedActionBase
    {
        protected DefaultActionBase()
        {
        }

        public sealed override bool IsConfigurerSettingRequired() => true;

        protected new Configurer GetExtensionConfigurer() => (Configurer)base.GetExtensionConfigurer();
    }
}
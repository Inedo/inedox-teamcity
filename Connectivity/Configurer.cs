using System;
using System.Linq;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web;
using Inedo.Serialization;
using System.Collections.Generic;

[assembly: ExtensionConfigurer(typeof(Inedo.BuildMasterExtensions.TeamCity.Configurer))]

namespace Inedo.BuildMasterExtensions.TeamCity
{
    /// <summary>
    /// LEGACY
    /// </summary>
    [CustomEditor(typeof(ConfigurerEditor))]
    public class Configurer : ExtensionConfigurerBase, IConnectionInfo
    {
        internal static readonly string ConfigurerName = typeof(Configurer).FullName + "," + typeof(Configurer).Assembly.GetName().Name;

        /// <summary>
        /// Gets the configurer based on the profile name, the default configurer if no name is specified, or null.
        /// </summary>
        /// <param name="profileName">Name of the profile.</param>
        internal static Configurer GetConfigurer(string profileName = null, int? configurerId = null)
        {

            var profiles = GetConfigurationProfiles();

            var configurer = profiles.FirstOrDefault(
                p => p.ExtensionConfiguration_Id == configurerId || string.Equals(profileName, p.Profile_Name, StringComparison.OrdinalIgnoreCase)
            );

            if (configurer == null)
                configurer = profiles.FirstOrDefault(p => p.Default_Indicator.Equals(Domains.YN.Yes));

            if (configurer == null)
                return null;

            return (Configurer)Persistence.DeserializeFromPersistedObjectXml(configurer.Extension_Configuration);
        }

        internal static IList<Tables.ExtensionConfigurations> GetConfigurationProfiles()
        {
             return DB.ExtensionConfiguration_GetConfigurations(ConfigurerName);
        }

        /// <summary>
        /// Gets or sets the server URL without the form of authentication included in the URL.
        /// </summary>
        [Persistent]
        public string ServerUrl { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        [Persistent]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        [Persistent(Encrypted = true)]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the default name of the branch to supply for TeamCity operations.
        /// </summary>
        [Persistent]
        public string DefaultBranchName { get; set; }

        /// <summary>
        /// Gets the base URL used for connections to the TeamCity server that incorporates the authentication mechanism.
        /// </summary>
        public string BaseUrl => $"{this.ServerUrl.TrimEnd('/')}/{(string.IsNullOrEmpty(this.UserName) ? "guestAuth" : "httpAuth")}/";

        
        internal TeamCityAPI GetAPI()
        {
            return new TeamCityAPI(this);
        }

        internal static TeamCityAPI GetConfigurerAPI()
        {
            return new TeamCityAPI(GetConfigurer());
        }

    }
}

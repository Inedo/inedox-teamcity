using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal interface IConnectionInfo
    {
        string ServerUrl { get; }
        string UserName { get; }
        string Password { get; }
    }

    internal static class IConnectionInfoExtensions
    {
        public static string GetApiUrl(this IConnectionInfo connectionInfo)
        {
            if ((connectionInfo == null) || string.IsNullOrEmpty(connectionInfo.ServerUrl))
                throw new InvalidOperationException("No connection information was available to connect to the TeamCity API. Please define at least a default Configuration Profile to TeamCity. Also, if using 'ImportArtifact' or 'QueueBuild' operations in one of your Plans, please ensure that a 'Resource Credential' is selected or that connection details are entered. ");

            return $"{connectionInfo.ServerUrl.TrimEnd('/')}/{(string.IsNullOrEmpty(connectionInfo.UserName) ? "guestAuth" : "httpAuth")}/";
        }
    }
}

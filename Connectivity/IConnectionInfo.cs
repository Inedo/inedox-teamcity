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
            return $"{connectionInfo.ServerUrl.TrimEnd('/')}/{(string.IsNullOrEmpty(connectionInfo.UserName) ? "guestAuth" : "httpAuth")}/";
        }
    }
}

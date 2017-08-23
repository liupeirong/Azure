using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.Azure.KeyVault;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Configuration;

namespace pbipaembed
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static readonly string ClientId = ConfigurationManager.AppSettings["clientId"];
        private static readonly string ClientSecret = ConfigurationManager.AppSettings["clientSecret"];
        private static readonly string PbiUserKeyvault = ConfigurationManager.AppSettings["pbiUserKeyvault"];
        private static readonly string PbiPasswordKeyvault = ConfigurationManager.AppSettings["pbiPasswordKeyvault"];
        private static readonly ClientCredential credential = new ClientCredential(ClientId, ClientSecret);
        public static string pbiUserName;
        public static string pbiPassword;

        //the method that will be provided to the KeyVaultClient
        public static async Task<string> GetToken(string authority, string resource, string scope)
        {
            var authenticationContext = new AuthenticationContext(authority);
            var result = await authenticationContext.AcquireTokenAsync(resource, credential);
            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token");

            return result.AccessToken;
        }

        private static async void GetSecrets()
        {
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetToken));
            var secu = await kv.GetSecretAsync(PbiUserKeyvault);
            var secp = await kv.GetSecretAsync(PbiPasswordKeyvault);
            pbiUserName = secu.Value;
            pbiPassword = secp.Value; 
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            GetSecrets();
        }

    }
}

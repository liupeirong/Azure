using System;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Configuration;

namespace pbipaembed
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static readonly string ClientId = ConfigurationManager.AppSettings["clientId"];
        private static readonly string PbiUserKeyvault = ConfigurationManager.AppSettings["pbiUserKeyvault"];
        private static readonly string PbiPasswordKeyvault = ConfigurationManager.AppSettings["pbiPasswordKeyvault"];
        public static string pbiUserName;
        public static string pbiPassword;

        private static async void GetSecrets()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
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

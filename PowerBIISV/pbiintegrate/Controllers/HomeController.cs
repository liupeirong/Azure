using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Mvc;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Web;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security.Cookies;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using pbiintegrate.Models;

namespace pbiintegrate.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Error()
        {
            return View();
        }

        [Authorize]
        public async Task<ActionResult> LOBReport()
        {
            ViewBag.accessToken = await GetTokenForApplication(); 
            string responseContent = string.Empty;
            string reportsUri = "https://api.powerbi.com/v1.0/myorg/reports";

            System.Net.WebRequest request = System.Net.WebRequest.Create(reportsUri) as System.Net.HttpWebRequest;
            request.Method = "GET";
            request.ContentLength = 0;
            request.Headers.Add("Authorization", String.Format("Bearer {0}", ViewBag.AccessToken));

            using (var response = request.GetResponse() as System.Net.HttpWebResponse)
            {
                using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    responseContent = reader.ReadToEnd();
                    PBIReports Reports = JsonConvert.DeserializeObject<PBIReports>(responseContent);
                    ViewBag.Reports = new SelectList(Reports.value.OrderBy(t => t.name), "embedUrl", "name");
                }
            }
            return View();
        }

        [Authorize]
        public async Task<ActionResult> LOBDashboard()
        {
            ViewBag.accessToken = await GetTokenForApplication();
            string responseContent = string.Empty;
            string dashboardsUri = "https://api.powerbi.com/beta/myorg/dashboards";

            WebRequest request = WebRequest.Create(dashboardsUri) as HttpWebRequest;
            request.Method = "GET";
            request.ContentLength = 0;
            request.Headers.Add("Authorization", String.Format("Bearer {0}", ViewBag.AccessToken));

            using (var response = request.GetResponse() as System.Net.HttpWebResponse)
            {
                using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    responseContent = reader.ReadToEnd();
                    PBIDashboards Dashboards = JsonConvert.DeserializeObject<PBIDashboards>(responseContent);
                    ViewBag.Dashboards = new SelectList(Dashboards.value.OrderBy(t => t.displayName), "embedUrl", "displayName");
                }
            }
            return View();
        }

        public async Task<string> GetTokenForApplication()
        {
            string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

            // get a token for the Graph without triggering any user interaction (from the cache, via multi-resource refresh token, etc)
            ClientCredential clientcred = new ClientCredential(Startup.clientId, Startup.clientKey);
            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's database
            AuthenticationContext authenticationContext = new AuthenticationContext(Startup.aadInstance + tenantID, new ADALTokenCache(signedInUserID));
            AuthenticationResult authenticationResult;
            try
            {
                authenticationResult = await authenticationContext.AcquireTokenSilentAsync(Startup.resourceId, clientcred, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));
                return authenticationResult.AccessToken;
            }catch (AdalException e)
            {
                if (e.ErrorCode == AdalError.FailedToAcquireTokenSilently)
                {
                    authenticationContext.TokenCache.Clear();
                    throw e;
                }
            }
            return null;
        }
    }
}

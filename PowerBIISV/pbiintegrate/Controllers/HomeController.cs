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
        public ActionResult LOBReport()
        {
            //if a user is already signed in but didn't go through the process of getting authorization code in auth config,
            //then GetAccessTokenAsync will succeed, but the returned access token can't be used to access the resource.
            //we have to ask the user to sign out and sign in again to get the authorization code first then access token.
            if (TokenCache.DefaultShared.Count > 0)
            {
                IEnumerable<TokenCacheItem> tokens = TokenCache.DefaultShared.ReadItems();
                ViewBag.accessToken = tokens.First().AccessToken;
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
            }
            return View();
        }

        public ActionResult LOBDashboard()
        {
            //if a user is already signed in but didn't go through the process of getting authorization code in auth config,
            //then GetAccessTokenAsync will succeed, but the returned access token can't be used to access the resource.
            //we have to ask the user to sign out and sign in again to get the authorization code first then access token.
            if (TokenCache.DefaultShared.Count > 0)
            {
                IEnumerable<TokenCacheItem> tokens = TokenCache.DefaultShared.ReadItems();
                ViewBag.accessToken = tokens.First().AccessToken;
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
            }
            return View();
        }

    }
}

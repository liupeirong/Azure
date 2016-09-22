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

namespace mvpembed.Controllers
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
            }
            return View();
        }
        public ActionResult ISVReport()
        {
            string workspaceId = "b841ef0c-a24d-4635-b07b-3552dcefb291";
            string workspaceName = "pliupbiws";
            string reportId = "69c06944-dcbb-4115-82b7-a98fd2a27d50";
            int expireDays = 1;

            int unixTimestamp = (int) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + expireDays * 3600;
            string pbieKey1 = "{\"typ\":\"JWT\",\"alg\":\"HS256\"}";
            string pbieKey2 = String.Format("{{\"wid\":\"{0}\",\"rid\":\"{1}\",\"wcn\":\"{2}\",\"iss\":\"PowerBISDK\",\"ver\":\"0.2.0\",\"aud\":\"{3}\",\"exp\":{4}}}",
                workspaceId, reportId, workspaceName, Startup.resourceId, unixTimestamp);
            string pbieKey1n2ToBase64 = Base64UrlEncode(pbieKey1) + "." + Base64UrlEncode(pbieKey2);
            string pbieKey3 = HMAC256EncryptBase64UrlEncode(pbieKey1n2ToBase64);

            ViewBag.accessToken = pbieKey1n2ToBase64 + "." + pbieKey3;
            ViewBag.reportId = reportId;
            return View();
        }

        private string Base64UrlEncode(string str)
        {
            var strBytes = System.Text.Encoding.UTF8.GetBytes(str);
            string b64Str = System.Convert.ToBase64String(strBytes);
            return b64Str.Replace('/', '_').Replace('+', '-').TrimEnd(new char[]{'='}); 
        }

        private string HMAC256EncryptBase64UrlEncode(string str)
        {
            var key = System.Text.Encoding.UTF8.GetBytes(Startup.pbieKey);
            var strBytes = System.Text.Encoding.UTF8.GetBytes(str);
            HMACSHA256 enc = new HMACSHA256(key);
            var hashBytes = enc.ComputeHash(strBytes);
            string b64Str = System.Convert.ToBase64String(hashBytes);
            return b64Str.Replace('/', '_').Replace('+', '-').TrimEnd(new char[]{'='}); 
        }
    }
}

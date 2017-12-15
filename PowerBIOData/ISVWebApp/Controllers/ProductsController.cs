using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Security.Claims;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using ISVWebApp.DAL;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ISVWebApp.Controllers
{
    public class ProductsController : Controller
    {
        [Authorize]
        // GET: Products
        public async Task<ActionResult> Index()
        {
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            string token = await getTokenForOData(tenantID, signedInUserID, userObjectID);

            HttpClient client = new HttpClient();
            var tenantInfo = Startup.tenantODataMap[tenantID];
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, tenantInfo[0]);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", tenantInfo[2]);
            HttpResponseMessage response = await client.SendAsync(request);

            ViewBag.Products = "";
            if (response.IsSuccessStatusCode)
            {
                //Dictionary<String, String> responseElements = new Dictionary<String, String>();
                //JsonSerializerSettings settings = new JsonSerializerSettings();
                String responseString = await response.Content.ReadAsStringAsync();
                //responseElements = JsonConvert.DeserializeObject<Dictionary<String, String>>(responseString, settings);
                //String products = responseElements["value"];
                //List<String> products = new List<string>();
                //foreach (Dictionary<String, String> responseElement in responseElements)
                //{
                //    products.Add(responseElement["Name"]);
                //}

                ViewBag.Products = responseString;
            }

            return View();
        }

        private async Task<string> getTokenForOData(string tenantID, string signedInUserID, string userObjectID)
        {
            // get a token for the Graph without triggering any user interaction (from the cache, via multi-resource refresh token, etc)
            ClientCredential clientcred = new ClientCredential(Startup.clientId, Startup.appKey);
            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
            AuthenticationContext authContext = new AuthenticationContext(Startup.authority, new EFADALTokenCache(signedInUserID));
            AuthenticationResult result = await authContext.AcquireTokenSilentAsync(Startup.appResourceId, clientcred, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));
            return result.AccessToken;
        }
    }
}
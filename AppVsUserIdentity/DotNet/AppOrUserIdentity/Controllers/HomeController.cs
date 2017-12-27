using System.Linq;
using System.Web.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;

namespace AppOrUserIdentity.Controllers
{
    public class HomeController : Controller
    {
        private static ClientCredential clientCredential = new ClientCredential(Startup.ClientId, Startup.ClientKey);

        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> GetEulaAppCred()
        {
            string subscription = Request.Form["subscription"].ToString();
            string token = await GetAuthorizationHeaderAppCred(subscription);
            ViewBag.Content = await GetEulaCommon(subscription, token);
            return View();
        }

        [Authorize]
        public async Task<ActionResult> GetEulaUserCred()
        {
            string subscription = Request.Form["subscription"].ToString();
            string token = await GetAuthorizationHeaderUserCred();
            ViewBag.Content = await GetEulaCommon(subscription, token);
            return View();
        }

        private async Task<string> GetEulaCommon(string subscription, string token)
        { 
            string publisher = Request.Form["publisher"].ToString();
            string offer = Request.Form["offer"].ToString();
            string plan = Request.Form["plan"].ToString();

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get,
                string.Format(
                    "https://management.azure.com/subscriptions/{0}/providers/Microsoft.MarketplaceOrdering/offerTypes/virtualmachine/publishers/{1}/offers/{2}/plans/{3}/agreements/current?api-version=2015-06-01",
                subscription, publisher, offer, plan));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            return body;
        }


        public async Task<ActionResult> AcceptEulaAppCred()
        {
            string subscription = Request.Form["subscription"].ToString();
            string token = await GetAuthorizationHeaderAppCred(subscription);
            ViewBag.Content = await AcceptEulaCommon(subscription, token);
            return View();
        }

        [Authorize]
        public async Task<ActionResult> AcceptEulaUserCred()
        {
            string subscription = Request.Form["subscription"].ToString();
            string token = await GetAuthorizationHeaderUserCred();
            ViewBag.Content = await AcceptEulaCommon(subscription, token);
            return View();
        }

        private async Task<string> AcceptEulaCommon(string subscription, string token)
        { 
            string publisher = Request.Form["publisher"].ToString();
            string offer = Request.Form["offer"].ToString();
            string plan = Request.Form["plan"].ToString();

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get,
                string.Format(
                    "https://management.azure.com/subscriptions/{0}/providers/Microsoft.MarketplaceOrdering/offerTypes/virtualmachine/publishers/{1}/offers/{2}/plans/{3}/agreements/current?api-version=2015-06-01",
                subscription, publisher, offer, plan));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            JsonReader reader = new JsonTextReader(new StringReader(body));
            //don't let the json parser change the datetime format, otherwise the signature won't match
            reader.DateParseHandling = DateParseHandling.None;
            JObject jo = JObject.Load(reader);
            bool accepted = (bool)jo["properties"]["accepted"];
            if (accepted)
                return "{\"result\": \"EULA already accepted, nothing to do\"}";

            string signature = (string)jo["properties"]["signature"];
            string licenseLink = (string)jo["properties"]["licenseTextLink"];
            string privacyLink = (string)jo["properties"]["privacyPolicyLink"];
            string retrieveDateTime = (string)jo["properties"]["retrieveDatetime"];
               
            client = new HttpClient();
            request = new HttpRequestMessage(HttpMethod.Put,
                string.Format(
                "https://management.azure.com/subscriptions/{0}/providers/Microsoft.MarketplaceOrdering/offerTypes/virtualmachine/publishers/{1}/offers/{2}/plans/{3}/agreements/current?api-version=2015-06-01",
                subscription, publisher, offer, plan));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            JObject reqBody = new JObject(
                new JProperty("properties",
                    new JObject(
                        new JProperty("publisher", publisher),
                        new JProperty("product", offer),
                        new JProperty("plan", plan),
                        new JProperty("licenseTextLink", licenseLink),
                        new JProperty("privacyPolicyLink", privacyLink),
                        new JProperty("retrieveDatetime", retrieveDateTime),
                        new JProperty("signature", signature),
                        new JProperty("accepted", true)
                    )
                )
            );
            request.Content = new StringContent(reqBody.ToString(), System.Text.Encoding.UTF8, "application/json");
            response = await client.SendAsync(request);
            body = await response.Content.ReadAsStringAsync();
            return body;
        }

        private async Task<string> GetTenantIDFromSubscription(string subscription)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get,
                string.Format(
                "https://management.azure.com/subscriptions/{0}?api-version=2015-07-01",
                subscription));
            HttpResponseMessage response = await client.SendAsync(request);
            string header = response.Headers.GetValues("WWW-Authenticate").First();
            string mark = "authorization_uri=";
            string mark2 = "https://";
            int idxBegin = header.IndexOf(mark) + mark.Length + 1;
            idxBegin = header.IndexOf(mark2, idxBegin) + mark2.Length + 1;
            idxBegin = header.IndexOf(@"/", idxBegin) + 1;
            int idxEnd = header.IndexOf('"', idxBegin);
            string tenantID = header.Substring(idxBegin, idxEnd - idxBegin);

            return tenantID;
        }

        private async Task<string> GetAuthorizationHeaderAppCred(string subscription)
        {
            string tenantID = await GetTenantIDFromSubscription(subscription);
            AuthenticationContext authContext = new AuthenticationContext(Startup.Authority + tenantID);
            AuthenticationResult result = await authContext.AcquireTokenAsync(Startup.ResourceUri, clientCredential);

            return result.AccessToken;
        }

        private async Task<string> GetAuthorizationHeaderUserCred()
        {
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            AuthenticationContext authContext = new AuthenticationContext(Startup.Authority + tenantID);
            string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            AuthenticationResult result = await authContext.AcquireTokenSilentAsync(Startup.ResourceUri, clientCredential,
                new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));

            return result.AccessToken;
        }
    }
}
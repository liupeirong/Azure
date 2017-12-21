using System;
using System.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Security.Claims;
using pbipaembed.Models;

namespace pbipaembed.Controllers
{
    public class MyReportController : Controller
    {
        private static readonly string AuthorityUrl = ConfigurationManager.AppSettings["authorityUrl"];
        public static readonly string ResourceUrl = ConfigurationManager.AppSettings["resourceUrl"];
        private static readonly string ClientId = ConfigurationManager.AppSettings["clientId"];
        private static readonly string ApiUrl = ConfigurationManager.AppSettings["apiUrl"];
        private static readonly string GroupId = ConfigurationManager.AppSettings["groupId"];
        private static readonly string EmbedUrlBase = ConfigurationManager.AppSettings["embedUrlBase"];
        private static readonly AuthenticationContext authenticationContext = new AuthenticationContext(AuthorityUrl);

        private string accessToken;

        public ActionResult Index()
        {
            return View();
        }

        private async Task<IPowerBIClient> CreatePowerBIClientForUser()
        {
            string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

            // get a token for the Graph without triggering any user interaction (from the cache, via multi-resource refresh token, etc)
            ClientCredential clientcred = new ClientCredential(Startup.clientId, Startup.appKey);
            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's database
            AuthenticationContext authenticationContext = new AuthenticationContext(
                Startup.aadInstance + tenantID, new ADALTokenCache(signedInUserID));
            AuthenticationResult authenticationResult;
            TokenCredentials tokenCredentials;
            PowerBIClient client = null;
            try
            {
                authenticationResult = await authenticationContext.AcquireTokenSilentAsync(
                    ResourceUrl, clientcred,
                    new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));
                accessToken = authenticationResult.AccessToken;
                tokenCredentials = new TokenCredentials(accessToken, "Bearer");
                client = new PowerBIClient(new Uri(ApiUrl), tokenCredentials);
            }
            catch (AdalException e)
            {
                if (e.ErrorCode == AdalError.FailedToAcquireTokenSilently)
                {
                    authenticationContext.TokenCache.Clear();
                    throw e;
                }
            }

            return client;
        }

        [Authorize]
        public async Task<ActionResult> ListUserGroups()
        {
            var viewModel = new GroupsViewModel();
            using (var client = await CreatePowerBIClientForUser())
            {
                var groups = client.Groups.GetGroups();
                viewModel.Groups = groups.Value.OrderBy(t => t.Name).ToList();
            }

            return View(viewModel);
        }

        [Authorize]
        public async Task<ActionResult> ListUserReports()
        {
            string groupId = Request.Form["SelectedGroup"].ToString();
            var viewModel = new ReportsViewModel();
            using (var client = await CreatePowerBIClientForUser())
            {
                var reports = await client.Reports.GetReportsInGroupAsync(groupId);
                viewModel.Reports = reports.Value.OrderBy(t => t.Name).ToList();
                viewModel.GroupId = groupId;
            }

            return View(viewModel);
        }

        [Authorize]
        public async Task<ActionResult> EmbedUserReport()
        {
            string reportId = Request.Form["SelectedReport"].ToString();
            string groupId = Request.Form["SelectedGroup"].ToString();
            using (var client = await CreatePowerBIClientForUser())
            {
                var report = client.Reports.GetReportInGroup(groupId, reportId);
                // No need to generate Embed Token, we use the bearer token

                // Generate Embed Configuration.
                var embedConfig = new EmbedConfig()
                {
                    EmbedToken = new EmbedToken(token: accessToken),
                    EmbedUrl = report.EmbedUrl,
                    Id = reportId,
                    Name = report.Name
                };

                return View(embedConfig);
            }
        }
    }
}
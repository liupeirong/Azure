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
    public class HomeController : Controller
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

        private async Task<IPowerBIClient> CreatePowerBIClientForISV()
        {
            var credential = new UserPasswordCredential(MvcApplication.pbiUserName, MvcApplication.pbiPassword);
            var authenticationResult = await authenticationContext.AcquireTokenAsync(ResourceUrl, ClientId, credential);
            var tokenCredentials = new TokenCredentials(authenticationResult.AccessToken, "Bearer");
            var client = new PowerBIClient(new Uri(ApiUrl), tokenCredentials);

            return client;
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
        public async Task<ActionResult> ListReports()
        {
            var viewModel = new ReportsViewModel();
            using (var client = await CreatePowerBIClientForISV())
            {
                var reports = await client.Reports.GetReportsInGroupAsync(GroupId);
                viewModel.Reports = reports.Value.ToList();
            }

            return View(viewModel);
        }

        [Authorize]
        public async Task<ActionResult> EmbedReport()
        {
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;

            string reportId = Request.Form["SelectedReport"].ToString();
            using (var client = await CreatePowerBIClientForISV())
            {
                var report = client.Reports.GetReportInGroup(GroupId, reportId);
                // map user for RLS
                var salesUser = new EffectiveIdentity(
                    username: "adventure-works\\pamela0",
                    roles: new List<string> { "Sales" },
                    datasets: new List<string> { report.DatasetId } );

                // Generate Embed Token.
                var generateTokenRequestParameters = tenantID.StartsWith("1") ?
                    new GenerateTokenRequest(
                    accessLevel: "edit", allowSaveAs: true) : 
                    new GenerateTokenRequest(
                    accessLevel: "edit", allowSaveAs: true,
                    identities: new List<EffectiveIdentity> { salesUser });
                var tokenResponse = await client.Reports.GenerateTokenInGroupAsync(GroupId, reportId, generateTokenRequestParameters);
                // Refresh the dataset
                // await client.Datasets.RefreshDatasetInGroupAsync(GroupId, report.DatasetId);

                // Generate Embed Configuration.
                var embedConfig = new EmbedConfig()
                {
                    EmbedToken = tokenResponse,
                    EmbedUrl = report.EmbedUrl,
                    Id = reportId,
                    Name = report.Name
                };

                return View(embedConfig);
            }
        }

        public async Task<ActionResult> ListDashboards()
        {
            var viewModel = new DashboardsViewModel();
            using (var client = await CreatePowerBIClientForISV())
            {
                var dashboards = await client.Dashboards.GetDashboardsInGroupAsync(GroupId);
                viewModel.Dashboards = dashboards.Value.ToList();
            }

            return View(viewModel);
        }

        public async Task<ActionResult> EmbedDashboard()
        {
            string dashboardId = Request.Form["SelectedDashboard"].ToString();
            using (var client = await CreatePowerBIClientForISV())
            {
                var dashboardsResult = await client.Dashboards.GetDashboardsInGroupAsync(GroupId);
                var dashboards = dashboardsResult.Value.ToList();
                var dashboard = dashboards.FirstOrDefault(d => d.Id == dashboardId);

                // Generate Embed Token.
                var generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");
                var tokenResponse = await client.Dashboards.GenerateTokenInGroupAsync(GroupId, dashboardId, generateTokenRequestParameters);

                // Generate Embed Configuration.
                var embedConfig = new EmbedConfig()
                {
                    EmbedToken = tokenResponse,
                    EmbedUrl = dashboard.EmbedUrl,
                    Id = dashboardId,
                    Name = dashboard.DisplayName
                };

                return View(embedConfig);
            }
        }

        public async Task<ActionResult> ListTiles()
        {
            string dashboardId = Request.Form["SelectedDashboard"].ToString();
            var viewModel = new TilesViewModel();
            using (var client = await CreatePowerBIClientForISV())
            {
                var tiles = await client.Dashboards.GetTilesInGroupAsync(GroupId, dashboardId);
                viewModel.Tiles = tiles.Value.ToList();
                viewModel.DashboardId = dashboardId;
            }

            return View(viewModel);
        }


        public async Task<ActionResult> EmbedTile()
        {
            string tileId = Request.Form["SelectedTile"].ToString();
            string dashboardId = Request.Form["SelectedDashboard"].ToString();
            using (var client = await CreatePowerBIClientForISV())
            {
                var tile = await client.Dashboards.GetTileInGroupAsync(GroupId, dashboardId, tileId);
                // Generate Embed Token for a tile.
                var generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");
                var tokenResponse = await client.Tiles.GenerateTokenInGroupAsync(GroupId, dashboardId, tileId, generateTokenRequestParameters);

                // Generate Embed Configuration.
                var embedConfig = new TileEmbedConfig()
                {
                    EmbedToken = tokenResponse,
                    EmbedUrl = tile.EmbedUrl,
                    Id = tileId,
                    dashboardId = dashboardId
                };

                return View(embedConfig);
            }
        }

        public async Task<ActionResult> ListDatasets()
        {
            var viewModel = new DatasetsViewModel();
            using (var client = await CreatePowerBIClientForISV())
            {
                var datasets = await client.Datasets.GetDatasetsInGroupAsync(GroupId);
                viewModel.Datasets = datasets.Value.ToList();
            }

            return View(viewModel);
        }

        [Authorize]
        public async Task<ActionResult> CreateReport()
        {
            string datasetId = Request.Form["SelectedDataset"].ToString();
            using (var client = await CreatePowerBIClientForISV())
            {
                var dataset = await client.Datasets.GetDatasetByIdInGroupAsync(GroupId, datasetId);

                // Generate Embed Token.
                var generateTokenRequestParameters = new GenerateTokenRequest(
                    accessLevel: "create", datasetId: datasetId);
                var tokenResponse = await client.Reports.GenerateTokenForCreateInGroupAsync(GroupId, generateTokenRequestParameters);

                // Generate Embed Configuration.
                var embedConfig = new EmbedConfig()
                {
                    EmbedToken = tokenResponse,
                    EmbedUrl = EmbedUrlBase + "reportEmbed",
                    Id = datasetId,
                    Name = dataset.Name
                };

                return View(embedConfig);
            }
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
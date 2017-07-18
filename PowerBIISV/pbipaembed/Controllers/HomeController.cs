using System;
using System.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;
using System.Threading.Tasks;
using System.Linq;
using System.Web.Mvc;
using pbipaembed.Models;

namespace pbipaembed.Controllers
{
    public class HomeController : Controller
    {
        private static readonly string Username = ConfigurationManager.AppSettings["pbiUsername"];
        private static readonly string Password = ConfigurationManager.AppSettings["pbiPassword"];
        private static readonly string AuthorityUrl = ConfigurationManager.AppSettings["authorityUrl"];
        private static readonly string ResourceUrl = ConfigurationManager.AppSettings["resourceUrl"];
        private static readonly string ClientId = ConfigurationManager.AppSettings["clientId"];
        private static readonly string ApiUrl = ConfigurationManager.AppSettings["apiUrl"];
        private static readonly string GroupId = ConfigurationManager.AppSettings["groupId"];
        private static readonly string EmbedUrlBase = ConfigurationManager.AppSettings["embedUrlBase"];
        private static readonly UserPasswordCredential credential = new UserPasswordCredential(Username, Password);
        private static readonly AuthenticationContext authenticationContext = new AuthenticationContext(AuthorityUrl);

        public ActionResult Index()
        {
            return View();
        }

        private async Task<IPowerBIClient> CreatePowerBIClient()
        {
            var authenticationResult = await authenticationContext.AcquireTokenAsync(ResourceUrl, ClientId, credential);
            var tokenCredentials = new TokenCredentials(authenticationResult.AccessToken, "Bearer");
            var client = new PowerBIClient(new Uri(ApiUrl), tokenCredentials);

            return client;
        }

        public async Task<ActionResult> ListReports()
        {
            var viewModel = new ReportsViewModel();
            using (var client = await CreatePowerBIClient())
            {
                var reports = await client.Reports.GetReportsInGroupAsync(GroupId);
                viewModel.Reports = reports.Value.ToList();
            }

            return View(viewModel);
        }

        public async Task<ActionResult> EmbedReport()
        {
            string reportId = Request.Form["SelectedReport"].ToString();
            using (var client = await CreatePowerBIClient())
            {
                var report = client.Reports.GetReportInGroup(GroupId, reportId);

                // Generate Embed Token.
                var generateTokenRequestParameters = new GenerateTokenRequest(
                    accessLevel: "edit", allowSaveAs: true);
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
            using (var client = await CreatePowerBIClient())
            {
                var dashboards = await client.Dashboards.GetDashboardsInGroupAsync(GroupId);
                viewModel.Dashboards = dashboards.Value.ToList();
            }

            return View(viewModel);
        }

        public async Task<ActionResult> EmbedDashboard()
        {
            string dashboardId = Request.Form["SelectedDashboard"].ToString();
            using (var client = await CreatePowerBIClient())
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
            using (var client = await CreatePowerBIClient())
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
            using (var client = await CreatePowerBIClient())
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
            using (var client = await CreatePowerBIClient())
            {
                var datasets = await client.Datasets.GetDatasetsInGroupAsync(GroupId);
                viewModel.Datasets = datasets.Value.ToList();
            }

            return View(viewModel);
        }

        public async Task<ActionResult> CreateReport()
        {
            string datasetId = Request.Form["SelectedDataset"].ToString();
            using (var client = await CreatePowerBIClient())
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
    }
}
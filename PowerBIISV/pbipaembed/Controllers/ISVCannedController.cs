﻿using System;
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
    public class ISVCannedController : Controller
    {
        private static readonly string AuthorityUrl = ConfigurationManager.AppSettings["authorityUrl"];
        public static readonly string ResourceUrl = ConfigurationManager.AppSettings["resourceUrl"];
        private static readonly string ClientId = ConfigurationManager.AppSettings["clientId"];
        private static readonly string ApiUrl = ConfigurationManager.AppSettings["apiUrl"];
        private static readonly string GroupId = ConfigurationManager.AppSettings["groupId"];
        private static readonly string EmbedUrlBase = ConfigurationManager.AppSettings["embedUrlBase"];
        private static readonly AuthenticationContext authenticationContext = new AuthenticationContext(AuthorityUrl);

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
                GenerateTokenRequest generateTokenRequestParameters = new GenerateTokenRequest(
                            accessLevel: "edit", allowSaveAs: true); //no role specified means you can access everything!
                // Generate Embed Token.
                if (!tenantID.StartsWith("1"))  // the owner tenant of this app starts with 1, which can access everything
                {
                    switch (report.Name.ToLower())
                    {
                        case "adventureworks":
                            // for non Analysis Service, map user to Username()
                            var salesUser = new EffectiveIdentity(
                                username: "adventure-works\\pamela0",
                                roles: new List<string> { "Sales" },
                                datasets: new List<string> { report.DatasetId });
                            generateTokenRequestParameters = new GenerateTokenRequest(
                            accessLevel: "edit", allowSaveAs: true,
                            identities: new List<EffectiveIdentity> { salesUser });
                            break;
                        case "testaasrls":
                            // for Analysis Service, map user to CustomData()
                            var embedUser = new EffectiveIdentity(
                                username: MvcApplication.pbiUserName,
                                customData: "embed",
                                roles: new List<string> { "Sales Embedded" },
                                datasets: new List<string> { report.DatasetId });
                            generateTokenRequestParameters = new GenerateTokenRequest(
                            accessLevel: "edit", allowSaveAs: true,
                            identities: new List<EffectiveIdentity> { embedUser });
                            break;
                    }
                }
                else
                {
                    switch (report.Name.ToLower())
                    {
                        case "testaasrls":
                            // for Analysis Service, map user to CustomData()
                            var embedUser = new EffectiveIdentity(
                                username: MvcApplication.pbiUserName,
                                customData: "joe@acme.com",
                                roles: new List<string> { "Sales BiDi" },
                                datasets: new List<string> { report.DatasetId });
                            generateTokenRequestParameters = new GenerateTokenRequest(
                            accessLevel: "edit", allowSaveAs: true,
                            identities: new List<EffectiveIdentity> { embedUser });
                            break;
                    }
                }
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

            }
}
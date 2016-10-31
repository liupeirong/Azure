using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.PowerBI.Api.V1;
using Microsoft.PowerBI.Security;
using Microsoft.Rest;
using ISVWebApp.Models;
using System.Configuration;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.PowerBI.Api.V1.Models;

namespace ISVWebApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly string workspaceCollection;
        private readonly string workspaceId;
        private readonly string accessKey;
        private readonly string apiUrl;
        private readonly string adminTenant;

        public DashboardController()
        {
            this.workspaceCollection = ConfigurationManager.AppSettings["powerbi:WorkspaceCollection"];
            this.workspaceId = ConfigurationManager.AppSettings["powerbi:WorkspaceId"];
            this.accessKey = ConfigurationManager.AppSettings["powerbi:AccessKey"];
            this.apiUrl = ConfigurationManager.AppSettings["powerbi:ApiUrl"];
            this.adminTenant = ConfigurationManager.AppSettings["AdminTenant"];
        }

        public ActionResult Reports()
        {
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;

            using (var client = this.CreatePowerBIClient())
            {
                var viewModel = new ReportsViewModel();

                var reportsResponse = client.Reports.GetReports(this.workspaceCollection, this.workspaceId);
                if (tenantID != adminTenant)
                {
                    string tenantReportName = Startup.tenantODataMap[tenantID][1];
                    var report = reportsResponse.Value.FirstOrDefault(r => r.Name == tenantReportName);
                    viewModel.Reports = new List<Microsoft.PowerBI.Api.V1.Models.Report>();
                    viewModel.Reports.Add(report); 
                }
                else
                {
                    viewModel.Reports = reportsResponse.Value.ToList();
                }

                return View(viewModel);
            }
        }

        public async Task<ActionResult> Report(string reportId = "")
        {
            //string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            //string reportName = Startup.tenantODataMap[tenantID][1];
            using (var client = this.CreatePowerBIClient())
            {
                var reportsResponse = await client.Reports.GetReportsAsync(this.workspaceCollection, this.workspaceId);
                //var report = reportsResponse.Value.FirstOrDefault(r => (r.Id == reportId || r.Name == reportName));
                //var embedToken = PowerBIToken.CreateReportEmbedToken(this.workspaceCollection, this.workspaceId, report.Id);
                var report = reportsResponse.Value.FirstOrDefault(r => r.Id == reportId);
                var embedToken = PowerBIToken.CreateReportEmbedToken(this.workspaceCollection, this.workspaceId, reportId);

                var viewModel = new ReportViewModel
                {
                    Report = report,
                    AccessToken = embedToken.Generate(this.accessKey)
                };
                return View(viewModel);
            }
        }

        private IPowerBIClient CreatePowerBIClient()
        {
            var credentials = new TokenCredentials(accessKey, "AppKey");
            var client = new PowerBIClient(credentials)
            {
                BaseUri = new Uri(apiUrl)
            };

            return client;
        }
    }
}
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
using System.Security.Cryptography;

namespace ISVWebApp.Controllers
{
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

        [Authorize]
        public ActionResult Reports()
        {
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;

            using (var client = this.CreatePowerBIClient())
            {
                var viewModel = new ReportsViewModel();

                var reportsResponse = client.Reports.GetReports(this.workspaceCollection, this.workspaceId);
                if (tenantID == adminTenant)
                { 
                    viewModel.Reports = reportsResponse.Value.ToList();
                }
                else
                { 
                    string tenantReportName = Startup.tenantODataMap[tenantID][1];
                    var report = reportsResponse.Value.FirstOrDefault(r => r.Name == tenantReportName);
                    viewModel.Reports = new List<Microsoft.PowerBI.Api.V1.Models.Report>();
                    viewModel.Reports.Add(report);
                }

                return View(viewModel);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Report()
        {
            string reportId = Request.Form["SelectedReport"].ToString();
            using (var client = this.CreatePowerBIClient())
            {
                var reportsResponse = await client.Reports.GetReportsAsync(this.workspaceCollection, this.workspaceId);
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

        public ActionResult PublicReport()
        {
            string reportId = "69c06944-dcbb-4115-82b7-a98fd2a27d50";
            int expireDays = 1;

            int unixTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + expireDays * 3600;
            string pbieKey1 = "{\"typ\":\"JWT\",\"alg\":\"HS256\"}";
            string pbieKey2 = String.Format("{{\"wid\":\"{0}\",\"rid\":\"{1}\",\"wcn\":\"{2}\",\"iss\":\"PowerBISDK\",\"ver\":\"0.2.0\",\"aud\":\"{3}\",\"exp\":{4}}}",
                workspaceId, reportId, workspaceCollection, Startup.powerbiResourceId, unixTimestamp);
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
            return b64Str.Replace('/', '_').Replace('+', '-').TrimEnd(new char[] { '=' });
        }

        private string HMAC256EncryptBase64UrlEncode(string str)
        {
            var key = System.Text.Encoding.UTF8.GetBytes(accessKey);
            var strBytes = System.Text.Encoding.UTF8.GetBytes(str);
            HMACSHA256 enc = new HMACSHA256(key);
            var hashBytes = enc.ComputeHash(strBytes);
            string b64Str = System.Convert.ToBase64String(hashBytes);
            return b64Str.Replace('/', '_').Replace('+', '-').TrimEnd(new char[] { '=' });
        }
    }
}
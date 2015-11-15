using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;
using System.Configuration;
using System.Web.Configuration;
using System.Threading;
using Microsoft.Azure;
using Microsoft.Azure.Management.StreamAnalytics;
using Microsoft.Azure.Management.StreamAnalytics.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using DeploySAJob.Models;
using DeploySAJob.Utils;
using System.Security.Claims;

namespace DeploySAJob.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View(new SAJobConfigModel());
        }
        [HttpPost]
        public ActionResult Index(SAJobConfigModel saJobConfig)
        {
            if (ModelState.IsValid)
            {
                DeployASJob(saJobConfig);
                return RedirectToAction("About");
            }
            return View(saJobConfig);
        }
        private void DeployASJob(SAJobConfigModel cfg)
        {
            // Get authentication token
            TokenCloudCredentials aadTokenCredentials =
                new TokenCloudCredentials(cfg.subscriptionID, GetAuthorizationHeader());

            // Create Stream Analytics management client
            StreamAnalyticsManagementClient client = new StreamAnalyticsManagementClient(aadTokenCredentials);

            // Create a Stream Analytics job
            JobCreateOrUpdateParameters jobCreateParameters = new JobCreateOrUpdateParameters()
            {
                Job = new Job()
                {
                    Name = cfg.streamAnalyticsJobName,
                    Location = cfg.location,
                    Properties = new JobProperties()
                    {
                        EventsOutOfOrderPolicy = EventsOutOfOrderPolicy.Adjust,
                        Sku = new Sku()
                        {
                            Name = "Standard"
                        }
                    }
                }
            };

            JobCreateOrUpdateResponse jobCreateResponse = client.StreamingJobs.CreateOrUpdate(cfg.resourceGroupName, jobCreateParameters);
            TempData["jobName"] = jobCreateResponse.Job.Name;
            TempData["jobCreationStatus"] = jobCreateResponse.StatusCode;

            // Create a Stream Analytics input source
            InputCreateOrUpdateParameters jobInputCreateParameters = new InputCreateOrUpdateParameters()
            {
                Input = new Input()
                {
                    Name = cfg.streamAnalyticsInputName,
                    Properties = new StreamInputProperties()
                    {
                        Serialization = new CsvSerialization
                        {
                            Properties = new CsvSerializationProperties
                            {
                                Encoding = "UTF8",
                                FieldDelimiter = ","
                            }
                        },
                        DataSource = new EventHubStreamInputDataSource
                        {
                            Properties = new EventHubStreamInputDataSourceProperties
                            {
                                EventHubName = cfg.EventHubName,
                                ServiceBusNamespace = cfg.ServiceBusNamespace,
                                SharedAccessPolicyKey = cfg.SharedAccessPolicyKey,
                                SharedAccessPolicyName = cfg.SharedAccessPolicyName,
                            }
                        }
                    }
                }
            };

            InputCreateOrUpdateResponse inputCreateResponse =
                client.Inputs.CreateOrUpdate(cfg.resourceGroupName, cfg.streamAnalyticsJobName, jobInputCreateParameters);
            TempData["jobInputName"] = inputCreateResponse.Input.Name;
            TempData["jobInputCreationStatus"] = inputCreateResponse.StatusCode;

        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        private string GetAuthorizationHeader()
        {
            AuthenticationResult result = null;
            ClientCredential credential = new ClientCredential(Startup.clientId, Startup.appKey);
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

            AuthenticationContext authContext = new AuthenticationContext(Startup.aadInstance + tenantID, new NaiveSessionCache(signedInUserID));
            //this is getting the token from the cache inserted when the user initially signed in.
            result = authContext.AcquireTokenSilent(Startup.asaResourceId, credential, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));

            return result.AccessToken;
        }
    }
}
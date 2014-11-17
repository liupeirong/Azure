using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Diagnostics;
using ReportWebSite.Models;
using rs = ReportWebSite.ReportingService;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.ServiceModel.Configuration;
using Microsoft.WindowsAzure;

namespace ReportWebSite.Controllers
{
    public class ReportGenerateController : ApiController
    {
        const string reportPath = "/"; //ssrs path for the reports, there's a leading / for the reports in the root folder
        List<Report> reports;
        List<string> endpointNames;
        Dictionary<string, DateTime> blacklist;

        // GET: api/ReportGenerate
        public IEnumerable<Report> Get()
        {
            string reportServiceUser = CloudConfigurationManager.GetSetting("reportServiceUser");
            string reportServicePassword = CloudConfigurationManager.GetSetting("reportServicePassword");
            ClientSection clientSec = System.Configuration.ConfigurationManager.GetSection("system.serviceModel/client") as ClientSection;
            ChannelEndpointElementCollection endpointCollection = clientSec.Endpoints;
            endpointNames = new List<string>();
            foreach (ChannelEndpointElement endpointElement in endpointCollection)
            {
                if (String.Compare(endpointElement.Contract, "ReportingService.ReportingService2010Soap", true) == 0)
                    endpointNames.Add(endpointElement.Name);
            }
            int retry = endpointNames.Count;
            blacklist = new Dictionary<string, DateTime>();

            while (retry > 0)
            {
                string rsEndpoint = PickReportingService();
                try
                {
                    System.Net.NetworkCredential clientCredential = new System.Net.NetworkCredential(reportServiceUser, reportServicePassword);
                    rs.ReportingService2010SoapClient rsClient = new rs.ReportingService2010SoapClient(rsEndpoint);
                    rsClient.ClientCredentials.Windows.ClientCredential = clientCredential;
                    rsClient.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
                    rs.TrustedUserHeader userHeader = new rs.TrustedUserHeader();
                    rs.CatalogItem[] items;

                    Trace.TraceInformation("Connecting to reporting service " + rsEndpoint);
                    rsClient.Open();
                    Trace.TraceInformation("Connected to reporting service " + rsEndpoint);
                    rs.ServerInfoHeader infoHeader = rsClient.ListChildren(userHeader, reportPath, true, out items);
                    Trace.TraceInformation("Fetched reports from reporting service " + rsEndpoint);

                    reports = new List<Report>(items.Length);

                    foreach (var item in items)
                    {
                        if (String.Compare(item.TypeName, "Report", true) == 0)
                        {
                            Report it = new Report { Name = item.Name, Path = item.Path, ModifiedDate = item.ModifiedDate };
                            reports.Add(it);
                        }
                    }
                    rsClient.Close();
                    break;
                }
                catch (Exception e)
                {
                    Trace.TraceError("Failed to fetch reports from reporting service. " + e.Message);
                    blacklist[rsEndpoint] = DateTime.Now;
                    --retry;
                    if (retry == 0) throw;
                }
            }

            return reports;
        }

        // Put: api/ReportGenerate
        public void Put(ReportRequest req)
        {
            string queuePath = CloudConfigurationManager.GetSetting("queuePath");
            string queueConnectionString = CloudConfigurationManager.GetSetting("queueConnectionString");

            NamespaceManager nsm = NamespaceManager.CreateFromConnectionString(queueConnectionString);

            //create queue if it doesn't exist
            if (!nsm.QueueExists(queuePath))
            {
                Trace.TraceInformation("Creating queue: {0}...", queuePath);
                QueueDescription qd = new QueueDescription(queuePath);
                qd.RequiresDuplicateDetection = true; //this can't be changed later
                qd = nsm.CreateQueue(qd);
                Trace.TraceInformation("Queue created.");
            }

            //insert the Path to the queue
            QueueClient queueClient = QueueClient.CreateFromConnectionString(queueConnectionString, queuePath);
            BrokeredMessage bm = new BrokeredMessage();
            bm.Properties.Add("reportReq", req.Path);
            
            Trace.TraceInformation("Sending report gen request: {0}", req.Path);
            queueClient.Send(bm);
            Trace.TraceInformation("Request queued.");
            queueClient.Close();
        }

        private string PickReportingService()
        {
            foreach (var key in blacklist.Keys)
            {
                TimeSpan ts = DateTime.Now - blacklist[key];
                if (ts.TotalMinutes > 10)
                    blacklist.Remove(key);
            }
            if (blacklist.Count > 0) //pick one that's not blacklisted
            {
                foreach (string ep in endpointNames)
                {
                    if (!blacklist.ContainsKey(ep))
                        return ep; 
                }
            }
            //all blacklisted or non blacklisted, pick a random one
            Random rnd = new Random(DateTime.Now.Millisecond);
            int choice = rnd.Next(0, endpointNames.Count - 1);
            return endpointNames[choice];
        }
    }
}

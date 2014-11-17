using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Web;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Collections.Specialized;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.IO;

namespace WorkerRoleWithSBQueue1
{
    public class WorkerRole : RoleEntryPoint
    {
        string [] reportServers;
        System.Net.NetworkCredential clientCredential;
        CloudBlobContainer container;
        string queueConnectionString;
        string queuePath;
        int maxNumOfReportsArchived;
        const string reportPath = "/"; //ssrs path for the reports, there's a leading / for the reports in the root folder
        const string reportBlobPath = "latest/";  //blob path for the reports, there's no leading / for the reports in the root folder
        const string archiveBlobPath = "archive/";
        const int msgLockDurationInMinutes = 5;
        const int updateLockLeaseInMilliSec = 30000;  //must update lock more frequent than lock duration
        const int msgMaxDeliveryCount = 3;
        Dictionary<string, DateTime> blacklist;

        // QueueClient is thread-safe. Recommended that you cache 
        // rather than recreating it on every request
        QueueClient Client;
        ManualResetEvent CompletedEvent = new ManualResetEvent(false);

        static void Main(string[] args) //console entry point
        {
            WorkerRole wr = new WorkerRole();
            if (wr.OnStart())
                wr.Run();
            wr.OnStop();
        }
        public override void Run()
        {
            Trace.TraceInformation("Worker started processing of messages");

            // Initiates the message pump and callback is invoked for each message that is received, calling close on the client will stop the pump.
            Client.OnMessage((msg) =>
                {
                    Timer renewTimer = new Timer(UpdateLock, msg, 1000, updateLockLeaseInMilliSec); //every 30 seconds after 1 sec
                    try
                    {
                        // Process the message
                        Trace.TraceInformation("Processing Service Bus message: " + msg.SequenceNumber.ToString());
                        string reportReq = (string)msg.Properties["reportReq"];
                            if (!CreateReport(reportReq))
                                msg.DeadLetter();
                        msg.Complete();
                    }
                    catch (Exception e)
                    {
                        // Handle any message processing specific exceptions here
                        Trace.TraceError("Message processing exception: " + e.Message);
                        msg.Abandon();
                    }
                    renewTimer.Dispose();
                });

            CompletedEvent.WaitOne();
        }

        public override bool OnStart()
        {
            //if we don't do this, Azure tracelistener only runs in the cloud, and we can't run this app standalone.
            Dictionary<string, SourceLevels> traceLevelMap = new Dictionary<string, SourceLevels>()
            {
                {"All", SourceLevels.All},
                {"Critical", SourceLevels.Critical},
                {"Error", SourceLevels.Error},
                {"Warning", SourceLevels.Warning},
                {"Information", SourceLevels.Information},
                {"ActivityTracing", SourceLevels.ActivityTracing},
                {"Verbose", SourceLevels.Verbose},
                {"Off", SourceLevels.Off}
            };
            if (RoleEnvironment.IsAvailable)
            {
                string cloudTraceLevel = CloudConfigurationManager.GetSetting("cloudTraceLevel");
                SourceLevels sl = traceLevelMap.ContainsKey(cloudTraceLevel) ? traceLevelMap[cloudTraceLevel] : SourceLevels.Error;

                Microsoft.WindowsAzure.Diagnostics.DiagnosticMonitorTraceListener tr = 
                    new Microsoft.WindowsAzure.Diagnostics.DiagnosticMonitorTraceListener();
                tr.Filter = new EventTypeFilter(sl);
                    
                Trace.Listeners.Add(tr);
                Trace.AutoFlush = true;
            }
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Initialize the connection to Service Bus Queue
            Initialize();
            return base.OnStart();
        }

        public override void OnStop()
        {
            // Close the connection to Service Bus Queue
            Client.Close();
            CompletedEvent.Set();
            base.OnStop();
        }

        private void UpdateLock(Object message)
        {
            BrokeredMessage msg = (BrokeredMessage)message;
            msg.RenewLock();
        }

        private void Initialize()
        {
            char[] separators = new char [] {';'};
            try
            {
                //fetch configuration for the report server and blob storage
                string reportServerURL = CloudConfigurationManager.GetSetting("reportServerURL");
                reportServers = reportServerURL.Split(separators);
                clientCredential = new System.Net.NetworkCredential(
                    CloudConfigurationManager.GetSetting("reportServerUser"), 
                    CloudConfigurationManager.GetSetting("reportServerPassword"));
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    CloudConfigurationManager.GetSetting("blobConnectionString"));
                string reportBlobContainer = CloudConfigurationManager.GetSetting("reportBlobContainer");
                maxNumOfReportsArchived = int.Parse(CloudConfigurationManager.GetSetting("maxNumOfReportsArchived"));
                queuePath = CloudConfigurationManager.GetSetting("queuePath");
                queueConnectionString = CloudConfigurationManager.GetSetting("queueConnectionString");
                NamespaceManager nsm = NamespaceManager.CreateFromConnectionString(queueConnectionString);
                blacklist = new Dictionary<string, DateTime>();

                //create queue if it doesn't exist
                if (!nsm.QueueExists(queuePath))
                {
                    Trace.TraceInformation("Creating queue: {0}...", queuePath);
                    QueueDescription qd = new QueueDescription(queuePath);
                    qd.LockDuration = TimeSpan.FromMinutes(msgLockDurationInMinutes);
                    qd.MaxDeliveryCount = msgMaxDeliveryCount;
                    qd.RequiresDuplicateDetection = true; //this can't be changed later
                    qd = nsm.CreateQueue(qd);
                    Trace.TraceInformation("Queue created.");
                }
                else //otherwise make sure it has all the correct settings
                {
                    bool changed = false; 
                    QueueDescription qd = nsm.GetQueue(queuePath);
                    if (qd.LockDuration < TimeSpan.FromMinutes(msgLockDurationInMinutes))
                    {
                        qd.LockDuration = TimeSpan.FromMinutes(msgLockDurationInMinutes);
                        changed = true;
                    }
                    if (qd.MaxDeliveryCount < msgMaxDeliveryCount)
                    {
                        qd.MaxDeliveryCount = msgMaxDeliveryCount;
                        changed = true;
                    }
                    if (changed)
                    {
                        nsm.UpdateQueue(qd);
                        Trace.TraceInformation("Worker updated queue settings.");
                    }
                }
                Client = QueueClient.CreateFromConnectionString(queueConnectionString, queuePath);

                //create blob container and set permission
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                container = blobClient.GetContainerReference(reportBlobContainer);
                container.CreateIfNotExists();
                BlobContainerPermissions containerPermissions = new BlobContainerPermissions();
                containerPermissions.PublicAccess = BlobContainerPublicAccessType.Blob;
                container.SetPermissions(containerPermissions);
            }
            catch (Exception e)
            {
                Trace.TraceError("Report processor initialization failed: " + e.Message);
                throw;
            }
        }

        private bool CreateReport(string reportReq)
        {
            bool success = false;
            //sanity check request
            string reportName;
            string reportPath;
            string fileExt = GetFileExtFromReportRequest(reportReq, out reportPath, out reportName);
            if (fileExt == null)
            {
                Trace.TraceError("Ignore invalid request: " + reportReq);
            }
            else
            {
                Trace.TraceInformation("Process valid request: " + reportReq);

                //did the report exist and we don't have to create anything?
                string fullBlobPath = (reportBlobPath + (
                    (reportBlobPath.EndsWith("/") && reportPath.StartsWith("/")) ? reportPath.Substring(1) : reportPath
                    ) + reportName + fileExt).ToLower();
                CloudBlockBlob blockReport = container.GetBlockBlobReference(fullBlobPath);
                bool reportExists = blockReport.Exists();

                //get the report from ssrs
                bool reportServerFailure = true;
                int retry = reportServers.Length;
                while (reportServerFailure && retry > 0)
                {
                    string reportServer = PickReportingService();
                    string fullPath = reportServer + (
                            (reportServer.EndsWith("/") && reportReq.StartsWith("/")) ? reportReq.Substring(1) : reportReq
                            );
                    try
                    {
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(fullPath);
                        req.Credentials = clientCredential;
                        req.ImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
                        using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                        {
                            HttpStatusCode statusCode = resp.StatusCode;
                            if (statusCode == HttpStatusCode.OK)
                            {
                                //streaming to blob
                                using (Stream objStream = resp.GetResponseStream())
                                {
                                    reportServerFailure = false;
                                    if (reportExists)
                                    {
                                        ArchiveOldReport(reportPath + reportName, reportName, fileExt, blockReport);
                                    }
                                    blockReport.UploadFromStream(objStream);
                                }
                            }
                            else
                            {
                                Trace.TraceError("Report server http status: " + statusCode);
                                --retry;
                                blacklist[reportServer] = DateTime.Now;
                            }
                        }
                    } catch (Exception e)
                    {
                        Trace.TraceError("Create report exception: " + e.Message);
                        if (reportServerFailure)
                        {
                            --retry;
                            blacklist[reportServer] = DateTime.Now;
                        }
                        else throw;
                    }
                }
                Trace.TraceInformation("Report created successfully. " + reportReq);
                success = true;
            }
            return success;
        }

        private string PickReportingService()
        {
            //sweep the blacklist and release everything older than 10 minutes
            foreach(var key in blacklist.Keys)
            {
                TimeSpan ts = DateTime.Now - blacklist[key];
                if (ts.TotalMinutes > 10)
                    blacklist.Remove(key);
            }   
            if (blacklist.Count > 0) //pick one that's not blacklisted
            {
                for (int ii = 0; ii < reportServers.Length; ++ii)
                {
                    if (!blacklist.ContainsKey(reportServers[ii]))
                        return reportServers[ii];
                }
            }
            //all blacklisted, or non blacklisted, pick a random one.
            Random rnd = new Random(DateTime.Now.Millisecond);
            int choice = rnd.Next(0, reportServers.Length - 1);
            return reportServers[choice];
        }
        static string GetFileExtFromReportRequest(string reportReq, out string reportPath, out string reportName)
        {
            Dictionary<string, string> reportFormat2fileExt = new Dictionary<string, string>()
                {
                    {"PDF", ".pdf"},
                    {"HTML4.0", ".html"},
                    {"MHTML", ".mhtml"},
                    {"EXCEL", ".xlsx"},
                    {"CSV", ".csv"},
                    {"WORD", ".docx"},
                    {"XML", ".xml"}
                };

            string fileExt = null;
            reportName = null;
            reportPath = null;
            try
            {
                NameValueCollection qscol = HttpUtility.ParseQueryString(reportReq);
                string reportFull = qscol[0];
                int separator = reportFull.LastIndexOf("/");
                if (separator < 0)
                {
                    reportName = reportFull;
                    reportPath = "";
                }
                else  //the report is in an folder
                {
                    if (!reportFull.StartsWith("/"))
                    {
                        Trace.TraceError("report in a folder but path not start with /:" + reportFull);
                        return fileExt;
                    }
                    reportName = reportFull.Substring(separator + 1);
                    reportPath = reportFull.Substring(0, separator + 1);
                }
                foreach (string param in qscol.AllKeys)
                {
                    if (String.Compare(param, "rs:Format", true) == 0)
                    {
                        string format = qscol[param].ToUpper();
                        fileExt = reportFormat2fileExt[format];
                        break;
                    }
                    else if (String.Compare(param, "rs:Command", true) == 0)
                    {
                        if (String.Compare(qscol[param], "render", true) != 0)
                        {
                            Trace.TraceError("Not a report render command:" + qscol[param]);
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("GetFilePathFromReportRequest exception:" + e);
            }

            return fileExt;
        }
        private void ArchiveOldReport(string reportPath, string reportName, string fileExt, CloudBlockBlob report)
        {
            if (!report.Exists()) return;
            report.FetchAttributes();

            string archiveDirString = (archiveBlobPath + (
                    (archiveBlobPath.EndsWith("/") && reportPath.StartsWith("/")) ?
                        reportPath.Substring(1) : reportPath
                )).ToLower();
            if (!archiveDirString.EndsWith("/"))
                archiveDirString = archiveDirString + "/";
            CloudBlobDirectory archiveDir = container.GetDirectoryReference(archiveDirString);
            //if the archive path exists, see if we need to delete the oldest blob in the path
            IEnumerable<IListBlobItem> blobs = archiveDir.ListBlobs(true);
            Dictionary<DateTimeOffset, string> oldReports = new Dictionary<DateTimeOffset, string>();
            DateTimeOffset oldest = DateTimeOffset.MaxValue;
            ICloudBlob oldestBlob = null;
            int count = 0;
            foreach (ICloudBlob blob in blobs)
            {
                ++count;
                if (blob.Properties.LastModified.Value < oldest)
                {
                    oldest = blob.Properties.LastModified.Value;
                    oldestBlob = blob;
                }
            }
            if (count >= maxNumOfReportsArchived)
                oldestBlob.Delete();

            //copy report to archive
            string newUri = (archiveDirString + reportName + "_" + report.Properties.LastModified.Value.ToString("u") + fileExt).ToLower();
            CloudBlockBlob newReport = container.GetBlockBlobReference(newUri);
            newReport.StartCopyFromBlob(report);
            int retries = 3;
            while (retries > 0)
            {
                if (newReport.CopyState.Status == CopyStatus.Success)
                {
                    Trace.TraceInformation("Blob archiving succeeded: " + newUri);
                    break;
                }
                else if (newReport.CopyState.Status == CopyStatus.Aborted ||
                         newReport.CopyState.Status == CopyStatus.Failed ||
                         newReport.CopyState.Status == CopyStatus.Invalid)
                {
                    Trace.TraceError("Blob archiving failed: " + newUri);
                    break;
                }
                else
                {
                    Thread.Sleep(1000);
                    --retries;
                    if (retries == 0) Trace.TraceError("Blob archiving timed out: " + newUri);
                }
            }
        }
    }
}

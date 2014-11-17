using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Specialized;
using System.Web;
using System.IO;
using System.Net;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.Threading;

namespace AzureSQLReportMgr
{
    public class ReportProcessor
    {
        String reportServerURL;
        System.Net.NetworkCredential clientCredential;
        CloudBlobContainer container;
        String queueConnectionString;
        String queuePath;
        int maxNumOfReportsArchived;
        const string reportPath = "/"; //ssrs path for the reports, there's a leading / for the reports in the root folder
        const string reportBlobPath = "latest/";  //blob path for the reports, there's no leading / for the reports in the root folder
        const string archiveBlobPath = "archive/";

        public ReportProcessor()
        { }

        public bool Initialize()
        {
            bool initOK = false;
            try
            {
                //fetch configuration for the report server and blob storage
                //for secureAppSettings, rename app.config to web.config, then use the following command to encrypt the section
                //%WINDOWS%\Microsoft.NET\Framework64\v4.0.30319\aspnet_regiis -pef secureAppSettings <folder, can be .>
                NameValueCollection secureAppSettings = (NameValueCollection)ConfigurationManager.GetSection("secureAppSettings");
                reportServerURL = ConfigurationManager.AppSettings["reportServerURL"];
                clientCredential = new System.Net.NetworkCredential(
                    ConfigurationManager.AppSettings["reportServerUser"], secureAppSettings["reportServerPassword"]);
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(secureAppSettings["blobConnectionString"]);
                String reportBlobContainer = ConfigurationManager.AppSettings["reportBlobContainer"];
                maxNumOfReportsArchived = int.Parse(ConfigurationManager.AppSettings["maxNumOfReportsArchived"]);
                queuePath = ConfigurationManager.AppSettings["queuePath"];
                queueConnectionString = ConfigurationManager.AppSettings["queueConnectionString"];
                NamespaceManager nsm = NamespaceManager.CreateFromConnectionString(queueConnectionString);

                //create queue if it doesn't exist
                if (!nsm.QueueExists(queuePath))
                {
                    QueueDescription qd = new QueueDescription(queuePath);
                    qd = nsm.CreateQueue(qd);
                }

                //create blob container and set permission
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                container = blobClient.GetContainerReference(reportBlobContainer);
                container.CreateIfNotExists();
                BlobContainerPermissions containerPermissions = new BlobContainerPermissions();
                containerPermissions.PublicAccess = BlobContainerPublicAccessType.Blob;
                container.SetPermissions(containerPermissions);

                initOK = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return initOK;
        }

        public void TestSendMsg(string msg)
        {
            QueueClient queueClient = QueueClient.CreateFromConnectionString(queueConnectionString, queuePath);
            BrokeredMessage bm = new BrokeredMessage();
            bm.Properties.Add("reportReq", msg);
            queueClient.Send(bm);
            queueClient.Close();
        }

        public void ProcessMsg()
        {
            QueueClient queueClient = QueueClient.CreateFromConnectionString(queueConnectionString, queuePath);
            while (true)
            {
                BrokeredMessage msg = queueClient.Receive();
                bool success = false;
                if (msg != null)
                {
                    try
                    {
                        String reportReq = (string) msg.Properties["reportReq"];
                        success = CreateReport(reportReq);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("ProcessMsg: " + e);
                    }
                    if (success)
                        msg.Complete();
                    else
                        msg.Abandon();
                }
            }
        }

        public void ArchiveOldReport(String reportPath, String reportName, String fileExt, CloudBlockBlob report)
        {
            if (!report.Exists()) return;

            string archiveDirString = (archiveBlobPath + (
                    (archiveBlobPath.EndsWith("/") && reportPath.StartsWith("/")) ?
                        reportPath.Substring(1) : reportPath
                )).ToLower();
            if (!archiveDirString.EndsWith("/"))
                archiveDirString = archiveDirString + "/";
            CloudBlobDirectory archiveDir = container.GetDirectoryReference(archiveDirString);
            //if the archive path exists, see if we need to delete the oldest blob in the path
            IEnumerable<IListBlobItem> blobs = archiveDir.ListBlobs();
            if (blobs.Count() >= maxNumOfReportsArchived)
            {
                Dictionary<DateTimeOffset, string> oldReports = new Dictionary<DateTimeOffset, string>();

                DateTimeOffset oldest = DateTimeOffset.MaxValue;
                ICloudBlob oldestBlob = null;
                foreach (ICloudBlob blob in blobs)
                {
                    if (blob.Properties.LastModified.Value < oldest)
                    {
                        oldest = blob.Properties.LastModified.Value;
                        oldestBlob = blob;
                    }
                }
                oldestBlob.Delete();
            }

            //copy report to archive
            String newUri = (archiveDirString + reportName + "_" + report.Properties.LastModified.Value.ToString("u") + fileExt).ToLower();
            CloudBlockBlob newReport = container.GetBlockBlobReference(newUri);
            newReport.StartCopyFromBlob(report);
            int retries = 3;
            while (true && retries > 0)
            {
                if (newReport.CopyState.Status == CopyStatus.Success)
                {
                    Console.WriteLine("Blob archiving succeeded: " + newUri);
                    break;
                }
                else if (newReport.CopyState.Status == CopyStatus.Aborted ||
                         newReport.CopyState.Status == CopyStatus.Failed ||
                         newReport.CopyState.Status == CopyStatus.Invalid)
                {
                    Console.WriteLine("Blob archiving failed: " + newUri);
                    break;
                }
                else
                {
                    Thread.Sleep(3000);
                    --retries;
                }
            }
        }

        public bool CreateReport(string reportReq)
        {
            bool success = false;
            //sanity check request
            String reportName;
            String reportPath;
            String fileExt = GetFileExtFromReportRequest(reportReq, out reportPath, out reportName);
            if (fileExt == null)
            {
                Console.WriteLine("Ignore invalid request: " + reportReq);
            }
            else
            {
                Console.WriteLine("Process valid request: " + reportReq);

                try
                {
                    //did the report exist and we don't have to create anything?
                    string fullBlobPath = (reportBlobPath + (
                        (reportBlobPath.EndsWith("/") && reportPath.StartsWith("/")) ? reportPath.Substring(1) : reportPath
                        ) + reportName + fileExt).ToLower();
                    CloudBlockBlob blockReport = container.GetBlockBlobReference(fullBlobPath);
                    bool reportExists = blockReport.Exists();
                    //get the report from ssrs
                    string fullPath = reportServerURL + (
                            (reportServerURL.EndsWith("/") && reportReq.StartsWith("/")) ? reportReq.Substring(1) : reportReq
                            );
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(fullPath);
                    req.Credentials = clientCredential;
                    req.ImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    {
                        HttpStatusCode statusCode = resp.StatusCode;
                        Console.WriteLine("Report http status: " + statusCode);
                        if (statusCode == HttpStatusCode.OK)
                        {
                            //streaming to blob
                            using (Stream objStream = resp.GetResponseStream())
                            {
                                if (reportExists)
                                {
                                    ArchiveOldReport(reportPath + reportName, reportName, fileExt, blockReport);
                                }
                                blockReport.UploadFromStream(objStream);
                            }
                        }
                    }
                    success = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("CreateReport exception:" + e);
                }
            }
            return success;
        }

        static String GetFileExtFromReportRequest(String reportReq, out String reportPath, out String reportName)
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

            String fileExt = null;
            reportName = null;
            reportPath = null;
            try
            {
                NameValueCollection qscol = HttpUtility.ParseQueryString(reportReq);
                String reportFull = qscol[0];
                int separator = reportFull.LastIndexOf("/");
                if (separator < 0 )
                {
                    reportName = reportFull;
                    reportPath = "";
                }
                else  //the report is in an folder
                {
                    if (!reportFull.StartsWith("/"))
                    {
                        Console.WriteLine("report in a folder but path not start with /:" + reportFull);
                        return fileExt;
                    }
                    reportName = reportFull.Substring(separator + 1);
                    reportPath = reportFull.Substring(0, separator + 1);
                }
                foreach (String param in qscol.AllKeys)
                {
                    if (String.Compare(param, "rs:Format", true) == 0)
                    {
                        String format = qscol[param].ToUpper();
                        fileExt = reportFormat2fileExt[format];
                        break;
                    } 
                    else if (String.Compare(param, "rs:Command", true) == 0)
                    {
                        if (String.Compare(qscol[param], "render", true) != 0)
                        {
                            Console.WriteLine("Not a report render command:" + qscol[param]);
                            break;
                        }
                    }
                }
            }catch(Exception e)
            {
                Console.WriteLine("GetFilePathFromReportRequest exception:" + e);
            }

            return fileExt;
        }
    }
}


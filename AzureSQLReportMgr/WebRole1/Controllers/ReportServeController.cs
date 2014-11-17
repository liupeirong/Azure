using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using ReportWebSite.Models;
using Microsoft.WindowsAzure;

namespace ReportWebSite.Controllers
{
    public class ReportServeController : ApiController
    {
        const string reportBlobPath = "latest/";  //blob path for the reports, there's no leading / for the reports in the root folder
        const string archiveBlobPath = "archive/";

        // GET: api/ReportServe/GetLatest
        public IEnumerable<StoredReport> GetLatest()
        {
            return GetLatestInternal();
        }

        // GET: api/ReportServe/GetLatest/Name
        public IEnumerable<StoredReport> GetLatest(string name)
        {
            return GetLatestInternal(name);
        }

        // GET: api/ReportServe/GetArchive
        public IEnumerable<StoredReport> GetArchive()
        {
            return GetArchiveInternal();
        }

        // GET: api/ReportServe/GetArchive/Name
        public IEnumerable<StoredReport> GetArchive(string name)
        {
            return GetArchiveInternal(name);
        }

        private IEnumerable<StoredReport> GetLatestInternal(string reportName = null)
        {
            IList<StoredReport> reports = null;

            string blobConnectionString = CloudConfigurationManager.GetSetting("blobConnectionString");
            string reportBlobContainer = CloudConfigurationManager.GetSetting("reportBlobContainer");
            string blobEndPoint = CloudConfigurationManager.GetSetting("blobEndPoint");
            string blobPrefix = blobEndPoint + reportBlobContainer + "/";

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(blobConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(reportBlobContainer);

            if (!container.Exists()) return null;
            CloudBlobDirectory reportDir = container.GetDirectoryReference(reportBlobPath);

            IEnumerable<IListBlobItem> blobs = reportDir.ListBlobs(true);
            foreach (ICloudBlob blob in blobs)
            {
                string thisReportName, thisReportFormat;
                thisReportName = blob.Name.Substring(reportBlobPath.Length - 1);
                int extStarts = thisReportName.LastIndexOf(".");
                thisReportFormat = thisReportName.Substring(extStarts+1);
                thisReportName = thisReportName.Substring(0, extStarts);
                if ((reportName != null) && (String.Compare(thisReportName, reportName, true) != 0))
                    continue;
                StoredReport report = new StoredReport();
                report.Name = thisReportName;
                report.Format = thisReportFormat;
                report.Path = blobPrefix + blob.Name;
                report.ModifiedDate = blob.Properties.LastModified == null ?
                    DateTime.MinValue : ((DateTimeOffset)blob.Properties.LastModified).UtcDateTime;
                if (reports == null) reports = new List<StoredReport>();
                reports.Add(report);
            }

            return reports;
        }

        private IEnumerable<StoredReport> GetArchiveInternal(string reportName = null)
        {
            IList<StoredReport> reports = null;

            string blobConnectionString = CloudConfigurationManager.GetSetting("blobConnectionString");
            string reportBlobContainer = CloudConfigurationManager.GetSetting("reportBlobContainer");
            string blobEndPoint = CloudConfigurationManager.GetSetting("blobEndPoint");
            string blobPrefix = blobEndPoint + reportBlobContainer + "/";

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(blobConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(reportBlobContainer);

            if (!container.Exists()) return null;

            string reportArchiveDir = archiveBlobPath;
            if (reportName != null)
            {
                reportArchiveDir += 
                    ((archiveBlobPath.EndsWith("/") && reportName.StartsWith("/")) ? reportName.Substring(1) : reportName);
            }
            if (!reportArchiveDir.EndsWith("/"))  reportArchiveDir += "/";
            reportArchiveDir.ToLower();
            CloudBlobDirectory archiveDir = container.GetDirectoryReference(reportArchiveDir);
            IEnumerable<IListBlobItem> archBlobs = archiveDir.ListBlobs(true);
            foreach (ICloudBlob archBlob in archBlobs)
            {
                string thisReportName, thisReportFormat;
                thisReportName = archBlob.Name.Substring(archiveBlobPath.Length - 1);
                int extStarts = thisReportName.LastIndexOf(".");
                thisReportFormat = thisReportName.Substring(extStarts+1);
                thisReportName = thisReportName.Substring(0, extStarts);
                StoredReport report = new StoredReport();
                report.Name = thisReportName;
                report.Format = thisReportFormat;
                report.Path = blobPrefix + archBlob.Name;
                report.ModifiedDate = archBlob.Properties.LastModified == null ? 
                    DateTime.MinValue : ((DateTimeOffset)archBlob.Properties.LastModified).UtcDateTime;
                if (reports == null) reports = new List<StoredReport>();
                reports.Add(report);
            }

            return reports;
        }
    }
}

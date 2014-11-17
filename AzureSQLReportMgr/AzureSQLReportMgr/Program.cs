using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using rs = AzureSQLReportMgr.ReportingService;
using rsexec = AzureSQLReportMgr.ReportExecutionService;

using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureSQLReportMgr
{
    class Program
    {
        static void ReportWS()
        {
            bool createReport = false;
            string reportPath = "/";

            NameValueCollection secureAppSettings = (NameValueCollection)ConfigurationManager.GetSection("secureAppSettings");
            System.Net.NetworkCredential clientCredential = new System.Net.NetworkCredential(
                ConfigurationManager.AppSettings["reportServerUser"], secureAppSettings["reportServerPassword"]);

            rs.ReportingService2010SoapClient rsClient = new rs.ReportingService2010SoapClient();
            rsClient.ClientCredentials.Windows.ClientCredential = clientCredential;
            rsClient.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
            rs.TrustedUserHeader userHeader = new rs.TrustedUserHeader();
            rs.CatalogItem[] reports;

            rsClient.Open();
            rs.ServerInfoHeader infoHeader = rsClient.ListChildren(userHeader, reportPath, true, out reports);

            //for creating report
            string reportFormat = "PDF";
            string reportOutputFileExt = ".pdf";
            string historyID = null;
            string deviceInfo = null;
            string reportOutputPath = "c:\\temp\\";
            rsexec.ReportExecutionServiceSoapClient rsExecClient = new rsexec.ReportExecutionServiceSoapClient();
            rsExecClient.ClientCredentials.Windows.ClientCredential = clientCredential;
            rsExecClient.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;

            foreach (var report in reports)
            {
                Console.WriteLine(String.Format("{0}:{1}:{2}:{3}", report.Name, report.ModifiedDate, report.Path, report.TypeName));

                if (createReport) 
                {
                    rsexec.ExecutionHeader execHeader = new rsexec.ExecutionHeader();
                    rsexec.TrustedUserHeader execUserHeader = new rsexec.TrustedUserHeader();
                    rsexec.ExecutionInfo execInfo = new rsexec.ExecutionInfo();
                    rsexec.ServerInfoHeader serviceInfo = new rsexec.ServerInfoHeader();
                    rsExecClient.LoadReport(execUserHeader, reportPath + report.Name, historyID, out serviceInfo, out execInfo);
                    execHeader.ExecutionID = execInfo.ExecutionID;

                    //rsexec.ParameterValue[] execParams = new rsexec.ParameterValue[2];
                    //rsExecClient.SetExecutionParameters(execHeader, execUserHeader, null, "en-us", out execInfo);

                    string extension;
                    string encoding;
                    string mimeType;
                    rsexec.Warning[] warnings = new rsexec.Warning[1];
                    warnings[0] = new rsexec.Warning();
                    string[] streamIDs = null;
                    Byte[] result;

                    rsExecClient.Render(execHeader, execUserHeader, reportFormat, deviceInfo, out result, out extension, out mimeType, out encoding, out warnings, out streamIDs);
                    string fileName = reportOutputPath + report.Name + reportOutputFileExt;
                    FileStream stream = File.OpenWrite(fileName);
                    stream.Write(result, 0, result.Length);
                    stream.Close();
                }
            }
            rsExecClient.Close();
            rsClient.Close();
        }

        static void Main(string[] args)
        {
            ReportProcessor rp = new ReportProcessor();
            if (!rp.Initialize()) return;

            //String reportReq = "/personal best/test&rs:Format=PDF";
            //String reportReq = "Company Sales SQL2008R2&rs:Format=PDF";
            //rp.CreateReport(reportReq);

            //rp.TestSendMsg(reportReq);
            rp.ProcessMsg();
        }
    }
}

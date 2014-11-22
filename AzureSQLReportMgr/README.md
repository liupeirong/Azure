# Azure SQL Server Reporting Manager Sample

This sample demonstrates how to issue a request from a web site or REST API to generate a SQL Server Reporting Service report and fetch or view the report generated in an Azure blob.  The request is placed in an Azure service bus queue, and a worker role listens on the queue to process any request.  With this architecture, the web site, the worker role, and the reporting service can be individually scaled. Main functional capabilities include the following:

  - generate SQL Server Reporting Service (SSRS) reports through a REST API
  - generate SSRS reports and retrieve generated reports from a web site
  - archive older reports as newer versions of the same reports are generated

### Usage

Download the Visual Studio solution and update the following configurations. The solution is built such that you can either deploy the cloud service to Azure and run it there, or run the Web application and the worker role console application standalone on your dev machine for efficient development and debugging.  For this reason, you need to update cscfg files for cloud deployment, and web.config and app.config for dev machine deployment. Specifically, search for the term "your" in the following configuration files:

  - ServiceConfiguration.Cloud.cscfg
  - ServiceCOnfiguration.Local.cscfg
  - Web.config
  - App.config

Provide the following configurations:

```java
reportServerURL        //this is the URL of the SQL Server Reporting Service (SSRS)
reportServiceUser      //this is user name to be used to access SSRS
reportServicePassword  //this is the password for the user to access SSRS
queueConnectionString  //this is the Azure Serivce Bus queue where requests will be placed for worker role to process
blobConnectionString   //this is the Azure Storage Account in which reports will be generated
blobEndPoint           //this is the blob endpoint URL that forms the links to the generated reports

//SSRS web service endpoint used for Service Reference, for example, 
<endpoint address="http://yourssrs/ReportServer/ReportService2010.asmx" binding="basicHttpBinding" bindingConfiguration="ReportingService2010Soap" contract="ReportingService.ReportingService2010Soap" name="ReportingServiceEP1" />
```

The SSRS must have reports defined and loaded.  There is no custom code running on SSRS, however, SSRS must be configured to allow [URL access].  You can configure multiple SSRS servers to balance the load.  The reporting manager will randomly choose one if all SSRS servers are healthy or unhealthy.  Otherwise, it will quarantine the unhealthy ones for a certain period of time before putting them back to service. 

Date and time are stored internally as UTC, but rendered as local time in the browser based on the end user’s locale settings. 

### Todo's

There is no UI to input report parameters right now.  So the UI can only be used to generate reports without parameters or with default parameter values.  You can, however, use the REST API to supply parameters the same way as SSRS [URL access].

License
----

MIT

[URL access]:http://msdn.microsoft.com/en-us/library/ms153586.aspx

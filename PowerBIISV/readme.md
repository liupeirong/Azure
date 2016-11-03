# Power BI and WebApp Integration Samples 

ISVs often have a need to provide reporting capabilities to their users.  Building reports and dashboards from scratch in web applications takes a lot of time and effort.  Leveraging Power BI could greatly reduce development effort and shorten time to market.

This solution demonstrates two ways to integrate Power BI with Web applications.  
* Integrate - The web application brings reports and dashboards from the users' own Power BI subscriptions for visualization and interaction in the application.  
In this scenario, users must sign in to Azure AD and consent to allow the web app to access their Power BI artifacts.  Users could also access the reports created by the ISV in the powerbi.com service.   
* Embed - The wed application provides reports from the web app owner (ISV)'s Power BI Embedded Azure subscription. 
In this scenario, neither the ISV nor the end user has to have a Power BI subscription.  Authentication between the web app and Power BI Embedded is using API key rather than Azure AD.  

Things to note: 
* Both samples are multi-tenant web applications
* The client/browser side programming model for both methods are very similar.  You can use
  * vanilla java script and iframe as demonstrated in [pbiintegrate/Views/Home/LOBDashboard.cshtml](pbiintegrate/Views/Home/LOBDashboard.cshtml)
  * powerbi.js sdk as demonstrated in [pbiintegrate/Views/Home/LOBReport.cshtml](pbiintegrate/Views/Home/LOBReport.cshtml) and [pbiembed/Views/Dashboard/PublicReport.cshtml](pbiembed/Views/Dashboard/PublicReport.cshtml)
  * Power BI Embedded asp.net sdk as demonstrated in [pbiembed/Views/Dashboard/Report.cshtml](pbiembed/Views/Dashboard/PublicReport.cshtml)
  


# Power BI Embed WebApp Samples 

This project demonstrates how to 
* integrate a Power BI report in a single tenant Line of Business (LOB) application. In this scenario, user must sign in to the Azure AD where this app is registered, the access token passed to Power BI is the Azure AD Bearer token
* embed a report in a multi-tenant ISV application. In this scenario, authentication can be controlled by the ISV. No authentication is implemented in this sample. The access token passed to Power BI is signed by the Power BI embed key 

The "Home" controller implements the above 2 actions.  In order to run the app, you need to 
* register the in your Azure AD tenant
* have a Power BI embedded workspace with reports

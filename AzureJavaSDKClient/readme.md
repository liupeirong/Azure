# Multi-tenant web app that accesses a user's Azure resources 

This sample demonstrates the code required for a SaaS web application to access a customer's Azure resources given a customer's Azure subscription ID. It does the following - 
* Sign in the user and trigger the consent flow if the user hasn't consented for this app to access the target Azure resources
* Check if the user has permission to assign this app a role to access the target Azure subscription
* Assign this app the required role to the target Azure subscription
* Call Azure Java Management API to get resource groups from the target subscription

Note that as a pre-requisite, you must -  
* Register this application as a multi-tenant application in your Azure AD 
* Put your registered app id and app secret in web.xml

Go to [this web site](http://javamgmtsdk.azurewebsites.net/azuremgmtsdksample) to try out this application.

Not yet implemented: 
* Sign out is not yet implemented. If you sign in and out as different users, then before you sign in as another user, sign out from Azure portal, close your browser, and open a new private browser session
* Organization ID works, Live ID is not yet implemented
* The first time you consent for this app to access your Azure resources, you may see an error when you hit the "Get resource groups" button. This is because it takes a few seconds for this app to be registered as a service principal in your Azure AD. Wait a few seconds and try again, it should succeed.
* In production, you'd store the registered customer's tenant ID and other info in a database rather than a local file on the web server


# Multi-tenant Azure Active Directory Web App that uses .NET SDK to deploy a StreamAnalytics job

This is a simple sample to demonstrate how to authenticate against Azure Active Directory to access Azure Management API on behalf of a user.  

### Key concepts
* Unlike desktop applications or web apps running locally on dev machine, a web app running on a web server does not pop up a sign in dialog for a user to authenticate againt Azure AD.  
* A web app has to direct the user to Azure AD endpoint to authenticate, then with the user's consent, uses the user's authorization code to obtain the bearer token to access the target resource.
* This sample uses OWin startup to authenticate the user at startup time, obtains the bearer token upon receiving the authorization code using AcquireTokenByAuthorizationCode, and caches it.  In the controller, it uses AcquireTokenSilent to access the cached token. 
* Works with both Org ID and Live ID.
* Learn more about Azure AD for Web App <a href="https://azure.microsoft.com/en-us/documentation/articles/active-directory-authentication-scenarios/#web-application-to-web-api">here</a>.

### Configuration
* Register this app in an Azure Active Directory that you have admin access to, place the ClientID and AppKey in web.config
* Make sure your app is registered as multi-tenant, and add the permission to access Azure Service Management API 
* Make sure the login and logout URLs are set to your app's URL, this could be either localhost:port if you run locally or the full URL if the app is hosted on a web server
* If you want to convert this app to single tenant, change the Azure AD domain in Startup.cs from "common" to your directory domain name, and register the app in Azure AD as a single tenant app

### TODO
* Refresh the cached token
* Logout


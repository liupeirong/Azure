# Azure Data Lake Store Example that helps understand app integration with Azure AD

This Java sample simply reads a file from Azure Data Lake Store (ADLS). It's based on [this original Azure sample](https://github.com/Azure-Samples/data-lake-store-java-upload-download-get-started/), but is meant to clarify common confusions when integrating an application with Azure AD (AAD). 

In a scenario where a Web API is protected by AAD, in this case ADLS API, and your application, this Java application, needs to access it, there are a few options:

### If the client application is a [public client](https://tools.ietf.org/html/rfc6749#section-2.1) 
A public client is installed on devices (for example, a console app, or javascript in a browser), so it cannot be trusted to store a secret.  In fact, when you register a native app in Azure AD, there's no way to generate a key or secret for the app.  Instead, the app must prompt the user to log in to AAD, and pass the user's credential to the target API.  In this example, if you set ```useAppCred = false```, it will use device login to sign in the user, and use the user's credential to access ADLS.  The user must have necessary ACL permissions on ADLS.  Note that when you register this native app in AAD, you need to require permission to access ADLS API.

### If the client application is a [confidential client](https://tools.ietf.org/html/rfc6749#section-2.1)
A confidential client could be a web app running on a secure web server, so it can be trusted to store a secret.  To access the target API, you can provide the Web app Id and its secret without any user interaction.  In this example, if you set ```useAppCred = true```, and if you add ACL to allow the registered web app to access ADLS, this app will be able to read the file in ADLS without any user login.  Note that when you register this web app in AAD, there's no need to require permission to access ADLS API.  

### So what can be learned from this sample - 

* A public client can only use user credential to access AAD protected API
* A confidential client can use either user credential or app credential to access AAD protected API.  This is further explained in [Web App to Web API authentication scenario](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-authentication-scenarios#web-application-to-web-api) and [Daemon or Server App to Web API authentication scenario](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-authentication-scenarios#daemon-or-server-application-to-web-api)
* Regardless of a public or confidential client, if it uses user credential, it must require necesary permissions to access the target Web API in AAD. If it uses app credential, this is not necessary.  This is also documented in the "Registering" section of [Web App to Web API authentication scenario](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-authentication-scenarios#web-application-to-web-api) and [Daemon or Server App to Web API authentication scenario](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-authentication-scenarios#daemon-or-server-application-to-web-api)
* If the target API controls a resource that requires a user credential (ex. a Power BI subscription), the client app must use user credential. 

For a comphrehensive understanding of app integration with Azure AD, read [Authentication Scenarios for Azure AD](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-authentication-scenarios).

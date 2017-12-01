# Azure Data Lake Store Example to understand Azure AD authentication and authorization

This sample Java app simply reads a file from Azure Data Lake Store (ADLS). It's insipred by [this sample](https://github.com/Azure-Samples/data-lake-store-java-upload-download-get-started/). It's meant to demonstrate the following points on how to integrate an application with Azure AD, which may not be clear when reading documentations and samples. 

In a scenario where a Web API is protected by Azure AD, in this case ADLS API, and your application, this Java application, needs to access it, there are the following options:

* If the client application is a [public client](https://tools.ietf.org/html/rfc6749#section-2.1)
A public client is installed on devices (for example, a console app, or javascript in a browser), so it cannot be trusted to store a secret.  In fact, when you register a native app in Azure AD, there's no way to generate a key/scret for the app.  Instead, the app must ask the user to log in to AAD, and pass the user's credential to the target API.  In this example, if you set ```useAppCred = false```, it will use device login to sign in the user, and use the user's credential to access ADLS.  The user must have necessary ACL permissions on ADLS.

* If the client application is a [confidential client](https://tools.ietf.org/html/rfc6749#section-2.1)
A confidential client could be a web app running on a secure web server, so it can be trusted to store a secret.  To access the target API, you can provide the Web app Id and key/secret without any user interaction.  In this example, if you set ```useAppCred = true```, and if you add ACL to allow the registered web app to access ADLS, this app will be able to read the file in ADLS without any user log in.  In this case, when you register the client web app, there's no need to specify required permissions to access ADLS API in Azure AD.  

So here are a few things that can be learned from this sample - 

* A public client can only use user credential to access AAD protected API
* A confidential client can use either user credential or app credential to access AAD protected API.  This is further explained in [Web App to Web API authentication scenario](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-authentication-scenarios#web-application-to-web-api) and [Daemon or Server App to Web API authentication scenario](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-authentication-scenarios#daemon-or-server-application-to-web-api)
* Regardless of the type of client app, if it uses user credential, it must require necesary permissions to access the Web API in Azure AD. If it uses app credential, this is not necessary as illustrated in this example.  This is also documented in the "Registering" section of [Web App to Web API authentication scenario](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-authentication-scenarios#web-application-to-web-api) and [Daemon or Server App to Web API authentication scenario](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-authentication-scenarios#daemon-or-server-application-to-web-api)
* If the target API controls a resource that requires a user credential (ex. a Power BI subscription), the client app must use user credential. 

For a comphrehensive understanding of app integration with Azure AD, read [Authentication Scenarios for Azure AD](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-authentication-scenarios).

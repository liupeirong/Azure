This example demonstrates the difference between using app credential vs. user credential when integrating applications with Azure Active Directory.  

This web app lets a user get or set the status of whether the terms of a specified Azure Marketplace VM image has been accepted in a specified subscription.  Register this app as a multi-tenant app in your Azure AD and update web.config with your app ID and app key to try it out.  Alternatively, you can try the [hosted version of this app](https://apporusercred.azurewebsites.net). 

Here are the key points to demonstrate:
### To use app credential
* This app only requires permission to __sign in users and read their profiles__.
* A user needs to consent to the app on first use.  Once consented, a service principal is created in the user's tenant. If proper rights are granted to this service principal, for example, a Reader role on the target subscription, then no user needs to sign in for this app to perform read/get operation.  Meanwhile write/set operation will fail due to lack of write privilege.

### To use user credential
* This app requires permission to __sign in users and read their profiles__, as well as permission to __Windows Azure Management Service__.
* A user has to sign in to perform read or write operations.  The service principal doesn't have to have any role on the target subscription, as long as the user has sufficient privileges to operate on the subscription. 

Notes:
* This app uses default ADAL token cache which shouldn't be used in production code.
 
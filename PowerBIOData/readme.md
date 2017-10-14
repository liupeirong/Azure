# Connecting Power BI to Azure AD OAuth2 Protected Web API or OData

Until a few months ago, it was not possible for first party Microsoft applications, such as Power BI, to access a third party Azure AD OAuth2 protected Web/OData API. This issue is explained in [Stack Overflow](https://stackoverflow.com/questions/28293791/waad-authentication-with-webapi-odata-service-consumed-by-excel-powerquery) and further explained [here](/PowerBIOData/PBIEODataExplained.pdf). Now that access from first party client to third party API is enabled, we created a [sample Web API](/PowerBIOData/Oauth2Odata) and a [sample Power BI Data Connector](/PowerBIOData/OAuth2DataConnector) to demonstrate how to use Azure AD OAuth2 authentication to secure a Web API, and how to connect Power BI to this API to import data. 

One thing to note is that the credential of the user logged into PowerBI.com is independent from the credential used to connect to data sources. You can use Power BI Row Level Security to control what data the login user can see. However, what we are discussing here only focuses on how Power BI connects to the data source, independent of the login user viewing the reports.

For Power BI to access your API, your API must handle unauthorized request by returning a "WWW-Authenticate" header, as seen in [this example](/PowerBIOData/Oauth2Odata/CustomAuthorization.cs).

### Scenario 1: The OAuth2 protected Web API is used by a single tenant
1. [Register the Web API in Azure AD](https://docs.microsoft.com/en-us/azure/active-directory/active-directory-app-registration) as a single tenant application (by default applications registered in Azure AD are single tenant)  
  1.1. In the __Properties__ page of the app, set the __APP ID URI__ to the same as the Web API URL (for example, https://mydataapi.azurewebsites.net)  
  1.2. In the __Reply URLs__ page, add "https://oauth.powerbi.com/views/oauthredirect.html" to the list of URLs, this is required for Power BI to connect to this API  
2. Register a client app as a single tenant application to test the API (optional)  
  In the __Required permissions__ page, add the Web API registered in the previous step to the list of APIs with delegated permissions  
3. Test the API (optional)  
  3.1. In a clean browser session, issue the following request. You'll prompted to log in if not already, and Azure AD will ask for your consent to allow this app to access the Web API. The request will then come back to your browser with a parameter __code__ in the return URL  

```  
GET https://login.microsoftonline.com/{{tenant_id}}/oauth2/authorize?client_id={{client_app_id}}&response_type=code  
```   
  3.2. Use CURL or Postman to issue the following request, copy "access_token" from the response  

```  
POST https://login.microsoftonline.com/{{tenant_id}}/oauth2/token
HEADER Content-Type: application/x-www-form-urlencoded
BODY: grant_type=authroization_code&client_id={{client_app_id}}&client_secret={{client_app_secret}}&code={{access code obtained in 3.2}}&resource={{APP ID URI registered in 1.1}}  
```  
        
  3.3. Access your Web API, for example  
    https://mydataapi.azurewebsites.net/api/Values  
    HEADER Authorization: Bearer {{access_token obtained in 3.2}}  
4. Access from Power BI  
  Open Power BI Desktop, __Get Data__ -> __Web__ or __OData Feed__, input the Web API URL (for example https://mydataapi.azurewebsites.net/api/Values), select __Organizational account__ and follow the wizard to sign in. If all is well, you should see you are signed in, and you can __Connect__ to see your data  

### Scenario 2: The OAuth2 protected Web API will be used by multiple tenants
1. [Register the Web API in Azure AD](https://docs.microsoft.com/en-us/azure/active-directory/active-directory-app-registration) as a multi-tenant application
  1.1. In the __Properties__ page of the app, set __Multi-tenanted__ to __Yes__
  1.2. In the __Reply URLs__ page, add "https://oauth.powerbi.com/views/oauthredirect.html" to the list of URLs, this is required for Power BI to connect to this API
2. For multi-tenant application, the __APP ID URI__ must be a verified domain in the Azure AD tenant. Meanwhile Power BI built-in Web/OData connect requires the target Web API URL to be the same as the __APP ID URI__ for security reasons
    2.1 if your Web API is serviced from a domain name that you own, [add this domain to your Azure AD tenant](https://docs.microsoft.com/en-us/azure/active-directory/add-custom-domain), and set the __APP ID URI__ to be the same as this domain
      2.1.1 Register a client app as a multi-tenant application to test the API (optional)
        2.1.1.1 In the __Properties__ page of the app, set __Multi-tenanted__ to __Yes__
        2.1.1.2 In the __Required permissions__ page, add the Web API registered in the previous step to the list of APIs with delegated permissions
      2.1.2 Test the API (optional)
        2.1.2.1 In a clean browser session, issue the following request. You'll prompted to log in if not already, and Azure AD will ask for your consent to allow this app to access the Web API. The request will then come back to your browser with a parameter __code__ in the return URL
            GET https://login.microsoftonline.com/common/oauth2/authorize?client_id={{client_app_id}}&response_type=code
        2.1.2.2 Use CURL or Postman to issue the following request, copy "access_token" from the response
            POST https://login.microsoftonline.com/common/oauth2/token
            HEADER Content-Type: application/x-www-form-urlencoded
            BODY: grant_type=authroization_code&client_id={{client_app_id}}&client_secret={{client_app_secret}}&code={{access code obtained in 2.1.2.1}}&resource={{APP ID URI registered in 2.1}}
        2.1.2.3 Access your Web API, for example
            https://mydataapi.azurewebsites.net/api/Values
            HEADER Authorization: Bearer {{access_token obtained in 2.1.2.2}}
      2.1.3 Access from Power BI
        2.1.3.1 Open Power BI Desktop, __Get Data__ -> __Web__ or __OData Feed__, input the Web API URL (for example https://mydataapi.com/api/Values), select __Organizational account__ and follow the wizard to sign in. If all is well, you should see you are signed in, and you can __Connect__ to see your data
    2.2 if your Web API is serviced from a domain name that you don't own, for example, https://mydataapi.azurewebsites.net, you have to set APP ID URI to something acceptable to Azure AD. By default, it your tenant domain followed by a sub directory, for example, https://mycompany.onmicrosoft.com/mydataapi
      2.2.1 Register a client app as a multi-tenant application to access the API, steps are same as 2.1.1, but this is required rather than optional
      2.2.2 Test the API (optional), stepas are same as 2.1.2
      2.2.3 Access from Power BI 
        2.2.3.1 Build a Power BI custom connector using the [Data Connector SDK](https://github.com/Microsoft/DataConnectors).  See [this barebone sample connector](/PowerBIOData/OAuth2DataConnector) 
        2.2.3.2 Open Power BI Desktop, __Get Data__ -> choose your custom connector, input the Web API URL (for example https://mydataapi.azurewebsites.net/api/Values), select __Organizational account__ and follow the wizard to sign in. If all is well, you should see you are signed in, and you can __Connect__ to see your data
       
The cached credentials on your browser and Power BI Desktop may cause login to fail persistently. If you encounter this, clean the browser cache, and clear Power BI data source credentials by going to Power BI Desktop, __File__ -> __Options and Settings__ -> __Data Source Settings__, select the data source and __Clear Permissions__. 


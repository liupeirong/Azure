## Power BI RLS (Row Level Security) with Azure Analysis Service

This doc describes how to use Row Level Security with Azure Analysis Service, especially in Power BI Embedded applications.  Azure Analysis Service leverages Azure Active Directory (AAD) for authentication.  With Power BI Embedded, the logged in user is typically not an AAD user or not in the same AAD as the application or Analysis Service.  What you'll need to do is to pass in a piece of custom data that can be related to the rest of you data model. 

1. Assuming you have a data model as following:

![Alt text](/PowerBIISV/Docs/Images/many2many.png?raw=true "Many to Many relationship")

2. In Analysis Service, you can do one of two things to add a role and define what the role can access:

  * Option 1: Define a filter on the table and column for what that role can access based on [CUSTOMDATA()](https://msdn.microsoft.com/en-us/library/hh213140.aspx)

![Alt text](/PowerBIISV/Docs/Images/roleDefDax.png?raw=true "Define role using DAX")

The DAX expression
```
='SalesLT DimSalesRegion'[salesRegion]=LOOKUPVALUE('SalesLT DimUserSecurity'[salesRegion],'SalesLT DimUserSecurity'[username],CUSTOMDATA())
```
means return true if the ```salesRegion``` column of ```DimSalesRegion``` table has the same value as the ```salesRegion``` column of ```DimUserSecurity``` table where the ```username``` column is equal to ```CUSTOMDATA()```. 
The DAX expression ```=FALSE() ``` always returns false, meaning this table will not be visible to users in this role.

  * Option 2: Enable bi-directional cross-filtering

![Alt text](/PowerBIISV/Docs/Images/biDiFilter.png?raw=true "Bi-directional cross filtering")

Bi-directional cross-filtering has implications on data modeling as well as performance.  Read the whitepaper linked in [this topic](https://powerbi.microsoft.com/en-us/blog/bidirectional-cross-filtering-whitepaper-2/) to ensure you fully understand it before using it. 
  
3. In order to embed Power BI in an application, you must have a Power BI Pro user that the app uses to communicate with Power BI. Add the Power BI Pro user's [UPN (User Principal Name)](https://docs.microsoft.com/en-us/power-bi/service-admin-rls) as a member of the role:

![Alt text](/PowerBIISV/Docs/Images/addUser2Role.png?raw=true "Add Power BI Pro user to the role")

4. Add users that you will pass in from the application to Power BI to the lookup table where the column is filtered by CUSTOMDATA in step 2. For example,if joe@acme.com is a user of your application belonging to the role defined above, add joe@acme.com to the ```DimUserSecurity``` table

![Alt text](/PowerBIISV/Docs/Images/addUser2SecTbl.png?raw=true "Add Power BI Pro user to the security table")

5. If your application uses [Power BI .NET SDK](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/powerbi-embedded?view=azure-dotnet), make sure to update Microsoft.PowerBI.Api Nuget package to at least 2.0.11.

6. Generate the embedded token by setting roles to include the role you defined in Step 1, username to the Power BI Pro user in Step 3, and customData to the user that logged into your application, one of those you added in Step 4.
```C#
var embedUser = new EffectiveIdentity(
      username: MvcApplication.pbiUserName,
      customData: "joe@acme.com",
      roles: new List<string> { "Sales Embedded" },
      datasets: new List<string> { report.DatasetId });
var tokenReq = new GenerateTokenRequest(
      accessLevel: "view",
      identities: new List<EffectiveIdentity> { embedUser });
```

7. Connect your Power BI Report to Azure Analysis Service using Live Connection. You can now test RLS in your embedded application.

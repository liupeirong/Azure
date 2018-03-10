## Power BI RLS (Row Level Security) with Azure Analysis Service

This doc describes how to use Row Level Security with Azure Analysis Service, and especially in Power BI Embedded applications.  Azure Analysis Service leverages Azure Active Directory (AAD) for authentication.  With Power BI Embedded, the logged in user is typically not an AAD user or not in the same AAD as the application or Analysis Service.  What you'll need to do is to pass in a piece of custom data that can be related to the rest of you data model. 

1. In Analysis Service, add a Role and define a filter for a column based on CUSTOMDATA
='SalesLT DimSalesRegion'[salesRegion]=LOOKUPVALUE('SalesLT DimUserSecurity'[salesRegion],'SalesLT DimUserSecurity'[username],CUSTOMDATA(),'SalesLT DimUserSecurity'[salesRegion],'SalesLT DimSalesRegion'[salesRegion])
2. Add the Power BI Pro user for the Embedded application to the role
3. Ensure the user passed in is in the lookup table where the column is filtered with customdata
3. Update Power BI API Nuget package to at least 15.5.7
4. Generate toke with username=Pro user and customdata=useridentifier 

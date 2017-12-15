# Dynamically create a .NET app domain on Azure Web App  

This simple sample demonstrates how to dynamically create a .NET app domain in an Azure Web App and run code in the newly created app domain.  

To see how it works, deploy the app, and go to <your web root>/swagger.  There are two REST APIs you can try:
* Get - displays the list of current app domains 
* Post - type in a string in the body, for example, "=mynewappdomain", set the content type to "application/x-www-form-urlencoded". This will create a new app domain, which you can verify by running the GET API again
* The code in the new app domain simply overwrites <your web root>/somehtml.html with a message "Hello <the name of the new app domain>". 

package com.paige.azure;

import com.microsoft.azure.credentials.MSICredentials;
import com.microsoft.azure.AzureEnvironment;
import com.microsoft.azure.storage.*;
import com.microsoft.azure.storage.blob.*;
import java.net.URL;
import java.net.URLEncoder;
import java.net.HttpURLConnection;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.BufferedReader;
import java.util.HashMap;
import java.util.Map;

public class App 
{
    public static void main( String[] args )
    {
        String storageAccountName = args[0];
        String userAssignedMSIClientId = args.length > 1 ? args[1] : null; //if null, use system assigned MSI
        String containers = listContainers(storageAccountName, userAssignedMSIClientId);
        System.out.println(containers);
    }

    private static String listContainers (String storageAccountName, String userAssignedMSIClientId)
    {
        String containers = "";
        try
        {
            String storageToken = retrieveMSIToken("https://storage.azure.com/", userAssignedMSIClientId);
            System.out.println("token: " + storageToken );

            StorageCredentialsToken storageCredentialsToken = 
                new StorageCredentialsToken(storageAccountName, storageToken);
            CloudStorageAccount account = new CloudStorageAccount(storageCredentialsToken, true);
            CloudBlobClient blobClient = account.createCloudBlobClient();
            for (final CloudBlobContainer container: blobClient.listContainers()){
                containers += container.getName() + ",";
            }
        } catch (Exception ex) {
            System.out.println(ex.getMessage());
        }
        return containers;
    }

    private static String retrieveMSIToken(String resource, String userAssignedMSIClientId)
    {
        //setting the resourceManagerEndpointUrl is a workaround for issue
        //https://github.com/Azure/autorest-clientruntime-for-java/issues/438,
        //should not be necessary in future releases of the SDK  
        String token = null;
        Map<String, String> endpoints = new HashMap<String, String>();
        endpoints.put("resourceManagerEndpointUrl", resource);
        AzureEnvironment environment = new AzureEnvironment(endpoints);
        MSICredentials credentials = new MSICredentials(environment).withClientId(userAssignedMSIClientId);
        try {
            token = credentials.getToken("");
        } catch (Exception ex)
        {
            System.out.println(ex.getMessage());
        }
        return token;
    }
}

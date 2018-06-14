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
            //String storageToken = retrieveTokenFromIDMS("https://storage.azure.com/", userAssignedMSIClientId);
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


    private static String retrieveTokenFromIDMS(String resource, String clientId) {
        String token = null;
        HttpURLConnection connection = null;

        try {
            StringBuilder payload = new StringBuilder();

            payload.append("api-version");
            payload.append("=");
            payload.append(URLEncoder.encode("2018-02-01", "UTF-8"));
            payload.append("&");
            payload.append("resource");
            payload.append("=");
            payload.append(URLEncoder.encode(resource, "UTF-8"));
            if (clientId != null) {
                payload.append("&");
                payload.append("client_id");
                payload.append("=");
                payload.append(URLEncoder.encode(clientId, "UTF-8"));
            }

            URL url = new URL(
                String.format("http://169.254.169.254/metadata/identity/oauth2/token?%s", 
                payload.toString()));
            connection = (HttpURLConnection) url.openConnection();
            connection.setRequestMethod("GET");
            connection.setRequestProperty("Metadata", "true");
            connection.connect();
            InputStream stream = connection.getInputStream();
            BufferedReader reader = new BufferedReader(new InputStreamReader(stream, "UTF-8"), 100);
            String result = reader.readLine();
            String lookfor = "\"access_token\":\""; 
            int begin = result.indexOf(lookfor) + lookfor.length();
            int end = result.indexOf("\"", begin);
            token = result.substring(begin, end);
        }
        catch (Exception ex) {
            System.out.println(ex.getMessage());
        } 
        finally {
            if (connection != null) 
                connection.disconnect();
        }
        return token;
    }
}

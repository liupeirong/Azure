// inspired by https://github.com/Azure-Samples/data-lake-store-java-upload-download-get-started/
package com.contoso.sample;

import com.microsoft.azure.datalake.store.ADLException;
import com.microsoft.azure.datalake.store.ADLStoreClient;
import com.microsoft.azure.datalake.store.oauth2.AccessTokenProvider;
import com.microsoft.azure.datalake.store.oauth2.ClientCredsTokenProvider;
import com.microsoft.azure.datalake.store.oauth2.DeviceCodeTokenProvider;

import java.io.*;

public class UploadDownloadApp {

    private static String accountFQDN = "[your lake].azuredatalakestore.net";  // full account FQDN, not just the account name
    private static String nativeClientId = "[your native app id]"; // must require permission to ADL API, user must have ACL
    
    private static String authTokenEndpoint = "https://login.microsoftonline.com/[your tenant]/oauth2/token";
    private static String webClientId = "[your web app id]"; // no need to require permission to ADL API, app must have ACL
    private static String webClientKey = "[your web app key]";
    
    private static Boolean useAppCred = false;

    public static void main(String[] args) {
        try {
            
            AccessTokenProvider provider = useAppCred ? 
            		new ClientCredsTokenProvider(authTokenEndpoint, webClientId, webClientKey): //using app credential
                    new DeviceCodeTokenProvider(nativeClientId); //using user credential
            
            ADLStoreClient client = ADLStoreClient.createClient(accountFQDN, provider);

            // read a file from data lake
            String filename = "/user/b/c.txt";
            InputStream in = client.getReadStream(filename);
            BufferedReader reader = new BufferedReader(new InputStreamReader(in));
            String line;
            while ( (line = reader.readLine()) != null) {
                System.out.println(line);
            }
            reader.close();
            System.out.println();
        } catch (ADLException ex) {
            printExceptionDetails(ex);
        } catch (Exception ex) {
            System.out.format(" Exception: %s%n Message: %s%n", ex.getClass().getName(), ex.getMessage());
        }
    }

    private static void printExceptionDetails(ADLException ex) {
        System.out.println("ADLException:");
        System.out.format("  Message: %s%n", ex.getMessage());
        System.out.format("  HTTP Response code: %s%n", ex.httpResponseCode);
        System.out.format("  Remote Exception Name: %s%n", ex.remoteExceptionName);
        System.out.format("  Remote Exception Message: %s%n", ex.remoteExceptionMessage);
        System.out.format("  Server Request ID: %s%n", ex.requestId);
        System.out.println();
    }

}

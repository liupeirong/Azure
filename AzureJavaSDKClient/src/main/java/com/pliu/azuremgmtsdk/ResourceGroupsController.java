/*******************************************************************************
 * Copyright © Microsoft Open Technologies, Inc.
 *
 * All Rights Reserved
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS//
 * OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION
 * ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A
 * PARTICULAR PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
 *
 * See the Apache License, Version 2.0 for the specific language
 * governing permissions and limitations under the License.
 ******************************************************************************/
package com.pliu.azuremgmtsdk;

import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.nio.file.StandardOpenOption;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpSession;

import org.json.JSONObject;
import org.springframework.stereotype.Controller;
import org.springframework.ui.ModelMap;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;

import com.microsoft.aad.adal4j.AuthenticationResult;
import com.microsoft.azure.management.Azure;
import com.microsoft.azure.management.resources.ResourceGroup;
import com.microsoft.azure.credentials.ApplicationTokenCredentials;
import com.microsoft.rest.LogLevel;

@Controller
@RequestMapping("/secure/resourcegroups")
public class ResourceGroupsController {
	
	private static String tenantFile = "tenants.txt"; //not for production
	private String subscriptionId;
	private String userTenant;

    @RequestMapping(method = { RequestMethod.GET, RequestMethod.POST })
    public String getResourceGroups(ModelMap model, HttpServletRequest httpRequest) {
        HttpSession session = httpRequest.getSession();
        subscriptionId = session.getAttribute("subscriptionId").toString();
        userTenant = session.getAttribute("userTenant").toString();
        AuthenticationResult result = (AuthenticationResult) session.getAttribute(AuthHelper.PRINCIPAL_SESSION_NAME);
        if (result == null) {
            model.addAttribute("//error", new Exception("AuthenticationResult not found in session."));
            return "/error";
        } else {
            String[] data;
            try {
                data = internalGetResourceGroups(result.getAccessToken());
                model.addAttribute("resourceGroups", data);
            } catch (Exception e) {
                model.addAttribute("error", e);
                return "/error";
            }
        }
        return "/secure/resourcegroups";
    }

    private Boolean isTenantRegistered() {
    	List<String>  tenants = null;
    	try {
    		tenants = Files.readAllLines(Paths.get(tenantFile), StandardCharsets.UTF_8);
        	if (tenants != null)
        	{
    	    	for (String t: tenants)
    	    	{
    	    		if (t.trim().equals(userTenant))
    	    		{
    	    			return true;
    	    		}
    	    	}
        	}
    	}
    	catch (Exception e)
    	{}
    	return false;
    }
    
    private Boolean userHasPermissionToAssignRole(String accessToken) throws Exception
    {
    	URL url = new URL("https://management.azure.com/subscriptions/" + subscriptionId 
    			+ "/providers/microsoft.authorization/permissions?api-version=2015-07-01");
        HttpURLConnection conn = (HttpURLConnection) url.openConnection();
        conn.setRequestProperty("Authorization", String.format("Bearer %s", accessToken));
        String respStr = HttpClientHelper.getResponseStringFromConn(conn, true);
        //TODO: check if "microsoft.authorization/roleassignments/write" not in "notActions" list

        return true;
    }
    
    private void assignRoleToSubscription(String accessToken) throws Exception
    {
    	String roleGUID = "acdd72a7-3385-48ef-bd42-f606fba81ae7"; //this is the Reader role

    	// get access token for graph API in order to get objectId of our service principal in the tenant's AAD
    	URL url = new URL(BasicFilter.authority + userTenant + "/oauth2/token");
        HttpURLConnection conn = (HttpURLConnection) url.openConnection();
        conn.setRequestMethod("POST");
        conn.setRequestProperty("Content-Type", "application/x-www-form-urlencoded");
        conn.setDoOutput(true);
        String body = "grant_type=client_credentials&client_id=" + BasicFilter.clientId + 
        		"&resource=https%3A%2F%2Fgraph.windows.net%2F&client_secret=" + BasicFilter.clientSecret; 
        conn.getOutputStream().write(body.getBytes());
        String respStr = HttpClientHelper.getResponseStringFromConn(conn, true);
        int respCode = conn.getResponseCode();
        JSONObject response = HttpClientHelper.processGoodRespStr(respCode, respStr);
        String graphAccessToken = response.getJSONObject("responseMsg").getString("access_token");
        
        //get the object id of the our service principal in the tenant's AAD 
    	url = new URL("https://graph.windows.net/" + userTenant 
    			+ "/servicePrincipals?api-version=1.5&$filter=appId%20eq%20"
    			+ "'" + BasicFilter.clientId + "'");
    	conn = (HttpURLConnection) url.openConnection();
        conn.setRequestProperty("Content-Type", "text/html; charset=utf-8");
        conn.setRequestProperty("Accept", "application/json");
        conn.setRequestProperty("Authorization", String.format("Bearer %s", graphAccessToken));
        respStr = HttpClientHelper.getResponseStringFromConn(conn, true);
        respCode = conn.getResponseCode();
        response = HttpClientHelper.processGoodRespStr(respCode, respStr);
        String objectId = response.getJSONObject("responseMsg").getJSONArray("value").getJSONObject(0).getString("objectId");
        
        //assign this app a reader role to the subscription
        //note assign the same app to the same role again with a different GUID will fail
        UUID roleAssignmentId = UUID.randomUUID();
    	url = new URL("https://management.azure.com/subscriptions/" + subscriptionId 
    			+ "/providers/microsoft.authorization/roleassignments/" 
    			+ roleAssignmentId.toString() + "?api-version=2015-07-01");
        conn = (HttpURLConnection) url.openConnection();
        conn.setRequestMethod("PUT");
        conn.setRequestProperty("Content-Type", "application/json");
        conn.setRequestProperty("Authorization", String.format("Bearer %s", accessToken));
        JSONObject cred = new JSONObject();
        cred.put("roleDefinitionId", "/subscriptions/" + subscriptionId 
        		+ "/providers/Microsoft.Authorization/roleDefinitions/" + roleGUID);
        cred.put("principalId", objectId); 
        JSONObject parent=new JSONObject();
        parent.put("properties", cred);
        conn.setDoOutput(true);
        conn.getOutputStream().write(parent.toString().getBytes());
        respStr = HttpClientHelper.getResponseStringFromConn(conn, true);

        Files.write(Paths.get(tenantFile), 
        		(userTenant+"\n").getBytes(), StandardOpenOption.CREATE, StandardOpenOption.APPEND);
    }
    
    private String[] internalGetResourceGroups(String accessToken) throws Exception {
    	ArrayList<String> rgs = new ArrayList<String>();
    	if (!isTenantRegistered() && userHasPermissionToAssignRole(accessToken))
   			assignRoleToSubscription(accessToken);
    	
        //list resource groups in the subscription by calling REST
        /*
    	URL url = new URL("https://management.azure.com/subscriptions/" + subscriptionId 
        		+ "/resourceGroups?api-version=2014-04-01");
        HttpURLConnection conn = (HttpURLConnection) url.openConnection();
        conn.setRequestProperty("Authorization", String.format("Bearer %s", accessToken));
        String respStr = HttpClientHelper.getResponseStringFromConn(conn, true);
        */
    	
        //call java sdk to list resource groups
        ApplicationTokenCredentials appcred = new ApplicationTokenCredentials(
        		BasicFilter.clientId,
        		userTenant, 
        		BasicFilter.clientSecret, 
        		null //azure global 
        		);
        Azure azure = Azure.configure()
                .withLogLevel(LogLevel.NONE)
                .authenticate(appcred)
                .withSubscription(subscriptionId);
        for (ResourceGroup rGroup : azure.resourceGroups().list()) {
            rgs.add(rGroup.name());
        }
		return rgs.toArray(new String[0]);
    }

}

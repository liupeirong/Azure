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
package com.pliu.powerbiembed;

import java.net.URI;
import java.util.List;
import java.net.URL;
import java.net.HttpURLConnection;

import javax.servlet.ServletContext;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpSession;

import org.apache.http.NameValuePair;
import org.springframework.stereotype.Controller;
import org.springframework.ui.ModelMap;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;
import org.apache.http.client.utils.URLEncodedUtils;
import com.microsoft.aad.adal4j.AuthenticationResult;
import org.json.JSONObject;

@Controller
@RequestMapping("/secure/report")
public class ReportController {

    @RequestMapping(method = { RequestMethod.GET, RequestMethod.POST })
    public String getReport(ModelMap model, HttpServletRequest httpRequest) {
        HttpSession session = httpRequest.getSession();
        AuthenticationResult result = (AuthenticationResult) session.getAttribute(AuthHelper.PRINCIPAL_SESSION_NAME);
        if (result == null) {
            model.addAttribute("//error", new Exception("AuthenticationResult not found in session."));
            return "/error";
        } 
        try {
                String userid = result.getUserInfo().getDisplayableId();
                // a simple demo of row level security
                String username = "anyone"; //username can't be empty if RLS is enabled on data source
                String roles = "Sales"; //default to a low privileged role
                if (userid.startsWith("me")) {
                    roles = ""; //see everything
                }
                else { 
                    username = "adventure-works\\\\pamela0";
                }

            ServletContext cxt = session.getServletContext();
            String proxyToken = AuthHelper.getAccessTokenFromProxyUserCredentials(cxt);
            String workspaceId = cxt.getInitParameter("workspaceId");
            String apiUrl = cxt.getInitParameter("apiUrl");
            String reportUrl = httpRequest.getParameter("reporturl");
            List<NameValuePair> params = URLEncodedUtils.parse(new URI(reportUrl), "UTF-8");
            String reportId = "";
            String datasetId = "";
            for (NameValuePair param : params) {
                if (param.getName().equals("reportId")) {
                    reportId = param.getValue();
                } else if (param.getName().equals("datasetId")) {
                    datasetId = param.getValue();
                }
            }
            String accessToken = generateEmbedToken(workspaceId, apiUrl, reportId, proxyToken,
                        username, roles, datasetId);
            model.addAttribute("accessToken", accessToken);
            model.addAttribute("embedUrl", reportUrl);
            model.addAttribute("reportId", reportId);
        } catch (Exception e) {
            model.addAttribute("error", e);
            return "/error";
        }

        return "/secure/report";
    }

    private String generateEmbedToken(String workspaceId, String apiUrl, String reportId, String proxyToken,
            String username, String roles, String datasetId)
            throws Exception {

        URL url = new URL(String.format("%s/v1.0/myorg/groups/%s/reports/%s/GenerateToken",
                //"http://requestbin.fullcontact.com/1nztzep1", 
                apiUrl, 
                workspaceId, reportId));

        String body = new StringBuilder()
            .append("{")
            .append("  \"accessLevel\": \"edit\",")
            .append("  \"allowSaveAs\": true,")
            .append("  \"identities\": [") 
            .append("     {")
            .append("       \"username\":\"").append(username).append("\",")
            .append("       \"roles\":[\"").append(roles).append("\"],")
            .append("       \"datasets\":[\"").append(datasetId).append("\"]")
            .append("     }")
            .append("   ]")
            .append("}")
            .toString();

        HttpURLConnection conn = (HttpURLConnection) url.openConnection();
        conn.setRequestMethod("POST");
        conn.setRequestProperty("Authorization", String.format("Bearer %s", proxyToken));
        conn.setRequestProperty("Content-Type", "application/json");

        String goodRespStr = HttpClientHelper.getResponseStringFromConn(conn, body);
        int responseCode = conn.getResponseCode();
        JSONObject response = HttpClientHelper.processGoodRespStr(responseCode, goodRespStr);
        String token = response.getJSONObject("responseMsg").getString("token");

        return token;
    }
}

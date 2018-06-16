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
 * THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
 * OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION
 * ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A
 * PARTICULAR PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
 * 
 * See the Apache License, Version 2.0 for the specific language
 * governing permissions and limitations under the License.
 ******************************************************************************/
package com.pliu.powerbiembed;

import java.util.Map;
import javax.servlet.ServletContext;
import javax.servlet.http.HttpServletRequest;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;
import javax.naming.ServiceUnavailableException;
import com.microsoft.aad.adal4j.AuthenticationResult;
import com.microsoft.aad.adal4j.AuthenticationContext;
import com.nimbusds.openid.connect.sdk.AuthenticationResponse;
import com.nimbusds.openid.connect.sdk.AuthenticationSuccessResponse;

public final class AuthHelper {

    public static final String PRINCIPAL_SESSION_NAME = "principal";

    private AuthHelper() {
    }

    public static boolean isAuthenticated(HttpServletRequest request) {
        return request.getSession().getAttribute(PRINCIPAL_SESSION_NAME) != null;
    }

    public static AuthenticationResult getAuthSessionObject(
            HttpServletRequest request) {
        return (AuthenticationResult) request.getSession().getAttribute(
                PRINCIPAL_SESSION_NAME);
    }

    public static boolean containsAuthenticationData(
            HttpServletRequest httpRequest) {
        Map<String, String[]> map = httpRequest.getParameterMap();
        return httpRequest.getMethod().equalsIgnoreCase("POST") && (map.containsKey(
                        AuthParameterNames.ERROR)
                        || httpRequest.getParameterMap().containsKey(
                                AuthParameterNames.ID_TOKEN) || httpRequest
                        .getParameterMap().containsKey(AuthParameterNames.CODE));
    }

    public static boolean isAuthenticationSuccessful(
            AuthenticationResponse authResponse) {
        return authResponse instanceof AuthenticationSuccessResponse;
    }

    public static String getAccessTokenFromProxyUserCredentials(ServletContext cxt)
            throws Exception {
        final String proxyUserName = cxt.getInitParameter("proxyUserName");
        final String proxyPassword = cxt.getInitParameter("proxyPassword");
        final String authority = cxt.getInitParameter("authority");
        final String tenant = cxt.getInitParameter("tenant");
        final String clientId = cxt.getInitParameter("clientId");
        final String resourceId = cxt.getInitParameter("resourceId");
        final String authUrl = authority + tenant + "/";

        ExecutorService service = Executors.newFixedThreadPool(1);
        AuthenticationContext authContext;
        AuthenticationResult result;
        try {
            authContext = new AuthenticationContext(authUrl, false, service);
            Future<AuthenticationResult> future = authContext.acquireToken(
                    resourceId, clientId, proxyUserName, proxyPassword, null);
            result = future.get();
        } finally {
            service.shutdown();
        }

        if (result == null) {
            throw new ServiceUnavailableException(
                    "authentication result was null");
        }
        return result.getAccessToken();
    }
}

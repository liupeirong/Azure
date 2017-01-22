using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.IdentityModel.Claims;
using System.Threading.Tasks;
using pbiintegrate.Models;

namespace pbiintegrate
{
    public partial class Startup
    {
        public static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        public static string clientKey = ConfigurationManager.AppSettings["ida:ClientKey"];
        public static string resourceId = ConfigurationManager.AppSettings["ida:ResourceId"];
        public static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        private static string authority = aadInstance + tenantId;

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = clientId,
                    Authority = authority,
                    TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
                    {
                        // instead of using the default validation (validating against a single issuer value, as we do in line of business apps), 
                        // we inject our own multitenant validation logic
                        ValidateIssuer = false,
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        AuthorizationCodeReceived = async (context) =>
                        {
                            var code = context.Code;

                            string signedInUserID = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.NameIdentifier).Value;

                            ClientCredential credential = new ClientCredential(clientId, clientKey);
                            AuthenticationContext authContext = new AuthenticationContext(authority, new ADALTokenCache(signedInUserID));
                            AuthenticationResult result = await authContext.AcquireTokenByAuthorizationCodeAsync(
                                code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, resourceId);
                        },
                        RedirectToIdentityProvider = (context) =>
                        {
                            // This ensures that the address used for sign in and sign out is picked up dynamically from the request
                            // this allows you to deploy your app (to Azure Web Sites, for example)without having to change settings
                            // Remember that the base URL of the address used here must be provisioned in Azure AD beforehand.
                            string appBaseUrl = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase;
                            context.ProtocolMessage.RedirectUri = appBaseUrl + "/";
                            context.ProtocolMessage.PostLogoutRedirectUri = appBaseUrl + "/Account/SignOutCallback";
                            return Task.FromResult(0);
                        },
                        AuthenticationFailed = (context) =>
                        {
                            // Pass in the context back to the app
                            context.OwinContext.Response.Redirect("/Home/Error");
                            context.HandleResponse(); // Suppress the exception
                            return Task.FromResult(0);
                        }
                    }
                });
        }
    }
}
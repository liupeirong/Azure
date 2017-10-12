using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Oauth2Odata
{
    public class CustomAuthorization: AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            if (!actionContext.RequestContext.Principal.Identity.IsAuthenticated)
            {
                actionContext.Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden);
                actionContext.Response.Headers.TryAddWithoutValidation("WWW-Authenticate", "Bearer authorization_uri=https://login.microsoftonline.com/common/oauth2/token");
            }
            else
            {
                base.HandleUnauthorizedRequest(actionContext);
            }
        }
    }
}
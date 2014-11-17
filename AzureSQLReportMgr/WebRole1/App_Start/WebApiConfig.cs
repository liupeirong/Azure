using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace ReportWebSite
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            // this is needed because in the AngularJS service, we don't want to create 2 resources for get and put.
            config.Routes.MapHttpRoute(
                name: "ReportGenApi",
                routeTemplate: "api/{controller}"
            );

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{*name}",
                defaults: new { name = RouteParameter.Optional}
            );

        }
    }
}

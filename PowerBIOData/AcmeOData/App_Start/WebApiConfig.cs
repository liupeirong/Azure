using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using AcmeOData.Models;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;

namespace AcmeOData
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            ODataModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Product>("Products");
            config.MapODataServiceRoute(
                routeName: "ODataRoute",
                routePrefix: null,
                model: builder.GetEdmModel());
        }
    }
}

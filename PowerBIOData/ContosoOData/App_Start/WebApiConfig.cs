using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using ContosoOData.Models;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;

namespace ContosoOData
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
            config.Count().Filter().OrderBy().Expand().Select().MaxTop(null);
        }
    }
}

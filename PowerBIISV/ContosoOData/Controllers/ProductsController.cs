using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ContosoOData.Models;
using System.Web.OData;
using System.Web.Http;

namespace ContosoOData.Controllers
{
    [Authorize]
    public class ProductsController : ODataController
    {
        public IQueryable<Product> Get()
        {
            var products = new List<Product>();
            products.Add(new Product {Id = 100, Name = "Hadoop", Price = 10, Category = "BigData"});
            products.Add(new Product {Id = 200, Name = "Spark", Price = 20, Category = "BigData" });
            products.Add(new Product { Id = 300, Name = "DataLake", Price = 20, Category = "BigData" });
            products.Add(new Product { Id = 400, Name = "Hive", Price = 40, Category = "BigData" });
            return products.AsQueryable();
        }
    }
}
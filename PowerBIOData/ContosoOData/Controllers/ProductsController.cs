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
        [EnableQuery]
        public IQueryable<Product> Get()
        {
            var products = new List<Product>();
            products.Add(new Product {Id = 100, Name = "Hadoop", Price = 10, Category = "BigData", TimeStamp = DateTime.Parse("11-21-2017 10:00")});
            products.Add(new Product {Id = 200, Name = "Spark", Price = 20, Category = "BigData", TimeStamp = DateTime.Parse("11-22-2017 13:00") });
            products.Add(new Product {Id = 300, Name = "Kafka", Price = 30, Category = "Streaming", TimeStamp = DateTime.Parse("11-23-2017 13:00") });
            products.Add(new Product {Id = 400, Name = "EventHub", Price = 15, Category = "Streaming", TimeStamp = DateTime.Parse("11-24-2017 13:00") });
            return products.AsQueryable();
        }
    }
}
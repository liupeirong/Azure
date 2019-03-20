using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AcmeOData.Models;
using Microsoft.AspNet.OData;
using System.Web.Http;

namespace AcmeOData.Controllers 
{
    public class ProductsController : ODataController
    {
        [Authorize]
        public IQueryable<Product> Get()
        {
            var products = new List<Product>();
            products.Add(new Product { Id = 1, Name = "banana", Price = 10, Category = "Fruits" });
            products.Add(new Product { Id = 2, Name = "blueberry", Price = 20, Category = "Fruits" });
            products.Add(new Product { Id = 3, Name = "strawberry", Price = 30, Category = "Fruits" });
            products.Add(new Product { Id = 3, Name = "pineapple", Price = 15, Category = "Fruits" });
            return products.AsQueryable();
        }
    }
}
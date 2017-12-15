using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContosoOData.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public DateTime TimeStamp { get; set; }

    }
}
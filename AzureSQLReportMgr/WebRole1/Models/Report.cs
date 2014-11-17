using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReportWebSite.Models
{
    public class Report
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
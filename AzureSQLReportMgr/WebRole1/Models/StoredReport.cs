using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReportWebSite.Models
{
    public class StoredReport
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Format { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
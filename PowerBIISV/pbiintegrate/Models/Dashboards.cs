using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace pbiintegrate.Models
{
    public class PBIDashboards
    {
        public PBIDashboard[] value { get; set; }
    }
    public class PBIDashboard
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public bool isReadOnly { get; set; }
        public string embedUrl { get; set; }
    }
}
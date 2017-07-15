using System.Collections.Generic;
using Microsoft.PowerBI.Api.V2.Models;

namespace pbipaembed.Models
{
    public class TilesViewModel
    {
        public List<Tile> Tiles { get; set; }
        public string DashboardId { get; set; }
    }
}
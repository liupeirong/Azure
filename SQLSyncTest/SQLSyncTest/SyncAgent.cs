namespace SQLSyncTest
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("SyncAgent")]
    public partial class SyncAgent
    {
        [Key]
        [StringLength(32)]
        public string Name { get; set; }

        public int Value { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}

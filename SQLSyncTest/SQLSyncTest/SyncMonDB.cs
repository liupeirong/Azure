namespace SQLSyncTest
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class SyncMonDB : DbContext
    {
        public SyncMonDB()
            : base("name=sqlhub")
        {
        }
        public SyncMonDB(string cxnString)
            : base(cxnString)
        {
        }

        public virtual DbSet<SyncAgent> SyncAgents { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SyncAgent>()
                .Property(e => e.Name)
                .IsUnicode(false);
        }
    }
}

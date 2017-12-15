using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using ISVWebApp.Models;

namespace ISVWebApp.DAL
{
    public class ISVWebAppContext : DbContext
    {
        public ISVWebAppContext()
            : base("ISVWebAppContext")
        { }

        public DbSet<PerWebUserCache> PerUserCacheList { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}
using Private_Clinic.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Private_Clinic.DAL
{
    public class ClinicDbContext : DbContext
    {
        public ClinicDbContext() : base("ClinicDb") { }

        public DbSet<AppUser> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            base.OnModelCreating(modelBuilder);
        }
    }
}

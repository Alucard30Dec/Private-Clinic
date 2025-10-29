namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SyncAfterIdentityFix : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.WorkingHours", "DoctorId");
            AddForeignKey("dbo.WorkingHours", "DoctorId", "dbo.Doctors", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.WorkingHours", "DoctorId", "dbo.Doctors");
            DropIndex("dbo.WorkingHours", new[] { "DoctorId" });
        }
    }
}

namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SyncModel_NoChanges : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.Appointments", "DoctorId");
            CreateIndex("dbo.Appointments", "ServiceId");
            CreateIndex("dbo.Appointments", "PatientProfileId");
            AddForeignKey("dbo.Appointments", "DoctorId", "dbo.Doctors", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Appointments", "PatientProfileId", "dbo.Patients", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Appointments", "ServiceId", "dbo.Services", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Appointments", "ServiceId", "dbo.Services");
            DropForeignKey("dbo.Appointments", "PatientProfileId", "dbo.Patients");
            DropForeignKey("dbo.Appointments", "DoctorId", "dbo.Doctors");
            DropIndex("dbo.Appointments", new[] { "PatientProfileId" });
            DropIndex("dbo.Appointments", new[] { "ServiceId" });
            DropIndex("dbo.Appointments", new[] { "DoctorId" });
        }
    }
}

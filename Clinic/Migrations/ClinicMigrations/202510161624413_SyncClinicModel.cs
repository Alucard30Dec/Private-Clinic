namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SyncClinicModel : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.PatientProfiles", newName: "Patients");
            AddColumn("dbo.Appointments", "CreatedAt", c => c.DateTime(nullable: false));
            AddColumn("dbo.Appointments", "UpdatedAt", c => c.DateTime());
            AlterColumn("dbo.Appointments", "Notes", c => c.String(maxLength: 2000));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Appointments", "Notes", c => c.String());
            DropColumn("dbo.Appointments", "UpdatedAt");
            DropColumn("dbo.Appointments", "CreatedAt");
            RenameTable(name: "dbo.Patients", newName: "PatientProfiles");
        }
    }
}

namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddExtendedPatientProperties : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Patients", "Gender", c => c.String(maxLength: 10));
            AddColumn("dbo.Patients", "BloodType", c => c.String(maxLength: 5));
            AddColumn("dbo.Patients", "MedicalHistory", c => c.String());
            AddColumn("dbo.Patients", "Allergies", c => c.String());
            AddColumn("dbo.Patients", "EmergencyContactName", c => c.String(maxLength: 200));
            AddColumn("dbo.Patients", "EmergencyContactPhone", c => c.String(maxLength: 30));
            AlterColumn("dbo.Patients", "PhoneNumber", c => c.String(nullable: false, maxLength: 30));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Patients", "PhoneNumber", c => c.String(maxLength: 30));
            DropColumn("dbo.Patients", "EmergencyContactPhone");
            DropColumn("dbo.Patients", "EmergencyContactName");
            DropColumn("dbo.Patients", "Allergies");
            DropColumn("dbo.Patients", "MedicalHistory");
            DropColumn("dbo.Patients", "BloodType");
            DropColumn("dbo.Patients", "Gender");
        }
    }
}

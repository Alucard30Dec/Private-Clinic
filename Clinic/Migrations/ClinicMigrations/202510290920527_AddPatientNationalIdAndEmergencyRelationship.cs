namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPatientNationalIdAndEmergencyRelationship : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Patients", "NationalId", c => c.String(maxLength: 20));
            AddColumn("dbo.Patients", "EmergencyContactRelationship", c => c.String(maxLength: 50));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Patients", "EmergencyContactRelationship");
            DropColumn("dbo.Patients", "NationalId");
        }
    }
}

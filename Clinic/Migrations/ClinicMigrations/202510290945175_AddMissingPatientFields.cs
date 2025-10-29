namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMissingPatientFields : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Patients", "EmergencyContactRelationship", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Patients", "EmergencyContactRelationship", c => c.String(maxLength: 50));
        }
    }
}

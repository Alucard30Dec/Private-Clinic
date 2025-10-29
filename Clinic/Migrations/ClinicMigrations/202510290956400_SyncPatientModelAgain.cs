namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SyncPatientModelAgain : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Patients", "NewPassword", c => c.String(maxLength: 100));
            AddColumn("dbo.Patients", "ConfirmPassword", c => c.String());
            AddColumn("dbo.Patients", "Discriminator", c => c.String(nullable: false, maxLength: 128));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Patients", "Discriminator");
            DropColumn("dbo.Patients", "ConfirmPassword");
            DropColumn("dbo.Patients", "NewPassword");
        }
    }
}

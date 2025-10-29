namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDoctorNationalIdAddress : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Doctors", "NationalId", c => c.String(maxLength: 20));
            AddColumn("dbo.Doctors", "Address", c => c.String(maxLength: 300));
            AddColumn("dbo.Doctors", "NewPassword", c => c.String(maxLength: 100));
            AddColumn("dbo.Doctors", "ConfirmPassword", c => c.String());
            AddColumn("dbo.Doctors", "Discriminator", c => c.String(nullable: false, maxLength: 128));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Doctors", "Discriminator");
            DropColumn("dbo.Doctors", "ConfirmPassword");
            DropColumn("dbo.Doctors", "NewPassword");
            DropColumn("dbo.Doctors", "Address");
            DropColumn("dbo.Doctors", "NationalId");
        }
    }
}

namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDoctorProfileFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Doctors", "DateOfBirth", c => c.DateTime());
            AddColumn("dbo.Doctors", "Gender", c => c.String(maxLength: 10));
            AddColumn("dbo.Doctors", "Email", c => c.String(maxLength: 120));
            AddColumn("dbo.Doctors", "PhoneNumber", c => c.String(maxLength: 30));
            AddColumn("dbo.Doctors", "YearsOfExperience", c => c.Int());
            AddColumn("dbo.Doctors", "Bio", c => c.String(maxLength: 800));
            AlterColumn("dbo.Doctors", "Name", c => c.String(nullable: false, maxLength: 120));
            AlterColumn("dbo.Doctors", "Specialty", c => c.String(nullable: false, maxLength: 80));
            AlterColumn("dbo.Doctors", "PhotoUrl", c => c.String(maxLength: 256));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Doctors", "PhotoUrl", c => c.String());
            AlterColumn("dbo.Doctors", "Specialty", c => c.String(nullable: false));
            AlterColumn("dbo.Doctors", "Name", c => c.String(nullable: false));
            DropColumn("dbo.Doctors", "Bio");
            DropColumn("dbo.Doctors", "YearsOfExperience");
            DropColumn("dbo.Doctors", "PhoneNumber");
            DropColumn("dbo.Doctors", "Email");
            DropColumn("dbo.Doctors", "Gender");
            DropColumn("dbo.Doctors", "DateOfBirth");
        }
    }
}

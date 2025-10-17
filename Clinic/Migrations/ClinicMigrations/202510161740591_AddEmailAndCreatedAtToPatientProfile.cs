namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddEmailAndCreatedAtToPatientProfile : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Patients", "Email", c => c.String(maxLength: 200));
            AddColumn("dbo.Patients", "PhoneNumber", c => c.String(maxLength: 30));
            AddColumn("dbo.Patients", "DateOfBirth", c => c.DateTime());
            AddColumn("dbo.Patients", "Address", c => c.String(maxLength: 300));
            AddColumn("dbo.Patients", "CreatedAt", c => c.DateTime(nullable: false));
            AddColumn("dbo.Patients", "UpdatedAt", c => c.DateTime());
            AlterColumn("dbo.Patients", "FullName", c => c.String(maxLength: 200));
            AlterColumn("dbo.Patients", "UserId", c => c.String(nullable: false));
            DropColumn("dbo.Patients", "Phone");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Patients", "Phone", c => c.String(nullable: false));
            AlterColumn("dbo.Patients", "UserId", c => c.String());
            AlterColumn("dbo.Patients", "FullName", c => c.String(nullable: false));
            DropColumn("dbo.Patients", "UpdatedAt");
            DropColumn("dbo.Patients", "CreatedAt");
            DropColumn("dbo.Patients", "Address");
            DropColumn("dbo.Patients", "DateOfBirth");
            DropColumn("dbo.Patients", "PhoneNumber");
            DropColumn("dbo.Patients", "Email");
        }
    }
}

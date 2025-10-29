namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MakePatientUserIdNullable : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Patients", "UserId", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Patients", "UserId", c => c.String(nullable: false));
        }
    }
}

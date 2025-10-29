namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ReflectPendingModelChanges : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Patients", "FullName", c => c.String(nullable: false, maxLength: 200));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Patients", "FullName", c => c.String(maxLength: 200));
        }
    }
}

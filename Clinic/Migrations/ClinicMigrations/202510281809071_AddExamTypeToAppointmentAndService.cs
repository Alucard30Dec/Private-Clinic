namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddExamTypeToAppointmentAndService : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Appointments", "ExamType", c => c.Int(nullable: false));
            AddColumn("dbo.Services", "ExamType", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Services", "ExamType");
            DropColumn("dbo.Appointments", "ExamType");
        }
    }
}

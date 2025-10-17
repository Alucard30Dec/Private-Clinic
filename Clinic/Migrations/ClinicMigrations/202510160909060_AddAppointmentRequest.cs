namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAppointmentRequest : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AppointmentRequests",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Email = c.String(nullable: false, maxLength: 200),
                        Phone = c.String(maxLength: 30),
                        DesiredDate = c.DateTime(nullable: false),
                        Department = c.String(maxLength: 100),
                        Message = c.String(maxLength: 2000),
                        CreatedAt = c.DateTime(nullable: false),
                        IsHandled = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.AppointmentRequests");
        }
    }
}

namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAppointmentReviews : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AppointmentReviews",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AppointmentId = c.Int(nullable: false),
                        Rating = c.Int(nullable: false),
                        Comments = c.String(maxLength: 1000),
                        ReviewDate = c.DateTime(nullable: false),
                        IsApproved = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Appointments", t => t.AppointmentId, cascadeDelete: true)
                .Index(t => t.AppointmentId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AppointmentReviews", "AppointmentId", "dbo.Appointments");
            DropIndex("dbo.AppointmentReviews", new[] { "AppointmentId" });
            DropTable("dbo.AppointmentReviews");
        }
    }
}

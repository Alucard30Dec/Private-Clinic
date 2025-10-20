namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class AddAppointmentRequest : DbMigration
    {
        public override void Up()
        {
            // Tạo bảng
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
                    CreatedAt = c.DateTime(nullable: false), // sẽ gắn default constraint bên dưới
                    IsHandled = c.Boolean(nullable: false),  // sẽ gắn default constraint bên dưới
                })
                .PrimaryKey(t => t.Id);

            // Default constraints
            Sql(@"
                ALTER TABLE dbo.AppointmentRequests
                ADD CONSTRAINT DF_AppointmentRequests_CreatedAt DEFAULT (GETUTCDATE()) FOR CreatedAt;

                ALTER TABLE dbo.AppointmentRequests
                ADD CONSTRAINT DF_AppointmentRequests_IsHandled DEFAULT ((0)) FOR IsHandled;
            ");

            // Indexes cho tra cứu
            CreateIndex("dbo.AppointmentRequests", "DesiredDate");
            CreateIndex("dbo.AppointmentRequests", "IsHandled");
            CreateIndex("dbo.AppointmentRequests", new[] { "Email", "DesiredDate" });
        }

        public override void Down()
        {
            // Xóa index trước
            DropIndex("dbo.AppointmentRequests", new[] { "Email", "DesiredDate" });
            DropIndex("dbo.AppointmentRequests", new[] { "IsHandled" });
            DropIndex("dbo.AppointmentRequests", new[] { "DesiredDate" });

            // Xóa default constraints (tên đúng như đã đặt ở Up)
            Sql(@"
                IF OBJECT_ID('DF_AppointmentRequests_CreatedAt', 'D') IS NOT NULL
                    ALTER TABLE dbo.AppointmentRequests DROP CONSTRAINT DF_AppointmentRequests_CreatedAt;
                IF OBJECT_ID('DF_AppointmentRequests_IsHandled', 'D') IS NOT NULL
                    ALTER TABLE dbo.AppointmentRequests DROP CONSTRAINT DF_AppointmentRequests_IsHandled;
            ");

            // Xóa bảng
            DropTable("dbo.AppointmentRequests");
        }
    }
}

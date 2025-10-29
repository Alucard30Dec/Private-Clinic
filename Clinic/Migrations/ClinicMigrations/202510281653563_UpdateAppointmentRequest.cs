namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class UpdateAppointmentRequest : DbMigration
    {
        public override void Up()
        {
            // Thêm các cột mới trước
            AddColumn("dbo.AppointmentRequests", "Specialty", c => c.String(nullable: false, maxLength: 100));
            AddColumn("dbo.AppointmentRequests", "RequestedSlot", c => c.DateTime(nullable: false));

            // *** THÊM BƯỚC NÀY: Xóa các Index cũ trước khi xóa cột ***
            DropIndex("dbo.AppointmentRequests", new[] { "DesiredDate" });
            DropIndex("dbo.AppointmentRequests", new[] { "Email", "DesiredDate" });
            // *** KẾT THÚC THÊM ***

            // Xóa các cột cũ
            DropColumn("dbo.AppointmentRequests", "DesiredDate");
            DropColumn("dbo.AppointmentRequests", "Department");

            // *** THÊM BƯỚC NÀY: Tạo Index mới cho cột RequestedSlot (tùy chọn nhưng nên có) ***
            CreateIndex("dbo.AppointmentRequests", "RequestedSlot");
            CreateIndex("dbo.AppointmentRequests", new[] { "Email", "RequestedSlot" }, name: "IX_Email_RequestedSlot"); // Đặt tên mới để tránh trùng
            // *** KẾT THÚC THÊM ***
        }

        public override void Down()
        {
            // *** THÊM BƯỚC NÀY: Xóa các Index mới trước khi thêm lại cột cũ ***
            DropIndex("dbo.AppointmentRequests", new[] { "RequestedSlot" });
            DropIndex("dbo.AppointmentRequests", "IX_Email_RequestedSlot"); // Xóa theo tên đã đặt
            // *** KẾT THÚC THÊM ***

            // Thêm lại các cột cũ
            AddColumn("dbo.AppointmentRequests", "Department", c => c.String(maxLength: 100));
            // Đảm bảo kiểu dữ liệu đúng khi thêm lại cột
            AddColumn("dbo.AppointmentRequests", "DesiredDate", c => c.DateTime(nullable: false, defaultValueSql: "GETUTCDATE()")); // Thêm giá trị default tạm thời để tránh lỗi null

            // Xóa các cột mới
            DropColumn("dbo.AppointmentRequests", "RequestedSlot");
            DropColumn("dbo.AppointmentRequests", "Specialty");

            // *** THÊM BƯỚC NÀY: Tạo lại các Index cũ sau khi đã thêm lại cột ***
            CreateIndex("dbo.AppointmentRequests", "DesiredDate");
            CreateIndex("dbo.AppointmentRequests", new[] { "Email", "DesiredDate" });
            // *** KẾT THÚC THÊM ***

            // Xóa default constraint đã thêm tạm ở trên
            Sql("DECLARE @ConstraintName nvarchar(200); SELECT @ConstraintName = Name FROM SYS.DEFAULT_CONSTRAINTS WHERE PARENT_OBJECT_ID = OBJECT_ID('dbo.AppointmentRequests') AND PARENT_COLUMN_ID = (SELECT column_id FROM sys.columns WHERE NAME = N'DesiredDate' AND object_id = OBJECT_ID(N'dbo.AppointmentRequests')); IF @ConstraintName IS NOT NULL EXEC('ALTER TABLE dbo.AppointmentRequests DROP CONSTRAINT ' + @ConstraintName)");
        }
    }
}


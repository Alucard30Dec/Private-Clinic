namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    using Clinic.Models; // Thêm using cho ExamType enum

    public partial class UpdateSeedServicesExamType : DbMigration
    {
        public override void Up()
        {
            // Xóa các dịch vụ cũ theo ID (1, 2, 3) nếu chúng tồn tại
            Sql("DELETE FROM dbo.Services WHERE Id IN (1, 2, 3)");

            // Thêm dịch vụ mới (hoặc cập nhật nếu dùng AddOrUpdate)
            // Đảm bảo không trùng ID nếu bảng đã có dữ liệu khác
            // Ở đây dùng INSERT thẳng với giả định ID 1, 2 chưa được dùng lại

            // Khám Dịch vụ (ID = 1)
            Sql($@"
                    IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id=1)
                    BEGIN
                        SET IDENTITY_INSERT dbo.Services ON;
                        INSERT INTO dbo.Services (Id, Name, Fee, DurationMinutes, ExamType)
                        VALUES (1, N'Khám Dịch vụ', 200000, 30, {(int)ExamType.Service});
                        SET IDENTITY_INSERT dbo.Services OFF;
                    END
                    ELSE
                    BEGIN
                        UPDATE dbo.Services
                        SET Name = N'Khám Dịch vụ', Fee = 200000, DurationMinutes = 30, ExamType = {(int)ExamType.Service}
                        WHERE Id = 1;
                    END
                ");

            // Khám BHYT (ID = 2)
            Sql($@"
                    IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id=2)
                    BEGIN
                        SET IDENTITY_INSERT dbo.Services ON;
                        INSERT INTO dbo.Services (Id, Name, Fee, DurationMinutes, ExamType)
                        VALUES (2, N'Khám BHYT', 50000, 20, {(int)ExamType.HealthInsurance});
                        SET IDENTITY_INSERT dbo.Services OFF;
                    END
                    ELSE
                    BEGIN
                        UPDATE dbo.Services
                        SET Name = N'Khám BHYT', Fee = 50000, DurationMinutes = 20, ExamType = {(int)ExamType.HealthInsurance}
                        WHERE Id = 2;
                    END
                ");
            // Lưu ý: Bạn có thể thêm các dịch vụ khác nếu cần sau này
        }

        public override void Down()
        {
            // Rollback: Xóa dịch vụ mới, thêm lại dịch vụ cũ (nếu muốn rollback hoàn toàn)
            Sql("DELETE FROM dbo.Services WHERE Id IN (1, 2)");

            // Thêm lại các dịch vụ cũ (giống file PopulateServiceTable)
            Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id=1)
                    BEGIN
                      SET IDENTITY_INSERT dbo.Services ON;
                      INSERT INTO dbo.Services (Id,Name,Fee,DurationMinutes, ExamType) VALUES (1,N'Khám tổng quát',150000,20, 0);
                      INSERT INTO dbo.Services (Id,Name,Fee,DurationMinutes, ExamType) VALUES (2,N'Khám Nhi',180000,25, 0);
                      INSERT INTO dbo.Services (Id,Name,Fee,DurationMinutes, ExamType) VALUES (3,N'Tai Mũi Họng',200000,20, 0);
                      SET IDENTITY_INSERT dbo.Services OFF;
                    END");
        }
    }
}


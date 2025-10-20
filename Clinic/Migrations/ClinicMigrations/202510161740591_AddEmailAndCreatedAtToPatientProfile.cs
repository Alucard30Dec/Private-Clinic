namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class AddEmailAndCreatedAtToPatient : DbMigration
    {
        public override void Up()
        {
            // --- Thêm cột nếu CHƯA có ---
            Sql(@"IF COL_LENGTH('dbo.Patients','Email') IS NULL
                  ALTER TABLE dbo.Patients ADD Email NVARCHAR(200) NULL;");

            Sql(@"IF COL_LENGTH('dbo.Patients','PhoneNumber') IS NULL
                  ALTER TABLE dbo.Patients ADD PhoneNumber NVARCHAR(30) NULL;");

            Sql(@"IF COL_LENGTH('dbo.Patients','DateOfBirth') IS NULL
                  ALTER TABLE dbo.Patients ADD DateOfBirth DATETIME NULL;");

            Sql(@"IF COL_LENGTH('dbo.Patients','Address') IS NULL
                  ALTER TABLE dbo.Patients ADD Address NVARCHAR(300) NULL;");

            Sql(@"IF COL_LENGTH('dbo.Patients','CreatedAt') IS NULL
                  ALTER TABLE dbo.Patients ADD CreatedAt DATETIME NULL;");

            Sql(@"IF COL_LENGTH('dbo.Patients','UpdatedAt') IS NULL
                  ALTER TABLE dbo.Patients ADD UpdatedAt DATETIME NULL;");

            // --- Đảm bảo dữ liệu/kiểu cho các cột hiện hữu ---
            // Set CreatedAt cho những dòng đang NULL rồi mới siết NOT NULL
            Sql(@"UPDATE dbo.Patients SET CreatedAt = ISNULL(CreatedAt, GETUTCDATE());");

            // ALTER cột an toàn (chỉ khi cột tồn tại)
            Sql(@"IF COL_LENGTH('dbo.Patients','FullName') IS NOT NULL
                  ALTER TABLE dbo.Patients ALTER COLUMN FullName NVARCHAR(200) NULL;");

            Sql(@"IF COL_LENGTH('dbo.Patients','UserId') IS NOT NULL
                  BEGIN
                      UPDATE dbo.Patients SET UserId = '' WHERE UserId IS NULL;
                      -- Tùy DB của bạn: NVARCHAR(MAX) hoặc NVARCHAR(128)
                      ALTER TABLE dbo.Patients ALTER COLUMN UserId NVARCHAR(MAX) NOT NULL;
                  END");

            // Xóa cột Phone nếu còn tồn tại
            Sql(@"IF COL_LENGTH('dbo.Patients','Phone') IS NOT NULL
                  ALTER TABLE dbo.Patients DROP COLUMN Phone;");
        }

        public override void Down()
        {
            // Khôi phục cột Phone (nếu cần rollback)
            Sql(@"IF COL_LENGTH('dbo.Patients','Phone') IS NULL
                  ALTER TABLE dbo.Patients ADD Phone NVARCHAR(MAX) NULL;");

            // Nới lỏng các ràng buộc (nếu đã siết ở Up)
            Sql(@"IF COL_LENGTH('dbo.Patients','UserId') IS NOT NULL
                  ALTER TABLE dbo.Patients ALTER COLUMN UserId NVARCHAR(MAX) NULL;");

            Sql(@"IF COL_LENGTH('dbo.Patients','FullName') IS NOT NULL
                  ALTER TABLE dbo.Patients ALTER COLUMN FullName NVARCHAR(MAX) NULL;");

            // Xóa các cột đã thêm (chỉ khi tồn tại)
            Sql(@"IF COL_LENGTH('dbo.Patients','UpdatedAt') IS NOT NULL
                  ALTER TABLE dbo.Patients DROP COLUMN UpdatedAt;");
            Sql(@"IF COL_LENGTH('dbo.Patients','CreatedAt') IS NOT NULL
                  ALTER TABLE dbo.Patients DROP COLUMN CreatedAt;");
            Sql(@"IF COL_LENGTH('dbo.Patients','Address') IS NOT NULL
                  ALTER TABLE dbo.Patients DROP COLUMN Address;");
            Sql(@"IF COL_LENGTH('dbo.Patients','DateOfBirth') IS NOT NULL
                  ALTER TABLE dbo.Patients DROP COLUMN DateOfBirth;");
            Sql(@"IF COL_LENGTH('dbo.Patients','PhoneNumber') IS NOT NULL
                  ALTER TABLE dbo.Patients DROP COLUMN PhoneNumber;");
            Sql(@"IF COL_LENGTH('dbo.Patients','Email') IS NOT NULL
                  ALTER TABLE dbo.Patients DROP COLUMN Email;");
        }
    }
}

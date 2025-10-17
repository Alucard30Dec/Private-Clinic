using System.Data.Entity.Migrations;

namespace Clinic.Migrations.ClinicMigrations
{
    public partial class PopulateMoreDoctors : DbMigration
    {
        public override void Up()
        {
            Sql(@"
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    -- Cho phép chèn vào cột identity (Id)
    SET IDENTITY_INSERT dbo.Doctors ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 4)
    INSERT dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio, UserId)
    VALUES (4,  N'Dr. Quân',  N'Hô hấp',               '/Resources/images/team-image1.jpg', NULL, N'Nam',  'quan@example.com',  '0904000004',  8,  N'Chuyên hô hấp, COPD, hen',             NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 5)
    INSERT dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio, UserId)
    VALUES (5,  N'Dr. Trang', N'Tiêu hoá',            '/Resources/images/team-image2.jpg', NULL, N'Nữ',   'trang@example.com', '0905000005',  7,  N'Nội soi tiêu hoá, HP, IBS',             NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 6)
    INSERT dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio, UserId)
    VALUES (6,  N'Dr. Vân',   N'Thận - Tiết niệu',    '/Resources/images/team-image3.jpg', NULL, N'Nữ',   'van@example.com',   '0906000006', 10,  N'Sỏi thận, rối loạn đường tiểu',         NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 7)
    INSERT dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio, UserId)
    VALUES (7,  N'Dr. Hùng',  N'Cơ xương khớp',       '/Resources/images/team-image1.jpg', NULL, N'Nam',  'hung@example.com',  '0907000007', 12,  N'Viêm khớp, loãng xương, phục hồi chức năng', NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 8)
    INSERT dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio, UserId)
    VALUES (8,  N'Dr. Hà',    N'Nội tiết',            '/Resources/images/team-image2.jpg', NULL, N'Nữ',   'ha@example.com',    '0908000008',  9,  N'Đái tháo đường, tuyến giáp',            NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 9)
    INSERT dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio, UserId)
    VALUES (9,  N'Dr. Minh',  N'Tim mạch',            '/Resources/images/team-image3.jpg', NULL, N'Nam',  'minh@example.com',  '0909000009', 11,  N'Tăng huyết áp, mạch vành',               NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 10)
    INSERT dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio, UserId)
    VALUES (10, N'Dr. Lan',   N'Da liễu',             '/Resources/images/team-image1.jpg', NULL, N'Nữ',   'lan@example.com',   '0910000010',  6,  N'Mụn, nám, viêm da, dị ứng',              NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 11)
    INSERT dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio, UserId)
    VALUES (11, N'Dr. Phong', N'Tai - Mũi - Họng',    '/Resources/images/team-image2.jpg', NULL, N'Nam',  'phong@example.com', '0911000011', 13,  N'Viêm xoang, viêm tai giữa, amidan',     NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 12)
    INSERT dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio, UserId)
    VALUES (12, N'Dr. Thảo',  N'Nhi',                 '/Resources/images/team-image3.jpg', NULL, N'Nữ',   'thao@example.com',  '0912000012',  5,  N'Sốt virus, dinh dưỡng trẻ em',          NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 13)
    INSERT dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio, UserId)
    VALUES (13, N'Dr. Khánh', N'Ngoại tổng quát',     '/Resources/images/team-image1.jpg', NULL, N'Nam',  'khanh@example.com', '0913000013', 15,  N'Thoát vị, dạ dày, túi mật',             NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 14)
    INSERT dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio, UserId)
    VALUES (14, N'Dr. Dũng',  N'Chấn thương chỉnh hình','/Resources/images/team-image2.jpg', NULL, N'Nam','dung@example.com',  '0914000014', 14,  N'Gãy xương, dây chằng, khớp gối',         NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 15)
    INSERT dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio, UserId)
    VALUES (15, N'Dr. Linh',  N'Sản - Phụ khoa',      '/Resources/images/team-image3.jpg', NULL, N'Nữ',   'linh@example.com',  '0915000015',  9,  N'Sản khoa, phụ khoa, kế hoạch hoá gia đình', NULL);

    SET IDENTITY_INSERT dbo.Doctors OFF;

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
");
        }

        public override void Down()
        {
            // Xoá gọn nhóm 4..15 nếu cần rollback
            Sql(@"DELETE FROM dbo.Doctors WHERE Id BETWEEN 4 AND 15;");
        }
    }
}

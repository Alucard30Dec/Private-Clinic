using System.Data.Entity.Migrations;

namespace Clinic.Migrations.ClinicMigrations
{
    public partial class PopulateDoctorProfiles : DbMigration
    {
        public override void Up()
        {
            Sql(@"
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    ----------------------------------------------------------------
    -- 1) Bổ sung thông tin cho các bác sĩ sẵn có: Id 1..3 (UPDATE)
    ----------------------------------------------------------------
    UPDATE dbo.Doctors SET 
        PhotoUrl = '/Resources/images/doctors/quan.jpg',
        DateOfBirth = '1980-04-12',
        Gender = N'Nam',
        Email = 'quan@clinic.vn',
        PhoneNumber = '0901001001',
        YearsOfExperience = 12,
        Bio = N'Bác sĩ Hô hấp, nhiều năm công tác tuyến cuối; chuyên COPD, hen, lao.',
        Specialty = N'Hô hấp'
    WHERE Id = 1;

    UPDATE dbo.Doctors SET 
        PhotoUrl = '/Resources/images/doctors/trang.jpg',
        DateOfBirth = '1985-09-08',
        Gender = N'Nữ',
        Email = 'trang@clinic.vn',
        PhoneNumber = '0902002002',
        YearsOfExperience = 9,
        Bio = N'Bác sĩ Tiêu hóa; nội soi chẩn đoán & can thiệp, điều trị viêm loét dạ dày-tá tràng.',
        Specialty = N'Tiêu hóa'
    WHERE Id = 2;

    UPDATE dbo.Doctors SET 
        PhotoUrl = '/Resources/images/doctors/van.jpg',
        DateOfBirth = '1982-02-20',
        Gender = N'Nữ',
        Email = 'van@clinic.vn',
        PhoneNumber = '0903003003',
        YearsOfExperience = 10,
        Bio = N'Bác sĩ Thận - Tiết niệu; điều trị sỏi, viêm tiết niệu, rối loạn tiểu tiện.',
        Specialty = N'Thận - Tiết niệu'
    WHERE Id = 3;

    ----------------------------------------------------------------
    -- 2) Upsert cho 4..15: Nếu đã có -> UPDATE; nếu chưa có -> INSERT
    ----------------------------------------------------------------
    SET IDENTITY_INSERT dbo.Doctors ON;

    -- 4
    IF EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 4)
        UPDATE dbo.Doctors
          SET Name = N'Dr. Hạnh',
              Specialty = N'Nhi',
              PhotoUrl = '/Resources/images/doctors/hanh.jpg',
              DateOfBirth = '1987-03-05',
              Gender = N'Nữ',
              Email = 'hanh@clinic.vn',
              PhoneNumber = '0904004004',
              YearsOfExperience = 8,
              Bio = N'Bác sĩ Nhi tổng quát; hô hấp trẻ em, dinh dưỡng, các bệnh lý thường gặp.'
        WHERE Id = 4;
    ELSE
        INSERT INTO dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio)
        VALUES (4 , N'Dr. Hạnh', N'Nhi', '/Resources/images/doctors/hanh.jpg', '1987-03-05', N'Nữ', 'hanh@clinic.vn', '0904004004', 8, N'Bác sĩ Nhi tổng quát; hô hấp trẻ em, dinh dưỡng, các bệnh lý thường gặp.');

    -- 5
    IF EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 5)
        UPDATE dbo.Doctors
          SET Name = N'Dr. Minh',
              Specialty = N'Nội tổng quát',
              PhotoUrl = '/Resources/images/doctors/minh.jpg',
              DateOfBirth = '1979-11-22',
              Gender = N'Nam',
              Email = 'minh@clinic.vn',
              PhoneNumber = '0905005005',
              YearsOfExperience = 15,
              Bio = N'Khám nội tổng quát, tầm soát bệnh mạn tính, quản lý điều trị dài hạn.'
        WHERE Id = 5;
    ELSE
        INSERT INTO dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio)
        VALUES (5 , N'Dr. Minh', N'Nội tổng quát', '/Resources/images/doctors/minh.jpg', '1979-11-22', N'Nam', 'minh@clinic.vn', '0905005005', 15, N'Khám nội tổng quát, tầm soát bệnh mạn tính, quản lý điều trị dài hạn.');

    -- 6
    IF EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 6)
        UPDATE dbo.Doctors
          SET Name = N'Dr. Dũng',
              Specialty = N'Tim mạch',
              PhotoUrl = '/Resources/images/doctors/dung.jpg',
              DateOfBirth = '1981-07-15',
              Gender = N'Nam',
              Email = 'dung@clinic.vn',
              PhoneNumber = '0906006006',
              YearsOfExperience = 12,
              Bio = N'Siêu âm tim, tăng huyết áp, rối loạn nhịp, bệnh mạch vành.'
        WHERE Id = 6;
    ELSE
        INSERT INTO dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio)
        VALUES (6 , N'Dr. Dũng', N'Tim mạch', '/Resources/images/doctors/dung.jpg', '1981-07-15', N'Nam', 'dung@clinic.vn', '0906006006', 12, N'Siêu âm tim, tăng huyết áp, rối loạn nhịp, bệnh mạch vành.');

    -- 7
    IF EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 7)
        UPDATE dbo.Doctors
          SET Name = N'Dr. Phượng',
              Specialty = N'Nội tiết',
              PhotoUrl = '/Resources/images/doctors/phuong.jpg',
              DateOfBirth = '1986-12-01',
              Gender = N'Nữ',
              Email = 'phuong@clinic.vn',
              PhoneNumber = '0907007007',
              YearsOfExperience = 9,
              Bio = N'Đái tháo đường, bệnh tuyến giáp, rối loạn chuyển hóa lipid.'
        WHERE Id = 7;
    ELSE
        INSERT INTO dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio)
        VALUES (7 , N'Dr. Phượng', N'Nội tiết', '/Resources/images/doctors/phuong.jpg', '1986-12-01', N'Nữ', 'phuong@clinic.vn', '0907007007', 9, N'Đái tháo đường, bệnh tuyến giáp, rối loạn chuyển hóa lipid.');

    -- 8
    IF EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 8)
        UPDATE dbo.Doctors
          SET Name = N'Dr. Khánh',
              Specialty = N'Cơ xương khớp',
              PhotoUrl = '/Resources/images/doctors/khanh.jpg',
              DateOfBirth = '1983-04-18',
              Gender = N'Nam',
              Email = 'khanh@clinic.vn',
              PhoneNumber = '0908008008',
              YearsOfExperience = 11,
              Bio = N'Thoái hóa khớp, gout, viêm khớp dạng thấp; phục hồi chức năng.'
        WHERE Id = 8;
    ELSE
        INSERT INTO dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio)
        VALUES (8 , N'Dr. Khánh', N'Cơ xương khớp', '/Resources/images/doctors/khanh.jpg', '1983-04-18', N'Nam', 'khanh@clinic.vn', '0908008008', 11, N'Thoái hóa khớp, gout, viêm khớp dạng thấp; phục hồi chức năng.');

    -- 9
    IF EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 9)
        UPDATE dbo.Doctors
          SET Name = N'Dr. Yến',
              Specialty = N'Sản phụ khoa',
              PhotoUrl = '/Resources/images/doctors/yen.jpg',
              DateOfBirth = '1988-06-20',
              Gender = N'Nữ',
              Email = 'yen@clinic.vn',
              PhoneNumber = '0909009009',
              YearsOfExperience = 7,
              Bio = N'Khám thai, phụ khoa, kế hoạch hóa gia đình, rối loạn nội tiết nữ.'
        WHERE Id = 9;
    ELSE
        INSERT INTO dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio)
        VALUES (9 , N'Dr. Yến', N'Sản phụ khoa', '/Resources/images/doctors/yen.jpg', '1988-06-20', N'Nữ', 'yen@clinic.vn', '0909009009', 7, N'Khám thai, phụ khoa, kế hoạch hóa gia đình, rối loạn nội tiết nữ.');

    -- 10
    IF EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 10)
        UPDATE dbo.Doctors
          SET Name = N'Dr. Lâm',
              Specialty = N'Tai Mũi Họng',
              PhotoUrl = '/Resources/images/doctors/lam.jpg',
              DateOfBirth = '1984-08-09',
              Gender = N'Nam',
              Email = 'lam@clinic.vn',
              PhoneNumber = '0910001000',
              YearsOfExperience = 10,
              Bio = N'Viêm xoang, viêm tai giữa, polyp mũi; tiểu phẫu Tai Mũi Họng.'
        WHERE Id = 10;
    ELSE
        INSERT INTO dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio)
        VALUES (10, N'Dr. Lâm', N'Tai Mũi Họng', '/Resources/images/doctors/lam.jpg', '1984-08-09', N'Nam', 'lam@clinic.vn', '0910001000', 10, N'Viêm xoang, viêm tai giữa, polyp mũi; tiểu phẫu Tai Mũi Họng.');

    -- 11
    IF EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 11)
        UPDATE dbo.Doctors
          SET Name = N'Dr. Thảo',
              Specialty = N'Da liễu',
              PhotoUrl = '/Resources/images/doctors/thao.jpg',
              DateOfBirth = '1990-05-10',
              Gender = N'Nữ',
              Email = 'thao@clinic.vn',
              PhoneNumber = '0911001100',
              YearsOfExperience = 6,
              Bio = N'Mụn trứng cá, viêm da cơ địa, rụng tóc, trẻ hóa da & laser cơ bản.'
        WHERE Id = 11;
    ELSE
        INSERT INTO dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio)
        VALUES (11, N'Dr. Thảo', N'Da liễu', '/Resources/images/doctors/thao.jpg', '1990-05-10', N'Nữ', 'thao@clinic.vn', '0911001100', 6, N'Mụn trứng cá, viêm da cơ địa, rụng tóc, trẻ hóa da & laser cơ bản.');

    -- 12
    IF EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 12)
        UPDATE dbo.Doctors
          SET Name = N'Dr. Quý',
              Specialty = N'Ngoại tổng quát',
              PhotoUrl = '/Resources/images/doctors/quy.jpg',
              DateOfBirth = '1978-10-03',
              Gender = N'Nam',
              Email = 'quy@clinic.vn',
              PhoneNumber = '0912001200',
              YearsOfExperience = 16,
              Bio = N'Phẫu thuật tiêu hóa, thoát vị bẹn, sỏi mật; chấn thương bụng.'
        WHERE Id = 12;
    ELSE
        INSERT INTO dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio)
        VALUES (12, N'Dr. Quý', N'Ngoại tổng quát', '/Resources/images/doctors/quy.jpg', '1978-10-03', N'Nam', 'quy@clinic.vn', '0912001200', 16, N'Phẫu thuật tiêu hóa, thoát vị bẹn, sỏi mật; chấn thương bụng.');

    -- 13
    IF EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 13)
        UPDATE dbo.Doctors
          SET Name = N'Dr. Hòa',
              Specialty = N'Mắt',
              PhotoUrl = '/Resources/images/doctors/hoa.jpg',
              DateOfBirth = '1985-01-25',
              Gender = N'Nam',
              Email = 'hoa@clinic.vn',
              PhoneNumber = '0913001300',
              YearsOfExperience = 9,
              Bio = N'Cận/viễn/loạn; đục thủy tinh thể, glôcôm; chăm sóc mắt người cao tuổi.'
        WHERE Id = 13;
    ELSE
        INSERT INTO dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio)
        VALUES (13, N'Dr. Hòa', N'Mắt', '/Resources/images/doctors/hoa.jpg', '1985-01-25', N'Nam', 'hoa@clinic.vn', '0913001300', 9, N'Cận/viễn/loạn; đục thủy tinh thể, glôcôm; chăm sóc mắt người cao tuổi.');

    -- 14
    IF EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 14)
        UPDATE dbo.Doctors
          SET Name = N'Dr. Nhung',
              Specialty = N'Thần kinh',
              PhotoUrl = '/Resources/images/doctors/nhung.jpg',
              DateOfBirth = '1987-09-12',
              Gender = N'Nữ',
              Email = 'nhung@clinic.vn',
              PhoneNumber = '0914001400',
              YearsOfExperience = 8,
              Bio = N'Đau đầu, rối loạn tiền đình, bệnh mạch máu não, sau đột quỵ.'
        WHERE Id = 14;
    ELSE
        INSERT INTO dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio)
        VALUES (14, N'Dr. Nhung', N'Thần kinh', '/Resources/images/doctors/nhung.jpg', '1987-09-12', N'Nữ', 'nhung@clinic.vn', '0914001400', 8, N'Đau đầu, rối loạn tiền đình, bệnh mạch máu não, sau đột quỵ.');

    -- 15
    IF EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id = 15)
        UPDATE dbo.Doctors
          SET Name = N'Dr. Sơn',
              Specialty = N'Chấn thương chỉnh hình',
              PhotoUrl = '/Resources/images/doctors/son.jpg',
              DateOfBirth = '1982-02-14',
              Gender = N'Nam',
              Email = 'son@clinic.vn',
              PhoneNumber = '0915001500',
              YearsOfExperience = 12,
              Bio = N'Gãy xương, trật khớp, thoát vị đĩa đệm; điều trị bảo tồn & phẫu thuật.'
        WHERE Id = 15;
    ELSE
        INSERT INTO dbo.Doctors (Id, Name, Specialty, PhotoUrl, DateOfBirth, Gender, Email, PhoneNumber, YearsOfExperience, Bio)
        VALUES (15, N'Dr. Sơn', N'Chấn thương chỉnh hình', '/Resources/images/doctors/son.jpg', '1982-02-14', N'Nam', 'son@clinic.vn', '0915001500', 12, N'Gãy xương, trật khớp, thoát vị đĩa đệm; điều trị bảo tồn & phẫu thuật.');

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
            
            Sql("DELETE FROM dbo.Doctors WHERE Id BETWEEN 4 AND 15");
        }
    }
}

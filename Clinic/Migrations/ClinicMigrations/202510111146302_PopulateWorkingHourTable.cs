namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class PopulateWorkingHourTable : DbMigration
    {
        public override void Up()
        {
            // Xóa dữ liệu cũ (nếu có) để đảm bảo không trùng lặp
            Sql("DELETE FROM dbo.WorkingHours WHERE DoctorId IN (1, 2, 3)");

            // Vòng lặp qua 3 bác sĩ đầu tiên
            for (int doc = 1; doc <= 3; doc++)
            {
                // Thứ 2 đến Thứ 6 (DayOfWeek 1 đến 5)
                for (int dow = 1; dow <= 5; dow++)
                {
                    // Ca sáng: 08:00 - 11:30
                    Sql($@"
IF NOT EXISTS (SELECT 1 FROM dbo.WorkingHours WHERE DoctorId={doc} AND DayOfWeek={dow} AND Start='08:00:00')
INSERT INTO dbo.WorkingHours (DoctorId,DayOfWeek,Start,[End])
VALUES ({doc},{dow},'08:00:00','11:30:00')");

                    // Ca chiều: 13:00 - 17:00
                    Sql($@"
IF NOT EXISTS (SELECT 1 FROM dbo.WorkingHours WHERE DoctorId={doc} AND DayOfWeek={dow} AND Start='13:00:00')
INSERT INTO dbo.WorkingHours (DoctorId,DayOfWeek,Start,[End])
VALUES ({doc},{dow},'13:00:00','17:00:00')");
                }

                // Thứ 7 (DayOfWeek = 6)
                // Ca sáng: 08:00 - 11:30
                Sql($@"
IF NOT EXISTS (SELECT 1 FROM dbo.WorkingHours WHERE DoctorId={doc} AND DayOfWeek=6 AND Start='08:00:00')
INSERT INTO dbo.WorkingHours (DoctorId,DayOfWeek,Start,[End])
VALUES ({doc},6,'08:00:00','11:30:00')");

                // Chủ nhật (DayOfWeek = 0): Không thêm
            }
        }

        public override void Down()
        {
            Sql("DELETE FROM dbo.WorkingHours WHERE DoctorId IN (1,2,3)");
        }
    }
}

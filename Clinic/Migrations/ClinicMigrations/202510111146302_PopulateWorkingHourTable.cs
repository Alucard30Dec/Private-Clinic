namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PopulateWorkingHourTable : DbMigration
    {
        public override void Up()
        {
            for (int doc = 1; doc <= 3; doc++)
                for (int dow = 1; dow <= 5; dow++)
                    Sql($@"
IF NOT EXISTS (SELECT 1 FROM dbo.WorkingHours WHERE DoctorId={doc} AND DayOfWeek={dow})
INSERT INTO dbo.WorkingHours (DoctorId,DayOfWeek,Start,[End])
VALUES ({doc},{dow},'08:00:00','17:00:00')");
        }
        public override void Down()
        {
            Sql("DELETE FROM dbo.WorkingHours WHERE DoctorId IN (1,2,3)");
        }

    }
}

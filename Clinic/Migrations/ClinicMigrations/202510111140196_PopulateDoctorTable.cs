namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PopulateDoctorTable : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Doctors WHERE Id=1)
BEGIN
  SET IDENTITY_INSERT dbo.Doctors ON;
  INSERT INTO dbo.Doctors (Id,Name,Specialty) VALUES (1,N'Dr. An',N'Nội tổng quát');
  INSERT INTO dbo.Doctors (Id,Name,Specialty) VALUES (2,N'Dr. Bình',N'Nhi');
  INSERT INTO dbo.Doctors (Id,Name,Specialty) VALUES (3,N'Dr. Châu',N'Tai Mũi Họng');
  SET IDENTITY_INSERT dbo.Doctors OFF;
END");
        }
        public override void Down()
        {
            Sql("DELETE FROM dbo.Doctors WHERE Id IN (1,2,3)");
        }

    }
}

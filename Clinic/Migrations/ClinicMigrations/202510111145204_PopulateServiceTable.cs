namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PopulateServiceTable : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Id=1)
BEGIN
  SET IDENTITY_INSERT dbo.Services ON;
  INSERT INTO dbo.Services (Id,Name,Fee,DurationMinutes) VALUES (1,N'Khám tổng quát',150000,20);
  INSERT INTO dbo.Services (Id,Name,Fee,DurationMinutes) VALUES (2,N'Khám Nhi',180000,25);
  INSERT INTO dbo.Services (Id,Name,Fee,DurationMinutes) VALUES (3,N'Tai Mũi Họng',200000,20);
  SET IDENTITY_INSERT dbo.Services OFF;
END");
        }
        public override void Down()
        {
            Sql("DELETE FROM dbo.Services WHERE Id IN (1,2,3)");
        }
    }
}

namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class CapturePendingModelChanges : DbMigration
    {
        // This migration focuses ONLY on structural changes.
        // Data migration SQL will be in the next migration.
        public override void Up()
        {
            // --- Step 1: Create Specialties Table (if not exists) ---
            CreateTable(
                "dbo.Specialties",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 100),
                    IsVisible = c.Boolean(nullable: false, defaultValue: true),
                })
                .PrimaryKey(t => t.Id);
            // Add robust SQL check just in case CreateTable has issues with existing tables in some scenarios
            Sql(@"IF OBJECT_ID('dbo.Specialties', 'U') IS NULL
                  BEGIN
                      CREATE TABLE dbo.Specialties (
                          Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                          Name NVARCHAR(100) NOT NULL,
                          IsVisible BIT NOT NULL DEFAULT 1
                      );
                  END");


            // --- Step 2: Add new columns ---
            // Add SpecialtyId (NULLABLE initially)
            AddColumn("dbo.Doctors", "SpecialtyId", c => c.Int(nullable: true));
            // Add IsVisible columns (NOT NULL with default)
            AddColumn("dbo.Doctors", "IsVisible", c => c.Boolean(nullable: false, defaultValue: true));
            AddColumn("dbo.Services", "IsVisible", c => c.Boolean(nullable: false, defaultValue: true));

            // --- Step 2.5: Ensure IsVisible defaults are applied to existing rows ---
            // This needs to happen BEFORE altering SpecialtyId to NOT NULL
            Sql(@"UPDATE dbo.Doctors SET IsVisible = 1 WHERE IsVisible IS NULL;");
            Sql(@"UPDATE dbo.Services SET IsVisible = 1 WHERE IsVisible IS NULL;");


            // --- Step 3: Make SpecialtyId non-nullable ---
            // We will run data fixing SQL in the *next* migration *before* this step happens effectively.
            // For now, keep the AlterColumn here structurally.
            // We will add SQL in the NEXT migration to populate SpecialtyId BEFORE this runs.
            // **COMMENTING OUT AlterColumn for now - will move to next migration**
            // AlterColumn("dbo.Doctors", "SpecialtyId", c => c.Int(nullable: false));


            // --- Step 4: Create Index and Foreign Key ---
            CreateIndex("dbo.Doctors", "SpecialtyId");
            // Add FK (assuming SpecialtyId exists and is ready - data fix comes next)
            // **COMMENTING OUT AddForeignKey for now - will move to next migration**
            // Sql(@"IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo.Doctors_dbo.Specialties_SpecialtyId' AND parent_object_id = OBJECT_ID('dbo.Doctors'))
            //       BEGIN
            //           ALTER TABLE dbo.Doctors ADD CONSTRAINT FK_dbo.Doctors_dbo.Specialties_SpecialtyId FOREIGN KEY (SpecialtyId) REFERENCES dbo.Specialties (Id);
            //       END");

            // --- Step 5: Drop the old column ---
            // **COMMENTING OUT DropColumn for now - will move to next migration AFTER data migration**
            // Sql(@"IF COL_LENGTH('dbo.Doctors', 'Specialty') IS NOT NULL
            //       BEGIN
            //           ALTER TABLE dbo.Doctors DROP COLUMN Specialty;
            //       END");
        }

        public override void Down()
        {
            // Reverse structural changes ONLY

            // 1. Add Specialty column back (if needed)
            Sql(@"IF COL_LENGTH('dbo.Doctors', 'Specialty') IS NULL
                   BEGIN
                       ALTER TABLE dbo.Doctors ADD Specialty NVARCHAR(80) NULL;
                       -- Add SQL here if you need to populate it back from SpecialtyId before dropping FK/Column
                       -- UPDATE d SET d.Specialty = s.Name FROM dbo.Doctors d JOIN dbo.Specialties s ON d.SpecialtyId = s.Id;
                       -- ALTER TABLE dbo.Doctors ALTER COLUMN Specialty NVARCHAR(80) NOT NULL;
                   END");


            // 2. Drop Foreign Key and Index (if they exist)
            Sql(@"IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_dbo.Doctors_dbo.Specialties_SpecialtyId' AND parent_object_id = OBJECT_ID('dbo.Doctors'))
                  BEGIN
                      ALTER TABLE dbo.Doctors DROP CONSTRAINT FK_dbo.Doctors_dbo.Specialties_SpecialtyId;
                  END");
            // Check if index exists before dropping
            Sql(@"IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SpecialtyId' AND object_id = OBJECT_ID('dbo.Doctors'))
                  BEGIN
                      DROP INDEX IX_SpecialtyId ON dbo.Doctors;
                  END");
            // DropIndex("dbo.Doctors", new[] { "SpecialtyId" }); // EF version might fail

            // 3. Drop the added columns (check existence)
            Sql(@"IF COL_LENGTH('dbo.Services', 'IsVisible') IS NOT NULL BEGIN ALTER TABLE dbo.Services DROP COLUMN IsVisible; END");
            Sql(@"IF COL_LENGTH('dbo.Doctors', 'IsVisible') IS NOT NULL BEGIN ALTER TABLE dbo.Doctors DROP COLUMN IsVisible; END");
            Sql(@"IF COL_LENGTH('dbo.Doctors', 'SpecialtyId') IS NOT NULL BEGIN ALTER TABLE dbo.Doctors DROP COLUMN SpecialtyId; END");

            // 4. Drop the Specialties table (if exists)
            DropTable("dbo.Specialties");
            Sql(@"IF OBJECT_ID('dbo.Specialties', 'U') IS NOT NULL DROP TABLE dbo.Specialties;"); // More robust drop
        }
    }
}


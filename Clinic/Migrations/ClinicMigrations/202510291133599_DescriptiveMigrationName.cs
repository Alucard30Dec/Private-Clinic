// ... usings ...

using System.Data.Entity.Migrations;

public partial class DescriptiveMigrationName : DbMigration
{
    public override void Up()
    {
        // 1. Create Specialties table with IsVisible default true
        CreateTable(
            "dbo.Specialties",
            c => new
            {
                Id = c.Int(nullable: false, identity: true),
                Name = c.String(nullable: false, maxLength: 100),
                IsVisible = c.Boolean(nullable: false, defaultValue: true), // Default true
            })
            .PrimaryKey(t => t.Id);

        // 2. Add SpecialtyId to Doctors, temporarily nullable
        AddColumn("dbo.Doctors", "SpecialtyId", c => c.Int(nullable: true)); // Temporarily nullable

        // 3. Add IsVisible columns with default true
        AddColumn("dbo.Doctors", "IsVisible", c => c.Boolean(nullable: false, defaultValue: true));
        AddColumn("dbo.Services", "IsVisible", c => c.Boolean(nullable: false, defaultValue: true));

        // --- START: Manual SQL to handle data migration ---
        Sql(@"
            -- Populate Specialties from existing distinct Doctor.Specialty values
            -- Ensures IsVisible is set correctly and avoids duplicates if run again
            INSERT INTO dbo.Specialties (Name, IsVisible)
            SELECT DISTINCT Specialty, 1 -- Set IsVisible to true for existing specialties
            FROM dbo.Doctors
            WHERE Specialty IS NOT NULL AND Specialty <> ''
            AND NOT EXISTS (SELECT 1 FROM dbo.Specialties s WHERE s.Name = dbo.Doctors.Specialty);

            -- Update Doctor.SpecialtyId based on the newly populated Specialties table
            UPDATE d
            SET d.SpecialtyId = s.Id
            FROM dbo.Doctors d
            INNER JOIN dbo.Specialties s ON d.Specialty = s.Name
            WHERE d.SpecialtyId IS NULL; -- Only update if not already set

            -- Ensure IsVisible is true for existing rows (redundant if default constraint works, but safe)
            UPDATE dbo.Doctors SET IsVisible = 1 WHERE IsVisible IS NULL;
            UPDATE dbo.Services SET IsVisible = 1 WHERE IsVisible IS NULL;
        ");
        // --- END: Manual SQL ---

        // 4. Now make SpecialtyId non-nullable AFTER populating it
        // If there are still NULLs (e.g., Specialty was empty/null in Doctors), this might fail.
        // Consider handling edge cases, e.g., assigning a default SpecialtyId or logging errors.
        // For simplicity, we assume all doctors had a valid Specialty string.
        AlterColumn("dbo.Doctors", "SpecialtyId", c => c.Int(nullable: false));

        // 5. Create Index
        CreateIndex("dbo.Doctors", "SpecialtyId");

        // 6. Add ForeignKey constraint (should succeed now)
        AddForeignKey("dbo.Doctors", "SpecialtyId", "dbo.Specialties", "Id", cascadeDelete: true); // Cascade delete might be risky, consider false

        // 7. Drop the old string column AFTER everything else
        DropColumn("dbo.Doctors", "Specialty");
    }

    public override void Down()
    {
        // 1. Add the old Specialty string column back (make it nullable temporarily)
        AddColumn("dbo.Doctors", "Specialty", c => c.String(maxLength: 80, nullable: true)); // Temporarily nullable

        // --- START: Manual SQL to restore data before dropping FK/column ---
        Sql(@"
            -- Update the old Specialty string column based on SpecialtyId
            UPDATE d
            SET d.Specialty = s.Name
            FROM dbo.Doctors d
            INNER JOIN dbo.Specialties s ON d.SpecialtyId = s.Id;
        ");
        // --- END: Manual SQL ---

        // 2. Make the old Specialty column non-nullable again (matching original constraint)
        AlterColumn("dbo.Doctors", "Specialty", c => c.String(nullable: false, maxLength: 80));

        // 3. Drop the foreign key and index
        DropForeignKey("dbo.Doctors", "SpecialtyId", "dbo.Specialties");
        DropIndex("dbo.Doctors", new[] { "SpecialtyId" });

        // 4. Drop the added columns
        DropColumn("dbo.Services", "IsVisible");
        DropColumn("dbo.Doctors", "IsVisible");
        DropColumn("dbo.Doctors", "SpecialtyId");

        // 5. Drop the Specialties table AFTER removing FK
        DropTable("dbo.Specialties");
    }
}

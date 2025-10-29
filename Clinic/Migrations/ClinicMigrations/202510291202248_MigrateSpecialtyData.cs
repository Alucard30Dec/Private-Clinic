namespace Clinic.Migrations.ClinicMigrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class MigrateSpecialtyData : DbMigration
    {
        public override void Up()
        {
            // --- MOVED SQL INSIDE Up() METHOD ---

            // 1) Ensure default specialty exists
            Sql(@"
            IF NOT EXISTS (SELECT 1 FROM [dbo].[Specialties] WHERE [Name] = N'Chưa xác định')
            BEGIN
                INSERT INTO [dbo].[Specialties] ([Name],[IsVisible]) VALUES (N'Chưa xác định', 1);
            END;
            ");

            // 2) Cache DefaultSpecialtyId
            Sql(@"
            IF OBJECT_ID('tempdb..#vars') IS NOT NULL DROP TABLE #vars;
            CREATE TABLE #vars(DefaultSpecialtyId INT NOT NULL);

            DECLARE @DefaultSpecialtyId INT;
            SELECT @DefaultSpecialtyId = [Id] FROM [dbo].[Specialties] WHERE [Name] = N'Chưa xác định';
            INSERT INTO #vars(DefaultSpecialtyId) VALUES (@DefaultSpecialtyId);
            ");

            // 3) Migrate từ cột text (nếu còn)
            Sql(@"
            IF COL_LENGTH('dbo.Doctors', 'Specialty') IS NOT NULL
            BEGIN
                INSERT INTO [dbo].[Specialties] ([Name],[IsVisible])
                SELECT DISTINCT LTRIM(RTRIM(d.[Specialty])), 1
                FROM [dbo].[Doctors] AS d
                WHERE d.[Specialty] IS NOT NULL
                  AND LTRIM(RTRIM(d.[Specialty])) <> N''
                  AND NOT EXISTS (
                        SELECT 1 FROM [dbo].[Specialties] AS s WHERE s.[Name] = d.[Specialty]
                  );

                UPDATE d
                SET d.[SpecialtyId] = s.[Id]
                FROM [dbo].[Doctors] AS d
                INNER JOIN [dbo].[Specialties] AS s ON s.[Name] = d.[Specialty]
                WHERE d.[SpecialtyId] IS NULL;
            END;
            ");

            // 4) Lấp các giá trị NULL/invalid bằng default
            Sql(@"
            DECLARE @DefaultSpecialtyId INT;
            SELECT @DefaultSpecialtyId = DefaultSpecialtyId FROM #vars;

            UPDATE d
            SET d.[SpecialtyId] = @DefaultSpecialtyId
            FROM [dbo].[Doctors] AS d
            WHERE d.[SpecialtyId] IS NULL
               OR d.[SpecialtyId] = 0
               OR NOT EXISTS (SELECT 1 FROM [dbo].[Specialties] AS s WHERE s.[Id] = d.[SpecialtyId]);
            ");

            // *** 4.5) DROP các index phụ thuộc SpecialtyId trước khi ALTER COLUMN ***
            Sql(@"
            IF EXISTS (SELECT 1 FROM sys.indexes
                       WHERE name = N'IX_SpecialtyId' AND object_id = OBJECT_ID(N'[dbo].[Doctors]'))
            BEGIN
                DROP INDEX [IX_SpecialtyId] ON [dbo].[Doctors];
            END;

            /* If you have other indexes depending on SpecialtyId (including filtered indexes),
               uncomment the block below to drop them generically.

            DECLARE @idx NVARCHAR(128), @sql NVARCHAR(MAX);

            DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
            SELECT DISTINCT i.[name]
            FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.[object_id]=i.[object_id] AND ic.[index_id]=i.[index_id]
            JOIN sys.columns c ON c.[object_id]=ic.[object_id] AND c.[column_id]=ic.[column_id]
            WHERE i.[object_id]=OBJECT_ID(N'[dbo].[Doctors]')
              AND c.[name]=N'SpecialtyId'
              AND i.is_primary_key=0
              AND i.is_unique_constraint=0;

            OPEN cur;
            FETCH NEXT FROM cur INTO @idx;
            WHILE @@FETCH_STATUS=0
            BEGIN
                SET @sql = N'DROP INDEX ' + QUOTENAME(@idx) + N' ON [dbo].[Doctors];';
                EXEC sp_executesql @sql;
                FETCH NEXT FROM cur INTO @idx;
            END
            CLOSE cur; DEALLOCATE cur;
            */
            ");

            // 5) ALTER COLUMN -> NOT NULL
            Sql(@"ALTER TABLE [dbo].[Doctors] ALTER COLUMN [SpecialtyId] INT NOT NULL;");

            // 6) Drop default constraint if exists, then ADD FK if not exists
            Sql(@"
            IF NOT EXISTS (
                SELECT 1
                FROM sys.foreign_keys
                WHERE [name] = N'FK_dbo.Doctors_dbo.Specialties_SpecialtyId'
                  AND [parent_object_id] = OBJECT_ID(N'[dbo].[Doctors]')
            )
            BEGIN
                DECLARE @ConstraintName NVARCHAR(200);
                SELECT TOP (1) @ConstraintName = dc.[name]
                FROM sys.default_constraints AS dc
                WHERE dc.[parent_object_id] = OBJECT_ID(N'[dbo].[Doctors]')
                  AND COL_NAME(dc.[parent_object_id], dc.[parent_column_id]) = N'SpecialtyId';

                IF @ConstraintName IS NOT NULL
                BEGIN
                    DECLARE @sql NVARCHAR(MAX) =
                        N'ALTER TABLE [dbo].[Doctors] DROP CONSTRAINT ' + QUOTENAME(@ConstraintName) + N';';
                    EXEC sp_executesql @sql;
                END;

                ALTER TABLE [dbo].[Doctors]  WITH CHECK
                ADD CONSTRAINT [FK_dbo.Doctors_dbo.Specialties_SpecialtyId]
                    FOREIGN KEY ([SpecialtyId]) REFERENCES [dbo].[Specialties] ([Id]);
            END;
            ");

            // *** 6.5) RECREATE index after changing nullability (if needed) ***
            Sql(@"
            IF NOT EXISTS (SELECT 1 FROM sys.indexes
                           WHERE name = N'IX_SpecialtyId' AND object_id = OBJECT_ID(N'[dbo].[Doctors]'))
            BEGIN
                CREATE NONCLUSTERED INDEX [IX_SpecialtyId] ON [dbo].[Doctors]([SpecialtyId]);
            END;
            ");

            // 7) Drop the text column Specialty if it still exists
            Sql(@"
            IF COL_LENGTH('dbo.Doctors', 'Specialty') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[Doctors] DROP COLUMN [Specialty];
            END;
            ");

            // 8) Cleanup temp table
            Sql(@"IF OBJECT_ID('tempdb..#vars') IS NOT NULL DROP TABLE #vars;");

            // --- END MOVED SQL ---
        }

        public override void Down()
        {
            // The Down() method logic seems correct, assuming it was inside the class originally.
            // If the error also occurred for Down(), you would move its SQL inside here similarly.
            // For now, let's assume Down() was correctly placed.

            // Recreate the Specialty column (make nullable first)
            AddColumn("dbo.Doctors", "Specialty", c => c.String(maxLength: 80, nullable: true));

            // Restore data from SpecialtyId to Specialty
            Sql(@"
                 UPDATE d
                 SET d.Specialty = s.Name
                 FROM dbo.Doctors d
                 INNER JOIN dbo.Specialties s ON d.SpecialtyId = s.Id;
             ");

            // Make Specialty non-nullable again
            AlterColumn("dbo.Doctors", "Specialty", c => c.String(nullable: false, maxLength: 80));

            // Drop FK and Index for SpecialtyId
            DropForeignKey("dbo.Doctors", "SpecialtyId", "dbo.Specialties");
            DropIndex("dbo.Doctors", new[] { "SpecialtyId" });

            // Drop SpecialtyId column
            DropColumn("dbo.Doctors", "SpecialtyId");

        }
    }
}

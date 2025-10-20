<connectionStrings>
  <!-- Identity -->
  <add name="DefaultConnection"
       connectionString="Data Source=DESKTOP-6PU2F8Q;Initial Catalog=ClinicDb;
         Integrated Security=True;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=True"
       providerName="System.Data.SqlClient" />

  <!-- App data (ClinicDbContext) -->
  <add name="ClinicDb"
       connectionString="Data Source=DESKTOP-6PU2F8Q;Initial Catalog=ClinicDb;
         Integrated Security=True;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>

Enable-Migrations -ContextTypeName "Clinic.Models.ClinicDbContext"
Enable-Migrations
Migrate phần Identity
Update-Database -ConfigurationTypeName "Clinic.Migrations.IdentityMigrations.Configuration" -Verbose
Migrate phần Clinic (app data)
Update-Database -ConfigurationTypeName "Clinic.Migrations.ClinicMigrations.Configuration" -Verbose
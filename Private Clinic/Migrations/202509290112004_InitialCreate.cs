namespace Private_Clinic.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AppUser",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserName = c.String(nullable: false, maxLength: 50),
                        Email = c.String(nullable: false, maxLength: 150),
                        PasswordHash = c.String(nullable: false),
                        Role = c.Int(nullable: false),
                        FullName = c.String(maxLength: 120),
                        Phone = c.String(maxLength: 20),
                        Address = c.String(maxLength: 200),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UX_AppUser_UserName")
                .Index(t => t.Email, unique: true, name: "UX_AppUser_Email");
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.AppUser", "UX_AppUser_Email");
            DropIndex("dbo.AppUser", "UX_AppUser_UserName");
            DropTable("dbo.AppUser");
        }
    }
}

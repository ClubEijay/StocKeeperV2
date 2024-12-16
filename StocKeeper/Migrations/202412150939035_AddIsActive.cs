namespace StocKeeper.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIsActive : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Suppliers", "IsActive", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Suppliers", "IsActive");
        }
    }
}

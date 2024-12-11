    namespace StocKeeper.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Inventories",
                c => new
                {
                    InventoryID = c.Int(nullable: false, identity: true),
                    ProductID = c.Int(nullable: false),
                    Quantity = c.Int(nullable: false),
                    AdjustmentType = c.String(nullable: false, maxLength: 50),
                    Date = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.InventoryID)
                .ForeignKey("dbo.Products", t => t.ProductID, cascadeDelete: true)
                .Index(t => t.ProductID);

            CreateTable(
                "dbo.Products",
                c => new
                {
                    ProductID = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 100),
                    Category = c.String(maxLength: 50),
                    SupplierID = c.Int(nullable: false),
                    StockLevel = c.Int(nullable: false),
                    Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                })
                .PrimaryKey(t => t.ProductID)
                .ForeignKey("dbo.Suppliers", t => t.SupplierID, cascadeDelete: true)
                .Index(t => t.SupplierID);

            CreateTable(
                "dbo.Suppliers",
                c => new
                {
                    SupplierID = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 100),
                    ContactInfo = c.String(maxLength: 200),
                    Address = c.String(maxLength: 200),
                })
                .PrimaryKey(t => t.SupplierID);

            CreateTable(
                "dbo.PurchaseOrders",
                c => new
                {
                    OrderID = c.Int(nullable: false, identity: true),
                    SupplierID = c.Int(nullable: false),
                    Date = c.DateTime(nullable: false),
                    Status = c.String(maxLength: 50),
                })
                .PrimaryKey(t => t.OrderID)
                .ForeignKey("dbo.Suppliers", t => t.SupplierID, cascadeDelete: true)
                .Index(t => t.SupplierID);

        }
        public override void Down()
        {
            DropForeignKey("dbo.PurchaseOrders", "SupplierID", "dbo.Suppliers");
            DropForeignKey("dbo.Inventories", "ProductID", "dbo.Products");
            DropForeignKey("dbo.Products", "SupplierID", "dbo.Suppliers");
            DropIndex("dbo.PurchaseOrders", new[] { "SupplierID" });
            DropIndex("dbo.Products", new[] { "SupplierID" });
            DropIndex("dbo.Inventories", new[] { "ProductID" });
            DropTable("dbo.PurchaseOrders");
            DropTable("dbo.Suppliers");
            DropTable("dbo.Products");
            DropTable("dbo.Inventories");
        }
    }
}

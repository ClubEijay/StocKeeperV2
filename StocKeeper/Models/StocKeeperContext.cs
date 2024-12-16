using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace StocKeeper.Models
{
    public class StocKeeperContext : DbContext
    {
        public StocKeeperContext() : base("name=StocKeeperConnection"){ }
        // DbSet properties for your entities
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchaseOrders> PurchaseOrders { get; set; }
        public DbSet<OrderDetails> OrderDetails { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

            // Product-Category relationship
            modelBuilder.Entity<Product>()
                .HasRequired(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .WillCascadeOnDelete(false);

            // Product-Supplier relationship
            modelBuilder.Entity<Product>()
                .HasRequired(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .WillCascadeOnDelete(false);

            // Order-Supplier relationship
            modelBuilder.Entity<PurchaseOrders>()
                .HasRequired(o => o.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(o => o.SupplierId)
                .WillCascadeOnDelete(false);

            // Order-OrderDetail relationship
            modelBuilder.Entity<OrderDetails>()
                .HasRequired(od => od.PurchaseOrders)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .WillCascadeOnDelete(false);

            // Product-OrderDetail relationship
            modelBuilder.Entity<OrderDetails>()
                .HasRequired(od => od.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(od => od.ProductId)
                .WillCascadeOnDelete(false);

            // Product-InventoryTransaction relationship
            modelBuilder.Entity<InventoryTransaction>()
                .HasRequired(it => it.Product)
                .WithMany(p => p.InventoryTransactions)
                .HasForeignKey(it => it.ProductId)
                .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}

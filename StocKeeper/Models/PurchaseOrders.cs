using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StocKeeper.Models
{
    public class PurchaseOrders
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Expected Delivery Date")]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [Display(Name = "Delivery Date")]
        public DateTime? DeliveryDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } // Pending, Shipped, Delivered, Cancelled

        [Required]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        public virtual Supplier Supplier { get; set; }
        public virtual ICollection<OrderDetails> OrderDetails { get; set; }
    }
}
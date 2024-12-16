using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StocKeeper.Models
{
    public class OrderDetails
    {
        [Key]
        public int OrderDetailId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Total Price")]
        public decimal TotalPrice { get; set; }

        // Navigation properties
        public virtual PurchaseOrders PurchaseOrders { get; set; }
        public virtual Product Product { get; set; }
    }
}
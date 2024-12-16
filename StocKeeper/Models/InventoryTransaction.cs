using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace StocKeeper.Models
{
    public class InventoryTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "Transaction Date")]
        public DateTime TransactionDate { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Transaction Type")]
        public string TransactionType { get; set; } // IN, OUT

        [Required]
        public int Quantity { get; set; }

        [StringLength(200)]
        public string Reason { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Reference Number")]
        public string ReferenceNumber { get; set; }

        // Navigation property
        public virtual Product Product { get; set; }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace StocKeeper.Models
{
    public class InventoryViewModel
    {
        public Inventory Inventory { get; set; } 
        public IEnumerable<SelectListItem> Products { get; set; } 
    }
}
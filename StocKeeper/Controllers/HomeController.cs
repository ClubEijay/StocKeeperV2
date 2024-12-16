using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StocKeeper.Models;

namespace StocKeeper.Controllers
{
    public class HomeController : Controller
    {
        private StocKeeperContext db = new StocKeeperContext();

        // Static dictionary for login credentials
        private static readonly Dictionary<string, (string Password, string Role)> ValidUsers = new Dictionary<string, (string, string)>
        {
            // Business Owners
            {"owner", ("owner123", "BusinessOwner")},
            {"business", ("business123", "BusinessOwner")},
            
            // Warehouse Managers
            {"warehouse", ("warehouse123", "WarehouseManager")},
            {"inventory", ("inventory123", "WarehouseManager")}
        };

        // Login Action
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            // Normalize username (remove domain if present)
            var normalizedUsername = username.Contains('@')
                ? username.Split('@')[0]
                : username;

            // Check credentials
            if (ValidUsers.TryGetValue(normalizedUsername, out var userInfo))
            {
                if (userInfo.Password == password)
                {
                    // Store role in Session
                    Session["UserRole"] = userInfo.Role;
                    Session["Username"] = normalizedUsername;

                    // Redirect based on user role
                    if (userInfo.Role == "WarehouseManager")
                    {
                        return RedirectToAction("Index", "InventoryTransaction");
                    }

                    // Redirect to Index for Business Owner
                    return RedirectToAction("Index");
                }
            }

            // Login failed
            ModelState.AddModelError(string.Empty, "Invalid username or password");
            return View();
        }

        // Logout Action
        public ActionResult Logout()
        {
            // Clear session
            Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult Index()
        {
            // Check if user is logged in
            if (Session["UserRole"] == null)
            {
                return RedirectToAction("Login");
            }

            // Check if Warehouse Manager tries to access Home
            if (Session["UserRole"].ToString() == "WarehouseManager")
            {
                return RedirectToAction("Index", "InventoryTransaction");
            }

            // Existing code for total products and suppliers
            ViewBag.TotalProducts = db.Products.Count();
            ViewBag.TotalSuppliers = db.Suppliers.Count();

            // Add low stock monitoring
            var lowStockProducts = db.Products
                .Where(p => p.CurrentStock <= p.MinimumStockLevel)
                .ToList();

            ViewBag.LowStock = lowStockProducts.Count;
            ViewBag.LowStockProducts = lowStockProducts;

            // Pass user information to the view
            ViewBag.Username = Session["Username"];
            ViewBag.UserRole = Session["UserRole"];

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }
    }
}
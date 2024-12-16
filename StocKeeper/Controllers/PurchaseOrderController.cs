using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using StocKeeper.Models;

namespace StocKeeper.Controllers
{
    public class PurchaseOrderController : Controller
    {
        private StocKeeperContext db = new StocKeeperContext();

        public ActionResult Index(string searchString = "", string status = "", DateTime? dateFrom = null, DateTime? dateTo = null)
{
    var purchaseOrders = db.PurchaseOrders
        .Include(p => p.Supplier)
        .Include(p => p.OrderDetails)
        .Include(p => p.OrderDetails.Select(od => od.Product))
        .AsQueryable();

    // Apply search filter
    if (!string.IsNullOrEmpty(searchString))
    {
        purchaseOrders = purchaseOrders.Where(p =>
            p.OrderId.ToString().Contains(searchString) ||
            p.Supplier.Name.ToLower().Contains(searchString.ToLower()));
    }

    // Apply status filter
    if (!string.IsNullOrEmpty(status))
    {
        purchaseOrders = purchaseOrders.Where(p => p.Status == status);
    }

    // Apply date range filter
    if (dateFrom.HasValue)
    {
        purchaseOrders = purchaseOrders.Where(p => p.OrderDate >= dateFrom.Value);
    }

    if (dateTo.HasValue)
    {
        // Adjust dateTo to include the entire day
        var adjustedDateTo = dateTo.Value.Date.AddDays(1).AddTicks(-1);
        purchaseOrders = purchaseOrders.Where(p => p.OrderDate <= adjustedDateTo);
    }

    // Order by most recent first
    purchaseOrders = purchaseOrders.OrderByDescending(p => p.OrderDate);

    // Store filter values in ViewBag to maintain state
    ViewBag.CurrentSearch = searchString;
    ViewBag.CurrentStatus = status;
    ViewBag.DateFrom = dateFrom.HasValue 
        ? dateFrom.Value.ToString("yyyy-MM-ddTHH:mm") 
        : null;
    ViewBag.DateTo = dateTo.HasValue 
        ? dateTo.Value.ToString("yyyy-MM-ddTHH:mm") 
        : null;

    return View(purchaseOrders.ToList());
}

        // GET: PurchaseOrder/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            PurchaseOrders purchaseOrder = db.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.OrderDetails)
                .Include(p => p.OrderDetails.Select(od => od.Product))
                .FirstOrDefault(p => p.OrderId == id);

            if (purchaseOrder == null)
            {
                return HttpNotFound();
            }

            return View(purchaseOrder);
        }

        // GET: PurchaseOrder/Create
        public ActionResult Create()
        {
            ViewBag.SupplierList = new SelectList(db.Suppliers.Where(s => s.IsActive), "SupplierId", "Name");
            // Initialize an empty product list - will be populated via AJAX
            ViewBag.ProductList = new SelectList(new List<Product>(), "ProductId", "Name");
            return View(new PurchaseOrders
            {
                OrderDate = DateTime.Now,
                Status = "Pending"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "OrderId,SupplierId,OrderDate,ExpectedDeliveryDate,Status,Notes")] PurchaseOrders purchaseOrder, List<OrderDetails> orderDetails)
        {
            if (ModelState.IsValid && orderDetails != null && orderDetails.Any())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Add the purchase order
                        db.PurchaseOrders.Add(purchaseOrder);
                        db.SaveChanges();

                        // Add order details and calculate total
                        decimal totalAmount = 0;
                        foreach (var detail in orderDetails)
                        {
                            if (detail.ProductId != 0 && detail.Quantity > 0)
                            {
                                detail.OrderId = purchaseOrder.OrderId;
                                detail.TotalPrice = detail.Quantity * detail.UnitPrice;
                                totalAmount += detail.TotalPrice;
                                db.OrderDetails.Add(detail);
                            }
                        }

                        // Update total amount
                        purchaseOrder.TotalAmount = totalAmount;
                        db.SaveChanges();

                        transaction.Commit();
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "Error creating purchase order: " + ex.Message);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.SupplierList = new SelectList(db.Suppliers.Where(s => s.IsActive), "SupplierId", "Name");
            ViewBag.ProductList = new SelectList(new List<Product>(), "ProductId", "Name"); // Empty list - will be populated via AJAX
            return View(purchaseOrder);
        }

        // GET: PurchaseOrder/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            PurchaseOrders purchaseOrder = db.PurchaseOrders
                .Include(p => p.OrderDetails)
                .FirstOrDefault(p => p.OrderId == id);

            if (purchaseOrder == null)
            {
                return HttpNotFound();
            }

            if (purchaseOrder.Status != "Pending")
            {
                TempData["Error"] = "Only pending orders can be edited.";
                return RedirectToAction("Index");
            }

            ViewBag.SupplierList = new SelectList(db.Suppliers.Where(s => s.IsActive), "SupplierId", "Name");
            ViewBag.ProductList = new SelectList(db.Products, "ProductId", "Name");
            return View(purchaseOrder);
        }

        // POST: PurchaseOrder/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "OrderId,SupplierId,OrderDate,ExpectedDeliveryDate,Status,Notes")] PurchaseOrders purchaseOrder, List<OrderDetails> orderDetails)
        {
            // Validate expected delivery date
            if (purchaseOrder.ExpectedDeliveryDate < purchaseOrder.OrderDate)
            {
                ModelState.AddModelError("ExpectedDeliveryDate", "Expected delivery date cannot be earlier than order date.");
                ViewBag.SupplierList = new SelectList(db.Suppliers.Where(s => s.IsActive), "SupplierId", "Name");
                ViewBag.ProductList = new SelectList(db.Products, "ProductId", "Name");
                return View(purchaseOrder);
            }

            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var existingOrder = db.PurchaseOrders.Find(purchaseOrder.OrderId);

                        // Check if status transition is valid
                        if (!IsValidStatusTransition(existingOrder.Status, purchaseOrder.Status))
                        {
                            ModelState.AddModelError("Status", "Invalid status transition.");
                            ViewBag.SupplierList = new SelectList(db.Suppliers.Where(s => s.IsActive), "SupplierId", "Name");
                            ViewBag.ProductList = new SelectList(db.Products, "ProductId", "Name");
                            return View(purchaseOrder);
                        }

                        // Update delivery date if status changed to Delivered
                        if (purchaseOrder.Status == "Delivered" && existingOrder.Status != "Delivered")
                        {
                            purchaseOrder.DeliveryDate = DateTime.Now;
                        }

                        // Update purchase order
                        db.Entry(purchaseOrder).State = EntityState.Modified;

                        // Remove existing order details
                        var existingDetails = db.OrderDetails.Where(od => od.OrderId == purchaseOrder.OrderId);
                        db.OrderDetails.RemoveRange(existingDetails);

                        // Add new order details and calculate total
                        decimal totalAmount = 0;
                        foreach (var detail in orderDetails)
                        {
                            detail.OrderId = purchaseOrder.OrderId;
                            detail.TotalPrice = detail.Quantity * detail.UnitPrice;
                            totalAmount += detail.TotalPrice;
                            db.OrderDetails.Add(detail);

                            // Update inventory if status is Delivered
                            if (purchaseOrder.Status == "Delivered" && existingOrder.Status != "Delivered")
                            {
                                var product = db.Products.Find(detail.ProductId);
                                product.CurrentStock += detail.Quantity;
                            }
                        }

                        // Update total amount
                        purchaseOrder.TotalAmount = totalAmount;
                        db.SaveChanges();

                        transaction.Commit();
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "Error updating purchase order: " + ex.Message);
                    }
                }
            }

            ViewBag.SupplierList = new SelectList(db.Suppliers.Where(s => s.IsActive), "SupplierId", "Name");
            ViewBag.ProductList = new SelectList(db.Products, "ProductId", "Name");
            return View(purchaseOrder);
        }

        // POST: PurchaseOrder/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string status)
        {
            var purchaseOrder = db.PurchaseOrders.Find(id);
            if (purchaseOrder == null)
            {
                return HttpNotFound();
            }

            // Add diagnostic logging
            System.Diagnostics.Debug.WriteLine($"Current Order Status: {purchaseOrder.Status}");
            System.Diagnostics.Debug.WriteLine($"New Status: {status}");

            purchaseOrder.Status = status;
            if (status == "Delivered")
            {
                purchaseOrder.DeliveryDate = DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"Delivery Date Set: {purchaseOrder.DeliveryDate}");

                var orderDetails = db.OrderDetails
                    .Include(od => od.Product)
                    .Where(od => od.OrderId == id);

                foreach (var detail in orderDetails)
                {
                    detail.Product.CurrentStock += detail.Quantity;
                }
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: PurchaseOrder/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            PurchaseOrders purchaseOrder = db.PurchaseOrders
                .Include(p => p.Supplier)
                .FirstOrDefault(p => p.OrderId == id);

            if (purchaseOrder == null)
            {
                return HttpNotFound();
            }

            if (purchaseOrder.Status != "Pending")
            {
                TempData["Error"] = "Only pending orders can be deleted.";
                return RedirectToAction("Index");
            }

            return View(purchaseOrder);
        }

        // POST: PurchaseOrder/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Delete order details first
                    var orderDetails = db.OrderDetails.Where(od => od.OrderId == id);
                    db.OrderDetails.RemoveRange(orderDetails);

                    // Delete purchase order
                    PurchaseOrders purchaseOrder = db.PurchaseOrders.Find(id);
                    db.PurchaseOrders.Remove(purchaseOrder);

                    db.SaveChanges();
                    transaction.Commit();
                    return RedirectToAction("Index");
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        [HttpGet]
        public JsonResult GetProductDetails(int productId)
        {
            var product = db.Products.Find(productId);
            if (product == null)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                unitPrice = product.UnitPrice,
                currentStock = product.CurrentStock,
                minimumStockLevel = product.MinimumStockLevel
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetSupplierProducts(int supplierId)
        {
            var products = db.Products
                .Where(p => p.SupplierId == supplierId)
                .Select(p => new
                {
                    productId = p.ProductId,
                    name = p.Name,
                    unitPrice = p.UnitPrice,
                    currentStock = p.CurrentStock,
                    minimumStockLevel = p.MinimumStockLevel
                })
                .ToList();

            return Json(products, JsonRequestBehavior.AllowGet);
        }

        private bool IsValidStatusTransition(string currentStatus, string newStatus)
        {
            // Define valid status transitions
            var validTransitions = new Dictionary<string, string[]>
            {
                { "Pending", new[] { "Pending", "Shipped", "Cancelled" } },
                { "Shipped", new[] { "Shipped", "Delivered", "Cancelled" } },
                { "Delivered", new[] { "Delivered" } },
                { "Cancelled", new[] { "Cancelled" } }
            };

            return validTransitions.ContainsKey(currentStatus) &&
                   validTransitions[currentStatus].Contains(newStatus);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
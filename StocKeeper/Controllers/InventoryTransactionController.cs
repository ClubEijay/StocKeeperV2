using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using StocKeeper.Models;

namespace StocKeeper.Controllers
{
    public class InventoryTransactionController : Controller
    {
        private readonly StocKeeperContext db = new StocKeeperContext();

        // GET: InventoryTransaction
        public ActionResult Index(string searchString, string transactionType, DateTime? dateFrom, DateTime? dateTo)
        {
            var transactionsQuery = db.InventoryTransactions
                .Include(t => t.Product);

            // Apply filters
            if (!string.IsNullOrEmpty(searchString))
            {
                transactionsQuery = transactionsQuery.Where(t =>
                    t.ReferenceNumber.Contains(searchString) ||
                    t.Product.Name.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(transactionType))
            {
                transactionsQuery = transactionsQuery.Where(t => t.TransactionType == transactionType);
            }

            if (dateFrom.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(t => t.TransactionDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                // Move AddDays logic to in-memory evaluation
                var adjustedDateTo = dateTo.Value.AddDays(1);
                transactionsQuery = transactionsQuery.Where(t => t.TransactionDate <= adjustedDateTo);
            }

            // Fetch data and apply ordering
            var transactions = transactionsQuery
                .OrderByDescending(t => t.TransactionDate)
                .ToList(); // Fetch data from the database

            // Store filter values for view
            ViewBag.CurrentSearch = searchString;
            ViewBag.DateFrom = dateFrom?.ToString("MM-dd-yyyyTHH:mm"); // Format for datetime-local input
            ViewBag.DateTo = dateTo?.ToString("MM-dd-yyyyTHH:mm");


            return View(transactions);
        }

        // GET: InventoryTransaction/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Fetch the transaction with related product data
            var transaction = db.InventoryTransactions
                .Include(t => t.Product)
                .FirstOrDefault(t => t.TransactionId == id);

            if (transaction == null)
            {
                return HttpNotFound();
            }

            // Fetch the product associated with the transaction
            var product = db.Products.Find(transaction.ProductId);
            if (product == null)
            {
                return HttpNotFound();
            }

            // Calculate stock before
            int stockBefore;
            if (transaction.TransactionType == "IN")
            {
                stockBefore = product.CurrentStock - transaction.Quantity;
            }
            else // OUT transaction
            {
                stockBefore = product.CurrentStock + transaction.Quantity;
            }

            // Calculate stock after (current stock of the product)
            int stockAfter = product.CurrentStock;

            // Add the calculated values to ViewBag for the view
            ViewBag.StockBefore = stockBefore;
            ViewBag.StockAfter = stockAfter;

            // Fetch all transactions for the same product to display in the chart
            var transactions = db.InventoryTransactions
                .Where(t => t.ProductId == transaction.ProductId)
                .OrderBy(t => t.TransactionDate)
                .ToList();

            // Prepare data for the bar graph
            var transactionDates = transactions.Select(t => t.TransactionDate.ToString("MM-dd-yyyy HH:mm")).ToList();
            var stockBeforeData = new List<int>();
            var stockAfterData = new List<int>();

            int runningStock = 0;
            foreach (var t in transactions)
            {
                stockBeforeData.Add(runningStock);
                runningStock += t.TransactionType == "IN" ? t.Quantity : -t.Quantity;
                stockAfterData.Add(runningStock);
            }

            ViewBag.TransactionDates = transactionDates;
            ViewBag.StockBeforeData = stockBeforeData;
            ViewBag.StockAfterData = stockAfterData;

            return View(transaction);
        }


        // GET: InventoryTransaction/Create
        public ActionResult Create()
        {
            ViewBag.ProductId = new SelectList(db.Products, "ProductId", "Name");
            return View();
        }

        // POST: InventoryTransaction/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ProductId,TransactionDate,TransactionType,Quantity,Reason,ReferenceNumber")] InventoryTransaction transaction)
        {
            if (ModelState.IsValid)
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var product = db.Products.Find(transaction.ProductId);
                        if (product == null)
                        {
                            ModelState.AddModelError("", "Product not found.");
                            return View(transaction);
                        }

                        // Update product stock
                        if (transaction.TransactionType == "IN")
                        {
                            product.CurrentStock += transaction.Quantity;
                        }
                        else if (transaction.TransactionType == "OUT")
                        {
                            if (product.CurrentStock < transaction.Quantity)
                            {
                                ModelState.AddModelError("", "Insufficient stock available.");
                                return View(transaction);
                            }
                            product.CurrentStock -= transaction.Quantity;
                        }

                        db.InventoryTransactions.Add(transaction);
                        db.SaveChanges();
                        dbContextTransaction.Commit();

                        return RedirectToAction("Index");
                    }
                    catch (Exception)
                    {
                        dbContextTransaction.Rollback();
                        ModelState.AddModelError("", "An error occurred while processing the transaction.");
                    }
                }
            }

            ViewBag.ProductId = new SelectList(db.Products, "ProductId", "Name", transaction.ProductId);
            return View(transaction);
        }
        // GET: InventoryTransaction/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var transaction = db.InventoryTransactions
                .Include(t => t.Product)
                .FirstOrDefault(t => t.TransactionId == id);

            if (transaction == null)
            {
                return HttpNotFound();
            }

            ViewBag.ProductId = new SelectList(db.Products, "ProductId", "Name", transaction.ProductId);
            return View(transaction);
        }

        // POST: InventoryTransaction/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TransactionId,ProductId,TransactionDate,TransactionType,Quantity,Reason,ReferenceNumber")] InventoryTransaction transaction)
        {
            if (ModelState.IsValid)
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Find the original transaction
                        var originalTransaction = db.InventoryTransactions.Find(transaction.TransactionId);
                        if (originalTransaction == null)
                        {
                            return HttpNotFound();
                        }

                        // Get the product
                        var product = db.Products.Find(transaction.ProductId);
                        if (product == null)
                        {
                            ModelState.AddModelError("", "Product not found.");
                            return View(transaction);
                        }

                        // Revert the original transaction's stock impact
                        if (originalTransaction.TransactionType == "IN")
                        {
                            product.CurrentStock -= originalTransaction.Quantity;
                        }
                        else if (originalTransaction.TransactionType == "OUT")
                        {
                            product.CurrentStock += originalTransaction.Quantity;
                        }

                        // Apply new transaction's stock impact
                        if (transaction.TransactionType == "IN")
                        {
                            product.CurrentStock += transaction.Quantity;
                        }
                        else if (transaction.TransactionType == "OUT")
                        {
                            if (product.CurrentStock < transaction.Quantity)
                            {
                                ModelState.AddModelError("", "Insufficient stock available.");
                                return View(transaction);
                            }
                            product.CurrentStock -= transaction.Quantity;
                        }

                        // Update the transaction
                        db.Entry(originalTransaction).CurrentValues.SetValues(transaction);
                        db.SaveChanges();
                        dbContextTransaction.Commit();

                        return RedirectToAction("Details", new { id = transaction.TransactionId });
                    }
                    catch (Exception)
                    {
                        dbContextTransaction.Rollback();
                        ModelState.AddModelError("", "An error occurred while processing the transaction.");
                    }
                }
            }

            ViewBag.ProductId = new SelectList(db.Products, "ProductId", "Name", transaction.ProductId);
            return View(transaction);
        }
        public JsonResult GetCurrentStock(int id)
        {
            var product = db.Products.Find(id);
            if (product == null)
            {
                return Json(0, JsonRequestBehavior.AllowGet);
            }
            return Json(product.CurrentStock, JsonRequestBehavior.AllowGet);
        }
        public ActionResult LowStockReport()
        {
            var lowStockProducts = db.Products
                .Where(p => p.CurrentStock <= p.MinimumStockLevel)
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .ToList();

            return View(lowStockProducts);
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
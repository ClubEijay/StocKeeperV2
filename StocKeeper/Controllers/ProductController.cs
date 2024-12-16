using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using StocKeeper.Models;

namespace StocKeeper.Controllers
{
    public class ProductController : Controller
    {
        private StocKeeperContext db = new StocKeeperContext();

        // GET: Product
        public ActionResult Index(string searchString, int? categoryId, int? supplierId, decimal? minPrice, decimal? maxPrice)
        {
            // Start with base query
            var query = db.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Name.Contains(searchString)
                    || p.Description.Contains(searchString)
                    || p.Category.Name.Contains(searchString)
                    || p.Supplier.Name.Contains(searchString));
            }

            // Apply category filter
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Apply supplier filter
            if (supplierId.HasValue)
            {
                query = query.Where(p => p.SupplierId == supplierId.Value);
            }

            // Apply price range filter
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.UnitPrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.UnitPrice <= maxPrice.Value);
            }

            // Populate filter dropdowns
            ViewBag.Categories = new SelectList(db.Categories, "CategoryId", "Name");
            ViewBag.Suppliers = new SelectList(db.Suppliers, "SupplierId", "Name");

            // Store filter values for maintaining state
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentSupplierId = supplierId;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;

            return View(query.ToList());
        }

        // GET: Product/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Eager load the Category and Supplier
            Product product = db.Products
                .Include(p => p.Category)  // Ensure Category is loaded
                .Include(p => p.Supplier)  // Ensure Supplier is loaded
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }

        // GET: Product/Create
        public ActionResult Create()
        {
            var viewModel = new ProductViewModel
            {
                Product = new Product(),
                Categories = db.Categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList(),
                Suppliers = GetActiveSuppliers() // Use the new helper method
            };

            return View(viewModel);
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                db.Products.Add(viewModel.Product);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // Repopulate dropdowns
            viewModel.Categories = db.Categories.Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.Name
            }).ToList();
            viewModel.Suppliers = GetActiveSuppliers(); // Use the new helper method

            return View(viewModel);
        }

        // GET: Product/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            var viewModel = new ProductViewModel
            {
                Product = product,
                Categories = db.Categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList(),
                Suppliers = GetActiveSuppliers() // Use the new helper method
            };

            return View(viewModel);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProductViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = db.Products.Find(viewModel.Product.ProductId);
                    if (existingProduct == null)
                    {
                        return HttpNotFound();
                    }

                    // Update the properties of the existing product
                    db.Entry(existingProduct).CurrentValues.SetValues(viewModel.Product);
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while saving the changes. Please try again.");
                }
            }

            // If we got this far, something failed, redisplay form
            viewModel.Categories = db.Categories.Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.Name,
                Selected = c.CategoryId == viewModel.Product.CategoryId
            }).ToList();

            viewModel.Suppliers = db.Suppliers
                .Where(s => s.IsActive) // Filter only active suppliers
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.Name,
                    Selected = s.SupplierId == viewModel.Product.SupplierId
                }).ToList();

            return View(viewModel);
        }

        // GET: Product/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Product product = db.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var product = db.Products
                .Include(p => p.InventoryTransactions)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            // Check if the product has related transactions
            if (product.InventoryTransactions.Any())
            {
                ModelState.AddModelError("", "Cannot delete this product because it is referenced in inventory transactions.");
                return View(product); // Redirects back to the delete view with an error message
            }

            db.Products.Remove(product);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private List<SelectListItem> GetActiveSuppliers()
        {
            return db.Suppliers
                .Where(s => s.IsActive) // Filter only active suppliers
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.Name
                })
                .ToList();
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
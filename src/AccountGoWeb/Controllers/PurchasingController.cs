using Dto.Purchasing;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountGoWeb.Controllers
{
    //[Microsoft.AspNetCore.Authorization.Authorize]
    public class PurchasingController : BaseController
    {
        private readonly ILogger<PurchasingController> _logger;

        public PurchasingController(IConfiguration config, ILogger<PurchasingController> logger)
        {
            _baseConfig = config;

            Models.SelectListItemHelper._config = config;

            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> PurchaseOrders()
        {
            ViewBag.PageContentHeader = "Purchase Orders";

            var purchaseOrders = await GetAsync<object>("purchasing/purchaseorders");
            if (purchaseOrders == null)
            {
                _logger.LogWarning("Failed to load purchase orders.");
                return View(model: string.Empty);
            }

            return View(model: purchaseOrders.ToString());
        }

        public IActionResult AddPurchaseOrder()
        {
            ViewBag.PageContentHeader = "Add Purchase Order";

            PurchaseOrder purchaseOrderModel = new PurchaseOrder
            {
                PurchaseOrderLines = new List<PurchaseOrderLine>
                {
                    new PurchaseOrderLine
                    {
                        Amount = 0,
                        Discount = 0,
                        ItemId = 1,
                        Quantity = 1,
                    }
                },
                No = new Random().Next(1, 99999).ToString()
            };

            PopulatePurchaseOrderFormViewBags();

            return View(purchaseOrderModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPurchaseOrder(PurchaseOrder purchaseOrder, string addRowBtn)
        {
            ViewBag.PageContentHeader = "Add Purchase Order";

            if (!string.IsNullOrEmpty(addRowBtn))
            {
                purchaseOrder.PurchaseOrderLines.Add(new PurchaseOrderLine
                {
                    Amount = 0,
                    Discount = 0,
                    ItemId = 1,
                    Quantity = 1
                });

                PopulatePurchaseOrderFormViewBags();

                return View(purchaseOrder);
            }

            if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(purchaseOrder);
                var content = new StringContent(serialize);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var response = await PostAsync("purchasing/savepurchaseorder", content);
                if (response == null || !response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to save purchase order.");
                    ModelState.AddModelError(string.Empty, "Failed to save purchase order.");

                    PopulatePurchaseOrderFormViewBags();
                    return View(purchaseOrder);
                }

                return RedirectToAction("PurchaseOrders");
            }

           
            PopulatePurchaseOrderFormViewBags();

            return View(purchaseOrder);
        }

        public async Task<IActionResult> PurchaseInvoice(int id)
        {
            ViewBag.PageContentHeader = "Purchase Invoice";

            if (id == 0)
            {
                ViewBag.PageContentHeader = "New Purchase Invoice";
                return View("PurchaseInvoice");
            }

            var purchaseInvoiceModel = await GetAsync<PurchaseInvoice>("Purchasing/PurchaseInvoice?id=" + id);
            if (purchaseInvoiceModel == null)
            {
                _logger.LogWarning("Purchase invoice {Id} not found.", id);
                return NotFound();
            }

            PopulatePurchaseOrderFormViewBags();

            return View(purchaseInvoiceModel);
        }

        public async Task<IActionResult> PurchaseOrder(int id)
        {
            ViewBag.PageContentHeader = "Purchase Order";

            if (id == 0)
            {
                ViewBag.PageContentHeader = "New Purchase Order";
                return View();
            }

            var purchaseOrderModel = await GetAsync<PurchaseOrder>("Purchasing/PurchaseOrder?id=" + id);
            if (purchaseOrderModel == null)
            {
                _logger.LogWarning("Purchase order {Id} not found.", id);
                return NotFound();
            }

            PopulatePurchaseOrderFormViewBags();

            return View(purchaseOrderModel);
        }

        public async Task<IActionResult> PurchaseInvoices()
        {
            ViewBag.PageContentHeader = "Purchase Invoices";

           
            var responseJson = await GetAsync<string>("purchasing/purchaseinvoices");
            if (responseJson == null)
            {
                _logger.LogWarning("Failed to load purchase invoices.");
                return View();
            }

            return View(model: responseJson);
        }

        public IActionResult AddPurchaseInvoice()
        {
            ViewBag.PageContentHeader = "New Invoice";

            PurchaseInvoice purchaseInvoiceModel = new PurchaseInvoice
            {
                PurchaseInvoiceLines = new List<PurchaseInvoiceLine>
                {
                    new PurchaseInvoiceLine
                    {
                        Amount = 0,
                        Discount = 0,
                        ItemId = 1,
                        Quantity = 1,
                    }
                },
                No = new Random().Next(1, 99999).ToString()
            };

            PopulatePurchaseOrderFormViewBags();

            return View(purchaseInvoiceModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPurchaseInvoice(PurchaseInvoice purchaseInvoice, string addRowBtn)
        {
            ViewBag.PageContentHeader = "New Invoice";

            if (!string.IsNullOrEmpty(addRowBtn))
            {
                purchaseInvoice.PurchaseInvoiceLines.Add(new PurchaseInvoiceLine
                {
                    Amount = 0,
                    Discount = 0,
                    ItemId = 1,
                    Quantity = 1
                });

                PopulatePurchaseOrderFormViewBags();

                return View(purchaseInvoice);
            }

            if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(purchaseInvoice);
                var content = new StringContent(serialize);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var response = await PostAsync("Purchasing/SavePurchaseInvoice", content);
                if (response == null || !response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to save purchase invoice.");
                    ModelState.AddModelError(string.Empty, "Failed to save purchase invoice.");

                    PopulatePurchaseOrderFormViewBags();
                    return View(purchaseInvoice);
                }

                _logger.LogInformation("Purchase Invoice Saved {Id}", purchaseInvoice.Id);
                return RedirectToAction("PurchaseInvoices");
            }

            PopulatePurchaseOrderFormViewBags();

            return View(purchaseInvoice);
        }

        public IActionResult AddPurchaseReceipt(int purchId = 0)
        {
            ViewBag.PageContentHeader = "New Receipt";

            return View();
        }

        public async Task<IActionResult> Vendors()
        {
            ViewBag.PageContentHeader = "Vendors";

            var responseJson = await GetAsync<string>("purchasing/vendors");
            if (responseJson == null)
            {
                _logger.LogWarning("Failed to load vendors.");
                return View();
            }

            return View(model: responseJson);
        }

        public async Task<IActionResult> Vendor(int id = -1)
        {
            Dto.Purchasing.Vendor vendorModel;

            if (id == -1)
            {
                ViewBag.PageContentHeader = "New Vendor";
                vendorModel = new Dto.Purchasing.Vendor
                {
                    No = new Random().Next(1, 99999).ToString() // TODO: Replace with system generated numbering.
                };
            }
            else
            {
                ViewBag.PageContentHeader = "Vendor Card";
                vendorModel = await GetAsync<Dto.Purchasing.Vendor>("purchasing/vendor?id=" + id);

                if (vendorModel == null)
                {
                    _logger.LogWarning("Vendor {Id} not found.", id);
                    return NotFound();
                }
            }

           
            PopulateVendorFormViewBags();

            return View(vendorModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveVendor(Dto.Purchasing.Vendor vendorModel)
        {
            if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(vendorModel);
                var content = new StringContent(serialize);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var response = await PostAsync("purchasing/savevendor", content);
                if (response != null && response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Vendors");
                }

                _logger.LogWarning("Failed to save vendor {No}.", vendorModel.No);
                ModelState.AddModelError(string.Empty, "Failed to save vendor.");
            }

            PopulateVendorFormViewBags();

            ViewBag.PageContentHeader = vendorModel.Id == -1 ? "New Vendor" : "Vendor Card";

            return View("Vendor", vendorModel);
        }

        [HttpGet]
        public async Task<IActionResult> Payment(int id)
        {
            ViewBag.PageContentHeader = "Make Payment";

            var invoice = await GetAsync<Dto.Purchasing.PurchaseInvoice>("purchasing/purchaseinvoice?id=" + id);
            if (invoice == null)
            {
                _logger.LogWarning("Purchase invoice {Id} not found for payment.", id);
                return NotFound();
            }

          
            var model = new Models.Purchasing.Payment
            {
                InvoiceId = invoice.Id,
                InvoiceNo = invoice.No,
                VendorId = invoice.VendorId,
                VendorName = invoice.VendorName,
                InvoiceAmount = invoice.Amount,
                AmountPaid = invoice.AmountPaid,
                Date = invoice.InvoiceDate
            };

            ViewBag.CashBanks = Models.SelectListItemHelper.CashBanks();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Payment(Models.Purchasing.Payment model)
        {
            if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                var content = new StringContent(serialize);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var response = await Post("purchasing/savepayment", content);
                if (response != null && response.IsSuccessStatusCode)
                {
                    return RedirectToAction("PurchaseInvoices");
                }

                _logger.LogWarning("Failed to save payment for invoice {InvoiceId}.", model.InvoiceId);
                ModelState.AddModelError(string.Empty, "Failed to save payment.");
            }

            ViewBag.PageContentHeader = "Make Payment";
            ViewBag.CashBanks = Models.SelectListItemHelper.CashBanks();
            return View(model);
        }

      
        private void PopulatePurchaseOrderFormViewBags()
        {
            ViewBag.Vendors = Models.SelectListItemHelper.Vendors();
            ViewBag.PaymentTerms = Models.SelectListItemHelper.PaymentTerms();
            ViewBag.Items = Models.SelectListItemHelper.Items();
            ViewBag.Measurements = Models.SelectListItemHelper.Measurements();
        }

        private void PopulateVendorFormViewBags()
        {
            ViewBag.Accounts = Models.SelectListItemHelper.Accounts();
            ViewBag.TaxGroups = Models.SelectListItemHelper.TaxGroups();
            ViewBag.PaymentTerms = Models.SelectListItemHelper.PaymentTerms();
        }
    }
}
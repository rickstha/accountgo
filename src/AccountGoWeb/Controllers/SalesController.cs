using AccountGoWeb.Models;
using Dto.Sales;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace AccountGoWeb.Controllers
{
    // [Microsoft.AspNetCore.Authorization.Authorize]
    public class SalesController : GoodController
    {
        private readonly ILogger<SalesController> _logger;

        public SalesController(IConfiguration config, ILogger<SalesController> logger)
        {
            _configuration = config;
            Models.SelectListItemHelper._config = config;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(SalesOrders));
        }

        public async Task<IActionResult> SalesOrders()
        {
            ViewBag.PageContentHeader = "Sales Orders";

            try
            {
                using var client = new HttpClient();
                var baseUri = _configuration!["ApiUrl"];
                client.BaseAddress = new Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();

                var response = await client.GetAsync(baseUri + "sales/salesorders");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }

                _logger.LogWarning("Failed to retrieve sales orders. Status: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales orders");
            }

            return View();
        }

        public IActionResult AddSalesOrder()
        {
            ViewBag.PageContentHeader = "Add Sales Order";

            var salesOrderModel = new SalesOrder
            {
                SalesOrderLines = new List<SalesOrderLine>
                {
                    new SalesOrderLine
                    {
                        Amount = 0,
                        Discount = 0,
                        ItemId = 1,
                        Quantity = 1,
                        MeasurementId = 1
                    }
                },
                No = new Random().Next(1, 99999).ToString() // TODO: Replace with system-generated numbering
            };

            PopulateSalesViewBags();
            return View(salesOrderModel);
        }

        [HttpPost]
        public IActionResult AddSalesOrder(SalesOrder dto, string addRowBtn)
        {
            if (!string.IsNullOrEmpty(addRowBtn))
            {
                dto.SalesOrderLines ??= new List<SalesOrderLine>();
                dto.SalesOrderLines.Add(new SalesOrderLine
                {
                    Amount = 0,
                    Quantity = 1,
                    Discount = 0,
                    ItemId = 1,
                    MeasurementId = 1
                });

                PopulateSalesViewBags();
                return View(dto);
            }

            if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
                var content = new StringContent(serialize, Encoding.UTF8, "application/json");

                var response = Post("Sales/addsalesorder", content);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(SalesOrders));
                }

                _logger.LogWarning("Failed to add sales order. Status: {StatusCode}", response.StatusCode);
            }

            PopulateSalesViewBags();
            return View(dto);
        }

        public async Task<IActionResult> SalesOrder(int id)
        {
            if (id == -1)
            {
                ViewBag.PageContentHeader = "Add Sales Order";
                return RedirectToAction(nameof(AddSalesOrder));
            }

            ViewBag.PageContentHeader = "Sales Order";

            var salesOrderModel = await GetAsync<SalesOrder>("Sales/SalesOrder?id=" + id);
            if (salesOrderModel == null)
            {
                return NotFound();
            }

            ViewBag.CustomerName = salesOrderModel.CustomerName;
            ViewBag.OrderDate = salesOrderModel.OrderDate;
            ViewBag.SalesOrderLines = salesOrderModel.SalesOrderLines;
            ViewBag.TotalAmount = salesOrderModel.Amount;

            PopulateSalesViewBags();
            return View(salesOrderModel);
        }

        public async Task<IActionResult> SalesInvoice(int id)
        {
            ViewBag.PageContentHeader = "Sales Invoice";

            if (id == 0)
            {
                ViewBag.PageContentHeader = "Add Sales Invoice";
                return RedirectToAction(nameof(AddSalesInvoice));
            }

            var salesInvoiceModel = await GetAsync<SalesInvoice>("Sales/SalesInvoice?id=" + id);
            if (salesInvoiceModel == null)
            {
                return NotFound();
            }

            ViewBag.Id = salesInvoiceModel.Id;
            ViewBag.CustomerName = salesInvoiceModel.CustomerName;
            ViewBag.InvoiceDate = salesInvoiceModel.InvoiceDate;
            ViewBag.SalesInvoiceLines = salesInvoiceModel.SalesInvoiceLines;
            ViewBag.TotalAmount = salesInvoiceModel.Amount;

            PopulateSalesViewBags();
            return View("SalesInvoice", salesInvoiceModel);
        }

        public async Task<IActionResult> SalesInvoices()
        {
            ViewBag.PageContentHeader = "Sales Invoices";

            try
            {
                using var client = new HttpClient();
                var baseUri = _configuration!["ApiUrl"];
                client.BaseAddress = new Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();

                var response = await client.GetAsync(baseUri + "sales/salesinvoices");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }

                _logger.LogWarning("Failed to retrieve sales invoices. Status: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales invoices");
            }

            PopulateSalesViewBags();
            return View();
        }

        [HttpGet]
        public IActionResult AddSalesInvoice()
        {
            ViewBag.PageContentHeader = "Add Sales Invoice";

            var salesInvoiceModel = new SalesInvoice
            {
                SalesInvoiceLines = new List<SalesInvoiceLine>
                {
                    new SalesInvoiceLine
                    {
                        Amount = 0,
                        Discount = 0,
                        ItemId = 1,
                        Quantity = 1,
                        MeasurementId = 1
                    }
                },
                No = new Random().Next(1, 99999).ToString() // TODO: Replace with system-generated numbering
            };

            PopulateSalesViewBags();
            return View(salesInvoiceModel);
        }

        [HttpPost]
        public async Task<IActionResult> AddSalesInvoice(SalesInvoice dto, string addRowBtn)
        {
            if (!string.IsNullOrEmpty(addRowBtn))
            {
                dto.SalesInvoiceLines ??= new List<SalesInvoiceLine>();
                dto.SalesInvoiceLines.Add(new SalesInvoiceLine
                {
                    Amount = 0,
                    Quantity = 1,
                    Discount = 0,
                    ItemId = 1,
                    MeasurementId = 1
                });

                PopulateSalesViewBags();
                return View(dto);
            }

            if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
                var content = new StringContent(serialize, Encoding.UTF8, "application/json");

                _logger.LogInformation("AddSalesInvoice payload: {Payload}", await content.ReadAsStringAsync());

                var response = Post("Sales/SaveSalesInvoice", content);
                _logger.LogInformation("AddSalesInvoice response: {Response}", response);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(SalesInvoices));
                }
            }

            PopulateSalesViewBags();
            return View(dto);
        }

        public async Task<IActionResult> SalesReceipts()
        {
            ViewBag.PageContentHeader = "Sales Receipts";

            try
            {
                using var client = new HttpClient();
                var baseUri = _configuration!["ApiUrl"];
                client.BaseAddress = new Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();

                var response = await client.GetAsync(baseUri + "sales/salesreceipts");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }

                _logger.LogWarning("Failed to retrieve sales receipts. Status: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales receipts");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> AddReceipt()
        {
            ViewBag.PageContentHeader = "New Receipt";

            var model = new Models.Sales.AddReceipt();

            ViewBag.Customers = SelectListItemHelper.Customers();
            ViewBag.DebitAccounts = SelectListItemHelper.CashBanks();
            ViewBag.CreditAccounts = SelectListItemHelper.Accounts();

            var customers = await GetAsync<IEnumerable<Customer>>("sales/customers");
            ViewBag.CustomersDetail = Newtonsoft.Json.JsonConvert.SerializeObject(customers ?? Enumerable.Empty<Customer>());

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddReceipt(Models.Sales.AddReceipt model)
        {
            if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                var content = new StringContent(serialize, Encoding.UTF8, "application/json");

                var response = Post("sales/savereceipt", content);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(SalesReceipts));
                }
            }

            ViewBag.PageContentHeader = "New Receipt";
            ViewBag.Customers = SelectListItemHelper.Customers();
            ViewBag.DebitAccounts = SelectListItemHelper.CashBanks();
            ViewBag.CreditAccounts = SelectListItemHelper.Accounts();

            var customers = await GetAsync<IEnumerable<Customer>>("sales/customers");
            ViewBag.CustomersDetail = Newtonsoft.Json.JsonConvert.SerializeObject(customers ?? Enumerable.Empty<Customer>());

            return View(model);
        }

        public async Task<IActionResult> Customers()
        {
            ViewBag.PageContentHeader = "Customers";

            try
            {
                using var client = new HttpClient();
                var baseUri = _configuration!["ApiUrl"];
                client.BaseAddress = new Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();

                var response = await client.GetAsync(baseUri + "sales/customers");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }

                _logger.LogWarning("Failed to retrieve customers. Status: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers");
            }

            return View();
        }

        public async Task<IActionResult> Customer(int id = -1)
        {
            Customer? customerModel;

            if (id == -1)
            {
                ViewBag.PageContentHeader = "New Customer";
                customerModel = new Customer
                {
                    No = new Random().Next(1, 99999).ToString() // TODO: Replace with system-generated numbering
                };
            }
            else
            {
                ViewBag.PageContentHeader = "Customer Card";
                customerModel = await GetAsync<Customer>("sales/customer?id=" + id);
                if (customerModel == null)
                {
                    return NotFound();
                }
            }

            ViewBag.Accounts = SelectListItemHelper.Accounts();
            ViewBag.TaxGroups = SelectListItemHelper.TaxGroups();
            ViewBag.PaymentTerms = SelectListItemHelper.PaymentTerms();

            return View(customerModel);
        }

        [HttpPost]
        public async Task<IActionResult> SaveSalesInvoice(SalesInvoice salesInvoiceModel)
        {
            if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(salesInvoiceModel);
                var content = new StringContent(serialize, Encoding.UTF8, "application/json");

                var payload = await content.ReadAsStringAsync();
                _logger.LogInformation("SaveSalesInvoice payload: {Payload}", payload);

                var response = Post("Sales/SaveSalesInvoice", content);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(SalesInvoices));
                }
            }

            PopulateSalesViewBags();
            return View("SalesInvoice", salesInvoiceModel);
        }

        [HttpPost]
        public async Task<IActionResult> SaveCustomer(Customer customerModel)
        {
            if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(customerModel);
                var content = new StringContent(serialize, Encoding.UTF8, "application/json");

                var response = await PostAsync("Sales/SaveCustomer", content);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Customers));
                }
            }

            ViewBag.Accounts = SelectListItemHelper.Accounts();
            ViewBag.TaxGroups = SelectListItemHelper.TaxGroups();
            ViewBag.PaymentTerms = SelectListItemHelper.PaymentTerms();

            ViewBag.PageContentHeader = customerModel.Id == -1 || customerModel.Id == 0
                ? "New Customer"
                : "Customer Card";

            return View("Customer", customerModel);
        }

        public IActionResult CustomerAllocations(int id)
        {
            ViewBag.PageContentHeader = "Customer Allocations";
            return View();
        }

        public async Task<IActionResult> Allocate(int id)
        {
            ViewBag.PageContentHeader = "Receipt Allocation";

            var receipt = await GetAsync<Dto.Sales.SalesReceipt>("sales/salesreceipt?id=" + id);
            if (receipt == null)
            {
                return NotFound();
            }

            var model = new Models.Sales.Allocate
            {
                CustomerId = receipt.CustomerId,
                ReceiptId = receipt.Id,
                Date = receipt.ReceiptDate,
                Amount = receipt.Amount,
                RemainingAmountToAllocate = receipt.RemainingAmountToAllocate,
                AllocationLines = new List<Models.Sales.AllocationLine>()
            };

            ViewBag.CustomerName = receipt.CustomerName;
            ViewBag.ReceiptNo = receipt.ReceiptNo;

            var invoices = await GetAsync<IEnumerable<Dto.Sales.SalesInvoice>>("sales/customerinvoices?id=" + receipt.CustomerId)
                           ?? Enumerable.Empty<Dto.Sales.SalesInvoice>();

            foreach (var invoice in invoices)
            {
                if (invoice.Posted && invoice.TotalAllocatedAmount < invoice.Amount)
                {
                    model.AllocationLines.Add(new Models.Sales.AllocationLine
                    {
                        InvoiceId = invoice.Id,
                        Amount = invoice.Amount,
                        AllocatedAmount = invoice.TotalAllocatedAmount
                    });
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Allocate(Models.Sales.Allocate model)
        {
            if (ModelState.IsValid && model.IsValid())
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                var content = new StringContent(serialize, Encoding.UTF8, "application/json");

                var response = Post("sales/saveallocation", content);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(SalesReceipts));
                }
            }

            var receipt = await GetAsync<Dto.Sales.SalesReceipt>("sales/salesreceipt?id=" + model.ReceiptId);
            if (receipt != null)
            {
                ViewBag.CustomerName = receipt.CustomerName;
                ViewBag.ReceiptNo = receipt.ReceiptNo;
            }

            return View(model);
        }

        public async Task<IActionResult> SalesInvoicePdf(int id)
        {
            var invoice = await GetAsync<Dto.Sales.SalesInvoice>("sales/salesinvoiceforprinting?id=" + id);
            if (invoice == null)
            {
                return NotFound();
            }

            var salesInvoiceModel = new SalesInvoice
            {
                ReferenceNo = invoice.ReferenceNo,
                No = invoice.No,
                InvoiceDate = invoice.InvoiceDate,
                CompanyName = invoice.CompanyName,
                TotalTax = invoice.TotalTax,
                TotalAmountAfterTax = invoice.TotalAmountAfterTax,
                CustomerName = invoice.CustomerName,
                SalesInvoiceLines = invoice.SalesInvoiceLines
            };

            return View(salesInvoiceModel);
        }

        #region Private Helpers

        private void PopulateSalesViewBags()
        {
            ViewBag.Customers = SelectListItemHelper.Customers();
            ViewBag.PaymentTerms = SelectListItemHelper.PaymentTerms();
            ViewBag.Items = SelectListItemHelper.Items();
            ViewBag.Measurements = SelectListItemHelper.Measurements();
        }

        #endregion
    }
}
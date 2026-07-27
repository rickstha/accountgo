using Dto.Sales;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountGoWeb.Controllers
{
    //[Microsoft.AspNetCore.Authorization.Authorize]
    // NOTE (flagged, not changed): every other controller reviewed in this app extends
    // BaseController. This one extends GoodController instead - please verify this is
    // intentional and not a typo, and that GoodController actually exposes _configuration,
    // GetAsync<T>, and PostAsync the same way BaseController does.
    public class QuotationsController : GoodController
    {
        //private readonly IConfiguration _configuration;
        private readonly ILogger<QuotationsController> _logger;

        public QuotationsController(IConfiguration config, ILogger<QuotationsController> logger)
        {
            _configuration = config;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Quotations");
        }

        public async Task<IActionResult> Quotations()
        {
            ViewBag.PageContentHeader = "Quotations";

            // Switched from a manually-created HttpClient (resource leak / socket
            // exhaustion risk under load) to the GetAsync<T> helper, matching how
            // Quotation(id) already uses it in this same class.
            var responseJson = await GetAsync<string>("sales/quotations");
            if (responseJson == null)
            {
                _logger.LogWarning("Failed to load quotations.");
                return View();
            }

            return View(model: responseJson);
        }

        [HttpGet]
        public IActionResult AddSalesQuotation()
        {
            ViewBag.PageContentHeader = "Add Sales Quotation";

            SalesQuotation model = new SalesQuotation
            {
                SalesQuotationLines = new List<SalesQuotationLine>
                {
                    new SalesQuotationLine
                    {
                        Amount = 0,
                        Quantity = 1,
                        Discount = 0,
                        ItemId = 1,
                        MeasurementId = 1,
                    }
                },
                No = new Random().Next(1, 99999).ToString() // TODO: Replace with system generated numbering.
            };

            PopulateQuotationFormViewBags();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSalesQuotation(SalesQuotation model, string addRowBtn)
        {
            if (!string.IsNullOrEmpty(addRowBtn))
            {
                _logger.LogInformation("Add Row Button Clicked");

                model.SalesQuotationLines ??= new List<SalesQuotationLine>();
                model.SalesQuotationLines.Add(new SalesQuotationLine
                {
                    Amount = 0,
                    Quantity = 1,
                    Discount = 0,
                    ItemId = 1,
                    MeasurementId = 1,
                });

                PopulateQuotationFormViewBags();

                return View(model);
            }

            if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                var content = new StringContent(serialize);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                _logger.LogInformation("Quotation ID is: " + model.Id);

                var response = await PostAsync("sales/savequotation", content);
                if (response != null && response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Quotations");
                }

                _logger.LogWarning("Failed to save sales quotation.");
                ModelState.AddModelError(string.Empty, "Failed to save sales quotation.");
            }

            // Redisplay the same form with data intact instead of a blank view.
            PopulateQuotationFormViewBags();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Quotation(int id)
        {
            ViewBag.PageContentHeader = "Sales";

            if (id == 0)
            {
                // Delegate to the dedicated Add action so the form gets its
                // required ViewBag select lists populated correctly, instead of
                // rendering that view here without them.
                return RedirectToAction(nameof(AddSalesQuotation));
            }

            var model = await GetAsync<SalesQuotation>("Sales/Quotation?id=" + id);
            if (model == null)
            {
                _logger.LogWarning("Sales quotation {Id} not found.", id);
                return NotFound();
            }

            ViewBag.Id = model.Id;
            ViewBag.QuotationDate = model.QuotationDate;
            ViewBag.CustomerName = model.CustomerName;
            ViewBag.PaymentTermId = model.PaymentTermId;
            ViewBag.SalesQuotationLines = model.SalesQuotationLines;
            ViewBag.TotalAmount = model.Amount;

            PopulateQuotationFormViewBags();

            return View(model);
        }

        // ------------------------------------------------------------------
        // Shared ViewBag population helper (extracted to remove duplicate
        // code that was repeated across 3 action methods).
        // ------------------------------------------------------------------
        private void PopulateQuotationFormViewBags()
        {
            ViewBag.Customers = Models.SelectListItemHelper.Customers();
            ViewBag.Items = Models.SelectListItemHelper.Items();
            ViewBag.PaymentTerms = Models.SelectListItemHelper.PaymentTerms();
            ViewBag.Measurements = Models.SelectListItemHelper.Measurements();
        }
    }
}
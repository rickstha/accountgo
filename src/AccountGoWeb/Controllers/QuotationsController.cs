using Dto.Sales;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountGoWeb.Controllers
{
   
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
                No = new Random().Next(1, 99999).ToString() 
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

           
            PopulateQuotationFormViewBags();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Quotation(int id)
        {
            ViewBag.PageContentHeader = "Sales";

            if (id == 0)
            {
              
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

    
        private void PopulateQuotationFormViewBags()
        {
            ViewBag.Customers = Models.SelectListItemHelper.Customers();
            ViewBag.Items = Models.SelectListItemHelper.Items();
            ViewBag.PaymentTerms = Models.SelectListItemHelper.PaymentTerms();
            ViewBag.Measurements = Models.SelectListItemHelper.Measurements();
        }
    }
}
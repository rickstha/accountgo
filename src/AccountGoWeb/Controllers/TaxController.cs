using Microsoft.AspNetCore.Mvc;

namespace AccountGoWeb.Controllers
{
    //[Microsoft.AspNetCore.Authorization.Authorize]
    public class TaxController : BaseController
    {
        public TaxController(Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _baseConfig = config;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Taxes");
        }

        public async Task<IActionResult> Taxes()
        {
            ViewBag.PageContentHeader = "Tax";

            using (var client = new System.Net.Http.HttpClient())
            {
                var baseUri = _baseConfig!["ApiUrl"];
                client.BaseAddress = new System.Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync(baseUri + "tax/taxes");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var taxSystemDto = Newtonsoft.Json.JsonConvert.DeserializeObject<Dto.TaxSystem.TaxSystemDto>(responseJson);
                    var taxSystemViewModel = new Models.TaxSystem.TaxSystemViewModel();
                    taxSystemViewModel.Taxes = taxSystemDto!.Taxes;
                    taxSystemViewModel.ItemTaxGroups = taxSystemDto.ItemTaxGroups;
                    taxSystemViewModel.TaxGroups = taxSystemDto.TaxGroups;

                    return View(taxSystemViewModel);
                }
            }

            return View();
        }

        // =====================================================
        // GENERATE TAX FOR CUSTOMER
        // =====================================================

        [HttpGet]
        public IActionResult GenerateTax()
        {
            ViewBag.PageContentHeader = "Generate Tax For Customer";

            ViewBag.Customers = Models.SelectListItemHelper.Customers();
            ViewBag.Items = Models.SelectListItemHelper.Items();

            return View(new Models.TaxSystem.GenerateTaxViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateTax(Models.TaxSystem.GenerateTaxViewModel model)
        {
            ViewBag.PageContentHeader = "Generate Tax For Customer";
            ViewBag.Customers = Models.SelectListItemHelper.Customers();
            ViewBag.Items = Models.SelectListItemHelper.Items();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.CustomerId <= 0)
            {
                ModelState.AddModelError(string.Empty, "Please select a customer.");
                return View(model);
            }

            if (model.ItemId <= 0)
            {
                ModelState.AddModelError(string.Empty, "Please select an item.");
                return View(model);
            }

            
            var query = "tax/generatetaxforcustomer" +
                        $"?customerId={model.CustomerId}" +
                        $"&itemId={model.ItemId}" +
                        $"&amount={model.Amount}" +
                        $"&quantity={model.Quantity.GetValueOrDefault(1)}" +
                        $"&discount={model.Discount.GetValueOrDefault(0)}";

            var result = await GetAsync<Models.TaxSystem.GenerateTaxViewModel>(query);

            if (result == null)
            {
                ModelState.AddModelError(string.Empty, "Failed to generate tax for the selected customer.");
                return View(model);
            }

            model.TaxAmount = result.TaxAmount;
            model.TotalAmountAfterTax = result.TotalAmountAfterTax;
            model.CustomerName = result.CustomerName;
            model.ItemDescription = result.ItemDescription;

            return View(model);
        }
    }
}
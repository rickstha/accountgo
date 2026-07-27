using Dto.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace AccountGoWeb.Controllers
{
    // [Microsoft.AspNetCore.Authorization.Authorize]
    public class InventoryController : BaseController
    {
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(
            Microsoft.Extensions.Configuration.IConfiguration config,
            ILogger<InventoryController> logger)
        {
            _baseConfig = config;
            Models.SelectListItemHelper._config = config;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.PageContentHeader = "Items";

            try
            {
                using var client = new HttpClient();
                var baseUri = _baseConfig!["ApiUrl"];
                client.BaseAddress = new Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();

                var response = await client.GetAsync(baseUri + "inventory/items");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }

                _logger.LogWarning("Failed to retrieve items. Status: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading items");
            }

            return View();
        }

        public async Task<IActionResult> ICJ()
        {
            ViewBag.PageContentHeader = "Inventory Control Journal";

            try
            {
                using var client = new HttpClient();
                var baseUri = _baseConfig!["ApiUrl"];
                client.BaseAddress = new Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();

                var response = await client.GetAsync(baseUri + "inventory/icj");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }

                _logger.LogWarning("Failed to retrieve ICJ. Status: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Inventory Control Journal");
            }

            return View();
        }

        public async Task<IActionResult> Item(int id)
        {
            _logger.LogInformation("GetItem: {Id}", id);

            Item? itemModel;

            if (id == -1)
            {
                ViewBag.PageContentHeader = "New Item";
                itemModel = new Item
                {
                    No = new Random().Next(1, 99999).ToString() // TODO: Replace with system-generated numbering
                };
            }
            else
            {
                ViewBag.PageContentHeader = "Item Card";
                itemModel = await GetAsync<Item>("inventory/item?id=" + id);

                if (itemModel == null)
                {
                    return NotFound();
                }
            }

            PopulateItemViewBags();
            return View(itemModel);
        }

        [HttpGet]
        public IActionResult AddItem()
        {
            ViewBag.PageContentHeader = "New Item";

            var itemModel = new Item
            {
                No = new Random().Next(1, 99999).ToString() // TODO: Replace with system-generated numbering
            };

            PopulateItemViewBags();
            return View(itemModel);
        }

        [HttpPost]
        public async Task<IActionResult> AddItem(Item itemModel)
        {
            ViewBag.PageContentHeader = "New Item";

            if (ModelState.IsValid)
            {
                _logger.LogInformation("Item Model is Valid: {Description}", itemModel.Description);

                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(itemModel);
                var content = new StringContent(serialize, Encoding.UTF8, "application/json");

                var response = await Post("Inventory/SaveItem", content);
                _logger.LogInformation("AddItem response: {Response}", response);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogWarning("Failed to save new item. Status: {StatusCode}", response.StatusCode);
            }

            PopulateItemViewBags();
            return View(itemModel);
        }

        [HttpPost]
        public async Task<IActionResult> SaveItem(Item itemModel)
        {
            if (ModelState.IsValid)
            {
                var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(itemModel);
                var content = new StringContent(serialize, Encoding.UTF8, "application/json");

                var response = await Post("inventory/saveitem", content);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogWarning("Failed to save item. Status: {StatusCode}", response.StatusCode);
            }

            // Re-populate dropdowns and return the edit view on failure
            PopulateItemViewBags();

            ViewBag.PageContentHeader = itemModel.Id > 0 ? "Item Card" : "New Item";

            // Return the Item view (not Index) so the user can correct the data
            return View("Item", itemModel);
        }

        #region Private Helpers

        private void PopulateItemViewBags()
        {
            ViewBag.Accounts = Models.SelectListItemHelper.Accounts();
            ViewBag.ItemTaxGroups = Models.SelectListItemHelper.ItemTaxGroups();
            ViewBag.Measurements = Models.SelectListItemHelper.UnitOfMeasurements();
            ViewBag.ItemCategories = Models.SelectListItemHelper.ItemCategories();
            ViewBag.PreferredVendorId = Models.SelectListItemHelper.Vendors();
        }

        #endregion
    }
}
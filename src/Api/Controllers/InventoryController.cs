using Microsoft.AspNetCore.Mvc;
using Dto.Inventory;
using Services.Administration;
using Services.Inventory;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : BaseController
    {
        private readonly IAdministrationService _adminService;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(
            IAdministrationService adminService,
            IInventoryService inventoryService,
            ILogger<InventoryController> logger)
        {
            _adminService = adminService;
            _inventoryService = inventoryService;
            _logger = logger;
        }

        // =====================================================
        // SAVE ITEM
        // =====================================================

        [HttpPost]
        [Route("saveitem")]
        public IActionResult SaveItem([FromBody] Item itemDto)
        {
            try
            {
                if (itemDto == null)
                {
                    return BadRequest("Item data is required.");
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToArray();

                    return BadRequest(errors);
                }

                bool isNew = itemDto.Id == 0;

                Core.Domain.Items.Item item;

                if (isNew)
                {
                    item = new Core.Domain.Items.Item();
                }
                else
                {
                    item = _inventoryService.GetItemById(itemDto.Id);

                    if (item == null)
                    {
                        return NotFound($"Item with Id {itemDto.Id} not found.");
                    }
                }

                _logger.LogInformation("Saving Item. IsNew: {IsNew}", isNew);

                item.No = itemDto.No;
                item.Code = itemDto.Code;
                item.Description = itemDto.Description;
                item.SellDescription = itemDto.SellDescription;
                item.PurchaseDescription = itemDto.PurchaseDescription;
                item.Cost = itemDto.Cost;
                item.Price = itemDto.Price;
                item.SmallestMeasurementId = itemDto.SmallestMeasurementId;
                item.SellMeasurementId = itemDto.SellMeasurementId;
                item.PurchaseMeasurementId = itemDto.PurchaseMeasurementId;
                item.ItemCategoryId = itemDto.ItemCategoryId;
                item.ItemTaxGroupId = itemDto.ItemTaxGroupId;
                item.SalesAccountId = itemDto.SalesAccountId;
                item.InventoryAccountId = itemDto.InventoryAccountId;
                item.InventoryAdjustmentAccountId = itemDto.InventoryAdjustmentAccountId;
                item.CostOfGoodsSoldAccountId = itemDto.CostOfGoodsSoldAccountId;
                item.PreferredVendorId = itemDto.PreferredVendorId;

                // NOTE: removed two blocks that used to sit here:
                //   1. item.MeasurementId / item.Quanitity / item.Discount = itemDto.ItemMeasurementId / .ItemQuantity / .ItemDsicount
                //   2. item.Code = itemsDto.ItemCode; ... item.ItemSaleItems = itemsDto.ItemItemSaleItems;
                // Block 2 referenced `itemsDto`, a variable that only exists in the unrelated
                // Items() method below — this did not compile (CS0103: the name 'itemsDto'
                // does not exist in the current context). It also duplicated fields
                // (Code, Description, Cost, Price, PurchaseMeasurementId, ItemTaxGroupId)
                // that are already set correctly above from `itemDto`.
                // Block 1 referenced properties (Quanitity, ItemDsicount, MeasurementId)
                // that don't appear anywhere else in this file's DTO/domain usage
                // (e.g. the Item(int id) GET endpoint below has no such fields) and looked
                // like unused/speculative leftovers rather than real requirements.
                // If a real "quantity/discount/measurement" field is actually needed on
                // save, it should be added back deliberately with a confirmed DTO/domain
                // property name, not copy-pasted from another method's variable.

                if (isNew)
                {
                    _inventoryService.AddItem(item);
                }
                else
                {
                    _inventoryService.UpdateItem(item);
                }

                return Ok(new
                {
                    message = "Item saved successfully."
                });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error while saving item");

                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        // =====================================================
        // GET ALL ITEMS
        // =====================================================

        [HttpGet]
        [Route("items")]
        public IActionResult Items()
        {
            try
            {
                var items = _inventoryService.GetAllItems();

                ICollection<Item> itemsDto = new List<Item>();

                foreach (var item in items)
                {
                    // NOTE: this object initializer previously ended with:
                    //   QuantityOnHand = item.ComputeQuantityOnHand()
                    //   MeasurementId = item.ItemMeasurementId;
                    //   Quanitity = item.ItemQuantity;
                    //   Discount = item.ItemDsicount;
                    // — missing comma after ComputeQuantityOnHand(), and semicolons
                    // used instead of commas inside a `{ }` object initializer, which
                    // is not valid C# syntax (object initializers are comma-separated
                    // Property = value pairs, not statements). This did not compile.
                    // The trailing MeasurementId/Quanitity/Discount properties also
                    // don't appear anywhere else in this controller's DTO usage, so
                    // they were dropped rather than guessed at; the shape below now
                    // matches the fields used consistently elsewhere in this file.
                    itemsDto.Add(new Item
                    {
                        Id = item.Id,
                        Code = item.Code,
                        Description = item.Description,
                        ItemTaxGroupName = item.ItemTaxGroup?.Name ?? "",
                        Measurement = item.PurchaseMeasurement?.Description ?? "",
                        Cost = item.Cost,
                        Price = item.Price,
                        QuantityOnHand = item.ComputeQuantityOnHand()
                    });
                }

                return Ok(itemsDto);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error while getting items");

                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        // =====================================================
        // GET SINGLE ITEM
        // =====================================================

        [HttpGet]
        [Route("item/{id}")]
        public IActionResult Item(int id)
        {
            try
            {
                var item = _inventoryService.GetItemById(id);

                if (item == null)
                {
                    return NotFound($"Item with Id {id} not found.");
                }

                var itemDto = new Item
                {
                    Id = item.Id,
                    Code = item.Code,
                    Description = item.Description,
                    Cost = item.Cost,
                    Price = item.Price,
                    SellDescription = item.SellDescription,
                    PurchaseDescription = item.PurchaseDescription,
                    QuantityOnHand = item.ComputeQuantityOnHand(),
                    ItemCategoryId = item.ItemCategoryId,
                    SmallestMeasurementId = item.SmallestMeasurementId,
                    SellMeasurementId = item.SellMeasurementId,
                    PurchaseMeasurementId = item.PurchaseMeasurementId,
                    PreferredVendorId = item.PreferredVendorId,
                    ItemTaxGroupId = item.ItemTaxGroupId,
                    SalesAccountId = item.SalesAccountId,
                    InventoryAccountId = item.InventoryAccountId,
                    CostOfGoodsSoldAccountId = item.CostOfGoodsSoldAccountId,
                    InventoryAdjustmentAccountId = item.InventoryAdjustmentAccountId
                };

                return Ok(itemDto);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error while getting item");

                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        // =====================================================
        // INVENTORY CONTROL JOURNAL
        // =====================================================

        [HttpGet]
        [Route("icj")]
        public IActionResult ICJ()
        {
            try
            {
                var invControlJournals =
                    _inventoryService.GetInventoryControlJournals();

                var icjDto = new List<InventoryControlJournal>();

                foreach (var icj in invControlJournals)
                {
                    icjDto.Add(new InventoryControlJournal
                    {
                        Id = icj.Id,
                        In = icj.INQty,
                        Out = icj.OUTQty,
                        Item = icj.Item?.Description ?? "",
                        Measurement = icj.Measurement?.Code ?? "",
                        Date = icj.Date
                    });
                }

                _logger.LogInformation(
                    "ICJ Count: {Count}",
                    icjDto.Count
                );

                return Ok(icjDto);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error while getting ICJ");

                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }
    }
}
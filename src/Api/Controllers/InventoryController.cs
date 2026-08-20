using Microsoft.AspNetCore.Mvc;
using Dto.Inventory;
using Services.Administration;
using Services.Inventory;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Api.Controllers
{
    // Recommended: enable authorization before production
    // [Microsoft.AspNetCore.Authorization.Authorize]
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

                if (isNew)
                {
                    _inventoryService.AddItem(item);
                }
                else
                {
                    _inventoryService.UpdateItem(item);
                }

                return Ok(new { message = "Item saved successfully." });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error while saving item");
                return StatusCode(500, new { message = "An error occurred while saving the item." });
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
                var items = _inventoryService.GetAllItems()
                            ?? Enumerable.Empty<Core.Domain.Items.Item>();

                var itemsDto = new List<Item>();

                foreach (var item in items)
                {
                    itemsDto.Add(new Item
                    {
                        Id = item.Id,
                        Code = item.Code,
                        Description = item.Description,
                        ItemTaxGroupName = item.ItemTaxGroup?.Name ?? "",
                        Measurement = item.PurchaseMeasurement?.Description ?? "",
                        Cost = item.Cost,
                        Price = item.Price,
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
                    });
                }

                return Ok(itemsDto);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error while getting items");
                return StatusCode(500, new { message = "An error occurred while retrieving items." });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the item." });
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
                    _inventoryService.GetInventoryControlJournals()
                    ?? Enumerable.Empty<Core.Domain.Items.InventoryControlJournal>();

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

                _logger.LogInformation("ICJ Count: {Count}", icjDto.Count);

                return Ok(icjDto);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error while getting ICJ");
                return StatusCode(500, new { message = "An error occurred while retrieving inventory control journals." });
            }
        }
    }
}
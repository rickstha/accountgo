using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Services.Purchasing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchasingController : BaseController
    {
        private readonly IPurchasingService _purchasingService;
        private readonly ILogger<PurchasingController> _logger;

        public PurchasingController(
            IPurchasingService purchasingService,
            ILogger<PurchasingController> logger)
        {
            _purchasingService = purchasingService;
            _logger = logger;
        }

        [HttpPost]
        [Route("savepurchaseorder")]
        public IActionResult SavePurchaseOrder(
            [FromBody] Dto.Purchasing.PurchaseOrder purchaseOrderDto)
        {
            try
            {
                if (purchaseOrderDto == null)
                {
                    return BadRequest("Purchase order data is required.");
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToArray();

                    return BadRequest(errors);
                }

                bool isNew = purchaseOrderDto.Id == 0;

                Core.Domain.Purchases.PurchaseOrderHeader purchaseOrder;

                if (isNew)
                {
                    purchaseOrder = new Core.Domain.Purchases.PurchaseOrderHeader
                    {
                        PurchaseOrderLines = new List<Core.Domain.Purchases.PurchaseOrderLine>()
                    };
                }
                else
                {
                    purchaseOrder = _purchasingService.GetPurchaseOrderById(purchaseOrderDto.Id);

                    if (purchaseOrder == null)
                        return NotFound("Purchase order not found.");

                    purchaseOrder.PurchaseOrderLines ??= new List<Core.Domain.Purchases.PurchaseOrderLine>();
                }

                purchaseOrder.No = purchaseOrderDto.No;
                purchaseOrder.ReferenceNo = purchaseOrderDto.ReferenceNo;
                purchaseOrder.PaymentTermId = purchaseOrderDto.PaymentTermId;
                purchaseOrder.VendorId = purchaseOrderDto.VendorId;
                purchaseOrder.Date = purchaseOrderDto.OrderDate;

                var incomingLines = purchaseOrderDto.PurchaseOrderLines
                    ?? new List<Dto.Purchasing.PurchaseOrderLine>();

                foreach (var line in incomingLines)
                {
                    var existingLine = purchaseOrder.PurchaseOrderLines
                        .FirstOrDefault(x => x.Id == line.Id);

                    if (existingLine != null)
                    {
                        existingLine.MeasurementId = line.MeasurementId ?? 0;
                        existingLine.Amount = line.Amount ?? 0;
                        existingLine.Discount = line.Discount ?? 0;
                        existingLine.Quantity = line.Quantity ?? 0;
                        existingLine.ItemId = line.ItemId ?? 0;
                    }
                    else
                    {
                        purchaseOrder.PurchaseOrderLines.Add(
                            new Core.Domain.Purchases.PurchaseOrderLine
                            {
                                Amount = line.Amount ?? 0,
                                Discount = line.Discount ?? 0,
                                Quantity = line.Quantity ?? 0,
                                ItemId = line.ItemId ?? 0,
                                MeasurementId = line.MeasurementId ?? 0
                            });
                    }
                }

                if (isNew)
                {
                    _purchasingService.AddPurchaseOrder(purchaseOrder, true);
                }
                else
                {
                    var deleted = purchaseOrder.PurchaseOrderLines
                        .Where(line => !incomingLines.Any(x => x.Id == line.Id))
                        .ToList();

                    foreach (var line in deleted)
                    {
                        if (line.PurchaseInvoiceLines != null && line.PurchaseInvoiceLines.Any())
                        {
                            return BadRequest("Cannot delete line because invoice exists.");
                        }
                    }

                    foreach (var line in deleted)
                    {
                        purchaseOrder.PurchaseOrderLines.Remove(line);
                    }

                    _purchasingService.UpdatePurchaseOrder(purchaseOrder);
                }

                return Ok(new
                {
                    message = "Purchase order saved successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save purchase order.");
                return StatusCode(500, new
                {
                    message = ex.InnerException?.Message ?? ex.Message
                });
            }
        }
    }
}
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
            purchaseOrder = _purchasingService
                .GetPurchaseOrder ById(purchaseOrderDto.Id);

            if (purchaseOrder == null)
                return NotFound("Purchase order not found.");

            // Defensive: guard against an uninitialized collection on existing entities too.
            purchaseOrder.PurchaseOrderLines ??= new List<Core.Domain.Purchases.PurchaseOrderLine>();
        }

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
                // Only line-level fields belong here — header fields
                // (ReferenceNo, PaymentTermId, VendorId, Date) were removed.
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
                    return BadRequest(
                        "Cannot delete line because invoice exists.");
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
        return StatusCode(500, new
        {
            message = ex.InnerException?.Message ?? ex.Message
        });
    }
}
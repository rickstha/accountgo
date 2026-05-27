[HttpPost]
[Route("savepurchaseorder")]
public IActionResult SavePurchaseOrder(
    [FromBody] Dto.Purchasing.PurchaseOrder purchaseOrderDto)
{
    try
    {
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
            purchaseOrder = new Core.Domain.Purchases.PurchaseOrderHeader();
        }
        else
        {
            purchaseOrder = _purchasingService
                .GetPurchaseOrderById(purchaseOrderDto.Id);

            if (purchaseOrder == null)
                return NotFound("Purchase order not found.");
        }

        purchaseOrder.ReferenceNo = purchaseOrderDto.ReferenceNo;
        purchaseOrder.PaymentTermId = purchaseOrderDto.PaymentTermId;
        purchaseOrder.VendorId = purchaseOrderDto.VendorId;
        purchaseOrder.Date = purchaseOrderDto.OrderDate;

        foreach (var line in purchaseOrderDto.PurchaseOrderLines)
        {
            var existingLine = purchaseOrder.PurchaseOrderLines
                .FirstOrDefault(x => x.Id == line.Id);

            if (existingLine != null)
            {
                existingLine.Amount = line.Amount ?? 0;
                existingLine.Discount = line.Discount ?? 0;
                existingLine.Quantity = line.Quantity ?? 0;
                existingLine.ItemId = line.ItemId ?? 0;
                existingLine.MeasurementId = line.MeasurementId ?? 0;
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
                .Where(line => !purchaseOrderDto.PurchaseOrderLines
                    .Any(x => x.Id == line.Id))
                .ToList();

            foreach (var line in deleted)
            {
                if (line.PurchaseInvoiceLines.Any())
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
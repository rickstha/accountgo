using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Services.Administration;
using Services.Financial;
using Services.Sales;
using Services.Inventory;
using Services.TaxSystem;
using Core.Domain;
using Core.Domain.Sales;
using Dto.Sales;
using CustomerDto = Dto.Sales.Customer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.Controllers
{
    // Recommended: enable authorization before production
    // [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SalesController : BaseController
    {
        private readonly IAdministrationService _adminService;
        private readonly ISalesService _salesService;
        private readonly IFinancialService _financialService;
        private readonly IInventoryService _inventoryService;
        private readonly ITaxService _taxService;
        private readonly ILogger<SalesController> _logger;

        public SalesController(
            IAdministrationService adminService,
            ISalesService salesService,
            IFinancialService financialService,
            IInventoryService inventoryService,
            ITaxService taxService,
            ILogger<SalesController> logger)
        {
            _adminService = adminService;
            _salesService = salesService;
            _financialService = financialService;
            _inventoryService = inventoryService;
            _taxService = taxService;
            _logger = logger;
        }

        // =========================================
        // CUSTOMER
        // =========================================

        [HttpPost]
        [Route("SaveCustomer")]
        public IActionResult SaveCustomer([FromBody] CustomerDto customerDto)
        {
            if (customerDto == null)
                return BadRequest(new[] { "Customer data is required." });

            try
            {
                bool isNew = customerDto.Id == 0;
                Core.Domain.Sales.Customer customer;

                if (isNew)
                {
                    customer = new Core.Domain.Sales.Customer
                    {
                        Party = new Party { PartyType = PartyTypes.Customer },
                        PrimaryContact = new Contact
                        {
                            ContactType = ContactTypes.Customer,
                            Party = new Party { PartyType = PartyTypes.Contact }
                        }
                    };
                }
                else
                {
                    customer = _salesService.GetCustomerById(customerDto.Id);
                    if (customer == null)
                        return NotFound(new[] { "Customer not found." });
                }

                customer.Party ??= new Party();
                customer.PrimaryContact ??= new Contact { Party = new Party() };
                customer.PrimaryContact.Party ??= new Party();

                customer.No = customerDto.No;
                customer.Party.Name = customerDto.Name;
                customer.Party.Phone = customerDto.Phone;
                customer.Party.Email = customerDto.Email;
                customer.Party.Fax = customerDto.Fax;
                customer.Party.Website = customerDto.Website;

                if (customerDto.PrimaryContact != null)
                {
                    customer.PrimaryContact.FirstName = customerDto.PrimaryContact.FirstName;
                    customer.PrimaryContact.LastName = customerDto.PrimaryContact.LastName;
                    customer.PrimaryContact.Party ??= new Party();
                    customer.PrimaryContact.Party.Name = customerDto.PrimaryContact.Party?.Name ?? customer.PrimaryContact.Party.Name;
                    customer.PrimaryContact.Party.Phone = customerDto.PrimaryContact.Party?.Phone ?? customer.PrimaryContact.Party.Phone;
                    customer.PrimaryContact.Party.Email = customerDto.PrimaryContact.Party?.Email ?? customer.PrimaryContact.Party.Email;
                    customer.PrimaryContact.Party.Fax = customerDto.PrimaryContact.Party?.Fax ?? customer.PrimaryContact.Party.Fax;
                    customer.PrimaryContact.Party.Website = customerDto.PrimaryContact.Party?.Website ?? customer.PrimaryContact.Party.Website;
                }

                customer.AccountsReceivableAccountId = customerDto.AccountsReceivableId;
                customer.SalesAccountId = customerDto.SalesAccountId;
                customer.CustomerAdvancesAccountId = customerDto.PrepaymentAccountId;
                customer.SalesDiscountAccountId = customerDto.SalesDiscountAccountId;
                customer.PaymentTermId = customerDto.PaymentTermId;
                customer.TaxGroupId = customerDto.TaxGroupId;
                customer.ModifiedBy = GetUserNameFromRequestHeader();

                if (isNew)
                    _salesService.AddCustomer(customer);
                else
                    _salesService.UpdateCustomer(customer);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveCustomer failed.");
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet]
        [Route("Customer")]
        public IActionResult Customer(int id)
        {
            try
            {
                var customer = _salesService.GetCustomerById(id);
                if (customer == null)
                    return NotFound(new[] { "Customer not found." });

                customer.Party ??= new Party();
                customer.PrimaryContact ??= new Contact { Party = new Party() };
                customer.PrimaryContact.Party ??= new Party();

                var customerDto = new Customer
                {
                    Id = customer.Id,
                    No = customer.No,
                    AccountsReceivableId = customer.AccountsReceivableAccountId.GetValueOrDefault(),
                    SalesAccountId = customer.SalesAccountId.GetValueOrDefault(),
                    PrepaymentAccountId = customer.CustomerAdvancesAccountId.GetValueOrDefault(),
                    SalesDiscountAccountId = customer.SalesDiscountAccountId.GetValueOrDefault(),
                    PaymentTermId = customer.PaymentTermId.GetValueOrDefault(),
                    TaxGroupId = customer.TaxGroupId.GetValueOrDefault(),
                    Name = customer.Party.Name,
                    Email = customer.Party.Email,
                    Website = customer.Party.Website,
                    Phone = customer.Party.Phone,
                    Fax = customer.Party.Fax
                };

                if (customer.PrimaryContact != null)
                {
                    customerDto.PrimaryContact = new Contact
                    {
                        FirstName = customer.PrimaryContact.FirstName,
                        LastName = customer.PrimaryContact.LastName,
                        Party = new Party
                        {
                            Name = customer.PrimaryContact.Party?.Name,
                            Email = customer.PrimaryContact.Party?.Email,
                            Phone = customer.PrimaryContact.Party?.Phone,
                            Fax = customer.PrimaryContact.Party?.Fax,
                            Website = customer.PrimaryContact.Party?.Website
                        }
                    };
                }

                return Ok(customerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Customer GET failed for id {Id}.", id);
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet]
        [Route("Customers")]
        public IActionResult Customers()
        {
            try
            {
                var customers = _salesService.GetCustomers()?
                    .Where(p => p.Party != null)
                    ?? Enumerable.Empty<Core.Domain.Sales.Customer>();

                var customersDto = customers.Select(customer => new Customer
                {
                    Id = customer.Id,
                    No = customer.No,
                    Name = customer.Party?.Name,
                    Email = customer.Party?.Email,
                    Website = customer.Party?.Website,
                    Phone = customer.Party?.Phone,
                    Fax = customer.Party?.Fax,
                    Balance = customer.Balance,
                    PrepaymentAccountId = customer.CustomerAdvancesAccountId,
                    Contact = customer.PrimaryContact != null
                        ? string.Join(" ", new[] { customer.PrimaryContact.FirstName, customer.PrimaryContact.LastName }
                            .Where(x => !string.IsNullOrWhiteSpace(x)))
                        : string.Empty,
                    TaxGroup = customer.TaxGroup?.Description ?? string.Empty
                }).ToList();

                return Ok(customersDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Customers GET failed.");
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        // =========================================
        // SALES ORDERS
        // =========================================

        [HttpGet]
        [Route("SalesOrders")]
        public IActionResult SalesOrders()
        {
            try
            {
                var salesOrders = _salesService.GetSalesOrders()
                                  ?? Enumerable.Empty<SalesOrderHeader>();

                var salesOrdersDto = new List<SalesOrder>();

                foreach (var salesOrder in salesOrders)
                {
                    var salesOrderDto = new SalesOrder
                    {
                        Id = salesOrder.Id,
                        PaymentTermId = salesOrder.PaymentTermId,
                        CustomerId = salesOrder.CustomerId.GetValueOrDefault(),
                        CustomerNo = salesOrder.Customer?.No ?? string.Empty,
                        CustomerName = salesOrder.Customer?.Party?.Name ?? string.Empty,
                        OrderDate = salesOrder.Date,
                        ReferenceNo = salesOrder.ReferenceNo,
                        StatusId = (int)salesOrder.Status.GetValueOrDefault(),
                        No = salesOrder.No,
                        SalesOrderLines = new List<SalesOrderLine>()
                    };

                    foreach (var line in salesOrder.SalesOrderLines ?? Enumerable.Empty<SalesOrderLine>())
                    {
                        salesOrderDto.SalesOrderLines.Add(new SalesOrderLine
                        {
                            ItemId = line.ItemId,
                            MeasurementId = line.MeasurementId,
                            Quantity = line.Quantity,
                            Amount = line.Amount,
                            Discount = line.Discount,
                            RemainingQtyToInvoice = line.GetRemainingQtyToInvoice()
                        });
                    }

                    salesOrdersDto.Add(salesOrderDto);
                }

                return Ok(salesOrdersDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SalesOrders GET failed.");
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet]
        [Route("SalesOrder")]
        public IActionResult SalesOrder(int id)
        {
            try
            {
                var salesOrder = _salesService.GetSalesOrderById(id);
                if (salesOrder == null)
                    return NotFound(new[] { "Sales order not found." });

                var salesOrderDto = new SalesOrder
                {
                    Id = salesOrder.Id,
                    CustomerId = salesOrder.CustomerId.GetValueOrDefault(),
                    CustomerNo = salesOrder.Customer?.No ?? string.Empty,
                    CustomerName = salesOrder.Customer?.Party?.Name ?? string.Empty,
                    OrderDate = salesOrder.Date,
                    PaymentTermId = salesOrder.PaymentTermId,
                    ReferenceNo = salesOrder.ReferenceNo,
                    StatusId = (int)salesOrder.Status.GetValueOrDefault(),
                    SalesOrderLines = new List<SalesOrderLine>()
                };

                foreach (var line in salesOrder.SalesOrderLines ?? Enumerable.Empty<SalesOrderLine>())
                {
                    salesOrderDto.SalesOrderLines.Add(new SalesOrderLine
                    {
                        Id = line.Id,
                        Amount = line.Amount,
                        Discount = line.Discount,
                        Quantity = line.Quantity,
                        ItemId = line.ItemId,
                        ItemDescription = line.Item?.Description,
                        MeasurementId = line.MeasurementId,
                        MeasurementDescription = line.Measurement?.Description,
                        RemainingQtyToInvoice = line.GetRemainingQtyToInvoice()
                    });
                }

                return Ok(salesOrderDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SalesOrder GET failed for id {Id}.", id);
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        [Route("addsalesorder")]
        public IActionResult AddSalesOrder([FromBody] SalesOrder salesorderDto)
        {
            try
            {
                if (salesorderDto == null)
                    return BadRequest(new[] { "Sales order data is required." });

                var salesOrder = new SalesOrderHeader
                {
                    CustomerId = salesorderDto.CustomerId,
                    Date = salesorderDto.OrderDate,
                    SalesOrderLines = new List<SalesOrderLine>()
                };

                foreach (var line in salesorderDto.SalesOrderLines ?? Enumerable.Empty<SalesOrderLine>())
                {
                    salesOrder.SalesOrderLines.Add(new SalesOrderLine
                    {
                        Amount = line.Amount.GetValueOrDefault(),
                        Discount = line.Discount.GetValueOrDefault(),
                        Quantity = line.Quantity.GetValueOrDefault(),
                        ItemId = line.ItemId.GetValueOrDefault(),
                        MeasurementId = line.MeasurementId.GetValueOrDefault()
                    });
                }

                _salesService.AddSalesOrder(salesOrder, true);
                salesorderDto.Id = salesOrder.Id;

                return Ok(salesorderDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddSalesOrder failed.");
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        [Route("SaveSalesOrder")]
        public IActionResult SaveSalesOrder([FromBody] SalesOrder salesOrderDto)
        {
            try
            {
                if (salesOrderDto == null)
                    return BadRequest(new[] { "Sales order data is required." });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToArray();
                    return BadRequest(errors);
                }

                bool isNew = salesOrderDto.Id == 0;
                SalesOrderHeader salesOrder;

                if (isNew)
                {
                    salesOrder = new SalesOrderHeader
                    {
                        Status = SalesOrderStatus.Open,
                        SalesOrderLines = new List<SalesOrderLine>()
                    };

                    if (salesOrderDto.QuotationId != null)
                    {
                        var quotation = _salesService.GetSalesQuotationById(salesOrderDto.QuotationId.Value);
                        if (quotation != null)
                        {
                            quotation.Status = SalesQuoteStatus.ClosedOrderCreated;
                            _salesService.UpdateSalesQuote(quotation);
                        }
                    }
                }
                else
                {
                    salesOrder = _salesService.GetSalesOrderById(salesOrderDto.Id);
                    if (salesOrder == null)
                        return NotFound(new[] { "Sales order not found." });

                    salesOrder.SalesOrderLines ??= new List<SalesOrderLine>();
                }

                salesOrder.CustomerId = salesOrderDto.CustomerId;
                salesOrder.Date = salesOrderDto.OrderDate;
                salesOrder.PaymentTermId = salesOrderDto.PaymentTermId;
                salesOrder.ReferenceNo = salesOrderDto.ReferenceNo;

                var incomingLines = salesOrderDto.SalesOrderLines ?? new List<SalesOrderLine>();

                foreach (var line in incomingLines)
                {
                    if (!isNew && line.Id != 0)
                    {
                        var existingLine = salesOrder.SalesOrderLines.FirstOrDefault(l => l.Id == line.Id);
                        if (existingLine != null)
                        {
                            existingLine.Amount = line.Amount.GetValueOrDefault();
                            existingLine.Discount = line.Discount.GetValueOrDefault();
                            existingLine.Quantity = line.Quantity.GetValueOrDefault();
                            existingLine.ItemId = line.ItemId.GetValueOrDefault();
                            existingLine.MeasurementId = line.MeasurementId.GetValueOrDefault();
                            continue;
                        }
                    }

                    salesOrder.SalesOrderLines.Add(new SalesOrderLine
                    {
                        Amount = line.Amount.GetValueOrDefault(),
                        Discount = line.Discount.GetValueOrDefault(),
                        Quantity = line.Quantity.GetValueOrDefault(),
                        ItemId = line.ItemId.GetValueOrDefault(),
                        MeasurementId = line.MeasurementId.GetValueOrDefault()
                    });
                }

                if (isNew)
                {
                    _salesService.AddSalesOrder(salesOrder, true);
                }
                else
                {
                    var deleted = salesOrder.SalesOrderLines
                        .Where(line => line.Id != 0 && !incomingLines.Any(x => x.Id == line.Id))
                        .ToList();

                    foreach (var line in deleted)
                    {
                        if (line.SalesInvoiceLines != null && line.SalesInvoiceLines.Any())
                            throw new Exception("The line cannot be deleted. An invoice line is created from the item.");

                        salesOrder.SalesOrderLines.Remove(line);
                    }

                    _salesService.UpdateSalesOrder(salesOrder);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveSalesOrder failed.");
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        // =========================================
        // SALES INVOICE
        // =========================================

        [HttpGet]
        [Route("SalesInvoice")]
        public IActionResult SalesInvoice(int id)
        {
            try
            {
                var salesInvoice = _salesService.GetSalesInvoiceById(id);
                if (salesInvoice == null)
                    return NotFound(new[] { "Sales invoice not found." });

                var salesInvoiceDto = new SalesInvoice
                {
                    Id = salesInvoice.Id,
                    CustomerId = salesInvoice.CustomerId,
                    CustomerName = salesInvoice.Customer?.Party?.Name ?? string.Empty,
                    InvoiceDate = salesInvoice.Date,
                    SalesInvoiceLines = new List<SalesInvoiceLine>(),
                    PaymentTermId = salesInvoice.PaymentTermId,
                    ReferenceNo = salesInvoice.ReferenceNo,
                    Posted = salesInvoice.GeneralLedgerHeaderId != null
                };

                foreach (var line in salesInvoice.SalesInvoiceLines ?? Enumerable.Empty<SalesInvoiceLine>())
                {
                    salesInvoiceDto.SalesInvoiceLines.Add(new SalesInvoiceLine
                    {
                        Id = line.Id,
                        Amount = line.Amount,
                        Discount = line.Discount,
                        Quantity = line.Quantity,
                        ItemId = line.ItemId,
                        MeasurementId = line.MeasurementId,
                        ItemDescription = line.Item?.Description,
                        MeasurementDescription = line.Measurement?.Description
                    });
                }

                if (!salesInvoiceDto.Posted && salesInvoiceDto.SalesInvoiceLines.Count >= 1)
                    salesInvoiceDto.ReadyForPosting = true;

                return Ok(salesInvoiceDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SalesInvoice GET failed for id {Id}.", id);
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet]
        [Route("SalesInvoices")]
        public IActionResult SalesInvoices()
        {
            try
            {
                var salesInvoices = _salesService.GetSalesInvoices()
                                    ?? Enumerable.Empty<SalesInvoiceHeader>();

                var salesInvoicesDto = new List<SalesInvoice>();

                foreach (var salesInvoice in salesInvoices)
                {
                    var salesInvoiceDto = new SalesInvoice
                    {
                        Id = salesInvoice.Id,
                        No = salesInvoice.No,
                        CustomerId = salesInvoice.CustomerId,
                        CustomerName = salesInvoice.Customer?.Party?.Name ?? string.Empty,
                        InvoiceDate = salesInvoice.Date,
                        ReferenceNo = salesInvoice.ReferenceNo,
                        Posted = salesInvoice.GeneralLedgerHeaderId != null,
                        SalesInvoiceLines = new List<SalesInvoiceLine>()
                    };

                    foreach (var line in salesInvoice.SalesInvoiceLines ?? Enumerable.Empty<SalesInvoiceLine>())
                    {
                        salesInvoiceDto.SalesInvoiceLines.Add(new SalesInvoiceLine
                        {
                            ItemId = line.ItemId,
                            MeasurementId = line.MeasurementId,
                            Quantity = line.Quantity,
                            Amount = line.Amount,
                            Discount = line.Discount
                        });
                    }

                    salesInvoicesDto.Add(salesInvoiceDto);
                }

                return Ok(salesInvoicesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SalesInvoices GET failed.");
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        [Route("PostSalesInvoice")]
        public IActionResult PostSalesInvoice([FromBody] SalesInvoice salesInvoiceDto)
        {
            try
            {
                if (salesInvoiceDto == null || salesInvoiceDto.Id <= 0)
                    return BadRequest(new[] { "A valid sales invoice is required." });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToArray();
                    return BadRequest(errors);
                }

                _logger.LogInformation("PostSalesInvoice for id {Id}", salesInvoiceDto.Id);
                _salesService.PostSalesInvoice(salesInvoiceDto.Id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostSalesInvoice failed.");
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        [Route("SaveSalesInvoice")]
        public IActionResult SaveSalesInvoice([FromBody] SalesInvoice salesInvoiceDto)
        {
            try
            {
                if (salesInvoiceDto == null)
                    return BadRequest(new[] { "Sales invoice data is required." });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToArray();
                    return BadRequest(errors);
                }

                bool isNew = salesInvoiceDto.Id == 0;
                SalesInvoiceHeader salesInvoice;
                SalesOrderHeader salesOrder = null;

                if (isNew)
                {
                    if (salesInvoiceDto.FromSalesOrderId.HasValue)
                    {
                        salesOrder = _salesService.GetSalesOrderById(salesInvoiceDto.FromSalesOrderId.Value);
                        if (salesOrder == null)
                            return NotFound(new[] { "Sales order not found." });
                    }
                    else
                    {
                        salesOrder = new SalesOrderHeader
                        {
                            Date = salesInvoiceDto.InvoiceDate,
                            PaymentTermId = salesInvoiceDto.PaymentTermId,
                            CustomerId = salesInvoiceDto.CustomerId,
                            ReferenceNo = salesInvoiceDto.ReferenceNo,
                            Status = SalesOrderStatus.FullyInvoiced,
                            SalesOrderLines = new List<SalesOrderLine>()
                        };
                    }

                    salesInvoice = new SalesInvoiceHeader
                    {
                        CustomerId = salesInvoiceDto.CustomerId,
                        Date = salesInvoiceDto.InvoiceDate,
                        PaymentTermId = salesInvoiceDto.PaymentTermId,
                        ReferenceNo = salesInvoiceDto.ReferenceNo,
                        SalesInvoiceLines = new List<SalesInvoiceLine>()
                    };

                    foreach (var line in salesInvoiceDto.SalesInvoiceLines ?? Enumerable.Empty<SalesInvoiceLine>())
                    {
                        var salesInvoiceLine = new SalesInvoiceLine
                        {
                            Amount = line.Amount.GetValueOrDefault(),
                            Discount = line.Discount.GetValueOrDefault(),
                            Quantity = line.Quantity.GetValueOrDefault(),
                            ItemId = line.ItemId.GetValueOrDefault(),
                            MeasurementId = line.MeasurementId.GetValueOrDefault()
                        };

                        if (line.Id != 0 && salesOrder != null)
                        {
                            salesInvoiceLine.SalesOrderLineId = line.Id;
                        }
                        else if (salesOrder != null)
                        {
                            var salesOrderLine = new SalesOrderLine
                            {
                                Amount = line.Amount.GetValueOrDefault(),
                                Discount = line.Discount.GetValueOrDefault(),
                                Quantity = line.Quantity.GetValueOrDefault(),
                                ItemId = line.ItemId.GetValueOrDefault(),
                                MeasurementId = line.MeasurementId.GetValueOrDefault()
                            };

                            salesOrder.SalesOrderLines ??= new List<SalesOrderLine>();
                            salesOrder.SalesOrderLines.Add(salesOrderLine);
                            salesInvoiceLine.SalesOrderLine = salesOrderLine;
                        }

                        salesInvoice.SalesInvoiceLines.Add(salesInvoiceLine);
                    }
                }
                else
                {
                    salesInvoice = _salesService.GetSalesInvoiceById(salesInvoiceDto.Id);
                    if (salesInvoice == null)
                        return NotFound(new[] { "Sales invoice not found." });

                    if (salesInvoice.GeneralLedgerHeaderId.HasValue)
                        throw new Exception("Invoice is already posted. Update is not allowed.");

                    salesInvoice.Date = salesInvoiceDto.InvoiceDate;
                    salesInvoice.PaymentTermId = salesInvoiceDto.PaymentTermId;
                    salesInvoice.ReferenceNo = salesInvoiceDto.ReferenceNo;
                    salesInvoice.CustomerId = salesInvoiceDto.CustomerId;
                    salesInvoice.SalesInvoiceLines ??= new List<SalesInvoiceLine>();

                    var incomingLines = salesInvoiceDto.SalesInvoiceLines ?? new List<SalesInvoiceLine>();

                    foreach (var line in incomingLines)
                    {
                        var existingLine = salesInvoice.SalesInvoiceLines
                            .FirstOrDefault(l => l.Id == line.Id && line.Id != 0);

                        if (existingLine != null)
                        {
                            existingLine.Amount = line.Amount.GetValueOrDefault();
                            existingLine.Discount = line.Discount.GetValueOrDefault();
                            existingLine.Quantity = line.Quantity.GetValueOrDefault();
                            existingLine.ItemId = line.ItemId.GetValueOrDefault();
                            existingLine.MeasurementId = line.MeasurementId.GetValueOrDefault();
                        }
                        else
                        {
                            salesInvoice.SalesInvoiceLines.Add(new SalesInvoiceLine
                            {
                                Amount = line.Amount.GetValueOrDefault(),
                                Discount = line.Discount.GetValueOrDefault(),
                                Quantity = line.Quantity.GetValueOrDefault(),
                                ItemId = line.ItemId.GetValueOrDefault(),
                                MeasurementId = line.MeasurementId.GetValueOrDefault()
                            });
                        }
                    }

                    var deleted = salesInvoice.SalesInvoiceLines
                        .Where(line => line.Id != 0 && !incomingLines.Any(x => x.Id == line.Id))
                        .ToList();

                    foreach (var line in deleted)
                        salesInvoice.SalesInvoiceLines.Remove(line);
                }

                _logger.LogInformation("SaveSalesInvoice API CustomerId={CustomerId}", salesInvoice.CustomerId);
                _salesService.SaveSalesInvoice(salesInvoice, salesOrder);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveSalesInvoice failed.");
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        // =========================================
        // QUOTATIONS
        // =========================================

        [HttpGet]
        [Route("Quotations")]
        public IActionResult Quotations()
        {
            try
            {
                var quotes = _salesService.GetSalesQuotes()
                             ?? Enumerable.Empty<SalesQuoteHeader>();

                var quoteDtos = new List<SalesQuotation>();

                foreach (var quote in quotes)
                {
                    var quoteDto = new SalesQuotation
                    {
                        Id = quote.Id,
                        No = quote.No,
                        CustomerId = quote.CustomerId,
                        CustomerName = quote.Customer?.Party?.Name ?? string.Empty,
                        PaymentTermId = quote.PaymentTermId,
                        QuotationDate = quote.Date,
                        ReferenceNo = quote.ReferenceNo,
                        SalesQuoteStatus = quote.Status.ToString(),
                        StatusId = (int)quote.Status,
                        SalesQuotationLines = new List<SalesQuotationLine>()
                    };

                    foreach (var line in quote.SalesQuoteLines ?? Enumerable.Empty<SalesQuoteLine>())
                    {
                        quoteDto.SalesQuotationLines.Add(new SalesQuotationLine
                        {
                            ItemId = line.ItemId,
                            MeasurementId = line.MeasurementId,
                            Quantity = line.Quantity,
                            Amount = line.Amount,
                            Discount = line.Discount
                        });
                    }

                    quoteDtos.Add(quoteDto);
                }

                return Ok(quoteDtos.OrderByDescending(q => q.Id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quotations GET failed.");
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet]
        [Route("Quotation")]
        public IActionResult Quotation(int id)
        {
            try
            {
                var quote = _salesService.GetSalesQuotationById(id);
                if (quote == null)
                    return NotFound(new[] { "Quotation not found." });

                var quoteDto = new SalesQuotation
                {
                    Id = quote.Id,
                    CustomerId = quote.CustomerId,
                    CustomerName = quote.Customer?.Party?.Name ?? string.Empty,
                    QuotationDate = quote.Date,
                    PaymentTermId = quote.PaymentTermId,
                    ReferenceNo = quote.ReferenceNo,
                    StatusId = (int)quote.Status,
                    SalesQuotationLines = new List<SalesQuotationLine>()
                };

                foreach (var line in quote.SalesQuoteLines ?? Enumerable.Empty<SalesQuoteLine>())
                {
                    quoteDto.SalesQuotationLines.Add(new SalesQuotationLine
                    {
                        Id = line.Id,
                        ItemId = line.ItemId,
                        MeasurementId = line.MeasurementId,
                        Quantity = line.Quantity,
                        Amount = line.Amount,
                        Discount = line.Discount,
                        ItemDescription = line.Item?.Description,
                        MeasurementDescription = line.Measurement?.Description
                    });
                }

                return Ok(quoteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quotation GET failed for id {Id}.", id);
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        [Route("SaveQuotation")]
        public IActionResult SaveQuotation([FromBody] SalesQuotation quotationDto)
        {
            try
            {
                if (quotationDto == null)
                    return BadRequest(new[] { "Quotation data is required." });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToArray();
                    return BadRequest(errors);
                }

                bool isNew = quotationDto.Id == 0;
                SalesQuoteHeader salesQuote;

                if (isNew)
                {
                    salesQuote = new SalesQuoteHeader
                    {
                        Status = SalesQuoteStatus.Draft,
                        SalesQuoteLines = new List<SalesQuoteLine>()
                    };
                }
                else
                {
                    salesQuote = _salesService.GetSalesQuotationById(quotationDto.Id);
                    if (salesQuote == null)
                        return NotFound(new[] { "Quotation not found." });

                    salesQuote.Status = (SalesQuoteStatus)quotationDto.StatusId;
                    salesQuote.SalesQuoteLines ??= new List<SalesQuoteLine>();
                }

                salesQuote.CustomerId = quotationDto.CustomerId.GetValueOrDefault();
                salesQuote.Date = quotationDto.QuotationDate;
                salesQuote.ReferenceNo = quotationDto.ReferenceNo;
                salesQuote.PaymentTermId = quotationDto.PaymentTermId;

                var incomingLines = quotationDto.SalesQuotationLines ?? new List<SalesQuotationLine>();

                foreach (var line in incomingLines)
                {
                    if (!isNew && line.Id != 0)
                    {
                        var existingLine = salesQuote.SalesQuoteLines.FirstOrDefault(l => l.Id == line.Id);
                        if (existingLine != null)
                        {
                            existingLine.Amount = line.Amount ?? 0;
                            existingLine.Discount = line.Discount ?? 0;
                            existingLine.Quantity = line.Quantity ?? 0;
                            existingLine.ItemId = line.ItemId.GetValueOrDefault();
                            existingLine.MeasurementId = line.MeasurementId.GetValueOrDefault();
                            continue;
                        }
                    }

                    salesQuote.SalesQuoteLines.Add(new SalesQuoteLine
                    {
                        Amount = line.Amount ?? 0,
                        Discount = line.Discount ?? 0,
                        Quantity = line.Quantity ?? 0,
                        ItemId = line.ItemId.GetValueOrDefault(),
                        MeasurementId = line.MeasurementId.GetValueOrDefault()
                    });
                }

                if (isNew)
                {
                    _salesService.AddSalesQuote(salesQuote);
                }
                else
                {
                    var deleted = salesQuote.SalesQuoteLines
                        .Where(line => line.Id != 0 && !incomingLines.Any(x => x.Id == line.Id))
                        .ToList();

                    foreach (var line in deleted)
                        salesQuote.SalesQuoteLines.Remove(line);

                    _salesService.UpdateSalesQuote(salesQuote);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveQuotation failed.");
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        [Route("BookQuotation")]
        public IActionResult BookQuotation(int id)
        {
            try
            {
                _salesService.BookQuotation(id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookQuotation failed for id {Id}.", id);
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        // =========================================
        // RECEIPTS & ALLOCATIONS
        // =========================================

        [HttpGet]
        [Route("SalesReceipts")]
        public IActionResult SalesReceipts()
        {
            try
            {
                var salesReceipts = _salesService.GetSalesReceipts()
                                    ?? Enumerable.Empty<SalesReceiptHeader>();

                var salesReceiptsDto = salesReceipts.Select(salesReceipt => new SalesReceipt
                {
                    Id = salesReceipt.Id,
                    ReceiptNo = salesReceipt.No,
                    CustomerId = salesReceipt.CustomerId,
                    CustomerName = salesReceipt.Customer?.Party?.Name ?? string.Empty,
                    ReceiptDate = salesReceipt.Date,
                    Amount = salesReceipt.Amount,
                    RemainingAmountToAllocate = salesReceipt.AvailableAmountToAllocate
                }).ToList();

                return Ok(salesReceiptsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SalesReceipts GET failed.");
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet]
        [Route("SalesReceipt")]
        public IActionResult SalesReceipt(int id)
        {
            try
            {
                var salesReceipt = _salesService.GetSalesReceiptById(id);
                if (salesReceipt == null)
                    return NotFound(new[] { "Sales receipt not found." });

                var salesReceiptDto = new SalesReceipt
                {
                    Id = salesReceipt.Id,
                    ReceiptNo = salesReceipt.No,
                    CustomerId = salesReceipt.CustomerId,
                    CustomerName = salesReceipt.Customer?.Party?.Name ?? string.Empty,
                    ReceiptDate = salesReceipt.Date,
                    Amount = salesReceipt.Amount,
                    RemainingAmountToAllocate = salesReceipt.AvailableAmountToAllocate
                };

                return Ok(salesReceiptDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SalesReceipt GET failed for id {Id}.", id);
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet]
        [Route("CustomerInvoices")]
        public IActionResult CustomerInvoices(int id)
        {
            try
            {
                var invoices = _salesService.GetCustomerInvoices(id)
                               ?? Enumerable.Empty<SalesInvoiceHeader>();

                var invoicesDto = new List<SalesInvoice>();

                foreach (var invoice in invoices)
                {
                    var invoiceDto = new SalesInvoice
                    {
                        Id = invoice.Id,
                        InvoiceDate = invoice.Date,
                        CustomerId = invoice.CustomerId,
                        TotalAllocatedAmount = invoice.CustomerAllocations?.Sum(i => i.Amount) ?? 0,
                        Posted = invoice.GeneralLedgerHeaderId.HasValue,
                        SalesInvoiceLines = new List<SalesInvoiceLine>()
                    };

                    foreach (var line in invoice.SalesInvoiceLines ?? Enumerable.Empty<SalesInvoiceLine>())
                    {
                        invoiceDto.SalesInvoiceLines.Add(new SalesInvoiceLine
                        {
                            Id = line.Id,
                            Amount = line.Amount,
                            Discount = line.Discount,
                            Quantity = line.Quantity,
                            ItemId = line.ItemId,
                            MeasurementId = line.MeasurementId
                        });
                    }

                    invoicesDto.Add(invoiceDto);
                }

                return Ok(invoicesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CustomerInvoices GET failed for customer {Id}.", id);
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        [Route("SaveReceipt")]
        public IActionResult SaveReceipt([FromBody] SaveReceiptRequest receiptDto)
        {
            try
            {
                if (receiptDto == null)
                    return BadRequest(new[] { "Receipt data is required." });

                int? accountToDebitId = receiptDto.AccountToDebitId;
                int? accountToCreditId = receiptDto.AccountToCreditId;
                int? customerId = receiptDto.CustomerId;
                decimal? amount = receiptDto.Amount;
                DateTime? receiptDate = receiptDto.ReceiptDate;

                if (!accountToDebitId.HasValue || !accountToCreditId.HasValue ||
                    !customerId.HasValue || !amount.HasValue || amount.Value <= 0 ||
                    !receiptDate.HasValue)
                {
                    return BadRequest(new[] { "Receipt payload is incomplete." });
                }

                var bank = _financialService.GetCashAndBanks()?
                    .FirstOrDefault(b => b.Id == accountToDebitId.Value);

                if (bank == null)
                    return BadRequest(new[] { "Invalid debit account." });

                var customer = _salesService.GetCustomerById(customerId.Value);
                if (customer == null)
                    return BadRequest(new[] { "Invalid customer." });

                if (customer.CustomerAdvancesAccountId != accountToCreditId.Value)
                    return BadRequest(new[] { "Invalid credit account for this customer." });

                var salesReceipt = new SalesReceiptHeader
                {
                    Date = receiptDate.Value,
                    CustomerId = customerId.Value,
                    AccountToDebitId = bank.AccountId,
                    Amount = amount.Value,
                    SalesReceiptLines = new List<SalesReceiptLine>()
                };

                salesReceipt.SalesReceiptLines.Add(new SalesReceiptLine
                {
                    AccountToCreditId = accountToCreditId.Value,
                    AmountPaid = amount.Value,
                    Amount = amount.Value,
                    Quantity = 1
                });

                _salesService.AddSalesReceiptNoInvoice(salesReceipt);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveReceipt failed.");
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        [Route("SaveAllocation")]
        public IActionResult SaveAllocation([FromBody] SaveAllocationRequest allocationDto)
        {
            try
            {
                if (allocationDto == null)
                    return BadRequest(new[] { "Allocation data is required." });

                int? customerId = allocationDto.CustomerId;
                int? receiptId = allocationDto.ReceiptId;
                DateTime? date = allocationDto.Date;

                if (!customerId.HasValue || !receiptId.HasValue || !date.HasValue)
                    return BadRequest(new[] { "Allocation payload is incomplete." });

                var allocationLines = allocationDto.AllocationLines;
                if (allocationLines == null || allocationLines.Count == 0)
                    return BadRequest(new[] { "Allocation lines are required." });

                var receipt = _salesService.GetSalesReceiptById(receiptId.Value);
                if (receipt == null || receipt.CustomerId != customerId.Value)
                    return BadRequest(new[] { "Invalid receipt for this customer." });

                decimal totalToAllocate = 0;

                foreach (var line in allocationLines)
                {
                    decimal? amount = line.AmountToAllocate;
                    int? invoiceId = line.InvoiceId;

                    if (!amount.HasValue || amount.Value <= 0 || !invoiceId.HasValue)
                        return BadRequest(new[] { "Each allocation line must contain a positive amount and invoice." });
                }

                foreach (var invoiceLines in allocationLines.GroupBy(line => line.InvoiceId!.Value))
                {
                    var invoice = _salesService.GetSalesInvoiceById(invoiceLines.Key);
                    if (invoice == null || invoice.CustomerId != customerId.Value ||
                        !invoice.GeneralLedgerHeaderId.HasValue)
                        return BadRequest(new[] { "Invalid invoice for this customer." });

                    var invoiceAmount = invoice.SalesInvoiceLines?.Sum(invoiceLine =>
                        (invoiceLine.Amount ?? 0) * (invoiceLine.Quantity ?? 0)) ?? 0;
                    var allocatedAmount = invoice.CustomerAllocations?.Sum(allocation => allocation.Amount) ?? 0;
                    var requestedAmount = invoiceLines.Sum(line => line.AmountToAllocate!.Value);
                    if (requestedAmount > invoiceAmount - allocatedAmount)
                        return BadRequest(new[] { "Allocation amount exceeds the invoice balance." });

                    totalToAllocate += requestedAmount;
                }

                if (totalToAllocate > receipt.AvailableAmountToAllocate)
                    return BadRequest(new[] { "Allocation amount exceeds the receipt balance." });

                foreach (var line in allocationLines)
                {
                    decimal amount = line.AmountToAllocate!.Value;
                    int invoiceId = line.InvoiceId!.Value;

                    var allocation = new CustomerAllocation
                    {
                        CustomerId = customerId.Value,
                        Date = date.Value,
                        SalesInvoiceHeaderId = invoiceId,
                        SalesReceiptHeaderId = receiptId.Value,
                        Amount = amount
                    };

                    _salesService.SaveCustomerAllocation(allocation);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveAllocation failed.");
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        // =========================================
        // REPORTS / PRINTING
        // =========================================

        [HttpGet]
        [Route("GetMonthlySales")]
        public IActionResult GetMonthlySales()
        {
            try
            {
                var salesInvoices = _salesService.GetSalesInvoices()?
                    .Where(a => a.GeneralLedgerHeaderId != null)
                    ?? Enumerable.Empty<SalesInvoiceHeader>();

                var monthlyTotals = new Dictionary<int, decimal>();

                foreach (var item in salesInvoices)
                {
                    if (item.Date.Year != DateTime.Now.Year)
                        continue;

                    int month = item.Date.Month;
                    decimal lineTotal = 0;

                    foreach (var line in item.SalesInvoiceLines ?? Enumerable.Empty<SalesInvoiceLine>())
                    {
                        var gross = (line.Amount ?? 0) * (line.Quantity ?? 0);
                        var discount = gross * ((line.Discount ?? 0) / 100m);
                        lineTotal += gross - discount;
                    }
                    

                    if (monthlyTotals.ContainsKey(month))
                        monthlyTotals[month] += lineTotal;
                    else
                        monthlyTotals[month] = lineTotal;
                }

                var finalMonthlySalesDto = new List<MonthlySales>();

                for (int i = 1; i <= DateTime.Now.Month; i++)
                {
                    var monthName = new DateTime(DateTime.Now.Year, i, 1).ToString("MMMM");
                    monthlyTotals.TryGetValue(i, out var amount);

                    finalMonthlySalesDto.Add(new MonthlySales
                    {
                        Month = monthName,
                        Amount = amount
                    });
                }

                return Ok(finalMonthlySalesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetMonthlySales failed.");
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet]
        [Route("SalesInvoiceForPrinting")]
        public IActionResult SalesInvoiceForPrinting(int id)
        {
            try
            {
                var salesInvoice = _salesService.GetSalesInvoiceById(id);
                if (salesInvoice == null)
                    return NotFound(new[] { "Sales invoice not found." });

                var company = _adminService.GetDefaultCompany();

                var salesInvoiceDto = new SalesInvoice
                {
                    Id = salesInvoice.Id,
                    CustomerId = salesInvoice.CustomerId,
                    CustomerName = salesInvoice.Customer?.Party?.Name ?? string.Empty,
                    CustomerEmail = salesInvoice.Customer?.Party?.Email,
                    InvoiceDate = salesInvoice.Date,
                    SalesInvoiceLines = new List<SalesInvoiceLine>(),
                    PaymentTermId = salesInvoice.PaymentTermId,
                    ReferenceNo = salesInvoice.ReferenceNo,
                    Posted = salesInvoice.GeneralLedgerHeaderId != null,
                    CompanyName = company?.Name
                };

                decimal totalTax = 0;
                var lines = salesInvoice.SalesInvoiceLines ?? Enumerable.Empty<SalesInvoiceLine>();

                var subtotal = lines.Sum(line =>
                {
                    var lineTotal = (line.Amount ?? 0) * (line.Quantity ?? 0);
                    var discount = lineTotal * ((line.Discount ?? 0) / 100m);
                    return lineTotal - discount;
                });

                foreach (var line in lines)
                {
                    var item = _inventoryService.GetItemById(line.ItemId);
                    var measurement = _inventoryService.GetMeasurementById(line.MeasurementId);

                    var lineDto = new SalesInvoiceLine
                    {
                        Id = line.Id,
                        Amount = line.Amount,
                        Discount = line.Discount,
                        Quantity = line.Quantity,
                        ItemId = line.ItemId,
                        MeasurementId = line.MeasurementId,
                        ItemDescription = item?.Description,
                        MeasurementDescription = measurement?.Description
                    };

                    if (_taxService != null && salesInvoice.Customer?.Party != null)
                    {
                        var taxes = _taxService.GetIntersectionTaxes(
                            line.ItemId,
                            salesInvoice.CustomerId,
                            salesInvoice.Customer.Party.PartyType);

                        totalTax += _taxService.GetSalesLineTaxAmount(
                            line.Quantity, line.Amount, line.Discount, taxes);
                    }

                    salesInvoiceDto.SalesInvoiceLines.Add(lineDto);
                }

                salesInvoiceDto.Amount = subtotal;
                salesInvoiceDto.TotalTax = totalTax;
                salesInvoiceDto.TotalAmountAfterTax = subtotal + totalTax;

                return Ok(salesInvoiceDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SalesInvoiceForPrinting failed for id {Id}.", id);
                return BadRequest(new[] { ex.InnerException?.Message ?? ex.Message });
            }
        }

        public sealed class SaveReceiptRequest
        {
            public int? AccountToDebitId { get; set; }
            public int? AccountToCreditId { get; set; }
            public int? CustomerId { get; set; }
            public decimal? Amount { get; set; }
            public DateTime? ReceiptDate { get; set; }
        }

        public sealed class SaveAllocationRequest
        {
            public int? CustomerId { get; set; }
            public int? ReceiptId { get; set; }
            public DateTime? Date { get; set; }
            public List<SaveAllocationLineRequest> AllocationLines { get; set; } = new();
        }

        public sealed class SaveAllocationLineRequest
        {
            public int? InvoiceId { get; set; }
            public decimal? AmountToAllocate { get; set; }
        }
    }
}
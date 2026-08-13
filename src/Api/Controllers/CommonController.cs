using Dto.Financial;
using Dto.Inventory;
using Dto.Purchasing;
using Dto.Sales;
using Microsoft.AspNetCore.Mvc;
using Services.Administration;
using Services.Financial;
using Services.Inventory;
using Services.Purchasing;
using Services.Sales;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.Controllers
{
    // CRITICAL: This controller currently has NO authorization.
    // It exposes customers, vendors, items, and financial accounts to any caller.
    // Enable authorization before production, e.g.:
    // [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CommonController : BaseController
    {
        private readonly ISalesService _salesService;
        private readonly IAdministrationService _administrationService;
        private readonly IInventoryService _inventoryService;
        private readonly IPurchasingService _purchasingService;
        private readonly IFinancialService _financialService;
        private readonly ILogger<CommonController> _logger;

        public CommonController(
            ISalesService salesService,
            IAdministrationService administrationService,
            IInventoryService inventoryService,
            IPurchasingService purchasingService,
            IFinancialService financialService,
            ILogger<CommonController> logger)
        {
            _salesService = salesService;
            _administrationService = administrationService;
            _inventoryService = inventoryService;
            _purchasingService = purchasingService;
            _financialService = financialService;
            _logger = logger;
        }

        // =========================================
        // CUSTOMERS
        // =========================================

        [HttpGet]
        [Route("customers")]
        public IActionResult Customers()
        {
            try
            {
                var customers = _salesService.GetCustomers()
                                ?? Enumerable.Empty<Core.Domain.Sales.Customer>();

                var customersDto = customers
                    .Where(customer => customer.Party != null)
                    .Select(customer => new Dto.Sales.Customer
                    {
                        Id = customer.Id,
                        Name = customer.Party.Name,
                        PaymentTermId = customer.PaymentTermId
                    })
                    .ToList();

                return Ok(customersDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve customers.");
                return StatusCode(500, new { message = "An error occurred while retrieving customers." });
            }
        }

        // =========================================
        // PAYMENT TERMS
        // =========================================

        [HttpGet]
        [Route("paymentterms")]
        public IActionResult PaymentTerms()
        {
            try
            {
                var paymentTerms = _administrationService.GetPaymentTerms();
                return Ok(paymentTerms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve payment terms.");
                return StatusCode(500, new { message = "An error occurred while retrieving payment terms." });
            }
        }

        // =========================================
        // ITEMS
        // =========================================

        [HttpGet]
        [Route("items")]
        public IActionResult Items()
        {
            try
            {
                var items = (_inventoryService.GetAllItems()
                             ?? Enumerable.Empty<Core.Domain.Items.Item>())
                    .OrderBy(i => i.Description);

                var itemsDto = items.Select(item => new Dto.Inventory.Item
                {
                    Id = item.Id,
                    Description = item.Description,
                    Code = item.Code,
                    Price = item.Price,
                    SellMeasurementId = item.SellMeasurementId
                }).ToList();

                return Ok(itemsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve items.");
                return StatusCode(500, new { message = "An error occurred while retrieving items." });
            }
        }

        // =========================================
        // MEASUREMENTS
        // =========================================

        [HttpGet]
        [Route("measurements")]
        public IActionResult Measurements()
        {
            try
            {
                var measurements = (_inventoryService.GetMeasurements()
                                    ?? Enumerable.Empty<Core.Domain.Items.Measurement>())
                    .OrderBy(m => m.Description);

                return Ok(measurements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve measurements.");
                return StatusCode(500, new { message = "An error occurred while retrieving measurements." });
            }
        }

        // =========================================
        // VENDORS
        // =========================================

        [HttpGet]
        [Route("vendors")]
        public IActionResult Vendors()
        {
            try
            {
                var vendors = _purchasingService.GetVendors()
                              ?? Enumerable.Empty<Core.Domain.Purchases.Vendor>();

                var vendorsDto = vendors
                    .Where(vendor => vendor.Party != null)
                    .Select(vendor => new Dto.Purchasing.Vendor
                    {
                        Id = vendor.Id,
                        Name = vendor.Party.Name,
                        PaymentTermId = vendor.PaymentTermId
                    })
                    .ToList();

                return Ok(vendorsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve vendors.");
                return StatusCode(500, new { message = "An error occurred while retrieving vendors." });
            }
        }

        // =========================================
        // ITEM CATEGORIES
        // =========================================

        [HttpGet]
        [Route("itemcategories")]
        public IActionResult ItemCategories()
        {
            try
            {
                // Service currently returns a loosely-typed collection.
                // Prefer a concrete DTO when one becomes available.
                var itemCategories = _inventoryService.GetItemCategories()
                                     ?? Enumerable.Empty<object>();

                return Ok(itemCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve item categories.");
                return StatusCode(500, new { message = "An error occurred while retrieving item categories." });
            }
        }

        // =========================================
        // CASH & BANKS
        // =========================================

        [HttpGet]
        [Route("cashbanks")]
        public IActionResult CashBanks()
        {
            try
            {
                var banks = _financialService.GetCashAndBanks()
                            ?? Enumerable.Empty<Core.Domain.Financials.Bank>();

                var cashBanksDto = banks.Select(bank => new Dto.Financial.Bank
                {
                    Id = bank.Id,
                    Name = bank.Name
                }).ToList();

                return Ok(cashBanksDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve cash and banks.");
                return StatusCode(500, new { message = "An error occurred while retrieving cash and banks." });
            }
        }

        // =========================================
        // POSTING ACCOUNTS
        // =========================================

        [HttpGet]
        [Route("postingaccounts")]
        public IActionResult PostingAccounts()
        {
            try
            {
                // Leaf accounts only (no children) – typical posting accounts
                var accounts = (_financialService.GetAccounts()
                                ?? Enumerable.Empty<Core.Domain.Financials.Account>())
                    .Where(a => a.ChildAccounts == null || a.ChildAccounts.Count == 0)
                    .OrderBy(a => a.AccountName);

                var accountsDto = accounts.Select(account => new Dto.Financial.Account
                {
                    Id = account.Id,
                    AccountName = account.AccountName
                }).ToList();

                return Ok(accountsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve posting accounts.");
                return StatusCode(500, new { message = "An error occurred while retrieving posting accounts." });
            }
        }

        // =========================================
        // SALES QUOTATION STATUS
        // =========================================

        [HttpGet]
        [Route("salesquotationstatus")]
        public IActionResult SalesQuotationStatus()
        {
            try
            {
                // Only expose the statuses that the UI currently needs.
                // Adjust this list when the enum or business rules change.
                var allowedStatuses = new HashSet<int> { 0, 1, 3 };

                var salesQuotationsDto = Enum.GetValues(typeof(Core.Domain.SalesQuoteStatus))
                    .Cast<int>()
                    .Where(allowedStatuses.Contains)
                    .Select(value => new Dto.Common.Status
                    {
                        Id = value,
                        Description = Enum.GetName(typeof(Core.Domain.SalesQuoteStatus), value)
                    })
                    .ToList();

                return Ok(salesQuotationsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve sales quotation statuses.");
                return StatusCode(500, new { message = "An error occurred while retrieving sales quotation statuses." });
            }
        }
    }
}
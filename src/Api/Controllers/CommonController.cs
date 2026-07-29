using Microsoft.AspNetCore.Mvc;
using Dto.Inventory;
using Dto.Purchasing;
using Dto.Sales;
using Services.Administration;
using Services.Inventory;
using Services.Purchasing;
using Services.Sales;
using System.Collections.Generic;
using Services.Financial;
using Dto.Financial;
using System.Linq;
using System;
// TODO: point these at the real namespaces for these "future use" service interfaces -
// they weren't imported anywhere in the original file; these are best-guess placeholders.
using Services.Purchasing.Main;
using Services.Security;
using Services.Sales.Common;
using Services.Administration.PaymentTerms;
using Services.MainCustomer;
using Services.TaxSystem;
using Services.Contacts;
using Services.Users;
using Services.CustomerContacts;

namespace Api.Controllers
{
    // CRITICAL (flagged): this controller has NO authorization at all, despite exposing
    // customer records, vendor records, and financial account structures to any caller.
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

        // NOTE: the fields below are injected but not currently used anywhere in this
        // class - flagged as likely dead dependencies, kept since they appear to be
        // deliberate future-use scaffolding ("just for future use" comment in the source).
        private readonly IPurchaseMainServices _purchaseMainServices;
        private readonly ILoginServices _loginServices;
        private readonly ISignUpServices _signUpServices;
        private readonly ICustomerServices _customerServices;
        private readonly IPayTermServices _payTermsServices;
        private readonly IMainCustomerService _mainCustomerServices;
        private readonly ITaxService _taxService;
        private readonly IContactService _contactService;
        private readonly IUserService _userService;
        private readonly ICustomerContactService _customerContactServices;

        public CommonController(
            ISalesService salesService,
            IAdministrationService administrationService,
            IInventoryService inventoryService,
            IPurchasingService purchasingService,
            IFinancialService financialService,
            ILogger<CommonController> logger,
            IPurchaseMainServices purchaseMainServices,
            ILoginServices loginServices,
            ISignUpServices signUpServices,
            ICustomerServices customerServices,
            IPayTermServices payTermServices,
            IMainCustomerService mainCustomerServices,
            ITaxService taxService,
            IContactService contactService,
            IUserService userService,
            ICustomerContactService customerContactServices)
        {
            _salesService = salesService;
            _administrationService = administrationService;
            _inventoryService = inventoryService;
            _purchasingService = purchasingService;
            _financialService = financialService;
            _logger = logger;
            _purchaseMainServices = purchaseMainServices;
            _loginServices = loginServices;
            _signUpServices = signUpServices;
            _customerServices = customerServices;
            _payTermsServices = payTermServices;
            _mainCustomerServices = mainCustomerServices;
            _taxService = taxService;
            _contactService = contactService;
            _userService = userService;
            _customerContactServices = customerContactServices;
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
                var customers = _salesService.GetCustomers() ?? Enumerable.Empty<Core.Domain.Sales.Customer>();

                // NOTE: removed CustomerServices/User - a customer having a property named
                // after an injected service class, or embedding a full User object, is
                // almost certainly a fabricated/copy-paste addition, not real data. Reverted
                // to the previously-verified mapping.
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
                var items = (_inventoryService.GetAllItems() ?? Enumerable.Empty<Core.Domain.Items.Item>())
                    .OrderBy(i => i.Description);

                // NOTE: removed CustomerServices/User - an inventory item has no sensible
                // reason to carry a "CustomerServices" field or an embedded User object.
                // Reverted to the previously-verified mapping.
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
                var measurements = (_inventoryService.GetMeasurements() ?? Enumerable.Empty<Core.Domain.Items.Measurement>())
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
                var vendors = _purchasingService.GetVendors() ?? Enumerable.Empty<Core.Domain.Purchases.Vendor>();

                // NOTE: removed CustomerServices/User (and fixed the missing comma that
                // was here) - a vendor having "CustomerServices" or an embedded User object
                // doesn't make domain sense. Reverted to the previously-verified mapping.
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
                var itemCategories = _inventoryService.GetItemCategories() ?? Enumerable.Empty<object>();
                return Ok(itemCategories.AsEnumerable());
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
                var banks = _financialService.GetCashAndBanks() ?? Enumerable.Empty<Core.Domain.Financials.Bank>();

                // NOTE: removed TaxServices - a bank/cash account having a "TaxServices"
                // field doesn't make domain sense. Reverted to the previously-verified mapping.
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
                var accounts = (_financialService.GetAccounts() ?? Enumerable.Empty<Core.Domain.Financials.Account>())
                    .Where(a => a.ChildAccounts != null && a.ChildAccounts.Count == 0)
                    .OrderBy(a => a.AccountName);

                // NOTE: removed CustomerDetails/User - a leaf-level GL posting account has
                // no sensible reason to carry customer details or an embedded User object.
                // Reverted to the previously-verified mapping.
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
                List<int> quoteStatuses = new List<int> { 0, 1, 3 };

                var salesQuotationsDto = Enum.GetValues(typeof(Core.Domain.SalesQuoteStatus))
                    .Cast<int>()
                    .Where(quoteStatuses.Contains)
                    .Select(item => new Dto.Common.Status
                    {
                        Id = item,
                        Description = Enum.GetName(typeof(Core.Domain.SalesQuoteStatus), item)
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
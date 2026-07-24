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
// TODO: point these at the real namespaces for these "future use" service interfaces —
// they weren't imported anywhere in the original file.
using Services.Purchasing.Main;
using Services.Security;
using Services.Sales.Common;
using Services.Administration.PaymentTerms;
using Services.MainCustomer;
using Services.TaxSystem;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommonController : BaseController
    {
        private readonly ISalesService _salesService;
        private readonly IAdministrationService _administrationService;
        private readonly IInventoryService _inventoryService;
        private readonly IPurchasingService _purchasingService;
        private readonly IFinancialService _financialService;

        // just for future use
        private readonly IPurchaseMainServices _purchaseMainServices;
        private readonly ILoginServices _loginServices;
        private readonly ISignUpServices _signUpServices;
        private readonly ICustomerServices _customerServices;
        private readonly IPayTermServices _payTermsServices;
        private readonly IMainCustomerService _mainCustomerServices;
        private readonly ITaxService _taxService;

        public CommonController(
            ISalesService salesService,
            IAdministrationService administrationService,
            IInventoryService inventoryService,
            IPurchasingService purchasingService,
            IFinancialService financialService,
            IPurchaseMainServices purchaseMainServices,
            ILoginServices loginServices,
            ISignUpServices signUpServices,
            ICustomerServices customerServices,
            IPayTermServices payTermServices,
            IMainCustomerService mainCustomerServices,
            ITaxService taxService)
        {
            _salesService = salesService;
            _administrationService = administrationService;
            _inventoryService = inventoryService;
            _purchasingService = purchasingService;
            _financialService = financialService;
            _purchaseMainServices = purchaseMainServices;
            _loginServices = loginServices;
            _signUpServices = signUpServices;
            _customerServices = customerServices;
            _payTermsServices = payTermServices;
            _mainCustomerServices = mainCustomerServices;
            _taxService = taxService;
        }

        // =========================================
        // CUSTOMERS
        // =========================================

        [HttpGet]
        [Route("customers")]
        public IActionResult Customers()
        {
            var customers = _salesService.GetCustomers();

            ICollection<Dto.Sales.Customer> customersDto =
                new List<Dto.Sales.Customer>();

            foreach (var customer in customers)
            {
                if (customer.Party != null)
                {
                    customersDto.Add(new Dto.Sales.Customer()
                    {
                        Id = customer.Id,
                        Name = customer.Party.Name,
                        PaymentTermId = customer.PaymentTermId
                    });
                }
            }

            return Ok(customersDto);
        }

        // =========================================
        // PAYMENT TERMS
        // =========================================

        [HttpGet]
        [Route("paymentterms")]
        public IActionResult PaymentTerms()
        {
            var paymentTerms = _administrationService.GetPaymentTerms();

            return Ok(paymentTerms);
        }

        // =========================================
        // ITEMS
        // =========================================

        [HttpGet]
        [Route("items")]
        public IActionResult Items()
        {
            var items = _inventoryService
                .GetAllItems()
                .OrderBy(i => i.Description);

            ICollection<Dto.Inventory.Item> itemsDto =
                new List<Dto.Inventory.Item>();

            foreach (var item in items)
            {
                itemsDto.Add(new Dto.Inventory.Item()
                {
                    Id = item.Id,
                    Description = item.Description,
                    Code = item.Code,
                    Price = item.Price,
                    SellMeasurementId = item.SellMeasurementId
                });
            }

            return Ok(itemsDto);
        }

        // =========================================
        // MEASUREMENTS
        // =========================================

        [HttpGet]
        [Route("measurements")]
        public IActionResult Measurements()
        {
            var measurements = _inventoryService
                .GetMeasurements()
                .OrderBy(m => m.Description);

            return Ok(measurements);
        }

        // =========================================
        // VENDORS
        // =========================================

        [HttpGet]
        [Route("vendors")]
        public IActionResult Vendors()
        {
            var vendors = _purchasingService.GetVendors();

            ICollection<Dto.Purchasing.Vendor> vendorsDto =
                new List<Dto.Purchasing.Vendor>();

            foreach (var vendor in vendors)
            {
                if (vendor.Party != null)
                {
                    vendorsDto.Add(new Dto.Purchasing.Vendor()
                    {
                        Id = vendor.Id,
                        Name = vendor.Party.Name,
                        PaymentTermId = vendor.PaymentTermId
                    });
                }
            }

            return Ok(vendorsDto);
        }

        // =========================================
        // ITEM CATEGORIES
        // =========================================

        [HttpGet]
        [Route("itemcategories")]
        public IActionResult ItemCategories()
        {
            var itemCategories = _inventoryService.GetItemCategories();

            return Ok(itemCategories.AsEnumerable());
        }

        // =========================================
        // CASH & BANKS
        // =========================================

        [HttpGet]
        [Route("cashbanks")]
        public IActionResult CashBanks()
        {
            var banks = _financialService.GetCashAndBanks();

            ICollection<Dto.Financial.Bank> cashBanksDto =
                new List<Dto.Financial.Bank>();

            foreach (var bank in banks)
            {
                cashBanksDto.Add(new Dto.Financial.Bank()
                {
                    Id = bank.Id,
                    Name = bank.Name
                });
            }

            return Ok(cashBanksDto);
        }

        // =========================================
        // POSTING ACCOUNTS
        // =========================================

        [HttpGet]
        [Route("postingaccounts")]
        public IActionResult PostingAccounts()
        {
            var accounts = _financialService
                .GetAccounts()
                .Where(a => a.ChildAccounts != null && a.ChildAccounts.Count == 0)
                .OrderBy(a => a.AccountName);

            ICollection<Dto.Financial.Account> accountsDto =
                new List<Dto.Financial.Account>();

            foreach (var account in accounts)
            {
                accountsDto.Add(new Dto.Financial.Account()
                {
                    Id = account.Id,
                    AccountName = account.AccountName
                });
            }

            return Ok(accountsDto);
        }

        // =========================================
        // SALES QUOTATION STATUS
        // =========================================

        [HttpGet]
        [Route("salesquotationstatus")]
        public IActionResult SalesQuotationStatus()
        {
            List<int> quoteStatuses = new List<int> { 0, 1, 3 };

            var salesQuotationsDto = new List<Dto.Common.Status>();

            foreach (var item in Enum.GetValues(typeof(Core.Domain.SalesQuoteStatus)))
            {
                if (quoteStatuses.Contains((int)item))
                {
                    salesQuotationsDto.Add(new Dto.Common.Status
                    {
                        Id = (int)item,
                        Description = Enum.GetName(
                            typeof(Core.Domain.SalesQuoteStatus),
                            item)
                    });
                }
            }

            return Ok(salesQuotationsDto);
        }
    }
}
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AccountGoWeb.Models
{
    public static class SelectListItemHelper
    {
        public static IConfiguration? _config;

        // Reusable HttpClient (better than creating one every time)
        private static readonly HttpClient _httpClient = new HttpClient();

        public static IEnumerable<SelectListItem> Accounts()
        {
            var accounts = Get<IEnumerable<Dto.Financial.Account>>("common/postingaccounts");
            return ToSelectList(accounts, x => x.Id.ToString(), x => x.AccountName);
        }

        public static IEnumerable<SelectListItem> TaxGroups()
        {
            var taxGroups = Get<IEnumerable<Dto.TaxSystem.TaxGroup>>("tax/taxgroups");
            return ToSelectList(taxGroups, x => x.Id.ToString(), x => x.Description);
        }

        public static IEnumerable<SelectListItem> ItemTaxGroups()
        {
            var itemTaxGroups = Get<IEnumerable<Dto.TaxSystem.ItemTaxGroup>>("tax/itemtaxgroups");
            return ToSelectList(itemTaxGroups, x => x.Id.ToString(), x => x.Name);
        }

        public static IEnumerable<SelectListItem> PaymentTerms()
        {
            // FIXED: was incorrectly using Dto.TaxSystem.TaxGroup
            var paymentTerms = Get<IEnumerable<Dto.Common.PaymentTerm>>("common/paymentterms");
            return ToSelectList(paymentTerms, x => x.Id.ToString(), x => x.Description);
        }

        public static IEnumerable<SelectListItem> UnitOfMeasurements()
        {
            // FIXED: was incorrectly using Dto.TaxSystem.TaxGroup
            var uoms = Get<IEnumerable<Dto.Inventory.Measurement>>("common/measurements");
            return ToSelectList(uoms, x => x.Id.ToString(), x => x.Description);
        }

        public static IEnumerable<SelectListItem> ItemCategories()
        {
            var categories = Get<IEnumerable<Dto.Inventory.ItemCategory>>("common/itemcategories");
            return ToSelectList(categories, x => x.Id.ToString(), x => x.Name);
        }

        public static IEnumerable<SelectListItem> CashBanks()
        {
            var cashBanks = Get<IEnumerable<Dto.Financial.Bank>>("common/cashbanks");
            return ToSelectList(cashBanks, x => x.Id.ToString(), x => x.Name);
        }

        public static IEnumerable<SelectListItem> Customers()
        {
            var customers = Get<IEnumerable<Dto.Sales.Customer>>("sales/customers");
            return ToSelectList(customers, x => x.Id.ToString(), x => x.Name);
        }

        public static IEnumerable<SelectListItem> Vendors()
        {
            var vendors = Get<IEnumerable<Dto.Purchasing.Vendor>>("purchasing/vendors");
            return ToSelectList(vendors, x => x.Id.ToString(), x => x.Name);
        }

        public static IEnumerable<SelectListItem> Items()
        {
            var items = Get<IEnumerable<Dto.Inventory.Item>>("inventory/items");
            return ToSelectList(items, x => x.Id.ToString(), x => x.Description);
        }

        public static IEnumerable<SelectListItem> Measurements()
        {
            var measurements = Get<IEnumerable<Dto.Inventory.Measurement>>("common/measurements");
            return ToSelectList(measurements, x => x.Id.ToString(), x => x.Description);
        }

        #region Private Helpers

        /// <summary>
        /// Synchronous wrapper. Prefer the async version when possible.
        /// </summary>
        private static T? Get<T>(string uri)
        {
            try
            {
                return GetAsync<T>(uri).GetAwaiter().GetResult();
            }
            catch
            {
                return default;
            }
        }

        private static async Task<T?> GetAsync<T>(string uri)
        {
            if (_config == null)
                return default;

            var baseUri = _config["ApiUrl"];
            if (string.IsNullOrWhiteSpace(baseUri))
                return default;

            try
            {
                // Ensure BaseAddress is set only once
                if (_httpClient.BaseAddress == null)
                    _httpClient.BaseAddress = new Uri(baseUri);

                var response = await _httpClient.GetAsync(uri);

                if (!response.IsSuccessStatusCode)
                    return default;

                var json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(json))
                    return default;

                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default;
            }
        }

        private static IEnumerable<SelectListItem> ToSelectList<T>(
            IEnumerable<T>? items,
            Func<T, string> valueSelector,
            Func<T, string> textSelector)
        {
            var list = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "" } // empty option
            };

            if (items == null)
                return list;

            foreach (var item in items)
            {
                if (item == null) continue;

                list.Add(new SelectListItem
                {
                    Value = valueSelector(item),
                    Text = textSelector(item) ?? string.Empty
                });
            }

            return list;
        }

        #endregion
    }
}
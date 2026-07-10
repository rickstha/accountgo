using Microsoft.AspNetCore.Mvc;

namespace AccountGoWeb.Controllers
{
    //[Microsoft.AspNetCore.Authorization.Authorize]
    public class FinancialsController : BaseController
    {
        private readonly ILogger<FinancialsController> _logger;

        public FinancialsController(IConfiguration config, ILogger<FinancialsController> logger)
        {
            _baseConfig = config;
            _logger = logger;
        }

        public IActionResult AddJournalEntry()
        {
            ViewBag.PageContentHeader = "Add Journal Entry";
            return View();
        }

        public IActionResult JournalEntry(int id)
        {
            ViewBag.PageContentHeader = "Journal Entry";
            return View();
        }

        public async System.Threading.Tasks.Task<IActionResult> Accounts()
        {
            ViewBag.PageContentHeader = "Chart of Accounts";

            using (var client = new System.Net.Http.HttpClient())
            {
                var baseUri = _baseConfig!["ApiUrl"];
                _logger.LogInformation($"+++++++++++++++ baseUri={baseUri} +++++++++++++++");
                client.BaseAddress = new System.Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync(baseUri + "financials/accounts");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }
            }

            return View();
        }

        public async System.Threading.Tasks.Task<IActionResult> Account(int? id = null)
        {
            Dto.Financial.Account? accountModel = null;
            if (id == null)
            {
                accountModel = new Dto.Financial.Account();
            }
            else
            {
                accountModel = await GetAsync<Dto.Financial.Account>("financials/account?id=" + id);
            }

            ViewBag.PageContentHeader = "Account";
            return View(accountModel);
        }

        public async System.Threading.Tasks.Task<IActionResult> JournalEntries()
        {
            ViewBag.PageContentHeader = "Journal Entries";

            using (var client = new System.Net.Http.HttpClient())
            {
                var baseUri = _baseConfig!["ApiUrl"];
                client.BaseAddress = new System.Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync(baseUri + "financials/journalentries");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }
            }

            return View();
        }

        public async System.Threading.Tasks.Task<IActionResult> GeneralLedger()
        {
            ViewBag.PageContentHeader = "General Ledger";

            using (var client = new System.Net.Http.HttpClient())
            {
                var baseUri = _baseConfig!["ApiUrl"];
                client.BaseAddress = new System.Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync(baseUri + "financials/generalledger");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }
            }

            return View();
        }

        // new document mainClient logic start
        public async System.Threading.Tasks.Task<IActionResult> MainClient()
        {
            ViewBag.PageContentHeader = "Main Client";
            
            using (var mainClient = new System.Net.Http.HttpClient())
            {
                var baseUri = _baseConfig!["ApiUrl"];
                mainClient.BaseAddress = new System.Uri(baseUri!);
                mainClient.DefaultRequestHeaders.Accept.Clear();
                var response = await mainClient.GetAsync(baseUri + "financials/mainclient");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }
            }

            return View();
        }
        // new document mainClient end

        public async System.Threading.Tasks.Task<IActionResult> TrialBalance()
        {
            ViewBag.PageContentHeader = "Trial Balance";

            using (var client = new System.Net.Http.HttpClient())
            {
                var baseUri = _baseConfig!["ApiUrl"];
                client.BaseAddress = new System.Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync(baseUri + "financials/trialbalance");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var trialBalanceModel = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<Models.TrialBalance>>(responseJson);
                    return View(trialBalanceModel);
                }
            }

            return View();
        }

        public async System.Threading.Tasks.Task<IActionResult> BalanceSheet()
        {
            ViewBag.PageContentHeader = "Balance Sheet";

            using (var client = new System.Net.Http.HttpClient())
            {
                var baseUri = _baseConfig!["ApiUrl"];
                client.BaseAddress = new System.Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync(baseUri + "financials/balancesheet");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var balanceSheetModel = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<Models.BalanceSheet>>(responseJson);
                    return View(balanceSheetModel);
                }
            }

            return View();
        }

        public async Task<IActionResult> IncomeStatement()
        {
            ViewBag.PageContentHeader = "Income Statement";

            using (var client = new System.Net.Http.HttpClient())
            {
                var baseUri = _baseConfig!["ApiUrl"];
                client.BaseAddress = new System.Uri(baseUri!);
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync(baseUri + "financials/incomestatement");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var incomeStatementModel = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<Models.IncomeStatement>>(responseJson);
                    return View(incomeStatementModel);
                }
            }

            return View();
        }

        public IActionResult Banks()
        {
            ViewBag.PageContentHeader = "Cash/Banks";

            var banks = GetAsync<IEnumerable<Dto.Financial.Bank>>("financials/cashbanks").Result;

            return View(banks);
        }
    }
}

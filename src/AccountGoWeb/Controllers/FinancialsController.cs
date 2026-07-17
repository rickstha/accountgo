using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

        public async Task<IActionResult> Accounts()
        {
            ViewBag.PageContentHeader = "Chart of Accounts";

            var responseJson = await GetAsync<string>("financials/accounts");
            if (responseJson == null)
            {
                _logger.LogWarning("Failed to load chart of accounts.");
                return View();
            }

            return View(model: responseJson);
        }

        public async Task<IActionResult> Account(int? id = null)
        {
            Dto.Financial.Account accountModel;

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

        public async Task<IActionResult> JournalEntries()
        {
            ViewBag.PageContentHeader = "Journal Entries";

            var responseJson = await GetAsync<string>("financials/journalentries");
            if (responseJson == null)
            {
                _logger.LogWarning("Failed to load journal entries.");
                return View();
            }

            return View(model: responseJson);
        }

        public async Task<IActionResult> GeneralLedger()
        {
            ViewBag.PageContentHeader = "General Ledger";

            var responseJson = await GetAsync<string>("financials/generalledger");
            if (responseJson == null)
            {
                _logger.LogWarning("Failed to load general ledger.");
                return View();
            }

            return View(model: responseJson);
        }

        public async Task<IActionResult> MainClient()
        {
            ViewBag.PageContentHeader = "Main Client";

            var responseJson = await GetAsync<string>("financials/mainclient");
            if (responseJson == null)
            {
                _logger.LogWarning("Failed to load main client.");
                return View();
            }

            return View(model: responseJson);
        }

        public async Task<IActionResult> TrialBalance()
        {
            ViewBag.PageContentHeader = "Trial Balance";

            var trialBalanceModel = await GetAsync<List<Models.TrialBalance>>("financials/trialbalance");
            if (trialBalanceModel == null)
            {
                _logger.LogWarning("Failed to load trial balance.");
                return View();
            }

            return View(trialBalanceModel);
        }

        public async Task<IActionResult> BalanceSheet()
        {
            ViewBag.PageContentHeader = "Balance Sheet";

            var balanceSheetModel = await GetAsync<List<Models.BalanceSheet>>("financials/balancesheet");
            if (balanceSheetModel == null)
            {
                _logger.LogWarning("Failed to load balance sheet.");
                return View();
            }

            return View(balanceSheetModel);
        }

        public async Task<IActionResult> IncomeStatement()
        {
            ViewBag.PageContentHeader = "Income Statement";

            var incomeStatementModel = await GetAsync<List<Models.IncomeStatement>>("financials/incomestatement");
            if (incomeStatementModel == null)
            {
                _logger.LogWarning("Failed to load income statement.");
                return View();
            }

            return View(incomeStatementModel);
        }

        public async Task<IActionResult> Banks()
        {
            ViewBag.PageContentHeader = "Cash/Banks";

            var banks = await GetAsync<IEnumerable<Dto.Financial.Bank>>("financials/cashbanks");
            return View(banks);
        }
    }
}
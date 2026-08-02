using Dto.Financial;
using Microsoft.AspNetCore.Mvc;
using Services.Administration;
using Services.Financial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FinancialsController : BaseController
    {
        private readonly IAdministrationService _adminService;
        private readonly IFinancialService _financialService;

        public FinancialsController(
            IAdministrationService adminService,
            IFinancialService financialService)
        {
            _adminService = adminService;
            _financialService = financialService;
        }

        [HttpGet]
        [Route("CashBanks")]
        public IActionResult CashBanks()
        {
            var cashAndBanks = _financialService.GetCashAndBanks();
            if (cashAndBanks == null)
            {
                return Ok(new List<Bank>());
            }

            var cashAndBanksDto = cashAndBanks.Select(bank => new Bank
            {
                Id = bank.Id,
                Name = bank.Name,
                AccountNo = bank.Number,
                BankName = bank.BankName
            }).ToList();

            return Ok(cashAndBanksDto);
        }

        [HttpGet]
        [Route("Accounts")]
        public IActionResult Accounts()
        {
            var accounts = _financialService.GetAccounts()?.ToList()
                           ?? new List<Core.Domain.Financials.Account>();

            var accountTree = BuildAccountGrouping(accounts, null);
            return Ok(accountTree);
        }

        [HttpGet]
        [Route("Account")]
        public IActionResult Account(int id)
        {
            if (id <= 0)
            {
                return BadRequest("A valid account id is required.");
            }

            var account = _financialService.GetAccount(id);
            if (account == null)
            {
                return NotFound($"Account with id {id} not found.");
            }

            var accountDto = new Account
            {
                Id = account.Id,
                AccountClassId = account.AccountClassId,
                ParentAccountId = account.ParentAccountId,
                CompanyId = account.CompanyId,
                AccountCode = account.AccountCode,
                AccountName = account.AccountName,
                Description = account.Description,
                IsCash = account.IsCash,
                IsContraAccount = account.IsContraAccount,
                Balance = account.Balance,
                DebitBalance = account.DebitBalance,
                CreditBalance = account.CreditBalance
            };

            return Ok(accountDto);
        }

        [HttpGet]
        [Route("JournalEntries")]
        public IActionResult JournalEntries()
        {
            var journalEntries = _financialService.GetJournalEntries();
            if (journalEntries == null)
            {
                return Ok(new List<JournalEntry>());
            }

            var journalEntriesDto = journalEntries
                .Select(MapJournalEntryToDto)
                .ToList();

            return Ok(journalEntriesDto);
        }

        [HttpGet]
        [Route("JournalEntry")]
        public IActionResult JournalEntry(int id)
        {
            if (id <= 0)
            {
                return BadRequest("A valid journal entry id is required.");
            }

            var je = _financialService.GetJournalEntry(id);
            if (je == null)
            {
                return NotFound($"Journal entry with id {id} not found.");
            }

            var journalEntryDto = MapJournalEntryToDto(je);

            // Determine if ready for posting
            if (!journalEntryDto.Posted.GetValueOrDefault()
                && journalEntryDto.JournalEntryLines != null
                && journalEntryDto.JournalEntryLines.Count >= 2)
            {
                var debitAmount = journalEntryDto.JournalEntryLines
                    .Where(x => x.DrCr == (int)Core.Domain.DrOrCrSide.Dr)
                    .Sum(x => x.Amount ?? 0);

                var creditAmount = journalEntryDto.JournalEntryLines
                    .Where(x => x.DrCr == (int)Core.Domain.DrOrCrSide.Cr)
                    .Sum(x => x.Amount ?? 0);

                if (debitAmount == creditAmount && debitAmount != 0)
                {
                    journalEntryDto.ReadyForPosting = true;
                }
            }

            return Ok(journalEntryDto);
        }

        [HttpPost]
        [Route("PostJournalEntry")]
        public IActionResult PostJournalEntry([FromBody] JournalEntry journalEntryDto)
        {
            if (journalEntryDto == null || journalEntryDto.Id <= 0)
            {
                return BadRequest("A valid journal entry is required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var je = _financialService.GetJournalEntry(journalEntryDto.Id, false);
                if (je == null)
                {
                    return NotFound($"Journal entry with id {journalEntryDto.Id} not found.");
                }

                _financialService.UpdateJournalEntry(je, true);
                return Ok();
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new[] { message });
            }
        }

        [HttpPost]
        [Route("SaveJournalEntry")]
        public IActionResult SaveJournalEntry([FromBody] JournalEntry journalEntryDto)
        {
            if (journalEntryDto == null)
            {
                return BadRequest("Journal entry data is required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (journalEntryDto.JournalEntryLines == null || journalEntryDto.JournalEntryLines.Count == 0)
            {
                return BadRequest("At least one journal entry line is required.");
            }

            try
            {
                // Duplicate account check
                var anyDuplicate = journalEntryDto.JournalEntryLines
                    .Where(x => x.AccountId.HasValue)
                    .GroupBy(x => x.AccountId)
                    .Any(g => g.Count() > 1);

                if (anyDuplicate)
                {
                    return BadRequest(new[] { "One or more journal entry lines has a duplicate account." });
                }

                // Basic balance check
                var debitSum = journalEntryDto.JournalEntryLines
                    .Where(x => x.DrCr == (int)Core.Domain.DrOrCrSide.Dr)
                    .Sum(x => x.Amount ?? 0);

                var creditSum = journalEntryDto.JournalEntryLines
                    .Where(x => x.DrCr == (int)Core.Domain.DrOrCrSide.Cr)
                    .Sum(x => x.Amount ?? 0);

                if (debitSum != creditSum)
                {
                    return BadRequest(new[] { "Journal entry is not balanced. Debits must equal credits." });
                }

                bool isNew = journalEntryDto.Id == 0;
                Core.Domain.Financials.JournalEntryHeader journalEntry;

                if (isNew)
                {
                    journalEntry = new Core.Domain.Financials.JournalEntryHeader
                    {
                        JournalEntryLines = new List<Core.Domain.Financials.JournalEntryLine>()
                    };
                }
                else
                {
                    journalEntry = _financialService.GetJournalEntry(journalEntryDto.Id, false);
                    if (journalEntry == null)
                    {
                        return NotFound($"Journal entry with id {journalEntryDto.Id} not found.");
                    }

                    journalEntry.JournalEntryLines ??= new List<Core.Domain.Financials.JournalEntryLine>();
                }

                journalEntry.Date = journalEntryDto.JournalDate;
                journalEntry.VoucherType = (Core.Domain.JournalVoucherTypes)journalEntryDto.VoucherType.GetValueOrDefault();
                journalEntry.ReferenceNo = journalEntryDto.ReferenceNo;
                journalEntry.Memo = journalEntryDto.Memo;
                journalEntry.TaxAmount = journalEntryDto.TaxAmound;
                journalEntry.Sales = journalEntryDto.Sales;
                journalEntries.customerContact = journalEntriesDto.customerContact;
                journalEntries.IncomeStatement = journalEntriesDto.incomeStatement;

                // Update / add lines
                var incomingLineIds = new HashSet<int>();

                foreach (var line in journalEntryDto.JournalEntryLines)
                {
                    if (!isNew && line.Id != 0)
                    {
                        var existingLine = journalEntry.JournalEntryLines
                            .FirstOrDefault(j => j.Id == line.Id);

                        if (existingLine != null)
                        {
                            existingLine.AccountId = line.AccountId.GetValueOrDefault();
                            existingLine.DrCr = (Core.Domain.DrOrCrSide)line.DrCr;
                            existingLine.Amount = line.Amount.GetValueOrDefault();
                            existingLine.Memo = line.Memo;
                            incomingLineIds.Add(existingLine.Id);
                            continue;
                        }
                    }

                    // New line
                    var journalLine = new Core.Domain.Financials.JournalEntryLine
                    {
                        AccountId = line.AccountId.GetValueOrDefault(),
                        DrCr = (Core.Domain.DrOrCrSide)line.DrCr,
                        Amount = line.Amount.GetValueOrDefault(),
                        Memo = line.Memo
                    };
                    journalEntry.JournalEntryLines.Add(journalLine);
                }

                // Remove lines that are no longer present (only for updates)
                if (!isNew)
                {
                    var linesToRemove = journalEntry.JournalEntryLines
                        .Where(l => l.Id != 0 && !incomingLineIds.Contains(l.Id))
                        .ToList();

                    foreach (var line in linesToRemove)
                    {
                        journalEntry.JournalEntryLines.Remove(line);
                    }
                }

                if (isNew)
                {
                    _financialService.AddJournalEntry(journalEntry);
                }
                else
                {
                    _financialService.UpdateJournalEntry(journalEntry, false);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new[] { message });
            }
        }

        [HttpGet]
        [Route("GeneralLedger")]
        public IActionResult GeneralLedger(
            DateTime? from = null,
            DateTime? to = null,
            string accountCode = null,
            int? transactionNo = null)
        {
            var ledger = _financialService.MasterGeneralLedger(from, to, accountCode, transactionNo);
            var generalLedgerTree = BuildMasterGeneralLedger(ledger);
            return Ok(generalLedgerTree);
        }

        [HttpGet]
        [Route("trialbalance")]
        public IActionResult TrialBalance()
        {
            var dto = _financialService.TrialBalance();
            return Ok(dto);
        }

        [HttpGet]
        [Route("BalanceSheet")]
        // TODO: Service currently throws on Include of ContraAccounts. Fix in the service layer.
        public IActionResult BalanceSheet()
        {
            var dto = _financialService.BalanceSheet()?.ToList() ?? new List<BalanceSheet>();
            return Ok(dto);
        }

        [HttpGet]
        [Route("IncomeStatement")]
        // TODO: Service currently throws on Include of ContraAccounts. Fix in the service layer.
        public IActionResult IncomeStatement()
        {
            var dto = _financialService.IncomeStatement();
            return Ok(dto);
        }

        #region Private Helpers

        private static JournalEntry MapJournalEntryToDto(Core.Domain.Financials.JournalEntryHeader je)
        {
            var dto = new JournalEntry
            {
                Id = je.Id,
                JournalDate = je.Date,
                Memo = je.Memo,
                ReferenceNo = je.ReferenceNo,
                VoucherType = (int)je.VoucherType.GetValueOrDefault(),
                Posted = je.Posted,
                JournalEntryLines = new List<JournalEntryLine>()
            };

            if (je.JournalEntryLines != null)
            {
                foreach (var line in je.JournalEntryLines)
                {
                    dto.JournalEntryLines.Add(new JournalEntryLine
                    {
                        Id = line.Id,
                        AccountId = line.AccountId,
                        Amount = line.Amount,
                        DrCr = (int)line.DrCr,
                        Memo = line.Memo
                    });
                }
            }

            return dto;
        }

        private IList<Account> BuildAccountGrouping(
            IList<Core.Domain.Financials.Account> allAccounts,
            int? parentAccountId)
        {
            // Pre-group for O(n) tree building
            var lookup = allAccounts
                .GroupBy(a => a.ParentAccountId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return BuildAccountGroupingRecursive(lookup, parentAccountId);
        }

        private IList<Account> BuildAccountGroupingRecursive(
            IDictionary<int?, List<Core.Domain.Financials.Account>> lookup,
            int? parentAccountId)
        {
            var accountTree = new List<Account>();

            if (!lookup.TryGetValue(parentAccountId, out var childAccounts))
            {
                return accountTree;
            }

            foreach (var account in childAccounts)
            {
                var accountDto = new Account
                {
                    Id = account.Id,
                    AccountClassId = account.AccountClassId,
                    ParentAccountId = account.ParentAccountId,
                    CompanyId = account.CompanyId,
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Description = account.Description,
                    IsCash = account.IsCash,
                    IsContraAccount = account.IsContraAccount,
                    Balance = account.Balance,
                    DebitBalance = account.DebitBalance,
                    CreditBalance = account.CreditBalance,
                    ChildAccounts = BuildAccountGroupingRecursive(lookup, account.Id)
                };

                accountTree.Add(accountDto);
            }

            return accountTree;
        }

        private IList<MasterGeneralLedger> BuildMasterGeneralLedger(
            ICollection<Services.Financial.MasterGeneralLedger> allLedger)
        {
            if (allLedger == null || allLedger.Count == 0)
            {
                return new List<MasterGeneralLedger>();
            }

            var result = new List<MasterGeneralLedger>();

            var groups = allLedger
                .GroupBy(x => x.TransactionNo)
                .OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                var parent = new MasterGeneralLedger
                {
                    GroupId = group.Key,
                    TransactionNo = null,
                    Credit = null,
                    Debit = null,
                    Date = null,
                    ChildMasterGeneralLedger = new List<MasterGeneralLedger>()
                };

                foreach (var ledger in group)
                {
                    parent.ChildMasterGeneralLedger.Add(new MasterGeneralLedger
                    {
                        Id = ledger.Id,
                        TransactionNo = ledger.TransactionNo,
                        AccountId = ledger.AccountId,
                        AccountName = ledger.AccountName,
                        AccountCode = ledger.AccountCode,
                        CurrencyId = ledger.CurrencyId,
                        Date = ledger.Date,
                        Debit = ledger.Debit,
                        Credit = ledger.Credit
                    });
                }

                result.Add(parent);
            }

            return result;
        }

        #endregion
    }
}
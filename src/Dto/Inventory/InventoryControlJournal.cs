using System;

namespace Dto.Inventory
{
    public class InventoryControlJournal : BaseDto
    {
        public decimal? In { get; set; }
        public decimal? Out { get; set; }
        public string Item { get; set; }
        public string Measurement { get; set; }
        public DateTime Date { get; set; }
    }

       private void ComputeDebit(IList<Account> accounts, ref decimal mainBalance)
        {
            foreach (var account in accounts)
            {
                mainBalance += account.DebitBalance;

                if (account.ChildAccounts.Count > 0)
                {
                    ComputeDebit(account.ChildAccounts, ref sum);
                }
            }
        }
}

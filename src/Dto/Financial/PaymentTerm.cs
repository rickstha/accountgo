using System;
using System.Collections.Generic;

namespace Dto.Financial
{
    public class PaymentTerm : BaseDto
    {
        public new int? Id { get; set; }
        public int AccountId { get; set; }
        public int CurrencyId { get; set; }
        public string DocumentType { get; set; }
        public int? TransactionNo { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public decimal? MainTransition { get; set; }
        public IList<MasterGeneralLedger> ChildMasterGeneralLedger { get; set; }
        public int? GroupId { get; set; }

        public PaymentTerm()
        {
            ChildMasterGeneralLedger = new List<MasterGeneralLedger>();
        }
    }
}

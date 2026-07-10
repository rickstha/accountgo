using Core.Domain.Financials;
using Core.Domain.Purchases;
using Core.Domain.Sales;
using Core.Domain.TaxSystem;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Core.Domain.Items
{
    [Table("Item")]
    public partial class Item : BaseEntity
    {
        public Item()
        {
            SalesInvoiceLines = new HashSet<SalesInvoiceLine>();
            PurchaseOrderLines = new HashSet<PurchaseOrderLine>();
            PurchaseReceiptLines = new HashSet<PurchaseReceiptLine>();
            PurchaseInvoiceLines = new HashSet<PurchaseInvoiceLine>();
            InventoryControlJournals = new HashSet<InventoryControlJournal>();
        }

        public int? ItemCategoryId { get; set; }
        public int? SmallestMeasurementId { get; set; }
        public int? SellMeasurementId { get; set; }
        public int? PurchaseMeasurementId { get; set; }
        public int? CustomerDetailMain { get; set; }
        public int? PreferredVendorId { get; set; }
        public int? ItemTaxGroupId { get; set; }
        public int? SalesAccountId { get; set; }
        public int? InventoryAccountId { get; set; }
        public int? CostOfGoodsSoldAccountId { get; set; }
        public int? InventoryAdjustmentAccountId { get; set; }

        [MaxLength(50)]
        public string No { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; }

        [Required]
        [MaxLength(255)]
        public string Description { get; set; }

        public string PurchaseDescription { get; set; }

        public string SellDescription { get; set; }

        public decimal? Cost { get; set; }

        public decimal? Price { get; set; }

        public virtual ItemCategory ItemCategory { get; set; }

        public virtual ItemTaxGroup ItemTaxGroup { get; set; }

        public virtual Vendor PreferredVendor { get; set; }

        public virtual Account InventoryAccount { get; set; }

        public virtual Account SalesAccount { get; set; }

        public virtual Account CostOfGoodsSoldAccount { get; set; }

        public virtual Account InventoryAdjustmentAccount { get; set; }

        public virtual Measurement SmallestMeasurement { get; set; }

        public virtual Measurement SellMeasurement { get; set; }

        public virtual Measurement PurchaseMeasurement { get; set; }

        public virtual ICollection<SalesInvoiceLine> SalesInvoiceLines { get; set; }

        public virtual ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; }

        public virtual ICollection<PurchaseReceiptLine> PurchaseReceiptLines { get; set; }

        public virtual ICollection<PurchaseInvoiceLine> PurchaseInvoiceLines { get; set; }

        public virtual ICollection<InventoryControlJournal> InventoryControlJournals { get; set; }

        #region NotMapped

        [NotMapped]
        public decimal ItemTaxAmountOutput => ComputeItemTaxAmountOutput();

        private decimal ComputeItemTaxAmountOutput()
        {
            if (Price == null ||
                ItemTaxGroup?.ItemTaxGroupTax == null)
            {
                return 0;
            }

            decimal totalItemTaxAmount = 0;

            foreach (var itemTaxGroup in ItemTaxGroup.ItemTaxGroupTax)
            {
                if (itemTaxGroup?.Tax == null)
                    continue;

                decimal rate = itemTaxGroup.Tax.Rate;

                if (rate <= -100)
                    continue;

                decimal salesPrice =
                    Price.Value / (1 + (rate / 100));

                decimal taxAmount =
                    salesPrice * (rate / 100);

                totalItemTaxAmount += taxAmount;
            }

            return totalItemTaxAmount;
        }

        [NotMapped]
        public decimal ItemTaxAmountInput => ComputeItemTaxAmountInput();

        private decimal ComputeItemTaxAmountInput()
        {
            if (Cost == null ||
                ItemTaxGroup?.ItemTaxGroupTax == null)
            {
                return 0;
            }

            decimal totalItemTaxAmount = 0;

            foreach (var itemTaxGroup in ItemTaxGroup.ItemTaxGroupTax)
            {
                if (itemTaxGroup?.Tax == null)
                    continue;

                decimal taxAmount =
                    Cost.Value * (itemTaxGroup.Tax.Rate / 100);

                totalItemTaxAmount += taxAmount;
            }

            return totalItemTaxAmount;
        }

        #endregion

        public decimal ComputeDiscountedPrice(decimal discount = 0)
        {
            if (Price == null)
                return 0;

            if (discount <= 0)
                return Price.Value;

            return Price.Value -
                   ((discount / 100) * Price.Value);
        }

        public decimal ComputeQuantityOnHand()
        {
            if (InventoryControlJournals == null)
                return 0;

            decimal inQty = 0;
            decimal outQty = 0;

            foreach (var journal in InventoryControlJournals)
            {
                if (journal == null)
                    continue;

                if (!journal.IsReverse)
                {
                    inQty += journal.INQty ?? 0;
                    outQty += journal.OUTQty ?? 0;
                }
            }

            return inQty - outQty;
        }

        public bool GLAccountsValidated()
        {
            return CostOfGoodsSoldAccount != null
                && InventoryAccount != null
                && InventoryAdjustmentAccount != null
                && SalesAccount != null;
        }
    }
}
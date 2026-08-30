using Core.Domain.Items;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Purchases
{
    [Table("PurchaseInvoiceLine")]
    public partial class PurchaseInvoiceLine : BaseEntity
    {
        public int PurchaseInvoiceHeaderId { get; set; }
        public int ItemId { get; set; }
        public int MeasurementId { get; set; }
        public int? InventoryControlJournalId { get; set; }
        public int? PurchaseOrderLineId { get; set; }
        public decimal Quantity { get; set; }
        public decimal? ReceivedQuantity { get; set; }
        public decimal? Cost { get; set; }
        public decimal? Discount { get; set; }
        public decimal Amount { get; set; }

        public virtual PurchaseInvoiceHeader PurchaseInvoiceHeader { get; set; }
        public virtual Item Item { get; set; }
        public virtual Measurement Measurement { get; set; }
        public virtual InventoryControlJournal InventoryControlJournal { get; set; }
        public virtual PurchaseOrderLine PurchaseOrderLine { get; set; }

        [NotMapped]
        public decimal LineTaxAmount
        {
            get { return ComputeLineTaxAmount(); }
        }

        [NotMapped]
        public decimal TotalTaxAmount
        {
            get
            {
                if (PurchaseInvoiceHeader == null ||
                    PurchaseInvoiceHeader.PurchaseInvoiceLines == null)
                {
                    return LineTaxAmount;
                }

                decimal totalTaxAmount = 0m;

                foreach (var line in PurchaseInvoiceHeader.PurchaseInvoiceLines)
                {
                    totalTaxAmount += line.LineTaxAmount;
                }

                return totalTaxAmount;
            }
        }

        private decimal ComputeLineTaxAmount()
        {
            if (Item == null ||
                Item.ItemTaxGroup == null ||
                Item.ItemTaxGroup.ItemTaxGroupTax == null)
            {
                return 0m;
            }

            decimal taxAmount = 0m;
            decimal lineAmount = Quantity * (Cost ?? 0m);

            foreach (var tax in Item.ItemTaxGroup.ItemTaxGroupTax)
            {
                if (tax.Tax != null)
                {
                    taxAmount += (tax.Tax.Rate / 100m) * lineAmount;
                }
            }

            return taxAmount;
        }
    }
}
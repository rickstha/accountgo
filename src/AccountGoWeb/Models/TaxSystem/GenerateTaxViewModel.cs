namespace AccountGoWeb.Models.TaxSystem
{
    public class GenerateTaxViewModel
    {
        public int CustomerId { get; set; }
        public int ItemId { get; set; }
        public decimal Amount { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Discount { get; set; }

        public decimal TaxAmount { get; set; }
        public decimal TotalAmountAfterTax { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ItemDescription { get; set; } = string.Empty;
    }
}

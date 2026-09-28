namespace Dto.Common
{
    public class PaymentTerm : BaseDto
    {
        public string Description { get; set; } = string.Empty;
        public int? DueAfterDays { get; set; }
        public bool IsActive { get; set; }
        public int PaymentType { get; set; }
    }
}

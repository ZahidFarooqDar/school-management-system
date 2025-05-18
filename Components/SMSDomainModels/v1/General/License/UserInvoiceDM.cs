using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SMSDomainModels.Foundation.Base;

namespace SMSDomainModels.v1.General.License
{
    public class UserInvoiceDM : SMSDomainModelBase<int>
    {
        [Required]
        public string StripeInvoiceId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime StartDateUTC { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime ExpiryDateUTC { get; set; }
        public double DiscountInPercentage { get; set; }
        public decimal ActualPaidPrice { get; set; }
        public long AmountDue { get; set; }
        public long AmountRemaining { get; set; }
        public string Currency { get; set; }
        [StringLength(50)]
        public string StripeCustomerId { get; set; }

        [ForeignKey(nameof(UserLicenseDetails))]
        public int UserLicenseDetailsId { get; set; }
        public virtual UserLicenseDetailsDM UserLicenseDetails { get; set; }
    }
}

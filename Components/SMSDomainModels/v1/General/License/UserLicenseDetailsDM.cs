using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using SMSDomainModels.Enums;
using SMSDomainModels.AppUser;
using SMSDomainModels.Foundation.Base;

namespace SMSDomainModels.v1.General.License
{
    public class UserLicenseDetailsDM : SMSDomainModelBase<int>
    {
        [StringLength(100), DefaultValue("")]
        public string? StripeCustomerId { get; set; }
        [StringLength(100), DefaultValue("")]
        public string? StripeSubscriptionId { get; set; }
        [StringLength(100), DefaultValue("")]
        public string? StripePriceId { get; set; }
        [StringLength(50), DefaultValue("")]
        public string? ProductName { get; set; }
        [StringLength(50), DefaultValue("")]
        public string? SubscriptionPlanName { get; set; }
        [StringLength(100), DefaultValue("")]
        public string? StripeProductId { get; set; }
        public int ValidityInDays { get; set; }
        public double DiscountInPercentage { get; set; }
        public decimal ActualPaidPrice { get; set; }
        [StringLength(10), DefaultValue("")]
        public string? Currency { get; set; }
        [StringLength(30), DefaultValue("")]
        public string? Status { get; set; }
        [DefaultValue(true)]
        public bool IsSuspended { get; set; }
        [DefaultValue(true)]
        public bool IsCancelled { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? CancelAt { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime? CancelledOn { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? StartDateUTC { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ExpiryDateUTC { get; set; }

        [ForeignKey(nameof(LicenseType))]
        public int? LicenseTypeId { get; set; }
        public virtual LicenseTypeDM? LicenseType { get; set; }


        [ForeignKey(nameof(ClientUser))]
        public int ClientUserId { get; set; }
        public virtual ClientUserDM ClientUser { get; set; }

        public PaymentMethodDM PaymentMethod { get; set; }

        public virtual ICollection<UserInvoiceDM> UserInvoices { get; set; }
    }
}

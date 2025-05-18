using SMSServiceModels.Enums;
using SMSServiceModels.Foundation.Base;

namespace SMSServiceModels.v1.General.License
{
    public class LicenseTypeSM : SMSServiceModelBase<int>
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public int ValidityInDays { get; set; }
        public double Amount { get; set; }
        public string LicenseTypeCode { get; set; }
        public string StripePriceId { get; set; }
        public bool IsPredefined { get; set; }
        public LicensePlanSM LicensePlan { get; set; }
        public RoleTypeSM ValidFor { get; set; }
    }
}

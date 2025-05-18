using SMSServiceModels.Foundation.Base;

namespace SMSServiceModels.Client
{
    public class ClientCompanyDetailSM : SMSServiceModelBase<int>
    {
        public string CompanyCode { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ContactEmail { get; set; }
        public string CompanyMobileNumber { get; set; }
        public string CompanyWebsite { get; set; }
        public string CompanyLogoPath { get; set; }
        public DateTime CompanyDateOfEstablishment { get; set; }
    }
}

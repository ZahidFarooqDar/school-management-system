using SMSServiceModels.Enums;

namespace SMSServiceModels.Foundation.Token
{
    public class TokenRequestSM : TokenRequestRoot
    {
        public string CompanyCode { get; set; }
        public RoleTypeSM RoleType { get; set; }
    }
}

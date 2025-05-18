using SMSServiceModels.Enums;
using SMSServiceModels.Foundation.Base;

namespace SMSServiceModels.AppUser
{
    public class ExternalUserSM : SMSServiceModelBase<int>
    {
        public string RefreshToken { get; set; }
        public int ClientUserId { get; set; }

        public ExternalUserTypeSM ExternalUserType { get; set; }
    }
}

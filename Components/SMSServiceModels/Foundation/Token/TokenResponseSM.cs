using SMSServiceModels.AppUser.Login;

namespace SMSServiceModels.Foundation.Token
{
    public class TokenResponseSM : TokenResponseRoot
    {
        public LoginUserSM LoginUserDetails { get; set; }
        public int ClientCompanyId { get; set; }
    }
}

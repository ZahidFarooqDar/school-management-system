using SMSServiceModels.AppUser.Login;
using SMSServiceModels.Enums;

namespace SMSServiceModels.AppUser
{
    public class ClientUserSM : LoginUserSM
    {
        public GenderSM Gender { get; set; }
        public string PersonalEmailId { get; set; }
        public int? ClientCompanyDetailId { get; set; }

    }
}

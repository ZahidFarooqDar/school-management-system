using SMSServiceModels.AppUser.Login;
using SMSServiceModels.Enums;

namespace SMSServiceModels.v1.Teachers
{
    public class TeacherSM : LoginUserSM
    {
        public GenderSM Gender { get; set; }
        public int? ClientUserId { get; set; }

    }
}

using SMSServiceModels.Enums;
using SMSServiceModels.Foundation.Base;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;

namespace SMSServiceModels.AppUser.Login
{
    public class LoginUserSM : SMSServiceModelBase<int>
    {
        public LoginUserSM()
        {
        }
        public RoleTypeSM RoleType { get; set; }
        public string LoginId { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }

        [IgnorePropertyOnWrite(AutoMapConversionType.Dm2SmOnly)]
        public string PasswordHash { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfilePicturePath { get; set; }

        public bool IsPhoneNumberConfirmed { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public LoginStatusSM LoginStatus { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}

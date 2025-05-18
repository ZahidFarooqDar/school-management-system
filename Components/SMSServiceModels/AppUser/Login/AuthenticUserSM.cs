using Microsoft.AspNetCore.Identity;
using SMSServiceModels.Enums;

namespace SMSServiceModels.AppUser.Login
{
    public class AuthenticUserSM : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public RoleTypeSM Role { get; set; }
    }
}

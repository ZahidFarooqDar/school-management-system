using Microsoft.AspNetCore.Authentication;

namespace SMSFoundation.Security
{
    public class SMSAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        public string JwtTokenSigningKey { get; set; }
    }
}

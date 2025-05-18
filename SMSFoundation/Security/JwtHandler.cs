using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace SMSFoundation.Security
{
    public class JwtHandler
    {
        private readonly string _tokenIssuier;

        public JwtHandler(string issuer)
        {
            _tokenIssuier = issuer;
        }
        public async Task<string> ProtectAsync(string encryptionKey, IEnumerable<Claim> lstClaims, DateTimeOffset issueDateOffset, DateTimeOffset expiryDateOffset, string audience)
        {
            if (string.IsNullOrWhiteSpace(encryptionKey) || encryptionKey.Length < 32)
            {
                throw new Exception("Key length must be at least 32 characters");
            }

            var keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _tokenIssuier,
                audience,
                lstClaims,
                issueDateOffset.UtcDateTime,
                expiryDateOffset.UtcDateTime,
                signingCredentials);

            return await Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
        }    
        public async Task<string> UnprotectToJwtStringAsync(string decryptionKey, string token)
        {
            if (decryptionKey.Length < 32)
            {
                throw new Exception("Key length should me more the 32 characters");
            }

            return token;
        }

        public async Task<JwtSecurityToken> UnprotectAsync(string decryptionKey, string token)
        {
            string text = await UnprotectToJwtStringAsync(decryptionKey, token);
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new Exception("Could not unprotect token.");
            }

            return new JwtSecurityTokenHandler().ReadToken(text) as JwtSecurityToken;
        }
    }
}

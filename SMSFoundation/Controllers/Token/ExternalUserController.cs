using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SMSBAL.AppUsers;
using SMSBAL.ExceptionHandler;
using SMSBAL.Token;
using SMSConfig.Configuration;
using SMSDAL.Context;
using SMSFoundation.Security;
using SMSServiceModels.AppUser;
using SMSServiceModels.Enums;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.Foundation.Token;
using System.Security.Claims;
using System.Text.Json;

namespace SMSFoundation.Controllers.Token
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ExternalUserController : ControllerBase
    {
        #region Properties
        private readonly APIConfiguration _configuration;
        private readonly JwtHandler _jwtHandler;
        private readonly TokenProcess _tokenProcess;
        private readonly ClientUserProcess _clientUserProcess;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ExternalUserProcess _externalUserProcess;
        private readonly ApiDbContext _apiDbContext;
        #endregion Properties

        #region Constructor
        public ExternalUserController(APIConfiguration config, TokenProcess tokenProcess, ApiDbContext apiDbContext, ClientUserProcess clientUserProcess,
            IHttpClientFactory httpClientFactory, JwtHandler jwtHandler, IHttpContextAccessor httpContextAccessor, ExternalUserProcess googleAuthProcess)
        {
            _configuration = config;
            _jwtHandler = jwtHandler;
            _tokenProcess = tokenProcess;
            _clientUserProcess = clientUserProcess;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _externalUserProcess = googleAuthProcess;
            _apiDbContext = apiDbContext;
        }

        #endregion Constructor

        #region Additional Method

        #endregion Additional Method

        #region Google Authentication

        #region Google SignUp

        [HttpGet("googlesignup")]
        public async Task<ActionResult<ApiResponse<TokenResponseSM>>> GoogleSignUp(string idToken, string companyCode)
        {
            if (string.IsNullOrEmpty(idToken))
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Id Token cannot be null or empty", "Id Token cannot be null or empty");
            }
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
            var existingClientUser = await _clientUserProcess.GetClientUserByEmail(payload.Email);
            string base64Picture;
            try
            {
                using (var httpClient = _httpClientFactory.CreateClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(payload.Picture);
                    base64Picture = Convert.ToBase64String(imageBytes);
                }
            }
            catch (Exception ex)
            {
                base64Picture = null;
            }
           
            if (existingClientUser != null)
            {
                var externalGoogleUser = new ExternalUserSM()
                {
                    ClientUserId = existingClientUser.Id,
                    RefreshToken = idToken,
                    ExternalUserType = ExternalUserTypeSM.Google
                };
                // confirm email as its from its google account
                if (!existingClientUser.IsEmailConfirmed)
                {
                    if (existingClientUser.ProfilePicturePath.IsNullOrEmpty())
                    {
                        existingClientUser.ProfilePicturePath = base64Picture;
                    }
                    existingClientUser = await _clientUserProcess.UpdateClientUser(existingClientUser.Id, existingClientUser,true);
                }
                var externalUser = await _externalUserProcess.GetExternalUserByClientUserIdandTypeAsync(existingClientUser.Id, ExternalUserTypeSM.Google);
                if (externalUser == null)
                {
                    externalUser = await _externalUserProcess.AddExternalUser(externalGoogleUser);
                }
                if (externalUser != null) // external user added / is already available
                    throw new SMSException(ApiErrorTypeSM.Success_NoLog, "Google Login Details Already Exist...Sign in Instead", "Google Login Details Already Exist. Please use login.");
                else // error in adding external user
                    throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Internal error occured, try again after sometime. If problem persists, contact support.", $"Error in adding external user with id token {idToken}");
            }
           
            var newUser = new ClientUserSM()
            {
                LoginId = payload.Email,
                EmailId = payload.Email,
                FirstName = payload.GivenName,
                LastName = payload.FamilyName,
                IsEmailConfirmed = true,
                ProfilePicturePath = base64Picture,
                Gender = GenderSM.Unknown,
            };
            var externalUserType = ExternalUserTypeSM.Google;
            ClientUserSM newUserSM = await _externalUserProcess.AddClientUserandExternalUserDetails(newUser, idToken, companyCode, externalUserType);
            if (newUserSM == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Error in saving your details... Try Again", "Error in saving your details... Try Again");
            }
            else
            {
                return await CreateTokenForUser(newUserSM, companyCode);
            }
        }

        #endregion Google SignUp

        #region Login 

        [HttpGet("googlelogin")]
        public async Task<ActionResult<ApiResponse<TokenResponseSM>>> GoogleLogin(string idToken, string companyCode)
        {
            if (string.IsNullOrEmpty(idToken))
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Id Token cannot be null or empty", "Id Token cannot be null or empty");
            }
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
            var existingClientUser = await _clientUserProcess.GetClientUserByEmail(payload.Email);
            string base64Picture;
            try
            {
                using (var httpClient = _httpClientFactory.CreateClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(payload.Picture);
                    base64Picture = Convert.ToBase64String(imageBytes);
                }
            }
            catch (Exception ex)
            {
                base64Picture = null;
            }

            if (existingClientUser != null)
            {
                var externalGoogleUser = new ExternalUserSM()
                {
                    ClientUserId = existingClientUser.Id,
                    RefreshToken = idToken,
                    ExternalUserType = ExternalUserTypeSM.Google
                };
                // confirm email as its from its google account
                if (!existingClientUser.IsEmailConfirmed)
                {
                    if (existingClientUser.ProfilePicturePath.IsNullOrEmpty())
                    {
                        existingClientUser.ProfilePicturePath = base64Picture;
                    }
                    existingClientUser = await _clientUserProcess.UpdateClientUser(existingClientUser.Id, existingClientUser,true);
                }
                var externalUser = await _externalUserProcess.GetExternalUserByClientUserIdandTypeAsync(existingClientUser.Id, ExternalUserTypeSM.Google);
                if (externalUser == null)
                {
                    externalUser = await _externalUserProcess.AddExternalUser(externalGoogleUser);
                }
                if (externalUser != null) // external user added / is already available
                                          // generate token
                    return await CreateTokenForUser(existingClientUser, companyCode);
                //throw new CodeVisionException(ApiErrorTypeSM.Success_NoLog, "Google Login Details Already Exist...Sign in Instead", "Google Login Details Already Exist. Please use login.");
                else // error in adding external user
                    throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Internal error occured, try again after sometime. If problem persists, contact support.", $"Error in adding external user with id token {idToken}");
            }
            
            var newUser = new ClientUserSM()
            {
                LoginId = payload.Email,
                EmailId = payload.Email,
                FirstName = payload.GivenName,
                LastName = payload.FamilyName,
                IsEmailConfirmed = true,
                ProfilePicturePath = base64Picture,
                Gender = GenderSM.Unknown,
            };
            var externalUserType = ExternalUserTypeSM.Google;
            ClientUserSM newUserSM = await _externalUserProcess.AddClientUserandExternalUserDetails(newUser, idToken, companyCode, externalUserType);
            if (newUserSM == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Error in saving your details... Try Again", "Error in saving your details... Try Again");
            }
            else
            {
                return await CreateTokenForUser(newUserSM, companyCode);
            }
        }

        #endregion Login 

        #endregion

        #region Facebook Authentication

        #region Facebook Sign Up

        [HttpGet("facebooksignup")]
        public async Task<ActionResult<ApiResponse<TokenResponseSM>>> FacebookSignUp(string idToken, string companyCode)
        {
            if (string.IsNullOrEmpty(idToken))
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Id Token cannot be null or empty", "Id Token cannot be null or empty");
            }

            var httpClient = _httpClientFactory.CreateClient();

            var userInfoUrl = $"https://graph.facebook.com/me?fields=id,name,email,first_name,last_name,picture&access_token={idToken}";
            var userInfoResponse = await httpClient.GetStringAsync(userInfoUrl);

            using var userInfoDoc = JsonDocument.Parse(userInfoResponse);
            var root = userInfoDoc.RootElement;

            // Check if the necessary properties exist before accessing them
            string userId = root.TryGetProperty("id", out var idProperty) ? idProperty.GetString() : null;
            string emailId = root.TryGetProperty("email", out var emailProperty) ? emailProperty.GetString() : null;
            string firstName = root.TryGetProperty("first_name", out var firstNameProperty) ? firstNameProperty.GetString() : null;
            string lastName = root.TryGetProperty("last_name", out var lastNameProperty) ? lastNameProperty.GetString() : null;
            string pictureUrl = root.TryGetProperty("picture", out var pictureProperty) &&
                                pictureProperty.TryGetProperty("data", out var dataProperty) ?
                                dataProperty.GetProperty("url").GetString() : null;

            if (emailId.IsNullOrEmpty())
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log,
                    "We couldn't retrieve your Facebook email address. Please ensure that your Facebook account is properly linked and try again.",
                    "We couldn't retrieve your Facebook email address. Please ensure that your Facebook account is properly linked and try again.");
            }

            var existingClientUser = await _clientUserProcess.GetClientUserByEmail(emailId);

            string base64Picture;
            try
            {
                using (var httpClients = _httpClientFactory.CreateClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(pictureUrl);
                    base64Picture = Convert.ToBase64String(imageBytes);
                }
            }
            catch (Exception ex)
            {
                base64Picture = null;
            }

            if (existingClientUser != null)
            {
                var externalGoogleUser = new ExternalUserSM()
                {
                    ClientUserId = existingClientUser.Id,
                    RefreshToken = idToken,
                    ExternalUserType = ExternalUserTypeSM.Facebook
                };
                // confirm email as its from its google account
                if (!existingClientUser.IsEmailConfirmed)
                {
                    if (existingClientUser.ProfilePicturePath.IsNullOrEmpty())
                    {
                        existingClientUser.ProfilePicturePath = base64Picture;
                    }
                    existingClientUser = await _clientUserProcess.UpdateClientUser(existingClientUser.Id, existingClientUser,true);
                }
                var externalUser = await _externalUserProcess.GetExternalUserByClientUserIdandTypeAsync(existingClientUser.Id, ExternalUserTypeSM.Google);
                if (externalUser == null)
                {
                    externalUser = await _externalUserProcess.AddExternalUser(externalGoogleUser);
                }
                if (externalUser != null) // external user added / is already available

                    throw new SMSException(ApiErrorTypeSM.Success_NoLog, "Google Login Details Already Exist...Sign in Instead", "Google Login Details Already Exist. Please use login.");
                else // error in adding external user
                    throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Internal error occured, try again after sometime. If problem persists, contact support.", $"Error in adding external user with id token {idToken}");
            }
            
            var newUser = new ClientUserSM()
            {
                LoginId = emailId,
                EmailId = emailId,
                FirstName = string.IsNullOrEmpty(firstName) ? emailId.Split('@')[0] : firstName,
                LastName = lastName,
                IsEmailConfirmed = true,
                ProfilePicturePath = base64Picture,
                Gender = GenderSM.Unknown,
            };
            var externalUserType = ExternalUserTypeSM.Facebook;
            ClientUserSM newUserSM = await _externalUserProcess.AddClientUserandExternalUserDetails(newUser, idToken, companyCode, externalUserType);
            if (newUserSM == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Error in saving your details... Try Again", "Error in saving your details... Try Again");
            }
            else
            {
                return await CreateTokenForUser(newUserSM, companyCode);
            }
        }

        #endregion Facebook Sign Up

        #region Login 

        [HttpGet("facebooklogin")]
        public async Task<ActionResult<ApiResponse<TokenResponseSM>>> FacebookbLogin(string idToken, string companyCode)
        {
            if (string.IsNullOrEmpty(idToken))
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Id Token cannot be null or empty", "Id Token cannot be null or empty");
            }
            var httpClient = _httpClientFactory.CreateClient();

            var userInfoUrl = $"https://graph.facebook.com/me?fields=id,name,email,first_name,last_name,picture&access_token={idToken}";
            var userInfoResponse = await httpClient.GetStringAsync(userInfoUrl);

            using var userInfoDoc = JsonDocument.Parse(userInfoResponse);
            var root = userInfoDoc.RootElement;

            // Check if the necessary properties exist before accessing them
            string userId = root.TryGetProperty("id", out var idProperty) ? idProperty.GetString() : null;
            string emailId = root.TryGetProperty("email", out var emailProperty) ? emailProperty.GetString() : null;
            string firstName = root.TryGetProperty("first_name", out var firstNameProperty) ? firstNameProperty.GetString() : null;
            string lastName = root.TryGetProperty("last_name", out var lastNameProperty) ? lastNameProperty.GetString() : null;
            string pictureUrl = root.TryGetProperty("picture", out var pictureProperty) &&
                                pictureProperty.TryGetProperty("data", out var dataProperty) ?
                                dataProperty.GetProperty("url").GetString() : null;

            if (emailId.IsNullOrEmpty())
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log,
                    "We couldn't retrieve your Facebook email address. Please ensure that your Facebook account is properly linked and try again.",
                    "We couldn't retrieve your Facebook email address. Please ensure that your Facebook account is properly linked and try again.");
            }

            var existingClientUser = await _clientUserProcess.GetClientUserByEmail(emailId);
            string base64Picture;
            try
            {
                using (var httpClients = _httpClientFactory.CreateClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(pictureUrl);
                    base64Picture = Convert.ToBase64String(imageBytes);
                }
            }
            catch (Exception ex)
            {
                base64Picture = null;
            }
            if (existingClientUser != null)
            {
                var externalFbUser = new ExternalUserSM()
                {
                    ClientUserId = existingClientUser.Id,
                    RefreshToken = idToken,
                    ExternalUserType = ExternalUserTypeSM.Facebook
                };
                // confirm email as its from its google account
                if (!existingClientUser.IsEmailConfirmed)
                {
                    if (existingClientUser.ProfilePicturePath.IsNullOrEmpty())
                    {
                        existingClientUser.ProfilePicturePath = base64Picture;
                    }
                    existingClientUser = await _clientUserProcess.UpdateClientUser(existingClientUser.Id, existingClientUser, true);
                }
                var externalUser = await _externalUserProcess.GetExternalUserByClientUserIdandTypeAsync(existingClientUser.Id, ExternalUserTypeSM.Facebook);
                if (externalUser == null)
                {
                    externalUser = await _externalUserProcess.AddExternalUser(externalFbUser);
                }
                if (externalUser != null) // external user added / is already available
                                          // generate token
                    return await CreateTokenForUser(existingClientUser, companyCode);
                else // error in adding external user
                    throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Internal error occured, try again after sometime. If problem persists, contact support.", $"Error in adding external user with id token {idToken}");
            }
            
            var newUser = new ClientUserSM()
            {
                LoginId = emailId,
                EmailId = emailId,
                FirstName = string.IsNullOrEmpty(firstName) ? emailId.Split('@')[0] : firstName,
                LastName = lastName,
                IsEmailConfirmed = true,
                ProfilePicturePath = base64Picture,
                Gender = GenderSM.Unknown,
            };
            var externalUserType = ExternalUserTypeSM.Facebook;
            ClientUserSM newUserSM = await _externalUserProcess.AddClientUserandExternalUserDetails(newUser, idToken, companyCode, externalUserType);
            if (newUserSM == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Error in saving your details... Try Again", "Error in saving your details... Try Again");
            }
            else
            {
                return await CreateTokenForUser(newUserSM, companyCode);
            }
        }

        #endregion Login 

        #endregion

        #region Apple Authentication

        #endregion

        #region PrivateFunctions

        private async Task<ActionResult<ApiResponse<TokenResponseSM>>> CreateTokenForUser(ClientUserSM clientUserSM, string companyCode)
        {
            ICollection<Claim> claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name,clientUserSM.LoginId),
                new Claim(ClaimTypes.Role,clientUserSM.RoleType.ToString()),
                new Claim(ClaimTypes.GivenName,$"{clientUserSM.FirstName} {clientUserSM.MiddleName} {clientUserSM.LastName}" ),
                new Claim(ClaimTypes.Email,clientUserSM.EmailId),
                new Claim(DomainConstantsRoot.ClaimsRoot.Claim_DbRecordId,clientUserSM.Id.ToString())
            };
            if (!string.IsNullOrWhiteSpace(companyCode))
            {
                var compId = await _apiDbContext.ClientCompanyDetails.Where(x => x.CompanyCode == companyCode).Select(x => x.Id).FirstOrDefaultAsync();
                if (compId != null || compId != 0)
                {
                    clientUserSM.ClientCompanyDetailId = compId;
                }
                claims.Add(new Claim(DomainConstantsRoot.ClaimsRoot.Claim_ClientCode, companyCode));
                claims.Add(new Claim(DomainConstantsRoot.ClaimsRoot.Claim_ClientId, clientUserSM.ClientCompanyDetailId.ToString()));
            }
            var expiryDate = DateTime.Now.AddDays(_configuration.DefaultTokenValidityDays);
            var token = await _jwtHandler.ProtectAsync(_configuration.JwtTokenSigningKey, claims, new DateTimeOffset(DateTime.Now), new DateTimeOffset(expiryDate), "SMS");
            var tokenResponse = new TokenResponseSM()
            {
                AccessToken = token,
                LoginUserDetails = clientUserSM,
                ExpiresUtc = expiryDate,
                ClientCompanyId = (int)clientUserSM.ClientCompanyDetailId,
            };
            return Ok(ModelConverter.FormNewSuccessResponse(tokenResponse));
        }
        #endregion
    }
}

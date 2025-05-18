using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SMSBAL.AppUsers;
using SMSBAL.Token;
using SMSConfig.Configuration;
using SMSFoundation.Controllers.Base;
using SMSFoundation.Security;
using SMSServiceModels.AppUser.Login;
using SMSServiceModels.Enums;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.Foundation.Token;
using System.Security.Claims;

namespace SMSFoundation.Controllers.Token
{
    public partial class TokenController : ApiControllerRoot
    {
        #region Properties

        private readonly TokenProcess _tokenProcess;
        private readonly JwtHandler _jwtHandler;
        private readonly APIConfiguration _apiConfiguration;
        private readonly ClientUserProcess _clientUserProcess;

        #endregion Properties

        #region Constructor
        public TokenController(TokenProcess TokenProcess, JwtHandler jwtHandler, APIConfiguration aPIConfiguration, ClientUserProcess clientUserProcess)
        {
            _tokenProcess = TokenProcess;
            _jwtHandler = jwtHandler;
            _apiConfiguration = aPIConfiguration;
            _clientUserProcess = clientUserProcess;
        }
        #endregion Constructor

        #region Validate Login And Generate Token 


        [HttpPost]
        [Route("ValidateLoginAndGenerateToken")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<TokenResponseSM>>> ValidateLoginAndGenerateToken(ApiRequest<TokenRequestSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_Log));
            }

            if (string.IsNullOrWhiteSpace(innerReq.LoginId) || /*string.IsNullOrWhiteSpace(innerReq.Password) ||*/ innerReq.RoleType == RoleTypeSM.Unknown)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessages.Display_InvalidRequiredDataInputs));
            }

            #endregion Check Request

            (LoginUserSM userSM, int compId) = await _tokenProcess.ValidateLoginAndGenerateToken(innerReq);
            if (userSM == null)
            {
                return NotFound(ModelConverter.FormNewErrorResponse("Invalid Credentials",
                    ApiErrorTypeSM.InvalidInputData_Log));
            }
            else if (userSM.LoginStatus == LoginStatusSM.Disabled && userSM.RoleType != RoleTypeSM.CompanyAutomation)
            {
                return Unauthorized(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessages.Display_UserDisabled, ApiErrorTypeSM.Access_Denied_Log));
            }
            else if (userSM.LoginStatus == LoginStatusSM.PasswordResetRequired)
            {
                return Unauthorized(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessages.Display_UserPasswordResetRequired, ApiErrorTypeSM.Access_Denied_Log));
            }
            else if (!userSM.IsEmailConfirmed)
            {
                return Unauthorized(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessages.Display_UserNotVerified, ApiErrorTypeSM.Access_Denied_Log));
            }
            else
            {
                ICollection<Claim> claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.Name,innerReq.LoginId),
                    new Claim(ClaimTypes.Role,userSM.RoleType.ToString()),
                    new Claim(ClaimTypes.GivenName,userSM.FirstName + " " + userSM.MiddleName + " " +userSM.LastName ),
                    new Claim(ClaimTypes.Email,userSM.EmailId),
                    new Claim(DomainConstantsRoot.ClaimsRoot.Claim_DbRecordId,userSM.Id.ToString())
                };
                if (compId != default)
                {
                    claims.Add(new Claim(DomainConstantsRoot.ClaimsRoot.Claim_ClientCode, innerReq.CompanyCode));
                    claims.Add(new Claim(DomainConstantsRoot.ClaimsRoot.Claim_ClientId, compId.ToString()));
                }
                var expiryDate = DateTime.Now.AddDays(_apiConfiguration.DefaultTokenValidityDays);

                //// creating object of DateTime 
                //DateTime date1 = DateTime.Now;

                //// creating object of DateTime 
                //DateTime date2 = new DateTime(2025, 12,
                //                         31, 11, 59, 59);
                //var x = date2.Subtract(date1);
                //expiryDate = DateTime.Now.AddDays(x.Days);
                var token = await _jwtHandler.ProtectAsync(_apiConfiguration.JwtTokenSigningKey, claims, new DateTimeOffset(DateTime.Now), new DateTimeOffset(expiryDate), "SMS");
                // here if user is derived class, all properties will be sent
                var tokenResponse = new TokenResponseSM()
                {
                    AccessToken = token,
                    LoginUserDetails = userSM,
                    ExpiresUtc = expiryDate,
                    ClientCompanyId = compId
                };
                return Ok(ModelConverter.FormNewSuccessResponse(tokenResponse));
            }
        }

        #endregion Validate Login And Generate Token 

        #region Client Token

        [HttpGet]
        [Route("GenerateTokenFromSuperAdmin/{userId}")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<TokenResponseSM>>> ValidateLoginAndGenerateToken(int userId, int tokenValidityInDays)
        {
            #region Check Request

            #endregion Check Request

            (LoginUserSM userSM, int compId, string companyCode) = await _clientUserProcess.GetCompanyAutomationUserById(userId);
            if (userSM == null)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessages.Display_UserNotFound,
                    ApiErrorTypeSM.InvalidInputData_Log));
            }
            else
            {
                ICollection<Claim> claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.Name,userSM.LoginId),
                    new Claim(ClaimTypes.Role,userSM.RoleType.ToString()),
                    new Claim(ClaimTypes.GivenName,userSM.FirstName + " " + userSM.MiddleName + " " +userSM.LastName ),
                    new Claim(ClaimTypes.Email,userSM.EmailId),
                    new Claim(DomainConstantsRoot.ClaimsRoot.Claim_DbRecordId,userSM.Id.ToString())
                };
                if (compId != default)
                {
                    claims.Add(new Claim(DomainConstantsRoot.ClaimsRoot.Claim_ClientCode, companyCode));
                    claims.Add(new Claim(DomainConstantsRoot.ClaimsRoot.Claim_ClientId, compId.ToString()));
                }
                //var expiryDate = DateTime.Now.AddDays(_apiConfiguration.DefaultTokenValidityDays);
                var expiryDate = DateTime.Now.AddDays(tokenValidityInDays);
                var token = await _jwtHandler.ProtectAsync(_apiConfiguration.JwtTokenSigningKey, claims, new DateTimeOffset(DateTime.Now), new DateTimeOffset(expiryDate), "SMS");
                // here if user is derived class, all properties will be sent
                var tokenResponse = new TokenResponseSM()
                {
                    AccessToken = token,
                    LoginUserDetails = userSM,
                    ExpiresUtc = expiryDate,
                    ClientCompanyId = compId
                };
                return Ok(ModelConverter.FormNewSuccessResponse(tokenResponse));
            }
        }

        #endregion Client Token
    }
}

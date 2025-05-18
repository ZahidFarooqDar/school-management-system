using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.IdentityModel.Tokens;
using SMSBAL.AppUsers;
using SMSBAL.Foundation.Web;
using SMSConfig.Configuration;
using SMSFoundation.Controllers.Base;
using SMSFoundation.Security;
using SMSServiceModels.AppUser;
using SMSServiceModels.Enums;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;

namespace SMSFoundation.Controllers.AppUsers
{
    [Route("api/v1/[controller]")]
    public partial class ApplicationUserController : ApiControllerWithOdataRoot<ApplicationUserSM>
    {
        private readonly ApplicationUserProcess _applicationUserProcess;
        private readonly IWebHostEnvironment _environment;
        private readonly APIConfiguration _configuration;

        public ApplicationUserController(ApplicationUserProcess process, IWebHostEnvironment environment, APIConfiguration configuration)
            : base(process)
        {
            _applicationUserProcess = process;
            _environment = environment;
            _configuration = configuration;
        }

        #region Odata EndPoints

        [HttpGet]
        [Route("odata")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ApplicationUserSM>>>> GetAsOdata(ODataQueryOptions<ApplicationUserSM> oDataOptions)
        {
            //TODO: validate inputs here probably 
            var retList = await GetAsEntitiesOdata(oDataOptions);
            return Ok(ModelConverter.FormNewSuccessResponse(retList));
        }

        #endregion Odata EndPoints

        #region Get Endpoints

        [HttpGet("{skip}/{top}")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ApplicationUserSM>>>> GetAll(int skip, int top)
        {
            /* var passKey = _configuration.SuperAdminUserAdditionKey;
             if (passKey != key)
             {
                 return BadRequest(ModelConverter.FormNewErrorResponse("Access Denied, Pass Key is Wrong", ApiErrorTypeSM.Access_Denied_Log));
             }*/
            var listSM = await _applicationUserProcess.GetAllApplicationUsers(skip, top);
            return Ok(ModelConverter.FormNewSuccessResponse(listSM));
        }


        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<ApplicationUserSM>>> GetApplicationUserById(int id)
        {
            var singleSM = await _applicationUserProcess.GetApplicationUserById(id);
            if (singleSM != null)
            {
                return ModelConverter.FormNewSuccessResponse(singleSM);
            }
            else
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotFound, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        [HttpGet("mine")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin,ClientAdmin, BlogAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<ApplicationUserSM>>> GetMineApplicationUserDetails()
        {
            int currentUserRecordId = User.GetUserRecordIdFromCurrentUserClaims();
            if (currentUserRecordId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotInClaims));
            }
            var res = await _applicationUserProcess.GetApplicationUserById(currentUserRecordId);
            return ModelConverter.FormNewSuccessResponse(res);
        }


        #endregion Get Endpoints

        #region Count

        [HttpGet("count")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<IntResponseRoot>>> GetAllApplicationUsersCount()
        {
            var count = await _applicationUserProcess.GetAllApplicationUsersCountResponse();
            return Ok(ModelConverter.FormNewSuccessResponse(new IntResponseRoot(count)));
        }

        #endregion Count

        #region Add/Update Endpoints

        [HttpPost()]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<ApplicationUserSM>>> PostApplicationUser(string key, [FromBody] ApiRequest<ApplicationUserSM> apiRequest)
        {
            #region Check Request
            var passKey = _configuration.SuperAdminUserAdditionKey;
            if (passKey != key)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse("Access Denied", ApiErrorTypeSM.Access_Denied_Log));
            }

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            #endregion Check Request
            string roleType = User.GetUserRoleTypeFromCurrentUserClaims().ToString();
            if (roleType.IsNullOrEmpty())
            {
                return BadRequest(ModelConverter.FormNewErrorResponse("User Role Not found in Claims", ApiErrorTypeSM.Access_Denied_Log));
            }

            var addedSM = await _applicationUserProcess.AddApplicationUser(innerReq, roleType);
            if (addedSM != null)
            {
                return ModelConverter.FormNewSuccessResponse(addedSM);

            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

       

        [HttpPut("mine")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin, SuperAdmin, BlogAdmin")]
        public async Task<ActionResult<ApiResponse<ApplicationUserSM>>> Put([FromBody] ApiRequest<ApplicationUserSM> apiRequest)
        {
            #region Check Request
            /*var passKey = _configuration.SuperAdminUserAdditionKey;
            if (passKey != key)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse("Access Denied", ApiErrorTypeSM.Access_Denied_Log));
            }*/

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var id = User.GetUserRecordIdFromCurrentUserClaims();
            if (id <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotInClaims));
            }

            #endregion Check Request

            var updatedSM = await _applicationUserProcess.UpdateApplicationUser(id, innerReq);
            if (updatedSM != null)
            {
                return Ok(ModelConverter.FormNewSuccessResponse(updatedSM));
            }
            else
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        [HttpPut("login-details")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<ApplicationUserSM>>> UpdateDetailsForLogin(int userId, bool isEmailConfirmed, bool isPhoneNumberConfirmed, LoginStatusSM loginStatus)
        {
            #region Check Request
            if (userId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotInClaims));
            }

            #endregion Check Request

            var updatedSM = await _applicationUserProcess.UpdateDetailsForLoginPurpose(userId, isEmailConfirmed, isPhoneNumberConfirmed, loginStatus);
            if (updatedSM != null)
            {
                return Ok(ModelConverter.FormNewSuccessResponse(updatedSM));
            }
            else
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        #endregion Add/Update Endpoints

        #region Delete Endpoints

        
        [HttpDelete("mine/logo")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin,SystemAdmin")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> DeleteUserProfilePicture()
        {
            #region Check Request

            int userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
            { return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotInClaims)); }

            #endregion Check Request

            var resp = await _applicationUserProcess.DeleteProfilePictureById(userId, _environment.WebRootPath);
            if (resp != null && resp.DeleteResult)
                return Ok(ModelConverter.FormNewSuccessResponse(resp));
            else
                return NotFound(ModelConverter.FormNewErrorResponse(resp?.DeleteMessage, ApiErrorTypeSM.NoRecord_NoLog));
        }


        #endregion Delete Endpoints
    }
}
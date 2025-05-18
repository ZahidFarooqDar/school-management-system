using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using SMSBAL.Foundation.Web;
using SMSBAL.License;
using SMSFoundation.Controllers.Base;
using SMSFoundation.Security;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.v1.General.License;

namespace SMSFoundation.Controllers.License
{
    [Route("api/v1/[controller]")]
    public class UserLicenseDetailsController : ApiControllerWithOdataRoot<UserLicenseDetailsSM>
    {
        #region Properties

        private readonly UserLicenseDetailsProcess _userLicenseDetailsProcess;
        private readonly PaymentProcess _paymentProcess;

        #endregion Properties

        #region Constructor
        public UserLicenseDetailsController(UserLicenseDetailsProcess userLicenseDetailsProcess, PaymentProcess paymentProcess)
            : base(userLicenseDetailsProcess)
        {
            _userLicenseDetailsProcess = userLicenseDetailsProcess;
            _paymentProcess = paymentProcess;
        }


        #endregion Constructor

        #region Odata EndPoints

        [HttpGet]
        [Route("odata")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin,SuperAdmin")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserLicenseDetailsSM>>>> GetAsOdata(ODataQueryOptions<UserLicenseDetailsSM> oDataOptions)
        {
            //TODO: validate inputs here probably 
            var retList = await GetAsEntitiesOdata(oDataOptions);
            return Ok(ModelConverter.FormNewSuccessResponse(retList));
        }

        #endregion Odata EndPoints

        #region GetAll Endpoint

        [HttpGet]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin,SuperAdmin")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserLicenseDetailsSM>>>> GetAll()
        {
            var userLicenseDetailsListsSM = await _userLicenseDetailsProcess.GetAllUserLicenseDetails();
            return Ok(ModelConverter.FormNewSuccessResponse(userLicenseDetailsListsSM));
        }


        #endregion GetAll Endpoint

        #region Get Single Endpoint

        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientAdmin, ClientEmployee,SystemAdmin,SuperAdmin")]
        public async Task<ActionResult<ApiResponse<UserLicenseDetailsSM>>> GetById(int id)
        {
            var singleUserLicenseDetailsSM = await _userLicenseDetailsProcess.GetUserSubscriptionById(id);
            if (singleUserLicenseDetailsSM != null)
            {
                return ModelConverter.FormNewSuccessResponse(singleUserLicenseDetailsSM);
            }
            else
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotFound, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        #endregion Get Single Endpoint

        #region Add/Update EndPoint

        [HttpPost]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientAdmin, ClientEmployee,SystemAdmin,SuperAdmin")]
        public async Task<ActionResult<ApiResponse<UserLicenseDetailsSM>>> Post([FromBody] ApiRequest<UserLicenseDetailsSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            #endregion Check Request

            var userLicesneDetailsSM = await _userLicenseDetailsProcess.AddUserSubscription(innerReq);
            if (userLicesneDetailsSM != null)
            {
                return CreatedAtAction(nameof(GetById), new
                {
                    id = userLicesneDetailsSM.Id
                }, ModelConverter.FormNewSuccessResponse(userLicesneDetailsSM));
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin,SuperAdmin")]
        public async Task<ActionResult<ApiResponse<UserLicenseDetailsSM>>> Put(int id, [FromBody] ApiRequest<UserLicenseDetailsSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            if (id <= 0)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdInvalid, ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            #endregion Check Request

            var resp = await _userLicenseDetailsProcess.UpdateUserSubscription(id, innerReq);
            if (resp != null)
            {
                return Ok(ModelConverter.FormNewSuccessResponse(resp));
            }
            else
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        #endregion Add/Update EndPoint

        #region Delete Endpoint

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin,SystemAdmin")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> Delete(int id)
        {
            var resp = await _userLicenseDetailsProcess.DeleteUserSubscriptionById(id);
            if (resp != null && resp.DeleteResult)
            {
                return Ok(ModelConverter.FormNewSuccessResponse(resp));
            }
            else
            {
                return NotFound(ModelConverter.FormNewErrorResponse(resp?.DeleteMessage, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        #endregion Delete Endpoint

        #region Mine-License

        [HttpGet("mine/ActiveTrialLicense")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientAdmin, ClientEmployee,SuperAdmin,SystemAdmin")]
        public async Task<ActionResult<ApiResponse<UserLicenseDetailsSM>>> GetMineActiveTrialLicense()
        {
            int currentUserRecordId = User.GetUserRecordIdFromCurrentUserClaims();
            if (currentUserRecordId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotInClaims));
            }
            UserLicenseDetailsSM? singleUserLicenseDetailsSM = await _userLicenseDetailsProcess.GetActiveTrialUserLicenseDetailsByUserId(currentUserRecordId);
            return ModelConverter.FormNewSuccessResponse(singleUserLicenseDetailsSM);
        }

        [HttpGet("mine/Active")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientAdmin, ClientEmployee,SuperAdmin,SystemAdmin")]
        public async Task<ActionResult<ApiResponse<UserLicenseDetailsSM>>> GetMineActiveLicense()
        {
            int currentUserRecordId = User.GetUserRecordIdFromCurrentUserClaims();
            if (currentUserRecordId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotInClaims));
            }
            UserLicenseDetailsSM? singleUserLicenseDetailsSM = await _paymentProcess.GetActiveLicenseDetailsByUserId(currentUserRecordId);
            return ModelConverter.FormNewSuccessResponse(singleUserLicenseDetailsSM);
        }
        #endregion Mine-License

        #region Trial

        [HttpPost("mine/AddTrial")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientAdmin, ClientEmployee,SuperAdmin,SystemAdmin")]
        public async Task<ActionResult<ApiResponse<UserLicenseDetailsSM>>> AddMineTrialLicense()
        {
            int userId = User.GetUserRecordIdFromCurrentUserClaims();

            var userLicenseDetailsSM = await _userLicenseDetailsProcess.AddTrialLicenseDetails(userId);
            if (userLicenseDetailsSM != null)
            {
                return CreatedAtAction(nameof(GetById), new
                {
                    id = userLicenseDetailsSM.Id
                }, ModelConverter.FormNewSuccessResponse(userLicenseDetailsSM));
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        #endregion Trial
    }
}

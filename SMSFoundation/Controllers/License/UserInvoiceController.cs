using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSBAL.ExceptionHandler;
using SMSBAL.Foundation.Web;
using SMSBAL.License;
using SMSFoundation.Controllers.Base;
using SMSFoundation.Security;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.v1.General.License;
using Stripe;

namespace SMSFoundation.Controllers.License
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UserInvoiceController : ApiControllerWithOdataRoot<UserInvoiceSM>
    {
        #region Properties
        private readonly UserInvoiceProcess _userInvoiceProcess;
        #endregion Properties

        #region Constructor
        public UserInvoiceController(UserInvoiceProcess userInvoiceProcess)
            : base(userInvoiceProcess)
        {
            _userInvoiceProcess = userInvoiceProcess;
        }
        #endregion Constructor

        #region Get All
        [HttpGet]

        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserInvoiceSM>>>> GetAll()
        {
            var listSM = await _userInvoiceProcess.GetAllUserInvoices();
            return Ok(ModelConverter.FormNewSuccessResponse(listSM));
        }

        [HttpGet("mine")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientEmployee")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserInvoiceSM>>>> GetAllMineInvoices()
        {
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotInClaims));
            }
            var listSM = await _userInvoiceProcess.GetMineInvoices(userId);
            return Ok(ModelConverter.FormNewSuccessResponse(listSM));
        }
        #endregion Get All

        #region Get Single

        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<UserInvoiceSM>>> GetById(int id)
        {
            var singleSM = await _userInvoiceProcess.GetUserInvoiceById(id);
            if (singleSM != null)
            {
                return ModelConverter.FormNewSuccessResponse(singleSM);
            }
            else
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotFound, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        #endregion Get Single

        #region Add

        [HttpPost]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin, SuperAdmin, ClientEmployee")]
        public async Task<ActionResult<ApiResponse<UserInvoiceSM>>> Post([FromBody] ApiRequest<UserInvoiceSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            #endregion Check Request

            var addedSM = await _userInvoiceProcess.AddUserInvoice(innerReq);
            if (addedSM != null)
            {
                return CreatedAtAction(nameof(UserLicenseDetailsController.GetById), new
                {
                    id = addedSM.Id
                }, ModelConverter.FormNewSuccessResponse(addedSM));
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        #endregion Add

        #region Put
        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<UserInvoiceSM>>> Put(int id, [FromBody] ApiRequest<UserInvoiceSM> apiRequest)
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

            var resp = await _userInvoiceProcess.UpdateUserInvoice(id, innerReq);
            if (resp != null)
            {
                return Ok(ModelConverter.FormNewSuccessResponse(resp));
            }
            else
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        #endregion Put

        #region Delete
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> Delete(int id)
        {
            var resp = await _userInvoiceProcess.DeleteUserInvoiceById(id);
            if (resp != null && resp.DeleteResult)
            {
                return Ok(ModelConverter.FormNewSuccessResponse(resp));
            }
            else
            {
                return NotFound(ModelConverter.FormNewErrorResponse(resp?.DeleteMessage, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        #endregion Delete

        #region Download Invoice
        [HttpPost("download/stripeInvoice")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientEmployee")]
        public async Task<IActionResult> DownloadInvoice([FromBody] ApiRequest<string> apiRequest)
        {
            try
            {
                var innerReq = apiRequest.ReqData;
                if (innerReq == null)
                {
                    return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
                }
                var downloadResponse = await _userInvoiceProcess.GetInvoiceContextFromStripe(innerReq);
                if (downloadResponse == null)
                {
                    return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
                }
                return Ok(ModelConverter.FormNewSuccessResponse(downloadResponse));

            }
            catch (SMSException ex)
            {
                // Handle Stripe API errors.
                return BadRequest("Error retrieving the invoice.");
            }

        }
        #endregion Download Invoice
    }
}

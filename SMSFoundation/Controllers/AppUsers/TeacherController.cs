using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using SMSBAL.AppUsers;
using SMSBAL.Foundation.Web;
using SMSFoundation.Controllers.Base;
using SMSFoundation.Security;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.v1.Teachers;

namespace SMSFoundation.Controllers.AppUsers
{
    [ApiController]
    [Route("api/v1/[controller]")]

    public partial class TeacherController : ApiControllerWithOdataRoot<TeacherSM>
    {
        #region Properties
        private readonly TeacherProcess _teacherProcess;
        #endregion Properties

        #region Constructor
        public TeacherController(TeacherProcess process)
            : base(process)
        {
            _teacherProcess = process;
        }
        #endregion Constructor

        #region Odata EndPoints
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin")]
        [HttpGet]
        [Route("odata")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<ApiResponse<IEnumerable<TeacherSM>>>> GetAsOdata(ODataQueryOptions<TeacherSM> oDataOptions)
        {
            //oDataOptions.Filter = new FilterQueryOption();
            //TODO: validate inputs here probably 
            //if (oDataOptions.Filter == null)
            //    oDataOptions.Filter. = "$filter=organisationUnitId%20eq%20" + 10 + ",";
            var retList = await GetAsEntitiesOdata(oDataOptions);

            return Ok(ModelConverter.FormNewSuccessResponse(retList));
        }

        #endregion Odata EndPoints

        #region Get Endpoints

        [HttpGet]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TeacherSM>>>> GetAll()
        {
            var listSM = await _teacherProcess.GetAllTeachers();
            return Ok(ModelConverter.FormNewSuccessResponse(listSM));
        }

        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin")]
        [HttpGet("count")]
        public async Task<ActionResult<ApiResponse<IntResponseRoot>>> GetCountOfAllTeachers()
        {
            var countResp = await _teacherProcess.GetAllTeachersCount();
            return Ok(ModelConverter.FormNewSuccessResponse(new IntResponseRoot(countResp, "Total Count of Teachers")));
        }

        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "Admin")]
        [HttpGet("my/{skip}/{top}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TeacherSM>>>> GetTeachersByAdmin(int skip, int top)
        {
            var adminId = User.GetUserRecordIdFromCurrentUserClaims();
            if (adminId <= 0)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdInvalid, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var listSM = await _teacherProcess.GetTeachersByAdminId(adminId, skip, top);
            return Ok(ModelConverter.FormNewSuccessResponse(listSM));
        }

        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "Admin")]
        [HttpGet("my/count")]
        public async Task<ActionResult<ApiResponse<IntResponseRoot>>> GetTeachersOfAdminCount()
        {
            var adminId = User.GetUserRecordIdFromCurrentUserClaims();
            if (adminId <= 0)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdInvalid, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var countResp = await _teacherProcess.GetTeachersOfAdminCount(adminId);
            return Ok(ModelConverter.FormNewSuccessResponse(new IntResponseRoot(countResp, "Total Count of My Teachers")));
        }

        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin, Admin")]
        public async Task<ActionResult<ApiResponse<TeacherSM>>> GetById(int id)
        {
            var singleSM = await _teacherProcess.GetTeacherById(id);
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
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "Teacher")]
        public async Task<ActionResult<ApiResponse<TeacherSM>>> GetMineById()
        {
            var id = User.GetUserRecordIdFromCurrentUserClaims();
            if (id <= 0)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdInvalid, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var singleSM = await _teacherProcess.GetTeacherById(id);
            if (singleSM != null)
            {
                return ModelConverter.FormNewSuccessResponse(singleSM);
            }
            else
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotFound, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        #endregion Get Endpoints

        #region Add/Update Endpoints

        [HttpPost()]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> PostNewTeacher([FromBody] ApiRequest<TeacherSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var adminId = User.GetUserRecordIdFromCurrentUserClaims();
            if (adminId <= 0)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdInvalid, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            #endregion Check Request

            var addedSM = await _teacherProcess.AddNewTeacher(innerReq, adminId);
            if (addedSM != null)
            {
                return ModelConverter.FormNewSuccessResponse(addedSM);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        [HttpPut("my")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<TeacherSM>>> Put(int id, [FromBody] ApiRequest<TeacherSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var adminId = User.GetUserRecordIdFromCurrentUserClaims();
            if (adminId <= 0)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdInvalid, ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            #endregion Check Request

            var updatedSM = await _teacherProcess.UpdateTeacher(adminId, id, innerReq);
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

        #region Check Existing Email/LoginId

        [HttpGet("check/email")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> CheckEmail(string email)
        {
            var resp = await _teacherProcess.CheckExistingEmail(email);
            return Ok(ModelConverter.FormNewSuccessResponse(resp));
        }

        [HttpGet("check/loginId")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> CheckLoginId(string loginId)
        {
            var resp = await _teacherProcess.CheckExistingLoginId(loginId);
            return Ok(ModelConverter.FormNewSuccessResponse(resp));
        }

        #endregion Check Existing Email/loginId 

        #region Delete Endpoints
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> Delete(int id)
        {
            var resp = await _teacherProcess.DeleteTeacherById(id);
            if (resp != null && resp.DeleteResult)
            {
                return Ok(ModelConverter.FormNewSuccessResponse(resp));
            }
            else
            {
                return NotFound(ModelConverter.FormNewErrorResponse(resp?.DeleteMessage, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        #endregion Delete Endpoints

    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using SMSFoundation.Controllers.Base;
using SMSFoundation.Security;
using SMSServiceModels.v1.General.License;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSBAL.License;
using SMSBAL.Foundation.Web;

namespace SMSFoundation.Controllers.License
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class LicenseTypeController : ApiControllerWithOdataRoot<LicenseTypeSM>
    {
        #region Properties
        private readonly LicenseTypeProcess _licenseTypeProcess;
        #endregion Properties

        #region Constructor
        public LicenseTypeController(LicenseTypeProcess licenseTypeProcess) : base(licenseTypeProcess)
        {
            _licenseTypeProcess = licenseTypeProcess;
        }
        #endregion Constructor

        #region Odata EndPoints
        [HttpGet]
        [Route("odata")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<ApiResponse<IEnumerable<LicenseTypeSM>>>> GetAsOdata(ODataQueryOptions<LicenseTypeSM> oDataOptions)
        {
            //TODO: validate inputs here probably 
            var retList = await GetAsEntitiesOdata(oDataOptions);
            return Ok(ModelConverter.FormNewSuccessResponse(retList));
        }

        #endregion Odata EndPoints

        #region GetAll Endpoint
        [HttpGet]
        //[Authorize(AuthenticationSchemes = CoreVisionBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientAdmin,SuperAdmin,SystemAdmin,ClientEmployee")]
        public async Task<ActionResult<ApiResponse<IEnumerable<LicenseTypeSM>>>> GetAll()
        {
            var LicenseTypeListSM = await _licenseTypeProcess.GetAllLicenses();
            return Ok(ModelConverter.FormNewSuccessResponse(LicenseTypeListSM));
        }


        #endregion GetAll Endpoint

        #region Get Single Endpoint

        [HttpGet("{id}")]
        //[Authorize(AuthenticationSchemes = CoreVisionBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientAdmin,SuperAdmin,SystemAdmin,ClientEmployee")]
        public async Task<ActionResult<ApiResponse<LicenseTypeSM>>> GetById(int id)
        {
            var singleLicenseTypeSM = await _licenseTypeProcess.GetSingleLicenseDetailById(id);
            if (singleLicenseTypeSM != null)
            {
                return ModelConverter.FormNewSuccessResponse(singleLicenseTypeSM);
            }
            else
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotFound, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        #endregion Get Single Endpoint

        #region My (Get) Endpoint
        
        [HttpGet("mine")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientAdmin,SuperAdmin,SystemAdmin,ClientEmployee")]
        public async Task<ActionResult<ApiResponse<LicenseTypeSM>>> GetMyLicensesWithFeatures()
        {
            int userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotInClaims));
            }
            var response = await _licenseTypeProcess.GetLicenseDetailsByUserId(userId);
            return ModelConverter.FormNewSuccessResponse(response);
        }
        #endregion My (Get) Endpoint

        #region --COUNT--

        [HttpGet]
        [Route("count")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientAdmin,ClientEmployee,SuperAdmin")]
        public async Task<ActionResult<ApiResponse<IntResponseRoot>>> GetLicenseCountsResponse()
        {
            var countResp = await _licenseTypeProcess.GetAllLicenseTypeCountResponse();
            return Ok(ModelConverter.FormNewSuccessResponse(new IntResponseRoot(countResp)));
        }

        #endregion --COUNT--
    }
}

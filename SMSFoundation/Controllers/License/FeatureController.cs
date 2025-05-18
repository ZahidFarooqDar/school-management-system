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
    public class FeatureController : ApiControllerWithOdataRoot<FeatureSM>
    {
        #region Properties
        private readonly FeatureProcess _featureProcess;
        #endregion Properties

        #region Constructor
        public FeatureController(FeatureProcess featureProcess) : base(featureProcess)
        {
            _featureProcess = featureProcess;
        }
        #endregion Constructor

        #region Odata EndPoints
        [HttpGet]
        [Route("odata")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<ApiResponse<IEnumerable<FeatureSM>>>> GetAsOdata(ODataQueryOptions<FeatureSM> oDataOptions)
        {
            //TODO: validate inputs here probably 
            var retList = await GetAsEntitiesOdata(oDataOptions);
            return Ok(ModelConverter.FormNewSuccessResponse(retList));
        }

        #endregion Odata EndPoints

        #region Get All Endpoint
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<FeatureSM>>>> GetAll()
        {
            var featureListSM = await _featureProcess.GetAllFeatures();
            return Ok(ModelConverter.FormNewSuccessResponse(featureListSM));
        }

        #endregion Get All Endpoint

        #region Get Features By License Id
        [HttpGet("license/{licenseId}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<FeatureSM>>>> GetFeaturesByLicenseId(int licenseId)
        {
            var featureListSM = await _featureProcess.GetFeaturesbylicenseId(licenseId);
            return Ok(ModelConverter.FormNewSuccessResponse(featureListSM));
        }
        #endregion Get Features By License Id
        #region Get Single Endpoint
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<FeatureSM>>> GetById(int id)
        {
            var singleFeatureSM = await _featureProcess.GetSingleFeatureById(id);
            if (singleFeatureSM != null)
            {
                return ModelConverter.FormNewSuccessResponse(singleFeatureSM);
            }
            else
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotFound, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        #endregion Get Single Endpoint

        #region My (Get) Endpoint
       

        [HttpGet("myFeatures")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientAdmin, ClientEmployee")]
        public async Task<ActionResult<ApiResponse<List<FeatureSM>>>> GetMyFeaturesAsync()
        {
            int userClientId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userClientId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotInClaims));
            }
            //return Ok(await _featureProcess.GetMyFeatures(userClientId));
            return Ok(await _featureProcess.GetMyFeatures(userClientId));
        }
        #endregion My (Get) Endpoint

        #region --COUNT--

        [HttpGet]
        [Route("FeatureCountResponse")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientAdmin, ClientEmployee,SystemAdmin,SuperAdmin")]
        public async Task<ActionResult<ApiResponse<IntResponseRoot>>> GetFeatureCountsResponse()
        {
            var countResp = await _featureProcess.GetAllFeatureCountResponse();
            return Ok(ModelConverter.FormNewSuccessResponse(new IntResponseRoot(countResp)));
        }

        #endregion --COUNT--

        #region Add Endpoint
        [HttpPost]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SystemAdmin,SuperAdmin")]
        public async Task<ActionResult<ApiResponse<FeatureSM>>> Post([FromBody] ApiRequest<FeatureSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            #endregion Check Request

            var FeatureSM = await _featureProcess.AddFeature(innerReq);
            if (FeatureSM != null)
            {
                return CreatedAtAction(nameof(GetById), new
                {
                    id = FeatureSM.Id
                }, ModelConverter.FormNewSuccessResponse(FeatureSM));
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        #endregion Add Endpoint

        #region Update Endpoint
        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<FeatureSM>>> Put(int id, [FromBody] ApiRequest<FeatureSM> apiRequest)
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

            var resp = await _featureProcess.UpdateFeature(id, innerReq);
            if (resp != null)
            {
                return Ok(ModelConverter.FormNewSuccessResponse(resp));
            }
            else
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        #endregion Update Endpoint

        #region Delete Endpoint
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> Delete(int id)
        {
            var resp = await _featureProcess.DeleteFeatureById(id);
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
    }
}

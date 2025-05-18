using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSBAL.License;
using SMSBAL.Projects.AzureAI;
using SMSBAL.Projects.BaseAIProcess;
using SMSBAL.Projects.HuggingFace;
using SMSBAL.Projects.StoryAI;
using SMSFoundation.Security;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.v1.General.AzureAI;
using SMSServiceModels.v1.General.HuggingFace;
using SMSServiceModels.v1.General.StoryAI;

namespace SMSFoundation.Controllers.AI
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class CoreVisionController : ControllerBase
    {
        public readonly HuggingfaceProcess _huggingfaceProcess;
        public readonly BaseAIProcess _baseAIProcess;
        private readonly AzureAIProcess _azureAIProcess;
        private readonly StoryProcess _storyProcess;
        private readonly PermissionProcess _permissionProcess;
        public CoreVisionController(HuggingfaceProcess huggingfaceProcess, BaseAIProcess baseAIProcess,
            StoryProcess storyProcess, AzureAIProcess azureAIProcess, PermissionProcess permissionProcess) 
        {
            _huggingfaceProcess = huggingfaceProcess;
            _baseAIProcess = baseAIProcess;
            _storyProcess = storyProcess;
            _azureAIProcess = azureAIProcess;
            _permissionProcess = permissionProcess;
        }

        [HttpPost("audio-transcription")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientEmployee, CompanyAutomation")]
        public async Task<ActionResult<ApiResponse<AudioTranscriptionRequestSM>>> TranscribeAudioUsingHuggingFaceAsync([FromBody] ApiRequest<AudioTranscriptionRequestSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            /*int userId = User.GetUserRecordIdFromCurrentUserClaims();
            var featureCode = "CVAUD-2025";
            await _permissionProcess.DoesUserHasPermission(userId, featureCode); */           
            var resp = await _huggingfaceProcess.TranscribeAudioUsingHuggingFaceAsync(innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(resp));

        }

        [HttpPost("audio-summary")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientEmployee, CompanyAutomation")]
        public async Task<ActionResult<ApiResponse<AudioTranscriptionResponseSM>>> AudioSummarizeAsync([FromBody] ApiRequest<AudioTranscriptionRequestSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            /*var featureCode = "CVAUDSUM-2025";
            int userId = User.GetUserRecordIdFromCurrentUserClaims();
            await _permissionProcess.DoesUserHasPermission(userId, featureCode);*/
            var resp = await _baseAIProcess.AudioSummerization(innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(resp));

        }

        [HttpPost("image-to-text")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientEmployee, CompanyAutomation")]
        public async Task<ActionResult<ApiResponse<HuggingFaceResponseSM>>> ExtractTextFromImageAsync([FromBody] ApiRequest<ImageDataSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
           /* var featureCode = "CVTE-2025";
            int userId = User.GetUserRecordIdFromCurrentUserClaims();
            await _permissionProcess.DoesUserHasPermission(userId, featureCode);*/
            var resp = await _baseAIProcess.BaseMethodForTextExtraction(innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(resp));

        }

        [HttpPost("qna")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientEmployee, CompanyAutomation")]
        public async Task<ActionResult<ApiResponse<HuggingFaceResponseSM>>> ExtractResponseUsingDeepSeekAsync([FromBody] ApiRequest<TextRequestSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            /*var featureCode = "CVCHAT-2025";
            int userId = User.GetUserRecordIdFromCurrentUserClaims();
            await _permissionProcess.DoesUserHasPermission(userId, featureCode);*/
            var resp = await _huggingfaceProcess.ExtractResponseUsingDeepSeekAsync(innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(resp));

        }

        [HttpPost("translate")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientEmployee, CompanyAutomation")]
        public async Task<ActionResult<ApiResponse<HuggingFaceResponseSM>>> TranslateTextAsync([FromBody] ApiRequest<TranslationRequestSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            /*var featureCode = "CVTT-2025";
            int userId = User.GetUserRecordIdFromCurrentUserClaims();
            await _permissionProcess.DoesUserHasPermission(userId, featureCode);*/
            var resp = await _baseAIProcess.BaseMethodForTextTranslation(innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(resp));

        }

        [HttpPost("short-summary")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientEmployee, CompanyAutomation")]
        public async Task<ActionResult<ApiResponse<HuggingFaceResponseSM>>> SummarizeTextAsync([FromBody] ApiRequest<AzureAIRequestSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            /*var featureCode = "CVSUM-2025";
            int userId = User.GetUserRecordIdFromCurrentUserClaims();
            await _permissionProcess.DoesUserHasPermission(userId, featureCode);*/
            var resp = await _baseAIProcess.BaseMethodForShortSummarization(innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(resp));

        }

        [HttpPost("descriptive-summary")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientEmployee, CompanyAutomation")]
        public async Task<ActionResult<ApiResponse<HuggingFaceResponseSM>>> DescriptiveSummarizeTextAsync([FromBody] ApiRequest<AzureAIRequestSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            /*var featureCode = "CVSUM-2025";
            int userId = User.GetUserRecordIdFromCurrentUserClaims();
            await _permissionProcess.DoesUserHasPermission(userId, featureCode);*/
            var resp = await _baseAIProcess.BaseMethodForExtensiveSummarization(innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(resp));

        }

        [HttpPost("image-generation")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientEmployee, CompanyAutomation")]
        public async Task<ActionResult<ApiResponse<Base64ImageResponseSM>>> GenerateHuggingImageAsync([FromBody] ApiRequest<HuggingFaceRequestSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            /*var featureCode = "CVIMG-2025";
            int userId = User.GetUserRecordIdFromCurrentUserClaims();
            await _permissionProcess.DoesUserHasPermission(userId, featureCode);*/
            var resp = await _huggingfaceProcess.GenerateHuggingImageAsync(innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(resp));

        }

        [HttpPost("generate-story")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientEmployee, CompanyAutomation")]
        public async Task<ActionResult<ApiResponse<ContentGenerationResponseSM>>> GenerateStoryAsync([FromBody] ApiRequest<ContentGenerationRequestSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            /*var featureCode = "CVSTORY-2025";
            int userId = User.GetUserRecordIdFromCurrentUserClaims();
            await _permissionProcess.DoesUserHasPermission(userId, featureCode);*/
            var resp = await _storyProcess.GenerateStory(innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(resp));

        }
    }
}

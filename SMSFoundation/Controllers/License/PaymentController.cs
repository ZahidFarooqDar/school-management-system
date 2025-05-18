using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSBAL.Foundation.Web;
using SMSBAL.License;
using SMSConfig.Configuration;
using SMSFoundation.Controllers.Base;
using SMSFoundation.Security;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.v1.General.License;
using Stripe;

namespace SMSFoundation.Controllers.License
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ApiControllerRoot
    {
        #region Properties
        private readonly PaymentProcess _paymentProcess;
        private readonly APIConfiguration _apiConfiguration;

        #endregion Properties

        #region Constructor
        public PaymentController(PaymentProcess paymentProcess, APIConfiguration apiConfiguration)
        {
            _paymentProcess = paymentProcess;
            _apiConfiguration = apiConfiguration;
        }
        #endregion Constructor

        #region Stripe Methods

        #region Create Checkout Session
        /// <summary>
        /// Create Checkout Session ID for priceId
        /// </summary>
        /// <param name="apiRequest"></param>
        /// <returns></returns>
        [HttpPost("mine/checkout")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientAdmin, ClientEmployee")]
        public async Task<ActionResult<ApiResponse<CheckoutSessionResponseSM>>> CreateCheckoutSession([FromBody] ApiRequest<CheckoutSessionRequestSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            int currentUserRecordId = User.GetUserRecordIdFromCurrentUserClaims();
            if (currentUserRecordId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotInClaims));
            }
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            CheckoutSessionResponseSM resp = await _paymentProcess.CheckoutSession(apiRequest.ReqData, currentUserRecordId);
            if (resp != null)
            {
                resp.PublicKey = _apiConfiguration.ExternalIntegrations.StripeConfiguration.PrivateKey;
                return Ok(ModelConverter.FormNewSuccessResponse(resp));
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        #endregion Create Checkout Session

        #region Customer Portal
        [HttpPost("mine/StripeCustomerPortal")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientAdmin, ClientEmployee")]
        public async Task<ActionResult<ApiResponse<CustomerPortalResponseSM>>> CustomerPortal([FromBody] ApiRequest<CustomerPortalRequestSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            int currentUserRecordId = User.GetUserRecordIdFromCurrentUserClaims();
            if (currentUserRecordId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_IdNotInClaims));
            }
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var resp = await _paymentProcess.GetCustomerPortalUrl(innerReq.ReturnUrl, currentUserRecordId);
            if (resp != null)
            {
                //resp.PublicKey = _apiConfiguration.StripeSettings.PublicKey;
                return Ok(ModelConverter.FormNewSuccessResponse(resp));
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        #endregion Customer Portal

        #region Upgrade/Downgrade Subscription
        [HttpPost("subscription/Upgrade")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientEmployee")]
        public async Task<IActionResult> UpgradeSubscription([FromBody] ApiRequest<SubscriptionUpgradeRequestSM> apiRequest)
        {

            try
            {
                var innerReq = apiRequest?.ReqData;
                var resp = await _paymentProcess.UpgradeSubscriptionAndChargeProration(innerReq);
                if (resp == null)
                    return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
                return Ok(ModelConverter.FormNewSuccessResponse(resp));
            }
            catch (StripeException e)
            {
                // Handle any Stripe API errors
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while upgrading the subscription.");
            }
        }
        #endregion Upgrade/Downgrade Subscription

        #region Upgrade/Downgrade Subscription Info
        [HttpPost("subscription/UpgradeInfo")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientEmployee")]
        public async Task<IActionResult> GetUpgradeInfo([FromBody] ApiRequest<SubscriptionUpgradeRequestSM> apiRequest)
        {
            try
            {
                var innerReq = apiRequest?.ReqData;
                var resp = await _paymentProcess.GetUpgradeInfo(innerReq);
                if (resp == null)
                    return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
                return Ok(ModelConverter.FormNewSuccessResponse(resp));
            }
            catch (StripeException e)
            {
                // Handle any Stripe API errors
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while upgrading the subscription.");
            }
        }
        #endregion Upgrade/Downgrade Subscription Info

        #region Stripe Webhook

        [HttpPost("stripewebhook")]
        public async Task<ActionResult<ApiResponse<CheckoutSessionResponseSM>>> StripeWebHook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var resp = await _paymentProcess.RegisterStripeWebhook(json, Request.Headers["Stripe-Signature"], _apiConfiguration.ExternalIntegrations.StripeConfiguration.WHSecret);
            if (resp != null)
            {
                if (resp)
                    return Ok();
                else
                    //log the object in payments logs and that the customer has paid but is not created.
                    return BadRequest();
            }
            else
            {
                //log the object in payments logs and that the customer has paid but is not created.
                return BadRequest();
            }
        }
        #endregion Stripe Webhook        


        [HttpPost("createcustomer")]
        [Authorize(AuthenticationSchemes = SMSBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "ClientAdmin, ClientEmployee")]
        public async Task<string> CreateCustomer()
        {
            int userId = User.GetUserRecordIdFromCurrentUserClaims();
            var response = await _paymentProcess.CreateCustomer(userId);
            return response;
        }

        #endregion Stripe Methods
    }
}

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SMSBAL.AppUsers;
using SMSBAL.ExceptionHandler;
using SMSBAL.Foundation.Base;
using SMSDAL.Context;
using SMSDomainModels.v1.General.License;
using SMSServiceModels.AppUser;
using SMSServiceModels.Enums;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.Foundation.Base.Interfaces;
using SMSServiceModels.v1.General.License;
using Stripe;
using Stripe.Checkout;

namespace SMSBAL.License
{
    public class PaymentProcess : SMSBalBase
    {
        #region Properties
        private readonly ILoginUserDetail _loginUserDetail;
        private readonly ClientUserProcess _clientUserProcess;
        private readonly UserLicenseDetailsProcess _userLicenseDetailsProcess;
        private readonly UserInvoiceProcess _userInvoiceProcess;
        private readonly LicenseTypeProcess _licenseTypeProcess;

        #endregion Properties

        #region Constructor
        public PaymentProcess(IMapper mapper, ApiDbContext apiDbContext, ILoginUserDetail loginUserDetail,
            ClientUserProcess clientUserProcess, UserLicenseDetailsProcess userLicenseDetailsProcess,
            UserInvoiceProcess userInvoiceProcess, LicenseTypeProcess licenseTypeProcess) : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
            _clientUserProcess = clientUserProcess;
            _userLicenseDetailsProcess = userLicenseDetailsProcess;
            _userInvoiceProcess = userInvoiceProcess;
            _licenseTypeProcess = licenseTypeProcess;
            StripeConfiguration.ApiKey = "";
        }
        #endregion Constructor

        #region Checkout Session
        /// <summary>
        /// Initiates a checkout session for the user, validating the user's details and creating a session with the specified payment options.
        /// This method creates a Stripe checkout session for subscription-based payments, using the user's email and provided payment mode.
        /// </summary>
        /// <param name="reqData">The request data containing the payment mode, price ID, success URL, and failure URL for the checkout session.</param>
        /// <param name="customerId">The unique identifier of the customer requesting the checkout session.</param>
        /// <returns>
        /// A <see cref="CheckoutSessionResponseSM"/> object containing the session ID and the URL for the user to complete the payment process.
        /// </returns>
        /// <exception cref="SMSException">
        /// Thrown if the user is not found, if their email is not confirmed, or if there are errors during the creation of the checkout session.
        /// </exception>


        public async Task<CheckoutSessionResponseSM> CheckoutSession(CheckoutSessionRequestSM reqData, int customerId)
        {

            var user = await _clientUserProcess.GetClientUserById(customerId);//check this method
            if (user == null)
                throw new SMSException(ApiErrorTypeSM.NoRecord_Log, $"User for id {customerId} not found to create checkout session.", "Error occurred in payment, Please login again.");
            if (!user.IsEmailConfirmed)
                throw new SMSException(ApiErrorTypeSM.NoRecord_NoLog, $"User for id {customerId} email not confirmed.", "Please verify your email in profile section.");
            //Todo: whether we need to check phone number confirmation or not
            /*if (!user.IsPhoneNumberConfirmed)
                throw new CoreVisionException(ApiErrorTypeSM.NoRecord_NoLog, $"User for id {customerId} phone number not confirmed.", "Please verify your contact number in profile section.");*/

            var paymentMethodTypes = new List<string>();
            switch (reqData.PaymentMode)
            {
                case PaymentMode.Card:
                    paymentMethodTypes.Add("card");
                    break;
                case PaymentMode.Wallet:
                    paymentMethodTypes.Add("wallet");
                    break;
                default:
                    paymentMethodTypes.Add("card");
                    break;
            }

            var res = await CheckCheckOutSession(customerId, reqData.PriceId);
            if (res.BoolResponse == false)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"{res.ResponseMessage}");
            }

            var options = new SessionCreateOptions
            {
                SuccessUrl = reqData.SuccessUrl,
                CancelUrl = reqData.FailureUrl,
                PaymentMethodTypes = paymentMethodTypes,
                Mode = "subscription",
                CustomerEmail = user.EmailId,
                LineItems = new List<SessionLineItemOptions>
                {
                    new ()
                    {
                        Price = reqData.PriceId,
                        Quantity = 1,
                    },
                },
                /* //adding extra info  - product id
                 Metadata = new Dictionary<string, string>
                 {
                     { "productId", reqData.ProductId.ToString() },
                 }*/
            };
            try
            {
                var service = new SessionService();
                var session = await service.CreateAsync(options);
                return new CheckoutSessionResponseSM() { SessionId = session.Id, Url = session.Url };
            }
            catch (StripeException e)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, e.Message, "Some error occurred, please try again.", e);
            }

        }
        #endregion Checkout Session

        #region Check CheckOutSession
        /// <summary>
        /// Checks whether a user already has an active subscription for the specified license, based on the provided price ID.
        /// If the user has an active license for the same price, it updates the license type and returns a message indicating that the user already has an active subscription.
        /// Otherwise, it confirms that the user can proceed with creating a new checkout session.
        /// </summary>
        /// <param name="userId">The unique identifier of the user requesting the checkout session.</param>
        /// <param name="priceId">The price ID associated with the license the user intends to subscribe to.</param>
        /// <returns>
        /// A <see cref="BoolResponseRoot"/> object containing a boolean indicating whether the user can proceed with the checkout session, 
        /// along with a message describing the status of the user's existing subscription.
        /// </returns>
        /// <exception cref="SMSException">
        /// Thrown if the specified license does not exist in the database.
        /// </exception>

        public async Task<BoolResponseRoot> CheckCheckOutSession(int userId, string priceId)
        {
            var licenseType = await _apiDbContext.LicenseTypes.FirstOrDefaultAsync(x => x.StripePriceId == priceId);

            if (licenseType == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log,
                    "The specified license does not exist. Please verify the license information and try again.",
                    "License not found. Please check the provided license information.");
            }

            var existingLicense = _apiDbContext.UserLicenseDetails
                .Where(x => x.ClientUserId == userId && x.StripePriceId == priceId && x.Status == "active")
                .FirstOrDefault();

            if (existingLicense != null)
            {
                existingLicense.LicenseTypeId = licenseType.Id;
                _apiDbContext.SaveChangesAsync();
                return new BoolResponseRoot(false, "You already have an active license for this type.");
            }

            return new BoolResponseRoot(true, "");
        }


        #endregion Check CheckOutSession

        #region Customer Portal
        /// <summary>
        /// Retrieves the URL for the Stripe customer portal session, allowing the user to manage their subscription details.
        /// This method ensures the user has an active, paid subscription and handles errors if the user is not found or has not confirmed their email.
        /// </summary>
        /// <param name="returnUrl">The URL to which the user will be redirected after completing the action in the customer portal.</param>
        /// <param name="customerId">The unique identifier of the customer requesting access to the customer portal.</param>
        /// <returns>
        /// A <see cref="CustomerPortalResponseSM"/> object containing the URL of the Stripe customer portal session.
        /// </returns>
        /// <exception cref="SMSException">
        /// Thrown if the user does not exist, if their email is not confirmed, if they don't have an active paid subscription,
        /// or if there is an issue with creating the customer portal session.
        /// </exception>

        public async Task<CustomerPortalResponseSM> GetCustomerPortalUrl(string returnUrl, int customerId)
        {
            var user = await _clientUserProcess.GetClientUserById(customerId);
            if (user == null)
                throw new SMSException(ApiErrorTypeSM.NoRecord_Log, $"User for id {customerId} not found to get to customer portal.", "Error occured in getting details, Please login again.");
            if (!user.IsEmailConfirmed)
                throw new SMSException(ApiErrorTypeSM.NoRecord_NoLog, $"User for id {customerId} email not confirmed.", "Please verify your email in profile section.");
            /*if (!user.IsPhoneNumberConfirmed)
                throw new CoreVisionException(ApiErrorTypeSM.NoRecord_NoLog, $"User for id {customerId} phone number not confirmed.", "Please verify your contact number in profile section.");*/

            var userSubscription = await _userLicenseDetailsProcess.GetUserSubscriptionByUserId(user.Id);

            if (userSubscription == null || userSubscription.StripeCustomerId.IsNullOrEmpty())
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log,
                    "It looks like you don't have an active paid subscription. Please purchase a license to access your details.",
                    "No active paid license found. Please buy a license to proceed.");
            }

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = userSubscription?.StripeCustomerId,
                //Customer = stripeCustomerId,
                ReturnUrl = returnUrl,
            };

            var resp = new CustomerPortalResponseSM();
            try
            {
                var service = new Stripe.BillingPortal.SessionService();
                var session = await service.CreateAsync(options);
                resp.Url = session.Url;
                return resp;
            }
            catch (StripeException e)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, e.Message, "Some error occurred, please try again", e);
            }

        }
        #endregion Customer Portal

        #region Stripe Webhook
        /// <summary>
        /// Handles incoming Stripe webhook events and processes them based on the event type.
        /// The method verifies the webhook signature, processes events related to customer and subscription updates, 
        /// and handles specific event types like customer creation, subscription creation, and payment failure.
        /// </summary>
        /// <param name="stripeWebhookJson">The JSON payload received from Stripe containing the event data.</param>
        /// <param name="stripeSignatureHeader">The Stripe-Signature header used to verify the webhook signature.</param>
        /// <param name="webHookSecret">The secret used to validate the webhook signature.</param>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> indicating whether the webhook was successfully processed.
        /// </returns>
        /// <exception cref="StripeException">
        /// Thrown if there is an error while processing the Stripe webhook event or if the signature verification fails.
        /// </exception>

        public async Task<bool> RegisterStripeWebhook(string stripeWebhookJson, string stripeSignatureHeader, string webHookSecret)
        {
            try
            {

                var stripeEvent = EventUtility.ConstructEvent(stripeWebhookJson, stripeSignatureHeader, webHookSecret, throwOnApiVersionMismatch: false);

                if (stripeEvent.Type == Events.CustomerCreated)
                {
                    var customer = stripeEvent.Data.Object as Customer;
                    await AddUpdateStripeCustomer(customer);
                }
                if (stripeEvent.Type == Events.CustomerUpdated)
                {
                    var customer = stripeEvent.Data.Object as Customer;
                    await AddUpdateStripeCustomer(customer);
                }
                // Handle the event
                if (stripeEvent.Type == Events.CustomerSubscriptionCreated)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    await AddUpdateStripeSubscription(subscription);
                }
                if (stripeEvent.Type == Events.CustomerSubscriptionUpdated)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    await AddUpdateStripeSubscription(subscription);
                }
                if (stripeEvent.Type == Events.CustomerSubscriptionDeleted)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    await AddUpdateStripeSubscription(subscription);
                }
                if (stripeEvent.Type == Events.SubscriptionScheduleCanceled)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    await AddUpdateStripeSubscription(subscription);
                }
                if (stripeEvent.Type == Events.SubscriptionScheduleUpdated)
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    await AddUpdateStripeSubscription(subscription);
                }
                // Handle the payment retry event
                if (stripeEvent.Type == "payment_intent.payment_failed")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    // Implement your logic to handle the payment retry event
                    // For example, you can send notifications or update your database
                    //await UpdateInvoiceDetails(paymentIntent);
                }
                //else if (stripeEvent.Type == Events.CheckoutSessionCompleted)
                //{
                //    var session = stripeEvent.Data.Object as Session;
                //    // Update Subsription
                //    //await UpdateSubscription(session);
                //}
                // ... handle other event types
                else
                {
                    // Unexpected event type
                    //Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
                }
                return true;
            }
            catch (StripeException e)
            {
                throw e;
                //log message
            }
        }
        #endregion Stripe Webhook

        #region Upgrade Subscription

        /// <summary>
        /// Upgrades an existing subscription to a new plan, calculates the proration amount, updates the subscription,
        /// and creates a payment intent to charge the prorated amount.
        /// </summary>
        /// <param name="upgradeLicenseSM">The subscription upgrade request model containing the subscription ID,
        /// new plan price ID, and other related information.</param>
        /// <returns>A <see cref="Subscription"/> object representing the updated subscription after the upgrade.</returns>
        /// <exception cref="StripeException">Thrown when an error occurs while interacting with the Stripe API.</exception>
        /// <exception cref="Exception">Thrown when a valid payment method is not found or if an unexpected error occurs.</exception>


        public async Task<Subscription> UpgradeSubscriptionAndChargeProration(SubscriptionUpgradeRequestSM? upgradeLicenseSM)
        {
            try
            {
                var subscriptionService = new SubscriptionService();
                var invoiceService = new InvoiceService();
                var paymentMethodService = new PaymentMethodService();
                var paymentIntentService = new PaymentIntentService();

                // Retrieve the existing subscription
                var subscription = subscriptionService.Get(upgradeLicenseSM?.StripeSubscriptionId);

                // Get the new plan details
                var newPlan = GetStripePlanById(upgradeLicenseSM?.NewStripePriceId);

                // Calculate the proration amount
                var prorationDate = DateTime.UtcNow;
                var prorationOptions = new UpcomingInvoiceOptions
                {
                    Customer = subscription.CustomerId,
                    Subscription = subscription.Id,
                    
                    SubscriptionItems = new List<InvoiceSubscriptionItemOptions>
                    {
                        new InvoiceSubscriptionItemOptions
                        {
                            Id = subscription.Items.Data[0].Id,
                            Price = upgradeLicenseSM?.NewStripePriceId
                        }
                    },
                    SubscriptionProrationDate = prorationDate,
                };

                var upcomingInvoice = invoiceService.Upcoming(prorationOptions);
                var prorationCents = upcomingInvoice.Lines
                    .Where(line => line.Period.Start.Date == prorationDate.Date)
                    .Sum(line => line.Amount);

                // Update the subscription with proration
                var updatedSubscription = subscriptionService.Update(
                    subscription.Id,
                    new SubscriptionUpdateOptions
                    {
                        ProrationBehavior = "always_invoice",                        
                        //PaymentBehavior = "default_incomplete",
                        Items = new List<SubscriptionItemOptions>
                        {
                            new SubscriptionItemOptions
                            {
                                Id = subscription.Items.Data[0].Id,
                                Price = upgradeLicenseSM?.NewStripePriceId,
                            }
                        }
                    });

                // Retrieve a valid payment method attached to the customer
                var paymentMethods = paymentMethodService.List(new PaymentMethodListOptions
                {
                    Customer = subscription.CustomerId,
                    Type = "card"
                });

                var paymentMethod = paymentMethods.FirstOrDefault();

                if (paymentMethod == null)
                {
                    throw new Exception("No valid payment method found.");
                }

                // Create and confirm a payment intent for the draft invoice
                var paymentIntentCreateOptions = new PaymentIntentCreateOptions
                {
                    Amount = prorationCents,
                    Currency = "usd",
                    Customer = subscription.CustomerId,
                    PaymentMethod = paymentMethod.Id,
                    Description = "Proration charge",
                    Confirm = true,
                    ReturnUrl = "https://www.google.com" // TODO: Replace with actual return URL
                };

                var paymentIntent = paymentIntentService.Create(paymentIntentCreateOptions);



                return updatedSubscription;
            }
            catch (StripeException ex)
            {
                // Handle Stripe API errors
                throw ex;
            }
            catch (Exception ex)
            {
                // Handle other unexpected errors
                throw ex;
            }
        }


        #endregion Upgrade Subscription

        #region Upgrade Subscription Info
        /// <summary>
        /// Retrieves detailed information about a subscription upgrade, including current and new plan details,
        /// and calculates the prorated amount for the upgrade.
        /// </summary>
        /// <param name="prorationSM">The subscription upgrade request model containing the subscription ID and customer ID.</param>
        /// <returns>A <see cref="SubscriptionUpgradeResponseSM"/> object that includes details about the current plan,
        /// new plan, the prorated amount, and the next billing date.</returns>
        /// <exception cref="StripeException">Thrown when an error occurs while interacting with the Stripe API.</exception>
        /// <exception cref="Exception">Thrown when a downgrade attempt is detected or if an unexpected error occurs.</exception>

        public async Task<SubscriptionUpgradeResponseSM> GetUpgradeInfo(SubscriptionUpgradeRequestSM? prorationSM)
        {
            try
            {
                var subService = new SubscriptionService();
                Subscription currentSubscription = subService.Get(prorationSM?.StripeSubscriptionId);

                // Get the current plan details
                decimal currentPlanPrice = (decimal)(currentSubscription.Items.Data[0].Plan.Amount / 100m); // Convert to dollars

                // Get the new plan details
                var newPlan = GetStripePlanById(prorationSM.NewStripePriceId);
                decimal newPlanPrice = (decimal)(newPlan.Amount / 100m); // Convert to dollars

                // Check if the new plan's amount is greater than the current plan's amount
                if (newPlanPrice <= currentPlanPrice)
                {
                    throw new Exception("Downgrades are not allowed.");
                }

                // Set the proration date to this moment:
                DateTimeOffset prorationDate = DateTimeOffset.UtcNow;

                // See what the next invoice would look like with a price switch and proration set:
                var items = new List<InvoiceSubscriptionItemOptions>
                {
                    new InvoiceSubscriptionItemOptions
                    {
                        Id = currentSubscription.Items.Data[0].Id,
                        Price = prorationSM.NewStripePriceId, // Switch to new price
                    },
                };

                var options = new UpcomingInvoiceOptions
                {
                    Customer = prorationSM.StripeCustomerId,
                    Subscription = prorationSM.StripeSubscriptionId,
                    SubscriptionItems = items,
                    SubscriptionProrationDate = prorationDate.UtcDateTime,
                };

                var invService = new InvoiceService();
                Invoice upcomingInvoice = invService.Upcoming(options);

                var prorationCents = upcomingInvoice.Lines
                    .Where(line => line.Period.Start.Date == prorationDate.Date)
                    .Sum(line => line.Amount);

                var prorationCents2 = upcomingInvoice.Lines
                    .Where(line => line.Type == "invoiceitem")
                    .Sum(line => line.Amount);
                decimal proratedAmount = prorationCents / 100m; // Convert to dollars
                var currentPlanName = await _apiDbContext.UserLicenseDetails.Where(x => x.StripeSubscriptionId == prorationSM.StripeSubscriptionId).Select(x => x.SubscriptionPlanName).FirstOrDefaultAsync();
                var newPlanName = await _licenseTypeProcess.GetLicenseNameByStripePriceId(prorationSM.NewStripePriceId);
                // Return all the relevant information in the response
                return new SubscriptionUpgradeResponseSM
                {
                    CurrentPlanName = currentPlanName,
                    CurrentPlanPrice = currentPlanPrice,
                    NewPlanName = newPlanName,
                    NewPlanPrice = newPlanPrice,
                    ProratedAmount = proratedAmount,
                    NextBillingDate = currentSubscription.CurrentPeriodEnd,
                };
            }
            catch (StripeException ex)
            {
                // Handle Stripe API errors
                throw;
            }
            catch (Exception ex)
            {
                // Handle other unexpected errors
                throw;
            }
        }

        /// <summary>
        /// Retrieves a specific plan from Stripe using the provided plan ID.
        /// </summary>
        /// <param name="planId">The ID of the plan to retrieve.</param>
        /// <returns>A <see cref="Plan"/> object containing the details of the specified plan.</returns>
        /// <exception cref="StripeException">Thrown when there is an error retrieving the plan from Stripe.</exception>

        private Plan GetStripePlanById(string planId)
        {
            var service = new PlanService();
            return service.Get(planId);
        }
        #endregion Upgrade Subscription Info

        #region Add/Update Stripe Subscription
        /// <summary>
        /// Adds or updates a Stripe customer record in the user subscription table based on the provided customer information.
        /// </summary>
        /// <param name="customer">The <see cref="Customer"/> object containing customer details from Stripe.</param>
        /// <returns>A <see cref="UserLicenseDetailsSM"/> object representing the added or updated user subscription details.</returns>
        /// <exception cref="SMSException">
        /// Thrown when there is an error during the process of adding or updating the user subscription in the database.
        /// </exception>

        private async Task<UserLicenseDetailsSM?> AddUpdateStripeCustomer(Customer? customer)
        {
            try
            {
                var user = await _clientUserProcess.GetClientUserByEmail(customer.Email);

                var userSubscriptionSM = new UserLicenseDetailsSM()
                {
                    StripeCustomerId = customer.Id,
                    ClientUserId = user.Id,
                    LicenseTypeId = null,
                };


                //checking if this user subscription already exists in the db for this customer id
                var userSubscriptionExistsInDb = await _userLicenseDetailsProcess.GetUserSubscriptionByStripeCustomerId(customer.Id, user.Id);

                if (userSubscriptionExistsInDb != null && user != null)
                    return await _userLicenseDetailsProcess.UpdateUserSubscription(userSubscriptionExistsInDb.Id, userSubscriptionSM);
                else
                    return await _userLicenseDetailsProcess.AddUserSubscription(userSubscriptionSM);
                //return null;
            }
            catch (Exception ex)
            {
                throw new SMSException(ApiErrorTypeSM.ModelError_Log, $"{ex.Message}", $"Add or Update failed in user subscription table", ex);
            }
        }
        /// <summary>
        /// Adds or updates a user's subscription details based on the provided Stripe subscription object.
        /// This method also updates or adds associated invoice details in the database.
        /// </summary>
        /// <param name="subscription">The <see cref="Subscription"/> object containing the subscription details from Stripe.</param>
        /// <returns>
        /// A nullable boolean indicating whether the subscription was successfully added or updated. 
        /// Returns <c>true</c> if the operation was successful, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="SMSException">
        /// Thrown when an error occurs while processing the subscription or interacting with the database.
        /// </exception>

        private async Task<bool?> AddUpdateStripeSubscription(Subscription? subscription)
        {
            try
            {
                ClientUserSM? user = null;
                UserLicenseDetailsSM? userSubscriptionAddUpdateResponse = null;
                var licenseDetail = await _licenseTypeProcess.GetSingleFeatureDetailByStripePriceId(subscription.Items.Data[0].Price.Id);
                var customerService = new CustomerService();
                var customer = customerService.Get(subscription.CustomerId);
                if (customer != null)
                {
                    user = await _clientUserProcess.GetClientUserByEmail(customer.Email);
                }
                //var priceId = subscription.Items.Data[0].Price.Id;

                //TODO: a user can purchase different products, so we have to allow that as well based on some validation
                //checking if the user is again subscribing to the same product
                //if (priceId != userSubscription.StripePriceId)
                
                    if (user != null)
                    {
                        UserLicenseDetailsSM? userSubscription = await _userLicenseDetailsProcess.GetUserSubscriptionByUserId(user.Id);
                        if (userSubscription != null)
                            userSubscriptionAddUpdateResponse = await _userLicenseDetailsProcess.UpdateUserSubscription(userSubscription.Id,  ConvertStripeSubscriptionToUserSubscriptionSM(subscription, user.Id, licenseDetail));
                        else
                            userSubscriptionAddUpdateResponse = await _userLicenseDetailsProcess.AddUserSubscription( ConvertStripeSubscriptionToUserSubscriptionSM(subscription, user.Id, licenseDetail));

                        Invoice invoice = new Invoice();
                        if (subscription.LatestInvoice == null)
                        {
                            var invoiceService = new InvoiceService();
                            invoice = invoiceService.Get(subscription.LatestInvoiceId);
                        }

                        var invoiceDb = await _userInvoiceProcess.GetSingleInvoiceByStripeInvoiceId(subscription.LatestInvoiceId);
                        if (invoiceDb != null)
                            await _userInvoiceProcess.UpdateUserInvoice(invoiceDb.Id, ConvertToInvoiceSM(subscription, invoice, userSubscriptionAddUpdateResponse?.Id ?? 0));
                        else
                            await _userInvoiceProcess.AddUserInvoice(ConvertToInvoiceSM(subscription, invoice, userSubscriptionAddUpdateResponse?.Id ?? 0));
                    }
                    return userSubscriptionAddUpdateResponse != null && userSubscriptionAddUpdateResponse.Id > 0;
                
            }
            catch (Exception ex)
            {
                throw new SMSException(ApiErrorTypeSM.ModelError_Log, $"{ex.Message}", $"Add or Update failed in user subscription table", ex);
            }
        }
        #endregion Add/Update Stripe Subscription

        #region Convert Stripe Subscription to User SubscriptionSM

        /// <summary>
        /// Converts a Stripe <see cref="Subscription"/> object into a <see cref="UserLicenseDetailsSM"/> object,
        /// mapping relevant subscription details and pricing information to user-specific subscription data.
        /// </summary>
        /// <param name="subscription">The <see cref="Subscription"/> object from Stripe containing subscription details.</param>
        /// <param name="userId">The unique identifier of the user associated with the subscription.</param>
        /// <param name="licenseType">The <see cref="LicenseTypeSM"/> object representing the license type associated with the subscription.</param>
        /// <returns>
        /// A <see cref="UserLicenseDetailsSM"/> object containing user subscription details if the conversion is successful,
        /// or <c>null</c> if the subscription or its related data is invalid or incomplete.
        /// </returns>
        /// <exception cref="StripeException">
        /// Thrown when there is an error while interacting with the Stripe API.
        /// </exception>

        private UserLicenseDetailsSM? ConvertStripeSubscriptionToUserSubscriptionSM(Subscription? subscription, int userId, LicenseTypeSM licenseType)
        {
            try
            {
                if (subscription != null && subscription.Items != null && subscription.Items.Data.Count > 0)
                {
                    // Retrieve the price details, which includes the associated product
                    var priceItem = subscription.Items.Data[0].Price;
                    if (priceItem != null)
                    {
                        var priceService = new PriceService();
                        var price = priceService.Get(priceItem.Id);

                        if (price != null)
                        {
                            
                            // Create and return the UserSubscriptionSM object
                            var userSubscriptionSM = new UserLicenseDetailsSM
                            {
                                ClientUserId = userId,
                                StripeCustomerId = subscription.CustomerId,
                                SubscriptionPlanName = licenseType.Title,
                                StripeProductId = priceItem.ProductId,
                                ProductName = GetProductName(price.ProductId),
                                StartDateUTC = subscription.CurrentPeriodStart,
                                ExpiryDateUTC = subscription.CurrentPeriodEnd,
                                IsSuspended = false,
                                ValidityInDays = (int)(subscription.CurrentPeriodEnd - subscription.CurrentPeriodStart).TotalDays,
                                ActualPaidPrice = priceItem.UnitAmountDecimal.Value / 100,
                                Currency = subscription.Currency,
                                StripeSubscriptionId = subscription.Id,
                                Status = subscription.Status,
                                StripePriceId = price.Id,
                                LicenseTypeId = licenseType.Id, //Todo: See how to figure this out LicenceTypeId
                            };
                            if (subscription.CancelAtPeriodEnd || subscription.CanceledAt.HasValue)
                            {
                                userSubscriptionSM.IsCancelled = true;
                                userSubscriptionSM.CancelAt = subscription.CancelAt?.ToUniversalTime();
                                userSubscriptionSM.CancelledOn = subscription.CanceledAt?.ToUniversalTime();
                            }
                            else
                            {
                                userSubscriptionSM.IsCancelled = false;
                                userSubscriptionSM.CancelAt = null;
                                userSubscriptionSM.CancelledOn = null;
                            }
                            return userSubscriptionSM;
                        }
                    }
                }

                // Handle the case where subscription or its related objects are null
                return null;
            }
            catch (StripeException ex)
            {
                throw ex;
            }
        }

        #region Get Product Name On StripeProductId
        /// <summary>
        /// Retrieves the name of a product from Stripe based on the provided product ID.
        /// If the product is not found or an error occurs, it returns a default name "Unknown Product".
        /// </summary>
        /// <param name="productId">The unique identifier for the product in Stripe.</param>
        /// <returns>
        /// The name of the product if found, or "Unknown Product" if the product does not exist or an error occurs.
        /// </returns>

        public string GetProductName(string productId)
        {
            try
            {
                var productService = new ProductService();
                var product = productService.Get(productId);

                if (product != null)
                {
                    return product.Name;
                }
                else
                {
                    return "Unknown Product";  //Todo: How to handle product name here
                    //throw new Exception("Product not found");
                }
            }
            catch
            {
                return "Unknown Product";
            }
        }
        #endregion Get Product Name On StripeProductId

        #endregion Convert Stripe Subscription to User SubscriptionSM

        #region Convert To InvoiceSM
        /// <summary>
        /// Converts a Stripe subscription and invoice into a UserInvoiceSM object containing user-specific invoice details.
        /// This includes customer, pricing, subscription period, and payment amounts information.
        /// </summary>
        /// <param name="subscription">The Stripe subscription that contains the customer's subscription details.</param>
        /// <param name="invoice">The Stripe invoice that contains the payment and amount details for the subscription.</param>
        /// <param name="userSubscriptionId">The unique identifier of the user's subscription record.</param>
        /// <returns>
        /// A UserInvoiceSM object containing the mapped user-specific invoice details, including the paid price, due amount,
        /// remaining amount, subscription start and end dates, and the associated user subscription ID.
        /// </returns>

        private UserInvoiceSM ConvertToInvoiceSM(Subscription subscription, Invoice invoice, int userSubscriptionId)
        {
            var userInvoiceSM = new UserInvoiceSM
            {
                StripeCustomerId = subscription.CustomerId,
                Currency = invoice.Currency,
                StartDateUTC = subscription.CurrentPeriodStart.ToUniversalTime(),
                ActualPaidPrice = invoice.AmountPaid / 100,
                AmountDue = invoice.AmountDue / 100,
                AmountRemaining = invoice.AmountRemaining / 100,
                ExpiryDateUTC = subscription.CurrentPeriodEnd.ToUniversalTime(),
                DiscountInPercentage = 0,//can be calculated;
                StripeInvoiceId = invoice.Id,
                UserLicenseDetailsId = userSubscriptionId,
                CreatedBy = _loginUserDetail.LoginId,
                CreatedOnUTC = DateTime.UtcNow,
            };
            return userInvoiceSM;
        }
        #endregion Convert To InvoiceSM

        #region Create Customer 
        /// <summary>
        /// Creates a new CustomerId for existing User 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<string> CreateCustomer(int userId)
        {
            var user = await _apiDbContext.ClientUsers.Where(x => x.Id == userId).FirstOrDefaultAsync();

            // Create options for creating a new customer
            var options = new CustomerCreateOptions
            {
                Name = user.LoginId,
                Email = user.EmailId
            };

            // Initialize the CustomerService
            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            // Call the CreateAsync method with the options to create a new customer
            // var customer = await service.CreateAsync(options);
            /*if(customer != null)
            {
                var userDetails = await _userLicenseDetailsProcess.GetUserSubscriptionByUserId(userId);

                userDetails.StripeCustomerId = customer.Id;
                await _apiDbContext.SaveChangesAsync();
            var data = await GetActiveSubscriptionByEmailAsync(user.Id, user.EmailId);
            }*/
            var customerId = customer.Id;
            return customerId;

        }



        #region Get Active Subscription of Customer

        public async Task<bool> GetActiveSubscriptionByEmailAsync(int userId, string email)
        {
            try
            {
                var customers = await GetCustomersByEmailAsync(email);
                if (customers.Count == 0)
                {
                    return false;
                }
                var details = new UserLicenseDetailsSM();
                details.ClientUserId = userId;
                var subscription = new Subscription();
                foreach (var customer in customers)
                {
                    var res = await GetActiveSubscriptionsAsync(customer.Id);
                    if (res.Count > 0)
                    {
                        if (!res[0].Id.IsNullOrEmpty())
                        {
                            subscription = res[0];
                            break;
                        }
                    }

                }
                if (subscription != null)
                {
                    if (!subscription.Id.IsNullOrEmpty())
                    {
                        var res = await AddUpdateStripeSubscription(subscription);
                        return true;
                    }

                }
                return false;
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                //Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        private async Task<List<Customer>> GetCustomersByEmailAsync(string email)
        {
            var service = new CustomerService();
            var customers = await service.ListAsync(new CustomerListOptions { Email = email });
            return customers.ToList(); // Return the list of customers matching the email
        }

        // Fetch active subscriptions for a customer
        private async Task<List<Subscription>> GetActiveSubscriptionsAsync(string customerId)
        {
            var service = new SubscriptionService();
            var subscriptions = await service.ListAsync(new SubscriptionListOptions
            {
                Customer = customerId,
                Status = "active" // Filter for active subscriptions
            });
            return subscriptions.ToList(); // Return the list of active subscriptions
        }

        #endregion Get Active Subscription of Customer

        #endregion Create Customer

        #region Create Price for existing Product

        public async Task<Price> CreatePriceExistingProduct(string productId, string currency, long amount)
        {

            try
            {
                var options = new PriceCreateOptions
                {
                    //Product = "prod_PzglP8MyEpLCNX",
                    Product = productId,
                    Currency = currency,
                    UnitAmount = amount,
                    Active = true,
                    Nickname = null,
                    Recurring = new PriceRecurringOptions
                    {
                        Interval = "month" // Set the interval for recurring payments
                    },
                };

                var service = new PriceService();
                var price = await service.CreateAsync(options);

                return price;
            }
            catch (StripeException ex)
            {
                // Handle Stripe API errors
                throw new Exception($"Stripe error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Handle other unexpected errors
                throw new Exception($"An error occurred: {ex.Message}");
            }
        }


        public async Task<Price> CreateProductWithPrice(string planName, string currency, long amount)
        {
            //StripeConfiguration.ApiKey = apiKey;

            try
            {

                var options = new ProductCreateOptions
                {
                    Name = planName
                };

                // Initialize the ProductService
                var planService = new ProductService();

                // Call the Create method with the options to create a new product
                var product = await planService.CreateAsync(options);

                var priceOptions = new PriceCreateOptions
                {
                    //Product = "prod_PzglP8MyEpLCNX",
                    Product = product.Id,
                    Currency = currency,
                    UnitAmount = amount,
                    Active = true,
                    Nickname = null,
                    Recurring = new PriceRecurringOptions
                    {
                        Interval = "month" // Set the interval for recurring payments
                    },
                };

                var service = new PriceService();
                var price = await service.CreateAsync(priceOptions);

                return price;
            }
            catch (StripeException ex)
            {
                // Handle Stripe API errors
                throw new Exception($"Stripe error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Handle other unexpected errors
                throw new Exception($"An error occurred: {ex.Message}");
            }
        }

        #endregion Create Price

        #region Active License
        /// <summary>
        /// Retrieves the active license details for a given user. 
        /// If the license has expired, an exception is thrown with a message prompting the user to purchase a new license. 
        /// If no active license is found, an exception is thrown indicating that no active license exists and the user needs to activate one.
        /// </summary>
        /// <param name="currentUserId">The ID of the user whose active license details are to be retrieved.</param>
        /// <returns>
        /// Returns the active license details mapped to a UserLicenseDetailsSM object if an active license exists and is valid.
        /// </returns>
        /// <exception cref="SMSException">
        /// Thrown when the user's license has expired, or no active license exists.
        /// </exception>
        
        public async Task<UserLicenseDetailsSM> GetActiveLicenseDetailsByUserId(int currentUserId)
        {
            var existingLicenseDetails = await _apiDbContext.UserLicenseDetails
                .FirstOrDefaultAsync(x => x.ClientUserId == currentUserId);
            var lastLicense = new UserLicenseDetailsDM();
            if (existingLicenseDetails != null)
            {
                if(existingLicenseDetails.Status == "active")
                {
                    
                    if(existingLicenseDetails.StripeSubscriptionId != null )
                    {
                        if (existingLicenseDetails.ExpiryDateUTC < DateTime.UtcNow)
                        {
                            existingLicenseDetails.Status = "expired";
                            await _apiDbContext.SaveChangesAsync(); // Update status even if not used
                            return _mapper.Map<UserLicenseDetailsSM>(existingLicenseDetails);
                            
                        }
                        else
                        {
                            if (existingLicenseDetails.IsCancelled == true)
                            {
                                existingLicenseDetails.Status = "renew";
                                await _apiDbContext.SaveChangesAsync();
                                return _mapper.Map<UserLicenseDetailsSM>(existingLicenseDetails);
                            }

                            var licenseType = await _apiDbContext.LicenseTypes
                                .FirstOrDefaultAsync(x => x.StripePriceId == existingLicenseDetails.StripePriceId);

                            if (licenseType != null && existingLicenseDetails.LicenseTypeId != licenseType.Id)
                            {
                                existingLicenseDetails.LicenseTypeId = licenseType.Id;
                                await _apiDbContext.SaveChangesAsync();
                            }

                            return _mapper.Map<UserLicenseDetailsSM>(existingLicenseDetails);
                        }
                    }
                    else
                    {
                        var user = await _clientUserProcess.GetClientUserById(currentUserId);
                        var isHavingSubscription = await GetActiveSubscriptionByEmailAsync(user.Id, user.EmailId);
                        if (isHavingSubscription)
                        {
                            var activeSubscription = await _apiDbContext.UserLicenseDetails
                            .Where(x => x.ClientUserId == currentUserId && x.Status == "active").FirstOrDefaultAsync();
                            if(activeSubscription != null)
                            {
                                var licenseType = await _apiDbContext.LicenseTypes
                                    .FirstOrDefaultAsync(x => x.StripePriceId == existingLicenseDetails.StripePriceId);

                                if (licenseType != null && existingLicenseDetails.LicenseTypeId != licenseType.Id)
                                {
                                    activeSubscription.LicenseTypeId = licenseType.Id;
                                    await _apiDbContext.SaveChangesAsync();
                                }
                                return _mapper.Map<UserLicenseDetailsSM>(activeSubscription);
                            }
                            else
                            {
                                lastLicense = await _apiDbContext.UserLicenseDetails
                                    .Where(x => x.ClientUserId == currentUserId && x.ExpiryDateUTC != null)
                                    .OrderByDescending(x => x.ExpiryDateUTC) 
                                    .FirstOrDefaultAsync();
                                return _mapper.Map<UserLicenseDetailsSM>(lastLicense);
                            }
                        }
                        else
                        {
                            existingLicenseDetails.Status = "expired";
                            await _apiDbContext.SaveChangesAsync(); // Update status even if not used
                            return _mapper.Map<UserLicenseDetailsSM>(existingLicenseDetails);
                        }
                    }
                }
                else
                {

                    /*if(existingLicenseDetails.IsCancelled == true || existingLicenseDetails.IsSuspended == true)
                    {
                        existingLicenseDetails.Status = "renew";
                        await _apiDbContext.SaveChangesAsync();
                        return _mapper.Map<UserLicenseDetailsSM>(existingLicenseDetails);
                    }*/
                    if (existingLicenseDetails.Status == "incomplete" || existingLicenseDetails.Status == "past_due")
                    {
                        var user = await _clientUserProcess.GetClientUserById(currentUserId);
                        var isHavingSubscription = await GetActiveSubscriptionByEmailAsync(user.Id, user.EmailId);
                        if (isHavingSubscription)
                        {
                            var activeSubscription = await _apiDbContext.UserLicenseDetails
                            .Where(x => x.ClientUserId == currentUserId && x.Status == "active").FirstOrDefaultAsync();
                            if (activeSubscription != null)
                            {
                                var licenseType = await _apiDbContext.LicenseTypes
                                    .FirstOrDefaultAsync(x => x.StripePriceId == existingLicenseDetails.StripePriceId);

                                if (licenseType != null && existingLicenseDetails.LicenseTypeId != licenseType.Id)
                                {
                                    activeSubscription.LicenseTypeId = licenseType.Id;
                                    await _apiDbContext.SaveChangesAsync();
                                }
                                return _mapper.Map<UserLicenseDetailsSM>(activeSubscription);
                            }
                            else
                            {
                                lastLicense = await _apiDbContext.UserLicenseDetails
                                    .Where(x => x.ClientUserId == currentUserId && x.ExpiryDateUTC != null)
                                    .OrderByDescending(x => x.ExpiryDateUTC)
                                    .FirstOrDefaultAsync();
                                return _mapper.Map<UserLicenseDetailsSM>(lastLicense);
                            }
                        }
                    }


                    if (!existingLicenseDetails.StripeProductId.IsNullOrEmpty())
                    {
                        var licenseType = await _apiDbContext.LicenseTypes
                                    .FirstOrDefaultAsync(x => x.StripePriceId == existingLicenseDetails.StripePriceId);

                        if (licenseType != null && existingLicenseDetails.LicenseTypeId != licenseType.Id)
                        {
                            existingLicenseDetails.LicenseTypeId = licenseType.Id;
                            await _apiDbContext.SaveChangesAsync();
                        }
                    }
                    
                    return _mapper.Map<UserLicenseDetailsSM>(existingLicenseDetails);
                }
            }
            else
            {
                
                return new UserLicenseDetailsSM();
            }
        }
        #endregion Active License
    }
}

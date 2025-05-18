using AutoMapper;
using Stripe;
using Microsoft.EntityFrameworkCore;
using SMSDAL.Context;
using SMSServiceModels.v1.General.License;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.Foundation.Base.Interfaces;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSDomainModels.v1.General.License;
using SMSBAL.Foundation.Base;
using SMSBAL.ExceptionHandler;

namespace SMSBAL.License
{
    public class UserInvoiceProcess : CoreVisionBalOdataBase<UserInvoiceSM>
    {
        #region Properties
        private readonly ILoginUserDetail _loginUserDetail;
        #endregion Properties

        #region Constructor
        public UserInvoiceProcess(IMapper mapper, ApiDbContext apiDbContext, ILoginUserDetail loginUserDetail) : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
        }
        #endregion Constructor

        #region Odata

        /// <summary>
        /// This method gets any UserInvoice(s) by filtering/sorting the data
        /// </summary>
        /// <returns>UserInvoice(s)</returns>
        public override async Task<IQueryable<UserInvoiceSM>> GetServiceModelEntitiesForOdata()
        {
            var entitySet = _apiDbContext.UserInvoices;
            IQueryable<UserInvoiceSM> retSM = await MapEntityAsToQuerable<UserInvoiceDM, UserInvoiceSM>(_mapper, entitySet);
            return retSM;
        }

        #endregion Odata

        #region --Count--

        /// <summary>
        /// Get UserInvoices Count in database.
        /// </summary>
        /// <returns>integer response</returns>

        public async Task<int> GetAllUserInvoicesCountResponse()
        {
            int resp = _apiDbContext.UserInvoices.Count();
            return resp;
        }

        #endregion --Count--

        #region Get All

        #region Get All
        /// <summary>
        /// Retrieves a list of all user invoices.
        /// </summary>
        /// <returns>A list of UserInvoiceSM or null if no invoices are found.</returns>
        public async Task<List<UserInvoiceSM>?> GetAllUserInvoices()
        {
            var userInvoicesFromDb = await _apiDbContext.UserInvoices.ToListAsync();
                if (userInvoicesFromDb == null)
            {
                return null;
            }
            return _mapper.Map<List<UserInvoiceSM>>(userInvoicesFromDb);
            
        }
        /// <summary>
        /// Retrieves a list of invoices for a specific user based on their user ID.
        /// This method queries the database for license details associated with the user
        /// that have a valid Stripe subscription ID. It then fetches the invoices related
        /// to those license details and returns them as a list.
        /// </summary>
        /// <param name="userId">The unique identifier of the user for whom invoices are to be retrieved.</param>
        /// <returns>A task representing the asynchronous operation, with a list of user invoices as the result.</returns>
        /// <exception cref="SMSException">Thrown when no invoices are found for the given user.</exception>
        public async Task<List<UserInvoiceSM>>GetMineInvoices(int userId)
        {
            var licenseDetailIds = await _apiDbContext.UserLicenseDetails.Where(x=>x.ClientUserId == userId && x.StripeSubscriptionId != null).Select(x=>x.Id).ToListAsync();
            if(licenseDetailIds.Count == 0)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "No Invoices Found", "No Invoices Found");
            }

            var response = new List<UserInvoiceSM>();
            foreach(var id in licenseDetailIds)
            {
                var res = await GetUserInvoicesByUserLicenseDetailId(id);
                if(res != null)
                {
                    response.AddRange(res);
                }
            }
            return response;
        }

        public async Task<List<UserInvoiceSM?>> GetUserInvoicesByUserLicenseDetailId(int id)
        {
            var dms = await _apiDbContext.UserInvoices.Where(x=>x.UserLicenseDetailsId == id).ToListAsync();
            if (dms.Count == 0)
            {
                return null;
            }
            return _mapper.Map<List<UserInvoiceSM>>(dms);
            
        }

        #endregion Get All


        #endregion Get All

        #region Get Single

        #region Get By Id
        /// <summary>
        /// Retrieves a user invoice by its unique ID.
        /// </summary>
        /// <param name="Id">The ID of the user invoice to retrieve.</param>
        /// <returns>The UserInvoiceSM with the specified ID, or null if not found.</returns>
        public async Task<UserInvoiceSM?> GetUserInvoiceById(int Id)
        {
            try
            {
                var singleUserInvoiceFromDb = await _apiDbContext.UserInvoices.FindAsync(Id);
                if (singleUserInvoiceFromDb == null)
                    return null;
                return _mapper.Map<UserInvoiceSM>(singleUserInvoiceFromDb);
            }
            catch (Exception ex)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, @$"{ex.Message}", @"Could not get user invoice, please try again", ex.InnerException);
            }
        }

        #endregion Get By Id

        #region Get By InvoiceId
        public async Task<UserInvoiceSM?> GetSingleInvoiceByStripeInvoiceId(string id)
        {
            UserInvoiceDM? UserInvoiceDb = await _apiDbContext.UserInvoices.FirstOrDefaultAsync(x => x.StripeInvoiceId == id);
            if (UserInvoiceDb != null)
            {
                var UserInvoiceSM = _mapper.Map<UserInvoiceSM>(UserInvoiceDb);
                return UserInvoiceSM;
            }
            else
            {
                return null;
            }
        }
        #endregion Get By InvoiceId

        #endregion Get Single

        #region Add
        /// <summary>
        /// Adds a new user to the database.
        /// </summary>
        /// <param name="userInvoiceSM">The UserInvoiceSM to be added.</param>
        /// <returns>The added UserInvoiceSM, or null if addition fails.</returns>
        public async Task<UserInvoiceSM?> AddUserInvoice(UserInvoiceSM userInvoiceSM)
        {
            if (userInvoiceSM == null)
                return null;

            var userInvoiceDM = _mapper.Map<UserInvoiceDM>(userInvoiceSM);
            userInvoiceDM.CreatedBy = _loginUserDetail.LoginId;
            userInvoiceDM.CreatedOnUTC = DateTime.UtcNow;

            try
            {
                await _apiDbContext.UserInvoices.AddAsync(userInvoiceDM);
                if (await _apiDbContext.SaveChangesAsync() > 0)
                {
                    return _mapper.Map<UserInvoiceSM>(userInvoiceDM);
                }
            }
            catch (Exception ex)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, @$"{ex.Message}", @"Could not add user invoice, please try again", ex.InnerException);
            }

            return null;
        }

        #endregion Add

        #region Update
        /// <summary>
        /// Updates a user invoice in the database.
        /// </summary>
        /// <param name="objIdToUpdate">The Id of the job opening to update.</param>
        /// <param name="userInvoiceSM">The updated UserInvoiceSM object.</param>
        /// <returns>
        /// If successful, returns the updated UserInvoiceSM; otherwise, returns null.
        /// </returns>
        public async Task<UserInvoiceSM?> UpdateUserInvoice(int objIdToUpdate, UserInvoiceSM userInvoiceSM)
        {
            try
            {
                if (userInvoiceSM != null && objIdToUpdate > 0)
                {
                    //retrieves target user invoice from db
                    //UserInvoiceDM? objDM = await _apiDbContext.UserInvoices.FindAsync(objIdToUpdate);
                    UserInvoiceDM? objDM = await _apiDbContext.UserInvoices.Where(x=>x.Id == objIdToUpdate).FirstOrDefaultAsync();

                    if (objDM != null)
                    {
                        userInvoiceSM.Id = objIdToUpdate;
                        _mapper.Map(userInvoiceSM, objDM);

                        objDM.LastModifiedBy = _loginUserDetail.LoginId;
                        objDM.LastModifiedOnUTC = DateTime.UtcNow;

                        if (await _apiDbContext.SaveChangesAsync() > 0)
                        {
                            return _mapper.Map<UserInvoiceSM>(objDM);
                        }
                        return null;
                    }
                    else
                    {
                        throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"User invoice not found: {objIdToUpdate}", "User invoice to update not found, add as new instead.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, @$"{ex.Message}", @"Could not user invoice, please try again", ex.InnerException);
            }
            return null;
        }

        #endregion Update

        #region Delete
        /// <summary>
        /// Deletes a user invoice by its unique ID.
        /// </summary>
        /// <param name="id">The ID of the user invoice to be deleted.</param>
        /// <returns>A DeleteResponseRoot indicating the result of the deletion operation.</returns>
        public async Task<DeleteResponseRoot> DeleteUserInvoiceById(int id)
        {
            try
            {
                // Check if the product with the specified ID exists in the database
                var isPresent = await _apiDbContext.UserInvoices.AnyAsync(x => x.Id == id);

                if (isPresent)
                {
                    // Create an instance of ProductDM with the specified ID for deletion
                    var dmToDelete = new UserInvoiceDM() { Id = id };

                    // Remove the user invoice from the database
                    _apiDbContext.UserInvoices.Remove(dmToDelete);

                    // Save changes to the database
                    if (await _apiDbContext.SaveChangesAsync() > 0)
                    {
                        // If deletion is successful, return a success response
                        return new DeleteResponseRoot(true, "User invoice with Id " + id + " deleted successfully!");
                    }
                }

                // If no product was found with the specified ID, return a failure response
                return new DeleteResponseRoot(false, "No such invoice found");
            }
            catch (Exception ex)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, @$"{ex.Message}", @"Could not delete user invoice, please try again", ex.InnerException);
            }
        }

        #endregion Delete

        #region Download Invoice

        public async Task<string> GetInvoiceContextFromStripe(string stripeInvoiceId)
        {
            try
            {
                var invoiceService = new InvoiceService();

                // Fetch the specific invoice by its ID and expand to include charge information.
                var invoiceOptions = new InvoiceGetOptions
                {
                    Expand = new List<string> { "charge" } // Expand to include charge information
                };
                var invoice = await invoiceService.GetAsync(stripeInvoiceId, invoiceOptions);

                if (invoice == null)// || invoice.Status != "paid")
                {
                    throw new SMSException(ApiErrorTypeSM.Fatal_Log, "No invoice found for the provided invoice ID.", "No invoice found for the provided invoice ID.");
                }

                // Access charge information from the expanded data
                var charge = invoice.Charge;

                // Fetch the invoice PDF content from Stripe.
                var pdfStream = await GetInvoicePdfStream(invoice.Id);

                // Convert the PDF stream to a byte array
                byte[] pdfBytes;
                using (var memoryStream = new MemoryStream())
                {
                    pdfStream.CopyTo(memoryStream);
                    pdfBytes = memoryStream.ToArray();
                }

                // Convert the byte array to a base64 string
                string base64Pdf = Convert.ToBase64String(pdfBytes);

                return base64Pdf;
            }
            catch (StripeException ex)
            {
                // Handle Stripe API errors.
                throw ex;
            }
            catch (SMSException ex)
            {
                // Handle other unexpected errors.
                throw ex;
            }
        }

        private async Task<Stream> GetInvoicePdfStream(string invoiceId)
        {
            var invoiceService = new InvoiceService();
            var invoice = await invoiceService.GetAsync(invoiceId);

            // Fetch the invoice PDF URL from the invoice object.
            string pdfUrl = invoice.InvoicePdf;

            // Create a WebClient to download the PDF content.
            using (var httpClient = new HttpClient())
            {
                byte[] pdfBytes = await httpClient.GetByteArrayAsync(pdfUrl);

                // Convert the byte array to a memory stream.
                var pdfStream = new MemoryStream(pdfBytes);

                return pdfStream;
            }
        }
        #endregion Download Invoice
    }
}

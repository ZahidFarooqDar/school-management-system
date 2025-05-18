using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SMSBAL.ExceptionHandler;
using SMSBAL.Foundation.Base;
using SMSBAL.Foundation.CommonUtils;
using SMSDAL.Context;
using SMSDomainModels.Client;
using SMSServiceModels.Client;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.Foundation.Base.Interfaces;

namespace SMSBAL.Clients
{
    public class ClientCompanyDetailProcess : CoreVisionBalOdataBase<ClientCompanyDetailSM>
    {
        #region Peoperties

        private readonly ILoginUserDetail _loginUserDetail;

        #endregion Peoperties

        #region Constructor
        public ClientCompanyDetailProcess(IMapper mapper, ILoginUserDetail loginUserDetail, ApiDbContext apiDbContext)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
        }

        #endregion Constructor

        #region Odata
        /// <summary>
        /// Odata for ClientCompanyDetails
        /// </summary>
        /// <returns>
        /// If Successful, Returns IQuerable ClientCompanyDetailsSM
        /// </returns>
        public override async Task<IQueryable<ClientCompanyDetailSM>> GetServiceModelEntitiesForOdata()
        {
            var entitySet = _apiDbContext.ClientCompanyDetails;
            IQueryable<ClientCompanyDetailSM> retSM = await MapEntityAsToQuerable<ClientCompanyDetailDM, ClientCompanyDetailSM>(_mapper, entitySet);
            return retSM;
        }

        #endregion Odata

        #region CRUD 

        #region Get
        /// <summary>
        /// Ferches All the client companies from database
        /// </summary>
        /// <returns>
        /// If Successful Returns List of ClientCompanySM
        /// </returns>
        public async Task<List<ClientCompanyDetailSM>> GetAllClientCompanyDetails()
        {
            var dm = await _apiDbContext.ClientCompanyDetails.AsNoTracking().ToListAsync();
            var sm = _mapper.Map<List<ClientCompanyDetailSM>>(dm);
            return sm;
        }

        public async Task<ClientCompanyDetailSM> GetClientCompanyDetailByCompanyCode(string cCode)
        {
            ClientCompanyDetailDM objDM = await _apiDbContext.ClientCompanyDetails.FirstOrDefaultAsync(x => string.Equals(x.CompanyCode, cCode));

            if (objDM != null)
            {
                return _mapper.Map<ClientCompanyDetailSM>(objDM);
            }
            else
            {
                return null;
            }
        }
        public async Task<ClientCompanyDetailSM> GetClientCompanyByCompanyCode(string companyCode)
        {
            var dm = await _apiDbContext.ClientCompanyDetails.AsNoTracking().Where(c => c.CompanyCode == companyCode).FirstOrDefaultAsync();
            var sm = _mapper.Map<ClientCompanyDetailSM>(dm);
            return sm;
        }

        public async Task<ClientCompanyDetailSM?> GetClientCompanyByEmail(string email)
        {
            var companyId = await _apiDbContext.ClientUsers.Where(x => x.EmailId == email).Select(c => c.ClientCompanyDetailId).FirstOrDefaultAsync();
            //var dm = await _apiDbContext.ClientCompanyDetails.AsNoTracking().Where(c => c.ContactEmail == email).FirstOrDefaultAsync();
            var dm = await _apiDbContext.ClientCompanyDetails.Where(x => x.Id == companyId).FirstOrDefaultAsync();
            if (dm != null)
            {
                return _mapper.Map<ClientCompanyDetailSM>(dm);
            }
            return null;
        }

        /// <summary>
        /// Fetches client company using Id
        /// </summary>
        /// <param name="id">Id of a client company to be fetched</param>
        /// <returns>
        ///  If Successful Returns ClientCompanySM Otherwise returns null
        /// </returns>
        public async Task<ClientCompanyDetailSM> GetClientCompanyDetailById(int id)
        {
            ClientCompanyDetailDM objDM = await _apiDbContext.ClientCompanyDetails.FindAsync(id);

            if (objDM != null)
            {
                var res = _mapper.Map<ClientCompanyDetailSM>(objDM);
                if (!res.CompanyLogoPath.IsNullOrEmpty())
                {
                    res.CompanyLogoPath = await ConvertToBase64(res.CompanyLogoPath);
                }
                else
                {
                    res.CompanyLogoPath = null;
                }
                return res;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Fetches client company using clientId (user)
        /// </summary>
        /// <param name="id">Id of a client company to be fetched</param>
        /// <returns>
        ///  If Successful Returns ClientCompanySM Otherwise returns null
        /// </returns>

        public async Task<ClientCompanyDetailSM> GetClientCompanyDetailByUserId(int id)
        {
            var objDM = await _apiDbContext.ClientUsers.FindAsync(id);
            if (objDM != null)
            {
                return await GetClientCompanyDetailById((int)objDM.ClientCompanyDetailId);
            }
            else
            {
                return null;
            }
        }

        #region Get Count
        /// <summary>
        /// Fetches count of all the client companies
        /// </summary>
        /// <returns>
        /// Returns count (int) 
        /// </returns>
        public async Task<int> GetCountOfClientCompanies()
        {
            var clientsCompaniesCount = await _apiDbContext.ClientCompanyDetails.AsNoTracking().CountAsync();

            return clientsCompaniesCount;
        }

        #endregion Get Count

        #endregion Get

        #region Add Update

        /// <summary>
        /// Creates a new Company in the database
        /// </summary>
        /// <param name="objSM">ClientCompanySM object to be added in the database</param>
        /// <returns>
        /// If Successful, returns newly created ClientCompanySM
        /// </returns>
        public async Task<ClientCompanyDetailSM> AddClientCompany(ClientCompanyDetailSM objSM)
        {
            using (var transaction = await _apiDbContext.Database.BeginTransactionAsync())
            {
                string? companyLogoPath = null;
                var companyDM = _mapper.Map<ClientCompanyDetailDM>(objSM);
                companyDM.CreatedBy = _loginUserDetail.LoginId;
                companyDM.CreatedOnUTC = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(objSM.CompanyLogoPath))
                {
                    companyLogoPath = await SaveFromBase64(objSM.CompanyLogoPath);
                    companyDM.CompanyLogoPath = companyLogoPath;
                }
                else
                {
                    companyDM.CompanyLogoPath = null;
                }

                var companyCode = GenerateCompanyCode();
                companyDM.CompanyCode = companyCode;
                
                await _apiDbContext.ClientCompanyDetails.AddAsync(companyDM);

                if (await _apiDbContext.SaveChangesAsync() <= 0)
                {
                    await transaction.RollbackAsync();
                    return null;
                }
                await transaction.CommitAsync();
                return _mapper.Map<ClientCompanyDetailSM>(companyDM);
            }
        }

        

        #region Update  Company Details

        /// <summary>
        /// Updates ClientCompanyDetail in the database
        /// </summary>
        /// <param name="objSM">ClientCompanyDetailSM object to update</param>
        /// <returns>
        /// If Successful, Returns updated ClientCompanyDetailSM, Otherwise returns null
        /// </returns>
        /// <exception cref="SMSException"></exception>
        public async Task<ClientCompanyDetailSM> UpdateClientCompany(int objIdToUpdate, ClientCompanyDetailSM objSM)
        {
            if (objSM == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Nothing to Update", "Nothing to Update");
            }

            ClientCompanyDetailDM objDM = await _apiDbContext.ClientCompanyDetails
                .Where(cc => cc.Id == objIdToUpdate)
                .FirstOrDefaultAsync();

            if (objDM != null)
            {
                objSM.Id = objDM.Id;
                objSM.CompanyCode = objDM.CompanyCode;
                if (!objSM.CompanyLogoPath.IsNullOrEmpty())
                {
                    var IsCompanyLogoUpdated = await UpdateCompanyLogo(objDM.CompanyCode, objSM.CompanyLogoPath);
                    if (IsCompanyLogoUpdated != null)
                    {
                        objSM.CompanyLogoPath = IsCompanyLogoUpdated.CompanyLogoPath;
                    }
                }
                else
                {
                    objSM.CompanyLogoPath = null;
                }


                var smProperties = objSM.GetType().GetProperties();
                var dmProperties = objDM.GetType().GetProperties();

                foreach (var smProperty in smProperties)
                {
                    var smValue = smProperty.GetValue(objSM, null);

                    // Find the corresponding property in objDM with the same name
                    var dmProperty = dmProperties.FirstOrDefault(p => p.Name == smProperty.Name);

                    if (dmProperty != null)
                    {
                        var dmValue = dmProperty.GetValue(objDM, null);

                        // Check if the value in objSM is null or empty, and update it with the corresponding value from objDM
                        if ((smValue == null || smValue is string strValue && string.IsNullOrEmpty(strValue)) && dmValue != null)
                        {
                            smProperty.SetValue(objSM, dmValue, null);
                        }
                    }
                }

                _mapper.Map(objSM, objDM);

                objDM.LastModifiedBy = _loginUserDetail.LoginId;
                objDM.LastModifiedOnUTC = DateTime.UtcNow;
                await _apiDbContext.SaveChangesAsync();
                return _mapper.Map<ClientCompanyDetailSM>(objDM);
            }
            else
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Client Company Detail not found: ", "Data to update not found, add as new instead.");
            }
        }

        #endregion Update  Company Details

        #region Update Company Logo
        /// <summary>
        /// Updates Company Logo Using companyCode 
        /// </summary>
        /// <param name="companyCode">CompanyCode of a company to which we need to update Logo</param>
        /// <param name="base64String">Base64 string of a Logo</param>
        /// <returns>
        /// If Successful, Returns updated UserCompanyDetailSM, Otherwise returns null
        /// </returns>
        /// <exception cref="Farm2iException"></exception>
        public async Task<ClientCompanyDetailSM> UpdateCompanyLogo(string companyCode, string base64String)
        {

            var objDM = await _apiDbContext.ClientCompanyDetails.Where(cc => cc.CompanyCode == companyCode).FirstOrDefaultAsync();

            if (objDM == null)
            {
                // If UserCompanyDetailDM is not found, throw an exception
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Company Details not found...Please check Again", "Company Details not found...Please check Again");
            }

            var imageFullPath = "";

            if (objDM.CompanyLogoPath != null)
            {
                imageFullPath = Path.GetFullPath(objDM.CompanyLogoPath);
            }

            if (base64String == null)
            {
                // If base64String is null, update CompanyLogoPath to null
                objDM.CompanyLogoPath = null;
            }
            else
            {
                // Convert base64String to image and store it inside a folder
                // Return the relative path of the image
                var imageRelativePath = await SaveFromBase64(base64String);

                if (imageRelativePath != null)
                {
                    objDM.CompanyLogoPath = imageRelativePath;
                }
            }
            objDM.LastModifiedBy = _loginUserDetail?.LoginId;
            objDM.LastModifiedOnUTC = DateTime.UtcNow;
            await _apiDbContext.SaveChangesAsync();

            // Needs to Delete Previous Logo if Existed
            /*if (File.Exists(imageFullPath))
                File.Delete(imageFullPath);*/

            // Return a mapped object (you might need to adjust this based on your requirements)
            return _mapper.Map<ClientCompanyDetailSM>(objDM);
        }

        #endregion Update Company Logo

        public async Task<string> AddOrUpdateCompanyDetailLogoInDb(int companyId, string webRootPath, IFormFile postedFile)
        {
            var companyDM = await _apiDbContext.ClientCompanyDetails.FirstOrDefaultAsync(x => x.Id == companyId);
            if (companyDM != null)
            {
                var currLogoPath = companyDM.CompanyLogoPath;
                var targetRelativePath = Path.Combine("content\\companies\\logos", $"{companyId}_{Guid.NewGuid()}_original{Path.GetExtension(postedFile.FileName)}");
                var targetPath = Path.Combine(webRootPath, targetRelativePath);
                if (await SavePostedFileAtPath(postedFile, targetPath))
                {
                    //Entry Method//
                    //var comp = new ClientCompanyDetailDM() { Id = companyId, CompanyLogoPath = targetRelativePath };
                    //_apiDbContext.ClientCompanyDetails.Attach(comp);
                    //_apiDbContext.Entry(comp).Property(e => e.CompanyLogoPath).IsModified = true;
                    companyDM.CompanyLogoPath = targetRelativePath.ConvertFromFilePathToUrl();
                    companyDM.LastModifiedBy = _loginUserDetail.LoginId;
                    companyDM.LastModifiedOnUTC = DateTime.UtcNow;
                    if (await _apiDbContext.SaveChangesAsync() > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(currLogoPath))
                        { File.Delete(Path.Combine(webRootPath, currLogoPath)); }
                        return targetRelativePath.ConvertFromFilePathToUrl();
                    }
                }
            }
            return "";
        }

        #endregion Add Update

        #region Delete
        public async Task<DeleteResponseRoot> DeleteClientCompanyDetailById(int id)
        {
            var clientCompany = await _apiDbContext.ClientCompanyDetails.AsNoTracking().Where(x => x.Id == id).FirstOrDefaultAsync();
            string imageFullPath = null;

            if (clientCompany != null)
            {
                if (!clientCompany.CompanyLogoPath.IsNullOrEmpty())
                {
                    imageFullPath = Path.GetFullPath(clientCompany.CompanyLogoPath);
                }
                var dmToDelete = new ClientCompanyDetailDM() { Id = id };
                //Handle if we need to delete ClientUsers of company as well if we delete Company


                _apiDbContext.ClientCompanyDetails.Remove(dmToDelete);
                if (await _apiDbContext.SaveChangesAsync() > 0)
                {
                    // Todo: Delete the Image as well 
                    /*if (File.Exists(imageFullPath))
                        File.Delete(imageFullPath);*/
                    return new DeleteResponseRoot(true, "Client Company Deleted Successfully");
                }
            }
            return new DeleteResponseRoot(false, "Item Not found");

        }

        public async Task<DeleteResponseRoot> DeleteClientCompanyDetailLogoById(int companyId, string webRootPath)
        {
            var companyDM = await _apiDbContext.ClientCompanyDetails.FirstOrDefaultAsync(x => x.Id == companyId);
            if (companyDM != null)
            {
                var currLogoPath = companyDM.CompanyLogoPath;
                companyDM.CompanyLogoPath = "";
                companyDM.LastModifiedBy = _loginUserDetail.LoginId;
                companyDM.LastModifiedOnUTC = DateTime.UtcNow;

                if (await _apiDbContext.SaveChangesAsync() > 0)
                {
                    if (!string.IsNullOrWhiteSpace(currLogoPath))
                    {
                        File.Delete(Path.Combine(webRootPath, currLogoPath));
                        return new DeleteResponseRoot(true);
                    }
                }
            }
            return new DeleteResponseRoot(false, "Company or Logo Not found");
        }

        #endregion Delete

        #endregion CRUD

        #region Private Functions

        #region Save From Base64 and Convert to Base64

        /// <summary>
        /// Saves a base64 encoded string as a jpg/jpeg/png etc file on the server.
        /// </summary>
        /// <param name="base64String">The base64 encoded string of the png extension</param>
        /// <returns>
        /// If successful, returns the relative file path of the saved file; 
        /// otherwise, returns null.
        /// </returns>
        static async Task<string?> SaveFromBase64(string base64String)
        {
            string imageExtension = "jpg";
            string? filePath = null;
            try
            {
                //convert bas64string to bytes
                byte[] imageBytes = Convert.FromBase64String(base64String);

                // Check if the file size exceeds 1MB (2 * 1024 * 1024 bytes)
                if (imageBytes.Length > 2 * 1024 * 1024) //change 1 to desired size 2,3,4 etc
                {
                    throw new Exception("File size exceeds 2 Mb limit.");
                }

                string fileName = Guid.NewGuid().ToString() + "." + imageExtension;

                // Specify the folder path where resumes are stored
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\content\companies/logos");

                // Create the folder if it doesn't exist
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Combine the folder path and file name to get the full file path
                filePath = Path.Combine(folderPath, fileName);

                // Write the bytes to the file asynchronously
                await File.WriteAllBytesAsync(filePath, imageBytes);

                // Return the relative file path
                return Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath);
            }
            catch
            {
                // If an error occurs, delete the file (if created) and return null
                if (File.Exists(filePath))
                    File.Delete(filePath);
                throw;
            }
        }

        /// <summary>
        /// Converts an image file to a base64 encoded string.
        /// </summary>
        /// <param name="filePath">The path to the image file.</param>
        /// <returns>
        /// If successful, returns the base64 encoded string; 
        /// otherwise, returns null.
        /// </returns>
        private async Task<string?> ConvertToBase64(string filePath)
        {
            try
            {
                // Read all bytes from the file asynchronously
                byte[] resumeBytes = await File.ReadAllBytesAsync(filePath);

                // Convert the bytes to a base64 string
                return Convert.ToBase64String(resumeBytes);
            }
            catch (Exception ex)
            {
                // Handle exceptions and return null
                return null;
            }
        }
        #endregion Save From Base64

        #region Generate Company Code

        /// <summary>
        /// Method used for generating CompanyCode
        /// </summary>
        /// <returns>
        /// Returns newly generated CompanyCode (string)
        /// </returns>
        public string GenerateCompanyCode()
        {
            // Get the maximum company code from the database
            string maxCompanyCode = _apiDbContext.ClientCompanyDetails
                .Select(u => u.CompanyCode)
                .OrderByDescending(c => c)
                .FirstOrDefault();
            if (maxCompanyCode == null)
            {
                maxCompanyCode = "100";
            }

            // Increment the maximum company code by 1
            int newCompanyCode = int.Parse(maxCompanyCode) + 1;
            return newCompanyCode.ToString().PadLeft(maxCompanyCode.Length, '0');
        }



        #endregion Generate Company Code

        #endregion Private Functions
    }
}

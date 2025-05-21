using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SMSDAL.Context;
using SMSServiceModels.Enums;
using SMSServiceModels.AppUser;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.Foundation.Base.Interfaces;
using SMSDomainModels.AppUser;
using SMSDomainModels.Enums;
using SMSBAL.Foundation.Base;
using SMSBAL.ExceptionHandler;
using SMSBAL.Clients;

namespace SMSBAL.AppUsers
{
    public partial class ExternalUserProcess : SMSBalBase
    {
        #region Properties

        private readonly ILoginUserDetail _loginUserDetail;
        private readonly ClientCompanyDetailProcess _clientCompanyDetailProcess;

        #endregion Properties

        #region Constructor
        public ExternalUserProcess(IMapper mapper, ApiDbContext context, ILoginUserDetail loginUserDetail, ClientCompanyDetailProcess clientCompanyDetailProcess)
            : base(mapper, context)
        {
            _loginUserDetail = loginUserDetail;
            _clientCompanyDetailProcess = clientCompanyDetailProcess;
        }

        #endregion Constructor

        #region Get
        /// <summary>
        /// Fetches Client User using user id fk
        /// </summary>
        /// <param name="clientUserId"></param>
        /// <param name="userType"></param>
        /// <returns></returns>
        public async Task<ExternalUserSM> GetExternalUserByClientUserIdandTypeAsync(int clientUserId, ExternalUserTypeSM userType)
        {
            var externalUser = await _apiDbContext.ExternalUsers.FirstOrDefaultAsync(u => u.ClientUserId == clientUserId && u.ExternalUserType == (ExternalUserTypeDM)userType);
            if (externalUser == null)
            {
                return null;
            }
            return _mapper.Map<ExternalUserSM>(externalUser);
        }
        #endregion

        #region Add / Update

        /// <summary>
        /// Creates new  External User (Facebook/Google)
        /// </summary>
        /// <param name="sm"></param>
        /// <returns></returns>
        public async Task<ExternalUserSM> AddExternalUser(ExternalUserSM sm)
        {
            ExternalUserDM dm = _mapper.Map<ExternalUserDM>(sm);
            dm.CreatedBy = _loginUserDetail.LoginId;
            dm.CreatedOnUTC = DateTime.UtcNow;
            await _apiDbContext.ExternalUsers.AddAsync(dm);
            if (await _apiDbContext.SaveChangesAsync() > 0)
            {
                return _mapper.Map<ExternalUserSM>(dm);
            }
            return null;
        }

        /// <summary>
        /// Creates a new External Login User based on Google Login
        /// </summary>
        /// <param name="signUpSM"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="SMSException"></exception>
        public async Task<ClientUserSM> AddClientUserandExternalUserDetails(ClientUserSM signUpSM, string refreshToken, string companyCode, ExternalUserTypeSM externalUserType)
        {
            using (var transaction = await _apiDbContext.Database.BeginTransactionAsync())
            {
                // Todo: Where we need to add company for external auth user or not
                var objDM = _mapper.Map<ClientUserDM>(signUpSM);
                
                signUpSM.ProfilePicturePath = signUpSM.ProfilePicturePath.IsNullOrEmpty() ? null : await SaveFromBase64(signUpSM.ProfilePicturePath);
                var companyDetail = await _clientCompanyDetailProcess.GetClientCompanyDetailByCompanyCode(companyCode);
                if (companyDetail == null)
                    throw new SMSException(ApiErrorTypeSM.NoRecord_Log, "Error in adding user, Try after sometime",
                        $"Company with companyCode= {companyCode} not found in db");

                objDM.ClientCompanyDetailId = companyDetail.Id;
                objDM.RoleType = RoleTypeDM.Admin;
                objDM.PasswordHash = null; //Todo: Null Password as its logged in/signed up from google
                objDM.CreatedBy = _loginUserDetail.LoginId;
                objDM.CreatedOnUTC = DateTime.UtcNow;
                objDM.IsPhoneNumberConfirmed = false;
                objDM.LoginStatus = LoginStatusDM.Enabled;
                await _apiDbContext.ClientUsers.AddAsync(objDM);
                if (await _apiDbContext.SaveChangesAsync() > 0)
                {
                    var externalUser = new ExternalUserDM()
                    {
                        ClientUserId = objDM.Id,
                        RefreshToken = refreshToken,
                        ExternalUserType = (ExternalUserTypeDM)externalUserType,
                        CreatedBy = _loginUserDetail.LoginId,
                        CreatedOnUTC = DateTime.UtcNow,
                    };
                    await _apiDbContext.ExternalUsers.AddAsync(externalUser);
                    await _apiDbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return _mapper.Map<ClientUserSM>(objDM);
                }

                await transaction.RollbackAsync();
                throw new SMSException(ApiErrorTypeSM.NoRecord_Log, "Error in adding user, Try after sometime",
                        $"Error in adding user, Try after sometime");
            }
        }

        public async Task<ExternalUserSM> UpdateExternalUser(int objIdToUpdate, ExternalUserSM externalGoogleUser)
        {
            if (externalGoogleUser != null && objIdToUpdate > 0)
            {
                var objDM = await _apiDbContext.ExternalUsers.FindAsync(objIdToUpdate);
                if (objDM != null)
                {
                    externalGoogleUser.Id = objIdToUpdate;
                    _mapper.Map(externalGoogleUser, objDM);

                    objDM.LastModifiedBy = _loginUserDetail.LoginId;
                    objDM.LastModifiedOnUTC = DateTime.UtcNow;

                    if (await _apiDbContext.SaveChangesAsync() > 0)
                    {
                        return _mapper.Map<ExternalUserSM>(objDM);
                    }
                    return null;
                }
                else
                {
                    throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"ClientUser not found: {objIdToUpdate}", "Data to update not found, add as new instead.");
                }
            }
            return null;
        }

        #endregion

        #region Additional Methods

        /// <summary>
        /// Saves uploaded image (base64)
        /// </summary>
        /// <param name="base64String"></param>
        /// <returns>
        /// returns the relative path of the saved image
        /// </returns>
        static async Task<string?> SaveFromBase64(string base64String)
        {
            string? filePath = null;
            string? imageExtension = "jpg";
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
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\content\loginusers\profile");

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

        #endregion Additional Methods

    }
}

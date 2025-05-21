using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SMSBAL.ExceptionHandler;
using SMSBAL.Foundation.Base;
using SMSDAL.Context;
using SMSDomainModels.Enums;
using SMSServiceModels.AppUser;
using SMSServiceModels.AppUser.Login;
using SMSServiceModels.Enums;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.Foundation.Base.Interfaces;
using SMSServiceModels.Foundation.Token;

namespace SMSBAL.Token
{
    public partial class TokenProcess : SMSBalBase
    {
        #region Properties

        private readonly IPasswordEncryptHelper _passwordEncryptHelper;

        #endregion Properties

        #region Constructor
        public TokenProcess(IMapper mapper, ApiDbContext context, IPasswordEncryptHelper passwordEncryptHelper) : base(mapper, context)
        {
            _passwordEncryptHelper = passwordEncryptHelper;
        }


        #endregion Constructor

        #region Token
        public async Task<(LoginUserSM, int)> ValidateLoginAndGenerateToken(TokenRequestSM tokenReq)
        {
            LoginUserSM? loginUserSM = null;
            int compId = default;
            // add hash
            var passwordHash = await _passwordEncryptHelper.ProtectAsync(tokenReq.Password);
            switch (tokenReq.RoleType)
            {
                case RoleTypeSM.SystemAdmin:
                    var appUser = await _apiDbContext.ApplicationUsers
                        .FirstOrDefaultAsync(x => x.LoginId == tokenReq.LoginId && x.PasswordHash == passwordHash && x.RoleType == (RoleTypeDM)tokenReq.RoleType);
                    if (appUser != null)
                    { loginUserSM = _mapper.Map<ApplicationUserSM>(appUser); }

                    break;
                case RoleTypeSM.Admin:
                case RoleTypeSM.Student:
                case RoleTypeSM.Parent:
                    {
                        var endUser = await _apiDbContext.ClientUsers
                        .Where(u => (u.EmailId == tokenReq.LoginId || u.LoginId == tokenReq.LoginId) && u.PasswordHash == null)
                        .FirstOrDefaultAsync();
                        if (endUser != null)
                        {

                            // Todo: Decide whether todo password less login or not
                            throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog, @"Please log in using your Google or Facebook account or Click on Forgot Password to change your password", "Please log in using your Google or Facebook account or Click on Forgot Password to change your password");

                        }
                        /*var data = await (from comp in _apiDbContext.ClientCompanyDetails
                                          join user in _apiDbContext.ClientUsers
                                          on comp.Id equals user.ClientCompanyDetailId
                                          where user.LoginId == tokenReq.LoginId && user.PasswordHash == passwordHash
                                          && comp.CompanyCode == tokenReq.CompanyCode && user.RoleType == (RoleTypeDM)tokenReq.RoleType
                                          select new { User = user, CompId = comp.Id }).FirstOrDefaultAsync();*/
                        var cId = await _apiDbContext.ClientCompanyDetails.Where(x => x.CompanyCode == tokenReq.CompanyCode).Select(x => x.Id).FirstOrDefaultAsync();

                        var data = await (from user in _apiDbContext.ClientUsers
                                          where (user.EmailId == tokenReq.LoginId || user.LoginId == tokenReq.LoginId) && user.PasswordHash == passwordHash
                                          select new { User = user, CompId = cId }).FirstOrDefaultAsync();

                        if (data != null && data.User != null)
                        {
                            loginUserSM = _mapper.Map<ClientUserSM>(data.User);
                            compId = data.CompId;
                        }
                    }
                    break;
                /*case RoleTypeSM.CompanyAutomation:
                    {
                        var data = await (from comp in _apiDbContext.ClientCompanyDetails
                                          join user in _apiDbContext.ClientUsers
                                          on comp.Id equals user.ClientCompanyDetailId
                                          where user.LoginId == tokenReq.LoginId && user.PasswordHash == passwordHash
                                          && comp.CompanyCode == tokenReq.CompanyCode && user.RoleType == (RoleTypeDM)tokenReq.RoleType
                                          select new { User = user, CompId = comp.Id }).FirstOrDefaultAsync();
                        if (data != null && data.User != null)
                        {
                            loginUserSM = _mapper.Map<ClientUserSM>(data.User);
                            compId = data.CompId;
                            // loginUserSM.LoginStatus = LoginStatusSM.Enabled;
                        }
                    }
                    break;*/
            }
            if (loginUserSM != null)
            {
                if (!loginUserSM.ProfilePicturePath.IsNullOrEmpty())
                {
                    loginUserSM.ProfilePicturePath = await ConvertToBase64(loginUserSM.ProfilePicturePath);
                }
                else
                {
                    loginUserSM.ProfilePicturePath = null;
                }
            }
            return (loginUserSM, compId);


        }

        #endregion Token

        #region Private Methods

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
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\content\loginusers/profile");

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

        #endregion Private Methods
    }
}

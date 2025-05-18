using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SMSBAL.ExceptionHandler;
using SMSDAL.Context;
using SMSDomainModels.AppUser;
using SMSDomainModels.Enums;
using SMSServiceModels.AppUser;
using SMSServiceModels.Enums;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.Foundation.Base.Interfaces;

namespace SMSBAL.AppUsers
{
    public partial class ApplicationUserProcess : LoginUserProcess<ApplicationUserSM>
    {
        #region Properties
        private readonly IPasswordEncryptHelper _passwordEncryptHelper;
        #endregion Properties

        #region Contructor
        public ApplicationUserProcess(IMapper mapper, ILoginUserDetail loginUserDetail, ApiDbContext apiDbContext, IPasswordEncryptHelper passwordEncryptHelper)
            : base(mapper, loginUserDetail, apiDbContext)
        {
            _passwordEncryptHelper = passwordEncryptHelper;
        }
        #endregion Contructor

        #region Odata
        public override async Task<IQueryable<ApplicationUserSM>> GetServiceModelEntitiesForOdata()
        {
            var entitySet = _apiDbContext.ApplicationUsers;
            IQueryable<ApplicationUserSM> retSM = await MapEntityAsToQuerable<ApplicationUserDM, ApplicationUserSM>(_mapper, entitySet);
            return retSM;
        }

        #endregion Odata

        #region CRUD 

        #region Get
        /// <summary>
        /// Fetches All Application Users
        /// </summary>
        /// <returns>
        /// If Successful, returns List of ApplicationUserSM 
        /// </returns>
        public async Task<List<ApplicationUserSM>> GetAllApplicationUsers(int skip, int top)
        {
            var dm = await _apiDbContext.ApplicationUsers.AsNoTracking()
                .Skip(skip).Take(top)
                .ToListAsync();
            var sm = _mapper.Map<List<ApplicationUserSM>>(dm);
            return sm;
        }

        /// <summary>
        /// Fetches Application User by Id
        /// </summary>
        /// <param name="id">Id of an Application User to be fetched</param>
        /// <returns>
        /// If Successful, returns ApplicationUserSM 
        /// </returns>
        public async Task<ApplicationUserSM> GetApplicationUserById(int id)
        {
            ApplicationUserDM applicationUserDM = await _apiDbContext.ApplicationUsers.FindAsync(id);
            /* //Todo: If we need Password as well
             var passDecryp = await _passwordEncryptHelper.UnprotectAsync<string>(applicationUserDM.PasswordHash);*/
            if (applicationUserDM != null)
            {

                var sm = _mapper.Map<ApplicationUserSM>(applicationUserDM);
                if (!sm.ProfilePicturePath.IsNullOrEmpty())
                {
                    sm.ProfilePicturePath = await ConvertToBase64(sm.ProfilePicturePath);

                }
                return sm;
            }
            else
            {
                return null;
            }
        }

        #region Count

        /// <summary>
        /// Get ApplicationUsers Count in database.
        /// </summary>
        /// <returns>integer response</returns>

        public async Task<int> GetAllApplicationUsersCountResponse()
        {
            int resp = _apiDbContext.ApplicationUsers.AsNoTracking().Count();
            return resp;
        }

        #endregion Count

        #endregion Get

        #region Add Update
        /// <summary>
        /// Creates a new ApplicationUser in the database
        /// </summary>
        /// <param name="applicationUserSM">ApplicationUser object to add</param>
        /// <returns>
        /// If Successful, returns newly created ApplicationUserSM 
        /// </returns>
        /// <exception cref="SMSException"></exception>
        public async Task<ApplicationUserSM> AddApplicationUser(ApplicationUserSM applicationUserSM, string roleType)
        {
            if (applicationUserSM == null)
            {
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog, @"Please Provide details to Add new User", "Please Provide details to Add new User");
            }

            if (roleType.ToString() == RoleTypeSM.SystemAdmin.ToString() && applicationUserSM.RoleType.ToString() != RoleTypeSM.SystemAdmin.ToString())
            {
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog, "Access Denied to Add another type of User", "Access Denied to Add another type of User");
            }
            var existingUserWithEmail = await _apiDbContext.ApplicationUsers.Where(x => x.EmailId == applicationUserSM.EmailId).FirstOrDefaultAsync();
            var existingUserWithLoginId = await _apiDbContext.ApplicationUsers.Where(x => x.LoginId == applicationUserSM.LoginId).FirstOrDefaultAsync();
            if (existingUserWithEmail != null)
            {
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog, "User With EmailId already existed...Try another EmailId", "User With EmailId already existed...Try another EmailId");
            }
            if (existingUserWithLoginId != null)
            {
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog, "User With LoginId already existed...Try another LoginId", "User With LoginId already existed...Try another LoginId");
            }
            string profilePicturePath = null;
            var objDM = _mapper.Map<ApplicationUserDM>(applicationUserSM);
            objDM.CreatedBy = _loginUserDetail.LoginId;
            objDM.CreatedOnUTC = DateTime.UtcNow;
            //Todo: Check about isEmailConformed, IsPhoneNumberConfrmed and LoginStatus
            objDM.IsEmailConfirmed = true;
            objDM.IsPhoneNumberConfirmed = true;
            // objDM.LoginStatus = DomainModels.Enums.LoginStatusDM.Enabled;
            if (objDM.PasswordHash.IsNullOrEmpty())
            {
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog, @"Password is Mandatory", "Please Enter  Password");
            }
            var passHash = await _passwordEncryptHelper.ProtectAsync<string>(objDM.PasswordHash);
            if (passHash != null)
            {
                objDM.PasswordHash = passHash;
            }
            if (!objDM.ProfilePicturePath.IsNullOrEmpty())
            {
                profilePicturePath = await SaveFromBase64(objDM.ProfilePicturePath);

            }

            objDM.ProfilePicturePath = profilePicturePath;
            await _apiDbContext.ApplicationUsers.AddAsync(objDM);
            if (await _apiDbContext.SaveChangesAsync() > 0)
            {
                var response = await GetApplicationUserById(objDM.Id);
                return response;
                //return _mapper.Map<ApplicationUserSM>(objDM);
            }
            else
            {
                return null;
            }
        }


        public async Task<string> AddOrUpdateProfilePictureInDb(int userId, string webRootPath, IFormFile postedFile)
            => await AddOrUpdateProfilePictureInDb(await _apiDbContext.ClientUsers.FirstOrDefaultAsync(x => x.Id == userId), webRootPath, postedFile);


        /// <summary>
        /// Updates ApplicationUser in the database using Id
        /// </summary>
        /// <param name="userId">Id of an ApplicationUser to Update</param>
        /// <param name="objSM">ApplicationUser Object to update </param>
        /// <returns>
        /// If Successful, returns ApplicationUserSM 
        /// </returns>
        /// <exception cref="SMSException"></exception>
        public async Task<ApplicationUserSM> UpdateApplicationUser(int userId, ApplicationUserSM objSM)
        {
            if (userId == null)
            {
                throw new SMSException(ApiErrorTypeSM.NoRecord_NoLog, $"Please Provide Value to Id", $"Please Provide Value to Id");
            }

            if (objSM == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Nothing to Update", "Nothing to Update");
            }

            ApplicationUserDM objDM = await _apiDbContext.ApplicationUsers.FindAsync(userId);
            if (objDM == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"User Not Found", "User Not Found");
            }
            if (!objSM.LoginId.IsNullOrEmpty())
            {
                if (objSM.LoginId != objDM.LoginId && objSM.LoginId.Length < 5)
                {
                    throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog, @"Please provide LoginId with minimum 5 characters", "Please provide LoginId with minimum 5 characters");
                }
            }

            if (objSM.PasswordHash.IsNullOrEmpty())
            {
                objSM.PasswordHash = objDM.PasswordHash;
            }
            else
            {
                var passHash = await _passwordEncryptHelper.ProtectAsync(objSM.PasswordHash);
                objSM.PasswordHash = passHash;
            }

            var existingClient = await _apiDbContext.ApplicationUsers
                   .Where(l => l.LoginId == objSM.LoginId)
                   .FirstOrDefaultAsync();

            var existingClientWithEmail = await _apiDbContext.ApplicationUsers
               .Where(l => l.EmailId == objSM.EmailId)
               .FirstOrDefaultAsync();
            string imageFullPath = null;
            if (objDM != null)
            {
                objSM.Id = objDM.Id;
                //objSM.PasswordHash = objDM.PasswordHash;
                objSM.RoleType = (RoleTypeSM)objDM.RoleType;
                //Todo: Whether ApplicationUser can update EmailConfirmation, PhoneNumberVerified Login Status
                objSM.IsEmailConfirmed = objDM.IsEmailConfirmed;
                objSM.IsPhoneNumberConfirmed = objDM.IsPhoneNumberConfirmed;
                objSM.LoginStatus = (LoginStatusSM)objDM.LoginStatus;

                if (!objSM.ProfilePicturePath.IsNullOrEmpty())
                {
                    if (!objDM.ProfilePicturePath.IsNullOrEmpty())
                    {
                        imageFullPath = Path.GetFullPath(objDM.ProfilePicturePath);
                    }
                    var IsCompanyLogoUpdated = await UpdateProfilePicture(userId, objSM.ProfilePicturePath);
                    if (IsCompanyLogoUpdated == true)
                    {
                        objSM.ProfilePicturePath = null;
                    }
                }
                else
                {
                    objSM.ProfilePicturePath = objDM.ProfilePicturePath;
                }

                if (existingClient != null && objSM.LoginId != objDM.LoginId)
                {
                    throw new SMSException(ApiErrorTypeSM.Access_Denied_Log, $"Application User With Login Id: {objSM.LoginId} Already Existed...Choose Another LoginId", $"Application User With Login Id: {objSM.LoginId} Already Existed...Choose Another LoginId");
                }

                if (existingClientWithEmail != null && objSM.EmailId != objDM.EmailId)
                {
                    throw new SMSException(ApiErrorTypeSM.Access_Denied_Log, $"Application User With Email Id: {objSM.EmailId} Already Existed...Choose Another Email Id", $"Application User With Email Id: {objSM.LoginId} Already Existed...Choose Another EmailId");
                }
                if (objSM.DateOfBirth == default)
                {
                    objSM.DateOfBirth = objDM.DateOfBirth;
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

                if (await _apiDbContext.SaveChangesAsync() > 0)
                {

                    // Todo: Delete the previous image 
                    // Check UpdateProfilePicture for this logic whether we need it to do here or not
                    /*if (File.Exists(imageFullPath))
                        File.Delete(imageFullPath);*/
                    var response = await GetApplicationUserById(userId);
                    return response;
                    //return _mapper.Map<ApplicationUserSM>(objDM);
                }
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Something went wrong while Updating Application User Details", "Something went wrong while Updating Application User Details");
            }
            else
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Application User not found: ", "Data to update not found, add as new instead.");
            }
        }


        #endregion Add Update

        #region Update details for Login Purpose
        /// <summary>
        /// Updates necessary details for Login Purpose
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isEmailConfirmed"></param>
        /// <param name="isPhoneNumberConfirmed"></param>
        /// <param name="loginStatus"></param>
        /// <returns>
        ///  If Successful, returns ApplicationUserSM Otherwise return null
        /// </returns>
        /// <exception cref="SMSException"></exception>
        public async Task<ApplicationUserSM> UpdateDetailsForLoginPurpose(int id, bool isEmailConfirmed, bool isPhoneNumberConfirmed, LoginStatusSM loginStatus)
        {
            var objDM = await _apiDbContext.ApplicationUsers.FindAsync(id);
            if (objDM == null)
            {
                return null;
            }
            objDM.IsEmailConfirmed = isEmailConfirmed;
            objDM.IsPhoneNumberConfirmed = isPhoneNumberConfirmed;
            objDM.LoginStatus = (LoginStatusDM)loginStatus;

            if (await _apiDbContext.SaveChangesAsync() > 0)
            {
                var response = await GetApplicationUserById(id);
                return response;
            }
            throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Something went wrong while Updating Application User Details", "Something went wrong while Updating Application User Details");
        }

        #endregion Update details for Login Purpose

        #region Delete
        /// <summary>
        /// Deletes Application User from the database using Id
        /// </summary>
        /// <param name="id">Id of an ApplicationUser to delete</param>
        /// <returns>
        /// DeleteResponseRoot
        /// </returns>
        public async Task<DeleteResponseRoot> DeleteApplicationUserById(int id)
        {
            var isPresent = await _apiDbContext.ApplicationUsers.AnyAsync(x => x.Id == id);

            //Linq to sql syntax
            //(from sub in _apiDbContext.ApplicationUsers  where sub.ID == id select sub).Any();

            if (isPresent)
            {
                var dmToDelete = new ApplicationUserDM() { Id = id };
                _apiDbContext.ApplicationUsers.Remove(dmToDelete);
                if (await _apiDbContext.SaveChangesAsync() > 0)
                {
                    return new DeleteResponseRoot(true, "Application User Deleted Successfully");
                }
            }
            return new DeleteResponseRoot(false, "Item Not found");

        }

        public async Task<DeleteResponseRoot> DeleteProfilePictureById(int userId, string webRootPath)
            => await DeleteProfilePictureById(await _apiDbContext.ApplicationUsers.FirstOrDefaultAsync(x => x.Id == userId), webRootPath);

        #endregion Delete

        #endregion CRUD

        #region Private Functions
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


        /// <summary>
        /// Converts an image file to a base64 encoded string.
        /// </summary>
        /// <param name="filePath">The path to the image file.</param>
        /// <returns>
        /// If successful, returns the base64 encoded string; 
        /// otherwise, returns null.
        /// </returns>
        static async Task<string?> ConvertToBase64(string filePath)
        {
            try
            {
                // Read all bytes from the file asynchronously
                byte[] imageBytes = await File.ReadAllBytesAsync(filePath);

                // Convert the bytes to a base64 string
                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                // Handle exceptions and return null
                //return ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Updates Profile Picture of a User
        /// </summary>
        /// <param name="userRole">Defines role of a user (Here it is Farmer) </param>
        /// <param name="base64String">Base64 string which represents the profile picture of a User</param>
        /// <returns>
        /// Returns message (string) response
        /// </returns>
        /// <exception cref="Farm2iException"></exception>
        private async Task<bool> UpdateProfilePicture(int userId, string base64String)
        {
            var imageFullPath = "";
            var objDM = await _apiDbContext.ApplicationUsers.FirstOrDefaultAsync(s => s.Id == userId);

            if (objDM == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Application User not found...Please check Again", "Application User not found...Please check Again");
            }
            if (!objDM.ProfilePicturePath.IsNullOrEmpty())
            {
                imageFullPath = Path.GetFullPath(objDM.ProfilePicturePath);
            }
            if (base64String == null)
            {
                objDM.ProfilePicturePath = null;
            }
            else
            {
                var imageRelativePath = await SaveFromBase64(base64String);
                if (imageRelativePath != null)
                {
                    objDM.ProfilePicturePath = imageRelativePath;
                }
            }
            objDM.LastModifiedBy = _loginUserDetail?.LoginId;
            objDM.LastModifiedOnUTC = DateTime.UtcNow;
            if (await _apiDbContext.SaveChangesAsync() > 0)
            {
                // Todo: Delete the previous image from the folder
                /* if (File.Exists(imageFullPath))
                     File.Delete(imageFullPath); */
                return true;
            }
            throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Cannot Update Profile Picture", "Cannot Update Profile Picture");
        }

        #endregion Private Functions

    }
}

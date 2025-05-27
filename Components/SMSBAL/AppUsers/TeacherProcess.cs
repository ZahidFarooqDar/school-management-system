using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SMSBAL.ExceptionHandler;
using SMSDAL.Context;
using SMSDomainModels.Enums;
using SMSDomainModels.v1.Teachers;
using SMSServiceModels.Enums;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.Foundation.Base.Interfaces;
using SMSServiceModels.v1.Teachers;

namespace SMSBAL.AppUsers
{
    public partial class TeacherProcess : LoginUserProcess<TeacherSM>
    {
        #region Properties

        private readonly IPasswordEncryptHelper _passwordEncryptHelper;

        #endregion Properties

        #region Constructor
        public TeacherProcess(IMapper mapper, ILoginUserDetail loginUserDetail, ApiDbContext apiDbContext,            
            IPasswordEncryptHelper passwordEncryptHelper)
            : base(mapper, loginUserDetail, apiDbContext)
        {
            _passwordEncryptHelper = passwordEncryptHelper;
        }

        #endregion Constructor

        #region Odata
        /// <summary>
        /// Odata for TeacherSM
        /// </summary>
        /// <returns>
        /// Returns Iquerable TeacherSM
        /// </returns>
        public override async Task<IQueryable<TeacherSM>> GetServiceModelEntitiesForOdata()
        {
            var entitySet = _apiDbContext.Teachers;
            IQueryable<TeacherSM> retSM = await MapEntityAsToQuerable<TeacherDM, TeacherSM>(_mapper, entitySet);
            return retSM;
        }

        #endregion Odata

        #region CRUD 

        #region Get All and Count
        /// <summary>
        /// Fetches All the Teachers from Database
        /// </summary>
        /// <returns>
        /// If Successful, Returns List of TeacherSM otherwise returns null
        /// </returns>
        public async Task<List<TeacherSM>> GetAllTeachers()
        {
            var dm = await _apiDbContext.Teachers.AsNoTracking().ToListAsync();
            var sm = _mapper.Map<List<TeacherSM>>(dm);
            return sm;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetAllTeachersCount()
        {
            var count = _apiDbContext.Teachers.AsNoTracking().Count();
            return count;
        }
        #endregion Get All

        #region Get Single

        #region Get By Id
        /// <summary>
        /// Fetches a client user from database using Id
        /// </summary>
        /// <param name="id">Id of a client user to fetch</param>
        /// <returns>
        /// If Successful, Returns  TeacherSM otherwise returns null
        /// </returns>
        public async Task<TeacherSM> GetTeacherById(int id)
        {
            var dm = await _apiDbContext.Teachers.FindAsync(id);

            if (dm != null)
            {
                var sm = _mapper.Map<TeacherSM>(dm);
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
        #endregion Get By Id

        #region Get By EmailID
        /// <summary>
        /// Gets clientUser by emailId (which is unique)
        /// </summary>
        /// <param name="email">email id of user</param>
        /// <returns>
        /// If Successful, Returns  TeacherSM otherwise returns null
        /// </returns>
        public async Task<TeacherSM?> GetTeacherByEmail(string email)
        {
            var dm = await _apiDbContext.Teachers.AsNoTracking().FirstOrDefaultAsync(x => x.EmailId == email);
            if (dm != null)
            {
                var sm = _mapper.Map<TeacherSM>(dm);
                if (!sm.ProfilePicturePath.IsNullOrEmpty())
                {
                    sm.ProfilePicturePath = await ConvertToBase64(sm.ProfilePicturePath);
                }
                return sm;
            }
            return null;
        }
        #endregion Get By EmailID

        #region Get By LoginId
        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginId"></param>
        /// <returns></returns>
        public async Task<TeacherSM?> GetTeacherByLoginId(string loginId)
        {
            var dm = await _apiDbContext.Teachers.AsNoTracking().FirstOrDefaultAsync(x => x.LoginId == loginId);
            if (dm != null)
                return _mapper.Map<TeacherSM>(dm);
            return null;
        }

        #endregion Get By LoginId

        #endregion Get Single

        #region Get by AdminId and Count
        /// <summary>
        /// Fetches client users using clientCompanyId
        /// </summary>
        /// <param name="companyId">Id of clientCompany by which we fetch client user</param>
        /// <returns>
        /// If Successful, Returns  TeacherSM otherwise returns null
        /// </returns>
        /// <exception cref="SMSException"></exception>
        public async Task<IEnumerable<TeacherSM?>> GetTeachersByAdminId(int adminId, int skip, int top)
        {
            List<TeacherSM> Teachers = new List<TeacherSM>();
            if (adminId <= 0)
            {
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog, @"Please provide valid Admin Id", "Please provide valid Id");
            }
            var dm = await _apiDbContext.Teachers.AsNoTracking().Where(x => x.ClientUserId == adminId)
                .Skip(skip).Take(top)
                .ToListAsync();
            return _mapper.Map(dm, Teachers);
        }


        #region Count

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adminId"></param>
        /// <returns></returns>
        public async Task<int> GetTeachersOfAdminCount(int adminId)
        {
            var count = _apiDbContext.Teachers.AsNoTracking().Where(x => x.ClientUserId == adminId).Count();
            return count;
        }

        #endregion Count

        #endregion Get by CompanyId and Count

        #region Add Update

        #region Add Teacher
        /// <summary>
        /// Creates new user for application
        /// </summary>
        /// <param name="TeacherSM"></param>
        /// <returns></returns>
        /// <exception cref="SMSException"></exception>
        public async Task<BoolResponseRoot?> AddNewTeacher(TeacherSM signUpSM, int adminId)
        {
            if (signUpSM == null)
            {
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog, "Please provide details for Sign Up", "Please provide details for Sign Up");
            }
            if (signUpSM.LoginId.Length < 5)
            {
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_Log, "Please provide LoginId with minimum 5 characters", "Please provide LoginId with minimum 5 characters");
            }

            if (signUpSM.EmailId.IsNullOrEmpty())
            {
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog, "Please provide EmailId ", "Please provide EmailId");
            }
            if (signUpSM.FirstName.IsNullOrEmpty())
            {
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog, "Please provide FirstName ", "Please provide FirstName");
            }

            var existingUserWithEmail = await GetTeacherByEmail(signUpSM.EmailId);
            if (existingUserWithEmail != null)
            {              
                
                //return new BoolResponseRoot(false, "Teacher with email already present, Use another email to continue...");
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog,
                    "This email is already registered. Please log in using your credentials or reset your password if you've forgotten it.",
                    "The provided Email ID is already in use. Please use a different Email ID.");
            }
            var existingUserWithLoginId = await GetTeacherByLoginId(signUpSM.LoginId);
            if (existingUserWithLoginId != null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "The provided Login ID is already in use. Please use a different Login ID.");
            }

            using (var transaction = await _apiDbContext.Database.BeginTransactionAsync())
            {

                var objDM = _mapper.Map<TeacherDM>(signUpSM);

                if (signUpSM.PasswordHash.IsNullOrEmpty())
                {
                    throw new SMSException(ApiErrorTypeSM.Access_Denied_Log, "Password Is Mandatory", "Password Is Mandatory");
                }

                if (!signUpSM.ProfilePicturePath.IsNullOrEmpty())
                {
                    var profilePath = await SaveFromBase64(signUpSM.ProfilePicturePath);
                    objDM.ProfilePicturePath = profilePath;
                }
                else
                {
                    objDM.ProfilePicturePath = null;
                }

                var passwordHash = await _passwordEncryptHelper.ProtectAsync(signUpSM.PasswordHash);
                objDM.ClientUserId = adminId;
                objDM.RoleType = RoleTypeDM.Teacher;
                objDM.PasswordHash = passwordHash;
                objDM.CreatedBy = _loginUserDetail.LoginId;
                objDM.CreatedOnUTC = DateTime.UtcNow;
                objDM.IsEmailConfirmed = true;
                objDM.IsPhoneNumberConfirmed = true;
                objDM.LoginStatus = LoginStatusDM.Enabled;

                await _apiDbContext.Teachers.AddAsync(objDM);
                if (await _apiDbContext.SaveChangesAsync() > 0)
                {
                    await transaction.CommitAsync();
                    
                    return new BoolResponseRoot(true, "Your account created Successfully, Please Login in to your account");
                }

                transaction.Rollback();
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Something went wrong while adding your details, Please try again later");
            }
        }


        #endregion Add Teacher

        #region Update Client

        /// <summary>
        /// Updates Client Details
        /// </summary>
        /// <param name="userId">Id of client to update</param>
        /// <param name="objSM">TeacherSM object to update</param>
        /// <returns>
        /// If Successful returns TeacherSM Otherwise null
        /// </returns>
        /// <exception cref="SMSException"></exception>

        public async Task<TeacherSM> UpdateTeacher(int adminId,int teacherId, TeacherSM objSM)
        {
            if (adminId == null)
            {
                throw new SMSException(ApiErrorTypeSM.NoRecord_NoLog, $"Please Provide Value to Id", $"Please Provide Value to Id");
            }

            if (objSM == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Nothing to Update", "Nothing to Update");
            }

            var objDM = await _apiDbContext.Teachers.FindAsync(teacherId);
            

            if (objSM.LoginId != objDM.LoginId)
            {
                throw new SMSException(ApiErrorTypeSM.Access_Denied_Log,
                    "LoginId update is not allowed.",
                    "The LoginId cannot be changed. Please contact support if you need assistance."
                    );
            }

            if (objSM.EmailId != objDM.EmailId)
            {
                throw new SMSException(ApiErrorTypeSM.Access_Denied_Log,
                    "Email update is not allowed.",
                    "The email address cannot be changed. Please contact support if you need assistance."
                    );
            }


            string imageFullPath = null;
            if (objDM != null)
            {
                objSM.Id = objDM.Id;
                objSM.PasswordHash = objDM.PasswordHash;
                objSM.ClientUserId =  objDM.ClientUserId;
                objSM.LoginId = objDM.LoginId;
                objSM.EmailId = objDM.EmailId;
                if (!objSM.ProfilePicturePath.IsNullOrEmpty())
                {
                    if (!objDM.ProfilePicturePath.IsNullOrEmpty())
                    {
                        imageFullPath = Path.GetFullPath(objDM.ProfilePicturePath);
                    }
                    var IsCompanyLogoUpdated = await UpdateProfilePicture(teacherId, objSM.ProfilePicturePath);
                    if (IsCompanyLogoUpdated == true)
                    {
                        objSM.ProfilePicturePath = null;
                    }
                }
                else
                {
                    objSM.ProfilePicturePath = objDM.ProfilePicturePath;
                }


                if (objSM.DateOfBirth == default)
                {
                    objSM.DateOfBirth = objDM.DateOfBirth;
                }
                
                objSM.LoginStatus = (LoginStatusSM)objDM.LoginStatus;
                objSM.IsPhoneNumberConfirmed = objDM.IsPhoneNumberConfirmed;
                objSM.RoleType = (RoleTypeSM)objDM.RoleType;

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
                    var response = await GetTeacherById(teacherId);
                    return response;

                    //return _mapper.Map<TeacherSM>(objDM);
                }
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Something went wrong while Updating Client Details", "Something went wrong while Updating Client Details");
            }
            else
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Teacher not found: for Id {teacherId}", "Data to update not found, add as new instead.");
            }
        }

        #endregion Update Client

        #endregion Add Update

        #region Check Email/Login Id 
        /// <summary>
        /// Check whether emailId already exist or not
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<BoolResponseRoot> CheckExistingEmail(string email)
        {
            // Retrieve user and company details in a single query to reduce database calls
            var existingUser = await _apiDbContext.Teachers
                .Where(x => x.EmailId == email)                
                .FirstOrDefaultAsync();

            if (existingUser == null)
            {
                return new BoolResponseRoot(true, "Email is Available");
            }

            return new BoolResponseRoot(false, "Email Already Exists");
        }

        /// <summary>
        /// Check whether loginId already exist or not
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<BoolResponseRoot> CheckExistingLoginId(string loginId)
        {
            var existingUser = await _apiDbContext.Teachers
                .Where(x => x.LoginId == loginId)
                .FirstOrDefaultAsync();

            if (existingUser == null)
            {
                return new BoolResponseRoot(true, "LoginId is Available");
            }
            return new BoolResponseRoot(false, "LoginId Already Exists");
        }

        #endregion Check Email/Login Id 

        #region Delete
        /// <summary>
        /// Deletes Application User from the database using Id
        /// </summary>
        /// <param name="id">Id of an ApplicationUser to delete</param>
        /// <returns>
        /// DeleteResponseRoot
        /// </returns>
        /// <exception cref="SMSException"></exception>
        public async Task<DeleteResponseRoot> DeleteTeacherById(int id)
        {
            var dmToDelete = await _apiDbContext.Teachers.FindAsync(id);
            string imageFullPath = null;
            //Linq to sql syntax
            //(from sub in _apiDbContext.Teachers  where sub.ID == id select sub).Any();

            if (dmToDelete != null)
            {
                       

                if (!dmToDelete.ProfilePicturePath.IsNullOrEmpty())
                {
                    imageFullPath = Path.GetFullPath(dmToDelete.ProfilePicturePath);
                }
                _apiDbContext.Teachers.Remove(dmToDelete);
                if (await _apiDbContext.SaveChangesAsync() > 0)
                {
                    if (File.Exists(imageFullPath))
                        File.Delete(imageFullPath);
                    return new DeleteResponseRoot(true, "Teacher Deleted Successfully");
                }
            }
            return new DeleteResponseRoot(false, "Teacher Not found");

        }
        #endregion Delete

        #endregion CRUD

        #region Private Functions

        /// <summary>
        /// Updates Profile Picture of a User
        /// </summary>
        /// <param name="userRole">Defines role of a user (Here it is Farmer) </param>
        /// <param name="base64String">Base64 string which represents the profile picture of a User</param>
        /// <returns>
        /// Returns message (string) response
        /// </returns>
        /// <exception cref="SMSException"></exception>
        private async Task<bool> UpdateProfilePicture(int teacherId, string base64String)
        {
            var imageFullPath = "";
            var objDM = await _apiDbContext.Teachers.FirstOrDefaultAsync(s => s.Id == teacherId);

            if (objDM == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Teacher not found...Please check Again", "Teacher not found...Please check Again");
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
                if (File.Exists(imageFullPath))
                    File.Delete(imageFullPath);
                return true;
            }
            throw new SMSException(ApiErrorTypeSM.Fatal_Log, "", "Cannot Update Profile Picture");
        }
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

        /* public async Task<string> AddOrUpdateProfilePictureInDb(int userId, string webRootPath, IFormFile postedFile)
            => await base.AddOrUpdateProfilePictureInDb(await _apiDbContext.Teachers.FirstOrDefaultAsync(x => x.Id == userId), webRootPath, postedFile);*/
        #endregion Private Functions

        #region Additional Methods

        #endregion Additional Methods
    }
}

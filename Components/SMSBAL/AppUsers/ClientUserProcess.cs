using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SMSDAL.Context;
using SMSConfig.Configuration;
using SMSServiceModels.v1.General;
using SMSServiceModels.Enums;
using SMSServiceModels.AppUser;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.Foundation.Base.Interfaces;
using SMSServiceModels.AppUser.Login;
using SMSDomainModels.AppUser;
using SMSDomainModels.Enums;
using SMSBAL.Base.Email;
using SMSBAL.ExceptionHandler;
using SMSBAL.Clients;
using SMSBAL.License;

namespace SMSBAL.AppUsers
{
    public partial class ClientUserProcess : LoginUserProcess<ClientUserSM>
    {
        #region Properties

        private readonly IPasswordEncryptHelper _passwordEncryptHelper;
        private readonly APIConfiguration _apiConfiguration;
        private readonly ClientCompanyDetailProcess _clientCompanyDetailProcess;
        private readonly EmailProcess _emailProcess;
        private readonly UserLicenseDetailsProcess _userLicenseDetailsProcess;

        #endregion Properties

        #region Constructor
        public ClientUserProcess(IMapper mapper, ILoginUserDetail loginUserDetail, ApiDbContext apiDbContext,
            ClientCompanyDetailProcess clientCompanyDetailProcess, EmailProcess emailProcess,UserLicenseDetailsProcess userLicenseDetailsProcess,
            IPasswordEncryptHelper passwordEncryptHelper, APIConfiguration apiConfiguration)
            : base(mapper, loginUserDetail, apiDbContext)
        {
            _passwordEncryptHelper = passwordEncryptHelper;
            _apiConfiguration = apiConfiguration;
            _clientCompanyDetailProcess = clientCompanyDetailProcess;
            _emailProcess = emailProcess;
            _userLicenseDetailsProcess = userLicenseDetailsProcess;
        }

        #endregion Constructor

        #region Odata
        /// <summary>
        /// Odata for ClientUserSM
        /// </summary>
        /// <returns>
        /// Returns Iquerable ClientUserSM
        /// </returns>
        public override async Task<IQueryable<ClientUserSM>> GetServiceModelEntitiesForOdata()
        {
            var entitySet = _apiDbContext.ClientUsers;
            IQueryable<ClientUserSM> retSM = await MapEntityAsToQuerable<ClientUserDM, ClientUserSM>(_mapper, entitySet);
            return retSM;
        }

        #endregion Odata

        #region CRUD 

        #region Get All
        /// <summary>
        /// Fetches All the ClientUsers from Database
        /// </summary>
        /// <returns>
        /// If Successful, Returns List of ClientUserSM otherwise returns null
        /// </returns>
        public async Task<List<ClientUserSM>> GetAllClientUsers()
        {
            var dm = await _apiDbContext.ClientUsers.AsNoTracking().ToListAsync();
            var sm = _mapper.Map<List<ClientUserSM>>(dm);
            return sm;
        }
        #endregion Get All

        #region Get Single

        #region Get By Id
        /// <summary>
        /// Fetches a client user from database using Id
        /// </summary>
        /// <param name="id">Id of a client user to fetch</param>
        /// <returns>
        /// If Successful, Returns  ClientUserSM otherwise returns null
        /// </returns>
        public async Task<ClientUserSM> GetClientUserById(int id)
        {
            ClientUserDM clientUserDM = await _apiDbContext.ClientUsers.FindAsync(id);

            if (clientUserDM != null)
            {
                var sm = _mapper.Map<ClientUserSM>(clientUserDM);
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
        /// If Successful, Returns  ClientUserSM otherwise returns null
        /// </returns>
        public async Task<ClientUserSM?> GetClientUserByEmail(string email)
        {
            ClientUserDM? clientUserDM = await _apiDbContext.ClientUsers.AsNoTracking().FirstOrDefaultAsync(x => x.EmailId == email);
            if (clientUserDM != null)
            {
                var sm = _mapper.Map<ClientUserSM>(clientUserDM);
                if (!sm.ProfilePicturePath.IsNullOrEmpty())
                {
                    sm.ProfilePicturePath = await ConvertToBase64(sm.ProfilePicturePath);
                }
                return sm;
            }
            return null;
        }

        public async Task<ClientUserSM?> GetClientUserByLoginId(string loginId)
        {
            ClientUserDM? clientUserDM = await _apiDbContext.ClientUsers.AsNoTracking().FirstOrDefaultAsync(x => x.LoginId == loginId);
            if (clientUserDM != null)
                return _mapper.Map<ClientUserSM>(clientUserDM);
            return null;
        }

        #endregion Get By EmailID

        #region Get by CompanyId and Count
        /// <summary>
        /// Fetches client users using clientCompanyId
        /// </summary>
        /// <param name="companyId">Id of clientCompany by which we fetch client user</param>
        /// <returns>
        /// If Successful, Returns  ClientUserSM otherwise returns null
        /// </returns>
        /// <exception cref="SMSException"></exception>
        public async Task<IEnumerable<ClientUserSM?>> GetUsersByCompanyId(int companyId, int skip, int top)
        {
            List<ClientUserSM> clientUsers = new List<ClientUserSM>();
            if (companyId <= 0)
            {
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog, @"Please provide valid Client Company Id", "Please provide valid Client Company Id");
            }
            var dm = await _apiDbContext.ClientUsers.AsNoTracking().Where(x => x.ClientCompanyDetailId == companyId)
                .Skip(skip).Take(top)
                .ToListAsync();
            return _mapper.Map(dm, clientUsers);
        }


        #region Count

        /// <summary>
        /// Fetches count of Users of Particular Company
        /// </summary>
        /// <returns>
        /// Int response of count
        /// </returns>
        public async Task<int> GetCountOfCompanyUsers(int companyId)
        {
            var count = await _apiDbContext.ClientUsers.AsNoTracking().Where(x => x.ClientCompanyDetailId == companyId).CountAsync();

            return count;
        }

        #endregion Count

        #endregion Get by CompanyId and Count

        #region Get CompanyAutomation User        

        /// <summary>
        /// Generates Token using clientid
        /// </summary>
        /// <param name="id">Id of a client to which a token is generated</param>
        /// <returns>
        /// Returns LoginUserSM, companyId, companycode
        /// </returns>
        public async Task<(LoginUserSM, int, string)> GetCompanyAutomationUserById(int id)
        {
            var user = await _apiDbContext.ClientUsers.FindAsync(id);
            if (user == null)
                throw new SMSException(ApiErrorTypeSM.NoRecord_Log, $"User not found in db with id {id}", $"User not found in db with id {id}");
            LoginUserSM? loginUserSM = null;
            int compId = default;
            string companyCode = default;
            loginUserSM = _mapper.Map<LoginUserSM>(user);
            loginUserSM.PasswordHash = null;
            compId = (int)user.ClientCompanyDetailId;
            companyCode = await _apiDbContext.ClientCompanyDetails.Where(x => x.Id == compId).Select(x => x.CompanyCode).FirstOrDefaultAsync();

            return (loginUserSM, compId, companyCode);
        }

        #endregion Get CompanyAutomation User

        #endregion Get Single

        #region Add Update

        #region Add User

        #region Add App User
        /// <summary>
        /// Creates new user for application
        /// </summary>
        /// <param name="clientUserSM"></param>
        /// <returns></returns>
        /// <exception cref="SMSException"></exception>
        public async Task<BoolResponseRoot?> AddNewUser(ClientUserSM signUpSM, string companyCode, string link)
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

            var existingUserWithEmail = await GetClientUserByEmail(signUpSM.EmailId);
            var existingCompanyDetail = await _clientCompanyDetailProcess.GetClientCompanyByCompanyCode(companyCode);
            if (existingUserWithEmail != null)
            {
                if (existingUserWithEmail.ClientCompanyDetailId == existingCompanyDetail.Id)
                {
                    if (existingUserWithEmail.EmailId == signUpSM.EmailId)
                    {
                        throw new SMSException(ApiErrorTypeSM.Fatal_Log, "The email address you entered is already in use. Please log in or use a different email address.");
                    }

                    if (existingUserWithEmail.LoginId == signUpSM.LoginId)
                    {
                        throw new SMSException(ApiErrorTypeSM.Fatal_Log, "The login ID you entered is already taken. Please log in or choose a different login ID.");
                    }
                }
                if (existingUserWithEmail.IsEmailConfirmed == true)
                {
                    return new BoolResponseRoot(true, $"Your account has been successfully created. Please log in using your email to continue.");
                }
                var obj = new EmailConfirmationSM()
                {
                    Email = existingUserWithEmail.EmailId
                };
                await SendEmailVerificationLink(obj, link);
                return new BoolResponseRoot(true, "Your account has been created Successfully, Please verify email to Login in to your account");
                /*throw new CodeVisionException(ApiErrorTypeSM.InvalidInputData_NoLog,
                    "This email is already registered. Please log in using your credentials or reset your password if you've forgotten it.",
                    "Email already registered, consider logging in or resetting your password.");*/
            }
            var existingUserWithLoginId = await GetClientUserByLoginId(signUpSM.LoginId);
            if (existingUserWithLoginId != null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "The provided Login ID is already in use. Please use a different Login ID.");
            }

            using (var transaction = await _apiDbContext.Database.BeginTransactionAsync())
            {
                var companyDetail = await _clientCompanyDetailProcess.GetClientCompanyByCompanyCode(companyCode);

                var objDM = _mapper.Map<ClientUserDM>(signUpSM);

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
                objDM.ClientCompanyDetailId = companyDetail.Id;
                objDM.RoleType = RoleTypeDM.Admin;
                objDM.PasswordHash = passwordHash;
                objDM.CreatedBy = _loginUserDetail.LoginId;
                objDM.CreatedOnUTC = DateTime.UtcNow;
                objDM.IsEmailConfirmed = false;
                objDM.IsPhoneNumberConfirmed = false;
                objDM.LoginStatus = LoginStatusDM.Enabled;

                await _apiDbContext.ClientUsers.AddAsync(objDM);
                if (await _apiDbContext.SaveChangesAsync() > 0)
                {
                    await transaction.CommitAsync();
                    var obj = new EmailConfirmationSM()
                    {
                        Email = objDM.EmailId
                    };
                    await SendEmailVerificationLink(obj, link);
                    return new BoolResponseRoot(true, "Your account created Successfully, Please verify email to Login in to your account");
                }

                transaction.Rollback();
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Something went wrong while adding your details, Please try again later");
            }
        }


        #endregion Add New Credit App User

        #endregion Add User

        #region Resend Verification Email

        public async Task<BoolResponseRoot> ResendEmailVerification(EmailConfirmationSM objSM, string link)
        {
            if (objSM.Email.IsNullOrEmpty())
            {
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog, "Please provide your email", "Please provide your email");
            }

            if (link.IsNullOrEmpty())
            {
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_NoLog, "Something went wrong while generating link to send email verification", "Something went wrong while generating link to send email verification");
            }

            var response = await SendEmailVerificationLink(objSM, link);
            return response;
        }

        #endregion Resend Verification Email

        #region Update Client

        /// <summary>
        /// Updates Client Details
        /// </summary>
        /// <param name="userId">Id of client to update</param>
        /// <param name="objSM">ClientUserSM object to update</param>
        /// <returns>
        /// If Successful returns ClientUserSM Otherwise null
        /// </returns>
        /// <exception cref="SMSException"></exception>

        public async Task<ClientUserSM> UpdateClientUser(int userId, ClientUserSM objSM,bool isSocialMediaUpdation = false)
        {
            if (userId == null)
            {
                throw new SMSException(ApiErrorTypeSM.NoRecord_NoLog, $"Please Provide Value to Id", $"Please Provide Value to Id");
            }

            if (objSM == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Nothing to Update", "Nothing to Update");
            }

            ClientUserDM objDM = await _apiDbContext.ClientUsers.FindAsync(userId);
            

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
                objSM.ClientCompanyDetailId =  objDM.ClientCompanyDetailId;
                objSM.LoginId = objDM.LoginId;
                objSM.EmailId = objDM.EmailId;
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


                if (objSM.DateOfBirth == default)
                {
                    objSM.DateOfBirth = objDM.DateOfBirth;
                }
                //Todo: Check how to handle LoginStatus, IsEmailConfirmed and IsPhoneNumberVerified
                if(isSocialMediaUpdation == true)
                {
                    objSM.IsEmailConfirmed = true;
                }
                else
                {
                    objSM.IsEmailConfirmed = objDM.IsEmailConfirmed;
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
                    var response = await GetClientUserById(userId);
                    return response;

                    //return _mapper.Map<ClientUserSM>(objDM);
                }
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Something went wrong while Updating Client Details", "Something went wrong while Updating Client Details");
            }
            else
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Client not found: ", "Data to update not found, add as new instead.");
            }
        }

        #endregion Update Client

        #region Check Email/Login Id 
        /// <summary>
        /// Check whether emailId already exist or not
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<BoolResponseRoot> CheckExistingEmail(string email, string compCode)
        {
            // Retrieve user and company details in a single query to reduce database calls
            var existingUser = await _apiDbContext.ClientUsers
                .Where(x => x.EmailId == email)
                .Select(x => new
                {
                    x.ClientCompanyDetailId,
                    CompanyId = _apiDbContext.ClientCompanyDetails
                                .Where(c => c.CompanyCode == compCode)
                                .Select(c => c.Id)
                                .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (existingUser == null)
            {
                return new BoolResponseRoot(true, "Email is Available");
            }

            if (existingUser.ClientCompanyDetailId != existingUser.CompanyId)
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
        public async Task<BoolResponseRoot> CheckExistingLoginId(string loginId, string companyCode)
        {
            var existingUser = await _apiDbContext.ClientUsers
                .Where(x => x.LoginId == loginId)
                .Select(x => new
                {
                    x.ClientCompanyDetailId,
                    CompanyId = _apiDbContext.ClientCompanyDetails
                                .Where(c => c.CompanyCode == companyCode)
                                .Select(c => c.Id)
                                .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (existingUser == null)
            {
                return new BoolResponseRoot(true, "LoginId is Available");
            }

            // Check if the email's associated company ID is different from the given company code
            if (existingUser.ClientCompanyDetailId != existingUser.CompanyId)
            {
                return new BoolResponseRoot(true, "LoginId is Available");
            }

            // Email already exists for the same company
            return new BoolResponseRoot(false, "LoginId Already Exists");
        }

        #endregion Check Email/Login Id 

        #endregion Add Update

        #region Delete
        /// <summary>
        /// Deletes Application User from the database using Id
        /// </summary>
        /// <param name="id">Id of an ApplicationUser to delete</param>
        /// <returns>
        /// DeleteResponseRoot
        /// </returns>
        /// <exception cref="SMSException"></exception>
        public async Task<DeleteResponseRoot> DeleteClientUserById(int id)
        {
            var client = await _apiDbContext.ClientUsers.AsNoTracking().Where(x => x.Id == id).FirstOrDefaultAsync();
            //var client = await _apiDbContext.ClientUsers.FindAsync(id);
            string imageFullPath = null;
            //Linq to sql syntax
            //(from sub in _apiDbContext.ClientUsers  where sub.ID == id select sub).Any();

            if (client != null)
            {
                //Todo: If we need to delete associated company as well
                /*var existingClientsWithCompanyCount = await _apiDbContext.ClientUsers.AsNoTracking()
                    .Where(x=>x.ClientCompanyDetailId == client.ClientCompanyDetailId && x.Id != client.Id).CountAsync();
                if (existingClientsWithCompanyCount == 0)
                {
                    var companyToDelete = new ClientCompanyDetailDM() { Id = client.ClientCompanyDetailId };
                    _apiDbContext.ClientCompanyDetails.Remove(companyToDelete);
                    await _apiDbContext.SaveChangesAsync();
                }*/

                var usersCountInCompany = await _apiDbContext.ClientUsers.
                    Where(x => x.ClientCompanyDetailId == client.ClientCompanyDetailId).CountAsync();
                if (usersCountInCompany < 1)
                {
                    throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"User cannot be deleted as we should have atleast one user associated with company", "User cannot be deleted as we should have atleast one user associated with company");
                }

                if (!client.ProfilePicturePath.IsNullOrEmpty())
                {
                    imageFullPath = Path.GetFullPath(client.ProfilePicturePath);
                }
                var dmToDelete = new ClientUserDM() { Id = id };
                _apiDbContext.ClientUsers.Remove(dmToDelete);
                if (await _apiDbContext.SaveChangesAsync() > 0)
                {
                    // Todo: Delete the Image as well 
                    /*if (File.Exists(imageFullPath))
                        File.Delete(imageFullPath);*/
                    return new DeleteResponseRoot(true, "Client User Deleted Successfully");
                }
            }
            return new DeleteResponseRoot(false, "Item Not found");

        }

        public async Task<DeleteResponseRoot> DeleteProfilePictureById(int userId, string webRootPath)
            => await DeleteProfilePictureById(await _apiDbContext.ClientUsers.FirstOrDefaultAsync(x => x.Id == userId), webRootPath);

        #endregion Delete

        #endregion CRUD

        #region Forgot Login Id

        public async Task<BoolResponseRoot> SendLoginIdToEmail(EmailConfirmationSM objSM)
        {
            if (string.IsNullOrWhiteSpace(objSM.Email))
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Please provide an email ID. Email cannot be empty.");


            var user = await GetClientUserByEmail(objSM.Email);
            if (user == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "This email is not associated with any registered account. Please sign up to create your account.");
            }

            if (!string.IsNullOrWhiteSpace(objSM.Email))
            {
                // Prepare email subject and body
                var subject = "Your Login ID Request";
                var body = $"Hi {user.FirstName},<br/><br/>" +
                           "We received a request to retrieve your Login ID associated with this email.<br/><br/>" +
                           $"Your Login ID is: <b>{user.LoginId}</b><br/><br/>" +
                           "If you did not request this information, please ignore this email. " +
                           "If you have any questions or need further assistance, feel free to contact our support team.<br/><br/>" +
                           "Thank you,<br/>" +
                           "The Support Team";

                _emailProcess.SendEmail(objSM.Email, subject, body);

                // Return success response
                return new BoolResponseRoot(true, "Your Login ID has been sent successfully to your registered email.");
            }
            else
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Unable to send the email. Please try again later.");
            }
        }


        #endregion Forgot Login Id

        #region Reset/Forgot Password

        #region Forgot Password and Reset Password

        /// <summary>
        /// Send Reset Password Link on Mail
        /// </summary>
        /// <param name="forgotPassword">ForgotPassword Object</param>
        /// <param name="companyId">Primary key of ClientCompanyDetailId</param>
        /// <returns>boolean value on success or failure</returns>
        /// <exception cref="CoinManagementException"></exception>
        public async Task<BoolResponseRoot> SendResetPasswordLink(ForgotPasswordSM forgotPassword, string link)
        {
            //var timeExpiry = _apiConfiguration.Time;
            var timeExpiry = 30;
            DateTime currentTime = DateTime.Now.AddMinutes(timeExpiry);
            forgotPassword.Expiry = currentTime;
            var authCode = await _passwordEncryptHelper.ProtectAsync(forgotPassword);
            //authCode = authCode.Replace("+", "%2B");
            if (string.IsNullOrWhiteSpace(forgotPassword.UserName))
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "User Name cannot be empty.");
            var user = ValidateUserFromDatabaseandGetEmail(forgotPassword);
            if (!string.IsNullOrWhiteSpace(user.email))
            {
                link = $"{link}?authCode={authCode}";
                var subject = "Password Reset Request";
                var body = $"Hi {forgotPassword.UserName}, <br/> You recently requested to reset your password for your account. " +
                    $"Click the link below to reset it. " +
                     $" <br/><br/><a href='{link}'>{link}</a> <br/><br/>" +
                     "If you did not request a password reset, please ignore this email.<br/><br/> Thank you";
                _emailProcess.SendEmail(user.email, subject, string.Format(body, forgotPassword.UserName, user.pwd));
                return new BoolResponseRoot(true, "Reset Password Link has been sent Successfully");
            }
            else
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"No ClientUser with username '{forgotPassword.UserName}' found.");
            }
        }

        public async Task<IntResponseRoot> ValidatePassword(string authCode)
        {
            ForgotPasswordSM forgotPassword = await _passwordEncryptHelper.UnprotectAsync<ForgotPasswordSM>(authCode);
            if (string.IsNullOrWhiteSpace(forgotPassword.UserName))
            {
                return new IntResponseRoot((int)ValidatePasswordLinkStatusSM.Invalid, "UserName Not Found");
            }
            if (forgotPassword.Expiry < DateTime.Now)
            {
                return new IntResponseRoot((int)ValidatePasswordLinkStatusSM.Invalid, "Password reset link expired.");
            }
            return new IntResponseRoot((int)ValidatePasswordLinkStatusSM.Valid, "Success");

        }

        public async Task<BoolResponseRoot> UpdatePassword(ResetPasswordRequestSM resetPasswordRequest)
        {
            ForgotPasswordSM forgotPassword = await _passwordEncryptHelper.UnprotectAsync<ForgotPasswordSM>(resetPasswordRequest.authCode);
            if (forgotPassword.Expiry < DateTime.Now)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Link expired.", $"Password reset link expired.");
            }
            if (!string.IsNullOrWhiteSpace(resetPasswordRequest.NewPassword))
            {
                if (string.IsNullOrEmpty(forgotPassword.UserName))
                {
                    throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"No ClientUser with username '{forgotPassword.UserName}' found.", $"No ClientUser with username '{forgotPassword.UserName}' found.");
                }

                var user = (from r in _apiDbContext.ClientUsers
                            where r.LoginId == forgotPassword.UserName.ToUpper()
                            select new { ClientUser = r }).FirstOrDefault();

                if (user != null)
                {
                    string decrypt = "";
                    string newPassword = await _passwordEncryptHelper.ProtectAsync(resetPasswordRequest.NewPassword);
                    if (string.Equals(user.ClientUser.PasswordHash, newPassword))
                    {
                        throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Please don't use old password, use new one.", $"Please don't use old password, use new one.");
                    }
                    else
                    {
                        resetPasswordRequest.NewPassword = await _passwordEncryptHelper.ProtectAsync(resetPasswordRequest.NewPassword);
                        user.ClientUser.PasswordHash = resetPasswordRequest.NewPassword;
                        await _apiDbContext.SaveChangesAsync();
                        return new BoolResponseRoot(true, "Password Updated Successfully");
                    }
                }
                else
                {
                    throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"No ClientUser with username '{forgotPassword.UserName}' found.", $"No ClientUser with username '{forgotPassword.UserName}' found.");
                }
            }
            else
            {

                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Password can not be empty", "Password should not contain Spaces.");
            }
        }

        #endregion Forgot Password and Reset Password

        #endregion Reset/Forgot Password

        #region Validate User 

        private string ValidateUserUsingEmail(EmailConfirmationSM objSM)
        {
            if (string.IsNullOrWhiteSpace(objSM.Email))
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Email cannot be empty.");

            var user = (from c in _apiDbContext.ClientUsers
                        where c.EmailId.ToUpper() == objSM.Email.ToUpper()
                        select new { ClientUser = c }).FirstOrDefault();

            if (user == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"No User with email '{objSM.Email}' found.", $"No User with email '{objSM.Email}' found.");
            }

            return objSM.Email;
        }

        private (string email, string pwd) ValidateUserFromDatabaseandGetEmail(ForgotPasswordSM forgotPassword)
        {
            if (string.IsNullOrWhiteSpace(forgotPassword.UserName))
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "User Name cannot be empty.");
            var user = (from u in _apiDbContext.ClientUsers
                        where u.LoginId.ToUpper() == forgotPassword.UserName.ToUpper()
                        select new { ClientUser = u }).FirstOrDefault();
            if (user == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"No ClientUser with username '{forgotPassword.UserName}' found.", $"No ClientUser with username '{forgotPassword.UserName}' found.");
            }
            if (string.IsNullOrWhiteSpace(user.ClientUser.EmailId))
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Please Update Email For ClientUser With Username '{forgotPassword.UserName}'.");
            return (user.ClientUser.EmailId, user.ClientUser.PasswordHash);
        }

        #endregion Validate User and Send Email

        #region Confirm Email

        /// <summary>
        /// Send Reset Password Link on Mail
        /// </summary>
        /// <param name="forgotPassword">ForgotPassword Object</param>
        /// <param name="link">String Object</param>
        /// <returns>boolean value on success or failure</returns>
        /// <exception cref="SMSException"></exception>
        public async Task<BoolResponseRoot> SendEmailVerificationLink(EmailConfirmationSM objSM, string link)
        {
            var authCode = await _passwordEncryptHelper.ProtectAsync(objSM);

            if (string.IsNullOrWhiteSpace(objSM.Email))
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Email cannot be empty.");
            var email = ValidateUserUsingEmail(objSM);
            if (!string.IsNullOrWhiteSpace(email))
            {

                link = $"{link}?authCode={authCode}";
                var subject = "Email Verification Request";
                var body = $"Hi {objSM.Email}, <br/> Your email confirmation requested for your account. " +
                    $"Click the link below to confirm your Email. " +
                     $" <br/><br/><a href='{link}'>{link}</a> <br/><br/>" +
                     "If you did not request an email verification, please ignore this email.<br/><br/> Thank you";
                _emailProcess.SendEmail(objSM.Email, subject, string.Format(body, objSM.Email));
                return new BoolResponseRoot(true, "Email Verification Link has been sent Successfully");
            }
            else
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"No User with email '{objSM.Email}' found.");
            }
        }
        /// <summary>
        /// Validation of Password Link that has been sent via Email.
        /// </summary>
        /// <param name="authCode">String Object</param>
        /// <returns>The integer Response Object.</returns>

        public async Task<BoolResponseRoot> ValidateEmail(string authCode)
        {
            EmailConfirmationSM sm = await _passwordEncryptHelper.UnprotectAsync<EmailConfirmationSM>(authCode);
            if (string.IsNullOrWhiteSpace(sm.Email))
            {
                return new BoolResponseRoot(false, "Email Not Found");
            }
            var request = new VerifyEmailRequestSM()
            {
                authCode = authCode
            };
            var response = await VerifyEmailRequest(request);
            return response;
        }



        /// <summary>
        /// This is Used for Updating the Password of a User.
        /// </summary>
        /// <param name="resetPasswordRequest">ResetPasswordRequestSM Object</param>
        /// <param name="newPassword">String NewPassword</param>
        /// <returns>The Boolen Response Object.</returns>
        /// <exception cref="SMSException"></exception>


        public async Task<BoolResponseRoot> VerifyEmailRequest(VerifyEmailRequestSM objSM)
        {
            EmailConfirmationSM sm = await _passwordEncryptHelper.UnprotectAsync<EmailConfirmationSM>(objSM.authCode);
            /*if (forgotPassword.Expiry < DateTime.Now)
            {
                throw new CodeVisionException(ApiErrorTypeSM.Fatal_Log, $"Link expired.", $"Password reset link expired.");
            }*/
            if (!string.IsNullOrWhiteSpace(sm.Email))
            {
                var user = await _apiDbContext.ClientUsers.Where(x => x.EmailId.ToUpper() == sm.Email.ToUpper()).FirstOrDefaultAsync();

                if (user != null)
                {
                    user.IsEmailConfirmed = true;
                    user.LastModifiedBy = _loginUserDetail.LoginId;
                    user.LastModifiedOnUTC = DateTime.UtcNow;
                    if (await _apiDbContext.SaveChangesAsync() > 0)
                    {
                        //await _userLicenseDetailsProcess.AddTrialLicenseDetails(user.Id);
                        return new BoolResponseRoot(true, "Your Email Verified Successfully, Login to your Account now");
                    }
                    throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Something went wrong while verifying your email, Please try again later");
                }
                else
                {
                    throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"No ClientUser with email '{sm.Email}' found.", $"No ClientUser with username '{sm.Email}' found.");
                }
            }
            else
            {

                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Email can not be empty", "Email can not be empty");
            }
        }

        #endregion Confirm Email

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
        private async Task<bool> UpdateProfilePicture(int userId, string base64String)
        {
            var imageFullPath = "";
            var objDM = await _apiDbContext.ClientUsers.FirstOrDefaultAsync(s => s.Id == userId);

            if (objDM == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Client not found...Please check Again", "Client not found...Please check Again");
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
            => await base.AddOrUpdateProfilePictureInDb(await _apiDbContext.ClientUsers.FirstOrDefaultAsync(x => x.Id == userId), webRootPath, postedFile);*/
        #endregion Private Functions

        #region Additional Methods

        #endregion Additional Methods
    }
}

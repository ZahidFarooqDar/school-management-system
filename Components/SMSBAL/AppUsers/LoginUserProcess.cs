using AutoMapper;
using SMSBAL.Foundation.Base;
using SMSBAL.Foundation.CommonUtils;
using SMSDAL.Context;
using SMSDomainModels.AppUser.Login;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Interfaces;

namespace SMSBAL.AppUsers
{
    public abstract class LoginUserProcess<T> : CoreVisionBalOdataBase<T>
    {
        #region Properties

        protected readonly ILoginUserDetail _loginUserDetail;

        #endregion Properties

        #region Constructor
        public LoginUserProcess(IMapper mapper, ILoginUserDetail loginUserDetail, ApiDbContext apiDbContext)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
        }
        #endregion Constructor

        #region CRUD 

        #region Add Update
        /// <summary>
        /// Add or Update profile picture of an user
        /// </summary>
        /// <param name="targetLoginUser"></param>
        /// <param name="webRootPath"></param>
        /// <param name="postedFile"></param>
        /// <returns>
        /// 
        /// </returns>
        protected async Task<string> AddOrUpdateProfilePictureInDb(LoginUserDM targetLoginUser, string webRootPath, IFormFile postedFile)
        {
            if (targetLoginUser != null)
            {
                var currLogoPath = targetLoginUser.ProfilePicturePath;
                var targetRelativePath = Path.Combine("content\\loginusers\\profile", $"{targetLoginUser.Id}_{Guid.NewGuid()}_original{Path.GetExtension(postedFile.FileName)}");
                var targetPath = Path.Combine(webRootPath, targetRelativePath);
                if (await SavePostedFileAtPath(postedFile, targetPath))
                {
                    //Entry Method//
                    //var comp = new ClientCompanyDetailDM() { Id = companyId, CompanyLogoPath = targetRelativePath };
                    //_apiDbContext.ClientCompanyDetails.Attach(comp);
                    //_apiDbContext.Entry(comp).Property(e => e.CompanyLogoPath).IsModified = true;
                    targetLoginUser.ProfilePicturePath = targetRelativePath.ConvertFromFilePathToUrl();
                    targetLoginUser.LastModifiedBy = _loginUserDetail.LoginId;
                    targetLoginUser.LastModifiedOnUTC = DateTime.UtcNow;
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
        /// <summary>
        /// Deletes the profile picture of an User
        /// </summary>
        /// <param name="targetLoginUser"></param>
        /// <param name="webRootPath"></param>
        /// <returns>
        /// Returns DeleteResponseRoot
        /// </returns>
        protected async Task<DeleteResponseRoot> DeleteProfilePictureById(LoginUserDM targetLoginUser, string webRootPath)
        {
            if (targetLoginUser != null)
            {
                var currLogoPath = targetLoginUser.ProfilePicturePath;
                targetLoginUser.ProfilePicturePath = "";
                targetLoginUser.LastModifiedBy = _loginUserDetail.LoginId;
                targetLoginUser.LastModifiedOnUTC = DateTime.UtcNow;

                if (await _apiDbContext.SaveChangesAsync() > 0)
                {
                    if (!string.IsNullOrWhiteSpace(currLogoPath))
                    {
                        File.Delete(Path.Combine(webRootPath, currLogoPath));
                        return new DeleteResponseRoot(true);
                    }
                }
            }
            return new DeleteResponseRoot(false, "User or Picture Not found");
        }

        #endregion Delete

        #endregion CRUD

        #region Private Functions
        #endregion Private Functions
    }

}

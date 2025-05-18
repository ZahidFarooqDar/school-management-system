using AutoMapper;
using SMSBAL.ExceptionHandler;
using SMSBAL.Foundation.Base;
using SMSDAL.Context;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.Foundation.Base.Interfaces;

namespace SMSBAL.License
{
    public class PermissionProcess : SMSBalBase
    {
        #region Properties
        private readonly ILoginUserDetail _loginUserDetail;
        private readonly UserLicenseDetailsProcess _userLicenseDetailsProcess;
        private readonly LicenseTypeProcess _licenseTypeProcess;
        private readonly FeatureProcess _featureProcess;
        #endregion Properties

        #region Constructor
        public PermissionProcess(IMapper mapper, ApiDbContext apiDbContext, ILoginUserDetail loginUserDetail,
            UserLicenseDetailsProcess userLicenseDetailsProcess, LicenseTypeProcess licenseTypeProcess, FeatureProcess featureProcess)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
            _licenseTypeProcess = licenseTypeProcess;
            _featureProcess = featureProcess;
            _licenseTypeProcess = licenseTypeProcess;
            _userLicenseDetailsProcess = userLicenseDetailsProcess;
        }
        #endregion Constructor

        #region Method permissions

        public async Task<BoolResponseRoot> DoesUserHasPermission(int userId, string featureCode)
        {
            var existingLicense = await _userLicenseDetailsProcess.GetActiveUserLicenseDetailsByUserId(userId);
            
            var features = await _featureProcess.GetFeaturesbylicenseId((int)existingLicense.LicenseTypeId);
            bool hasPermission = features.Any(feature => feature.FeatureCode == featureCode);
            if(hasPermission == false)
            {
                throw new SMSException(ApiErrorTypeSM.Access_Denied_Log, $"User with Id : {userId} tried to access non permissible feature", "This feature is not available in your current license. Please upgrade to access it.");
            }
            return new BoolResponseRoot(true, "User has permission for this feature.");
            
        }

        #endregion Method permissions
    }
}

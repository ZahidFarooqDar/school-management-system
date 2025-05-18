using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SMSBAL.ExceptionHandler;
using SMSBAL.Foundation.Base;
using SMSDAL.Context;
using SMSDomainModels.v1.General.License;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.Foundation.Base.Interfaces;
using SMSServiceModels.v1.General.License;

namespace SMSBAL.License
{
    public class FeatureProcess : CoreVisionBalOdataBase<FeatureSM>
    {
        #region Properties
        private readonly ILoginUserDetail _loginUserDetail;
        #endregion Properties

        #region Constructor
        public FeatureProcess(IMapper mapper, ApiDbContext apiDbContext, ILoginUserDetail loginUserDetail) : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
        }
        #endregion Constructor

        #region Odata
        /// <summary>
        /// This method gets any Feature(s) by filtering/sorting the data
        /// </summary>
        /// <returns>Feature(s)</returns>
        public override async Task<IQueryable<FeatureSM>> GetServiceModelEntitiesForOdata()
        {
            var entitySet = _apiDbContext.Features;
            IQueryable<FeatureSM> retSM = await MapEntityAsToQuerable<FeatureDM, FeatureSM>(_mapper, entitySet);
            return retSM;
        }
        #endregion Odata

        #region --Count--

        /// <summary>
        /// Get Feature Count in database.
        /// </summary>
        /// <returns>integer response</returns>

        public async Task<int> GetAllFeatureCountResponse()
        {
            int resp = _apiDbContext.Features.Count();
            return resp;
        }

        #endregion --Count--

        #region CRUD

        #region Get All
        /// <summary>
        /// This methods gets all Features (List of Features)
        /// </summary>
        /// <returns>All Features</returns>
        public async Task<List<FeatureSM>> GetAllFeatures()
        {
            var _featuresDb = await _apiDbContext.Features.ToListAsync();
            return _mapper.Map<List<FeatureSM>>(_featuresDb);
        }
        #endregion Get All

        #region Get My Features
        public async Task<List<FeatureSM>> GetMyFeatures(int clientUserId)
        {
            var userLicense = await _apiDbContext.UserLicenseDetails
                .Where(c => c.ClientUserId == clientUserId && c.IsCancelled == false && c.IsSuspended == false && c.Status == "active")
                .FirstOrDefaultAsync();
            if (userLicense == null)
            {
                throw new SMSException(ApiErrorTypeSM.Access_Denied_Log, $"Access denied for getting permissions for user Id : {clientUserId}", "Access denied. A valid license is required to unlock features.");
            }
            
            var featureIds = await _apiDbContext.LicenseFeatures
                .Where(x => x.LicenseTypeId == userLicense.LicenseTypeId) 
                .Select(x=>x.FeatureId)
                .ToListAsync();
            var features = await _apiDbContext.Features
                .Where(f => featureIds.Contains(f.Id))
                .ToListAsync();
            return _mapper.Map<List<FeatureSM>>(features);
        }
        #endregion Get My Features

        #region Get Features by License Id

        public async Task<List<FeatureSM>> GetFeaturesbylicenseId(int licenseTypeId)
        {
            
            var featureIds = await _apiDbContext.LicenseFeatures
                .Where(x => x.LicenseTypeId == licenseTypeId)
                .Select(x => x.FeatureId)
                .ToListAsync();
            var features = await _apiDbContext.Features
                .Where(f => featureIds.Contains(f.Id))
                .ToListAsync();
            return _mapper.Map<List<FeatureSM>>(features);
        }

        #endregion Get Features by License Id

        #region Get Single
        /// <summary>
        /// This method gets a single Feature on id
        /// </summary>
        /// <param name="id">Feature Id</param>
        /// <returns>Single Feature</returns>
        public async Task<FeatureSM?> GetSingleFeatureById(int id)
        {
            FeatureDM? _featureDM = await _apiDbContext.Features.FindAsync(id);

            if (_featureDM != null)
            {
                return _mapper.Map<FeatureSM>(_featureDM);
            }
            else
            {
                return null;
            }
        }
        #endregion Get Single

        #region Post
        /// <summary>
        /// This method add a new Feature into the database
        /// </summary>
        /// <param name="_featureSM">Feature To Save</param>
        /// <returns>Newly Added Feature</returns>
        public async Task<FeatureSM?> AddFeature(FeatureSM _featureSM)
        {
            if (_featureSM == null)
                return null;
            else
            {
                var _featureDM = _mapper.Map<FeatureDM>(_featureSM);
                _featureDM.CreatedBy = _loginUserDetail.LoginId;
                _featureDM.CreatedOnUTC = DateTime.UtcNow;

                await _apiDbContext.Features.AddAsync(_featureDM);
                if (await _apiDbContext.SaveChangesAsync() > 0)
                {
                    return _featureSM;
                }
            }
            return null;
        }
        #endregion Post

        #region Put
        /// <summary>
        /// This method updates an existing Feature on Id
        /// </summary>
        /// <param name="_featureIdToUpdate">Feature Id</param>
        /// <param name="_featureSM">New Details of Feature (Feature Object)</param>
        /// <returns></returns>
        /// <exception cref="SurveyBoxException">Throws exception if Feature to update does not exist.</exception>
        public async Task<FeatureSM?> UpdateFeature(int _featureIdToUpdate, FeatureSM _featureSM)
        {

            if (_featureSM != null && _featureIdToUpdate > 0)
            {
                var isPresent = await _apiDbContext.Features.AnyAsync(x => x.Id == _featureIdToUpdate);
                if (isPresent)
                {
                    _featureSM.Id = _featureIdToUpdate;
                    FeatureDM _featureDbDM = await _apiDbContext.Features.FindAsync(_featureIdToUpdate);
                    _mapper.Map<FeatureSM, FeatureDM>(_featureSM, _featureDbDM);
                    _featureDbDM.LastModifiedBy = _loginUserDetail.LoginId;
                    _featureDbDM.LastModifiedOnUTC = DateTime.UtcNow;

                    if (await _apiDbContext.SaveChangesAsync() > 0)
                    {
                        return _mapper.Map<FeatureSM>(_featureDbDM);
                    }
                }
                else
                {
                    throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Feature not found: {_featureIdToUpdate}", "Feature to update not found, add as new instead.");
                }
            }

            return null;
        }
        #endregion Put

        #region Delete
        /// <summary>
        /// This method deletes any existing Feature on Id
        /// </summary>
        /// <param name="id">Feature Id to delete</param>
        /// <returns>Status of deletion, true if deleted successfully otherwise false with a message.</returns>
        public async Task<DeleteResponseRoot> DeleteFeatureById(int id)
        {
            var isPresent = await _apiDbContext.Features.AnyAsync(x => x.Id == id);

            if (isPresent)
            {
                var featureDMToDelete = new FeatureDM() { Id = id };
                _apiDbContext.Features.Remove(featureDMToDelete);
                if (await _apiDbContext.SaveChangesAsync() > 0)
                {
                    return new DeleteResponseRoot(true, "Feature deleted successfully.");
                }
            }
            return new DeleteResponseRoot(false, "Feature Not found");

        }
        #endregion Delete

        #endregion CRUD
    }
}

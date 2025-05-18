using SMSBAL.Foundation.Config;
using SMSDAL.Foundation;
using SMSDomainModels.Foundation;

namespace SMSBAL.Foundation
{
    public class ErrorLogProcessRoot
    {
        private readonly ErrorLogDALRoot _errorLogDALRoot;

        private readonly ApplicationIdentificationRoot _applicationIdentificationRoot;

        public ErrorLogProcessRoot(string connectionStr, ApplicationIdentificationRoot applicationIdentificationRoot, ErrorLogDALRoot errorLogDALRoot = null)
        {
            if (errorLogDALRoot == null)
            {
                _errorLogDALRoot = new ErrorLogDALRoot(connectionStr);
            }
            else
            {
                _errorLogDALRoot = errorLogDALRoot;
            }

            _applicationIdentificationRoot = applicationIdentificationRoot;
        }

        public async Task<bool> SaveErrorObjectInDb(ErrorLogRoot errorLog)
        {
            return await _errorLogDALRoot.SaveErrorObjectInDb(errorLog);
        }
    }
}

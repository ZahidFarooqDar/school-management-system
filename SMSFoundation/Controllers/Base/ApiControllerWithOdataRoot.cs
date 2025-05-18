using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using SMSBAL.Foundation.Odata;
using SMSServiceModels.Foundation.Base;

namespace SMSFoundation.Controllers.Base
{
    public abstract class ApiControllerWithOdataRoot<T> : ApiControllerRoot where T : BaseServiceModelRoot
    {
        private readonly BalOdataRoot<T> _balOdataRoot;

        public ApiControllerWithOdataRoot(BalOdataRoot<T> balOdataRoot)
        {
            _balOdataRoot = balOdataRoot;
        }

        protected async Task<IEnumerable<T>> GetAsEntitiesOdata(ODataQueryOptions<T> oDataOptions)
        {
            return await ((oDataOptions.ApplyTo(await _balOdataRoot.GetServiceModelEntitiesForOdata()) as IQueryable<T>)?.ToListAsync());
        }
    }
}

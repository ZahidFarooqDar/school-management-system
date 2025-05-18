using AutoMapper;
using SMSBAL.Foundation.Odata;
using SMSDAL.Context;

namespace SMSBAL.Foundation.Base
{
    public class SMSBalBase : BalRoot
    {
        protected readonly IMapper _mapper;
        protected readonly ApiDbContext _apiDbContext;

        public SMSBalBase(IMapper mapper, ApiDbContext apiDbContext)
        {
            _mapper = mapper;
            _apiDbContext = apiDbContext;
        }
    }
    public abstract class CoreVisionBalOdataBase<T> : BalOdataRoot<T>
    {
        protected readonly IMapper _mapper;
        protected readonly ApiDbContext _apiDbContext;

        protected CoreVisionBalOdataBase(IMapper mapper, ApiDbContext apiDbContext)
        {
            _mapper = mapper;
            _apiDbContext = apiDbContext;
        }
    }
}

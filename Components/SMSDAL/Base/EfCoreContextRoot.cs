using Microsoft.EntityFrameworkCore;

namespace SMSDAL.Base
{
    public abstract class EfCoreContextRoot : DbContext, IEfCoreContextRoot
    {
        public EfCoreContextRoot(DbContextOptions options)
            : base(options)
        {
        }
    }
}

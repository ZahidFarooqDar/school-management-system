using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SMSDAL.Context;
using SMSFoundation.Controllers.Base;
using SMSServiceModels.Foundation.Base.Interfaces;

namespace SMSFoundation.Controllers.Common
{
    [ApiController]
    [Route("[controller]")]
    public partial class DatabaseSeedController : ApiControllerRoot
    {
        private readonly ApiDbContext _apiDbContext;
        private readonly IPasswordEncryptHelper _passwordEncryptHelper;

        public DatabaseSeedController(ApiDbContext context, IPasswordEncryptHelper passwordEncryptHelper)
        {
            _apiDbContext = context;
            _passwordEncryptHelper = passwordEncryptHelper;
        }

        [HttpGet]
        [Route("Init")]
        public async Task<IActionResult> Get()
        {
            DatabaseSeeder<ApiDbContext> databaseSeeder = new DatabaseSeeder<ApiDbContext>();
            var retVal = await databaseSeeder.SetupDatabaseWithTestData(_apiDbContext, (x) => _passwordEncryptHelper.ProtectAsync(x).Result);
            return Ok(retVal);
        }
    }
}

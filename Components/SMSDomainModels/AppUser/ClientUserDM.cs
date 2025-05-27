using SMSDomainModels.AppUser.Login;
using SMSDomainModels.Client;
using SMSDomainModels.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMSDomainModels.AppUser
{
    public class ClientUserDM : LoginUserDM
    {
        public ClientUserDM()
        {
        }
        public GenderDM? Gender { get; set; }

        [ForeignKey(nameof(ClientCompanyDetail))]
        public int? ClientCompanyDetailId { get; set; }
        public virtual ClientCompanyDetailDM? ClientCompanyDetail { get; set; }

    }
}

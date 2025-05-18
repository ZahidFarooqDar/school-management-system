using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SMSDomainModels.Enums;
using SMSDomainModels.Client;
using SMSDomainModels.AppUser.Login;

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

using SMSDomainModels.Enums;
using SMSDomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMSDomainModels.AppUser
{
    public class ExternalUserDM : SMSDomainModelBase<int>
    {
        public string RefreshToken { get; set; }

        [ForeignKey(nameof(ClientUser))]
        public int ClientUserId { get; set; }

        public ExternalUserTypeDM ExternalUserType { get; set; }
        public virtual ClientUserDM ClientUser { get; set; }

    }
}

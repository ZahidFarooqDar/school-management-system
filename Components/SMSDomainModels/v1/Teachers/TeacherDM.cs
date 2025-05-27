using SMSDomainModels.AppUser;
using SMSDomainModels.AppUser.Login;
using SMSDomainModels.Enums;
using System.ComponentModel.DataAnnotations.Schema;
namespace SMSDomainModels.v1.Teachers
{
    public class TeacherDM : LoginUserDM
    {
        public GenderDM? Gender { get; set; }

        [ForeignKey(nameof(ClientUser))]
        public int? ClientUserId { get; set; }
        public virtual ClientUserDM? ClientUser { get; set; }
    }
}

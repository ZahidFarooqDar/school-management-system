using SMSDomainModels.AppUser;
using SMSDomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;

namespace SMSDomainModels.Client
{
    [Microsoft.EntityFrameworkCore.Index(nameof(CompanyCode), IsUnique = true)]
    public class ClientCompanyDetailDM : SMSDomainModelBase<int>
    {
        public ClientCompanyDetailDM()
        {
        }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string CompanyCode { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; }

        [StringLength(100, MinimumLength = 0)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(50)]
        [EmailAddress]
        public string ContactEmail { get; set; }

        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^\(?([0-9]{2})[-. ]?([0-9]{4})[-. ]?([0-9]{3})[-. ]?([0-9]{3})$", ErrorMessage = "Not a valid Phone number")]
        public string? CompanyMobileNumber { get; set; }

        [Url]
        public string? CompanyWebsite { get; set; }

        public string? CompanyLogoPath { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CompanyDateOfEstablishment { get; set; }
        public virtual HashSet<ClientUserDM> ClientEmployeeUsers { get; set; }

    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SMSDomainModels.Enums;
using SMSDomainModels.Foundation.Base;

namespace SMSDomainModels.AppUser.Login
{
    public class LoginUserDM : SMSDomainModelBase<int>
    {
        public LoginUserDM()
        {
            //this.ProfilePicturePath = "Content/loginusers/profile/default_original.jpg";
        }

        [NotNull]
        [Required]
        public RoleTypeDM RoleType { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string LoginId { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        [DefaultValue("")]
        public string FirstName { get; set; }

        [StringLength(50, MinimumLength = 0)]
        public string? MiddleName { get; set; }
        [StringLength(50)]
        public string? LastName { get; set; }
        [MaxLength(50)]
        [EmailAddress]
        public string EmailId { get; set; }
        
        public string? PasswordHash { get; set; }
        [DataType(DataType.PhoneNumber)]
        [DefaultValue(null)]

        public string? PhoneNumber { get; set; }
        public string? ProfilePicturePath { get; set; }

        [DefaultValue(false)]
        public bool IsEmailConfirmed { get; set; }
        [DefaultValue(false)]
        public bool IsPhoneNumberConfirmed { get; set; }
        public LoginStatusDM LoginStatus { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? DateOfBirth { get; set; }
    }
}

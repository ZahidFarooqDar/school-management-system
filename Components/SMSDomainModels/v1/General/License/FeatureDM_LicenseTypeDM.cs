using System.ComponentModel.DataAnnotations;

namespace SMSDomainModels.v1.General.License
{
    public class FeatureDM_LicenseTypeDM
    {
        [Key]
        public int Id { get; set; }
        public int FeatureId { get; set; }
        public virtual FeatureDM Feature { get; set; }

        public int LicenseTypeId { get; set; }
        public virtual LicenseTypeDM LicenseType { get; set; }
    }
}

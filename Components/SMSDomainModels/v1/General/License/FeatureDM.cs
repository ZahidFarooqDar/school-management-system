using SMSDomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;

namespace SMSDomainModels.v1.General.License
{
    public class FeatureDM : SMSDomainModelBase<int>
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }
        [StringLength(50)]
        public string FeatureCode { get; set; }
        public int UsageCount { get; set; }
        public bool isFeatureCountable { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime EndDate { get; set; }

    }
}

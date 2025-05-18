using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SMSDomainModels.Foundation.Base
{
    public class DomainModelRoot<T> : DomainModelRootBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public T Id { get; set; }

        public string? CreatedBy { get; set; }

        public string? LastModifiedBy { get; set; }
    }
}

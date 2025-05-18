namespace SMSDomainModels.Foundation.Base
{
    public class DomainModelRootBase
    {
        public DateTime CreatedOnUTC { get; set; }

        public DateTime? LastModifiedOnUTC { get; set; }

        protected DomainModelRootBase()
        {
            CreatedOnUTC = DateTime.UtcNow;
        }
    }
}

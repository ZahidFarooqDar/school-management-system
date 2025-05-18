using SMSServiceModels.Foundation.Base.CommonResponseRoot;

namespace SMSFoundation.AutoMapperBindings
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ConvertFilePathToUriAttribute : AutoInjectRootAttribute
    {
        public string? SourcePropertyName { get; set; }
    }
}

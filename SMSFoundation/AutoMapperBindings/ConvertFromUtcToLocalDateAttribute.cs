using SMSServiceModels.Foundation.Base.CommonResponseRoot;

namespace SMSFoundation.AutoMapperBindings
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ConvertFromUtcToLocalDateAttribute : AutoInjectRootAttribute
    {
        public string? SourcePropertyName { get; set; }
    }
}

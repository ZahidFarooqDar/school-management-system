using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSServiceModels.Foundation.Base.Enums;

namespace SMSFoundation.AutoMapperBindings
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class IgnorePropertyOnReadAttribute : AutoInjectRootAttribute
    {
        public AutoMapConversionType ConversionType { get; set; }

        public IgnorePropertyOnReadAttribute(AutoMapConversionType conversionType = AutoMapConversionType.All)
        {
            ConversionType = conversionType;
        }
    }
}

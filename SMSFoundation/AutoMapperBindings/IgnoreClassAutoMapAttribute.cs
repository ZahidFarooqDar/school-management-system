using SMSServiceModels.Foundation.Base.CommonResponseRoot;

namespace SMSFoundation.AutoMapperBindings
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class IgnoreClassAutoMapAttribute : AutoInjectRootAttribute
    {
    }
}

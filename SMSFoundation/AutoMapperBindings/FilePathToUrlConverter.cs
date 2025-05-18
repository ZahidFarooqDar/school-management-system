using AutoMapper;
using SMSBAL.Foundation.CommonUtils;

namespace SMSFoundation.AutoMapperBindings
{
    public class FilePathToUrlConverter : IValueConverter<string, string>
    {
        public string Convert(string sourceMember, ResolutionContext context)
        {
            return sourceMember.ConvertFromFilePathToUrl();
        }
    }
}

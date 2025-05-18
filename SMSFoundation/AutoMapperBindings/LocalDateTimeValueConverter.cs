using AutoMapper;

namespace SMSFoundation.AutoMapperBindings
{
    public class LocalDateTimeValueConverter : IValueConverter<DateTime, DateTime>
    {
        public DateTime Convert(DateTime sourceMember, ResolutionContext context)
        {
            return sourceMember.ConvertFromUTCToSystemTimezone();
        }
    }
}

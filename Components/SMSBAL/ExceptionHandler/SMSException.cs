using SMSServiceModels.Foundation.Base.Enums;
namespace SMSBAL.ExceptionHandler
{
    public class SMSException : ApiExceptionRoot
    {
        
        public SMSException(ApiErrorTypeSM exceptionType, string devMessage,
           string displayMessage = "", Exception innerException = null)
            : base(exceptionType, devMessage, displayMessage, innerException)
        { }
    }
}

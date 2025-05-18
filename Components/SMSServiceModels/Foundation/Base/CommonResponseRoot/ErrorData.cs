using Newtonsoft.Json;
using SMSServiceModels.Foundation.Base.Enums;

namespace SMSServiceModels.Foundation.Base.CommonResponseRoot
{
    public class ErrorData
    {
        [JsonProperty("errorType")]
        public ApiErrorTypeSM ApiErrorType { get; set; }

        [JsonProperty("displayMessage")]
        public string DisplayMessage { get; set; }

        [JsonProperty("additionalProps")]
        public Dictionary<string, object>? AdditionalProps { get; set; }

        public ErrorData()
        {
            AdditionalProps = new Dictionary<string, object>();
        }
    }
}

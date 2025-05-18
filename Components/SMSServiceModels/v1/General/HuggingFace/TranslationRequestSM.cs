using SMSServiceModels.Enums;

namespace SMSServiceModels.v1.General.HuggingFace
{
    public class TranslationRequestSM
    {
        public string Text { get; set; }
        public TextTranslationLanguageSupportSM Language { get; set; }
    }
}

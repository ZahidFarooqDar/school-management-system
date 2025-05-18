﻿using SMSServiceModels.Foundation.Base;

namespace SMSServiceModels.v1.General.ScanCodes
{
    public class ScanCodesFormatSM : SMSServiceModelBase<int>
    {
        public string BarcodeFormat { get; set; }
        public string BarcodeFormatName { get; set; }
        public string Regex { get; set; }
        public string Description { get; set; }
        public string ErrorData { get; set; }
        public string ValidationMessage { get; set; }
    }
}

﻿using SMSServiceModels.Foundation.Base;

namespace SMSServiceModels.v1.General.License
{
    public class FeatureSM : SMSServiceModelBase<int>
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string FeatureCode { get; set; }
        public int ValidityInDays { get; set; }
        public int UsageCount { get; set; }
        public bool isFeatureCountable { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}

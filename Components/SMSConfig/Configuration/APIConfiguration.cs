namespace SMSConfig.Configuration
{
    public class APIConfiguration : APIConfigRoot
    {
        #region General Config Settings
        public string ApiDbConnectionString { get; set; }
        public string JwtTokenSigningKey { get; set; }
        public double DefaultTokenValidityDays { get; set; }
        public string JwtIssuerName { get; set; }
        public string AuthTokenEncryptionKey { get; set; }
        public string AuthTokenDecryptionKey { get; set; }
        public string SuperAdminUserAdditionKey { get; set; }
        public string BlogApiUrl { get; set; }
        public int ValidityInDays { get; set; }
        public int RedeemCodeValidity { get; set; }

        public string AzureProcessingModel { get; set; }
        public string HuggingFaceProcessingModel { get; set; }


        #region External App Integration
        public ExternalIntegrations ExternalIntegrations { get; set; }

        #endregion External App Integration


        #endregion General Config Settings

        #region SmtpMail Settings
        public SmtpMailSettings SmtpMailSettings { get; set; }


        #endregion
    }
}

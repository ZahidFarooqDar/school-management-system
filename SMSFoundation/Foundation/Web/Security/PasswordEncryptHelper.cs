using SMSFoundation.Foundation.AuthenticationHelper;
using SMSServiceModels.Foundation.Base.Interfaces;

namespace SMSFoundation.Foundation.Web.Security
{
    public class PasswordEncryptHelper : Rfc2898Helper, IPasswordEncryptHelper, IEncryptHelper
    {
        public PasswordEncryptHelper(string encryptionKey, string decryptionKey)
            : base(encryptionKey, decryptionKey)
        {
        }
    }
}

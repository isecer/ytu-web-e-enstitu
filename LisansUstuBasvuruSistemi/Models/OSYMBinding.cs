using System.IdentityModel.Tokens;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
namespace OsymWebServiceClient
{
    public class OSYMBinding : CustomBinding
    {
        public OSYMBinding()
        {
            var security =
            TransportSecurityBindingElement.CreateUserNameOverTransportBindingElement(); security.IncludeTimestamp = true;
            security.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Basic256; security.MessageSecurityVersion =
            MessageSecurityVersion.WSSecurity10WSTrust13WSSecureConversation13WSSecurityPolicy12BasicSecurityProfile10;
            security.EnableUnsecuredResponse = true;
            var encoding = new TextMessageEncodingBindingElement(); encoding.MessageVersion = MessageVersion.Soap11;
            var transport = new HttpsTransportBindingElement(); transport.MaxReceivedMessageSize = 2000000; Elements.Add(security);
            Elements.Add(encoding);
            Elements.Add(transport);
        }
    }
    public class OSYMCredentials : ClientCredentials
    {
        public OSYMCredentials() { }
        protected OSYMCredentials(OSYMCredentials cc) : base(cc) { }
        public override System.IdentityModel.Selectors.SecurityTokenManager
        CreateSecurityTokenManager()
        {
            return new OSYMSecurityTokenManager(this);
        }
        protected override ClientCredentials CloneCore()
        {
            return new OSYMCredentials(this);
        }
    }
    public class OSYMSecurityTokenManager : ClientCredentialsSecurityTokenManager
    {
        public OSYMSecurityTokenManager(OSYMCredentials cred) : base(cred) { }
        public override System.IdentityModel.Selectors.SecurityTokenSerializer
        CreateSecurityTokenSerializer(System.IdentityModel.Selectors.SecurityTokenVersion version)
        {
            return new OSYMTokenSerializer(SecurityVersion.WSSecurity10);
        }
    }
    public class OSYMTokenSerializer : WSSecurityTokenSerializer
    {
        public OSYMTokenSerializer(SecurityVersion sv) : base(sv) { }
        protected override void WriteTokenCore(System.Xml.XmlWriter writer, SecurityToken token)
        {
            UserNameSecurityToken userToken = token as UserNameSecurityToken;
            string tokennamespace = "o";
            string stringToWrite = string.Format(
            @"<{0}:UsernameToken u:Id=""{1}"" xmlns:u=""http://docs.oasisopen.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">
<{0}:Username>{2}</{0}:Username>
<{0}:Password Type=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wssusername-token-profile-1.0#PasswordText"">{3}</{0}:Password>
</{0}:UsernameToken>", tokennamespace, token.Id, userToken.UserName, userToken.Password);
            writer.WriteRaw(stringToWrite);
        }
    }
}
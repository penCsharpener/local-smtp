// source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/ApiModel

namespace LocalSmtp.Shared.ApiModels
{
    public class ServerRelayOptions
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string[] AutomaticEmails { get; set; }
        public string SenderAddress { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string TlsMode { get; set; }
    }
}

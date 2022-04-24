// source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/ApiModel

namespace LocalSmtp.Shared.ApiModels
{
    public class AttachmentSummary
    {
        public string FileName { get; set; }
        public string ContentId { get; set; }
        public string Id { get; set; }
        public string Url { get; set; }
    }
}

// source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/DbModel
using System.ComponentModel.DataAnnotations;

namespace LocalSmtp.Server.Infrastructure.Models;

public class Session
{
    public Session()
    {

    }

    [Key]
    public Guid Id { get; set; }
    public string Log { get; set; }
    public string ClientAddress { get; internal set; }
    public string ClientName { get; internal set; }
    public DateTime? EndDate { get; internal set; }
    public DateTime StartDate { get; internal set; }
    public int NumberOfMessages { get; internal set; }
    public string SessionError { get; internal set; }
    public InternalSessionErrorType? SessionErrorType { get; internal set; }
}

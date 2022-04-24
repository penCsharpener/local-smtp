namespace LocalSmtp.Server.Infrastructure.Models;

public enum InternalSessionErrorType
{
    None,
    NetworkError,
    UnexpectedException,
    ServerShutdown
}

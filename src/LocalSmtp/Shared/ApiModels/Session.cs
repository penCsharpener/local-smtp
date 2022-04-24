// source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/ApiModel

namespace LocalSmtp.Shared.ApiModels
{
    public class Session
    {
        //public Session(DbModel.Session dbSession)
        //{
        //    this.Id = dbSession.Id;
        //    this.Error = dbSession.SessionError;
        //    this.ErrorType = dbSession.SessionErrorType.ToString();
        //}
        public Guid Id { get; private set; }
        public string ErrorType { get; private set; }
        public string Error { get; private set; }
    }
}

namespace LocalSmtp.Server.Application.Services.Abstractions;

public interface ITaskQueue
{
    Task QueueTask(Action action, bool priority);
    void Start();
}

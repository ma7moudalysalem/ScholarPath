namespace ScholarPath.Application.Common.Interfaces;

public interface ISessionExpiryJob
{
    Task RunAsync(CancellationToken cancellationToken);
}

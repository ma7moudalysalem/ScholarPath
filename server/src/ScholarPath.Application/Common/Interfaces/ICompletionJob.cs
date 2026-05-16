namespace ScholarPath.Application.Common.Interfaces;

public interface ICompletionJob
{
    Task RunAsync(CancellationToken cancellationToken);
}

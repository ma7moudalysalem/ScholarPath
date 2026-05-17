namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Recurring job that closes scholarship listings once their deadline has passed
/// (FR-230). Implemented by Infrastructure, scheduled by the API host.
/// </summary>
public interface IScholarshipAutoCloseJob
{
    Task RunAsync(CancellationToken cancellationToken);
}

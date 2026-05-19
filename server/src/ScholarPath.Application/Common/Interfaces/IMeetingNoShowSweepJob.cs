namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// FR-217 — recurring sweep that attributes an automated no-show to whichever
/// booking participant never joined the session room.
/// </summary>
public interface IMeetingNoShowSweepJob
{
    Task RunAsync(CancellationToken ct);
}

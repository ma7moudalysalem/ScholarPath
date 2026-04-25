namespace ScholarPath.Application.Common.Interfaces;

public interface ICommunityRealtimeNotifier
{
    Task NotifyNewPostAsync(Guid postId, string categorySlug, CancellationToken ct);
    Task NotifyNewReplyAsync(Guid replyId, Guid parentPostId, CancellationToken ct);
}

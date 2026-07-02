using FluentAssertions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;
using Xunit;

namespace ScholarPath.UnitTests.Notifications;

public class NotificationCatalogTests
{
    private readonly NotificationCatalog _catalog = new();

    [Fact]
    public void Renders_resource_approved_with_the_resource_title()
    {
        var content = _catalog.Render(NotificationType.ResourceApproved,
            new NotificationParams { TitleEn = "My Guide", TitleAr = "دليلي" });

        content.BodyEn.Should().Contain("My Guide");
        content.BodyAr.Should().Contain("دليلي");
    }

    [Theory]
    [InlineData("Full")]
    [InlineData("Partial")]
    [InlineData("Timeout")]
    public void Renders_a_non_empty_review_refund_message_per_variant(string kind)
    {
        var content = _catalog.Render(NotificationType.ScholarshipProviderReviewRefunded,
            new NotificationParams { RefundKind = kind });

        content.TitleEn.Should().NotBeNullOrEmpty();
        content.BodyEn.Should().NotBeNullOrEmpty();
        content.BodyAr.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Partial_and_full_refund_messages_differ()
    {
        var partial = _catalog.Render(NotificationType.ScholarshipProviderReviewRefunded,
            new NotificationParams { RefundKind = "Partial" });
        var full = _catalog.Render(NotificationType.ScholarshipProviderReviewRefunded,
            new NotificationParams { RefundKind = "Full" });

        partial.BodyEn.Should().NotBe(full.BodyEn);
    }

    [Fact]
    public void Broadcast_passes_raw_admin_content_through()
    {
        var raw = new NotificationContent("Title", "عنوان", "Body", "نص");

        var content = _catalog.Render(NotificationType.Broadcast,
            new NotificationParams { RawContent = raw });

        content.Should().Be(raw);
    }

    [Fact]
    public void Unmapped_type_falls_back_to_a_generic_message()
    {
        var content = _catalog.Render(NotificationType.BookingConfirmed, NotificationParams.Empty);

        content.TitleEn.Should().NotBeNullOrEmpty();
        content.BodyEn.Should().NotBeNullOrEmpty();
        content.BodyAr.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Renders_onboarding_submitted_with_applicant_name_and_role()
    {
        var content = _catalog.Render(NotificationType.OnboardingSubmitted,
            new NotificationParams { CounterpartyName = "Acme University", StatusText = "ScholarshipProvider" });

        content.BodyEn.Should().Contain("Acme University").And.Contain("ScholarshipProvider");
        content.TitleAr.Should().NotBeNullOrEmpty();
        content.BodyAr.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Renders_upgrade_request_submitted_with_student_name()
    {
        var content = _catalog.Render(NotificationType.UpgradeRequestSubmitted,
            new NotificationParams { CounterpartyName = "Mona Hassan", StatusText = "Consultant" });

        content.BodyEn.Should().Contain("Mona Hassan");
        content.BodyAr.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Renders_content_reported_with_the_reason()
    {
        var content = _catalog.Render(NotificationType.ContentReported,
            new NotificationParams { Reason = "Spam" });

        content.BodyEn.Should().Contain("Spam");
        content.BodyAr.Should().Contain("Spam");
        content.TitleEn.Should().NotBeNullOrEmpty();
    }
}

namespace ScholarPath.Domain.Enums;

public enum AccountStatus
{
    Unassigned = 0,
    PendingApproval = 1,
    Active = 2,
    Suspended = 3,
    Deactivated = 4,
}

public enum UpgradeTarget
{
    Company = 1,
    Consultant = 2,
}

public enum UpgradeRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3,
}

public enum ScholarshipStatus
{
    Draft = 0,
    Open = 1,
    Closed = 2,
    Archived = 3,
    UnderReview = 4,
}

public enum ListingMode
{
    InApp = 0,
    ExternalUrl = 1,
}

public enum FundingType
{
    FullyFunded = 0,
    PartiallyFunded = 1,
    TuitionOnly = 2,
    StipendOnly = 3,
    Other = 99,
}

public enum AcademicLevel
{
    HighSchool = 0,
    Undergrad = 1,
    Masters = 2,
    PhD = 3,
    PostDoc = 4,
    Other = 99,
}

public enum ApplicationStatus
{
    Draft = 0,
    Pending = 1,
    UnderReview = 2,
    Shortlisted = 3,
    Accepted = 4,
    Rejected = 5,
    Withdrawn = 6,

    // External self-tracked states
    Intending = 100,
    Applied = 101,
    WaitingResult = 102,
}

public enum ApplicationMode
{
    InApp = 0,
    External = 1,
}

public enum BookingStatus
{
    Requested = 0,
    Confirmed = 1,
    Rejected = 2,
    Expired = 3,
    Cancelled = 4,
    Completed = 5,
    NoShowStudent = 6,
    NoShowConsultant = 7,
}

public enum PaymentType
{
    ConsultantBooking = 0,
    CompanyReview = 1,
}

public enum PaymentStatus
{
    Pending = 0,
    Held = 1,
    Captured = 2,
    Refunded = 3,
    PartiallyRefunded = 4,
    Failed = 5,
    Cancelled = 6,
}

public enum PayoutStatus
{
    Pending = 0,
    InTransit = 1,
    Paid = 2,
    Failed = 3,
}

public enum NotificationChannel
{
    InApp = 0,
    Email = 1,
    Push = 2,
}

public enum NotificationType
{
    // Applications (PB-004)
    ApplicationSubmitted = 100,
    ApplicationStatusChanged = 101,
    ApplicationDeadlineApproaching = 102,
    ApplicationWithdrawn = 103,

    // Company review + payment (PB-005)
    CompanyRatingReceived = 110,
    CompanyReviewPaymentSuccess = 111,
    CompanyReviewRefunded = 112,

    // Consultant booking (PB-006)
    BookingRequested = 200,
    BookingConfirmed = 201,
    BookingRejected = 202,
    BookingExpired = 203,
    BookingCancelled = 204,
    BookingReminder = 205,
    BookingCompleted = 206,
    ConsultantRatingReceived = 207,

    // Payments (PB-013)
    PaymentSuccess = 300,
    PaymentRefunded = 301,
    PayoutInitiated = 302,
    PayoutCompleted = 303,
    PayoutFailed = 304,

    // Admin / onboarding (PB-001, PB-011)
    OnboardingApproved = 400,
    OnboardingRejected = 401,
    UpgradeRequestApproved = 402,
    UpgradeRequestRejected = 403,
    AdminApprovalRequired = 404,

    // Community (PB-007)
    ReplyOnYourPost = 500,
    PostAutoHidden = 501,

    // Resources (PB-009)
    ResourceApproved = 600,
    ResourceRejected = 601,

    // Broadcast
    Broadcast = 900,
}

public enum PostModerationStatus
{
    Visible = 0,
    Hidden = 1,
    Removed = 2,
    PendingReview = 3,
}

public enum VoteType
{
    Up = 1,
    Down = -1,
}

public enum ResourceStatus
{
    Draft = 0,
    PendingReview = 1,
    Published = 2,
    Hidden = 3,
    Removed = 4,
}

public enum ResourceType
{
    Article = 0,
    Guide = 1,
    Checklist = 2,
    VideoLink = 3,
}

public enum AiFeature
{
    Recommendation = 0,
    Eligibility = 1,
    Chatbot = 2,
}

public enum AiProvider
{
    Stub = 0,
    OpenAi = 1,
    AzureOpenAi = 2,
}

/// <summary>
/// PII-redaction audit verdict per sample (PB-017 US-178 / FR-255).
/// </summary>
public enum RedactionVerdict
{
    Clean = 0,
    MissedEmail = 1,
    MissedPhone = 2,
    MissedCard = 3,
}

public enum UserDataRequestType
{
    Export = 0,
    Delete = 1,
}

public enum UserDataRequestStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Cancelled = 3,
    Failed = 4,
}

public enum AuditAction
{
    Create = 0,
    Update = 1,
    Delete = 2,
    Login = 3,
    Logout = 4,
    LoginFailed = 5,
    PasswordReset = 6,
    RoleChanged = 7,
    Approved = 8,
    Rejected = 9,
    Moderated = 10,
    PaymentCaptured = 11,
    PaymentRefunded = 12,
    ConfigChanged = 13,
    BroadcastSent = 14,

    BookingRequested = 100,
    BookingAccepted = 101,
    BookingRejected = 102,
    BookingCancelled = 103,
    BookingNoShowMarked = 104,
    ConsultantRatingSubmitted = 105,
    ConsultantAvailabilityUpdated = 106
}

public enum CancellationReason
{
    StudentCancelledBeforeAcceptance = 0,
    StudentCancelledMoreThan24HoursBefore = 1,
    StudentCancelledLessThan24HoursBefore = 2,
    ConsultantCancelledAfterAcceptance = 3,
    ConsultantNoShow = 4,
    StudentNoShow = 5,
    AutoExpiredNoResponse = 6,
    RejectedByConsultant = 7,
    Other = 99
}

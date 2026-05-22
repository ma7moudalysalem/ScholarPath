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

/// <summary>
/// Lifecycle of a paid application-support / document-review request raised by a
/// Student against a Company-owned scholarship (PB-005 "Apply Now" flow). The
/// state machine is intentionally distinct from <see cref="ApplicationStatus"/>
/// — an Application is the catalog-tracking entity, a CompanyReviewRequest is
/// the paid engagement with its associated <see cref="PaymentType.CompanyReview"/>
/// payment row.
///
/// Allowed transitions:
///   Draft        → Submitted
///   Submitted    → Pending | Cancelled | Failed
///   Pending      → UnderReview | CancelledByStudent | RejectedByCompany | Expired
///   UnderReview  → Completed | CancelledByStudent
///   Completed    → Closed
///
/// Explicitly NOT modelled here: Disputed and CompanyFailure refund cases — the
/// platform does not support those for this flow.
/// </summary>
public enum CompanyReviewRequestStatus
{
    Draft = 0,
    Submitted = 1,
    Pending = 2,
    UnderReview = 3,
    Completed = 4,
    Closed = 5,
    Cancelled = 6,
    Failed = 7,
    CancelledByStudent = 8,
    RejectedByCompany = 9,
    Expired = 10,
}

/// <summary>Whether a financial-config rule's platform fee is a percentage or a flat amount.</summary>
public enum FeeKind
{
    Percentage = 0,
    FixedAmount = 1,
}

/// <summary>Lifecycle state of an admin financial-configuration rule (FR-163..176).</summary>
public enum FinancialRuleStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2,
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
    Disputed = 7,
}

public enum PayoutStatus
{
    Pending = 0,
    InTransit = 1,
    Paid = 2,
    Failed = 3,
}

public enum StripeConnectStatus
{
    None = 0,
    Pending = 1,
    Verified = 2,
    Restricted = 3,
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
    ApplicationDraftReminder = 104,

    // Company review + payment (PB-005)
    CompanyRatingReceived = 110,
    CompanyReviewPaymentSuccess = 111,
    CompanyReviewRefunded = 112,

    // Company review REQUEST lifecycle (PB-005 Apply Now → paid support flow).
    // Each event carries the financial breakdown (held / captured / refunded,
    // platform commission, company share) via NotificationParams so both
    // in-app and email channels render the same numbers.
    CompanyReviewRequestPaymentHeld = 113,
    CompanyReviewRequestPaymentCaptured = 114,
    CompanyReviewRequestPaymentHoldCancelled = 115,
    CompanyReviewRequestPartiallyRefunded = 116,
    CompanyReviewRequestCompleted = 117,
    CompanyReviewRequestIncoming = 118,

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
    PaymentDisputed = 305,
    PaymentReceived = 306,
    PaymentHeld = 307,

    // Admin / onboarding (PB-001, PB-011)
    OnboardingApproved = 400,
    OnboardingRejected = 401,
    UpgradeRequestApproved = 402,
    UpgradeRequestRejected = 403,
    AdminApprovalRequired = 404,
    // Admin-inbound: a Company/Consultant applicant has reached the onboarding queue.
    OnboardingSubmitted = 405,
    // Admin-inbound: a Student has submitted a consultant upgrade request.
    UpgradeRequestSubmitted = 406,

    // Community (PB-007)
    ReplyOnYourPost = 500,
    PostAutoHidden = 501,
    // Admin-inbound: a community post was reported/flagged and needs moderation.
    ContentReported = 502,

    // Resources (PB-009)
    ResourceApproved = 600,
    ResourceRejected = 601,

    // Chat (PB-007)
    ChatMessageReceived = 700,

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
/// Origin of a <c>KnowledgeDocument</c> in the RAG knowledge base.
/// </summary>
public enum KnowledgeSourceType
{
    /// <summary>Indexed from a scholarship listing (in-app or imported from a dataset).</summary>
    Scholarship = 0,

    /// <summary>A curated help / frequently-asked-question entry.</summary>
    Faq = 1,

    /// <summary>Indexed from a published Resources Hub article or guide (PB-009).</summary>
    Resource = 2,

    /// <summary>Indexed from a verified consultant's profile so the AI can answer
    /// questions like "who's good at SoP review for German universities?".</summary>
    Consultant = 3,

    /// <summary>Indexed from a high-quality community post (top voted, not hidden)
    /// so the AI can surface peer wisdom on application questions.</summary>
    CommunityPost = 4,
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

/// <summary>
/// The data type stored in a <c>PlatformSetting.Value</c> string (PB-011).
/// Drives validation server-side and input rendering client-side.
/// </summary>
public enum PlatformSettingType
{
    Text = 0,
    Boolean = 1,
    Number = 2,
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

/// <summary>
/// Folder a vault document is filed under (FR-216). Lets a user organise
/// their personal document store and attach the right file to an application.
/// </summary>
public enum DocumentCategory
{
    Other = 0,
    Transcript = 1,
    Certificate = 2,
    RecommendationLetter = 3,
    PersonalStatement = 4,
    Resume = 5,
    IdentityDocument = 6,
    ProofOfEnglish = 7,
    FinancialDocument = 8,
    Portfolio = 9,

    /// <summary>
    /// A verification document (business registration, professional credential)
    /// a Company or Consultant uploads to support their onboarding request.
    /// </summary>
    OnboardingDocument = 10,
}

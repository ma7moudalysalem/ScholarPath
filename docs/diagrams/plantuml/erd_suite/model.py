#!/usr/bin/env python3
"""ScholarPath data-model — the SINGLE source of truth for all three diagram
views (plain ERD, Enhanced ERD, Relational Mapping), each rendered both as one
big overview and as six per-module detail diagrams.

Verified 2026-07-05 against server/src/ScholarPath.Domain/Entities/*.cs and the
EF Core migration snapshot. Payments are out of scope (free-mode) and are not
present here at all. Company has been renamed ScholarshipProvider throughout.

An attribute is (label, kind) where kind is one of:
    'key'        primary/identifying key      -> solid underline
    'pkey'       partial key (weak entities)  -> dashed underline
    None         plain attribute              -> plain oval
    'multi'      multivalued                  -> double oval
    'derived'    derived / computed           -> dashed oval
    ('comp', [sub...])  composite             -> oval with child ovals

A relationship carries cardinality + participation on each side:
    part '=' total participation (double line)   '-' partial (single line)
    card '1' | 'N' | 'M'
plus  identifying=True for weak-entity owners (double diamond) and an optional
attrs list (for M:N relationships that carry their own attributes, drawn Chen-
style as ovals hanging off the diamond).
"""

def comp(*subs):
    return ("comp", list(subs))

# ============================================================================
#  ENTITIES  — name -> dict(module, weak, standalone, owner, attrs, roles)
#  'roles' (module-1 USER_PROFILE only) splits attributes into the shared set
#  and the per-subclass sets, so the plain ERD can show them all on the profile
#  while the EERD distributes the role sets onto the specialization subclasses.
# ============================================================================

M1 = "Identity, Access & Profile"
M2 = "Scholarships, Applications & Documents"
M3 = "Consultant Booking & Ratings"
M4 = "Community & Chat"
M5 = "Resources Hub & Notifications"
M6 = "AI, Knowledge Base & Platform"

ENTITIES = {
    # ---- Module 1 : Identity ------------------------------------------------
    "USER": dict(module=M1, attrs=[
        ("Id", "key"), ("Email", None), ("Name", comp("FirstName", "LastName")),
        ("AccountStatus", None), ("ActiveRole", None), ("IsOnboardingComplete", None),
        ("PreferredLanguage", None), ("CountryOfResidence", None), ("LastLoginAt", None)]),
    "USER_PROFILE": dict(module=M1, attrs=[
        ("Id", "key"), ("Biography", None), ("DateOfBirth", None), ("Nationality", None),
        ("LinkedInUrl", None), ("Timezone", None),
        ("ProfileCompletenessPercent", "derived")]),
    "ROLE": dict(module=M1, attrs=[("Id", "key"), ("Name", None), ("Description", None)]),
    "EDUCATION_ENTRY": dict(module=M1, weak=True, owner="USER_PROFILE", attrs=[
        ("InstitutionName", "pkey"), ("Degree", None), ("FieldOfStudy", None),
        ("StartDate", None), ("EndDate", None), ("Gpa", None)]),
    "EXPERTISE_TAG": dict(module=M1, standalone=True, attrs=[
        ("Id", "key"), ("Slug", None), ("Name", comp("NameEn", "NameAr")), ("Category", None)]),
    "REFRESH_TOKEN": dict(module=M1, attrs=[
        ("Id", "key"), ("TokenHash", None), ("ExpiresAt", None), ("IsRevoked", None), ("RevokedAt", None)]),
    "PASSWORD_RESET_TOKEN": dict(module=M1, attrs=[
        ("Id", "key"), ("TokenHash", None), ("ExpiresAt", None), ("UsedAt", None)]),
    "LOGIN_ATTEMPT": dict(module=M1, attrs=[
        ("Id", "key"), ("Email", None), ("Succeeded", None), ("FailureReason", None), ("OccurredAt", None)]),
    "UPGRADE_REQUEST": dict(module=M1, attrs=[
        ("Id", "key"), ("Target", None), ("Status", None), ("Reason", None),
        ("ReviewerNotes", None), ("ReviewedAt", None)]),
    "UPGRADE_REQUEST_FILE": dict(module=M1, weak=True, owner="UPGRADE_REQUEST", attrs=[
        ("FileName", "pkey"), ("BlobUrl", None), ("ContentType", None), ("SizeBytes", None)]),
    "UPGRADE_REQUEST_LINK": dict(module=M1, weak=True, owner="UPGRADE_REQUEST", attrs=[
        ("Label", "pkey"), ("Url", None)]),

    # ---- Module 2 : Scholarships -------------------------------------------
    "CATEGORY": dict(module=M2, attrs=[
        ("Id", "key"), ("Slug", None), ("Name", comp("NameEn", "NameAr")), ("DisplayOrder", None)]),
    "SCHOLARSHIP": dict(module=M2, attrs=[
        ("Id", "key"), ("Slug", None), ("Title", comp("TitleEn", "TitleAr")), ("Mode", None),
        ("Status", None), ("Deadline", None), ("FundingType", None), ("FundingAmountUsd", None),
        ("TargetLevel", None), ("TargetCountries", "multi"), ("FieldsOfStudy", "multi"), ("IsFeatured", None)]),
    "SCHOLARSHIP_CHILD": dict(module=M2, weak=True, owner="SCHOLARSHIP", attrs=[
        ("ChildType", "pkey"), ("KeyEn", None), ("ValueEn", None), ("SortOrder", None)]),
    "APPLICATION": dict(module=M2, attrs=[
        ("Id", "key"), ("Status", None), ("Mode", None), ("SubmittedAt", None),
        ("DecisionAt", None), ("DecisionReason", None), ("PersonalNotes", None),
        ("ExternalTitle", None), ("ExternalProvider", None)]),
    "APPLICATION_CHILD": dict(module=M2, weak=True, owner="APPLICATION", attrs=[
        ("ChildType", "pkey"), ("Title", None), ("Content", None), ("OccurredAt", None)]),
    "DOCUMENT": dict(module=M2, attrs=[
        ("Id", "key"), ("FileName", None), ("Category", None), ("ContentType", None),
        ("SizeBytes", None), ("StoragePath", None), ("UploadedAt", None)]),
    "SCHOLARSHIP_PROVIDER_REVIEW": dict(module=M2, attrs=[
        ("Id", "key"), ("Rating", None), ("Comment", None), ("IsHiddenByAdmin", None)]),
    "SCHOLARSHIP_PROVIDER_REVIEW_REQUEST": dict(module=M2, attrs=[
        ("Id", "key"), ("Status", None), ("SubmittedAt", None), ("AcceptedAt", None),
        ("CompletedAt", None), ("RejectReason", None)]),

    # ---- Module 3 : Booking -------------------------------------------------
    "AVAILABILITY": dict(module=M3, attrs=[
        ("Id", "key"), ("DayOfWeek", None), ("TimeWindow", comp("StartTime", "EndTime")),
        ("Timezone", None), ("IsRecurring", None), ("IsActive", None)]),
    "BOOKING": dict(module=M3, attrs=[
        ("Id", "key"), ("Schedule", comp("ScheduledStartAt", "ScheduledEndAt")),
        ("DurationMinutes", None), ("MeetingRoomId", None), ("Status", None),
        ("CancellationReason", None), ("IsNoShowStudent", None), ("IsNoShowConsultant", None)]),
    "NO_SHOW_REPORT": dict(module=M3, attrs=[
        ("Id", "key"), ("AccusedRole", None), ("Status", None), ("ResolvedAt", None),
        ("AdminNote", None), ("ReporterNote", None)]),
    "CONSULTANT_REVIEW": dict(module=M3, attrs=[
        ("Id", "key"), ("Rating", None), ("Comment", None), ("IsHiddenByAdmin", None)]),
    "SESSION_RECORDING": dict(module=M3, attrs=[  # STRONG: Booking FK is Restrict (not Cascade) + own independent Id
        ("Id", "key"), ("RecordingId", None), ("StoragePath", None), ("ContentType", None),
        ("SizeBytes", None), ("RecordedAt", None)]),

    # ---- Module 4 : Community ----------------------------------------------
    "FORUM_CATEGORY": dict(module=M4, attrs=[
        ("Id", "key"), ("Slug", None), ("Name", comp("NameEn", "NameAr")), ("DisplayOrder", None), ("IsActive", None)]),
    "FORUM_POST": dict(module=M4, attrs=[
        ("Id", "key"), ("BodyMarkdown", None), ("ModerationStatus", None),
        ("UpvoteCount", "derived"), ("DownvoteCount", "derived"), ("FlagCount", "derived"),
        ("ReplyCount", "derived"), ("IsAutoHidden", None)]),
    "FORUM_POST_ATTACHMENT": dict(module=M4, weak=True, owner="FORUM_POST", attrs=[
        ("FileName", "pkey"), ("BlobUrl", None), ("ContentType", None)]),
    "FORUM_TAG": dict(module=M4, attrs=[("Id", "key"), ("Slug", None), ("Name", None)]),
    "CONVERSATION": dict(module=M4, attrs=[
        ("Id", "key"), ("LastMessageAt", None),
        ("IsArchivedForParticipantOne", None), ("IsArchivedForParticipantTwo", None)]),
    "MESSAGE": dict(module=M4, weak=True, owner="CONVERSATION", attrs=[
        ("SentAt", "pkey"), ("Body", None), ("ReadAt", None)]),

    # ---- Module 5 : Resources ----------------------------------------------
    "RESOURCE": dict(module=M5, attrs=[
        ("Id", "key"), ("Slug", None), ("Title", comp("TitleEn", "TitleAr")), ("Type", None),
        ("Status", None), ("AuthorRole", None), ("IsFeatured", None), ("PublishedAt", None)]),
    "RESOURCE_CHAPTER": dict(module=M5, weak=True, owner="RESOURCE", attrs=[
        ("SortOrder", "pkey"), ("TitleEn", None), ("EstimatedReadMinutes", None)]),
    "RESOURCE_PROGRESS": dict(module=M5, attrs=[
        ("Id", "key"), ("ChaptersCompletedCount", "derived"), ("LastAccessedAt", None)]),
    "RESOURCE_PROGRESS_CHILD": dict(module=M5, weak=True, owner="RESOURCE_PROGRESS", attrs=[
        ("ResourceChildId", "pkey"), ("IsCompleted", None), ("CompletedAt", None)]),
    "NOTIFICATION": dict(module=M5, attrs=[
        ("Id", "key"), ("Type", None), ("Channel", None), ("IsRead", None), ("Priority", None), ("DispatchedAt", None)]),
    "NOTIFICATION_PREFERENCE": dict(module=M5, attrs=[  # STRONG: own PK, UserId has no FK relationship / no cascade
        ("Id", "key"), ("Type", None), ("Channel", None), ("IsEnabled", None)]),

    # ---- Module 6 : AI / Platform ------------------------------------------
    "AI_INTERACTION": dict(module=M6, attrs=[
        ("Id", "key"), ("Feature", None), ("Provider", None), ("SessionId", None),
        ("PromptTokens", None), ("CompletionTokens", None), ("CostUsd", "derived")]),
    "RECOMMENDATION_CLICK_EVENT": dict(module=M6, attrs=[
        ("Id", "key"), ("ClickedAt", None), ("Source", None)]),
    "AI_REDACTION_AUDIT_SAMPLE": dict(module=M6, attrs=[
        ("Id", "key"), ("SampledAt", None), ("Verdict", None), ("ReviewedAt", None)]),
    "KNOWLEDGE_DOCUMENT": dict(module=M6, standalone=True, attrs=[
        ("Id", "key"), ("SourceType", None), ("SourceKey", None), ("TitleEn", None),
        ("ContentHash", None), ("EmbeddingDimensions", None)]),
    "PLATFORM_SETTING": dict(module=M6, standalone=True, attrs=[
        ("Id", "key"), ("SettingKey", None), ("Value", None), ("ValueType", None), ("Category", None)]),
    "SUCCESS_STORY": dict(module=M6, attrs=[
        ("Id", "key"), ("AuthorDisplayName", None), ("HeadlineEn", None), ("IsApproved", None), ("IsFeatured", None)]),
    "USER_RISK_FLAG": dict(module=M6, attrs=[
        ("Id", "key"), ("Score", "derived"), ("IsAtRisk", None), ("Reason", None), ("ComputedAt", None)]),
    "AUDIT_LOG": dict(module=M6, attrs=[
        ("Id", "key"), ("Action", None), ("TargetType", None), ("TargetId", None),
        ("OccurredAt", None), ("Summary", None)]),
    "USER_DATA_REQUEST": dict(module=M6, attrs=[
        ("Id", "key"), ("Type", None), ("Status", None), ("RequestedAt", None), ("CompletedAt", None)]),
}

# ============================================================================
#  SPECIALIZATION (the EER "Enhanced" part) — USER super-type -> 4 subclasses.
#  Overlapping {o} (dual roles) and partial (an Unassigned user has no subclass).
#  The subclass attribute sets are the role-specific columns physically stored on
#  UserProfiles; in the plain ERD they all sit on USER_PROFILE instead.
# ============================================================================
SPECIALIZATION = dict(
    superclass="USER",
    disjoint="o",         # overlapping
    total=False,          # partial participation
    subclasses={
        "STUDENT": [("AcademicLevel", None), ("FieldOfStudy", None), ("Gpa", None),
                    ("PreferredCountries", "multi"), ("PreferredFields", "multi")],
        "SCHOLARSHIP_PROVIDER": [("OrganizationLegalName", None), ("OrganizationVerificationStatus", None),
                    ("ScholarshipProviderType", None), ("ContactPersonFullName", None),
                    ("ScholarshipProviderAverageRating", "derived"), ("ScholarshipProviderReviewCount", "derived")],
        "CONSULTANT": [("ProfessionalTitle", None), ("FieldOfExpertise", None), ("YearsOfExperience", None),
                    ("ExpertiseTags", "multi"), ("Languages", "multi"), ("SessionDurationMinutes", None),
                    ("ConsultantAverageRating", "derived"), ("ConsultantReviewCount", "derived"),
                    ("ConsultantRatingPenaltyFactor", "derived")],
        "ADMIN": [],
    },
    order=["STUDENT", "SCHOLARSHIP_PROVIDER", "CONSULTANT", "ADMIN"],
)

# For the plain ERD, USER_PROFILE carries the union of shared + all role columns.
PROFILE_ROLE_ATTRS = (SPECIALIZATION["subclasses"]["STUDENT"]
                      + SPECIALIZATION["subclasses"]["SCHOLARSHIP_PROVIDER"]
                      + SPECIALIZATION["subclasses"]["CONSULTANT"])

# ============================================================================
#  RELATIONSHIPS
#  (name, e1, card1, part1, e2, card2, part2, **opts)
#    opts: identifying=True (weak owner -> double diamond),
#          attrs=[(label,kind)...] (M:N relationship attributes),
#          role1=/role2= (participant role label shown by the entity end),
#          module= (which module diagram it belongs to; defaults to e-side)
#  part: '=' total, '-' partial.  card: '1' | 'N' | 'M'.
# ============================================================================
def R(name, e1, c1, p1, e2, c2, p2, **o):
    o.update(name=name, e1=e1, c1=c1, p1=p1, e2=e2, c2=c2, p2=p2)
    return o

RELATIONSHIPS = [
    # ---- Module 1 ----------------------------------------------------------
    R("HAS_PROFILE", "USER", "1", "-", "USER_PROFILE", "1", "=", module=M1),
    R("HAS_ROLE", "USER", "M", "=", "ROLE", "N", "-", module=M1),          # M:N
    R("HAS", "USER_PROFILE", "1", "-", "EDUCATION_ENTRY", "N", "=", identifying=True, module=M1),
    R("ISSUES", "USER", "1", "-", "REFRESH_TOKEN", "N", "=", module=M1),
    R("REQUESTS", "USER", "1", "-", "PASSWORD_RESET_TOKEN", "N", "=", module=M1),
    R("LOGS", "USER", "1", "-", "LOGIN_ATTEMPT", "N", "-", module=M1),
    R("SUBMITS_UPG", "USER", "1", "-", "UPGRADE_REQUEST", "N", "=", module=M1),
    R("ATTACHES_FILE", "UPGRADE_REQUEST", "1", "-", "UPGRADE_REQUEST_FILE", "N", "=", identifying=True, module=M1),
    R("ATTACHES_LINK", "UPGRADE_REQUEST", "1", "-", "UPGRADE_REQUEST_LINK", "N", "=", identifying=True, module=M1),

    # ---- Module 2 ----------------------------------------------------------
    R("CLASSIFIES", "CATEGORY", "1", "-", "SCHOLARSHIP", "N", "-", module=M2),
    R("OWNS", "USER", "1", "-", "SCHOLARSHIP", "N", "-", role1="provider", module=M2),
    R("DETAILS", "SCHOLARSHIP", "1", "-", "SCHOLARSHIP_CHILD", "N", "=", identifying=True, module=M2),
    R("SAVES", "USER", "M", "-", "SCHOLARSHIP", "N", "-", attrs=[("SavedAt", None), ("Note", None)], module=M2),  # M:N
    R("SUBMITS", "USER", "1", "-", "APPLICATION", "N", "=", role1="student", module=M2),
    R("FOR", "APPLICATION", "N", "-", "SCHOLARSHIP", "1", "-", module=M2),
    R("HISTORY", "APPLICATION", "1", "-", "APPLICATION_CHILD", "N", "=", identifying=True, module=M2),
    R("OWNS_DOC", "USER", "1", "-", "DOCUMENT", "N", "=", module=M2),
    R("SUPPORTS", "APPLICATION", "1", "-", "DOCUMENT", "N", "-", module=M2),
    R("RAISES", "USER", "1", "-", "SCHOLARSHIP_PROVIDER_REVIEW_REQUEST", "N", "=", role1="student", module=M2),
    R("FULFILS", "USER", "1", "-", "SCHOLARSHIP_PROVIDER_REVIEW_REQUEST", "N", "=", role1="provider", module=M2),
    R("UNDER", "SCHOLARSHIP", "1", "-", "SCHOLARSHIP_PROVIDER_REVIEW_REQUEST", "N", "=", module=M2),
    R("RATED_BY", "APPLICATION", "1", "-", "SCHOLARSHIP_PROVIDER_REVIEW", "1", "=", module=M2),
    R("WRITES", "USER", "1", "-", "SCHOLARSHIP_PROVIDER_REVIEW", "N", "=", role1="student", module=M2),
    R("ABOUT", "USER", "1", "-", "SCHOLARSHIP_PROVIDER_REVIEW", "N", "=", role1="provider", module=M2),

    # ---- Module 3 ----------------------------------------------------------
    R("PUBLISHES", "USER", "1", "-", "AVAILABILITY", "N", "=", role1="consultant", module=M3),
    R("BOOKS", "USER", "1", "-", "BOOKING", "N", "=", role1="student", module=M3),
    R("CONDUCTS", "USER", "1", "-", "BOOKING", "N", "=", role1="consultant", module=M3),
    R("RESERVES", "AVAILABILITY", "1", "-", "BOOKING", "N", "-", module=M3),
    R("CONCERNS", "BOOKING", "1", "-", "NO_SHOW_REPORT", "N", "=", module=M3),
    R("FILES", "USER", "1", "-", "NO_SHOW_REPORT", "N", "=", role1="reporter", module=M3),
    R("ACCUSES", "USER", "1", "-", "NO_SHOW_REPORT", "N", "=", role1="accused", module=M3),
    R("RATED", "BOOKING", "1", "-", "CONSULTANT_REVIEW", "1", "=", module=M3),
    R("WRITES_CR", "USER", "1", "-", "CONSULTANT_REVIEW", "N", "=", role1="student", module=M3),
    R("ABOUT_CR", "USER", "1", "-", "CONSULTANT_REVIEW", "N", "=", role1="consultant", module=M3),
    R("RECORDED", "BOOKING", "1", "-", "SESSION_RECORDING", "N", "=", module=M3),  # non-identifying: SESSION_RECORDING strong

    # ---- Module 4 ----------------------------------------------------------
    R("GROUPS", "FORUM_CATEGORY", "1", "-", "FORUM_POST", "N", "-", module=M4),
    R("AUTHORS", "USER", "1", "-", "FORUM_POST", "N", "=", module=M4),
    R("REPLIES_TO", "FORUM_POST", "1", "-", "FORUM_POST", "N", "-", module=M4),
    R("ATTACHED", "FORUM_POST", "1", "-", "FORUM_POST_ATTACHMENT", "N", "=", identifying=True, module=M4),
    R("VOTES", "USER", "M", "-", "FORUM_POST", "N", "-", attrs=[("VoteType", None), ("VotedAt", None)], module=M4),  # M:N
    R("FLAGS", "USER", "M", "-", "FORUM_POST", "N", "-", attrs=[("Reason", None), ("AdditionalDetails", None), ("FlaggedAt", None), ("IsValid", None)], module=M4),  # M:N
    R("BOOKMARKS", "USER", "M", "-", "FORUM_POST", "N", "-", attrs=[("CreatedAt", None)], module=M4),  # M:N
    R("TAGGED_WITH", "FORUM_POST", "M", "-", "FORUM_TAG", "N", "-", module=M4),  # M:N pure
    R("PARTICIPATES", "USER", "M", "-", "CONVERSATION", "N", "-", module=M4),  # M:N (exactly 2)
    R("CONTAINS", "CONVERSATION", "1", "-", "MESSAGE", "N", "=", identifying=True, module=M4),
    R("SENDS", "USER", "1", "-", "MESSAGE", "N", "-", module=M4),
    R("BLOCKS", "USER", "M", "-", "USER", "N", "-", attrs=[("BlockedAt", None), ("Reason", None)], module=M4),  # M:N self

    # ---- Module 5 ----------------------------------------------------------
    R("AUTHORS_RES", "USER", "1", "-", "RESOURCE", "N", "=", module=M5),
    R("SPLIT_INTO", "RESOURCE", "1", "-", "RESOURCE_CHAPTER", "N", "=", identifying=True, module=M5),
    R("BOOKMARKS_RES", "USER", "M", "-", "RESOURCE", "N", "-", attrs=[("BookmarkedAt", None)], module=M5),  # M:N
    R("TRACKED_BY", "USER", "1", "-", "RESOURCE_PROGRESS", "N", "=", module=M5),
    R("PROGRESS_ON", "RESOURCE", "1", "-", "RESOURCE_PROGRESS", "N", "=", module=M5),
    R("PER_CHAPTER", "RESOURCE_PROGRESS", "1", "-", "RESOURCE_PROGRESS_CHILD", "N", "=", identifying=True, module=M5),
    R("RECEIVES", "USER", "1", "-", "NOTIFICATION", "N", "=", module=M5),  # Notification.RecipientUserId required -> total
    R("CONFIGURES", "USER", "1", "-", "NOTIFICATION_PREFERENCE", "N", "=", module=M5),  # non-identifying: NOTIFICATION_PREFERENCE strong

    # ---- Module 6 ----------------------------------------------------------
    R("INVOKES", "USER", "1", "-", "AI_INTERACTION", "N", "-", module=M6),
    R("CLICKS", "USER", "1", "-", "RECOMMENDATION_CLICK_EVENT", "N", "=", module=M6),
    R("ON_SCH", "SCHOLARSHIP", "1", "-", "RECOMMENDATION_CLICK_EVENT", "N", "=", module=M6),
    R("FROM_AI", "AI_INTERACTION", "1", "-", "RECOMMENDATION_CLICK_EVENT", "N", "-", module=M6),
    R("SAMPLED", "AI_INTERACTION", "1", "-", "AI_REDACTION_AUDIT_SAMPLE", "1", "=", module=M6),
    R("SUBJECT", "USER", "1", "-", "AI_REDACTION_AUDIT_SAMPLE", "N", "=", role1="subject", module=M6),
    R("REVIEWS", "USER", "1", "-", "AI_REDACTION_AUDIT_SAMPLE", "N", "-", role1="reviewer", module=M6),
    R("SCORED", "USER", "1", "-", "USER_RISK_FLAG", "1", "=", module=M6),
    R("ACTS", "USER", "1", "-", "AUDIT_LOG", "N", "-", module=M6),
    R("RAISES_DR", "USER", "1", "-", "USER_DATA_REQUEST", "N", "=", module=M6),
    R("FEATURED", "USER", "1", "-", "SUCCESS_STORY", "N", "-", role1="student", module=M6),
]

MODULES = [M1, M2, M3, M4, M5, M6]
MODULE_NO = {M1: 1, M2: 2, M3: 3, M4: 4, M5: 5, M6: 6}

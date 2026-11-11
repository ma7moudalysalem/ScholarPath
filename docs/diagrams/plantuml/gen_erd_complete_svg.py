#!/usr/bin/env python3
"""Hand-routed, single-canvas COMPLETE ScholarPath EERD in authentic Chen /
Elmasri notation (Fundamentals of Database Systems, Fig. 3.14):
  rectangle = entity type        diamond = relationship type
  oval on a line = attribute     underlined text in oval = key attribute
  double-outline oval = multivalued attribute
  dashed-outline oval = derived / computed attribute
  composite attribute = an oval with its own sub-attribute ovals branching
    off it (e.g. Name -> FirstName/LastName), exactly per Fig. 3.14
  single line = PARTIAL participation   double line = TOTAL participation
  1 / N near the diamond = cardinality ratio on that side

ONE real USER entity and ONE real SCHOLARSHIP entity - every subject area
connects to them directly (no per-area stand-ins); there is no bounding box
around any subject area, so the model reads as one connected graph rather
than six separate islands. Subject-area labels are floating text only.

Why hand-routed instead of PlantUML @startchen: verified that PlantUML's
built-in Chen/EER layout engine (Smetana - no Graphviz `dot` binary installed
on this machine) cannot lay out ~51 entities in one diagram; it collapses
everything into a single unreadable row regardless of how the USER hub is
split. This script lays the whole model out on one large canvas we control -
size is not constrained; the canvas grows to whatever the content needs.

Source of truth: verified 2026-07-05 against server/src/ScholarPath.Domain
/Entities/*.cs + the EF Core migration snapshot (see eerd-chen-complete.puml
header comment for the full verification note and payment-exclusion scope).
"""
import os, math

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "img")

FS_TITLE = 15
FS_ATTR  = 11
FS_REL   = 11.5
FS_CARD  = 11
CHW_TITLE = 8.6
CHW_ATTR  = 6.5
PADX_T = 12
PADX_O = 10
TITLEH = 34
OVALH = 27
OVAL_VGAP = 6
OVAL_HGAP = 10
SUB_VGAP = 20     # gap between a composite's own oval and its sub-oval row
FAN_GAP = 34      # vertical gap between entity box and the top of its oval halo

def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")

def textw(s, chw):
    return len(s) * chw

# ---------------------------------------------------------------------------
def A(label, kind=None):
    return {"label": label, "kind": kind, "sub": None}

def C(label, subattrs):
    """Composite attribute: subattrs is a list of plain sub-label strings."""
    return {"label": label, "kind": "composite", "sub": [{"label": s, "kind": None} for s in subattrs]}

# =====================================================================
# ENTITIES (merged, single global registry - USER and SCHOLARSHIP each
# appear exactly once no matter how many subject areas reference them)
# =====================================================================
ENTITIES = {}
STANDALONE = set()

ENTITIES["USER"] = [A("Id","key"), A("Email"), C("Name", ["FirstName","LastName"]),
    A("AccountStatus"), A("ActiveRole"), A("IsOnboardingComplete"),
    A("PreferredLanguage"), A("CountryOfResidence"), A("LastLoginAt")]
ENTITIES["ROLE"] = [A("Id","key"), A("Name"), A("Description")]
ENTITIES["USER_PROFILE"] = [A("Id","key"), A("Biography"), A("DateOfBirth"), A("Nationality"),
    A("AcademicLevel"), A("FieldOfStudy"), A("Gpa"),
    A("PreferredCountries","multi"), A("PreferredFields","multi"),
    A("BookingAccessStatus"), A("BookingBlockReason"), A("BookingBlockUntil"),
    A("OrganizationLegalName"), A("OrganizationVerificationStatus"),
    A("ScholarshipProviderType"),
    A("ScholarshipProviderAverageRating","derived"), A("ScholarshipProviderReviewCount","derived"),
    A("ProfessionalTitle"), A("FieldOfExpertise"), A("YearsOfExperience"),
    A("ExpertiseTags","multi"), A("Languages","multi"), A("SessionDurationMinutes"),
    A("ConsultantAverageRating","derived"), A("ConsultantReviewCount","derived"),
    A("ConsultantRatingPenaltyFactor","derived"), A("ProfileCompletenessPercent","derived")]
ENTITIES["EDUCATION_ENTRY"] = [A("Id","key"), A("InstitutionName"), A("Degree"),
    A("FieldOfStudy"), A("StartDate"), A("EndDate"), A("Gpa")]
ENTITIES["EXPERTISE_TAG"] = [A("Id","key"), A("Slug"), C("Name", ["NameEn","NameAr"]), A("Category")]
STANDALONE.add("EXPERTISE_TAG")
ENTITIES["REFRESH_TOKEN"] = [A("Id","key"), A("TokenHash"), A("ExpiresAt"), A("IsRevoked"), A("RevokedAt")]
ENTITIES["PASSWORD_RESET_TOKEN"] = [A("Id","key"), A("TokenHash"), A("ExpiresAt"), A("UsedAt")]
ENTITIES["LOGIN_ATTEMPT"] = [A("Id","key"), A("Email"), A("Succeeded"), A("FailureReason"), A("OccurredAt")]
ENTITIES["UPGRADE_REQUEST"] = [A("Id","key"), A("Target"), A("Status"), A("Reason"), A("ReviewerNotes"), A("ReviewedAt")]
ENTITIES["UPGRADE_REQUEST_FILE"] = [A("Id","key"), A("FileName"), A("BlobUrl")]
ENTITIES["UPGRADE_REQUEST_LINK"] = [A("Id","key"), A("Label"), A("Url")]

ENTITIES["CATEGORY"] = [A("Id","key"), A("Slug"), C("Name", ["NameEn","NameAr"]), A("DisplayOrder")]
ENTITIES["SCHOLARSHIP"] = [A("Id","key"), A("Slug"), C("Title", ["TitleEn","TitleAr"]), A("Mode"),
    A("Status"), A("Deadline"), A("FundingType"), A("FundingAmountUsd"), A("TargetLevel"), A("IsFeatured")]
ENTITIES["SCHOLARSHIP_CHILD"] = [A("Id","key"), A("ChildType"), A("KeyEn"), A("ValueEn"), A("SortOrder")]
ENTITIES["SAVED_SCHOLARSHIP"] = [A("Id","key"), A("SavedAt"), A("Note")]
ENTITIES["APPLICATION"] = [A("Id","key"), A("Status"), A("Mode"), A("SubmittedAt"),
    A("DecisionAt"), A("PersonalNotes"), A("ExternalTitle"), A("ExternalProvider")]
ENTITIES["APPLICATION_CHILD"] = [A("Id","key"), A("ChildType"), A("Title"), A("Content"), A("OccurredAt")]
ENTITIES["DOCUMENT"] = [A("Id","key"), A("FileName"), A("Category"), A("StoragePath"), A("UploadedAt")]
ENTITIES["SCHOLARSHIP_PROVIDER_REVIEW"] = [A("Id","key"), A("Rating"), A("Comment"), A("IsHiddenByAdmin")]
ENTITIES["SCHOLARSHIP_PROVIDER_REVIEW_REQUEST"] = [A("Id","key"), A("Status"), A("SubmittedAt"), A("CompletedAt")]

ENTITIES["AVAILABILITY"] = [A("Id","key"), A("DayOfWeek"), C("TimeWindow", ["StartTime","EndTime"]),
    A("Timezone"), A("IsRecurring"), A("IsActive")]
ENTITIES["BOOKING"] = [A("Id","key"), C("ScheduledPeriod", ["ScheduledStartAt","ScheduledEndAt"]),
    A("DurationMinutes"), A("MeetingRoomId"), A("Status"), A("CancellationReason"),
    A("IsNoShowStudent"), A("IsNoShowConsultant")]
ENTITIES["NO_SHOW_REPORT"] = [A("Id","key"), A("AccusedRole"), A("Status"), A("ResolvedAt"), A("AdminNote"), A("ReporterNote")]
ENTITIES["CONSULTANT_REVIEW"] = [A("Id","key"), A("Rating"), A("Comment"), A("IsHiddenByAdmin")]
ENTITIES["SESSION_RECORDING"] = [A("Id","key"), A("RecordingId"), A("StoragePath"), A("ContentType"), A("RecordedAt")]

ENTITIES["FORUM_CATEGORY"] = [A("Id","key"), A("Slug"), C("Name", ["NameEn","NameAr"]), A("DisplayOrder"), A("IsActive")]
ENTITIES["FORUM_POST"] = [A("Id","key"), A("BodyMarkdown"), A("ModerationStatus"),
    A("UpvoteCount","derived"), A("DownvoteCount","derived"), A("FlagCount","derived"),
    A("ReplyCount","derived"), A("IsAutoHidden")]
ENTITIES["FORUM_POST_ATTACHMENT"] = [A("Id","key"), A("FileName"), A("BlobUrl"), A("ContentType")]
ENTITIES["FORUM_VOTE"] = [A("Id","key"), A("VoteType"), A("VotedAt")]
ENTITIES["FORUM_FLAG"] = [A("Id","key"), A("Reason"), A("FlaggedAt"), A("IsValid")]
ENTITIES["FORUM_BOOKMARK"] = [A("Id","key"), A("CreatedAt")]
ENTITIES["FORUM_TAG"] = [A("Id","key"), A("Slug"), A("Name")]
ENTITIES["CONVERSATION"] = [A("Id","key"), A("LastMessageAt"), A("IsArchivedForParticipantOne"), A("IsArchivedForParticipantTwo")]
ENTITIES["MESSAGE"] = [A("Id","key"), A("Body"), A("SentAt"), A("ReadAt")]
ENTITIES["USER_BLOCK"] = [A("Id","key"), A("BlockedAt"), A("Reason")]

ENTITIES["RESOURCE"] = [A("Id","key"), A("Slug"), C("Title", ["TitleEn","TitleAr"]), A("Type"),
    A("Status"), A("AuthorRole"), A("IsFeatured"), A("PublishedAt")]
ENTITIES["RESOURCE_CHAPTER"] = [A("Id","key"), A("TitleEn"), A("SortOrder"), A("EstimatedReadMinutes")]
ENTITIES["RESOURCE_BOOKMARK"] = [A("Id","key"), A("BookmarkedAt")]
ENTITIES["RESOURCE_PROGRESS"] = [A("Id","key"), A("ChaptersCompletedCount","derived"), A("LastAccessedAt")]
ENTITIES["RESOURCE_PROGRESS_CHILD"] = [A("Id","key"), A("IsCompleted"), A("CompletedAt")]
ENTITIES["NOTIFICATION"] = [A("Id","key"), A("Type"), A("Channel"), A("IsRead"), A("Priority"), A("DispatchedAt")]
ENTITIES["NOTIFICATION_PREFERENCE"] = [A("Id","key"), A("Type"), A("Channel"), A("IsEnabled")]

ENTITIES["AI_INTERACTION"] = [A("Id","key"), A("Feature"), A("Provider"), A("SessionId"),
    A("PromptTokens"), A("CompletionTokens"), A("CostUsd","derived")]
ENTITIES["RECOMMENDATION_CLICK_EVENT"] = [A("Id","key"), A("ClickedAt"), A("Source")]
ENTITIES["AI_REDACTION_AUDIT_SAMPLE"] = [A("Id","key"), A("SampledAt"), A("Verdict"), A("ReviewedAt")]
ENTITIES["KNOWLEDGE_DOCUMENT"] = [A("Id","key"), A("SourceType"), A("SourceKey"), A("TitleEn"), A("ContentHash"), A("EmbeddingDimensions")]
STANDALONE.add("KNOWLEDGE_DOCUMENT")
ENTITIES["PLATFORM_SETTING"] = [A("Id","key"), A("SettingKey"), A("Value"), A("ValueType"), A("Category")]
STANDALONE.add("PLATFORM_SETTING")
ENTITIES["SUCCESS_STORY"] = [A("Id","key"), A("AuthorDisplayName"), A("HeadlineEn"), A("IsApproved"), A("IsFeatured")]
ENTITIES["USER_RISK_FLAG"] = [A("Id","key"), A("Score","derived"), A("IsAtRisk"), A("Reason"), A("ComputedAt")]
ENTITIES["AUDIT_LOG"] = [A("Id","key"), A("Action"), A("TargetType"), A("OccurredAt"), A("Summary")]
ENTITIES["USER_DATA_REQUEST"] = [A("Id","key"), A("Type"), A("Status"), A("RequestedAt"), A("CompletedAt")]

# =====================================================================
# LOCAL LAYOUTS per subject area (grid units), each with its own small
# local relationship list. USER / SCHOLARSHIP are referenced by name only
# where an area needs them - they are placed once, in AREA 1 / AREA 2.
# =====================================================================
AREAS = {}

AREAS["A1"] = {
    "label": "Identity, Access & Profile",
    "offset": (0, 0),
    "pos": {
        "ROLE": (0, 1), "UPGRADE_REQUEST": (1, 1), "USER": (2, 0), "USER_PROFILE": (2.45, 2),
        "REFRESH_TOKEN": (3, 1), "PASSWORD_RESET_TOKEN": (4, 1), "LOGIN_ATTEMPT": (5, 1),
        "EDUCATION_ENTRY": (2.45, 4),
        "UPGRADE_REQUEST_FILE": (0.5, 3), "UPGRADE_REQUEST_LINK": (1.5, 3),
        "EXPERTISE_TAG": (5, 0),
    },
    "rels": [
        ("HAS_PROFILE", "USER", "1", "-", "USER_PROFILE", "1", "="),
        ("HAS_ROLE", "USER", "N", "-", "ROLE", "N", "-"),
        ("HAS_EDU", "USER_PROFILE", "1", "-", "EDUCATION_ENTRY", "N", "="),
        ("ISSUES", "USER", "1", "-", "REFRESH_TOKEN", "N", "="),
        ("RESETS", "USER", "1", "-", "PASSWORD_RESET_TOKEN", "N", "="),
        ("ATTEMPTS", "USER", "1", "-", "LOGIN_ATTEMPT", "N", "-"),
        ("REQUESTS_UPG", "USER", "1", "-", "UPGRADE_REQUEST", "N", "="),
        ("HAS_FILE", "UPGRADE_REQUEST", "1", "-", "UPGRADE_REQUEST_FILE", "N", "="),
        ("HAS_LINK", "UPGRADE_REQUEST", "1", "-", "UPGRADE_REQUEST_LINK", "N", "="),
    ],
}

AREAS["A2"] = {
    "label": "Scholarships, Applications & Documents",
    "offset": (3900, -420),
    "pos": {
        "CATEGORY": (1, 0), "SCHOLARSHIP": (2.6, 0), "SCHOLARSHIP_CHILD": (4.4, 0),
        "SAVED_SCHOLARSHIP": (0.8, 2), "APPLICATION": (2.6, 2),
        "SCHOLARSHIP_PROVIDER_REVIEW_REQUEST": (4.4, 2),
        "APPLICATION_CHILD": (1.6, 4), "DOCUMENT": (2.9, 4), "SCHOLARSHIP_PROVIDER_REVIEW": (4.4, 4),
    },
    "rels": [
        ("CLASSIFIES", "CATEGORY", "1", "-", "SCHOLARSHIP", "N", "-"),
        ("OWNS", "USER", "1", "-", "SCHOLARSHIP", "N", "-"),
        ("HAS_DETAIL", "SCHOLARSHIP", "1", "-", "SCHOLARSHIP_CHILD", "N", "="),
        ("SAVES", "USER", "1", "-", "SAVED_SCHOLARSHIP", "N", "-"),
        ("SAVES_SCH", "SCHOLARSHIP", "1", "-", "SAVED_SCHOLARSHIP", "N", "="),
        ("SUBMITS", "USER", "1", "-", "APPLICATION", "N", "="),
        ("FOR_SCH", "APPLICATION", "N", "-", "SCHOLARSHIP", "1", "-"),
        ("HAS_HISTORY", "APPLICATION", "1", "-", "APPLICATION_CHILD", "N", "="),
        ("OWNS_DOC", "USER", "1", "-", "DOCUMENT", "N", "="),
        ("SUPPORTS", "APPLICATION", "1", "-", "DOCUMENT", "N", "-"),
        ("RAISES/FULFILS", "USER", "1", "-", "SCHOLARSHIP_PROVIDER_REVIEW_REQUEST", "N", "="),
        ("UNDER", "SCHOLARSHIP", "1", "-", "SCHOLARSHIP_PROVIDER_REVIEW_REQUEST", "N", "="),
        ("RATED_BY", "APPLICATION", "1", "-", "SCHOLARSHIP_PROVIDER_REVIEW", "1", "="),
        ("AUTHORS/RECEIVES_REV", "USER", "1", "-", "SCHOLARSHIP_PROVIDER_REVIEW", "N", "="),
    ],
}

AREAS["A3"] = {
    "label": "Consultant Booking & Ratings",
    "offset": (8100, 1350),
    "pos": {
        "AVAILABILITY": (1.4, 0), "BOOKING": (3.2, 0),
        "NO_SHOW_REPORT": (2, 2.2), "CONSULTANT_REVIEW": (3.6, 2.2), "SESSION_RECORDING": (5.2, 2.2),
    },
    "rels": [
        ("PUBLISHES", "USER", "1", "-", "AVAILABILITY", "N", "="),
        ("BOOKS/CONDUCTS", "USER", "1", "-", "BOOKING", "N", "="),
        ("RESERVES", "AVAILABILITY", "1", "-", "BOOKING", "N", "-"),
        ("REPORTED_IN", "BOOKING", "1", "-", "NO_SHOW_REPORT", "N", "="),
        ("FILES/ACCUSED_IN", "USER", "1", "-", "NO_SHOW_REPORT", "N", "="),
        ("RATED_SESSION", "BOOKING", "1", "-", "CONSULTANT_REVIEW", "1", "="),
        ("REVIEWS/REVIEWED", "USER", "1", "-", "CONSULTANT_REVIEW", "N", "="),
        ("RECORDED_AS", "BOOKING", "1", "-", "SESSION_RECORDING", "N", "="),
    ],
}

AREAS["A4"] = {
    "label": "Community & Chat",
    "offset": (300, 2650),
    "pos": {
        "FORUM_CATEGORY": (1.4, 0), "FORUM_POST": (3.2, 0), "FORUM_TAG": (5.2, 0),
        "FORUM_POST_ATTACHMENT": (2.2, 2.2), "FORUM_VOTE": (3.5, 2.2), "FORUM_FLAG": (4.6, 2.2), "FORUM_BOOKMARK": (5.9, 2.2),
        "CONVERSATION": (0.6, 4.2), "MESSAGE": (2, 4.2), "USER_BLOCK": (-1.2, 2.2),
    },
    "rels": [
        ("GROUPS", "FORUM_CATEGORY", "1", "-", "FORUM_POST", "N", "-"),
        ("AUTHORS_POST", "USER", "1", "-", "FORUM_POST", "N", "="),
        ("REPLIES(self)", "FORUM_POST", "1", "-", "FORUM_POST", "N", "-"),
        ("ATTACHES", "FORUM_POST", "1", "-", "FORUM_POST_ATTACHMENT", "N", "="),
        ("RECEIVES_VOTE", "FORUM_POST", "1", "-", "FORUM_VOTE", "N", "="),
        ("RECEIVES_FLAG", "FORUM_POST", "1", "-", "FORUM_FLAG", "N", "="),
        ("SAVED_IN", "FORUM_POST", "1", "-", "FORUM_BOOKMARK", "N", "="),
        ("TAGS(M:N)", "FORUM_POST", "N", "-", "FORUM_TAG", "N", "-"),
        ("PARTICIPATES(M:N)", "USER", "N", "-", "CONVERSATION", "N", "-"),
        ("CONTAINS", "CONVERSATION", "1", "-", "MESSAGE", "N", "="),
        ("BLOCKS/TARGETED_BY", "USER", "1", "-", "USER_BLOCK", "N", "-"),
    ],
}

AREAS["A5"] = {
    "label": "Resources Hub & Notifications",
    "offset": (4200, 2650),
    "pos": {
        "RESOURCE": (1.4, 0), "NOTIFICATION": (3.6, 0), "NOTIFICATION_PREFERENCE": (5.2, 0),
        "RESOURCE_CHAPTER": (0.2, 2.2), "RESOURCE_BOOKMARK": (1.6, 2.2), "RESOURCE_PROGRESS": (3, 2.2),
        "RESOURCE_PROGRESS_CHILD": (3, 4.2),
    },
    "rels": [
        ("AUTHORS_RES", "USER", "1", "-", "RESOURCE", "N", "="),
        ("SPLIT_INTO", "RESOURCE", "1", "-", "RESOURCE_CHAPTER", "N", "="),
        ("SAVED_IN", "RESOURCE", "1", "-", "RESOURCE_BOOKMARK", "N", "="),
        ("TRACKED_IN", "RESOURCE", "1", "-", "RESOURCE_PROGRESS", "N", "="),
        ("PER_CHAPTER", "RESOURCE_PROGRESS", "1", "-", "RESOURCE_PROGRESS_CHILD", "N", "="),
        ("RECEIVES", "USER", "1", "-", "NOTIFICATION", "N", "-"),
        ("CONFIGURES", "USER", "1", "-", "NOTIFICATION_PREFERENCE", "N", "-"),
    ],
}

AREAS["A6"] = {
    "label": "AI, Knowledge Base & Platform",
    "offset": (8000, -1100),
    "pos": {
        "AI_INTERACTION": (1.4, 0), "RECOMMENDATION_CLICK_EVENT": (3.4, 0),
        "AI_REDACTION_AUDIT_SAMPLE": (1.4, 2.4), "USER_RISK_FLAG": (-1.2, 2.4),
        "AUDIT_LOG": (0, 4.4), "USER_DATA_REQUEST": (1.2, 4.4), "SUCCESS_STORY": (-1.2, 4.4),
        "KNOWLEDGE_DOCUMENT": (3.6, 2.4), "PLATFORM_SETTING": (5, 2.4),
    },
    "rels": [
        ("INVOKES", "USER", "1", "-", "AI_INTERACTION", "N", "-"),
        ("CLICKS", "USER", "1", "-", "RECOMMENDATION_CLICK_EVENT", "N", "="),
        ("CLICKED", "SCHOLARSHIP", "1", "-", "RECOMMENDATION_CLICK_EVENT", "N", "="),
        ("FROM_AI", "AI_INTERACTION", "1", "-", "RECOMMENDATION_CLICK_EVENT", "N", "-"),
        ("SAMPLED", "AI_INTERACTION", "1", "-", "AI_REDACTION_AUDIT_SAMPLE", "1", "="),
        ("SUBJECT/REVIEWS", "USER", "1", "-", "AI_REDACTION_AUDIT_SAMPLE", "N", "="),
        ("SCORED", "USER", "1", "-", "USER_RISK_FLAG", "1", "="),
        ("ACTOR_OF", "USER", "1", "-", "AUDIT_LOG", "N", "-"),
        ("RAISES", "USER", "1", "-", "USER_DATA_REQUEST", "N", "-"),
        ("STARS_IN", "USER", "1", "-", "SUCCESS_STORY", "N", "-"),
    ],
}

AREA_ORDER = ["A1", "A2", "A3", "A4", "A5", "A6"]

# --------------------------------------------------------------------------
def oval_size(label, kind):
    w = max(56, textw(label, CHW_ATTR) + 2*PADX_O + (10 if kind else 0))
    return w, OVALH

def layout_halo(attrs, box_w):
    """Return (ovals, subovals, halo_w, halo_h).
    ovals: top-level attribute ovals (dict rx,ry,w,h,label,kind).
    subovals: for composite attrs, their children (dict rx,ry,w,h,label,kind,
    parent_rx,parent_ry,parent_w -> used to draw the branch line)."""
    if not attrs:
        return [], [], 0, 0
    target_row_w = max(box_w * 1.7, 380)
    ovals = []
    x = 0; y = 0; row_h = 0; max_w = 0
    for a in attrs:
        label, kind = a["label"], a["kind"]
        w, h = oval_size(label, kind if kind != "composite" else None)
        if x > 0 and x + w > target_row_w:
            max_w = max(max_w, x - OVAL_HGAP)
            y += row_h + OVAL_VGAP
            x = 0; row_h = 0
        ovals.append({"rx": x, "ry": y, "w": w, "h": h, "label": label, "kind": kind, "sub": a["sub"]})
        x += w + OVAL_HGAP
        row_h = max(row_h, h)
    max_w = max(max_w, x - OVAL_HGAP)
    halo_h = y + row_h
    max_w = max(max_w, box_w)

    # composites get a sub-row of their own children directly beneath the
    # simple-attribute grid, positioned under their own parent oval x.
    composites = [o for o in ovals if o["kind"] == "composite"]
    subovals = []
    if composites:
        sub_y0 = halo_h + SUB_VGAP
        max_sub_bottom = halo_h
        for comp in composites:
            subs = comp["sub"]
            total_sub_w = sum(oval_size(s["label"], None)[0] for s in subs) + OVAL_HGAP*(len(subs)-1)
            parent_cx = comp["rx"] + comp["w"]/2.0
            sx = parent_cx - total_sub_w/2.0
            for s in subs:
                sw, sh = oval_size(s["label"], None)
                subovals.append({"rx": sx, "ry": sub_y0, "w": sw, "h": sh, "label": s["label"], "kind": None,
                                  "parent_rx": parent_cx, "parent_ry": comp["ry"]+comp["h"]})
                sx += sw + OVAL_HGAP
            max_sub_bottom = max(max_sub_bottom, sub_y0 + OVALH)
            max_w = max(max_w, sx)
        halo_h = max_sub_bottom
    return ovals, subovals, max_w, halo_h

def draw_oval(svg, ocx, ocy, rx, ry, kind, label):
    if kind == "multi":
        svg.append('<ellipse cx="%.1f" cy="%.1f" rx="%.1f" ry="%.1f" fill="white" stroke="black" stroke-width="1.1"/>' % (ocx, ocy, rx, ry))
        svg.append('<ellipse cx="%.1f" cy="%.1f" rx="%.1f" ry="%.1f" fill="none" stroke="black" stroke-width="1.1"/>' % (ocx, ocy, rx-3, ry-3))
    elif kind == "derived":
        svg.append('<ellipse cx="%.1f" cy="%.1f" rx="%.1f" ry="%.1f" fill="white" stroke="black" stroke-width="1.1" stroke-dasharray="4,3"/>' % (ocx, ocy, rx, ry))
    else:
        svg.append('<ellipse cx="%.1f" cy="%.1f" rx="%.1f" ry="%.1f" fill="white" stroke="black" stroke-width="1.1"/>' % (ocx, ocy, rx, ry))
    deco = ' text-decoration="underline"' if kind == "key" else ""
    svg.append('<text x="%.1f" y="%.1f" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-size="%d"%s>%s</text>'
               % (ocx, ocy + 4, FS_ATTR, deco, esc(label)))

def render_entity(svg, fx, fy, name, attrs, standalone=False):
    """fx,fy = top-left of the entity's FULL FOOTPRINT (title box + halo).
    Returns dict describing geometry (box + footprint)."""
    title_w = max(150, textw(name, CHW_TITLE) + 2*PADX_T)
    ovals, subovals, halo_w, halo_h = layout_halo(attrs, title_w)
    foot_w = max(title_w, halo_w)
    box_x = fx + (foot_w - title_w) / 2.0
    box_y = fy
    fill = "#e8eefc" if not standalone else "#f5ecd9"
    svg.append('<rect x="%.1f" y="%.1f" width="%.1f" height="%.1f" fill="%s" stroke="black" stroke-width="1.5" rx="3"/>'
               % (box_x, box_y, title_w, TITLEH, fill))
    svg.append('<text x="%.1f" y="%.1f" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-weight="bold" font-size="%d">%s</text>'
               % (box_x + title_w/2, box_y + TITLEH - 11, FS_TITLE, esc(name)))
    if standalone:
        svg.append('<text x="%.1f" y="%.1f" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-style="italic" font-size="10" fill="#777">(standalone — no FK relationships)</text>'
                   % (box_x + title_w/2, box_y + TITLEH + 12))

    halo_x0 = fx + (foot_w - halo_w) / 2.0 if halo_w > 0 else fx
    halo_y0 = box_y + TITLEH + FAN_GAP
    lines = []
    for ov in ovals:
        ocx = halo_x0 + ov["rx"] + ov["w"]/2.0
        ocy = halo_y0 + ov["ry"] + ov["h"]/2.0
        frac = 0.5 if halo_w <= 1 else (ov["rx"] + ov["w"]/2.0) / halo_w
        ax = box_x + frac * title_w
        ay = box_y + TITLEH
        lines.append('<line x1="%.1f" y1="%.1f" x2="%.1f" y2="%.1f" stroke="#555" stroke-width="0.9"/>' % (ax, ay, ocx, ocy - ov["h"]/2.0))
        draw_oval(lines, ocx, ocy, ov["w"]/2.0, ov["h"]/2.0, ov["kind"], ov["label"])
    for so in subovals:
        pcx = halo_x0 + so["parent_rx"]
        pcy = halo_y0 + so["parent_ry"]
        ocx = halo_x0 + so["rx"] + so["w"]/2.0
        ocy = halo_y0 + so["ry"] + so["h"]/2.0
        lines.append('<line x1="%.1f" y1="%.1f" x2="%.1f" y2="%.1f" stroke="#555" stroke-width="0.9"/>' % (pcx, pcy, ocx, ocy - so["h"]/2.0))
        draw_oval(lines, ocx, ocy, so["w"]/2.0, so["h"]/2.0, so["kind"], so["label"])
    svg += lines
    total_h = TITLEH + (FAN_GAP + halo_h if ovals else 0) + (16 if standalone else 0)
    return {"box_x": box_x, "box_y": box_y, "box_w": title_w, "box_h": TITLEH,
            "fx": fx, "fy": fy, "fw": foot_w, "fh": total_h}

def diamond_points(cx, cy, hw, hh):
    return "%.1f,%.1f %.1f,%.1f %.1f,%.1f %.1f,%.1f" % (cx, cy-hh, cx+hw, cy, cx, cy+hh, cx-hw, cy)

def layout_area_local(area):
    """Compute LOCAL (unoffset) pixel geometry for one area's entities.
    Returns (svg_list, boxes) where boxes maps entity name -> local geo."""
    pos = area["pos"]
    names = list(pos.keys())
    sizes = {}
    for name in names:
        title_w = max(150, textw(name, CHW_TITLE) + 2*PADX_T)
        _, _, halo_w, halo_h = layout_halo(ENTITIES[name], title_w)
        fw = max(title_w, halo_w)
        fh = TITLEH + (FAN_GAP + halo_h if ENTITIES[name] else 0) + (16 if name in STANDALONE else 0)
        sizes[name] = (fw, fh)

    cols = sorted(set(pos[n][0] for n in names))
    rows = sorted(set(pos[n][1] for n in names))
    col_w = {c: max(sizes[n][0] for n in names if pos[n][0] == c) for c in cols}
    row_h = {r: max(sizes[n][1] for n in names if pos[n][1] == r) for r in rows}

    COL_GAP = 110
    ROW_GAP = 90
    col_x = {}
    x = 0
    for c in cols:
        col_x[c] = x
        x += col_w[c] + COL_GAP
    row_y = {}
    y = 0
    for r in rows:
        row_y[r] = y
        y += row_h[r] + ROW_GAP

    svg = []
    boxes = {}
    for name in names:
        c, r = pos[name]
        fw, fh = sizes[name]
        fx = col_x[c] + (col_w[c] - fw) / 2.0
        fy = row_y[r]
        geo = render_entity(svg, fx, fy, name, ENTITIES[name], standalone=(name in STANDALONE))
        boxes[name] = geo
    return svg, boxes

def render_all():
    all_svg = []
    all_boxes = {}     # entity name -> GLOBAL geo (USER/SCHOLARSHIP end up here once)
    labels = []

    for ak in AREA_ORDER:
        area = AREAS[ak]
        ox, oy = area["offset"]
        svg, boxes = layout_area_local(area)
        # translate this area's fresh entities into global space; if an
        # entity (USER / SCHOLARSHIP) already exists globally, DO NOT
        # re-place or re-draw it - just reuse its existing global geo so it
        # remains the single real entity every area connects into.
        min_x = min(b["fx"] for b in boxes.values())
        min_y = min(b["fy"] for b in boxes.values())
        for name, b in boxes.items():
            if name in all_boxes:
                continue
            gx = ox + (b["fx"] - min_x)
            gy = oy + (b["fy"] - min_y)
            dx, dy = gx - b["fx"], gy - b["fy"]
            all_boxes[name] = {"box_x": b["box_x"]+dx, "box_y": b["box_y"]+dy,
                                "box_w": b["box_w"], "box_h": b["box_h"],
                                "fx": gx, "fy": gy, "fw": b["fw"], "fh": b["fh"]}
        # emit this area's own entity SVG translated by its offset (each area's
        # "pos" dict only contains entities that live there - USER/SCHOLARSHIP
        # are only in A1/A2's pos dicts, so nothing is ever drawn twice)
        all_svg.append('<g transform="translate(%.1f,%.1f)">' % (ox - min_x, oy - min_y))
        all_svg += svg
        all_svg.append('</g>')
        cx = sum(b["fx"] for b in boxes.values())/len(boxes) - min_x + ox
        top_y = min(b["fy"] for b in boxes.values()) - min_y + oy
        labels.append((cx, top_y - 26, area["label"]))

    # merge all relationships (real entity names only - no stand-ins anywhere)
    all_rels = []
    for ak in AREA_ORDER:
        all_rels += AREAS[ak]["rels"]

    # ---- BUNDLE long-haul relationships that all leave the same hub (USER /
    # SCHOLARSHIP) heading toward the same distant area. Routing each one as
    # its own independent straight line makes them bunch into a single knot
    # right at the hub and cross each other unpredictably. Instead, give each
    # relationship in the bundle its own perpendicular "lane" offset along the
    # hub->area trunk direction, so the whole group reads as an organised
    # parallel sheaf (like ribbon cable) rather than a tangle. Relationships
    # where both ends live in the same area are unaffected (rendered as a
    # single direct line, same as before).
    entity_area = {}
    for ak in AREA_ORDER:
        for name in AREAS[ak]["pos"]:
            entity_area[name] = ak
    HUBS = {"USER", "SCHOLARSHIP"}

    bundle_key_of = {}   # id(rel) -> (hub, area_key)
    bundle_members = {}  # (hub, area_key) -> [rel, ...]
    for rel in all_rels:
        rname, e1, c1, cp1, e2, c2, cp2 = rel
        hub = other = None
        if e1 in HUBS and entity_area.get(e2) != entity_area.get(e1):
            hub, other = e1, e2
        elif e2 in HUBS and entity_area.get(e1) != entity_area.get(e2):
            hub, other = e2, e1
        if hub:
            key = (hub, entity_area[other])
            bundle_key_of[id(rel)] = key
            bundle_members.setdefault(key, []).append(rel)

    LANE_GAP = 36
    rel_svg = []
    placed_diamonds = []
    for rel in all_rels:
        rname, e1, c1, p1, e2, c2, p2 = rel
        b1, b2 = all_boxes[e1], all_boxes[e2]
        cx1, cy1 = b1["box_x"] + b1["box_w"]/2, b1["box_y"] + b1["box_h"]/2
        cx2, cy2 = b2["box_x"] + b2["box_w"]/2, b2["box_y"] + b2["box_h"]/2

        def clip(bx, cx, cy, tx, ty):
            halo_bottom = bx["fy"] + bx["fh"]
            box_bottom = bx["box_y"] + bx["box_h"]
            hw = bx["box_w"]/2 + 3
            if ty >= box_bottom and halo_bottom > box_bottom + 1:
                hh_down = halo_bottom - cy
            else:
                hh_down = bx["box_h"]/2 + 3
            hh_up = bx["box_h"]/2 + 3
            dx, dy = tx-cx, ty-cy
            if dx == 0 and dy == 0: return cx, cy
            sx = hw/abs(dx) if dx != 0 else 1e9
            hh = hh_down if dy > 0 else hh_up
            sy = hh/abs(dy) if dy != 0 else 1e9
            s = min(sx, sy)
            return cx+dx*s, cy+dy*s

        bkey = bundle_key_of.get(id(rel))
        if bkey:
            members = bundle_members[bkey]
            i = members.index(rel)
            n = len(members)
            tdx, tdy = cx2-cx1, cy2-cy1
            tlen = max(1e-6, math.hypot(tdx, tdy))
            ux, uy = tdx/tlen, tdy/tlen
            perp_x, perp_y = -uy, ux
            offset = (i - (n-1)/2.0) * LANE_GAP
            frac = 0.5 + ((i % 3) - 1) * 0.05   # stagger along the trunk too, spreads diamonds apart
            base_x = cx1 + tdx*frac
            base_y = cy1 + tdy*frac
            mx = base_x + perp_x*offset
            my = base_y + perp_y*offset
            p1x, p1y = clip(b1, cx1, cy1, mx, my)
            p2x, p2y = clip(b2, cx2, cy2, mx, my)
        else:
            p1x, p1y = clip(b1, cx1, cy1, cx2, cy2)
            p2x, p2y = clip(b2, cx2, cy2, cx1, cy1)
            mx, my = (p1x+p2x)/2, (p1y+p2y)/2

        dw = max(46, textw(rname, 6.6) + 30)
        dh = 28

        lx, ly = mx - p1x, my - p1y
        llen = max(1e-6, math.hypot(lx, ly))
        ux, uy = lx/llen, ly/llen
        step = 0
        while True:
            tx_, ty_ = mx + ux*step*30, my + uy*step*30
            clash = any(abs(tx_-ox2) < (dw+odw)/2*0.82 and abs(ty_-oy2) < (dh+odh)/2*0.82
                        for (ox2, oy2, odw, odh) in placed_diamonds)
            if not clash or step > 10:
                mx, my = tx_, ty_
                break
            step += 1
        placed_diamonds.append((mx, my, dw, dh))

        def draw_line(x1, y1, x2, y2, total):
            if total == "=":
                ang = math.atan2(y2-y1, x2-x1) + math.pi/2
                off = 2.8
                oxx, oyy = off*math.cos(ang), off*math.sin(ang)
                rel_svg.append('<line x1="%.1f" y1="%.1f" x2="%.1f" y2="%.1f" stroke="black" stroke-width="1.1"/>' % (x1+oxx, y1+oyy, x2+oxx, y2+oyy))
                rel_svg.append('<line x1="%.1f" y1="%.1f" x2="%.1f" y2="%.1f" stroke="black" stroke-width="1.1"/>' % (x1-oxx, y1-oyy, x2-oxx, y2-oyy))
            else:
                rel_svg.append('<line x1="%.1f" y1="%.1f" x2="%.1f" y2="%.1f" stroke="black" stroke-width="1.1"/>' % (x1, y1, x2, y2))

        draw_line(p1x, p1y, mx, my, p1)
        draw_line(mx, my, p2x, p2y, p2)
        rel_svg.append('<polygon points="%s" fill="#fff7e0" stroke="black" stroke-width="1.3"/>' % diamond_points(mx, my, dw/2, dh/2))
        rel_svg.append('<text x="%.1f" y="%.1f" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-size="%d">%s</text>' % (mx, my+4, FS_REL, esc(rname)))

        def label_near(px, py, cx, cy, txt):
            fxx = px + (cx-px)*0.24
            fyy = py + (cy-py)*0.24
            rel_svg.append('<rect x="%.1f" y="%.1f" width="15" height="15" fill="white"/>' % (fxx-7.5, fyy-11))
            rel_svg.append('<text x="%.1f" y="%.1f" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-size="%d" font-weight="bold">%s</text>' % (fxx, fyy, FS_CARD, txt))
        label_near(p1x, p1y, mx, my, c1)
        label_near(p2x, p2y, mx, my, c2)

    label_svg = []
    for (lx, ly, text) in labels:
        label_svg.append('<text x="%.1f" y="%.1f" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-weight="bold" font-style="italic" font-size="17" fill="#334">%s</text>' % (lx, ly, esc(text)))

    # normalise: areas can carry negative offsets (e.g. a cluster placed
    # "above" the home area), so shift the WHOLE diagram body by whatever is
    # needed to bring every coordinate back into positive canvas space -
    # only the body shifts; header/legend are placed after, in final coords.
    min_x = min(b["fx"] for b in all_boxes.values())
    min_y = min(b["fy"] for b in all_boxes.values())
    shift_x = -min_x + 60
    shift_y = -min_y + 110

    canvas_w = max(b["fx"]+b["fw"] for b in all_boxes.values()) + shift_x + 60
    canvas_h = max(b["fy"]+b["fh"] for b in all_boxes.values()) + shift_y + 60 + 330

    header = ['<text x="40" y="50" font-family="Segoe UI,Arial,sans-serif" font-weight="bold" font-size="34">ScholarPath — Complete Entity-Relationship Diagram (Chen / Elmasri notation)</text>']

    legend_y = canvas_h - 300
    legend = []
    legend.append('<rect x="40" y="%.1f" width="%.1f" height="260" fill="#fffdf4" stroke="#999" stroke-width="1.3" rx="6"/>' % (legend_y, canvas_w - 80))
    lx = 60; ly = legend_y + 28
    legend.append('<text x="%.1f" y="%.1f" font-family="Segoe UI,Arial,sans-serif" font-weight="bold" font-size="17">Legend &amp; notation (Elmasri &amp; Navathe, Fig. 3.14)</text>' % (lx, ly))
    lines = [
        "Rectangle = entity type.   Diamond = relationship type.   Oval on a line = attribute of the entity it is connected to.   1 / N on an edge = cardinality ratio on that side.",
        "Single line ( — ) = PARTIAL participation.   Double line ( = ) = TOTAL participation (every instance of that entity must appear in the relationship).",
        "Underlined text inside an oval = key attribute.   Double-outline oval = multivalued attribute.   Dashed-outline oval = derived / computed attribute.",
        "Composite attribute = an oval with its own sub-attribute ovals branching off it, e.g. USER.Name -> {FirstName, LastName}; every bilingual En/Ar field pair",
        "  (Category/ForumCategory.Name, Scholarship/Resource.Title, ExpertiseTag.Name) and Booking.ScheduledPeriod / Availability.TimeWindow are modelled the same way.",
        "USER and SCHOLARSHIP each appear exactly ONCE on this canvas (in the Identity / Scholarships areas respectively) — every other subject area's relationships reach",
        "  those two real entities directly across the canvas; nothing here is a stand-in copy. Subject-area names are floating labels only, not boxes — the model is one graph.",
        "Foreign keys are conveyed by the relationship diamonds, not drawn as separate attribute ovals (authentic Chen practice). Every entity also carries CreatedAt / UpdatedAt /",
        "  RowVersion, and (where soft-deletable) IsDeleted / DeletedAt / DeletedByUserId — omitted from every box for legibility, since they are common to nearly all tables.",
        "PAYMENTS ARE INTENTIONALLY OUT OF SCOPE: Payment, Payout, StripeWebhookEvent, ProfitShareConfig, FinancialConfigRule and the legacy ScholarshipProviderReviewPayment,",
        "  plus every FK/attribute that exists only to price or settle them (Booking.PriceUsd/PaymentId/StripePaymentIntentId, UserProfile.SessionFeeUsd/StripeConnect*,",
        "  Scholarship.ReviewFeeUsd, ScholarshipProviderReviewRequest.ReviewFeeUsdSnapshot/PaymentId) are deliberately excluded — payments are hidden platform-wide while the",
        "  product runs in free mode. Scholarship.FundingAmountUsd is KEPT: it is the external award value the scholarship itself grants, not a figure ScholarPath processes.",
        "Verified 2026-07-05 directly against server/src/ScholarPath.Domain/Entities/*.cs and the EF Core migration snapshot (58 domain tables total; 6 payment tables excluded).",
    ]
    for i, t in enumerate(lines):
        legend.append('<text x="%.1f" y="%.1f" font-family="Segoe UI,Arial,sans-serif" font-size="13.5">%s</text>' % (lx, ly + 26 + i*18, esc(t)))

    head_svg = ('<?xml version="1.0" encoding="UTF-8"?>\n'
                '<svg xmlns="http://www.w3.org/2000/svg" width="%.0f" height="%.0f" viewBox="0 0 %.0f %.0f" '
                'font-family="Segoe UI,Arial,sans-serif">\n<rect width="100%%" height="100%%" fill="white"/>\n'
                % (canvas_w, canvas_h, canvas_w, canvas_h))
    body_group = ['<g transform="translate(%.1f,%.1f)">' % (shift_x, shift_y)] + label_svg + all_svg + rel_svg + ['</g>']
    out = head_svg + "\n".join(header + body_group + legend) + "\n</svg>\n"
    path = os.path.join(OUT, "EERD_Complete_Poster.svg")
    with open(path, "w", encoding="utf-8") as f:
        f.write(out)
    print("wrote img/EERD_Complete_Poster.svg  (%.0fx%.0f)" % (canvas_w, canvas_h))

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    render_all()

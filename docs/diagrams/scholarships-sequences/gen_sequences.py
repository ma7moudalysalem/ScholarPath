#!/usr/bin/env python3
"""Generate the ESSENTIAL Scholarships-module sequence diagrams, corrected against the
current ScholarPath codebase (per 'Scholarships Module — Corrected Object & Service
Mapping'). Renders each with PlantUML and assembles one Word file.

Style: no autoactivate (robust, no activation/return errors); explicit request arrows and
dashed responses; alt/opt fragments capture the real business rules. Actors/components/
controllers/commands/entities use the EXACT names verified in the mapping + code.
Payment objects are intentionally excluded (free-mode).
"""
import os, subprocess, shutil, tempfile
from PIL import Image, ImageStat
Image.MAX_IMAGE_PIXELS = None

HERE = r"D:/Projects/ScholarPath/docs/diagrams/scholarships-sequences"
SRC = os.path.join(HERE, "src"); PNG = os.path.join(HERE, "png")
JAVA = r"C:/Program Files/Microsoft/jdk-17.0.19.10-hotspot/bin/java.exe"
JAR = r"C:/Users/Mahmoud Salem/plantuml-tools/plantuml.jar"
os.makedirs(SRC, exist_ok=True); os.makedirs(PNG, exist_ok=True)

HEADER = """@startuml
skinparam backgroundColor #FFFFFF
skinparam shadowing false
skinparam defaultFontName "Segoe UI"
skinparam dpi 150
skinparam maxMessageSize 240
skinparam sequenceMessageAlign center
skinparam sequence {
  ArrowColor #2E4A7B
  LifeLineBorderColor #6C8EBF
  LifeLineBackgroundColor #EAF1FB
  ParticipantBorderColor #2E5496
  ParticipantBackgroundColor #DAE8FC
  ParticipantFontColor #1A1A1A
  ParticipantFontStyle bold
  ActorBorderColor #2E5496
  ActorBackgroundColor #DAE8FC
  ActorFontColor #1A1A1A
}
skinparam note {
  BackgroundColor #FFF9E6
  BorderColor #D6B656
  FontColor #1A1A1A
}
"""

def uml(title, body):
    return f"{HEADER}title {title}\n{body}\n@enduml\n"

# ---------------------------------------------------------------- the 14 essential flows
DIAGRAMS = []
def D(code, title, body): DIAGRAMS.append((code, title, uml(title, body)))

D("SCH-SEQ-01", "Browse, Search and Filter Scholarships", """
actor "Student" as ST
participant "ScholarshipsPage" as PAGE <<page>>
participant "scholarshipsApi" as API <<api>>
participant "ScholarshipsController\\n[Authorize]" as CTRL <<controller>>
participant "GetScholarshipsQuery" as Q <<query>>
database "Scholarship" as DB <<entity>>
ST -> PAGE : open list, set search / filters
PAGE -> API : scholarshipsApi.search(filters)
API -> CTRL : GET /api/scholarships
CTRL -> Q : GetScholarshipsQuery(filters)
Q -> DB : query Status = Open + filters
DB --> Q : matching page
Q --> CTRL : paged result
CTRL --> API : 200 OK
API --> PAGE : scholarships
alt results found
  PAGE --> ST : display paginated list
else no match
  PAGE --> ST : show empty state
end
note over Q : Defaults to ScholarshipStatus.Open. Filters:\\nterm, category, funding type, academic level,\\ncountry, deadline range, field, funded-only, sort.
""")

D("SCH-SEQ-02", "View Scholarship Details", """
actor "Student" as ST
participant "ScholarshipDetail" as PAGE <<page>>
participant "scholarshipsApi" as API <<api>>
participant "ScholarshipsController" as CTRL <<controller>>
participant "GetScholarshipByIdQuery" as Q <<query>>
database "Scholarship" as DB <<entity>>
ST -> PAGE : open scholarship detail
PAGE -> API : scholarshipsApi.getById(id)
API -> CTRL : GET /api/scholarships/{id}
CTRL -> Q : GetScholarshipByIdQuery(id)
Q -> DB : load scholarship (+ child rows)
DB --> Q : scholarship
Q --> CTRL : detail
CTRL --> API : 200 OK
API --> PAGE : scholarship detail
PAGE --> ST : show details and Apply Now
""")

D("SCH-SEQ-03", "Bookmark / Unbookmark Scholarship", """
actor "Student" as ST
participant "ScholarshipsPage /\\nScholarshipDetail" as PAGE <<page>>
participant "scholarshipsApi" as API <<api>>
participant "ScholarshipsController" as CTRL <<controller>>
participant "BookmarkToggleCommand" as CMD <<command>>
database "SavedScholarship" as SS <<entity>>
ST -> PAGE : click bookmark
PAGE -> API : scholarshipsApi.toggleBookmark(id)
API -> CTRL : POST /api/scholarships/{id}/bookmark
CTRL -> CMD : BookmarkToggleCommand
alt not yet bookmarked
  CMD -> SS : create SavedScholarship
else already bookmarked
  CMD -> SS : remove SavedScholarship
end
SS --> CMD : new state
CMD --> CTRL : bookmarked?
CTRL --> API : 200 OK
API --> PAGE : state
PAGE --> ST : update bookmark icon
note over SS : Bookmark entity is SavedScholarship\\n(not Bookmark / ScholarshipBookmark).
""")

D("SCH-SEQ-04", "Apply Now — Start Local Draft Application", """
actor "Student" as ST
participant "ScholarshipDetail" as PAGE <<page>>
participant "applicationsApi" as API <<api>>
participant "ApplicationsController" as CTRL <<controller>>
participant "StartApplicationCommand" as CMD <<command>>
database "Scholarship" as SCH <<entity>>
database "ApplicationTracker" as APP <<entity>>
ST -> PAGE : click Apply Now (in-app scholarship)
PAGE -> API : applicationsApi.start(scholarshipId)
API -> CTRL : POST /api/applications
CTRL -> CMD : StartApplicationCommand
CMD -> SCH : load and validate
note right of CMD : rejects ExternalUrl listings; requires Status = Open;\\ndeadline not passed; checks profile academic level & field.
alt existing non-terminal application
  CMD -> APP : find resumable application (Draft/Pending/UnderReview/Shortlisted)
  APP --> CMD : existing application
  CMD --> CTRL : resume existing
else eligible, none active
  CMD -> APP : create ApplicationTracker(Status = Draft, Mode = InApp)
  APP --> CMD : new Draft
  CMD --> CTRL : created
else not eligible / closed / past deadline
  CMD --> CTRL : rejected (business rule)
end
CTRL --> API : result
API --> PAGE : draft / error
PAGE --> ST : open application form or show reason
""")

D("SCH-SEQ-05", "Save Application Draft", """
actor "Student" as ST
participant "StudentApplicationDetail" as PAGE <<page>>
participant "applicationsApi" as API <<api>>
participant "ApplicationsController" as CTRL <<controller>>
participant "SaveApplicationDraftCommand" as CMD <<command>>
database "ApplicationTracker" as APP <<entity>>
ST -> PAGE : edit form data / documents / notes
PAGE -> API : applicationsApi.saveDraft(id, data)
API -> CTRL : PUT /api/applications/{id}/draft
CTRL -> CMD : SaveApplicationDraftCommand
CMD -> APP : verify ownership + Draft status
alt owned Draft
  CMD -> APP : persist form data / documents / notes
  APP --> CMD : saved
  CMD --> CTRL : ok
else not owner / not Draft
  CMD --> CTRL : rejected
end
CTRL --> API : result
API --> PAGE : saved state
PAGE --> ST : draft saved
""")

D("SCH-SEQ-06", "Submit Application", """
actor "Student" as ST
participant "StudentApplicationDetail" as PAGE <<page>>
participant "applicationsApi" as API <<api>>
participant "ApplicationsController" as CTRL <<controller>>
participant "SubmitApplicationCommand" as CMD <<command>>
participant "ApplicationStateMachine" as SM <<service>>
database "ApplicationTracker" as APP <<entity>>
participant "Domain Events" as EV <<events>>
ST -> PAGE : click Submit
PAGE -> API : applicationsApi.submit(id)
API -> CTRL : PUT /api/applications/{id}/submit
CTRL -> CMD : SubmitApplicationCommand
CMD -> APP : verify ownership + required form data / documents
CMD -> SM : EnsureTransition(Draft -> Pending)
alt valid & before deadline
  SM --> CMD : transition allowed
  CMD -> APP : set Status = Pending
  CMD -> EV : raise ApplicationSubmittedEvent + ApplicationStatusChangedEvent
  EV --> CMD : queued (async handlers notify student & provider)
  CMD --> CTRL : submitted
else incomplete or past deadline
  SM --> CMD : rejected
  CMD --> CTRL : validation error
end
CTRL --> API : result
API --> PAGE : status
PAGE --> ST : show Pending / show errors
""")

D("SCH-SEQ-07", "Withdraw Application", """
actor "Student" as ST
participant "StudentApplicationDetail" as PAGE <<page>>
participant "applicationsApi" as API <<api>>
participant "ApplicationsController" as CTRL <<controller>>
participant "WithdrawApplicationCommand" as CMD <<command>>
database "ApplicationTracker" as APP <<entity>>
participant "NotificationDispatcher" as NOT <<service>>
ST -> PAGE : click Withdraw
PAGE -> API : applicationsApi.withdraw(id)
API -> CTRL : POST /api/applications/{id}/withdraw
CTRL -> CMD : WithdrawApplicationCommand
CMD -> APP : verify ownership; check IsReadOnly
alt not read-only (active application)
  CMD -> APP : set Status = Withdrawn
  CMD -> NOT : NotificationType.ApplicationWithdrawn -> student
  NOT --> CMD : notified (also raises ApplicationStatusChangedEvent)
  CMD --> CTRL : withdrawn
else Accepted / Rejected / already Withdrawn
  APP --> CMD : blocked (read-only)
  CMD --> CTRL : conflict
end
CTRL --> API : result
API --> PAGE : status
PAGE --> ST : show Withdrawn / show reason
note over CMD : Withdraw gates on ApplicationTracker.IsReadOnly\\n(Accepted / Rejected / Withdrawn) — NOT the ApplicationStateMachine.
""")

D("SCH-SEQ-08", "Track External Application", """
actor "Student" as ST
participant "ScholarshipDetail" as PAGE <<page>>
participant "applicationsApi" as API <<api>>
participant "ApplicationsController" as CTRL <<controller>>
participant "ExternalIntentCommand" as CMD <<command>>
database "ApplicationTracker" as APP <<entity>>
ST -> PAGE : Apply Now on external scholarship
PAGE -> ST : redirect to ExternalApplicationUrl
PAGE -> API : applicationsApi.createExternal(intent)
API -> CTRL : POST /api/applications/external
CTRL -> CMD : ExternalIntentCommand
alt linked external listing
  CMD -> APP : create tracker (ScholarshipId, Mode = External, Status = Intending)
else pure off-platform
  CMD -> APP : create tracker (Title / Provider / Deadline, Status = Intending)
end
APP --> CMD : tracker
CMD --> CTRL : created
CTRL --> API : 201 Created
API --> PAGE : external tracker
PAGE --> ST : external application tracked
""")

D("SCH-SEQ-09", "Provider Creates Scholarship Listing", """
actor "Scholarship Provider" as SP
actor "Admin / SuperAdmin" as AD
participant "ScholarshipForm" as PAGE <<page>>
participant "scholarshipsApi" as API <<api>>
participant "ScholarshipsController" as CTRL <<controller>>
participant "CreateScholarshipCommand" as CMD <<command>>
database "Scholarship" as DB <<entity>>
alt created by Scholarship Provider
  SP -> PAGE : fill in-app listing form
  PAGE -> API : scholarshipsApi.createScholarship(data)
  API -> CTRL : POST /api/scholarships
  CTRL -> CMD : CreateScholarshipCommand
  CMD -> DB : create Scholarship(Status = UnderReview)
else created by Admin
  AD -> PAGE : fill listing form (may be ExternalUrl)
  PAGE -> API : scholarshipsApi.createScholarship(data)
  API -> CTRL : POST /api/scholarships
  CTRL -> CMD : CreateScholarshipCommand
  CMD -> DB : create Scholarship(Status = Open)
end
DB --> CMD : listing id
CMD --> CTRL : created
CTRL --> API : 201 Created
API --> PAGE : listing
note over CMD : Provider listings start in UnderReview; Admin listings open\\nimmediately. ExternalUrl is Admin-only; Providers create InApp only.
""")

D("SCH-SEQ-10", "Admin Approves Scholarship", """
actor "Admin" as AD
participant "AdminScholarships" as PAGE <<page>>
participant "scholarshipsApi" as API <<api>>
participant "ScholarshipsController" as CTRL <<controller>>
participant "ApproveScholarshipCommand" as CMD <<command>>
database "Scholarship" as DB <<entity>>
participant "NotificationDispatcher" as NOT <<service>>
AD -> PAGE : open moderation queue, approve listing
PAGE -> API : scholarshipsApi.approve(id)
API -> CTRL : POST /api/scholarships/{id}/approve
CTRL -> CMD : ApproveScholarshipCommand
CMD -> DB : require Status = UnderReview; re-check 7-day deadline
CMD -> DB : set Status = Open, OpenedAt = now; clear RejectionReason / RejectedAt
DB --> CMD : opened
CMD -> NOT : NotificationType.ScholarshipApproved -> provider
NOT --> CMD : notified
CMD --> CTRL : done
CTRL --> API : 204 No Content
API --> PAGE : updated
PAGE --> AD : listing is now Open
note over CMD : Approve does NOT call KnowledgeBaseIndexer. Open scholarships\\nmay be indexed later during a knowledge-base rebuild only.
""")

D("SCH-SEQ-11", "Admin Rejects Scholarship", """
actor "Admin" as AD
participant "AdminScholarships" as PAGE <<page>>
participant "scholarshipsApi" as API <<api>>
participant "ScholarshipsController" as CTRL <<controller>>
participant "RejectScholarshipCommand" as CMD <<command>>
database "Scholarship" as DB <<entity>>
participant "NotificationDispatcher" as NOT <<service>>
AD -> PAGE : reject listing with reason
PAGE -> API : scholarshipsApi.reject(id, reason)
API -> CTRL : POST /api/scholarships/{id}/reject
CTRL -> CTRL : RejectScholarshipCommandValidator (reason required)
alt reason missing
  CTRL --> API : 400 Validation Problem
  API --> PAGE : error
  PAGE --> AD : rejection reason is required
else reason provided
  CTRL -> CMD : RejectScholarshipCommand
  CMD -> DB : require Status = UnderReview; set Status = Draft, RejectionReason, RejectedAt = now
  DB --> CMD : rejected
  CMD -> NOT : NotificationType.ScholarshipRejected (with reason) -> provider
  NOT --> CMD : notified
  CMD --> CTRL : done
  CTRL --> API : 204 No Content
  API --> PAGE : updated
  PAGE --> AD : listing returned to Draft with reason
end
""")

D("SCH-SEQ-12", "Provider Reviews Student Application", """
actor "Scholarship Provider" as SP
participant "ApplicationsReview" as PAGE <<page>>
participant "applicationsApi" as API <<api>>
participant "ApplicationsController" as CTRL <<controller>>
participant "GetScholarshipProviderApplicationsQuery" as Q <<query>>
participant "ReviewApplicationCommand" as CMD <<command>>
participant "ApplicationStateMachine" as SM <<service>>
database "ApplicationTracker" as APP <<entity>>
participant "NotificationDispatcher" as NOT <<service>>
SP -> PAGE : open applications for own scholarships
PAGE -> API : applicationsApi.getScholarshipProviderApplications()
API -> CTRL : GET /api/applications/company
CTRL -> Q : GetScholarshipProviderApplicationsQuery
Q -> APP : load owned-scholarship applications
APP --> Q : applications
Q --> CTRL : list
CTRL --> API : 200 OK
API --> PAGE : applications
SP -> PAGE : set decision (In Assessment / Shortlisted / Accepted / Rejected)
PAGE -> API : applicationsApi.reviewApplication(id, decision)
API -> CTRL : POST /api/applications/{id}/review
CTRL -> CMD : ReviewApplicationCommand
CMD -> APP : verify provider owns the scholarship & app not read-only
CMD -> SM : EnsureTransition(current -> decision)
alt transition allowed
  SM --> CMD : allowed
  CMD -> APP : set Status = UnderReview / Shortlisted / Accepted / Rejected
  CMD -> NOT : NotificationType.ApplicationStatusChanged -> student
  NOT --> CMD : notified (raises ApplicationStatusChangedEvent)
  CMD --> CTRL : updated
else not owner / invalid transition / final state
  CMD --> CTRL : forbidden / conflict
end
CTRL --> API : result
API --> PAGE : status
PAGE --> SP : updated application status
note over CMD : "In Assessment" = ScholarshipStatus/ApplicationStatus.UnderReview.\\nAccepted / Rejected / Withdrawn are final, read-only.
""")

D("SCH-SEQ-13", "Student Rates Scholarship Provider", """
actor "Student" as ST
participant "StudentApplicationDetail" as PAGE <<page>>
participant "applicationsApi" as API <<api>>
participant "ScholarshipProviderReviewsController" as CTRL <<controller>>
participant "SubmitScholarshipProviderRatingCommand" as CMD <<command>>
database "ScholarshipProviderReview" as REV <<entity>>
database "UserProfile" as UP <<entity>>
participant "NotificationDispatcher" as NOT <<service>>
ST -> PAGE : rate provider after final outcome (1-5, comment)
PAGE -> API : applicationsApi.submitReview(applicationId, rating)
API -> CTRL : POST /api/company-reviews
CTRL -> CMD : SubmitScholarshipProviderRatingCommand
CMD -> REV : check application owned & Accepted/Rejected & not yet rated
alt eligible (one review per finalized application)
  CMD -> REV : create ScholarshipProviderReview (provider resolved server-side)
  CMD -> UP : update ScholarshipProviderAverageRating + ScholarshipProviderReviewCount
  CMD -> NOT : NotificationType.ScholarshipProviderRatingReceived -> provider
  NOT --> CMD : notified
  CMD -> REV : average of latest 20 visible reviews (RatingWindowSize = 20)
  opt average < 2.5 and >= 1 review in window (first flagging only)
    CMD -> UP : set ScholarshipProviderLowRatingFlaggedAt
    CMD -> NOT : NotificationType.ScholarshipProviderLowRatingFlagged -> Admins
  end
  CMD --> CTRL : rating saved
else not owner / not final / already rated / rating out of 1-5
  CMD --> CTRL : rejected
end
CTRL --> API : result
API --> PAGE : outcome
PAGE --> ST : rating submitted
""")

D("SCH-SEQ-14", "Auto-Close Scholarship on Deadline", """
participant "Scheduled Job" as JOB <<job>>
participant "ScholarshipAutoCloseJob" as ACJ <<job>>
database "Scholarship" as DB <<entity>>
JOB -> ACJ : run on schedule
ACJ -> DB : select Status = Open AND Deadline < now
DB --> ACJ : expired open scholarships
loop each expired scholarship
  ACJ -> DB : set Status = Closed
end
ACJ --> JOB : closed count (logged)
note over ACJ : Closes ONLY Open scholarships whose Deadline is in the past.\\nNo notification is dispatched by the job.
""")

# ---------------------------------------------------------------- render + validate
def render(code, puml_text):
    p = os.path.join(SRC, f"{code}.puml")
    open(p, "w", encoding="utf-8").write(puml_text)
    d = tempfile.mkdtemp()
    r = subprocess.run([JAVA, "-jar", JAR, "-tpng", "-DPLANTUML_LIMIT_SIZE=32768", "-o", d, p],
                       capture_output=True, text=True)
    pngs = [f for f in os.listdir(d) if f.endswith(".png")]
    if not pngs:
        shutil.rmtree(d, ignore_errors=True)
        return None, f"NO PNG: {r.stderr[:200]}"
    out = os.path.join(PNG, f"{code}.png"); shutil.copy(os.path.join(d, pngs[0]), out)
    mean = ImageStat.Stat(Image.open(out).convert("L")).mean[0]
    shutil.rmtree(d, ignore_errors=True)
    return out, ("OK" if mean > 200 else f"ERROR-IMAGE mean={mean:.0f}")

if __name__ == "__main__":
    results = []
    for code, title, text in DIAGRAMS:
        out, status = render(code, text)
        results.append((code, title, out, status))
        print(f"{code}: {status}")
    ok = sum(1 for r in results if r[3] == "OK")
    print(f"\n{ok}/{len(DIAGRAMS)} rendered clean")

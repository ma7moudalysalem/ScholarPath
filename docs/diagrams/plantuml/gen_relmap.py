#!/usr/bin/env python3
"""Generate Elmasri-style relational-schema diagrams (Fundamentals of Database
Systems, Fig. 9.2): each relation is a box of attribute cells, the primary key
underlined, and a directed arrow runs from every foreign key to the primary key
it references. Output = one PlantUML @startdot file per cluster (rendered by
plantuml.jar via its bundled Graphviz).

NOTE: this PlantUML/Graphviz form uses splines=polyline because Graphviz's
splines=ortho IGNORES HTML cell ports and lands arrowheads on the wrong cell.
polyline honours the ports (correct FK->PK endpoints, no overlap) but draws
diagonal connectors. The canonical *orthogonal* print diagrams (right angles +
correct endpoints + no overlap) are produced by gen_relmap_svg.py, which routes
every edge by hand; those SVG/PNGs are the ones embedded in the LaTeX report.
Both generators read the same CLUSTERS dict below, so the data never diverges."""

import os

# role: 'pk' (underlined), 'fk' (solid arrow -> target), 'soft' (dashed arrow),
#       'uk' (unique), '' (plain).  target = "Table" (its PK) or "Table:Col".
OUT = os.path.dirname(os.path.abspath(__file__))

CLUSTERS = {
"RelMap_Identity_Profile": {
  "title": "Relational schema — Identity & Profile",
  "tables": {
    "Users": [("Id","pk",""),("Email","uk",""),("FirstName","",""),("LastName","",""),
              ("AccountStatus","",""),("ActiveRole","",""),("IsDeleted","","")],
    "Roles": [("Id","pk",""),("Name","","")],
    "UserRoles": [("UserId","pk fk","Users"),("RoleId","pk fk","Roles")],
    "UserProfiles": [("Id","pk",""),("UserId","fk uk","Users"),("Gpa","",""),
                     ("AcademicLevel","",""),("SessionFeeUsd","",""),
                     ("CompanyAverageRating","",""),("StripeConnectStatus","",""),
                     ("ProfileCompletenessPercent","","")],
    "EducationEntries": [("Id","pk",""),("UserProfileId","fk","UserProfiles"),
                         ("InstitutionName","",""),("Degree","","")],
    "RefreshTokens": [("Id","pk",""),("UserId","fk","Users"),("TokenHash","uk",""),
                      ("ExpiresAt","",""),("IsRevoked","","")],
    "PasswordResetTokens": [("Id","pk",""),("UserId","fk","Users"),("TokenHash","uk",""),("UsedAt","","")],
    "UpgradeRequests": [("Id","pk",""),("UserId","fk","Users"),("Target","",""),("Status","","")],
    "UpgradeRequestFiles": [("Id","pk",""),("UpgradeRequestId","fk","UpgradeRequests"),("BlobUrl","","")],
  }},
"RelMap_Scholarships_Applications": {
  "title": "Relational schema — Scholarships, Applications & Documents",
  "tables": {
    "Users": [("Id","pk","")],
    "Categories": [("Id","pk",""),("Slug","uk",""),("NameEn","","")],
    "Scholarships": [("Id","pk",""),("CategoryId","fk","Categories"),
                     ("OwnerCompanyId","fk","Users"),("Slug","uk",""),("Mode","",""),
                     ("Status","",""),("Deadline","",""),("ReviewFeeUsd","","")],
    "SavedScholarships": [("Id","pk",""),("UserId","soft","Users"),
                          ("ScholarshipId","fk","Scholarships"),("SavedAt","","")],
    "Applications": [("Id","pk",""),("StudentId","fk","Users"),
                     ("ScholarshipId","fk","Scholarships"),("Mode","",""),
                     ("Status","",""),("SubmittedAt","","")],
    "Documents": [("Id","pk",""),("OwnerUserId","fk","Users"),
                  ("ApplicationTrackerId","fk","Applications"),("Category","",""),("StoragePath","","")],
    "CompanyReviewRequests": [("Id","pk",""),("StudentId","fk","Users"),
                              ("CompanyId","fk","Users"),("ScholarshipId","fk","Scholarships"),
                              ("PaymentId","fk","Payments"),("Status","",""),("ReviewFeeUsdSnapshot","","")],
    "CompanyReviews": [("Id","pk",""),("ApplicationTrackerId","fk uk","Applications"),
                       ("StudentId","fk","Users"),("CompanyId","fk","Users"),("Rating","","")],
    "CompanyReviewPayments": [("Id","pk",""),("ApplicationTrackerId","soft","Applications"),
                              ("CompanyId","soft","Users"),("StripePaymentIntentId","uk",""),
                              ("IdempotencyKey","uk",""),("Status","","")],
    "Payments": [("Id","pk","")],
  }},
"RelMap_Booking_Payments_Ratings": {
  "title": "Relational schema — Booking, Payments & Ratings",
  "tables": {
    "Users": [("Id","pk","")],
    "Availabilities": [("Id","pk",""),("ConsultantId","fk","Users"),("IsRecurring","",""),("IsActive","","")],
    "Bookings": [("Id","pk",""),("StudentId","fk","Users"),("ConsultantId","fk","Users"),
                 ("AvailabilityId","fk","Availabilities"),("PaymentId","fk","Payments"),
                 ("ScheduledStartAt","",""),("PriceUsd","",""),("Status","","")],
    "Payments": [("Id","pk",""),("PayerUserId","soft","Users"),("PayeeUserId","soft","Users"),
                 ("PayoutId","soft","Payouts"),("Type","",""),("Status","",""),("AmountCents","",""),
                 ("IdempotencyKey","uk","")],
    "Payouts": [("Id","pk",""),("PayeeUserId","soft","Users"),("AmountCents","",""),("Status","","")],
    "ConsultantReviews": [("Id","pk",""),("BookingId","fk uk","Bookings"),
                          ("StudentId","fk","Users"),("ConsultantId","fk","Users"),("Rating","","")],
    "SessionRecordings": [("Id","pk",""),("BookingId","fk","Bookings"),("RecordingId","",""),("StoragePath","","")],
  }},
"RelMap_Community_Chat": {
  "title": "Relational schema — Community & Chat",
  "tables": {
    "Users": [("Id","pk","")],
    "ForumCategories": [("Id","pk",""),("Slug","uk","")],
    "ForumPosts": [("Id","pk",""),("AuthorId","fk","Users"),("CategoryId","fk","ForumCategories"),
                   ("ParentPostId","fk","ForumPosts"),("ModerationStatus","",""),("UpvoteCount","",""),("FlagCount","","")],
    "ForumVotes": [("Id","pk",""),("ForumPostId","fk","ForumPosts"),("UserId","soft","Users"),("VoteType","","")],
    "ForumFlags": [("Id","pk",""),("ForumPostId","fk","ForumPosts"),("FlaggedByUserId","soft","Users"),("Reason","","")],
    "ForumBookmarks": [("Id","pk",""),("ForumPostId","fk","ForumPosts"),("UserId","soft","Users")],
    "ForumTags": [("Id","pk",""),("Slug","uk","")],
    "ForumPostTags": [("ForumPostId","pk fk","ForumPosts"),("ForumTagId","pk fk","ForumTags")],
    "Conversations": [("Id","pk",""),("ParticipantOneId","soft","Users"),("ParticipantTwoId","soft","Users"),("LastMessageAt","","")],
    "Messages": [("Id","pk",""),("ConversationId","fk","Conversations"),("SenderId","soft","Users"),("Body","","")],
  }},
"RelMap_Resources_Notifications": {
  "title": "Relational schema — Resources Hub & Notifications",
  "tables": {
    "Users": [("Id","pk","")],
    "Resources": [("Id","pk",""),("AuthorUserId","fk","Users"),("Slug","uk",""),("Type","",""),("Status","","")],
    "ResourceChapters": [("Id","pk",""),("ResourceId","fk","Resources"),("SortOrder","","")],
    "ResourceBookmarks": [("Id","pk",""),("ResourceId","fk","Resources"),("UserId","soft","Users")],
    "ResourceProgress": [("Id","pk",""),("ResourceId","fk","Resources"),("UserId","soft","Users"),("ChaptersCompletedCount","","")],
    "ResourceProgressChildren": [("Id","pk",""),("ResourceProgressId","fk","ResourceProgress"),("IsCompleted","","")],
    "Notifications": [("Id","pk",""),("RecipientUserId","soft","Users"),("Type","",""),("Channel","",""),("IsRead","","")],
    "NotificationPreferences": [("Id","pk",""),("UserId","soft","Users"),("Type","",""),("Channel","",""),("IsEnabled","","")],
  }},
"RelMap_AI_Platform": {
  "title": "Relational schema — AI, Knowledge, Platform & Cross-cutting",
  "tables": {
    "Users": [("Id","pk","")],
    "Scholarships": [("Id","pk","")],
    "AiInteractions": [("Id","pk",""),("UserId","soft","Users"),("Feature","",""),("Provider","",""),("CostUsd","","")],
    "RecommendationClickEvents": [("Id","pk",""),("UserId","fk","Users"),("ScholarshipId","fk","Scholarships"),
                                  ("AiInteractionId","fk","AiInteractions"),("Source","","")],
    "AiRedactionAuditSamples": [("Id","pk",""),("AiInteractionId","fk uk","AiInteractions"),
                                ("UserId","fk","Users"),("ReviewerUserId","fk","Users"),("Verdict","","")],
    "UserRiskFlags": [("Id","pk",""),("UserId","fk uk","Users"),("Score","",""),("IsAtRisk","","")],
    "AuditLogs": [("Id","pk",""),("ActorUserId","soft","Users"),("Action","",""),("TargetType","",""),("TargetId","","")],
    "KnowledgeDocuments": [("Id","pk",""),("SourceType","",""),("SourceId","",""),("SourceKey","uk","")],
    "PlatformSettings": [("Id","pk",""),("Key","uk",""),("ValueType","","")],
    "UserDataRequests": [("Id","pk",""),("UserId","soft","Users"),("Type","",""),("Status","","")],
  }},
}

PERROW = 40  # one row of cells per relation (Elmasri Fig 9.2 style — no wrapping)

def esc(s): return s.replace("&","&amp;").replace("<","&lt;").replace(">","&gt;")

def cell(col, role):
    label = esc(col)
    if "pk" in role: label = "<U>%s</U>" % label
    bg = ' BGCOLOR="#fff7e6"' if ("fk" in role or "soft" in role) else ""
    return '<TD PORT="%s"%s>%s</TD>' % (col, bg, label)

def node(name, cols):
    tds = "".join(cell(c, r) for c, r, _ in cols)
    header = '<TR><TD COLSPAN="%d" BGCOLOR="#dbe6ff"><B>%s</B></TD></TR>' % (len(cols), esc(name))
    label = '<<TABLE BORDER="0" CELLBORDER="1" CELLSPACING="0">%s<TR>%s</TR></TABLE>>' % (header, tds)
    return '  %s [label=%s];' % (name, label)

def edges(tables):
    out = []
    for tname, cols in tables.items():
        for col, role, target in cols:
            if not target: continue
            if ":" in target: ttab, tcol = target.split(":")
            else: ttab, tcol = target, "Id"
            # FK arrows drive the layered ranking so Graphviz minimises crossings;
            # spline routing respects the HTML cell ports (correct FK->PK endpoints)
            extra = " [style=dashed]" if "soft" in role else ""
            out.append('  %s:%s -> %s:%s%s;' % (tname, col, ttab, tcol, extra))
    return out

def main():
    for fname, c in CLUSTERS.items():
        lines = ["@startdot", "digraph G {",
                 '  rankdir=BT; splines=polyline; nodesep=0.7; ranksep=1.1;',
                 '  node [shape=plaintext];']
        for tname, cols in c["tables"].items():
            lines.append(node(tname, cols))
        lines += edges(c["tables"])
        lines += ["}", "@enddot", ""]
        path = os.path.join(OUT, fname + ".puml")
        with open(path, "w", encoding="utf-8") as f:
            f.write("\n".join(lines))
        print("wrote", fname + ".puml", "(%d tables)" % len(c["tables"]))

if __name__ == "__main__":
    main()

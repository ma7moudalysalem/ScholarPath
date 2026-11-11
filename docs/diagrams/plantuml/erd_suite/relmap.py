#!/usr/bin/env python3
"""Relational-mapping diagrams (Elmasri Fig. 9.2 style): each relation is a row
of attribute cells, the PK underlined, FK cells shaded, and an orthogonal arrow
runs from every foreign key to the primary key it references (solid = DB-
enforced FK, dashed = application-enforced 'loose' reference).

Corrected model (2026-07-05): payments removed, Company -> ScholarshipProvider,
SessionRecording/NoShowReport added, aligned to the six ERD modules. One SVG per
module (mapping-module-N) plus a combined atlas is assembled separately.
Self-contained hand router (no Graphviz) — every coordinate under our control.
"""
import os

OUT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "img", "erd_suite"))

# role tokens: 'pk' underline, 'fk' solid arrow, 'soft' dashed arrow, 'uk' unique.
# target = "Table" (its PK) or "Table:Col".
CLUSTERS = {
"mapping-module-1": {"title": "Relational mapping — Module 1: Identity, Access & Profile", "tables": {
    "Users": [("Id","pk",""),("Email","uk",""),("FirstName","",""),("LastName","",""),
              ("AccountStatus","",""),("ActiveRole","",""),("IsOnboardingComplete","",""),("IsDeleted","","")],
    "Roles": [("Id","pk",""),("Name","","")],
    "UserRoles": [("UserId","pk fk","Users"),("RoleId","pk fk","Roles")],
    "UserProfiles": [("Id","pk",""),("UserId","fk uk","Users"),("Gpa","",""),("AcademicLevel","",""),
                     ("BookingAccessStatus","",""),("ConsultantRatingPenaltyFactor","",""),
                     ("ConsultantAverageRating","",""),("ScholarshipProviderAverageRating","",""),
                     ("ProfileCompletenessPercent","","")],
    "EducationEntries": [("Id","pk",""),("UserProfileId","fk","UserProfiles"),("InstitutionName","",""),("Degree","","")],
    "RefreshTokens": [("Id","pk",""),("UserId","fk","Users"),("TokenHash","uk",""),("ExpiresAt","",""),("IsRevoked","","")],
    "PasswordResetTokens": [("Id","pk",""),("UserId","fk","Users"),("TokenHash","uk",""),("UsedAt","","")],
    "ExpertiseTags": [("Id","pk",""),("Slug","uk",""),("NameEn","",""),("NameAr","","")],
    "UpgradeRequests": [("Id","pk",""),("UserId","fk","Users"),("Target","",""),("Status","","")],
    "UpgradeRequestFiles": [("Id","pk",""),("UpgradeRequestId","fk","UpgradeRequests"),("BlobUrl","","")],
    "UpgradeRequestLinks": [("Id","pk",""),("UpgradeRequestId","fk","UpgradeRequests"),("Url","","")],
}},
"mapping-module-2": {"title": "Relational mapping — Module 2: Scholarships, Applications & Documents", "tables": {
    "Users": [("Id","pk","")],
    "Categories": [("Id","pk",""),("Slug","uk",""),("NameEn","","")],
    "Scholarships": [("Id","pk",""),("CategoryId","fk","Categories"),("OwnerScholarshipProviderId","fk","Users"),
                     ("Slug","uk",""),("Mode","",""),("Status","",""),("Deadline","","")],
    "ScholarshipChildren": [("Id","pk",""),("ScholarshipId","fk","Scholarships"),("ChildType","",""),("KeyEn","","")],
    "SavedScholarships": [("Id","pk",""),("UserId","soft","Users"),("ScholarshipId","fk","Scholarships"),("SavedAt","","")],
    "Applications": [("Id","pk",""),("StudentId","fk","Users"),("ScholarshipId","fk","Scholarships"),
                     ("Mode","",""),("Status","",""),("SubmittedAt","","")],
    "ApplicationChildren": [("Id","pk",""),("ApplicationTrackerId","fk","Applications"),("ChildType","","")],
    "Documents": [("Id","pk",""),("OwnerUserId","fk","Users"),("ApplicationTrackerId","fk","Applications"),
                  ("Category","",""),("StoragePath","","")],
    "ScholarshipProviderReviewRequests": [("Id","pk",""),("StudentId","fk","Users"),
                     ("ScholarshipProviderId","fk","Users"),("ScholarshipId","fk","Scholarships"),("Status","","")],
    "ScholarshipProviderReviews": [("Id","pk",""),("ApplicationTrackerId","fk uk","Applications"),
                     ("StudentId","fk","Users"),("ScholarshipProviderId","fk","Users"),("Rating","","")],
}},
"mapping-module-3": {"title": "Relational mapping — Module 3: Consultant Booking & Ratings", "tables": {
    "Users": [("Id","pk","")],
    "Availabilities": [("Id","pk",""),("ConsultantId","fk","Users"),("IsRecurring","",""),("IsActive","","")],
    "Bookings": [("Id","pk",""),("StudentId","fk","Users"),("ConsultantId","fk","Users"),
                 ("AvailabilityId","fk","Availabilities"),("ScheduledStartAt","",""),("Status","","")],
    "NoShowReports": [("Id","pk",""),("BookingId","fk","Bookings"),("ReporterUserId","fk","Users"),
                      ("AccusedUserId","fk","Users"),("AccusedRole","",""),("Status","","")],
    "ConsultantReviews": [("Id","pk",""),("BookingId","fk uk","Bookings"),("StudentId","fk","Users"),
                          ("ConsultantId","fk","Users"),("Rating","","")],
    "SessionRecordings": [("Id","pk",""),("BookingId","fk","Bookings"),("RecordingId","",""),
                          ("StoragePath","",""),("SizeBytes","","")],
}},
"mapping-module-4": {"title": "Relational mapping — Module 4: Community & Chat", "tables": {
    "Users": [("Id","pk","")],
    "ForumCategories": [("Id","pk",""),("Slug","uk","")],
    "ForumPosts": [("Id","pk",""),("AuthorId","fk","Users"),("CategoryId","fk","ForumCategories"),
                   ("ParentPostId","fk","ForumPosts"),("ModerationStatus","","")],
    "ForumPostAttachments": [("Id","pk",""),("ForumPostId","fk","ForumPosts"),("BlobUrl","","")],
    "ForumVotes": [("Id","pk",""),("ForumPostId","fk","ForumPosts"),("UserId","soft","Users"),("VoteType","","")],
    "ForumFlags": [("Id","pk",""),("ForumPostId","fk","ForumPosts"),("FlaggedByUserId","soft","Users"),("Reason","","")],
    "ForumBookmarks": [("Id","pk",""),("ForumPostId","fk","ForumPosts"),("UserId","soft","Users")],
    "ForumTags": [("Id","pk",""),("Slug","uk","")],
    "ForumPostTags": [("ForumPostId","pk fk","ForumPosts"),("ForumTagId","pk fk","ForumTags")],
    "Conversations": [("Id","pk",""),("ParticipantOneId","soft","Users"),("ParticipantTwoId","soft","Users")],
    "Messages": [("Id","pk",""),("ConversationId","fk","Conversations"),("SenderId","soft","Users"),("Body","","")],
    "UserBlocks": [("Id","pk",""),("BlockerId","soft","Users"),("BlockedUserId","soft","Users")],
}},
"mapping-module-5": {"title": "Relational mapping — Module 5: Resources Hub & Notifications", "tables": {
    "Users": [("Id","pk","")],
    "Resources": [("Id","pk",""),("AuthorUserId","fk","Users"),("Slug","uk",""),("Type","",""),("Status","","")],
    "ResourceChapters": [("Id","pk",""),("ResourceId","fk","Resources"),("SortOrder","","")],
    "ResourceBookmarks": [("Id","pk",""),("ResourceId","fk","Resources"),("UserId","soft","Users")],
    "ResourceProgress": [("Id","pk",""),("ResourceId","fk","Resources"),("UserId","soft","Users"),("ChaptersCompletedCount","","")],
    "ResourceProgressChildren": [("Id","pk",""),("ResourceProgressId","fk","ResourceProgress"),("IsCompleted","","")],
    "Notifications": [("Id","pk",""),("RecipientUserId","soft","Users"),("Type","",""),("Channel","",""),("IsRead","","")],
    "NotificationPreferences": [("Id","pk",""),("UserId","soft","Users"),("Type","",""),("Channel","",""),("IsEnabled","","")],
}},
"mapping-module-6": {"title": "Relational mapping — Module 6: AI, Knowledge Base & Platform", "tables": {
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
    "SuccessStories": [("Id","pk",""),("StudentId","soft","Users"),("IsApproved","","")],
}},
}

# ---------------- metrics (hand router, adapted from gen_relmap_svg) ----------
FS, CHW, PADX, ROWH, HEADH = 13, 6.9, 9, 24, 23
VGAP, LANE, BUSPAD, RIGHTM, TOPM, BOTM, STUB = 60, 12, 14, 44, 40, 34, 7
HEADER_FILL, FK_FILL, ARROW = "#dbe6ff", "#fff7e6", 8

def esc(s): return s.replace("&","&amp;").replace("<","&lt;").replace(">","&gt;")
def textw(s): return len(s)*CHW
def cellw(l): return max(20.0, textw(l)+2*PADX)

def layout(tables):
    names = list(tables.keys()); indeg = {n:0 for n in names}
    for cols in tables.values():
        for _c,_r,t in cols:
            if t: tt=t.split(":")[0];
            else: continue
            if tt in indeg: indeg[tt]+=1
    return sorted(names, key=lambda n:(-indeg[n], names.index(n)))

def build(name, cols, x0, y):
    tot = sum(cellw(c) for c,_,_ in cols); p=[]
    p.append('<rect x="%.1f" y="%.1f" width="%.1f" height="%d" fill="%s" stroke="black"/>'%(x0,y,tot,HEADH,HEADER_FILL))
    p.append('<text x="%.1f" y="%.1f" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-weight="bold" font-size="%d">%s</text>'%(x0+tot/2,y+HEADH-7,FS,esc(name)))
    boxes={}; cx=x0; yr=y+HEADH
    for c,role,_ in cols:
        w=cellw(c); fill=FK_FILL if("fk" in role or "soft" in role) else "white"
        p.append('<rect x="%.1f" y="%.1f" width="%.1f" height="%d" fill="%s" stroke="black"/>'%(cx,yr,w,ROWH,fill))
        deco=' text-decoration="underline"' if "pk" in role else ""
        p.append('<text x="%.1f" y="%.1f" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-size="%d"%s>%s</text>'%(cx+w/2,yr+ROWH-7,FS,deco,esc(c)))
        boxes[c]=(cx,yr,w,ROWH); cx+=w
    return p,boxes,tot

def render(fname, cluster):
    tables=cluster["tables"]; order=layout(tables)
    edges=[]
    for t in order:
        for c,role,tgt in tables[t]:
            if not tgt: continue
            dt,dc=(tgt.split(":")+["Id"])[:2] if ":" in tgt else (tgt,"Id")
            if dt in tables: edges.append([t,c,dt,dc,"soft" in role])
    nl=len(edges); x0=BUSPAD+nl*LANE+10
    svg=[]; ytops={}; boxes={}; widths={}; y=TOPM
    for t in order:
        parts,bx,w=build(t,tables[t],x0,y); svg+=parts; ytops[t]=y; boxes[t]=bx; widths[t]=w; y+=HEADH+ROWH+VGAP
    ch=y-VGAP+BOTM; cw=x0+max(widths.values())+RIGHTM
    inc_total={};
    for s,sc,dt,dc,d in edges: inc_total[(dt,dc)]=inc_total.get((dt,dc),0)+1
    inc_idx={}; out_idx={t:0 for t in order}; paths=[]
    for k,(s,sc,dt,dc,dash) in enumerate(edges):
        sb=boxes[s][sc]; db=boxes[dt][dc]
        sxc=sb[0]+sb[2]/2.0; sbot=sb[1]+sb[3]; lane=BUSPAD+k*LANE
        oi=out_idx[s]; out_idx[s]+=1; ty0=sbot+8+oi*STUB
        m=inc_total[(dt,dc)]; ii=inc_idx.get((dt,dc),0); inc_idx[(dt,dc)]=ii+1
        ty=db[1]+ROWH*(ii+1)/(m+1); dx=db[0]
        d="M %.1f %.1f V %.1f H %.1f V %.1f H %.1f"%(sxc,sbot,ty0,lane,ty,dx-1)
        da=' stroke-dasharray="5,3"' if dash else ""
        paths.append('<path d="%s" fill="none" stroke="black" stroke-width="1.1"%s/>'%(d,da))
        paths.append('<polygon points="%.1f,%.1f %.1f,%.1f %.1f,%.1f" fill="black"/>'%(dx,ty,dx-ARROW,ty-4,dx-ARROW,ty+4))
    title=cluster["title"]
    head=('<?xml version="1.0" encoding="UTF-8"?>\n<svg xmlns="http://www.w3.org/2000/svg" width="%.0f" height="%.0f" '
          'viewBox="0 0 %.0f %.0f" font-family="Segoe UI,Arial,sans-serif">\n<rect width="100%%" height="100%%" fill="white"/>'
          '<text x="20" y="26" font-weight="bold" font-size="17">%s</text>'%(cw,ch,cw,ch,esc(title)))
    out=head+"\n"+"\n".join(paths+svg)+"\n</svg>\n"
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT,fname+".svg"),"w",encoding="utf-8") as f: f.write(out)
    print("wrote %s.svg (%d tables, %d FK, %.0fx%.0f)"%(fname,len(tables),nl,cw,ch))

if __name__ == "__main__":
    for fname,c in CLUSTERS.items(): render(fname,c)

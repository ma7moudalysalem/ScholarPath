#!/usr/bin/env python3
"""Emit PlantUML @startchen per module from the verified model (model.py), for
both the plain ERD and the Enhanced EERD. PlantUML's Chen engine (Smetana) does
the layout + crossing-minimisation, so the diagrams come out clean (no tangle) —
unlike straight-line hand routing — while still supporting the full notation:
weak entities (<<weak>> double rectangle), multivalued (<<multi>> double oval),
derived (<<derived>> dashed oval), relationship attributes (M:N), key underline,
and total/partial participation (=/- with the cardinality).

ERD view = plain (single boxes, plain ovals, no weak/multi/derived).
EERD view = enhanced (weak, multi, derived). Composite attributes are flattened
to their leaf columns (Chen @startchen has no nested-composite syntax).
"""
import os
from model import ENTITIES, RELATIONSHIPS, MODULES, MODULE_NO, PROFILE_ROLE_ATTRS

OUTDIR = os.path.dirname(os.path.abspath(__file__))

# The conceptual ER/EER shows the KEY plus a few DEFINING attributes per entity
# (not every column — full columns live in the relational-mapping + class
# diagrams). This keeps every module diagram clean and crossing-free. Keys,
# multivalued and derived attributes are ALWAYS kept (they carry EER notation);
# below are the extra plain attributes worth showing per entity.
SHORTLIST = {
    "USER": ["Email", "AccountStatus"], "ROLE": ["Name"],
    "USER_PROFILE": ["Nationality"], "EDUCATION_ENTRY": ["Degree"],
    "EXPERTISE_TAG": ["Slug"], "REFRESH_TOKEN": ["ExpiresAt"],
    "PASSWORD_RESET_TOKEN": ["ExpiresAt"], "LOGIN_ATTEMPT": ["Succeeded"],
    "UPGRADE_REQUEST": ["Target", "Status"], "UPGRADE_REQUEST_LINK": ["Url"],
    "CATEGORY": ["Slug"], "SCHOLARSHIP": ["Slug", "Status", "Mode", "Deadline"],
    "APPLICATION": ["Status", "Mode"], "DOCUMENT": ["Category"],
    "SCHOLARSHIP_PROVIDER_REVIEW": ["Rating"], "SCHOLARSHIP_PROVIDER_REVIEW_REQUEST": ["Status"],
    "AVAILABILITY": ["IsActive"], "BOOKING": ["Status"],
    "NO_SHOW_REPORT": ["AccusedRole", "Status"], "CONSULTANT_REVIEW": ["Rating"],
    "SESSION_RECORDING": ["RecordingId"], "FORUM_CATEGORY": ["Slug"],
    "FORUM_POST": ["ModerationStatus"], "FORUM_TAG": ["Slug"],
    "RESOURCE": ["Slug", "Type", "Status"], "RESOURCE_CHAPTER": ["TitleEn"],
    "RESOURCE_PROGRESS_CHILD": ["IsCompleted"], "NOTIFICATION": ["Type", "Channel"],
    "NOTIFICATION_PREFERENCE": ["Type", "Channel"], "AI_INTERACTION": ["Feature", "Provider"],
    "RECOMMENDATION_CLICK_EVENT": ["Source"], "AI_REDACTION_AUDIT_SAMPLE": ["Verdict"],
    "KNOWLEDGE_DOCUMENT": ["SourceType", "SourceKey"], "PLATFORM_SETTING": ["SettingKey", "ValueType"],
    "SUCCESS_STORY": ["IsApproved"], "USER_RISK_FLAG": ["IsAtRisk"],
    "AUDIT_LOG": ["Action", "TargetType"], "USER_DATA_REQUEST": ["Type", "Status"],
}
DEFINING_COMPOSITES = {"Name", "Title"}   # composite parents worth one oval

def attr_line(label, kind, view):
    # weak entities, multivalued and derived attributes are all part of the
    # BASIC ER model (Elmasri ch.3) -> shown in BOTH the ERD and the EERD.
    # The EER "enhancement" the diagrams add is the USER specialization only.
    tag = ""
    if kind in ("key", "pkey"):
        tag = " <<key>>"
    elif kind == "multi":
        tag = " <<multi>>"
    elif kind == "derived":
        tag = " <<derived>>"
    return f"  {label}{tag}"

def display_attrs(name, module, view):
    native = ENTITIES[name]["module"]
    attrs = ENTITIES[name]["attrs"]
    if native != module:                       # cross-module hub -> key only
        keys = [a for a in attrs if a[1] == "key"]
        return keys or attrs[:1]
    keep = SHORTLIST.get(name, [])
    out = []
    for label, kind in attrs:
        if isinstance(kind, tuple) and kind[0] == "comp":
            if label in DEFINING_COMPOSITES:   # composite -> one plain oval (parent)
                out.append((label, None))
            continue
        if kind in ("key", "pkey", "multi", "derived"):
            out.append((label, kind))
        elif label in keep:
            out.append((label, None))
    # THE ERD -> EERD difference: in the plain ERD, USER_PROFILE is the FLAT
    # single-table profile carrying every role-specific column; the EERD then
    # refactors these columns onto the USER specialization subclasses (Student /
    # ScholarshipProvider / Consultant), so the lean EERD profile keeps only the
    # shared attributes. This is exactly why the EERD exists.
    if name == "USER_PROFILE" and view == "erd":
        # a representative slice of the role columns from all three roles
        # (enough to show the flat single-table profile without 20+ ovals)
        out += [("AcademicLevel", None), ("Gpa", None), ("PreferredCountries", "multi"),
                ("OrganizationLegalName", None), ("ScholarshipProviderAverageRating", "derived"),
                ("ProfessionalTitle", None), ("ExpertiseTags", "multi"),
                ("ConsultantAverageRating", "derived"), ("ConsultantRatingPenaltyFactor", "derived")]
    return out

def emit(module, view):
    no = MODULE_NO[module]
    rels = [r for r in RELATIONSHIPS if r.get("module") == module]
    names = list(dict.fromkeys(
        [n for n in ENTITIES if ENTITIES[n]["module"] == module]
        + [r["e1"] for r in rels] + [r["e2"] for r in rels]))
    tag = "EERD" if view == "eerd" else "ERD"
    L = [f"@startchen {view}_module_{no}"]
    L.append("skinparam DefaultFontName Segoe UI")
    L.append("skinparam shadowing false")
    L.append("skinparam ranksep 55")
    L.append("skinparam nodesep 35")
    L.append("skinparam titleFontSize 24")
    L.append("skinparam titleFontStyle bold")
    L.append(f"title ScholarPath {tag}  —  Module {no}: {module}")
    for n in names:
        ent = ENTITIES[n]
        weak = ent.get("weak")   # basic ER construct -> double rectangle in BOTH ERD and EERD
        hdr = f"entity {n}" + (" <<weak>>" if weak else "")
        L.append(hdr + " {")
        for label, kind in display_attrs(n, module, view):
            L.append(attr_line(label, kind, view))
        L.append("}")
    for i, r in enumerate(rels):
        L.append(f'relationship {r["name"]} {{')
        for label, kind in (r.get("attrs") or []):
            L.append(f"  {label}")
        L.append("}")
    for r in rels:
        p1, p2 = r["p1"], r["p2"]
        L.append(f'{r["e1"]} {p1}{r["c1"]}{p1} {r["name"]}')
        L.append(f'{r["name"]} {p2}{r["c2"]}{p2} {r["e2"]}')
    L.append("@endchen")
    return "\n".join(L)

def main():
    d = os.path.join(OUTDIR, "chen_gen")
    os.makedirs(d, exist_ok=True)
    for view in ("erd", "eerd"):
        for m in MODULES:
            p = os.path.join(d, f"{view}-module-{MODULE_NO[m]}.puml")
            open(p, "w", encoding="utf-8").write(emit(m, view) + "\n")
            print("wrote", os.path.relpath(p, OUTDIR))

if __name__ == "__main__":
    main()

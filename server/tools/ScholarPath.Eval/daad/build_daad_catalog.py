"""Build an evaluation catalog from the DAAD scholarship database.

    python -I build_daad_catalog.py <download-dir> <out.sql> [--as-of 2026-10-27]

Downloads the public data files behind the DAAD scholarship database
(www2.daad.de, "Scholarship Database"), keeps only facts about each programme
(name, the academic levels it funds, its subject groups, the destination, the
application deadline stated for applicants from Egypt or, failing that, any
country) and writes a SQL script that replaces the listings of a copy of the
evaluation database with them.

The programme descriptions are not copied: DAAD's texts are protected by
copyright, and the evaluation needs only the facts the eligibility checker
reads. Neither the downloaded files nor the generated SQL are committed; the
script is, so the catalog can be rebuilt by anyone from the live source.

Mapping decisions (each is printed in the summary):
  * one listing per (programme, academic level), since a ScholarPath listing
    targets a single level; faculty-only programmes have no level and are left out;
  * a programme open to all seven DAAD subject groups is unrestricted ([]); otherwise
    each group maps to the ScholarPath categories the German subject
    classification places under it;
  * the destination is Germany for every programme;
  * the nationalities a programme admits are listed by English name when DAAD
    restricts them, and left empty when every country DAAD lists is admitted;
  * DAAD states that deadlines recur in the same period each year, so a stated
    date that has passed is moved forward by whole years; a programme with no
    stated date (rolling) is given one year from the as-of date;
  * the award amount is not a structured field and is left empty.
"""
import datetime as dt
import json
import re
import sys
import urllib.request
import uuid
from collections import Counter
from pathlib import Path

BASE = "https://www2.daad.de/bundles/daadstipendiendatenbanklsh/data/a/js/"
FILES = ["scholarships", "deadlines", "origin", "status", "subjectgroups", "intentions"]
UA = "ScholarPath evaluation (Cairo University graduation project)"

# DAAD status id -> ScholarPath AcademicLevel; 5 (Faculty) has no counterpart.
LEVEL = {1: "Undergrad", 3: "Masters", 4: "PhD", 2: "PostDoc"}

# DAAD subject group -> ScholarPath field categories.
GROUP = {
    "A": ["Arts & Humanities"],
    "B": ["Law", "Economics & Finance", "Social Sciences", "Business & Management",
          "Education", "Media & Communications"],
    "C": ["Mathematics & Statistics", "Natural Sciences", "Computer Science"],
    "D": ["Medicine & Health"],
    "E": ["Agriculture & Environment"],
    "F": ["Engineering", "Architecture & Design"],
    "G": ["Arts & Humanities", "Architecture & Design"],
}
EGYPT = "2"
NS = uuid.UUID("6f1c2a7e-3d0b-4b8e-9a51-daad00000000")


def load(path):
    s = path.read_text(encoding="utf-8")
    return json.loads(s[s.index("TAFFY(") + 6: s.rindex(")")])


def dates(text):
    for d, m, y in re.findall(r"\b(\d{2})\.(\d{2})\.(\d{4})\b", text or ""):
        try:
            yield dt.date(int(y), int(m), int(d))
        except ValueError:
            pass


def roll(d, as_of):
    while d <= as_of:
        d = d.replace(year=d.year + 1) if not (d.month == 2 and d.day == 29) else dt.date(d.year + 1, 2, 28)
    return d


def q(s):
    return "NULL" if s is None else "N'" + str(s).replace("'", "''") + "'"


def main():
    out_dir, out_sql = Path(sys.argv[1]), Path(sys.argv[2])
    as_of = dt.date.fromisoformat(sys.argv[sys.argv.index("--as-of") + 1]) if "--as-of" in sys.argv else dt.date.today()
    out_dir.mkdir(parents=True, exist_ok=True)
    for f in FILES:
        p = out_dir / f"{f}.js"
        if not p.exists():
            req = urllib.request.Request(BASE + f + ".js", headers={"User-Agent": UA})
            p.write_bytes(urllib.request.urlopen(req, timeout=60).read())

    programmes = load(out_dir / "scholarships.js")
    deadlines = {d["id"]: d for d in load(out_dir / "deadlines.js")}
    origin = {o["id"]: o["nameEn"] for o in load(out_dir / "origin.js")}

    rows, stats = [], Counter()
    for p in programmes:
        levels = [LEVEL[s] for s in p["status"] if s in LEVEL]
        if not levels:
            stats["faculty-only, left out"] += 1
            continue
        groups = sorted(set(p["subjectGrps"]))
        fields = [] if len(groups) == 7 else sorted({f for g in groups for f in GROUP[g]})
        stats["unrestricted field" if not fields else "restricted field"] += 1
        stats["open to Egypt" if 2 in p["origin"] else "closed to Egypt"] += 1
        restricted = not set(origin) <= set(p["origin"])
        nationalities = json.dumps(sorted(origin[i] for i in p["origin"] if i in origin), ensure_ascii=False) if restricted else None
        stats["nationality restricted" if restricted else "any nationality"] += 1

        dl = deadlines.get(p["sapProgid"], {"general": {"en": ""}, "countries": []})
        countries = dl["countries"] if isinstance(dl["countries"], dict) else {}
        stated = [d for e in countries.get(EGYPT, []) for d in dates(e.get("en"))]
        if not stated:
            stated = [d for v in countries.values() for e in v for d in dates(e.get("en"))]
            stated += list(dates(dl["general"].get("en")))
        if stated:
            deadline = min(roll(d, as_of) for d in stated)
            stats["stated deadline"] += 1
        else:
            deadline = as_of + dt.timedelta(days=365)
            stats["rolling (no stated date)"] += 1

        for level in levels:
            rows.append(dict(
                id=str(uuid.uuid5(NS, f"{p['sapProgid']}|{level}")),
                title=p["nameEn"].strip(),
                slug=f"daad-{p['sapProgid']}-{level.lower()}",
                level=level, fields=json.dumps(fields, ensure_ascii=False),
                deadline=deadline.isoformat(), daad=bool(p["isDaad"]), nationalities=nationalities,
            ))
            stats[f"level {level}"] += 1

    now = dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds")
    # The copy keeps its users and profiles; everything hanging off the synthetic
    # listings goes with them. Constraints are suspended for the delete because
    # the application tables reference each other in a chain the harness never reads.
    sql = ["SET NOCOUNT ON;", "SET QUOTED_IDENTIFIER ON;", "SET XACT_ABORT ON;", "BEGIN TRAN;",
           "EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';",
           "DELETE FROM RecommendationClickEvents;", "DELETE FROM ScholarshipProviderReviewRequests;",
           "DELETE FROM ScholarshipProviderReviews;", "DELETE FROM Applications;", "DELETE FROM SavedScholarships;",
           "DELETE FROM ScholarshipChildren;", "DELETE FROM KnowledgeDocuments;", "DELETE FROM Scholarships;"]
    for r in rows:
        desc = f"{'DAAD' if r['daad'] else 'Other funder listed by DAAD'} programme; facts imported from the DAAD scholarship database."
        sql.append(
            "INSERT INTO Scholarships (Id,TitleEn,TitleAr,DescriptionEn,DescriptionAr,Slug,Mode,Status,Deadline,OpenedAt,"
            "IsFeatured,FeaturedOrder,FundingType,FundingAmountUsd,Currency,TargetLevel,TargetCountriesJson,FieldsOfStudyJson,"
            "EligibleNationalitiesJson,IsDeleted,CreatedAt) VALUES ("
            f"'{r['id']}',{q(r['title'])},{q(r['title'])},{q(desc)},{q(desc)},{q(r['slug'])},N'ExternalUrl',N'Open',"
            f"'{r['deadline']}T23:59:59+00:00','{now}',0,0,N'Other',NULL,N'EUR',N'{r['level']}',N'[\"DE\"]',{q(r['fields'])},{q(r['nationalities'])},0,'{now}');")
    sql.append("EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL';")
    sql.append("COMMIT;")
    out_sql.write_text("\n".join(sql), encoding="utf-8")

    print(f"programmes : {len(programmes)}  ->  listings: {len(rows)}  (as of {as_of})")
    for k, v in sorted(stats.items()):
        print(f"  {k:<28}{v}")


if __name__ == "__main__":
    main()

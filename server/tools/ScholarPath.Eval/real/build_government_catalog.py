"""Append the hand-checked government scholarships to an evaluation catalog.

    python -I build_government_catalog.py government_scholarships.json <out.sql> [--as-of 2026-10-27]

The facts in the JSON were read from each programme's official site (the
`source` field); this script only turns them into one listing per level.
Run its SQL after build_daad_catalog.py's on the same database copy.

Stipendium Hungaricum is listed at the levels its partners page offers
applicants from Egypt (bachelor's and doctoral); the scheme's other partner
countries are not enumerated, so nationality is left open.
"""
import datetime as dt
import json
import re
import sys
import uuid
from pathlib import Path

NS = uuid.UUID("6f1c2a7e-3d0b-4b8e-9a51-90e000000000")


def q(s):
    return "NULL" if s is None else "N'" + str(s).replace("'", "''") + "'"


def main():
    src, out_sql = Path(sys.argv[1]), Path(sys.argv[2])
    as_of = dt.date.fromisoformat(sys.argv[sys.argv.index("--as-of") + 1]) if "--as-of" in sys.argv else dt.date.today()
    now = dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds")
    sql, n = ["SET NOCOUNT ON;", "SET QUOTED_IDENTIFIER ON;", "SET XACT_ABORT ON;", "BEGIN TRAN;"], 0
    for p in json.loads(src.read_text(encoding="utf-8"))["programmes"]:
        deadline = dt.date.fromisoformat(p["deadline"]) if p["deadline"] else as_of + dt.timedelta(days=365)
        nat = json.dumps(p["nationalities"], ensure_ascii=False) if p["nationalities"] else None
        desc = "Government scholarship; facts read from the official programme site."
        for level in p["levels"]:
            sid = uuid.uuid5(NS, f"{p['title']}|{level}")
            slug = "gov-" + re.sub(r"[^a-z0-9]+", "-", p["title"].lower()).strip("-") + "-" + level.lower()
            sql.append(
                "INSERT INTO Scholarships (Id,TitleEn,TitleAr,DescriptionEn,DescriptionAr,Slug,Mode,Status,Deadline,OpenedAt,"
                "IsFeatured,FeaturedOrder,FundingType,FundingAmountUsd,Currency,TargetLevel,TargetCountriesJson,FieldsOfStudyJson,"
                "EligibleNationalitiesJson,ExternalApplicationUrl,IsDeleted,CreatedAt) VALUES ("
                f"'{sid}',{q(p['title'])},{q(p['title'])},{q(desc)},{q(desc)},{q(slug)},N'ExternalUrl',N'Open',"
                f"'{deadline.isoformat()}T23:59:59+00:00','{now}',0,0,N'Other',NULL,N'USD',N'{level}',"
                f"{q(json.dumps(p['destination']))},{q(json.dumps(p['fields']))},{q(nat)},{q(p['source'])},0,'{now}');")
            n += 1
    sql.append("COMMIT;")
    out_sql.write_text("\n".join(sql), encoding="utf-8")
    print(f"government listings: {n}")


if __name__ == "__main__":
    main()

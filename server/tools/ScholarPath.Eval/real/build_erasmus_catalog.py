"""Add the Erasmus Mundus Joint Masters to an evaluation catalog.

    python -I build_erasmus_catalog.py <download-dir> <out.sql> [--as-of 2026-10-27]

Reads the Erasmus Mundus Catalogue published by the European Education and
Culture Executive Agency (www.eacea.ec.europa.eu, "Erasmus Mundus Catalogue")
and keeps, for each programme, its title, its official website and the
countries its consortium teaches in (the catalogue's own country filter).
Commission content may be reused with acknowledgement (Decision 2011/833/EU);
only these facts are taken, and the downloaded pages are not committed.

The generated SQL *appends* to the listings already in the database, so it is
run after build_daad_catalog.py's script on the same copy.

Mapping decisions (each is printed in the summary):
  * every programme is a master's, open to applicants of any nationality;
  * the destination is the list of consortium countries;
  * the fields are inferred from the programme title with the keyword table
    below; a title that matches none is left unrestricted ([]) and counted;
  * the catalogue states that applications run from October to January, so
    the deadline is 15 January following the as-of date (stated as an
    assumption, not a per-programme date).
"""
import datetime as dt
import html
import json
import re
import sys
import time
import urllib.parse
import urllib.request
import uuid
from collections import Counter
from pathlib import Path

URL = "https://www.eacea.ec.europa.eu/scholarships/erasmus-mundus-catalogue_en"
UA = "ScholarPath evaluation (Cairo University graduation project)"
NS = uuid.UUID("6f1c2a7e-3d0b-4b8e-9a51-e7a500000000")
CARD = re.compile(r'ecl-content-block__title"><a\s+href="([^"]+)".*?class="ecl-link__label">([^<]+)</span>', re.S)
OPTION = re.compile(r'<option\s+value="([A-Z]{2})"\s*>([^<]+)</option>')

# Title keyword -> canonical field. Checked in order; a title may match several.
KEYWORDS = [
    (r"\b(data|software|comput|cyber|informat|artificial intelligence|machine learning|digital|robot)", "Computer Science"),
    (r"\b(engineer|mechatron|aero|aeronaut|electric|power system|photonic|microelectronic|materials|nuclear|transport|manufactur|construction|naval|marine techn|space)", "Engineering"),
    (r"\b(mathemat|statist|quantum)", "Mathematics & Statistics"),
    (r"\b(physic|chemi|biolog|bio|astro|geo|ocean|marine|nano|molecul|neuro)", "Natural Sciences"),
    (r"\b(health|medic|pharma|nurs|epidem|disease|nutrition|public health|vaccin)", "Medicine & Health"),
    (r"\b(agri|food|forest|environment|climate|sustainab|water|ecolog|soil|insect|renewable|energy|biodivers|fish)", "Agriculture & Environment"),
    (r"\b(econom|financ|bank)", "Economics & Finance"),
    (r"\b(business|management|entrepreneur|innovation|leadership|work)", "Business & Management"),
    (r"\b(law|legal|rights|justice|gender)", "Law"),
    (r"\b(politic|polic|societ|social|migration|peace|security|international|europe|development|urban|psycholog|anthropolog|governance|human)", "Social Sciences"),
    (r"\b(educat|teach|learning)", "Education"),
    (r"\b(media|journalism|communicat|film|cinema)", "Media & Communications"),
    (r"\b(architect|design|heritage|landscape|earthen)", "Architecture & Design"),
    (r"\b(histor|philosoph|literat|language|linguist|multilingual|culture|cultural|arts?\b|music|classic|theolog|archaeolog|translation)", "Arts & Humanities"),
    (r"\b(sport)", "Education"),
]


def get(url, cache):
    p = cache / (re.sub(r"[^A-Za-z0-9]+", "_", url[len(URL):]) or "_root")
    p = p.with_suffix(".html")
    if not p.exists():
        req = urllib.request.Request(url, headers={"User-Agent": UA})
        for wait in (30, 60, 120, 240, 0):
            try:
                p.write_bytes(urllib.request.urlopen(req, timeout=60).read())
                break
            except urllib.error.HTTPError as e:
                if e.code != 429 or not wait:
                    raise
                time.sleep(wait)
        time.sleep(3)
    return p.read_text(encoding="utf-8")


def crawl(cache, query=""):
    found, page = {}, 0
    while True:
        sep = "&" if query else "?"
        text = get(URL + query + (f"{sep}page={page}" if page else ""), cache)
        cards = CARD.findall(text)
        new = {html.unescape(t).strip(): u for u, t in cards}
        if not new or set(new) <= set(found):
            return found
        found.update(new)
        page += 1


def fields_of(title):
    t = title.lower()
    return sorted({f for pat, f in KEYWORDS if re.search(pat, t)})


def q(s):
    return "NULL" if s is None else "N'" + str(s).replace("'", "''") + "'"


def main():
    cache, out_sql = Path(sys.argv[1]), Path(sys.argv[2])
    as_of = dt.date.fromisoformat(sys.argv[sys.argv.index("--as-of") + 1]) if "--as-of" in sys.argv else dt.date.today()
    cache.mkdir(parents=True, exist_ok=True)

    root = get(URL, cache)
    countries = dict(OPTION.findall(root))
    programmes = crawl(cache)
    where = {t: set() for t in programmes}
    for code in countries:
        query = "?" + urllib.parse.urlencode({"f[0]": f"oe_project_locations_country:{code}"})
        for title in crawl(cache, query):
            where.setdefault(title, set()).add(code)

    deadline = dt.date(as_of.year + (1 if as_of >= dt.date(as_of.year, 1, 15) else 0), 1, 15)
    now = dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds")
    stats, sql = Counter(), ["SET NOCOUNT ON;", "SET QUOTED_IDENTIFIER ON;", "SET XACT_ABORT ON;", "BEGIN TRAN;"]
    for title, site in sorted(programmes.items()):
        fields = fields_of(title)
        dest = sorted(where.get(title, ()))
        stats["fields inferred" if fields else "no field keyword (unrestricted)"] += 1
        stats["countries known" if dest else "countries unknown"] += 1
        sid = uuid.uuid5(NS, title)
        slug = "em-" + re.sub(r"[^a-z0-9]+", "-", title.lower()).strip("-")[:80] + "-" + sid.hex[:6]
        desc = "Erasmus Mundus Joint Master; facts imported from the EACEA Erasmus Mundus Catalogue."
        sql.append(
            "INSERT INTO Scholarships (Id,TitleEn,TitleAr,DescriptionEn,DescriptionAr,Slug,Mode,Status,Deadline,OpenedAt,"
            "IsFeatured,FeaturedOrder,FundingType,FundingAmountUsd,Currency,TargetLevel,TargetCountriesJson,FieldsOfStudyJson,"
            "EligibleNationalitiesJson,ExternalApplicationUrl,IsDeleted,CreatedAt) VALUES ("
            f"'{sid}',{q(title)},{q(title)},{q(desc)},{q(desc)},{q(slug)},N'ExternalUrl',N'Open',"
            f"'{deadline.isoformat()}T23:59:59+00:00','{now}',0,0,N'Other',NULL,N'EUR',N'Masters',"
            f"{q(json.dumps(dest))},{q(json.dumps(fields))},NULL,{q(site)},0,'{now}');")
    sql.append("COMMIT;")
    out_sql.write_text("\n".join(sql), encoding="utf-8")

    print(f"programmes : {len(programmes)}  countries in filter: {len(countries)}  deadline: {deadline}")
    for k, v in sorted(stats.items()):
        print(f"  {k:<34}{v}")


if __name__ == "__main__":
    main()

"""Check the platform's eligibility verdicts against DAAD's own published data.

    python -I external_check.py <daad-download-dir> <verdicts.tsv>

The reference is computed here, independently of the platform: from DAAD's raw
data files (scholarships.js) and nothing else, a student meets a DAAD
programme's level criterion when the programme's status list contains the
student's level, and its nationality criterion when the programme's origin list
contains the student's country. verdicts.tsv is written by the harness
(`--mode verdicts`): one line per (student, DAAD listing) with the student's level
and nationality and the platform's outcome for each criterion. The script
reports agreement per criterion and on the combined level-and-nationality
decision, with the disagreements listed.
"""
import json
import sys

LEVEL = {1: "Undergrad", 3: "Masters", 4: "PhD", 2: "PostDoc"}
ORIGIN = {"EG": 2, "MA": 188, "SA": 95, "DZ": 186, "JO": 89, "TN": 189, "LB": 92,
          "PS": 63, "OM": 93, "MR": 158, "QA": 90, "BH": 87}


def main():
    s = open(f"{sys.argv[1]}/scholarships.js", encoding="utf-8").read()
    progs = {str(p["sapProgid"]): p for p in json.loads(s[s.index("TAFFY(") + 6: s.rindex(")")])}
    agree = {"level": [0, 0], "nationality": [0, 0], "both": [0, 0]}
    diffs = []
    for line in open(sys.argv[2], encoding="utf-8-sig"):
        f = line.rstrip("\n").split("\t")
        if len(f) < 6 or not f[1].startswith("daad-"):
            continue
        user, slug, level, nat, lv_platform, nat_platform = f[:6]
        progid = slug.split("-")[1]
        p = progs.get(progid)
        if p is None or nat not in ORIGIN:
            continue
        listing_level = slug.split("-")[2]
        ref_level = level.lower() == listing_level.lower() and listing_level in {v.lower() for k, v in LEVEL.items() if k in p["status"]}
        ref_nat = ORIGIN[nat] in p["origin"]
        got_level = lv_platform == "yes"
        got_nat = nat_platform in ("yes", "none")  # "none": the listing restricts no nationality
        for key, ref, got in (("level", ref_level, got_level), ("nationality", ref_nat, got_nat),
                              ("both", ref_level and ref_nat, got_level and got_nat)):
            agree[key][0] += ref == got
            agree[key][1] += 1
        if (ref_level, ref_nat) != (got_level, got_nat):
            diffs.append((slug, level, nat, ref_level, got_level, ref_nat, got_nat))
    for k, (a, n) in agree.items():
        print(f"{k:12} agreement {a}/{n} ({100 * a / n:.2f}%)")
    print(f"disagreements: {len(diffs)}")
    for d in diffs[:20]:
        print("  ", d)


if __name__ == "__main__":
    main()

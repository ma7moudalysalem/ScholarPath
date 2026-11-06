"""Measure how well a language model extracts structured eligibility from text.

    python -I extract_eval.py <download-dir>

Uses the DAAD scholarship database (see daad/build_daad_catalog.py): each
programme's English introduction is the input, and the structured facts DAAD
publishes for the same programme are the ground truth: the academic levels it
funds, its subject groups (mapped to ScholarPath's fields), and whether
applicants from Egypt are admitted. The model named in AZ_OAI_EXTRACT (default
gpt-6.1-sol) reads only the text. The texts are read in memory and not stored.

Reports, over programmes with an introduction: exact match and micro precision/
recall of levels, micro precision/recall of fields, and accuracy on Egypt where
the model states an answer, with how often it says "not stated".
"""
import json
import os
import re
import sys
import time
import urllib.request

LEVEL = {1: "Undergrad", 3: "Masters", 4: "PhD", 2: "PostDoc"}
GROUP = {
    "A": ["Arts & Humanities"],
    "B": ["Law", "Economics & Finance", "Social Sciences", "Business & Management", "Education", "Media & Communications"],
    "C": ["Mathematics & Statistics", "Natural Sciences", "Computer Science"],
    "D": ["Medicine & Health"], "E": ["Agriculture & Environment"],
    "F": ["Engineering", "Architecture & Design"], "G": ["Arts & Humanities", "Architecture & Design"],
}
FIELDS = sorted({f for v in GROUP.values() for f in v})
EGYPT = 2
DEP = os.environ.get("AZ_OAI_EXTRACT", "gpt-6.1-sol")
URL = f"{os.environ['AZ_OAI_ENDPOINT'].rstrip('/')}/openai/deployments/{DEP}/chat/completions?api-version=2025-04-01-preview"
SYSTEM = ("Extract structured eligibility from a scholarship programme description. Reply ONLY with JSON "
          '{"levels":[...],"fields":[...],"open_to_egypt":"yes"|"no"|"not stated"}. '
          "levels: any of Undergrad, Masters, PhD, PostDoc that the programme funds (PostDoc = postdoctoral researchers). "
          f"fields: the subject areas it accepts, from exactly this list: {FIELDS}; use [] if it accepts all subjects. "
          "open_to_egypt: whether citizens of Egypt may apply, only if the text says so.")


def ask(text):
    body = json.dumps({"messages": [{"role": "system", "content": SYSTEM}, {"role": "user", "content": text}],
                       "max_completion_tokens": 4000, "response_format": {"type": "json_object"}}).encode()
    for attempt in range(6):
        req = urllib.request.Request(URL, data=body, headers={"api-key": os.environ["AZ_OAI_KEY"], "Content-Type": "application/json"})
        try:
            r = json.load(urllib.request.urlopen(req, timeout=300))
            return json.loads(r["choices"][0]["message"]["content"])
        except urllib.error.HTTPError as e:
            if e.code not in (429, 500, 503):
                raise
        except TimeoutError:
            pass
        time.sleep(10 * (attempt + 1))
    raise RuntimeError("model unavailable")


def main():
    s = open(os.path.join(sys.argv[1], "scholarships.js"), encoding="utf-8").read()
    progs = json.loads(s[s.index("TAFFY(") + 6: s.rindex(")")])
    lv_tp = lv_fp = lv_fn = lv_exact = 0
    f_tp = f_fp = f_fn = 0
    eg_right = eg_answered = eg_total = 0
    n = 0
    for p in progs:
        intro = (p.get("introduction") or {}).get("en") or ""
        text = re.sub(r"<[^>]+>", " ", intro).strip()
        levels = {LEVEL[x] for x in p["status"] if x in LEVEL}
        if len(text) < 80 or not levels:
            continue
        n += 1
        got = ask(f"{p['nameEn']}\n\n{text}")
        gl = set(got.get("levels") or []) & set(LEVEL.values())
        lv_tp += len(gl & levels); lv_fp += len(gl - levels); lv_fn += len(levels - gl)
        lv_exact += gl == levels
        groups = sorted(set(p["subjectGrps"]))
        truth_f = set() if len(groups) == 7 else {f for g in groups for f in GROUP[g]}
        gf = set(got.get("fields") or []) & set(FIELDS)
        # "all subjects" on either side is compared as the full list
        tf, pf = (truth_f or set(FIELDS)), (gf or set(FIELDS))
        f_tp += len(pf & tf); f_fp += len(pf - tf); f_fn += len(tf - pf)
        eg_total += 1
        ans = str(got.get("open_to_egypt", "not stated")).lower()
        if ans in ("yes", "no"):
            eg_answered += 1
            eg_right += (ans == "yes") == (EGYPT in p["origin"])
        print(f"{n}", end=" ", flush=True)
    print()
    pr = lambda tp, fp: tp / (tp + fp) if tp + fp else 0
    print(f"programmes with an English introduction and a level: {n}")
    print(f"levels : exact {lv_exact}/{n} ({100 * lv_exact / n:.1f}%), precision {100 * pr(lv_tp, lv_fp):.1f}%, recall {100 * pr(lv_tp, lv_fn):.1f}%")
    print(f"fields : precision {100 * pr(f_tp, f_fp):.1f}%, recall {100 * pr(f_tp, f_fn):.1f}%")
    print(f"egypt  : answered {eg_answered}/{eg_total} ({100 * eg_answered / eg_total:.1f}%), "
          f"correct {eg_right}/{eg_answered} ({100 * eg_right / max(1, eg_answered):.1f}%) when answered")


if __name__ == "__main__":
    main()

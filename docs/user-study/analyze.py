# -*- coding: utf-8 -*-
"""Compute every figure the ScholarPath user study reports.

    python docs/user-study/analyze.py docs/user-study/responses.csv

This script is written before the first session and is not edited afterwards,
so that the way a number is computed cannot be adjusted once the number is
visible. Run it against the empty template to see the shape of the result.

It will not compute a usability result from a row whose `role` is `author`. A
score given by someone who built the system measures their familiarity with it,
not the system, and pooling such a row with participant data would misreport
the study to a reader. Pilot rows are kept in a separate file and are read only
with --pilot, which prints task timings for checking the script and refuses to
print a usability score at all.
"""
import argparse
import csv
import statistics
import sys

TASKS = [
    ("t1", "Find an award"),
    ("t2", "Judge eligibility"),
    ("t3", "Get a document ready"),
    ("t4", "Apply"),
    ("t5", "Find out what happened"),
    ("t6", "Ask a question"),
]

OUTCOMES = ("completed", "completed_with_help", "failed")

# Brooke (1996): odd items score-1, even items 5-score, sum x 2.5.
SUS_ITEMS = 10


def sus_score(row):
    """One participant's scale score, or None if any item is missing."""
    values = []
    for i in range(1, SUS_ITEMS + 1):
        raw = (row.get("sus%d" % i) or "").strip()
        if not raw:
            return None
        v = int(raw)
        if not 1 <= v <= 5:
            raise ValueError("participant %s: sus%d is %d, expected 1-5"
                             % (row.get("participant", "?"), i, v))
        values.append(v - 1 if i % 2 else 5 - v)
    return sum(values) * 2.5


def band(score):
    if score < 51:
        return "poor"
    if score < 68:
        return "fair"
    if score < 69:
        return "average"
    if score < 80:
        return "good"
    return "excellent"


def read(path):
    with open(path, newline="", encoding="utf-8-sig") as fh:
        return [r for r in csv.DictReader(fh) if (r.get("participant") or "").strip()]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("csv_path")
    ap.add_argument("--pilot", action="store_true",
                    help="read a pilot file: timings only, no usability score")
    args = ap.parse_args()

    rows = read(args.csv_path)
    authors = [r for r in rows if (r.get("role") or "").strip().lower() == "author"]

    if authors and not args.pilot:
        sys.stderr.write(
            "refusing to analyze: %d row(s) have role=author (%s).\n"
            "An author's rating is not evidence about the system. Move these rows to\n"
            "a pilot file and read it with --pilot, or correct the role column.\n"
            % (len(authors), ", ".join(r["participant"] for r in authors)))
        return 2

    if args.pilot:
        rows = authors or rows
        print("PILOT DATA - instrument check only. Not a result. Not for publication.\n")

    n = len(rows)
    print("participants: %d  (source: %s)\n" % (n, args.csv_path))
    if n == 0:
        print("No rows yet. The tables below are the ones the study will report.\n")

    # ── per task ────────────────────────────────────────────────────────────
    print("%-26s %10s %12s %12s" % ("Task", "Completed", "Unaided", "Median time"))
    print("-" * 64)
    for key, label in TASKS:
        outcomes = [(r.get("%s_outcome" % key) or "").strip().lower() for r in rows]
        outcomes = [o for o in outcomes if o]
        for o in outcomes:
            if o not in OUTCOMES:
                raise ValueError("%s: unknown outcome %r" % (key, o))
        times = [float(r["%s_seconds" % key]) for r in rows
                 if (r.get("%s_seconds" % key) or "").strip()]
        done = sum(1 for o in outcomes if o != "failed")
        unaided = sum(1 for o in outcomes if o == "completed")
        pct = (lambda c: "%.0f%%" % (100.0 * c / len(outcomes))) if outcomes else (lambda c: "-")
        median = "%.0f s" % statistics.median(times) if times else "-"
        print("%-26s %10s %12s %12s" % (label[:26], pct(done), pct(unaided), median))
    print()

    # ── usability ───────────────────────────────────────────────────────────
    if args.pilot:
        print("Usability score withheld: pilot data does not measure usability.")
        return 0

    scores = [s for s in (sus_score(r) for r in rows) if s is not None]
    if not scores:
        print("System Usability Scale: no complete response sheets yet.")
        print("  (0-100, not a percentage. Average across published studies is 68.)")
        return 0

    mean = statistics.mean(scores)
    sd = statistics.stdev(scores) if len(scores) > 1 else 0.0
    print("System Usability Scale (0-100, not a percentage)")
    print("  complete sheets : %d of %d" % (len(scores), n))
    print("  mean            : %.1f  (sd %.1f)" % (mean, sd))
    print("  median          : %.1f" % statistics.median(scores))
    print("  range           : %.1f to %.1f" % (min(scores), max(scores)))
    print("  reading         : %s  (average across studies is 68)" % band(mean))
    print()
    print("  distribution")
    for label in ("poor", "fair", "average", "good", "excellent"):
        c = sum(1 for s in scores if band(s) == label)
        print("    %-10s %2d  %s" % (label, c, "#" * c))
    return 0


if __name__ == "__main__":
    sys.exit(main())

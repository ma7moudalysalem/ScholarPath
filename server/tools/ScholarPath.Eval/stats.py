"""Confidence intervals and paired tests for the recommendation results.

    python -I stats.py <current.csv> <intermediate.csv> <legacy.csv>

Each CSV is written by `--mode recommendation --csv <path>` for one ranker on
the same sample (same seed and database). Students are the unit of resampling:
precision@5 is eligible slots over filled slots, recall@5 is the share of
students with an eligible listing anywhere whose top five holds one.
Intervals are percentile bootstraps (10,000 resamples, seed 20260918). The
precision difference between two rankers is bootstrapped on paired students;
the recall difference is tested with an exact McNemar test. p-values are
Holm-adjusted over the four tests (two rules, precision and recall).
"""
import csv
import random
import sys
from math import comb

USABLE = {"Eligible", "PartiallyEligible"}


def load(path):
    slots, oracle = {}, {}
    with open(path, newline="", encoding="utf-8") as f:
        for r in csv.DictReader(f):
            u, arm = r["userId"], r["arm"]
            if arm == "recommended":
                s = slots.setdefault(u, [0, 0])
                s[0] += r["verdict"] in USABLE
                s[1] += 1
            elif arm == "oracle":
                oracle[u] = int(r["matchScore"])
    return slots, oracle


def precision(students, slots):
    num = sum(slots.get(u, [0, 0])[0] for u in students)
    den = sum(slots.get(u, [0, 0])[1] for u in students)
    return num / den if den else 0.0


def found(u, slots):
    return slots.get(u, [0, 0])[0] > 0


def recall(students, slots, oracle):
    has = [u for u in students if oracle.get(u, 0) > 0]
    return sum(found(u, slots) for u in has) / len(has) if has else 0.0


def boot(stat, students, n=10_000, seed=20260918):
    rng = random.Random(seed)
    vals = sorted(stat([rng.choice(students) for _ in students]) for _ in range(n))
    return vals[int(0.025 * n)], vals[int(0.975 * n) - 1]


def mcnemar(b, c):
    n = b + c
    if n == 0:
        return 1.0
    k = min(b, c)
    p = 2 * sum(comb(n, i) for i in range(k + 1)) / 2 ** n
    return min(1.0, p)


def main():
    names = ["fixed-maximum", "proportional", "additive"]
    runs = [load(p) for p in sys.argv[1:4]]
    students = sorted(runs[0][1])
    oracle = runs[0][1]
    for name, (slots, _) in zip(names, runs):
        p = precision(students, slots)
        lo, hi = boot(lambda s: precision(s, slots), students)
        r = recall(students, slots, oracle)
        rlo, rhi = boot(lambda s: recall(s, slots, oracle), students)
        print(f"{name:14} precision@5 {100*p:5.1f}% [{100*lo:5.1f}, {100*hi:5.1f}]   recall@5 {100*r:5.1f}% [{100*rlo:5.1f}, {100*rhi:5.1f}]")
    base = runs[0][0]
    rows = []
    for name, (slots, _) in zip(names[1:], runs[1:]):
        diff = lambda s: precision(s, base) - precision(s, slots)
        d = diff(students)
        lo, hi = boot(diff, students)
        # two-sided bootstrap p-value for the precision difference
        rng = random.Random(20260918)
        reps = [diff([rng.choice(students) for _ in students]) for _ in range(10_000)]
        p_prec = min(1.0, 2 * min(sum(r <= 0 for r in reps), sum(r >= 0 for r in reps)) / len(reps))
        has = [u for u in students if oracle.get(u, 0) > 0]
        b = sum(found(u, base) and not found(u, slots) for u in has)
        c = sum(found(u, slots) and not found(u, base) for u in has)
        eligible_base = sum(slots_ok for slots_ok in (base.get(u, [0, 0])[0] for u in students))
        eligible_other = sum(slots.get(u, [0, 0])[0] for u in students)
        rows.append((name, d, lo, hi, p_prec, b, c, mcnemar(b, c), eligible_base, eligible_other))
    # Holm correction over the family of tests (two precision, two recall)
    ps = sorted([(r[4], i, 'prec') for i, r in enumerate(rows)] + [(r[7], i, 'rec') for i, r in enumerate(rows)])
    adj, running = {}, 0.0
    for k, (pv, i, kind) in enumerate(ps):
        running = max(running, min(1.0, (len(ps) - k) * pv))
        adj[(i, kind)] = running
    for i, (name, d, lo, hi, pp, b, c, pm, eb, eo) in enumerate(rows):
        print(f"fixed-maximum minus {name:12}: eligible shown {eb} vs {eo}; precision {100*d:+5.1f} points [{100*lo:+5.1f}, {100*hi:+5.1f}] "
              f"p={pp:.4f} (Holm {adj[(i, 'prec')]:.4f}); recall McNemar b={b} c={c} p={pm:.4f} (Holm {adj[(i, 'rec')]:.4f})")


if __name__ == "__main__":
    main()

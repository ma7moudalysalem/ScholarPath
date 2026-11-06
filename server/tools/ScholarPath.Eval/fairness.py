"""Recommendation quality by student nationality.

    python -I fairness.py <recommendation.csv> <nationalities.tsv>

recommendation.csv is written by `--mode recommendation --csv`; nationalities.tsv
holds userId<TAB>nationality (UserProfiles). For each nationality: students,
precision@5, the attainable precision (each student can fill at most as many
slots as the catalog holds eligible listings for them), the share of that
ceiling reached, and recall@5. The ceiling separates what the catalog offers a
nationality from how well the ranker uses it.
"""
import csv
import sys
from collections import defaultdict

USABLE = {"Eligible", "PartiallyEligible"}


def main():
    nat = dict(line.rstrip("\n").split("\t")[:2] for line in open(sys.argv[2], encoding="utf-8") if "\t" in line)
    slots, good, oracle = defaultdict(int), defaultdict(int), {}
    for r in csv.DictReader(open(sys.argv[1], encoding="utf-8")):
        u = r["userId"].lower()
        if r["arm"] == "recommended":
            slots[u] += 1
            good[u] += r["verdict"] in USABLE
        elif r["arm"] == "oracle":
            oracle[u] = int(r["matchScore"])
    groups = defaultdict(list)
    for u in oracle:
        groups[nat.get(u, "?")].append(u)
    print(f"{'nationality':12}{'students':>9}{'eligible/slots':>16}{'precision':>10}{'ceiling':>9}{'reached':>9}{'recall@5':>10}")
    for g, us in sorted(groups.items(), key=lambda kv: -len(kv[1])):
        s = sum(slots[u] for u in us)
        e = sum(good[u] for u in us)
        c = sum(min(slots[u], oracle[u]) for u in us)
        has = [u for u in us if oracle[u] > 0]
        rec = sum(good[u] > 0 for u in has)
        print(f"{g:12}{len(us):>9}{f'{e}/{s}':>16}{(100 * e / s if s else 0):>9.1f}%{(100 * c / s if s else 0):>8.1f}%"
              f"{(100 * e / c if c else 0):>8.1f}%{(f'{rec}/{len(has)}' if has else '-'):>10}")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Compose the six per-module diagrams of one view into a single large 'atlas'
poster (2 columns x 3 rows), each module nested as its own SVG viewport so the
whole system reads on one sheet WITHOUT any inter-module line tangle. The EERD
atlas also gets the specialization panel on top. Vector throughout (SVG -> PDF).
"""
import os, re, sys

IMG = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "img", "erd_suite"))

def read_svg(path):
    raw = open(path, encoding="utf-8").read()
    # viewBox is the reliable root size (PlantUML width/height carry px/pt units
    # and there are inner width= attributes that a naive regex would grab).
    m = re.search(r'viewBox="0 0 ([\d.]+) ([\d.]+)"', raw)
    if m:
        w, h = float(m.group(1)), float(m.group(2))
    else:
        w = float(re.search(r'\swidth="([\d.]+)', raw).group(1))
        h = float(re.search(r'\sheight="([\d.]+)', raw).group(1))
    inner = re.sub(r'(?s)^.*?<svg[^>]*>', '', raw)
    inner = re.sub(r'(?s)</svg>\s*$', '', inner)
    return inner, w, h

MODNAMES = {
    1: "Module 1 · Identity, Access & Profile",
    2: "Module 2 · Scholarships, Applications & Documents",
    3: "Module 3 · Consultant Booking & Ratings",
    4: "Module 4 · Community & Chat",
    5: "Module 5 · Resources Hub & Notifications",
    6: "Module 6 · AI, Knowledge Base & Platform",
}

def compose(view, title, files, cols=2, gap=60, pad=70, cell_title_h=44):
    cells = [read_svg(os.path.join(IMG, f + ".svg")) for f in files]
    # scale each cell to a common column width = max width
    colw = max(w for _, w, _ in cells)
    rows = (len(cells) + cols - 1) // cols
    # per-row height = max scaled height in that row (scale keeps aspect since colw>=w -> scale up small ones a bit; cap scale at 1.35)
    placed = []
    y = pad + 60
    i = 0
    row_tops = []
    for r in range(rows):
        rowcells = cells[i:i+cols]
        scales = [min(1.35, colw / w) for _, w, _ in rowcells]
        rowh = max(h*s for (_, w, h), s in zip(rowcells, scales)) if rowcells else 0
        x = pad
        for j, ((inner, w, h), s) in enumerate(zip(rowcells, scales)):
            placed.append((x, y, w, h, s, inner))
            x += colw + gap
        row_tops.append((y, rowh))
        y += rowh + gap + 30
        i += cols
    W = pad*2 + cols*colw + (cols-1)*gap
    H = y + pad
    parts = [f'<?xml version="1.0" encoding="UTF-8"?>',
             f'<svg xmlns="http://www.w3.org/2000/svg" width="{W:.0f}" height="{H:.0f}" viewBox="0 0 {W:.0f} {H:.0f}" font-family="Segoe UI,Arial,sans-serif">',
             '<rect width="100%" height="100%" fill="white"/>',
             f'<text x="{pad}" y="46" font-weight="bold" font-size="30">{title}</text>']
    for (x, y, w, h, s, inner) in placed:
        parts.append(f'<svg x="{x:.1f}" y="{y:.1f}" width="{w*s:.1f}" height="{h*s:.1f}" viewBox="0 0 {w:.0f} {h:.0f}">{inner}</svg>')
        parts.append(f'<rect x="{x-8:.1f}" y="{y-8:.1f}" width="{w*s+16:.1f}" height="{h*s+16:.1f}" fill="none" stroke="#c8ccd4" stroke-width="1.2" rx="8"/>')
    parts.append('</svg>')
    out = os.path.join(IMG, f"{view}-atlas.svg")
    open(out, "w", encoding="utf-8").write("\n".join(parts))
    print(f"wrote {view}-atlas.svg  ({W:.0f}x{H:.0f})")

TITLES = {
    "erd":  "ScholarPath — Entity-Relationship Diagram (ERD) · all modules",
    "eerd": "ScholarPath — Enhanced Entity-Relationship Diagram (EERD) · all modules",
    "mapping": "ScholarPath — Relational Mapping · all modules",
}

if __name__ == "__main__":
    view = sys.argv[1] if len(sys.argv) > 1 else "eerd"
    mods = [f"{view}-module-{n}" for n in range(1, 7)]
    if view == "eerd":
        # specialization spans the full width on top, then the 6 modules
        compose_files = mods
        compose(view, TITLES[view], compose_files, cols=2)
    else:
        compose(view, TITLES[view], mods, cols=2)

#!/usr/bin/env python3
"""Render Elmasri-style relational-schema diagrams (Fundamentals of Database
Systems, Fig. 9.2) as clean SVG with ORTHOGONAL foreign-key arrows that
(a) start exactly at the FK attribute cell, (b) end exactly at the referenced
primary-key cell, and (c) never overlap.

Why not Graphviz?  splines=ortho gives right angles but IGNORES HTML/record
ports, so its arrowheads land on the wrong cell (verified: a UserRoles.UserId
-> Users.Id edge lands on Users.IsDeleted).  splines=polyline honours the
ports but draws diagonal connectors.  No Graphviz mode gives ortho + correct
endpoints + no overlap together, so we lay the relations out ourselves in a
single vertical column and route each FK through its own lane in a left-hand
bus.  Every coordinate is under our control => all three properties hold.

Output: one <fname>.svg per cluster in ../img (same CLUSTERS source of truth
as gen_relmap.py, so the data never diverges).
"""
import os
from gen_relmap import CLUSTERS, esc

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "img")

# ---- metrics (px) ----
FS       = 13      # font size
CHW      = 6.9     # mean glyph advance for Times at 13px
PADX     = 9       # cell horizontal padding
ROWH     = 24      # attribute-row height
HEADH    = 23      # title-row height
VGAP     = 52      # vertical gap between relations (room for arrow stubs)
LANE     = 12      # x-spacing between bus lanes
BUSPAD   = 14      # left padding before first lane
RIGHTM   = 40      # right margin
TOPM     = 26      # top margin
BOTM     = 30      # bottom margin
STUB     = 7       # exit-stub increment in the gap
HEADER_FILL = "#dbe6ff"
FK_FILL     = "#fff7e6"
ARROW       = 8    # arrowhead length

def textw(s):
    return len(s) * CHW

def cellw(label):
    return max(20.0, textw(label) + 2 * PADX)

def x_escape(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")

# ---------------------------------------------------------------- layout ----
def layout(tables):
    """Order relations: most-referenced (high in-degree) at the top so most
    arrows travel upward; referenced 'stub' tables sit above their referrers."""
    names = list(tables.keys())
    indeg = {n: 0 for n in names}
    for cols in tables.values():
        for _c, _r, tgt in cols:
            if not tgt:
                continue
            t = tgt.split(":")[0]
            if t in indeg:
                indeg[t] += 1
    order = sorted(names, key=lambda n: (-indeg[n], names.index(n)))
    return order

def build(name, cols, x0, y_top):
    """Emit the SVG for one relation; return (svg_parts, cellboxes, width)."""
    total_w = sum(cellw(c) for c, _, _ in cols)
    p = []
    # header
    p.append('<rect x="%.1f" y="%.1f" width="%.1f" height="%d" fill="%s" stroke="black"/>'
             % (x0, y_top, total_w, HEADH, HEADER_FILL))
    p.append('<text x="%.1f" y="%.1f" text-anchor="middle" font-family="Times New Roman,serif" '
             'font-weight="bold" font-size="%d">%s</text>'
             % (x0 + total_w / 2, y_top + HEADH - 7, FS, x_escape(name)))
    boxes = {}
    cx = x0
    yrow = y_top + HEADH
    for c, role, _ in cols:
        w = cellw(c)
        fill = FK_FILL if ("fk" in role or "soft" in role) else "white"
        p.append('<rect x="%.1f" y="%.1f" width="%.1f" height="%d" fill="%s" stroke="black"/>'
                 % (cx, yrow, w, ROWH, fill))
        deco = ' text-decoration="underline"' if "pk" in role else ""
        p.append('<text x="%.1f" y="%.1f" text-anchor="middle" font-family="Times New Roman,serif" '
                 'font-size="%d"%s>%s</text>'
                 % (cx + w / 2, yrow + ROWH - 7, FS, deco, x_escape(c)))
        boxes[c] = (cx, yrow, w, ROWH)
        cx += w
    return p, boxes, total_w

def render(fname, cluster):
    tables = cluster["tables"]
    order = layout(tables)

    # collect FK edges: (src_table, src_col, dst_table, dst_col, dashed)
    edges = []
    for t in order:
        for c, role, tgt in tables[t]:
            if not tgt:
                continue
            if ":" in tgt:
                dt, dc = tgt.split(":")
            else:
                dt, dc = tgt, "Id"
            if dt not in tables:
                continue
            edges.append([t, c, dt, dc, ("soft" in role)])

    nlanes = len(edges)
    x0 = BUSPAD + nlanes * LANE + 10           # all PK Id cells left-aligned here

    # place relations top->bottom; remember y_top + cellboxes
    svg = []
    ytops = {}
    boxes = {}
    widths = {}
    y = TOPM
    for t in order:
        parts, bx, w = build(t, tables[t], x0, y)
        svg += parts
        ytops[t] = y
        boxes[t] = bx
        widths[t] = w
        y += HEADH + ROWH + VGAP
    canvas_h = y - VGAP + BOTM
    canvas_w = x0 + max(widths.values()) + RIGHTM

    # pre-count incoming arrows per (table, col) to spread the fan-in
    incoming = {}
    for s, sc, dt, dc, dash in edges:
        incoming.setdefault((dt, dc), 0)
    inc_total = {k: 0 for k in incoming}
    for s, sc, dt, dc, dash in edges:
        inc_total[(dt, dc)] += 1
    inc_idx = {k: 0 for k in incoming}
    out_idx = {t: 0 for t in order}

    # route every edge through its own lane
    paths = []
    for k, (s, sc, dt, dc, dash) in enumerate(edges):
        sbx = boxes[s][sc]
        dbx = boxes[dt][dc]
        sx_c = sbx[0] + sbx[2] / 2.0          # FK cell centre-x
        s_bottom = sbx[1] + sbx[3]            # FK cell bottom
        lane_x = BUSPAD + k * LANE            # this edge's unique vertical lane

        # exit stub drops into the gap below the source relation (staggered)
        oi = out_idx[s]; out_idx[s] += 1
        turn_y = s_bottom + 8 + oi * STUB

        # entry y: spread incoming arrows across the destination cell height
        m = inc_total[(dt, dc)]
        ii = inc_idx[(dt, dc)]; inc_idx[(dt, dc)] += 1
        ty = dbx[1] + ROWH * (ii + 1) / (m + 1)
        dx = dbx[0]                            # PK cell LEFT edge (arrow target)

        d = "M %.1f %.1f V %.1f H %.1f V %.1f H %.1f" % (
            sx_c, s_bottom, turn_y, lane_x, ty, dx - 1)
        dash_attr = ' stroke-dasharray="5,3"' if dash else ""
        paths.append('<path d="%s" fill="none" stroke="black" stroke-width="1.1"%s/>'
                     % (d, dash_attr))
        # arrowhead pointing right into the PK cell
        paths.append('<polygon points="%.1f,%.1f %.1f,%.1f %.1f,%.1f" fill="black"/>'
                     % (dx, ty, dx - ARROW, ty - 4, dx - ARROW, ty + 4))

    head = ('<?xml version="1.0" encoding="UTF-8"?>\n'
            '<svg xmlns="http://www.w3.org/2000/svg" width="%.0f" height="%.0f" '
            'viewBox="0 0 %.0f %.0f" font-family="Times New Roman,serif">\n'
            '<rect width="100%%" height="100%%" fill="white"/>'
            % (canvas_w, canvas_h, canvas_w, canvas_h))
    body = "\n".join(paths + svg)        # arrows first, boxes on top
    out = head + "\n" + body + "\n</svg>\n"
    path = os.path.join(OUT, fname + ".svg")
    with open(path, "w", encoding="utf-8") as f:
        f.write(out)
    print("wrote img/%s.svg  (%d tables, %d FK arrows, %.0fx%.0f)"
          % (fname, len(tables), nlanes, canvas_w, canvas_h))

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for fname, cluster in CLUSTERS.items():
        render(fname, cluster)

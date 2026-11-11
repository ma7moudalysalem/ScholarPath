#!/usr/bin/env python3
"""Renderer for the ScholarPath ERD / EERD suite (Chen / Elmasri notation).

Two views share one model (model.py):
  * 'erd'  — plain ER: single-rectangle entities, plain ovals (key underlined),
             composite attributes branched, M:N drawn as relationships. No
             weak-entity, multivalued, derived or specialization notation.
  * 'eerd' — Enhanced ER: adds weak entities (double rectangle) with identifying
             relationships (double diamond) and partial keys (dashed underline),
             multivalued (double oval), derived (dashed oval), and the USER
             specialization (rendered by render_specialization, a separate page).

Each view is emitted per-module (six detail diagrams) and as one overview.
Output: standalone .svg files under ../../img/erd_suite/.
"""
import os, math
from model import (ENTITIES, RELATIONSHIPS, SPECIALIZATION, PROFILE_ROLE_ATTRS,
                   MODULES, MODULE_NO, M1, M2, M3, M4, M5, M6)

OUT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "img", "erd_suite"))

# ---- metrics ----
FS_TITLE, FS_ATTR, FS_REL, FS_CARD = 15, 11, 11, 12
CHW_TITLE, CHW_ATTR = 8.6, 6.5
PADX_T, PADX_O = 13, 10
TITLEH, OVALH = 32, 26
OVAL_VGAP, OVAL_HGAP, SUB_VGAP, FAN_GAP = 7, 11, 20, 34
FILL_STRONG, FILL_WEAK, FILL_STD = "#e8eefc", "#dbeafe", "#eef2f7"
FILL_DIAMOND, FILL_SUB = "#fff3d6", "#e9f7ea"

def esc(s): return str(s).replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
def tw(s, chw): return len(str(s)) * chw

# ---------------------------------------------------------------- attributes --
def oval_size(label, kind):
    return max(56, tw(label, CHW_ATTR) + 2*PADX_O + (10 if kind in ("multi", "derived", "key", "pkey") else 0)), OVALH

def layout_halo(attrs, box_w, view):
    """Grid-pack attribute ovals under an entity; composite attrs get a sub-row."""
    if not attrs:
        return [], [], box_w, 0
    target = max(box_w * 1.75, 380)
    ovals, x, y, rowh, maxw = [], 0, 0, 0, 0
    for label, kind in attrs:
        is_comp = isinstance(kind, tuple) and kind[0] == "comp"
        w, h = oval_size(label, None if is_comp else kind)
        if x > 0 and x + w > target:
            maxw = max(maxw, x - OVAL_HGAP); y += rowh + OVAL_VGAP; x = 0; rowh = 0
        ovals.append(dict(rx=x, ry=y, w=w, h=h, label=label, kind=kind,
                          sub=(kind[1] if is_comp else None)))
        x += w + OVAL_HGAP; rowh = max(rowh, h)
    maxw = max(maxw, x - OVAL_HGAP, box_w)
    haloh = y + rowh
    comps = [o for o in ovals if o["sub"]]
    subs = []
    if comps:
        sy0 = haloh + SUB_VGAP; bottom = haloh
        for c in comps:
            cw = sum(oval_size(s, None)[0] for s in c["sub"]) + OVAL_HGAP*(len(c["sub"])-1)
            sx = c["rx"] + c["w"]/2.0 - cw/2.0
            for s in c["sub"]:
                sw, sh = oval_size(s, None)
                subs.append(dict(rx=sx, ry=sy0, w=sw, h=sh, label=s,
                                 parent_cx=c["rx"]+c["w"]/2.0, parent_by=c["ry"]+c["h"]))
                sx += sw + OVAL_HGAP
            bottom = max(bottom, sy0 + OVALH); maxw = max(maxw, sx)
        haloh = bottom
    return ovals, subs, maxw, haloh

def draw_oval(out, cx, cy, rx, ry, kind, label, view):
    multi = view == "eerd" and kind == "multi"
    deriv = view == "eerd" and kind == "derived"
    if multi:
        out.append(f'<ellipse cx="{cx:.1f}" cy="{cy:.1f}" rx="{rx:.1f}" ry="{ry:.1f}" fill="white" stroke="black" stroke-width="1.1"/>')
        out.append(f'<ellipse cx="{cx:.1f}" cy="{cy:.1f}" rx="{rx-3:.1f}" ry="{ry-3:.1f}" fill="none" stroke="black" stroke-width="1.1"/>')
    elif deriv:
        out.append(f'<ellipse cx="{cx:.1f}" cy="{cy:.1f}" rx="{rx:.1f}" ry="{ry:.1f}" fill="white" stroke="black" stroke-width="1.1" stroke-dasharray="4,3"/>')
    else:
        out.append(f'<ellipse cx="{cx:.1f}" cy="{cy:.1f}" rx="{rx:.1f}" ry="{ry:.1f}" fill="white" stroke="black" stroke-width="1.1"/>')
    deco = ""
    if kind == "key":
        deco = ' text-decoration="underline"'
    elif kind == "pkey":
        deco = ' text-decoration="underline" style="text-decoration-style:dashed"' if view == "eerd" else ' text-decoration="underline"'
    out.append(f'<text x="{cx:.1f}" y="{cy+4:.1f}" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-size="{FS_ATTR}"{deco}>{esc(label)}</text>')

# ---------------------------------------------------------------- entity box --
def entity_geo(name, attrs, view):
    tw_ = max(150, tw(name, CHW_TITLE) + 2*PADX_T)
    ovals, subs, halow, haloh = layout_halo(attrs, tw_, view)
    footw = max(tw_, halow)
    footh = TITLEH + (FAN_GAP + haloh if attrs else 0)
    return dict(title_w=tw_, ovals=ovals, subs=subs, footw=footw, footh=footh)

def render_entity(out, fx, fy, name, attrs, view, weak=False, standalone=False):
    g = entity_geo(name, attrs, view)
    tw_, footw = g["title_w"], g["footw"]
    bx = fx + (footw - tw_)/2.0
    by = fy
    fill = FILL_WEAK if (weak and view == "eerd") else (FILL_STD if standalone else FILL_STRONG)
    if weak and view == "eerd":
        out.append(f'<rect x="{bx-4:.1f}" y="{by-4:.1f}" width="{tw_+8:.1f}" height="{TITLEH+8:.1f}" fill="{fill}" stroke="black" stroke-width="1.4" rx="3"/>')
    out.append(f'<rect x="{bx:.1f}" y="{by:.1f}" width="{tw_:.1f}" height="{TITLEH:.1f}" fill="{fill}" stroke="black" stroke-width="1.5" rx="3"/>')
    out.append(f'<text x="{bx+tw_/2:.1f}" y="{by+TITLEH-10:.1f}" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-weight="bold" font-size="{FS_TITLE}">{esc(name)}</text>')
    halo_x0 = fx + (footw - max(150, tw_))/2.0
    # recompute halo width origin using stored ovals extent
    halow = max((o["rx"]+o["w"] for o in g["ovals"]), default=tw_)
    halow = max(halow, max((s["rx"]+s["w"] for s in g["subs"]), default=0))
    halo_x0 = fx + (footw - halow)/2.0
    hy0 = by + TITLEH + FAN_GAP
    for o in g["ovals"]:
        ocx = halo_x0 + o["rx"] + o["w"]/2.0
        ocy = hy0 + o["ry"] + o["h"]/2.0
        frac = 0.5 if halow <= 1 else (o["rx"]+o["w"]/2.0)/halow
        out.append(f'<line x1="{bx+frac*tw_:.1f}" y1="{by+TITLEH:.1f}" x2="{ocx:.1f}" y2="{ocy-o["h"]/2.0:.1f}" stroke="#555" stroke-width="0.9"/>')
        draw_oval(out, ocx, ocy, o["w"]/2.0, o["h"]/2.0, o["kind"], o["label"], view)
    for s in g["subs"]:
        pcx = halo_x0 + s["parent_cx"]; pcy = hy0 + s["parent_by"]
        ocx = halo_x0 + s["rx"] + s["w"]/2.0; ocy = hy0 + s["ry"] + s["h"]/2.0
        out.append(f'<line x1="{pcx:.1f}" y1="{pcy:.1f}" x2="{ocx:.1f}" y2="{ocy-s["h"]/2.0:.1f}" stroke="#555" stroke-width="0.9"/>')
        draw_oval(out, ocx, ocy, s["w"]/2.0, s["h"]/2.0, None, s["label"], view)
    return dict(bx=bx, by=by, bw=tw_, bh=TITLEH, fx=fx, fy=fy, fw=footw, fh=g["footh"])

# ---------------------------------------------------------------- relationship --
def diamond_pts(cx, cy, hw, hh):
    return f"{cx:.1f},{cy-hh:.1f} {cx+hw:.1f},{cy:.1f} {cx:.1f},{cy+hh:.1f} {cx-hw:.1f},{cy:.1f}"

def clip(b, cx, cy, tx, ty):
    halo_bottom = b["fy"] + b["fh"]; box_bottom = b["by"] + b["bh"]
    hw = b["bw"]/2 + 3
    hh_down = (halo_bottom - cy) if (ty >= box_bottom and halo_bottom > box_bottom+1) else b["bh"]/2+3
    hh_up = b["bh"]/2+3
    dx, dy = tx-cx, ty-cy
    if dx == 0 and dy == 0: return cx, cy
    sx = hw/abs(dx) if dx else 1e9
    hh = hh_down if dy > 0 else hh_up
    sy = hh/abs(dy) if dy else 1e9
    s = min(sx, sy)
    return cx+dx*s, cy+dy*s

def draw_partition_line(out, x1, y1, x2, y2, total):
    if total == "=":
        a = math.atan2(y2-y1, x2-x1) + math.pi/2; off = 2.8
        ox, oy = off*math.cos(a), off*math.sin(a)
        out.append(f'<line x1="{x1+ox:.1f}" y1="{y1+oy:.1f}" x2="{x2+ox:.1f}" y2="{y2+oy:.1f}" stroke="black" stroke-width="1.1"/>')
        out.append(f'<line x1="{x1-ox:.1f}" y1="{y1-oy:.1f}" x2="{x2-ox:.1f}" y2="{y2-oy:.1f}" stroke="black" stroke-width="1.1"/>')
    else:
        out.append(f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" stroke="black" stroke-width="1.1"/>')

def render_relationship(out, rel, boxes, view, placed, lane=(0, 1)):
    b1, b2 = boxes[rel["e1"]], boxes[rel["e2"]]
    identifying = rel.get("identifying") and view == "eerd"
    li, ln = lane
    if rel["e1"] == rel["e2"]:  # self relationship (BLOCKS, REPLIES_TO)
        b = b1
        mx = b["bx"] + b["bw"] + 92; my = b["by"] + b["bh"]/2
        p1x, p1y = b["bx"]+b["bw"], b["by"]+b["bh"]*0.33
        p2x, p2y = b["bx"]+b["bw"], b["by"]+b["bh"]*0.66
    else:
        cx1, cy1 = b1["bx"]+b1["bw"]/2, b1["by"]+b1["bh"]/2
        cx2, cy2 = b2["bx"]+b2["bw"]/2, b2["by"]+b2["bh"]/2
        # perpendicular lane offset so parallel edges between the SAME pair of
        # entities (e.g. USER BOOKS / CONDUCTS BOOKING) fan apart instead of
        # stacking their diamonds on top of one another.
        offx = offy = 0.0
        if ln > 1:
            dx, dy = cx2-cx1, cy2-cy1; L = max(1e-6, math.hypot(dx, dy))
            px, py = -dy/L, dx/L
            d = (li - (ln-1)/2.0) * 150
            offx, offy = px*d, py*d
        p1x, p1y = clip(b1, cx1, cy1, cx2+offx, cy2+offy)
        p2x, p2y = clip(b2, cx2, cy2, cx1+offx, cy1+offy)
        mx, my = (p1x+p2x)/2+offx, (p1y+p2y)/2+offy
    dw = max(48, tw(rel["name"], 6.6) + 30); dh = 30
    # nudge overlapping diamonds along the connector
    ux, uy = mx-p1x, my-p1y
    L = max(1e-6, math.hypot(ux, uy)); ux, uy = ux/L, uy/L
    for _ in range(8):
        if any(abs(mx-ox) < (dw+odw)/2*0.8 and abs(my-oy) < (dh+odh)/2*0.85 for ox, oy, odw, odh in placed):
            mx += ux*26; my += uy*26
        else:
            break
    placed.append((mx, my, dw, dh))
    draw_partition_line(out, p1x, p1y, mx, my, rel["p1"])
    draw_partition_line(out, mx, my, p2x, p2y, rel["p2"])
    if identifying:
        out.append(f'<polygon points="{diamond_pts(mx, my, dw/2+5, dh/2+5)}" fill="{FILL_DIAMOND}" stroke="black" stroke-width="1.3"/>')
    out.append(f'<polygon points="{diamond_pts(mx, my, dw/2, dh/2)}" fill="{FILL_DIAMOND}" stroke="black" stroke-width="1.3"/>')
    out.append(f'<text x="{mx:.1f}" y="{my+4:.1f}" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-size="{FS_REL}">{esc(rel["name"])}</text>')
    # cardinality + role labels
    def lab(px, py, txt, dyoff=0, w=15):
        fx = px + (mx-px)*0.22; fy = py + (my-py)*0.22 + dyoff
        out.append(f'<rect x="{fx-w/2:.1f}" y="{fy-11:.1f}" width="{w:.1f}" height="15" fill="white"/>')
        out.append(f'<text x="{fx:.1f}" y="{fy:.1f}" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-size="{FS_CARD}" font-weight="bold">{esc(txt)}</text>')
    lab(p1x, p1y, rel["c1"]); lab(p2x, p2y, rel["c2"])
    if rel.get("role1"):
        rx = p1x + (mx-p1x)*0.4; ry = p1y + (my-p1y)*0.4
        out.append(f'<text x="{rx:.1f}" y="{ry-5:.1f}" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-size="9.5" font-style="italic" fill="#556">[{esc(rel["role1"])}]</text>')
    # M:N relationship attributes hang off the diamond (basic ER — shown in both views)
    if rel.get("attrs"):
        ax = mx - (len(rel["attrs"])-1)*46/2
        for label, kind in rel["attrs"]:
            ocy = my + dh/2 + 30
            out.append(f'<line x1="{mx:.1f}" y1="{my+dh/2:.1f}" x2="{ax:.1f}" y2="{ocy-13:.1f}" stroke="#555" stroke-width="0.9"/>')
            draw_oval(out, ax, ocy, 42, 13, kind, label, view)
            ax += 92

# ---------------------------------------------------------------- positions --
# grid (col,row) per module; USER/SCHOLARSHIP repeated where a module needs them
POS = {
 M1: {"USER":(2,0),"USER_PROFILE":(2.4,2),"ROLE":(0,1),"EDUCATION_ENTRY":(2.4,4),
      "REFRESH_TOKEN":(3,1),"PASSWORD_RESET_TOKEN":(4,1),"LOGIN_ATTEMPT":(5,1),
      "UPGRADE_REQUEST":(0.6,2.6),"UPGRADE_REQUEST_FILE":(0,4),"UPGRADE_REQUEST_LINK":(1.1,4),
      "EXPERTISE_TAG":(5,0)},
 M2: {"USER":(0,1.2),"CATEGORY":(1.4,0),"SCHOLARSHIP":(2.9,0),"SCHOLARSHIP_CHILD":(4.6,0),
      "SAVED":(0.9,2.2),"APPLICATION":(2.9,2.2),"SCHOLARSHIP_PROVIDER_REVIEW_REQUEST":(4.6,2.2),
      "APPLICATION_CHILD":(1.7,4.2),"DOCUMENT":(3,4.2),"SCHOLARSHIP_PROVIDER_REVIEW":(4.6,4.2)},
 M3: {"USER":(0,1),"AVAILABILITY":(1.7,0),"BOOKING":(3.4,1),
      "NO_SHOW_REPORT":(1.9,2.4),"CONSULTANT_REVIEW":(3.5,2.4),"SESSION_RECORDING":(5.1,1)},
 M4: {"USER":(0,0.6),"FORUM_CATEGORY":(3.3,-1.25),"FORUM_POST":(3.3,0.6),"FORUM_TAG":(5.4,0.6),
      "FORUM_POST_ATTACHMENT":(4.7,2.4),"CONVERSATION":(0.7,2.6),"MESSAGE":(0.7,4.6)},
 M5: {"USER":(0,0),"RESOURCE":(1.6,0),"NOTIFICATION":(3.6,0),"NOTIFICATION_PREFERENCE":(5,0),
      "RESOURCE_CHAPTER":(0.4,2.2),"RESOURCE_PROGRESS":(2.6,2.2),"RESOURCE_PROGRESS_CHILD":(2.6,4.2)},
 M6: {"USER":(0,0.5),"AI_INTERACTION":(2.8,0),"RECOMMENDATION_CLICK_EVENT":(4.8,0),"SCHOLARSHIP":(4.0,1.4),
      "AI_REDACTION_AUDIT_SAMPLE":(2.8,2.4),"USER_RISK_FLAG":(0,2.4),"AUDIT_LOG":(1.2,4.4),
      "USER_DATA_REQUEST":(2.4,4.4),"SUCCESS_STORY":(0,4.4),"KNOWLEDGE_DOCUMENT":(5.4,2.4),"PLATFORM_SETTING":(6.7,2.4)},
}

# association entities that become M:N relationships (so they are NOT drawn as boxes)
ASSOC = {"SAVED"}  # placeholder present in POS only for layout spacing; skipped in draw

def entity_attrs_for(name, view, module=None):
    """Full attributes when the entity is drawn in its OWN module; only the key
    attribute(s) when it appears as a cross-module hub (USER in modules 2-6,
    SCHOLARSHIP in module 6) — its full detail lives in its native module."""
    native = ENTITIES[name]["module"]
    if module is not None and native != module:
        return [a for a in ENTITIES[name]["attrs"] if a[1] == "key"] or ENTITIES[name]["attrs"][:1]
    base = list(ENTITIES[name]["attrs"])
    if name == "USER_PROFILE" and view == "erd":
        base = base[:-1] + PROFILE_ROLE_ATTRS + [base[-1]]  # keep ProfileCompleteness last
    return base

def module_entities(module):
    names = [n for n in POS[module] if n in ENTITIES]
    return names

def render_module(module, view):
    pos = POS[module]
    names = [n for n in pos if n in ENTITIES]
    sizes = {n: entity_geo(n, entity_attrs_for(n, view, module), view) for n in names}
    cols = sorted(set(pos[n][0] for n in names)); rows = sorted(set(pos[n][1] for n in names))
    colw = {c: max(sizes[n]["footw"] for n in names if pos[n][0] == c) for c in cols}
    rowh = {r: max(sizes[n]["footh"] for n in names if pos[n][1] == r) for r in rows}
    COLGAP, ROWGAP = 120, 96
    cx = {}; x = 30
    for c in cols: cx[c] = x; x += colw[c] + COLGAP
    cyy = {}; y = 60
    for r in rows: cyy[r] = y; y += rowh[r] + ROWGAP
    out = []; boxes = {}
    for n in names:
        c, r = pos[n]
        fw = sizes[n]["footw"]; fx = cx[c] + (colw[c]-fw)/2.0; fy = cyy[r]
        ent = ENTITIES[n]
        boxes[n] = render_entity(out, fx, fy, n, entity_attrs_for(n, view, module), view,
                                 weak=ent.get("weak", False), standalone=ent.get("standalone", False))
    mrels = [r for r in RELATIONSHIPS if r.get("module") == module
             and r["e1"] in boxes and r["e2"] in boxes]
    # group parallel edges that share the same unordered entity pair
    groups = {}
    for r in mrels:
        groups.setdefault(frozenset((r["e1"], r["e2"])), []).append(r)
    placed = []
    for r in mrels:
        g = groups[frozenset((r["e1"], r["e2"]))]
        render_relationship(out, r, boxes, view, placed, lane=(g.index(r), len(g)))
    W = max(b["fx"]+b["fw"] for b in boxes.values()) + 60
    H = max(b["fy"]+b["fh"] for b in boxes.values()) + 110  # room for M:N attr ovals
    return out, W, H

# ---------------------------------------------------------------- specialization --
def render_specialization():
    sp = SPECIALIZATION; out = []
    MARGIN = 70
    subs = sp["order"]
    # 1) size each subclass footprint and lay them out left->right with gaps
    geos = {n: entity_geo(n, sp["subclasses"][n], "eerd") for n in subs}
    GAP = 150
    xcur = MARGIN
    centers = {}
    sy = 0  # set later
    for n in subs:
        fw = geos[n]["footw"]
        centers[n] = xcur + fw/2.0
        xcur += fw + GAP
    total_w = xcur - GAP + MARGIN
    ccx = (centers[subs[0]] + centers[subs[-1]]) / 2.0   # circle centred over the group
    # 2) USER superclass centred above the circle
    uw = 210; ux = ccx - uw/2; uy = 40
    for i, (lbl, kd) in enumerate([("Id", "key"), ("Email", None), ("AccountStatus", None), ("ActiveRole", None)]):
        ox = ux - 120 + i*(uw+240)/3.0
        draw_oval(out, ox, uy-12, 48, 13, kd, lbl, "eerd")
        out.append(f'<line x1="{ox:.1f}" y1="{uy-12+13:.1f}" x2="{ux+uw/2:.1f}" y2="{uy:.1f}" stroke="#555" stroke-width="0.9"/>')
    out.append(f'<rect x="{ux:.1f}" y="{uy:.1f}" width="{uw}" height="{TITLEH}" fill="{FILL_STRONG}" stroke="black" stroke-width="1.6" rx="3"/>')
    out.append(f'<text x="{ux+uw/2:.1f}" y="{uy+TITLEH-10:.1f}" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-weight="bold" font-size="16">USER</text>')
    # 3) specialization circle ('o' overlapping); single line to USER = partial
    ccy, cr = uy+TITLEH+72, 24
    draw_partition_line(out, ccx, uy+TITLEH, ccx, ccy-cr, "=" if sp["total"] else "-")
    out.append(f'<circle cx="{ccx:.1f}" cy="{ccy:.1f}" r="{cr}" fill="#fff7e0" stroke="black" stroke-width="1.5"/>')
    out.append(f'<text x="{ccx:.1f}" y="{ccy+6:.1f}" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-size="18" font-weight="bold" font-style="italic">{sp["disjoint"]}</text>')
    # 'partial' participation label on the superclass line
    out.append(f'<rect x="{ccx+6:.1f}" y="{uy+TITLEH+16:.1f}" width="50" height="16" fill="white"/>')
    out.append(f'<text x="{ccx+10:.1f}" y="{uy+TITLEH+28:.1f}" font-family="Segoe UI,Arial,sans-serif" font-size="11" font-style="italic" fill="#556">partial</text>')
    # 4) subclasses, each on a subset line carrying the ⊂ (IS-A) glyph opening toward USER
    sy = ccy + 165
    for n in subs:
        scx = centers[n]
        out.append(f'<line x1="{ccx:.1f}" y1="{ccy+cr:.1f}" x2="{scx:.1f}" y2="{sy:.1f}" stroke="black" stroke-width="1.2"/>')
        midx, midy = ccx + (scx-ccx)*0.5, (ccy+cr) + (sy-(ccy+cr))*0.5
        # subset "⊂" symbol: horizontal U opening UP toward the superclass (Elmasri)
        r = 12
        out.append(f'<rect x="{midx-r-3:.1f}" y="{midy-r-2:.1f}" width="{2*r+6}" height="{2*r+4}" fill="white"/>')
        out.append(f'<path d="M {midx-r:.1f} {midy-r:.1f} A {r} {r} 0 0 0 {midx-r:.1f} {midy+r:.1f}" transform="rotate(-90 {midx:.1f} {midy:.1f})" fill="none" stroke="black" stroke-width="2.2"/>')
        render_entity(out, scx - geos[n]["footw"]/2.0, sy, n, sp["subclasses"][n], "eerd", weak=False, standalone=(n == "ADMIN"))
    # 5) compact notation legend (bottom-left)
    ly = sy + max(geos[n]["footh"] for n in subs) + 30
    out.append(f'<rect x="{MARGIN:.1f}" y="{ly:.1f}" width="560" height="96" fill="#fffdf4" stroke="#999" stroke-width="1.2" rx="6"/>')
    out.append(f'<text x="{MARGIN+16:.1f}" y="{ly+24:.1f}" font-family="Segoe UI,Arial,sans-serif" font-weight="bold" font-size="14">EER specialization — notation</text>')
    leg = ['circle  o  = OVERLAPPING (a user may hold several roles at once, e.g. Student + Consultant)',
           'single line USER–circle = PARTIAL (an Unassigned newcomer belongs to no subclass yet)',
           '⊃ on each link = subset symbol: "subclass IS-A USER" (inherits USER’s attributes)']
    for i, t in enumerate(leg):
        out.append(f'<text x="{MARGIN+16:.1f}" y="{ly+44+i*18:.1f}" font-family="Segoe UI,Arial,sans-serif" font-size="12.5">{esc(t)}</text>')
    H = ly + 96 + 40
    return out, total_w, H

# ---------------------------------------------------------------- assembly --
def svg_doc(parts, W, H, title=None, subtitle=None):
    head = (f'<?xml version="1.0" encoding="UTF-8"?>\n<svg xmlns="http://www.w3.org/2000/svg" '
            f'width="{W:.0f}" height="{H:.0f}" viewBox="0 0 {W:.0f} {H:.0f}" font-family="Segoe UI,Arial,sans-serif">\n'
            f'<rect width="100%" height="100%" fill="white"/>\n')
    hdr = []
    if title:
        hdr.append(f'<text x="30" y="34" font-family="Segoe UI,Arial,sans-serif" font-weight="bold" font-size="20">{esc(title)}</text>')
    if subtitle:
        hdr.append(f'<text x="30" y="52" font-family="Segoe UI,Arial,sans-serif" font-size="12" fill="#667">{esc(subtitle)}</text>')
    return head + "\n".join(hdr + parts) + "\n</svg>\n"

def write(name, svg):
    os.makedirs(OUT, exist_ok=True)
    p = os.path.join(OUT, name + ".svg")
    with open(p, "w", encoding="utf-8") as f: f.write(svg)
    print("wrote", os.path.relpath(p, OUT), f"({len(svg)} bytes)")

if __name__ == "__main__":
    import sys
    view = sys.argv[1] if len(sys.argv) > 1 else "eerd"
    which = sys.argv[2] if len(sys.argv) > 2 else "all"
    if which in ("spec", "all") and view == "eerd":
        parts, W, H = render_specialization()
        pre = ['<g transform="translate(0,70)">'] + parts + ['</g>']
        write("eerd-specialization", svg_doc(pre, W, H+90,
              title="ScholarPath — EERD: USER Specialization (Enhanced ER)",
              subtitle="Overlapping {o}, partial — a user may hold several roles; an Unassigned newcomer belongs to no subclass"))
    for m in MODULES:
        if which not in ("all", "modules"): break
        parts, W, H = render_module(m, view)
        pre = ['<g transform="translate(0,64)">'] + parts + ['</g>']
        tag = "EERD" if view == "eerd" else "ERD"
        write(f"{view}-module-{MODULE_NO[m]}",
              svg_doc(pre, W, H+80, title=f"ScholarPath {tag} — Module {MODULE_NO[m]}: {m}"))

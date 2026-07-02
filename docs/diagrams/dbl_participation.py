#!/usr/bin/env python3
"""Convert @startchen TOTAL-participation strokes into true Elmasri double lines.

PlantUML's @startchen draws total participation as a single thick stroke
(stroke-width:2); Elmasri draws it as two parallel lines.  We keep @startchen's
clean layout and rewrite each width-2 path as a thick black stroke with a
thinner white stroke on top, so two parallel black edges show with white
between -- a real double line, for any path shape (straight or curved).

Usage:  python dbl_participation.py <file1.svg> [file2.svg ...]
Only paths with stroke-width:2 are touched, so it is a no-op on UML/class SVGs.
"""
import re, sys, os

OUTER = 3.6   # black outer stroke width (px)
INNER = 1.8   # white inner stroke width (px) -> leaves two ~0.9px black rails

def convert(svg):
    def repl(m):
        el = m.group(0)
        dm = re.search(r'\bd="([^"]+)"', el)
        if not dm:
            return el
        d = dm.group(1)
        return ('<path d="%s" fill="none" style="stroke:#181818;stroke-width:%s;"/>'
                '<path d="%s" fill="none" style="stroke:#FFFFFF;stroke-width:%s;"/>'
                % (d, OUTER, d, INNER))
    # match any <path ...stroke-width:2;.../> element (total-participation edges)
    return re.sub(r'<path\b[^>]*?stroke-width:2;[^>]*?/>', repl, svg)

def process(path):
    s = open(path, encoding='utf-8').read()
    n = len(re.findall(r'<path\b[^>]*?stroke-width:2;[^>]*?/>', s))
    if n:
        open(path, 'w', encoding='utf-8').write(convert(s))
    return n

if __name__ == '__main__':
    total = 0
    for f in sys.argv[1:]:
        if os.path.exists(f):
            c = process(f); total += c
            print("  %s: %d total-participation line(s) -> double" % (os.path.basename(f), c))
        else:
            print("  (missing) %s" % f)
    print("converted %d edge(s)" % total)

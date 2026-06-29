#!/usr/bin/env bash
# Render every ScholarPath diagram source to PNG under ./images
#
# Requires: graphviz (dot) and plantuml on PATH.
#   Debian/Ubuntu:  sudo apt-get install -y graphviz plantuml
#
# Usage:  ./render.sh            # render PNG (default)
#         ./render.sh svg        # render SVG instead
set -euo pipefail
cd "$(dirname "$0")"
FMT="${1:-png}"
mkdir -p images

echo "Rendering class diagrams (PlantUML)..."
plantuml -t"$FMT" -o "$(pwd)/images" class/*.puml

echo "Rendering relational mapping (PlantUML)..."
plantuml -t"$FMT" -o "$(pwd)/images" mapping/*.puml

echo "Rendering EERD (Graphviz Chen notation)..."
for f in eerd/*.dot; do
  dot -T"$FMT" "$f" -o "images/eerd-$(basename "${f%.dot}").$FMT"
done

echo "Done. Output in ./images ($(ls images | wc -l) files)."

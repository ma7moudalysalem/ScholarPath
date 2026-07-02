# ScholarPath — Diagrams Print Pack

Two deliverables, built from the (live-schema-verified) code-derived diagrams.

## 1. Large-format posters (for big-paper printing)
Each poster is a **vector PDF** at **A0** — sharp at ANY print size; hand the `.pdf`
to the print shop. The `.png` next to it is a 150-DPI on-screen **preview only**.

| File | What | Size |
|---|---|---|
| `01-DataModel-Progression.pdf` | The teaching flow **ERD → EERD → Relational schema** (3 panels + arrows) | A0 landscape |
| `02-DataModel-Atlas.pdf` | **Full reference**: basic ERD + EERD master + 6 EERD clusters + 6 relational-mapping sheets | A0 portrait |
| `03-ClassModel-Overview.pdf` | UML **domain model** + **ports & adapters** (Clean Architecture) | A0 portrait |
| `04-ClassModel-Atlas.pdf` | **Full reference**: domain model + ports/adapters + 7 per-area class diagrams | A0 portrait |

## 2. Explainer documents (A4 PDFs, English)
A four-part teaching series — take the system and walk the whole pipeline:

| File | Pages | Covers |
|---|---|---|
| `Explain-1-ERD.pdf` | 8 | The system + actors; Chen ER notation; the basic conceptual ERD; why it must be enhanced |
| `Explain-2-EERD.pdf` | 12 | The EER toolbox; the USER specialization (ISA, overlapping/partial); weak/multivalued/derived; FK-vs-loose; the full EERD + clusters |
| `Explain-3-Mapping.pdf` | 15 | Elmasri & Navathe's 9-step ER→relational mapping applied; the relational schemas; delete-rule strategy; live-schema accuracy notes (FR-057 app-enforced; money typing) |
| `Explain-4-Class.pdf` | 11 | From relations to the UML object model; inheritance; composition/aggregation; the per-area class diagrams; Clean-Architecture ports & adapters |

Sources (editable): `../latex/poster-*.tex` and `../latex/explain-*.tex`; figures in `../img/`.
Re-render figures with `../render.ps1`; recompile a doc with `pdflatex <file>.tex` (run twice for the TOC).

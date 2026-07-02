# ScholarPath — System Design Diagrams

Authoritative, **code-derived** design models for the whole ScholarPath
platform — all 18 PB backlog modules (11 SRS functional modules), **62 database
tables** (≈58 domain entities + 4 ASP.NET Identity satellite tables). Built by reverse-engineering the
live source — domain entities, EF Core model snapshot, application ports and
Infrastructure adapters — and cross-checked against the 5 module SRS documents
(Auth/Onboarding, Profile, Scholarship Discovery, Applications/Documents,
Consultant Booking/Payment/Rating).

> **These files supersede the older `docs/ERD-MAPPING.md` and
> `docs/CLASS-DIAGRAMS.md`,** which predate several merged modules
> (PB-005 paid review, community tags/bookmarks, session recording, RAG
> knowledge base, AI redaction audit, churn-risk flags, platform settings,
> financial-config rules) and are therefore incomplete.

## Contents

| # | Artifact | Notation | File | Rendered images |
|---|---|---|---|---|
| 1 | **EERD** (Enhanced ER) | Elmasri / Chen | [`01-EERD.md`](01-EERD.md) | `img/eerd-*.png` (8) |
| 2 | **Relational mapping** | Elmasri ER→relational (9-step) | [`02-RELATIONAL-MAPPING.md`](02-RELATIONAL-MAPPING.md) | text schemas + `img/RelMap_*.svg` (6) |
| 3 | **Class diagrams** | UML (Sommerville) | [`03-CLASS-DIAGRAMS.md`](03-CLASS-DIAGRAMS.md) | `img/class-*.png` (4) |
| 4 | **Component diagram** | UML (Sommerville) | [`04-COMPONENT-DIAGRAM.md`](04-COMPONENT-DIAGRAM.md) | `img/component-*.png` (1) |

Every diagram is delivered as **vector SVG** under `img/` (sharp at any zoom —
no pixelation), in **two notations**:

- **PlantUML — authentic textbook notation** (the primary deliverable):
  - `img/ScholarPath_EERD_Core.svg` + `img/EERD_*.svg` — EERD in **Chen /
    Elmasri** notation (rectangles = entities, diamonds = relationships,
    ellipses = attributes), from `plantuml/eerd-chen-core.puml` (conceptual
    master) and `plantuml/eerd-chen-clusters.puml` (six per-subject-area views).
  - `img/ScholarPath_Domain_Model.svg`, `img/ScholarPath_Ports_Adapters.svg` —
    UML class diagrams from `plantuml/class-diagrams.puml`.
  - `img/ScholarPath_Components.svg` — UML component diagram (lollipop/socket)
    from `plantuml/component-diagram.puml`.
- **Mermaid** — equivalent views embedded in the `.md` files (render on GitHub),
  also exported to vector SVG (`img/eerd-*.svg`, `img/class-*.svg`,
  `img/component-*.svg`). For the EERD these are *crow's-foot* detail companions
  (they list every entity + key attributes); the Chen SVGs above are the
  master in the exact notation Elmasri uses.

> The PlantUML SVGs were rendered via the official PlantUML server
> (`plantuml.com`) — the same renderer the VS Code PlantUML extension uses.
> Re-render anytime from the `.puml` sources (see *How to render*). Low-res
> `.png` copies also exist in `img/` but the `.svg` versions are preferred.

## Notation reference

**Elmasri / Chen (EERD + mapping)** — entity = rectangle, weak entity = double
rectangle, relationship = diamond, attribute = ellipse, key = underlined,
cardinality `1 / N / M`, participation total = double line (or `(1,*)` min-max),
partial = single line (or `(0,*)`). Specialization (ISA) with overlap/disjoint +
total/partial. See `01-EERD.md` §1 for the full legend, including the project-
specific **solid = DB-enforced FK vs dashed = application-enforced loose
reference** distinction.

**UML / Sommerville (class + component)** — class = name/attributes/operations;
generalization `△` (`<|--`), realization `△ dashed` (`<|..`), composition `◆`
(`*--`), aggregation `◇` (`o--`), association with multiplicities; component =
box with provided `○─` and required `⊃` interfaces.

## How to render

**Mermaid** (already embedded; to re-export):

- Push to GitHub — the `.md` files render automatically, **or**
- paste any ```mermaid block into <https://mermaid.live> and export SVG/PNG.

**PlantUML — one command regenerates everything** (SVG + 200-DPI PNG into `img/`):

```powershell
cd docs/diagrams
./render.ps1        # auto-detects Java + plantuml.jar
```

`render.ps1` resolves `java` from PATH (or a portable JRE in
`%USERPROFILE%\plantuml-tools\jre`) and `plantuml.jar` from `$env:PLANTUML_JAR`
(or `%USERPROFILE%\plantuml-tools\plantuml.jar`). Get `plantuml.jar` from
<https://plantuml.com/download> and a no-install JRE from <https://adoptium.net>.
You can also open any `.puml` in VS Code (PlantUML extension, `Alt+D`) or paste
into <https://www.plantuml.com/plantuml>.

Every PlantUML diagram is exported in **both** `img/*.svg` (vector — for the
report, sharp at any zoom) **and** `img/*.png` (200 DPI — for tools that don't
accept SVG, e.g. some Word workflows).

**Authoritative SQL schema** (to accompany the relational mapping):

```bash
cd server
dotnet ef migrations script \
  --project src/ScholarPath.Infrastructure \
  --startup-project src/ScholarPath.API \
  > ../docs/diagrams/schema.sql
```

## Source of truth

| Concern | Where it was read from |
|---|---|
| Tables, columns, keys, indexes, FK delete rules | `server/src/ScholarPath.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs` |
| Entity shape, navigation, computed members | `server/src/ScholarPath.Domain/Entities/*.cs` |
| Ports & adapters | `…/Application/Common/Interfaces/*` + `…/Infrastructure/Services/*` |
| Requirements, state machines, business rules | the 5 module SRS `.docx` documents |

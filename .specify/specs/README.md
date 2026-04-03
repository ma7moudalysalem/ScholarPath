# ScholarPath Specs

14 module specs mapped 1:1 to the SRS Product Backlog (PB-001 .. PB-014).

## Module map and ownership

| ID | Module | Owner | SRS User Stories | Priority |
|----|--------|-------|------------------|----------|
| PB-001 | Authentication, Access, Onboarding | **@Madiha6776** | US-001..US-014 | Essential |
| PB-002 | Profile and Account Management | **@Madiha6776** | US-015..US-019 | Essential |
| PB-003 | Scholarship Discovery and Listing Mgmt | **@norra-mmhamed** | US-020..US-030 | Essential |
| PB-004 | In-App Application and External Tracking | **@norra-mmhamed** | US-031..US-044 | Essential |
| PB-005 | Company Review, Payment, Rating | **@yousra-elnoby** | US-045..US-050 | High |
| PB-006 | Consultant Booking, Payment, Rating | **@TasneemShaaban** | US-051..US-071 | Essential |
| PB-007 | Community and Chat | **@yousra-elnoby** | US-072..US-081, US-095..US-099 | High |
| PB-008 | AI Features | **@ma7moudalysalem** | US-082..US-087 | High |
| PB-009 | Resources Hub | **@yousra-elnoby** | US-088..US-094, US-100..US-102 | High |
| PB-010 | Notifications | **@Madiha6776** | US-103..US-117 | High |
| PB-011 | Admin Portal and Oversight | **@ma7moudalysalem** | US-118..US-138 | Essential |
| PB-012 | Audit, Compliance, System Integrity | **@ma7moudalysalem** | US-139..US-142 | High |
| PB-013 | Payment Processing and Settlement | **@norra-mmhamed** | US-143..US-152 | Essential |
| PB-014 | Portal Profit Share | **@TasneemShaaban** | US-153..US-159 | High |

**Team lead + architect**: @ma7moudalysalem (reviews shared infrastructure, Program.cs, DbContext, migrations, CI, design system).

## How to work on your module

1. Open `spec.md` in your module folder and confirm the user stories match what you need.
2. Read `plan.md` for the architecture touchpoints (which entities, endpoints, UI pages).
3. Work through `tasks.md` in order. Mark items `[x]` as you finish.
4. Back-reference FR-xxx and US-xxx in every commit and PR.

## Dependency graph

```
PB-001 (Auth) -> everything else
PB-002 (Profile) -> PB-003, PB-004
PB-003 (Scholarships) -> PB-004
PB-013 (Payments) -> PB-005, PB-006, PB-014
PB-004 -> PB-005, PB-010
PB-006 -> PB-010
PB-011 (Admin) -> depends on PB-001 + can moderate all others
```

Work iteration order (from SRS Section 3 Backlog):
- Iteration 1: PB-001, PB-002, PB-011
- Iteration 2: PB-003, PB-004, PB-013
- Iteration 3: PB-005, PB-006, PB-010, PB-014
- Iteration 4: PB-007, PB-008, PB-009, PB-012

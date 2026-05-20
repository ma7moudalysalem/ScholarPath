# Power BI Report Specifications

**PB-015 T-007..T-011** — Detailed visual specifications for the five ScholarPath Power BI dashboards.

All reports connect to the Gold star schema in Synapse Serverless (see `analytics/dbt/models/marts/`).
Before opening Power BI Desktop, complete T-005 (point DirectQuery to Synapse Serverless endpoints).
RLS is documented in `docs/ANALYTICS-RLS.md`.

---

## T-007 — Executive Dashboard (`ExecutiveDashboard`)

**Audience**: Admin / SuperAdmin  
**RLS**: No role filter (Admin sees everything)  
**Refresh**: Scheduled every 4 hours  

### Pages

#### Page 1 — Overview
| Visual | Type | Fields | Notes |
|--------|------|--------|-------|
| Total Users | KPI card | `COUNT(dim_user.user_sk) WHERE is_current=1` | |
| New Users (30d) | KPI card | `COUNT WHERE registration_date >= TODAY()-30` | Trend vs prior 30d |
| Applications | KPI card | `COUNT(fct_application)` | |
| Revenue (captured) | KPI card | `SUM(fct_payment.gross_amount_usd)` | |
| Application funnel | Funnel chart | Draft → Submitted → Accepted by `status_stage` | |
| User growth | Line chart | X=date, Y=cumulative users | 90-day window |
| Top-10 scholarships | Bar chart | X=scholarship_title, Y=application_count | Descending |
| Applicants by country | Filled map | Country=`dim_country.country_name`, Size=count | |

#### Page 2 — Trends
| Visual | Type | Fields |
|--------|------|--------|
| Daily signups | Area chart | X=date_key, Y=new_registrations |
| Acceptance rate by field | Clustered bar | X=field_of_study, Y=acceptance_rate |
| Bookings over time | Line chart | X=date_key, Y=booking_count |
| Revenue split | Donut chart | Segments: Booking, CompanyReview |

---

## T-008 — Student Success Dashboard (`StudentSuccessDashboard`)

**Audience**: Admin / SuperAdmin  
**RLS**: No role filter  
**Refresh**: Scheduled every 4 hours  

### Pages

#### Page 1 — Acceptance Rates
| Visual | Type | Fields |
|--------|------|--------|
| Overall acceptance rate | Gauge | Accepted / (Accepted + Rejected) |
| Acceptance by field | Bar chart | X=field_of_study, Y=acceptance_rate |
| Acceptance by country | Map | Country=destination_country, Color=acceptance_rate |
| Top consultants by uplift | Table | consultant_name, bookings, avg_acceptance_with_booking vs without |

#### Page 2 — Student Journey
| Visual | Type | Fields |
|--------|------|--------|
| Avg time Draft→Submit | KPI card | `AVG(submitted_at - created_at)` in days |
| Avg time Submit→Decision | KPI card | `AVG(decided_at - submitted_at)` in days |
| Dropout funnel | Funnel | Draft / Submitted / Under Review / Accepted / Withdrawn |
| Scholarship match score distribution | Histogram | Buckets: 0-20%, 20-40%, ..., 80-100% |
| Applications per student | Column chart | Buckets: 1, 2-3, 4-5, 6+ |

---

## T-009 — Financial Dashboard (`FinancialDashboard`)

**Audience**: Admin / SuperAdmin  
**RLS**: No role filter  
**Refresh**: Scheduled every 4 hours  

### Pages

#### Page 1 — Revenue
| Visual | Type | Fields |
|--------|------|--------|
| Gross revenue | KPI card | `SUM(fct_payment.gross_amount_usd)` |
| Net revenue | KPI card | `SUM(gross) - SUM(platform_fee + profit_share)` |
| Profit share | KPI card | `SUM(fct_payment.profit_share_usd)` |
| Revenue by type | Column chart | X=date, Y=amount, Legend=payment_type (Booking/Review) |
| Monthly trend | Line chart | X=month, Y=gross_revenue; comparison line: prior year |
| Refunds | KPI card | `COUNT + SUM(refund_amount_usd)` |

#### Page 2 — Profit Share History
| Visual | Type | Fields |
|--------|------|--------|
| Rate timeline | Step line chart | X=effective_from, Y=platform_rate_pct |
| Accumulated profit share | Area chart | X=date, Y=cumulative profit_share_usd |
| Config change log | Table | effective_from, platform_rate_pct, consultant_rate_pct, company_rate_pct |

---

## T-010 — Consultant Self-Analytics (`ConsultantSelfAnalytics`)

**Audience**: Consultant  
**RLS**: `ConsultantScope` — filters to `fct_booking.consultant_email = USERNAME()`  
**Refresh**: Scheduled every 4 hours  

### Pages

#### Page 1 — My Performance
| Visual | Type | Fields |
|--------|------|--------|
| Total bookings | KPI card | `COUNT(fct_booking)` |
| Completed bookings | KPI card | `COUNT WHERE status='Completed'` |
| Avg rating | KPI card | `AVG(rating_stars)` |
| Earnings (net) | KPI card | `SUM(fct_payment.consultant_payout_usd)` |
| Bookings over time | Bar chart | X=week, Y=count |
| Rating distribution | Column chart | X=1-5 stars, Y=count |
| Student success rate | KPI | Applications accepted / total for students I worked with |

#### Page 2 — Earnings Detail
| Visual | Type | Fields |
|--------|------|--------|
| Monthly earnings | Area chart | X=month, Y=net_earnings |
| Top student nationalities | Donut | dim_country.country_name |
| Booking outcomes | Pie | Segments: Completed / Cancelled / NoShow |

---

## T-011 — Student Self-Analytics (`StudentSelfAnalytics`)

**Audience**: Student  
**RLS**: `StudentScope` — filters to `fct_application.student_email = USERNAME()`  
**Refresh**: Scheduled every 4 hours  

### Pages

#### Page 1 — My Applications
| Visual | Type | Fields |
|--------|------|--------|
| Active applications | KPI card | `COUNT WHERE status NOT IN (Accepted, Rejected, Withdrawn)` |
| Acceptance rate | Gauge | Accepted / (Accepted + Rejected) |
| Applications by status | Donut | Draft / Submitted / UnderReview / Accepted / Rejected / Withdrawn |
| Application timeline | Gantt / timeline | X=time range, Y=scholarship, Color=status |
| Match score trend | Line chart | X=application_date, Y=match_score |

#### Page 2 — My Journey
| Visual | Type | Fields |
|--------|------|--------|
| Avg response time | KPI card | `AVG(decided_at - submitted_at)` |
| Bookings made | KPI card | `COUNT(fct_booking WHERE student_email = USERNAME())` |
| Scholarship recommendations viewed | KPI card | `COUNT(fct_recommendation_click)` |
| Success factors | Table | scholarship_title, status, consultant_name (if booked), match_score |

---

## Deployment checklist

After Power BI workspace is provisioned:

1. `git pull` to get the latest Gold dbt models.
2. In Synapse Studio, confirm the 5 fact tables and 5 dim tables are queryable.
3. Open Power BI Desktop → **Get Data → Synapse Analytics (SQL DW)** → connect to Gold schema.
4. Build each report page following the specs above.
5. Configure RLS roles per `docs/ANALYTICS-RLS.md`.
6. Fill in `appsettings.json` → `PowerBi.ReportIds.*` with each report's GUID after publishing.
7. Run `analytics/powerbi/test-rls-impersonation.py` to verify embed tokens.

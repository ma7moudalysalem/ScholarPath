# 1. Executive Summary

Scholar Path is an authenticated web-based platform that connects scholarship seekers with studying-abroad companies (scholarship providers), mentors, and a peer academic community. The platform is fully gated: the home page is the only publicly visible surface; all other features require registration and login. The system covers the full scholarship lifecycle (from discovery and eligibility assessment through to application tracking and outcome notification) while integrating AI-driven personalization, Stripe-based payment processing for mentor sessions, and real-time communication.

This BRD is the authoritative reference for all functional requirements, non-functional requirements, business rules, workflows, constraints, and stakeholder definitions for the first production release (v1). It governs development, testing, and stakeholder acceptance activities.

## 1.1 Business Objectives

Enable students to discover, evaluate, and apply for scholarships in a single centralized, gated platform.

Allow studying-abroad companies to publish scholarship listings and review in-web applications.

Connect students with verified mentors and advisors for personalized guidance, with Stripe-powered session payments.

Foster a peer knowledge-sharing community through forums, Q&A, and real-time chat.

Deliver AI-powered features that improve match quality, eligibility awareness, and application guidance.

Provide a rich Resources hub with courses, internships, and training opportunities curated by mentors, providers, and admins.

Provide the Admin with full oversight, content control, and analytics capabilities.

# 2. Project Scope

## 2.1 In Scope

User registration and authentication (Email/Password, Google SSO, Microsoft SSO )and role management (including role upgrade).

Gated platform: home page is public; all other routes require an authenticated session.

Scholarship discovery: full-text search, advanced filters, bookmarking, and AI match scoring (authenticated users only).

Scholarship provider portal (studying-abroad companies): listing creation with in-web application forms, applicant review, and status management.

Student application tracker: in-web application and admin-created external-URL applications, document uploads, status timeline, and deadline reminders.

External URL application self-tracking: students manually update their own application status for listings that redirect to an external site.

Stripe payment integration for mentor session bookings.

Mentor/Advisor module: verified mentor profiles, availability management, booking, and post-session reviews.

Community module: forum threads, Q&A, upvoting, and real-time chat via SignalR.

Resources hub: courses, internships, and training opportunities uploaded by mentors, scholarship providers, and admins.

AI integration: personalized scholarship recommendations, eligibility checker, and AI chatbot.

Notification engine: in-web application and email notifications for deadlines, status changes, and admin broadcasts.

Admin portal: full CRUD over all entities, user management, content moderation, and analytics dashboard.

## 2.2 Out of Scope

Mobile native applications (iOS/Android) web-responsive only for v1.

Deep integration with external university student information systems.

Automated scholarship sourcing via web scraping.

WCAG(Web Content Accessibility Guidelines) 2.1 Level AA accessibility compliance deferred to v2.

3. Stakeholders & System Roles

The following roles are defined within Scholar Path. The platform is fully gated: the home page is the only surface accessible without authentication. Any interaction beyond the home page prompts the visitor to register or log in.


| Role | Users | Account Type | Key Responsibilities |
|---|---|---|---|
| Guest | Unauthenticated visitors | None required | View the home page only. Clicking any feature, listing, or resource prompts registration or login. |
| Student / Applicant | Undergraduate, Postgraduate, PhD, International students, Professionals | Registered account | Search scholarships, track applications, book mentors, use AI features, engage in community, access resources. |
| Scholarship Provider | Studying-abroad companies | Verified organization account (Admin approved) | Create and manage scholarship listings (in-web app mode), review and decide on submitted applications. |
| Mentor / Advisor | Academic advisors, alumni, consultants | Verified professional account (Admin approved) | Publish availability, conduct paid mentoring sessions, upload resources, contribute to community forums. |
| Admin | Platform operations team | Internal staff account | Full platform oversight: managing all users, listings, resources, community moderation, and analytics. Creates external-URL listings. |

3.1 Role Permission Matrix


| Feature / Action | Guest | Student | Provider | Mentor | Admin |
|---|---|---|---|---|---|
| View home page | ✓ | ✓ | ✓ | ✓ | ✓ |
| View any other page | ✗ → login | ✓ | ✓ | ✓ | ✓ |
| Browse & search scholarships | ✗ → login | ✓ | ✓ | ✓ | ✓ |
| Apply for scholarships (in-app) | ✗ | ✓ | ✗ | ✗ | – |
| Self-track external URL applications | ✗ | ✓ | ✗ | ✗ | – |
| Create scholarship listings (in-app) | ✗ | ✗ | ✓ | ✗ | ✓ |
| Create external URL listings | ✗ | ✗ | ✗ | ✗ | ✓ |
| Review applications | ✗ | ✗ | ✓ | ✗ | ✓ |
| Book mentor session | ✗ | ✓ | ✗ | ✗ | – |
| Offer mentoring services | ✗ | ✗ | ✗ | ✓ | – |
| Upload resources | ✗ | ✗ | ✓ | ✓ | ✓ |
| Post in community forum | ✗ | ✓ | ✓ | ✓ | ✓ |
| Use AI chatbot / recommendations | ✗ | ✓ | ✗ | ✗ | – |
| Moderate community content | ✗ | ✗ | ✗ | ✗ | ✓ |
| Access admin dashboard | ✗ | ✗ | ✗ | ✗ | ✓ |
| Upgrade role | ✗ | ✓ | ✗ | ✗ | ✓ |

## Section 1 — SRS Scope + Requirement Specification

### 1.1 System scope

Scholar-Path is a gated web-based scholarship platform. The home page is the only public page. All other system features require authentication and are delivered through role-based experiences for Student, Company, Consultant, Unassigned, and Admin. The target system covers authentication and onboarding, scholarship discovery and management, in-app and external application tracking, consultant booking and payment processing, company-side application review, community and chat, AI assistance, article-based resources, notifications, and full administrative control. This scope is based on the BRD’s full first-release platform vision, with naming aligned to the codebase and structural corrections drawn from the audit and the post-audit database design.

### 1.2 Requirement-writing convention

Per the course material, requirements are written in testable form using “The system shall …” for mandatory requirements and “The system should …” for desirable requirements. To support later prioritization and traceability, the table uses a numeric priority scale:

5 = Essential

4 = High

3 = Desirable

2 = Optional

1 = Future

### 1.3 Alignment decisions for this SRS

Official business naming in this SRS: Student, Company, Consultant, Admin, Unassigned

Guest remains a valid external actor but does not have an account

Unassigned is treated as a real system state/actor during onboarding

A Student may upgrade to Consultant and then access both modes through an in-session role switcher

Company and Consultant accounts require admin authorization after onboarding submission

Resources are limited to articles only

A Student may reapply after withdrawal, subject to scholarship still being open and no other active application existing

Company and Consultant flows both integrate with Stripe, but for different business purposes

# 1.4 Requirement Specification

## A. General platform scope and access control


| Identifier | Priority | Requirement |
|---|---|---|
| FR-001 | 5 | The system shall provide a gated web platform for scholarship discovery, application tracking, consulting services, company-side review, community interaction, AI assistance, article publishing, and administration. |
| FR-002 | 5 | The system shall expose the Home Page as the only public page accessible without authentication. |
| FR-003 | 5 | The system shall require authentication for all pages and actions other than the Home Page and public registration/login entry points. |
| FR-004 | 5 | The system shall support the following account role names in all business-facing workflows: Student, Company, Consultant, Admin, and Unassigned. |
| FR-005 | 5 | The system shall treat Unassigned as a valid account state during onboarding before a final approved role becomes active. |
| FR-006 | 4 | The system should display clear gated-access prompts when a Guest attempts to access a protected feature, scholarship, article, or dashboard page. |

## B. Registration, authentication, and onboarding


| Identifier | Priority | Requirement |
|---|---|---|
| FR-007 | 5 | The system shall allow a Guest to register using email and password. |
| FR-008 | 4 | The system shall support registration and login using Google SSO. |
| FR-009 | 4 | The system shall support registration and login using Microsoft SSO. |
| FR-010 | 5 | The system shall allow a newly registered account to enter the onboarding flow immediately after registration. |
| FR-011 | 5 | The system shall create newly registered users with role state Unassigned until onboarding is completed and role activation rules are satisfied. |
| FR-012 | 5 | The system shall allow a user to choose Student, Company, or Consultant during onboarding. |
| FR-013 | 5 | The system shall allow a Student to complete onboarding without admin approval and activate Student permissions immediately after successful onboarding completion. |
| FR-014 | 5 | The system shall require Company onboarding submissions to enter an admin review workflow before Company permissions are activated. |
| FR-015 | 5 | The system shall require Consultant onboarding submissions to enter an admin review workflow before Consultant permissions are activated. |
| FR-016 | 5 | The system shall allow a Student to submit an upgrade request to Consultant from the authenticated account settings or profile area. |
| FR-017 | 5 | The system shall require admin approval before a Student-to-Consultant upgrade takes effect. |
| FR-018 | 5 | The system shall preserve the Student profile and historical Student activities when the same account is approved as a Consultant. |
| FR-019 | 5 | The system shall provide an in-session role switcher for accounts that hold both Student and Consultant access. |
| FR-020 | 5 | The system shall adapt the visible dashboard, navigation, permissions, and interface content according to the user’s active mode selected in the in-session role switcher. |
| FR-021 | 5 | The system shall enforce password rules requiring at least 8 characters, one uppercase letter, one digit, and one special character. |
| FR-022 | 5 | The system shall issue an authenticated session using access and refresh token mechanisms. |
| FR-023 | 4 | The system shall support a Remember Me option that extends the refresh-token lifetime. |
| FR-024 | 5 | The system shall provide a password reset flow through a time-limited email link. |
| FR-025 | 5 | The system shall invalidate active refresh tokens after a successful password reset. |
| FR-026 | 5 | The system shall lock an account temporarily after repeated failed login attempts according to the configured security policy. |
| FR-027 | 5 | The system shall maintain onboarding status and account status as explicit user-state data used by route guards and authorization logic. |

## C. User profiles and role-specific data


| Identifier | Priority | Requirement |
|---|---|---|
| FR-028 | 5 | The system shall maintain a base user profile containing identity and personal account information. |
| FR-029 | 5 | The system shall maintain Student-specific profile attributes required for scholarship discovery, eligibility analysis, and application workflows. |
| FR-030 | 5 | The system shall maintain Company-specific profile attributes required for organization verification, scholarship listing management, and application review. |
| FR-031 | 5 | The system shall maintain Consultant-specific profile attributes required for expertise presentation, session booking, and payout processing. |
| FR-032 | 4 | The system should display a profile completeness indicator to help users complete recommended data fields. |
| FR-033 | 4 | The system should allow users to update profile photo, biography, language preferences, and other editable profile details. |

## D. Scholarship discovery and scholarship data


| Identifier | Priority | Requirement |
|---|---|---|
| FR-034 | 5 | The system shall allow authenticated users to browse and search scholarship listings. |
| FR-035 | 5 | The system shall support filtering scholarship listings by category, country, deadline, funding type, academic level, tags, and other configured filter fields. |
| FR-036 | 4 | The system should support sorting scholarship listings by relevance, deadline, newest, and recommended order. |
| FR-037 | 5 | The system shall support two scholarship listing types: in-app listings and external-URL listings. |
| FR-038 | 5 | The system shall allow Company accounts to create and manage in-app scholarship listings. |
| FR-039 | 5 | The system shall restrict creation of external-URL scholarship listings to Admin accounts only. |
| FR-040 | 5 | The system shall require each scholarship listing to include mandatory core fields such as title, description, deadline, eligibility details, and relevant application information. |
| FR-041 | 5 | The system shall allow in-app scholarship listings to define application form fields. |
| FR-042 | 5 | The system shall allow in-app scholarship listings to define required application documents. |
| FR-043 | 5 | The system shall prevent creation of a scholarship listing whose deadline violates the configured minimum lead time rule. |
| FR-044 | 4 | The system should allow scholarship listings to be tagged for discovery and recommendation features. |
| FR-045 | 5 | The system shall allow Students to bookmark scholarship listings. |
| FR-046 | 4 | The system should notify relevant Students as configured for bookmarked scholarship deadlines and draft reminders. |

## E. Application management and tracking


| Identifier | Priority | Requirement |
|---|---|---|
| FR-047 | 5 | The system shall allow a Student to submit an in-app application to a Company-managed scholarship listing. |
| FR-048 | 5 | The system shall allow a Student to save an in-app application as draft before submission. |
| FR-049 | 5 | The system shall allow a Student to attach documents from the user’s document vault to an in-app application. |
| FR-050 | 5 | The system shall maintain an application status lifecycle for in-app applications. |
| FR-051 | 5 | The system shall record application status changes in a status-history trail. |
| FR-052 | 5 | The system shall allow a Company to review submitted applications and update their statuses according to the configured workflow. |
| FR-053 | 5 | The system shall allow Admin-created external-URL listings to generate self-tracked application records for Students. |
| FR-054 | 5 | The system shall redirect a Student to the external application URL when the Student initiates an application for an external-URL scholarship listing. |
| FR-055 | 5 | The system shall allow the Student to manually update the status of a self-tracked external application record. |
| FR-056 | 5 | The system shall allow the Student to add personal notes to a self-tracked external application record. |
| FR-057 | 5 | The system shall enforce one active application per Student per scholarship at a time. |
| FR-058 | 5 | The system shall allow a Student to withdraw an application only while the application remains eligible for withdrawal and has not reached Accepted or Rejected final outcomes.  |
| FR-059 | 5 | The system shall allow a Student to reapply to the same scholarship after withdrawal only if the scholarship remains open and no other active application exists for the same Student and scholarship. |
| FR-060 | 5 | The system shall lock accepted and rejected in-app applications as read-only final outcomes. |
| FR-061 | 4 | The system should display application timeline information and next-step guidance to Students. |
| FR-062 | 4 | The system should remind Students of imminent deadlines, incomplete drafts, and status changes. |

## F. Company-side application review and payment model


| Identifier | Priority | Requirement |
|---|---|---|
| FR-063 | 5 | The system shall provide Company accounts with a dashboard for listing management and application review. |
| FR-064 | 5 | The system shall allow a Company to view submitted applications for its own scholarship listings only. |
| FR-065 | 5 | The system shall allow a Company to review student application materials and update decisions according to the configured status sequence. |
| FR-066 | 5 | The system shall integrate Stripe for Company-side payment-related business flows. |
| FR-067 | 5 | The system shall support a business rule in which Company-side revenue is tied to reviewing student scholarship applications and related paperwork. |
| FR-068 | 4 | The system should support configurable pricing or fee rules for Company review services at the platform level. |
| FR-069 | 4 | The system should generate payment and settlement records for Company-related review transactions. |

## G. Company review and rating requirements


| Identifier | Priority | Requirement |
|---|---|---|
| FR-070 | 5 | The system shall allow a Student to submit a rating and review for a Company after the related scholarship application reaches a final review outcome or closed review state. |
| FR-071 | 5 | The system shall associate each Company rating and review with the related Student and application record for traceability. |
| FR-072 | 4 | The system shall display Company rating information on the Company profile and other relevant Company-facing scholarship pages. |
| FR-073 | 4 | The system should calculate and display the Company’s average rating and total review count. |
| FR-074 | 4 | The system should prevent duplicate Company ratings from the same Student for the same application record. |
| FR-075 | 4 | The system should allow Admins to moderate, hide, or remove inappropriate Company reviews. |

## H. Consultant booking, cancellation, refund, and payments


| Identifier | Priority | Requirement |
|---|---|---|
| FR-076 | 5 | The system shall provide a Consultant directory searchable by Students. |
| FR-077 | 5 | The system shall display Consultant profiles including expertise, credentials, availability, reviews, and fee information. |
| FR-078 | 5 | The system shall allow a Student to request a booking with a Consultant by selecting an available time slot. |
| FR-079 | 5 | The system shall integrate Stripe to process Consultant session payments. |
| FR-080 | 5 | The system shall create a payment hold when a Student submits a paid booking request to a Consultant. |
| FR-081 | 5 | The system shall capture payment only if the Consultant accepts the booking request. |
| FR-082 | 5 | The system shall release the payment hold automatically if the Consultant rejects the request or if the request expires. |
| FR-083 | 5 | The system shall allow a Consultant to accept or reject a booking request within the configured response window. |
| FR-084 | 5 | The system shall record booking details including session type, duration, meeting link, cancellation reason, and attendance indicators. |
| FR-085 | 5 | The system shall allow a Student to cancel a booking before consultant acceptance and receive a full refund. |
| FR-086 | 5 | The system shall provide a full refund if the Consultant rejects the request or the request expires. |
| FR-087 | 5 | The system shall provide a full refund when the student cancels more than 24 hours before the scheduled session after acceptance. |
| FR-088 | 5 | The system shall provide a 50% refund when the student cancels less than 24 hours before the scheduled session after acceptance. |
| FR-089 | 5 | The system shall provide a full refund when the Consultant cancels after accepting the booking. |
| FR-090 | 5 | The system shall provide a full refund when the Consultant is marked as a no-show. |
| FR-091 | 5 | The system shall provide no refund when the student is marked as a no-show. |
| FR-092 | 5 | The system shall allow both the Student and the Consultant to view booking status, refund status, and payment outcome. |
| FR-093 | 5 | The system shall allow Students to submit ratings and reviews for completed Consultant sessions. |
| FR-094 | 4 | The system should automatically suspend booking intake for consultants whose rating falls below the configured policy threshold pending Admin review. |
| FR-095 | 5 | The system shall log Stripe webhook events to ensure idempotent payment and refund processing. |

## I. Consultant review and rating requirements


| Identifier | Priority | Requirement |
|---|---|---|
| FR-096 | 5 | The system shall allow a Student to submit a rating and review for a Consultant after the related session reaches a completed or closed state. |
| FR-097 | 5 | The system shall associate each Consultant rating and review with the related Student and booking record for traceability. |
| FR-098 | 4 | The system shall display Consultant rating information on the Consultant profile and other relevant Consultant-facing pages. |
| FR-099 | 4 | The system should calculate and display the Consultant’s average rating and total review count. |
| FR-100 | 4 | The system should prevent duplicate Consultant ratings from the same Student for the same booking record. |
| FR-101 | 4 | The system should allow Admins to moderate, hide, or remove inappropriate Consultant reviews. |

## J. Community


| Identifier | Priority | Requirement |
|---|---|---|
| FR-102 | 5 | The system shall provide a community forum for authenticated users. |
| FR-103 | 5 | The system shall allow authenticated users to create posts and replies within configured categories. |
| FR-104 | 5 | The system shall allow authenticated users to upvote or downvote posts and replies. |
| FR-105 | 5 | The system shall prevent users from voting on their own content. |
| FR-106 | 5 | The system shall allow authenticated users to flag inappropriate community content. |
| FR-107 | 5 | The system shall automatically hide a post after three or more distinct valid flags and send it to the admin moderation queue. |
| FR-108 | 4 | The system should allow forum posts to contain attachment content subject to moderation controls. |

## K. Chat


| Identifier | Priority | Requirement |
|---|---|---|
| FR-109 | 5 | The system shall support one-to-one real-time chat between authenticated users and show their status it they are online or offline. |
| FR-110 | 4 | The system should display online/offline presence indicators in chat. |
| FR-111 | 5 | The system shall persist chat messages and make them available in conversation history. |
| FR-112 | 5 | The system shall allow users to block other users from initiating new chat conversations. |

## L. AI features


| Identifier | Priority | Requirement |
|---|---|---|
| FR-113 | 5 | The system shall generate personalized scholarship recommendations for authenticated Students. |
| FR-114 | 5 | The system shall display a match score and brief explanation for each recommendation. |
| FR-115 | 5 | The system shall refresh recommendation results when relevant Student profile information changes. |
| FR-116 | 5 | The system shall provide an eligibility checker for scholarship listings. |
| FR-117 | 5 | The eligibility checker shall return Eligible, Partially Eligible, or Not Eligible with per-criterion detail. |
| FR-118 | 4 | The system should suggest profile improvements that may increase eligibility or match quality. |
| FR-119 | 5 | The system shall provide an AI chatbot for scholarship guidance and related study-abroad assistance. |
| FR-120 | 5 | The system shall retain chatbot conversation history per session. |
| FR-121 | 5 | The system shall display an AI-generated-response disclaimer in every chatbot response. |

## M. Resources hub


| Identifier | Priority | Requirement |
|---|---|---|
| FR-122 | 5 | The system shall provide a Resources Hub accessible to authenticated users only. |
| FR-123 | 5 | The system shall limit Resources Hub content to articles only for the first release. |
| FR-124 | 5 | The system shall allow Consultant, Company, and Admin accounts to publish articles. |
| FR-125 | 5 | The system shall subject all article publishing and visibility to Admin moderation authority. |
| FR-126 | 5 | The system shall require each article resource to include title, description, author or provider name, publication date, and article content or article link according to the configured article format. |
| FR-127 | 4 | The system should allow authenticated users to search and filter articles by category, author, and publication date. |
| FR-128 | 4 | The system should allow Admins to feature selected articles on the Resources landing area. |

## M.1 Validation rules for publishing, flagging, and chat blocking


| Identifier | Priority | Requirement |
|---|---|---|
| FR-129 | 5 | The system shall validate community post submissions before publishing by checking that the post content is not empty, does not exceed configured length limits, and satisfies configured category and attachment rules. |
| FR-130 | 5 | The system shall validate reply/comment submissions before publishing by checking that the reply content is not empty, does not exceed configured length limits, and is attached to a valid existing post. |
| FR-131 | 4 | The system shall return clear validation errors when a post or reply/comment submission fails publishing rules so that the user can correct and resubmit the content. |
| FR-132 | 5 | The system shall enforce flagging rules by allowing only authenticated users to flag content, preventing users from flagging their own content, preventing duplicate active flags by the same user on the same post or reply, and requiring a valid flag reason from the configured moderation options. |
| FR-133 | 5 | The system shall enforce chat-block rules such that when User A blocks User B, User B cannot initiate a new one-to-one conversation with User A and cannot send new direct messages to User A until the block is removed. |
| FR-134 | 4 | The system should allow the blocking user to unblock another user so that future direct-chat initiation and message delivery can resume according to current platform policy. |
| FR-135 | 5 | The system shall validate article submissions before publishing by checking that all required article fields are present, including title, description, author or provider name, publication date, and either article content or a valid article link according to the configured article format. |
| FR-136 | 5 | The system shall reject article submissions that contain empty required fields, invalid links, unsupported formats, or content that violates configured length or formatting rules, and shall return clear validation errors to the publisher. |
| FR-137 | 4 | The system shall prevent an article from becoming visible in the Resources Hub until article validation succeeds and the article satisfies the configured moderation and visibility workflow. |

## N. Notifications


| Identifier | Priority | Requirement |
|---|---|---|
| FR-138 | 5 | The system shall support notification delivery through both in-platform notifications and email notifications. |
| FR-139 | 5 | The system shall ensure that notification delivery applies consistently to Student, Company, Consultant, and Admin users according to the events relevant to each role. |
| FR-140 | 5 | The system shall send in-app and/or email notifications for application submission events. |
| FR-141 | 5 | The system shall notify Students when application status changes occur. |
| FR-142 | 5 | The system shall notify Companies when new applications are received for their listings. |
| FR-143 | 5 | The system shall notify a Company when a Student submits a rating or review related to the Company’s scholarship review process. |
| FR-144 | 5 | The system shall notify Companies of review-payment events, successful payment processing, refund impact, payout status, and other payment-related updates. |
| FR-145 | 5 | The system shall notify Students about booking confirmations, declines, expirations, and refund outcomes. |
| FR-146 | 5 | The system shall notify Consultants of new reservation requests, booking confirmations, cancellations, expirations, and no-show outcomes. |
| FR-147 | 5 | The system shall notify a Consultant when a Student submits a rating or review related to the Consultant’s completed session. |
| FR-148 | 5 | The system shall notify Consultants of payment capture, refund impact, payout status, and other payment-related updates. |
| FR-149 | 5 | The system shall notify Admins about pending Company and Consultant approval requests. |
| FR-150 | 4 | The system should support admin broadcast announcements to all or filtered user groups. |
| FR-151 | 4 | The system should mark in-platform notifications as read or unread so that users can track which notifications still need attention. |
| FR-152 | 5 | The system shall send email notifications to the user’s registered email address for supported notification events. |

## O. Admin portal and platform oversight


| Identifier | Priority | Requirement |
|---|---|---|
| FR-153 | 5 | The system shall provide an Admin portal for platform oversight. |
| FR-154 | 5 | The system shall allow Admins to view, search, and filter users by role, status, and registration details. |
| FR-155 | 5 | The system shall allow Admins to approve or reject Company onboarding requests. |
| FR-156 | 5 | The system shall allow Admins to approve or reject Consultant onboarding requests. |
| FR-157 | 5 | The system shall allow Admins to approve or reject Student-to-Consultant upgrade requests. |
| FR-158 | 5 | The system shall allow Admins to activate, suspend, deactivate, or delete user accounts according to platform policy. |
| FR-159 | 5 | The system shall allow Admins to manage scholarship content, article content, community categories, moderation queues, and featured content. |
| FR-160 | 5 | The system shall allow Admins to moderate flagged community content and record moderation actions. |
| FR-161 | 4 | The system should provide an analytics dashboard for users, scholarships, applications, bookings, resources, and AI usage. |
| FR-162   | 4 | The system should support CSV export for major analytics and administrative reports.   |
| FR-163 | 5 | The system shall allow Admins, through the Admin Dashboard, to manage payment fee configuration for Consultant booking payments and Company review-service payments. |
| FR-164 | 5 | The system shall allow Admins, through the Admin Dashboard, to configure portal profit-share percentages separately for Consultant payments and Company review-service payments. |
| FR-165 | 5 | The system shall allow Admins to create, update, activate, deactivate, and archive financial configuration rules for fees and portal profit share without requiring code changes or direct database changes. |
| FR-166 | 5 | The system shall treat Admin-configured payment fee and portal profit-share rules as platform-level defaults for the corresponding payment type in the first release. |
| FR-167 | 4 | The system should provide the Admin with a calculation preview showing gross amount, configured fee, portal profit-share percentage, portal profit-share amount, total platform retention, and resulting net payout before saving a financial configuration rule. |
| FR-168 | 5 | The system shall validate Admin-entered fee values and portal profit-share percentages to prevent invalid, negative, conflicting, or out-of-policy configurations. |
| FR-169 | 4 | The system should allow Admins to define an effective start date and, optionally, an effective end date for a financial configuration rule from the Admin Dashboard. |
| FR-170 | 5 | The system shall ensure that only one active financial configuration rule exists at a time for Consultant payments and only one active financial configuration rule exists at a time for Company review-service payments. |
| FR-171 | 5 | The system shall maintain an audit trail for Admin financial configuration changes, including old values, new values, changed by, changed at, rule status, and effective dates. |
| FR-172 | 5 | The system shall restrict financial configuration management to authorized Admin users only. |
| FR-173 | 4 | The system should allow Admins to view current active financial rules and historical financial rules for Consultant and Company payment flows from the Admin Dashboard. |
| FR-174 | 4 | The system should allow Admins to search and filter financial configuration records by payment type, status, effective date, and last updated date. |
| FR-175 | 4 | The system should allow Admins to simulate sample transaction values before activating a financial configuration rule so that the effect on fee deduction, portal share, and net payout can be reviewed in advance. |
| FR-176 | 5 | The system shall prevent deletion of a financial configuration rule that has already been used in recorded transactions; instead, the system shall allow it to be archived or deactivated. |

## P. Data integrity, audit, and alignment requirements


| Identifier | Priority | Requirement |
|---|---|---|
| FR-177 | 5 | The system shall maintain audit records for critical create, update, delete, approval, moderation, and payment actions. |
| FR-178 | 5 | The system shall maintain login-attempt records for security monitoring. |
| FR-179 | 5 | The system shall maintain explicit role-upgrade request records. |
| FR-180 | 5 | The system shall preserve student historical data when a Student is approved as a Consultant under the same account. |
| FR-181 | 5 | The system shall structure user-related data so that Unassigned accounts can exist before subclass profile completion. |
| FR-182 | 4 | The system should support user data export and deletion-request tracking for compliance workflows. |

## Q. Payment processing, refunds, and settlement


| Identifier | Priority | Requirement |
|---|---|---|
| FR-183 | 5 | The system shall provide a centralized payment-processing capability for Consultant session payments and Company review-service payments. |
| FR-184 | 5 | The system shall integrate with Stripe as the primary payment gateway for all supported online payment transactions. |
| FR-185 | 5 | The system shall create and manage a Stripe PaymentIntent for each payable Consultant booking transaction. |
| FR-186 | 5 | The system shall place a payment hold on the Student’s selected payment method when a paid Consultant booking request is submitted. |
| FR-187 | 5 | The system shall capture payment only after the Consultant accepts the booking request. |
| FR-188 | 5 | The system shall release the payment hold automatically if the Consultant declines the request or if the booking request expires without response. |
| FR-189 | 5 | The system shall support Stripe Connect onboarding for Consultants so that Consultant payouts can be transferred to the Consultant’s linked financial account. |
| FR-190 | 5 | The system shall retain a configurable platform service fee from each Consultant payment and transfer the remaining net amount to the Consultant payout flow. |
| FR-191 | 5 | The system shall support Company-related payment and refund transactions for application-review services according to platform-defined pricing, refund, and settlement rules. |
| FR-192 | 5 | The system shall record payment details including transaction identifier, amount, payment status, capture status, refund status, payer, payee, payment date, and related business record reference. |
| FR-193 | 5 | The system shall support full and partial refunds according to the configured platform refund policy for Consultant session cancellations, no-show cases, and other approved payment-reversal scenarios. |
| FR-194 | 5 | The system shall generate and send a payment confirmation or receipt to the Student after successful payment capture. |
| FR-195 | 5 | The system shall validate and log Stripe webhook events to ensure secure, idempotent processing of capture, refund, payout, and related payment events. |
| FR-196     | 4 | The system should provide payment history and settlement history views for Students, Consultants, Companies, and Admins according to their authorized access scope.   |
| FR-197 | 5 | The system shall apply the active financial configuration rule when a payable transaction is created or calculated. |
| FR-198 | 5 | The system shall use the financial values stored for the original transaction when executing payment, refund, and settlement logic. |
| FR-199 | 5 | The system shall recalculate refund impact based on the financial basis of the original transaction. |
| FR-200 | 5 | The system shall preserve, within each transaction record, the applied fee, portal-share, and payout values used at the time of calculation. |

## R. Portal profit share


| Identifier | Priority | Requirement |
|---|---|---|
| FR-201 | 5 | The system shall calculate and retain a configurable portal profit share from each successfully captured Consultant payment. |
| FR-202 | 5 | The system shall use a default Consultant portal profit-share percentage of 10% unless overridden by Admin-configured payment settings. |
| FR-203 | 5 | The system shall calculate and retain a configurable portal profit share from each successfully captured Company review-service payment. |
| FR-204 | 4 | The system should use a default Company portal profit-share percentage of 10% unless overridden by Admin-configured payment settings. |
| FR-205 | 5 | The system shall record for each paid transaction the gross amount, portal share percentage, portal share amount, net payee amount, refund impact, and payout status. |
| FR-206 | 5 | The system shall recalculate portal profit share automatically when a full or partial refund affects a previously captured payment. |
| FR-207 | 5 | The system shall allow Admins to configure portal-share percentages separately for Consultant payments and Company payments. |
| FR-208    | 5 | The system shall expose portal-share and net-payout values to authorized Admin reporting and financial audit views.   |
| FR-209 | 5 | The system shall calculate platform share, net payout, and refund impact according to the applicable financial configuration rule. |
| FR-210 | 5 | The system shall preserve financial calculation snapshots for reporting and audit purposes. |
| FR-211 | 5 | The system shall expose gross amount, platform share, and net payout values in authorized financial reporting and audit views. |

# 2. User Stories

## A. Authentication, Access, and Onboarding


| Identifier | User story | size |
|---|---|---|
| US-001 | As a Guest, I can view the public home page so that I can understand the platform before registering. | 2pt |
| US-002 | As a Guest, I can register using email and password so that I can create an account on Scholar-Path. | 3pt |
| US-003 | As a Guest, I can register or log in using Google so that I can access the platform faster. | 3pt |
| US-004 | As a Guest, I can register or log in using Microsoft so that I can access the platform faster. | 3pt |
| US-005 | As a User, I can log in using my credentials so that I can access my role-based dashboard. | 3pt |
| US-006 | As a User, I can reset my password through an email link so that I can recover access to my account securely. | 4pt |
| US-007 | As an Unassigned user, I can complete onboarding so that I can activate my intended account role. | 4pt |
| US-008 | As an Unassigned user, I can choose Student during onboarding so that I can activate my account immediately without admin review. | 3pt |
| US-009 | As an Unassigned user, I can choose Company during onboarding and submit verification documents so that the Admin can authorize my company account. | 5pt |
| US-010 | As an Unassigned user, I can choose Consultant during onboarding and submit verification documents so that the Admin can authorize my consultant account. | 5pt |
| US-011 | As a Student, I can request an upgrade to Consultant so that I can keep my student account and also provide consulting services. | 5pt |
| US-012 | As a Student-Consultant user, I can switch my active mode inside the same session so that the full website adapts to Student view or Consultant view without logging out. | 5pt |
| US-013 | As the system, I can lock an account after repeated failed login attempts so that unauthorized access is reduced. | 3pt |
| US-014 | As the system, I can block protected pages for unauthenticated users so that the platform remains gated. | 2pt |

## B. Profile and Account Management


| Identifier | User story | size |
|---|---|---|
| US-015 | As a Student, I can edit my profile information so that recommendations, eligibility checks, and applications are more accurate. | 3pt |
| US-016 | As a Company, I can manage my organization profile so that students can trust my scholarship listings. | 3pt |
| US-017 | As a Consultant, I can manage my expertise, credentials, and session fee so that students can evaluate and book my services. | 4pt |
| US-018 | As a User, I can view my profile completeness so that I know what information is still missing. | 2pt |
| US-019 | As a User, I can update my password and security settings so that my account stays secure. | 3pt |

## C. Scholarship Discovery and Listing Management


| Identifier | User story | size |
|---|---|---|
| US-020 | As a Student, I can browse scholarship listings so that I can explore available opportunities. | 3pt |
| US-021 | As a Student, I can search scholarships by keyword so that I can find relevant opportunities faster. | 3pt |
| US-022 | As a Student, I can filter scholarships by country, deadline, funding type, academic level, and tags so that I can narrow results to my needs. | 5pt |
| US-023 | As a Student, I can view scholarship details so that I can understand eligibility, deadlines, and application requirements. | 3pt |
| US-024 | As a Student, I can bookmark scholarships so that I can return to them later. | 2pt |
| US-025 | As a Company, I can create an in-app scholarship listing so that students can apply directly on the platform. | 5pt |
| US-026 | As a Company, I can define application form fields for a listing so that I can collect the right applicant information. | 5pt |
| US-027 | As a Company, I can define required application documents for a listing so that I can receive complete applications. | 4pt |
| US-028 | As a Company, I can edit, archive, and manage my scholarship listings so that I can keep them accurate and current. | 4pt |
| US-029 | As an Admin, I can create an external-URL scholarship listing so that students can track applications that happen outside the platform. | 4pt |
| US-030 | As an Admin, I can feature selected scholarships so that important opportunities gain more visibility. | 2pt |

## D. In-App Application and External Tracking


| Identifier | User story | size |
|---|---|---|
| US-031 | As a Student, I can start an in-app scholarship application so that I can apply directly from the platform. | 4pt |
| US-032 | As a Student, I can save my application as a draft so that I can complete it later before the deadline. | 3pt |
| US-033 | As a Student, I can upload and attach documents from my document vault so that I can submit the required paperwork. | 5pt |
| US-034 | As a Student, I can submit an application so that the Company can review it. | 3pt |
| US-035 | As a Student, I can track my application timeline and status changes so that I always know where I stand. | 4pt |
| US-036 | As a Student, I can withdraw my application while it is still eligible for withdrawal and has not reached an Accepted or Rejected final outcome so that I can stop the process if needed.  | 3pt |
| US-037 | As a Student, I can reapply after withdrawal if the scholarship is still open so that I still have a chance to compete later. | 3pt |
| US-038 | As a Student, I can initiate an external application from an Admin-created listing so that I can continue the process on the external website. | 3pt |
| US-039 | As a Student, I can manually track the status of an external application so that I can manage opportunities outside the platform. | 4pt |
| US-040 | As a Student, I can add notes to my external application record so that I can remember progress and next actions. | 2pt |
| US-041 | As a Company, I can view submitted applications for my listings so that I can evaluate candidates. | 4pt |
| US-042 | As a Company, I can update application statuses such as Under Review, Shortlisted, Accepted, or Rejected so that students receive progress updates. | 4pt |
| US-043 | As the system, I can prevent duplicate active applications for the same student and scholarship so that application data stays valid. | 3pt |
| US-044 | As the system, I can lock accepted and rejected applications as read-only so that final decisions are preserved. | 2pt |

## E. Company review, payment, and rating


| Identifier | User story | size |
|---|---|---|
| US-045 | As a Company, I can review student applications and attached documents so that I can assess eligibility and paperwork quality. | 5pt |
| US-046 | As a Company, I can manage review outcomes for applications so that the scholarship process moves forward properly. | 4pt |
| US-047 | As the platform, I can record and reverse Company-side review-related payments according to configured refund rules so that review services can be monetized, refunded when approved, and tracked.  | 5pt |
| US-048 | As an Admin, I can configure Company review pricing rules so that the business model can be controlled centrally. | 4pt |
| US-049 | As a Student, I can rate and review a Company after my scholarship application reaches a final review outcome so that other students can understand the quality of the Company’s review experience. | 3pt |
| US-050 | As a Company, I can view ratings and reviews submitted by Students so that I can monitor my reputation and improve my review process. | 2pt |
|  |  |  |

## F. Consultant booking, payment, and rating


| Identifier | User story | size |
|---|---|---|
| US-051 | As a Student, I can browse Consultant profiles so that I can choose the right expert for guidance. | 3pt |
| US-052 | As a Student, I can view Consultant expertise, credentials, availability, ratings, and fee so that I can decide whether to book. | 4pt |
| US-053 | As a Consultant, I can manage my availability slots so that students can request appointments with me. | 4pt |
| US-054 | As a Student, I can request a booking with a Consultant so that I can receive personalized support. | 4pt |
| US-055 | As the platform, I can create a Stripe payment hold when a Student submits a booking request so that payment is secured before confirmation. | 5pt |
| US-056 | As a Consultant, I can accept or reject a booking request so that I control my appointments. | 4pt |
| US-057 | As the platform, I can capture payment only after Consultant acceptance so that Students are not charged for unconfirmed sessions. | 5pt |
| US-058 | As the platform, I can release the payment hold when a booking is rejected or expires so that the Student is not charged unfairly. | 4pt |
| US-059 | As a Student, I can cancel a request before Consultant acceptance and receive a full refund so that I am not penalized for an unconfirmed session. | 3pt |
| US-060 | As a Student, I can receive a full refund if I cancel more than 24 hours before an accepted session so that early cancellation is treated fairly. | 3pt |
| US-061 | As a Student, I can receive a 50% refund if I cancel less than 24 hours before an accepted session so that the refund policy is enforced consistently. | 3pt |
| US-062 | As a Student, I can receive a full refund if the Consultant cancels after acceptance so that I am protected from provider-side cancellation. | 3pt |
| US-063 | As a Student, I can receive a full refund if the Consultant is marked as a no-show so that missed consultant sessions do not cost me unfairly. | 3pt |
| US-064 | As a Consultant, I can retain the no-show outcome policy when a Student misses the session so that the platform enforces attendance rules consistently. | 3pt |
| US-065 | As a Student, I can view my booking and refund status so that I can track my financial and appointment outcomes. | 3pt |
| US-066 | As a Consultant, I can view my booking decisions and payment outcomes so that I can manage my work and earnings. | 3pt |
| US-067 | As a Student, I can rate and review a Consultant after my session reaches a completed or closed state so that other students can understand the quality of the Consultant’s service. | 3pt |
| US-068 | As a Consultant, I can view ratings and reviews submitted by Students so that I can monitor my reputation and improve my service quality. | 2pt |
| US-069 | As an Admin, I can moderate Consultant ratings and reviews so that inappropriate or abusive feedback can be controlled. | 3pt |
| US-070 | As the platform, I can suspend new bookings for low-rated Consultants pending Admin review so that service quality is maintained. | 4pt |
| US-071 | As the platform, I can log Stripe webhook events so that payment and refund processing is reliable and idempotent. | 5pt |

## G. Community


| Identifier | User story | size |
|---|---|---|
| US-072 | As an authenticated user, I can create a community post so that I can ask questions or share experiences. | 3pt |
| US-073 | As an authenticated user, I can reply to posts so that I can participate in discussions. | 3pt |
| US-074 | As an authenticated user, I can upvote or downvote posts and replies so that useful content becomes more visible. | 3pt |
| US-075 | As the platform, I can prevent users from voting on their own posts so that the forum remains fair. | 2pt |
| US-076 | As an authenticated user, I can flag inappropriate content so that Admins can moderate the community. | 3pt |
| US-077 | As the platform, I can auto-hide a post after 3 valid distinct flags so that harmful content is contained quickly. | 3pt |
| US-078 | As an Admin, I can moderate flagged posts, restore them, remove them, or suspend offending users so that the community remains safe. | 5pt |

## H. Chat


| Identifier | User story | size |
|---|---|---|
| US-079 | As an authenticated user, I can start a one-to-one chat with another user so that I can communicate directly and I can see their status if they are online or offline. | 4pt |
| US-080 | As an authenticated user, I can view chat history so that I can continue conversations over time. | 3pt |
| US-081 | As an authenticated user, I can block another user so that they cannot initiate new chats with me. | 3pt |

## I. AI features


| Identifier | User story | size |
|---|---|---|
| US-082 | As a Student, I can receive personalized scholarship recommendations so that I can discover opportunities that fit my profile. | 5pt |
| US-083 | As a Student, I can view a match score and explanation so that I understand why a scholarship is recommended. | 4pt |
| US-084 | As a Student, I can use an eligibility checker on a scholarship so that I know whether I meet the main criteria before applying. | 4pt |
| US-085 | As a Student, I can receive criterion-by-criterion eligibility feedback so that I understand what is missing or matched. | 4pt |
| US-086 | As an authenticated user, I can ask the AI chatbot questions so that I can get guidance about scholarships and study-abroad preparation. | 4pt |
| US-087 | As the platform, I can display an AI disclaimer on each generated answer so that users understand the advisory nature of the response. | 2pt |

## J. Resources hub


| Identifier | User story | size |
|---|---|---|
| US-088 | As an authenticated user, I can browse articles in the Resources Hub so that I can benefit from educational content. | 3pt |
| US-089 | As an authenticated user, I can search and filter articles so that I can find relevant content quickly. | 3pt |
| US-090 | As a Consultant, I can publish an article so that I can share expertise with students. | 4pt |
| US-091 | As a Company, I can publish an article so that I can share scholarship and application guidance with students. | 4pt |
| US-092 | As an Admin, I can publish an article so that the platform can provide official curated content. | 3pt |
| US-093 | As an Admin, I can moderate article visibility so that only appropriate resources appear to users. | 4pt |
| US-094 | As an Admin, I can feature selected articles so that important content gains visibility. | 2pt |

## J.1 Validation rule stories for publishing, flagging, and chat blocking


| Identifier | User story | size |
|---|---|---|
| US-095 | As an authenticated user, I can receive validation feedback when publishing a community post so that I can correct invalid content before it is posted. | 2pt |
| US-096 | As an authenticated user, I can receive validation feedback when publishing a reply/comment so that I can correct invalid content before it is posted. | 2pt |
| US-097 | As the platform, I can enforce flagging rules so that content reports are valid, fair, and not duplicated. | 3pt |
| US-098 | As an authenticated user, I can block another user from starting new chats or sending new direct messages to me so that my communication boundaries are respected. | 3pt |
| US-099 | As an authenticated user, I can unblock a previously blocked user so that direct communication can resume when I choose. | 2pt |
| US-100 | As a Consultant, I can receive validation feedback when publishing an article so that I can correct missing or invalid fields before submission. | 2pt |
| US-101 | As a Company, I can receive validation feedback when publishing an article so that I can correct missing or invalid fields before submission. | 2pt |
| US-102 | As an Admin, I can receive validation feedback when publishing an article so that official content also satisfies the configured publishing rules before it becomes visible. | 2pt |

## K. Notifications


| Identifier | User story | size |
|---|---|---|
| US-103 | As a Student, I can receive a notification when my application is submitted so that I know the process started successfully. | 2pt |
| US-104 | As a Student, I can receive notifications when my application status changes so that I stay informed. | 3pt |
| US-105 | As a Company, I can receive notifications when a new application is submitted to one of my listings so that I can review it promptly. | 2pt |
| US-106 | As a Company, I can receive notifications for review-payment events, successful payment processing, refund impact, payout status, and other payment-related updates so that I can track my financial activity accurately. | 3pt |
| US-107 | As a Company, I can receive notifications when a Student submits a rating or review about my application-review service so that I can monitor feedback and reputation updates. | 2pt |
| US-108 | As a Student, I can receive deadline reminders for bookmarked scholarships and saved drafts so that I do not miss important dates. | 3pt |
| US-109 | As a Student, I can receive booking confirmation, rejection, expiry, and refund notifications so that I understand my consultant-session outcome. | 3pt |
| US-110 | As a Consultant, I can receive notifications for new reservation requests, booking confirmations, cancellations, expirations, and no-show outcomes so that I can manage my schedule and respond on time. | 3pt |
| US-111 | As a Consultant, I can receive notifications when a Student submits feedback or a rating after a completed session so that I can monitor my service quality and reputation. | 2pt |
| US-112 | As a Consultant, I can receive notifications for payment capture, refund impact, payout status, and other payment-related events so that I can track my financial outcomes accurately. | 3pt |
| US-113 | As an Admin, I can send broadcast announcements so that I can communicate important platform-wide messages.  | 3pt |
| US-114 | As a user, I can receive notifications by email so that I can stay informed even when I am not logged into the platform. | 3pt |
| US-115 | As a user, I can review my previous in-platform notifications so that I can track important updates later. | 3pt |
| US-116 | As a user, I can distinguish between read and unread notifications so that I can identify which updates still need my attention. | 2pt |
| US-117 | As the platform, I can send supported notification events through both in-platform and email channels so that users receive important updates reliably. | 4pt |

## L. Admin portal and oversight


| Identifier | User story | size |
|---|---|---|
| US-118 | As an Admin, I can view and search all users by role, status, and registration data so that I can manage the platform effectively. | 4pt |
| US-119 | As an Admin, I can approve or reject Company onboarding requests so that only verified companies gain access. | 4pt |
| US-120 | As an Admin, I can approve or reject Consultant onboarding requests so that only verified consultants gain access. | 4pt |
| US-121 | As an Admin, I can approve or reject Student-to-Consultant upgrade requests so that the dual-role process is controlled. | 4pt |
| US-122 | As an Admin, I can activate, suspend, deactivate, or delete accounts so that I can enforce platform policy. | 4pt |
| US-123 | As an Admin, I can manage scholarship listings, article content, categories, and moderation queues so that platform content remains organized and compliant. | 5pt |
| US-124 | As an Admin, I can view analytics for users, listings, applications, bookings, resources, and AI usage so that I can monitor business performance. | 5pt |
| US-125  | As an Admin, I can export analytics and reports so that I can use the data outside the platform when needed. | 3pt |
| US-126 | As an Admin, I can manage payment fee and portal profit-share configurations from the Admin Dashboard so that platform financial policies can be controlled without code changes. | 4pt |
| US-127 | As an Admin, I can configure Consultant payment fee rules from the Admin Dashboard so that Consultant booking payments follow the platform’s financial policy. | 4pt |
| US-128 | As an Admin, I can configure Company review-service payment fee rules from the Admin Dashboard so that Company payment flows follow the platform’s financial policy. | 4pt |
| US-129 | As an Admin, I can configure portal profit-share percentages separately for Consultant payments and Company review-service payments so that platform revenue is controlled centrally. | 3pt |
| US-130 | As an Admin, I can create, update, activate, deactivate, or archive financial configuration rules from the Admin Dashboard so that payment policy changes can be managed without changing the codebase. | 4pt |
| US-131 | As an Admin, I can preview the fee, portal share, total platform retention, and net payout calculation before saving a financial rule so that I understand its business impact. | 3pt |
| US-132 | As an Admin, I can define when a financial configuration rule becomes effective so that policy changes start at the intended business time. | 3pt |
| US-133 | As an Admin, I can view current and historical financial configuration rules from the Admin Dashboard so that I can track which payment policy is active and which policies were used previously. | 3pt |
| US-134 | As an Admin, I can view the history of financial configuration changes, including who changed them and when, so that these updates remain traceable and auditable. | 3pt |
| US-135 | As an Admin, I can validate fee values and portal profit-share percentages before saving so that invalid financial rules are blocked. | 3pt |
| US-136 | As an Admin, I can ensure that only one active rule exists per payment type so that the system always has one clear financial policy in effect. | 3pt |
| US-137 | As an Admin, I can search and filter financial configuration records by payment type, status, and effective date so that I can manage configurations efficiently. | 3pt |
| US-138 | As an Admin, I can simulate sample transaction amounts against a financial rule before activation so that I can verify the resulting fee deduction, portal share, and net payout. | 3pt |

## M. Audit, compliance, and system integrity


| Identifier | User story | size |
|---|---|---|
| US-139 | As the platform, I can record audit logs for critical actions so that important system changes are traceable. | 4pt |
| US-140 | As the platform, I can record login attempts so that suspicious authentication behavior can be monitored. | 3pt |
| US-141 | As the platform, I can preserve student history when a Student becomes a Consultant so that prior applications, bookmarks, and records remain valid. | 4pt |
| US-142 | As a User, I can request my data export or deletion workflow so that compliance processes can be supported. | 4pt |

## N. Payment processing, refunds, and settlement


| Identifier | User story | size |
|---|---|---|
| US-143 | As a Student, I can pay for a Consultant booking through Stripe so that my reservation can be processed securely. | 4pt |
| US-144 | As a Consultant, I can onboard to Stripe Connect so that I can receive payouts for completed paid sessions. | 4pt |
| US-145 | As a Company, I can receive payments related to application-review services according to the platform business rules so that my review work can be monetized. | 4pt |
| US-146 | As a Student, I can receive a receipt after a successful payment capture so that I have proof of payment. | 2pt |
| US-147 | As an Admin, I can define or update payment fee and settlement rules so that the platform business model can be controlled centrally. | 4pt |
| US-148    | As the platform, I can process payment capture, release, refund, and webhook events reliably so that payment records remain accurate and idempotent. | 5pt |
| US-149 | As the platform, I can apply the active financial configuration rule when a payable transaction is created or calculated so that payment processing follows the current financial policy. | 4pt |
| US-150 | As the platform, I can use the financial values stored for the original transaction when executing payment, refund, and settlement logic so that financial processing remains consistent and accurate. | 4pt |
| US-151 | As the platform, I can recalculate refund impact based on the financial basis of the original transaction so that refunds reflect the correct financial rule context. | 4pt |
| US-152 | As the platform, I can preserve the applied fee, portal-share, and payout values within each transaction record so that the exact calculation basis remains available for later processing and review. | 4pt |

## O. Portal profit share


| Identifier | User story | size |
|---|---|---|
| US-153 | As an Admin, I can configure the portal profit-share percentage for Consultant payments so that the platform revenue model is controlled centrally. | 3pt |
| US-154 | As an Admin, I can configure the portal profit-share percentage for Company review-service payments so that the platform revenue model is controlled centrally. | 3pt |
| US-155 | As a Consultant, I can view the portal share deducted from each paid booking so that I understand my net payout. | 2pt |
| US-156 | As a Company, I can view the portal share deducted from each paid review-service transaction so that I understand my net payout. | 2pt |
| US-157     | As an Admin, I can view gross amount, portal share, net payout, and refund impact for each financial transaction so that I can audit platform revenue accurately. | 4pt |
| US-158 | As the platform, I can calculate platform share, net payout, and refund impact according to the applicable financial configuration rule so that financial outcomes are consistent with the configured business policy. | 4pt |
| US-159 | As the platform, I can preserve financial calculation snapshots for reporting and audit purposes so that historical financial outcomes remain traceable and verifiable. | 4pt |

3. Work Backlog


| Backlog ID | Module / Epic | Included User Stories | Total Story Points | Priority | Dependency | Iteration no. | Estimated work duration |
|---|---|---|---|---|---|---|---|
| PB-001 | Authentication, Access, and Onboarding | US-001 to US-014 | 50pt | Essential | None | Iteration 1 | 5 days |
| PB-011 | Admin Portal and Oversight | US-118 to US-138 | 76pt | Essential | PB-001 | Iteration 1 | 4 days |
| PB-002 | Profile and Account Management | US-015 to US-019 | 15pt | Essential | PB-001 | Iteration 1 | 3 days |
| PB-003 | Scholarship Discovery and Listing Management | US-020 to US-030 | 40pt | Essential | PB-001, PB-002 | Iteration 2 | 4 days |
| PB-004 | In-App Application and External Tracking | US-031 to US-044 | 47pt | Essential | PB-001, PB-002, PB-003 | Iteration 2 | 5 days |
| PB-013 | Payment Processing and Settlement | US-143 to US-152 | 39pt | Essential | PB-001 | Iteration 2 | 3 days |
| PB-006 | Consultant Booking, Payment, and Rating | US-051 to US-070 | 74pt | Essential | PB-001, PB-002, PB-013 | Iteration 3 | 5 days |
| PB-005 | Company Review, Payment, and Rating | US-045 to US-050 | 23pt | High | PB-003, PB-004 | Iteration 3 | 4 days |
| PB-010 | Notifications | US-103 to US-117 | 41pt | High | PB-004, PB-005, PB-006, PB-011 | Iteration 3 | 3 days |
| PB-014 | Portal Profit Share | US-153 to US-159 | 22pt | High | PB-013 | Iteration 3 | 2 days |
| PB-007 | Community and Chat | US-072 to US-081, US-095 to US-099 | 44pt | High | PB-001 | Iteration 4 | 4 days |
| PB-009 | Resources Hub | US-088 to US-094, US-100 to US-102 | 29pt | High | PB-001, PB-011 | Iteration 4 | 3 days |
| PB-008 | AI Features | US-082 to US-087 | 23pt | High | PB-002, PB-003, PB-004 | Iteration 4 | 4 days |
| PB-012 | Audit, Compliance, and System Integrity | US-139 to US-142 | 15pt | High | PB-001, PB-011 | Iteration 4 | 2 days |

### 4.Project Duration

▶  Total work size = ∑ points-for-story i   (i = 1..N)

▶  For our case study:

▶  Total work size = 50 + 76 + 15 + 40 + 47 + 39 + 74 + 23 + 41 + 22 + 44 + 29 + 23 + 15 = 538 points.

▶  Project duration =  Path size 

                                                 Travel velocity

▶  = 538 / 26.9 = 20 working days (may extend to 1 month)

## 5.System Actors 

## 5.1 Primary human actors — actor’s goal and use case name


| Actor ID | Actor Name | Actor Type | Actor’s Goal | Use case name |
|---|---|---|---|---|
| ACT-01 | Guest | Primary human actor | Explore the platform publicly | View Home Page – (UC-01) |
| ACT-01 | Guest | Primary human actor | Create a new account | Register Account – (UC-02) |
| ACT-01 | Guest | Primary human actor | Access the system using credentials | Log In – (UC-03) |
| ACT-01 | Guest | Primary human actor | Access the system using Google | Authenticate via Google – (UC-04) |
| ACT-01 | Guest | Primary human actor | Access the system using Microsoft | Authenticate via Microsoft – (UC-05) |
| ACT-01 | Registered user  | Primary human actor | Recover account access | Request Password Reset – (UC-06) |
| ACT-02 | Unassigned User | Primary human actor / transitional state | Finish account activation | Complete Onboarding – (UC-07) |
| ACT-02 | Unassigned User | Primary human actor / transitional state | Activate direct student access | Select Student Role – (UC-08) |
| ACT-02 | Unassigned User | Primary human actor / transitional state | Request organization authorization | Submit Company Onboarding Request – (UC-09) |
| ACT-02 | Unassigned User | Primary human actor / transitional state | Request consultant authorization | Submit Consultant Onboarding Request – (UC-10) |
| ACT-03 | Student | Primary human actor | Upgrade to consultant role | Submit Student-to-Consultant Upgrade Request – (UC-11) |
| ACT-03 | Student | Primary human actor | Switch between student and consultant views | Switch Active Role – (UC-12) |
| ACT-03 | Student | Primary human actor | Maintain personal account information | Manage User Profile – (UC-13) |
| ACT-03 | Student | Primary human actor | Discover available scholarships | Browse/Search Scholarships – (UC-14) |
| ACT-03 | Student | Primary human actor | Read detailed scholarship information | View Scholarship Details – (UC-15) |
| ACT-03 | Student | Primary human actor | Save scholarships for later | Bookmark Scholarship – (UC-16) |
| ACT-03 | Student | Primary human actor | Apply directly through the platform | Apply to In-App Scholarship – (UC-18) |
| ACT-03 | Student | Primary human actor | Track and manage submitted applications | Track and Manage Application – (UC-19) |
| ACT-03 | Student | Primary human actor | Apply to external opportunities and track them | Initiate and Track External Application – (UC-20) |
| ACT-03 | Student | Primary human actor | Book a paid or free consultant session | Book Consultant Session – (UC-22) |
| ACT-03 | Student | Primary human actor | Complete payment and receive refunds if eligible | Process Payments and Refunds – (UC-24) |
| ACT-03 | Student | Primary human actor | Submit ratings and reviews | Submit and Manage Ratings/Reviews – (UC-25) |
| ACT-03 | Student | Primary human actor | Get AI support and recommendations | Use AI Services – (UC-26) |
| ACT-03 | Student | Primary human actor | Join discussions and direct chats | Use Community and Chat – (UC-27) |
| ACT-03 | Student | Primary human actor | Read knowledge content | Access and Publish Articles – (UC-28) |
| ACT-03 | Student | Primary human actor | Receive application, booking, and reminder updates | Manage Notifications – (UC-29) |
| ACT-04 | Company | Primary human actor | Maintain organization account information | Manage User Profile – (UC-13) |
| ACT-04 | Company | Primary human actor | Create and manage scholarship listings | Create/Manage In-App Scholarship Listing – (UC-17) |
| ACT-04 | Company | Primary human actor | Review student submissions and decide outcomes | Review Applications and Update Status – (UC-21) |
| ACT-04 | Company | Primary human actor | Receive and track review-service payments | Process Payments and Refunds – (UC-24) |
| ACT-04 | Company | Primary human actor | View ratings and reviews from students | Submit and Manage Ratings/Reviews – (UC-25) |
| ACT-04 | Company | Primary human actor | Publish educational content | Access and Publish Articles – (UC-28) |
| ACT-04 | Company | Primary human actor | Receive application, payment, and rating updates | Manage Notifications – (UC-29) |
| ACT-05 | Consultant | Primary human actor | Maintain consultant profile information | Manage User Profile – (UC-13) |
| ACT-05 | Consultant | Primary human actor | Manage availability and booking requests | Manage Consultant Availability and Booking Decisions – (UC-23) |
| ACT-05 | Consultant | Primary human actor | Receive booking payments, payouts, and refunds impact | Process Payments and Refunds – (UC-24) |
| ACT-05 | Consultant | Primary human actor | View ratings and reviews from students | Submit and Manage Ratings/Reviews – (UC-25) |
| ACT-05 | Consultant | Primary human actor | Participate in community contribution | Use Community and Chat – (UC-27) |
| ACT-05 | Consultant | Primary human actor | Publish educational articles | Access and Publish Articles – (UC-28) |
| ACT-05 | Consultant | Primary human actor | Receive reservation, rating, and payment updates | Manage Notifications – (UC-29) |
| ACT-06 | Admin | Primary human actor | Approve and control platform roles and permissions | Administer Platform and Approvals – (UC-30) |
| ACT-06 | Admin | Primary human actor | Moderate content and user behavior | Moderate Content and Users – (UC-31) |
| ACT-06 | Admin | Primary human actor | Configure payment settings and portal share | Configure Financial Rules and Portal Profit Share – (UC-32) |
| ACT-06 | Admin | Primary human actor | Monitor platform performance and reports | View Analytics and Reports – (UC-33) |
| ACT-06 | Admin | Primary human actor | Manage articles, listings, users, and categories | Administer Platform and Approvals – (UC-30) |
| ACT-06 | Admin | Primary human actor | Receive pending approval requests and system alerts | Manage Notifications – (UC-29) |

## 5.2 Supporting external actors — actor’s goal and use case name


| Actor ID | Actor Name | Actor Type | Actor’s Goal | Use case name |
|---|---|---|---|---|
| ACT-07 | Stripe Payment Gateway | Supporting external actor | Create and manage payment transactions | Process Payments and Refunds – (UC-24) |
| ACT-07 | Stripe Payment Gateway | Supporting external actor | Support payout and revenue-share handling | Configure Financial Rules and Portal Profit Share – (UC-32) |
| ACT-08 | Google OAuth Provider | Supporting external actor | Authenticate users through Google SSO | Authenticate via Google – (UC-04) |
| ACT-09 | Microsoft OAuth Provider | Supporting external actor | Authenticate users through Microsoft SSO | Authenticate via Microsoft – (UC-05) |
| ACT-10 | Email / Notification Service | Supporting external actor | Send reset links and account-related messages | Request Password Reset – (UC-06) |
| ACT-10 | Email / Notification Service | Supporting external actor | Deliver system notifications and reminders | Manage Notifications – (UC-29) |
| ACT-10 | Email / Notification Service | Supporting external actor | Deliver approval outcomes and admin-triggered messages | Administer Platform and Approvals – (UC-30) |

Use Case Diagrams

View home page:

![SRS figure](srs-images/image1.png)

Figure 1: View home page

Register Account:

![SRS figure](srs-images/image2.png)

Figure 2: Diagram

 Log In:

![SRS figure](srs-images/image3.png)

Figure 3: Register Account

Authenticate via Google:

![SRS figure](srs-images/image4.png)

Figure 4: Diagram

Authenticate via Microsoft:

![SRS figure](srs-images/image5.png)

Figure 5: Diagram

Request Password Reset:

![SRS figure](srs-images/image6.png)

Figure 6: Diagram

![SRS figure](srs-images/image7.png)

*Complete Onboarding:*

Figure 7: Diagram

 Select Student Role:

![SRS figure](srs-images/image8.png)

Figure 8: Diagram

Submit Company Onboarding Request:

![SRS figure](srs-images/image9.png)

Figure 9: Diagram

 Submit Consultant Onboarding Request:

![SRS figure](srs-images/image10.png)

Figure 10: Diagram

Submit Student-to-Consultant Upgrade Request:

![SRS figure](srs-images/image11.png)

Figure 11: Diagram

Switch Active Role:

![SRS figure](srs-images/image12.png)

Figure 12: Figure 8: Diagram

Manage User Profile:

1-

![SRS figure](srs-images/image13.png)

Figure 13: Diagram

2-

![SRS figure](srs-images/image14.png)

Figure 14: Figure 13: Diagram

Browse/Search Scholarships:

![SRS figure](srs-images/image15.png)

Figure 15: Diagram

 View Scholarship Details:

![SRS figure](srs-images/image16.png)

Figure 16: Diagram

Scholarship Management Flow : 

![SRS figure](srs-images/image17.jpg)

Figure 17: Browse/Search Scholarships

Application Management Flow:

![SRS figure](srs-images/image18.jpg)

Figure 18: Figure 15: Diagram

In-APP & External Application:

![SRS figure](srs-images/image19.jpg)

Figure 19: Application Management Flow

Track and Manage Application:

![SRS figure](srs-images/image20.png)

Figure 20: Diagram

Initiate and Track External Application:

![SRS figure](srs-images/image21.png)

Figure 21: Diagram

Review Applications and Update Status:

![SRS figure](srs-images/image22.png)

Figure 22: Track and Manage Application

Company Review and Payment:

![SRS figure](srs-images/image23.png)

Figure 23: Figure 20: Diagram

Consultant Booking and Payment system:

![SRS figure](srs-images/image24.jpg)

Figure 24: Figure 20: Diagram

Rating and Reviews Management:

![SRS figure](srs-images/image25.png)

Figure 25: Review Applications and Update Status

Community:

![SRS figure](srs-images/image26.png)

Figure 26: Diagram

Chat:

![SRS figure](srs-images/image27.png)

Figure 27: Diagram

AI Features Flow :

![SRS figure](srs-images/image28.png)

Figure 28: Diagram

## Resources hub:

![SRS figure](srs-images/image29.png)

Figure 29: Diagram

##  Notifications:

![SRS figure](srs-images/image30.png)

Figure 30: Figure 25: Review Applications and Update Status

![SRS figure](srs-images/image30.png)

Figure 31: Figure 27: Diagram

Administer Dashboard:

![SRS figure](srs-images/image31.png)

Figure 32: Diagram

Moderate Content and Users:

![SRS figure](srs-images/image32.png)

Figure 33: Diagram

Configure Financial Rules and Portal Profit Share:

![SRS figure](srs-images/image33.png)

Figure 34: Diagram

View Analytics and Reports:

![SRS figure](srs-images/image34.png)

Figure 35: Diagram

6.Use case Description 

use cases.


| Use Case 1 – (UC-01) | Use Case 1 – (UC-01) |
|---|---|
| Use Case Name | View Home Page |
| Use Case Type | Main |
| Related Requirements | FR-001, FR-002, FR-006 |
| Initiating Actor | Guest |
| Actor’s Goal | To access the public entry page of Scholar-Path and understand the platform before registration or login. |
| Participating Actors | None |
| Preconditions | 1. The Scholar-Path web application is available. 2. The Home Page route is configured as publicly accessible. 3. The user is not required to authenticate before accessing the Home Page. |
| Postconditions | 1. The Guest can view the public Home Page. 2. The Guest may proceed to Login or Register if they want to access protected features. 3. If the Guest attempts to access a protected feature, the system redirects the Guest to Login or Register. |
| Flow of Events for Main Success Scenario | 1. The Guest opens the Scholar-Path website. 2. The system receives the request for the Home Page. 3. The system verifies that the requested page is publicly accessible. 4. The system loads the Home Page content. 5. The system displays the Home Page to the Guest. 6. The Guest reviews the available public information and navigation options. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The Home Page service is temporarily unavailable. 1. The system cannot retrieve the Home Page content. 2. The system displays a temporary error or unavailable message. 5.1 The Guest clicks a protected feature from the Home Page. 1. The system detects that the selected destination requires authentication. 2. The system redirects the Guest to Login or Register. 6.1 The Guest selects Login or Register from the Home Page. 1. The system opens the selected public entry page. 2. The Guest continues to authentication or account creation. |


| Use Case 2 – (UC-02) | Use Case 2 – (UC-02) |
|---|---|
| Use Case Name | Redirect to Login / Register |
| Use Case Type | Extended |
| Related Requirements | FR-003, FR-006 |
| Initiating Actor | Guest |
| Actor’s Goal | To reach the appropriate authentication entry point after attempting to access a protected feature without being logged in. |
| Participating Actors | None |
| Preconditions | 1. The Guest is not authenticated. 2. The Guest is currently viewing the Home Page or another public entry page. 3. The Guest selects a feature, page, or action that requires authentication. |
| Postconditions | 1. The Guest is prevented from accessing the protected feature directly. 2. The system redirects the Guest to Login or Register. 3. The Guest can continue with authentication or account creation. |
| Flow of Events for Main Success Scenario | 1. The Guest browses the Home Page. 2. The Guest selects a protected feature, page, or action. 3. The system receives the request for the protected destination. 4. The system checks the authentication status of the requester. 5. The system detects that the requester is not authenticated. 6. The system blocks direct access to the protected destination. 7. The system redirects the Guest to Login or Register. 8. The Guest views the selected authentication entry page. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The system cannot validate the request because of a temporary system issue. 1. The system does not open the protected destination. 2. The system displays an error or temporary unavailable message. 7.1 The Login page is unavailable. 1. The system cannot load the Login page. 2. The system redirects the Guest to Register or displays an unavailable message. 7.2 The Register page is unavailable. 1. The system cannot load the Register page. 2. The system redirects the Guest to Login or displays an unavailable message. |


| Use Case 3 – (UC-03) | Use Case 3 – (UC-03) |
|---|---|
| Use Case Name | Register Account |
| Use Case Type | Main |
| Related Requirements | FR-007, FR-010, FR-011, FR-012, FR-021 |
| Initiating Actor | Guest |
| Actor’s Goal | To create a new account in Scholar-Path and begin onboarding. |
| Participating Actors | None |
| Preconditions | 1. The Guest is not authenticated. 2. The registration page is accessible. 3. The Guest has not already registered with the same email address. |
| Postconditions | 1. A new user account is created. 2. The account is stored with role state Unassigned. 3. Onboarding is marked as incomplete. 4. The user is redirected to the onboarding flow. |
| Flow of Events for Main Success Scenario | 1. The Guest opens the registration page. 2. The system displays the registration form. 3. The Guest enters the required registration data. 4. The Guest submits the registration form. 5. The system validates the entered data. 6. The system validates the email and password rules. 7. The system checks whether the email is already in use. 8. The system creates the user account. 9. The system assigns the initial role state as Unassigned. 10. The system marks onboarding as incomplete. 11. The system redirects the user to the onboarding flow. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 Required registration data is missing or invalid. 1. The system rejects the submission. 2. The system highlights the invalid or missing fields. 3. The Guest corrects the data and resubmits the form. 6.1 The email format is invalid. 1. The system displays an email validation error. 2. The Guest enters a valid email address. 6.2 The password does not satisfy the required rules. 1. The system displays the password rule violations. 2. The Guest enters a compliant password. 7.1 The email already exists in the system. 1. The system rejects the registration request. 2. The system informs the Guest that the email is already registered. 8.1 Account creation fails because of a technical error. 1. The system does not create the account. 2. The system displays an error message and asks the Guest to try again later. |


| Use Case 4 – (UC-04) | Use Case 4 – (UC-04) |
|---|---|
| Use Case Name | Validate E-mail and Password Rules |
| Use Case Type | Included |
| Related Requirements | FR-007, FR-021 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the entered e-mail and password satisfy the registration validation rules before account creation proceeds. |
| Participating Actors | Guest |
| Preconditions | 1. The Guest is on the registration page. 2. The Guest has entered registration data. 3. The registration form has been submitted or is being checked before submission is accepted. |
| Postconditions | 1. The system determines whether the entered e-mail and password are valid. 2. If the entered data is valid, the registration flow continues. 3. If the entered data is invalid, the system rejects the submission and displays the corresponding validation error. |
| Flow of Events for Main Success Scenario | 1. The Guest enters an e-mail address and password in the registration form. 2. The Guest submits the registration form. 3. The system receives the submitted registration data. 4. The system validates the e-mail format. 5. The system checks whether the e-mail satisfies the required registration rules. 6. The system validates the password against the security rules. 7. The system confirms that the password contains at least 8 characters, one uppercase letter, one digit, and one special character. 8. The system marks the e-mail and password validation as successful. 9. The system returns control to the Register Account use case so registration can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The e-mail format is invalid. 1. The system marks the e-mail as invalid. 2. The system displays an e-mail validation error message. 3. The Guest enters a valid e-mail address. 5.1 The e-mail does not satisfy registration rules. 1. The system rejects the submitted e-mail value. 2. The system displays the corresponding validation message. 6.1 The password is missing required characters or length. 1. The system detects that the password violates the password policy. 2. The system displays the password rule violations. 3. The Guest enters a compliant password. 8.1 Validation fails because of a temporary technical issue. 1. The system cannot complete the validation process. 2. The system displays an error message and asks the Guest to try again later. |


| Use Case 5 – (UC-05) | Use Case 5 – (UC-05) |
|---|---|
| Use Case Name | Show Validation Error |
| Use Case Type | Extended |
| Related Requirements | FR-007, FR-021 |
| Initiating Actor | System |
| Actor’s Goal | To inform the Guest that the submitted registration data is invalid and explain what must be corrected before registration can continue. |
| Participating Actors | Guest |
| Preconditions | 1. The Guest is on the registration page. 2. The Guest has entered registration data and submitted the form. 3. The system has detected one or more validation failures in the submitted data. |
| Postconditions | 1. The Guest is informed about the validation failure. 2. The invalid or missing fields are highlighted. 3. The registration process is paused until the Guest corrects the errors and resubmits the form. |
| Flow of Events for Main Success Scenario | 1. The Guest enters registration data and submits the registration form. 2. The system validates the submitted information. 3. The system detects one or more invalid or missing values. 4. The system rejects the current submission. 5. The system highlights the invalid or missing fields. 6. The system displays the corresponding validation error messages. 7. The Guest reviews the displayed errors. 8. The Guest corrects the invalid data. 9. The Guest resubmits the registration form. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The e-mail format is invalid. 1. The system highlights the e-mail field. 2. The system displays an invalid e-mail format message. 3.2 The password does not satisfy the password rules. 1. The system highlights the password field. 2. The system displays the password rule violations. 3.3 Required registration fields are missing. 1. The system highlights the missing fields. 2. The system displays a message asking the Guest to complete the required information. 6.1 The system cannot display the validation details because of a temporary technical issue. 1. The system displays a general error message. 2. The Guest is asked to try again later. |


| Use Case 6 – (UC-06) | Use Case 6 – (UC-06) |
|---|---|
| Use Case Name | Log In |
| Use Case Type | Main |
| Related Requirements | FR-003, FR-022, FR-023, FR-026, FR-027 |
| Initiating Actor | Registered User |
| Actor’s Goal | To access the Scholar-Path system using valid account credentials. |
| Participating Actors | None |
| Preconditions | 1. The user already has a registered account. 2. The login page is accessible. 3. The account is not permanently disabled. 4. The user is not currently authenticated. |
| Postconditions | 1. The user is authenticated successfully. 2. The system issues an access token and a refresh token. 3. The system redirects the user according to account state, role, and onboarding status. 4. If login fails repeatedly, the system may temporarily lock the account. |
| Flow of Events for Main Success Scenario | 1. The Registered User opens the login page. 2. The system displays the login form. 3. The Registered User enters email and password. 4. The Registered User submits the login request. 5. The system validates the entered credentials. 6. The system confirms that the account is active and not locked. 7. The system creates an authenticated session. 8. The system issues an access token and refresh token. 9. The system checks the user’s account state and onboarding status. 10. The system redirects the user to the correct next destination. 11. The user accesses the appropriate dashboard or next page. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The email or password is incorrect. 1. The system rejects the login attempt. 2. The system increments the failed login attempt counter. 3. The system displays an invalid credentials message. 5.2 The user selects Remember Me. 1. The system applies the Remember Me policy. 2. The system extends the refresh token lifetime according to configuration. 6.1 The account is temporarily locked after repeated failed attempts. 1. The system denies access. 2. The system informs the user that the account is temporarily locked. 9.1 The authenticated user is still Unassigned or onboarding is incomplete. 1. The system does not redirect the user to the normal dashboard. 2. The system redirects the user to the onboarding flow. 7.1 A temporary technical issue occurs during session creation. 1. The system cannot complete the login process. 2. The system displays an error message and asks the user to try again later. |


| Use Case 7 – (UC-07) | Use Case 7 – (UC-07) |
|---|---|
| Use Case Name | Validate Credentials |
| Use Case Type | Included |
| Related Requirements | FR-022, FR-026, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the submitted login credentials are correct and that the account is eligible for authentication. |
| Participating Actors | Registered User |
| Preconditions | 1. The Registered User is on the login page. 2. The Registered User has entered login credentials. 3. The login request has been submitted to the system. |
| Postconditions | 1. The system determines whether the submitted credentials are valid. 2. If the credentials are valid, the login process continues. 3. If the credentials are invalid, the login attempt is rejected and the failed-attempt counter is updated. 4. If the failed-attempt threshold is reached, the account may be temporarily locked. |
| Flow of Events for Main Success Scenario | 1. The Registered User enters email and password in the login form. 2. The Registered User submits the login request. 3. The system receives the submitted credentials. 4. The system locates the user account using the submitted email address. 5. The system verifies the submitted password against the stored password data. 6. The system checks the current account state and lock status. 7. The system confirms that the credentials are valid. 8. The system returns a successful validation result to the Log In use case so authentication can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 No account exists for the submitted email address. 1. The system cannot match the submitted email to a registered account. 2. The system marks the validation as failed. 3. The system returns an invalid credentials result. 5.1 The submitted password is incorrect. 1. The system detects that the password does not match the stored password data. 2. The system marks the validation as failed. 3. The system increments the failed login attempt counter. 6.1 The account is already temporarily locked. 1. The system does not allow authentication to continue. 2. The system returns a locked-account result. 5.2 The failed login attempt threshold is reached during validation. 1. The system updates the failed-attempt count. 2. The system locks the account temporarily according to the configured security policy. 3. The system returns a locked-account result. 4.2 A temporary technical issue occurs while checking the account data. 1. The system cannot complete the validation process. 2. The system returns an error result to the calling use case. |


| Use Case 8 – (UC-08) | Use Case 8 – (UC-08) |
|---|---|
| Use Case Name | Redirect to Onboarding |
| Use Case Type | Extended |
| Related Requirements | FR-010, FR-011, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To route an authenticated user to the onboarding flow when the account is still Unassigned or onboarding is incomplete. |
| Participating Actors | Registered User |
| Preconditions | 1. The Registered User has submitted valid login credentials. 2. The system has authenticated the user successfully. 3. The system has checked the user’s account state and onboarding status. 4. The user account is still in Unassigned state or onboarding is incomplete. |
| Postconditions | 1. The user is not redirected to the normal dashboard. 2. The user is redirected to the onboarding flow. 3. The user can continue account setup and role selection. |
| Flow of Events for Main Success Scenario | 1. The Registered User submits valid login credentials. 2. The system authenticates the user successfully. 3. The system issues the access token and refresh token. 4. The system checks the user’s account state and onboarding status. 5. The system detects that the account is still Unassigned or onboarding is incomplete. 6. The system prevents access to the normal role-based dashboard. 7. The system redirects the user to the onboarding flow. 8. The user views the onboarding page and continues the setup process. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The user is already fully onboarded. 1. The system determines that onboarding is complete. 2. The system does not redirect the user to onboarding. 3. The system sends the user to the normal dashboard. 5.1 The account state data cannot be retrieved because of a temporary technical issue. 1. The system cannot determine whether onboarding is required. 2. The system displays an error message or temporary unavailable message. 7.1 The onboarding page cannot be loaded. 1. The system cannot open the onboarding flow. 2. The system displays an error message and asks the user to try again later. |


| Use Case 9 – (UC-09) | Use Case 9 – (UC-09) |
|---|---|
| Use Case Name | Extend Token Lifetime (Remember Me) |
| Use Case Type | Extended |
| Related Requirements | FR-023, FR-022 |
| Initiating Actor | Registered User |
| Actor’s Goal | To remain signed in for a longer period by extending the refresh token lifetime when selecting Remember Me during login. |
| Participating Actors | System |
| Preconditions | 1. The Registered User is on the login page. 2. The Registered User enters valid login credentials. 3. The Registered User selects the Remember Me option before submitting the login request. 4. The system successfully authenticates the user. |
| Postconditions | 1. The user is authenticated successfully. 2. The system issues an access token and a refresh token. 3. The refresh token lifetime is extended according to the configured Remember Me policy. 4. The user remains signed in for a longer period unless the session is invalidated manually or by policy. |
| Flow of Events for Main Success Scenario | 1. The Registered User opens the login page. 2. The system displays the login form. 3. The Registered User enters email and password. 4. The Registered User selects the Remember Me option. 5. The Registered User submits the login request. 6. The system validates the submitted credentials. 7. The system authenticates the user successfully. 8. The system creates an authenticated session. 9. The system applies the Remember Me policy. 10. The system extends the refresh token lifetime according to configuration. 11. The system issues the access token and the extended-lifetime refresh token. 12. The system continues the normal post-login routing process. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The submitted credentials are invalid. 1. The system rejects the login attempt. 2. The system does not apply the Remember Me policy. 3. The system displays an invalid credentials message. 7.1 The account is locked or not eligible for authentication. 1. The system denies access. 2. The system does not extend the token lifetime. 3. The system displays the corresponding account status message. 9.1 The Remember Me policy cannot be applied because of a temporary technical issue. 1. The system completes the login process using the default refresh token lifetime. 2. The system may log the issue for monitoring. 10.1 Token generation fails because of a temporary system error. 1. The system cannot complete the authenticated session setup. 2. The system displays an error message and asks the user to try again later. |


| Use Case 10 – (UC-10) | Use Case 10 – (UC-10) |
|---|---|
| Use Case Name | Lock Account |
| Use Case Type | Extended |
| Related Requirements | FR-026, FR-027, US-013 |
| Initiating Actor | System |
| Actor’s Goal | To temporarily lock a user account after repeated failed login attempts in order to reduce unauthorized access. |
| Participating Actors | Registered User |
| Preconditions | 1. The Registered User is on the login page. 2. The Registered User has submitted login credentials. 3. The system is validating the submitted credentials. 4. The submitted credentials are invalid. 5. The failed login attempt count reaches the configured security threshold. |
| Postconditions | 1. The user account is temporarily locked according to the configured security policy. 2. The current login attempt is rejected. 3. The system informs the user that the account is temporarily locked. 4. The user cannot complete authentication until the lock period ends or the policy allows access again. |
| Flow of Events for Main Success Scenario | 1. The Registered User enters login credentials and submits the login request. 2. The system receives the submitted credentials. 3. The system validates the credentials. 4. The system detects that the submitted credentials are invalid. 5. The system increments the failed login attempt counter. 6. The system checks whether the failed-attempt threshold has been reached. 7. The system determines that the threshold has been reached. 8. The system locks the account temporarily according to the configured security policy. 9. The system rejects the login attempt. 10. The system displays a temporary lockout message to the user. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The failed-attempt threshold has not yet been reached. 1. The system does not lock the account. 2. The system rejects the login attempt. 3. The system displays an invalid credentials message. 8.1 The account is already locked from previous failed attempts. 1. The system keeps the existing lock state. 2. The system denies access immediately. 3. The system informs the user that the account is temporarily locked. 8.2 A temporary technical issue occurs while updating the lock state. 1. The system cannot complete the account lock operation normally. 2. The system rejects the current login attempt. 3. The system displays an error message and asks the user to try again later. 10.1 The lock period later expires according to policy. 1. The system removes the temporary lock state when the configured lock duration ends. 2. The user may attempt to log in again. |


| Use Case 11 – (UC-11) | Use Case 11 – (UC-11) |
|---|---|
| Use Case Name | Authenticate via Google |
| Use Case Type | Main |
| Related Requirements | FR-008, FR-010, FR-022 |
| Initiating Actor | Guest |
| Actor’s Goal | To register or log in using a Google account instead of local credentials. |
| Participating Actors | Google OAuth Provider |
| Preconditions | 1. Google SSO is enabled and configured. 2. The Guest has a valid Google account. 3. The Guest is not currently authenticated in Scholar-Path. 4. The login page is accessible. |
| Postconditions | 1. The user is authenticated through Google. 2. A local Scholar-Path account is created or matched. 3. The system issues the authenticated session tokens. 4. The user is redirected according to account state and onboarding status. |
| Flow of Events for Main Success Scenario | 1. The Guest opens the login page. 2. The system displays the available login options. 3. The Guest selects Authenticate via Google. 4. The system redirects the Guest to Google OAuth authentication. 5. Google prompts the Guest to authenticate and authorize access. 6. The Guest completes the Google authentication step. 7. Google returns the authentication response to Scholar-Path. 8. The system validates the returned Google identity data. 9. The system checks whether a linked local Scholar-Path account already exists. 10. The system matches the existing account or creates a new linked account. 11. The system creates an authenticated session. 12. The system issues the access token and refresh token. 13. The system checks the account state and onboarding status. 14. The system redirects the user to the correct next destination. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The Guest cancels the Google authentication flow. 1. Google returns a cancellation response. 2. The system returns the Guest to the login page. 8.1 The returned Google identity data is invalid or cannot be verified. 1. The system rejects the authentication response. 2. The system displays an authentication error message. 9.1 No linked local account exists for the returned Google identity. 1. The system creates a new local Scholar-Path account. 2. The system links the Google identity to the new account. 3. The system assigns the proper initial account state. 4. The system begins onboarding. 11.1 A temporary technical issue occurs during session creation. 1. The system cannot complete the authenticated session setup. 2. The system displays an error message and asks the Guest to try again later. |


| Use Case 12 – (UC-12) | Use Case 12 – (UC-12) |
|---|---|
| Use Case Name | Validate Google Identity Data |
| Use Case Type | Included |
| Related Requirements | FR-008, FR-022 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the identity data returned by Google is valid and can be trusted before the authentication process continues. |
| Participating Actors | Guest, Google OAuth Provider |
| Preconditions | 1. Google SSO is enabled and configured. 2. The Guest has selected Authenticate via Google. 3. Google has returned an authentication response to Scholar-Path. 4. The system has received the returned Google identity data. |
| Postconditions | 1. The system determines whether the returned Google identity data is valid. 2. If the identity data is valid, the authentication process continues. 3. If the identity data is invalid, the authentication request is rejected. |
| Flow of Events for Main Success Scenario | 1. The Guest selects Google authentication and completes the Google sign-in step. 2. Google returns the authentication response to Scholar-Path. 3. The system receives the returned Google identity data. 4. The system checks that the returned response is complete and properly formed. 5. The system verifies the authenticity of the returned Google identity data. 6. The system confirms that the identity data can be trusted for authentication. 7. The system extracts the required user identity information from the returned data. 8. The system marks the Google identity validation as successful. 9. The system returns control to the Authenticate via Google use case so authentication can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The returned Google response is incomplete. 1. The system cannot use the returned response for authentication. 2. The system marks the validation as failed. 3. The system returns an invalid authentication result. 5.1 The returned Google identity data cannot be verified. 1. The system rejects the returned authentication response. 2. The system marks the validation as failed. 3. The system returns an authentication error result. 6.1 The returned identity data does not satisfy the expected authentication checks. 1. The system does not continue the Google login flow. 2. The system returns an invalid authentication result. 8.1 A temporary technical issue occurs during validation. 1. The system cannot complete the identity validation process. 2. The system returns an error result to the calling use case. |


| Use Case 13 – (UC-13) | Use Case 13 – (UC-13) |
|---|---|
| Use Case Name | Cancel Google Authentication |
| Use Case Type | Extended |
| Related Requirements | FR-008, FR-010 |
| Initiating Actor | Guest |
| Actor’s Goal | To stop the Google authentication process before completion and return to the normal login entry point. |
| Participating Actors | Google OAuth Provider |
| Preconditions | 1. Google SSO is enabled and configured. 2. The Guest has selected Authenticate via Google. 3. The system has redirected the Guest to the Google authentication page. 4. The Google authentication flow is currently in progress. |
| Postconditions | 1. The Google authentication process is not completed. 2. No authenticated Scholar-Path session is created. 3. The Guest is returned to the login page. 4. The Guest may choose another login method or restart Google authentication. |
| Flow of Events for Main Success Scenario | 1. The Guest opens the login page. 2. The Guest selects Authenticate via Google. 3. The system redirects the Guest to Google OAuth authentication. 4. Google displays the authentication and authorization page. 5. The Guest decides not to continue with Google authentication. 6. The Guest cancels or closes the Google authentication flow. 7. Google returns a cancellation response to Scholar-Path. 8. The system receives the cancellation response. 9. The system stops the Google authentication process. 10. The system returns the Guest to the login page. |
| Flow of Events for Extensions (Alternate Scenarios) | 7.1 The cancellation response is not returned correctly. 1. The system cannot complete the cancellation flow normally. 2. The system displays an error message or returns the Guest to the login page if possible. 8.1 A temporary technical issue occurs while processing the cancellation response. 1. The system cannot process the Google cancellation response correctly. 2. The system displays an error message and asks the Guest to try again later. 10.1 The Guest decides to restart Google authentication after cancellation. 1. The Guest selects Authenticate via Google again from the login page. 2. The system starts a new Google authentication flow. |


| Use Case 14 – (UC-14) | Use Case 14 – (UC-14) |
|---|---|
| Use Case Name | Create New Linked Account (Google flow) |
| Use Case Type | Extended |
| Related Requirements | FR-008, FR-010, FR-011 |
| Initiating Actor | System |
| Actor’s Goal | To create a new local Scholar-Path account linked to the authenticated Google identity when no matching local account already exists. |
| Participating Actors | Guest, Google OAuth Provider |
| Preconditions | 1. Google SSO is enabled and configured. 2. The Guest has selected Authenticate via Google. 3. Google has authenticated the Guest successfully. 4. The system has validated the returned Google identity data. 5. No linked local Scholar-Path account exists for the returned Google identity. |
| Postconditions | 1. A new local Scholar-Path account is created. 2. The Google identity is linked to the new local account. 3. The new account is stored with the proper initial account state. 4. The account is marked for onboarding and the user can continue to the onboarding flow. |
| Flow of Events for Main Success Scenario | 1. The Guest completes Google authentication successfully. 2. Google returns valid identity data to Scholar-Path. 3. The system validates the returned Google identity data. 4. The system checks whether a linked local Scholar-Path account already exists. 5. The system determines that no linked account exists. 6. The system creates a new local Scholar-Path account. 7. The system links the Google identity to the new local account. 8. The system assigns the proper initial account state. 9. The system marks onboarding as incomplete. 10. The system creates the authenticated session. 11. The system issues the access token and refresh token. 12. The system redirects the user to the onboarding flow. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 A linked local account already exists. 1. The system does not create a new account. 2. The system continues authentication using the existing linked account. 6.1 The system cannot create the local account because of a temporary technical issue. 1. The system does not create the new account. 2. The system displays an error message and asks the user to try again later. 7.1 The Google identity cannot be linked to the new local account. 1. The system stops the account creation flow. 2. The system displays an authentication or linking error message. 10.1 The authenticated session cannot be created after the account is created. 1. The system stores the account creation result if applicable. 2. The system cannot complete sign-in automatically. 3. The system displays an error message and asks the user to try logging in again. |


| Use Case 15 – (UC-15) | Use Case 15 – (UC-15) |
|---|---|
| Use Case Name | Authenticate via Microsoft |
| Use Case Type | Main |
| Related Requirements | FR-009, FR-010, FR-022 |
| Initiating Actor | Guest |
| Actor’s Goal | To register or log in using a Microsoft account instead of local credentials. |
| Participating Actors | Microsoft OAuth Provider |
| Preconditions | 1. Microsoft SSO is enabled and configured. 2. The Guest has a valid Microsoft account. 3. The Guest is not currently authenticated in Scholar-Path. 4. The login page is accessible. |
| Postconditions | 1. The user is authenticated through Microsoft. 2. A local Scholar-Path account is created or matched. 3. The system issues the authenticated session tokens. 4. The user is redirected according to account state and onboarding status. |
| Flow of Events for Main Success Scenario | 1. The Guest opens the login page. 2. The system displays the available login options. 3. The Guest selects Authenticate via Microsoft. 4. The system redirects the Guest to Microsoft OAuth authentication. 5. Microsoft prompts the Guest to authenticate and authorize access. 6. The Guest completes the Microsoft authentication step. 7. Microsoft returns the authentication response to Scholar-Path. 8. The system validates the returned Microsoft identity data. 9. The system checks whether a linked local Scholar-Path account already exists. 10. The system matches the existing account or creates a new linked account. 11. The system creates an authenticated session. 12. The system issues the access token and refresh token. 13. The system checks the account state and onboarding status. 14. The system redirects the user to the correct next destination. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The Guest cancels the Microsoft authentication flow. 1. Microsoft returns a cancellation response. 2. The system returns the Guest to the login page. 8.1 The returned Microsoft identity data is invalid or cannot be verified. 1. The system rejects the authentication response. 2. The system displays an authentication error message. 9.1 No linked local account exists for the returned Microsoft identity. 1. The system creates a new local Scholar-Path account. 2. The system links the Microsoft identity to the new account. 3. The system assigns the proper initial account state. 4. The system begins onboarding. 11.1 A temporary technical issue occurs during session creation. 1. The system cannot complete the authenticated session setup. 2. The system displays an error message and asks the Guest to try again later. |


| Use Case 16 – (UC-16) | Use Case 16 – (UC-16) |
|---|---|
| Use Case Name | Validate Microsoft Identity Data |
| Use Case Type | Included |
| Related Requirements | FR-009, FR-022 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the identity data returned by Microsoft is valid and can be trusted before the authentication process continues. |
| Participating Actors | Guest, Microsoft OAuth Provider |
| Preconditions | 1. Microsoft SSO is enabled and configured. 2. The Guest has selected Authenticate via Microsoft. 3. Microsoft has returned an authentication response to Scholar-Path. 4. The system has received the returned Microsoft identity data. |
| Postconditions | 1. The system determines whether the returned Microsoft identity data is valid. 2. If the identity data is valid, the authentication process continues. 3. If the identity data is invalid, the authentication request is rejected. |
| Flow of Events for Main Success Scenario | 1. The Guest selects Microsoft authentication and completes the Microsoft sign-in step. 2. Microsoft returns the authentication response to Scholar-Path. 3. The system receives the returned Microsoft identity data. 4. The system checks that the returned response is complete and properly formed. 5. The system verifies the authenticity of the returned Microsoft identity data. 6. The system confirms that the identity data can be trusted for authentication. 7. The system extracts the required user identity information from the returned data. 8. The system marks the Microsoft identity validation as successful. 9. The system returns control to the Authenticate via Microsoft use case so authentication can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The returned Microsoft response is incomplete. 1. The system cannot use the returned response for authentication. 2. The system marks the validation as failed. 3. The system returns an invalid authentication result. 5.1 The returned Microsoft identity data cannot be verified. 1. The system rejects the returned authentication response. 2. The system marks the validation as failed. 3. The system returns an authentication error result. 6.1 The returned identity data does not satisfy the expected authentication checks. 1. The system does not continue the Microsoft login flow. 2. The system returns an invalid authentication result. 8.1 A temporary technical issue occurs during validation. 1. The system cannot complete the identity validation process. 2. The system returns an error result to the calling use case. |


| Use Case 17 – (UC-17) | Use Case 17 – (UC-17) |
|---|---|
| Use Case Name | Cancel Microsoft Authentication |
| Use Case Type | Extended |
| Related Requirements | FR-009, FR-010 |
| Initiating Actor | Guest |
| Actor’s Goal | To stop the Microsoft authentication process before completion and return to the normal login entry point. |
| Participating Actors | Microsoft OAuth Provider |
| Preconditions | 1. Microsoft SSO is enabled and configured. 2. The Guest has selected Authenticate via Microsoft. 3. The system has redirected the Guest to the Microsoft authentication page. 4. The Microsoft authentication flow is currently in progress. |
| Postconditions | 1. The Microsoft authentication process is not completed. 2. No authenticated Scholar-Path session is created. 3. The Guest is returned to the login page. 4. The Guest may choose another login method or restart Microsoft authentication. |
| Flow of Events for Main Success Scenario | 1. The Guest opens the login page. 2. The Guest selects Authenticate via Microsoft. 3. The system redirects the Guest to Microsoft OAuth authentication. 4. Microsoft displays the authentication and authorization page. 5. The Guest decides not to continue with Microsoft authentication. 6. The Guest cancels or closes the Microsoft authentication flow. 7. Microsoft returns a cancellation response to Scholar-Path. 8. The system receives the cancellation response. 9. The system stops the Microsoft authentication process. 10. The system returns the Guest to the login page. |
| Flow of Events for Extensions (Alternate Scenarios) | 7.1 The cancellation response is not returned correctly. 1. The system cannot complete the cancellation flow normally. 2. The system displays an error message or returns the Guest to the login page if possible. 8.1 A temporary technical issue occurs while processing the cancellation response. 1. The system cannot process the Microsoft cancellation response correctly. 2. The system displays an error message and asks the Guest to try again later. 10.1 The Guest decides to restart Microsoft authentication after cancellation. 1. The Guest selects Authenticate via Microsoft again from the login page. 2. The system starts a new Microsoft authentication flow. |


| Use Case 18 – (UC-18) | Use Case 18 – (UC-18) |
|---|---|
| Use Case Name | Create New Linked Account (Microsoft flow) |
| Use Case Type | Extended |
| Related Requirements | FR-009, FR-010, FR-011 |
| Initiating Actor | System |
| Actor’s Goal | To create a new local Scholar-Path account linked to the authenticated Microsoft identity when no matching local account already exists. |
| Participating Actors | Guest, Microsoft OAuth Provider |
| Preconditions | 1. Microsoft SSO is enabled and configured. 2. The Guest has selected Authenticate via Microsoft. 3. Microsoft has authenticated the Guest successfully. 4. The system has validated the returned Microsoft identity data. 5. No linked local Scholar-Path account exists for the returned Microsoft identity. |
| Postconditions | 1. A new local Scholar-Path account is created. 2. The Microsoft identity is linked to the new local account. 3. The new account is stored with the proper initial account state. 4. The account is marked for onboarding and the user can continue to the onboarding flow. |
| Flow of Events for Main Success Scenario | 1. The Guest completes Microsoft authentication successfully. 2. Microsoft returns valid identity data to Scholar-Path. 3. The system validates the returned Microsoft identity data. 4. The system checks whether a linked local Scholar-Path account already exists. 5. The system determines that no linked account exists. 6. The system creates a new local Scholar-Path account. 7. The system links the Microsoft identity to the new local account. 8. The system assigns the proper initial account state. 9. The system marks onboarding as incomplete. 10. The system creates the authenticated session. 11. The system issues the access token and refresh token. 12. The system redirects the user to the onboarding flow. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 A linked local account already exists. 1. The system does not create a new account. 2. The system continues authentication using the existing linked account. 6.1 The system cannot create the local account because of a temporary technical issue. 1. The system does not create the new account. 2. The system displays an error message and asks the user to try again later. 7.1 The Microsoft identity cannot be linked to the new local account. 1. The system stops the account creation flow. 2. The system displays an authentication or linking error message. 10.1 The authenticated session cannot be created after the account is created. 1. The system stores the account creation result if applicable. 2. The system cannot complete sign-in automatically. 3. The system displays an error message and asks the user to try logging in again. |


| Use Case 19 – (UC-19) | Use Case 19 – (UC-19) |
|---|---|
| Use Case Name | Request Password Reset |
| Use Case Type | Main |
| Related Requirements | FR-024, FR-025 |
| Initiating Actor | Registered User |
| Actor’s Goal | To recover account access by receiving a time-limited password reset link. |
| Participating Actors | Email / Notification Service |
| Preconditions | 1. The user already has a registered account. 2. The password reset page is accessible without login. 3. The email delivery service is available. |
| Postconditions | 1. A time-limited password reset link is generated and sent to the user’s email address. 2. After a successful password reset, all active refresh tokens for the user are invalidated. 3. The user can continue the password recovery flow securely. |
| Flow of Events for Main Success Scenario | 1. The Registered User opens the password reset page. 2. The system displays the password reset request form. 3. The Registered User enters the account email address. 4. The Registered User submits the password reset request. 5. The system validates the email address format. 6. The system locates the related user account. 7. The system generates a time-limited password reset token or reset link. 8. The system sends the reset link through the email service. 9. The system displays a confirmation message to the user. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 No account exists for the submitted email address. 1. The system does not reveal whether the account exists. 2. The system displays a neutral confirmation message. 8.1 The email service fails to send the reset message. 1. The system logs the delivery failure. 2. The system displays an error message or retry message. 7.1 A temporary technical issue occurs while generating the reset token. 1. The system cannot complete the password reset request normally. 2. The system displays an error message and asks the user to try again later. 9.1 The user later attempts to use an expired reset link. 1. The system rejects the expired token. 2. The system asks the user to request a new password reset link. |


| Use Case 20 – (UC-20) | Use Case 20 – (UC-20) |
|---|---|
| Use Case Name | Generate Time-Limited Reset Token |
| Use Case Type | Included |
| Related Requirements | FR-024 |
| Initiating Actor | System |
| Actor’s Goal | To generate a secure, time-limited reset token that can be used to complete the password reset process. |
| Participating Actors | Registered User |
| Preconditions | 1. The Registered User has submitted a password reset request. 2. The system has received the submitted email address. 3. The password reset request is being processed by the system. |
| Postconditions | 1. A secure reset token is generated for the password reset request. 2. The token is associated with the correct account if applicable. 3. The token is configured with a limited validity period. 4. The password reset flow can continue to the delivery step. |
| Flow of Events for Main Success Scenario | 1. The Registered User submits a password reset request. 2. The system receives the submitted email address. 3. The system processes the password reset request. 4. The system locates the related account if applicable. 5. The system generates a secure reset token. 6. The system assigns an expiration time to the reset token. 7. The system associates the token with the password reset request. 8. The system marks the token as ready for delivery through the password reset flow. 9. The system returns control to the Request Password Reset use case so the reset message can be sent. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 No account exists for the submitted email address. 1. The system does not reveal whether the account exists. 2. The system may skip real token generation for security reasons. 3. The system continues the flow with a neutral response. 5.1 The system cannot generate the reset token because of a temporary technical issue. 1. The system cannot complete the token generation process. 2. The system returns an error result to the calling use case. 6.1 The token expiration policy cannot be applied correctly. 1. The system does not issue the token for use. 2. The system treats the generation attempt as failed. 7.1 The generated token cannot be linked to the password reset request. 1. The system stops the reset-token preparation step. 2. The system returns an error result to the calling use case. |


| Use Case 21 – (UC-21) | Use Case 21 – (UC-21) |
|---|---|
| Use Case Name | Invalidate Active Refresh Tokens |
| Use Case Type | Included |
| Related Requirements | FR-025 |
| Initiating Actor | System |
| Actor’s Goal | To invalidate all active refresh tokens associated with the user after a successful password reset so that old authenticated sessions can no longer be reused. |
| Participating Actors | Registered User |
| Preconditions | 1. The Registered User has completed the password reset process successfully. 2. The system has identified the account whose password was reset. 3. The account may have one or more active refresh tokens. |
| Postconditions | 1. All active refresh tokens linked to the account are invalidated. 2. Previously issued refresh tokens can no longer be used to obtain new access tokens. 3. The account remains protected after the password reset. 4. The password reset process is completed securely. |
| Flow of Events for Main Success Scenario | 1. The Registered User completes the password reset successfully. 2. The system confirms that the new password has been stored. 3. The system identifies the account related to the password reset. 4. The system retrieves all active refresh tokens associated with the account. 5. The system invalidates the active refresh tokens. 6. The system marks the previously active refresh tokens as unusable. 7. The system completes the token invalidation process. 8. The system returns control to the password reset flow so the process can finish securely. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 No active refresh tokens exist for the account. 1. The system finds no active refresh tokens to invalidate. 2. The system completes the step without error. 5.1 A temporary technical issue occurs while retrieving the active refresh tokens. 1. The system cannot complete the token lookup normally. 2. The system returns an error result to the calling use case or logs the failure according to policy. 5.2 A temporary technical issue occurs while invalidating the active refresh tokens. 1. The system cannot complete the invalidation operation normally. 2. The system treats the password reset security finalization step as failed or incomplete according to policy. 6.1 Some refresh tokens were already expired or invalid before this step. 1. The system ignores already unusable tokens. 2. The system invalidates the remaining active refresh tokens and completes the process. |


| Use Case 22 – (UC-22) | Use Case 22 – (UC-22) |
|---|---|
| Use Case Name | Email Delivery Fails |
| Use Case Type | Extended |
| Related Requirements | FR-024 |
| Initiating Actor | Email / Notification Service |
| Actor’s Goal | To handle the situation where the password reset email cannot be delivered successfully. |
| Participating Actors | Registered User, System |
| Preconditions | 1. The Registered User has submitted a password reset request. 2. The system has generated the password reset link or token. 3. The system has attempted to send the reset message through the Email / Notification Service. 4. The email delivery attempt fails. |
| Postconditions | 1. The reset message is not delivered successfully. 2. The system logs or recognizes the delivery failure. 3. The user is informed that the reset message could not be delivered or is asked to retry later. 4. The password reset request is not completed through email delivery in the current attempt. |
| Flow of Events for Main Success Scenario | 1. The Registered User submits a password reset request. 2. The system processes the request and prepares the reset message. 3. The system sends the reset message through the Email / Notification Service. 4. The Email / Notification Service cannot deliver the message successfully. 5. The system receives or detects the delivery failure result. 6. The system logs the email delivery failure. 7. The system does not confirm successful delivery of the reset message. 8. The system displays an error message or retry message to the Registered User. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The email service is temporarily unavailable. 1. The delivery request cannot be completed. 2. The system records the failure and informs the user to try again later. 5.1 The system does not receive a clear delivery result from the email service. 1. The system treats the delivery attempt as failed or uncertain according to policy. 2. The system logs the issue for monitoring. 6.1 Logging the failure also encounters a temporary technical issue. 1. The system may still display a delivery error message to the user. 2. The system handles the logging failure according to system error policy. 8.1 The Registered User retries the password reset request later. 1. The Registered User submits a new password reset request. 2. The system starts a new password reset flow. |


| Use Case 23 – (UC-23) | Use Case 23 – (UC-23) |
|---|---|
| Use Case Name | Token Expires Before Use |
| Use Case Type | Extended |
| Related Requirements | FR-024 |
| Initiating Actor | System |
| Actor’s Goal | To prevent the use of an expired password reset token and require the user to request a new reset link. |
| Participating Actors | Registered User |
| Preconditions | 1. The Registered User has requested a password reset. 2. The system has generated and delivered a time-limited reset token or reset link. 3. The token has a defined expiration period. 4. The Registered User attempts to use the reset token after it has expired. |
| Postconditions | 1. The expired reset token is rejected. 2. The password reset process cannot continue with the expired token. 3. The Registered User is informed that the token has expired. 4. The Registered User must request a new password reset link to continue. |
| Flow of Events for Main Success Scenario | 1. The Registered User receives the password reset link or token. 2. The Registered User delays using the reset link until after its validity period ends. 3. The Registered User opens the expired reset link or submits the expired reset token. 4. The system receives the reset request containing the token. 5. The system checks the token validity and expiration time. 6. The system detects that the token has expired. 7. The system rejects the expired token. 8. The system does not allow the password reset process to continue. 9. The system displays a message informing the Registered User that the reset token has expired. 10. The system asks the Registered User to request a new password reset link. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The token is still valid. 1. The system accepts the token as valid. 2. The password reset process continues normally. 6.1 A temporary technical issue occurs while checking token validity. 1. The system cannot determine whether the token is still valid. 2. The system displays an error message and asks the user to try again later. 7.1 The token is malformed or does not match the expected reset request. 1. The system rejects the token. 2. The system displays an invalid or expired token message. 10.1 The Registered User requests a new password reset link. 1. The Registered User starts a new password reset request. 2. The system begins a new password reset flow. |

 Use Case 24 – (UC-24) Complete Onboarding


| Field | Description |
|---|---|
| Use Case 24 – (UC-24) | Complete Onboarding |
| Related Requirements | FR-010, FR-011, FR-012, FR-027 |
| Initiating Actor | Unassigned User |
| Actor’s Goal | To complete the first-time account setup and activate the appropriate role-based path in Scholar-Path. |
| Participating Actors | None |
| Preconditions | 1. The user has already registered or authenticated successfully.2. The user is authenticated.3. The account is in Unassigned state or onboarding is incomplete.4. The onboarding flow is accessible. |
| Postconditions | 1. The onboarding submission is processed.2. The system updates the onboarding status according to the selected path.3. If the selected path is Student, the account is activated directly.4. If the selected path is Company or Consultant, the request is marked as pending Admin review before activation.5. If the user is already onboarded, the system redirects the user to the appropriate dashboard instead of reopening onboarding. |
| Flow of Events for Main Success Scenario | 1. The Unassigned User enters the onboarding flow.2. The system displays the available onboarding role options.3. The user reviews the available paths.4. The user selects the intended role path.5. The system displays the required onboarding fields for the selected path.6. The user enters the required onboarding information.7. The user submits the onboarding data.8. The system validates the submitted onboarding data.9. The system determines the selected onboarding path.10. The system updates the onboarding status and account state accordingly.11. The system routes the user to the correct next outcome based on the selected path. |
| Flow of Events for Extensions (Alternate Scenarios) | 1.1 The user is already fully onboarded. 1. The system detects that onboarding is already complete. 2. The system does not reopen the onboarding flow. 3. The system redirects the user to the appropriate dashboard. 8.1 The submitted onboarding data is missing or invalid. 1. The system rejects the submission. 2. The system highlights the invalid or missing fields. 3. The system displays the related validation error messages. 4. The user corrects the data and resubmits it. 9.1 The selected path requires Admin approval. 1. The system determines that the selected path is Company or Consultant. 2. The system marks the onboarding request as Pending Admin Review. 3. The system informs the user that activation depends on Admin approval. 10.1 A temporary technical issue occurs while saving onboarding data. 1. The system cannot complete the onboarding update. 2. The system displays an error message and asks the user to try again later. |

Use Case 25 – (UC-25) Validate Onboarding Data


| Field | Description |
|---|---|
| Use Case 25 – (UC-25) | Validate Onboarding Data |
| Related Requirements | FR-010, FR-011, FR-012, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the submitted onboarding data is complete, valid, and suitable for the selected onboarding path before the onboarding process continues. |
| Participating Actors | Unassigned User |
| Preconditions | 1. The Unassigned User is authenticated.2. The Unassigned User is in the onboarding flow.3. The user has selected or is completing an onboarding path.4. The user has entered onboarding data and submitted it for processing. |
| Postconditions | 1. The system determines whether the submitted onboarding data is valid.2. If the data is valid, the onboarding flow continues.3. If the data is invalid or incomplete, the system rejects the submission and returns validation errors.4. The onboarding process does not proceed until the required data is corrected. |
| Flow of Events for Main Success Scenario | 1. The Unassigned User enters the required onboarding information.2. The Unassigned User submits the onboarding form.3. The system receives the submitted onboarding data.4. The system identifies the selected onboarding path.5. The system checks whether all required fields for that path are present.6. The system validates the entered values according to the selected role path rules.7. The system confirms that the submitted onboarding data is complete and valid.8. The system marks the validation step as successful.9. The system returns control to the Complete Onboarding use case so the onboarding process can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 Required onboarding fields are missing. 1. The system detects that one or more required fields are not provided. 2. The system marks the validation as failed. 3. The system returns the missing-field result to the onboarding flow. 6.1 One or more entered values are invalid. 1. The system detects that one or more values do not satisfy the required rules. 2. The system marks the validation as failed. 3. The system returns the validation error result to the onboarding flow. 4.1 The selected onboarding path requires different validation rules. 1. The system applies the validation rules that match the selected role path. 2. The system validates the data according to the Student, Company, or Consultant onboarding requirements. 8.1 A temporary technical issue occurs during validation. 1. The system cannot complete the onboarding validation process. 2. The system returns an error result to the calling use case. 3. The system asks the user to try again later. |

## Use Case 26 – (UC-26) Show Validation Error (Complete Onboarding)


| Field | Description |
|---|---|
| Use Case 26 – (UC-26) | Show Validation Error (Complete Onboarding) |
| Related Requirements | FR-010, FR-011, FR-012, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To inform the Unassigned User that the submitted onboarding data is invalid, incomplete, or inconsistent, and to explain what must be corrected before onboarding can continue. |
| Participating Actors | Unassigned User |
| Preconditions | 1. The Unassigned User is authenticated. 2. The Unassigned User is in the onboarding flow. 3. The user has submitted onboarding data. 4. The system has validated the submitted onboarding data. 5. The system has detected one or more validation failures. |
| Postconditions | 1. The onboarding submission is rejected for the current attempt. 2. The invalid or missing fields are highlighted. 3. The system displays the corresponding validation error messages. 4. The user is able to correct the data and resubmit the onboarding form. 5. The onboarding process does not continue until the validation issues are resolved. |
| Flow of Events for Main Success Scenario | 1. The Unassigned User enters onboarding information and submits the form. 2. The system receives the submitted onboarding data. 3. The system validates the data according to the selected onboarding path. 4. The system detects one or more missing, invalid, or inconsistent values. 5. The system rejects the current onboarding submission. 6. The system highlights the affected fields. 7. The system displays the corresponding validation error messages. 8. The Unassigned User reviews the displayed errors. 9. The Unassigned User corrects the invalid or missing data. 10. The user resubmits the onboarding form. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 Required onboarding fields are missing. 1. The system detects that one or more required fields are not provided. 2. The system highlights the missing fields. 3. The system displays messages asking the user to complete the required information.  4.2 One or more entered values are invalid. 1. The system detects that one or more values do not satisfy the required rules. 2. The system highlights the invalid fields. 3. The system displays the related validation messages.  4.3 The selected onboarding path has role-specific validation failures. 1. The system applies the validation rules for the selected Student, Company, or Consultant path. 2. The system detects that the submitted data does not satisfy that path’s requirements. 3. The system displays the corresponding role-specific validation errors.  7.1 A temporary technical issue occurs while displaying validation details. 1. The system cannot display the full validation details normally. 2. The system displays a general error message. 3. The user is asked to try again later. |

## Use Case 27 – (UC-27) Mark Pending Admin Review


| Field | Description |
|---|---|
| Use Case 27 – (UC-27) | Mark Pending Admin Review |
| Related Requirements | FR-010, FR-011, FR-012, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To place the onboarding request in a pending state when the selected onboarding path requires Admin approval before activation. |
| Participating Actors | Unassigned User |
| Preconditions | 1. The Unassigned User is authenticated. 2. The Unassigned User is in the onboarding flow. 3. The user has submitted onboarding data successfully. 4. The system has validated the submitted onboarding data. 5. The selected onboarding path is one that requires Admin approval. 6. The selected path is Company or Consultant. |
| Postconditions | 1. The onboarding request is recorded as pending Admin review. 2. The selected role is not activated immediately. 3. The user account remains in a non-activated approval state for that role path. 4. The user is informed that activation depends on Admin approval. 5. The request becomes available for Admin review and decision. |
| Flow of Events for Main Success Scenario | 1. The Unassigned User enters the onboarding information and submits the form. 2. The system receives the submitted onboarding data. 3. The system validates the submitted data successfully. 4. The system determines the selected onboarding path. 5. The system detects that the selected path requires Admin approval. 6. The system creates or updates the onboarding request record. 7. The system marks the onboarding request as Pending Admin Review. 8. The system does not activate the selected Company or Consultant permissions immediately. 9. The system informs the user that the request has been submitted for Admin review. 10. The system routes the user to the appropriate waiting or post-submission page. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The selected path does not require Admin approval. 1. The system determines that the selected path is Student. 2. The system does not mark the request as pending Admin review. 3. The system continues with the direct activation flow.  6.1 The request record cannot be created or updated. 1. The system cannot save the pending review request correctly. 2. The system does not complete the pending-review step. 3. The system displays an error message and asks the user to try again later.  7.1 A temporary technical issue occurs while assigning the pending status. 1. The system cannot mark the onboarding request as pending review. 2. The system treats the onboarding submission as incomplete. 3. The system displays an error message.  9.1 The user closes the process after submission. 1. The system preserves the pending review state if it was saved successfully. 2. The user can return later and view the request status. |

## Use Case 28 – (UC-28) Redirect to Dashboard (Already Onboarded)


| Field | Description |
|---|---|
| Use Case 28 – (UC-28) | Redirect to Dashboard (Already Onboarded) |
| Related Requirements | FR-010, FR-011, FR-012, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To prevent a fully onboarded user from re-entering the onboarding flow and route the user directly to the appropriate dashboard. |
| Participating Actors | Unassigned User |
| Preconditions | 1. The user is authenticated. 2. The system checks the user’s onboarding status. 3. The user attempts to access the onboarding flow. 4. The user account is already fully onboarded. |
| Postconditions | 1. The onboarding flow is not reopened. 2. The user is redirected to the appropriate dashboard or landing page. 3. The user continues normal system access according to the active account state and role. |
| Flow of Events for Main Success Scenario | 1. The user attempts to enter the onboarding flow. 2. The system receives the onboarding access request. 3. The system checks the user’s onboarding status and account state. 4. The system determines that onboarding is already complete. 5. The system blocks re-entry to the onboarding process. 6. The system identifies the correct destination based on the user’s account role and state. 7. The system redirects the user to the appropriate dashboard or landing page. 8. The user accesses the normal post-onboarding experience. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The user is not fully onboarded. 1. The system determines that onboarding is still incomplete. 2. The system does not redirect the user to the dashboard. 3. The system allows the onboarding flow to continue.  6.1 The correct dashboard destination cannot be determined. 1. The system cannot resolve the appropriate post-onboarding destination. 2. The system displays an error message or routes the user to a safe default page.  7.1 The dashboard page cannot be loaded. 1. The system cannot open the destination page successfully. 2. The system displays an error or temporary unavailable message.  3.1 A temporary technical issue occurs while checking onboarding status. 1. The system cannot confirm whether the user is already onboarded. 2. The system displays an error message and asks the user to try again later. |

## Use Case 29 – (UC-29) Select Student Role


| Field | Description |
|---|---|
| Use Case 29 – (UC-29) | Select Student Role |
| Related Requirements | FR-012, FR-013, FR-027 |
| Initiating Actor | Unassigned User |
| Actor’s Goal | To activate Student access without requiring Admin approval. |
| Participating Actors | None |
| Preconditions | 1. The user is authenticated. 2. The user is in the onboarding flow. 3. The account is currently in Unassigned state. 4. The Student role is available for selection. |
| Postconditions | 1. The account is activated with Student permissions. 2. Onboarding is marked as complete. 3. The user is redirected to the Student experience. 4. The Student profile path becomes active without Admin review. |
| Flow of Events for Main Success Scenario | 1. The Unassigned User enters the onboarding flow. 2. The system displays the available role options. 3. The user selects Student. 4. The system displays the required Student onboarding fields. 5. The user enters the required Student information. 6. The user submits the Student onboarding form. 7. The system validates the submitted Student data. 8. The system activates Student permissions. 9. The system updates the account role to Student. 10. The system marks onboarding as complete. 11. The system redirects the user to the Student dashboard or Student landing area. |
| Flow of Events for Extensions (Alternate Scenarios) | 7.1 The submitted Student data is missing or invalid. 1. The system does not continue Student activation. 2. The system displays the related validation errors. 3. The user corrects the data and resubmits the form.  8.1 Student activation fails because of a temporary technical issue. 1. The system does not finalize Student activation. 2. The system displays an error message and asks the user to try again later.  11.1 The Student dashboard cannot be loaded. 1. The system cannot open the Student destination page. 2. The system displays an error or temporary unavailable message. |

## Use Case 30 – (UC-30) Validate Student Data


| Field | Description |
|---|---|
| Use Case 30 – (UC-30) | Validate Student Data |
| Related Requirements | FR-012, FR-013, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the submitted Student onboarding data is complete and valid before Student activation can continue. |
| Participating Actors | Unassigned User |
| Preconditions | 1. The Unassigned User is authenticated. 2. The user is in the onboarding flow. 3. The user has selected the Student role. 4. The user has entered Student onboarding data and submitted it for processing. |
| Postconditions | 1. The system determines whether the submitted Student data is valid. 2. If the data is valid, the Student role activation flow continues. 3. If the data is invalid or incomplete, the system rejects the submission and returns validation errors. 4. Student activation does not proceed until the data is corrected. |
| Flow of Events for Main Success Scenario | 1. The Unassigned User selects the Student role. 2. The system displays the Student onboarding fields. 3. The user enters the required Student information. 4. The user submits the Student onboarding form. 5. The system receives the submitted Student data. 6. The system checks whether all required Student fields are present. 7. The system validates the entered values according to Student onboarding rules. 8. The system confirms that the submitted Student data is complete and valid. 9. The system marks the validation step as successful. 10. The system returns control to the Select Student Role use case so Student activation can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 Required Student fields are missing. 1. The system detects that one or more required fields are not provided. 2. The system marks the validation as failed. 3. The system returns the missing-field result to the Student onboarding flow.  7.1 One or more entered values are invalid. 1. The system detects that one or more values do not satisfy Student onboarding rules. 2. The system marks the validation as failed. 3. The system returns the validation error result to the Student onboarding flow.  8.1 A temporary technical issue occurs during Student data validation. 1. The system cannot complete the Student data validation process. 2. The system returns an error result to the calling use case. 3. The system asks the user to try again later. |

## Use Case 31 – (UC-31) Show Validation Error (Student Role)


| Field | Description |
|---|---|
| Use Case 31 – (UC-31) | Show Validation Error (Student Role) |
| Related Requirements | FR-012, FR-013, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To inform the Unassigned User that the submitted Student onboarding data is missing, invalid, or inconsistent, and to explain what must be corrected before Student activation can continue. |
| Participating Actors | Unassigned User |
| Preconditions | 1. The Unassigned User is authenticated. 2. The user is in the onboarding flow. 3. The user has selected the Student role. 4. The user has submitted Student onboarding data. 5. The system has validated the submitted Student data. 6. The system has detected one or more validation failures. |
| Postconditions | 1. The current Student onboarding submission is rejected. 2. The invalid or missing Student fields are highlighted. 3. The system displays the corresponding validation error messages. 4. The user is able to correct the data and resubmit the Student onboarding form. 5. Student activation does not continue until the validation issues are resolved. |
| Flow of Events for Main Success Scenario | 1. The Unassigned User selects the Student role. 2. The system displays the Student onboarding fields. 3. The user enters the required Student information. 4. The user submits the Student onboarding form. 5. The system receives the submitted Student data. 6. The system validates the Student data according to the Student onboarding rules. 7. The system detects one or more missing, invalid, or inconsistent values. 8. The system rejects the current submission. 9. The system highlights the affected Student fields. 10. The system displays the corresponding validation error messages. 11. The Unassigned User reviews the displayed errors. 12. The user corrects the invalid or missing data. 13. The user resubmits the Student onboarding form. |
| Flow of Events for Extensions (Alternate Scenarios) | 7.1 Required Student fields are missing. 1. The system detects that one or more required Student fields are not provided. 2. The system highlights the missing fields. 3. The system displays messages asking the user to complete the required information.  7.2 One or more entered values are invalid. 1. The system detects that one or more values do not satisfy Student onboarding rules. 2. The system highlights the invalid fields. 3. The system displays the related validation messages.  7.3 The submitted Student data is inconsistent. 1. The system detects that one or more entered values conflict with the expected Student data rules. 2. The system highlights the affected fields. 3. The system displays the corresponding validation error messages.  10.1 A temporary technical issue occurs while displaying validation details. 1. The system cannot display the full validation details normally. 2. The system displays a general error message. 3. The user is asked to try again later. |

## Use Case 32 – (UC-32) Activate Student Permissions


| Field | Description |
|---|---|
| Use Case 32 – (UC-32) | Activate Student Permissions |
| Related Requirements | FR-012, FR-013, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To activate Student permissions immediately after successful Student onboarding without requiring Admin approval. |
| Participating Actors | Unassigned User |
| Preconditions | 1. The Unassigned User is authenticated. 2. The user has selected the Student role. 3. The submitted Student onboarding data has been validated successfully. 4. The account is eligible for direct Student activation. |
| Postconditions | 1. Student permissions are activated for the account. 2. The account role is updated to Student. 3. Onboarding is marked as complete. 4. The user becomes eligible for Student dashboard access. |
| Flow of Events for Main Success Scenario | 1. The system receives the successful Student onboarding result. 2. The system confirms that the Student path does not require Admin approval. 3. The system activates Student permissions for the account. 4. The system updates the account role to Student. 5. The system marks onboarding as complete. 6. The system stores the updated account state. 7. The system returns control to the Select Student Role use case so the user can be redirected to the Student dashboard. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The account is not eligible for direct activation. 1. The system detects that the activation conditions are not satisfied. 2. The system does not activate Student permissions. 3. The system returns an error result to the calling use case.  3.1 A temporary technical issue occurs while activating Student permissions. 1. The system cannot complete the Student activation step. 2. The system returns an error result to the calling use case.  5.1 The system cannot mark onboarding as complete. 1. The system cannot finalize the onboarding state update. 2. The system treats the activation flow as incomplete or failed according to policy. 3. The system displays an error message or returns an error result. |

## Use Case 33 – (UC-33) Activation Fails (System Error)


| Field | Description |
|---|---|
| Use Case 33 – (UC-33) | Activation Fails (System Error) |
| Related Requirements | FR-012, FR-013, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To handle the situation where Student activation cannot be completed because of a system-side technical issue. |
| Participating Actors | Unassigned User |
| Preconditions | 1. The Unassigned User is authenticated. 2. The user has selected the Student role. 3. The submitted Student data has been validated successfully. 4. The system has started the Student activation process. 5. A technical issue occurs during activation. |
| Postconditions | 1. Student permissions are not fully activated. 2. The onboarding process is not completed successfully in the current attempt. 3. The system informs the user that activation could not be completed. 4. The user is asked to try again later or repeat the process according to system policy. |
| Flow of Events for Main Success Scenario | 1. The Unassigned User submits valid Student onboarding data. 2. The system validates the Student data successfully. 3. The system starts the Student activation process. 4. A technical issue occurs while activating Student permissions or saving the updated account state. 5. The system cannot complete the activation process successfully. 6. The system prevents incomplete or inconsistent activation from being treated as successful. 7. The system displays an activation failure or temporary error message. 8. The system asks the user to try again later or restart the process. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The technical issue occurs while updating the account role. 1. The system cannot save the role change correctly. 2. The system stops the activation flow. 3. The system displays an error message.  4.2 The technical issue occurs while marking onboarding as complete. 1. The system cannot finalize the onboarding state. 2. The system does not treat the activation as fully successful. 3. The system displays an error message.  7.1 The error details cannot be shown fully to the user. 1. The system displays a general failure or temporary unavailable message. 2. The user is still informed that activation was not completed. |

## Use Case 34 – (UC-34) Submit Company Onboarding Request


| Field | Description |
|---|---|
| Use Case 34 – (UC-34) | Submit Company Onboarding Request |
| Related Requirements | FR-012, FR-014, FR-027 |
| Initiating Actor | Unassigned User |
| Actor’s Goal | To submit Company onboarding information for Admin review so that Company access can be approved and activated later. |
| Participating Actors | Admin |
| Preconditions | 1. The user is authenticated. 2. The user is in the onboarding flow. 3. The account is currently in Unassigned state. 4. The Company role is available for selection. |
| Postconditions | 1. A Company onboarding request is created. 2. The request is recorded in a pending approval state. 3. Company permissions are not activated immediately. 4. The Admin is notified of the pending request. 5. The user is informed that Company activation depends on Admin approval. |
| Flow of Events for Main Success Scenario | 1. The Unassigned User enters the onboarding flow. 2. The system displays the available role options. 3. The user selects Company. 4. The system displays the required Company onboarding fields. 5. The user enters the required organization information. 6. The user submits the Company onboarding form. 7. The system validates the submitted organization data. 8. The system creates the Company onboarding request. 9. The system marks the request as pending Admin review. 10. The system notifies the Admin of the pending request. 11. The system informs the user that Company access will activate only after Admin approval. |
| Flow of Events for Extensions (Alternate Scenarios) | 7.1 The submitted organization data is missing or invalid. 1. The system does not create the onboarding request. 2. The system displays the related validation errors. 3. The user corrects the data and resubmits the form.  10.1 Admin notification delivery fails. 1. The system preserves the pending request if it was created successfully. 2. The system logs or handles the notification failure according to policy. 3. The user is still informed that the request is pending review.  8.1 A temporary technical issue occurs while creating the request. 1. The system cannot create the Company onboarding request. 2. The system displays an error message and asks the user to try again later. |

## Use Case 35 – (UC-35) Validate Organization Data


| Field | Description |
|---|---|
| Use Case 35 – (UC-35) | Validate Organization Data |
| Related Requirements | FR-012, FR-014, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the submitted Company organization data is complete and valid before the onboarding request can be accepted for Admin review. |
| Participating Actors | Unassigned User |
| Preconditions | 1. The Unassigned User is authenticated. 2. The user is in the onboarding flow. 3. The user has selected the Company role. 4. The user has entered Company onboarding data and submitted it for processing. |
| Postconditions | 1. The system determines whether the submitted organization data is valid. 2. If the data is valid, the Company onboarding request flow continues. 3. If the data is invalid or incomplete, the system rejects the submission and returns validation errors. 4. Request creation does not proceed until the data is corrected. |
| Flow of Events for Main Success Scenario | 1. The Unassigned User selects the Company role. 2. The system displays the Company onboarding fields. 3. The user enters the required organization information. 4. The user submits the Company onboarding form. 5. The system receives the submitted organization data. 6. The system checks whether all required Company fields are present. 7. The system validates the entered values according to Company onboarding rules. 8. The system confirms that the submitted organization data is complete and valid. 9. The system marks the validation step as successful. 10. The system returns control to the Submit Company Onboarding Request use case so the request can be created. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 Required organization fields are missing. 1. The system detects that one or more required fields are not provided. 2. The system marks the validation as failed. 3. The system returns the missing-field result to the Company onboarding flow.  7.1 One or more entered values are invalid. 1. The system detects that one or more values do not satisfy Company onboarding rules. 2. The system marks the validation as failed. 3. The system returns the validation error result to the Company onboarding flow.  8.1 A temporary technical issue occurs during organization data validation. 1. The system cannot complete the validation process. 2. The system returns an error result to the calling use case. 3. The system asks the user to try again later. |

## Use Case 36 – (UC-36) Show Validation Error (Company Onboarding)


| Field | Description |
|---|---|
| Use Case 36 – (UC-36) | Show Validation Error (Company Onboarding) |
| Related Requirements | FR-012, FR-014, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To inform the Unassigned User that the submitted Company onboarding data is missing, invalid, or inconsistent, and to explain what must be corrected before the request can continue. |
| Participating Actors | Unassigned User |
| Preconditions | 1. The Unassigned User is authenticated. 2. The user is in the onboarding flow. 3. The user has selected the Company role. 4. The user has submitted Company onboarding data. 5. The system has validated the submitted organization data. 6. The system has detected one or more validation failures. |
| Postconditions | 1. The current Company onboarding submission is rejected. 2. The invalid or missing organization fields are highlighted. 3. The system displays the corresponding validation error messages. 4. The user is able to correct the data and resubmit the Company onboarding form. 5. Request creation does not continue until the validation issues are resolved. |
| Flow of Events for Main Success Scenario | 1. The Unassigned User selects the Company role. 2. The system displays the Company onboarding fields. 3. The user enters the required organization information. 4. The user submits the Company onboarding form. 5. The system receives the submitted organization data. 6. The system validates the data according to Company onboarding rules. 7. The system detects one or more missing, invalid, or inconsistent values. 8. The system rejects the current submission. 9. The system highlights the affected organization fields. 10. The system displays the corresponding validation error messages. 11. The user reviews the displayed errors. 12. The user corrects the invalid or missing data. 13. The user resubmits the Company onboarding form. |
| Flow of Events for Extensions (Alternate Scenarios) | 7.1 Required organization fields are missing. 1. The system detects that one or more required organization fields are not provided. 2. The system highlights the missing fields. 3. The system displays messages asking the user to complete the required information.  7.2 One or more entered values are invalid. 1. The system detects that one or more values do not satisfy Company onboarding rules. 2. The system highlights the invalid fields. 3. The system displays the related validation messages.  7.3 The submitted organization data is inconsistent. 1. The system detects that one or more values conflict with the expected Company data rules. 2. The system highlights the affected fields. 3. The system displays the corresponding validation error messages.  10.1 A temporary technical issue occurs while displaying validation details. 1. The system cannot display the full validation details normally. 2. The system displays a general error message. 3. The user is asked to try again later. |

## Use Case 37 – (UC-37) Notify Admin of Pending Request


| Field | Description |
|---|---|
| Use Case 37 – (UC-37) | Notify Admin of Pending Request |
| Related Requirements | FR-012, FR-014, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To notify the Admin that a new Company onboarding request has been submitted and is awaiting review. |
| Participating Actors | Admin, Unassigned User |
| Preconditions | 1. The Unassigned User is authenticated. 2. The user has selected the Company role. 3. The submitted organization data has been validated successfully. 4. The Company onboarding request has been created and marked as pending review. 5. An Admin notification mechanism is available. |
| Postconditions | 1. The Admin is notified of the pending Company onboarding request. 2. The request becomes visible or actionable in the Admin review flow. 3. The pending request remains stored for later approval or rejection. |
| Flow of Events for Main Success Scenario | 1. The system creates the Company onboarding request successfully. 2. The system marks the request as pending Admin review. 3. The system prepares the pending-request notification details. 4. The system sends the notification to the Admin through the configured internal or external notification channel. 5. The Admin receives the pending request notification. 6. The system completes the notification step. 7. The Company onboarding flow continues with the request remaining in pending status. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The notification channel is temporarily unavailable. 1. The system cannot deliver the Admin notification successfully. 2. The system handles the issue according to notification failure policy. 3. The pending request remains stored for Admin review.  5.1 The Admin does not view the notification immediately. 1. The system still preserves the pending request in the review queue. 2. The request remains available for later Admin action.  3.1 A temporary technical issue occurs while preparing notification details. 1. The system cannot complete the notification preparation step. 2. The system returns an error or failure result to the calling use case. |

## Use Case 38 – (UC-38) Notification Delivery Fails


| Field | Description |
|---|---|
| Use Case 38 – (UC-38) | Notification Delivery Fails |
| Related Requirements | FR-012, FR-014, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To handle the situation where the system cannot successfully notify the Admin after a Company onboarding request has been submitted. |
| Participating Actors | Admin, Unassigned User |
| Preconditions | 1. The Unassigned User has submitted a Company onboarding request successfully. 2. The system has created the pending request. 3. The system attempts to notify the Admin of the pending request. 4. The notification delivery attempt fails. |
| Postconditions | 1. The pending Company onboarding request remains stored in the system. 2. Company permissions are still not activated. 3. The notification failure is recognized, logged, or handled according to system policy. 4. The request remains available for later Admin review. |
| Flow of Events for Main Success Scenario | 1. The system creates the Company onboarding request successfully. 2. The system marks the request as pending Admin review. 3. The system attempts to notify the Admin. 4. The notification delivery fails. 5. The system detects or receives the notification failure result. 6. The system does not cancel the pending onboarding request. 7. The system preserves the request in the Admin review queue or equivalent storage. 8. The system logs the delivery failure or handles it according to policy. 9. The system completes the current flow with the request still pending review. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The failure is temporary. 1. The system may retry notification delivery according to policy. 2. If retry succeeds, the Admin receives the notification. 3. If retry fails, the request still remains pending review.  5.1 The system cannot confirm whether delivery failed or succeeded. 1. The system treats the notification as failed or uncertain according to policy. 2. The system preserves the pending request and logs the issue.  8.1 Logging the notification failure also encounters a technical issue. 1. The system may still preserve the pending request. 2. The system handles the secondary failure according to error policy. |

## Use Case 39 – (UC-39) Submit Consultant Onboarding Request


| Field | Description |
|---|---|
| Use Case 39 – (UC-39) | Submit Consultant Onboarding Request |
| Related Requirements | FR-012, FR-015, FR-027 |
| Initiating Actor | Unassigned User |
| Actor’s Goal | To submit Consultant onboarding information for Admin review so that Consultant access can be approved and activated later. |
| Participating Actors | Admin |
| Preconditions | 1. The user is authenticated. 2. The user is in the onboarding flow. 3. The account is currently in Unassigned state. 4. The Consultant role is available for selection. |
| Postconditions | 1. A Consultant onboarding request is created. 2. The request is recorded in a pending approval state. 3. Consultant permissions are not activated immediately. 4. The Admin is notified of the pending request. 5. The user is informed that Consultant activation depends on Admin approval. |
| Flow of Events for Main Success Scenario | 1. The Unassigned User enters the onboarding flow. 2. The system displays the available role options. 3. The user selects Consultant. 4. The system displays the required Consultant onboarding fields. 5. The user enters the required professional information. 6. The user submits the Consultant onboarding form. 7. The system validates the submitted professional data. 8. The system creates the Consultant onboarding request. 9. The system marks the request as pending Admin review. 10. The system notifies the Admin of the pending request. 11. The system informs the user that Consultant access will activate only after Admin approval. |
| Flow of Events for Extensions (Alternate Scenarios) | 7.1 The submitted professional data is missing or invalid. 1. The system does not create the onboarding request. 2. The system displays the related validation errors. 3. The user corrects the data and resubmits the form.  10.1 Admin notification delivery fails. 1. The system preserves the pending request if it was created successfully. 2. The system logs or handles the notification failure according to policy. 3. The user is still informed that the request is pending review.  8.1 A temporary technical issue occurs while creating the request. 1. The system cannot create the Consultant onboarding request. 2. The system displays an error message and asks the user to try again later. |

## Use Case 40 – (UC-40) Validate Professional Data


| Field | Description |
|---|---|
| Use Case 40 – (UC-40) | Validate Professional Data |
| Related Requirements | FR-012, FR-015, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the submitted Consultant professional data is complete and valid before the onboarding request can be accepted for Admin review. |
| Participating Actors | Unassigned User |
| Preconditions | 1. The Unassigned User is authenticated. 2. The user is in the onboarding flow. 3. The user has selected the Consultant role. 4. The user has entered Consultant onboarding data and submitted it for processing. |
| Postconditions | 1. The system determines whether the submitted professional data is valid. 2. If the data is valid, the Consultant onboarding request flow continues. 3. If the data is invalid or incomplete, the system rejects the submission and returns validation errors. 4. Request creation does not proceed until the data is corrected. |
| Flow of Events for Main Success Scenario | 1. The Unassigned User selects the Consultant role. 2. The system displays the Consultant onboarding fields. 3. The user enters the required professional information. 4. The user submits the Consultant onboarding form. 5. The system receives the submitted professional data. 6. The system checks whether all required Consultant fields are present. 7. The system validates the entered values according to Consultant onboarding rules. 8. The system confirms that the submitted professional data is complete and valid. 9. The system marks the validation step as successful. 10. The system returns control to the Submit Consultant Onboarding Request use case so the request can be created. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 Required professional fields are missing. 1. The system detects that one or more required fields are not provided. 2. The system marks the validation as failed. 3. The system returns the missing-field result to the Consultant onboarding flow.  7.1 One or more entered values are invalid. 1. The system detects that one or more values do not satisfy Consultant onboarding rules. 2. The system marks the validation as failed. 3. The system returns the validation error result to the Consultant onboarding flow.  8.1 A temporary technical issue occurs during professional data validation. 1. The system cannot complete the validation process. 2. The system returns an error result to the calling use case. 3. The system asks the user to try again later. |

## Use Case 41 – (UC-41) Show Validation Error (Consultant Onboarding)


| Field | Description |
|---|---|
| Use Case 41 – (UC-41) | Show Validation Error (Consultant Onboarding) |
| Related Requirements | FR-012, FR-015, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To inform the Unassigned User that the submitted Consultant onboarding data is missing, invalid, or inconsistent, and to explain what must be corrected before the request can continue. |
| Participating Actors | Unassigned User |
| Preconditions | 1. The Unassigned User is authenticated. 2. The user is in the onboarding flow. 3. The user has selected the Consultant role. 4. The user has submitted Consultant onboarding data. 5. The system has validated the submitted professional data. 6. The system has detected one or more validation failures. |
| Postconditions | 1. The current Consultant onboarding submission is rejected. 2. The invalid or missing professional fields are highlighted. 3. The system displays the corresponding validation error messages. 4. The user is able to correct the data and resubmit the Consultant onboarding form. 5. Request creation does not continue until the validation issues are resolved. |
| Flow of Events for Main Success Scenario | 1. The Unassigned User selects the Consultant role. 2. The system displays the Consultant onboarding fields. 3. The user enters the required professional information. 4. The user submits the Consultant onboarding form. 5. The system receives the submitted professional data. 6. The system validates the data according to Consultant onboarding rules. 7. The system detects one or more missing, invalid, or inconsistent values. 8. The system rejects the current submission. 9. The system highlights the affected professional fields. 10. The system displays the corresponding validation error messages. 11. The user reviews the displayed errors. 12. The user corrects the invalid or missing data. 13. The user resubmits the Consultant onboarding form. |
| Flow of Events for Extensions (Alternate Scenarios) | 7.1 Required professional fields are missing. 1. The system detects that one or more required professional fields are not provided. 2. The system highlights the missing fields. 3. The system displays messages asking the user to complete the required information.  7.2 One or more entered values are invalid. 1. The system detects that one or more values do not satisfy Consultant onboarding rules. 2. The system highlights the invalid fields. 3. The system displays the related validation messages.  7.3 The submitted professional data is inconsistent. 1. The system detects that one or more values conflict with the expected Consultant data rules. 2. The system highlights the affected fields. 3. The system displays the corresponding validation error messages.  10.1 A temporary technical issue occurs while displaying validation details. 1. The system cannot display the full validation details normally. 2. The system displays a general error message. 3. The user is asked to try again later. |

## Use Case 42 – (UC-42) Notify Admin of Pending Request (Consultant Onboarding)


| Field | Description |
|---|---|
| Use Case 42 – (UC-42) | Notify Admin of Pending Request (Consultant Onboarding) |
| Related Requirements | FR-012, FR-015, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To notify the Admin that a new Consultant onboarding request has been submitted and is awaiting review. |
| Participating Actors | Admin, Unassigned User |
| Preconditions | 1. The Unassigned User is authenticated. 2. The user has selected the Consultant role. 3. The submitted professional data has been validated successfully. 4. The Consultant onboarding request has been created and marked as pending review. 5. An Admin notification mechanism is available. |
| Postconditions | 1. The Admin is notified of the pending Consultant onboarding request. 2. The request becomes visible or actionable in the Admin review flow. 3. The pending request remains stored for later approval or rejection. |
| Flow of Events for Main Success Scenario | 1. The system creates the Consultant onboarding request successfully. 2. The system marks the request as pending Admin review. 3. The system prepares the pending-request notification details. 4. The system sends the notification to the Admin through the configured internal or external notification channel. 5. The Admin receives the pending request notification. 6. The system completes the notification step. 7. The Consultant onboarding flow continues with the request remaining in pending status. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The notification channel is temporarily unavailable. 1. The system cannot deliver the Admin notification successfully. 2. The system handles the issue according to notification failure policy. 3. The pending request remains stored for Admin review.  5.1 The Admin does not view the notification immediately. 1. The system still preserves the pending request in the review queue. 2. The request remains available for later Admin action.  3.1 A temporary technical issue occurs while preparing notification details. 1. The system cannot complete the notification preparation step. 2. The system returns an error or failure result to the calling use case. |

## Use Case 43 – (UC-43) Notification Delivery Fails (Consultant Onboarding)


| Field | Description |
|---|---|
| Use Case 43 – (UC-43) | Notification Delivery Fails (Consultant Onboarding) |
| Related Requirements | FR-012, FR-015, FR-027 |
| Initiating Actor | System |
| Actor’s Goal | To handle the situation where the system cannot successfully notify the Admin after a Consultant onboarding request has been submitted. |
| Participating Actors | Admin, Unassigned User |
| Preconditions | 1. The Unassigned User has submitted a Consultant onboarding request successfully. 2. The system has created the pending request. 3. The system attempts to notify the Admin of the pending request. 4. The notification delivery attempt fails. |
| Postconditions | 1. The pending Consultant onboarding request remains stored in the system. 2. Consultant permissions are still not activated. 3. The notification failure is recognized, logged, or handled according to system policy. 4. The request remains available for later Admin review. |
| Flow of Events for Main Success Scenario | 1. The system creates the Consultant onboarding request successfully. 2. The system marks the request as pending Admin review. 3. The system attempts to notify the Admin. 4. The notification delivery fails. 5. The system detects or receives the notification failure result. 6. The system does not cancel the pending onboarding request. 7. The system preserves the request in the Admin review queue or equivalent storage. 8. The system logs the delivery failure or handles it according to policy. 9. The system completes the current flow with the request still pending review. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The failure is temporary. 1. The system may retry notification delivery according to policy. 2. If retry succeeds, the Admin receives the notification. 3. If retry fails, the request still remains pending review.  5.1 The system cannot confirm whether delivery failed or succeeded. 1. The system treats the notification as failed or uncertain according to policy. 2. The system preserves the pending request and logs the issue.  8.1 Logging the notification failure also encounters a technical issue. 1. The system may still preserve the pending request. 2. The system handles the secondary failure according to error policy. |

## Use Case 44 – (UC-44) Submit Student-to-Consultant Upgrade Request


| Field | Description |
|---|---|
| Use Case 44 – (UC-44) | Submit Student-to-Consultant Upgrade Request |
| Related Requirements | FR-016, FR-017, FR-018 |
| Initiating Actor | Student |
| Actor’s Goal | To submit a request to upgrade the current Student account so that Consultant access can be added after Admin approval while preserving the Student account and history. |
| Participating Actors | Admin |
| Preconditions | 1. The user is authenticated. 2. The user currently has an active Student account. 3. The Student is allowed to access the upgrade-request feature. 4. The upgrade request page or form is available. |
| Postconditions | 1. A Student-to-Consultant upgrade request is created. 2. The request is recorded in a pending approval state. 3. The Student keeps current Student permissions until Admin approval is granted. 4. The Admin is notified of the pending upgrade request. 5. The Student is informed that Consultant access will activate only after Admin approval. 6. Existing Student data and history remain preserved. |
| Flow of Events for Main Success Scenario | 1. The Student opens the upgrade request feature. 2. The system displays the upgrade request form. 3. The Student enters the required upgrade information. 4. The Student submits the upgrade request form. 5. The system validates the submitted upgrade data. 6. The system checks whether a duplicate pending or blocking request already exists. 7. The system creates the Student-to-Consultant upgrade request. 8. The system marks the request as pending Admin approval. 9. The system preserves the Student account as the currently active access mode until approval. 10. The system notifies the Admin of the pending upgrade request. 11. The system informs the Student that the request has been submitted and is awaiting Admin review. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The submitted upgrade data is missing or invalid. 1. The system does not create the upgrade request. 2. The system displays the related validation errors. 3. The Student corrects the data and resubmits the form.  6.1 A duplicate upgrade request already exists. 1. The system does not create a new upgrade request. 2. The system informs the Student that a pending or existing request already exists.  10.1 Admin notification delivery fails. 1. The system preserves the pending upgrade request if it was created successfully. 2. The system logs or handles the notification failure according to policy. 3. The Student is still informed that the request is pending review.  7.1 A temporary technical issue occurs while creating the upgrade request. 1. The system cannot create the Student-to-Consultant upgrade request. 2. The system displays an error message and asks the Student to try again later. |

## Use Case 45 – (UC-45) Validate Upgrade Data


| Field | Description |
|---|---|
| Use Case 45 – (UC-45) | Validate Upgrade Data |
| Related Requirements | FR-016, FR-017, FR-018 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the submitted Student-to-Consultant upgrade data is complete and valid before the upgrade request can be accepted for Admin review. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The Student currently has an active Student account. 3. The Student has opened the upgrade request form. 4. The Student has entered upgrade data and submitted it for processing. |
| Postconditions | 1. The system determines whether the submitted upgrade data is valid. 2. If the data is valid, the upgrade request flow continues. 3. If the data is invalid or incomplete, the system rejects the submission and returns validation errors. 4. Upgrade request creation does not proceed until the data is corrected. |
| Flow of Events for Main Success Scenario | 1. The Student opens the Student-to-Consultant upgrade request form. 2. The system displays the required upgrade fields. 3. The Student enters the required upgrade information. 4. The Student submits the upgrade form. 5. The system receives the submitted upgrade data. 6. The system checks whether all required upgrade fields are present. 7. The system validates the entered values according to upgrade-request rules. 8. The system confirms that the submitted upgrade data is complete and valid. 9. The system marks the validation step as successful. 10. The system returns control to the Submit Student-to-Consultant Upgrade Request use case so the request can be created. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 Required upgrade fields are missing. 1. The system detects that one or more required fields are not provided. 2. The system marks the validation as failed. 3. The system returns the missing-field result to the upgrade flow.  7.1 One or more entered values are invalid. 1. The system detects that one or more values do not satisfy upgrade-request rules. 2. The system marks the validation as failed. 3. The system returns the validation error result to the upgrade flow.  8.1 A temporary technical issue occurs during upgrade data validation. 1. The system cannot complete the validation process. 2. The system returns an error result to the calling use case. 3. The system asks the Student to try again later. |

## Use Case 46 – (UC-46) Show Validation Error (Upgrade Request)


| Field | Description |
|---|---|
| Use Case 46 – (UC-46) | Show Validation Error (Upgrade Request) |
| Related Requirements | FR-016, FR-017, FR-018 |
| Initiating Actor | System |
| Actor’s Goal | To inform the Student that the submitted upgrade data is missing, invalid, or inconsistent, and to explain what must be corrected before the upgrade request can continue. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The Student has opened the upgrade request flow. 3. The Student has submitted upgrade data. 4. The system has validated the submitted upgrade data. 5. The system has detected one or more validation failures. |
| Postconditions | 1. The current upgrade submission is rejected. 2. The invalid or missing fields are highlighted. 3. The system displays the corresponding validation error messages. 4. The Student is able to correct the data and resubmit the upgrade form. 5. Upgrade request creation does not continue until the validation issues are resolved. |
| Flow of Events for Main Success Scenario | 1. The Student opens the Student-to-Consultant upgrade request form. 2. The system displays the required upgrade fields. 3. The Student enters the required information. 4. The Student submits the upgrade form. 5. The system receives the submitted upgrade data. 6. The system validates the data according to upgrade-request rules. 7. The system detects one or more missing, invalid, or inconsistent values. 8. The system rejects the current submission. 9. The system highlights the affected fields. 10. The system displays the corresponding validation error messages. 11. The Student reviews the displayed errors. 12. The Student corrects the invalid or missing data. 13. The Student resubmits the upgrade form. |
| Flow of Events for Extensions (Alternate Scenarios) | 7.1 Required upgrade fields are missing. 1. The system detects that one or more required fields are not provided. 2. The system highlights the missing fields. 3. The system displays messages asking the Student to complete the required information.  7.2 One or more entered values are invalid. 1. The system detects that one or more values do not satisfy upgrade-request rules. 2. The system highlights the invalid fields. 3. The system displays the related validation messages.  7.3 The submitted upgrade data is inconsistent. 1. The system detects that one or more values conflict with the expected upgrade data rules. 2. The system highlights the affected fields. 3. The system displays the corresponding validation error messages.  10.1 A temporary technical issue occurs while displaying validation details. 1. The system cannot display the full validation details normally. 2. The system displays a general error message. 3. The Student is asked to try again later. |

## Use Case 47 – (UC-47) Duplicate Request Already Exists


| Field | Description |
|---|---|
| Use Case 47 – (UC-47) | Duplicate Request Already Exists |
| Related Requirements | FR-016, FR-017, FR-018 |
| Initiating Actor | System |
| Actor’s Goal | To prevent the creation of a new Student-to-Consultant upgrade request when the Student already has an existing pending or blocking upgrade request. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The Student currently has an active Student account. 3. The Student has submitted an upgrade request form successfully up to the duplicate-check stage. 4. The system checks existing upgrade request records for the same Student. 5. A duplicate pending or blocking upgrade request already exists. |
| Postconditions | 1. A new upgrade request is not created. 2. The existing pending or blocking request remains unchanged. 3. The Student is informed that another request already exists. 4. The Student keeps current Student permissions. |
| Flow of Events for Main Success Scenario | 1. The Student submits a Student-to-Consultant upgrade request form. 2. The system validates the submitted upgrade data successfully. 3. The system checks whether an existing upgrade request already exists for the Student. 4. The system detects a pending or otherwise blocking request record. 5. The system prevents creation of a new upgrade request. 6. The system displays a message informing the Student that a duplicate request already exists. 7. The system keeps the existing request state unchanged. 8. The Student remains in the current Student access mode. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 No duplicate request exists. 1. The system determines that no blocking request is present. 2. The system continues the normal upgrade request creation flow.  4.1 The system cannot determine request duplication because of a temporary technical issue. 1. The system cannot safely continue request creation. 2. The system displays an error message and asks the Student to try again later.  6.1 The Student attempts to resubmit without resolving the duplicate condition. 1. The system again detects the existing request. 2. The system continues to block creation of a new request. |

## Use Case 48 – (UC-48) Notify Admin of Pending Upgrade


| Field | Description |
|---|---|
| Use Case 48 – (UC-48) | Notify Admin of Pending Upgrade |
| Related Requirements | FR-016, FR-017, FR-018 |
| Initiating Actor | System |
| Actor’s Goal | To notify the Admin that a new Student-to-Consultant upgrade request has been submitted and is awaiting review. |
| Participating Actors | Admin, Student |
| Preconditions | 1. The Student is authenticated. 2. The Student has submitted the upgrade request successfully. 3. The submitted upgrade data has been validated successfully. 4. The upgrade request has been created and marked as pending review. 5. An Admin notification mechanism is available. |
| Postconditions | 1. The Admin is notified of the pending upgrade request. 2. The request becomes visible or actionable in the Admin review flow. 3. The pending request remains stored for later approval or rejection. 4. The Student keeps current Student permissions until Admin approval is granted. |
| Flow of Events for Main Success Scenario | 1. The system creates the Student-to-Consultant upgrade request successfully. 2. The system marks the request as pending Admin review. 3. The system prepares the pending-upgrade notification details. 4. The system sends the notification to the Admin through the configured internal or external notification channel. 5. The Admin receives the pending upgrade notification. 6. The system completes the notification step. 7. The upgrade request flow continues with the request remaining in pending status. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The notification channel is temporarily unavailable. 1. The system cannot deliver the Admin notification successfully. 2. The system handles the issue according to notification failure policy. 3. The pending request remains stored for Admin review.  5.1 The Admin does not view the notification immediately. 1. The system still preserves the pending request in the review queue. 2. The request remains available for later Admin action.  3.1 A temporary technical issue occurs while preparing notification details. 1. The system cannot complete the notification preparation step. 2. The system returns an error or failure result to the calling use case. |

## Use Case 49 – (UC-49) Notification Delivery Fails (Upgrade Request)


| Field | Description |
|---|---|
| Use Case 49 – (UC-49) | Notification Delivery Fails (Upgrade Request) |
| Related Requirements | FR-016, FR-017, FR-018 |
| Initiating Actor | System |
| Actor’s Goal | To handle the situation where the system cannot successfully notify the Admin after a Student-to-Consultant upgrade request has been submitted. |
| Participating Actors | Admin, Student |
| Preconditions | 1. The Student has submitted a Student-to-Consultant upgrade request successfully. 2. The system has created the pending upgrade request. 3. The system attempts to notify the Admin of the pending upgrade. 4. The notification delivery attempt fails. |
| Postconditions | 1. The pending upgrade request remains stored in the system. 2. The Student keeps current Student permissions until Admin approval is granted. 3. The notification failure is recognized, logged, or handled according to system policy. 4. The request remains available for later Admin review. |
| Flow of Events for Main Success Scenario | 1. The system creates the Student-to-Consultant upgrade request successfully. 2. The system marks the request as pending Admin review. 3. The system attempts to notify the Admin. 4. The notification delivery fails. 5. The system detects or receives the notification failure result. 6. The system does not cancel the pending upgrade request. 7. The system preserves the request in the Admin review queue or equivalent storage. 8. The system logs the delivery failure or handles it according to policy. 9. The system completes the current flow with the request still pending review. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The failure is temporary. 1. The system may retry notification delivery according to policy. 2. If retry succeeds, the Admin receives the notification. 3. If retry fails, the request still remains pending review.  5.1 The system cannot confirm whether delivery failed or succeeded. 1. The system treats the notification as failed or uncertain according to policy. 2. The system preserves the pending request and logs the issue.  8.1 Logging the notification failure also encounters a technical issue. 1. The system may still preserve the pending request. 2. The system handles the secondary failure according to error policy. |

## Use Case 50 – (UC-50) Switch Active Role


| Field | Description |
|---|---|
| Use Case 50 – (UC-50) | Switch Active Role |
| Related Requirements | FR-019, FR-020 |
| Initiating Actor | Student |
| Actor’s Goal | To switch the active mode of the account between Student and Consultant so that the system adapts to the selected role without requiring a separate login. |
| Participating Actors | None |
| Preconditions | 1. The user is authenticated. 2. The account has both Student and Consultant access. 3. The role switcher is available in the current session. 4. The user is currently operating in one active mode. |
| Postconditions | 1. The active role is changed to the selected mode. 2. The system updates the session to reflect the selected active role. 3. The dashboard, navigation, visible features, and permissions adapt to the selected mode. 4. The user continues in the same authenticated session under the selected role context. |
| Flow of Events for Main Success Scenario | 1. The Student opens the role switcher in the authenticated session. 2. The system displays the available roles for the account. 3. The Student selects the target role to activate. 4. The system validates that the requested role is available for the account. 5. The system updates the active role in the current session. 6. The system reloads the role-based interface. 7. The system adapts the dashboard, navigation, content, and permissions to the selected role. 8. The Student continues using the platform in the newly selected active mode. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The requested role is not available for the account. 1. The system does not switch the active role. 2. The system informs the user that the requested role is not available.  5.1 A temporary technical issue occurs while updating the active session role. 1. The system cannot complete the role switch. 2. The system keeps the existing active role unchanged. 3. The system displays an error message and asks the user to try again later.  6.1 The role-based interface cannot be reloaded successfully. 1. The system cannot fully refresh the interface for the selected role. 2. The system displays an error or temporary unavailable message.  2.1 Only one role is currently available for the account. 1. The system does not provide a valid switch target. 2. The user remains in the current active mode. |

## Use Case 51 – (UC-51) Validate Role Permissions


| Field | Description |
|---|---|
| Use Case 51 – (UC-51) | Validate Role Permissions |
| Related Requirements | FR-019, FR-020 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the requested target role is assigned to the account and can be activated in the current session. |
| Participating Actors | Student |
| Preconditions | 1. The user is authenticated. 2. The user has initiated a role switch request. 3. The system has received the requested target role. 4. The current account identity and assigned roles are available to the system. |
| Postconditions | 1. The system determines whether the requested role can be activated for the current account. 2. If the requested role is valid and available, the role switch flow continues. 3. If the requested role is not valid or not available, the role switch does not continue. |
| Flow of Events for Main Success Scenario | 1. The Student selects a target role from the role switcher. 2. The system receives the requested role-switch action. 3. The system identifies the roles assigned to the current account. 4. The system checks whether the requested target role belongs to the account. 5. The system confirms that the requested role is valid for activation. 6. The system marks the role-permission validation as successful. 7. The system returns control to the Switch Active Role use case so the session can be updated. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The requested role does not belong to the account. 1. The system marks the validation as failed. 2. The system returns a role-not-available result to the calling use case.  3.1 The assigned-role data cannot be retrieved. 1. The system cannot confirm the role availability for the account. 2. The system returns an error result to the calling use case.  5.1 The requested role is already the active role. 1. The system determines that no switch is required. 2. The system may keep the current interface unchanged.  6.1 A temporary technical issue occurs during role validation. 1. The system cannot complete the validation process. 2. The system returns an error result to the calling use case. 3. The user is asked to try again later. |

## Use Case 52 – (UC-52) Reload Role-Based Interface


| Field | Description |
|---|---|
| Use Case 52 – (UC-52) | Reload Role-Based Interface |
| Related Requirements | FR-019, FR-020 |
| Initiating Actor | System |
| Actor’s Goal | To refresh the interface so that the dashboard, navigation, content, and permitted actions match the newly selected active role. |
| Participating Actors | Student |
| Preconditions | 1. The user is authenticated. 2. The requested role switch has been validated successfully. 3. The system has updated or is ready to update the active role in the current session. 4. The role-based interface components are available for loading. |
| Postconditions | 1. The interface is refreshed according to the selected active role. 2. The dashboard, navigation, and visible functionality match the selected role. 3. The user continues the session with the correct role-based experience. |
| Flow of Events for Main Success Scenario | 1. The system receives the confirmed target role after successful validation. 2. The system updates the active role context for the current session. 3. The system loads the dashboard and interface components associated with the selected role. 4. The system updates the visible menus, pages, and actions. 5. The system applies the correct permissions and role-based content visibility. 6. The system completes the interface refresh. 7. The system returns control to the Switch Active Role use case so the user can continue in the selected mode. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The interface components for the selected role cannot be loaded. 1. The system cannot complete the role-based interface refresh. 2. The system displays an error or temporary unavailable message.  4.1 Some navigation elements fail to update correctly. 1. The system detects that the interface is not fully synchronized with the selected role. 2. The system may retry the refresh or display an error message according to policy.  5.1 Permission data cannot be applied correctly. 1. The system cannot guarantee the correct role-based experience. 2. The system treats the reload as failed or incomplete according to policy.  6.1 A temporary technical issue occurs during interface refresh. 1. The system cannot complete the reload operation. 2. The system asks the user to try again later. |

## Use Case 53 – (UC-53) Role Not Available for Account


| Field | Description |
|---|---|
| Use Case 53 – (UC-53) | Role Not Available for Account |
| Related Requirements | FR-019, FR-020 |
| Initiating Actor | System |
| Actor’s Goal | To prevent switching to a role that is not assigned to the current account and keep the session in a valid active mode. |
| Participating Actors | Student |
| Preconditions | 1. The user is authenticated. 2. The user has attempted to switch to another role. 3. The system has checked or is checking the roles assigned to the account. 4. The requested target role is not available for the current account. |
| Postconditions | 1. The requested role is not activated. 2. The current active role remains unchanged. 3. The system informs the user that the requested role is not available for the account. 4. The session continues safely in the existing active mode. |
| Flow of Events for Main Success Scenario | 1. The Student opens the role switcher and selects a target role. 2. The system receives the role-switch request. 3. The system validates the roles assigned to the account. 4. The system detects that the requested role is not available for the current account. 5. The system blocks the requested role switch. 6. The system keeps the current active role unchanged. 7. The system displays a message informing the user that the selected role is not available for the account. 8. The Student continues using the system in the current active mode. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The role validation cannot be completed because of a temporary technical issue. 1. The system cannot confirm whether the requested role is available. 2. The system does not perform the role switch. 3. The system displays an error message and asks the user to try again later.  4.1 The requested role becomes available after a later account change. 1. The current attempt still fails if the role was not available at validation time. 2. The user may attempt the role switch again later.  7.1 The detailed reason cannot be displayed to the user. 1. The system displays a general role-unavailable message. 2. The current session remains unchanged. |

## Use Case 54 – (UC-54) Manage User Profile


| Field | Description |
|---|---|
| Use Case 54 – (UC-54) | Manage User Profile |
| Related Requirements | FR-028, FR-029, FR-030, FR-031, FR-032, FR-033 |
| Initiating Actor | Student, Company, Consultant |
| Actor’s Goal | To view and update profile information so that account data remains accurate, role-specific fields are maintained correctly, and derived indicators such as profile completeness are refreshed. |
| Participating Actors | None |
| Preconditions | 1. The user is authenticated. 2. The user has an active role-based account. 3. The profile management page is accessible. 4. The user has permission to update the profile fields associated with the active role. |
| Postconditions | 1. The submitted profile changes are saved successfully. 2. The profile data is updated according to the active role. 3. The system recalculates derived indicators such as profile completeness. 4. The updated profile is displayed to the user. |
| Flow of Events for Main Success Scenario | 1. The user opens the profile management page. 2. The system displays the current profile information and editable fields for the active role. 3. The user reviews the existing profile data. 4. The user edits one or more profile fields. 5. The user submits the profile update form. 6. The system validates the submitted profile data. 7. The system saves the updated profile information. 8. The system recalculates profile completeness and related derived indicators. 9. The system refreshes the displayed profile information. 10. The user views the updated profile successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The submitted profile data is missing or invalid. 1. The system does not save the profile changes. 2. The system displays the related validation errors. 3. The user corrects the data and resubmits the form.  7.1 A server-side technical issue occurs during profile update. 1. The system cannot complete the profile update. 2. The system displays an error message and asks the user to try again later.  8.1 Profile completeness cannot be recalculated successfully. 1. The system may save the profile changes if allowed by policy. 2. The system handles the derived-indicator refresh failure according to system policy.  2.1 The active role has role-specific profile fields. 1. The system displays the profile fields relevant to the active Student, Company, or Consultant role. 2. The user edits only the fields permitted for that role. |

## Use Case 55 – (UC-55) Validate Profile Data


| Field | Description |
|---|---|
| Use Case 55 – (UC-55) | Validate Profile Data |
| Related Requirements | FR-028, FR-029, FR-030, FR-031, FR-033 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the submitted profile data is complete, valid, and consistent with the active role before the profile update is saved. |
| Participating Actors | Student, Company, Consultant |
| Preconditions | 1. The user is authenticated. 2. The user has opened the profile management page. 3. The user has edited profile data. 4. The user has submitted the profile update form. 5. The active role context is available to the system. |
| Postconditions | 1. The system determines whether the submitted profile data is valid. 2. If the data is valid, the profile update flow continues. 3. If the data is invalid or incomplete, the system rejects the submission and returns validation errors. 4. Profile changes are not saved until the validation issues are resolved. |
| Flow of Events for Main Success Scenario | 1. The user edits the profile fields and submits the profile update form. 2. The system receives the submitted profile data. 3. The system identifies the active role of the user. 4. The system checks whether all required profile fields for that role are present. 5. The system validates the entered values according to the rules for the active role. 6. The system confirms that the submitted profile data is complete and valid. 7. The system marks the validation step as successful. 8. The system returns control to the Manage User Profile use case so the update can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 Required profile fields are missing. 1. The system detects that one or more required fields are not provided. 2. The system marks the validation as failed. 3. The system returns the missing-field result to the profile-update flow.  5.1 One or more entered values are invalid. 1. The system detects that one or more values do not satisfy the profile rules. 2. The system marks the validation as failed. 3. The system returns the validation error result to the profile-update flow.  3.1 The active role determines different validation rules. 1. The system applies the validation rules corresponding to the active Student, Company, or Consultant role. 2. The system validates the submitted data against that role’s profile requirements.  6.1 A temporary technical issue occurs during profile validation. 1. The system cannot complete the validation process. 2. The system returns an error result to the calling use case. 3. The user is asked to try again later. |

## Use Case 56 – (UC-56) Recalculate Profile Completeness


| Field | Description |
|---|---|
| Use Case 56 – (UC-56) | Recalculate Profile Completeness |
| Related Requirements | FR-032, FR-033 |
| Initiating Actor | System |
| Actor’s Goal | To refresh derived indicators such as profile completeness after a successful profile update so that the user sees the latest completion status. |
| Participating Actors | Student, Company, Consultant |
| Preconditions | 1. The user is authenticated. 2. The profile update has been saved successfully or is ready for post-update processing. 3. The system has access to the updated profile data. 4. The completeness calculation rules are available. |
| Postconditions | 1. The system recalculates the profile completeness value or related derived indicators. 2. The updated completeness result is stored or displayed according to system design. 3. The user sees refreshed profile-completeness information if applicable. |
| Flow of Events for Main Success Scenario | 1. The system saves the updated profile information successfully. 2. The system retrieves the latest profile data for the user. 3. The system determines the active role and corresponding completeness rules. 4. The system calculates the updated profile-completeness value. 5. The system updates the derived profile indicators. 6. The system refreshes the displayed profile information. 7. The system returns control to the Manage User Profile use case so the user can view the updated profile state. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 Different roles use different completeness rules. 1. The system applies the completeness rules relevant to the active Student, Company, or Consultant profile. 2. The system calculates the result according to that role’s required and optional fields.  4.1 The completeness calculation cannot be completed because of a temporary technical issue. 1. The system cannot refresh the completeness indicator normally. 2. The system handles the failure according to policy.  5.1 The derived indicator cannot be stored successfully. 1. The system may still preserve the main profile changes if allowed by policy. 2. The system treats the completeness refresh as failed or incomplete according to policy. |

## Use Case 57 – (UC-57) Show Validation Error (Manage User Profile)


| Field | Description |
|---|---|
| Use Case 57 – (UC-57) | Show Validation Error (Manage User Profile) |
| Related Requirements | FR-028, FR-029, FR-030, FR-031, FR-033 |
| Initiating Actor | System |
| Actor’s Goal | To inform the user that the submitted profile data is missing, invalid, or inconsistent, and to explain what must be corrected before the profile update can continue. |
| Participating Actors | Student, Company, Consultant |
| Preconditions | 1. The user is authenticated. 2. The user has opened the profile management page. 3. The user has submitted profile changes. 4. The system has validated the submitted profile data. 5. The system has detected one or more validation failures. |
| Postconditions | 1. The current profile update submission is rejected. 2. The invalid or missing fields are highlighted. 3. The system displays the corresponding validation error messages. 4. The user can correct the data and resubmit the profile form. 5. Profile changes are not saved until the validation issues are resolved. |
| Flow of Events for Main Success Scenario | 1. The user edits the profile fields and submits the profile update form. 2. The system receives the submitted profile data. 3. The system validates the data according to the active role’s profile rules. 4. The system detects one or more missing, invalid, or inconsistent values. 5. The system rejects the current submission. 6. The system highlights the affected fields. 7. The system displays the corresponding validation error messages. 8. The user reviews the displayed errors. 9. The user corrects the invalid or missing data. 10. The user resubmits the profile update form. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 Required profile fields are missing. 1. The system detects that one or more required fields are not provided. 2. The system highlights the missing fields. 3. The system displays messages asking the user to complete the required information.  4.2 One or more entered values are invalid. 1. The system detects that one or more values do not satisfy the applicable profile rules. 2. The system highlights the invalid fields. 3. The system displays the related validation messages.  4.3 The submitted profile data is inconsistent with the active role rules. 1. The system detects that one or more values conflict with the expected Student, Company, or Consultant profile requirements. 2. The system highlights the affected fields. 3. The system displays the corresponding validation error messages.  7.1 A temporary technical issue occurs while displaying validation details. 1. The system cannot display the full validation details normally. 2. The system displays a general error message. 3. The user is asked to try again later. |

## Use Case 58 – (UC-58) Profile Update Fails (Server Error)


| Field | Description |
|---|---|
| Use Case 58 – (UC-58) | Profile Update Fails (Server Error) |
| Related Requirements | FR-028, FR-029, FR-030, FR-031, FR-033 |
| Initiating Actor | System |
| Actor’s Goal | To handle the situation where the system cannot complete the profile update because of a server-side technical issue. |
| Participating Actors | Student, Company, Consultant |
| Preconditions | 1. The user is authenticated. 2. The user has submitted valid profile data. 3. The system has started the profile update process. 4. A server-side technical issue occurs during update processing. |
| Postconditions | 1. The profile update is not completed successfully in the current attempt. 2. The system informs the user that the profile could not be updated. 3. The user is asked to try again later or repeat the process according to system policy. 4. The system preserves data consistency by preventing an incomplete update from being treated as successful. |
| Flow of Events for Main Success Scenario | 1. The user submits valid profile changes. 2. The system validates the submitted profile data successfully. 3. The system starts the profile update process. 4. A server-side technical issue occurs while saving the updated profile information or refreshing related derived data. 5. The system cannot complete the update successfully. 6. The system prevents the incomplete or inconsistent update from being treated as successful. 7. The system displays a profile update failure or temporary error message. 8. The user is asked to try again later or resubmit the profile changes. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The technical issue occurs while saving the profile record. 1. The system cannot store the profile changes correctly. 2. The system stops the update flow. 3. The system displays an error message.  4.2 The technical issue occurs while refreshing derived indicators. 1. The system cannot complete all post-update processing. 2. The system handles the situation according to policy for partial update or derived-data refresh failure.  7.1 The detailed error cannot be shown fully to the user. 1. The system displays a general profile update failure or temporary unavailable message. 2. The user is still informed that the update was not completed successfully. |

## Use Case 59 – (UC-59) Manage Organization Profile


| Field | Description |
|---|---|
| Use Case 59 – (UC-59) | Manage Organization Profile |
| Related Requirements | FR-028, FR-030, FR-033 |
| Initiating Actor | Company |
| Actor’s Goal | To view and update organization-specific profile information so that the company account remains accurate and trustworthy to students and the platform. |
| Participating Actors | None |
| Preconditions | 1. The Company user is authenticated. 2. The Company account is active. 3. The organization profile page is accessible. 4. The user has permission to edit company profile fields. |
| Postconditions | 1. The organization profile information is updated successfully. 2. The saved data reflects the latest submitted changes. 3. The updated organization profile becomes available in relevant company-facing views. |
| Flow of Events for Main Success Scenario | 1. The Company user opens the organization profile page. 2. The system displays the current organization profile information. 3. The Company user reviews the existing data. 4. The Company user edits one or more organization-specific fields. 5. The Company user submits the profile update form. 6. The system validates the submitted profile data. 7. The system saves the updated organization profile information. 8. The system refreshes the displayed profile data. 9. The Company user views the updated organization profile successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The submitted organization data is missing or invalid. 1. The system does not save the profile changes. 2. The system displays the related validation errors. 3. The Company user corrects the data and resubmits the form.  7.1 A server-side technical issue occurs during profile update. 1. The system cannot complete the organization profile update. 2. The system displays an error message and asks the user to try again later.  4.1 The Company user edits role-restricted fields. 1. The system allows editing only for the fields permitted for the Company role. 2. The system rejects unauthorized field changes according to policy. |

## Use Case 60 – (UC-60) Edit Student Profile


| Field | Description |
|---|---|
| Use Case 60 – (UC-60) | Edit Student Profile |
| Related Requirements | FR-028, FR-029, FR-032, FR-033 |
| Initiating Actor | Student |
| Actor’s Goal | To update student-specific profile information so that scholarships, recommendations, eligibility checks, and application flows use accurate data. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student account is active. 3. The Student profile page is accessible. 4. The user has permission to edit Student profile fields. |
| Postconditions | 1. The Student profile data is updated successfully. 2. The saved data reflects the latest submitted changes. 3. The system may refresh derived indicators such as profile completeness. |
| Flow of Events for Main Success Scenario | 1. The Student opens the Student profile page. 2. The system displays the current Student profile information. 3. The Student reviews the existing data. 4. The Student edits one or more Student-specific fields. 5. The Student submits the profile update form. 6. The system validates the submitted Student profile data. 7. The system saves the updated Student profile information. 8. The system refreshes the displayed profile data. 9. The Student views the updated profile successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The submitted Student data is missing or invalid. 1. The system does not save the profile changes. 2. The system displays the related validation errors. 3. The Student corrects the data and resubmits the form.  7.1 A server-side technical issue occurs during profile update. 1. The system cannot complete the Student profile update. 2. The system displays an error message and asks the Student to try again later.  4.1 The Student edits fields not allowed for the Student role. 1. The system restricts editing to permitted Student profile fields only. 2. Unauthorized field changes are rejected according to policy. |

## Use Case 61 – (UC-61) Update Photo, Bio & Language


| Field | Description |
|---|---|
| Use Case 61 – (UC-61) | Update Photo, Bio & Language |
| Related Requirements | FR-028, FR-033 |
| Initiating Actor | User |
| Actor’s Goal | To update shared personal profile details such as profile photo, biography, and language preferences. |
| Participating Actors | None |
| Preconditions | 1. The User is authenticated. 2. The account is active. 3. The shared profile settings area is accessible. 4. The user has permission to update common profile fields. |
| Postconditions | 1. The profile photo, bio, and language preferences are updated successfully. 2. The updated values are saved and displayed in the user profile. 3. The new values are available to other relevant system views. |
| Flow of Events for Main Success Scenario | 1. The User opens the shared profile settings area. 2. The system displays the current photo, bio, and language values. 3. The User updates one or more of these fields. 4. The User submits the changes. 5. The system validates the submitted data. 6. The system saves the updated profile values. 7. The system refreshes the displayed profile information. 8. The User views the updated profile details successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The submitted values are missing or invalid. 1. The system does not save the changes. 2. The system displays the related validation errors. 3. The User corrects the data and resubmits the form.  6.1 A server-side technical issue occurs during update. 1. The system cannot complete the update operation. 2. The system displays an error message and asks the user to try again later.  3.1 The profile photo format is not accepted. 1. The system rejects the submitted photo value. 2. The system asks the user to upload a supported file or image format. |

## Use Case 62 – (UC-62) View Profile Completeness


| Field | Description |
|---|---|
| Use Case 62 – (UC-62) | View Profile Completeness |
| Related Requirements | FR-032 |
| Initiating Actor | User |
| Actor’s Goal | To view the current profile completeness status so that missing or recommended profile information can be identified. |
| Participating Actors | None |
| Preconditions | 1. The User is authenticated. 2. The account is active. 3. The profile completeness indicator is available in the interface. |
| Postconditions | 1. The current profile completeness value or status is displayed to the user. 2. The user can identify whether additional profile data is recommended or missing. |
| Flow of Events for Main Success Scenario | 1. The User opens the profile page or relevant account area. 2. The system retrieves the stored profile data and completeness indicator. 3. The system displays the current profile completeness status. 4. The User reviews the completeness value and related profile state. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The completeness value is not available. 1. The system cannot retrieve the current completeness indicator. 2. The system displays a general unavailable or not-yet-calculated message.  3.1 A temporary technical issue occurs while loading the indicator. 1. The system cannot display the completeness status normally. 2. The system asks the user to try again later. |

## Use Case 63 – (UC-63) Manage Security Settings


| Field | Description |
|---|---|
| Use Case 63 – (UC-63) | Manage Security Settings |
| Related Requirements | FR-021, FR-033 |
| Initiating Actor | User |
| Actor’s Goal | To access and manage account-security options such as password-related settings and other available security controls. |
| Participating Actors | None |
| Preconditions | 1. The User is authenticated. 2. The account is active. 3. The security settings page is accessible. |
| Postconditions | 1. The User can view and manage available security settings. 2. Any permitted security changes are saved successfully if submitted. 3. The updated security state becomes active according to policy. |
| Flow of Events for Main Success Scenario | 1. The User opens the security settings page. 2. The system displays the available security settings and controls. 3. The User reviews the available security options. 4. The User selects a security action to manage. 5. The system opens the relevant security-management flow. 6. The User completes the selected security update. 7. The system saves the change if applicable. 8. The system displays the updated security state or confirmation message. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The selected security option is not available for the account. 1. The system does not continue the selected action. 2. The system informs the User that the option is unavailable.  7.1 A technical issue occurs while saving the security change. 1. The system cannot complete the update. 2. The system displays an error message and asks the User to try again later.  2.1 The security settings page cannot be loaded. 1. The system cannot display the security settings normally. 2. The system shows an error or temporary unavailable message. |

## Use Case 64 – (UC-64) Change Password


| Field | Description |
|---|---|
| Use Case 64 – (UC-64) | Change Password |
| Related Requirements | FR-021, FR-033 |
| Initiating Actor | User |
| Actor’s Goal | To change the current account password so that account access remains secure. |
| Participating Actors | None |
| Preconditions | 1. The User is authenticated. 2. The account is active. 3. The password-change feature is accessible. 4. The user is allowed to change the password for the account. |
| Postconditions | 1. The account password is updated successfully. 2. The new password satisfies the applicable password rules. 3. The user can use the new password for future authentication. |
| Flow of Events for Main Success Scenario | 1. The User opens the change-password form. 2. The system displays the required password fields. 3. The User enters the current password if required. 4. The User enters the new password and confirmation value. 5. The User submits the password-change request. 6. The system validates the submitted password data. 7. The system verifies that the new password satisfies the password rules. 8. The system updates the stored account password. 9. The system confirms that the password change is successful. 10. The User continues using the account with the updated security state. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The submitted password data is missing or invalid. 1. The system does not change the password. 2. The system displays the related validation errors. 3. The User corrects the data and resubmits the form.  7.1 The new password does not satisfy the password rules. 1. The system rejects the new password value. 2. The system displays the password rule violations.  8.1 A technical issue occurs while saving the new password. 1. The system cannot complete the password update. 2. The system displays an error message and asks the User to try again later. |

## Use Case 65 – (UC-65) Validate Password Rules


| Field | Description |
|---|---|
| Use Case 65 – (UC-65) | Validate Password Rules |
| Related Requirements | FR-021 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the submitted new password satisfies the required password policy before the password change is accepted. |
| Participating Actors | User |
| Preconditions | 1. The User is authenticated. 2. The User has initiated the password-change flow. 3. The User has submitted a new password value. 4. The password validation rules are available to the system. |
| Postconditions | 1. The system determines whether the submitted password satisfies the required policy. 2. If the password is valid, the change-password flow continues. 3. If the password is invalid, the system rejects the password value and returns validation errors. |
| Flow of Events for Main Success Scenario | 1. The User submits a new password during the password-change flow. 2. The system receives the submitted password value. 3. The system checks the password against the configured password rules. 4. The system confirms that the password meets the minimum length requirement. 5. The system confirms that the password contains the required uppercase letter, digit, and special character. 6. The system marks the password validation step as successful. 7. The system returns control to the Change Password use case so the password update can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The password is shorter than the minimum allowed length. 1. The system marks the validation as failed. 2. The system returns a password-rule error to the calling use case.  5.1 The password does not contain the required character types. 1. The system marks the validation as failed. 2. The system returns the corresponding rule-violation result.  6.1 A temporary technical issue occurs during password validation. 1. The system cannot complete the validation process. 2. The system returns an error result to the calling use case. 3. The User is asked to try again later. |

## Use Case 66 – (UC-66) View Profile


| Field | Description |
|---|---|
| Use Case 66 – (UC-66) | View Profile |
| Related Requirements | FR-028, FR-029, FR-030, FR-031 |
| Initiating Actor | User |
| Actor’s Goal | To view the current account profile and role-specific profile information. |
| Participating Actors | None |
| Preconditions | 1. The User is authenticated. 2. The account is active. 3. The profile view page is accessible. |
| Postconditions | 1. The current profile information is displayed successfully. 2. The user can review common and role-specific profile details according to access scope. |
| Flow of Events for Main Success Scenario | 1. The User opens the profile page. 2. The system retrieves the current profile data. 3. The system identifies the active role and applicable profile sections. 4. The system displays the common account information and relevant role-specific fields. 5. The User reviews the displayed profile information. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The profile data cannot be retrieved. 1. The system cannot load the profile information normally. 2. The system displays an error or temporary unavailable message.  3.1 The active role determines different visible profile sections. 1. The system displays the profile layout corresponding to the active Student, Company, or Consultant role. 2. The User sees only the profile information permitted for that role context. |

## Use Case 67 – (UC-67) Manage Consultant Profile


| Field | Description |
|---|---|
| Use Case 67 – (UC-67) | Manage Consultant Profile |
| Related Requirements | FR-028, FR-031, FR-033 |
| Initiating Actor | Consultant |
| Actor’s Goal | To view and update consultant-specific profile information so that expertise, credentials, and service-related profile details remain accurate. |
| Participating Actors | None |
| Preconditions | 1. The Consultant is authenticated. 2. The Consultant account is active. 3. The Consultant profile page is accessible. 4. The user has permission to edit Consultant-specific profile fields. |
| Postconditions | 1. The Consultant profile data is updated successfully. 2. The saved data reflects the latest submitted changes. 3. The updated profile becomes available in relevant consultant-facing views. |
| Flow of Events for Main Success Scenario | 1. The Consultant opens the Consultant profile page. 2. The system displays the current Consultant profile information. 3. The Consultant reviews the existing data. 4. The Consultant edits one or more Consultant-specific fields. 5. The Consultant submits the profile update form. 6. The system validates the submitted Consultant profile data. 7. The system saves the updated Consultant profile information. 8. The system refreshes the displayed profile data. 9. The Consultant views the updated profile successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The submitted Consultant data is missing or invalid. 1. The system does not save the profile changes. 2. The system displays the related validation errors. 3. The Consultant corrects the data and resubmits the form.  7.1 A server-side technical issue occurs during profile update. 1. The system cannot complete the Consultant profile update. 2. The system displays an error message and asks the Consultant to try again later.  4.1 The Consultant edits fields not allowed for the Consultant role. 1. The system restricts editing to permitted Consultant profile fields only. 2. Unauthorized field changes are rejected according to policy. |

## Use Case 68 – (UC-68) Browse / Search Scholarships


| Field | Description |
|---|---|
| Use Case 68 – (UC-68) | Browse / Search Scholarships |
| Related Requirements | FR-034, FR-035, FR-036, FR-044 |
| Initiating Actor | Student |
| Actor’s Goal | To browse, search, and refine scholarship listings so that relevant opportunities can be found efficiently. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has access to the scholarship discovery area. 3. Scholarship listings are available in the system. |
| Postconditions | 1. The system displays scholarship results based on the Student’s current browse, search, filter, and sort selections. 2. The Student can review the displayed scholarship list and continue to related actions such as viewing details. |
| Flow of Events for Main Success Scenario | 1. The Student opens the scholarship discovery page. 2. The system displays the available scholarship listings and discovery controls. 3. The Student enters a search term or browses the available opportunities. 4. The system applies the selected search and filter criteria. 5. The system retrieves the matching scholarship results. 6. The system sorts the result set according to the selected or default sort option. 7. The system displays the resulting scholarship list to the Student. 8. The Student reviews the displayed scholarships. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 No scholarships match the current criteria. 1. The system returns an empty result set. 2. The system informs the Student that no matching scholarships were found.  4.1 The Student wants to clear the current criteria. 1. The Student selects the reset option. 2. The system clears the applied filters and search values. 3. The system restores a broader result set.  6.1 A temporary technical issue occurs while retrieving or displaying results. 1. The system cannot display the scholarship results normally. 2. The system shows an error or temporary unavailable message. |

## Use Case 69 – (UC-69) Apply Search & Filter Criteria


| Field | Description |
|---|---|
| Use Case 69 – (UC-69) | Apply Search & Filter Criteria |
| Related Requirements | FR-034, FR-035, FR-044 |
| Initiating Actor | System |
| Actor’s Goal | To apply the Student’s selected search terms and filter criteria so that the scholarship result set is narrowed to relevant opportunities. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The Student is on the scholarship discovery page. 3. The Student has entered a search term, selected one or more filters, or both. 4. Scholarship search and filtering controls are available. |
| Postconditions | 1. The system applies the selected search and filter criteria to the scholarship dataset. 2. The result set is narrowed according to the submitted criteria. 3. The filtered result set becomes available for sorting and display. |
| Flow of Events for Main Success Scenario | 1. The Student enters a search term or selects one or more filter values. 2. The Student submits or confirms the criteria. 3. The system receives the selected search and filter values. 4. The system applies the criteria to the scholarship listings. 5. The system narrows the result set according to the selected category, country, deadline, funding type, academic level, tags, or other configured fields. 6. The system prepares the filtered scholarship results. 7. The system returns control to the Browse / Search Scholarships use case so the results can be sorted and displayed. |
| Flow of Events for Extensions (Alternate Scenarios) | 1.1 The Student applies only filters without a keyword search. 1. The system processes the selected filters only. 2. The system narrows the result set accordingly.  1.2 The Student enters only a search term without filters. 1. The system processes the search term only. 2. The system retrieves the matching scholarships accordingly.  5.1 The selected criteria return no matching scholarships. 1. The system produces an empty result set. 2. The system returns the no-results outcome to the calling use case.  4.1 A temporary technical issue occurs while applying the criteria. 1. The system cannot complete the filter/search operation. 2. The system returns an error result to the calling use case. 3. The Student is asked to try again later. |

## Use Case 70 – (UC-70) Sort Results


| Field | Description |
|---|---|
| Use Case 70 – (UC-70) | Sort Results |
| Related Requirements | FR-036 |
| Initiating Actor | System |
| Actor’s Goal | To arrange the scholarship results according to the selected or default sort order so that the Student can review them more effectively. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. A scholarship result set is available after browsing, searching, or filtering. 3. One or more supported sort options are available. |
| Postconditions | 1. The scholarship results are ordered according to the selected or default sort option. 2. The sorted result set is ready for display to the Student. |
| Flow of Events for Main Success Scenario | 1. The system receives a scholarship result set. 2. The Student selects a sort option, or the system applies the default ordering. 3. The system determines the applicable sort rule. 4. The system orders the results by relevance, deadline, newest, recommended, or other supported configured option. 5. The system prepares the sorted result set for display. 6. The system returns control to the Browse / Search Scholarships use case so the sorted results can be shown to the Student. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The Student does not select a sort option. 1. The system applies the default sort order. 2. The result set is ordered accordingly.  4.1 The selected sort option is unavailable or invalid. 1. The system cannot apply the requested sort rule. 2. The system falls back to the default sort order or returns an error according to policy.  4.2 A temporary technical issue occurs during result ordering. 1. The system cannot complete the sorting operation. 2. The system returns an error or fallback result to the calling use case. |

## Use Case 71 – (UC-71) No Results Found


| Field | Description |
|---|---|
| Use Case 71 – (UC-71) | No Results Found |
| Related Requirements | FR-034, FR-035, FR-036, FR-044 |
| Initiating Actor | System |
| Actor’s Goal | To inform the Student that the current search or filter criteria produced no matching scholarship results. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The Student has performed a browse, search, or filter action. 3. The system has applied the selected criteria to the scholarship listings. 4. No scholarship records match the resulting criteria. |
| Postconditions | 1. The system displays a no-results message to the Student. 2. The Student remains able to modify or clear the applied criteria and search again. |
| Flow of Events for Main Success Scenario | 1. The Student performs a search or applies one or more filters. 2. The system processes the request and checks the scholarship listings. 3. The system determines that no scholarships match the current criteria. 4. The system displays a no-results message. 5. The system keeps the current search and filter controls available. 6. The Student reviews the no-results state and may refine the criteria. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The system can suggest retry actions or broader discovery options. 1. The system may prompt the Student to change or remove some filters. 2. The Student may continue by editing the criteria.  5.1 The Student chooses to reset the current filters. 1. The system clears the applied criteria. 2. The system restores a broader result set.  3.1 A temporary technical issue prevents the result count from being determined correctly. 1. The system cannot confirm whether matching results exist. 2. The system displays an error or temporary unavailable message instead of a confirmed no-results message. |

## Use Case 72 – (UC-72) Reset Filters


| Field | Description |
|---|---|
| Use Case 72 – (UC-72) | Reset Filters |
| Related Requirements | FR-035, FR-036, FR-044 |
| Initiating Actor | Student |
| Actor’s Goal | To clear the currently applied search and filter criteria so that a broader or default scholarship result set can be shown again. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student is on the scholarship discovery page. 3. One or more search terms or filters are currently applied. |
| Postconditions | 1. The applied search and filter criteria are cleared according to system behavior. 2. The scholarship result set is refreshed using the broader or default criteria. 3. The Student can continue browsing from the reset state. |
| Flow of Events for Main Success Scenario | 1. The Student reviews the current filtered scholarship results. 2. The Student selects the reset option. 3. The system clears the active search term and filter values according to design. 4. The system refreshes the scholarship result set. 5. The system applies the default browse state or broader criteria. 6. The system displays the refreshed scholarship results. 7. The Student continues browsing from the reset state. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 No filters or search values are currently applied. 1. The system detects that there is nothing to reset. 2. The system keeps the current default result set unchanged.  4.1 A temporary technical issue occurs while refreshing the result set. 1. The system cannot complete the reset-and-refresh action normally. 2. The system displays an error or temporary unavailable message.  5.1 The default browse state cannot be restored correctly. 1. The system cannot rebuild the expected broad result set. 2. The system displays an error message or fallback listing according to policy. |

## Use Case 73 – (UC-73) View Scholarship Details


| Field | Description |
|---|---|
| Use Case 73 – (UC-73) | View Scholarship Details |
| Related Requirements | FR-040, FR-041, FR-042, FR-043 |
| Initiating Actor | Student |
| Actor’s Goal | To view the full scholarship details so that the Student can understand the opportunity, requirements, and application method before taking further action. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has access to scholarship discovery or listing pages. 3. The Student selects a scholarship listing to view. |
| Postconditions | 1. The scholarship details are displayed successfully if the listing is available. 2. The Student can review the scholarship information, required details, and applicable application path. 3. If the listing is an in-app scholarship, the system can display the application form structure and required documents. 4. If the listing is an external scholarship, the system can display or prepare the external application URL. 5. If the listing is not found or inactive, the system does not continue the normal details flow. |
| Flow of Events for Main Success Scenario | 1. The Student selects a scholarship listing from the available results. 2. The system receives the scholarship-details request. 3. The system retrieves the scholarship attributes. 4. The system determines the scholarship listing type. 5. The system displays the scholarship core details, including the title, description, deadline, eligibility information, and other configured attributes. 6. If the scholarship is an in-app listing, the system loads the application form structure and required document information. 7. If the scholarship is an external listing, the system loads the external application URL information. 8. The system displays the completed scholarship details view to the Student. 9. The Student reviews the scholarship details successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The selected scholarship is not found. 1. The system cannot locate the requested scholarship record. 2. The system displays a not-found message or equivalent result.  4.1 The selected scholarship is inactive or unavailable. 1. The system determines that the scholarship cannot be viewed as an active opportunity. 2. The system displays an inactive or unavailable message according to policy.  6.1 The scholarship is not an in-app listing. 1. The system does not load the in-app application form structure. 2. The system continues according to the listing type.  7.1 The scholarship is not an external listing. 1. The system does not load an external application URL. 2. The system continues according to the listing type.  8.1 A temporary technical issue occurs while loading scholarship details. 1. The system cannot complete the scholarship details page normally. 2. The system displays an error or temporary unavailable message. |

## Use Case 74 – (UC-74) Retrieve Scholarship Attributes


| Field | Description |
|---|---|
| Use Case 74 – (UC-74) | Retrieve Scholarship Attributes |
| Related Requirements | FR-040, FR-043 |
| Initiating Actor | System |
| Actor’s Goal | To retrieve the scholarship’s stored attributes so that the system can display the correct scholarship details to the Student. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The Student has requested to view a scholarship listing. 3. The system has received the scholarship identifier or equivalent selection context. |
| Postconditions | 1. The system retrieves the scholarship attributes if the listing exists and is available for retrieval. 2. The scholarship data becomes available for the details page. 3. If the scholarship cannot be retrieved, the normal details flow does not continue. |
| Flow of Events for Main Success Scenario | 1. The Student selects a scholarship listing to view. 2. The system receives the scholarship-details request. 3. The system identifies the target scholarship record. 4. The system retrieves the stored scholarship attributes. 5. The system confirms that the scholarship data is available for display. 6. The system returns the retrieved scholarship attributes to the View Scholarship Details use case so the details page can continue loading. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The scholarship identifier is invalid or incomplete. 1. The system cannot resolve the requested scholarship record. 2. The system returns a not-found or invalid-request result.  4.1 The scholarship record does not exist. 1. The system cannot retrieve the scholarship attributes. 2. The system returns a not-found result to the calling use case.  5.1 The scholarship exists but is inactive or unavailable. 1. The system determines that the scholarship cannot continue through the normal display flow. 2. The system returns an inactive or unavailable result to the calling use case.  4.2 A temporary technical issue occurs during attribute retrieval. 1. The system cannot complete the scholarship retrieval process. 2. The system returns an error result to the calling use case. 3. The Student is asked to try again later. |

## Use Case 75 – (UC-75) Load Application Form Structure [In-App Listing]


| Field | Description |
|---|---|
| Use Case 75 – (UC-75) | Load Application Form Structure [In-App Listing] |
| Related Requirements | FR-041, FR-042 |
| Initiating Actor | System |
| Actor’s Goal | To load the application form structure and required document information when the selected scholarship is an in-app listing. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The system has retrieved the scholarship attributes successfully. 3. The selected scholarship is identified as an in-app listing. 4. The scholarship has configured application form fields and/or required documents. |
| Postconditions | 1. The application form structure for the in-app scholarship is loaded successfully. 2. The required application document information is made available for the scholarship details view. 3. The Student can review the in-app application requirements from the details page. |
| Flow of Events for Main Success Scenario | 1. The system determines that the selected scholarship is an in-app listing. 2. The system retrieves the configured application form fields for the scholarship. 3. The system retrieves the required document definitions associated with the scholarship. 4. The system prepares the form structure and document requirements for display. 5. The system returns the prepared in-app application structure to the View Scholarship Details use case. 6. The scholarship details page displays the application fields and required documents to the Student. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No application form fields are configured. 1. The system determines that no form fields are available for the selected in-app listing. 2. The system handles the missing structure according to configuration or policy.  3.1 No required documents are configured. 1. The system determines that no document requirements are stored for the selected listing. 2. The system continues with the available in-app details according to policy.  1.1 The scholarship is not an in-app listing. 1. The system does not continue this use case. 2. The system returns control to the calling use case so the correct listing-type flow can continue.  4.1 A temporary technical issue occurs while loading the in-app structure. 1. The system cannot prepare the application form structure normally. 2. The system returns an error result or incomplete-data result to the calling use case. |

## Use Case 76 – (UC-76) Load External Application URL [External Listing]


| Field | Description |
|---|---|
| Use Case 76 – (UC-76) | Load External Application URL [External Listing] |
| Related Requirements | FR-040, FR-043 |
| Initiating Actor | System |
| Actor’s Goal | To load the external application URL when the selected scholarship is an external listing. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The system has retrieved the scholarship attributes successfully. 3. The selected scholarship is identified as an external listing. 4. The scholarship has an external application URL configured. |
| Postconditions | 1. The external application URL is loaded successfully for the scholarship details view. 2. The Student can review or use the external application path from the scholarship details page. |
| Flow of Events for Main Success Scenario | 1. The system determines that the selected scholarship is an external listing. 2. The system retrieves the configured external application URL for the scholarship. 3. The system prepares the external application reference for display. 4. The system returns the external URL information to the View Scholarship Details use case. 5. The scholarship details page displays the external application path to the Student. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The external application URL is missing or not configured correctly. 1. The system cannot provide a valid external application path. 2. The system handles the missing URL according to policy.  1.1 The scholarship is not an external listing. 1. The system does not continue this use case. 2. The system returns control to the calling use case so the correct listing-type flow can continue.  3.1 A temporary technical issue occurs while loading the external URL information. 1. The system cannot prepare the external application reference normally. 2. The system returns an error result or incomplete-data result to the calling use case. |

## Use Case 77 – (UC-77) Bookmark Scholarship


| Field | Description |
|---|---|
| Use Case 77 – (UC-77) | Bookmark Scholarship |
| Related Requirements | FR-045, FR-046 |
| Initiating Actor | Student |
| Actor’s Goal | To save a scholarship listing for later review and follow-up. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student can access scholarship listings or scholarship details. 3. The selected scholarship listing is available for bookmarking. |
| Postconditions | 1. The selected scholarship is saved to the Student’s bookmarks. 2. The system can use the bookmark for later reminders or related notifications. 3. The Student can access the saved scholarship from bookmarked items or equivalent saved-list views. |
| Flow of Events for Main Success Scenario | 1. The Student opens a scholarship listing or scholarship details page. 2. The Student selects the bookmark action. 3. The system receives the bookmark request. 4. The system verifies that the scholarship can be bookmarked for the current Student. 5. The system saves the scholarship in the Student’s bookmarked items. 6. The system confirms that the bookmark operation is successful. 7. The Student sees that the scholarship has been bookmarked successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The scholarship is already bookmarked. 1. The system detects that the scholarship is already in the Student’s saved list. 2. The system does not create a duplicate bookmark. 3. The system informs the Student that the scholarship is already bookmarked.  4.2 The scholarship is no longer available for bookmarking. 1. The system determines that the scholarship cannot be bookmarked in its current state. 2. The system displays an unavailable-action message according to policy.  5.1 A temporary technical issue occurs while saving the bookmark. 1. The system cannot complete the bookmark operation. 2. The system displays an error message and asks the Student to try again later. |

## Use Case 78 – (UC-78) Receive Reminders / Notifications


| Field | Description |
|---|---|
| Use Case 78 – (UC-78) | Receive Reminders / Notifications |
| Related Requirements | FR-046, FR-140, FR-181 |
| Initiating Actor | System |
| Actor’s Goal | To notify the Student about bookmarked scholarships or related important scholarship events such as deadlines or follow-up reminders. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated and has relevant bookmarks or monitored scholarship records. 2. The system has a valid event, trigger, or schedule for sending the reminder or notification. 3. Notification delivery channels are available according to user settings and system configuration. |
| Postconditions | 1. The Student receives a reminder or notification related to a bookmarked scholarship or scholarship event if delivery succeeds. 2. The system records or completes the reminder process according to policy. |
| Flow of Events for Main Success Scenario | 1. The Student has previously bookmarked a scholarship or has a relevant scholarship-related event in the system. 2. The system detects that a configured reminder or notification trigger has been reached. 3. The system prepares the scholarship reminder or notification content. 4. The system sends the reminder or notification through the configured channel. 5. The Student receives the reminder or notification. 6. The system completes the notification process successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No valid trigger exists for the current scholarship. 1. The system determines that no reminder or notification should be sent at this time. 2. The flow ends without delivery.  4.1 Notification delivery fails. 1. The system cannot deliver the reminder through the intended channel. 2. The system handles the failure according to notification policy.  3.1 The scholarship is no longer relevant for reminder delivery. 1. The system determines that the scholarship is expired, unavailable, or otherwise not eligible for the reminder. 2. The system cancels or suppresses the reminder according to policy. |

## Use Case 79 – (UC-79) Create In-App Scholarship


| Field | Description |
|---|---|
| Use Case 79 – (UC-79) | Create In-App Scholarship |
| Related Requirements | FR-038, FR-040, FR-041, FR-042, FR-043 |
| Initiating Actor | Company |
| Actor’s Goal | To create an in-app scholarship listing so that Students can view and apply directly through the platform. |
| Participating Actors | None |
| Preconditions | 1. The Company is authenticated. 2. The Company account is active and permitted to manage scholarship listings. 3. The in-app scholarship creation feature is accessible. |
| Postconditions | 1. A new in-app scholarship listing is created successfully if the submitted data is valid. 2. The scholarship includes the required core details. 3. The application form and required documents can be associated with the listing. 4. The new scholarship becomes available according to system publication rules and status. |
| Flow of Events for Main Success Scenario | 1. The Company opens the scholarship creation feature. 2. The system displays the in-app scholarship creation form. 3. The Company enters the required scholarship information. 4. The Company defines the application form structure and required documents. 5. The Company submits the in-app scholarship listing. 6. The system validates the submitted scholarship data. 7. The system validates the applicable deadline rule. 8. The system creates the new in-app scholarship listing. 9. The system stores the application form structure and required document requirements. 10. The system confirms that the scholarship listing has been created successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 Required scholarship data is missing or invalid. 1. The system does not create the scholarship listing. 2. The system displays the related validation errors.  7.1 The deadline violates the configured rule. 1. The system rejects the submitted listing. 2. The system displays the deadline validation message.  4.1 The application form or required documents are incomplete or invalid. 1. The system rejects the invalid configuration. 2. The Company corrects the form or document settings and resubmits.  8.1 A technical issue occurs while creating the listing. 1. The system cannot create the scholarship successfully. 2. The system displays an error message and asks the Company to try again later. |

## Use Case 80 – (UC-80) Define Application Form & Docs


| Field | Description |
|---|---|
| Use Case 80 – (UC-80) | Define Application Form & Docs |
| Related Requirements | FR-041, FR-042 |
| Initiating Actor | Company |
| Actor’s Goal | To define the in-app application form fields and required documents for a scholarship listing so that Students can submit complete applications. |
| Participating Actors | None |
| Preconditions | 1. The Company is authenticated. 2. The Company is creating or updating an in-app scholarship listing. 3. The scholarship listing supports application form configuration and document requirements. |
| Postconditions | 1. The application form structure is defined for the in-app scholarship. 2. The required documents are configured for the listing. 3. The configured form and document requirements are available for later application use. |
| Flow of Events for Main Success Scenario | 1. The Company enters the in-app scholarship creation or editing flow. 2. The system displays the form-configuration and document-requirement section. 3. The Company defines the application form fields. 4. The Company defines the required document list. 5. The Company saves or submits the form-and-document configuration. 6. The system validates the submitted structure. 7. The system stores the configured application form fields and required documents. 8. The system returns control to the scholarship creation or update flow. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The submitted form structure is incomplete or invalid. 1. The system does not accept the invalid form configuration. 2. The system displays the related validation errors.  4.1 The required document setup is incomplete or invalid. 1. The system does not accept the invalid document configuration. 2. The system displays the related validation errors.  7.1 A temporary technical issue occurs while saving the configuration. 1. The system cannot store the form-and-document setup successfully. 2. The system returns an error message or failure result. |

## Use Case 81 – (UC-81) Manage / Edit / Archive Listings


| Field | Description |
|---|---|
| Use Case 81 – (UC-81) | Manage / Edit / Archive Listings |
| Related Requirements | FR-038, FR-040, FR-043 |
| Initiating Actor | Company |
| Actor’s Goal | To manage existing scholarship listings by editing, updating, or archiving them so that listing information remains accurate and current. |
| Participating Actors | None |
| Preconditions | 1. The Company is authenticated. 2. The Company account has access to its scholarship listing management area. 3. One or more company-owned scholarship listings exist or are available to manage. |
| Postconditions | 1. The selected scholarship listing is updated or archived successfully if the action is valid. 2. The system reflects the latest listing state in relevant views. 3. Archived listings are no longer treated as actively managed opportunities according to policy. |
| Flow of Events for Main Success Scenario | 1. The Company opens the scholarship listing management area. 2. The system displays the company-owned scholarship listings. 3. The Company selects a listing to manage. 4. The Company chooses to edit, update, or archive the selected listing. 5. The system displays the relevant management form or confirmation flow. 6. The Company submits the requested changes. 7. The system validates the requested action and submitted data. 8. The system saves the updates or archives the listing. 9. The system refreshes the displayed scholarship management results. 10. The Company views the updated listing state successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 7.1 The submitted updates are missing or invalid. 1. The system does not save the listing changes. 2. The system displays the related validation errors.  4.1 The selected listing cannot be archived or edited in its current state. 1. The system blocks the requested action according to policy. 2. The system informs the Company that the action is not allowed.  8.1 A technical issue occurs while saving the listing changes. 1. The system cannot complete the update or archive action. 2. The system displays an error message and asks the Company to try again later. |

## Use Case 82 – (UC-82) Create External-URL Listing


| Field | Description |
|---|---|
| Use Case 82 – (UC-82) | Create External-URL Listing |
| Related Requirements | FR-039, FR-040, FR-043 |
| Initiating Actor | Admin |
| Actor’s Goal | To create an external scholarship listing that points Students to an external application destination outside the platform. |
| Participating Actors | None |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has access to scholarship administration features. 3. The external-URL listing creation feature is accessible. |
| Postconditions | 1. A new external-URL scholarship listing is created successfully if the data is valid. 2. The listing contains the required core scholarship information and the external application path. 3. The listing becomes available according to system publication rules and status. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the scholarship creation feature for external listings. 2. The system displays the external-URL listing form. 3. The Admin enters the required scholarship information and the external application URL. 4. The Admin submits the external listing. 5. The system validates the submitted scholarship data. 6. The system validates the applicable deadline rule. 7. The system creates the external-URL scholarship listing. 8. The system confirms that the scholarship listing has been created successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 Required scholarship data is missing or invalid. 1. The system does not create the external listing. 2. The system displays the related validation errors.  6.1 The deadline violates the configured rule. 1. The system rejects the submitted listing. 2. The system displays the deadline validation message.  3.1 The external application URL is missing or invalid. 1. The system does not accept the submitted external path. 2. The system displays the related validation message.  7.1 A technical issue occurs while creating the listing. 1. The system cannot create the scholarship successfully. 2. The system displays an error message and asks the Admin to try again later. |

## Use Case 83 – (UC-83) Validate Deadline Rule


| Field | Description |
|---|---|
| Use Case 83 – (UC-83) | Validate Deadline Rule |
| Related Requirements | FR-043 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the scholarship deadline satisfies the configured minimum lead-time and deadline validity rules before the listing is accepted. |
| Participating Actors | Company, Admin |
| Preconditions | 1. A Company or Admin is creating or updating a scholarship listing. 2. A scholarship deadline value has been submitted. 3. The deadline-validation rules are available to the system. |
| Postconditions | 1. The system determines whether the submitted deadline is valid. 2. If the deadline is valid, the scholarship creation or update flow continues. 3. If the deadline is invalid, the system rejects the submission and returns a deadline-validation error. |
| Flow of Events for Main Success Scenario | 1. The user submits a scholarship listing with a deadline value. 2. The system receives the submitted deadline. 3. The system checks the deadline against the configured rule set. 4. The system determines that the deadline satisfies the minimum lead-time and other applicable validity conditions. 5. The system marks the deadline validation step as successful. 6. The system returns control to the scholarship creation or update flow. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The deadline is too close or violates the configured lead-time rule. 1. The system marks the validation as failed. 2. The system returns a deadline-rule violation result to the calling use case.  3.1 The deadline value is missing or malformed. 1. The system cannot validate the deadline correctly. 2. The system returns an invalid-input result to the calling use case.  5.1 A temporary technical issue occurs during deadline validation. 1. The system cannot complete the deadline validation process. 2. The system returns an error result to the calling use case. |

## Use Case 84 – (UC-84) Feature Scholarship


| Field | Description |
|---|---|
| Use Case 84 – (UC-84) | Feature Scholarship |
| Related Requirements | FR-030, FR-186 |
| Initiating Actor | Admin |
| Actor’s Goal | To feature selected scholarship listings so that important opportunities gain higher visibility in the platform. |
| Participating Actors | None |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has access to scholarship management or featured-content controls. 3. One or more scholarship listings are available to feature. |
| Postconditions | 1. The selected scholarship is marked as featured successfully. 2. The platform reflects the featured status in relevant scholarship views according to system design. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the scholarship management or feature-control area. 2. The system displays the available scholarship listings. 3. The Admin selects a scholarship to feature. 4. The Admin confirms the featuring action. 5. The system validates that the selected listing can be featured. 6. The system updates the scholarship’s featured status. 7. The system refreshes the relevant scholarship-management results. 8. The Admin sees that the scholarship is now featured successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The selected scholarship cannot be featured in its current state. 1. The system blocks the action according to policy. 2. The system informs the Admin that the listing cannot be featured.  6.1 A technical issue occurs while updating the featured status. 1. The system cannot complete the feature action. 2. The system displays an error message and asks the Admin to try again later.  3.1 The selected scholarship is already featured. 1. The system detects that the listing is already marked as featured. 2. The system may keep the current state unchanged and inform the Admin accordingly. |

## Use Case 85 – (UC-85) Manually Update External Status


| Field | Description |
|---|---|
| Use Case 85 – (UC-85) | Manually Update External Status |
| Related Requirements | FR-053, FR-055, FR-056 |
| Initiating Actor | Student |
| Actor’s Goal | To manually update the status of an external scholarship application record so that progress outside the platform can still be tracked inside Scholar-Path. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has an external application record in the system. 3. The external application record is accessible for update. |
| Postconditions | 1. The external application record is updated with the new status successfully. 2. The latest external status becomes visible in the Student’s tracking view. 3. The Student may also add notes if needed. |
| Flow of Events for Main Success Scenario | 1. The Student opens an existing external application record. 2. The system displays the current external application details and status. 3. The Student selects the status-update action. 4. The Student chooses or enters the updated external application status. 5. The Student submits the status update. 6. The system validates the submitted status value according to the allowed tracking logic. 7. The system saves the updated external status. 8. The system refreshes the external application record. 9. The Student views the updated status successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The submitted status value is missing or invalid. 1. The system does not save the update. 2. The system displays the related validation message.  7.1 A temporary technical issue occurs while saving the external status. 1. The system cannot complete the update. 2. The system displays an error message and asks the Student to try again later.  3.1 The Student chooses to add notes together with the update. 1. The system allows the Student to continue with the related note-update flow. |

## Use Case 86 – (UC-86) Add Notes to External Record


| Field | Description |
|---|---|
| Use Case 86 – (UC-86) | Add Notes to External Record |
| Related Requirements | FR-056 |
| Initiating Actor | Student |
| Actor’s Goal | To add personal notes to an external application record so that progress, reminders, and observations can be stored for later reference. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has opened an existing external application record. 3. The external record is available for note updates. |
| Postconditions | 1. The submitted note is saved successfully to the external record. 2. The Student can view the saved note later within the same application record. |
| Flow of Events for Main Success Scenario | 1. The Student opens an external application record. 2. The system displays the current record details. 3. The Student chooses to add or edit a note. 4. The Student enters the desired note content. 5. The Student submits the note update. 6. The system validates the submitted note data according to system rules. 7. The system saves the note in the external application record. 8. The system refreshes the record view. 9. The Student sees the note saved successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The note content is invalid or exceeds allowed limits. 1. The system rejects the note submission. 2. The system displays the related validation message.  7.1 A temporary technical issue occurs while saving the note. 1. The system cannot complete the note update. 2. The system displays an error message and asks the Student to try again later. |

## Use Case 87 – (UC-87) Submit In-App Application


| Field | Description |
|---|---|
| Use Case 87 – (UC-87) | Submit In-App Application |
| Related Requirements | FR-047, FR-048, FR-049, FR-050, FR-057 |
| Initiating Actor | Student |
| Actor’s Goal | To submit an application directly to an in-app scholarship listing through Scholar-Path. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The selected scholarship is an in-app listing. 3. The scholarship is open and available for application. 4. The Student has access to the application form. |
| Postconditions | 1. The in-app application is submitted successfully if all checks pass. 2. The application is recorded with the appropriate initial status. 3. The application becomes available for Company review. 4. The single-active-application rule remains enforced. |
| Flow of Events for Main Success Scenario | 1. The Student opens the in-app scholarship application form. 2. The system displays the configured application fields and required document requirements. 3. The Student enters the required application information. 4. The Student attaches the required documents from the document vault. 5. The Student submits the application. 6. The system validates the submitted application data. 7. The system enforces the single active application rule for the Student and scholarship. 8. The system creates the in-app application record. 9. The system assigns the initial application status. 10. The system confirms that the application has been submitted successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The submitted application data is missing or invalid. 1. The system does not create the application. 2. The system displays the related validation errors.  4.1 Required documents are missing or invalid. 1. The system does not allow final submission. 2. The system informs the Student about the missing or invalid document requirements.  7.1 The single active application rule blocks submission. 1. The system does not create the application. 2. The system informs the Student that another active application already exists for the same scholarship.  5.1 The Student chooses to save the application instead of final submission. 1. The system continues through the draft-saving flow.  8.1 A temporary technical issue occurs while creating the application. 1. The system cannot complete the submission. 2. The system displays an error message and asks the Student to try again later. |

## Use Case 88 – (UC-88) Attach Documents from Vault


| Field | Description |
|---|---|
| Use Case 88 – (UC-88) | Attach Documents from Vault |
| Related Requirements | FR-049 |
| Initiating Actor | Student |
| Actor’s Goal | To attach required application documents from the document vault to an in-app scholarship application. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student is filling or preparing an in-app application. 3. The Student has one or more documents available in the document vault. 4. The application supports document attachment. |
| Postconditions | 1. The selected documents are attached to the application successfully. 2. The system associates the chosen vault documents with the current application submission or draft. |
| Flow of Events for Main Success Scenario | 1. The Student opens the application form. 2. The Student reaches the document-attachment step. 3. The system displays the available document-vault items. 4. The Student selects one or more documents to attach. 5. The Student confirms the attachment action. 6. The system validates that the selected documents meet the application requirements. 7. The system associates the documents with the current application. 8. The system confirms that the documents have been attached successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 One or more required documents are missing. 1. The system informs the Student that the document requirements are incomplete. 2. The Student must select the missing required items before final submission if required by policy.  6.2 One or more selected documents are invalid or not eligible for attachment. 1. The system rejects the invalid attachment selection. 2. The system displays the related validation message.  7.1 A temporary technical issue occurs while attaching the documents. 1. The system cannot complete the document-attachment step. 2. The system displays an error message and asks the Student to try again later. |

## Use Case 89 – (UC-89) Save Application as Draft


| Field | Description |
|---|---|
| Use Case 89 – (UC-89) | Save Application as Draft |
| Related Requirements | FR-048 |
| Initiating Actor | Student |
| Actor’s Goal | To save an incomplete in-app application as a draft so that it can be completed and submitted later before the deadline. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student is working on an in-app scholarship application. 3. The application form is accessible for draft saving. |
| Postconditions | 1. The current application data is saved as a draft successfully. 2. The Student can return later to continue editing the draft before submission. |
| Flow of Events for Main Success Scenario | 1. The Student opens an in-app scholarship application form. 2. The Student enters some or all of the application data. 3. The Student selects the save-as-draft action. 4. The system receives the current application content. 5. The system saves the application in draft status. 6. The system confirms that the draft was saved successfully. 7. The Student can later reopen the draft to continue editing. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 A temporary technical issue occurs while saving the draft. 1. The system cannot save the draft successfully. 2. The system displays an error message and asks the Student to try again later.  2.1 The draft contains incomplete required fields. 1. The system still allows saving as draft if the partial-draft policy permits it. 2. Final submission remains unavailable until required data is completed. |

## Use Case 90 – (UC-90) Enforce Single Active Application Rule


| Field | Description |
|---|---|
| Use Case 90 – (UC-90) | Enforce Single Active Application Rule |
| Related Requirements | FR-057, FR-059 |
| Initiating Actor | Automated System |
| Actor’s Goal | To prevent the Student from having more than one active application for the same scholarship at the same time. |
| Participating Actors | Student |
| Preconditions | 1. The Student is attempting to submit an application or create a new active record for a scholarship. 2. The system can access existing application records for the same Student and scholarship. |
| Postconditions | 1. The system either allows the new application to proceed or blocks it according to the single-active-application rule. 2. Duplicate active applications are prevented. |
| Flow of Events for Main Success Scenario | 1. The Student initiates submission of an application for a scholarship. 2. The system checks existing applications for the same Student and scholarship. 3. The system determines whether another active application already exists. 4. If no conflicting active application exists, the system allows the submission flow to continue. 5. If a conflicting active application exists, the system blocks the new submission. 6. The system returns the result to the calling use case. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 A previous application was withdrawn and reapplication is allowed. 1. The system determines that no active conflicting application currently exists. 2. The system allows the new submission to proceed if the scholarship remains open.  2.1 A temporary technical issue occurs while checking existing application records. 1. The system cannot safely determine whether an active conflict exists. 2. The system returns an error result and does not continue submission automatically. |

## Use Case 91 – (UC-91) Track Application Timeline & Status


| Field | Description |
|---|---|
| Use Case 91 – (UC-91) | Track Application Timeline & Status |
| Related Requirements | FR-050, FR-051, FR-061 |
| Initiating Actor | Student |
| Actor’s Goal | To view the current application status, timeline, and related progress information so that the Student can understand the application outcome and next steps. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has one or more application records in the system. 3. The application-tracking page is accessible. |
| Postconditions | 1. The application status, timeline, and history information are displayed successfully. 2. The Student can review the current state of the application and any related progress data. |
| Flow of Events for Main Success Scenario | 1. The Student opens the application tracking area. 2. The system displays the Student’s available applications. 3. The Student selects an application record to review. 4. The system retrieves the current status and related status-history information. 5. The system displays the application timeline, current status, and related progress details. 6. The Student reviews the application history and status successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The selected application record cannot be retrieved. 1. The system cannot display the requested tracking information normally. 2. The system displays an error or unavailable message.  5.1 Timeline information is incomplete or partially unavailable. 1. The system displays the available status information according to policy. 2. The Student still sees the currently retrievable application data.  1.1 The Student has no application records yet. 1. The system displays an empty-state message or equivalent result. |

## Use Case 92 – (UC-92) Withdraw Application


| Field | Description |
|---|---|
| Use Case 92 – (UC-92) | Withdraw Application |
| Related Requirements | FR-058, FR-059 |
| Initiating Actor | Student |
| Actor’s Goal | To withdraw an existing application so that the Student can stop the application process for that scholarship. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has an application record that is currently eligible for withdrawal. 3. The application is accessible in the Student’s application management area. |
| Postconditions | 1. The selected application is marked as withdrawn successfully. 2. The application no longer remains in an active competing state. 3. Reapplication may become possible later if the scholarship remains open and no active application exists. |
| Flow of Events for Main Success Scenario | 1. The Student opens the application management or tracking area. 2. The Student selects an application eligible for withdrawal. 3. The system displays the application details and the withdrawal action. 4. The Student confirms the withdrawal request. 5. The system validates that the application is still eligible for withdrawal. 6. The system updates the application status to withdrawn. 7. The system records the updated status according to the application lifecycle logic. 8. The system confirms that the application has been withdrawn successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The application is no longer eligible for withdrawal. 1. The system blocks the withdrawal action. 2. The system informs the Student that withdrawal is not allowed in the current state.  6.1 A temporary technical issue occurs while updating the application. 1. The system cannot complete the withdrawal. 2. The system displays an error message and asks the Student to try again later. |

## Use Case 93 – (UC-93) Initiate External Application


| Field | Description |
|---|---|
| Use Case 93 – (UC-93) | Initiate External Application |
| Related Requirements | FR-053, FR-054 |
| Initiating Actor | Student |
| Actor’s Goal | To start an application for an external scholarship listing and create a self-tracked application record inside the platform. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The selected scholarship is an external-URL listing. 3. The scholarship details and external application path are available. |
| Postconditions | 1. A self-tracked external application record is created for the Student. 2. The Student is redirected to the external application URL. 3. The Student can later track the external application manually inside the platform. |
| Flow of Events for Main Success Scenario | 1. The Student opens an external scholarship listing. 2. The Student selects the action to apply externally. 3. The system creates a self-tracked external application record for the Student. 4. The system retrieves the configured external application URL. 5. The system redirects the Student to the external application destination. 6. The Student continues the application process outside the platform. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The external application URL is missing or invalid. 1. The system cannot provide a valid redirect destination. 2. The system displays an error message or unavailable-action result.  3.1 A temporary technical issue occurs while creating the self-tracked record. 1. The system cannot create the external application record successfully. 2. The system does not continue the normal initiation flow.  5.1 The redirect cannot be completed successfully. 1. The system cannot send the Student to the external destination normally. 2. The system displays an error or fallback message according to policy. |

## Use Case 94 – (UC-94) Review Submitted Applications


| Field | Description |
|---|---|
| Use Case 94 – (UC-94) | Review Submitted Applications |
| Related Requirements | FR-052, FR-063, FR-064, FR-065 |
| Initiating Actor | Company |
| Actor’s Goal | To review submitted student applications for the Company’s own scholarship listings. |
| Participating Actors | None |
| Preconditions | 1. The Company is authenticated. 2. The Company has access to scholarship application review features. 3. The Company owns one or more scholarship listings with submitted applications. |
| Postconditions | 1. The Company can view the submitted applications and their relevant materials. 2. The Company can proceed to status review or decision actions for eligible applications. |
| Flow of Events for Main Success Scenario | 1. The Company opens the submitted-application review area. 2. The system displays the applications associated with the Company’s own scholarship listings. 3. The Company selects an application to review. 4. The system displays the submitted application data and related materials. 5. The Company reviews the application content successfully. 6. The Company may proceed to status updates or related review decisions. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No submitted applications are available. 1. The system displays an empty-state message or equivalent result.  2.2 The Company attempts to access an application outside its ownership scope. 1. The system blocks the unauthorized access. 2. The system displays an access-denied or unavailable result.  4.1 The selected application data cannot be retrieved. 1. The system cannot display the application normally. 2. The system displays an error or temporary unavailable message. |

## Use Case 95 – (UC-95) Update Application Status


| Field | Description |
|---|---|
| Use Case 95 – (UC-95) | Update Application Status |
| Related Requirements | FR-050, FR-051, FR-052, FR-060 |
| Initiating Actor | Company |
| Actor’s Goal | To update the status of a submitted application according to the configured review workflow. |
| Participating Actors | None |
| Preconditions | 1. The Company is authenticated. 2. The Company has access to an application belonging to its own scholarship listing. 3. The application is eligible for status update in the current workflow stage. |
| Postconditions | 1. The application status is updated successfully if the requested transition is valid. 2. The system records the status change in the status history log. 3. If the new status is a final outcome, the application may become read-only. |
| Flow of Events for Main Success Scenario | 1. The Company opens a submitted application record. 2. The system displays the current application status and available workflow actions. 3. The Company selects a new status or decision outcome. 4. The Company submits the requested status update. 5. The system validates the requested status transition. 6. The system updates the application status. 7. The system maintains the status history log. 8. If the new status is Accepted or Rejected, the system locks the application as read-only according to policy. 9. The system confirms that the status update was completed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The requested status transition is invalid. 1. The system rejects the status update. 2. The system informs the Company that the requested transition is not allowed.  6.1 A temporary technical issue occurs while updating the application status. 1. The system cannot complete the status update. 2. The system displays an error message and asks the Company to try again later.  8.1 The final-outcome locking step cannot be completed successfully. 1. The system handles the issue according to policy for final-status consistency. 2. The system may treat the overall update as incomplete or failed according to policy. |

## Use Case 96 – (UC-96) Maintain Status History Log


| Field | Description |
|---|---|
| Use Case 96 – (UC-96) | Maintain Status History Log |
| Related Requirements | FR-051 |
| Initiating Actor | Automated System |
| Actor’s Goal | To record each application status change in a status-history trail for auditability and timeline tracking. |
| Participating Actors | Company, Student |
| Preconditions | 1. An application status change has been requested or completed. 2. The system has access to the application record and new status information. |
| Postconditions | 1. The application status-history log is updated with the new change successfully. 2. The new history entry becomes available for timeline and audit views. |
| Flow of Events for Main Success Scenario | 1. The system detects a valid application status change. 2. The system prepares the status-history entry details. 3. The system records the previous status, new status, and relevant change metadata according to design. 4. The system stores the new history entry. 5. The system confirms that the status-history log has been updated successfully. 6. The system returns control to the calling status-update flow. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 A temporary technical issue occurs while saving the history log. 1. The system cannot complete the history update. 2. The system returns an error or failure result to the calling use case.  2.1 The status change details are incomplete. 1. The system cannot create a valid history entry. 2. The system handles the issue according to consistency policy. |

## Use Case 97 – (UC-97) Lock Final Outcomes - Read Only


| Field | Description |
|---|---|
| Use Case 97 – (UC-97) | Lock Final Outcomes - Read Only |
| Related Requirements | FR-060 |
| Initiating Actor | Automated System |
| Actor’s Goal | To lock applications with final outcomes so that Accepted and Rejected records cannot be modified further. |
| Participating Actors | Company, Student |
| Preconditions | 1. An application record exists. 2. The application status has been updated to a final outcome. 3. The final-outcome locking logic is available to the system. |
| Postconditions | 1. The application record is set to read-only according to final-outcome policy. 2. Further modification actions are blocked for the locked final record. |
| Flow of Events for Main Success Scenario | 1. The system detects that an application status has reached a final outcome. 2. The system identifies that the final status is Accepted or Rejected. 3. The system applies the read-only lock to the application record. 4. The system updates the application state accordingly. 5. The system confirms that the final-outcome lock has been applied successfully. 6. The application remains viewable but no longer editable. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The application status is not a final outcome. 1. The system does not apply the final read-only lock. 2. The normal editable workflow continues if allowed.  3.1 A temporary technical issue occurs while applying the lock. 1. The system cannot complete the lock operation successfully. 2. The system handles the issue according to final-state consistency policy. |

## Use Case 98 – (UC-98) Manually Track External Status


| Field | Description |
|---|---|
| Use Case 98 – (UC-98) | Manually Track External Status |
| Related Requirements | FR-053, FR-055, FR-056 |
| Initiating Actor | Student |
| Actor’s Goal | To manually record and update the progress of an external scholarship application inside the platform. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has an external application record in the system. 3. The external record is accessible for tracking updates. |
| Postconditions | 1. The external application status is updated successfully. 2. The updated status becomes visible in the Student’s tracking view. 3. The Student may continue monitoring the external application inside the platform. |
| Flow of Events for Main Success Scenario | 1. The Student opens an existing external application record. 2. The system displays the current external application details and status. 3. The Student selects the manual tracking or status update action. 4. The Student enters or selects the updated status. 5. The Student submits the external status update. 6. The system validates the submitted status value. 7. The system saves the updated external status. 8. The system refreshes the external application record. 9. The Student sees the updated status successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The submitted status value is missing or invalid. 1. The system does not save the update. 2. The system displays the related validation message.  7.1 A temporary technical issue occurs while saving the status. 1. The system cannot complete the update. 2. The system displays an error message and asks the Student to try again later.  3.1 The Student chooses to add notes during tracking. 1. The system allows the Student to continue with the related note-entry flow. |

## Use Case 99 – (UC-99) Add Notes to External Record


| Field | Description |
|---|---|
| Use Case 99 – (UC-99) | Add Notes to External Record |
| Related Requirements | FR-056 |
| Initiating Actor | Student |
| Actor’s Goal | To add personal notes to an external application record for follow-up, reminders, and context. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has opened an existing external application record. 3. The record is available for note updates. |
| Postconditions | 1. The note is saved successfully to the external record. 2. The note becomes available for later review in the same record. |
| Flow of Events for Main Success Scenario | 1. The Student opens an existing external application record. 2. The system displays the current record details. 3. The Student selects the add-note action. 4. The Student enters the note content. 5. The Student submits the note. 6. The system validates the note data according to system rules. 7. The system saves the note to the external record. 8. The system refreshes the record view. 9. The Student sees the note saved successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The note content is invalid or exceeds allowed limits. 1. The system rejects the note submission. 2. The system displays the related validation message.  7.1 A temporary technical issue occurs while saving the note. 1. The system cannot complete the note update. 2. The system displays an error message and asks the Student to try again later. |

## Use Case 100 – (UC-100) Start In-App Application


| Field | Description |
|---|---|
| Use Case 100 – (UC-100) | Start In-App Application |
| Related Requirements | FR-047, FR-048, FR-049 |
| Initiating Actor | Student |
| Actor’s Goal | To begin filling an in-app scholarship application within the platform. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The selected scholarship is an in-app listing. 3. The scholarship is open and available for application. 4. The Student has access to the application entry point. |
| Postconditions | 1. The in-app application form is opened successfully. 2. The Student can begin entering application data. 3. The current application session becomes available for draft saving or later submission according to policy. |
| Flow of Events for Main Success Scenario | 1. The Student opens an in-app scholarship listing. 2. The Student selects the action to start the application. 3. The system checks that the scholarship supports in-app application. 4. The system loads the configured application form and document requirements. 5. The system opens the application form for the Student. 6. The Student begins entering the required application information. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The selected scholarship is not available for in-app application. 1. The system blocks the start action. 2. The system displays an unavailable-action message.  4.1 The application form structure cannot be loaded. 1. The system cannot prepare the application form normally. 2. The system displays an error or temporary unavailable message.  6.1 The Student chooses to stop before submission. 1. The system keeps the current progress only if draft behavior or session behavior allows it. |

## Use Case 101 – (UC-101) Submit Application


| Field | Description |
|---|---|
| Use Case 101 – (UC-101) | Submit Application |
| Related Requirements | FR-047, FR-048, FR-049, FR-050, FR-057 |
| Initiating Actor | Student |
| Actor’s Goal | To submit an in-app scholarship application successfully to the scholarship provider through the platform. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has started an in-app application. 3. The scholarship is still open for application. 4. The Student has access to the submission action. |
| Postconditions | 1. The application is submitted successfully if all rules pass. 2. The system records the application with an initial status. 3. The application becomes available for review by the Company. 4. The application becomes available for later timeline and status tracking. |
| Flow of Events for Main Success Scenario | 1. The Student completes the in-app application form. 2. The Student uploads or attaches the required documents. 3. The Student selects the final submit action. 4. The system validates the submitted application data. 5. The system validates the attached document requirements. 6. The system prevents duplicate active applications according to the configured rule. 7. The system creates the application record. 8. The system assigns the initial application status. 9. The system confirms successful submission to the Student. 10. The system makes the application available for tracking and Company review. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The submitted application data is missing or invalid. 1. The system does not create the application. 2. The system displays the related validation errors.  5.1 Required documents are missing or invalid. 1. The system does not allow final submission. 2. The system informs the Student about the missing or invalid requirements.  6.1 A duplicate active application already exists. 1. The system blocks the submission. 2. The system informs the Student that another active application already exists for the same scholarship.  3.1 The Student chooses to save as draft instead of final submission. 1. The system continues through the draft-saving flow.  7.1 A temporary technical issue occurs while creating the application. 1. The system cannot complete the submission. 2. The system displays an error message and asks the Student to try again later. |

## Use Case 102 – (UC-102) Upload Documents


| Field | Description |
|---|---|
| Use Case 102 – (UC-102) | Upload Documents |
| Related Requirements | FR-049 |
| Initiating Actor | Student |
| Actor’s Goal | To provide the required documents for an in-app scholarship application. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student is working on an in-app application. 3. The application requires one or more supporting documents. |
| Postconditions | 1. The required documents are uploaded or attached successfully if valid. 2. The documents become associated with the current application or draft. |
| Flow of Events for Main Success Scenario | 1. The Student reaches the document-upload step in the application flow. 2. The system displays the required document list and upload controls. 3. The Student selects or uploads the needed documents. 4. The Student confirms the upload or attachment action. 5. The system validates the submitted documents according to the configured requirements. 6. The system stores or associates the documents with the current application. 7. The system confirms that the document step was completed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 One or more required documents are missing. 1. The system informs the Student that the requirements are incomplete. 2. Final submission remains blocked if policy requires all documents.  5.2 One or more uploaded documents are invalid. 1. The system rejects the invalid upload or attachment. 2. The system displays the related validation message.  6.1 A temporary technical issue occurs while storing the documents. 1. The system cannot complete the document step. 2. The system displays an error message and asks the Student to try again later. |

## Use Case 103 – (UC-103) Save as Draft


| Field | Description |
|---|---|
| Use Case 103 – (UC-103) | Save as Draft |
| Related Requirements | FR-048 |
| Initiating Actor | Student |
| Actor’s Goal | To save an incomplete in-app application and continue it later before final submission. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has started an in-app application. 3. The draft-save option is available in the current application flow. |
| Postconditions | 1. The current application content is saved in draft status successfully. 2. The Student can reopen the draft later and continue editing. |
| Flow of Events for Main Success Scenario | 1. The Student enters some or all of the in-app application data. 2. The Student selects the save-as-draft action. 3. The system receives the current application content. 4. The system saves the application in draft status. 5. The system confirms that the draft was saved successfully. 6. The Student can later reopen the draft to continue editing or submit it. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 A temporary technical issue occurs while saving the draft. 1. The system cannot save the draft successfully. 2. The system displays an error message and asks the Student to try again later.  1.1 The current draft content is incomplete. 1. The system still allows saving as draft if the draft policy permits it. 2. Final submission remains unavailable until required data is completed. |

## Use Case 104 – (UC-104) Track Timeline & Status


| Field | Description |
|---|---|
| Use Case 104 – (UC-104) | Track Timeline & Status |
| Related Requirements | FR-050, FR-051, FR-055, FR-061 |
| Initiating Actor | Student |
| Actor’s Goal | To view the current application progress, status history, and tracking timeline for in-app or external applications. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has one or more application records in the system. 3. The tracking page or record view is accessible. |
| Postconditions | 1. The application timeline and status information are displayed successfully. 2. The Student can review the current state and progress of the selected application. |
| Flow of Events for Main Success Scenario | 1. The Student opens the application tracking area. 2. The system displays the Student’s available application records. 3. The Student selects an application record to review. 4. The system retrieves the current status and available timeline information. 5. The system displays the timeline, current status, and related progress details. 6. The Student reviews the selected application successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The selected application record cannot be retrieved. 1. The system cannot display the requested tracking information normally. 2. The system displays an error or unavailable message.  5.1 Timeline information is incomplete or partially unavailable. 1. The system displays the available status information according to policy. 2. The Student still sees the currently retrievable data.  1.1 The Student has no application records yet. 1. The system displays an empty-state message or equivalent result. |

## Use Case 105 – (UC-105) Withdraw Application


| Field | Description |
|---|---|
| Use Case 105 – (UC-105) | Withdraw Application |
| Related Requirements | FR-058, FR-059 |
| Initiating Actor | Student |
| Actor’s Goal | To withdraw an existing application and stop participating in the current application process for that scholarship. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has an application record that is eligible for withdrawal. 3. The application is accessible from the Student’s application area. |
| Postconditions | 1. The application is marked as withdrawn successfully. 2. The application no longer remains in an active competing state. 3. Reapplication may become possible later if the scholarship remains open and no other active application exists. |
| Flow of Events for Main Success Scenario | 1. The Student opens the application management or tracking area. 2. The Student selects an application eligible for withdrawal. 3. The system displays the application details and the withdrawal action. 4. The Student confirms the withdrawal request. 5. The system validates that the application is still eligible for withdrawal. 6. The system updates the application status to withdrawn. 7. The system confirms that the application has been withdrawn successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The application is no longer eligible for withdrawal. 1. The system blocks the withdrawal action. 2. The system informs the Student that withdrawal is not allowed in the current state.  6.1 A temporary technical issue occurs while updating the application. 1. The system cannot complete the withdrawal. 2. The system displays an error message and asks the Student to try again later.  7.1 The Student later becomes eligible to reapply. 1. The system allows a future reapplication only if the scholarship remains open and no active conflicting application exists. |

## Use Case 106 – (UC-106) Reapply After Withdrawal


| Field | Description |
|---|---|
| Use Case 106 – (UC-106) | Reapply After Withdrawal |
| Related Requirements | FR-059 |
| Initiating Actor | Student |
| Actor’s Goal | To submit a new application after withdrawing a previous one, provided the scholarship is still open and no other active application exists. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student previously withdrew an application for the same scholarship. 3. The scholarship remains open. 4. No other active application exists for the same Student and scholarship. |
| Postconditions | 1. The Student is allowed to start or submit a new application if the reapplication conditions are satisfied. 2. The new application becomes the current application record for the renewed attempt. |
| Flow of Events for Main Success Scenario | 1. The Student attempts to apply again for a scholarship after a previous withdrawal. 2. The system checks the prior application state. 3. The system confirms that the previous application was withdrawn. 4. The system checks whether the scholarship is still open. 5. The system checks whether another active application currently exists. 6. The system determines that reapplication is allowed. 7. The system allows the Student to continue with a new application attempt. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The scholarship is no longer open. 1. The system does not allow reapplication. 2. The system informs the Student that the scholarship is closed.  5.1 Another active application already exists. 1. The system blocks the reapplication attempt. 2. The system informs the Student that an active application already exists.  2.1 A temporary technical issue occurs while checking reapplication eligibility. 1. The system cannot confirm whether reapplication is allowed. 2. The system asks the Student to try again later. |

## Use Case 107 – (UC-107) Initiate External Application


| Field | Description |
|---|---|
| Use Case 107 – (UC-107) | Initiate External Application |
| Related Requirements | FR-053, FR-054 |
| Initiating Actor | Student |
| Actor’s Goal | To begin an external scholarship application and create a self-tracked record inside the platform. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The selected scholarship is an external listing. 3. The scholarship details and external application path are available. |
| Postconditions | 1. A self-tracked external application record is created for the Student. 2. The Student is redirected to the external application destination. 3. The Student can later track the external application manually inside the platform. |
| Flow of Events for Main Success Scenario | 1. The Student opens an external scholarship listing. 2. The Student selects the action to apply externally. 3. The system creates a self-tracked external application record. 4. The system retrieves the configured external application URL. 5. The system redirects the Student to the external application destination. 6. The Student continues the application process outside the platform. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The external application URL is missing or invalid. 1. The system cannot provide a valid redirect destination. 2. The system displays an error or unavailable-action message.  3.1 A temporary technical issue occurs while creating the self-tracked record. 1. The system cannot create the external application record successfully. 2. The system does not continue the normal flow.  5.1 The redirect cannot be completed successfully. 1. The system cannot send the Student to the external destination normally. 2. The system displays an error or fallback message according to policy. |

## Use Case 108 – (UC-108) View Submitted Applications


| Field | Description |
|---|---|
| Use Case 108 – (UC-108) | View Submitted Applications |
| Related Requirements | FR-052, FR-063, FR-064, FR-065 |
| Initiating Actor | Company |
| Actor’s Goal | To view submitted student applications belonging to the Company’s scholarship listings. |
| Participating Actors | None |
| Preconditions | 1. The Company is authenticated. 2. The Company has access to its application review area. 3. One or more submitted applications may exist for the Company’s listings. |
| Postconditions | 1. The Company can review the list of submitted applications. 2. The Company can open a specific application for detailed review and later status action. |
| Flow of Events for Main Success Scenario | 1. The Company opens the submitted-applications area. 2. The system displays the applications associated with the Company’s own scholarship listings. 3. The Company selects an application to review. 4. The system displays the submitted application details and related materials. 5. The Company reviews the submitted application successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No submitted applications are available. 1. The system displays an empty-state message or equivalent result.  2.2 The Company attempts to access an application outside its ownership scope. 1. The system blocks the unauthorized access. 2. The system displays an access-denied or unavailable result.  4.1 The selected application data cannot be retrieved. 1. The system cannot display the application normally. 2. The system displays an error or temporary unavailable message. |

## Use Case 109 – (UC-109) Update Status


| Field | Description |
|---|---|
| Use Case 109 – (UC-109) | Update Status |
| Related Requirements | FR-050, FR-051, FR-052, FR-060 |
| Initiating Actor | Company |
| Actor’s Goal | To update the status of a submitted application according to the review workflow and decision process. |
| Participating Actors | None |
| Preconditions | 1. The Company is authenticated. 2. The Company has access to an application belonging to its own scholarship listing. 3. The selected application is eligible for status update in the current workflow stage. |
| Postconditions | 1. The application status is updated successfully if the requested transition is valid. 2. If the new status is final, the system may lock the application as read-only. 3. The updated status becomes visible in application tracking views. |
| Flow of Events for Main Success Scenario | 1. The Company opens a submitted application record. 2. The system displays the current status and available status actions. 3. The Company selects a new status or decision outcome. 4. The Company submits the requested status change. 5. The system validates the requested status transition. 6. The system updates the application status. 7. If the new status is a final decision, the system locks the final decision record according to policy. 8. The system confirms that the status update was completed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The requested status transition is invalid. 1. The system rejects the status update. 2. The system informs the Company that the requested transition is not allowed.  6.1 A temporary technical issue occurs while updating the status. 1. The system cannot complete the status update. 2. The system displays an error message and asks the Company to try again later.  7.1 The final-decision locking step cannot be completed successfully. 1. The system handles the issue according to policy for final-status consistency. 2. The system may treat the overall update as incomplete or failed according to policy. |

## Use Case 110 – (UC-110) Prevent Duplicate Applications


| Field | Description |
|---|---|
| Use Case 110 – (UC-110) | Prevent Duplicate Applications |
| Related Requirements | FR-057, FR-059 |
| Initiating Actor | Automated System |
| Actor’s Goal | To prevent creation of duplicate active applications for the same Student and scholarship. |
| Participating Actors | Student |
| Preconditions | 1. The Student is attempting to start or submit an application for a scholarship. 2. The system can access existing application records for the same Student and scholarship. |
| Postconditions | 1. The system either allows the new application flow to continue or blocks it according to the duplicate-prevention rule. 2. More than one active application for the same Student and scholarship is prevented. |
| Flow of Events for Main Success Scenario | 1. The Student attempts to start or submit an application for a scholarship. 2. The automated system checks existing application records for the same Student and scholarship. 3. The system determines whether another active application already exists. 4. If no active duplicate exists, the system allows the application flow to continue. 5. If an active duplicate exists, the system blocks the new application flow. 6. The system returns the result to the calling use case. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 A previous application was withdrawn and reapplication is allowed. 1. The system determines that no active conflicting application currently exists. 2. The system allows the new application flow to proceed if the scholarship remains open.  2.1 A temporary technical issue occurs while checking existing records. 1. The system cannot safely determine whether an active conflict exists. 2. The system returns an error result and does not continue the application flow automatically. |

## Use Case 111 – (UC-111) Lock Final Decisions


| Field | Description |
|---|---|
| Use Case 111 – (UC-111) | Lock Final Decisions |
| Related Requirements | FR-060 |
| Initiating Actor | Automated System |
| Actor’s Goal | To lock application records that reach a final decision so they become read-only. |
| Participating Actors | Company, Student |
| Preconditions | 1. An application record exists. 2. The application status has been updated to a final decision outcome. 3. The final-decision locking logic is available to the system. |
| Postconditions | 1. The application record is set to read-only according to final-outcome policy. 2. Further modification actions are blocked for the locked final record. |
| Flow of Events for Main Success Scenario | 1. The system detects that an application status has reached a final decision. 2. The system identifies that the final status is Accepted or Rejected. 3. The system applies the read-only lock to the application record. 4. The system updates the application state accordingly. 5. The system confirms that the final-decision lock has been applied successfully. 6. The application remains viewable but no longer editable. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The application status is not a final decision. 1. The system does not apply the final read-only lock. 2. The normal editable workflow continues if allowed.  3.1 A temporary technical issue occurs while applying the lock. 1. The system cannot complete the lock operation successfully. 2. The system handles the issue according to final-state consistency policy. |

## Use Case 112 – (UC-112) Track and Manage Application


| Field | Description |
|---|---|
| Use Case 112 – (UC-112) | Track and Manage Application |
| Related Requirements | FR-050, FR-051, FR-058, FR-059, FR-060, FR-061, FR-062 |
| Initiating Actor | Student |
| Actor’s Goal | To view the current application state, review its timeline and status history, and perform only the actions allowed for the application’s current status. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has one or more application records in the system. 3. The application tracking and management area is accessible. |
| Postconditions | 1. The selected application record is displayed with its current status and history. 2. The system shows the actions allowed for the application’s current state. 3. The Student can continue with a permitted management action if applicable. 4. Final Accepted or Rejected applications remain read-only. |
| Flow of Events for Main Success Scenario | 1. The Student opens the application tracking and management area. 2. The system displays the Student’s application records. 3. The Student selects an application to review. 4. The system retrieves the application record and its status history. 5. The system determines the current application status. 6. The system displays the current application details, timeline, and status history. 7. The system displays the actions allowed for the current status. 8. The Student reviews the application and available actions. 9. The Student may continue with one of the permitted actions if needed. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The application record cannot be retrieved. 1. The system cannot load the selected application normally. 2. The system displays an error or unavailable message.  7.1 The selected application is in Draft status. 1. The system displays the option to continue editing the draft. 2. The Student may continue editing the draft application.  7.2 The selected application is in a withdrawable state. 1. The system displays the withdrawal option. 2. The Student may proceed with the withdrawal flow.  7.3 The selected application is in a final Accepted or Rejected state. 1. The system treats the application as read-only. 2. The system does not allow modification actions beyond permitted viewing behavior.  9.1 The Student previously withdrew the application and the scholarship is still open. 1. The system may allow reapplication if no active application exists for the same scholarship. 2. The Student may continue with a new application attempt according to policy. |

## Use Case 113 – (UC-113) Retrieve Application Records & Status History


| Field | Description |
|---|---|
| Use Case 113 – (UC-113) | Retrieve Application Records & Status History |
| Related Requirements | FR-050, FR-051, FR-061 |
| Initiating Actor | System |
| Actor’s Goal | To retrieve the selected application record together with its current status and history so that the Student can review progress and timeline information. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The Student has requested to view or manage an application record. 3. The system has access to the relevant application data and status history storage. |
| Postconditions | 1. The application record is retrieved successfully if available. 2. The related status history becomes available for display. 3. The calling use case can continue with timeline and action display. |
| Flow of Events for Main Success Scenario | 1. The Student selects an application record from the tracking area. 2. The system receives the request to open the selected application. 3. The system identifies the target application record. 4. The system retrieves the application details. 5. The system retrieves the application’s current status and status-history entries. 6. The system prepares the retrieved information for display. 7. The system returns control to the Track and Manage Application use case so the record can be shown to the Student. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The selected application identifier is invalid or incomplete. 1. The system cannot resolve the requested application record. 2. The system returns an invalid-request or not-found result.  4.1 The application record does not exist or is not accessible to the Student. 1. The system cannot retrieve the requested application. 2. The system returns a not-found or unavailable result to the calling use case.  5.1 The status-history entries are incomplete or partially unavailable. 1. The system retrieves the available application data according to policy. 2. The system returns the available result with reduced history information if permitted.  4.2 A temporary technical issue occurs during retrieval. 1. The system cannot complete the retrieval process. 2. The system returns an error result to the calling use case. 3. The Student is asked to try again later. |

## Use Case 114 – (UC-114) Display Allowed Actions by Status


| Field | Description |
|---|---|
| Use Case 114 – (UC-114) | Display Allowed Actions by Status |
| Related Requirements | FR-058, FR-059, FR-060, FR-061, FR-062 |
| Initiating Actor | System |
| Actor’s Goal | To determine and display only the actions that are permitted for the application’s current status. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The system has retrieved the application record successfully. 3. The current application status is available. 4. The status-based action rules are available to the system. |
| Postconditions | 1. The system displays the actions allowed for the application’s current status. 2. Disallowed actions remain hidden, disabled, or unavailable according to design. 3. The Student can proceed only with permitted actions. |
| Flow of Events for Main Success Scenario | 1. The system receives the selected application record and its current status. 2. The system identifies the action rules associated with that status. 3. The system determines which actions are currently permitted. 4. The system displays the allowed actions in the application management view. 5. The Student reviews the available actions for the selected application. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The application is in Draft status. 1. The system displays the option to continue editing the draft. 2. Other actions are shown only if allowed by policy.  3.2 The application is in a withdrawable state. 1. The system displays the withdrawal option. 2. The Student may proceed to withdraw the application.  3.3 The application is in a final Accepted or Rejected state. 1. The system displays the record as read-only. 2. Edit or withdrawal actions are not displayed if not permitted.  3.4 The application was withdrawn and reapplication conditions are satisfied. 1. The system displays the reapplication option according to policy. 2. The Student may continue with a new application attempt.  2.1 A temporary technical issue occurs while determining the allowed actions. 1. The system cannot resolve the correct action set. 2. The system displays an error or safe limited-action state according to policy. |

## Use Case 115 – (UC-115) Continue Editing Draft


| Field | Description |
|---|---|
| Use Case 115 – (UC-115) | Continue Editing Draft |
| Related Requirements | FR-048, FR-050, FR-061, FR-062 |
| Initiating Actor | Student |
| Actor’s Goal | To reopen and continue editing a draft application before it is submitted. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has an application record in Draft status. 3. The draft application is still accessible for editing. |
| Postconditions | 1. The draft application is reopened successfully. 2. The Student can continue editing the draft content. 3. The draft remains available for later saving or final submission according to policy. |
| Flow of Events for Main Success Scenario | 1. The Student opens the application management area. 2. The Student selects an application that is in Draft status. 3. The system determines that the draft is editable. 4. The system opens the draft application form. 5. The system loads the saved draft content. 6. The Student continues editing the draft application. 7. The Student may later save changes again or proceed to final submission. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The selected application is no longer in Draft status. 1. The system does not allow draft editing. 2. The system displays the actions allowed for the current status instead.  5.1 The saved draft content cannot be loaded. 1. The system cannot open the draft normally. 2. The system displays an error or temporary unavailable message.  4.1 A temporary technical issue occurs while opening the draft. 1. The system cannot complete the draft-editing flow. 2. The system asks the Student to try again later. |

## Use Case 116 – (UC-116) Withdraw Application


| Field | Description |
|---|---|
| Use Case 116 – (UC-116) | Withdraw Application |
| Related Requirements | FR-058, FR-059, FR-061, FR-062 |
| Initiating Actor | Student |
| Actor’s Goal | To withdraw an application that is still eligible for withdrawal so that the Student can stop the current application process. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has selected an application in a withdrawable state. 3. The application is accessible in the application management area. |
| Postconditions | 1. The application is marked as withdrawn successfully. 2. The application no longer remains in an active competing state. 3. The Student may later become eligible to reapply if the scholarship remains open and no active application exists. |
| Flow of Events for Main Success Scenario | 1. The Student opens the application management area. 2. The Student selects an application that is eligible for withdrawal. 3. The system displays the withdrawal option and related application details. 4. The Student confirms the withdrawal request. 5. The system validates that the application is still in a withdrawable state. 6. The system updates the application status to withdrawn. 7. The system refreshes the application record and available actions. 8. The system confirms that the application has been withdrawn successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The application is no longer eligible for withdrawal. 1. The system blocks the withdrawal action. 2. The system informs the Student that withdrawal is not allowed in the current state.  6.1 A temporary technical issue occurs while updating the withdrawal state. 1. The system cannot complete the withdrawal. 2. The system displays an error message and asks the Student to try again later.  7.1 The system later determines that reapplication may be allowed. 1. The system displays or enables reapplication only if the policy conditions are satisfied. |

## Use Case 117 – (UC-117) Allow Reapplication After Withdrawal


| Field | Description |
|---|---|
| Use Case 117 – (UC-117) | Allow Reapplication After Withdrawal |
| Related Requirements | FR-059, FR-060, FR-061, FR-062 |
| Initiating Actor | System |
| Actor’s Goal | To allow the Student to reapply after withdrawing a previous application only when the scholarship is still open and no other active application exists. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The Student previously withdrew an application for the same scholarship. 3. The system can access the prior application state and the current scholarship availability. 4. The system can check whether any active application already exists for the same Student and scholarship. |
| Postconditions | 1. The system either allows or blocks reapplication according to the configured conditions. 2. If allowed, the Student can continue with a new application attempt. 3. If blocked, the Student remains unable to reapply until the conditions are satisfied. |
| Flow of Events for Main Success Scenario | 1. The Student attempts to apply again after withdrawing a previous application. 2. The system checks the previous application state. 3. The system confirms that the previous application was withdrawn. 4. The system checks whether the scholarship is still open. 5. The system checks whether another active application currently exists for the same Student and scholarship. 6. The system determines whether reapplication is allowed. 7. If the conditions are satisfied, the system enables the Student to start a new application attempt. 8. The Student may continue with the new application flow. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The scholarship is no longer open. 1. The system does not allow reapplication. 2. The system informs the Student that the scholarship is closed.  5.1 Another active application already exists. 1. The system blocks reapplication. 2. The system informs the Student that an active application already exists for the same scholarship.  3.1 The previous application was not actually withdrawn. 1. The system does not treat the current attempt as eligible for reapplication logic. 2. The normal action rules continue according to the current application state.  6.1 A temporary technical issue occurs while checking reapplication eligibility. 1. The system cannot confirm whether reapplication is allowed. 2. The system asks the Student to try again later. |

## Use Case 118 – (UC-118) Initiate and Track External Application


| Field | Description |
|---|---|
| Use Case 118 – (UC-118) | Initiate and Track External Application |
| Related Requirements | FR-053, FR-054, FR-055, FR-056 |
| Initiating Actor | Student |
| Actor’s Goal | To begin an external scholarship application and track its progress inside Scholar-Path through a self-tracked application record. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The selected scholarship is an external listing. 3. The scholarship details page is accessible. 4. The external application destination is configured for the scholarship listing. |
| Postconditions | 1. A self-tracked external application record is created for the Student. 2. The default initial status is recorded as Intent to Apply. 3. The Student is redirected to the external application URL. 4. The Student can later return to update the status manually and add personal notes. |
| Flow of Events for Main Success Scenario | 1. The Student opens an external scholarship listing. 2. The Student selects the action to apply externally. 3. The system creates a self-tracked external application record for the Student. 4. The system assigns the default initial status as Intent to Apply. 5. The system retrieves the configured external application URL. 6. The system redirects the Student to the external application destination. 7. The Student continues the application process outside the platform. 8. The Student may later return to the platform to track and update the external application record manually. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The self-tracked record cannot be created. 1. The system cannot create the external application record successfully. 2. The system does not continue the normal initiation flow. 3. The system displays an error message and asks the Student to try again later.  5.1 The external application URL is missing or invalid. 1. The system cannot provide a valid redirect destination. 2. The system displays an error or unavailable-action message.  6.1 The redirect cannot be completed successfully. 1. The system cannot send the Student to the external destination normally. 2. The system displays an error or fallback message according to policy.  8.1 The Student returns later to continue tracking. 1. The system allows the Student to open the self-tracked external application record. 2. The Student may update status manually or add personal notes. |

## Use Case 119 – (UC-119) Redirect to External URL


| Field | Description |
|---|---|
| Use Case 119 – (UC-119) | Redirect to External URL |
| Related Requirements | FR-054 |
| Initiating Actor | System |
| Actor’s Goal | To send the Student from Scholar-Path to the external application destination for the selected scholarship. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The selected scholarship is an external listing. 3. A valid external application URL is configured for the scholarship. 4. The external application initiation flow has reached the redirect step. |
| Postconditions | 1. The Student is redirected to the external application destination successfully if the URL is valid and reachable enough for redirect action. 2. The Student can continue the application process outside the platform. |
| Flow of Events for Main Success Scenario | 1. The system determines that the selected scholarship is an external listing. 2. The system retrieves the configured external application URL. 3. The system prepares the redirect action. 4. The system sends the Student to the external application URL. 5. The Student reaches the external application destination and continues the process there. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The external application URL is missing. 1. The system cannot prepare the redirect normally. 2. The system displays an unavailable-action message.  2.2 The external application URL is invalid or malformed. 1. The system rejects the redirect step. 2. The system displays an error message.  4.1 A temporary technical issue occurs during redirect processing. 1. The system cannot complete the redirect normally. 2. The system displays an error or fallback message according to policy. |

## Use Case 120 – (UC-120) Create Self-Tracked Application Record


| Field | Description |
|---|---|
| Use Case 120 – (UC-120) | Create Self-Tracked Application Record |
| Related Requirements | FR-053, FR-055 |
| Initiating Actor | System |
| Actor’s Goal | To create an internal record for an external scholarship application so that the Student can track its progress manually inside Scholar-Path. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The Student has selected an external scholarship listing. 3. The system has sufficient scholarship and Student context to create the tracking record. |
| Postconditions | 1. A self-tracked external application record is created successfully. 2. The record is linked to the Student and the selected scholarship. 3. The record is initialized with the default status Intent to Apply. 4. The record becomes available for later manual status updates and personal notes. |
| Flow of Events for Main Success Scenario | 1. The Student selects the action to apply externally. 2. The system identifies the Student and the selected external scholarship listing. 3. The system creates a new self-tracked application record. 4. The system links the record to the Student and scholarship. 5. The system sets the initial status to Intent to Apply. 6. The system stores the new record successfully. 7. The system returns control to the calling use case so the external redirect flow can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 A self-tracked record already exists for the same external application context. 1. The system handles the duplicate creation attempt according to policy. 2. The system may reuse the existing record or block duplicate creation according to design.  6.1 A temporary technical issue occurs while saving the self-tracked record. 1. The system cannot complete record creation. 2. The system returns an error result to the calling use case.  5.1 The initial status cannot be assigned correctly. 1. The system cannot finalize the record in the expected initial state. 2. The system treats the creation flow as failed or incomplete according to policy. |

## Use Case 121 – (UC-121) Add Personal Notes


| Field | Description |
|---|---|
| Use Case 121 – (UC-121) | Add Personal Notes |
| Related Requirements | FR-056 |
| Initiating Actor | Student |
| Actor’s Goal | To add personal notes to the self-tracked external application record for reminders, context, and follow-up information. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. A self-tracked external application record already exists. 3. The Student has opened the external application record. 4. The record supports note entry. |
| Postconditions | 1. The personal note is saved successfully to the external record. 2. The note becomes visible when the Student reviews the record later. |
| Flow of Events for Main Success Scenario | 1. The Student opens a self-tracked external application record. 2. The system displays the current record details. 3. The Student selects the add-note action. 4. The Student enters the personal note content. 5. The Student submits the note. 6. The system validates the note data according to system rules. 7. The system saves the note to the record. 8. The system refreshes the record view. 9. The Student sees the note saved successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The note content is missing, invalid, or exceeds allowed limits. 1. The system rejects the note submission. 2. The system displays the related validation message.  7.1 A temporary technical issue occurs while saving the note. 1. The system cannot complete the note update. 2. The system displays an error message and asks the Student to try again later. |

## Use Case 122 – (UC-122) Update Status Manually


| Field | Description |
|---|---|
| Use Case 122 – (UC-122) | Update Status Manually |
| Related Requirements | FR-055, FR-056 |
| Initiating Actor | Student |
| Actor’s Goal | To manually update the status of the self-tracked external application record after progress occurs outside the platform. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. A self-tracked external application record already exists. 3. The Student has returned later to the record after external progress has occurred. 4. The record is available for manual status update. |
| Postconditions | 1. The self-tracked external application record is updated with the new manually entered status. 2. The updated status becomes visible in the external application tracking view. 3. The status remains a self-tracked value and is not automatically validated against the external application system. |
| Flow of Events for Main Success Scenario | 1. The Student opens an existing self-tracked external application record. 2. The system displays the current status and record details. 3. The Student selects the manual status update action. 4. The Student enters or selects the updated status. 5. The Student submits the status change. 6. The system validates the submitted status value according to internal tracking rules. 7. The system saves the updated manual status. 8. The system refreshes the record view. 9. The Student sees the updated status successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The submitted status value is missing or invalid. 1. The system does not save the update. 2. The system displays the related validation message.  7.1 A temporary technical issue occurs while saving the manual status update. 1. The system cannot complete the update. 2. The system displays an error message and asks the Student to try again later.  3.1 The Student also wants to add personal notes. 1. The system allows the Student to continue with the related note-entry flow for the same record. |

## Use Case 123 – (UC-123) Review Applications and Update Status


| Field | Description |
|---|---|
| Use Case 123 – (UC-123) | Review Applications and Update Status |
| Related Requirements | FR-052, FR-063, FR-064, FR-065 |
| Initiating Actor | Company |
| Actor’s Goal | To review submitted applications for the Company’s own scholarship listings and update each application status according to the configured workflow. |
| Participating Actors | Student |
| Preconditions | 1. The Company is authenticated. 2. The Company has access to application-review features. 3. The selected application belongs to one of the Company’s own scholarship listings. 4. The application is available for review in the current workflow stage. |
| Postconditions | 1. The application is reviewed successfully by the Company. 2. If the requested transition is valid, the application status is updated. 3. The status change is recorded in the application history. 4. The Student is notified of the updated status. 5. If the final outcome is Accepted or Rejected, the application becomes read-only. |
| Flow of Events for Main Success Scenario | 1. The Company opens the submitted-application review area. 2. The system displays applications belonging only to the Company’s own listings. 3. The Company selects an application to review. 4. The system displays the submitted application details and current status. 5. The Company reviews the application content. 6. The Company selects a new status or decision outcome. 7. The system validates the requested status transition. 8. The system updates the application status. 9. The system records the status change in the application history. 10. The system notifies the Student of the updated status. 11. If the new status is Accepted or Rejected, the system blocks further changes by setting the record to a final read-only state. 12. The system confirms that the review and status update were completed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The selected application does not belong to the Company’s own listings. 1. The system blocks access to the application. 2. The system displays an access-denied or unavailable message.  7.1 The requested status transition is invalid. 1. The system rejects the requested update. 2. The system displays a status-transition error message.  8.1 A temporary technical issue occurs while saving the new status. 1. The system cannot complete the status update. 2. The system displays an error message and asks the Company to try again later.  10.1 Student notification delivery fails. 1. The system preserves the updated application status if it was saved successfully. 2. The system logs or handles the notification failure according to policy.  11.1 The final read-only lock cannot be applied successfully. 1. The system handles the issue according to final-state consistency policy. 2. The update may be treated as incomplete or failed according to policy. |

## Use Case 124 – (UC-124) Validate Status Transition


| Field | Description |
|---|---|
| Use Case 124 – (UC-124) | Validate Status Transition |
| Related Requirements | FR-052, FR-065 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the requested application status change is allowed according to the configured review workflow. |
| Participating Actors | Company |
| Preconditions | 1. The Company is authenticated. 2. The Company has opened an application belonging to its own scholarship listing. 3. The Company has selected a requested new status. 4. The current application status is available to the system. 5. The status-transition rules are available to the system. |
| Postconditions | 1. The system determines whether the requested status transition is valid. 2. If the transition is valid, the status-update flow continues. 3. If the transition is invalid, the status-update flow does not continue. |
| Flow of Events for Main Success Scenario | 1. The Company selects a new status for the application. 2. The system receives the requested status change. 3. The system retrieves the current application status. 4. The system checks the requested transition against the configured workflow rules. 5. The system confirms that the requested transition is allowed. 6. The system marks the validation step as successful. 7. The system returns control to the Review Applications and Update Status use case so the update can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The requested transition is not allowed from the current status. 1. The system marks the validation as failed. 2. The system returns a transition-error result to the calling use case.  3.1 The current application status cannot be retrieved. 1. The system cannot validate the requested transition normally. 2. The system returns an error result to the calling use case.  5.1 The requested status is incomplete or invalid. 1. The system rejects the submitted value. 2. The system returns a validation error result.  6.1 A temporary technical issue occurs during transition validation. 1. The system cannot complete the validation process. 2. The system returns an error result to the calling use case. 3. The Company is asked to try again later. |

## Use Case 125 – (UC-125) Record Status Change in History


| Field | Description |
|---|---|
| Use Case 125 – (UC-125) | Record Status Change in History |
| Related Requirements | FR-051, FR-052, FR-065 |
| Initiating Actor | System |
| Actor’s Goal | To record each successful application status change in the status history trail for auditability and timeline tracking. |
| Participating Actors | Company, Student |
| Preconditions | 1. A valid application status update has been completed successfully. 2. The system has access to the application record, previous status, and new status. 3. The status-history storage is available. |
| Postconditions | 1. A new status-history entry is recorded successfully. 2. The updated history becomes available in timeline and tracking views. 3. The application record remains traceable through its status-change trail. |
| Flow of Events for Main Success Scenario | 1. The system detects that the application status was updated successfully. 2. The system prepares the status-history entry details. 3. The system records the previous status and the new status. 4. The system stores the new history entry with the relevant change metadata according to design. 5. The system confirms that the history record was saved successfully. 6. The system returns control to the Review Applications and Update Status use case. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 A temporary technical issue occurs while saving the history entry. 1. The system cannot complete the history update. 2. The system returns an error or failure result to the calling use case.  2.1 The status-change details are incomplete. 1. The system cannot create a valid history entry. 2. The system handles the issue according to consistency policy.  5.1 The history record is saved partially or inconsistently. 1. The system detects that the history update did not complete normally. 2. The system handles the issue according to audit and data-consistency policy. |

## Use Case 126 – (UC-126) Notify Student of Updated Status


| Field | Description |
|---|---|
| Use Case 126 – (UC-126) | Notify Student of Updated Status |
| Related Requirements | FR-052, FR-065, FR-181 |
| Initiating Actor | System |
| Actor’s Goal | To inform the Student that the application status has been updated after Company review. |
| Participating Actors | Student, Company |
| Preconditions | 1. A valid application status update has been completed successfully. 2. The affected Student is identifiable from the application record. 3. The notification delivery mechanism is available. |
| Postconditions | 1. The Student is notified of the updated application status if delivery succeeds. 2. The notification event is completed or handled according to system policy. |
| Flow of Events for Main Success Scenario | 1. The system detects that the application status was updated successfully. 2. The system identifies the Student linked to the application. 3. The system prepares the status-update notification content. 4. The system sends the notification through the configured channel. 5. The Student receives the updated-status notification. 6. The system completes the notification step successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 Notification delivery fails. 1. The system cannot deliver the status update notification successfully. 2. The system logs or handles the failure according to notification policy.  2.1 The Student recipient data cannot be resolved correctly. 1. The system cannot complete notification targeting normally. 2. The system handles the issue according to system policy.  3.1 A temporary technical issue occurs while preparing the notification content. 1. The system cannot complete the notification preparation step. 2. The system returns an error or failure result to the calling use case. |

## Use Case 127 – (UC-127) Block Further Changes [Final Read-Only State]


| Field | Description |
|---|---|
| Use Case 127 – (UC-127) | Block Further Changes [Final Read-Only State] |
| Related Requirements | FR-060, FR-065 |
| Initiating Actor | System |
| Actor’s Goal | To lock the application record when it reaches a final outcome so that Accepted and Rejected records cannot be modified further. |
| Participating Actors | Company, Student |
| Preconditions | 1. An application record exists. 2. The application status has been updated successfully. 3. The new status is a final outcome. 4. The final-state locking logic is available to the system. |
| Postconditions | 1. The application record is set to a final read-only state. 2. Further modification actions are blocked for the record. 3. The application remains viewable but no longer editable. |
| Flow of Events for Main Success Scenario | 1. The system detects that the application status has been updated successfully. 2. The system determines that the new status is Accepted or Rejected. 3. The system applies the final read-only lock to the application record. 4. The system updates the record state accordingly. 5. The system confirms that further changes are now blocked. 6. The application remains available for viewing only. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The updated status is not a final outcome. 1. The system does not apply the final read-only lock. 2. The normal editable workflow continues if allowed.  3.1 A temporary technical issue occurs while applying the final lock. 1. The system cannot complete the lock operation successfully. 2. The system handles the issue according to final-state consistency policy.  4.1 The read-only state cannot be persisted correctly. 1. The system detects that the final-state update did not complete normally. 2. The system treats the overall outcome according to data-consistency policy. |

## Use Case 128 – (UC-128) Review Student Applications


| Field | Description |
|---|---|
| Use Case 128 – (UC-128) | Review Student Applications |
| Related Requirements | FR-052, FR-063, FR-064, FR-065 |
| Initiating Actor | Company |
| Actor’s Goal | To review submitted student applications that belong only to the Company’s own scholarship listings. |
| Participating Actors | None |
| Preconditions | 1. The Company is authenticated. 2. The Company has access to the application review area. 3. One or more submitted applications exist for the Company’s own listings. |
| Postconditions | 1. The Company can review the submitted application content successfully. 2. The Company can continue to decision and status-management actions if needed. |
| Flow of Events for Main Success Scenario | 1. The Company opens the application review area. 2. The system displays applications that belong only to the Company’s own scholarship listings. 3. The Company selects an application to review. 4. The system displays the application details and related materials. 5. The Company reviews the submitted information. 6. The Company may continue to outcome management if needed. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No submitted applications are available. 1. The system displays an empty-state message or equivalent result.  2.2 The Company attempts to access an application outside its ownership scope. 1. The system blocks the unauthorized access. 2. The system displays an access-denied or unavailable message.  4.1 The application data cannot be retrieved. 1. The system cannot display the application normally. 2. The system displays an error or temporary unavailable message. |

## Use Case 129 – (UC-129) View Submitted Applications


| Field | Description |
|---|---|
| Use Case 129 – (UC-129) | View Submitted Applications |
| Related Requirements | FR-052, FR-063, FR-064 |
| Initiating Actor | Company |
| Actor’s Goal | To view the list of submitted applications for the Company’s scholarship listings. |
| Participating Actors | None |
| Preconditions | 1. The Company is authenticated. 2. The Company has permission to access submitted applications. 3. The review area is accessible. |
| Postconditions | 1. The submitted application list is displayed successfully. 2. The Company can select a specific application for detailed review. |
| Flow of Events for Main Success Scenario | 1. The Company opens the submitted-applications area. 2. The system retrieves applications belonging to the Company’s own listings. 3. The system displays the submitted application list. 4. The Company reviews the available submissions. 5. The Company may select one application for deeper review. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No submitted applications exist. 1. The system displays an empty-state message.  2.2 A temporary technical issue occurs while loading the list. 1. The system cannot display the submitted applications normally. 2. The system shows an error or temporary unavailable message. |

## Use Case 130 – (UC-130) Review Attached Documents


| Field | Description |
|---|---|
| Use Case 130 – (UC-130) | Review Attached Documents |
| Related Requirements | FR-052, FR-065 |
| Initiating Actor | Company |
| Actor’s Goal | To review the documents attached to a submitted application so that the application can be evaluated more accurately. |
| Participating Actors | None |
| Preconditions | 1. The Company is authenticated. 2. The Company has opened a submitted application belonging to its own listing. 3. The application contains attached documents or document references. |
| Postconditions | 1. The Company can review the attached documents successfully. 2. The document review can inform the final application decision. |
| Flow of Events for Main Success Scenario | 1. The Company opens a submitted application record. 2. The system displays the attached documents or document list. 3. The Company selects a document to review. 4. The system opens or displays the selected document according to system design. 5. The Company reviews the attached documents. 6. The Company returns to the application review flow. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No documents are attached. 1. The system informs the Company that no documents are available for review.  4.1 A selected document cannot be opened or retrieved. 1. The system cannot display the document normally. 2. The system shows an error or unavailable message.  4.2 The Company attempts to access a document outside the permitted application scope. 1. The system blocks the access attempt. 2. The system displays an access-denied message. |

## Use Case 131 – (UC-131) Manage Application Review Outcomes


| Field | Description |
|---|---|
| Use Case 131 – (UC-131) | Manage Application Review Outcomes |
| Related Requirements | FR-052, FR-063, FR-064, FR-065 |
| Initiating Actor | Company |
| Actor’s Goal | To manage the review decision and update the application outcome according to the configured workflow. |
| Participating Actors | Student |
| Preconditions | 1. The Company is authenticated. 2. The Company has reviewed an application belonging to its own listing. 3. The application is eligible for an outcome update. |
| Postconditions | 1. The review outcome is updated successfully if valid. 2. The decision is recorded in the application flow. 3. The Student can later see the updated result. 4. Final Accepted or Rejected outcomes may trigger read-only behavior according to policy. |
| Flow of Events for Main Success Scenario | 1. The Company opens a submitted application record. 2. The Company reviews the application content and attached materials. 3. The Company selects a review outcome or status decision. 4. The system validates the requested decision. 5. The system updates the application outcome. 6. The system records the status change in the application history. 7. The system notifies the Student of the updated status. 8. If the final outcome is Accepted or Rejected, the system blocks further changes according to policy. 9. The system confirms that the outcome update was completed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The requested decision is invalid. 1. The system rejects the requested outcome update. 2. The system displays a decision-validation error message.  5.1 A temporary technical issue occurs while saving the outcome. 1. The system cannot complete the outcome update. 2. The system displays an error message and asks the Company to try again later.  7.1 Student notification delivery fails. 1. The system preserves the saved outcome if it was stored successfully. 2. The system logs or handles the notification failure according to policy. |

## Use Case 132 – (UC-132) Validate Decision


| Field | Description |
|---|---|
| Use Case 132 – (UC-132) | Validate Decision |
| Related Requirements | FR-052, FR-065 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the requested review outcome or status decision is allowed for the application’s current state. |
| Participating Actors | Company |
| Preconditions | 1. The Company is authenticated. 2. The Company has selected a requested review outcome. 3. The current application state is available to the system. 4. The decision rules are available to the system. |
| Postconditions | 1. The system determines whether the requested decision is valid. 2. If valid, the outcome-management flow continues. 3. If invalid, the outcome-management flow does not continue. |
| Flow of Events for Main Success Scenario | 1. The Company selects a review outcome or status decision. 2. The system receives the requested decision. 3. The system retrieves the current application state. 4. The system checks the requested decision against the configured rules. 5. The system confirms that the requested decision is valid. 6. The system marks the validation step as successful. 7. The system returns control to the calling use case so the update can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The requested decision is not allowed. 1. The system marks the validation as failed. 2. The system returns a validation error result to the calling use case.  3.1 The application state cannot be retrieved. 1. The system cannot validate the requested decision normally. 2. The system returns an error result to the calling use case.  6.1 A temporary technical issue occurs during decision validation. 1. The system cannot complete the validation process. 2. The system asks the Company to try again later. |

## Use Case 133 – (UC-133) View Company Ratings & Reviews


| Field | Description |
|---|---|
| Use Case 133 – (UC-133) | View Company Ratings & Reviews |
| Related Requirements | FR-072, FR-073, FR-075 |
| Initiating Actor | Company |
| Actor’s Goal | To view ratings and reviews submitted by Students about the Company’s scholarship review experience. |
| Participating Actors | None |
| Preconditions | 1. The Company is authenticated. 2. The Company has access to ratings and review views. 3. One or more ratings or reviews may exist for the Company. |
| Postconditions | 1. The Company can view the available ratings and reviews successfully. 2. The Company can monitor reputation-related feedback. |
| Flow of Events for Main Success Scenario | 1. The Company opens the ratings and reviews area. 2. The system retrieves the reviews associated with the Company. 3. The system displays the available ratings, review content, and summary indicators according to design. 4. The Company reviews the displayed feedback successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No ratings or reviews are available. 1. The system displays an empty-state message or equivalent result.  2.2 A temporary technical issue occurs while loading ratings or reviews. 1. The system cannot display the feedback normally. 2. The system shows an error or temporary unavailable message. |

## Use Case 134 – (UC-134) Rate & Review Company


| Field | Description |
|---|---|
| Use Case 134 – (UC-134) | Rate & Review Company |
| Related Requirements | FR-070, FR-071, FR-074 |
| Initiating Actor | Student |
| Actor’s Goal | To submit a rating and written review for a Company after the related scholarship application reaches an eligible review outcome. |
| Participating Actors | Company |
| Preconditions | 1. The Student is authenticated. 2. The Student has a related application record tied to the Company. 3. The application has reached a final review outcome or closed review state that allows feedback. 4. The Student has not already submitted a duplicate review for the same application if duplication is blocked by policy. |
| Postconditions | 1. The rating and review are saved successfully if valid. 2. The feedback becomes associated with the related Student and application record. 3. The Company can view the submitted feedback according to system rules. |
| Flow of Events for Main Success Scenario | 1. The Student opens the eligible application or review-feedback area. 2. The system displays the rating and review form. 3. The Student selects a rating value and enters review text. 4. The Student submits the feedback. 5. The system validates the submitted review data and eligibility conditions. 6. The system saves the rating and review. 7. The system associates the feedback with the related application and Company. 8. The system confirms that the feedback was submitted successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The application is not eligible for Company review feedback. 1. The system blocks the feedback submission. 2. The system informs the Student that review is not allowed for the current application state.  5.2 A duplicate review already exists for the same application. 1. The system rejects the duplicate submission according to policy. 2. The system informs the Student that the review already exists.  6.1 A temporary technical issue occurs while saving the review. 1. The system cannot complete the feedback submission. 2. The system displays an error message and asks the Student to try again later. |

## Use Case 135 – (UC-135) Configure Company Review Pricing Rules


| Field | Description |
|---|---|
| Use Case 135 – (UC-135) | Configure Company Review Pricing Rules |
| Related Requirements | FR-068, FR-191 |
| Initiating Actor | Admin |
| Actor’s Goal | To configure the pricing rules used for Company review-service payments so that review fees can be controlled centrally. |
| Participating Actors | None |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has access to financial or pricing configuration features. 3. Company review-pricing configuration is enabled for management. |
| Postconditions | 1. The Company review pricing rules are created or updated successfully if valid. 2. The configured rules become available for future fee calculations according to policy. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the Company review pricing configuration area. 2. The system displays the current pricing configuration. 3. The Admin enters or updates the pricing rule values. 4. The Admin submits the pricing configuration. 5. The system validates the submitted configuration values. 6. The system stores the updated Company review pricing rules. 7. The system confirms that the pricing configuration was updated successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The submitted pricing values are invalid or out of policy. 1. The system rejects the configuration update. 2. The system displays the related validation errors.  6.1 A temporary technical issue occurs while saving the configuration. 1. The system cannot complete the update. 2. The system displays an error message and asks the Admin to try again later. |

## Use Case 136 – (UC-136) Moderate Company Ratings & Reviews


| Field | Description |
|---|---|
| Use Case 136 – (UC-136) | Moderate Company Ratings & Reviews |
| Related Requirements | FR-075 |
| Initiating Actor | Admin |
| Actor’s Goal | To moderate inappropriate or abusive Company ratings and reviews so that platform content remains safe and relevant. |
| Participating Actors | Company, Student |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has access to review moderation tools. 3. One or more Company ratings or reviews exist and are available for moderation. |
| Postconditions | 1. The selected rating or review is moderated successfully according to policy. 2. The moderation action is reflected in review visibility or status according to system rules. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the Company review moderation area. 2. The system displays the available Company ratings and reviews. 3. The Admin selects a review to moderate. 4. The Admin chooses the moderation action allowed by policy. 5. The system validates the moderation request. 6. The system applies the moderation action. 7. The system confirms that moderation was completed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The requested moderation action is not allowed. 1. The system rejects the action. 2. The system displays the related moderation error message.  6.1 A temporary technical issue occurs while applying moderation. 1. The system cannot complete the moderation step. 2. The system displays an error message and asks the Admin to try again later. |

## Use Case 137 – (UC-137) Calculate Review Fee


| Field | Description |
|---|---|
| Use Case 137 – (UC-137) | Calculate Review Fee |
| Related Requirements | FR-067, FR-068, FR-191 |
| Initiating Actor | System |
| Actor’s Goal | To calculate the fee for Company review-service transactions according to the configured platform pricing rules. |
| Participating Actors | Company, Admin |
| Preconditions | 1. A Company review-service transaction or eligible review event exists. 2. The applicable pricing rules are available to the system. 3. The system has the data needed for fee calculation. |
| Postconditions | 1. The review fee is calculated successfully if the required pricing inputs are valid. 2. The calculated fee becomes available for payment recording and settlement processing. |
| Flow of Events for Main Success Scenario | 1. The system identifies a Company review-service transaction that requires fee calculation. 2. The system retrieves the applicable pricing rules. 3. The system applies the pricing logic to the transaction data. 4. The system calculates the review fee amount. 5. The system stores or returns the calculated fee for the next financial step. 6. The system confirms that fee calculation was completed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No valid pricing rule is available. 1. The system cannot calculate the review fee normally. 2. The system returns an error or fallback result according to policy.  3.1 Required pricing inputs are missing or invalid. 1. The system cannot calculate the review fee correctly. 2. The system returns a validation or error result.  4.1 A temporary technical issue occurs during calculation. 1. The system cannot complete the fee-calculation process. 2. The system returns an error result to the calling flow. |

## Use Case 138 – (UC-138) Record Company Review Payment


| Field | Description |
|---|---|
| Use Case 138 – (UC-138) | Record Company Review Payment |
| Related Requirements | FR-066, FR-067, FR-069, FR-191, FR-210 |
| Initiating Actor | Stripe Payment Gateway |
| Actor’s Goal | To record Company review-service payment information so that the financial transaction can be tracked inside the platform. |
| Participating Actors | Company, Admin |
| Preconditions | 1. A Company review-service payment event exists. 2. The payment gateway integration is available. 3. The system has enough transaction context to record the payment properly. |
| Postconditions | 1. The Company review payment is recorded successfully. 2. The recorded payment becomes available for financial views, settlement processing, and audit use. 3. The system can later generate settlement records based on the recorded payment. |
| Flow of Events for Main Success Scenario | 1. A Company review-service payment event is received through the payment integration. 2. The system identifies the related Company review transaction. 3. The system calculates or retrieves the applicable review fee if needed. 4. The system records the payment details, amount, status, and related references. 5. The system stores the Company review payment successfully. 6. The system confirms that the payment was recorded successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The related Company review transaction cannot be resolved. 1. The system cannot safely record the payment against the correct business record. 2. The system returns an error or handles the event according to policy.  4.1 The payment data is incomplete or invalid. 1. The system cannot store a correct payment record. 2. The system returns an error or validation result.  5.1 A temporary technical issue occurs while recording the payment. 1. The system cannot complete the payment-recording process. 2. The system handles the issue according to financial consistency policy. |

## Use Case 139 – (UC-139) Generate Settlement Record


| Field | Description |
|---|---|
| Use Case 139 – (UC-139) | Generate Settlement Record |
| Related Requirements | FR-069, FR-210, FR-209 |
| Initiating Actor | Admin |
| Actor’s Goal | To generate a settlement record for Company review-service transactions so that the financial outcome can be tracked, reconciled, and reported. |
| Participating Actors | Company, Stripe Payment Gateway |
| Preconditions | 1. The Admin is authenticated. 2. A Company review payment or eligible review-service financial transaction already exists. 3. The settlement-generation feature is accessible. |
| Postconditions | 1. A settlement record is generated successfully for the selected Company review transaction or set of transactions. 2. The settlement data becomes available for financial review, reporting, or audit. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the settlement area for Company review-service transactions. 2. The system displays the available eligible financial records. 3. The Admin selects the payment or transaction set to settle. 4. The system retrieves the related financial details. 5. The system generates the settlement record. 6. The system stores the settlement result successfully. 7. The Admin sees that the settlement record was generated successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The required financial details cannot be retrieved. 1. The system cannot generate the settlement record normally. 2. The system displays an error or unavailable message.  5.1 A temporary technical issue occurs during settlement generation. 1. The system cannot complete the settlement process. 2. The system displays an error message and asks the Admin to try again later.  3.1 No eligible payment records are available for settlement. 1. The system displays an empty-state or no-eligible-records message. |

## Use Case 140 – (UC-140) Search Consultant Directory


| Field | Description |
|---|---|
| Use Case 140 – (UC-140) | Search Consultant Directory |
| Related Requirements | FR-076, FR-077, FR-078, FR-079, FR-080, FR-084 |
| Initiating Actor | Student |
| Actor’s Goal | To search and browse available consultants so that a suitable consultant can be found for booking or profile review. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The consultant directory is accessible. 3. Consultant records are available in the system. |
| Postconditions | 1. The system displays consultant search results successfully. 2. The Student can continue to view consultant details or initiate a booking flow. |
| Flow of Events for Main Success Scenario | 1. The Student opens the consultant directory. 2. The system displays available consultant listings and discovery controls. 3. The Student enters search terms or browses the available consultants. 4. The system retrieves the matching consultant results. 5. The system displays the consultant results to the Student. 6. The Student reviews the available consultants successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 No consultants match the selected criteria. 1. The system returns an empty result set. 2. The system informs the Student that no matching consultants were found.  4.2 A temporary technical issue occurs while loading the directory. 1. The system cannot display the consultant results normally. 2. The system displays an error or temporary unavailable message. |

## Use Case 141 – (UC-141) View Webinar Events


| Field | Description |
|---|---|
| Use Case 141 – (UC-141) | View Webinar Events |
| Related Requirements | FR-079, FR-080, FR-081, FR-185, FR-186 |
| Initiating Actor | Student |
| Actor’s Goal | To view available webinar events related to consultants so that the Student can explore additional learning or engagement options. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. Webinar events are available in the system. 3. The webinar-events area is accessible. |
| Postconditions | 1. The available webinar events are displayed successfully. 2. The Student can review webinar information and decide on further action if applicable. |
| Flow of Events for Main Success Scenario | 1. The Student opens the webinar-events area. 2. The system retrieves the available webinar events. 3. The system displays the webinar list and related information. 4. The Student reviews the available webinar events successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No webinar events are currently available. 1. The system displays an empty-state message or equivalent result.  2.2 A temporary technical issue occurs while retrieving webinar events. 1. The system cannot display the webinar list normally. 2. The system shows an error or temporary unavailable message. |

## Use Case 142 – (UC-142) View Consultant Profile


| Field | Description |
|---|---|
| Use Case 142 – (UC-142) | View Consultant Profile |
| Related Requirements | FR-057, FR-078, FR-084 |
| Initiating Actor | Student |
| Actor’s Goal | To review a consultant’s profile, consultation information, and visible booking-related details before requesting a booking. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The selected consultant profile exists and is accessible. 3. The Student has selected a consultant from the directory or related area. |
| Postconditions | 1. The consultant profile is displayed successfully. 2. The Student can review consultant information and proceed to booking if desired. |
| Flow of Events for Main Success Scenario | 1. The Student selects a consultant from the directory or related list. 2. The system retrieves the consultant profile information. 3. The system displays the consultant profile details. 4. The Student reviews the consultant information successfully. 5. The Student may continue to request a booking. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The consultant profile cannot be retrieved. 1. The system cannot display the consultant profile normally. 2. The system displays an error or unavailable message.  3.1 Some consultant profile sections are temporarily unavailable. 1. The system displays the available profile information according to policy. 2. The Student may still review the accessible content. |

## Use Case 143 – (UC-143) Request Booking


| Field | Description |
|---|---|
| Use Case 143 – (UC-143) | Request Booking |
| Related Requirements | FR-138, FR-145, FR-146, FR-152 |
| Initiating Actor | Student |
| Actor’s Goal | To request a consultant booking and reserve the session through the platform. |
| Participating Actors | Consultant, Stripe API |
| Preconditions | 1. The Student is authenticated. 2. The Student has selected a consultant. 3. The consultant has bookable availability or booking access is enabled. 4. The booking request flow is accessible. |
| Postconditions | 1. A booking request is created successfully if valid. 2. The payment hold process is triggered successfully if required. 3. The booking request becomes available for consultant response. |
| Flow of Events for Main Success Scenario | 1. The Student opens the consultant booking flow. 2. The system displays the booking request form or available booking options. 3. The Student selects the desired session or booking details. 4. The Student submits the booking request. 5. The system validates the booking data and eligibility conditions. 6. The system processes the payment hold if required. 7. The system creates the booking request. 8. The system makes the request available to the Consultant for acceptance or rejection. 9. The Student sees that the booking request was submitted successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The submitted booking data is missing or invalid. 1. The system does not create the booking request. 2. The system displays the related validation errors.  6.1 The payment hold cannot be completed successfully. 1. The system does not finalize the booking request according to policy. 2. The system displays an error or payment-related message.  7.1 A temporary technical issue occurs while creating the booking request. 1. The system cannot complete the request creation. 2. The system asks the Student to try again later. |

## Use Case 144 – (UC-144) Process Payment Hold


| Field | Description |
|---|---|
| Use Case 144 – (UC-144) | Process Payment Hold |
| Related Requirements | FR-082, FR-083, FR-086, FR-188 |
| Initiating Actor | System |
| Actor’s Goal | To place a payment hold for a requested consultant booking before final capture conditions are met. |
| Participating Actors | Student, Stripe API |
| Preconditions | 1. A valid booking request is being processed. 2. Payment-hold logic is enabled for the booking flow. 3. Payment gateway integration is available. |
| Postconditions | 1. The payment hold is created successfully if authorized. 2. The held amount becomes available for later capture or refund handling according to policy. 3. The booking flow can continue if the hold succeeds. |
| Flow of Events for Main Success Scenario | 1. The system receives a valid booking request requiring payment hold. 2. The system prepares the payment-hold request data. 3. The system sends the hold request to the Stripe API. 4. The Stripe API processes the hold request. 5. The system receives the successful hold result. 6. The system records the held-payment state. 7. The system returns control to the booking-request flow. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The payment hold is declined or fails. 1. The system cannot complete the hold successfully. 2. The system returns a payment-failure result to the calling use case.  3.1 The payment request data is incomplete or invalid. 1. The system cannot submit a valid hold request. 2. The system returns an error result.  5.1 A temporary technical issue occurs during gateway communication. 1. The system cannot confirm successful payment hold creation. 2. The system handles the issue according to payment-consistency policy. |

## Use Case 145 – (UC-145) Accept / Reject Booking Request


| Field | Description |
|---|---|
| Use Case 145 – (UC-145) | Accept / Reject Booking Request |
| Related Requirements | FR-081, FR-082, FR-083, FR-187, FR-188 |
| Initiating Actor | Consultant |
| Actor’s Goal | To accept or reject an incoming booking request according to availability and booking rules. |
| Participating Actors | Student |
| Preconditions | 1. The Consultant is authenticated. 2. A booking request exists and is accessible to the Consultant. 3. The booking request is awaiting consultant action. |
| Postconditions | 1. The booking request is updated with the Consultant’s decision. 2. The booking record reflects acceptance or rejection successfully. 3. Downstream payment behavior can continue according to the decision and session policy. |
| Flow of Events for Main Success Scenario | 1. The Consultant opens the booking-request area. 2. The system displays the pending booking requests. 3. The Consultant selects a booking request. 4. The Consultant reviews the request details. 5. The Consultant chooses to accept or reject the booking request. 6. The system validates the requested action. 7. The system updates the booking decision. 8. The system confirms that the request decision was saved successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The requested action is no longer valid for the booking state. 1. The system rejects the action. 2. The system displays a booking-state error message.  7.1 A temporary technical issue occurs while saving the consultant decision. 1. The system cannot complete the update. 2. The system displays an error message and asks the Consultant to try again later. |

## Use Case 146 – (UC-146) Manage Availability Slots


| Field | Description |
|---|---|
| Use Case 146 – (UC-146) | Manage Availability Slots |
| Related Requirements | FR-077, FR-084 |
| Initiating Actor | Consultant |
| Actor’s Goal | To create, update, or manage availability slots for future booking requests. |
| Participating Actors | None |
| Preconditions | 1. The Consultant is authenticated. 2. The Consultant has access to availability-management features. 3. Availability scheduling is enabled in the system. |
| Postconditions | 1. The Consultant’s availability slots are updated successfully. 2. The updated availability becomes available for booking discovery according to system rules. |
| Flow of Events for Main Success Scenario | 1. The Consultant opens the availability-management area. 2. The system displays the current availability slots. 3. The Consultant creates, edits, or removes an availability slot. 4. The Consultant submits the availability changes. 5. The system validates the submitted slot data. 6. The system saves the updated availability. 7. The system refreshes the consultant availability view. 8. The Consultant sees the updated availability successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The submitted availability slot data is invalid or conflicting. 1. The system rejects the change. 2. The system displays the related validation message.  6.1 A temporary technical issue occurs while saving availability. 1. The system cannot complete the update. 2. The system asks the Consultant to try again later. |

## Use Case 147 – (UC-147) Cancel Booking


| Field | Description |
|---|---|
| Use Case 147 – (UC-147) | Cancel Booking |
| Related Requirements | FR-085, FR-086, FR-087, FR-088, FR-089, FR-090, FR-091, FR-193 |
| Initiating Actor | Student |
| Actor’s Goal | To cancel an existing booking request or session when cancellation is allowed by system policy. |
| Participating Actors | Consultant, Stripe API |
| Preconditions | 1. The Student is authenticated. 2. A booking exists and is accessible to the Student. 3. The booking is still eligible for cancellation according to the current state and policy. |
| Postconditions | 1. The booking is marked as canceled successfully if allowed. 2. The system checks the session state before determining downstream financial behavior. 3. Refund behavior may be triggered according to the cancellation policy and session state. |
| Flow of Events for Main Success Scenario | 1. The Student opens the booking record. 2. The system displays the booking details and cancellation option if available. 3. The Student confirms the booking cancellation request. 4. The system checks the current session state. 5. The system validates that cancellation is allowed. 6. The system updates the booking as canceled. 7. The system determines whether refund processing is required according to the session state and policy. 8. The system confirms that the booking was canceled successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The booking is no longer eligible for cancellation. 1. The system blocks the cancellation action. 2. The system informs the Student that cancellation is not allowed in the current state.  4.1 The session state cannot be determined. 1. The system cannot safely continue the cancellation flow. 2. The system displays an error or temporary unavailable message.  7.1 Refund processing is required. 1. The system continues to the refund-processing flow according to policy.  6.1 A temporary technical issue occurs while updating the booking. 1. The system cannot complete the cancellation. 2. The system asks the Student to try again later. |

## Use Case 148 – (UC-148) Check Session State


| Field | Description |
|---|---|
| Use Case 148 – (UC-148) | Check Session State |
| Related Requirements | FR-084, FR-090, FR-091, FR-092 |
| Initiating Actor | System |
| Actor’s Goal | To determine the current session or booking state before cancellation, refund, or payment-capture decisions are applied. |
| Participating Actors | Student, Consultant |
| Preconditions | 1. A booking or session record exists. 2. The system needs the current session state to continue the booking financial or cancellation flow. |
| Postconditions | 1. The system determines the current session state if available. 2. The resulting state becomes available for cancellation, refund, or capture logic. |
| Flow of Events for Main Success Scenario | 1. The system receives a request that depends on the booking or session state. 2. The system identifies the target booking or session record. 3. The system retrieves the current session state. 4. The system confirms the state needed for downstream logic. 5. The system returns the session-state result to the calling use case. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The session state cannot be retrieved. 1. The system cannot determine the required state. 2. The system returns an error result to the calling use case.  4.1 The retrieved state is incomplete or inconsistent. 1. The system cannot safely continue dependent logic automatically. 2. The system handles the issue according to consistency policy. |

## Use Case 149 – (UC-149) Process Refund


| Field | Description |
|---|---|
| Use Case 149 – (UC-149) | Process Refund |
| Related Requirements | FR-085, FR-086, FR-087, FR-088, FR-089, FR-090, FR-091, FR-193, FR-199 |
| Initiating Actor | System |
| Actor’s Goal | To process a refund for a canceled booking when refund conditions are satisfied. |
| Participating Actors | Student, Stripe API |
| Preconditions | 1. A booking cancellation or refund-triggering event exists. 2. The system has determined that refund processing is required according to session state and policy. 3. Payment gateway integration is available. |
| Postconditions | 1. The refund is processed successfully if eligible. 2. The refund result is recorded in the booking payment flow. 3. The financial outcome becomes available for later reporting or audit. |
| Flow of Events for Main Success Scenario | 1. The system determines that a booking refund is required. 2. The system prepares the refund request data. 3. The system sends the refund request to the Stripe API. 4. The Stripe API processes the refund request. 5. The system receives the refund result. 6. The system records the refund outcome successfully. 7. The system confirms that the refund flow was completed. |
| Flow of Events for Extensions (Alternate Scenarios) | 1.1 The booking is not eligible for refund. 1. The system does not continue the refund flow. 2. The system follows the non-refundable policy path.  3.1 The refund request fails or is rejected by the payment gateway. 1. The system cannot complete the refund successfully. 2. The system handles the failure according to payment policy.  5.1 A temporary technical issue occurs during refund processing. 1. The system cannot confirm the refund result normally. 2. The system handles the issue according to financial consistency policy. |

## Use Case 150 – (UC-150) Capture Payment


| Field | Description |
|---|---|
| Use Case 150 – (UC-150) | Capture Payment |
| Related Requirements | FR-081, FR-187, FR-189, FR-190, FR-192, FR-194, FR-195, FR-197, FR-200, FR-095 |
| Initiating Actor | System |
| Actor’s Goal | To capture a previously held booking payment when the booking reaches the required payment-capture condition. |
| Participating Actors | Student, Consultant, Stripe API |
| Preconditions | 1. A booking exists with a previously successful payment hold. 2. The booking has reached the state that allows or requires payment capture. 3. Payment gateway integration is available. |
| Postconditions | 1. The held payment is captured successfully if eligible. 2. The captured-payment state is recorded in the booking transaction flow. 3. The payment outcome becomes available for consultant payment history and financial records. |
| Flow of Events for Main Success Scenario | 1. The system determines that the booking has reached the required payment-capture condition. 2. The system retrieves the prior payment-hold information. 3. The system prepares the payment-capture request. 4. The system sends the capture request to the Stripe API. 5. The Stripe API processes the capture request. 6. The system receives the successful capture result. 7. The system records the captured-payment outcome. 8. The system confirms that payment capture was completed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 1.1 The booking is not yet eligible for capture. 1. The system does not continue the capture flow. 2. The held-payment state remains unchanged according to policy.  4.1 The capture request fails or is rejected. 1. The system cannot complete capture successfully. 2. The system handles the failure according to payment policy.  6.1 A temporary technical issue occurs during capture processing. 1. The system cannot confirm the capture result normally. 2. The system handles the issue according to financial consistency policy. |

## Use Case 151 – (UC-151) Submit Rating & Review


| Field | Description |
|---|---|
| Use Case 151 – (UC-151) | Submit Rating & Review |
| Related Requirements | FR-093, FR-096, FR-097, FR-098, FR-100 |
| Initiating Actor | Student |
| Actor’s Goal | To submit a rating and review for a consultant after an eligible booking or session outcome. |
| Participating Actors | Consultant, System Admin |
| Preconditions | 1. The Student is authenticated. 2. The Student has a related consultant booking or completed session eligible for review. 3. The review feature is accessible. 4. Duplicate-review restrictions are satisfied if applicable. |
| Postconditions | 1. The rating and review are saved successfully if valid. 2. The feedback becomes associated with the consultant and related booking context. 3. The review may affect consultant profile standing if moderation or low-score rules apply. |
| Flow of Events for Main Success Scenario | 1. The Student opens the eligible booking or review area. 2. The system displays the rating and review form. 3. The Student selects a rating value and enters review text. 4. The Student submits the feedback. 5. The system validates the submitted review data and eligibility conditions. 6. The system saves the rating and review. 7. The system associates the feedback with the related consultant and booking record. 8. The system confirms that the rating and review were submitted successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The booking is not eligible for consultant review feedback. 1. The system blocks the feedback submission. 2. The system informs the Student that review is not allowed for the current booking state.  5.2 A duplicate review already exists for the same booking. 1. The system rejects the duplicate submission according to policy. 2. The system informs the Student that the review already exists.  6.1 A temporary technical issue occurs while saving the review. 1. The system cannot complete the feedback submission. 2. The system asks the Student to try again later.  7.1 The submitted rating triggers a low-score threshold condition. 1. The system may continue to the related profile-control flow according to policy. |

## Use Case 152 – (UC-152) View Ratings & Payment History


| Field | Description |
|---|---|
| Use Case 152 – (UC-152) | View Ratings & Payment History |
| Related Requirements | FR-092, FR-098, FR-099, FR-196 |
| Initiating Actor | Consultant |
| Actor’s Goal | To view received ratings and the related payment history inside the consultant account area. |
| Participating Actors | None |
| Preconditions | 1. The Consultant is authenticated. 2. The Consultant has access to ratings and payment-history views. 3. Ratings and payment records may exist for the consultant account. |
| Postconditions | 1. The consultant ratings and payment history are displayed successfully. 2. The Consultant can review reputation and payment-related information in one place. |
| Flow of Events for Main Success Scenario | 1. The Consultant opens the ratings and payment-history area. 2. The system retrieves the available rating and payment records for the consultant account. 3. The system displays the consultant ratings, reviews, and payment-history information. 4. The Consultant reviews the displayed records successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No ratings or payment history records are available. 1. The system displays an empty-state message or equivalent result.  2.2 A temporary technical issue occurs while loading ratings or payment history. 1. The system cannot display the records normally. 2. The system shows an error or temporary unavailable message. |

## Use Case 153 – (UC-153) Moderate Reviews


| Field | Description |
|---|---|
| Use Case 153 – (UC-153) | Moderate Reviews |
| Related Requirements | FR-075, FR-101 |
| Initiating Actor | System Admin |
| Actor’s Goal | To moderate inappropriate or abusive consultant reviews so that review content remains safe and policy-compliant. |
| Participating Actors | Student, Consultant |
| Preconditions | 1. The System Admin is authenticated. 2. The System Admin has access to review-moderation tools. 3. One or more consultant reviews exist and are available for moderation. |
| Postconditions | 1. The selected review is moderated successfully according to policy. 2. The moderation result is reflected in review visibility or status according to system rules. |
| Flow of Events for Main Success Scenario | 1. The System Admin opens the consultant review moderation area. 2. The system displays the available consultant reviews. 3. The System Admin selects a review to moderate. 4. The System Admin chooses the moderation action allowed by policy. 5. The system validates the moderation request. 6. The system applies the moderation action. 7. The system confirms that review moderation was completed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The requested moderation action is not allowed. 1. The system rejects the moderation request. 2. The system displays the related moderation error message.  6.1 A temporary technical issue occurs while applying moderation. 1. The system cannot complete the moderation step. 2. The system asks the System Admin to try again later. |

## Use Case 154 – (UC-154) Suspend Low-Rated Profile


| Field | Description |
|---|---|
| Use Case 154 – (UC-154) | Suspend Low-Rated Profile |
| Related Requirements | FR-094 |
| Initiating Actor | System |
| Actor’s Goal | To suspend or restrict a consultant profile when the profile breaches the configured low-rating threshold according to platform policy. |
| Participating Actors | Consultant, System Admin |
| Preconditions | 1. The system has access to consultant rating data. 2. A configured low-rating threshold or suspension rule exists. 3. A consultant profile has reached the condition that triggers suspension logic. |
| Postconditions | 1. The consultant profile is suspended or restricted according to policy if the threshold condition is satisfied. 2. The consultant profile state is updated accordingly. 3. The resulting profile-control action becomes visible in the relevant management views. |
| Flow of Events for Main Success Scenario | 1. The system evaluates consultant rating outcomes according to the configured threshold logic. 2. The system determines that the consultant profile has reached the low-rating threshold. 3. The system applies the suspension or restriction action to the consultant profile. 4. The system updates the consultant profile state successfully. 5. The system confirms that the low-rated profile action was completed. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The low-rating threshold is not actually breached. 1. The system does not suspend the consultant profile. 2. The profile remains active according to policy.  3.1 A temporary technical issue occurs while applying the suspension action. 1. The system cannot complete the profile restriction normally. 2. The system handles the issue according to profile-governance policy.  1.1 Rating data is incomplete or unavailable. 1. The system cannot safely determine whether suspension should occur. 2. The system handles the issue according to consistency policy. |

## Use Case 155 – (UC-155) Submit Rating & Review


| Field | Description |
|---|---|
| Use Case 155 – (UC-155) | Submit Rating & Review |
| Related Requirements | FR-070, FR-071, FR-074, FR-096, FR-097, FR-100 |
| Initiating Actor | Student |
| Actor’s Goal | To submit a rating and written review after an eligible completed consultant session or finalized application outcome. |
| Participating Actors | Company, Consultant |
| Preconditions | 1. The Student is authenticated. 2. The Student has an eligible completed session or finalized application outcome. 3. The related Company or Consultant target exists and is reviewable. 4. The rating and review feature is accessible. 5. The Student has not violated duplicate-review rules for the same eligible context. |
| Postconditions | 1. The rating and review are saved successfully if valid. 2. The review is linked to the related booking or application context. 3. The target entity’s average rating is recalculated. 4. The submitted review becomes available according to system visibility rules. |
| Flow of Events for Main Success Scenario | 1. The Student opens the eligible feedback area. 2. The system displays the rating and review form. 3. The Student selects the related review context. 4. The Student enters the rating value and review text. 5. The Student submits the review. 6. The system validates eligibility and duplicate-review rules. 7. The system links the review to the related booking or application. 8. The system saves the rating and review. 9. The system recalculates the target entity’s average rating. 10. The system confirms that the review was submitted successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The Student is not eligible to submit the review. 1. The system blocks the review submission. 2. The system informs the Student that feedback is not allowed for the selected context.  6.2 A duplicate review already exists for the same booking or application context. 1. The system rejects the duplicate submission according to policy. 2. The system informs the Student that a review already exists.  8.1 A temporary technical issue occurs while saving the review. 1. The system cannot complete the review submission. 2. The system displays an error message and asks the Student to try again later. |

## Use Case 156 – (UC-156) Validate Eligibility & Duplicate Rules


| Field | Description |
|---|---|
| Use Case 156 – (UC-156) | Validate Eligibility & Duplicate Rules |
| Related Requirements | FR-070, FR-071, FR-074, FR-096, FR-097, FR-100 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the Student is allowed to submit the review and that no duplicate review already exists for the same eligible context. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The Student has initiated the review-submission flow. 3. The related booking or application context is available to the system. 4. The eligibility and duplicate-check rules are available. |
| Postconditions | 1. The system determines whether the review submission is allowed. 2. If valid, the review-submission flow continues. 3. If invalid, the review-submission flow stops and the Student is informed. |
| Flow of Events for Main Success Scenario | 1. The Student submits a rating and review request. 2. The system identifies the related booking or application context. 3. The system checks whether that context is eligible for review submission. 4. The system checks whether the Student has already submitted a review for the same eligible context. 5. The system confirms that the Student is eligible and no duplicate review exists. 6. The system marks the validation step as successful. 7. The system returns control to the review-submission flow so the review can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The related context is not eligible for review. 1. The system marks the validation as failed. 2. The system returns an eligibility error result to the calling use case.  4.1 A duplicate review already exists. 1. The system marks the validation as failed. 2. The system returns a duplicate-review result to the calling use case.  2.1 The related booking or application context cannot be resolved. 1. The system cannot validate eligibility correctly. 2. The system returns an error result to the calling use case.  6.1 A temporary technical issue occurs during validation. 1. The system cannot complete the validation process. 2. The system returns an error result to the calling use case. 3. The Student is asked to try again later. |

## Use Case 157 – (UC-157) Link Review to Booking / Application


| Field | Description |
|---|---|
| Use Case 157 – (UC-157) | Link Review to Booking / Application |
| Related Requirements | FR-070, FR-071, FR-074, FR-096, FR-097 |
| Initiating Actor | System |
| Actor’s Goal | To associate the submitted review with the correct completed booking or finalized application outcome. |
| Participating Actors | Student, Company, Consultant |
| Preconditions | 1. A valid review submission is in progress. 2. The related booking or application context has been identified. 3. The context is eligible for review linkage. |
| Postconditions | 1. The review is linked successfully to the correct booking or application context. 2. The review becomes traceable to its originating interaction or outcome. 3. The linked review can be used for later display and rating calculation. |
| Flow of Events for Main Success Scenario | 1. The system receives a validated review submission. 2. The system identifies the related booking or application record. 3. The system verifies the relationship between the Student and the selected review target. 4. The system associates the review with the correct booking or application context. 5. The system stores the review linkage successfully. 6. The system returns control to the review-submission flow so processing can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The booking or application record cannot be found. 1. The system cannot link the review correctly. 2. The system returns an error result to the calling use case.  3.1 The selected target does not match the eligible review context. 1. The system rejects the linkage attempt. 2. The system returns a linkage-validation error result.  5.1 A temporary technical issue occurs while saving the review linkage. 1. The system cannot complete the link operation. 2. The system returns an error result to the calling use case. |

## Use Case 158 – (UC-158) Recalculate Average Rating


| Field | Description |
|---|---|
| Use Case 158 – (UC-158) | Recalculate Average Rating |
| Related Requirements | FR-071, FR-074, FR-096, FR-097, FR-100 |
| Initiating Actor | System |
| Actor’s Goal | To recalculate the target entity’s average rating after a valid review is submitted. |
| Participating Actors | Company, Consultant |
| Preconditions | 1. A review has been saved successfully. 2. The target Company or Consultant record is identifiable. 3. Rating-calculation rules are available. |
| Postconditions | 1. The average rating is recalculated successfully. 2. The updated average rating becomes available in relevant views. 3. Derived rating indicators are refreshed according to system rules. |
| Flow of Events for Main Success Scenario | 1. The system detects that a valid review was saved successfully. 2. The system identifies the target Company or Consultant. 3. The system retrieves the applicable active review set. 4. The system recalculates the average rating according to the configured rules. 5. The system updates the stored or displayed rating indicators. 6. The system confirms that the recalculation was completed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 No eligible reviews are available for aggregation. 1. The system handles the rating state according to policy for unrated profiles. 2. The system updates the rating display accordingly.  4.1 A temporary technical issue occurs during rating recalculation. 1. The system cannot complete the recalculation normally. 2. The system handles the issue according to data-consistency policy.  5.1 The updated rating indicators cannot be stored successfully. 1. The system detects that the rating refresh did not complete normally. 2. The system handles the issue according to system policy. |

## Use Case 159 – (UC-159) View Own Ratings & Reviews


| Field | Description |
|---|---|
| Use Case 159 – (UC-159) | View Own Ratings & Reviews |
| Related Requirements | FR-071, FR-074, FR-097, FR-100 |
| Initiating Actor | Company, Consultant |
| Actor’s Goal | To view received ratings and reviews associated with the user’s own Company or Consultant profile. |
| Participating Actors | None |
| Preconditions | 1. The Company or Consultant is authenticated. 2. The user has access to the ratings-and-reviews area. 3. Ratings or reviews may exist for the user’s own profile. |
| Postconditions | 1. The user can view the available received ratings and reviews successfully. 2. The user can review reputation-related feedback associated with the own profile. |
| Flow of Events for Main Success Scenario | 1. The Company or Consultant opens the ratings-and-reviews area. 2. The system retrieves the ratings and reviews associated with the user’s own profile. 3. The system displays the available ratings, review content, and summary indicators according to design. 4. The user reviews the displayed feedback successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No ratings or reviews are available. 1. The system displays an empty-state message or equivalent result.  2.2 A temporary technical issue occurs while loading ratings or reviews. 1. The system cannot display the feedback normally. 2. The system shows an error or temporary unavailable message. |

## Use Case 160 – (UC-160) Use Community Forum


| Field | Description |
|---|---|
| Use Case 160 – (UC-160) | Use Community Forum |
| Related Requirements | FR-102, FR-103, FR-104, FR-105, FR-106, FR-107, FR-112, FR-108 |
| Initiating Actor | Student, Company, Consultant |
| Actor’s Goal | To participate in the community forum by viewing content, creating posts or replies, voting on content, and flagging inappropriate content within the platform rules. |
| Participating Actors | Admin |
| Preconditions | 1. The user is authenticated. 2. The user has access to the community forum module. 3. Community content and interaction features are enabled. |
| Postconditions | 1. The user can interact with forum content according to the community rules. 2. Any permitted action such as posting, replying, voting, or flagging is processed successfully. 3. Violating or flagged content may be routed to moderation according to system policy. |
| Flow of Events for Main Success Scenario | 1. The user opens the community forum. 2. The system displays available posts, replies, and interaction controls. 3. The user browses the available content. 4. The user performs a permitted forum action such as creating a post, replying, voting, or flagging content. 5. The system validates the action against the community rules. 6. The system applies the permitted action successfully. 7. The system refreshes the forum content or interaction state. 8. The user continues using the community forum successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The requested forum action violates community rules. 1. The system rejects the action. 2. The system displays the related validation or policy message.  4.1 The user chooses to vote on content. 1. The system continues through the voting flow.  4.2 The user chooses to flag content. 1. The system continues through the flagging flow.  6.1 The flagged content reaches the auto-hide threshold. 1. The system auto-hides the content. 2. The system sends the item to the Admin moderation queue.  2.1 A temporary technical issue occurs while loading forum content. 1. The system cannot display the community forum normally. 2. The system displays an error or temporary unavailable message. |

## Use Case 161 – (UC-161) Validate Community Rules


| Field | Description |
|---|---|
| Use Case 161 – (UC-161) | Validate Community Rules |
| Related Requirements | FR-102, FR-103, FR-104, FR-105, FR-106, FR-107, FR-112, FR-129, FR-130, FR-131 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the requested forum action complies with community rules before the action is accepted. |
| Participating Actors | Student, Company, Consultant |
| Preconditions | 1. The user is authenticated. 2. The user has initiated a forum action. 3. The relevant forum content and action context are available to the system. 4. The community-rule checks are available to the system. |
| Postconditions | 1. The system determines whether the requested forum action is allowed. 2. If valid, the forum action continues. 3. If invalid, the forum action is blocked and the user is informed. |
| Flow of Events for Main Success Scenario | 1. The user initiates a community forum action. 2. The system receives the requested action and its context. 3. The system checks the request against the applicable community rules. 4. The system verifies that the requested action is permitted. 5. The system marks the validation step as successful. 6. The system returns control to the calling use case so the forum action can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The requested action violates a content or interaction rule. 1. The system marks the validation as failed. 2. The system returns a rule-violation result to the calling use case.  3.2 The user attempts to vote on own content. 1. The system detects a self-vote attempt. 2. The system blocks the action according to policy.  2.1 The forum action context cannot be resolved correctly. 1. The system cannot validate the request normally. 2. The system returns an error result to the calling use case.  5.1 A temporary technical issue occurs during rule validation. 1. The system cannot complete the validation process. 2. The system returns an error result to the calling use case. 3. The user is asked to try again later. |

## Use Case 162 – (UC-162) Flag Inappropriate Content


| Field | Description |
|---|---|
| Use Case 162 – (UC-162) | Flag Inappropriate Content |
| Related Requirements | FR-106, FR-107, FR-112, FR-132 |
| Initiating Actor | Student, Company, Consultant |
| Actor’s Goal | To report inappropriate forum content so that the system can track abuse and apply moderation rules when the flag threshold is reached. |
| Participating Actors | Admin |
| Preconditions | 1. The user is authenticated. 2. The user has access to a forum post or reply. 3. The selected content is eligible for flagging. 4. The flagging feature is available. |
| Postconditions | 1. A valid flag is recorded successfully against the selected content. 2. The flagged content remains tracked for moderation purposes. 3. If the valid distinct-flag threshold is reached, the content may be auto-hidden and routed to the Admin queue. |
| Flow of Events for Main Success Scenario | 1. The user opens or views a forum post or reply. 2. The user selects the flag action. 3. The system receives the flag request. 4. The system validates that the flag action is allowed and distinct according to policy. 5. The system records the flag against the selected content. 6. The system checks whether the content has reached the auto-hide threshold. 7. If the threshold is not reached, the content remains visible under current rules. 8. The system confirms that the content was flagged successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The user has already flagged the same content and duplicate flags are not allowed. 1. The system rejects the duplicate flag. 2. The system informs the user that the content was already flagged by that user.  6.1 The content reaches 3 or more distinct valid flags. 1. The system auto-hides the content. 2. The system sends the item to the Admin moderation queue.  5.1 A temporary technical issue occurs while recording the flag. 1. The system cannot complete the flagging action. 2. The system displays an error message and asks the user to try again later. |

## Use Case 163 – (UC-163) Upvote / Downvote Content


| Field | Description |
|---|---|
| Use Case 163 – (UC-163) | Upvote / Downvote Content |
| Related Requirements | FR-104, FR-105 |
| Initiating Actor | Student, Company, Consultant |
| Actor’s Goal | To express a positive or negative reaction to community content through voting, within the voting rules of the forum. |
| Participating Actors | None |
| Preconditions | 1. The user is authenticated. 2. The user is viewing forum content that supports voting. 3. The voting feature is enabled for the selected content. |
| Postconditions | 1. The user’s vote is recorded successfully if valid. 2. The visible vote state or total is refreshed according to system design. 3. Self-voting remains blocked according to policy. |
| Flow of Events for Main Success Scenario | 1. The user views a forum post or reply. 2. The user selects either the upvote or downvote action. 3. The system receives the vote request. 4. The system validates that the vote is allowed for the selected content and user. 5. The system records the vote successfully. 6. The system refreshes the visible vote state or count. 7. The user sees that the vote was applied successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The user attempts to vote on own content. 1. The system blocks the self-vote attempt. 2. The system informs the user that self-voting is not allowed.  4.2 The vote is otherwise invalid under forum policy. 1. The system rejects the vote request. 2. The system displays the related validation message.  5.1 A temporary technical issue occurs while saving the vote. 1. The system cannot complete the voting action. 2. The system displays an error message and asks the user to try again later. |

## Use Case 164 – (UC-164) Auto-Hide Post & Send to Admin Queue [3+ Flags]


| Field | Description |
|---|---|
| Use Case 164 – (UC-164) | Auto-Hide Post & Send to Admin Queue [3+ Flags] |
| Related Requirements | FR-107, FR-112 |
| Initiating Actor | System |
| Actor’s Goal | To automatically hide forum content and send it to the Admin moderation queue after the content receives 3 or more distinct valid flags. |
| Participating Actors | Admin, Student, Company, Consultant |
| Preconditions | 1. Forum content exists and is flaggable. 2. The system tracks valid distinct flags for that content. 3. The content has reached the configured threshold of 3 or more distinct valid flags. 4. The Admin moderation queue is available. |
| Postconditions | 1. The flagged content is auto-hidden successfully. 2. The content is sent to the Admin moderation queue. 3. The content is no longer treated as normally visible forum content under current policy. |
| Flow of Events for Main Success Scenario | 1. The system detects that a piece of forum content has reached 3 or more distinct valid flags. 2. The system verifies that the auto-hide threshold condition is satisfied. 3. The system updates the content visibility to hidden or equivalent moderation-pending state. 4. The system creates or sends a moderation item to the Admin queue. 5. The system confirms that the content was auto-hidden and routed for Admin review. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The recorded flags do not satisfy the valid distinct-flag rule. 1. The system does not auto-hide the content. 2. The content remains under normal visibility rules until the threshold is actually reached.  4.1 The Admin queue cannot be updated because of a temporary technical issue. 1. The system may still hide the content according to policy. 2. The system handles the queueing failure according to moderation-consistency rules.  3.1 The content was already hidden or already queued for moderation. 1. The system avoids duplicating the moderation action. 2. The existing moderation state remains unchanged. |

## Use Case 165 – (UC-165) Use One-to-One Chat


| Field | Description |
|---|---|
| Use Case 165 – (UC-165) | Use One-to-One Chat |
| Related Requirements | FR-109, FR-110, FR-111, FR-112 |
| Initiating Actor | Student, Company, Consultant |
| Actor’s Goal | To exchange real-time direct messages with another user through the platform. |
| Participating Actors | None |
| Preconditions | 1. The user is authenticated. 2. The user has access to the one-to-one chat feature. 3. The intended recipient exists and is reachable within the platform rules. 4. The sender is not blocked from initiating a new conversation with the target user. |
| Postconditions | 1. A one-to-one conversation is opened or continued successfully. 2. Messages exchanged in the conversation are persisted in chat history. 3. The system displays online or offline presence for the conversation context according to availability rules. |
| Flow of Events for Main Success Scenario | 1. The user opens the chat area. 2. The system displays available conversation entries or user targets. 3. The user selects an existing conversation or initiates a new one-to-one chat. 4. The system checks whether the chat can be initiated under the platform rules. 5. The system displays the chat conversation interface. 6. The system displays the online or offline presence of the other user. 7. The user types and sends a message. 8. The system delivers the message in real time through the chat infrastructure. 9. The system persists the message in the conversation history. 10. The conversation remains available for continued messaging. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The sender is blocked from initiating chat. 1. The system blocks the chat initiation attempt. 2. The system informs the sender that a new conversation cannot be started with the selected user.  6.1 Presence information is temporarily unavailable. 1. The system cannot display the current online or offline state reliably. 2. The system continues the chat flow with reduced presence visibility according to policy.  8.1 A temporary technical issue occurs during real-time delivery. 1. The system cannot complete message delivery normally. 2. The system displays an error or retry message according to policy.  9.1 The message cannot be persisted successfully. 1. The system cannot safely finalize the chat message state. 2. The system handles the issue according to messaging-consistency policy. |

## Use Case 166 – (UC-166) Display Online / Offline Presence


| Field | Description |
|---|---|
| Use Case 166 – (UC-166) | Display Online / Offline Presence |
| Related Requirements | FR-110, FR-112 |
| Initiating Actor | System |
| Actor’s Goal | To show whether the other chat participant is currently online or offline so that users can understand current chat availability. |
| Participating Actors | Student, Company, Consultant |
| Preconditions | 1. A user is authenticated and has opened a one-to-one chat context. 2. The relevant chat participant identity is known. 3. Presence-tracking information is available to the system. |
| Postconditions | 1. The system displays the participant’s online or offline presence if available. 2. The chat view reflects the latest known presence state according to system rules. |
| Flow of Events for Main Success Scenario | 1. The user opens a one-to-one chat conversation. 2. The system identifies the other participant in the conversation. 3. The system retrieves the participant’s current or latest known presence state. 4. The system determines the applicable presence label or indicator. 5. The system displays the participant as online or offline in the chat interface. 6. The system refreshes the presence indicator as needed according to chat-session behavior. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 Presence information cannot be retrieved. 1. The system cannot determine the current presence state reliably. 2. The system displays no presence indicator or a fallback state according to policy.  4.1 The presence state changes while the chat is open. 1. The system updates the displayed presence indicator accordingly. 2. The chat continues normally.  5.1 A temporary technical issue occurs while refreshing presence. 1. The system cannot update the indicator normally. 2. The system continues with the last known or fallback presence state according to policy. |

## Use Case 167 – (UC-167) Persist Chat Messages


| Field | Description |
|---|---|
| Use Case 167 – (UC-167) | Persist Chat Messages |
| Related Requirements | FR-111, FR-112 |
| Initiating Actor | System |
| Actor’s Goal | To store one-to-one chat messages in conversation history so that users can access prior messages later. |
| Participating Actors | Student, Company, Consultant |
| Preconditions | 1. A valid one-to-one chat message has been created or received by the system. 2. The sender and recipient conversation context is known. 3. Chat-message storage is available. |
| Postconditions | 1. The chat message is saved successfully in the conversation history. 2. The saved message becomes available in current and future conversation views according to policy. |
| Flow of Events for Main Success Scenario | 1. A user sends a message in a one-to-one chat conversation. 2. The system receives the message data and conversation context. 3. The system validates that the message can be stored in the identified conversation. 4. The system writes the message to conversation history storage. 5. The system associates the message with the correct sender, recipient, and timestamp context. 6. The system confirms that the message was persisted successfully. 7. The system makes the message available in the conversation history view. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The message data is incomplete or invalid for persistence. 1. The system rejects the persistence step. 2. The system handles the issue according to messaging policy.  4.1 A temporary technical issue occurs while storing the message. 1. The system cannot complete message persistence normally. 2. The system handles the issue according to messaging-consistency policy.  7.1 The message is stored but history refresh is delayed. 1. The system eventually synchronizes the conversation history according to system behavior. 2. The conversation remains consistent after refresh. |

## Use Case 168 – (UC-168) Block User from Initiating Chat


| Field | Description |
|---|---|
| Use Case 168 – (UC-168) | Block User from Initiating Chat |
| Related Requirements | FR-112, FR-133, FR-134 |
| Initiating Actor | System |
| Actor’s Goal | To prevent a blocked sender from initiating a new one-to-one conversation with a user who has blocked them. |
| Participating Actors | Student, Company, Consultant |
| Preconditions | 1. A user attempts to initiate a new one-to-one chat. 2. The target user exists in the system. 3. A blocking rule or block record exists that prevents the sender from starting the conversation. |
| Postconditions | 1. The new chat initiation is blocked successfully. 2. No new one-to-one conversation is created for that blocked attempt. 3. The sender is informed according to system policy that the chat cannot be initiated. |
| Flow of Events for Main Success Scenario | 1. A user selects another user to start a new one-to-one chat. 2. The system receives the chat-initiation request. 3. The system checks whether the sender is blocked from initiating a new conversation with the selected target. 4. The system determines that a blocking rule applies. 5. The system prevents creation of the new chat conversation. 6. The system displays a message indicating that the conversation cannot be initiated. 7. The sender remains outside the blocked conversation flow. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 No blocking rule applies. 1. The system determines that chat initiation is allowed. 2. The normal one-to-one chat flow continues.  3.2 The blocking status cannot be resolved because of a temporary technical issue. 1. The system cannot safely determine whether chat initiation is allowed. 2. The system returns an error or safe-fail result according to policy.  5.1 The conversation already exists from a prior allowed state. 1. The system applies the relevant chat-access rules according to policy. 2. The system does not create a duplicate new conversation. |

## Use Case 169 – (UC-169) Use AI Services


| Field | Description |
|---|---|
| Use Case 169 – (UC-169) | Use AI Services |
| Related Requirements | FR-113, FR-114, FR-115, FR-116, FR-117, FR-118, FR-119, FR-120, FR-121 |
| Initiating Actor | Student |
| Actor’s Goal | To use AI-powered assistance for scholarship recommendations, eligibility checking, and chatbot support based on the Student’s profile and context. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has access to the AI services area. 3. The AI feature set is enabled for the account. 4. The system can access the Student’s profile and contextual data needed for AI processing. |
| Postconditions | 1. The Student receives the requested AI output successfully if the request is valid. 2. The AI output is based on the retrieved Student profile and context according to system rules. 3. The Student can continue using the requested AI feature such as chatbot, eligibility checking, or recommendations. |
| Flow of Events for Main Success Scenario | 1. The Student opens the AI services area. 2. The system displays the available AI capabilities. 3. The Student selects an AI service to use. 4. The system retrieves the Student profile and contextual information. 5. The system prepares the input needed for the requested AI service. 6. The system executes the selected AI capability. 7. The system displays the AI response or result to the Student. 8. The Student reviews the returned AI output successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The Student opens the chatbot. 1. The system continues through the chatbot flow with the required disclaimer behavior.  3.2 The Student requests eligibility checking. 1. The system continues through the eligibility-check flow.  3.3 The Student requests scholarship recommendations. 1. The system continues through the recommendation flow.  4.1 The Student profile or context cannot be retrieved. 1. The system cannot continue the AI request normally. 2. The system displays an error or limited-result message according to policy.  6.1 A temporary technical issue occurs during AI processing. 1. The system cannot complete the AI request. 2. The system asks the Student to try again later. |

## Use Case 170 – (UC-170) Retrieve Student Profile & Context


| Field | Description |
|---|---|
| Use Case 170 – (UC-170) | Retrieve Student Profile & Context |
| Related Requirements | FR-113, FR-114, FR-115, FR-116, FR-117, FR-118, FR-119, FR-120, FR-121 |
| Initiating Actor | System |
| Actor’s Goal | To retrieve the Student’s profile data and relevant contextual information so that AI outputs can be personalized and context-aware. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The Student has initiated an AI-service request. 3. The Student profile and related contextual records are available to the system. |
| Postconditions | 1. The system retrieves the Student profile and relevant context successfully if available. 2. The retrieved data becomes available for the requested AI capability. 3. If the data cannot be retrieved, the requested AI flow cannot continue normally. |
| Flow of Events for Main Success Scenario | 1. The Student initiates an AI-service request. 2. The system identifies the Student account and active context. 3. The system retrieves the Student profile information. 4. The system retrieves any additional relevant context needed for the requested AI feature. 5. The system prepares the retrieved data for AI processing. 6. The system returns control to the calling AI use case so the selected feature can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The Student profile is incomplete or partially unavailable. 1. The system retrieves the available profile data according to policy. 2. The system may continue with reduced personalization or return a limited-result message.  4.1 Required contextual information cannot be retrieved. 1. The system cannot prepare the full AI input normally. 2. The system returns an error or limited-context result to the calling use case.  5.1 A temporary technical issue occurs during retrieval. 1. The system cannot complete the retrieval process. 2. The system returns an error result to the calling use case. 3. The Student is asked to try again later. |

## Use Case 171 – (UC-171) Use AI Chatbot (with Disclaimer)


| Field | Description |
|---|---|
| Use Case 171 – (UC-171) | Use AI Chatbot (with Disclaimer) |
| Related Requirements | FR-118, FR-119, FR-120, FR-121 |
| Initiating Actor | Student |
| Actor’s Goal | To ask questions and receive conversational AI assistance through the chatbot, with clear disclosure that the feature is AI-generated. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has access to the AI chatbot feature. 3. The chatbot entry point is available in the system. 4. The Student profile and context can be used for chatbot support if required by design. |
| Postconditions | 1. The Student receives chatbot responses successfully if processing completes normally. 2. The AI disclaimer is displayed according to system rules. 3. Chatbot interaction history may be preserved according to system design. |
| Flow of Events for Main Success Scenario | 1. The Student opens the AI chatbot. 2. The system displays the chatbot interface. 3. The system displays the required AI disclaimer. 4. The Student enters a question or prompt. 5. The system retrieves the Student profile and context if needed. 6. The system processes the chatbot request. 7. The system returns the chatbot response. 8. The system displays the response in the conversation interface. 9. The Student continues interacting with the chatbot if desired. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The Student does not proceed after seeing the disclaimer. 1. The chatbot interaction does not continue. 2. The Student exits or closes the chatbot flow.  6.1 A temporary technical issue occurs during chatbot processing. 1. The system cannot generate the chatbot response normally. 2. The system displays an error or retry message.  8.1 Conversation history cannot be refreshed or displayed correctly. 1. The system shows the available chatbot output according to policy. 2. The interaction continues with reduced session visibility if allowed. |

## Use Case 172 – (UC-172) Check Scholarship Eligibility (Per-Criterion)


| Field | Description |
|---|---|
| Use Case 172 – (UC-172) | Check Scholarship Eligibility (Per-Criterion) |
| Related Requirements | FR-113, FR-114, FR-115, FR-116, FR-117 |
| Initiating Actor | Student |
| Actor’s Goal | To check eligibility for a scholarship on a per-criterion basis and understand whether the Student is eligible, partially eligible, or not eligible. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has selected a scholarship or eligibility-check context. 3. The system can access the Student profile and the scholarship criteria. |
| Postconditions | 1. The system returns an eligibility evaluation for the selected scholarship. 2. The eligibility output shows per-criterion results according to system rules. 3. The Student can review an overall outcome such as Eligible, Partial, or Not eligible. |
| Flow of Events for Main Success Scenario | 1. The Student requests an eligibility check for a scholarship. 2. The system retrieves the Student profile and relevant context. 3. The system retrieves the scholarship criteria. 4. The system compares the Student data against each applicable criterion. 5. The system determines the per-criterion results. 6. The system derives the overall eligibility outcome according to system rules. 7. The system displays the eligibility breakdown and overall result to the Student. 8. The Student reviews the eligibility outcome successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The scholarship criteria cannot be retrieved completely. 1. The system cannot complete the eligibility check normally. 2. The system displays an error or limited-result message according to policy.  2.1 The Student profile is incomplete for eligibility comparison. 1. The system uses the available data according to policy. 2. The system may return a partial-result or insufficient-data outcome.  6.1 A temporary technical issue occurs during eligibility evaluation. 1. The system cannot complete the eligibility check. 2. The system asks the Student to try again later. |

## Use Case 173 – (UC-173) Get Scholarship Recommendations (Match Scores)


| Field | Description |
|---|---|
| Use Case 173 – (UC-173) | Get Scholarship Recommendations (Match Scores) |
| Related Requirements | FR-113, FR-114, FR-115, FR-116, FR-117 |
| Initiating Actor | Student |
| Actor’s Goal | To receive AI-generated scholarship recommendations ranked or presented with match scores based on the Student’s profile and context. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Student has access to the recommendation feature. 3. The system can retrieve the Student profile and relevant context. 4. Scholarship recommendation data is available for evaluation. |
| Postconditions | 1. The system returns scholarship recommendations successfully if processing completes normally. 2. Each recommendation is associated with a match score or similar recommendation indicator according to system design. 3. The Student can review the recommended scholarship set. |
| Flow of Events for Main Success Scenario | 1. The Student requests scholarship recommendations. 2. The system retrieves the Student profile and relevant context. 3. The system retrieves candidate scholarship records for recommendation analysis. 4. The system evaluates the candidate scholarships against the Student profile and context. 5. The system calculates match scores or equivalent recommendation indicators. 6. The system ranks or prepares the recommendation set according to system rules. 7. The system displays the recommended scholarships and their match scores to the Student. 8. The Student reviews the recommendation results successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 No suitable scholarship candidates are available for recommendation. 1. The system returns an empty or low-confidence recommendation result according to policy. 2. The system informs the Student appropriately.  2.1 The Student profile or context is incomplete for strong recommendations. 1. The system uses the available data according to policy. 2. The system may return limited or weaker recommendations.  5.1 A temporary technical issue occurs during recommendation processing. 1. The system cannot complete the recommendation request normally. 2. The system asks the Student to try again later. |

## Use Case 174 – (UC-174) Browse / Search Articles


| Field | Description |
|---|---|
| Use Case 174 – (UC-174) | Browse / Search Articles |
| Related Requirements | FR-122, FR-123, FR-124, FR-128 |
| Initiating Actor | Student |
| Actor’s Goal | To browse and search published articles so that useful educational content can be found and read. |
| Participating Actors | None |
| Preconditions | 1. The Student is authenticated. 2. The Resources Hub is accessible. 3. One or more published articles are available in the system. |
| Postconditions | 1. The system displays article results successfully. 2. The Student can review the article list and continue to read selected content. 3. Search and filter behavior is applied according to the selected criteria. |
| Flow of Events for Main Success Scenario | 1. The Student opens the Resources Hub. 2. The system displays the available published articles. 3. The Student browses the article list or enters a search request. 4. The system applies search and filter criteria. 5. The system retrieves the matching published articles. 6. The system displays the resulting article list to the Student. 7. The Student reviews the displayed articles successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 The Student applies search or filter criteria. 1. The system continues through the search-and-filter flow.  5.1 No articles match the selected criteria. 1. The system returns an empty result set. 2. The system informs the Student that no matching articles were found.  5.2 A temporary technical issue occurs while retrieving article results. 1. The system cannot display the article list normally. 2. The system displays an error or temporary unavailable message.  3.1 The user has a publisher role. 1. The user may access the article publishing flow according to role permissions. |

## Use Case 175 – (UC-175) Search & Filter by Category, Author & Date


| Field | Description |
|---|---|
| Use Case 175 – (UC-175) | Search & Filter by Category, Author & Date |
| Related Requirements | FR-123, FR-124 |
| Initiating Actor | System |
| Actor’s Goal | To apply article search and filter criteria so that the article list is narrowed to relevant content. |
| Participating Actors | Student |
| Preconditions | 1. The Student is authenticated. 2. The Student is in the Resources Hub. 3. The Student has entered a search term, selected one or more filters, or both. 4. Search and filtering controls are available. |
| Postconditions | 1. The system applies the selected search and filter criteria to the article dataset. 2. The result set is narrowed according to the submitted criteria. 3. The filtered article list becomes available for display. |
| Flow of Events for Main Success Scenario | 1. The Student enters a search term or selects one or more filter values. 2. The Student confirms the selected criteria. 3. The system receives the search and filter values. 4. The system applies the criteria to the published articles. 5. The system narrows the result set according to category, author, date, or other configured supported fields. 6. The system prepares the filtered article results. 7. The system returns control to the Browse / Search Articles use case so the results can be displayed. |
| Flow of Events for Extensions (Alternate Scenarios) | 1.1 The Student applies only filters without a keyword search. 1. The system processes the selected filters only. 2. The system narrows the result set accordingly.  1.2 The Student enters only a search term without filters. 1. The system processes the search term only. 2. The system retrieves the matching articles accordingly.  5.1 The selected criteria return no matching articles. 1. The system produces an empty result set. 2. The system returns the no-results outcome to the calling use case.  4.1 A temporary technical issue occurs while applying the criteria. 1. The system cannot complete the article search or filter operation. 2. The system returns an error result to the calling use case. |

## Use Case 176 – (UC-176) Publish Article


| Field | Description |
|---|---|
| Use Case 176 – (UC-176) | Publish Article |
| Related Requirements | FR-125, FR-126, FR-127, FR-128 |
| Initiating Actor | Admin, Company, Consultant |
| Actor’s Goal | To publish an article in the Resources Hub so that platform users can access useful educational content. |
| Participating Actors | None |
| Preconditions | 1. The user is authenticated. 2. The user has a publisher role that is allowed to create articles. 3. The article publishing feature is accessible. 4. The Resources Hub supports article publishing in the current release. |
| Postconditions | 1. A valid article is created successfully. 2. The article is stored with the submitted content and metadata. 3. The article becomes visible according to publication and moderation rules. |
| Flow of Events for Main Success Scenario | 1. The publisher opens the article publishing feature. 2. The system displays the article creation form. 3. The publisher enters the article content and metadata. 4. The publisher submits the article for publication. 5. The system validates the article data. 6. The system creates and stores the article record. 7. The system sets the article visibility according to the applicable publishing rules. 8. The system confirms that the article was published successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The submitted article data is missing or invalid. 1. The system does not publish the article. 2. The system displays the related validation errors.  6.1 A temporary technical issue occurs while saving the article. 1. The system cannot complete the publishing flow. 2. The system displays an error message and asks the user to try again later.  1.1 The user does not have a publisher role. 1. The system blocks access to the publishing feature. 2. The system displays an access-denied or unavailable message. |

## Use Case 177 – (UC-177) Validate Article Data


| Field | Description |
|---|---|
| Use Case 177 – (UC-177) | Validate Article Data |
| Related Requirements | FR-125, FR-128, FR-135, FR-136, FR-137 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the submitted article content and metadata are complete and valid before publication is accepted. |
| Participating Actors | Admin, Company, Consultant |
| Preconditions | 1. A publisher is authenticated. 2. The publisher has initiated the article publishing flow. 3. The article submission data is available to the system. 4. Article-validation rules are available. |
| Postconditions | 1. The system determines whether the submitted article is valid. 2. If valid, the article publishing flow continues. 3. If invalid, the article publishing flow stops and the publisher is informed. |
| Flow of Events for Main Success Scenario | 1. The publisher submits an article for publication. 2. The system receives the article data and metadata. 3. The system checks whether the required article fields are present. 4. The system validates the submitted content and metadata according to the publishing rules. 5. The system confirms that the article data is valid. 6. The system marks the validation step as successful. 7. The system returns control to the Publish Article use case so publication can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 Required article fields are missing. 1. The system marks the validation as failed. 2. The system returns a missing-data result to the calling use case.  4.1 The submitted content or metadata is invalid. 1. The system marks the validation as failed. 2. The system returns a validation-error result to the calling use case.  6.1 A temporary technical issue occurs during validation. 1. The system cannot complete the validation process. 2. The system returns an error result to the calling use case. 3. The publisher is asked to try again later. |

## Use Case 178 – (UC-178) Feature Selected Articles


| Field | Description |
|---|---|
| Use Case 178 – (UC-178) | Feature Selected Articles |
| Related Requirements | FR-127, FR-128 |
| Initiating Actor | Admin |
| Actor’s Goal | To mark selected articles as featured so that important content gains higher visibility in the Resources Hub. |
| Participating Actors | None |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has access to article editorial controls. 3. One or more published articles are available to feature. |
| Postconditions | 1. The selected article is marked as featured successfully. 2. The featured status is reflected in relevant article views according to system design. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the article editorial or management area. 2. The system displays the available published articles. 3. The Admin selects an article to feature. 4. The Admin confirms the featuring action. 5. The system validates that the selected article can be featured. 6. The system updates the article’s featured status. 7. The system refreshes the article-management results. 8. The Admin sees that the article is now featured successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The selected article cannot be featured in its current state. 1. The system blocks the action according to policy. 2. The system informs the Admin that the article cannot be featured.  6.1 A temporary technical issue occurs while updating the featured status. 1. The system cannot complete the feature action. 2. The system displays an error message and asks the Admin to try again later.  3.1 The selected article is already featured. 1. The system detects that the article is already marked as featured. 2. The system may keep the current state unchanged and inform the Admin accordingly. |

## Use Case 179 – (UC-179) Moderate Article Visibility


| Field | Description |
|---|---|
| Use Case 179 – (UC-179) | Moderate Article Visibility |
| Related Requirements | FR-126, FR-127, FR-128 |
| Initiating Actor | Admin |
| Actor’s Goal | To control article visibility so that only appropriate and intended content appears to platform users. |
| Participating Actors | Admin, Company, Consultant |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has access to article visibility and moderation controls. 3. One or more article records are available for moderation or visibility updates. |
| Postconditions | 1. The selected article visibility state is updated successfully according to policy. 2. The article appears or is hidden from reader-facing views according to the applied moderation result. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the article moderation or visibility-management area. 2. The system displays the available article records. 3. The Admin selects an article to moderate or update visibility. 4. The Admin chooses the permitted visibility action. 5. The system validates the requested change. 6. The system applies the visibility update. 7. The system refreshes the article-management results. 8. The Admin sees that the article visibility was updated successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The requested visibility change is not allowed. 1. The system rejects the requested action. 2. The system displays the related moderation or policy error message.  6.1 A temporary technical issue occurs while applying the visibility update. 1. The system cannot complete the moderation step. 2. The system asks the Admin to try again later.  3.1 The selected article cannot be found or retrieved. 1. The system cannot continue the moderation flow normally. 2. The system displays an error or unavailable message. |

## Use Case 180 – (UC-180) Manage Notifications


| Field | Description |
|---|---|
| Use Case 180 – (UC-180) | Manage Notifications |
| Related Requirements | FR-140, FR-141, FR-142, FR-143, FR-144, FR-145, FR-146, FR-147, FR-148, FR-149, FR-150, FR-138, FR-139, FR-152 |
| Initiating Actor | Student, Company, Consultant, Admin |
| Actor’s Goal | To receive, view, and manage event-driven notifications generated by the platform through the configured delivery channels. |
| Participating Actors | Email / Notification Service |
| Preconditions | 1. The user is authenticated. 2. The user has access to the notifications area. 3. A notification-triggering event exists or a notification record already exists for the user. 4. The configured delivery channel is available according to system settings and user context. |
| Postconditions | 1. A notification is created and delivered successfully if the trigger and delivery steps complete normally. 2. The notification becomes visible in the user’s notification area according to system rules. 3. The user may mark the notification as read if desired. 4. If the initiating actor is Admin, the user may trigger a broadcast announcement according to permissions. |
| Flow of Events for Main Success Scenario | 1. A notification-triggering event occurs in the system. 2. The system determines the affected recipient or recipients. 3. The system creates a notification record. 4. The system determines the configured delivery channel for the notification. 5. The system delivers the notification through the configured channel. 6. The recipient user opens the notifications area. 7. The system displays the available notifications. 8. The user reviews the notification successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The user later marks the notification as read. 1. The system continues through the mark-as-read flow.  5.1 Delivery through the configured channel fails. 1. The system handles the failure according to notification policy. 2. The notification record remains available according to system rules.  1.1 Admin initiates a broadcast announcement. 1. The system continues through the broadcast-announcement flow.  7.1 A temporary technical issue occurs while loading notifications. 1. The system cannot display the notification area normally. 2. The system shows an error or temporary unavailable message. |

## Use Case 181 – (UC-181) Create Notification Record


| Field | Description |
|---|---|
| Use Case 181 – (UC-181) | Create Notification Record |
| Related Requirements | FR-140, FR-181, FR-182, FR-183, FR-184, FR-185, FR-186, FR-187, FR-188, FR-189 |
| Initiating Actor | System |
| Actor’s Goal | To create a persistent notification record when a valid platform event triggers a notification. |
| Participating Actors | Student, Company, Consultant, Admin |
| Preconditions | 1. A valid notification-triggering event has occurred. 2. The system can identify the intended recipient or recipients. 3. Notification-record storage is available. |
| Postconditions | 1. A notification record is created successfully. 2. The record is associated with the correct recipient or recipients. 3. The created record becomes available for channel delivery and later notification display. |
| Flow of Events for Main Success Scenario | 1. The system detects a notification-triggering event. 2. The system identifies the recipient or recipients affected by the event. 3. The system prepares the notification content and metadata. 4. The system creates the notification record. 5. The system stores the notification successfully. 6. The system returns control to the calling notification flow so delivery can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The intended recipient cannot be resolved correctly. 1. The system cannot create the notification normally. 2. The system returns an error or unresolved-recipient result according to policy.  3.1 The notification content cannot be prepared correctly. 1. The system cannot finalize the notification record normally. 2. The system returns an error result to the calling flow.  5.1 A temporary technical issue occurs while storing the notification record. 1. The system cannot complete record creation. 2. The system returns an error result to the calling use case. |

## Use Case 182 – (UC-182) Deliver via Configured Channel


| Field | Description |
|---|---|
| Use Case 182 – (UC-182) | Deliver via Configured Channel |
| Related Requirements | FR-140, FR-141, FR-142, FR-143, FR-144, FR-145, FR-146, FR-147, FR-148, FR-149, FR-150, FR-138, FR-152 |
| Initiating Actor | System |
| Actor’s Goal | To deliver a created notification through the configured channel such as in-app and/or email. |
| Participating Actors | Email / Notification Service |
| Preconditions | 1. A notification record already exists. 2. The intended delivery channel is known. 3. The delivery mechanism or service is available. |
| Postconditions | 1. The notification is delivered successfully through the configured channel if delivery succeeds. 2. The delivery outcome is recorded or completed according to system policy. 3. The recipient can access the notification through the applicable channel behavior. |
| Flow of Events for Main Success Scenario | 1. The system receives a created notification record ready for delivery. 2. The system determines the configured delivery channel for the notification. 3. The system prepares the channel-specific delivery payload. 4. The system sends the notification through the configured channel or service. 5. The delivery channel processes the notification request. 6. The system receives the delivery result. 7. The system confirms that delivery was completed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No valid delivery channel is configured for the notification. 1. The system cannot deliver the notification normally. 2. The system handles the issue according to notification policy.  4.1 The delivery service rejects or fails the request. 1. The system cannot complete delivery successfully. 2. The system handles the failure according to retry or fallback policy.  6.1 A temporary technical issue occurs while confirming delivery. 1. The system cannot determine the final delivery outcome reliably. 2. The system handles the issue according to delivery-consistency policy. |

## Use Case 183 – (UC-183) Mark Notification as Read


| Field | Description |
|---|---|
| Use Case 183 – (UC-183) | Mark Notification as Read |
| Related Requirements | FR-140, FR-141, FR-149, FR-150, FR-151 |
| Initiating Actor | Student, Company, Consultant, Admin |
| Actor’s Goal | To mark a received notification as read so that the notification state reflects that the user has already reviewed it. |
| Participating Actors | None |
| Preconditions | 1. The user is authenticated. 2. A notification record exists and is visible to the user. 3. The notification is eligible to be marked as read. |
| Postconditions | 1. The selected notification is marked as read successfully. 2. The notification state is updated in the user’s notification view. 3. The system reflects the read status according to design. |
| Flow of Events for Main Success Scenario | 1. The user opens the notifications area. 2. The system displays the available notification records. 3. The user selects a notification to review. 4. The user marks the notification as read. 5. The system updates the notification state to read. 6. The system refreshes the notification view. 7. The user sees that the notification is now marked as read. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The notification cannot be updated because of a temporary technical issue. 1. The system cannot complete the read-state update. 2. The system displays an error message and asks the user to try again later.  4.1 The notification is already marked as read. 1. The system keeps the current state unchanged. 2. The user continues viewing the notification list normally. |

## Use Case 184 – (UC-184) Send Broadcast Announcement


| Field | Description |
|---|---|
| Use Case 184 – (UC-184) | Send Broadcast Announcement |
| Related Requirements | FR-147, FR-148, FR-149, FR-150 |
| Initiating Actor | Admin |
| Actor’s Goal | To send a broadcast announcement to all users or to a filtered group of users through the platform’s notification system. |
| Participating Actors | Email / Notification Service, Student, Company, Consultant |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has permission to send broadcast announcements. 3. The broadcast-notification feature is accessible. 4. A valid target audience can be resolved as all users or filtered user groups. |
| Postconditions | 1. Broadcast notification records are created for the target recipients successfully if valid. 2. The broadcast announcement is delivered through the configured channel according to system policy. 3. The targeted recipients can view or receive the announcement. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the broadcast-announcement feature. 2. The system displays the announcement form and audience controls. 3. The Admin enters the announcement content. 4. The Admin selects the target audience as all users or a filtered user group. 5. The Admin submits the broadcast request. 6. The system validates the announcement data and audience selection. 7. The system creates notification records for the targeted recipients. 8. The system delivers the broadcast announcement through the configured channel. 9. The system confirms that the broadcast was sent successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 6.1 The announcement content or audience selection is invalid. 1. The system rejects the broadcast request. 2. The system displays the related validation errors.  7.1 The notification records cannot be created for one or more recipients. 1. The system handles the issue according to partial-delivery or broadcast policy. 2. The Admin is informed according to system behavior.  8.1 Delivery through the configured channel fails. 1. The system handles the delivery failure according to notification policy. 2. The Admin is informed according to system behavior.  9.1 A temporary technical issue occurs during broadcast processing. 1. The system cannot complete the broadcast flow normally. 2. The system displays an error message and asks the Admin to try again later. |

## Use Case 185 – (UC-185) Administer Dashboard


| Field | Description |
|---|---|
| Use Case 185 – (UC-185) | Administer Dashboard |
| Related Requirements | FR-153, FR-154, FR-155, FR-156, FR-157, FR-158, FR-159 |
| Initiating Actor | Admin |
| Actor’s Goal | To access the administrative control area in order to review pending requests, manage user lifecycle actions, control platform content, and issue onboarding decisions. |
| Participating Actors | Company, Consultant, Student |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has permission to access the administrative dashboard. 3. Administrative data such as users, pending requests, and content records are available to the system. |
| Postconditions | 1. The Admin can view the current administrative workload successfully. 2. The Admin can continue to pending-request review, user lifecycle actions, and platform content management. 3. Affected users may be notified when administrative decisions are applied. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the administrative dashboard. 2. The system verifies the Admin’s access rights. 3. The system loads the dashboard overview and available administrative controls. 4. The system displays pending requests, user-related records, and available management actions. 5. The Admin reviews the dashboard information. 6. The Admin selects the required administrative function. 7. The system continues to the selected management flow. 8. The Admin performs the desired administrative action successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 4.1 Pending onboarding requests exist. 1. The system displays the pending requests in the dashboard. 2. The Admin may continue to the approval or rejection flow.  6.1 The Admin chooses to manage platform content. 1. The system continues to the platform-content management flow.  6.2 The Admin chooses to manage user account status. 1. The system continues to the user-lifecycle management flow.  3.1 A temporary technical issue occurs while loading the dashboard. 1. The system cannot display the dashboard normally. 2. The system shows an error or temporary unavailable message. |

## Use Case 186 – (UC-186) View Pending Requests & Users


| Field | Description |
|---|---|
| Use Case 186 – (UC-186) | View Pending Requests & Users |
| Related Requirements | FR-153, FR-154, FR-155, FR-156 |
| Initiating Actor | System |
| Actor’s Goal | To present pending onboarding requests and user records to the Admin so that administrative decisions can be made efficiently. |
| Participating Actors | Admin |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has opened the administrative dashboard. 3. The relevant request and user data are available to the system. |
| Postconditions | 1. The current pending requests and user records are displayed successfully. 2. The Admin can select a specific request or user for further action. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the administrative dashboard. 2. The system retrieves pending onboarding requests and user records. 3. The system organizes the data for administrative display. 4. The system displays pending requests and user information to the Admin. 5. The Admin reviews the displayed records. 6. The Admin selects a request or user for further management if needed. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No pending requests currently exist. 1. The system displays the available user records and an empty-state message for pending requests.  2.2 No relevant user records can be retrieved. 1. The system cannot display the user list normally. 2. The system shows an error or unavailable message.  4.1 A temporary technical issue occurs while loading records. 1. The system cannot display the records normally. 2. The system returns an error or temporary unavailable result. |

## Use Case 187 – (UC-187) Manage Platform Content


| Field | Description |
|---|---|
| Use Case 187 – (UC-187) | Manage Platform Content |
| Related Requirements | FR-186 |
| Initiating Actor | Admin |
| Actor’s Goal | To manage platform content such as listings, categories, and articles so that platform information remains accurate, governed, and organized. |
| Participating Actors | None |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has access to platform-content management controls. 3. Content records such as listings, categories, or articles are available to the system. |
| Postconditions | 1. The selected content-management action is applied successfully if valid. 2. The updated content state is reflected in relevant platform views according to policy. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the platform-content management area. 2. The system displays the available content records and management controls. 3. The Admin selects a content item or content type to manage. 4. The Admin chooses the desired content-management action. 5. The system validates the requested action. 6. The system applies the content update successfully. 7. The system refreshes the displayed content-management results. 8. The Admin sees that the platform content was updated successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The requested content-management action is not allowed. 1. The system rejects the requested action. 2. The system displays the related validation or policy message.  6.1 A temporary technical issue occurs while applying the content update. 1. The system cannot complete the content-management action. 2. The system asks the Admin to try again later.  3.1 The selected content record cannot be found. 1. The system cannot continue the management flow normally. 2. The system displays an error or unavailable message. |

## Use Case 188 – (UC-188) Manage User Account Status


| Field | Description |
|---|---|
| Use Case 188 – (UC-188) | Manage User Account Status |
| Related Requirements | FR-183, FR-184, FR-185 |
| Initiating Actor | Admin |
| Actor’s Goal | To manage user lifecycle actions such as activate, suspend, deactivate, or delete according to administrative policy. |
| Participating Actors | Company, Consultant, Student |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has access to user-lifecycle management controls. 3. The selected target user exists in the system. 4. The requested lifecycle action is available for administrative use. |
| Postconditions | 1. The selected user account status is updated successfully if the action is valid. 2. The updated account state becomes effective according to policy. 3. The affected user may be notified according to system behavior. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the user-management area or selects a user from the dashboard. 2. The system displays the selected user details and available lifecycle actions. 3. The Admin chooses a lifecycle action such as activate, suspend, deactivate, or delete. 4. The Admin confirms the requested action. 5. The system validates that the action is allowed for the selected user and current account state. 6. The system updates the user account status. 7. The system stores the updated lifecycle state successfully. 8. The system confirms that the user account status was updated successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The requested lifecycle action is not allowed for the selected account state. 1. The system rejects the action. 2. The system displays the related validation or policy message.  6.1 A temporary technical issue occurs while updating the account status. 1. The system cannot complete the lifecycle update. 2. The system asks the Admin to try again later.  3.1 The selected user record cannot be retrieved. 1. The system cannot continue the account-status management flow normally. 2. The system displays an error or unavailable message. |

## Use Case 189 – (UC-189) Approve / Reject Onboarding Request


| Field | Description |
|---|---|
| Use Case 189 – (UC-189) | Approve / Reject Onboarding Request |
| Related Requirements | FR-153, FR-154, FR-155, FR-158 |
| Initiating Actor | Admin |
| Actor’s Goal | To review and approve or reject pending Company, Consultant, or upgrade onboarding requests. |
| Participating Actors | Company, Consultant, Student |
| Preconditions | 1. The Admin is authenticated. 2. A pending onboarding request exists in the system. 3. The Admin has permission to review and decide on onboarding requests. 4. The request details are accessible to the Admin. |
| Postconditions | 1. The onboarding request is updated with the Admin’s decision successfully. 2. If approved, the target role or access change becomes effective according to system rules. 3. If rejected, the request remains unapproved and the affected access is not activated. 4. The affected user is notified of the decision. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the pending onboarding requests area. 2. The system displays pending Company, Consultant, or upgrade requests. 3. The Admin selects a request to review. 4. The system displays the request details and current pending status. 5. The Admin reviews the request content. 6. The Admin selects either approve or reject. 7. The system validates that the request is still pending and eligible for decision. 8. The system records the Admin’s decision. 9. The system updates the request and related account state according to the decision. 10. The system notifies the affected user of the decision. 11. The system confirms that the onboarding request was processed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 7.1 The request is no longer in pending state. 1. The system rejects the decision attempt. 2. The system informs the Admin that the request can no longer be processed in the current flow.  8.1 A temporary technical issue occurs while saving the decision. 1. The system cannot complete the onboarding decision update. 2. The system displays an error message and asks the Admin to try again later.  10.1 Notification delivery to the affected user fails. 1. The system preserves the saved decision if it was stored successfully. 2. The system handles the notification failure according to policy. |

## Use Case 190 – (UC-190) Notify Affected User of Decision


| Field | Description |
|---|---|
| Use Case 190 – (UC-190) | Notify Affected User of Decision |
| Related Requirements | FR-185 |
| Initiating Actor | System |
| Actor’s Goal | To inform the affected Company, Consultant, or Student user that an administrative onboarding decision has been made بشأن الطلب. |
| Participating Actors | Company, Consultant, Student |
| Preconditions | 1. An Admin onboarding decision has been saved successfully. 2. The affected user can be identified from the request record. 3. Notification delivery is available according to system configuration. |
| Postconditions | 1. The affected user is notified of the decision if delivery succeeds. 2. The notification outcome is completed or handled according to system policy. |
| Flow of Events for Main Success Scenario | 1. The system detects that an onboarding decision was saved successfully. 2. The system identifies the affected user from the related request. 3. The system prepares the decision notification content. 4. The system sends the notification through the configured delivery mechanism. 5. The affected user receives the decision notification. 6. The system confirms that the notification step was completed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The affected user cannot be resolved correctly from the request. 1. The system cannot complete notification targeting normally. 2. The system handles the issue according to system policy.  4.1 Notification delivery fails. 1. The system cannot deliver the notification successfully. 2. The system logs or handles the failure according to notification policy.  3.1 A temporary technical issue occurs while preparing the notification. 1. The system cannot complete the notification-preparation step. 2. The system returns an error or failure result according to policy. |

## Use Case 191 – (UC-191) Moderate Content and Users


| Field | Description |
|---|---|
| Use Case 191 – (UC-191) | Moderate Content and Users |
| Related Requirements | FR-187, FR-208, FR-160 |
| Initiating Actor | Admin |
| Actor’s Goal | To review flagged or escalated community content and apply moderation actions to content or users according to platform policy. |
| Participating Actors | None |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has access to moderation tools. 3. One or more flagged or escalated content items exist in the moderation queue. |
| Postconditions | 1. The selected moderation case is reviewed successfully. 2. A moderation action is recorded for the affected content or user. 3. Auto-hidden content may be restored if appropriate. 4. A repeat offender may be suspended according to moderation policy. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the moderation area. 2. The system displays flagged or escalated content items. 3. The Admin selects an item to review. 4. The system displays the content details and moderation context. 5. The Admin reviews the flagged content or related user behavior. 6. The Admin chooses the appropriate moderation action according to policy. 7. The system records the moderation action. 8. The system updates the content or user state according to the selected action. 9. The system confirms that the moderation action was completed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No flagged or escalated content is available. 1. The system displays an empty-state message for the moderation queue.  6.1 The reviewed content was auto-hidden and may be restored. 1. The system continues through the restore-content flow if the Admin chooses that action.  6.2 The reviewed user is identified as a repeat offender. 1. The system continues through the suspend-user flow if the Admin chooses that action.  7.1 A temporary technical issue occurs while saving the moderation action. 1. The system cannot complete the moderation update. 2. The system displays an error message and asks the Admin to try again later. |

## Use Case 192 – (UC-192) Display Flagged / Escalated Content


| Field | Description |
|---|---|
| Use Case 192 – (UC-192) | Display Flagged / Escalated Content |
| Related Requirements | FR-187, FR-208, FR-177 |
| Initiating Actor | System |
| Actor’s Goal | To present flagged community posts, replies, reviews, or escalated content items to the Admin for moderation review. |
| Participating Actors | Admin |
| Preconditions | 1. The Admin is authenticated. 2. The moderation area is accessible. 3. Flagged or escalated content records are available to the system. |
| Postconditions | 1. The flagged or escalated content list is displayed successfully. 2. The Admin can select a content item for detailed moderation review. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the moderation dashboard or queue. 2. The system retrieves flagged and escalated content items. 3. The system organizes the items for moderation display. 4. The system displays the available content items to the Admin. 5. The Admin reviews the displayed queue. 6. The Admin selects one item for further moderation if needed. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No flagged or escalated content exists. 1. The system displays an empty-state message.  2.2 The moderation queue cannot be retrieved correctly. 1. The system cannot display the queue normally. 2. The system displays an error or unavailable message.  4.1 A temporary technical issue occurs while loading the moderation list. 1. The system cannot complete the display flow normally. 2. The system returns an error or temporary unavailable result. |

## Use Case 193 – (UC-193) Record Moderation Action


| Field | Description |
|---|---|
| Use Case 193 – (UC-193) | Record Moderation Action |
| Related Requirements | FR-187, FR-208 |
| Initiating Actor | System |
| Actor’s Goal | To record the moderation decision applied to a flagged content item or offending user so that moderation actions remain traceable. |
| Participating Actors | Admin |
| Preconditions | 1. The Admin is authenticated. 2. A moderation action has been selected for a flagged content item or related user. 3. The content or user record is identifiable to the system. 4. Moderation-history storage is available. |
| Postconditions | 1. The moderation action is stored successfully. 2. The recorded moderation result becomes traceable for later review or audit. 3. The associated content or user state can be updated consistently. |
| Flow of Events for Main Success Scenario | 1. The Admin selects a moderation action for a flagged item or user. 2. The system receives the selected moderation outcome. 3. The system identifies the affected content or user record. 4. The system prepares the moderation entry details. 5. The system stores the moderation action successfully. 6. The system confirms that the moderation record was saved. 7. The system returns control to the moderation flow so state updates can continue. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The affected content or user record cannot be resolved. 1. The system cannot save the moderation action correctly. 2. The system returns an error result to the calling use case.  5.1 A temporary technical issue occurs while recording the moderation action. 1. The system cannot complete the save operation. 2. The system returns an error result to the calling use case.  4.1 The moderation entry details are incomplete. 1. The system cannot create a valid moderation record. 2. The system handles the issue according to moderation-consistency policy. |

## Use Case 194 – (UC-194) Restore Auto-Hidden Content


| Field | Description |
|---|---|
| Use Case 194 – (UC-194) | Restore Auto-Hidden Content |
| Related Requirements | FR-187, FR-208 |
| Initiating Actor | Admin |
| Actor’s Goal | To restore content that was automatically hidden after flag accumulation when the Admin determines that the content should remain visible. |
| Participating Actors | None |
| Preconditions | 1. The Admin is authenticated. 2. The selected content exists in the moderation queue. 3. The content is currently in an auto-hidden state. 4. The Admin has permission to restore moderated content. |
| Postconditions | 1. The auto-hidden content is restored successfully if the action is allowed. 2. The content returns to its visible state according to system rules. 3. The restoration decision is reflected in moderation records. |
| Flow of Events for Main Success Scenario | 1. The Admin opens a flagged content item from the moderation queue. 2. The system displays that the content is auto-hidden. 3. The Admin reviews the content and moderation context. 4. The Admin selects the restore action. 5. The system validates that the content can be restored. 6. The system updates the content visibility state from auto-hidden to restored or visible. 7. The system records the restoration outcome. 8. The system confirms that the content was restored successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 The content is not actually in an auto-hidden state. 1. The system rejects the restore action. 2. The system informs the Admin that restore is not applicable in the current state.  6.1 A temporary technical issue occurs while restoring the content. 1. The system cannot complete the restore operation. 2. The system asks the Admin to try again later.  3.1 The content is still considered policy-violating after review. 1. The Admin does not restore the content. 2. The system keeps the current hidden or moderation state unchanged. |

## Use Case 195 – (UC-195) Suspend Offending User


| Field | Description |
|---|---|
| Use Case 195 – (UC-195) | Suspend Offending User |
| Related Requirements | FR-187, FR-208 |
| Initiating Actor | Admin |
| Actor’s Goal | To suspend a repeat offending user when moderation history or current behavior satisfies the platform’s suspension policy. |
| Participating Actors | None |
| Preconditions | 1. The Admin is authenticated. 2. The selected moderation case is linked to a user account. 3. The user meets the repeat-offender or suspension condition according to policy. 4. The Admin has permission to suspend user accounts. |
| Postconditions | 1. The offending user account is suspended successfully if the action is allowed. 2. The suspension outcome is reflected in the user account state. 3. The suspension decision is recorded in moderation history or user governance records. |
| Flow of Events for Main Success Scenario | 1. The Admin reviews a moderation case linked to a user account. 2. The system displays the relevant moderation history and current offense context. 3. The Admin determines that the user qualifies as a repeat offender under policy. 4. The Admin selects the suspend-user action. 5. The system validates that the suspension action is allowed. 6. The system updates the user account to suspended status. 7. The system records the suspension outcome. 8. The system confirms that the offending user was suspended successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The user does not meet the repeat-offender condition. 1. The system rejects the suspension action. 2. The system informs the Admin that suspension is not justified under current policy.  6.1 A temporary technical issue occurs while suspending the user. 1. The system cannot complete the suspension update. 2. The system asks the Admin to try again later.  5.1 The selected account cannot be suspended in its current state. 1. The system blocks the requested action. 2. The system displays the related validation or policy message. |

## Use Case 196 – (UC-196) Configure Financial Rules & Portal Profit Share


| Field | Description |
|---|---|
| Use Case 196 – (UC-196) | Configure Financial Rules & Portal Profit Share |
| Related Requirements | FR-201, FR-202, FR-203, FR-204, FR-207, FR-208, FR-190, FR-191, FR-210, FR-211, FR-199, FR-203, FR-206, FR-207, FR-200, FR-200, FR-201, FR-202, FR-203, FR-204, FR-207, FR-208, FR-163, FR-164, FR-165, FR-166, FR-170, FR-172 |
| Initiating Actor | Admin |
| Actor’s Goal | To create, update, activate, deactivate, archive, and manage financial configuration rules for Consultant booking payments and Company review-service payments from the Admin Dashboard so that platform fee and portal profit-share behavior is controlled centrally without code or direct database changes. |
| Participating Actors | None |
| Preconditions | 1. The Admin is authenticated. 2. The Admin is authorized to manage financial configuration rules. 3. The financial configuration area is accessible from the Admin Dashboard. 4. The system supports separate rule sets for Consultant payment flows and Company review-service payment flows. |
| Postconditions | 1. A financial configuration rule is created or updated successfully if the submitted values are valid. 2. The rule is stored with the selected status such as Draft, Active, Inactive, or Archived. 3. If activated, the rule becomes the active effective configuration for its payment type according to the effective-date rules. 4. The system preserves previously stored configuration if the new save or activation fails. 5. The configuration becomes available for future payment-calculation and settlement logic only, while historical transactions remain unchanged through stored financial snapshots. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the financial configuration area from the Admin Dashboard. 2. The system displays the current active configuration and available rule-management actions. 3. The Admin selects the payment type to manage as either Consultant payments or Company review-service payments. 4. The Admin chooses to create a new rule or edit an existing eligible rule version. 5. The Admin enters or updates the platform service fee, portal profit-share percentage, effective dates, and status information. 6. The Admin requests a calculation preview or simulation using an example gross transaction amount. 7. The system displays the preview breakdown, including gross amount, platform service fee, portal profit-share percentage, portal profit-share amount, total platform retention, and resulting net payout. 8. The Admin reviews the preview and submits the configuration for save or activation. 9. The system validates the configuration values, business rules, and payment-type constraints. 10. The system stores the rule successfully. 11. If the Admin selected activation, the system applies the activation state for that payment type and ensures only one active rule set exists for that payment type at a time. 12. The system records the configuration action in the audit trail. 13. The system confirms that the financial configuration was processed successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 The Admin selects Consultant payment rules. 1. The system loads the Consultant financial rule context. 2. The Admin manages Consultant fee and portal-share settings separately from Company settings.  3.2 The Admin selects Company review-service payment rules. 1. The system loads the Company financial rule context. 2. The Admin manages Company fee and portal-share settings separately from Consultant settings.  6.1 The Admin uses simulation before activation. 1. The system calculates the sample transaction outcome using the entered values. 2. The Admin reviews the expected fee, portal share, and net payout before proceeding.  9.1 The submitted values are invalid, negative, out of policy, or produce an invalid net payout. 1. The system rejects the configuration. 2. The system displays the related validation errors. 3. The current stored configuration remains unchanged.  11.1 Another active rule already exists for the same payment type. 1. The system prevents multiple active rules for that payment type. 2. The system requires replacement, deactivation, or activation handling according to policy.  10.1 A temporary technical issue occurs while saving or activating the rule. 1. The system cannot complete the configuration update. 2. The system preserves the previous configuration state. 3. The system displays an error message and asks the Admin to try again later.  4.1 The Admin attempts to delete a rule already used in recorded transactions. 1. The system blocks deletion. 2. The system allows archive or deactivate behavior instead according to policy. |

## Use Case 197 – (UC-197) Display Current Configuration


| Field | Description |
|---|---|
| Use Case 197 – (UC-197) | Display Current Configuration |
| Related Requirements | FR-190, FR-191, FR-201, FR-202, FR-206, FR-207, FR-167, FR-169, FR-173, FR-174 |
| Initiating Actor | System |
| Actor’s Goal | To display the currently active financial configuration and related historical configuration records so that the Admin can understand which rule is in effect and review prior rule versions. |
| Participating Actors | Admin |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has opened the financial configuration area. 3. Financial configuration records are available in the system. |
| Postconditions | 1. The current active financial rule for the selected payment type is displayed successfully. 2. Historical financial rule versions are available for review according to system design. 3. Search and filter controls are available for efficient configuration management. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the financial configuration area. 2. The system retrieves the active Consultant and Company financial rules. 3. The system retrieves historical financial rule records relevant to the selected view. 4. The system displays the current active configuration values and related metadata. 5. The system displays available historical rule versions. 6. The system provides search and filter controls by payment type, status, effective date, and last updated date. 7. The Admin reviews the displayed configuration records successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 No active rule exists for one payment type. 1. The system displays the absence of an active configuration for that payment type. 2. The system may indicate fallback behavior according to policy if supported.  3.1 No historical rule versions are available. 1. The system displays only the current active configuration or an empty historical view.  6.1 The Admin applies search or filter criteria. 1. The system narrows the displayed rule list according to the selected criteria. 2. The Admin reviews the filtered result set.  2.2 A temporary technical issue occurs while retrieving configuration records. 1. The system cannot display the configuration area normally. 2. The system shows an error or temporary unavailable message. |

## Use Case 198 – (UC-198) Validate Configuration Values


| Field | Description |
|---|---|
| Use Case 198 – (UC-198) | Validate Configuration Values |
| Related Requirements | FR-204, FR-205, FR-207, FR-210, FR-211, FR-199, FR-204, US-136, US-151, FR-168, FR-175 |
| Initiating Actor | System |
| Actor’s Goal | To verify that the submitted financial configuration values are valid, policy-compliant, and safe to save or activate for the selected payment type. |
| Participating Actors | Admin |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has entered or updated financial rule data. 3. The selected payment type is known. 4. The system has access to the applicable financial validation rules and current active-rule state. |
| Postconditions | 1. The system determines whether the submitted configuration is valid. 2. If valid, the save or activation flow continues. 3. If invalid, the configuration is rejected and the existing stored configuration remains unchanged. |
| Flow of Events for Main Success Scenario | 1. The Admin submits a financial configuration for save or activation. 2. The system receives the submitted payment type, fee values, portal-share percentage, status, and effective-date values. 3. The system validates that the entered fee and portal-share values are not invalid, negative, or out of policy. 4. The system validates that the resulting net payout is valid according to platform rules. 5. The system validates the effective-date logic for the rule. 6. The system checks whether another active rule already exists for the same payment type. 7. The system confirms that the submitted configuration satisfies all validation conditions. 8. The system marks the validation step as successful. 9. The system returns control to the financial-configuration flow so the rule can be saved or activated. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 A fee or portal-share value is invalid, negative, or outside allowed policy limits. 1. The system marks the validation as failed. 2. The system returns a validation-error result to the calling use case.  4.1 The resulting net payout would be invalid. 1. The system blocks the configuration from being saved. 2. The system returns a payout-validation error result.  5.1 The effective dates are invalid or inconsistent. 1. The system rejects the submitted timing rules. 2. The system returns an effective-date validation error.  6.1 Another active rule already exists for the same payment type. 1. The system prevents multiple active rules for that payment type. 2. The system returns an activation-conflict result.  7.1 A temporary technical issue occurs during validation. 1. The system cannot complete the validation process. 2. The system returns an error result to the calling use case. 3. The Admin is asked to try again later. |

## Use Case 199 – (UC-199) Apply Updated Rules to Future Payments


| Field | Description |
|---|---|
| Use Case 199 – (UC-199) | Apply Updated Rules to Future Payments |
| Related Requirements | FR-201, FR-203, FR-205, FR-206, FR-208, FR-200, FR-209, FR-201, FR-202, FR-210, FR-209, FR-210, FR-211, FR-199, FR-205, FR-206, FR-207, FR-171, FR-176, FR-197, FR-198 |
| Initiating Actor | System |
| Actor’s Goal | To make the newly activated financial rule available to future eligible payment calculations while preserving historical transactions through stored transaction-level financial snapshots. |
| Participating Actors | None |
| Preconditions | 1. A financial configuration rule has been saved and activated successfully for a payment type. 2. The rule is active and effective according to its timing logic. 3. Payment-calculation and settlement components can access active financial configuration data. |
| Postconditions | 1. New eligible Consultant or Company payable transactions use the active effective rule automatically. 2. Existing recorded transactions remain unchanged and continue to use their stored rule snapshots. 3. Refund and settlement adjustments use the financial rule snapshot associated with the original transaction. 4. Authorized Admin reporting can display the applied fee, portal share, and net payout values. |
| Flow of Events for Main Success Scenario | 1. The system detects that a financial rule has been activated successfully for a payment type. 2. The system exposes the active configuration to the relevant payment-calculation and settlement components. 3. The system notifies the relevant internal financial-processing components that a new rule is active. 4. When a new eligible Consultant booking payment or Company review-service payment is created or calculated, the system identifies the payment type. 5. The system selects the rule set that is both active and effective at the time of transaction calculation. 6. The system applies the active financial configuration automatically to the new transaction. 7. The system records the exact fee values, portal-share values, and resulting payout values used for that transaction as a rule snapshot. 8. The system preserves that snapshot for future audit, refund, and settlement behavior. 9. The system continues using the transaction snapshot even if the Admin later changes the active rule configuration. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 No future-dated rule has yet become active and fallback configuration is supported. 1. The system applies the default fallback configuration for that payment type according to policy. 2. Financial calculation remains possible.  6.1 The transaction is an already-recorded historical transaction. 1. The system does not retroactively replace the historical rule values. 2. The previously stored transaction snapshot remains authoritative.  8.1 A refund or settlement adjustment occurs later on the transaction. 1. The system uses the original transaction snapshot to recalculate refund impact, portal-share impact, and net payout impact. 2. The system does not use newer admin configuration values retroactively.  3.1 Internal financial-processing components cannot be notified successfully. 1. The system handles the issue according to financial-consistency policy. 2. The rule application flow is protected from inconsistent future calculations according to policy.  7.1 A temporary technical issue occurs while saving the transaction rule snapshot. 1. The system cannot safely finalize the transaction calculation state. 2. The system handles the issue according to payment and audit consistency rules. |

## Use Case 200 – (UC-200) View Analytics and Reports


| Field | Description |
|---|---|
| Use Case 200 – (UC-200) | View Analytics and Reports |
| Related Requirements | FR-188, FR-189, FR-161, FR-162 |
| Initiating Actor | Admin |
| Actor’s Goal | To view platform analytics and reports so that administrative performance, activity, and usage trends can be monitored and analyzed. |
| Participating Actors | None |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has access to the analytics and reports dashboard. 3. Dashboard metrics and reporting data are available in the system. |
| Postconditions | 1. The analytics dashboard is displayed successfully. 2. The Admin can review platform metrics across supported reporting domains. 3. The Admin may optionally apply filters or export report data. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the analytics and reports dashboard. 2. The system retrieves the dashboard metrics. 3. The system displays the analytics dashboard to the Admin. 4. The Admin reviews the displayed metrics and reporting summaries. 5. The Admin may continue with filtering or export actions if needed. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The Admin chooses to filter the report data. 1. The system continues through the date-range or report-filter flow.  4.1 The Admin chooses to export report data. 1. The system continues through the CSV export flow.  2.2 A temporary technical issue occurs while loading dashboard data. 1. The system cannot display the analytics dashboard normally. 2. The system shows an error or temporary unavailable message. |

## Use Case 201 – (UC-201) Retrieve & Display Dashboard Metrics


| Field | Description |
|---|---|
| Use Case 201 – (UC-201) | Retrieve & Display Dashboard Metrics |
| Related Requirements | FR-188, FR-189 |
| Initiating Actor | System |
| Actor’s Goal | To retrieve and display the dashboard metrics needed for administrative analytics and reporting. |
| Participating Actors | Admin |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has opened the analytics and reports dashboard. 3. The relevant reporting data sources are available to the system. |
| Postconditions | 1. The dashboard metrics are retrieved successfully. 2. The system displays the metrics in the analytics dashboard. 3. The Admin can review the metrics across supported dashboard areas such as users, scholarships, applications, bookings, resources, and AI usage. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the analytics and reports dashboard. 2. The system identifies the dashboard metrics required for the view. 3. The system retrieves the relevant data from the reporting sources. 4. The system prepares the metric summaries and dashboard values. 5. The system displays the dashboard metrics to the Admin. 6. The Admin reviews the analytics data successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 3.1 One or more reporting data sources are temporarily unavailable. 1. The system retrieves the available data according to policy. 2. The system displays partial dashboard results or an unavailable indicator where needed.  4.1 A temporary technical issue occurs while preparing the dashboard metrics. 1. The system cannot complete the dashboard preparation normally. 2. The system displays an error or temporary unavailable message.  5.1 Some dashboard sections have no available data. 1. The system displays the available sections normally. 2. The system shows empty-state indicators for sections with no data. |

## Use Case 202 – (UC-202) Apply Date Range / Report Filter


| Field | Description |
|---|---|
| Use Case 202 – (UC-202) | Apply Date Range / Report Filter |
| Related Requirements | FR-189, FR-162 |
| Initiating Actor | Admin |
| Actor’s Goal | To filter analytics and report data so that the dashboard shows only the required time period or reporting scope. |
| Participating Actors | None |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has opened the analytics and reports dashboard. 3. Filtering controls are available in the reporting interface. |
| Postconditions | 1. The selected date range or report filter is applied successfully. 2. The dashboard metrics are refreshed according to the selected criteria. 3. The Admin can review the filtered reporting results. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the analytics and reports dashboard. 2. The Admin selects a date range or other available report filter. 3. The Admin confirms the filter selection. 4. The system receives the filter criteria. 5. The system applies the selected filter to the report data. 6. The system refreshes the dashboard metrics and reporting results. 7. The Admin reviews the filtered analytics successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 2.1 The selected filter values are invalid or incomplete. 1. The system rejects the filter request. 2. The system displays the related validation message.  5.1 The filter returns no matching reporting data. 1. The system displays an empty-state or no-results message. 2. The Admin may change the filter criteria and try again.  6.1 A temporary technical issue occurs while refreshing the filtered results. 1. The system cannot update the dashboard normally. 2. The system displays an error or temporary unavailable message. |

## Use Case 203 – (UC-203) Export Report as CSV


| Field | Description |
|---|---|
| Use Case 203 – (UC-203) | Export Report as CSV |
| Related Requirements | FR-189 |
| Initiating Actor | Admin |
| Actor’s Goal | To export analytics or report data as a CSV file so that it can be reviewed, shared, or analyzed outside the platform. |
| Participating Actors | None |
| Preconditions | 1. The Admin is authenticated. 2. The Admin has opened the analytics and reports dashboard. 3. Report data is available for export. 4. The export feature is enabled for the current report context. |
| Postconditions | 1. A CSV export is generated successfully if the request is valid. 2. The exported report reflects the currently selected report scope or filters according to system behavior. 3. The Admin can access the generated CSV output. |
| Flow of Events for Main Success Scenario | 1. The Admin opens the analytics and reports dashboard. 2. The Admin optionally applies the desired report filters. 3. The Admin selects the export-as-CSV action. 4. The system receives the export request. 5. The system prepares the report data for CSV output. 6. The system generates the CSV file. 7. The system makes the CSV export available to the Admin. 8. The Admin accesses the exported report successfully. |
| Flow of Events for Extensions (Alternate Scenarios) | 5.1 No exportable data is available for the selected report scope. 1. The system does not generate the CSV file. 2. The system informs the Admin that no exportable data is available.  6.1 A temporary technical issue occurs while generating the CSV file. 1. The system cannot complete the export operation. 2. The system displays an error message and asks the Admin to try again later.  2.1 The applied filters produce an empty report result. 1. The system may generate an empty export according to policy or block the export with a no-data message. |

# 7. Traceability Matrix

This traceability matrix maps all 211 functional requirements (FR-001 through FR-211) against all 203 granular use cases (UC-01 through UC-203). Each block covers a functional area. An X indicates that the FR is addressed by the UC. PW = Priority Weight. Max PW shows the highest priority FR in each UC column. Total PW sums all mapped FR priorities per UC.

## 7.1 Traceability Matrix — Block A: Authentication & Access


| FR | PW | UC-01 | UC-02 | UC-03 | UC-04 | UC-05 | UC-06 | UC-07 | UC-08 | UC-09 | UC-10 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| FR-001 | 5 | X |  |  |  |  |  |  |  |  |  |
| FR-002 | 5 | X |  |  |  |  |  |  |  |  |  |
| FR-003 | 5 |  | X |  |  |  | X |  |  |  |  |
| FR-006 | 4 | X | X |  |  |  |  |  |  |  |  |
| FR-007 | 5 |  |  | X | X | X |  |  |  |  |  |
| FR-010 | 5 |  |  | X |  |  |  |  | X |  |  |
| FR-011 | 5 |  |  | X |  |  |  |  | X |  |  |
| FR-012 | 5 |  |  | X |  |  |  |  |  |  |  |
| FR-021 | 5 |  |  | X | X | X |  |  |  |  |  |
| FR-022 | 5 |  |  |  |  |  | X | X |  | X |  |
| FR-023 | 4 |  |  |  |  |  | X |  |  | X |  |
| FR-026 | 5 |  |  |  |  |  | X | X |  |  | X |
| FR-027 | 5 |  |  |  |  |  | X | X | X |  | X |
| Max PW |  | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 14 | 9 | 25 | 10 | 10 | 24 | 15 | 15 | 9 | 10 |

## 7.2 Traceability Matrix — Block B: Onboarding (General)


| FR | PW | UC-11 | UC-12 | UC-13 | UC-14 | UC-15 | UC-16 | UC-17 | UC-18 | UC-19 | UC-20 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| FR-008 | 4 | X | X | X | X |  |  |  |  |  |  |
| FR-009 | 4 |  |  |  |  | X | X | X | X |  |  |
| FR-010 | 5 | X |  | X | X | X |  | X | X |  |  |
| FR-011 | 5 |  |  |  | X |  |  |  | X |  |  |
| FR-022 | 5 | X | X |  |  | X | X |  |  |  |  |
| FR-024 | 5 |  |  |  |  |  |  |  |  | X | X |
| FR-025 | 5 |  |  |  |  |  |  |  |  | X |  |
| Max PW |  | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 14 | 9 | 9 | 14 | 14 | 9 | 9 | 14 | 10 | 5 |

## 7.3 Traceability Matrix — Block C: Onboarding (Student)


| FR | PW | UC-21 | UC-22 | UC-23 | UC-24 | UC-25 | UC-26 | UC-27 | UC-28 | UC-29 | UC-30 | UC-31 | UC-32 | UC-33 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FR-010 | 5 |  |  |  | X | X | X | X | X |  |  |  |  |  |
| FR-011 | 5 |  |  |  | X | X | X | X | X |  |  |  |  |  |
| FR-012 | 5 |  |  |  | X | X | X | X | X | X | X | X | X | X |
| FR-013 | 5 |  |  |  |  |  |  |  |  | X | X | X | X | X |
| FR-024 | 5 |  | X | X |  |  |  |  |  |  |  |  |  |  |
| FR-025 | 5 | X |  |  |  |  |  |  |  |  |  |  |  |  |
| FR-027 | 5 |  |  |  | X | X | X | X | X | X | X | X | X | X |
| Max PW |  | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 5 | 5 | 5 | 20 | 20 | 20 | 20 | 20 | 15 | 15 | 15 | 15 | 15 |

## 7.4 Traceability Matrix — Block D: Onboarding (Company & Consultant)


| FR | PW | UC-34 | UC-35 | UC-36 | UC-37 | UC-38 | UC-39 | UC-40 | UC-41 | UC-42 | UC-43 | UC-44 | UC-45 | UC-46 | UC-47 | UC-48 | UC-49 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FR-012 | 5 | X | X | X | X | X | X | X | X | X | X |  |  |  |  |  |  |
| FR-014 | 5 | X | X | X | X | X |  |  |  |  |  |  |  |  |  |  |  |
| FR-015 | 5 |  |  |  |  |  | X | X | X | X | X |  |  |  |  |  |  |
| FR-016 | 5 |  |  |  |  |  |  |  |  |  |  | X | X | X | X | X | X |
| FR-017 | 5 |  |  |  |  |  |  |  |  |  |  | X | X | X | X | X | X |
| FR-018 | 5 |  |  |  |  |  |  |  |  |  |  | X | X | X | X | X | X |
| FR-027 | 5 | X | X | X | X | X | X | X | X | X | X |  |  |  |  |  |  |
| Max PW |  | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 15 | 15 | 15 | 15 | 15 | 15 | 15 | 15 | 15 | 15 | 15 | 15 | 15 | 15 | 15 | 15 |

## 7.5 Traceability Matrix — Block E: Role Upgrade & Profile


| FR | PW | UC-50 | UC-51 | UC-52 | UC-53 | UC-54 | UC-55 | UC-56 | UC-57 | UC-58 | UC-59 | UC-60 | UC-61 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FR-019 | 5 | X | X | X | X |  |  |  |  |  |  |  |  |
| FR-020 | 5 | X | X | X | X |  |  |  |  |  |  |  |  |
| FR-028 | 5 |  |  |  |  | X | X |  | X | X | X | X | X |
| FR-029 | 5 |  |  |  |  | X | X |  | X | X |  | X |  |
| FR-030 | 5 |  |  |  |  | X | X |  | X | X | X |  |  |
| FR-031 | 5 |  |  |  |  | X | X |  | X | X |  |  |  |
| FR-032 | 4 |  |  |  |  | X |  | X |  |  |  | X |  |
| FR-033 | 4 |  |  |  |  | X | X | X | X | X | X | X | X |
| Max PW |  | 5 | 5 | 5 | 5 | 5 | 5 | 4 | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 10 | 10 | 10 | 10 | 28 | 24 | 8 | 24 | 24 | 14 | 18 | 9 |

## 7.6 Traceability Matrix — Block F: Scholarship Discovery


| FR | PW | UC-62 | UC-63 | UC-64 | UC-65 | UC-66 | UC-67 | UC-68 | UC-69 | UC-70 | UC-71 | UC-72 | UC-73 | UC-74 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FR-021 | 5 |  | X | X | X |  |  |  |  |  |  |  |  |  |
| FR-028 | 5 |  |  |  |  | X | X |  |  |  |  |  |  |  |
| FR-029 | 5 |  |  |  |  | X |  |  |  |  |  |  |  |  |
| FR-030 | 5 |  |  |  |  | X |  |  |  |  |  |  |  |  |
| FR-031 | 5 |  |  |  |  | X | X |  |  |  |  |  |  |  |
| FR-032 | 4 | X |  |  |  |  |  |  |  |  |  |  |  |  |
| FR-033 | 4 |  | X | X |  |  | X |  |  |  |  |  |  |  |
| FR-034 | 5 |  |  |  |  |  |  | X | X |  | X |  |  |  |
| FR-035 | 5 |  |  |  |  |  |  | X | X |  | X | X |  |  |
| FR-036 | 4 |  |  |  |  |  |  | X |  | X | X | X |  |  |
| FR-040 | 5 |  |  |  |  |  |  |  |  |  |  |  | X | X |
| FR-041 | 5 |  |  |  |  |  |  |  |  |  |  |  | X |  |
| FR-042 | 5 |  |  |  |  |  |  |  |  |  |  |  | X |  |
| FR-043 | 5 |  |  |  |  |  |  |  |  |  |  |  | X | X |
| FR-044 | 4 |  |  |  |  |  |  | X | X |  | X | X |  |  |
| Max PW |  | 4 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 4 | 5 | 5 | 5 | 5 |
| Total PW |  | 4 | 9 | 9 | 5 | 20 | 14 | 18 | 14 | 4 | 18 | 13 | 20 | 10 |

## 7.7 Traceability Matrix — Block G: Application (In-App)


| FR | PW | UC-75 | UC-76 | UC-77 | UC-78 | UC-79 | UC-80 | UC-81 | UC-82 | UC-83 | UC-84 | UC-85 | UC-86 | UC-87 | UC-88 | UC-89 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FR-030 | 5 |  |  |  |  |  |  |  |  |  | X |  |  |  |  |  |
| FR-038 | 5 |  |  |  |  | X |  | X |  |  |  |  |  |  |  |  |
| FR-039 | 5 |  |  |  |  |  |  |  | X |  |  |  |  |  |  |  |
| FR-040 | 5 |  | X |  |  | X |  | X | X |  |  |  |  |  |  |  |
| FR-041 | 5 | X |  |  |  | X | X |  |  |  |  |  |  |  |  |  |
| FR-042 | 5 | X |  |  |  | X | X |  |  |  |  |  |  |  |  |  |
| FR-043 | 5 |  | X |  |  | X |  | X | X | X |  |  |  |  |  |  |
| FR-045 | 5 |  |  | X |  |  |  |  |  |  |  |  |  |  |  |  |
| FR-046 | 4 |  |  | X | X |  |  |  |  |  |  |  |  |  |  |  |
| FR-047 | 5 |  |  |  |  |  |  |  |  |  |  |  |  | X |  |  |
| FR-048 | 5 |  |  |  |  |  |  |  |  |  |  |  |  | X |  | X |
| FR-049 | 5 |  |  |  |  |  |  |  |  |  |  |  |  | X | X |  |
| FR-050 | 5 |  |  |  |  |  |  |  |  |  |  |  |  | X |  |  |
| FR-053 | 5 |  |  |  |  |  |  |  |  |  |  | X |  |  |  |  |
| FR-055 | 5 |  |  |  |  |  |  |  |  |  |  | X |  |  |  |  |
| FR-056 | 5 |  |  |  |  |  |  |  |  |  |  | X | X |  |  |  |
| FR-057 | 5 |  |  |  |  |  |  |  |  |  |  |  |  | X |  |  |
| FR-140 | 5 |  |  |  | X |  |  |  |  |  |  |  |  |  |  |  |
| FR-181 | 5 |  |  |  | X |  |  |  |  |  |  |  |  |  |  |  |
| FR-186 | 5 |  |  |  |  |  |  |  |  |  | X |  |  |  |  |  |
| Max PW |  | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 10 | 10 | 9 | 14 | 25 | 10 | 15 | 15 | 5 | 10 | 15 | 5 | 25 | 5 | 5 |

## 7.8 Traceability Matrix — Block H: Application (External & Review)


| FR | PW | UC-90 | UC-91 | UC-92 | UC-93 | UC-94 | UC-95 | UC-96 | UC-97 | UC-98 | UC-99 | UC-100 | UC-101 | UC-102 | UC-103 | UC-104 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FR-047 | 5 |  |  |  |  |  |  |  |  |  |  | X | X |  |  |  |
| FR-048 | 5 |  |  |  |  |  |  |  |  |  |  | X | X |  | X |  |
| FR-049 | 5 |  |  |  |  |  |  |  |  |  |  | X | X | X |  |  |
| FR-050 | 5 |  | X |  |  |  | X |  |  |  |  |  | X |  |  | X |
| FR-051 | 5 |  | X |  |  |  | X | X |  |  |  |  |  |  |  | X |
| FR-052 | 5 |  |  |  |  | X | X |  |  |  |  |  |  |  |  |  |
| FR-053 | 5 |  |  |  | X |  |  |  |  | X |  |  |  |  |  |  |
| FR-054 | 5 |  |  |  | X |  |  |  |  |  |  |  |  |  |  |  |
| FR-055 | 5 |  |  |  |  |  |  |  |  | X |  |  |  |  |  | X |
| FR-056 | 5 |  |  |  |  |  |  |  |  | X | X |  |  |  |  |  |
| FR-057 | 5 | X |  |  |  |  |  |  |  |  |  |  | X |  |  |  |
| FR-058 | 5 |  |  | X |  |  |  |  |  |  |  |  |  |  |  |  |
| FR-059 | 5 | X |  | X |  |  |  |  |  |  |  |  |  |  |  |  |
| FR-060 | 5 |  |  |  |  |  | X |  | X |  |  |  |  |  |  |  |
| FR-061 | 4 |  | X |  |  |  |  |  |  |  |  |  |  |  |  | X |
| FR-063 | 5 |  |  |  |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-064 | 5 |  |  |  |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-065 | 5 |  |  |  |  | X |  |  |  |  |  |  |  |  |  |  |
| Max PW |  | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 10 | 14 | 10 | 10 | 20 | 20 | 5 | 5 | 15 | 5 | 15 | 25 | 5 | 5 | 19 |

## 7.9 Traceability Matrix — Block I: Company Review & Payment


| FR | PW | UC-105 | UC-106 | UC-107 | UC-108 | UC-109 | UC-110 | UC-111 | UC-112 | UC-113 | UC-114 | UC-115 | UC-116 | UC-117 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FR-048 | 5 |  |  |  |  |  |  |  |  |  |  | X |  |  |
| FR-050 | 5 |  |  |  |  | X |  |  | X | X |  | X |  |  |
| FR-051 | 5 |  |  |  |  | X |  |  | X | X |  |  |  |  |
| FR-052 | 5 |  |  |  | X | X |  |  |  |  |  |  |  |  |
| FR-053 | 5 |  |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-054 | 5 |  |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-057 | 5 |  |  |  |  |  | X |  |  |  |  |  |  |  |
| FR-058 | 5 | X |  |  |  |  |  |  | X |  | X |  | X |  |
| FR-059 | 5 | X | X |  |  |  | X |  | X |  | X |  | X | X |
| FR-060 | 5 |  |  |  |  | X |  | X | X |  | X |  |  | X |
| FR-061 | 4 |  |  |  |  |  |  |  | X | X | X | X | X | X |
| FR-062 | 4 |  |  |  |  |  |  |  | X |  | X | X | X | X |
| FR-063 | 5 |  |  |  | X |  |  |  |  |  |  |  |  |  |
| FR-064 | 5 |  |  |  | X |  |  |  |  |  |  |  |  |  |
| FR-065 | 5 |  |  |  | X |  |  |  |  |  |  |  |  |  |
| Max PW |  | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 10 | 5 | 10 | 20 | 20 | 10 | 5 | 33 | 14 | 23 | 18 | 18 | 18 |

## 7.10 Traceability Matrix — Block J: Consultant Booking


| FR | PW | UC-118 | UC-119 | UC-120 | UC-121 | UC-122 | UC-123 | UC-124 | UC-125 | UC-126 | UC-127 | UC-128 | UC-129 | UC-130 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FR-051 | 5 |  |  |  |  |  |  |  | X |  |  |  |  |  |
| FR-052 | 5 |  |  |  |  |  | X | X | X | X |  | X | X | X |
| FR-053 | 5 | X |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-054 | 5 | X | X |  |  |  |  |  |  |  |  |  |  |  |
| FR-055 | 5 | X |  | X |  | X |  |  |  |  |  |  |  |  |
| FR-056 | 5 | X |  |  | X | X |  |  |  |  |  |  |  |  |
| FR-060 | 5 |  |  |  |  |  |  |  |  |  | X |  |  |  |
| FR-063 | 5 |  |  |  |  |  | X |  |  |  |  | X | X |  |
| FR-064 | 5 |  |  |  |  |  | X |  |  |  |  | X | X |  |
| FR-065 | 5 |  |  |  |  |  | X | X | X | X | X | X |  | X |
| FR-181 | 5 |  |  |  |  |  |  |  |  | X |  |  |  |  |
| Max PW |  | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 20 | 5 | 10 | 5 | 10 | 20 | 10 | 15 | 15 | 10 | 20 | 15 | 10 |

## 7.11 Traceability Matrix — Block K: Booking Payment & Refund


| FR | PW | UC-131 | UC-132 | UC-133 | UC-134 | UC-135 | UC-136 | UC-137 | UC-138 | UC-139 | UC-140 | UC-141 | UC-142 | UC-143 | UC-144 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FR-052 | 5 | X | X |  |  |  |  |  |  |  |  |  |  |  |  |
| FR-057 | 5 |  |  |  |  |  |  |  |  |  |  |  | X |  |  |
| FR-063 | 5 | X |  |  |  |  |  |  |  |  |  |  |  |  |  |
| FR-064 | 5 | X |  |  |  |  |  |  |  |  |  |  |  |  |  |
| FR-065 | 5 | X | X |  |  |  |  |  |  |  |  |  |  |  |  |
| FR-066 | 5 |  |  |  |  |  |  |  | X |  |  |  |  |  |  |
| FR-067 | 5 |  |  |  |  |  |  | X | X |  |  |  |  |  |  |
| FR-068 | 4 |  |  |  |  | X |  | X |  |  |  |  |  |  |  |
| FR-069 | 4 |  |  |  |  |  |  |  | X | X |  |  |  |  |  |
| FR-070 | 5 |  |  |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-071 | 5 |  |  |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-072 | 4 |  |  | X |  |  |  |  |  |  |  |  |  |  |  |
| FR-073 | 4 |  |  | X |  |  |  |  |  |  |  |  |  |  |  |
| FR-074 | 4 |  |  |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-075 | 4 |  |  | X |  |  | X |  |  |  |  |  |  |  |  |
| FR-076 | 5 |  |  |  |  |  |  |  |  |  | X |  |  |  |  |
| FR-077 | 5 |  |  |  |  |  |  |  |  |  | X |  |  |  |  |
| FR-078 | 5 |  |  |  |  |  |  |  |  |  | X |  | X |  |  |
| FR-079 | 5 |  |  |  |  |  |  |  |  |  | X | X |  |  |  |
| FR-080 | 5 |  |  |  |  |  |  |  |  |  | X | X |  |  |  |
| FR-081 | 5 |  |  |  |  |  |  |  |  |  |  | X |  |  |  |
| FR-082 | 5 |  |  |  |  |  |  |  |  |  |  |  |  |  | X |
| FR-083 | 5 |  |  |  |  |  |  |  |  |  |  |  |  |  | X |
| FR-084 | 5 |  |  |  |  |  |  |  |  |  | X |  | X |  |  |
| FR-086 | 5 |  |  |  |  |  |  |  |  |  |  |  |  |  | X |
| FR-138 | 5 |  |  |  |  |  |  |  |  |  |  |  |  | X |  |
| FR-145 | 5 |  |  |  |  |  |  |  |  |  |  |  |  | X |  |
| FR-146 | 5 |  |  |  |  |  |  |  |  |  |  |  |  | X |  |
| FR-152 | 5 |  |  |  |  |  |  |  |  |  |  |  |  | X |  |
| FR-185 | 5 |  |  |  |  |  |  |  |  |  |  | X |  |  |  |
| FR-186 | 5 |  |  |  |  |  |  |  |  |  |  | X |  |  |  |
| FR-188 | 4 |  |  |  |  |  |  |  |  |  |  |  |  |  | X |
| FR-191 | 5 |  |  |  |  | X |  | X | X |  |  |  |  |  |  |
| FR-209 | 5 |  |  |  |  |  |  |  |  | X |  |  |  |  |  |
| FR-210 | 5 |  |  |  |  |  |  |  | X | X |  |  |  |  |  |
| Max PW |  | 5 | 5 | 4 | 5 | 5 | 4 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 20 | 10 | 12 | 14 | 9 | 4 | 14 | 24 | 14 | 30 | 25 | 15 | 20 | 19 |

## 7.12 Traceability Matrix — Block L: Consultant Rating & Review


| FR | PW | UC-145 | UC-146 | UC-147 | UC-148 | UC-149 | UC-150 | UC-151 | UC-152 | UC-153 | UC-154 | UC-155 | UC-156 | UC-157 | UC-158 | UC-159 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FR-070 | 5 |  |  |  |  |  |  |  |  |  |  | X | X | X |  |  |
| FR-071 | 5 |  |  |  |  |  |  |  |  |  |  | X | X | X | X | X |
| FR-074 | 4 |  |  |  |  |  |  |  |  |  |  | X | X | X | X | X |
| FR-075 | 4 |  |  |  |  |  |  |  |  | X |  |  |  |  |  |  |
| FR-077 | 5 |  | X |  |  |  |  |  |  |  |  |  |  |  |  |  |
| FR-081 | 5 | X |  |  |  |  | X |  |  |  |  |  |  |  |  |  |
| FR-082 | 5 | X |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| FR-083 | 5 | X |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| FR-084 | 5 |  | X |  | X |  |  |  |  |  |  |  |  |  |  |  |
| FR-085 | 5 |  |  | X |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-086 | 5 |  |  | X |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-087 | 5 |  |  | X |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-088 | 5 |  |  | X |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-089 | 5 |  |  | X |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-090 | 5 |  |  | X | X | X |  |  |  |  |  |  |  |  |  |  |
| FR-091 | 5 |  |  | X | X | X |  |  |  |  |  |  |  |  |  |  |
| FR-092 | 5 |  |  |  | X |  |  |  | X |  |  |  |  |  |  |  |
| FR-093 | 5 |  |  |  |  |  |  | X |  |  |  |  |  |  |  |  |
| FR-094 | 4 |  |  |  |  |  |  |  |  |  | X |  |  |  |  |  |
| FR-095 | 5 |  |  |  |  |  | X |  |  |  |  |  |  |  |  |  |
| FR-096 | 5 |  |  |  |  |  |  | X |  |  |  | X | X | X | X |  |
| FR-097 | 5 |  |  |  |  |  |  | X |  |  |  | X | X | X | X | X |
| FR-098 | 4 |  |  |  |  |  |  | X | X |  |  |  |  |  |  |  |
| FR-099 | 4 |  |  |  |  |  |  |  | X |  |  |  |  |  |  |  |
| FR-100 | 4 |  |  |  |  |  |  | X |  |  |  | X | X |  | X | X |
| FR-101 | 4 |  |  |  |  |  |  |  |  | X |  |  |  |  |  |  |
| FR-187 | 5 | X |  |  |  |  | X |  |  |  |  |  |  |  |  |  |
| FR-188 | 4 | X |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| FR-189 | 4 |  |  |  |  |  | X |  |  |  |  |  |  |  |  |  |
| FR-190 | 5 |  |  |  |  |  | X |  |  |  |  |  |  |  |  |  |
| FR-192 | 5 |  |  |  |  |  | X |  |  |  |  |  |  |  |  |  |
| FR-193 | 5 |  |  | X |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-194 | 5 |  |  |  |  |  | X |  |  |  |  |  |  |  |  |  |
| FR-195 | 5 |  |  |  |  |  | X |  |  |  |  |  |  |  |  |  |
| FR-196 | 4 |  |  |  |  |  |  |  | X |  |  |  |  |  |  |  |
| FR-197 | 5 |  |  |  |  |  | X |  |  |  |  |  |  |  |  |  |
| FR-199 | 5 |  |  |  |  | X |  |  |  |  |  |  |  |  |  |  |
| FR-200 | 5 |  |  |  |  |  | X |  |  |  |  |  |  |  |  |  |
| Max PW |  | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 4 | 4 | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 24 | 10 | 40 | 20 | 45 | 49 | 23 | 17 | 8 | 4 | 28 | 28 | 24 | 23 | 18 |

## 7.13 Traceability Matrix — Block M: Community & Chat


| FR | PW | UC-160 | UC-161 | UC-162 | UC-163 | UC-164 | UC-165 | UC-166 | UC-167 | UC-168 | UC-169 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| FR-102 | 5 | X | X |  |  |  |  |  |  |  |  |
| FR-103 | 5 | X | X |  |  |  |  |  |  |  |  |
| FR-104 | 5 | X | X |  | X |  |  |  |  |  |  |
| FR-105 | 5 | X | X |  | X |  |  |  |  |  |  |
| FR-106 | 5 | X | X | X |  |  |  |  |  |  |  |
| FR-107 | 5 | X | X | X |  | X |  |  |  |  |  |
| FR-108 | 4 | X |  |  |  |  |  |  |  |  |  |
| FR-109 | 5 |  |  |  |  |  | X |  |  |  |  |
| FR-110 | 4 |  |  |  |  |  | X | X |  |  |  |
| FR-111 | 5 |  |  |  |  |  | X |  | X |  |  |
| FR-112 | 5 | X | X | X |  | X | X | X | X | X |  |
| FR-113 | 5 |  |  |  |  |  |  |  |  |  | X |
| FR-114 | 5 |  |  |  |  |  |  |  |  |  | X |
| FR-115 | 5 |  |  |  |  |  |  |  |  |  | X |
| FR-116 | 5 |  |  |  |  |  |  |  |  |  | X |
| FR-117 | 5 |  |  |  |  |  |  |  |  |  | X |
| FR-118 | 4 |  |  |  |  |  |  |  |  |  | X |
| FR-119 | 5 |  |  |  |  |  |  |  |  |  | X |
| FR-120 | 5 |  |  |  |  |  |  |  |  |  | X |
| FR-121 | 5 |  |  |  |  |  |  |  |  |  | X |
| FR-129 | 5 |  | X |  |  |  |  |  |  |  |  |
| FR-130 | 5 |  | X |  |  |  |  |  |  |  |  |
| FR-131 | 4 |  | X |  |  |  |  |  |  |  |  |
| FR-132 | 5 |  |  | X |  |  |  |  |  |  |  |
| FR-133 | 5 |  |  |  |  |  |  |  |  | X |  |
| FR-134 | 4 |  |  |  |  |  |  |  |  | X |  |
| Max PW |  | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 39 | 49 | 20 | 10 | 10 | 19 | 9 | 10 | 14 | 44 |

## 7.14 Traceability Matrix — Block N: AI Features


| FR | PW | UC-170 | UC-171 | UC-172 | UC-173 | UC-174 |
|---|---|---|---|---|---|---|
| FR-113 | 5 | X |  | X | X |  |
| FR-114 | 5 | X |  | X | X |  |
| FR-115 | 5 | X |  | X | X |  |
| FR-116 | 5 | X |  | X | X |  |
| FR-117 | 5 | X |  | X | X |  |
| FR-118 | 4 | X | X |  |  |  |
| FR-119 | 5 | X | X |  |  |  |
| FR-120 | 5 | X | X |  |  |  |
| FR-121 | 5 | X | X |  |  |  |
| FR-122 | 5 |  |  |  |  | X |
| FR-123 | 5 |  |  |  |  | X |
| FR-124 | 5 |  |  |  |  | X |
| FR-128 | 4 |  |  |  |  | X |
| Max PW |  | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 44 | 19 | 25 | 25 | 19 |

## 7.15 Traceability Matrix — Block O: Resources Hub


| FR | PW | UC-175 | UC-176 | UC-177 | UC-178 | UC-179 | UC-180 |
|---|---|---|---|---|---|---|---|
| FR-123 | 5 | X |  |  |  |  |  |
| FR-124 | 5 | X |  |  |  |  |  |
| FR-125 | 5 |  | X | X |  |  |  |
| FR-126 | 5 |  | X |  |  | X |  |
| FR-127 | 4 |  | X |  | X | X |  |
| FR-128 | 4 |  | X | X | X | X |  |
| FR-135 | 5 |  |  | X |  |  |  |
| FR-136 | 5 |  |  | X |  |  |  |
| FR-137 | 4 |  |  | X |  |  |  |
| FR-138 | 5 |  |  |  |  |  | X |
| FR-139 | 4 |  |  |  |  |  | X |
| FR-140 | 5 |  |  |  |  |  | X |
| FR-141 | 5 |  |  |  |  |  | X |
| FR-142 | 5 |  |  |  |  |  | X |
| FR-143 | 5 |  |  |  |  |  | X |
| FR-144 | 5 |  |  |  |  |  | X |
| FR-145 | 5 |  |  |  |  |  | X |
| FR-146 | 5 |  |  |  |  |  | X |
| FR-147 | 5 |  |  |  |  |  | X |
| FR-148 | 5 |  |  |  |  |  | X |
| FR-149 | 5 |  |  |  |  |  | X |
| FR-150 | 4 |  |  |  |  |  | X |
| FR-152 | 5 |  |  |  |  |  | X |
| Max PW |  | 5 | 5 | 5 | 4 | 5 | 5 |
| Total PW |  | 10 | 18 | 23 | 8 | 13 | 68 |

## 7.16 Traceability Matrix — Block P: Notifications


| FR | PW | UC-181 | UC-182 | UC-183 | UC-184 | UC-185 |
|---|---|---|---|---|---|---|
| FR-138 | 5 |  | X |  |  |  |
| FR-140 | 5 | X | X | X |  |  |
| FR-141 | 5 |  | X | X |  |  |
| FR-142 | 5 |  | X |  |  |  |
| FR-143 | 5 |  | X |  |  |  |
| FR-144 | 5 |  | X |  |  |  |
| FR-145 | 5 |  | X |  |  |  |
| FR-146 | 5 |  | X |  |  |  |
| FR-147 | 5 |  | X |  | X |  |
| FR-148 | 5 |  | X |  | X |  |
| FR-149 | 5 |  | X | X | X |  |
| FR-150 | 4 |  | X | X | X |  |
| FR-151 | 4 |  |  | X |  |  |
| FR-152 | 5 |  | X |  |  |  |
| FR-153 | 5 |  |  |  |  | X |
| FR-154 | 5 |  |  |  |  | X |
| FR-155 | 5 |  |  |  |  | X |
| FR-156 | 5 |  |  |  |  | X |
| FR-157 | 5 |  |  |  |  | X |
| FR-158 | 5 |  |  |  |  | X |
| FR-159 | 5 |  |  |  |  | X |
| FR-181 | 5 | X |  |  |  |  |
| FR-182 | 5 | X |  |  |  |  |
| FR-183 | 5 | X |  |  |  |  |
| FR-184 | 5 | X |  |  |  |  |
| FR-185 | 5 | X |  |  |  |  |
| FR-186 | 5 | X |  |  |  |  |
| FR-187 | 5 | X |  |  |  |  |
| FR-188 | 4 | X |  |  |  |  |
| FR-189 | 4 | X |  |  |  |  |
| Max PW |  | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 48 | 64 | 23 | 19 | 35 |

## 7.17 Traceability Matrix — Block Q: Admin Dashboard & Moderation


| FR | PW | UC-186 | UC-187 | UC-188 | UC-189 | UC-190 | UC-191 | UC-192 | UC-193 | UC-194 | UC-195 | UC-196 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FR-153 | 5 | X |  |  | X |  |  |  |  |  |  |  |
| FR-154 | 5 | X |  |  | X |  |  |  |  |  |  |  |
| FR-155 | 5 | X |  |  | X |  |  |  |  |  |  |  |
| FR-156 | 5 | X |  |  |  |  |  |  |  |  |  |  |
| FR-158 | 5 |  |  |  | X |  |  |  |  |  |  |  |
| FR-160 | 5 |  |  |  |  |  | X |  |  |  |  |  |
| FR-163 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-164 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-165 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-166 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-170 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-172 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-177 | 5 |  |  |  |  |  |  | X |  |  |  |  |
| FR-183 | 5 |  |  | X |  |  |  |  |  |  |  |  |
| FR-184 | 5 |  |  | X |  |  |  |  |  |  |  |  |
| FR-185 | 5 |  |  | X |  | X |  |  |  |  |  |  |
| FR-186 | 5 |  | X |  |  |  |  |  |  |  |  |  |
| FR-187 | 5 |  |  |  |  |  | X | X | X | X | X |  |
| FR-190 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-191 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-199 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-200 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-201 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-202 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-203 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-204 | 4 |  |  |  |  |  |  |  |  |  |  | X |
| FR-206 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-207 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-208 | 5 |  |  |  |  |  | X | X | X | X | X | X |
| FR-210 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| FR-211 | 5 |  |  |  |  |  |  |  |  |  |  | X |
| Max PW |  | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 5 |
| Total PW |  | 20 | 5 | 15 | 20 | 5 | 15 | 15 | 10 | 10 | 10 | 94 |

## 7.18 Traceability Matrix — Block R: Financial Config & Analytics


| FR | PW | UC-197 | UC-198 | UC-199 | UC-200 | UC-201 | UC-202 | UC-203 |
|---|---|---|---|---|---|---|---|---|
| FR-161 | 4 |  |  |  | X |  |  |  |
| FR-162 | 4 |  |  |  | X |  | X |  |
| FR-167 | 5 | X |  |  |  |  |  |  |
| FR-168 | 5 |  | X |  |  |  |  |  |
| FR-169 | 5 | X |  |  |  |  |  |  |
| FR-171 | 5 |  |  | X |  |  |  |  |
| FR-173 | 5 | X |  |  |  |  |  |  |
| FR-174 | 5 | X |  |  |  |  |  |  |
| FR-175 | 5 |  | X |  |  |  |  |  |
| FR-176 | 5 |  |  | X |  |  |  |  |
| FR-188 | 4 |  |  |  | X | X |  |  |
| FR-189 | 4 |  |  |  | X | X | X | X |
| FR-190 | 5 | X |  |  |  |  |  |  |
| FR-191 | 5 | X |  |  |  |  |  |  |
| FR-197 | 5 |  |  | X |  |  |  |  |
| FR-198 | 5 |  |  | X |  |  |  |  |
| FR-199 | 5 |  | X | X |  |  |  |  |
| FR-200 | 5 |  |  | X |  |  |  |  |
| FR-201 | 5 | X |  | X |  |  |  |  |
| FR-202 | 5 | X |  | X |  |  |  |  |
| FR-203 | 5 |  |  | X |  |  |  |  |
| FR-204 | 4 |  | X |  |  |  |  |  |
| FR-205 | 5 |  | X | X |  |  |  |  |
| FR-206 | 5 | X |  | X |  |  |  |  |
| FR-207 | 5 | X | X | X |  |  |  |  |
| FR-208 | 5 |  |  | X |  |  |  |  |
| FR-209 | 5 |  |  | X |  |  |  |  |
| FR-210 | 5 |  | X | X |  |  |  |  |
| FR-211 | 5 |  | X | X |  |  |  |  |
| Max PW |  | 5 | 5 | 5 | 4 | 4 | 4 | 4 |
| Total PW |  | 50 | 39 | 80 | 16 | 8 | 8 | 4 |

Summary: This matrix covers 211 functional requirements mapped against 203 use cases across 18 functional blocks. Total FR-UC links: 714. FRs not mapped to any specific UC (cross-cutting foundational requirements): FR-004, FR-005, FR-037, FR-178, FR-179, FR-180.

Activity Diagrams :

Register Account:

![SRS figure](srs-images/image35.png)

Figure 36: 7.2 Traceability Matrix — Block B

Login Flow:

![SRS figure](srs-images/image36.png)

Figure 37: 7.2 Traceability Matrix — Block B

SSO Login:

![SRS figure](srs-images/image37.png)

Figure 38: 7.3 Traceability Matrix — Block C

Edit Profile:

![SRS figure](srs-images/image38.png)

Figure 39: 7.4 Traceability Matrix — Block D

Reset Password Flow:

![SRS figure](srs-images/image39.png)

Figure 40: Activity Diagrams

Student to Consultant Upgrade Flow:

![SRS figure](srs-images/image40.png)

Figure 41: Diagram

Preserve Student History:

![SRS figure](srs-images/image41.png)

Figure 42: Diagram

Scholarships Listing Flow:

![SRS figure](srs-images/image42.jpg)

Figure 43: Diagram

View Scholarship Details:

![SRS figure](srs-images/image43.jpg)

Figure 44: Edit Profile

Scholarship Bookmark:

![SRS figure](srs-images/image44.jpg)

Figure 45: Figure 39: 7.4 Traceability Matrix — Block D

In-App Scholarship:

![SRS figure](srs-images/image45.jpg)

Figure 46: Reset Password Flow

External Scholarship Application:

![SRS figure](srs-images/image46.jpg)

Figure 47: Diagram

Track & Manage Application:

![SRS figure](srs-images/image47.jpg)

Figure 48: Diagram

Application Review:

![SRS figure](srs-images/image48.jpg)

Figure 49: Figure 44: Edit Profile

Company Application Review:

![SRS figure](srs-images/image49.png)

Figure 50: Scholarship Bookmark

Application Review Payment Flow:

![SRS figure](srs-images/image50.png)

Figure 51: In-App Scholarship

Company Rating Flow:

![SRS figure](srs-images/image51.png)

Figure 52: Diagram

Consultation Booking:

![SRS figure](srs-images/image52.jpg)

Figure 53: Diagram

Manage Availability and Booking Decision:

![SRS figure](srs-images/image53.jpg)

Figure 54: Figure 51: In-App Scholarship

Rating and reviews management:

![SRS figure](srs-images/image54.jpg)

Figure 55: Diagram

Community Posting:

![SRS figure](srs-images/image55.png)

Figure 56: Company Rating Flow

Community Flagging and Voting Flow:

![SRS figure](srs-images/image56.png)

Figure 57: Diagram

Chat Flow:

![SRS figure](srs-images/image57.png)

Figure 58: Diagram

Start Chat and View History:

![SRS figure](srs-images/image58.png)

Figure 59: Diagram

Block Flow:

![SRS figure](srs-images/image59.png)

Figure 60: Diagram

Admin Moderation Flow:

![SRS figure](srs-images/image60.png)

Figure 61: Diagram

AI Recommendations Flow:

![SRS figure](srs-images/image61.png)

Figure 62: Diagram

Check Eligibility Flow:

![SRS figure](srs-images/image62.png)

Figure 63: Diagram

AI Chatbot:

![SRS figure](srs-images/image63.png)

Figure 64: Diagram

Resource Hub:

![SRS figure](srs-images/image64.png)

Figure 65: Diagram

Publish Articles:

![SRS figure](srs-images/image65.png)

Figure 66: Figure 62: Diagram

Articles Moderation and Features :

![SRS figure](srs-images/image66.png)

Figure 67: Figure 63: Diagram

Admin Dashboard:

![SRS figure](srs-images/image67.png)

Figure 68: Figure 64: Diagram

Admin Account Management:

![SRS figure](srs-images/image68.png)

Figure 69: Diagram

Admin Content Management:

![SRS figure](srs-images/image69.png)

Figure 70: Diagram

Notifications Flow:

1-Admin

![SRS figure](srs-images/image70.png)

![SRS figure](srs-images/image71.png)

Figure 71: Articles Moderation and Features

2-Student:

![SRS figure](srs-images/image72.png)

Figure 72: Figure 67: Figure 63: Diagram

Student Booking Notification:

![SRS figure](srs-images/image73.png)

Figure 73: Figure 68: Figure 64: Diagram

3-Company:

![SRS figure](srs-images/image74.png)

Figure 74: Diagram

4-Consultant:

![SRS figure](srs-images/image75.png)

Figure 75: Diagram

Admin dashboard Analytics:

![SRS figure](srs-images/image76.png)

Figure 76: Diagram

System Audit:

![SRS figure](srs-images/image77.png)

Figure 77: Diagram

Payment Flow:

1-Student payment to company:

![SRS figure](srs-images/image78.png)

Figure 78: Diagram

2-Student payment to Consultant:

![SRS figure](srs-images/image79.png)

Figure 79: Diagram

3-Student Refund flow of Consultant payment:

![SRS figure](srs-images/image80.png)

Figure 80: Diagram

Export Account Data:

![SRS figure](srs-images/image81.png)

Figure 81: Diagram

Delete Account Data:

![SRS figure](srs-images/image82.png)

Figure 82: Diagram

Sequence Diagrams:

Login:

![SRS figure](srs-images/image83.png)

Figure 83: Diagram

Access Control:

![SRS figure](srs-images/image84.png)

Figure 84: Diagram

Onboarding:

![SRS figure](srs-images/image85.png)

Figure 85: Figure 76: Diagram

View Profile Completeness:

![SRS figure](srs-images/image86.png)

Figure 86: Figure 77: Diagram

Change Password:

![SRS figure](srs-images/image87.png)

Figure 87: Diagram

Edit Student Profile:

![SRS figure](srs-images/image88.png)

Figure 88: Diagram

Manage Company Profile:

![SRS figure](srs-images/image89.png)

Figure 89: Diagram

Manage Consultant Profile:

![SRS figure](srs-images/image90.png)

Figure 90: Login

Scholarship Search And Filleration:

![SRS figure](srs-images/image91.png)

Figure 91: Diagram

Scholarships View and Bookmark:

![SRS figure](srs-images/image92.png)

Figure 92: Diagram

In-App Scholarship Creation:

![SRS figure](srs-images/image93.png)

Figure 93: Figure 87: Diagram

Edit In-App Scholarship Listing:

![SRS figure](srs-images/image94.png)

Figure 94: Figure 89: Diagram

Scholarship Archive:

![SRS figure](srs-images/image95.png)

Figure 95: Manage Consultant Profile

External Scholarship Listing:

![SRS figure](srs-images/image96.png)

Figure 96: Figure 91: Diagram

Scholarship Featuring:

![SRS figure](srs-images/image97.png)

Figure 97: Diagram

Student Apply for  In-App Application:

![SRS figure](srs-images/image98.png)

Figure 98: Diagram

Company Application Review:

![SRS figure](srs-images/image99.png)

Figure 99: Edit In-App Scholarship Listing

Withdrawal and Re-Application For Scholarships:

![SRS figure](srs-images/image100.png)

Figure 100: Diagram

External Application Flow:

![SRS figure](srs-images/image101.png)

Figure 101: Diagram

Company Review:

![SRS figure](srs-images/image102.png)

Figure 102: Diagram

Company Review Payment:

![SRS figure](srs-images/image103.png)

Figure 103: Diagram

Company Rating:

![SRS figure](srs-images/image104.png)

Figure 104: Diagram

Browse Consultant Profile:

![SRS figure](srs-images/image105.png)

Figure 105: Diagram

Manage Consultant Availability:

![SRS figure](srs-images/image106.png)

Figure 106: Figure 99: Edit In-App Scholarship Listing

Request Consultant Booking:

![SRS figure](srs-images/image107.png)

Figure 107: Figure 101: Diagram

Booking and Refund Statuses:

![SRS figure](srs-images/image108.png)

Figure 108: Figure 102: Diagram

Create a Post:

![SRS figure](srs-images/image109.png)

Figure 109: Company Review Payment

Community Flag Content:

![SRS figure](srs-images/image110.png)

Figure 110: Diagram

Admin Community Moderation:

![SRS figure](srs-images/image111.png)

Figure 111: Browse Consultant Profile

Chat one-to-one:

![SRS figure](srs-images/image112.png)

Figure 112: Figure 105: Diagram

Block User in Chat:

![SRS figure](srs-images/image113.png)

Figure 113: Diagram

Reply to Post:

![SRS figure](srs-images/image114.png)

Figure 114: Diagram

Vote:

![SRS figure](srs-images/image115.png)

Figure 115: Booking and Refund Statuses

AI Scholarship Recommendation:

![SRS figure](srs-images/image116.png)

Figure 116: Diagram

AI Eligibility Checker:

![SRS figure](srs-images/image117.png)

Figure 117: Diagram

AI Chatbot:

![SRS figure](srs-images/image118.png)

Figure 118: Chat one-to-one

Publish Article:

![SRS figure](srs-images/image119.png)

Figure 119: Figure 113: Diagram

Browse/Search Articles:

![SRS figure](srs-images/image120.png)

Figure 120: Figure 114: Diagram

Notifications:

Students Notifications:

![SRS figure](srs-images/image121.png)

Figure 121: Diagram

Company Notification:

![SRS figure](srs-images/image122.png)

Figure 122: Diagram

Consultant Notification:

![SRS figure](srs-images/image123.png)

Figure 123: Diagram

Admin Broadcast:

![SRS figure](srs-images/image124.png)

Figure 124: Diagram

5. Non-Functional Requirements

5.1 Performance

Page load time for any authenticated page shall be under 2 seconds on a 50 Mbps connection.

Scholarship search results shall return within 500 ms over a dataset of up to 100,000 listings.

The system shall support at least 5,000 concurrent authenticated users without performance degradation.

AI recommendation generation shall complete within 3 seconds per user request.

Real-time chat message delivery latency shall not exceed 200 ms under normal load.

5.2 Scalability

The backend shall support horizontal scaling via containerisation (Docker / Azure App Service).

The database tier shall support read replicas for query-heavy operations.

Redis shall serve as both the caching layer and the SignalR backplane across multi-instance deployments.

Blob storage shall decouple file storage from application servers.

5.3 Security

All data in transit shall be encrypted using TLS 1.2 or higher.

Passwords shall be hashed using the ASP.NET Core Identity default hasher (PBKDF2 with HMAC-SHA256).

JWT tokens shall be signed with RS256 asymmetric keys stored in Azure Key Vault.

All API endpoints except the home page endpoint require a valid JWT token.

The system shall implement OWASP Top 10 mitigations including SQL injection, XSS, CSRF, and insecure deserialization protections.

File uploads shall be scanned with antivirus checks before being committed to storage.

Stripe webhook payloads shall be verified using Stripe's signature validation before processing.

Sensitive personal data fields (nationality, financial details) shall be encrypted at rest.

5.4 Availability & Reliability

The system shall target a 99.5% monthly uptime SLA (Service Level Agreement), excluding scheduled maintenance windows.

Scheduled maintenance shall not exceed 2 hours per month and shall be communicated 48 hours in advance.

Automated daily database backups shall be retained for 30 days.

If the AI service is unavailable, scholarship browsing and application tracking shall remain fully functional (graceful degradation).

If Stripe is unavailable, paid mentor session bookings shall be queued and users notified; free session bookings shall proceed normally.

5.5 Usability

The platform shall be responsive and fully functional on screens from 375px (mobile) to 1920px (desktop).

All interactive elements shall be navigable via keyboard.

The UI shall be designed to accommodate Arabic RTL localization without structural rework .

Note: WCAG 2.1 Level AA formal accessibility compliance is out of scope for v1 and will be addressed in v2.

5.6 Maintainability

The codebase shall follow Clean Architecture principles with clear separation between API, service, domain, and data layers.

All API endpoints shall be documented via auto-generated Open API (Swagger) specifications.

Code test coverage shall not drop below 70% across unit and integration tests.


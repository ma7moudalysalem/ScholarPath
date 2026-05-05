# Sprint 1 Completion Design

## Scope
2 backend tasks + 14 frontend tasks to complete Sprint 1.

## Backend
1. **Google & Microsoft OAuth SSO** - ExternalAuthController, AddGoogle/AddMicrosoftAccount
2. **Real Email Service** - SMTP/SendGrid with bilingual templates

## Frontend (Build Order)
1. Auth modal infrastructure (AuthModalProvider, useAuthModal)
2. Login modal + page (SSO buttons, remember me, lockout)
3. Register modal + page (SSO flow, complete profile step)
4. Forgot Password modal + Reset Password page
5. Role-Adaptive Header (guest vs auth states)
6. Redirect-Back System (sessionStorage intendedDestination)
7. Logout confirmation + Session Expiry handling
8. Profile Page (tabs: Overview, Security, Upgrade Account)
9. Change Password form (SSO-aware)
10. Consultant Upgrade Request Form
11. Company Upgrade Request Form
12. Upgrade Status Banner (global)
13. Admin Upgrade Review Page (table + detail + decisions)
14. Home Page 5-section redesign

## Architecture
- Auth modals: Context-based AuthModalProvider + useAuthModal hook
- Forms: Controlled components with manual validation matching backend rules
- Admin table: MUI Table with pagination/filters
- Status banner: Global component in AuthenticatedLayout
- OAuth: ASP.NET Identity AddGoogle() + AddMicrosoftAccount()

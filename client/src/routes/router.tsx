import { lazy, Suspense } from "react";
import { Routes, Route } from "react-router";
import { PublicLayout } from "@/components/layout/PublicLayout";
import { AuthenticatedLayout } from "@/components/layout/AuthenticatedLayout";
import { AdminLayout } from "@/components/layout/AdminLayout";
import { RequireAuth, RequireRole } from "@/routes/RequireAuth";
import { EmptyState } from "@/components/common/EmptyState";

const Home = lazy(() => import("@/pages/public/Home").then((m) => ({ default: m.Home })));
const Login = lazy(() => import("@/pages/auth/Login").then((m) => ({ default: m.Login })));
const Register = lazy(() => import("@/pages/auth/Register").then((m) => ({ default: m.Register })));
const ForgotPassword = lazy(() =>
  import("@/pages/auth/ForgotPassword").then((m) => ({ default: m.ForgotPassword })),
);
const ResetPassword = lazy(() =>
  import("@/pages/auth/ResetPassword").then((m) => ({ default: m.ResetPassword })),
);
const OnboardingWizard = lazy(() =>
  import("@/pages/auth/OnboardingWizard").then((m) => ({ default: m.OnboardingWizard })),
);
const SsoCallback = lazy(() =>
  import("@/pages/auth/SsoCallback").then((m) => ({ default: m.SsoCallback })),
);
const NotFound = lazy(() => import("@/pages/NotFound").then((m) => ({ default: m.NotFound })));

// Module skeletons per PB area (spec folder path encoded)
const stub = (owner: string, moduleName: string, specPath: string) => () => (
  <EmptyState owner={owner} module={moduleName} specPath={specPath} />
);

const StudentDashboard = stub(
  "@Madiha6776 + everyone",
  "Student Dashboard",
  ".specify/specs/PB-001-auth-access-onboarding",
);
const StudentScholarships = stub("@norra-mmhamed", "PB-003 Discovery", ".specify/specs/PB-003-scholarship-discovery");
const StudentScholarshipDetail = stub(
  "@norra-mmhamed",
  "PB-003 Scholarship Detail",
  ".specify/specs/PB-003-scholarship-discovery",
);
const StudentApplications = stub(
  "@norra-mmhamed",
  "PB-004 Applications",
  ".specify/specs/PB-004-application-tracking",
);
const StudentApplicationDetail = stub(
  "@norra-mmhamed",
  "PB-004 Application detail",
  ".specify/specs/PB-004-application-tracking",
);
const StudentBookmarks = stub("@norra-mmhamed", "PB-003 Bookmarks", ".specify/specs/PB-003-scholarship-discovery");
const StudentConsultants = stub("@norra-mmhamed", "PB-006 Consultants", ".specify/specs/PB-006-consultant-booking");
const StudentConsultantDetail = stub(
  "@norra-mmhamed",
  "PB-006 Consultant detail",
  ".specify/specs/PB-006-consultant-booking",
);
const StudentBookings = stub("@norra-mmhamed", "PB-006 My bookings", ".specify/specs/PB-006-consultant-booking");
const StudentCommunity = stub("@yousra-elnoby", "PB-007 Community", ".specify/specs/PB-007-community-chat");
const StudentResources = stub("@yousra-elnoby", "PB-009 Resources", ".specify/specs/PB-009-resources-hub");
const StudentAi = lazy(() => import("@/pages/student/AiFeatures").then((m) => ({ default: m.AiFeatures })));
const StudentMessages = stub("@yousra-elnoby", "PB-007 Chat", ".specify/specs/PB-007-community-chat");

const CompanyDashboard = stub("@Madiha6776", "Company Dashboard", ".specify/specs/PB-005-company-review-payment");
const CompanyScholarships = stub(
  "@norra-mmhamed",
  "PB-003 Company listings",
  ".specify/specs/PB-003-scholarship-discovery",
);
const CompanyApplicationsReview = stub(
  "@Madiha6776",
  "PB-005 Review applications",
  ".specify/specs/PB-005-company-review-payment",
);
const CompanyBilling = stub("@norra-mmhamed", "PB-013 Billing", ".specify/specs/PB-013-payment-processing");

const ConsultantDashboard = stub(
  "@norra-mmhamed",
  "Consultant Dashboard",
  ".specify/specs/PB-006-consultant-booking",
);
const ConsultantAvailability = stub(
  "@norra-mmhamed",
  "PB-006 Availability",
  ".specify/specs/PB-006-consultant-booking",
);
const ConsultantBookings = stub("@norra-mmhamed", "PB-006 Bookings", ".specify/specs/PB-006-consultant-booking");
const ConsultantEarnings = stub("@norra-mmhamed", "PB-013 Earnings", ".specify/specs/PB-013-payment-processing");

const AdminDashboard = lazy(() => import("@/pages/admin/AdminDashboard").then((m) => ({ default: m.AdminDashboard })));
const AdminUsers = lazy(() => import("@/pages/admin/UsersAdmin").then((m) => ({ default: m.UsersAdmin })));
const AdminOnboarding = lazy(() => import("@/pages/admin/OnboardingQueue").then((m) => ({ default: m.OnboardingQueue })));
const AdminUpgrades = lazy(() => import("@/pages/admin/UpgradeQueue").then((m) => ({ default: m.UpgradeQueue })));
const AdminBroadcast = lazy(() => import("@/pages/admin/BroadcastComposer").then((m) => ({ default: m.BroadcastComposer })));
const AdminAnalytics = lazy(() => import("@/pages/admin/AnalyticsPage").then((m) => ({ default: m.AnalyticsPage })));
const AdminScholarships = stub("@yousra-elnoby", "PB-011 Scholarships", ".specify/specs/PB-011-admin-portal");
const AdminArticles = stub("@yousra-elnoby", "PB-009 Articles moderation", ".specify/specs/PB-009-resources-hub");
const AdminCommunity = stub("@yousra-elnoby", "PB-007 Community moderation", ".specify/specs/PB-007-community-chat");
const AdminPayments = stub("@norra-mmhamed", "PB-013 Payments", ".specify/specs/PB-013-payment-processing");
const AdminProfitShare = stub("@norra-mmhamed", "PB-014 Profit share", ".specify/specs/PB-014-profit-share");
const AdminAuditLog = lazy(() => import("@/pages/admin/AuditLogViewer").then((m) => ({ default: m.AuditLogViewer })));
const AdminSettings = stub("@yousra-elnoby", "PB-011 Settings", ".specify/specs/PB-011-admin-portal");

const Profile = stub("@Madiha6776", "PB-002 Profile", ".specify/specs/PB-002-profile-account");
const Notifications = stub("@Madiha6776", "PB-010 Notifications", ".specify/specs/PB-010-notifications");
const DataPrivacy = lazy(() =>
  import("@/pages/profile/DataPrivacy").then((m) => ({ default: m.DataPrivacy })),
);

function SuspenseOutlet({ children }: { children: React.ReactNode }) {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-screen items-center justify-center text-text-tertiary">Loading…</div>
      }
    >
      {children}
    </Suspense>
  );
}

export function AppRouter() {
  return (
    <SuspenseOutlet>
      <Routes>
        {/* Public */}
        <Route
          path="/"
          element={
            <PublicLayout>
              <Home />
            </PublicLayout>
          }
        />
        <Route
          path="/login"
          element={
            <PublicLayout>
              <Login />
            </PublicLayout>
          }
        />
        <Route
          path="/register"
          element={
            <PublicLayout>
              <Register />
            </PublicLayout>
          }
        />
        <Route
          path="/forgot-password"
          element={
            <PublicLayout>
              <ForgotPassword />
            </PublicLayout>
          }
        />
        <Route
          path="/reset-password"
          element={
            <PublicLayout>
              <ResetPassword />
            </PublicLayout>
          }
        />
        <Route
          path="/auth/sso-callback"
          element={
            <PublicLayout>
              <SsoCallback />
            </PublicLayout>
          }
        />

        {/* Onboarding (auth required but no layout nav) */}
        <Route
          path="/onboarding"
          element={
            <RequireAuth>
              <PublicLayout>
                <OnboardingWizard />
              </PublicLayout>
            </RequireAuth>
          }
        />

        {/* Authenticated area */}
        <Route
          element={
            <RequireAuth>
              <AuthenticatedLayout />
            </RequireAuth>
          }
        >
          {/* Profile / Notifications — shared across roles */}
          <Route path="/profile" element={<Profile />} />
          <Route path="/profile/privacy" element={<DataPrivacy />} />
          <Route path="/notifications" element={<Notifications />} />

          {/* Student */}
          <Route
            path="/student"
            element={
              <RequireRole roles={["Student"]}>
                <StudentDashboard />
              </RequireRole>
            }
          />
          <Route path="/student/scholarships" element={<StudentScholarships />} />
          <Route path="/student/scholarships/:id" element={<StudentScholarshipDetail />} />
          <Route path="/student/applications" element={<StudentApplications />} />
          <Route path="/student/applications/:id" element={<StudentApplicationDetail />} />
          <Route path="/student/bookmarks" element={<StudentBookmarks />} />
          <Route path="/student/consultants" element={<StudentConsultants />} />
          <Route path="/student/consultants/:id" element={<StudentConsultantDetail />} />
          <Route path="/student/bookings" element={<StudentBookings />} />
          <Route path="/student/community" element={<StudentCommunity />} />
          <Route path="/student/resources" element={<StudentResources />} />
          <Route path="/student/ai" element={<StudentAi />} />
          <Route path="/student/messages" element={<StudentMessages />} />

          {/* Company */}
          <Route
            path="/company"
            element={
              <RequireRole roles={["Company"]}>
                <CompanyDashboard />
              </RequireRole>
            }
          />
          <Route path="/company/scholarships" element={<CompanyScholarships />} />
          <Route path="/company/applications-review" element={<CompanyApplicationsReview />} />
          <Route path="/company/billing" element={<CompanyBilling />} />

          {/* Consultant */}
          <Route
            path="/consultant"
            element={
              <RequireRole roles={["Consultant"]}>
                <ConsultantDashboard />
              </RequireRole>
            }
          />
          <Route path="/consultant/availability" element={<ConsultantAvailability />} />
          <Route path="/consultant/bookings" element={<ConsultantBookings />} />
          <Route path="/consultant/earnings" element={<ConsultantEarnings />} />

        </Route>

        {/* Admin — has its own layout + role guard */}
        <Route
          element={
            <RequireAuth>
              <RequireRole roles={["Admin", "SuperAdmin"]}>
                <AdminLayout />
              </RequireRole>
            </RequireAuth>
          }
        >
          <Route path="/admin" element={<AdminDashboard />} />
          <Route path="/admin/users" element={<AdminUsers />} />
          <Route path="/admin/onboarding" element={<AdminOnboarding />} />
          <Route path="/admin/upgrades" element={<AdminUpgrades />} />
          <Route path="/admin/broadcast" element={<AdminBroadcast />} />
          <Route path="/admin/analytics" element={<AdminAnalytics />} />
          <Route path="/admin/scholarships" element={<AdminScholarships />} />
          <Route path="/admin/articles" element={<AdminArticles />} />
          <Route path="/admin/community" element={<AdminCommunity />} />
          <Route path="/admin/payments" element={<AdminPayments />} />
          <Route path="/admin/profit-share" element={<AdminProfitShare />} />
          <Route path="/admin/audit-log" element={<AdminAuditLog />} />
          <Route path="/admin/settings" element={<AdminSettings />} />
        </Route>

        {/* 404 */}
        <Route
          path="*"
          element={
            <PublicLayout>
              <NotFound />
            </PublicLayout>
          }
        />
      </Routes>
    </SuspenseOutlet>
  );
}

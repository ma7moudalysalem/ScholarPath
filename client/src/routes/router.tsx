import { EmptyState } from "@/components/common/EmptyState";
import { AdminLayout } from "@/components/layout/AdminLayout";
import { AuthenticatedLayout } from "@/components/layout/AuthenticatedLayout";
import { PublicLayout } from "@/components/layout/PublicLayout";
import { RequireAuth, RequireRole } from "@/routes/RequireAuth";
import { lazy, Suspense, type ReactNode } from "react";
import { Route, Routes } from "react-router";

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

const stub = (owner: string, moduleName: string, specPath: string) => () => (
  <EmptyState owner={owner} module={moduleName} specPath={specPath} />
);

// Student
const StudentDashboard = stub(
  "@Madiha6776 + everyone",
  "Student Dashboard",
  ".specify/specs/PB-001-auth-access-onboarding",
);
const StudentScholarships = stub(
  "@norra-mmhamed",
  "PB-003 Discovery",
  ".specify/specs/PB-003-scholarship-discovery",
);
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
const StudentBookmarks = stub(
  "@norra-mmhamed",
  "PB-003 Bookmarks",
  ".specify/specs/PB-003-scholarship-discovery",
);
const StudentConsultants = lazy(() =>
  import("@/pages/student/ConsultantsBrowse").then((m) => ({ default: m.ConsultantsBrowse })),
);
const StudentConsultantDetail = lazy(() =>
  import("@/pages/student/ConsultantDetail").then((m) => ({ default: m.ConsultantDetail })),
);
const StudentBookingCheckout = lazy(() =>
  import("@/pages/student/BookingCheckout").then((m) => ({ default: m.BookingCheckout })),
);
const StudentBookings = lazy(() =>
  import("@/pages/student/StudentBookings").then((m) => ({ default: m.StudentBookings })),
);
const StudentBookingDetails = lazy(() =>
  import("@/pages/student/StudentBookingDetails").then((m) => ({
    default: m.StudentBookingDetails,
  })),
);
const StudentCommunity = stub(
  "@yousra-elnoby",
  "PB-007 Community",
  ".specify/specs/PB-007-community-chat",
);
const StudentResources = stub(
  "@yousra-elnoby",
  "PB-009 Resources",
  ".specify/specs/PB-009-resources-hub",
);
const StudentAi = lazy(() =>
  import("@/pages/student/AiFeatures").then((m) => ({ default: m.AiFeatures })),
);
const StudentMessages = stub(
  "@yousra-elnoby",
  "PB-007 Chat",
  ".specify/specs/PB-007-community-chat",
);

// Company
const CompanyDashboard = stub(
  "@Madiha6776",
  "Company Dashboard",
  ".specify/specs/PB-005-company-review-payment",
);
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
const CompanyBilling = stub(
  "@norra-mmhamed",
  "PB-013 Billing",
  ".specify/specs/PB-013-payment-processing",
);

// Consultant
const ConsultantDashboard = stub(
  "@norra-mmhamed",
  "Consultant Dashboard",
  ".specify/specs/PB-006-consultant-booking",
);
const ConsultantAvailability = lazy(() =>
  import("@/pages/consultant/ConsultantAvailability").then((m) => ({
    default: m.ConsultantAvailability,
  })),
);
const ConsultantBookings = lazy(() =>
  import("@/pages/consultant/ConsultantBookings").then((m) => ({
    default: m.ConsultantBookings,
  })),
);
const ConsultantBookingDetails = lazy(() =>
  import("@/pages/consultant/ConsultantBookingDetails").then((m) => ({
    default: m.ConsultantBookingDetails,
  })),
);
const ConsultantEarnings = stub(
  "@norra-mmhamed",
  "PB-013 Earnings",
  ".specify/specs/PB-013-payment-processing",
);

// Admin
const AdminDashboard = stub(
  "@ma7moudalysalem",
  "PB-011 Admin dashboard",
  ".specify/specs/PB-011-admin-portal",
);
const AdminUsers = lazy(() =>
  import("@/pages/admin/UsersAdmin").then((m) => ({ default: m.UsersAdmin })),
);
const AdminOnboarding = lazy(() =>
  import("@/pages/admin/OnboardingQueue").then((m) => ({ default: m.OnboardingQueue })),
);
const AdminUpgrades = lazy(() =>
  import("@/pages/admin/UpgradeQueue").then((m) => ({ default: m.UpgradeQueue })),
);
const AdminBroadcast = lazy(() =>
  import("@/pages/admin/BroadcastComposer").then((m) => ({ default: m.BroadcastComposer })),
);
const AdminAnalytics = lazy(() =>
  import("@/pages/admin/AnalyticsPage").then((m) => ({ default: m.AnalyticsPage })),
);
const AdminAiEconomy = stub(
  "@ma7moudalysalem",
  "PB-017 AI economy analytics",
  ".specify/specs/PB-017-ai-economy-analytics",
);
const AdminRedactionAudit = stub(
  "@ma7moudalysalem",
  "PB-012 Redaction audit",
  ".specify/specs/PB-012-audit-compliance",
);
const AdminScholarships = stub(
  "@yousra-elnoby",
  "PB-011 Scholarships",
  ".specify/specs/PB-011-admin-portal",
);
const AdminArticles = stub(
  "@yousra-elnoby",
  "PB-009 Articles moderation",
  ".specify/specs/PB-009-resources-hub",
);
const AdminCommunity = stub(
  "@yousra-elnoby",
  "PB-007 Community moderation",
  ".specify/specs/PB-007-community-chat",
);
const AdminPayments = stub(
  "@norra-mmhamed",
  "PB-013 Payments",
  ".specify/specs/PB-013-payment-processing",
);
const AdminProfitShare = stub(
  "@TasneemShaaban",
  "PB-014 Profit share",
  ".specify/specs/PB-014-profit-share",
);
const AdminAuditLog = stub(
  "@ma7moudalysalem",
  "PB-012 Audit log",
  ".specify/specs/PB-012-audit-compliance",
);
const AdminSettings = stub(
  "@ma7moudalysalem",
  "PB-011 Settings",
  ".specify/specs/PB-011-admin-portal",
);

// Shared
const Profile = stub("@Madiha6776", "PB-002 Profile", ".specify/specs/PB-002-profile-account");
const Notifications = stub(
  "@Madiha6776",
  "PB-010 Notifications",
  ".specify/specs/PB-010-notifications",
);
const DataPrivacy = lazy(() =>
  import("@/pages/profile/DataPrivacy").then((m) => ({ default: m.DataPrivacy })),
);

function SuspenseOutlet({ children }: { children: ReactNode }) {
  return (
    <Suspense
      fallback={
        <div className="text-text-tertiary flex min-h-screen items-center justify-center">
          Loading…
        </div>
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

        {/* Dev preview routes */}
        <Route
          path="/dev/consultants"
          element={
            <PublicLayout>
              <StudentConsultants />
            </PublicLayout>
          }
        />
        <Route
          path="/dev/consultants/:id"
          element={
            <PublicLayout>
              <StudentConsultantDetail />
            </PublicLayout>
          }
        />
        <Route
          path="/dev/checkout"
          element={
            <PublicLayout>
              <StudentBookingCheckout />
            </PublicLayout>
          }
        />
        <Route
          path="/dev/bookings"
          element={
            <PublicLayout>
              <StudentBookings />
            </PublicLayout>
          }
        />
        <Route
          path="/dev/bookings/:id"
          element={
            <PublicLayout>
              <StudentBookingDetails />
            </PublicLayout>
          }
        />
        <Route
          path="/dev/consultant/availability"
          element={
            <PublicLayout>
              <ConsultantAvailability />
            </PublicLayout>
          }
        />
        <Route
          path="/dev/consultant/bookings"
          element={
            <PublicLayout>
              <ConsultantBookings />
            </PublicLayout>
          }
        />
        <Route
          path="/dev/consultant/bookings/:id"
          element={
            <PublicLayout>
              <ConsultantBookingDetails />
            </PublicLayout>
          }
        />

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

        <Route
          element={
            <RequireAuth>
              <AuthenticatedLayout />
            </RequireAuth>
          }
        >
          <Route path="/profile" element={<Profile />} />
          <Route path="/profile/privacy" element={<DataPrivacy />} />
          <Route path="/notifications" element={<Notifications />} />

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
          <Route path="/student/checkout" element={<StudentBookingCheckout />} />
          <Route path="/student/bookings" element={<StudentBookings />} />
          <Route path="/student/bookings/:id" element={<StudentBookingDetails />} />
          <Route path="/student/community" element={<StudentCommunity />} />
          <Route path="/student/resources" element={<StudentResources />} />
          <Route path="/student/ai" element={<StudentAi />} />
          <Route path="/student/messages" element={<StudentMessages />} />

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
          <Route path="/consultant/bookings/:id" element={<ConsultantBookingDetails />} />
          <Route path="/consultant/earnings" element={<ConsultantEarnings />} />
        </Route>

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
          <Route path="/admin/ai-economy" element={<AdminAiEconomy />} />
          <Route path="/admin/redaction-audit" element={<AdminRedactionAudit />} />
          <Route path="/admin/scholarships" element={<AdminScholarships />} />
          <Route path="/admin/articles" element={<AdminArticles />} />
          <Route path="/admin/community" element={<AdminCommunity />} />
          <Route path="/admin/payments" element={<AdminPayments />} />
          <Route path="/admin/profit-share" element={<AdminProfitShare />} />
          <Route path="/admin/audit-log" element={<AdminAuditLog />} />
          <Route path="/admin/settings" element={<AdminSettings />} />
        </Route>

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

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

// ── PB-003: Scholarship Discovery ─────────────────────────────────────────────
const StudentScholarships = lazy(() =>
  import("@/pages/student/ScholarshipsPage").then((m) => ({ default: m.ScholarshipsPage })),
);
const StudentScholarshipDetail = lazy(() =>
  import("@/pages/student/ScholarshipDetail").then((m) => ({ default: m.ScholarshipDetail })),
);
const StudentBookmarks = lazy(() =>
  import("@/pages/student/BookmarksPage").then((m) => ({ default: m.BookmarksPage })),
);

// Student
const StudentDashboard = lazy(() =>
  import("@/pages/student/StudentDashboard").then((m) => ({ default: m.StudentDashboard })),
);
const StudentApplications = lazy(() =>
  import("@/pages/student/Applications").then((m) => ({ default: m.Applications })),
);
const StudentApplicationDetail = lazy(() =>
  import("@/pages/student/StudentApplicationDetail").then((m) => ({
    default: m.StudentApplicationDetail,
  })),
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
const StudentCommunity = lazy(() =>
  import("@/pages/community/Community").then((m) => ({ default: m.Community })),
);
const StudentCommunityThread = lazy(() =>
  import("@/pages/community/CommunityThread").then((m) => ({ default: m.CommunityThread })),
);
const StudentResources = lazy(() =>
  import("@/pages/student/StudentResources").then((m) => ({
    default: m.StudentResources,
  })),
);
const StudentDocuments = lazy(() =>
  import("@/pages/student/Documents").then((m) => ({ default: m.Documents })),
);
const StudentAi = lazy(() =>
  import("@/pages/student/AiFeatures").then((m) => ({ default: m.AiFeatures })),
);
const StudentMessages = lazy(() =>
  import("@/pages/chat/Chat").then((m) => ({ default: m.Chat })),
);

// Company
const CompanyDashboard = lazy(() =>
  import("@/pages/company/Dashboard").then((m) => ({ default: m.CompanyDashboard })),
);
const CompanyScholarships = lazy(() =>
  import("@/pages/company/CompanyScholarships").then((m) => ({
    default: m.CompanyScholarships,
  })),
);
const CompanyApplicationsReview = lazy(() =>
  import("@/pages/company/ApplicationsReview").then((m) => ({ default: m.ApplicationsReview })),
);
const CompanyBilling = lazy(() =>
  import("@/pages/company/CompanyBilling").then((m) => ({ default: m.CompanyBilling })),
);

// Consultant
const ConsultantDashboard = lazy(() =>
  import("@/pages/consultant/ConsultantDashboard").then((m) => ({
    default: m.ConsultantDashboard,
  })),
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
const ConsultantEarnings = lazy(() =>
  import("@/pages/consultant/ConsultantEarnings").then((m) => ({
    default: m.ConsultantEarnings,
  })),
);

// Admin
const AdminDashboard = lazy(() =>
  import("@/pages/admin/AdminDashboard").then((m) => ({ default: m.AdminDashboard })),
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
const AdminAiEconomy = lazy(() =>
  import("@/pages/admin/AiEconomyPage").then((m) => ({ default: m.AiEconomyPage })),
);
const AdminRedactionAudit = lazy(() =>
  import("@/pages/admin/RedactionAuditPage").then((m) => ({ default: m.RedactionAuditPage })),
);
const AdminScholarships = lazy(() =>
  import("@/pages/admin/AdminScholarships").then((m) => ({
    default: m.AdminScholarships,
  })),
);
const AdminArticles = lazy(() =>
  import("@/pages/admin/AdminArticles").then((m) => ({ default: m.AdminArticles })),
);
const AdminCommunity = lazy(() =>
  import("@/pages/admin/AdminCommunity").then((m) => ({ default: m.AdminCommunity })),
);
const AdminPayments = lazy(() =>
  import("@/pages/admin/AdminPayments").then((m) => ({ default: m.AdminPayments })),
);
const AdminProfitShare = lazy(() =>
  import("@/pages/admin/AdminProfitShare").then((m) => ({ default: m.AdminProfitShare })),
);
const AdminAuditLog = lazy(() =>
  import("@/pages/admin/AuditLogViewer").then((m) => ({ default: m.AuditLogViewer })),
);
const AdminSettings = lazy(() =>
  import("@/pages/admin/AdminSettings").then((m) => ({ default: m.AdminSettings })),
);

// Shared
const Profile = lazy(() =>
  import("@/pages/profile/Profile").then((m) => ({ default: m.Profile })),
);
const Notifications = lazy(() =>
  import("@/pages/notifications/Notifications").then((m) => ({ default: m.Notifications })),
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

        {/* Onboarding */}
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
          {/* Profile / Notifications — shared */}
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
          {/* PB-003: Scholarship Discovery */}
          <Route path="/student/scholarships"      element={<StudentScholarships />} />
          <Route path="/student/scholarships/:id"  element={<StudentScholarshipDetail />} />
          <Route path="/student/bookmarks"         element={<StudentBookmarks />} />

          {/* PB-004: Applications */}
          <Route path="/student/applications"      element={<StudentApplications />} />
          <Route path="/student/applications/:id"  element={<StudentApplicationDetail />} />

          {/* PB-006: Consultant Booking */}
          <Route path="/student/consultants"       element={<StudentConsultants />} />
          <Route path="/student/consultants/:id"   element={<StudentConsultantDetail />} />
          <Route path="/student/checkout"          element={<StudentBookingCheckout />} />
          <Route path="/student/bookings"          element={<StudentBookings />} />
          <Route path="/student/bookings/:id"      element={<StudentBookingDetails />} />

          {/* PB-007: Community + Chat */}
          <Route path="/student/community"         element={<StudentCommunity />} />
          <Route path="/student/community/:id"     element={<StudentCommunityThread />} />
          <Route path="/student/messages"          element={<StudentMessages />} />

          {/* Others */}
          <Route path="/student/resources"         element={<StudentResources />} />
          <Route path="/student/documents"         element={<StudentDocuments />} />
          <Route path="/student/ai"                element={<StudentAi />} />

          <Route
            path="/company"
            element={
              <RequireRole roles={["Company"]}>
                <CompanyDashboard />
              </RequireRole>
            }
          />
          <Route
            path="/company/scholarships"
            element={
              <RequireRole roles={["Company"]}>
                <CompanyScholarships />
              </RequireRole>
            }
          />
          <Route
            path="/company/applications-review"
            element={
              <RequireRole roles={["Company"]}>
                <CompanyApplicationsReview />
              </RequireRole>
            }
          />
          <Route
            path="/company/billing"
            element={
              <RequireRole roles={["Company"]}>
                <CompanyBilling />
              </RequireRole>
            }
          />

          <Route
            path="/consultant"
            element={
              <RequireRole roles={["Consultant"]}>
                <ConsultantDashboard />
              </RequireRole>
            }
          />
          <Route
            path="/consultant/availability"
            element={
              <RequireRole roles={["Consultant"]}>
                <ConsultantAvailability />
              </RequireRole>
            }
          />
          <Route
            path="/consultant/bookings"
            element={
              <RequireRole roles={["Consultant"]}>
                <ConsultantBookings />
              </RequireRole>
            }
          />
          <Route
            path="/consultant/bookings/:id"
            element={
              <RequireRole roles={["Consultant"]}>
                <ConsultantBookingDetails />
              </RequireRole>
            }
          />
          <Route
            path="/consultant/earnings"
            element={
              <RequireRole roles={["Consultant"]}>
                <ConsultantEarnings />
              </RequireRole>
            }
          />
        </Route>

        {/* Admin */}
        <Route
          element={
            <RequireAuth>
              <RequireRole roles={["Admin", "SuperAdmin"]}>
                <AdminLayout />
              </RequireRole>
            </RequireAuth>
          }
        >
          <Route path="/admin"                  element={<AdminDashboard />} />
          <Route path="/admin/users"            element={<AdminUsers />} />
          <Route path="/admin/onboarding"       element={<AdminOnboarding />} />
          <Route path="/admin/upgrades"         element={<AdminUpgrades />} />
          <Route path="/admin/broadcast"        element={<AdminBroadcast />} />
          <Route path="/admin/analytics"        element={<AdminAnalytics />} />
          <Route path="/admin/ai-economy"       element={<AdminAiEconomy />} />
          <Route path="/admin/redaction-audit"  element={<AdminRedactionAudit />} />
          <Route path="/admin/scholarships"     element={<AdminScholarships />} />
          <Route path="/admin/articles"         element={<AdminArticles />} />
          <Route path="/admin/community"        element={<AdminCommunity />} />
          <Route path="/admin/payments"         element={<AdminPayments />} />
          <Route path="/admin/profit-share"     element={<AdminProfitShare />} />
          <Route path="/admin/audit-log"        element={<AdminAuditLog />} />
          <Route path="/admin/settings"         element={<AdminSettings />} />
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

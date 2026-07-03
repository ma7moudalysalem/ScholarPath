import { AdminLayout } from "@/components/layout/AdminLayout";
import { AuthenticatedLayout } from "@/components/layout/AuthenticatedLayout";
import { PublicLayout } from "@/components/layout/PublicLayout";
import { AnimatedRoute } from "@/components/common/AnimatedRoute";
import { RequireAuth, RequireRole, RequirePayments } from "@/routes/RequireAuth";
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
const VerifyEmail = lazy(() =>
  import("@/pages/auth/VerifyEmail").then((m) => ({ default: m.VerifyEmail })),
);
const OnboardingWizard = lazy(() =>
  import("@/pages/auth/OnboardingWizard").then((m) => ({ default: m.OnboardingWizard })),
);
const SsoCallback = lazy(() =>
  import("@/pages/auth/SsoCallback").then((m) => ({ default: m.SsoCallback })),
);
const NotFound = lazy(() => import("@/pages/NotFound").then((m) => ({ default: m.NotFound })));

// ── Public informational pages (footer) ───────────────────────────────────────
const Privacy = lazy(() => import("@/pages/legal/Privacy").then((m) => ({ default: m.Privacy })));
const Terms = lazy(() => import("@/pages/legal/Terms").then((m) => ({ default: m.Terms })));
const Help = lazy(() => import("@/pages/legal/Help").then((m) => ({ default: m.Help })));
const About = lazy(() => import("@/pages/legal/About").then((m) => ({ default: m.About })));
const Contact = lazy(() => import("@/pages/legal/Contact").then((m) => ({ default: m.Contact })));

// ── PB-009: Resource author management ────────────────────────────────────────
const AuthorMyResources = lazy(() =>
  import("@/pages/author/MyResources").then((m) => ({ default: m.MyResources })),
);
const AuthorResourceEditor = lazy(() =>
  import("@/pages/author/ResourceEditor").then((m) => ({ default: m.ResourceEditor })),
);

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
const StudentResourceDetail = lazy(() =>
  import("@/pages/student/ResourceDetail").then((m) => ({
    default: m.ResourceDetail,
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

// PB-005: paid ScholarshipProviderReview support requests
const StudentReviewRequests = lazy(() =>
  import("@/pages/student/StudentReviewRequests").then((m) => ({
    default: m.StudentReviewRequests,
  })),
);
const ScholarshipProviderReviewRequestsPage = lazy(() =>
  import("@/pages/company/ScholarshipProviderReviewRequests").then((m) => ({
    default: m.ScholarshipProviderReviewRequests,
  })),
);

// ScholarshipProvider
const ScholarshipProviderDashboard = lazy(() =>
  import("@/pages/company/Dashboard").then((m) => ({ default: m.ScholarshipProviderDashboard })),
);
const ScholarshipProviderScholarships = lazy(() =>
  import("@/pages/company/ScholarshipProviderScholarships").then((m) => ({
    default: m.ScholarshipProviderScholarships,
  })),
);
const ScholarshipProviderScholarshipForm = lazy(() =>
  import("@/pages/company/ScholarshipForm").then((m) => ({
    default: m.ScholarshipForm,
  })),
);
const ScholarshipProviderApplicationsReview = lazy(() =>
  import("@/pages/company/ApplicationsReview").then((m) => ({ default: m.ApplicationsReview })),
);
const ScholarshipProviderBilling = lazy(() =>
  import("@/pages/company/ScholarshipProviderBilling").then((m) => ({ default: m.ScholarshipProviderBilling })),
);
const ScholarshipProviderInsights = lazy(() =>
  import("@/pages/company/ScholarshipProviderInsights").then((m) => ({ default: m.ScholarshipProviderInsights })),
);
const ScholarshipProviderReviews = lazy(() =>
  import("@/pages/company/ScholarshipProviderReviews").then((m) => ({ default: m.ScholarshipProviderReviews })),
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
const ConsultantAnalytics = lazy(() =>
  import("@/pages/consultant/ConsultantAnalytics").then((m) => ({
    default: m.ConsultantAnalytics,
  })),
);
const ConsultantEarningsTrend = lazy(() =>
  import("@/pages/consultant/ConsultantEarningsTrend").then((m) => ({
    default: m.ConsultantEarningsTrend,
  })),
);
const ConsultantReviews = lazy(() =>
  import("@/pages/consultant/ConsultantReviews").then((m) => ({
    default: m.ConsultantReviews,
  })),
);

// ── PB-015: Student self-analytics ────────────────────────────────────────────
const StudentAnalytics = lazy(() =>
  import("@/pages/student/StudentAnalytics").then((m) => ({
    default: m.StudentAnalytics,
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
const AdminLowRatedCompanies = lazy(() =>
  import("@/pages/admin/AdminLowRatedCompanies").then((m) => ({
    default: m.AdminLowRatedCompanies,
  })),
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
const AdminKnowledgeBase = lazy(() =>
  import("@/pages/admin/AdminKnowledgeBase").then((m) => ({ default: m.AdminKnowledgeBase })),
);
const AdminRedactionAudit = lazy(() =>
  import("@/pages/admin/RedactionAuditPage").then((m) => ({ default: m.RedactionAuditPage })),
);
const AdminScholarships = lazy(() =>
  import("@/pages/admin/AdminScholarships").then((m) => ({
    default: m.AdminScholarships,
  })),
);
const AdminFeaturedScholarships = lazy(() =>
  import("@/pages/admin/AdminFeaturedScholarships").then((m) => ({
    default: m.AdminFeaturedScholarships,
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
const AdminFinancialConfig = lazy(() =>
  import("@/pages/admin/AdminFinancialConfig").then((m) => ({
    default: m.AdminFinancialConfig,
  })),
);
const AdminAuditLog = lazy(() =>
  import("@/pages/admin/AuditLogViewer").then((m) => ({ default: m.AuditLogViewer })),
);
const AdminSettings = lazy(() =>
  import("@/pages/admin/AdminSettings").then((m) => ({ default: m.AdminSettings })),
);
const AdminRevenueReport = lazy(() =>
  import("@/pages/admin/AdminRevenueReport").then((m) => ({ default: m.AdminRevenueReport })),
);

// Shared
const Profile = lazy(() =>
  import("@/pages/profile/Profile").then((m) => ({ default: m.Profile })),
);
const Notifications = lazy(() =>
  import("@/pages/notifications/Notifications").then((m) => ({ default: m.Notifications })),
);
const NotificationPreferences = lazy(() =>
  import("@/pages/notifications/NotificationPreferences").then((m) => ({ default: m.NotificationPreferences })),
);
const DataPrivacy = lazy(() =>
  import("@/pages/profile/DataPrivacy").then((m) => ({ default: m.DataPrivacy })),
);
const Meeting = lazy(() =>
  import("@/pages/meeting/Meeting").then((m) => ({ default: m.Meeting })),
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
              <AnimatedRoute>
                <Home />
              </AnimatedRoute>
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
          path="/verify-email"
          element={
            <PublicLayout>
              <VerifyEmail />
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

        {/* Public informational pages — Privacy / Terms / Help / About / Contact */}
        <Route
          path="/legal/privacy"
          element={
            <PublicLayout>
              <AnimatedRoute><Privacy /></AnimatedRoute>
            </PublicLayout>
          }
        />
        <Route
          path="/legal/terms"
          element={
            <PublicLayout>
              <AnimatedRoute><Terms /></AnimatedRoute>
            </PublicLayout>
          }
        />
        <Route
          path="/legal/contact"
          element={
            <PublicLayout>
              <AnimatedRoute><Contact /></AnimatedRoute>
            </PublicLayout>
          }
        />
        <Route
          path="/help"
          element={
            <PublicLayout>
              <AnimatedRoute><Help /></AnimatedRoute>
            </PublicLayout>
          }
        />
        <Route
          path="/about"
          element={
            <PublicLayout>
              <AnimatedRoute><About /></AnimatedRoute>
            </PublicLayout>
          }
        />
        <Route
          path="/contact"
          element={
            <PublicLayout>
              <AnimatedRoute><Contact /></AnimatedRoute>
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

        {/* PB-006 video session — full-screen, no portal chrome */}
        <Route
          path="/meeting/:bookingId"
          element={
            <RequireAuth>
              <Meeting />
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
          <Route path="/profile" element={<AnimatedRoute><Profile /></AnimatedRoute>} />
          <Route path="/profile/privacy" element={<AnimatedRoute><DataPrivacy /></AnimatedRoute>} />
          <Route path="/notifications" element={<AnimatedRoute><Notifications /></AnimatedRoute>} />
          <Route path="/notifications/preferences" element={<AnimatedRoute><NotificationPreferences /></AnimatedRoute>} />

          <Route
            path="/student"
            element={
              <RequireRole roles={["Student"]}>
                <AnimatedRoute>
                  <StudentDashboard />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          {/* PB-003: Scholarship Discovery */}
          <Route path="/student/scholarships"      element={<AnimatedRoute><StudentScholarships /></AnimatedRoute>} />
          <Route path="/student/scholarships/:id"  element={<AnimatedRoute><StudentScholarshipDetail /></AnimatedRoute>} />
          <Route path="/student/bookmarks"         element={<AnimatedRoute><StudentBookmarks /></AnimatedRoute>} />

          {/* PB-004: Applications */}
          <Route path="/student/applications"      element={<AnimatedRoute><StudentApplications /></AnimatedRoute>} />
          <Route path="/student/applications/:id"  element={<AnimatedRoute><StudentApplicationDetail /></AnimatedRoute>} />

          {/* PB-005: paid ScholarshipProviderReview support requests */}
          <Route
            path="/student/review-requests"
            element={
              <RequireRole roles={["Student"]}>
                <AnimatedRoute><StudentReviewRequests /></AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/student/review-requests/:id"
            element={
              <RequireRole roles={["Student"]}>
                <AnimatedRoute><StudentReviewRequests /></AnimatedRoute>
              </RequireRole>
            }
          />

          {/* PB-006: Consultant Booking */}
          <Route path="/student/consultants"       element={<AnimatedRoute><StudentConsultants /></AnimatedRoute>} />
          <Route path="/student/consultants/:id"   element={<AnimatedRoute><StudentConsultantDetail /></AnimatedRoute>} />
          <Route path="/student/checkout"          element={<AnimatedRoute><StudentBookingCheckout /></AnimatedRoute>} />
          <Route path="/student/bookings"          element={<AnimatedRoute><StudentBookings /></AnimatedRoute>} />
          <Route path="/student/bookings/:id"      element={<AnimatedRoute><StudentBookingDetails /></AnimatedRoute>} />

          {/* PB-007: Community + Chat */}
          <Route path="/student/community"         element={<AnimatedRoute><StudentCommunity /></AnimatedRoute>} />
          <Route path="/student/community/:id"     element={<AnimatedRoute><StudentCommunityThread /></AnimatedRoute>} />
          <Route path="/student/messages"          element={<AnimatedRoute><StudentMessages /></AnimatedRoute>} />
          {/* Same Chat component is mounted under each role prefix so deep
              links from the notification dispatcher and from cross-role chat
              partners resolve correctly. Without these, an email link sent
              to a consultant would 404 on /consultant/messages. */}
          <Route path="/consultant/messages"       element={<AnimatedRoute><StudentMessages /></AnimatedRoute>} />
          <Route path="/company/messages"          element={<AnimatedRoute><StudentMessages /></AnimatedRoute>} />

          {/* Others */}
          <Route path="/student/resources"         element={<AnimatedRoute><StudentResources /></AnimatedRoute>} />
          <Route path="/student/resources/:idOrSlug" element={<AnimatedRoute><StudentResourceDetail /></AnimatedRoute>} />
          <Route path="/student/documents"         element={<AnimatedRoute><StudentDocuments /></AnimatedRoute>} />
          <Route path="/student/ai"                element={<AnimatedRoute><StudentAi /></AnimatedRoute>} />
          <Route path="/student/analytics"         element={<AnimatedRoute><StudentAnalytics /></AnimatedRoute>} />

          {/* PB-009: Author resource management (Consultant, ScholarshipProvider, Admin) */}
          <Route
            path="/author/resources"
            element={
              <RequireRole roles={["Consultant", "ScholarshipProvider", "Admin", "SuperAdmin"]}>
                <AnimatedRoute>
                  <AuthorMyResources />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/author/resources/new"
            element={
              <RequireRole roles={["Consultant", "ScholarshipProvider", "Admin", "SuperAdmin"]}>
                <AnimatedRoute>
                  <AuthorResourceEditor />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/author/resources/:id/edit"
            element={
              <RequireRole roles={["Consultant", "ScholarshipProvider", "Admin", "SuperAdmin"]}>
                <AnimatedRoute>
                  <AuthorResourceEditor />
                </AnimatedRoute>
              </RequireRole>
            }
          />

          <Route
            path="/company"
            element={
              <RequireRole roles={["ScholarshipProvider"]}>
                <AnimatedRoute>
                  <ScholarshipProviderDashboard />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/company/scholarships"
            element={
              <RequireRole roles={["ScholarshipProvider"]}>
                <AnimatedRoute>
                  <ScholarshipProviderScholarships />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/company/scholarships/new"
            element={
              <RequireRole roles={["ScholarshipProvider"]}>
                <AnimatedRoute>
                  <ScholarshipProviderScholarshipForm />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/company/scholarships/:id/edit"
            element={
              <RequireRole roles={["ScholarshipProvider"]}>
                <AnimatedRoute>
                  <ScholarshipProviderScholarshipForm />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/company/review-requests"
            element={
              <RequireRole roles={["ScholarshipProvider"]}>
                <AnimatedRoute>
                  <ScholarshipProviderReviewRequestsPage />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/company/applications-review"
            element={
              <RequireRole roles={["ScholarshipProvider"]}>
                <AnimatedRoute>
                  <ScholarshipProviderApplicationsReview />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/company/billing"
            element={
              <RequireRole roles={["ScholarshipProvider"]}>
                <RequirePayments>
                  <AnimatedRoute>
                    <ScholarshipProviderBilling />
                  </AnimatedRoute>
                </RequirePayments>
              </RequireRole>
            }
          />
          <Route
            path="/company/insights"
            element={
              <RequireRole roles={["ScholarshipProvider"]}>
                <AnimatedRoute>
                  <ScholarshipProviderInsights />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/company/reviews"
            element={
              <RequireRole roles={["ScholarshipProvider"]}>
                <AnimatedRoute>
                  <ScholarshipProviderReviews />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/consultant"
            element={
              <RequireRole roles={["Consultant"]}>
                <AnimatedRoute>
                  <ConsultantDashboard />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/consultant/availability"
            element={
              <RequireRole roles={["Consultant"]}>
                <AnimatedRoute>
                  <ConsultantAvailability />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/consultant/bookings"
            element={
              <RequireRole roles={["Consultant"]}>
                <AnimatedRoute>
                  <ConsultantBookings />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/consultant/bookings/:id"
            element={
              <RequireRole roles={["Consultant"]}>
                <AnimatedRoute>
                  <ConsultantBookingDetails />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/consultant/earnings"
            element={
              <RequireRole roles={["Consultant"]}>
                <RequirePayments>
                  <AnimatedRoute>
                    <ConsultantEarnings />
                  </AnimatedRoute>
                </RequirePayments>
              </RequireRole>
            }
          />
          <Route
            path="/consultant/analytics"
            element={
              <RequireRole roles={["Consultant"]}>
                <AnimatedRoute>
                  <ConsultantAnalytics />
                </AnimatedRoute>
              </RequireRole>
            }
          />
          <Route
            path="/consultant/earnings-trend"
            element={
              <RequireRole roles={["Consultant"]}>
                <RequirePayments>
                  <AnimatedRoute>
                    <ConsultantEarningsTrend />
                  </AnimatedRoute>
                </RequirePayments>
              </RequireRole>
            }
          />
          <Route
            path="/consultant/reviews"
            element={
              <RequireRole roles={["Consultant"]}>
                <AnimatedRoute>
                  <ConsultantReviews />
                </AnimatedRoute>
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
          <Route path="/admin"                  element={<AnimatedRoute><AdminDashboard /></AnimatedRoute>} />
          <Route path="/admin/users"            element={<AnimatedRoute><AdminUsers /></AnimatedRoute>} />
          <Route path="/admin/onboarding"       element={<AnimatedRoute><AdminOnboarding /></AnimatedRoute>} />
          <Route path="/admin/upgrades"         element={<AnimatedRoute><AdminUpgrades /></AnimatedRoute>} />
          {/* PB-005R: low-rated companies admin queue */}
          <Route path="/admin/low-rated-companies" element={<AnimatedRoute><AdminLowRatedCompanies /></AnimatedRoute>} />
          <Route path="/admin/broadcast"        element={<AnimatedRoute><AdminBroadcast /></AnimatedRoute>} />
          <Route path="/admin/analytics"        element={<AnimatedRoute><AdminAnalytics /></AnimatedRoute>} />
          <Route path="/admin/reports/revenue"  element={<RequirePayments><AnimatedRoute><AdminRevenueReport /></AnimatedRoute></RequirePayments>} />
          <Route path="/admin/ai-economy"       element={<AnimatedRoute><AdminAiEconomy /></AnimatedRoute>} />
          <Route path="/admin/knowledge-base"   element={<AnimatedRoute><AdminKnowledgeBase /></AnimatedRoute>} />
          <Route path="/admin/redaction-audit"  element={<AnimatedRoute><AdminRedactionAudit /></AnimatedRoute>} />
          <Route path="/admin/scholarships"          element={<AnimatedRoute><AdminScholarships /></AnimatedRoute>} />
          <Route path="/admin/scholarships/new"      element={<AnimatedRoute><ScholarshipProviderScholarshipForm /></AnimatedRoute>} />
          <Route path="/admin/featured-scholarships" element={<AnimatedRoute><AdminFeaturedScholarships /></AnimatedRoute>} />
          <Route path="/admin/articles"         element={<AnimatedRoute><AdminArticles /></AnimatedRoute>} />
          <Route path="/admin/community"        element={<AnimatedRoute><AdminCommunity /></AnimatedRoute>} />
          <Route path="/admin/payments"         element={<RequirePayments><AnimatedRoute><AdminPayments /></AnimatedRoute></RequirePayments>} />
          <Route path="/admin/profit-share"     element={<RequirePayments><AnimatedRoute><AdminProfitShare /></AnimatedRoute></RequirePayments>} />
          <Route path="/admin/financial-config" element={<RequirePayments><AnimatedRoute><AdminFinancialConfig /></AnimatedRoute></RequirePayments>} />
          <Route path="/admin/audit-log"        element={<AnimatedRoute><AdminAuditLog /></AnimatedRoute>} />
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

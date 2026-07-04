using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Notifications;

/// <summary>
/// The single, centralized source of bilingual (EN/AR) notification text (Task 5B).
/// Handlers pass a <see cref="NotificationType"/> + <see cref="NotificationParams"/>;
/// this renders the title and body for both languages. Replaces the EN/AR string
/// literals that were previously scattered across ~a dozen handlers.
/// </summary>
public sealed class NotificationCatalog : INotificationCatalog
{
    public NotificationContent Render(NotificationType type, NotificationParams p) => type switch
    {
        NotificationType.ApplicationStatusChanged => new(
            "Application updated", "تحديث على الطلب",
            $"Your application status changed to {p.StatusText ?? "updated"}." +
                (string.IsNullOrWhiteSpace(p.Reason) ? "" : $" Reason: {p.Reason}"),
            $"تغيّرت حالة طلبك إلى {p.StatusText ?? "محدّثة"}." +
                (string.IsNullOrWhiteSpace(p.Reason) ? "" : $" السبب: {p.Reason}")),

        // Sent to the scholarship's owning company when a student submits a new
        // in-app application (QA BUG-017 — the company was never told).
        NotificationType.ApplicationSubmitted => new(
            "New application received", "طلب جديد وارد",
            $"A new application was submitted for \"{p.TitleEn ?? "your scholarship"}\".",
            $"تم استلام طلب جديد على \"{p.TitleAr ?? "منحتك"}\"."),

        NotificationType.ApplicationWithdrawn => new(
            "Application withdrawn", "تم سحب الطلب",
            "Your application has been withdrawn.",
            "تم سحب طلبك."),

        // Sent to the student to confirm their own submission (FR-APP-17).
        NotificationType.ApplicationSubmittedConfirmation => new(
            "Application submitted", "تم إرسال طلبك",
            $"Your application for \"{p.TitleEn ?? "the scholarship"}\" was submitted successfully.",
            $"تم إرسال طلبك للمنحة \"{p.TitleAr ?? "المنحة"}\" بنجاح."),

        // Sent to the owning provider when an admin approves/rejects their listing.
        NotificationType.ScholarshipApproved => new(
            "Scholarship approved", "تمت الموافقة على المنحة",
            $"Your scholarship \"{p.TitleEn ?? "listing"}\" was approved and is now live.",
            $"تمت الموافقة على منحتك \"{p.TitleAr ?? "المنحة"}\" وأصبحت متاحة الآن."),

        NotificationType.ScholarshipRejected => new(
            "Scholarship needs changes", "المنحة تحتاج تعديلات",
            $"Your scholarship \"{p.TitleEn ?? "listing"}\" was returned to draft: {p.Reason ?? "see the review notes"}.",
            $"أُعيدت منحتك \"{p.TitleAr ?? "المنحة"}\" إلى المسودة: {p.Reason ?? "راجِع ملاحظات المراجعة"}."),

        NotificationType.ApplicationDeadlineApproaching => new(
            "Scholarship deadline approaching", "اقتراب موعد إغلاق المنحة",
            $"\"{p.TitleEn ?? "A scholarship you saved"}\" closes in {p.Count ?? 0} day(s) — submit before the deadline.",
            $"تُغلق \"{p.TitleAr ?? "منحة قمت بحفظها"}\" خلال {p.Count ?? 0} يوم — قدّم طلبك قبل الموعد النهائي."),

        NotificationType.ApplicationDraftReminder => new(
            "Finish your draft application", "أكمل طلبك المحفوظ",
            $"You have an unsubmitted draft application for \"{p.TitleEn ?? "a scholarship"}\" — complete and submit it before the deadline.",
            $"لديك طلب مسودة لم يُرسَل للمنحة \"{p.TitleAr ?? "إحدى المنح"}\" — أكمله وأرسله قبل الموعد النهائي."),

        NotificationType.ScholarshipProviderReviewPaymentSuccess => new(
            "Review fee paid", "تم دفع رسوم المراجعة",
            "Your scholarship review-fee payment was successful.",
            "تم دفع رسوم مراجعة المنحة بنجاح."),

        NotificationType.ScholarshipProviderReviewRefunded => RenderReviewRefund(p),

        NotificationType.ScholarshipProviderReviewRequestPaymentHeld => new(
            "Review fee on hold", "تم حجز رسوم المراجعة",
            $"Your card was authorised for {p.HeldAmountText ?? "the review fee"} on \"{p.ScholarshipNameEn ?? "the scholarship"}\" — the amount is held but not captured. The ScholarshipProvider will be charged only if they accept your request. Reference: {p.PaymentReference ?? "—"}.",
            $"تم حجز مبلغ {p.HeldAmountText ?? "رسوم المراجعة"} على بطاقتك للمنحة \"{p.ScholarshipNameAr ?? "المنحة"}\" — لن يُخصم المبلغ ما لم تقبل الشركة الطلب. المرجع: {p.PaymentReference ?? "—"}."),

        NotificationType.ScholarshipProviderReviewRequestPaymentCaptured => new(
            "Review fee charged", "تم خصم رسوم المراجعة",
            $"The ScholarshipProvider accepted your request — {p.CapturedAmountText ?? "the review fee"} was captured from your card for \"{p.ScholarshipNameEn ?? "the scholarship"}\". Reference: {p.PaymentReference ?? "—"}.",
            $"قبلت الشركة طلبك — تم خصم {p.CapturedAmountText ?? "رسوم المراجعة"} من بطاقتك للمنحة \"{p.ScholarshipNameAr ?? "المنحة"}\". المرجع: {p.PaymentReference ?? "—"}."),

        NotificationType.ScholarshipProviderReviewRequestPaymentHoldCancelled => new(
            "Review fee hold released", "تم الإفراج عن حجز رسوم المراجعة",
            $"Your request for \"{p.ScholarshipNameEn ?? "the scholarship"}\" was {p.RequestStatusText ?? "closed"} — the {p.HeldAmountText ?? "card hold"} was released and nothing was captured. Reference: {p.PaymentReference ?? "—"}.",
            $"تم {p.RequestStatusText ?? "إغلاق"} طلبك للمنحة \"{p.ScholarshipNameAr ?? "المنحة"}\" — أُفرج عن حجز {p.HeldAmountText ?? "البطاقة"} ولم يُخصم أي مبلغ. المرجع: {p.PaymentReference ?? "—"}."),

        NotificationType.ScholarshipProviderReviewRequestPartiallyRefunded => new(
            "50% review fee refunded", "تم استرداد 50% من رسوم المراجعة",
            $"Your cancellation of \"{p.ScholarshipNameEn ?? "the scholarship"}\" request was processed — {p.RefundAmountText ?? "50%"} refunded, {p.RetainedAmountText ?? "the remainder"} retained. Reference: {p.PaymentReference ?? "—"}.",
            $"تمت معالجة إلغائك لطلب \"{p.ScholarshipNameAr ?? "المنحة"}\" — تم استرداد {p.RefundAmountText ?? "50%"} والاحتفاظ بـ {p.RetainedAmountText ?? "النصف المتبقي"}. المرجع: {p.PaymentReference ?? "—"}."),

        NotificationType.ScholarshipProviderReviewRequestCompleted => new(
            "Application support service completed", "اكتملت خدمة دعم الطلب",
            $"The ScholarshipProvider marked your support service for \"{p.ScholarshipNameEn ?? "the scholarship"}\" as completed. Reference: {p.PaymentReference ?? "—"}.",
            $"وضعت الشركة خدمة دعم طلبك للمنحة \"{p.ScholarshipNameAr ?? "المنحة"}\" كمكتملة. المرجع: {p.PaymentReference ?? "—"}."),

        NotificationType.ScholarshipProviderReviewRequestIncoming => new(
            "New paid review request", "طلب مراجعة مدفوع جديد",
            $"{p.CounterpartyName ?? "A student"} submitted a paid review request for \"{p.ScholarshipNameEn ?? "your scholarship"}\". Hold: {p.HeldAmountText ?? "—"}. Accept to capture and start the service. Reference: {p.PaymentReference ?? "—"}.",
            $"قدّم {p.CounterpartyName ?? "أحد الطلاب"} طلب مراجعة مدفوع للمنحة \"{p.ScholarshipNameAr ?? "الخاصة بك"}\". الحجز: {p.HeldAmountText ?? "—"}. اقبل الطلب لخصم المبلغ وبدء الخدمة. المرجع: {p.PaymentReference ?? "—"}."),

        NotificationType.ScholarshipProviderRatingReceived => new(
            "New company rating", "تقييم جديد للشركة",
            $"You received a {p.Count ?? 0}-star company rating.",
            $"حصلت على تقييم للشركة بـ {p.Count ?? 0} نجوم."),

        NotificationType.ResourceApproved => new(
            "Resource published", "تم نشر المورد",
            $"Your resource \"{p.TitleEn ?? "resource"}\" is now live.",
            $"تم نشر المورد \"{p.TitleAr ?? "المورد"}\" بنجاح."),

        NotificationType.ResourceRejected => new(
            "Resource needs changes", "المورد يحتاج تعديلات",
            $"Your resource \"{p.TitleEn ?? "resource"}\" was sent back: {p.Reason ?? "see the review notes"}.",
            $"أُعيد المورد \"{p.TitleAr ?? "المورد"}\" للتعديل: {p.Reason ?? "راجِع ملاحظات المراجعة"}."),

        NotificationType.PostAutoHidden => new(
            "Post hidden for review", "تم إخفاء منشور للمراجعة",
            $"A community post was automatically hidden after {p.Count ?? 0} flags.",
            $"تم إخفاء منشور في المجتمع تلقائيًا بعد {p.Count ?? 0} بلاغات."),

        NotificationType.ReplyOnYourPost => new(
            "New reply on your post", "رد جديد على منشورك",
            string.IsNullOrEmpty(p.TitleEn)
                ? $"{p.CounterpartyName ?? "Someone"} replied to your community post."
                : $"{p.CounterpartyName ?? "Someone"} replied to \"{p.TitleEn}\".",
            string.IsNullOrEmpty(p.TitleAr)
                ? $"رد {p.CounterpartyName ?? "أحدهم"} على منشورك في المجتمع."
                : $"رد {p.CounterpartyName ?? "أحدهم"} على \"{p.TitleAr}\"."),

        NotificationType.PaymentSuccess => new(
            "Payment receipt", "إيصال الدفع",
            $"Your payment of {p.AmountText ?? "the session fee"} was received — keep this as your receipt.",
            $"تم استلام دفعتك بقيمة {p.AmountText ?? "رسوم الجلسة"} — احتفظ بهذا الإشعار كإيصال."),

        NotificationType.PaymentReceived => new(
            "Payment received", "تم استلام دفعة",
            $"You received a payment of {p.AmountText ?? "the session fee"} for your consultation.",
            $"استلمت دفعة بقيمة {p.AmountText ?? "رسوم الجلسة"} مقابل الاستشارة."),

        NotificationType.PaymentHeld => new(
            "Card authorised", "تم حجز المبلغ على البطاقة",
            $"Your card was authorised for {p.AmountText ?? "the session fee"} — the charge is held until the consultant accepts your booking.",
            $"تم حجز مبلغ {p.AmountText ?? "رسوم الجلسة"} على بطاقتك — لن يُخصم المبلغ حتى يقبل المستشار الحجز."),

        NotificationType.PaymentRefunded => new(
            "Payment refunded", "تم استرداد الدفعة",
            $"A refund of {p.AmountText ?? "your payment"} has been issued to your card.",
            $"تم استرداد مبلغ {p.AmountText ?? "دفعتك"} إلى بطاقتك."),

        NotificationType.PaymentDisputed => new(
            "Payment dispute opened", "تم فتح نزاع على دفعة",
            $"A cardholder opened a payment dispute ({p.Reason ?? "unspecified"}). Review it in the payments dashboard.",
            $"فتح حامل البطاقة نزاعًا على دفعة ({p.Reason ?? "غير محدد"}). راجِع النزاع من لوحة المدفوعات."),

        NotificationType.PayoutInitiated => new(
            "Payout on the way", "دفعتك في الطريق",
            $"A payout of {p.AmountText ?? "your earnings"} is on its way to your account.",
            $"دفعة بقيمة {p.AmountText ?? "أرباحك"} في طريقها إلى حسابك."),

        NotificationType.PayoutFailed => new(
            "Payout failed", "فشلت الدفعة",
            "We couldn't complete your payout. Our team is looking into it.",
            "تعذّر إتمام دفعتك، وفريقنا يتابع المشكلة."),

        NotificationType.BookingRequested => new(
            "New booking request", "طلب حجز جديد",
            $"{p.CounterpartyName ?? "A student"} has requested a session with you{(string.IsNullOrEmpty(p.StartAtText) ? string.Empty : $" on {p.StartAtText}")}. Review the request from your bookings page.",
            $"طلب {p.CounterpartyName ?? "أحد الطلاب"} حجز جلسة معك{(string.IsNullOrEmpty(p.StartAtText) ? string.Empty : $" بتاريخ {p.StartAtText}")}. راجِع الطلب من صفحة حجوزاتك."),

        NotificationType.BookingConfirmed => new(
            "Booking confirmed", "تم تأكيد الحجز",
            $"Your booking{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" with {p.CounterpartyName}")} is confirmed{(string.IsNullOrEmpty(p.StartAtText) ? string.Empty : $" for {p.StartAtText}")}.",
            $"تم تأكيد حجزك{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" مع {p.CounterpartyName}")}{(string.IsNullOrEmpty(p.StartAtText) ? string.Empty : $" بتاريخ {p.StartAtText}")}."),

        NotificationType.BookingRejected => new(
            "Booking declined", "تم رفض الحجز",
            $"Your booking{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" with {p.CounterpartyName}")} was declined — any payment hold has been released.",
            $"تم رفض حجزك{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" مع {p.CounterpartyName}")} — تم إلغاء أي حجز للمبلغ على بطاقتك."),

        NotificationType.BookingCancelled => new(
            "Booking cancelled", "تم إلغاء الحجز",
            $"A booking{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" with {p.CounterpartyName}")}{(string.IsNullOrEmpty(p.StartAtText) ? string.Empty : $" on {p.StartAtText}")} was cancelled{(string.IsNullOrEmpty(p.Reason) ? string.Empty : $" ({p.Reason})")}.",
            $"تم إلغاء حجز{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" مع {p.CounterpartyName}")}{(string.IsNullOrEmpty(p.StartAtText) ? string.Empty : $" بتاريخ {p.StartAtText}")}{(string.IsNullOrEmpty(p.Reason) ? string.Empty : $" ({p.Reason})")}."),

        // Sent to the student when a booking request expires unanswered and the
        // SessionExpiryJob releases the payment hold (previously silent).
        NotificationType.BookingExpired => new(
            "Booking request expired", "انتهت صلاحية طلب الحجز",
            $"Your booking request{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" with {p.CounterpartyName}")} expired before it was answered — any payment hold has been released. You can request another session anytime.",
            $"انتهت صلاحية طلب حجزك{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" مع {p.CounterpartyName}")} قبل الرد عليه — تم إلغاء أي حجز للمبلغ على بطاقتك. يمكنك طلب جلسة أخرى في أي وقت."),

        NotificationType.BookingCompleted => new(
            "Session completed", "اكتملت الجلسة",
            $"Your session{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" with {p.CounterpartyName}")} is complete — you can now leave a rating.",
            $"اكتملت جلستك{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" مع {p.CounterpartyName}")} — يمكنك الآن إضافة تقييمك."),

        NotificationType.BookingReminder => RenderBookingReminder(p),

        NotificationType.OnboardingApproved => new(
            "Account approved", "تم اعتماد حسابك",
            "Your onboarding request was approved — your account is now active.",
            "تمت الموافقة على طلب انضمامك — حسابك الآن مُفعَّل."),

        NotificationType.OnboardingRejected => new(
            "Onboarding not approved", "لم يُعتمد طلب الانضمام",
            $"Your onboarding request was not approved: {p.Reason ?? "please contact support for details"}.",
            $"لم تتم الموافقة على طلب انضمامك: {p.Reason ?? "يرجى التواصل مع الدعم لمعرفة التفاصيل"}."),

        NotificationType.UpgradeRequestApproved => new(
            "Upgrade approved", "تمت الموافقة على الترقية",
            $"Your request to become a {p.StatusText ?? "new role"} was approved.",
            $"تمت الموافقة على طلب ترقيتك إلى {p.StatusText ?? "الدور الجديد"}."),

        NotificationType.UpgradeRequestRejected => new(
            "Upgrade not approved", "لم تتم الموافقة على الترقية",
            $"Your upgrade request was not approved: {p.Reason ?? "see the reviewer notes"}.",
            $"لم تتم الموافقة على طلب ترقيتك: {p.Reason ?? "راجِع ملاحظات المراجِع"}."),

        // Admin-inbound: a new applicant is waiting in the onboarding queue.
        NotificationType.OnboardingSubmitted => new(
            "New onboarding request", "طلب انضمام جديد",
            $"{p.CounterpartyName ?? "An applicant"} submitted a {p.StatusText ?? "partner"} onboarding request for review.",
            $"قدّم {p.CounterpartyName ?? "أحد المتقدمين"} طلب انضمام ({p.StatusText ?? "شريك"}) للمراجعة."),

        // Admin-inbound: a student wants to become a consultant.
        NotificationType.UpgradeRequestSubmitted => new(
            "New upgrade request", "طلب ترقية جديد",
            $"{p.CounterpartyName ?? "A student"} submitted a {p.StatusText ?? "consultant"} upgrade request for review.",
            $"قدّم {p.CounterpartyName ?? "أحد الطلاب"} طلب ترقية إلى {p.StatusText ?? "مستشار"} للمراجعة."),

        // Admin-inbound: a ScholarshipProvider's average rating fell below the low-rating
        // threshold (PB-005R). AmountText carries the formatted average and
        // Count carries the number of visible reviews behind that average.
        NotificationType.ScholarshipProviderLowRatingFlagged => new(
            "ScholarshipProvider flagged for low rating", "تم تنبيه على شركة بسبب تقييم منخفض",
            $"{p.CounterpartyName ?? "A company"} dropped to an average rating of {p.AmountText ?? "below threshold"} over {p.Count ?? 0} review(s). Review and decide whether to suspend the account.",
            $"انخفض متوسط تقييم {p.CounterpartyName ?? "إحدى الشركات"} إلى {p.AmountText ?? "أقل من الحد"} عبر {p.Count ?? 0} تقييم. راجِع الحساب وقرّر ما إذا كان يجب إيقافه."),

        // Admin-inbound: a consultant's penalized average dropped below threshold (PB-006R).
        NotificationType.ConsultantLowRatingFlagged => new(
            "Consultant flagged for low rating", "تم تنبيه على مستشار بسبب تقييم منخفض",
            $"{p.CounterpartyName ?? "A consultant"} dropped to an average rating of {p.AmountText ?? "below threshold"} over {p.Count ?? 0} review(s). Review the consultant and decide on any action.",
            $"انخفض متوسط تقييم {p.CounterpartyName ?? "أحد المستشارين"} إلى {p.AmountText ?? "أقل من الحد"} عبر {p.Count ?? 0} تقييم. راجِع المستشار وقرّر الإجراء المناسب."),

        // Admin-inbound: a community post was reported and needs a moderation decision.
        NotificationType.ContentReported => new(
            "Post reported", "تم الإبلاغ عن منشور",
            $"A community post was reported{(string.IsNullOrEmpty(p.Reason) ? string.Empty : $" ({p.Reason})")} and needs a moderation decision.",
            $"تم الإبلاغ عن منشور في المجتمع{(string.IsNullOrEmpty(p.Reason) ? string.Empty : $" ({p.Reason})")} ويحتاج إلى قرار إشراف."),

        NotificationType.ChatMessageReceived => new(
            "New message", "رسالة جديدة",
            string.IsNullOrEmpty(p.Preview)
                ? $"{p.CounterpartyName ?? "Someone"} sent you a message."
                : $"{p.CounterpartyName ?? "Someone"}: {p.Preview}",
            string.IsNullOrEmpty(p.Preview)
                ? $"أرسل {p.CounterpartyName ?? "أحدهم"} رسالة جديدة لك."
                : $"{p.CounterpartyName ?? "أحدهم"}: {p.Preview}"),

        // Admin-authored announcement — text comes through verbatim, not templated.
        NotificationType.Broadcast => p.RawContent ?? new(
            "Announcement", "إعلان", string.Empty, string.Empty),

        // Safe fallback so an un-templated type never throws.
        _ => new(
            "New notification", "إشعار جديد",
            "You have a new notification.",
            "لديك إشعار جديد."),
    };

    // BookingReminder has two variants — a day-out heads-up and a final 1-hour ping — so
    // the catalog picks the wording per kind and falls back to a neutral message if the
    // discriminator was somehow missed.
    private static NotificationContent RenderBookingReminder(NotificationParams p) => p.ReminderKind switch
    {
        "1h" => new(
            "Session starts in 1 hour", "تبدأ جلستك خلال ساعة",
            $"Your session{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" with {p.CounterpartyName}")} starts in about 1 hour{(string.IsNullOrEmpty(p.StartAtText) ? string.Empty : $" ({p.StartAtText})")}.",
            $"تبدأ جلستك{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" مع {p.CounterpartyName}")} خلال ساعة تقريبًا{(string.IsNullOrEmpty(p.StartAtText) ? string.Empty : $" ({p.StartAtText})")}."),

        "24h" => new(
            "Session reminder — tomorrow", "تذكير بالجلسة — غدًا",
            $"Reminder: your session{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" with {p.CounterpartyName}")} is scheduled in about 24 hours{(string.IsNullOrEmpty(p.StartAtText) ? string.Empty : $" on {p.StartAtText}")}.",
            $"تذكير: جلستك{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" مع {p.CounterpartyName}")} مجدولة خلال 24 ساعة تقريبًا{(string.IsNullOrEmpty(p.StartAtText) ? string.Empty : $" بتاريخ {p.StartAtText}")}."),

        _ => new(
            "Session reminder", "تذكير بالجلسة",
            $"Reminder: your session{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" with {p.CounterpartyName}")} is coming up{(string.IsNullOrEmpty(p.StartAtText) ? string.Empty : $" on {p.StartAtText}")}.",
            $"تذكير: جلستك{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" مع {p.CounterpartyName}")} قادمة{(string.IsNullOrEmpty(p.StartAtText) ? string.Empty : $" بتاريخ {p.StartAtText}")}."),
    };

    private static NotificationContent RenderReviewRefund(NotificationParams p) => p.RefundKind switch
    {
        "Partial" => new(
            "Review fee partially refunded", "استرداد جزئي لرسوم المراجعة",
            "A 50% refund of your scholarship review fee has been issued.",
            "تم استرداد 50% من رسوم مراجعة المنحة."),

        "Timeout" => new(
            "Review fee refunded", "تم استرداد رسوم المراجعة",
            "The company did not review your application in time — your review fee was fully refunded.",
            "لم تُراجع الشركة طلبك في الوقت المحدد — تم استرداد كامل رسوم المراجعة."),

        _ => new(
            "Review fee refunded", "تم استرداد رسوم المراجعة",
            "Your scholarship review fee has been fully refunded.",
            "تم استرداد كامل رسوم مراجعة المنحة."),
    };
}

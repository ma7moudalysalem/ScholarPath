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
            $"Your application status changed to {p.StatusText ?? "updated"}.",
            $"تغيّرت حالة طلبك إلى {p.StatusText ?? "محدّثة"}."),

        NotificationType.ApplicationWithdrawn => new(
            "Application withdrawn", "تم سحب الطلب",
            "Your application has been withdrawn.",
            "تم سحب طلبك."),

        NotificationType.ApplicationDeadlineApproaching => new(
            "Scholarship deadline approaching", "اقتراب موعد إغلاق المنحة",
            $"\"{p.TitleEn ?? "A scholarship you saved"}\" closes in {p.Count ?? 0} day(s) — submit before the deadline.",
            $"تُغلق \"{p.TitleAr ?? "منحة قمت بحفظها"}\" خلال {p.Count ?? 0} يوم — قدّم طلبك قبل الموعد النهائي."),

        NotificationType.ApplicationDraftReminder => new(
            "Finish your draft application", "أكمل طلبك المحفوظ",
            $"You have an unsubmitted draft application for \"{p.TitleEn ?? "a scholarship"}\" — complete and submit it before the deadline.",
            $"لديك طلب مسودة لم يُرسَل للمنحة \"{p.TitleAr ?? "إحدى المنح"}\" — أكمله وأرسله قبل الموعد النهائي."),

        NotificationType.CompanyReviewPaymentSuccess => new(
            "Review fee paid", "تم دفع رسوم المراجعة",
            "Your scholarship review-fee payment was successful.",
            "تم دفع رسوم مراجعة المنحة بنجاح."),

        NotificationType.CompanyReviewRefunded => RenderReviewRefund(p),

        NotificationType.CompanyRatingReceived => new(
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

        NotificationType.BookingCompleted => new(
            "Session completed", "اكتملت الجلسة",
            $"Your session{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" with {p.CounterpartyName}")} is complete — you can now leave a rating.",
            $"اكتملت جلستك{(string.IsNullOrEmpty(p.CounterpartyName) ? string.Empty : $" مع {p.CounterpartyName}")} — يمكنك الآن إضافة تقييمك."),

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

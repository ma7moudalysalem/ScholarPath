import { createMockBookingRequest, type MockBookingRecord } from "@/lib/mockBookingStore";
import { useMemo, useState } from "react";
import { Link, useLocation } from "react-router";

type ConsultantSummary = {
  id: string;
  name: string;
  expertise: string;
  fee: string;
  defaultDuration: string;
};

type CheckoutFormState = {
  cardholderName: string;
  cardNumber: string;
  expiryDate: string;
  cvc: string;
  billingEmail: string;
  billingCountry: string;
  savePaymentMethod: boolean;
  acceptHoldNotice: boolean;
};

type CheckoutErrors = Partial<Record<keyof CheckoutFormState, string>>;

const consultants: Record<string, ConsultantSummary> = {
  "1": {
    id: "1",
    name: "Dr. Sarah Adel",
    expertise: "Scholarship Strategy · Personal Statements · Interview Preparation",
    fee: "$35",
    defaultDuration: "45 min",
  },
  "2": {
    id: "2",
    name: "Ahmed Mostafa",
    expertise: "Visa Guidance · University Shortlisting · Funding Plans",
    fee: "$25",
    defaultDuration: "30 min",
  },
  "3": {
    id: "3",
    name: "Nour Elhassan",
    expertise: "Full Scholarship Planning · Application Review · Deadline Strategy",
    fee: "$40",
    defaultDuration: "60 min",
  },
};

const defaultFormState: CheckoutFormState = {
  cardholderName: "",
  cardNumber: "",
  expiryDate: "",
  cvc: "",
  billingEmail: "student@example.com",
  billingCountry: "Egypt",
  savePaymentMethod: false,
  acceptHoldNotice: false,
};

function sanitizeDigits(value: string) {
  return value.replace(/\D/g, "");
}

function formatCardNumber(value: string) {
  return sanitizeDigits(value)
    .slice(0, 16)
    .replace(/(.{4})/g, "$1 ")
    .trim();
}

function formatExpiryDate(value: string) {
  const digits = sanitizeDigits(value).slice(0, 4);

  if (digits.length <= 2) {
    return digits;
  }

  return `${digits.slice(0, 2)}/${digits.slice(2)}`;
}

function buildBookingReference(consultantId: string, date: string, time: string) {
  const compactDate = date.replace(/\s+/g, "-").replace(/[^a-zA-Z0-9-]/g, "");
  const compactTime = time.replace(/\s+/g, "").replace(/[^a-zA-Z0-9]/g, "");
  return `BK-${consultantId}-${compactDate}-${compactTime}`;
}

function getRequestedTopic(expertise: string) {
  const firstTopic = expertise.split("·")[0]?.trim();
  return firstTopic || expertise;
}

function parseNumericFee(value: string) {
  const amount = Number(value.replace(/[^0-9.]/g, ""));
  return Number.isFinite(amount) ? amount.toFixed(2) : "0.00";
}

function validateForm(values: CheckoutFormState): CheckoutErrors {
  const errors: CheckoutErrors = {};

  if (!values.cardholderName.trim()) {
    errors.cardholderName = "Cardholder name is required.";
  }

  if (sanitizeDigits(values.cardNumber).length < 16) {
    errors.cardNumber = "Card number must contain 16 digits.";
  }

  const expiryDigits = sanitizeDigits(values.expiryDate);
  if (expiryDigits.length !== 4) {
    errors.expiryDate = "Expiry date must be in MM/YY format.";
  } else {
    const month = Number(expiryDigits.slice(0, 2));
    if (month < 1 || month > 12) {
      errors.expiryDate = "Expiry month must be between 01 and 12.";
    }
  }

  if (sanitizeDigits(values.cvc).length < 3) {
    errors.cvc = "CVC must contain 3 digits.";
  }

  if (!values.billingEmail.trim()) {
    errors.billingEmail = "Billing email is required.";
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.billingEmail.trim())) {
    errors.billingEmail = "Enter a valid billing email.";
  }

  if (!values.billingCountry.trim()) {
    errors.billingCountry = "Billing country is required.";
  }

  if (!values.acceptHoldNotice) {
    errors.acceptHoldNotice = "You must confirm the payment hold notice before continuing.";
  }

  return errors;
}

export function BookingCheckout() {
  const location = useLocation();
  const query = useMemo(() => new URLSearchParams(location.search), [location.search]);

  const consultantId = query.get("consultantId") ?? "1";
  const consultant = consultants[consultantId] ?? consultants["1"];

  const selectedDay = query.get("day") ?? "";
  const selectedDate = query.get("date") ?? "";
  const selectedTime = query.get("time") ?? "";
  const selectedDuration = query.get("duration") ?? consultant.defaultDuration;

  const hasCompleteSlotParams =
    Boolean(query.get("consultantId")) &&
    Boolean(query.get("day")) &&
    Boolean(query.get("date")) &&
    Boolean(query.get("time")) &&
    Boolean(query.get("duration"));

  const [form, setForm] = useState<CheckoutFormState>(defaultFormState);
  const [errors, setErrors] = useState<CheckoutErrors>({});
  const [submittedBooking, setSubmittedBooking] = useState<MockBookingRecord | null>(null);

  const bookingReference = useMemo(() => {
    if (!hasCompleteSlotParams) {
      return "Will be generated after slot selection";
    }

    return buildBookingReference(consultant.id, selectedDate, selectedTime);
  }, [consultant.id, hasCompleteSlotParams, selectedDate, selectedTime]);

  const feeAmount = useMemo(() => parseNumericFee(consultant.fee), [consultant.fee]);

  const holdSummary = useMemo(() => {
    if (!hasCompleteSlotParams) {
      return "No consultation slot selected yet";
    }

    return `${selectedDay} · ${selectedDate} · ${selectedTime} · ${selectedDuration}`;
  }, [hasCompleteSlotParams, selectedDay, selectedDate, selectedTime, selectedDuration]);

  function updateField<K extends keyof CheckoutFormState>(key: K, value: CheckoutFormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
    setErrors((current) => ({ ...current, [key]: undefined }));
  }

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!hasCompleteSlotParams) {
      return;
    }

    const nextErrors = validateForm(form);
    setErrors(nextErrors);

    if (Object.keys(nextErrors).length > 0) {
      return;
    }

    const created = createMockBookingRequest({
      reference: bookingReference,
      consultantId: consultant.id,
      consultantName: consultant.name,
      studentName: form.cardholderName.trim(),
      studentEmail: form.billingEmail.trim(),
      topic: getRequestedTopic(consultant.expertise),
      studentStage: "Booking created from checkout flow",
      sessionType: "1:1 online consultation",
      date: selectedDate,
      time: selectedTime,
      duration: selectedDuration,
      fee: `$${feeAmount}`,
    });

    setSubmittedBooking(created);
  }

  if (!hasCompleteSlotParams) {
    return (
      <main className="min-h-screen bg-[#f5f5f7]">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-3">
            <p className="text-sm font-medium text-[#2563eb]">PB-006</p>

            <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
              Booking checkout
            </h1>

            <p className="max-w-3xl text-base leading-7 text-[#4b5563]">
              Review the selected consultant, session details, and payment summary before confirming
              your booking.
            </p>
          </div>

          <div className="mt-8 rounded-2xl border border-[#e5e7eb] bg-white p-8 shadow-sm">
            <h2 className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
              No consultation slot selected
            </h2>

            <p className="mt-3 text-sm leading-7 text-[#4b5563]">
              Start from the consultant browsing flow, choose a consultant, and select an available
              slot before continuing to checkout.
            </p>

            <div className="mt-6 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-4">
              <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                Current state
              </p>
              <p className="mt-2 text-sm font-medium text-[#1d1d1f]">{holdSummary}</p>
            </div>

            <div className="mt-6 flex flex-col gap-3 sm:flex-row">
              <Link
                to="/dev/consultants"
                className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
              >
                Browse consultants
              </Link>

              <Link
                to="/dev/bookings"
                className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
              >
                Open my bookings
              </Link>
            </div>
          </div>
        </section>
      </main>
    );
  }

  if (submittedBooking) {
    return (
      <main className="min-h-screen bg-[#f5f5f7]">
        <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-3">
            <p className="text-sm font-medium text-[#2563eb]">PB-006</p>

            <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
              Booking checkout
            </h1>

            <p className="max-w-3xl text-base leading-7 text-[#4b5563]">
              Review the selected consultant, session details, and payment summary before confirming
              your booking.
            </p>
          </div>

          <div className="mt-8 rounded-xl border border-[#bbf7d0] bg-[#f0fdf4] p-6">
            <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
              <div>
                <p className="text-xs font-medium tracking-[0.02em] text-[#15803d] uppercase">
                  PB-006
                </p>
                <h2 className="mt-2 text-2xl font-semibold tracking-[-0.01em] text-[#166534]">
                  Payment hold authorized
                </h2>
                <p className="mt-3 max-w-3xl text-sm leading-7 text-[#166534]">
                  The checkout form is valid and the booking request was added to the shared mock
                  store successfully.
                </p>
              </div>

              <span className="inline-flex rounded-full bg-[#dcfce7] px-3 py-1 text-xs font-medium text-[#15803d]">
                Request created
              </span>
            </div>
          </div>

          <div className="mt-6 grid gap-6 lg:grid-cols-12">
            <section className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-8">
              <h3 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                Booking request summary
              </h3>

              <div className="mt-5 grid gap-4 rounded-xl bg-[#f9fafb] p-5 sm:grid-cols-2 lg:grid-cols-4">
                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Booking reference
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {submittedBooking.reference}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Request status
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    Awaiting consultant acceptance
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Consultant
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {submittedBooking.consultantName}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Session type
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {submittedBooking.sessionType}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Selected date
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {selectedDay} · {submittedBooking.date}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Selected time
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{submittedBooking.time}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Duration
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {submittedBooking.duration}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Topic
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {submittedBooking.topic}
                  </p>
                </div>
              </div>
            </section>

            <aside className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-4">
              <h3 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                Payment summary
              </h3>

              <div className="mt-5 space-y-4 text-sm">
                <div className="flex items-center justify-between">
                  <span className="text-[#4b5563]">Session fee</span>
                  <span className="font-medium text-[#1d1d1f]">${feeAmount}</span>
                </div>

                <div className="flex items-center justify-between">
                  <span className="text-[#4b5563]">Service fee</span>
                  <span className="font-medium text-[#1d1d1f]">$0.00</span>
                </div>

                <div className="flex items-center justify-between">
                  <span className="text-[#4b5563]">Tax</span>
                  <span className="font-medium text-[#1d1d1f]">$0.00</span>
                </div>

                <div className="border-t border-[#e5e7eb] pt-4">
                  <div className="flex items-center justify-between">
                    <span className="font-medium text-[#1d1d1f]">Held amount</span>
                    <span className="text-2xl font-semibold text-[#1d1d1f]">${feeAmount}</span>
                  </div>
                </div>
              </div>

              <div className="mt-6 rounded-xl bg-[#f9fafb] p-4">
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  Billing contact
                </p>
                <p className="mt-2 text-sm font-medium text-[#1d1d1f]">{form.cardholderName}</p>
                <p className="mt-1 text-sm text-[#4b5563]">{form.billingEmail}</p>
                <p className="mt-1 text-sm text-[#4b5563]">{form.billingCountry}</p>
              </div>

              <div className="mt-6 flex flex-col gap-3">
                <Link
                  to="/dev/bookings"
                  className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                >
                  Open my bookings
                </Link>

                <Link
                  to={`/dev/bookings/${submittedBooking.id}`}
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  Open this booking
                </Link>
              </div>
            </aside>
          </div>
        </section>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <p className="text-sm font-medium text-[#2563eb]">PB-006</p>

          <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">Booking checkout</h1>

          <p className="max-w-3xl text-base leading-7 text-[#4b5563]">
            Review the selected consultant, session details, and payment summary before confirming
            your booking.
          </p>
        </div>

        <div className="mt-8 grid gap-6 lg:grid-cols-12">
          <form
            id="checkout-form"
            className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-8"
            onSubmit={handleSubmit}
          >
            <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
              Session summary
            </h2>

            <p className="mt-2 text-sm leading-6 text-[#4b5563]">
              Confirm the consultant and selected slot before moving to payment.
            </p>

            <div className="mt-5 grid gap-4 rounded-xl bg-[#f9fafb] p-5 sm:grid-cols-2">
              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  Consultant
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{consultant.name}</p>
                <p className="mt-2 text-sm leading-6 text-[#4b5563]">{consultant.expertise}</p>
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  Session type
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">1:1 online consultation</p>
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  Selected date
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                  {selectedDay} · {selectedDate}
                </p>
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  Selected time
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{selectedTime}</p>
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  Duration
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{selectedDuration}</p>
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  Consultant fee
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                  {consultant.fee} / {selectedDuration}
                </p>
              </div>
            </div>

            <div className="mt-6">
              <h3 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                Payment method
              </h3>

              <div className="mt-4 rounded-xl border border-[#2563eb] bg-[#eff6ff] p-4">
                <div className="flex items-start gap-3">
                  <input checked readOnly type="radio" className="mt-1 h-4 w-4 accent-[#2563eb]" />
                  <div>
                    <p className="text-sm font-medium text-[#1d1d1f]">Card payment</p>
                    <p className="mt-1 text-sm leading-6 text-[#4b5563]">
                      This page is prepared for Stripe handoff. These inputs simulate the payment
                      capture step until real Stripe Elements are connected.
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <div className="mt-6 grid gap-5 sm:grid-cols-2">
              <div className="sm:col-span-2">
                <label className="block text-sm font-medium text-[#1d1d1f]">Cardholder name</label>
                <input
                  type="text"
                  value={form.cardholderName}
                  onChange={(event) => updateField("cardholderName", event.target.value)}
                  placeholder="Full name on card"
                  className={`mt-2 h-12 w-full rounded-lg border bg-white px-4 text-sm text-[#1d1d1f] transition outline-none placeholder:text-[#9ca3af] ${
                    errors.cardholderName
                      ? "border-[#ef4444] focus:border-[#ef4444]"
                      : "border-[#d1d5db] focus:border-[#2563eb]"
                  }`}
                />
                {errors.cardholderName && (
                  <p className="mt-2 text-sm text-[#dc2626]">{errors.cardholderName}</p>
                )}
              </div>

              <div className="sm:col-span-2">
                <label className="block text-sm font-medium text-[#1d1d1f]">Card number</label>
                <input
                  type="text"
                  inputMode="numeric"
                  value={form.cardNumber}
                  onChange={(event) =>
                    updateField("cardNumber", formatCardNumber(event.target.value))
                  }
                  placeholder="1234 1234 1234 1234"
                  className={`mt-2 h-12 w-full rounded-lg border bg-white px-4 text-sm text-[#1d1d1f] transition outline-none placeholder:text-[#9ca3af] ${
                    errors.cardNumber
                      ? "border-[#ef4444] focus:border-[#ef4444]"
                      : "border-[#d1d5db] focus:border-[#2563eb]"
                  }`}
                />
                {errors.cardNumber && (
                  <p className="mt-2 text-sm text-[#dc2626]">{errors.cardNumber}</p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-[#1d1d1f]">Expiry date</label>
                <input
                  type="text"
                  inputMode="numeric"
                  value={form.expiryDate}
                  onChange={(event) =>
                    updateField("expiryDate", formatExpiryDate(event.target.value))
                  }
                  placeholder="MM/YY"
                  className={`mt-2 h-12 w-full rounded-lg border bg-white px-4 text-sm text-[#1d1d1f] transition outline-none placeholder:text-[#9ca3af] ${
                    errors.expiryDate
                      ? "border-[#ef4444] focus:border-[#ef4444]"
                      : "border-[#d1d5db] focus:border-[#2563eb]"
                  }`}
                />
                {errors.expiryDate && (
                  <p className="mt-2 text-sm text-[#dc2626]">{errors.expiryDate}</p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-[#1d1d1f]">CVC</label>
                <input
                  type="text"
                  inputMode="numeric"
                  value={form.cvc}
                  onChange={(event) =>
                    updateField("cvc", sanitizeDigits(event.target.value).slice(0, 3))
                  }
                  placeholder="123"
                  className={`mt-2 h-12 w-full rounded-lg border bg-white px-4 text-sm text-[#1d1d1f] transition outline-none placeholder:text-[#9ca3af] ${
                    errors.cvc
                      ? "border-[#ef4444] focus:border-[#ef4444]"
                      : "border-[#d1d5db] focus:border-[#2563eb]"
                  }`}
                />
                {errors.cvc && <p className="mt-2 text-sm text-[#dc2626]">{errors.cvc}</p>}
              </div>

              <div>
                <label className="block text-sm font-medium text-[#1d1d1f]">Billing email</label>
                <input
                  type="email"
                  value={form.billingEmail}
                  onChange={(event) => updateField("billingEmail", event.target.value)}
                  placeholder="student@example.com"
                  className={`mt-2 h-12 w-full rounded-lg border bg-white px-4 text-sm text-[#1d1d1f] transition outline-none placeholder:text-[#9ca3af] ${
                    errors.billingEmail
                      ? "border-[#ef4444] focus:border-[#ef4444]"
                      : "border-[#d1d5db] focus:border-[#2563eb]"
                  }`}
                />
                {errors.billingEmail && (
                  <p className="mt-2 text-sm text-[#dc2626]">{errors.billingEmail}</p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-[#1d1d1f]">Billing country</label>
                <select
                  value={form.billingCountry}
                  onChange={(event) => updateField("billingCountry", event.target.value)}
                  className={`mt-2 h-12 w-full rounded-lg border bg-white px-4 text-sm text-[#1d1d1f] transition outline-none ${
                    errors.billingCountry
                      ? "border-[#ef4444] focus:border-[#ef4444]"
                      : "border-[#d1d5db] focus:border-[#2563eb]"
                  }`}
                >
                  <option>Egypt</option>
                  <option>Saudi Arabia</option>
                  <option>United Arab Emirates</option>
                  <option>Jordan</option>
                  <option>Qatar</option>
                </select>
                {errors.billingCountry && (
                  <p className="mt-2 text-sm text-[#dc2626]">{errors.billingCountry}</p>
                )}
              </div>
            </div>

            <label className="mt-6 flex items-start gap-3 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-4">
              <input
                type="checkbox"
                checked={form.savePaymentMethod}
                onChange={(event) => updateField("savePaymentMethod", event.target.checked)}
                className="mt-1 h-4 w-4 accent-[#2563eb]"
              />
              <div>
                <p className="text-sm font-medium text-[#1d1d1f]">
                  Save payment method for future bookings
                </p>
                <p className="mt-1 text-sm leading-6 text-[#4b5563]">
                  Placeholder behavior for a future stored-card flow.
                </p>
              </div>
            </label>

            <div className="mt-6 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-4">
              <p className="text-sm font-medium text-[#1d1d1f]">Authorization hold notice</p>
              <p className="mt-2 text-sm leading-7 text-[#4b5563]">
                When you continue, the system will place a payment hold for this booking request.
                The amount is captured only after the consultant accepts the session.
              </p>
            </div>

            <div className="mt-6">
              <label className="flex items-start gap-3">
                <input
                  type="checkbox"
                  checked={form.acceptHoldNotice}
                  onChange={(event) => updateField("acceptHoldNotice", event.target.checked)}
                  className="mt-1 h-4 w-4 accent-[#2563eb]"
                />
                <span className="text-sm text-[#1d1d1f]">
                  I understand and accept the payment hold behavior for this booking.
                </span>
              </label>

              {errors.acceptHoldNotice && (
                <p className="mt-2 text-sm text-[#dc2626]">{errors.acceptHoldNotice}</p>
              )}
            </div>
          </form>

          <aside className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-4">
            <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
              Price summary
            </h2>

            <div className="mt-5 space-y-4 text-sm">
              <div className="flex items-center justify-between">
                <span className="text-[#4b5563]">Session fee</span>
                <span className="font-medium text-[#1d1d1f]">${feeAmount}</span>
              </div>

              <div className="flex items-center justify-between">
                <span className="text-[#4b5563]">Service fee</span>
                <span className="font-medium text-[#1d1d1f]">$0.00</span>
              </div>

              <div className="flex items-center justify-between">
                <span className="text-[#4b5563]">Tax</span>
                <span className="font-medium text-[#1d1d1f]">$0.00</span>
              </div>

              <div className="border-t border-[#e5e7eb] pt-4">
                <div className="flex items-center justify-between">
                  <span className="font-medium text-[#1d1d1f]">Total</span>
                  <span className="text-2xl font-semibold text-[#1d1d1f]">${feeAmount}</span>
                </div>
              </div>
            </div>

            <div className="mt-6 rounded-xl bg-[#f9fafb] p-4">
              <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                Selected slot
              </p>
              <p className="mt-2 text-sm font-medium text-[#1d1d1f]">{holdSummary}</p>
            </div>

            <div className="mt-4 rounded-xl bg-[#f9fafb] p-4">
              <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                Booking reference
              </p>
              <p className="mt-2 text-sm font-medium text-[#1d1d1f]">{bookingReference}</p>
            </div>

            <div className="mt-4 rounded-xl bg-[#f9fafb] p-4">
              <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                Confirmation
              </p>
              <p className="mt-2 text-sm leading-7 text-[#4b5563]">
                By continuing, you confirm the selected consultant, selected slot, payment hold
                behavior, and booking terms before moving to payment.
              </p>
            </div>

            <div className="mt-6 flex flex-col gap-3">
              <button
                type="submit"
                form="checkout-form"
                className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
              >
                Authorize payment hold
              </button>

              <Link
                to={`/dev/consultants/${consultant.id}`}
                className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
              >
                Back to profile
              </Link>
            </div>
          </aside>
        </div>
      </section>
    </main>
  );
}

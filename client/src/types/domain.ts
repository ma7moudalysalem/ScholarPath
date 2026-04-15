/**
 * Shared domain types mirroring the backend Domain layer enums.
 * When the backend adds new enum values, update here too.
 * (Teammates: consider generating these from the OpenAPI spec in CI.)
 */

export type AccountStatus =
  | "Unassigned"
  | "PendingApproval"
  | "Active"
  | "Suspended"
  | "Deactivated";

export type Role = "Admin" | "Student" | "Company" | "Consultant" | "Unassigned";

export type FundingType =
  | "FullyFunded"
  | "PartiallyFunded"
  | "TuitionOnly"
  | "StipendOnly"
  | "Other";

export type AcademicLevel = "HighSchool" | "Undergrad" | "Masters" | "PhD" | "PostDoc" | "Other";

export type ScholarshipStatus = "Draft" | "Open" | "Closed" | "Archived" | "UnderReview";

export type ListingMode = "InApp" | "ExternalUrl";

export type ApplicationStatus =
  | "Draft"
  | "Pending"
  | "UnderReview"
  | "Shortlisted"
  | "Accepted"
  | "Rejected"
  | "Withdrawn"
  | "Intending"
  | "Applied"
  | "WaitingResult";

export type BookingStatus =
  | "Requested"
  | "Confirmed"
  | "Rejected"
  | "Expired"
  | "Cancelled"
  | "Completed"
  | "NoShowStudent"
  | "NoShowConsultant";

export type PaymentType = "ConsultantBooking" | "CompanyReview";

export type PaymentStatus =
  | "Pending"
  | "Held"
  | "Captured"
  | "Refunded"
  | "PartiallyRefunded"
  | "Failed"
  | "Cancelled";

export type NotificationChannel = "InApp" | "Email" | "Push";

// ─── Enums (mirroring backend) ───────────────────────────────────────────────

export enum UserRole {
  Student = 0,
  Consultant = 1,
  Company = 2,
  Admin = 3,
}

export enum AccountStatus {
  Active = 0,
  Pending = 1,
  Suspended = 2,
  Rejected = 3,
}

export enum UpgradeRequestStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  NeedsMoreInfo = 3,
}

export enum NotificationType {
  General = 0,
  ScholarshipDeadline = 1,
  ApplicationUpdate = 2,
  CommunityActivity = 3,
  AccountUpgrade = 4,
}

export enum ScholarshipFundingType {
  FullyFunded = 0,
  PartiallyFunded = 1,
  SelfFunded = 2,
}

export enum DegreeLevel {
  Bachelor = 0,
  Master = 1,
  PhD = 2,
  Diploma = 3,
  Certificate = 4,
}

// ─── DTOs ────────────────────────────────────────────────────────────────────

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  profileImageUrl: string | null;
  role: UserRole;
  accountStatus: AccountStatus;
  isOnboardingComplete: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface AuthResponse {
  user: UserDto;
  accessToken: string;
  refreshToken: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface OnboardingRequest {
  role: UserRole;
  bio?: string;
  organization?: string;
  fieldOfStudy?: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ScholarshipDto {
  id: string;
  title: string;
  titleAr: string | null;
  description: string;
  descriptionAr: string | null;
  country: string | null;
  fieldOfStudy: string | null;
  fundingType: ScholarshipFundingType;
  degreeLevel: DegreeLevel;
  awardAmount: number | null;
  currency: string | null;
  deadline: string | null;
  eligibilityDescription: string | null;
  requiredDocuments: string | null;
  officialLink: string | null;
  imageUrl: string | null;
  isActive: boolean;
  minGPA: number | null;
  maxAge: number | null;
  eligibleCountries: string[] | null;
  eligibleMajors: string[] | null;
  categoryId: string | null;
  categoryName: string | null;
  isSaved: boolean;
  createdAt: string;
}

export interface ScholarshipFilters {
  search?: string;
  country?: string;
  fieldOfStudy?: string;
  fundingType?: ScholarshipFundingType;
  degreeLevel?: DegreeLevel;
  page?: number;
  pageSize?: number;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface NotificationDto {
  id: string;
  type: NotificationType;
  title: string;
  titleAr: string | null;
  message: string;
  messageAr: string | null;
  isRead: boolean;
  readAt: string | null;
  relatedEntityId: string | null;
  relatedEntityType: string | null;
  createdAt: string;
}

export interface UserProfileDto {
  id: string;
  userId: string;
  bio: string | null;
  organization: string | null;
  fieldOfStudy: string | null;
  country: string | null;
  dateOfBirth: string | null;
  phoneNumber: string | null;
  linkedInUrl: string | null;
  websiteUrl: string | null;
}

export interface UpdateProfileRequest {
  bio?: string;
  organization?: string;
  fieldOfStudy?: string;
  country?: string;
  dateOfBirth?: string;
  phoneNumber?: string;
  linkedInUrl?: string;
  websiteUrl?: string;
}

export interface UpgradeRequestDto {
  id: string;
  userId: string;
  userEmail: string;
  userName: string;
  requestedRole: UserRole;
  status: UpgradeRequestStatus;
  reason: string;
  adminNotes: string | null;
  createdAt: string;
  reviewedAt: string | null;
}

export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
  statusCode?: number;
}

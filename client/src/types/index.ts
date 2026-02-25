// ─── Enums (mirroring backend) ───────────────────────────────────────────────

export enum UserRole {
  Unassigned = 0,
  Student = 1,
  Consultant = 2,
  Company = 3,
  Admin = 4,
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
  System = 0,
  UpgradeStatus = 1,
  ScholarshipAlert = 2,
  CommunityMention = 3,
  Message = 4,
  SessionReminder = 5,
}

export enum ScholarshipFundingType {
  FullyFunded = 0,
  PartiallyFunded = 1,
  SelfFunded = 2,
  Other = 3,
}

export enum DegreeLevel {
  Bachelors = 0,
  Masters = 1,
  PhD = 2,
  Diploma = 3,
  Other = 4,
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
}

export interface AuthResponse {
  user: UserDto;
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface LoginRequest {
  identifier: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface OnboardingRequest {
  selectedRole: UserRole;
  companyName?: string;
  expertiseArea?: string;
  bio?: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
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
  fieldOfStudy: string | null;
  country: string | null;
  dateOfBirth: string | null;
  phoneNumber: string | null;
  gpa?: number;
  interests?: string;
  targetCountry?: string;
}

export interface UpdateProfileRequest {
  bio?: string;
  fieldOfStudy?: string;
  country?: string;
  dateOfBirth?: string;
  phoneNumber?: string;
  gpa?: number;
  interests?: string;
  targetCountry?: string;
}

export interface UpgradeRequestDto {
  id: string;
  userId: string;
  userEmail: string;
  userName: string;
  requestedRole: UserRole;
  status: UpgradeRequestStatus;
  adminNotes: string | null;
  createdAt: string;
  reviewedAt: string | null;
}

export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
  statusCode?: number;
}

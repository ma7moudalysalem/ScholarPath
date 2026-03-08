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
  hasPassword?: boolean;
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
  currentPassword?: string;
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

export enum ScholarshipSortBy {
  Relevance = 0,
  DeadlineSoonest = 1,
  Newest = 2,
  HighestFunding = 3,
}

export interface ScholarshipSearchFilters {
  search?: string;
  country?: string;
  degreeLevel?: DegreeLevel;
  fieldOfStudy?: string;
  fundingType?: ScholarshipFundingType;
  deadlineFrom?: string;
  deadlineTo?: string;
  page?: number;
  pageSize?: number;
  sortBy?: ScholarshipSortBy;
  includeExpired?: boolean;
}

export interface ScholarshipListItemDto {
  id: string;
  title: string;
  titleAr: string | null;
  providerName: string | null;
  providerNameAr: string | null;
  country: string | null;
  degreeLevel: DegreeLevel;
  fundingType: ScholarshipFundingType;
  awardAmount: number | null;
  currency: string | null;
  deadline: string | null;
  deadlineCountdownDays: number | null;
  isExpiringSoon: boolean;
  isSaved: boolean;
  imageUrl: string | null;
  createdAt: string;
}

export interface RecommendedScholarshipDto extends ScholarshipListItemDto {
  score: number;
  matchReasons: string[];
}

export interface RecommendedResponse {
  items: RecommendedScholarshipDto[];
  profileIncomplete: boolean;
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

// ─── External Auth ──────────────────────────────────────────────────────────

export interface ExternalLoginRequest {
  provider: string;
  idToken: string;
  providerKey?: string;
}

export interface LinkProviderRequest {
  provider: string;
  providerKey: string;
}

// ─── Upgrade Request (Consultant) ───────────────────────────────────────────

export interface EducationEntryDto {
  id?: string;
  institutionName: string;
  degreeName: string;
  fieldOfStudy: string;
  startYear: number;
  endYear?: number;
  isCurrentlyStudying: boolean;
}

export interface UpgradeRequestLinkDto {
  url: string;
  label: 'LinkedIn' | 'Portfolio' | 'Website' | 'Other';
}

export interface UpgradeRequestFileDto {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
}

export interface ConsultantUpgradeRequest {
  education: EducationEntryDto[];
  experienceSummary: string;
  expertiseTags: string[];
  languages: string[];
  links: UpgradeRequestLinkDto[];
}

// ─── Upgrade Request (Company) ──────────────────────────────────────────────

export interface CompanyUpgradeRequest {
  companyName: string;
  country: string;
  website?: string;
  contactPersonName: string;
  contactEmail: string;
  contactPhone?: string;
  companyRegistrationNumber: string;
}

// ─── Upgrade Request Detail (Admin) ─────────────────────────────────────────

export interface UpgradeRequestDetailDto {
  id: string;
  userId: string;
  userEmail: string;
  userName: string;
  requestedRole: UserRole;
  status: UpgradeRequestStatus;
  adminNotes: string | null;
  rejectionReasons: string[] | null;
  createdAt: string;
  reviewedAt: string | null;
  reviewedBy: string | null;
  // Consultant fields
  experienceSummary: string | null;
  expertiseTags: string[];
  languages: string[];
  education: EducationEntryDto[];
  links: UpgradeRequestLinkDto[];
  files: UpgradeRequestFileDto[];
  // Company fields
  companyName: string | null;
  country: string | null;
  website: string | null;
  contactPersonName: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
  companyRegistrationNumber: string | null;
}

export interface UpgradeReviewRequest {
  reviewNotes?: string;
}

export interface UpgradeRejectRequest {
  reviewNotes: string;
  rejectionReasons: string[];
}

// ─── Application Tracking ───────────────────────────────────────────────────

export enum ApplicationStatus {
  Planned = 0,
  Applied = 1,
  Pending = 2,
  Accepted = 3,
  Rejected = 4,
}

export interface ScholarshipDetailDto {
  id: string;
  title: string;
  titleAr: string | null;
  description: string;
  descriptionAr: string | null;
  providerName: string | null;
  providerNameAr: string | null;
  country: string | null;
  fieldOfStudy: string | null;
  degreeLevel: DegreeLevel;
  fundingType: ScholarshipFundingType;
  awardAmount: number | null;
  currency: string | null;
  deadline: string | null;
  deadlineCountdownDays: number | null;
  eligibilityDescription: string | null;
  requiredDocuments: string | null;
  overviewHtml: string | null;
  howToApplyHtml: string | null;
  documentsChecklist: string | null;
  officialLink: string | null;
  imageUrl: string | null;
  minGPA: number | null;
  maxAge: number | null;
  eligibleCountries: string | null;
  eligibleMajors: string | null;
  tags: string | null;
  viewCount: number;
  categoryId: string | null;
  categoryName: string | null;
  isSaved: boolean;
  isTracked: boolean;
  createdAt: string;
}

export interface TrackApplicationRequest {
  scholarshipId: string;
  status?: ApplicationStatus;
  notes?: string;
}

export interface TrackApplicationResponse {
  id: string;
  status: ApplicationStatus;
  alreadyExisted: boolean;
}

// ─── Dashboard ─────────────────────────────────────────────────────────────

export interface DashboardSummaryDto {
  statusCounts: Record<string, number>;
  deadlinesSoon: UpcomingDeadlineDto[];
  recommendedActions: string[];
}

export interface UpcomingDeadlineDto {
  scholarshipId: string;
  title: string;
  titleAr: string | null;
  providerName: string | null;
  deadline: string;
  countdownDays: number;
  status: ApplicationStatus;
}

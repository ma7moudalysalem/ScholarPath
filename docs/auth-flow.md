# ScholarPath Authentication and Authorization Flows

## Overview

ScholarPath uses JWT-based stateless authentication with refresh token rotation. ASP.NET Identity handles user management and password hashing. Authorization is role-based with four roles: Student, Consultant, Company, and Admin.

---

## 1. Registration and Onboarding Flow

New users register with first name, last name, email, and password. They then complete an onboarding step where they select their role. Students are activated immediately. Consultants and Companies require admin approval.

```mermaid
sequenceDiagram
    actor User
    participant Frontend
    participant API
    participant Identity as ASP.NET Identity
    participant DB as Database
    participant Email as Email Service

    User->>Frontend: Fill registration form<br/>(firstName, lastName, email, password, confirmPassword)
    Frontend->>API: POST /api/v1/auth/register<br/>{ firstName, lastName, email,<br/>password, confirmPassword }
    API->>Identity: CreateAsync(user, password)
    Identity->>DB: Insert ApplicationUser<br/>(Role=Student, Status=Active,<br/>IsOnboardingComplete=false)
    DB-->>Identity: User created
    Identity-->>API: Success
    API->>DB: Create RefreshToken
    API-->>Frontend: 200 OK<br/>{ accessToken, refreshToken,<br/>expiresAt, user: UserDto }
    Frontend->>Frontend: Store tokens

    Note over User,Frontend: Onboarding Step

    Frontend->>Frontend: Redirect to /onboarding
    User->>Frontend: Select desired role + optional details<br/>(selectedRole, companyName?, expertiseArea?, bio?)
    Frontend->>API: POST /api/v1/auth/complete-onboarding<br/>{ selectedRole, companyName?,<br/>expertiseArea?, bio? }

    alt Role = Student
        API->>DB: Update user<br/>(IsOnboardingComplete=true)
        API-->>Frontend: 200 OK - Account active
        Frontend->>Frontend: Redirect to /dashboard
    else Role = Consultant or Company
        API->>DB: Update user<br/>(IsOnboardingComplete=true)
        API->>DB: Create UpgradeRequest<br/>(Status=Pending, RequestedRole)
        API->>DB: Set AccountStatus=Pending
        API->>DB: Create Notification for Admins
        API->>Email: Send admin notification email
        API-->>Frontend: 200 OK - Pending approval
        Frontend->>Frontend: Show "pending approval" screen
    end
```

---

## 2. Login Flow

Users authenticate with an identifier (email or username) and password. The API validates credentials, generates a JWT access token and a refresh token, and returns both along with user details.

```mermaid
sequenceDiagram
    actor User
    participant Frontend
    participant API
    participant Identity as ASP.NET Identity
    participant JWT as Token Service
    participant DB as Database

    User->>Frontend: Enter identifier + password
    Frontend->>API: POST /api/v1/auth/login<br/>{ identifier, password, rememberMe? }
    API->>Identity: FindByEmailAsync(identifier)<br/>or FindByNameAsync(identifier)
    Identity->>DB: Query ApplicationUser
    DB-->>Identity: User record

    alt User not found or password invalid
        API->>Identity: CheckPasswordAsync(user, password)
        Identity-->>API: Failed
        API-->>Frontend: 401 Unauthorized<br/>"Invalid credentials"
    else Account is Suspended or Rejected
        API-->>Frontend: 403 Forbidden<br/>"Account is not active"
    else Valid credentials
        API->>Identity: CheckPasswordAsync(user, password)
        Identity-->>API: Success
        API->>JWT: GenerateAccessToken(user)
        JWT-->>API: JWT access token (short-lived)
        API->>DB: Create RefreshToken (long-lived)
        DB-->>API: Token stored
        API-->>Frontend: 200 OK<br/>{ accessToken, refreshToken,<br/>expiresAt, user: UserDto }
        Note over API,Frontend: UserDto: { id, firstName, lastName,<br/>email, role, accountStatus,<br/>profileImageUrl, isOnboardingComplete }
        Frontend->>Frontend: Store tokens in memory / secure storage
        Frontend->>Frontend: Redirect to /dashboard
    end
```

---

## 3. Token Refresh Flow

When the access token expires, the frontend sends the refresh token to obtain a new access token. The old refresh token is revoked and a new one is issued (rotation).

```mermaid
sequenceDiagram
    actor Frontend
    participant API
    participant JWT as Token Service
    participant DB as Database

    Frontend->>API: Any authenticated request
    API-->>Frontend: 401 Unauthorized (token expired)

    Frontend->>API: POST /api/v1/auth/refresh-token<br/>{ refreshToken }
    API->>DB: Find RefreshToken by value

    alt Token not found or revoked
        API-->>Frontend: 401 Unauthorized<br/>"Invalid refresh token"
        Frontend->>Frontend: Redirect to /login
    else Token expired
        API->>DB: Mark token as revoked
        API-->>Frontend: 401 Unauthorized<br/>"Refresh token expired"
        Frontend->>Frontend: Redirect to /login
    else Token valid
        API->>DB: Revoke old RefreshToken
        API->>JWT: GenerateAccessToken(user)
        JWT-->>API: New JWT access token
        API->>DB: Create new RefreshToken
        DB-->>API: New token stored
        API-->>Frontend: 200 OK<br/>{ accessToken, refreshToken,<br/>expiresAt, user: UserDto }
        Frontend->>Frontend: Update stored tokens
        Frontend->>API: Retry original request with new access token
        API-->>Frontend: Original response
    end
```

---

## 4. Password Reset Flow

Users request a password reset via email. The API always returns a generic success message to prevent email enumeration. If the user exists, a reset token is sent via email.

```mermaid
sequenceDiagram
    actor User
    participant Frontend
    participant API
    participant Identity as ASP.NET Identity
    participant Email as Email Service
    participant DB as Database

    Note over User,DB: Step 1 - Request Reset

    User->>Frontend: Click "Forgot Password"
    Frontend->>Frontend: Show email input form
    User->>Frontend: Enter email address
    Frontend->>API: POST /api/v1/auth/forgot-password<br/>{ email }

    alt User exists
        API->>Identity: GeneratePasswordResetTokenAsync(user)
        Identity-->>API: Reset token
        API->>Email: Send reset email with link<br/>(includes token)
        Email-->>User: Email with reset link
    else User does not exist
        Note over API: No action taken
    end

    API-->>Frontend: 200 OK<br/>"If an account exists, a reset email has been sent"

    Note over User,DB: Step 2 - Reset Password

    User->>Frontend: Click link in email
    Frontend->>Frontend: Open /reset-password?token=xxx
    User->>Frontend: Enter new password + confirm
    Frontend->>API: POST /api/v1/auth/reset-password<br/>{ token, newPassword, confirmNewPassword }
    API->>Identity: ResetPasswordAsync(user, token, newPassword)

    alt Token invalid or expired
        Identity-->>API: Failed
        API-->>Frontend: 400 Bad Request<br/>"Invalid or expired token"
    else Success
        Identity->>DB: Update password hash
        Identity-->>API: Success
        API->>DB: Revoke all RefreshTokens for user
        API-->>Frontend: 200 OK<br/>"Password has been reset"
        Frontend->>Frontend: Redirect to /login
    end
```

---

## 5. Role Upgrade Flow

Authenticated students can request an upgrade to Consultant or Company. Admins review the request and can approve, reject, or request more information.

```mermaid
sequenceDiagram
    actor Student
    participant Frontend
    participant API
    participant DB as Database
    participant Email as Email Service
    actor Admin

    Note over Student,Admin: Step 1 - Submit Request

    Student->>Frontend: Navigate to profile settings
    Student->>Frontend: Click "Upgrade to Consultant"<br/>Fill justification form
    Frontend->>API: POST /api/v1/profile/upgrade-request/consultant<br/>{ justification, documents }
    API->>DB: Create UpgradeRequest<br/>(Status=Pending)
    API->>DB: Create Notification for all Admins<br/>(Type=System)
    API->>Email: Notify admins of new request
    API-->>Frontend: 200 OK "Request submitted"

    Note over Student,Admin: Step 2 - Admin Review

    Admin->>Frontend: View /admin/upgrade-requests
    Frontend->>API: GET /api/v1/admin/upgrade-requests
    API->>DB: Query pending UpgradeRequests
    API-->>Frontend: List of requests

    Admin->>Frontend: Review request details

    alt Approve
        Admin->>Frontend: Click Approve
        Frontend->>API: PUT /api/v1/admin/upgrade-requests/{id}/approve
        API->>DB: Update UpgradeRequest (Status=Approved)
        API->>DB: Update User Role to Consultant
        API->>DB: Update AccountStatus to Active
        API->>DB: Create Notification for Student<br/>(Type=UpgradeStatus)
        API->>Email: Send approval email to student
        API-->>Frontend: 200 OK
    else Reject
        Admin->>Frontend: Click Reject + add reason
        Frontend->>API: PUT /api/v1/admin/upgrade-requests/{id}/reject<br/>{ reviewNotes }
        API->>DB: Update UpgradeRequest (Status=Rejected)
        API->>DB: Create Notification for Student<br/>(Type=UpgradeStatus)
        API->>Email: Send rejection email to student
        API-->>Frontend: 200 OK
    else Request More Info
        Admin->>Frontend: Click "Request More Info" + add notes
        Frontend->>API: PUT /api/v1/admin/upgrade-requests/{id}/request-info<br/>{ reviewNotes }
        API->>DB: Update UpgradeRequest<br/>(Status=NeedsMoreInfo)
        API->>DB: Create Notification for Student<br/>(Type=UpgradeStatus)
        API->>Email: Send info-request email to student
        API-->>Frontend: 200 OK
    end

    Note over Student,Admin: Step 3 - Student Receives Outcome

    Student->>Frontend: Views notification
    alt Approved
        Frontend->>Frontend: Dashboard now shows<br/>Consultant features unlocked
    else Rejected
        Frontend->>Frontend: Show rejection reason<br/>Option to re-apply
    else More Info Requested
        Frontend->>Frontend: Show admin notes<br/>Option to update and resubmit
    end
```

---

## Token Configuration

| Parameter | Value | Notes |
|---|---|---|
| Access Token Lifetime | 60 minutes (default) | Short-lived for security |
| Refresh Token Lifetime | 7 days | Rotated on each use |
| Password Reset Token | 24 hours | Single use |
| JWT Signing Algorithm | HS256 | HMAC with symmetric key |
| Token Storage (Frontend) | Memory + httpOnly cookie | Prevents XSS access |

---

## Security Measures

| Measure | Implementation |
|---|---|
| Password Hashing | ASP.NET Identity (PBKDF2 with HMAC-SHA256) |
| Refresh Token Rotation | Old token revoked on each refresh |
| Email Enumeration Prevention | Generic responses on forgot-password and registration |
| Brute Force Protection | Account lockout after N failed attempts (ASP.NET Identity) |
| Token Revocation on Password Reset | All refresh tokens invalidated when password changes |
| Role-Based Authorization | `[Authorize(Roles = "Admin")]` attributes on protected endpoints |
| HTTPS Only | Enforced in production |

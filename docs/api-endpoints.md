# ScholarPath API Endpoints Reference

> **Note:** This document mixes implemented and planned endpoints. `AuthController` and `AdminController` are implemented; other sections may still be roadmap/specification and should be validated against Swagger.

## Overview

All endpoints are prefixed with `/api/v1`. The API uses JSON request/response bodies. Authentication is via Bearer JWT token in the `Authorization` header.

### Common Response Codes

| Code | Meaning |
|---|---|
| 200 | Success |
| 201 | Created |
| 204 | No Content (successful delete) |
| 400 | Bad Request (validation error) |
| 401 | Unauthorized (missing or invalid token) |
| 403 | Forbidden (insufficient role) |
| 404 | Not Found |
| 409 | Conflict (duplicate resource) |
| 500 | Internal Server Error |

### Pagination

List endpoints support pagination via query parameters:

| Parameter | Type | Default | Description |
|---|---|---|---|
| `page` | int | 1 | Page number (1-indexed) |
| `pageSize` | int | 20 | Items per page (max 100) |

Paginated responses include:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 150,
  "totalPages": 8
}
```

---

## Auth Endpoints

Base path: `/api/v1/auth`

| Method | Path | Auth | Roles | Description |
|---|---|---|---|---|
| POST | `/register` | No | -- | Register a new user account |
| POST | `/login` | No | -- | Authenticate and receive JWT + refresh token |
| POST | `/refresh` | No | -- | Exchange a valid refresh token for new tokens |
| POST | `/logout` | Yes | All | Revoke the current refresh token |
| POST | `/forgot-password` | No | -- | **Planned** -- Request a password reset email |
| POST | `/reset-password` | No | -- | **Planned** -- Reset password using emailed token |
| POST | `/change-password` | Yes | All | **Planned** -- Change password (requires current password) |
| POST | `/onboarding` | Yes | All | Select role and complete onboarding |
| GET | `/me` | Yes | All | Get current authenticated user info |

> **Note:** `/forgot-password`, `/reset-password`, and `/change-password` have DTOs and validators defined in the Application layer but the controller endpoints are not yet implemented.

### Request/Response Details

**POST /register**

```
Request:
{
  "firstName": "string",
  "lastName": "string",
  "email": "string",
  "password": "string",
  "confirmPassword": "string"
}

Response:
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresAt": "datetime",
  "user": {
    "id": "guid",
    "firstName": "string",
    "lastName": "string",
    "email": "string",
    "role": "Unassigned|Student|Consultant|Company|Admin",
    "accountStatus": "Active|Pending|Suspended|Rejected",
    "profileImageUrl": "string?",
    "isOnboardingComplete": false
  }
}
```

**POST /login**

```
Request:
{
  "identifier": "string",
  "password": "string",
  "rememberMe": false
}

Response:
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresAt": "datetime",
  "user": {
    "id": "guid",
    "firstName": "string",
    "lastName": "string",
    "email": "string",
    "role": "Unassigned|Student|Consultant|Company|Admin",
    "accountStatus": "Active|Pending|Suspended|Rejected",
    "profileImageUrl": "string?",
    "isOnboardingComplete": true
  }
}
```

**POST /refresh**

```
Request:
{
  "refreshToken": "string"
}

Response:
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresAt": "datetime",
  "user": {
    "id": "guid",
    "firstName": "string",
    "lastName": "string",
    "email": "string",
    "role": "Unassigned|Student|Consultant|Company|Admin",
    "accountStatus": "Active|Pending|Suspended|Rejected",
    "profileImageUrl": "string?",
    "isOnboardingComplete": true
  }
}
```

**POST /logout**

```
Request:
{
  "refreshToken": "string"
}

Response: 204 No Content
```

**POST /forgot-password**

```
Request:
{
  "email": "string"
}

Response:
{
  "message": "If an account exists, a reset email has been sent"
}
```

**POST /reset-password**

```
Request:
{
  "token": "string",
  "newPassword": "string",
  "confirmNewPassword": "string"
}

Response:
{
  "message": "Password has been reset"
}
```

**POST /change-password**

```
Request:
{
  "currentPassword": "string",
  "newPassword": "string",
  "confirmNewPassword": "string"
}

Response: 204 No Content
```

**POST /onboarding**

```
Request:
{
  "selectedRole": "Student|Consultant|Company",
  "companyName": "string?",
  "expertiseArea": "string?",
  "bio": "string?"
}

Response (Student - immediate activation):
{
  "id": "guid",
  "firstName": "string",
  "lastName": "string",
  "email": "string",
  "role": "Student",
  "accountStatus": "Active",
  "profileImageUrl": "string?",
  "isOnboardingComplete": true
}

Response (Consultant/Company - pending approval):
{
  "id": "guid",
  "firstName": "string",
  "lastName": "string",
  "email": "string",
  "role": "Unassigned",
  "accountStatus": "Pending",
  "profileImageUrl": "string?",
  "isOnboardingComplete": true
}
```

**GET /me**

```
Response:
{
  "id": "guid",
  "firstName": "string",
  "lastName": "string",
  "email": "string",
  "role": "Unassigned|Student|Consultant|Company|Admin",
  "accountStatus": "Active|Pending|Suspended|Rejected",
  "profileImageUrl": "string?",
  "isOnboardingComplete": true
}
```

---

> **The following sections (Profile, Scholarships, Community, Notifications) are planned specifications. No controllers are implemented yet. These contracts are subject to change during implementation.**

---

## Profile Endpoints

Base path: `/api/v1/profile`

| Method | Path | Auth | Roles | Description |
|---|---|---|---|---|
| GET | `/` | Yes | All | Get the current user's profile |
| PUT | `/` | Yes | All | Update the current user's profile |
| POST | `/image` | Yes | All | Upload or replace profile image |
| GET | `/upgrade-status` | Yes | Student | Get status of pending upgrade request |
| POST | `/upgrade-request/consultant` | Yes | Student | Submit upgrade request for Consultant role |
| POST | `/upgrade-request/company` | Yes | Student | Submit upgrade request for Company role |

### Request/Response Details

**GET /profile**

```
Response: { id, userId, fieldOfStudy, gpa, interests, country, bio, profileImageUrl }
```

**PUT /profile**

```
Request:  { fieldOfStudy?, gpa?, interests?, country?, bio? }
Response: { id, userId, fieldOfStudy, gpa, interests, country, bio, profileImageUrl }
```

**POST /profile/image**

```
Request:  multipart/form-data { image: file }
Response: { profileImageUrl }
```

**GET /profile/upgrade-status**

```
Response: { id, requestedRole, status, justification, reviewNotes?, reviewedAt?, createdAt }
```

**POST /profile/upgrade-request/consultant**

```
Request:  { justification }
Response: { id, requestedRole, status, createdAt }
```

**POST /profile/upgrade-request/company**

```
Request:  { justification, companyName, companyWebsite }
Response: { id, requestedRole, status, createdAt }
```

---

## Scholarship Endpoints

Base path: `/api/v1/scholarships`

| Method | Path | Auth | Roles | Description |
|---|---|---|---|---|
| GET | `/` | No | -- | List scholarships with filters and pagination |
| GET | `/{id}` | No | -- | Get scholarship details by ID |
| POST | `/{id}/save` | Yes | All | Save a scholarship to the user's list |
| DELETE | `/{id}/save` | Yes | All | Remove a scholarship from the user's saved list |

### Query Parameters for GET /scholarships

| Parameter | Type | Description |
|---|---|---|
| `search` | string | Full-text search on title and description |
| `country` | string | Filter by country |
| `fundingType` | int | Filter by funding type enum value |
| `degreeLevel` | int | Filter by degree level enum value |
| `categoryId` | guid | Filter by category ID |
| `deadlineFrom` | date | Scholarships with deadline on or after this date |
| `deadlineTo` | date | Scholarships with deadline on or before this date |
| `sortBy` | string | Sort field: `deadline`, `title`, `createdAt` (default: `deadline`) |
| `sortOrder` | string | `asc` or `desc` (default: `asc`) |
| `page` | int | Page number |
| `pageSize` | int | Items per page |

### Request/Response Details

**GET /scholarships**

```
Response: {
  items: [{ id, title, country, fundingType, degreeLevel, deadline, categoryName }],
  page, pageSize, totalCount, totalPages
}
```

**GET /scholarships/{id}**

```
Response: { id, title, description, country, fundingType, degreeLevel, deadline, officialLink, categoryId, categoryName, isSaved }
```

**POST /scholarships/{id}/save**

```
Response: 201 Created
```

**DELETE /scholarships/{id}/save**

```
Response: 204 No Content
```

---

## Community Endpoints

Base path: `/api/v1/community`

| Method | Path | Auth | Roles | Description |
|---|---|---|---|---|
| GET | `/groups` | Yes | All | List groups (with search and pagination) |
| POST | `/groups` | Yes | All | Create a new group |
| GET | `/groups/{id}` | Yes | All | Get group details and membership info |
| POST | `/groups/{id}/join` | Yes | All | Join a group (or request to join if private) |
| DELETE | `/groups/{id}/leave` | Yes | All | Leave a group |
| GET | `/groups/{id}/posts` | Yes | All | List posts in a group (paginated) |
| POST | `/groups/{id}/posts` | Yes | All | Create a post in a group |
| GET | `/posts/{id}` | Yes | All | Get a single post with comments |
| PUT | `/posts/{id}` | Yes | All | Edit own post |
| DELETE | `/posts/{id}` | Yes | All | Delete own post (soft delete) |
| POST | `/posts/{id}/comments` | Yes | All | Add a comment to a post |
| PUT | `/comments/{id}` | Yes | All | Edit own comment |
| DELETE | `/comments/{id}` | Yes | All | Delete own comment (soft delete) |
| POST | `/posts/{id}/like` | Yes | All | Toggle like on a post |
| POST | `/comments/{id}/like` | Yes | All | Toggle like on a comment |

### Request/Response Details

**GET /community/groups**

```
Query:    { search?, isPrivate?, page, pageSize }
Response: { items: [{ id, name, description, isPrivate, memberCount, imageUrl }], page, pageSize, totalCount, totalPages }
```

**POST /community/groups**

```
Request:  { name, description, isPrivate }
Response: { id, name, description, isPrivate, creatorId, createdAt }
```

**GET /community/groups/{id}**

```
Response: { id, name, description, isPrivate, creatorId, imageUrl, memberCount, isMember, myRole, createdAt }
```

**POST /community/groups/{id}/join**

```
Response: 200 OK { status: "joined" } or { status: "pending" }
```

**GET /community/groups/{id}/posts**

```
Query:    { page, pageSize }
Response: { items: [{ id, authorId, authorName, content, imageUrl, likeCount, commentCount, isLiked, createdAt }], ... }
```

**POST /community/groups/{id}/posts**

```
Request:  { content, imageUrl? }
Response: { id, groupId, authorId, content, imageUrl, createdAt }
```

**POST /community/posts/{id}/comments**

```
Request:  { content, parentCommentId? }
Response: { id, postId, authorId, content, parentCommentId, createdAt }
```

**POST /community/posts/{id}/like**

```
Response: { liked: true, likeCount: 15 } or { liked: false, likeCount: 14 }
```

---

## Notification Endpoints

Base path: `/api/v1/notifications`

| Method | Path | Auth | Roles | Description |
|---|---|---|---|---|
| GET | `/` | Yes | All | List notifications for current user (paginated) |
| PUT | `/{id}/read` | Yes | All | Mark a single notification as read |
| PUT | `/read-all` | Yes | All | Mark all notifications as read |
| GET | `/unread-count` | Yes | All | Get count of unread notifications |

### Request/Response Details

**GET /notifications**

```
Query:    { isRead?, page, pageSize }
Response: { items: [{ id, type, title, body, isRead, referenceId, createdAt }], page, pageSize, totalCount, totalPages, unreadCount }
```

**PUT /notifications/{id}/read**

```
Response: 204 No Content
```

**PUT /notifications/read-all**

```
Response: 204 No Content
```

**GET /notifications/unread-count**

```
Response: { count: 5 }
```

---

## Admin Endpoints

Base path: `/api/v1/admin`

| Method | Path | Auth | Roles | Description |
|---|---|---|---|---|
| GET | `/upgrade-requests` | Yes | Admin | List all upgrade requests (filterable by status) |
| GET | `/upgrade-requests/{id}` | Yes | Admin | Get detailed info about a specific upgrade request |
| PUT | `/upgrade-requests/{id}/approve` | Yes | Admin | Approve an upgrade request |
| PUT | `/upgrade-requests/{id}/reject` | Yes | Admin | Reject an upgrade request |
| PUT | `/upgrade-requests/{id}/request-info` | Yes | Admin | Request additional info from the applicant |
| GET | `/users` | Yes | Admin | List all users (with search, filter, pagination) |
| PUT | `/users/{id}/suspend` | Yes | Admin | Suspend a user account |
| PUT | `/users/{id}/activate` | Yes | Admin | Reactivate a suspended user account |
| GET | `/scholarships` | Yes | Admin | List all scholarships (including soft-deleted) |
| POST | `/scholarships` | Yes | Admin | Create a new scholarship |
| PUT | `/scholarships/{id}` | Yes | Admin | Update a scholarship |
| DELETE | `/scholarships/{id}` | Yes | Admin | Soft-delete a scholarship |
| GET | `/success-stories` | Yes | Admin | List success stories pending approval |
| PUT | `/success-stories/{id}/approve` | Yes | Admin | Approve a success story |
| PUT | `/success-stories/{id}/reject` | Yes | Admin | Reject a success story |

> **Currently implemented:** `GET /upgrade-requests`, `PUT .../approve`, `PUT .../reject`, `PUT .../request-info`. Other admin endpoints listed above are planned.

### Request/Response Details

**GET /admin/upgrade-requests**

```
Query:    { status?, page, pageSize }
Response: { items: [{ id, userId, userName, userEmail, requestedRole, status, adminNotes, createdAt }], page, pageSize, totalCount, totalPages }
```

**PUT /admin/upgrade-requests/{id}/approve**

```
Response: { id, status: "Approved", reviewedAt }
```

**PUT /admin/upgrade-requests/{id}/reject**

```
Request:  { adminNotes }
Response: { id, status: "Rejected", adminNotes, reviewedAt }
```

**PUT /admin/upgrade-requests/{id}/request-info**

```
Request:  { adminNotes }
Response: { id, status: "NeedsMoreInfo", adminNotes, reviewedAt }
```

**POST /admin/scholarships**

```
Request:  { title, description, country, fundingType, degreeLevel, deadline, officialLink, categoryId }
Response: { id, title, ... , createdAt }
```

---

## Error Response Format

> **Note:** There is currently an inconsistency in error responses. Controller-level errors (e.g., validation, business logic) return `{ "error": "..." }` format, while unhandled exceptions and framework-level errors use the standard ProblemDetails format shown below. This will be unified in a future update.

```json
{
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/auth/register",
  "errors": [
    "Email is required.",
    "Password must be at least 8 characters."
  ]
}
```

---

## Versioning

The API uses URL-based versioning (`/api/v1/`). When breaking changes are introduced, a new version (`/api/v2/`) will be created while maintaining backward compatibility for the previous version during a deprecation period.

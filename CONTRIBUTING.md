# Contributing to ScholarPath

This is the team workflow guide. Read it carefully before writing any code.

---

## Table of Contents

- [Important: Branch Protection](#important-branch-protection)
- [Setup (One Time)](#setup-one-time)
- [Daily Workflow](#daily-workflow)
- [Branch Naming Convention](#branch-naming-convention)
- [Commit Message Format](#commit-message-format)
- [Pull Request Guide](#pull-request-guide)
- [Code Review Process](#code-review-process)
- [Code Style](#code-style)
- [Common Git Commands Reference](#common-git-commands-reference)

---

## Important: Branch Protection

The `main` branch is **protected**. This means:

- **Nobody** can push directly to `main` (not even the project lead).
- All changes **must** go through a Pull Request (PR).
- Every PR **must** be reviewed and approved by at least one team member.
- CI checks (build + lint) **must** pass before merging.
- Branches must be **up to date** with `main` before merging.

This prevents broken code from reaching `main` and ensures every change is reviewed.

---

## Setup (One Time)

### 1. Accept the collaboration invitation

Check your GitHub notifications or email and accept the invite to `ma7moudalysalem/ScholarPath`.

### 2. Clone the repository

```bash
git clone https://github.com/ma7moudalysalem/ScholarPath.git
cd ScholarPath
```

### 3. Install dependencies

**Backend:**

```bash
cd server
dotnet restore
```

**Frontend:**

```bash
cd client
npm install
```

### 4. Set up your environment

Copy the example environment files and fill in your local values:

```bash
# Root
cp .env.example .env

# Frontend
cp client/.env.example client/.env
```

### 5. Verify everything works

```bash
# Backend build
cd server
dotnet build ScholarPath.slnx

# Frontend build
cd ../client
npm run build
```

---

## Daily Workflow

This is the workflow you will follow **every time** you work on a task:

### Step 1: Update your local `main`

```bash
git checkout main
git pull origin main
```

### Step 2: Create a new branch

```bash
git checkout -b feature/your-task-name
```

> Use the [branch naming convention](#branch-naming-convention) below.

### Step 3: Write your code

Make changes, commit as you go:

```bash
git add .
git commit -m "feat(auth): add login endpoint"
```

### Step 4: Push your branch

```bash
git push origin feature/your-task-name
```

### Step 5: Open a Pull Request on GitHub

1. Go to the repository on GitHub.
2. You will see a banner saying "Compare & pull request" -- click it.
3. Fill in the PR title (use commit message format: `feat(auth): add login endpoint`).
4. Write a short description of what you did.
5. Assign a reviewer from the team.
6. Click "Create pull request".

### Step 6: Wait for review

- A team member will review your code.
- If they request changes, make the fixes on the **same branch**, commit, and push again. The PR updates automatically.
- Once approved and CI passes, click **"Squash and merge"**.

### Step 7: Clean up

After your PR is merged:

```bash
git checkout main
git pull origin main
git branch -d feature/your-task-name
```

---

## Branch Naming Convention

| Prefix | Purpose | Example |
|---|---|---|
| `feature/` | New features or enhancements | `feature/scholarship-search` |
| `bugfix/` | Bug fixes | `bugfix/login-redirect-loop` |
| `hotfix/` | Urgent production fixes | `hotfix/token-expiry-crash` |
| `docs/` | Documentation updates | `docs/api-endpoint-guide` |
| `refactor/` | Code refactoring (no behavior change) | `refactor/user-service-cleanup` |
| `test/` | Adding or updating tests | `test/notification-service` |

Rules:
- Lowercase letters, numbers, and hyphens only.
- Keep it short and descriptive.
- One branch per task. Do not mix unrelated work.

---

## Commit Message Format

Follow the **Conventional Commits** specification:

```
type(scope): description
```

### Types

| Type | Description |
|---|---|
| `feat` | A new feature |
| `fix` | A bug fix |
| `docs` | Documentation changes only |
| `style` | Formatting, whitespace (no logic change) |
| `refactor` | Code restructuring without changing behavior |
| `test` | Adding or updating tests |
| `chore` | Build scripts, CI config, dependencies |

### Scope

The scope identifies the area affected: `auth`, `scholarships`, `chat`, `notifications`, `api`, `ui`, `db`, `profile`.

### Examples

```
feat(scholarships): add deadline filtering to search results
fix(auth): resolve token refresh race condition
docs(api): document pagination query parameters
refactor(notifications): extract builder into separate service
test(chat): add integration tests for group messages
chore(ci): add code coverage to GitHub Actions
```

### Rules

- Use imperative mood: "add" not "added" or "adds".
- Do not capitalize the first letter of the description.
- Do not end with a period.
- Keep the subject line under 72 characters.

---

## Pull Request Guide

### Before opening a PR

Run these checks locally:

```bash
# Backend
cd server
dotnet build ScholarPath.slnx
dotnet test

# Frontend
cd client
npm run lint
npm run build
```

### PR requirements

1. **Title**: Use commit message format (`type(scope): description`).
2. **Description**: Explain what the PR does and why. Reference related issues (`Closes #42`).
3. **Focused scope**: One PR = one task. Do not mix unrelated changes.
4. **Tests**: Add tests for new functionality. Existing tests must pass.
5. **No lint errors**: Both backend and frontend must pass linting.
6. **CI passes**: The automated pipeline must complete successfully.
7. **Up to date**: Rebase on latest `main` if your branch is behind.

### How to rebase if your branch is behind `main`

```bash
git checkout main
git pull origin main
git checkout feature/your-branch
git rebase main
# If there are conflicts, resolve them, then:
git add .
git rebase --continue
# Push the updated branch (force push needed after rebase):
git push origin feature/your-branch --force-with-lease
```

---

## Code Review Process

1. All PRs require at least **one approving review** before merging.
2. Reviewers check for:
   - Correctness and completeness.
   - Test coverage.
   - Architecture and coding convention adherence.
   - Performance and edge cases.
   - Security (especially auth and data handling).
3. Address all review comments before requesting re-review.
4. The PR author merges after approval using **"Squash and merge"**.

### Review assignments

| Team Member | Primary Review Area |
|---|---|
| Mahmoud Salem | Full Stack / Architecture |
| Yousra Elnoby | Frontend / Backend |
| Tasneem Shaaban | Backend / Data |
| Madiha | Frontend / Backend |
| Nora Mohamed | Backend |

---

## Code Style

### General

- Follow existing codebase conventions. Consistency over personal preference.
- Respect the `.editorconfig` settings.

### Backend (C# / .NET)

- Follow the official [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- Use meaningful, descriptive names.
- Apply the CQRS pattern: separate Command and Query handlers.
- Place validators alongside their corresponding commands/queries.
- Use `async`/`await` for all I/O-bound operations.

### Frontend (React / TypeScript)

- Functional components with hooks only.
- Explicit TypeScript types; never use `any`.
- Zustand for global state, TanStack Query for server state.
- Reusable components in `components/`, page components in `pages/`.
- Follow existing MUI theme conventions.

---

## Common Git Commands Reference

| What you want to do | Command |
|---|---|
| Update local main | `git checkout main && git pull origin main` |
| Create new branch | `git checkout -b feature/my-task` |
| Check current branch | `git branch` |
| Stage all changes | `git add .` |
| Commit changes | `git commit -m "feat(scope): description"` |
| Push branch | `git push origin feature/my-task` |
| Switch branches | `git checkout branch-name` |
| Delete local branch after merge | `git branch -d feature/my-task` |
| See status of changes | `git status` |
| See commit history | `git log --oneline -10` |
| Undo last commit (keep changes) | `git reset --soft HEAD~1` |

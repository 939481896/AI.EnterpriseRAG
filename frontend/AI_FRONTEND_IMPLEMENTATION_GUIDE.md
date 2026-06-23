# AI Frontend Implementation Guide

This document is the operational handbook for a new AI agent working on this frontend.

It explains:
- architecture and boundaries
- coding patterns and workflows
- how to add or modify features safely
- quality gates and release readiness

Use this as the source of truth before making code changes.

---

## 1. Project Snapshot

Name: enterprise-rag-frontend

Core purpose:
- Enterprise knowledge assistant frontend with Chat, Document Management, Agent Workspace, and Admin (RBAC).

Current maturity:
- Phase 1 complete: module registry, i18n centralization, reusable page templates, query key standardization.
- Phase 2 complete: unit test infrastructure, auth/error/JWT tests, monitoring and performance services.

Main stack:
- React 18, TypeScript 5, Vite 5
- React Router 6
- React Query 5 for server state
- Zustand for client state
- Axios client with interceptors
- Ant Design 5
- Vitest + Testing Library
- Sentry (@sentry/react) + local performance monitor

---

## 2. Non-Negotiable Architecture Rules

1. Do not call HTTP directly from pages/components.
- Always use domain API modules under src/api.

2. Keep business data orchestration in hooks.
- Query + mutation + invalidation + local side effects belong in src/hooks.

3. Keep client state and server state separate.
- Server state: React Query.
- Client state: Zustand stores.

4. Never hardcode business UI strings.
- Add text keys in src/config/uiText.ts and consume through the centralized accessor.

5. Route/menu/permission must stay aligned.
- Update src/config/modules.ts for any new module page.

6. Keep query keys centralized.
- Add/modify keys only in src/config/queryKeys.ts.

7. Follow existing security pipeline.
- Reuse src/api/client.ts for auth headers, 401 handling, and sanitization.

---

## 3. System Map (Where Logic Lives)

Entry and app shell:
- src/main.tsx
- src/App.tsx

Routing and module registry:
- src/config/modules.ts

Layout and menu rendering:
- src/components/Layout/AppLayout.tsx

Domain APIs:
- src/api/auth.ts
- src/api/chat.ts
- src/api/document.ts
- src/api/permission.ts
- src/api/user.ts
- src/api/agent.ts
- shared client: src/api/client.ts

Business hooks:
- src/hooks/useAuth.ts
- src/hooks/useChat.ts
- src/hooks/useDocument.ts
- src/hooks/usePermission.ts

Client stores:
- src/store/authStore.ts
- src/store/chatStore.ts
- src/store/localeStore.ts

Cross-cutting config/services:
- src/config/uiText.ts
- src/config/queryKeys.ts
- src/config/errorPolicy.ts
- src/services/notification.ts
- src/services/monitoring.ts
- src/services/performance.ts

Testing:
- vitest.config.ts
- src/__tests__/setup.ts
- src/__tests__/utils.tsx
- src/__tests__/*.test.ts

---

## 4. Runtime and Data Flow

Standard flow:
1. Page triggers user action.
2. Hook runs query/mutation.
3. Hook calls API module.
4. API module uses shared Axios client.
5. Response returns through client interceptors.
6. Hook updates cache or invalidates query keys.
7. Page rerenders from query/store state.

Auth flow:
1. Login request in useAuth mutation.
2. JWT validated locally (format + expiration).
3. Token/user stored in authStore.
4. ProtectedRoute checks isAuthenticated + validateToken.
5. Invalid or expired token forces logout and redirect.

Permission flow:
1. PermissionProvider loads permissions from backend via hook.
2. hasPermission/hasAnyPermission/hasAllPermissions exposed by context.
3. AppLayout and guarded UI actions use permission checks.

---

## 5. Canonical Implementation Patterns

### 5.1 Add a New Page Module

Required steps:
1. Create page in src/pages/<Domain>/.
2. Add API functions in src/api/<domain>.ts.
3. Add hook in src/hooks/use<Domain>.ts.
4. Add query keys in src/config/queryKeys.ts.
5. Add i18n text keys in src/config/uiText.ts.
6. Register route/menu/permissions in src/config/modules.ts.
7. Add/adjust tests.

If step 6 is skipped, route/menu/permission consistency breaks.

### 5.2 Add New Server Query

1. Define key in src/config/queryKeys.ts.
2. Implement useQuery in domain hook.
3. Use meta options for notification behavior as needed.
4. Keep staleTime/retry behavior consistent with current policy.

### 5.3 Add New Mutation

1. Implement in domain hook via useMutation.
2. Use local notification strategy and avoid duplicate global toasts.
3. Invalidate exact query key(s) on success.
4. Keep payload/result typed.

### 5.4 Add New Permission-Protected Action

1. Add permission code in modules config or domain permission constants.
2. Ensure backend returns that permission for authorized roles.
3. Guard UI action using permission context or guard component.
4. Validate behavior with admin RBAC pages.

### 5.5 Add New i18n Text

1. Add keys to both zh-CN and en-US dictionaries in src/config/uiText.ts.
2. Replace raw strings in component/page with centralized keys.
3. Verify switch behavior from locale selector in AppLayout.

---

## 6. Security and Reliability Guardrails

1. Always use shared API client.
- It adds JWT header and applies global 401 handling.

2. Keep sanitization in place.
- client.ts sanitizes response and error message content.

3. Do not leak sensitive values in logs.
- Keep production logging minimal and metadata-only.

4. Respect token validation logic.
- JWT base64url decoding and expiration buffer are already implemented.

5. Avoid bypassing auth store lifecycle.
- Set token/user through store actions only.

---

## 7. Observability and Performance

Sentry:
- Initialized in src/main.tsx through initSentry().
- Controlled via VITE_SENTRY_DSN and production mode.

Performance monitor:
- Initialized in src/main.tsx through initPerformanceMonitoring().
- Captures Web Vitals and API timing.
- Optional outbound metric send via VITE_ANALYTICS_ENABLED.

When adding new expensive UI or API calls:
- record component or API timing through performance service.
- avoid noisy logs in production.

---

## 8. Testing Strategy for AI Changes

Minimum expectation for logic change:
1. Add or update at least one relevant unit test.
2. Keep existing test setup and helpers usage.
3. Do not introduce flaky asynchronous assertions.

Current high-value suites:
- auth store behavior
- JWT validation behavior
- error utility behavior

Test commands:
- npm run test
- npm run test:watch
- npm run test:coverage

---

## 9. Quality Gate Before Merge

Always run:
1. npm run lint
2. npm run type-check
3. npm run test
4. npm run build

A change is not complete unless all 4 pass.

---

## 10. Anti-Patterns to Avoid

1. Hardcoded UI labels inside pages/components.
2. New route added in page but not in modules registry.
3. Calling axios directly from UI layer.
4. Mutation without query invalidation.
5. Using any where exact types are available.
6. Duplicated global + local notifications for same failure.
7. Storing server lists in Zustand that belong in React Query cache.

---

## 11. Fast Start for a New AI Session

Read in this order:
1. src/main.tsx
2. src/App.tsx
3. src/config/modules.ts
4. src/api/client.ts
5. src/config/queryKeys.ts
6. src/hooks/useAuth.ts
7. src/store/authStore.ts
8. src/components/Layout/AppLayout.tsx

Then run:
1. npm run test
2. npm run build

Then implement smallest safe change first.

---

## 12. Delivery Checklist Template

For every task, confirm:
1. Which layer changed (page/hook/api/store/config/service)?
2. Is module registry update needed?
3. Are query keys or invalidation rules impacted?
4. Are new i18n keys required?
5. Is permission behavior impacted?
6. Are tests updated?
7. Do lint/type-check/test/build all pass?

---

## 13. Enterprise Readiness Assessment

Current strengths:
- Clear layering and modular architecture
- strict TypeScript patterns
- centralized routing/permission/i18n/query keys
- baseline observability and unit-test foundation

Recommended next maturity upgrades:
1. Add E2E tests for critical user journeys.
2. Add CI pipeline that enforces quality gate commands.
3. Add accessibility checks (keyboard and screen reader compliance).
4. Add bundle budget and performance alert thresholds.
5. Add release versioning and rollback runbook.

---

## 14. Supporting Documents

- FIRST_PRIORITY_COMPLETE.md
- FIRST_PRIORITY_COMPLETION.md
- QUERY_KEYS_GUIDE.md
- PHASE_2_IMPROVEMENTS.md
- PHASE_2_COMPLETE.md
- src/config/MODULES_GUIDE.md
- src/pages/Templates/TEMPLATES_GUIDE.md

Use these with this guide for deep context.

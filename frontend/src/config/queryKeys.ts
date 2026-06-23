/**
 * React Query key registry.
 *
 * Why centralized:
 * 1) Prevent typo-based cache misses.
 * 2) Keep invalidateQueries/refetchQueries consistent across modules.
 * 3) Make key evolution (prefix/versioning) safe and discoverable.
 */
export const queryKeys = {
  /** Chat domain cache keys */
  chat: {
    /** Session list for current authenticated user */
    sessions: ['chat', 'sessions'] as const,
    /** Messages under a specific chat session */
    sessionMessages: (sessionId: string | null) => ['chat', 'session-messages', sessionId] as const,
  },
  /** Document domain cache keys */
  document: {
    /** Prefix key for invalidating all document-list related queries */
    all: ['documents'] as const,
    /** Paged document list */
    list: (page: number, pageSize: number) => ['documents', page, pageSize] as const,
    /** Static/rarely changed document categories */
    categories: ['documentCategories'] as const,
  },
  /** RBAC domain cache keys */
  permission: {
    roles: ['roles'] as const,
    role: (roleId: number | null) => ['role', roleId] as const,
    permissions: ['permissions'] as const,
    groupedPermissions: ['permissions', 'grouped'] as const,
    userRoles: (userId: number | null) => ['user-roles', userId] as const,
    userPermissions: (userId: number | null) => ['user-permissions', userId] as const,
  },
  /** User management cache keys */
  user: {
    list: ['users'] as const,
  },
} as const

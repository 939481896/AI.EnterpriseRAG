/**
 * Error Monitoring & Reporting with Sentry
 *
 * Integrates Sentry for:
 * - Error tracking and reporting
 * - Performance monitoring
 * - Release tracking
 * - User context
 *
 * NOTE: Sentry is optional and installed separately
 * This file uses dynamic imports to avoid bundling Sentry if not needed
 */

type Sentry = any

export function initSentry() {
  // Check if Sentry should be initialized
  if (!shouldInitSentry()) {
    console.log('[Monitoring] Sentry not configured for this environment')
    return
  }

  console.log('[Monitoring] Initializing Sentry...')

  // Dynamic import to avoid bundling Sentry if not needed
  try {
    // @ts-ignore - Sentry is optional, may not be installed
    import('@sentry/react').then((Sentry: Sentry) => {
      const dsn = import.meta.env.VITE_SENTRY_DSN as string
      const environment = import.meta.env.MODE

      Sentry.init({
        dsn,
        environment,
        tracesSampleRate: getTracesSampleRate(environment),
        release: `enterprise-rag-frontend@${__VERSION__}`,
        beforeSend(event: any, hint: any) {
          // Filter out certain errors before sending
          if (shouldIgnoreError(hint.originalException)) {
            return null
          }
          return event
        },
        integrations: [
          new Sentry.Replay({
            maskAllText: true,
            blockAllMedia: true,
          }),
        ],
        // Session Replay
        replaysSessionSampleRate: 0.1,
        replaysOnErrorSampleRate: 1.0,
      })

      // Set user context when available
      if (isUserAuthenticated()) {
        const user = getCurrentUser()
        Sentry.setUser({
          id: user.id,
          username: user.account,
          email: user.email,
        })
      }
    })
  } catch {
    console.warn('[Monitoring] Sentry not available - install with: npm install @sentry/react')
  }
}

/**
 * Capture exception with context
 */
export function captureException(error: Error, context?: Record<string, any>) {
  try {
    // @ts-ignore - Sentry is optional, may not be installed
    import('@sentry/react').then((Sentry: Sentry) => {
      if (context) {
        Sentry.captureException(error, { contexts: { custom: context } })
      } else {
        Sentry.captureException(error)
      }
    })
  } catch {
    // Sentry not available, error will be logged locally
    console.error('[Error]', error, context)
  }
}

/**
 * Capture message for non-error events
 */
export function captureMessage(message: string, level: 'info' | 'warning' | 'error' = 'info') {
  try {
    // @ts-ignore - Sentry is optional, may not be installed
    import('@sentry/react').then((Sentry: Sentry) => {
      Sentry.captureMessage(message, level)
    })
  } catch {
    // Sentry not available
    console.log(`[${level.toUpperCase()}]`, message)
  }
}

/**
 * Start a performance transaction
 */
export function startTransaction(name: string, op: string) {
  try {
    // @ts-ignore - Sentry is optional, may not be installed
    return import('@sentry/react').then((Sentry: Sentry) => {
      return Sentry.startTransaction({
        name,
        op,
      })
    })
  } catch {
    // Sentry not available
    return Promise.resolve(null)
  }
}

/**
 * Helper: Check if Sentry should be initialized
 */
function shouldInitSentry(): boolean {
  const dsn = import.meta.env.VITE_SENTRY_DSN
  const isProduction = import.meta.env.MODE === 'production'
  return !!dsn && isProduction
}

/**
 * Helper: Determine trace sample rate based on environment
 */
function getTracesSampleRate(environment: string): number {
  switch (environment) {
    case 'production':
      return 0.1 // 10% sampling in production
    case 'staging':
      return 0.5 // 50% sampling in staging
    default:
      return 1.0 // 100% in development
  }
}

/**
 * Helper: Filter errors that shouldn't be reported
 */
function shouldIgnoreError(error: any): boolean {
  // Ignore network errors (handled locally)
  if (error?.message?.includes('Network')) return true

  // Ignore user cancellations
  if (error?.message?.includes('cancelled')) return true

  // Ignore 401/403 errors (auth handled locally)
  if (error?.response?.status === 401 || error?.response?.status === 403) return true

  return false
}

/**
 * Helper: Check if user is authenticated
 */
function isUserAuthenticated(): boolean {
  try {
    const user = localStorage.getItem('user')
    return !!user
  } catch {
    return false
  }
}

/**
 * Helper: Get current user
 */
function getCurrentUser(): any {
  try {
    const user = localStorage.getItem('user')
    return user ? JSON.parse(user) : null
  } catch {
    return null
  }
}

/**
 * Placeholder for version - would be set by build process
 */
declare const __VERSION__: string

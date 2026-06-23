import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { lazy, Suspense, useEffect } from 'react'
import { useAuthStore } from './store/authStore'
import { PermissionProvider } from './contexts/PermissionContext'
import AppLayout from './components/Layout/AppLayout'
import { getRouteConfigs } from './config/modules'

type ComponentProps = object

type PreloadableLazy<T extends React.ComponentType<ComponentProps>> = React.LazyExoticComponent<T> & {
  preload: () => Promise<{ default: T }>
}

function lazyWithPreload<T extends React.ComponentType<ComponentProps>>(
  factory: () => Promise<{ default: T }>
): PreloadableLazy<T> {
  const Component = lazy(factory) as PreloadableLazy<T>
  Component.preload = factory
  return Component
}

type IdleWindow = Window & {
  requestIdleCallback?: (callback: IdleRequestCallback, options?: IdleRequestOptions) => number
  cancelIdleCallback?: (handle: number) => void
}

// ✅ Loading fallback component
const PageLoadingFallback = () => (
  <div style={{
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    height: '100vh',
    fontSize: '18px',
    color: '#666',
  }}>
    Loading...
  </div>
)

const ProtectedContentLoadingFallback = () => (
  <div
    style={{
      display: 'flex',
      justifyContent: 'center',
      alignItems: 'center',
      minHeight: 280,
      color: '#666',
      fontSize: 14,
    }}
  >
    Loading...
  </div>
)

// ✅ Public routes (lazy loaded)
const LoginPage = lazyWithPreload(() => import('./pages/Auth/LoginPage'))
const RegisterPage = lazyWithPreload(() => import('./pages/Auth/RegisterPage'))

// ✅ Protected routes are now dynamically generated from module registry
const routeConfigs = getRouteConfigs()
const protectedPages = routeConfigs.map((config) => {
  const component = config.component as PreloadableLazy<React.ComponentType<any>>
  return component
})

// Protected route wrapper
function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, validateToken, logout } = useAuthStore()

  useEffect(() => {
    // Validate token on mount and every time route changes
    if (isAuthenticated && !validateToken()) {
      logout()
    }
  }, [isAuthenticated, validateToken, logout])

  useEffect(() => {
    if (!isAuthenticated) return

    const preloadAll = () => {
      protectedPages.forEach((page) => {
        if (page && typeof (page as any).preload === 'function') {
          void (page as any).preload()
        }
      })
    }

    // Preload page chunks in idle time to avoid first-open route flashing.
    const idleWindow = window as IdleWindow
    if (typeof idleWindow.requestIdleCallback === 'function') {
      const idleId = idleWindow.requestIdleCallback(preloadAll, { timeout: 1500 })
      return () => {
        if (typeof idleWindow.cancelIdleCallback === 'function') {
          idleWindow.cancelIdleCallback(idleId)
        }
      }
    }

    const timer = setTimeout(preloadAll, 300)
    return () => {
      clearTimeout(timer)
    }
  }, [isAuthenticated])

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}

function App() {
  return (
    <BrowserRouter>
      <Suspense fallback={<PageLoadingFallback />}>
        <Routes>
          {/* Public routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          {/* Protected routes */}
          <Route
            path="/*"
            element={
              <ProtectedRoute>
                <PermissionProvider>
                  <AppLayout>
                    <Suspense fallback={<ProtectedContentLoadingFallback />}>
                      <Routes>
                        <Route path="/" element={<Navigate to="/chat" replace />} />
                        {routeConfigs.map((config) => (
                          <Route key={config.id} path={config.path} element={<config.component />} />
                        ))}
                      </Routes>
                    </Suspense>
                  </AppLayout>
                </PermissionProvider>
              </ProtectedRoute>
            }
          />
        </Routes>
      </Suspense>
    </BrowserRouter>
  )
}

export default App

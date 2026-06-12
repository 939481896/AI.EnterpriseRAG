import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useEffect } from 'react'
import { useAuthStore } from './store/authStore'
import { PermissionProvider } from './contexts/PermissionContext'
import AppLayout from './components/Layout/AppLayout'
import LoginPage from './pages/Auth/LoginPage'
import RegisterPage from './pages/Auth/RegisterPage'
import ChatPage from './pages/Chat/ChatPage'
import DocumentPage from './pages/Document/DocumentPage'
import AgentWorkspace from './pages/Agent/AgentWorkspace'
import Dashboard from './pages/Admin/Dashboard'
import UserManagement from './pages/Admin/UserManagement'
import RoleManagement from './pages/Admin/RoleManagement'
import PermissionManagement from './pages/Admin/PermissionManagement'
import RBACDebug from './pages/Admin/RBACDebug'

// Protected route wrapper
function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, validateToken, logout } = useAuthStore()

  useEffect(() => {
    // Validate token on mount and every time route changes
    if (isAuthenticated && !validateToken()) {
      logout()
    }
  }, [isAuthenticated, validateToken, logout])

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}

function App() {
  return (
    <BrowserRouter>
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
                  <Routes>
                    <Route path="/" element={<Navigate to="/chat" replace />} />
                    <Route path="/chat" element={<ChatPage />} />
                    <Route path="/documents" element={<DocumentPage />} />
                    <Route path="/agent" element={<AgentWorkspace />} />
                    <Route path="/admin/dashboard" element={<Dashboard />} />
                    <Route path="/admin/users" element={<UserManagement />} />
                    <Route path="/admin/roles" element={<RoleManagement />} />
                    <Route path="/admin/permissions" element={<PermissionManagement />} />
                    <Route path="/admin/debug-rbac" element={<RBACDebug />} />
                  </Routes>
                </AppLayout>
              </PermissionProvider>
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  )
}

export default App

import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useAuthStore } from './store/authStore'
import AppLayout from './components/Layout/AppLayout'
import LoginPage from './pages/Auth/LoginPage'
import RegisterPage from './pages/Auth/RegisterPage'
import ChatPage from './pages/Chat/ChatPage'
import DocumentPage from './pages/Document/DocumentPage'
import AgentWorkspace from './pages/Agent/AgentWorkspace'
import Dashboard from './pages/Admin/Dashboard'
import UserManagement from './pages/Admin/UserManagement'

// Protected route wrapper
function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuthStore()
  
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
              <AppLayout>
                <Routes>
                  <Route path="/" element={<Navigate to="/chat" replace />} />
                  <Route path="/chat" element={<ChatPage />} />
                  <Route path="/documents" element={<DocumentPage />} />
                  <Route path="/agent" element={<AgentWorkspace />} />
                  <Route path="/admin/dashboard" element={<Dashboard />} />
                  <Route path="/admin/users" element={<UserManagement />} />
                </Routes>
              </AppLayout>
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  )
}

export default App

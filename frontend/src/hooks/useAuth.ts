import { useMutation } from '@tanstack/react-query'
import { AxiosError } from 'axios'
import { authApi } from '@/api/auth'
import { useAuthStore } from '@/store/authStore'
import type { LoginRequest, RegisterRequest, User } from '@/types/auth'
import { notification } from '@/services/notification'
import { uiText } from '@/config/uiText'
import { getErrorMessage } from '@/types/error'

// ✅ Helper to validate JWT format locally
const isValidJWT = (token: string): boolean => {
  try {
    const parts = token.split('.')
    if (parts.length !== 3) {
      console.warn('[Auth] JWT does not have 3 parts')
      return false
    }

    // Decode each part - handle base64url encoding
    parts.forEach((part, index) => {
      try {
        // Convert base64url to base64 (replace - with + and _ with /)
        const base64 = part
          .replace(/-/g, '+')
          .replace(/_/g, '/')
        
        // Add padding if needed
        const padded = base64 + '='.repeat((4 - base64.length % 4) % 4)
        
        // Try to decode
        atob(padded)
      } catch (e) {
        console.error(`[Auth] Failed to decode JWT part ${index}:`, e)
        throw e
      }
    })
    return true
  } catch (e) {
    console.error('[Auth] JWT validation failed:', e)
    return false
  }
}

// ✅ Helper to check if token is expired
const isExpired = (token: string): boolean => {
  try {
    const parts = token.split('.')
    if (parts.length !== 3) return true
    
    // Convert base64url to base64 and add padding
    const base64 = parts[1]
      .replace(/-/g, '+')
      .replace(/_/g, '/')
    const padded = base64 + '='.repeat((4 - base64.length % 4) % 4)
    
    const payload = JSON.parse(atob(padded))
    if (!payload.exp) {
      console.warn('[Auth] Token has no exp claim')
      return true
    }
    
    const expirationTime = payload.exp * 1000
    const currentTime = Date.now()
    const isTokenExpired = currentTime >= expirationTime - 30000 // 30 second buffer
    
    if (isTokenExpired) {
      console.warn('[Auth] Token is expired', { 
        expiresIn: Math.round((expirationTime - currentTime) / 1000) 
      })
    }
    
    return isTokenExpired
  } catch (e) {
    console.error('[Auth] Token expiration check failed:', e)
    return true
  }
}

/**
 * Login mutation hook.
 * Responsibilities:
 * 1) Call auth API.
 * 2) Persist token/user in store + localStorage.
 * 3) Emit local user notification.
 */
export function useLogin() {
  const { setUser, setToken, setRefreshToken } = useAuthStore()

    return useMutation({
    meta: { silentError: true },
        mutationFn: (data: LoginRequest) => authApi.login(data),
        onSuccess: (response) => {
            // Handle cases where API returns HTTP 200 but success: false
            if (!response.success) {
              const errorMsg = response.message || uiText.auth.loginFailed
              notification.error(errorMsg)
              console.error('[Login] API returned success: false', response)
              return
            }

            // Handle missing data
            if (!response.data) {
              notification.error(uiText.auth.loginFailed)
              console.error('[Login] API response missing data')
              return
            }

            const { userId, accessToken, refreshToken, userName, permissions } = response.data

            console.log('[Login] Token received, length:', accessToken?.length)

            // ✅ Validate token before storing
            if (!accessToken) {
              notification.error(uiText.auth.loginFailed)
              console.error('[Login] No access token in response')
              return
            }

            if (!isValidJWT(accessToken)) {
              notification.error(uiText.auth.loginFailed)
              console.error('[Login] Invalid JWT format:', accessToken.substring(0, 50))
              return
            }

            if (isExpired(accessToken)) {
              notification.error(uiText.auth.loginFailed)
              console.error('[Login] Token is already expired')
              return
            }

            // 组装用户信息
            const userInfo: User = {
                id: userId,
                account: userName,
                userName,
                permissions,
            }

            // ✅ Token is validated, now set it and refresh token
            setToken(accessToken)
            if (refreshToken) {
              setRefreshToken(refreshToken)
            }
            setUser(userInfo)

            notification.success(uiText.auth.loginSuccess)
        },
        onError: (error: unknown) => {
              // Detect 401 errors and show user-friendly message
              if (error instanceof AxiosError && error.response?.status === 401) {
                notification.error(uiText.auth.loginFailed)
                return
              }

              const errorMessage = getErrorMessage(error) || uiText.auth.loginFailed
              notification.error(errorMessage)
            console.error('[Login] Request error:', error)
        },
    })
}

export function useRegister() {
  // Register flow keeps local notifications and suppresses global duplicate toast.
  return useMutation({
    meta: { silentError: true },
    mutationFn: (data: RegisterRequest) => authApi.register(data),
    onSuccess: (response) => {
      if (response.success) {
        notification.success(uiText.auth.registerSuccess)
      }
    },
    onError: () => {
      notification.error(uiText.auth.registerFailed)
    },
  })
}

export function useLogout() {
  const { logout } = useAuthStore()

  return () => {
    // Local logout always clears client state first to avoid stale-auth UI.
    authApi.logout()
    logout()
    notification.success(uiText.auth.logoutSuccess)
  }
}

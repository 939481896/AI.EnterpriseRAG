import { useMutation } from '@tanstack/react-query'
import { message } from 'antd'
import { authApi } from '@/api/auth'
import { useAuthStore } from '@/store/authStore'
import type { LoginRequest, RegisterRequest } from '@/types/auth'

export function useLogin() {
  const { setUser, setToken } = useAuthStore()

  return useMutation({
    mutationFn: (data: LoginRequest) => authApi.login(data),
    onSuccess: (response) => {
      if (response.success && response.data) {
        const { token, user } = response.data
        setToken(token)
        setUser(user)
        localStorage.setItem('token', token)
        localStorage.setItem('user', JSON.stringify(user))
        message.success('登录成功')
      }
    },
    onError: () => {
      message.error('登录失败，请检查账号密码')
    },
  })
}

export function useRegister() {
  return useMutation({
    mutationFn: (data: RegisterRequest) => authApi.register(data),
    onSuccess: (response) => {
      if (response.success) {
        message.success('注册成功，请登录')
      }
    },
    onError: () => {
      message.error('注册失败，请重试')
    },
  })
}

export function useLogout() {
  const { logout } = useAuthStore()

  return () => {
    authApi.logout()
    logout()
    message.success('已退出登录')
  }
}

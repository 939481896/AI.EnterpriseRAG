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
                const { accessToken, userName, permissions } = response.data

                // 组装用户信息
                const userInfo: any = {
                    account: userName, // 暂时使用 userName 作为 account
                    userName,
                    permissions,
                }

                setToken(accessToken)
                setUser(userInfo)

                localStorage.setItem('token', accessToken)
                localStorage.setItem('user', JSON.stringify(userInfo))

                message.success('登录成功')
            }
        },
        onError: (error: any) => {
            // ✅ 正确提取后端返回的错误消息
            const errorMessage = error.response?.data?.message 
                              || error.response?.data?.error
                              || error.message 
                              || '登录失败，请检查账号密码'
            message.error(errorMessage)
            console.error('登录错误:', error)
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

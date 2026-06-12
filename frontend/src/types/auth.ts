export interface User {
  id?: string
  account: string
  userName: string
  permissions: string[]
  isEnabled?: boolean
  createTime?: string
}

export interface LoginRequest {
  account: string
  password: string
  tenantId: string
}

export interface RegisterRequest {
  account: string
  password: string
  realName: string
  email: string
  phone?: string
  department?: string
}

export interface LoginResponse {
  userId: string
  accessToken: string
  refreshToken: string
  expiresIn: number
  userName: string
  permissions: string[]
}

export interface ApiResponse<T = any> {
  success: boolean
  data?: T
  message?: string
  code?: number
}

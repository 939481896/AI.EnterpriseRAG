export interface User {
  id: string
  account: string
  realName: string
  email: string
  department?: string
  isActive: boolean
  createTime: string
}

export interface LoginRequest {
  account: string
  password: string
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
  token: string
  expiresIn: number
  user: User
}

export interface ApiResponse<T = any> {
  success: boolean
  data?: T
  message?: string
  code?: number
}

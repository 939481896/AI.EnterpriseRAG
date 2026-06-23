export interface ApiResponse<T = unknown> {
  success: boolean
  data?: T
  message?: string
  code?: number | string
}

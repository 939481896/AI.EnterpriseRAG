/**
 * Error types and interfaces for API and application errors
 */

import { AxiosError } from 'axios'

/** ✅ API Error Response Structure */
export interface ApiErrorResponse {
  success: false
  code?: string | number
  message: string
  data?: unknown
}

/** ✅ Mutation Error Metadata */
export interface MutationErrorMeta {
  silentError?: boolean
  notifyOnBackground?: boolean
}

/** ✅ Query Error Metadata */
export interface QueryErrorMeta {
  silentError?: boolean
  notifyOnBackground?: boolean
}

/** ✅ Query Context Error Metadata */
export interface QueryContextMeta {
  silentError?: boolean
  notifyOnBackground?: boolean
}

/** ✅ Typed Axios Error */
export type ApiError = AxiosError<ApiErrorResponse>

/** ✅ Safe error extraction utility */
export function getErrorMessage(error: unknown): string {
  if (error instanceof AxiosError) {
    // For 401/403 errors, return statusText to avoid leaking sensitive details
    if (error.response?.status === 401 || error.response?.status === 403) {
      return error.response?.statusText || 'Unauthorized'
    }

    // For other errors, try to get the API error message
    const apiMessage = error.response?.data?.message
    if (apiMessage) {
      return apiMessage
    }

    return error.message || 'Unknown error occurred'
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'Unknown error occurred'
}

/** ✅ Safe error code extraction utility */
export function getErrorCode(error: unknown): string | number | undefined {
  if (error instanceof AxiosError) {
    return error.response?.data?.code || error.response?.status
  }

  return undefined
}

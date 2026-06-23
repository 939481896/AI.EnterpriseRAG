import type { AxiosError } from 'axios'
import { uiText } from '@/config/uiText'

export type NotifyLevel = 'error' | 'warning' | 'info'

interface ErrorPolicyEntry {
  message: string
  level?: NotifyLevel
  silent?: boolean
}

interface BackendErrorBody {
  code?: string | number
  message?: string
  error?: string
}

export interface ErrorNotifyContext {
  source?: 'api' | 'query' | 'mutation' | 'runtime'
  isBackground?: boolean
  meta?: {
    silentError?: boolean
    notifyOnBackground?: boolean
  }
}

function getHttpStatusMap(): Record<number, ErrorPolicyEntry> {
  return {
    400: { message: uiText.common.invalidRequest },
    401: { message: uiText.common.authExpired },
    403: { message: uiText.common.permissionDenied },
    404: { message: uiText.common.resourceNotFound },
    408: { message: uiText.common.requestTimeout },
    409: { message: uiText.common.conflictError },
    422: { message: uiText.common.validationFailed },
    429: { message: uiText.common.tooManyRequests },
    500: { message: uiText.common.serverError },
    502: { message: uiText.common.gatewayError },
    503: { message: uiText.common.serviceUnavailable },
    504: { message: uiText.common.responseTimeout },
  }
}

function getApiCodeMap(): Record<string, ErrorPolicyEntry> {
  return {
    AUTH_EXPIRED: { message: uiText.common.authExpired },
    AUTH_INVALID: { message: uiText.common.authInvalid },
    PERMISSION_DENIED: { message: uiText.common.permissionDenied },
    RESOURCE_NOT_FOUND: { message: uiText.common.resourceNotFound },
    VALIDATION_ERROR: { message: uiText.common.validationFailed },
    RATE_LIMITED: { message: uiText.common.tooManyRequests },
    NETWORK_ERROR: { message: uiText.common.networkError },
    CANCELED: { message: '', silent: true },
  }
}

export function shouldNotifyError(context?: ErrorNotifyContext): boolean {
  if (!context) return true
  if (context.meta?.silentError) return false

  const isBackground = !!context.isBackground
  if (context.source === 'query' && isBackground && !context.meta?.notifyOnBackground) {
    return false
  }

  return true
}

export function resolveErrorPolicy(error: unknown): ErrorPolicyEntry {
  const axiosError = error as AxiosError<BackendErrorBody>
  const apiCodeMap = getApiCodeMap()
  const httpStatusMap = getHttpStatusMap()

  if (axiosError.code === 'ERR_CANCELED') {
    return apiCodeMap.CANCELED
  }

  const responseData = axiosError.response?.data
  const apiCode = responseData?.code
  if (apiCode !== undefined && apiCode !== null) {
    const mapped = apiCodeMap[String(apiCode)]
    if (mapped) return mapped
  }

  const status = axiosError.response?.status
  if (status && httpStatusMap[status]) {
    return httpStatusMap[status]
  }

  if (axiosError.message.toLowerCase().includes('network')) {
    return apiCodeMap.NETWORK_ERROR
  }

  return {
    message:
      responseData?.message ||
      responseData?.error ||
      (error as Error).message ||
      uiText.common.unknownError,
    level: 'error',
  }
}

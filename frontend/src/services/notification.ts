import { message } from 'antd'
import type { AxiosError } from 'axios'
import { uiText } from '@/config/uiText'
import {
  resolveErrorPolicy,
  shouldNotifyError,
  type ErrorNotifyContext,
} from '@/config/errorPolicy'

/**
 * Unified notification service.
 *
 * Rules:
 * - UI layer calls success/info/warning/error directly.
 * - API/query/mutation flows use fromApiError() for policy-driven messaging.
 * - silentError/meta flags suppress duplicated toasts for locally handled mutations.
 */
type MessageType = 'success' | 'error' | 'info' | 'warning' | 'loading'

interface NotifyOptions {
  key?: string
  duration?: number
}

const AUTH_EXPIRED_MESSAGE = '登录已过期，请重新登录'

function open(type: MessageType, content: string, options?: NotifyOptions) {
  // Keep message rendering in one place for consistent key/duration behavior.
  message.open({
    type,
    content,
    key: options?.key,
    duration: options?.duration,
  })
}

function getApiErrorMessage(error: unknown, fallback: string = uiText.common.unknownError): string {
  const policy = resolveErrorPolicy(error)
  if (policy.message) {
    return policy.message
  }

  const axiosError = error as AxiosError<{ message?: string; error?: string }>
  return (
    axiosError.response?.data.message ||
    axiosError.response?.data.error ||
    (error as Error).message ||
    fallback
  )
}

function getAuthErrorToastKey(error: unknown, content: string): string | undefined {
  const axiosError = error as AxiosError<{ code?: string | number }>
  const status = axiosError.response?.status
  const code = axiosError.response?.data?.code

  if (status === 401) return 'auth-expired'
  if (code === 'AUTH_EXPIRED' || code === 'AUTH_INVALID') return 'auth-expired'
  if (content === AUTH_EXPIRED_MESSAGE) return 'auth-expired'

  return undefined
}

export const notification = {
  success(content: string, options?: NotifyOptions) {
    open('success', content, options)
  },
  error(content: string, options?: NotifyOptions) {
    open('error', content, options)
  },
  info(content: string, options?: NotifyOptions) {
    open('info', content, options)
  },
  warning(content: string, options?: NotifyOptions) {
    open('warning', content, options)
  },
  fromApiError(
    error: unknown,
    fallback?: string,
    options?: NotifyOptions,
    context?: ErrorNotifyContext
  ) {
    if (!shouldNotifyError(context)) {
      return
    }

    const policy = resolveErrorPolicy(error)
    if (policy.silent) {
      return
    }

    const level = policy.level || 'error'
    const content = policy.message || getApiErrorMessage(error, fallback)
    const authToastKey = getAuthErrorToastKey(error, content)
    open(level, content, {
      ...options,
      key: options?.key || authToastKey,
    })
  },
  getApiErrorMessage,
}

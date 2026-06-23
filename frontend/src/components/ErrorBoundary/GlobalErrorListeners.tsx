import { useEffect } from 'react'
import { notification } from '@/services/notification'
import { uiText } from '@/config/uiText'

export default function GlobalErrorListeners() {
  useEffect(() => {
    const onError = (event: ErrorEvent) => {
      const message = event.error?.message || event.message || uiText.common.unknownError
      notification.error(message, { key: 'window-error' })
    }

    const onUnhandledRejection = (event: PromiseRejectionEvent) => {
      const reason = event.reason
      const message =
        typeof reason === 'string'
          ? reason
          : reason?.message || uiText.common.unknownError
      notification.error(message, { key: 'promise-rejection' })
    }

    window.addEventListener('error', onError)
    window.addEventListener('unhandledrejection', onUnhandledRejection)

    return () => {
      window.removeEventListener('error', onError)
      window.removeEventListener('unhandledrejection', onUnhandledRejection)
    }
  }, [])

  return null
}

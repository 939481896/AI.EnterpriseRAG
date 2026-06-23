import React from 'react'
import { Alert, Button, Result, Typography } from 'antd'
import { uiText } from '@/config/uiText'
import { notification } from '@/services/notification'

interface GlobalErrorBoundaryState {
  hasError: boolean
  error?: Error
}

interface GlobalErrorBoundaryProps {
  children: React.ReactNode
}

export default class GlobalErrorBoundary extends React.Component<
  GlobalErrorBoundaryProps,
  GlobalErrorBoundaryState
> {
  state: GlobalErrorBoundaryState = {
    hasError: false,
    error: undefined,
  }

  static getDerivedStateFromError(error: Error): GlobalErrorBoundaryState {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo): void {
    console.error('GlobalErrorBoundary caught:', error, errorInfo)
    notification.error(uiText.common.unknownError, { key: 'global-runtime-error' })
  }

  render() {
    if (!this.state.hasError) {
      return this.props.children
    }

    return (
      <Result
        status="500"
        title={uiText.errorBoundary.title}
        subTitle={uiText.errorBoundary.subTitle}
        extra={
          <Button type="primary" onClick={() => { window.location.reload(); }}>
            {uiText.common.reloadPage}
          </Button>
        }
      >
        {this.state.error && (
          <Alert
            type="error"
            showIcon
            message={uiText.errorBoundary.details}
            description={
              <Typography.Text code>{this.state.error.message}</Typography.Text>
            }
          />
        )}
      </Result>
    )
  }
}

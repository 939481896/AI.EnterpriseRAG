import React, { useEffect } from 'react'
import ReactDOM from 'react-dom/client'
import { ConfigProvider, App as AntdApp } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import enUS from 'antd/locale/en_US'
import dayjs from 'dayjs'
import 'dayjs/locale/zh-cn'
import 'dayjs/locale/en'
import {
  MutationCache,
  QueryCache,
  QueryClient,
  QueryClientProvider,
} from '@tanstack/react-query'
import App from './App'
import GlobalErrorBoundary from '@/components/ErrorBoundary/GlobalErrorBoundary'
import GlobalErrorListeners from '@/components/ErrorBoundary/GlobalErrorListeners'
import { notification } from '@/services/notification'
import { dayjsLocaleMap, useLocaleStore } from '@/store/localeStore'
import './styles/global.css'

// Create React Query client
const queryClient = new QueryClient({
  queryCache: new QueryCache({
    onError: (error, query) => {
      notification.fromApiError(error, undefined, undefined, {
        source: 'query',
        isBackground: query.state.data !== undefined,
        meta: query.meta as { silentError?: boolean; notifyOnBackground?: boolean } | undefined,
      })
    },
  }),
  mutationCache: new MutationCache({
    onError: (error, _variables, _context, mutation) => {
      notification.fromApiError(error, undefined, undefined, {
        source: 'mutation',
        meta: mutation.meta as { silentError?: boolean; notifyOnBackground?: boolean } | undefined,
      })
    },
  }),
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
      staleTime: 5 * 60 * 1000, // 5 minutes
    },
  },
})

function AppProviders() {
  const locale = useLocaleStore((state) => state.locale)

  useEffect(() => {
    dayjs.locale(dayjsLocaleMap[locale])
  }, [locale])

  return (
    <ConfigProvider
      locale={locale === 'en-US' ? enUS : zhCN}
      theme={{
        token: {
          colorPrimary: '#1890ff',
          borderRadius: 6,
          fontSize: 14,
        },
      }}
    >
      <AntdApp>
        <GlobalErrorListeners />
        <App />
      </AntdApp>
    </ConfigProvider>
  )
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <GlobalErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <AppProviders />
      </QueryClientProvider>
    </GlobalErrorBoundary>
  </React.StrictMode>,
)

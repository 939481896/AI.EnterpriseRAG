import React from 'react'
import ReactDOM from 'react-dom/client'
import { ConfigProvider, App as AntdApp } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import dayjs from 'dayjs'
import 'dayjs/locale/zh-cn'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import App from './App'
import './styles/global.css'

// Set dayjs locale
dayjs.locale('zh-cn')

// Create React Query client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
      staleTime: 5 * 60 * 1000, // 5 minutes
    },
  },
})

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ConfigProvider
        locale={zhCN}
        theme={{
          token: {
            colorPrimary: '#1890ff',
            borderRadius: 6,
            fontSize: 14,
          },
        }}
      >
        <AntdApp>
          <App />
        </AntdApp>
      </ConfigProvider>
    </QueryClientProvider>
  </React.StrictMode>,
)

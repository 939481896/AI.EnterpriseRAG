import { ReactElement, ReactNode } from 'react'
import { render, RenderOptions } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

/**
 * Custom render function that wraps components with necessary providers
 */
export function renderWithProviders(
  ui: ReactElement,
  {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    }),
    ...renderOptions
  }: Omit<RenderOptions, 'wrapper'> & { queryClient?: QueryClient } = {}
) {
  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        {children}
      </QueryClientProvider>
    )
  }

  return { ...render(ui, { wrapper: Wrapper, ...renderOptions }), queryClient }
}

export * from '@testing-library/react'

/**
 * Mock API response builder
 */
export function mockApiResponse<T>(data: T, success = true) {
  return {
    success,
    code: success ? 0 : 1,
    message: success ? 'Success' : 'Error',
    data,
  }
}

/**
 * Create a mock token for testing
 */
export function createMockToken(expiresInHours = 24): string {
  const now = Math.floor(Date.now() / 1000)
  const exp = now + expiresInHours * 3600

  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
  const payload = btoa(JSON.stringify({ sub: 'user123', exp }))
  const signature = btoa('mock-signature')

  return `${header}.${payload}.${signature}`
}

/**
 * Create a mock user
 */
export function createMockUser(overrides = {}) {
  return {
    id: 'user-123',
    account: 'testuser',
    userName: 'Test User',
    permissions: ['menu.chat', 'chat.send', 'menu.document'],
    ...overrides,
  }
}

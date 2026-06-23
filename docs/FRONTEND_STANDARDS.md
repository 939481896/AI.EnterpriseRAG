# Enterprise Frontend Development Standards

> **Purpose**: Ensure consistency, maintainability, and quality across all frontend pages

## 📋 Table of Contents

1. [Architecture Patterns](#architecture-patterns)
2. [Component Standards](#component-standards)
3. [Page Structure](#page-structure)
4. [Styling Guidelines](#styling-guidelines)
5. [State Management](#state-management)
6. [API Integration](#api-integration)
7. [Error Handling](#error-handling)
8. [Testing Standards](#testing-standards)
9. [Code Examples](#code-examples)

---

## 1. Architecture Patterns

### File Organization

```
src/
├── components/
│   ├── Standard/           # Pre-built standard components
│   ├── Layout/             # Layout-specific components
│   ├── Common/             # Shared components
│   └── ErrorBoundary/      # Error handling
├── pages/
│   ├── Templates/          # Page templates
│   ├── [Feature]/          # Feature-specific pages
│   └── Admin/              # Admin pages
├── hooks/
│   ├── standard/           # Standard workflow hooks
│   ├── common/             # Common utility hooks
│   └── business/           # Business logic hooks
├── api/
│   └── modules/            # API endpoints by feature
├── utils/
├── types/
├── contexts/
├── locales/
└── assets/
```

### Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Component | PascalCase | `UserManagement.tsx` |
| Hook | camelCase with 'use' prefix | `useUserData.ts` |
| Utility | camelCase | `formatDate.ts` |
| Constant | UPPER_SNAKE_CASE | `API_BASE_URL` |
| Interface | PascalCase with 'I' prefix (optional) | `User` or `IUser` |
| Type | PascalCase | `UserRole` |

---

## 2. Component Standards

### Standard Components Library

All pages MUST use these standard components for consistency:

#### Layout Components
```tsx
import {
  PageContainer,    // Page wrapper with padding
  PageHeader,       // Standard page header with breadcrumbs
  ContentCard,      // Content card wrapper
  Section,          // Content section with title
  EmptyState,       // Empty state placeholder
  StatsCard,        // Statistics card
} from '@/components/Standard'
```

#### Table Components
```tsx
import {
  StandardTable,    // Standardized Ant Design table
  TableToolbar,     // Search, filters, actions bar
  ActionButtons,    // Action buttons for table rows
  StatusBadge,      // Status badge renderer
  createColumn,     // Helper for column definition
  ColumnRenderers,  // Common column renderers
} from '@/components/Standard'
```

#### Form Components
```tsx
import {
  StandardForm,     // Form wrapper with consistent styling
  TextField,        // Text input field
  TextAreaField,    // Textarea field
  SelectField,      // Select dropdown
  DateField,        // Date picker
  SwitchField,      // Switch toggle
  UploadField,      // File upload
  FieldGroup,       // Group related fields
} from '@/components/Standard'
```

### Component Template

```tsx
/**
 * Component Name
 * Brief description of what this component does
 */
import React from 'react'

interface ComponentProps {
  // Props interface
  title: string
  onAction?: () => void
}

export const ComponentName: React.FC<ComponentProps> = ({
  title,
  onAction,
}) => {
  return (
    <div className="component-wrapper">
      <h1>{title}</h1>
      {onAction && <button onClick={onAction}>Action</button>}
    </div>
  )
}
```

---

## 3. Page Structure

### Standard Page Pattern

**ALWAYS follow this structure for ALL pages:**

```tsx
import React, { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import {
  PageContainer,
  PageHeader,
  ContentCard,
  StandardTable,
  StandardForm,
} from '@/components/Standard'
import {
  useDataFetch,
  usePagination,
  useModal,
} from '@/hooks/standard'

export default function YourPage() {
  const { t } = useTranslation()

  // ============================================
  // 1. State Management
  // ============================================
  const { data, loading, fetch } = useDataFetch(fetchData)
  const { pagination } = usePagination()
  const { visible, open, close } = useModal()

  // ============================================
  // 2. Effects
  // ============================================
  useEffect(() => {
    fetch()
  }, [])

  // ============================================
  // 3. Event Handlers
  // ============================================
  const handleCreate = () => {
    open()
  }

  const handleEdit = (record) => {
    open(record)
  }

  // ============================================
  // 4. Render
  // ============================================
  return (
    <PageContainer>
      <PageHeader
        title={t('page.title')}
        subtitle={t('page.subtitle')}
        breadcrumbs={[...]}
        extra={<Button onClick={handleCreate}>Create</Button>}
      />

      <ContentCard>
        <StandardTable
          dataSource={data}
          loading={loading}
          pagination={pagination}
        />
      </ContentCard>
    </PageContainer>
  )
}
```

### Page Checklist

Before committing, ensure your page has:

- [ ] Used `PageContainer` wrapper
- [ ] Included `PageHeader` with title and breadcrumbs
- [ ] Used `ContentCard` for main content
- [ ] Implemented proper loading states
- [ ] Added error handling
- [ ] Used i18n for all text
- [ ] Followed consistent event handler naming
- [ ] Added JSDoc comments
- [ ] Proper TypeScript types
- [ ] Responsive design with Tailwind

---

## 4. Styling Guidelines

### Tailwind CSS Standards

#### DO ✅
```tsx
// Use Tailwind utility classes
<div className="flex items-center gap-4 p-6 bg-white dark:bg-gray-800 rounded-lg shadow-sm">

// Use custom utility classes from global.css
<button className="btn-primary">
<div className="card">
<input className="input-base">
<span className="badge-success">

// Combine with Ant Design components
<Card className="shadow-sm mb-4">
```

#### DON'T ❌
```tsx
// Don't use inline styles (except for dynamic values)
<div style={{ padding: '24px', margin: '16px' }}>

// Don't create CSS modules for simple styling
import styles from './MyComponent.module.css'

// Don't mix multiple styling approaches
<div className="flex" style={{ display: 'flex' }}>
```

### Color System

Use the predefined color system:

```tsx
// Primary colors
bg-primary-500, text-primary-600, border-primary-400

// Status colors
bg-success, text-success-dark, border-success-light
bg-error, text-error-dark, border-error-light
bg-warning, text-warning-dark, border-warning-light
bg-info, text-info-dark, border-info-light

// Neutral colors
bg-gray-50, bg-gray-100, bg-gray-200, ...
text-gray-600, text-gray-700, text-gray-800
```

### Responsive Design

Always consider mobile, tablet, and desktop:

```tsx
<div className="
  grid 
  grid-cols-1         // Mobile: 1 column
  md:grid-cols-2      // Tablet: 2 columns
  lg:grid-cols-3      // Desktop: 3 columns
  gap-4 
  p-4 
  md:p-6              // More padding on larger screens
">
```

---

## 5. State Management

### Standard Hooks Pattern

**ALWAYS use these standard hooks:**

```tsx
// Data fetching
const { data, loading, fetch, refetch } = useDataFetch(fetchFn)

// Form submission
const { submit, loading } = useFormSubmit(submitFn, {
  onSuccess: () => {},
  successMessage: 'Success!',
})

// Delete operations
const { deleteItem, loading } = useDelete(deleteFn, {
  onSuccess: () => {},
})

// Pagination
const { current, pageSize, total, pagination } = usePagination(10)

// Search & Filters
const { filters, updateFilter, resetFilters } = useSearch({
  search: '',
  status: '',
})

// Modal
const { visible, data, open, close } = useModal()

// Table selection
const { selectedRowKeys, rowSelection, clearSelection } = useTableSelection()

// File upload
const { upload, uploading, progress } = useUpload(uploadFn)
```

### Zustand for Global State

```tsx
// store/userStore.ts
import { create } from 'zustand'

interface UserStore {
  user: User | null
  setUser: (user: User) => void
  logout: () => void
}

export const useUserStore = create<UserStore>((set) => ({
  user: null,
  setUser: (user) => set({ user }),
  logout: () => set({ user: null }),
}))

// Usage in component
const { user, setUser } = useUserStore()
```

---

## 6. API Integration

### API Client Pattern

```typescript
// api/modules/users.ts
import apiClient from '../client'

export interface User {
  id: string
  name: string
  email: string
}

export const userApi = {
  list: async (params?: any): Promise<{ data: User[]; total: number }> => {
    return apiClient.get('/users', { params })
  },

  get: async (id: string): Promise<User> => {
    return apiClient.get(`/users/${id}`)
  },

  create: async (data: Partial<User>): Promise<User> => {
    return apiClient.post('/users', data)
  },

  update: async (id: string, data: Partial<User>): Promise<User> => {
    return apiClient.put(`/users/${id}`, data)
  },

  delete: async (id: string): Promise<void> => {
    return apiClient.delete(`/users/${id}`)
  },
}
```

### Usage in Components

```tsx
import { userApi } from '@/api/modules/users'
import { useDataFetch } from '@/hooks/standard'

const { data, loading, fetch } = useDataFetch(() => userApi.list())
```

---

## 7. Error Handling

### Error Boundary

**All pages are automatically wrapped in ErrorBoundary (in main.tsx)**

For specific sections:

```tsx
import { ErrorBoundary } from '@/components/Standard'

<ErrorBoundary fallback={<CustomErrorFallback />}>
  <RiskyComponent />
</ErrorBoundary>
```

### Error Messages

```tsx
import { message } from 'antd'
import { useTranslation } from 'react-i18next'

const { t } = useTranslation()

// Success
message.success(t('common.success'))

// Error
message.error(t('errors.networkError'))

// Warning
message.warning(t('common.warning'))

// Info
message.info(t('common.info'))
```

---

## 8. Testing Standards

### Unit Test Template

```tsx
// YourComponent.test.tsx
import { render, screen, fireEvent } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import YourComponent from './YourComponent'

describe('YourComponent', () => {
  it('renders correctly', () => {
    render(<YourComponent title="Test" />)
    expect(screen.getByText('Test')).toBeInTheDocument()
  })

  it('handles click events', () => {
    const handleClick = vi.fn()
    render(<YourComponent onClick={handleClick} />)
    
    fireEvent.click(screen.getByRole('button'))
    expect(handleClick).toHaveBeenCalled()
  })
})
```

### Run Tests

```bash
npm run test              # Run tests
npm run test:ui           # Run with UI
npm run test:coverage     # With coverage report
```

---

## 9. Code Examples

### Example 1: Simple List Page

```tsx
import React, { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from 'antd'
import { PlusOutlined } from '@ant-design/icons'
import { PageContainer, PageHeader, ContentCard, StandardTable } from '@/components/Standard'
import { useDataFetch, usePagination } from '@/hooks/standard'
import { userApi } from '@/api/modules/users'

export default function UserList() {
  const { t } = useTranslation()
  const { data, loading, fetch } = useDataFetch(() => userApi.list())
  const { pagination } = usePagination()

  useEffect(() => {
    fetch()
  }, [])

  const columns = [
    { title: t('user.name'), dataIndex: 'name', key: 'name' },
    { title: t('user.email'), dataIndex: 'email', key: 'email' },
  ]

  return (
    <PageContainer>
      <PageHeader
        title={t('menu.users')}
        extra={<Button type="primary" icon={<PlusOutlined />}>Create</Button>}
      />
      <ContentCard>
        <StandardTable
          columns={columns}
          dataSource={data}
          loading={loading}
          pagination={pagination}
        />
      </ContentCard>
    </PageContainer>
  )
}
```

### Example 2: Form Page

```tsx
import React from 'react'
import { Form } from 'antd'
import { useTranslation } from 'react-i18next'
import { PageContainer, PageHeader, ContentCard } from '@/components/Standard'
import { StandardForm, TextField, SelectField } from '@/components/Standard'
import { useFormSubmit } from '@/hooks/standard'
import { userApi } from '@/api/modules/users'

export default function UserCreate() {
  const { t } = useTranslation()
  const [form] = Form.useForm()

  const { submit, loading } = useFormSubmit(userApi.create, {
    onSuccess: () => {
      form.resetFields()
    },
  })

  return (
    <PageContainer>
      <PageHeader title={t('user.create')} />
      <ContentCard>
        <StandardForm form={form} onFinish={submit} loading={loading}>
          <TextField name="name" label={t('user.name')} required />
          <TextField name="email" label={t('user.email')} required />
          <SelectField
            name="role"
            label={t('user.role')}
            options={[
              { label: 'Admin', value: 'admin' },
              { label: 'User', value: 'user' },
            ]}
          />
        </StandardForm>
      </ContentCard>
    </PageContainer>
  )
}
```

---

## ✅ Pre-Commit Checklist

Before committing your code, verify:

- [ ] Follows standard page structure pattern
- [ ] Uses standard components from `@/components/Standard`
- [ ] Uses standard hooks from `@/hooks/standard`
- [ ] All text is internationalized (uses `t()`)
- [ ] TypeScript types are defined
- [ ] No console.log statements (use logging utility)
- [ ] Tailwind CSS classes are used
- [ ] Responsive design implemented
- [ ] Loading states handled
- [ ] Error states handled
- [ ] JSDoc comments added
- [ ] Tests written (if applicable)
- [ ] Runs without errors: `npm run dev`
- [ ] Passes linting: `npm run lint`
- [ ] Formatted: `npm run format`

---

## 🎯 Quick Reference

### Import Standard Components
```tsx
import {
  PageContainer,
  PageHeader,
  ContentCard,
  StandardTable,
  StandardForm,
  TextField,
} from '@/components/Standard'
```

### Import Standard Hooks
```tsx
import {
  useDataFetch,
  useFormSubmit,
  usePagination,
  useModal,
} from '@/hooks/standard'
```

### Copy Template
```bash
cp src/pages/Templates/StandardPageTemplate.tsx src/pages/YourFeature/YourPage.tsx
```

---

**Questions?** See `frontend/src/pages/Templates/StandardPageTemplate.tsx` for a complete example.

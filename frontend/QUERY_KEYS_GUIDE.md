# Query Key 与数据失效规则文档

本文档说明如何使用 React Query (TanStack Query) 来管理服务端数据状态，包括缓存策略、失效规则和最佳实践。

## Query Key 设计规范

### 目的

Query Key 是 React Query 用来唯一标识和管理缓存的关键。良好的 Query Key 设计能够：
- 避免不必要的重复请求
- 精确控制缓存失效范围
- 简化数据同步逻辑

### 命名规范

使用**分层结构**的数组形式，按优先级从高到低：

```typescript
[domain, resource, ...filters]

// 示例：
['chat', 'sessions']                    // 所有聊天会话
['chat', 'sessions', userId]            // 特定用户的会话
['chat', 'messages', sessionId]         // 特定会话的消息
['document', 'list', { page: 1, size: 20 }]  // 分页文档列表
['admin', 'users', { role: 'admin', page: 1 }]  // 管理员用户列表
```

### 设计原则

1. **第一层：domain（业务域）**
   - `chat` - 聊天模块
   - `document` - 文档模块
   - `agent` - Agent 模块
   - `admin` - 管理后台

2. **第二层：resource（资源）**
   - `sessions` - 会话
   - `messages` - 消息
   - `list` - 列表
   - `detail` - 详情

3. **第三层+：filters/params（参数）**
   - 用户 ID、会话 ID 等
   - 分页参数 `{ page, pageSize }`
   - 搜索参数 `{ search, status }`

### 现有 Query Keys

以下是系统中现有的 Query Key 定义：

```typescript
// Chat 模块
export const chatKeys = {
  all: ['chat'] as const,
  sessions: () => [...chatKeys.all, 'sessions'] as const,
  sessionsByUser: (userId: string) => 
    [...chatKeys.sessions(), userId] as const,
  messages: () => [...chatKeys.all, 'messages'] as const,
  messagesBySession: (sessionId: string) => 
    [...chatKeys.messages(), sessionId] as const,
}

// Document 模块
export const documentKeys = {
  all: ['document'] as const,
  lists: () => [...documentKeys.all, 'list'] as const,
  list: (page: number, pageSize: number) =>
    [...documentKeys.lists(), { page, pageSize }] as const,
  details: () => [...documentKeys.all, 'detail'] as const,
  detail: (id: string) => [...documentKeys.details(), id] as const,
}

// Admin 模块
export const adminKeys = {
  all: ['admin'] as const,
  users: () => [...adminKeys.all, 'users'] as const,
  userList: (page: number, pageSize: number, filters?: any) =>
    [...adminKeys.users(), { page, pageSize, ...filters }] as const,
  roles: () => [...adminKeys.all, 'roles'] as const,
  roleList: (page: number, pageSize: number) =>
    [...adminKeys.roles(), { page, pageSize }] as const,
  permissions: () => [...adminKeys.all, 'permissions'] as const,
}
```

## 缓存配置

### 默认配置

```typescript
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5,      // 5 分钟内，缓存数据被视为 fresh（不重新获取）
      gcTime: 1000 * 60 * 10,        // 10 分钟内，如果 query 未使用，缓存会被垃圾回收
      retry: 1,                       // 失败重试 1 次
      refetchOnWindowFocus: false,    // 窗口获得焦点时不自动重新获取
    },
  },
})
```

### 按模块自定义配置

| 模块 | staleTime | gcTime | 场景 |
|------|-----------|--------|------|
| Chat | 0 | 5m | 实时消息，需要频繁更新 |
| Document | 5m | 10m | 文档列表，更新频率较低 |
| Admin | 10m | 15m | 用户/角色数据，稳定 |
| Agent | 0 | 2m | 一次性执行任务，不需要缓存 |

### 使用自定义配置

```typescript
// Chat hook - 消息实时更新
const { data: messages } = useQuery({
  queryKey: chatKeys.messagesBySession(sessionId),
  queryFn: () => chatApi.getMessages(sessionId),
  staleTime: 0,  // 始终重新获取（流式对话）
})

// Document hook - 偶尔更新
const { data: documents } = useQuery({
  queryKey: documentKeys.list(page, pageSize),
  queryFn: () => documentApi.list(page, pageSize),
  staleTime: 1000 * 60 * 5,  // 5 分钟内使用缓存
})

// Admin hook - 定期更新
const { data: users } = useQuery({
  queryKey: adminKeys.userList(page, pageSize, filters),
  queryFn: () => adminApi.listUsers(page, pageSize, filters),
  staleTime: 1000 * 60 * 10,  // 10 分钟内使用缓存
})
```

## 数据失效规则

### 何时使该手动失效缓存

使用 `queryClient.invalidateQueries()` 来手动标记缓存为 stale，强制重新获取。

| 操作 | 失效范围 | 代码 |
|------|--------|------|
| 创建新文档 | 所有文档列表 | `invalidateQueries({ queryKey: documentKeys.lists() })` |
| 修改文档 | 该文档 + 列表 | `invalidateQueries({ queryKey: documentKeys.detail(id) })` 和 `documentKeys.lists()` |
| 删除文档 | 所有文档列表 | `invalidateQueries({ queryKey: documentKeys.lists() })` |
| 发送消息 | 当前会话消息 + 会话列表 | `invalidateQueries({ queryKey: chatKeys.messagesBySession(sessionId) })` + `chatKeys.sessions()` |
| 更新用户 | 用户列表 | `invalidateQueries({ queryKey: adminKeys.users() })` |

### 失效规则匹配

React Query 使用**前缀匹配**来失效相关的 queries：

```typescript
// ❌ 只失效精确匹配的 key
invalidateQueries({ 
  queryKey: ['chat', 'messages', 'session123'] 
})

// ✅ 失效所有相关的 keys（推荐）
invalidateQueries({ 
  queryKey: ['chat', 'messages'],
  exact: false  // 默认行为，前缀匹配
})

// ✅ 失效整个模块
invalidateQueries({ 
  queryKey: ['chat']  // 失效 chat 下的所有 queries
})
```

### 失效策略

**立即失效（推荐用于增删改）**：
```typescript
const createDocument = useMutation({
  mutationFn: (file: File) => documentApi.upload(file),
  onSuccess: async () => {
    // 立即失效文档列表，用户会看到加载状态然后新数据
    await queryClient.invalidateQueries({ 
      queryKey: documentKeys.lists() 
    })
    notification.success('文档已上传')
  },
})
```

**延迟更新（推荐用于 CRUD 列表）**：
```typescript
const updateUser = useMutation({
  mutationFn: (user: User) => adminApi.updateUser(user),
  onSuccess: (updatedUser) => {
    // 立即更新缓存（乐观更新）
    queryClient.setQueryData(
      adminKeys.detail(updatedUser.id), 
      updatedUser
    )
    // 稍后重新验证列表数据
    queryClient.invalidateQueries({ 
      queryKey: adminKeys.users() 
    })
  },
})
```

**静默错误（用于非关键数据）**：
```typescript
// 重新获取失败时不显示错误提示
const { data, isError } = useQuery({
  queryKey: someKey,
  queryFn: fetchData,
  retry: 1,
})

// 与 notification 结合
if (isError && !silentError) {
  notification.error('获取数据失败')
}
```

## Hook 实现示例

### 基础 Query Hook

```typescript
// src/hooks/useDocument.ts
import { useQuery, useMutation } from '@tanstack/react-query'
import { queryClient } from '@/config/queryClient'
import { documentApi } from '@/api/document'
import { documentKeys } from './queryKeys'
import { notification } from '@/services/notification'

export function useDocuments(page: number, pageSize: number) {
  return useQuery({
    queryKey: documentKeys.list(page, pageSize),
    queryFn: () => documentApi.list(page, pageSize),
    staleTime: 1000 * 60 * 5,
  })
}

export function useDocumentDetail(id: string) {
  return useQuery({
    queryKey: documentKeys.detail(id),
    queryFn: () => documentApi.getDetail(id),
    staleTime: 1000 * 60 * 10,
    enabled: !!id,  // 只在有 ID 时执行
  })
}

export function useUploadDocument() {
  return useMutation({
    mutationFn: (file: File) => documentApi.upload(file),
    onSuccess: async (data) => {
      notification.success('文档上传成功')
      await queryClient.invalidateQueries({
        queryKey: documentKeys.lists(),
      })
    },
    onError: (error) => {
      notification.error(`上传失败: ${error.message}`)
    },
  })
}

export function useDeleteDocument() {
  return useMutation({
    mutationFn: (id: string) => documentApi.delete(id),
    onSuccess: async () => {
      notification.success('文档已删除')
      await queryClient.invalidateQueries({
        queryKey: documentKeys.lists(),
      })
    },
    onError: (error) => {
      notification.error(`删除失败: ${error.message}`)
    },
  })
}
```

### 复杂 Hook 示例

```typescript
// 带流式响应的 Chat Hook
export function useSendMessage(ragVersion: 'v0' | 'v1') {
  return useMutation({
    mutationFn: (message: string) =>
      chatApi.sendMessage(message, ragVersion),
    onSuccess: async (response) => {
      // 立即失效会话消息列表（触发重新获取）
      await queryClient.invalidateQueries({
        queryKey: chatKeys.messagesBySession(currentSessionId),
      })
      // 更新会话列表（修改最后消息时间）
      await queryClient.invalidateQueries({
        queryKey: chatKeys.sessions(),
      })
    },
    onError: (error) => {
      notification.error(getErrorMessage(error))
    },
  })
}

// 分页 + 搜索 + 过滤的复杂 Hook
export function useUserList(
  page: number,
  pageSize: number,
  filters?: { search?: string; role?: string }
) {
  return useQuery({
    queryKey: adminKeys.userList(page, pageSize, filters),
    queryFn: () => adminApi.listUsers(page, pageSize, filters),
    staleTime: 1000 * 60 * 10,
    // 搜索条件变化时，重置分页
    keepPreviousData: true,  // 切页时保持旧数据，避免闪烁
  })
}
```

## 最佳实践

### ✅ DO（推荐）

1. **使用分层 Query Key**
   ```typescript
   // ✅ 推荐
   queryKey: ['chat', 'messages', sessionId]
   
   // ❌ 不推荐
   queryKey: [`chat-messages-${sessionId}`]
   ```

2. **在 Mutation 成功时失效缓存**
   ```typescript
   // ✅ 推荐
   useMutation({
     mutationFn: createUser,
     onSuccess: () => {
       queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
     },
   })
   ```

3. **设置合理的 staleTime**
   ```typescript
   // ✅ 根据数据更新频率调整
   const { data } = useQuery({
     queryKey: ['document', 'list'],
     queryFn: fetchDocuments,
     staleTime: 1000 * 60 * 5,  // 5 分钟
   })
   ```

4. **使用 enabled 条件查询**
   ```typescript
   // ✅ 避免不必要的请求
   const { data } = useQuery({
     queryKey: ['user', userId],
     queryFn: () => fetchUser(userId),
     enabled: !!userId,  // userId 为空时不执行查询
   })
   ```

### ❌ DON'T（不推荐）

1. **硬编码字符串 Query Key**
   ```typescript
   // ❌ 不推荐
   queryKey: ['users-list-page-1']
   
   // ✅ 推荐
   queryKey: ['admin', 'users', { page: 1 }]
   ```

2. **过度使用 refetch**
   ```typescript
   // ❌ 不推荐 - 过度刷新
   setInterval(() => {
     queryClient.refetchQueries({ queryKey: ['data'] })
   }, 1000)
   
   // ✅ 推荐 - 设置合理的 staleTime
   useQuery({
     queryKey: ['data'],
     staleTime: 1000 * 60,  // 1 分钟
   })
   ```

3. **忘记处理错误**
   ```typescript
   // ❌ 不推荐 - 没有错误处理
   const { data } = useQuery({
     queryKey: ['users'],
     queryFn: fetchUsers,
   })
   
   // ✅ 推荐 - 显示错误提示
   const { data, isError, error } = useQuery({
     queryKey: ['users'],
     queryFn: fetchUsers,
     onError: (error) => notification.error(error.message),
   })
   ```

4. **在 Query 函数中有副作用**
   ```typescript
   // ❌ 不推荐 - Query 函数应该是纯函数
   queryFn: async () => {
     const data = await fetchUsers()
     notification.success('数据已加载')  // ❌ 副作用
     return data
   }
   
   // ✅ 推荐 - 在 callback 中处理副作用
   const { data } = useQuery({
     queryKey: ['users'],
     queryFn: fetchUsers,
     onSuccess: () => notification.success('数据已加载'),
   })
   ```

## 调试技巧

### 查看缓存状态

```typescript
// 在浏览器控制台查看所有缓存
import { QueryClient } from '@tanstack/react-query'

// 假设 queryClient 已导出
console.log(queryClient.getQueryData(['chat', 'messages', 'session123']))

// 查看所有 queries 的元数据
const state = queryClient.getQueryCache().getAll()
console.table(state)
```

### DevTools

```tsx
// src/main.tsx
import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <Routes />
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  )
}
```

使用快捷键 `Ctrl+Shift+Q` 打开 DevTools，查看实时缓存状态。

---

## 常见问题

**Q: 什么时候应该使用 `keepPreviousData`？**

A: 当进行分页或搜索条件变化时，使用 `keepPreviousData: true` 可以保持前一页的数据显示，避免切页时的白屏闪烁。

```typescript
const { data, isPreviousData } = useQuery({
  queryKey: ['users', page],
  queryFn: () => fetchUsers(page),
  keepPreviousData: true,
})

// 显示"正在加载新页面"的提示
if (isPreviousData) {
  <Alert message="Loading new page..." />
}
```

**Q: 如何实现"拉取刷新"功能？**

A: 使用 `refetch` 方法：

```typescript
const { data, refetch } = useQuery({
  queryKey: ['messages'],
  queryFn: fetchMessages,
})

// 用户拉取刷新
const handleRefresh = async () => {
  await refetch()
}
```

**Q: 为什么我的数据没有更新？**

A: 常见原因：
1. `staleTime` 设置过长 - 缓存还未过期
2. 忘记调用 `invalidateQueries()` - 缓存未被标记为 stale
3. `enabled: false` - 查询被禁用
4. Query Key 不匹配 - 检查 key 是否一致

使用 DevTools 检查缓存状态和 query 元数据。

---

## 延伸阅读

- [React Query 官方文档](https://tanstack.com/query/latest)
- [高级 Query 管理模式](https://tkdodo.eu/blog/practical-react-query)
- [缓存失效策略深入](https://tkdodo.eu/blog/smart-invalidation)

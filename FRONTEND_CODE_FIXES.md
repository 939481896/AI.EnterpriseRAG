# 前端代码修复指南

## 🐛 发现的问题

### 问题1: API路径大小写不匹配
**前端**: `/api/document/` (小写)  
**后端**: `/api/Document/` (大写D)  
**后端**: `/api/Chat/` (大写C)

ASP.NET Core默认路由区分大小写！

### 问题2: 对话历史API不存在
**前端调用**: `/api/chat/sessions` (获取会话列表)  
**后端实际**: `/api/Chat/history` (对话历史)

前端期望的是会话管理（sessions），后端提供的是历史对话（history）

---

## ✅ 修复方案

### 方案A: 修改前端API路径（推荐 - 最小改动）

#### 1. 修复文档API路径

**文件**: `frontend/src/api/document.ts`

```typescript
import apiClient from './client'
import type { Document, DocumentCategory, ApiResponse } from '@/types/document'

export const documentApi = {
  /**
   * Upload document
   */
  upload: async (
    file: File,
    onProgress?: (progress: number) => void
  ): Promise<ApiResponse<Document>> => {
    const formData = new FormData()
    formData.append('file', file)

    // ✅ 修改：Document 大写
    return apiClient.post('/api/Document/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: (progressEvent) => {
        if (progressEvent.total && onProgress) {
          const progress = Math.round((progressEvent.loaded * 100) / progressEvent.total)
          onProgress(progress)
        }
      },
    })
  },

  /**
   * Get user's documents
   */
  getDocuments: async (page = 1, pageSize = 20): Promise<ApiResponse<{
    items: Document[]
    total: number
  }>> => {
    // ✅ 修改：Document 大写
    return apiClient.get('/api/Document/list', {
      params: { page, pageSize },
    })
  },

  /**
   * Delete document
   */
  deleteDocument: async (documentId: string): Promise<ApiResponse> => {
    // ✅ 修改：Document 大写
    return apiClient.delete(`/api/Document/${documentId}`)
  },

  /**
   * Reprocess failed document - 新增
   */
  reprocessDocument: async (documentId: string): Promise<ApiResponse> => {
    return apiClient.post(`/api/Document/${documentId}/reprocess`)
  },

  /**
   * Get document categories
   */
  getCategories: async (): Promise<ApiResponse<DocumentCategory[]>> => {
    // ✅ 修改：Document 大写
    return apiClient.get('/api/Document/categories')
  },

  /**
   * Get document preview URL
   */
  getPreviewUrl: (documentId: string): string => {
    // ✅ 修改：Document 大写
    return `${apiClient.defaults.baseURL}/api/Document/${documentId}/preview`
  },
}
```

#### 2. 修复聊天API - 使用历史接口

**文件**: `frontend/src/api/chat.ts`

```typescript
import apiClient from './client'
import type { ChatRequest, ChatResponse, ApiResponse } from '@/types/chat'

// ✅ 新增：对话历史类型
export interface ChatHistory {
  id: string
  question: string
  answer: string
  createTime: string
  userId: string
}

export const chatApi = {
  /**
   * Send chat message (V0 - basic RAG)
   */
  sendMessage: async (data: ChatRequest): Promise<ApiResponse<ChatResponse>> => {
    // ✅ 修改：Chat 大写
    return apiClient.post('/api/Chat/ask', data)
  },

  /**
   * Send chat message (V1 - enhanced RAG)
   */
  sendMessageV1: async (data: ChatRequest): Promise<ApiResponse<ChatResponse>> => {
    // ✅ 修改：Chat 大写
    return apiClient.post('/api/Chat/ask-v1', data)
  },

  /**
   * ✅ 修改：获取对话历史（替换sessions接口）
   */
  getHistory: async (pageSize = 20): Promise<ApiResponse<ChatHistory[]>> => {
    return apiClient.get('/api/Chat/history', {
      params: { pageSize },
    })
  },

  /**
   * ✅ 修改：删除对话记录（替换deleteSession）
   */
  deleteHistory: async (conversationId: string): Promise<ApiResponse> => {
    return apiClient.delete(`/api/Chat/history/${conversationId}`)
  },

  // ❌ 以下接口后端未实现，暂时保留但不可用
  /**
   * Get user's conversation sessions
   * @deprecated 后端未实现，使用 getHistory 替代
   */
  getSessions: async (userId: string, limit = 20): Promise<ApiResponse<any[]>> => {
    console.warn('getSessions API not implemented, use getHistory instead')
    return Promise.resolve({ success: true, data: [] })
  },

  /**
   * Create new session
   * @deprecated 后端未实现
   */
  createSession: async (data: { userId: string; title?: string }): Promise<ApiResponse<any>> => {
    console.warn('createSession API not implemented')
    return Promise.resolve({ success: true, data: null })
  },

  /**
   * Delete session
   * @deprecated 使用 deleteHistory 替代
   */
  deleteSession: async (sessionId: string): Promise<ApiResponse> => {
    console.warn('Use deleteHistory instead of deleteSession')
    return chatApi.deleteHistory(sessionId)
  },
}
```

#### 3. 更新 useChat Hook

**文件**: `frontend/src/hooks/useChat.ts`

```typescript
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { message } from 'antd'
import { chatApi, ChatHistory } from '@/api/chat'
import { useChatStore } from '@/store/chatStore'
import { useAuthStore } from '@/store/authStore'
import type { ChatRequest } from '@/types/chat'

export function useSendMessage(version: 'v0' | 'v1' = 'v1') {
  const { user } = useAuthStore()
  const { addMessage, setStreaming } = useChatStore()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (question: string) => {
      if (!user) throw new Error('User not authenticated')

      // Add user message immediately
      addMessage({
        id: `user-${Date.now()}`,
        role: 'user',
        content: question,
        timestamp: new Date(),
      })

      const request: ChatRequest = {
        userId: user.account,
        question,
      }

      setStreaming(true)

      const response = version === 'v1'
        ? await chatApi.sendMessageV1(request)
        : await chatApi.sendMessage(request)

      setStreaming(false)

      return response
    },
    onSuccess: (response) => {
      if (response.success && response.data) {
        const { answer, references, costSeconds } = response.data

        addMessage({
          id: `assistant-${Date.now()}`,
          role: 'assistant',
          content: answer,
          references,
          costSeconds,
          timestamp: new Date(),
          isSuccess: true,
        })

        // ✅ 刷新对话历史
        queryClient.invalidateQueries({ queryKey: ['chatHistory'] })
      }
    },
    onError: (error: any) => {
      setStreaming(false)
      message.error(error.response?.data?.message || '发送失败，请重试')
    },
  })
}

/**
 * ✅ 新增：使用对话历史替代sessions
 */
export function useChatHistory(pageSize = 20) {
  const { user } = useAuthStore()

  return useQuery({
    queryKey: ['chatHistory', user?.account, pageSize],
    queryFn: async () => {
      if (!user) return []
      const response = await chatApi.getHistory(pageSize)
      return response.data || []
    },
    enabled: !!user,
  })
}

/**
 * ✅ 新增：删除对话历史
 */
export function useDeleteHistory() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (conversationId: string) => chatApi.deleteHistory(conversationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['chatHistory'] })
      message.success('对话已删除')
    },
    onError: () => {
      message.error('删除失败')
    },
  })
}

/**
 * @deprecated 使用 useChatHistory 替代
 */
export function useSessions() {
  console.warn('useSessions is deprecated, use useChatHistory instead')
  return useChatHistory()
}

/**
 * @deprecated 使用 useDeleteHistory 替代
 */
export function useDeleteSession() {
  console.warn('useDeleteSession is deprecated, use useDeleteHistory instead')
  return useDeleteHistory()
}
```

#### 4. 更新 SessionSidebar 组件

**文件**: `frontend/src/components/Chat/SessionSidebar.tsx`

```typescript
import { useState } from 'react'
import { List, Button, Popconfirm, Empty, Spin, Typography, Space } from 'antd'
import { MessageOutlined, DeleteOutlined, ClockCircleOutlined } from '@ant-design/icons'
import { useChatHistory, useDeleteHistory } from '@/hooks/useChat'
import type { ChatHistory } from '@/api/chat'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import 'dayjs/locale/zh-cn'

dayjs.extend(relativeTime)
dayjs.locale('zh-cn')

const { Text } = Typography

interface SessionSidebarProps {
  loading?: boolean
}

export default function SessionSidebar({ loading: externalLoading }: SessionSidebarProps) {
  const [hoveredId, setHoveredId] = useState<string | null>(null)

  // ✅ 使用对话历史API
  const { data: history = [], isLoading } = useChatHistory(20)
  const deleteHistory = useDeleteHistory()

  const handleDelete = (conversationId: string, e: React.MouseEvent) => {
    e.stopPropagation()
    deleteHistory.mutate(conversationId)
  }

  const loading = isLoading || externalLoading

  if (loading) {
    return (
      <div style={{ padding: '24px', textAlign: 'center' }}>
        <Spin />
      </div>
    )
  }

  if (!history || history.length === 0) {
    return (
      <Empty
        description="暂无对话历史"
        image={Empty.PRESENTED_IMAGE_SIMPLE}
        style={{ marginTop: '40px' }}
      />
    )
  }

  return (
    <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <div style={{ padding: '16px', borderBottom: '1px solid #f0f0f0' }}>
        <Text strong style={{ fontSize: 16 }}>对话历史</Text>
      </div>

      <div style={{ flex: 1, overflowY: 'auto' }}>
        <List
          dataSource={history}
          renderItem={(item: ChatHistory) => (
            <List.Item
              key={item.id}
              onMouseEnter={() => setHoveredId(item.id)}
              onMouseLeave={() => setHoveredId(null)}
              style={{
                padding: '12px 16px',
                cursor: 'pointer',
                borderLeft: '3px solid transparent',
                transition: 'all 0.2s',
              }}
              className="session-item"
            >
              <List.Item.Meta
                avatar={<MessageOutlined style={{ fontSize: 18, color: '#1890ff' }} />}
                title={
                  <div style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                  }}>
                    <Text
                      ellipsis={{ tooltip: item.question }}
                      style={{ flex: 1, marginRight: 8 }}
                    >
                      {item.question}
                    </Text>
                    {hoveredId === item.id && (
                      <Popconfirm
                        title="确定删除这条对话吗？"
                        onConfirm={(e) => handleDelete(item.id, e as any)}
                        okText="删除"
                        cancelText="取消"
                        okType="danger"
                      >
                        <Button
                          type="text"
                          size="small"
                          danger
                          icon={<DeleteOutlined />}
                          onClick={(e) => e.stopPropagation()}
                        />
                      </Popconfirm>
                    )}
                  </div>
                }
                description={
                  <Space size={4} style={{ fontSize: 12, color: '#999' }}>
                    <ClockCircleOutlined />
                    <span>{dayjs(item.createTime).fromNow()}</span>
                  </Space>
                }
              />
            </List.Item>
          )}
        />
      </div>
    </div>
  )
}
```

#### 5. 更新 ChatPage 组件

**文件**: `frontend/src/pages/Chat/ChatPage.tsx`

```typescript
import { useState, useEffect, useRef } from 'react'
import { Layout, Input, Button, Empty, Select, Space, Divider } from 'antd'
import { SendOutlined, RobotOutlined } from '@ant-design/icons'
import { useSendMessage, useChatHistory } from '@/hooks/useChat'
import { useChatStore } from '@/store/chatStore'
import ChatMessage from '@/components/Chat/ChatMessage'
import SessionSidebar from '@/components/Chat/SessionSidebar'
import './ChatPage.css'

const { Content, Sider } = Layout
const { TextArea } = Input

export default function ChatPage() {
  const [inputValue, setInputValue] = useState('')
  const [ragVersion, setRagVersion] = useState<'v0' | 'v1'>('v1')
  const messagesEndRef = useRef<HTMLDivElement>(null)
  
  const { messages, isStreaming } = useChatStore()
  // ✅ 使用对话历史
  const { isLoading: historyLoading } = useChatHistory()
  const sendMessage = useSendMessage(ragVersion)

  // Auto-scroll to bottom when new messages arrive
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const handleSend = async () => {
    if (!inputValue.trim() || isStreaming || sendMessage.isPending) return

    try {
      await sendMessage.mutateAsync(inputValue)
      setInputValue('')
    } catch (error) {
      console.error('Send message error:', error)
    }
  }

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  return (
    <Layout style={{ height: 'calc(100vh - 112px)', background: '#fff' }}>
      <Sider
        width={280}
        theme="light"
        style={{
          borderRight: '1px solid #f0f0f0',
          height: '100%',
          overflow: 'hidden',
        }}
      >
        {/* ✅ 移除sessions prop，组件内部自己获取 */}
        <SessionSidebar loading={historyLoading} />
      </Sider>

      <Layout>
        <Content
          style={{
            display: 'flex',
            flexDirection: 'column',
            height: '100%',
          }}
        >
          {/* Header */}
          <div style={{ padding: '12px 24px', borderBottom: '1px solid #f0f0f0' }}>
            <Space split={<Divider type="vertical" />}>
              <span style={{ fontWeight: 500 }}>智能问答</span>
              <Select
                value={ragVersion}
                onChange={setRagVersion}
                style={{ width: 120 }}
                size="small"
                options={[
                  { value: 'v0', label: 'RAG V0 (基础版)' },
                  { value: 'v1', label: 'RAG V1 (增强版)' },
                ]}
              />
            </Space>
          </div>

          {/* Messages */}
          <div
            className="messages-container"
            style={{
              flex: 1,
              overflowY: 'auto',
              padding: '24px',
            }}
          >
            {messages.length === 0 ? (
              <Empty
                image={<RobotOutlined style={{ fontSize: 64, color: '#1890ff' }} />}
                description="暂无对话记录，开始提问吧！"
                style={{ marginTop: '20%' }}
              />
            ) : (
              <>
                {messages.map((msg) => (
                  <ChatMessage key={msg.id} message={msg} />
                ))}
                <div ref={messagesEndRef} />
              </>
            )}
          </div>

          {/* Input */}
          <div style={{ padding: '16px 24px', borderTop: '1px solid #f0f0f0' }}>
            <Space.Compact style={{ width: '100%' }}>
              <TextArea
                value={inputValue}
                onChange={(e) => setInputValue(e.target.value)}
                onKeyPress={handleKeyPress}
                placeholder="请输入问题... (Shift+Enter换行)"
                autoSize={{ minRows: 1, maxRows: 4 }}
                disabled={isStreaming || sendMessage.isPending}
              />
              <Button
                type="primary"
                icon={<SendOutlined />}
                loading={isStreaming || sendMessage.isPending}
                onClick={handleSend}
                style={{ height: 'auto' }}
              >
                发送
              </Button>
            </Space.Compact>
          </div>
        </Content>
      </Layout>
    </Layout>
  )
}
```

---

## 🔧 修复步骤总结

### 立即修复（必须）
1. ✅ 修改 `frontend/src/api/document.ts` - 所有路径改为大写 `/api/Document/`
2. ✅ 修改 `frontend/src/api/chat.ts` - 所有路径改为大写 `/api/Chat/`，添加 `getHistory` 和 `deleteHistory`
3. ✅ 修改 `frontend/src/hooks/useChat.ts` - 添加 `useChatHistory` 和 `useDeleteHistory`
4. ✅ 修改 `frontend/src/components/Chat/SessionSidebar.tsx` - 使用对话历史API
5. ✅ 修改 `frontend/src/pages/Chat/ChatPage.tsx` - 移除session相关逻辑

### 可选优化
- [ ] 添加文档重新处理功能UI
- [ ] 添加对话历史分页加载
- [ ] 添加对话搜索功能
- [ ] 添加文档预览功能

---

## ⚠️ 方案B: 修改后端路由（不推荐 - 需要重启）

如果你想保持前端不变，可以修改后端使用小写路由：

**文件**: `AI.EnterpriseRAG.WebAPI/Controllers/DocumentController.cs`

```csharp
[Route("api/document")]  // ✅ 改为小写
public class DocumentController : ControllerBase
```

**文件**: `AI.EnterpriseRAG.WebAPI/Controllers/ChatController.cs`

```csharp
[Route("api/chat")]  // ✅ 改为小写
public class ChatController : ControllerBase
```

但这样需要重启应用，而且不符合C#命名惯例。

---

## 📝 测试清单

### 文档功能
- [ ] 上传文档成功
- [ ] 查看文档列表
- [ ] 删除文档成功
- [ ] 删除后列表自动刷新

### 聊天功能
- [ ] 发送消息成功
- [ ] 查看对话历史
- [ ] 删除对话记录
- [ ] 删除后历史列表刷新

---

## 🚀 快速测试

在浏览器控制台执行：

```javascript
// 测试文档API
fetch('/api/Document/list?page=1&pageSize=20', {
  headers: { 'Authorization': 'Bearer ' + localStorage.getItem('token') }
}).then(r => r.json()).then(console.log)

// 测试聊天历史API
fetch('/api/Chat/history?pageSize=20', {
  headers: { 'Authorization': 'Bearer ' + localStorage.getItem('token') }
}).then(r => r.json()).then(console.log)
```

---

## 💡 重要提示

1. **大小写敏感**: ASP.NET Core默认路由区分大小写
2. **API一致性**: 确保前后端API路径完全一致
3. **Token验证**: 所有接口都需要Authorization头
4. **错误处理**: 检查浏览器Console和Network标签查看错误

---

## 📞 问题排查

### 问题: 404 Not Found
**原因**: API路径大小写不匹配
**解决**: 检查前端API路径是否与后端Controller路由一致

### 问题: 401 Unauthorized  
**原因**: Token无效或未提供
**解决**: 检查localStorage中的token是否存在且有效

### 问题: 对话历史为空
**原因**: 数据库无数据或查询条件不匹配
**解决**: 
1. 先发送几条消息
2. 检查数据库 `chat_conversations` 表
3. 确认UserId匹配

---

## ✅ 完成标准

修复完成后，应该能够：
- ✅ 成功上传文档
- ✅ 查看文档列表
- ✅ 删除文档并自动刷新
- ✅ 发送聊天消息
- ✅ 查看对话历史
- ✅ 删除对话记录

全部功能正常即表示修复成功！

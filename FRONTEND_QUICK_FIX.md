# 前端5分钟快速修复清单

## ✅ 只需修改5个文件！

### 文件1: `frontend/src/api/document.ts`

**查找并替换**:
- `/api/document/` → `/api/Document/`

**修改点** (3处):
```typescript
// Line 15: 上传
return apiClient.post('/api/Document/upload', formData, {

// Line 35: 列表
return apiClient.get('/api/Document/list', {

// Line 44: 删除
return apiClient.delete(`/api/Document/${documentId}`)
```

**新增方法**:
```typescript
  /**
   * Reprocess failed document
   */
  reprocessDocument: async (documentId: string): Promise<ApiResponse> => {
    return apiClient.post(`/api/Document/${documentId}/reprocess`)
  },
```

---

### 文件2: `frontend/src/api/chat.ts`

**第1步**: 添加类型定义（文件顶部）
```typescript
export interface ChatHistory {
  id: string
  question: string
  answer: string
  createTime: string
  userId: string
}
```

**第2步**: 修改API路径
```typescript
// Line 9: ask
return apiClient.post('/api/Chat/ask', data)

// Line 16: ask-v1  
return apiClient.post('/api/Chat/ask-v1', data)
```

**第3步**: 替换getSessions和deleteSession
```typescript
  /**
   * 获取对话历史
   */
  getHistory: async (pageSize = 20): Promise<ApiResponse<ChatHistory[]>> => {
    return apiClient.get('/api/Chat/history', {
      params: { pageSize },
    })
  },

  /**
   * 删除对话记录
   */
  deleteHistory: async (conversationId: string): Promise<ApiResponse> => {
    return apiClient.delete(`/api/Chat/history/${conversationId}`)
  },
```

---

### 文件3: `frontend/src/hooks/useChat.ts`

**新增Hooks** (在文件末尾添加):
```typescript
/**
 * 使用对话历史
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
 * 删除对话历史
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
```

**修改useSendMessage** (删除session相关代码):
```typescript
export function useSendMessage(version: 'v0' | 'v1' = 'v1') {
  const { user } = useAuthStore()
  const { addMessage, setStreaming } = useChatStore()  // 移除 currentSessionId 相关
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (question: string) => {
      if (!user) throw new Error('User not authenticated')

      addMessage({
        id: `user-${Date.now()}`,
        role: 'user',
        content: question,
        timestamp: new Date(),
      })

      // 移除session创建代码
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
        queryClient.invalidateQueries({ queryKey: ['chatHistory'] })  // 改为 chatHistory
      }
    },
    onError: (error: any) => {
      setStreaming(false)
      message.error(error.response?.data?.message || '发送失败，请重试')
    },
  })
}
```

---

### 文件4: `frontend/src/components/Chat/SessionSidebar.tsx`

**完整替换文件内容**:
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
            >
              <List.Item.Meta
                avatar={<MessageOutlined style={{ fontSize: 18, color: '#1890ff' }} />}
                title={
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Text ellipsis={{ tooltip: item.question }} style={{ flex: 1, marginRight: 8 }}>
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

---

### 文件5: `frontend/src/pages/Chat/ChatPage.tsx`

**修改import** (Line 4):
```typescript
import { useSendMessage, useChatHistory } from '@/hooks/useChat'
```

**修改hooks调用** (Line 18-19):
```typescript
const { data: history, isLoading: historyLoading } = useChatHistory()
const sendMessage = useSendMessage(ragVersion)
```

**修改SessionSidebar** (Line 56):
```typescript
<SessionSidebar loading={historyLoading} />
```

**删除不需要的代码**:
- 移除 `sessions` 和 `sessionsLoading`
- 移除 `currentSessionId` 和 `setCurrentSessionId`

---

## 🚀 测试步骤

### 1. 重启前端
```bash
npm run dev
```

### 2. 测试功能
- [ ] 上传文档
- [ ] 删除文档
- [ ] 发送消息
- [ ] 查看对话历史
- [ ] 删除对话

### 3. 检查错误
打开浏览器 F12 → Console 和 Network 标签

---

## ⚠️ 常见错误

### 404 Not Found
**原因**: API路径大小写错误  
**解决**: 确认是 `/api/Document/` 和 `/api/Chat/`（大写）

### 401 Unauthorized
**原因**: Token无效  
**解决**: 重新登录

### 对话历史为空
**原因**: 先发送消息才有历史  
**解决**: 发送几条消息后刷新页面

---

## ✅ 完成标准

修复成功的标志：
- ✅ 文档可以上传
- ✅ 文档可以删除
- ✅ 消息可以发送
- ✅ 对话历史可以查看
- ✅ 对话可以删除
- ✅ Console无错误

---

## 📞 需要帮助？

1. 检查 `FRONTEND_CODE_FIXES.md` 详细说明
2. 使用 `api-test.html` 测试后端API
3. 查看浏览器Console错误信息

---

**预计修改时间**: ⏱️ 5-10分钟  
**难度**: 😊 简单  
**风险**: 🛡️ 低（只是修改API调用）

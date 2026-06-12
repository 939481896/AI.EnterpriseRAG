import { useEffect } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { message } from 'antd'
import { chatApi } from '@/api/chat'
import { useChatStore } from '@/store/chatStore'
import { useAuthStore } from '@/store/authStore'
import type { ChatRequest } from '@/types/chat'

export function useSendMessage(version: 'v0' | 'v1' = 'v1') {
  const { user } = useAuthStore()
  const { addMessage, setStreaming, currentSessionId, setCurrentSessionId } = useChatStore()
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

      // Create session if none exists
      let sessionId = currentSessionId
      if (!sessionId) {
        const sessionResponse = await chatApi.createSession({
          title: question.substring(0, 30) + (question.length > 30 ? '...' : ''),
        })
        if (sessionResponse.success && sessionResponse.data) {
          sessionId = sessionResponse.data.id
          setCurrentSessionId(sessionId)
        }
      }

      const request: ChatRequest = {
        userId: user.account,
        question,
        sessionId: sessionId || undefined, // Convert null to undefined
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

        // Invalidate sessions query to refresh list
        queryClient.invalidateQueries({ queryKey: ['sessions'] })

        // ✅ 关键：当前会话发送新消息后，刷新该会话的消息列表
        if (currentSessionId) {
          queryClient.invalidateQueries({ 
            queryKey: ['session-messages', currentSessionId] 
          })
        }
      }
    },
    onError: (error: any) => {
      setStreaming(false)
      message.error(error.response?.data?.message || '发送失败，请重试')
    },
  })
}

export function useSessions() {
  const { user } = useAuthStore()

  return useQuery({
    queryKey: ['sessions'],
    queryFn: async () => {
      const response = await chatApi.getSessions()
      return response.data || []
    },
    enabled: !!user,
    // ✅ 优化：短时间缓存，避免过度请求
    staleTime: 30 * 1000,    // 30秒后标记为过期
    gcTime: 5 * 60 * 1000, // 5分钟后清除缓存 (previously cacheTime)
    refetchOnMount: true,     // 组件挂载时刷新
    refetchOnWindowFocus: true, // 窗口聚焦时刷新
  })
}

export function useDeleteSession() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (sessionId: string) => chatApi.deleteSession(sessionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sessions'] })
      message.success('会话已删除')
    },
    onError: () => {
      message.error('删除失败')
    },
  })
}

export function useSessionMessages(sessionId: string | null) {
  const { setMessages, clearMessages } = useChatStore()

  const query = useQuery({
    queryKey: ['session-messages', sessionId],
    queryFn: async () => {
      if (!sessionId) return null
      const response = await chatApi.getSessionMessages(sessionId)
      if (response.success && response.data) {
        // Transform backend messages to frontend format
        const messages = response.data.messages.map((msg: any) => ({
          id: msg.id,
          role: msg.role,
          content: msg.message,
          timestamp: new Date(msg.timestamp),
        }))
        return { messages, raw: response.data }
      }
      return null
    },
    enabled: !!sessionId,
    // ✅ 优化后的缓存策略 - 充分利用缓存
    staleTime: 5 * 60 * 1000,    // 5分钟内认为数据是新鲜的（历史消息不会变）
    gcTime: 30 * 60 * 1000,   // 30分钟后才清除缓存 (previously cacheTime)
    refetchOnMount: false,       // 组件挂载时不自动刷新（使用缓存）
    refetchOnWindowFocus: false, // 窗口聚焦时不刷新
    // 注意：当前会话发送新消息后，会通过 invalidateQueries 主动刷新
  })

  // ✅ 关键修复：监听查询数据变化，无论来自API还是缓存，都更新store
  useEffect(() => {
    if (query.data?.messages) {
      setMessages(query.data.messages)
    } else if (!sessionId) {
      clearMessages()
    }
  }, [query.data, sessionId, setMessages, clearMessages])

  return query
}

export function useUpdateSessionTitle() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ sessionId, title }: { sessionId: string; title: string }) =>
      chatApi.updateSessionTitle(sessionId, title),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sessions'] })
      message.success('会话标题已更新')
    },
    onError: () => {
      message.error('更新标题失败')
    },
  })
}

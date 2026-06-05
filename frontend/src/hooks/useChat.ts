import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { message } from 'antd'
import { chatApi } from '@/api/chat'
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

        // Invalidate sessions query to refresh list
        queryClient.invalidateQueries({ queryKey: ['sessions'] })
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
    queryKey: ['sessions', user?.account],
    queryFn: async () => {
      if (!user) return []
      const response = await chatApi.getSessions(user.account)
      return response.data || []
    },
    enabled: !!user,
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

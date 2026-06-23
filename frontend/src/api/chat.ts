import apiClient from './client'
import type { ChatRequest, ChatResponse, ConversationSession, Message } from '@/types/chat'
import type { ApiResponse } from '@/types/api'

interface SessionMessageDto {
  id: string
  role: Message['role']
  message: string
  timestamp: string
}

export const chatApi = {
  /**
   * Send chat message (V0 - basic RAG)
   */
  sendMessage: async (data: ChatRequest): Promise<ApiResponse<ChatResponse>> => {
    return apiClient.post('/api/chat/ask', data)
  },

  /**
   * Send chat message (V1 - enhanced RAG with HyDE, Memory, etc.)
   */
  sendMessageV1: async (data: ChatRequest): Promise<ApiResponse<ChatResponse>> => {
    return apiClient.post('/api/chat/ask-v1', data)
  },

  /**
   * Get user's conversation sessions
   */
  getSessions: async (limit = 20): Promise<ApiResponse<ConversationSession[]>> => {
    return apiClient.get('/api/chat/sessions', {
      params: { limit },
    })
  },

  /**
   * Get session messages
   */
  getSessionMessages: async (sessionId: string): Promise<ApiResponse<{
    session: ConversationSession
    messages: SessionMessageDto[]
  }>> => {
    return apiClient.get(`/api/chat/sessions/${sessionId}/messages`)
  },

  /**
   * Create new session
   */
  createSession: async (data: {
    title?: string
  }): Promise<ApiResponse<{ id: string; userId: string; title: string; createdAt: string }>> => {
    return apiClient.post('/api/chat/sessions', data)
  },

  /**
   * Delete session
   */
  deleteSession: async (sessionId: string): Promise<ApiResponse> => {
    return apiClient.delete(`/api/chat/sessions/${sessionId}`)
  },

  /**
   * Update session title
   */
  updateSessionTitle: async (sessionId: string, title: string): Promise<ApiResponse> => {
    return apiClient.patch(`/api/chat/sessions/${sessionId}`, { title })
  },
}

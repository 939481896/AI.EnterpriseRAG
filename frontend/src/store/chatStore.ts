import { create } from 'zustand'
import type { Message } from '@/types/chat'

/**
 * Chat UI state store.
 *
 * Boundary rules:
 * - Server state (sessions/messages from backend) belongs to React Query.
 * - Ephemeral UI state (currentSessionId, streaming flag, optimistic buffer) stays here.
 */
interface ChatState {
  /** Active session selected in sidebar */
  currentSessionId: string | null
  /** Rendered messages for the active session (synced from query data) */
  messages: Message[]
  /** Assistant is currently generating content */
  isStreaming: boolean
  
  setCurrentSessionId: (sessionId: string | null) => void
  setMessages: (messages: Message[]) => void
  addMessage: (message: Message) => void
  updateLastMessage: (content: string) => void
  setStreaming: (isStreaming: boolean) => void
  clearMessages: () => void
}

export const useChatStore = create<ChatState>((set) => ({
  currentSessionId: null,
  messages: [],
  isStreaming: false,

  setCurrentSessionId: (sessionId) =>
    { set({ currentSessionId: sessionId }); },

  setMessages: (messages) =>
    { set({ messages }); },

  addMessage: (message) =>
    { set((state) => ({
      // Optimistic append for immediate UI feedback.
      messages: [...state.messages, message],
    })); },

  updateLastMessage: (content) =>
    { set((state) => {
      const messages = [...state.messages]
      const lastMessage = messages[messages.length - 1]
      // Used by streaming-like scenarios to patch the last assistant message.
      if (lastMessage && lastMessage.role === 'assistant') {
        lastMessage.content = content
      }
      return { messages }
    }); },

  setStreaming: (isStreaming) =>
    { set({ isStreaming }); },

  clearMessages: () =>
    { set({ messages: [] }); },
}))

import { create } from 'zustand'
import type { Message, ConversationSession } from '@/types/chat'

interface ChatState {
  currentSessionId: string | null
  sessions: ConversationSession[]
  messages: Message[]
  isStreaming: boolean
  
  setCurrentSessionId: (sessionId: string | null) => void
  setSessions: (sessions: ConversationSession[]) => void
  setMessages: (messages: Message[]) => void
  addMessage: (message: Message) => void
  updateLastMessage: (content: string) => void
  setStreaming: (isStreaming: boolean) => void
  clearMessages: () => void
}

export const useChatStore = create<ChatState>((set) => ({
  currentSessionId: null,
  sessions: [],
  messages: [],
  isStreaming: false,

  setCurrentSessionId: (sessionId) =>
    set({ currentSessionId: sessionId }),

  setSessions: (sessions) =>
    set({ sessions }),

  setMessages: (messages) =>
    set({ messages }),

  addMessage: (message) =>
    set((state) => ({
      messages: [...state.messages, message],
    })),

  updateLastMessage: (content) =>
    set((state) => {
      const messages = [...state.messages]
      const lastMessage = messages[messages.length - 1]
      if (lastMessage && lastMessage.role === 'assistant') {
        lastMessage.content = content
      }
      return { messages }
    }),

  setStreaming: (isStreaming) =>
    set({ isStreaming }),

  clearMessages: () =>
    set({ messages: [] }),
}))

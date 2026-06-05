export interface Message {
  id: string
  sessionId?: string
  role: 'user' | 'assistant'
  content: string
  references?: string[]
  costSeconds?: number
  timestamp: Date
  isSuccess?: boolean
}

export interface ConversationSession {
  id: string
  userId: string
  title: string
  createdAt: string
  lastInteractionAt: string
  isActive: boolean
  messageCount: number
}

export interface ChatRequest {
  userId: string
  question: string
  sessionId?: string
}

export interface ChatResponse {
  answer: string
  references: string[]
  costSeconds: number
  sessionId?: string
}

export interface StreamingChunk {
  content: string
  done: boolean
}

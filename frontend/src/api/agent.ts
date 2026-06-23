import apiClient from './client'

export interface AgentExecuteRequest {
  input: string
  maxIterations?: number
}

export interface AgentStep {
  id: string
  type: 'thinking' | 'action' | 'observation' | 'final' | 'error'
  content: string
  tool?: string
  args?: Record<string, unknown>
  result?: unknown
  duration?: number
  timestamp: Date
}

interface AgentStepPayload extends Omit<AgentStep, 'timestamp'> {
  timestamp?: string | number
}

interface AgentErrorPayload {
  message?: string
}

export interface IntentRecognitionResult {
  type: string
  confidence: number
  reasoning: string
}

/**
 * Execute Agent task with SSE streaming
 */
export async function executeAgent(
  request: AgentExecuteRequest,
  onStep: (step: AgentStep) => void,
  onIntent?: (intent: IntentRecognitionResult) => void,
  onComplete?: () => void,
  onError?: (error: string) => void
): Promise<void> {
  const response = await fetch('/api/agent/execute', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${localStorage.getItem('token')}`,
    },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    onError?.(`Request failed: ${response.statusText}`)
    return
  }

  const reader = response.body?.getReader()
  const decoder = new TextDecoder()

  if (!reader) {
    onError?.('No response body')
    return
  }

  try {
    let doneReading = false
    while (!doneReading) {
      const { done, value } = await reader.read()
      if (done) {
        doneReading = true
        onComplete?.()
        break
      }

      const chunk = decoder.decode(value)
      const lines = chunk.split('\n\n')

      for (const line of lines) {
        if (!line.trim()) continue

        // Parse SSE format: "event: type\ndata: json"
        const eventMatch = line.match(/event:\s*(\w+)\ndata:\s*(.+)/s)
        if (eventMatch) {
          const [, eventType, dataStr] = eventMatch
          try {
            const data = JSON.parse(dataStr) as unknown

            if (eventType === 'intent') {
              onIntent?.(data as IntentRecognitionResult)
            } else if (eventType === 'step') {
              const stepData = data as AgentStepPayload
              onStep({
                ...stepData,
                timestamp: new Date(stepData.timestamp || Date.now()),
              })
            } else if (eventType === 'error') {
              const errorData = data as AgentErrorPayload
              onError?.(errorData.message || 'Unknown error')
            } else if (eventType === 'complete') {
              onComplete?.()
            }
          } catch (e) {
            console.error('Failed to parse SSE data:', e, dataStr)
          }
        }
      }
    }
  } catch (error) {
    console.error('SSE streaming error:', error)
    onError?.(`Streaming error: ${error}`)
  } finally {
    reader.releaseLock()
  }
}

/**
 * Execute Agent task synchronously (for testing, not streaming)
 */
export async function executeAgentSync(request: AgentExecuteRequest): Promise<unknown> {
  return apiClient.post('/api/agent/execute-sync', request)
}

export const agentApi = {
  executeAgent,
  executeAgentSync,
}

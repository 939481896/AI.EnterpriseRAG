import { useState, useEffect, useRef } from 'react'
import { Layout, Input, Button, Empty, Spin, Select, Space, Divider } from 'antd'
import { SendOutlined, RobotOutlined } from '@ant-design/icons'
import { useSendMessage, useSessions, useSessionMessages } from '@/hooks/useChat'
import { useChatStore } from '@/store/chatStore'
import ChatMessage from '@/components/Chat/ChatMessage'
import SessionSidebar from '@/components/Chat/SessionSidebar'

const { Content, Sider } = Layout
const { TextArea } = Input

export default function ChatPage() {
  const [inputValue, setInputValue] = useState('')
  const [ragVersion, setRagVersion] = useState<'v0' | 'v1'>('v1')
  const messagesEndRef = useRef<HTMLDivElement>(null)

  const { messages, isStreaming, currentSessionId } = useChatStore()
  const { data: sessions, isLoading: sessionsLoading } = useSessions()
  const { isLoading: loadingMessages } = useSessionMessages(currentSessionId)
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
    <Layout className="h-[calc(100vh-112px)] bg-white">
      {/* Sidebar */}
      <Sider
        width={280}
        theme="light"
        className="border-r border-gray-200 h-full overflow-hidden"
      >
        <SessionSidebar sessions={sessions || []} loading={sessionsLoading} />
      </Sider>

      <Layout>
        <Content className="flex flex-col h-full">
          {/* Header */}
          <div className="px-6 py-3 border-b border-gray-200">
            <Space split={<Divider type="vertical" />}>
              <span className="font-medium">智能问答</span>
              <Select
                value={ragVersion}
                onChange={setRagVersion}
                className="w-32"
                size="small"
                options={[
                  { value: 'v0', label: 'RAG V0 (基础版)' },
                  { value: 'v1', label: 'RAG V1 (增强版)' },
                ]}
              />
            </Space>
          </div>

          {/* Messages Container */}
          <div className="flex-1 overflow-y-auto p-6 scroll-smooth">
            {loadingMessages ? (
              <div className="text-center py-16">
                <Spin size="large" tip="加载会话记录中..." />
              </div>
            ) : messages.length === 0 ? (
              <Empty
                image={<RobotOutlined className="text-6xl text-primary" />}
                description="暂无对话记录，开始提问吧！"
                className="mt-[20%]"
              />
            ) : (
              <>
                {messages.map((message) => (
                  <ChatMessage key={message.id} message={message} />
                ))}
                {isStreaming && (
                  <div className="text-center py-5">
                    <Spin tip="AI 正在思考..." />
                  </div>
                )}
                <div ref={messagesEndRef} />
              </>
            )}
          </div>

          {/* Input Area */}
          <div className="px-6 py-4 border-t border-gray-200 bg-gray-50">
            <div className="flex gap-2 items-end">
              <TextArea
                value={inputValue}
                onChange={(e) => setInputValue(e.target.value)}
                onKeyDown={handleKeyPress}
                placeholder="输入问题... (Shift+Enter 换行，Enter 发送)"
                autoSize={{ minRows: 1, maxRows: 4 }}
                className="flex-1"
                disabled={isStreaming || sendMessage.isPending}
              />
              <Button
                type="primary"
                icon={<SendOutlined />}
                onClick={handleSend}
                loading={isStreaming || sendMessage.isPending}
                disabled={!inputValue.trim()}
                size="large"
              >
                发送
              </Button>
            </div>
            <div className="mt-2 text-gray-500 text-xs">
              💡 提示：本系统基于文档知识库回答问题，回答仅供参考
            </div>
          </div>
        </Content>
      </Layout>
    </Layout>
  )
}

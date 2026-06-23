import { useState, useEffect, useRef } from 'react'
import { Layout, Input, Button, Empty, Spin, Select, Space, Divider } from 'antd'
import { SendOutlined, RobotOutlined } from '@ant-design/icons'
import { useSendMessage, useSessions, useSessionMessages } from '@/hooks/useChat'
import { useChatStore } from '@/store/chatStore'
import ChatMessage from '@/components/Chat/ChatMessage'
import SessionSidebar from '@/components/Chat/SessionSidebar'
import { uiText } from '@/config/uiText'

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
        <SessionSidebar sessions={sessions || []} loading={sessionsLoading} />
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
              <span style={{ fontWeight: 500 }}>{uiText.chat.pageTitle}</span>
              <Select
                value={ragVersion}
                onChange={setRagVersion}
                style={{ width: 120 }}
                size="small"
                options={[
                  { value: 'v0', label: uiText.chat.ragV0Label },
                  { value: 'v1', label: uiText.chat.ragV1Label },
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
            {loadingMessages ? (
              <div style={{ textAlign: 'center', padding: 60 }}>
                <Spin size="large" tip={uiText.chat.loadingHistory} />
              </div>
            ) : messages.length === 0 ? (
              <Empty
                image={<RobotOutlined style={{ fontSize: 64, color: '#1890ff' }} />}
                description={uiText.chat.emptyDescription}
                style={{ marginTop: '20%' }}
              />
            ) : (
              <>
                {messages.map((message) => (
                  <ChatMessage key={message.id} message={message} />
                ))}
                {isStreaming && (
                  <div style={{ textAlign: 'center', padding: 20 }}>
                    <Spin tip={uiText.chat.generating} />
                  </div>
                )}
                <div ref={messagesEndRef} />
              </>
            )}
          </div>

          {/* Input */}
          <div
            style={{
              padding: '16px 24px',
              borderTop: '1px solid #f0f0f0',
              background: '#fafafa',
            }}
          >
            <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end' }}>
              <TextArea
                value={inputValue}
                onChange={(e) => { setInputValue(e.target.value); }}
                onKeyDown={handleKeyPress}
                placeholder={uiText.chat.inputPlaceholder}
                autoSize={{ minRows: 1, maxRows: 4 }}
                style={{ flex: 1 }}
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
                {uiText.chat.sending}
              </Button>
            </div>
            <div style={{ marginTop: 8, color: '#8c8c8c', fontSize: 12 }}>
              💡 {uiText.chat.hint}
            </div>
          </div>
        </Content>
      </Layout>
    </Layout>
  )
}

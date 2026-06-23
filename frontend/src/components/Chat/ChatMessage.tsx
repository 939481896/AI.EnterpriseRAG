import { Avatar, Button, Space, Tag, Typography } from 'antd'
import { UserOutlined, RobotOutlined, CopyOutlined, LikeOutlined, DislikeOutlined, ReloadOutlined } from '@ant-design/icons'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import copy from 'copy-to-clipboard'
import type { Message } from '@/types/chat'
import { notification } from '@/services/notification'
import { uiText, formatText } from '@/config/uiText'
import './ChatMessage.css'

const { Text } = Typography

interface ChatMessageProps {
  message: Message
  onRegenerate?: (messageId: string) => void
  onFeedback?: (messageId: string, feedback: 'like' | 'dislike') => void
}

export default function ChatMessage({ message, onRegenerate, onFeedback }: ChatMessageProps) {
  const isUser = message.role === 'user'

  const handleCopy = () => {
    copy(message.content)
    notification.success(uiText.chat.copySuccess)
  }

  const handleCitationClick = (index: number) => {
    notification.info(formatText(uiText.chat.citationPreview, { index: index + 1 }))
    // TODO: Open citation preview modal
  }

  return (
    <div className={`chat-message ${isUser ? 'chat-message-user' : 'chat-message-assistant'}`}>
      <Avatar
        className="message-avatar"
        size={36}
        icon={isUser ? <UserOutlined /> : <RobotOutlined />}
        style={{
          backgroundColor: isUser ? '#1890ff' : '#52c41a',
        }}
      />

      <div className="message-content-wrapper">
        <div className={`message-bubble ${isUser ? 'user-bubble' : 'assistant-bubble'}`}>
          {isUser ? (
            <p className="user-message-text">{message.content}</p>
          ) : (
            <div className="assistant-message-content">
              <ReactMarkdown
                className="markdown-body"
                remarkPlugins={[remarkGfm]}
                components={{
                  code({ className, children, ...props }) {
                    const isInline = !className
                    return isInline ? (
                      <code className={className} {...props}>
                        {children}
                      </code>
                    ) : (
                      <pre className="code-block">
                        <code className={className} {...props}>
                          {children}
                        </code>
                      </pre>
                    )
                  },
                }}
              >
                {message.content}
              </ReactMarkdown>

              {/* Citations */}
              {message.references && message.references.length > 0 && (
                <div className="message-citations">
                  <Text type="secondary" style={{ fontSize: 12, marginBottom: 8, display: 'block' }}>
                    📚 {uiText.chat.references}
                  </Text>
                  <Space wrap size={[4, 8]}>
                    {message.references.map((ref, index) => (
                      <Tag
                        key={index}
                        color="blue"
                        style={{ cursor: 'pointer', margin: 0 }}
                        onClick={() => { handleCitationClick(index); }}
                      >
                        [{index + 1}] {ref.substring(0, 30)}...
                      </Tag>
                    ))}
                  </Space>
                </div>
              )}

              {/* Actions */}
              {!isUser && (
                <div className="message-actions">
                  <Space size="small">
                    <Button
                      type="text"
                      size="small"
                      icon={<CopyOutlined />}
                      onClick={handleCopy}
                    />
                    <Button
                      type="text"
                      size="small"
                      icon={<LikeOutlined />}
                      onClick={() => onFeedback?.(message.id, 'like')}
                    />
                    <Button
                      type="text"
                      size="small"
                      icon={<DislikeOutlined />}
                      onClick={() => onFeedback?.(message.id, 'dislike')}
                    />
                    <Button
                      type="text"
                      size="small"
                      icon={<ReloadOutlined />}
                      onClick={() => onRegenerate?.(message.id)}
                    >
                      {uiText.chat.regenerate}
                    </Button>
                  </Space>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Timestamp and cost */}
        <div className="message-meta">
          <Text type="secondary" style={{ fontSize: 12 }}>
            {message.timestamp.toLocaleTimeString('zh-CN', {
              hour: '2-digit',
              minute: '2-digit',
            })}
            {message.costSeconds && (
              <> • {uiText.chat.costSeconds} {message.costSeconds.toFixed(2)}s</>
            )}
          </Text>
        </div>
      </div>
    </div>
  )
}

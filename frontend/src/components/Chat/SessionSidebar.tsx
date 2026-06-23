import { useState } from 'react'
import { Button, Input, Empty, Dropdown, Typography, Spin, Modal } from 'antd'
import {
  PlusOutlined,
  MessageOutlined,
  EllipsisOutlined,
  DeleteOutlined,
  EditOutlined,
  CheckOutlined,
  CloseOutlined,
  ExclamationCircleOutlined,
} from '@ant-design/icons'
import { useChatStore } from '@/store/chatStore'
import { useDeleteSession, useUpdateSessionTitle } from '@/hooks/useChat'
import type { ConversationSession } from '@/types/chat'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import 'dayjs/locale/zh-cn'
import { formatText, uiText } from '@/config/uiText'
import './SessionSidebar.css'

dayjs.extend(relativeTime)

const { Text } = Typography
const { confirm } = Modal

interface SessionSidebarProps {
  sessions: ConversationSession[]
  loading?: boolean
}

export default function SessionSidebar({ sessions, loading }: SessionSidebarProps) {
  const { currentSessionId, setCurrentSessionId, clearMessages } = useChatStore()
  const deleteSession = useDeleteSession()
  const updateSessionTitle = useUpdateSessionTitle()
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editTitle, setEditTitle] = useState('')

  const handleNewChat = () => {
    setCurrentSessionId(null)
    clearMessages()
  }

  const handleSelectSession = (sessionId: string) => {
    if (sessionId === currentSessionId) return
    setCurrentSessionId(sessionId)
    // Messages will be loaded by useSessionMessages hook in ChatPage
  }

  const handleStartEdit = (session: ConversationSession, e: { stopPropagation: () => void }) => {
    e.stopPropagation()
    setEditingId(session.id)
    setEditTitle(session.title)
  }

  const handleSaveEdit = async () => {
    if (!editingId || !editTitle.trim()) {
      setEditingId(null)
      return
    }

    try {
      await updateSessionTitle.mutateAsync({
        sessionId: editingId,
        title: editTitle.trim(),
      })
      setEditingId(null)
      setEditTitle('')
    } catch (error) {
      console.error('Failed to update title:', error)
    }
  }

  const handleCancelEdit = () => {
    setEditingId(null)
    setEditTitle('')
  }

  const handleDelete = (
    sessionId: string,
    sessionTitle: string,
    e: { stopPropagation: () => void }
  ) => {
    e.stopPropagation()

    confirm({
      title: uiText.session.deleteConfirmTitle,
      icon: <ExclamationCircleOutlined />,
      content: formatText(uiText.session.deleteConfirmContent, { title: sessionTitle }),
      okText: uiText.common.delete,
      okType: 'danger',
      cancelText: uiText.common.cancel,
      onOk() {
        deleteSession.mutate(sessionId)
        // If deleting current session, clear it
        if (sessionId === currentSessionId) {
          setCurrentSessionId(null)
          clearMessages()
        }
      },
    })
  }

  const groupedSessions = {
    today: [] as ConversationSession[],
    yesterday: [] as ConversationSession[],
    thisWeek: [] as ConversationSession[],
    older: [] as ConversationSession[],
  }

  const now = dayjs()
  sessions.forEach((session) => {
    const sessionDate = dayjs(session.lastInteractionAt)
    const diffDays = now.diff(sessionDate, 'day')

    if (diffDays === 0) {
      groupedSessions.today.push(session)
    } else if (diffDays === 1) {
      groupedSessions.yesterday.push(session)
    } else if (diffDays < 7) {
      groupedSessions.thisWeek.push(session)
    } else {
      groupedSessions.older.push(session)
    }
  })

  const renderSessionItem = (session: ConversationSession) => {
    const isEditing = editingId === session.id
    const isActive = currentSessionId === session.id

    return (
      <div
        key={session.id}
        className={`session-item ${isActive ? 'session-item-active' : ''}`}
        onClick={() => !isEditing && handleSelectSession(session.id)}
      >
        <div className="session-item-content">
          <MessageOutlined className="session-icon" />
          {isEditing ? (
            <Input
              size="small"
              value={editTitle}
              onChange={(e) => { setEditTitle(e.target.value); }}
              onPressEnter={handleSaveEdit}
              onClick={(e) => { e.stopPropagation(); }}
              autoFocus
            />
          ) : (
            <div className="session-title">{session.title}</div>
          )}
        </div>

        {isEditing ? (
          <div className="session-actions" onClick={(e) => { e.stopPropagation(); }}>
            <CheckOutlined onClick={handleSaveEdit} style={{ color: '#52c41a' }} />
            <CloseOutlined onClick={handleCancelEdit} style={{ color: '#ff4d4f' }} />
          </div>
        ) : (
          <Dropdown
            menu={{
              items: [
                {
                  key: 'edit',
                  icon: <EditOutlined />,
                  label: uiText.session.rename,
                  onClick: (info) => {
                    info.domEvent.stopPropagation()
                    handleStartEdit(session, info.domEvent)
                  },
                },
                {
                  key: 'delete',
                  icon: <DeleteOutlined />,
                  label: uiText.common.delete,
                  danger: true,
                  onClick: (info) => {
                    info.domEvent.stopPropagation()
                    handleDelete(session.id, session.title, info.domEvent)
                  },
                },
              ],
            }}
            trigger={['click']}
          >
            <EllipsisOutlined
              className="session-menu"
              onClick={(e) => { e.stopPropagation(); }}
            />
          </Dropdown>
        )}
      </div>
    )
  }

  if (loading) {
    return (
      <div className="session-sidebar">
        <div className="session-header">
          <Button type="primary" icon={<PlusOutlined />} block disabled>
            {uiText.session.newChat}
          </Button>
        </div>
        <div style={{ textAlign: 'center', padding: 40 }}>
          <Spin />
        </div>
      </div>
    )
  }

  return (
    <div className="session-sidebar">
      <div className="session-header">
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={handleNewChat}
          block
        >
          {uiText.session.newChat}
        </Button>
      </div>

      <div className="session-list">
        {sessions.length === 0 ? (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description={uiText.session.empty}
            style={{ marginTop: 60 }}
          />
        ) : (
          <>
            {groupedSessions.today.length > 0 && (
              <div className="session-group">
                <Text type="secondary" className="session-group-title">
                  {uiText.session.today}
                </Text>
                {groupedSessions.today.map(renderSessionItem)}
              </div>
            )}

            {groupedSessions.yesterday.length > 0 && (
              <div className="session-group">
                <Text type="secondary" className="session-group-title">
                  {uiText.session.yesterday}
                </Text>
                {groupedSessions.yesterday.map(renderSessionItem)}
              </div>
            )}

            {groupedSessions.thisWeek.length > 0 && (
              <div className="session-group">
                <Text type="secondary" className="session-group-title">
                  {uiText.session.thisWeek}
                </Text>
                {groupedSessions.thisWeek.map(renderSessionItem)}
              </div>
            )}

            {groupedSessions.older.length > 0 && (
              <div className="session-group">
                <Text type="secondary" className="session-group-title">
                  {uiText.session.older}
                </Text>
                {groupedSessions.older.map(renderSessionItem)}
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}

import { useState } from 'react'
import {
  Card,
  Input,
  Button,
  Timeline,
  Tag,
  Typography,
  Space,
  Alert,
  Empty,
} from 'antd'
import {
  ThunderboltOutlined,
  ClockCircleOutlined,
  CheckCircleOutlined,
  CodeOutlined,
  DatabaseOutlined,
  FileSearchOutlined,
} from '@ant-design/icons'
import ReactMarkdown from 'react-markdown'
import { agentApi, type AgentStep, type IntentRecognitionResult } from '@/api/agent'
import { uiText, formatText } from '@/config/uiText'
import { notification } from '@/services/notification'
import { getErrorMessage } from '@/types/error'

const { Title, Text, Paragraph } = Typography
const { TextArea } = Input

export default function AgentWorkspace() {
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(false)
  const [steps, setSteps] = useState<AgentStep[]>([])
  const [intent, setIntent] = useState<IntentRecognitionResult | null>(null)

  const handleExecute = async () => {
    if (!input.trim()) return

    setLoading(true)
    setSteps([])
    setIntent(null)

    try {
      await agentApi.executeAgent(
        {
          input: input.trim(),
          maxIterations: 10,
        },
        // onStep callback
        (step) => {
          setSteps((prev) => [...prev, step])
        },
        // onIntent callback
        (intentResult) => {
          setIntent(intentResult)
        },
        // onComplete callback
        () => {
          setLoading(false)
          notification.success(uiText.agent.taskCompleted)
        },
        // onError callback
        (error) => {
          setLoading(false)
          notification.error(`${uiText.agent.executeFailed}: ${error}`)
        }
      )
    } catch (error: unknown) {
      setLoading(false)
      notification.error(getErrorMessage(error) || uiText.agent.executeFailed)
    }
  }

  const getStepIcon = (type: string) => {
    switch (type) {
      case 'thinking':
        return <ThunderboltOutlined style={{ color: '#1890ff' }} />
      case 'action':
        return <CodeOutlined style={{ color: '#52c41a' }} />
      case 'observation':
        return <DatabaseOutlined style={{ color: '#faad14' }} />
      case 'final':
        return <CheckCircleOutlined style={{ color: '#52c41a' }} />
      default:
        return <ClockCircleOutlined />
    }
  }

  const getStepColor = (type: string) => {
    switch (type) {
      case 'thinking':
        return 'blue'
      case 'action':
        return 'green'
      case 'observation':
        return 'orange'
      case 'final':
        return 'success'
      default:
        return 'default'
    }
  }

  const totalDuration = steps.reduce((sum, step) => sum + (step.duration || 0), 0)

  return (
    <div className="agent-workspace">
      <Title level={3}>🤖 {uiText.agent.pageTitle}</Title>
      <Paragraph type="secondary">
        {uiText.agent.pageDescription}
      </Paragraph>

      <Card className="input-card">
        <TextArea
          placeholder={uiText.agent.inputPlaceholder}
          value={input}
          onChange={(e) => { setInput(e.target.value); }}
          autoSize={{ minRows: 3, maxRows: 6 }}
          disabled={loading}
        />
        <div style={{ marginTop: 16, textAlign: 'right' }}>
          <Button
            type="primary"
            size="large"
            icon={<ThunderboltOutlined />}
            onClick={handleExecute}
            loading={loading}
            disabled={!input.trim()}
          >
            {uiText.agent.execute}
          </Button>
        </div>
      </Card>

      {intent && (
        <Alert
          message={uiText.agent.intentTitle}
          description={
            <Space direction="vertical" style={{ width: '100%' }}>
              <div>
                <Text strong>{uiText.agent.intentType}</Text>
                <Tag color="blue" style={{ marginLeft: 8 }}>
                  {intent.type}
                </Tag>
              </div>
              <div>
                <Text strong>{uiText.agent.intentConfidence}</Text>
                <Tag color="green" style={{ marginLeft: 8 }}>
                  {(intent.confidence * 100).toFixed(0)}%
                </Tag>
              </div>
              <div>
                <Text strong>{uiText.agent.intentReasoning}</Text> {intent.reasoning}
              </div>
            </Space>
          }
          type="info"
          showIcon
          icon={<FileSearchOutlined />}
          style={{ marginBottom: 24 }}
        />
      )}

      {steps.length > 0 && (
        <Card
          title={
            <Space>
              <span>{uiText.agent.executionFlow}</span>
              {totalDuration > 0 && (
                <Tag color="processing">
                  {formatText(uiText.agent.totalCost, { seconds: totalDuration.toFixed(2) })}
                </Tag>
              )}
            </Space>
          }
          className="execution-card"
        >
          <Timeline
            items={steps.map((step, index) => ({
              dot: getStepIcon(step.type),
              color: getStepColor(step.type),
              children: (
                <div className="step-content" key={step.id}>
                  <div className="step-header">
                    <Space>
                      <Text strong>
                        {index + 1}. {step.type === 'thinking' && `💭 ${uiText.agent.stepThinking}`}
                        {step.type === 'action' && `🔧 ${uiText.agent.stepAction}`}
                        {step.type === 'observation' && `📊 ${uiText.agent.stepObservation}`}
                        {step.type === 'final' && `✅ ${uiText.agent.stepFinal}`}
                      </Text>
                      {step.duration && (
                        <Tag color="default">{step.duration.toFixed(2)}s</Tag>
                      )}
                    </Space>
                  </div>

                  <div className="step-body">
                    {step.type === 'action' && step.tool && (
                      <div className="action-details">
                        <Text type="secondary">{uiText.agent.toolLabel}</Text>
                        <Tag color="blue">{step.tool}</Tag>
                        {step.args && (
                          <pre className="code-block">
                            {JSON.stringify(step.args, null, 2)}
                          </pre>
                        )}
                      </div>
                    )}

                    {step.type === 'observation' && Boolean(step.result) ? (
                      <div className="observation-result">
                        <pre className="code-block">
                          {JSON.stringify(step.result, null, 2)}
                        </pre>
                      </div>
                    ) : null}

                    {step.type === 'final' ? (
                      <div className="final-answer">
                        <ReactMarkdown>{step.content}</ReactMarkdown>
                      </div>
                    ) : (
                      <Paragraph>{step.content}</Paragraph>
                    )}
                  </div>
                </div>
              ),
            }))}
          />
        </Card>
      )}

      {!loading && steps.length === 0 && !intent && (
        <Empty
          description={uiText.agent.emptyDescription}
          style={{ marginTop: 60 }}
        />
      )}
    </div>
  )
}

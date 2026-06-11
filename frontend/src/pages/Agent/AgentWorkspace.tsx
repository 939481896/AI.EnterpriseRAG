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
  message,
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
import './AgentWorkspace.css'

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
          message.success('Agent 任务完成')
        },
        // onError callback
        (error) => {
          setLoading(false)
          message.error(`Agent 执行失败: ${error}`)
        }
      )
    } catch (error: any) {
      setLoading(false)
      message.error(error.message || 'Agent 执行失败')
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
      <Title level={3}>🤖 Agent 工作区</Title>
      <Paragraph type="secondary">
        智能 Agent 可以自动选择工具、执行任务并生成结果。支持 RAG 搜索、数据查询、日志分析等功能。
      </Paragraph>

      <Card className="input-card">
        <TextArea
          placeholder="输入您的需求，例如：查询上个月订单数量最多的产品"
          value={input}
          onChange={(e) => setInput(e.target.value)}
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
            执行
          </Button>
        </div>
      </Card>

      {intent && (
        <Alert
          message="意图识别"
          description={
            <Space direction="vertical" style={{ width: '100%' }}>
              <div>
                <Text strong>类型：</Text>
                <Tag color="blue" style={{ marginLeft: 8 }}>
                  {intent.type}
                </Tag>
              </div>
              <div>
                <Text strong>置信度：</Text>
                <Tag color="green" style={{ marginLeft: 8 }}>
                  {(intent.confidence * 100).toFixed(0)}%
                </Tag>
              </div>
              <div>
                <Text strong>推理：</Text> {intent.reasoning}
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
              <span>执行流程</span>
              {totalDuration > 0 && (
                <Tag color="processing">
                  总耗时: {totalDuration.toFixed(2)}s
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
                        {index + 1}. {step.type === 'thinking' && '💭 Thinking'}
                        {step.type === 'action' && '🔧 Action'}
                        {step.type === 'observation' && '📊 Observation'}
                        {step.type === 'final' && '✅ Final Answer'}
                      </Text>
                      {step.duration && (
                        <Tag color="default">{step.duration.toFixed(2)}s</Tag>
                      )}
                    </Space>
                  </div>

                  <div className="step-body">
                    {step.type === 'action' && step.tool && (
                      <div className="action-details">
                        <Text type="secondary">工具：</Text>
                        <Tag color="blue">{step.tool}</Tag>
                        {step.args && (
                          <pre className="code-block">
                            {JSON.stringify(step.args, null, 2)}
                          </pre>
                        )}
                      </div>
                    )}

                    {step.type === 'observation' && step.result && (
                      <div className="observation-result">
                        <pre className="code-block">
                          {JSON.stringify(step.result, null, 2)}
                        </pre>
                      </div>
                    )}

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
          description="输入需求并点击执行，Agent 将自动完成任务"
          style={{ marginTop: 60 }}
        />
      )}
    </div>
  )
}

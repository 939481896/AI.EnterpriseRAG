import { useState } from 'react'
import {
  Card,
  Input,
  Button,
  Timeline,
  Tag,
  Typography,
  Space,
  Divider,
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
import './AgentWorkspace.css'

const { Title, Text, Paragraph } = Typography
const { TextArea } = Input

interface AgentStep {
  id: string
  type: 'thinking' | 'action' | 'observation' | 'final'
  content: string
  tool?: string
  args?: Record<string, any>
  result?: any
  duration?: number
  timestamp: Date
}

export default function AgentWorkspace() {
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(false)
  const [steps, setSteps] = useState<AgentStep[]>([])
  const [intent, setIntent] = useState<{
    type: string
    confidence: number
    reasoning: string
  } | null>(null)

  const handleExecute = async () => {
    if (!input.trim()) return

    setLoading(true)
    setSteps([])
    setIntent(null)

    // Mock intent recognition
    setTimeout(() => {
      setIntent({
        type: 'data_query',
        confidence: 0.95,
        reasoning: '用户想要查询数据库中的信息',
      })
    }, 500)

    // Mock agent execution steps
    const mockSteps: AgentStep[] = [
      {
        id: '1',
        type: 'thinking',
        content: '我需要查询数据库来获取用户请求的信息',
        timestamp: new Date(),
        duration: 0.5,
      },
      {
        id: '2',
        type: 'action',
        content: '执行 SQL 查询',
        tool: 'sql_query',
        args: {
          query: 'SELECT product_name, COUNT(*) as order_count FROM orders WHERE order_date >= DATE_SUB(NOW(), INTERVAL 1 MONTH) GROUP BY product_name ORDER BY order_count DESC LIMIT 5',
        },
        timestamp: new Date(),
        duration: 1.2,
      },
      {
        id: '3',
        type: 'observation',
        content: '查询成功',
        result: [
          { product_name: 'iPhone 15 Pro', order_count: 1250 },
          { product_name: 'MacBook Pro M3', order_count: 980 },
          { product_name: 'AirPods Pro', order_count: 850 },
          { product_name: 'iPad Air', order_count: 720 },
          { product_name: 'Apple Watch', order_count: 650 },
        ],
        timestamp: new Date(),
        duration: 0.3,
      },
      {
        id: '4',
        type: 'thinking',
        content: '我已经获得了查询结果，可以生成最终答案了',
        timestamp: new Date(),
        duration: 0.2,
      },
      {
        id: '5',
        type: 'final',
        content: `根据查询结果，上个月订单数量最多的前5个产品是：

1. **iPhone 15 Pro** - 1,250 单
2. **MacBook Pro M3** - 980 单
3. **AirPods Pro** - 850 单
4. **iPad Air** - 720 单
5. **Apple Watch** - 650 单

其中 iPhone 15 Pro 以绝对优势领先，占据了最多的订单量。`,
        timestamp: new Date(),
        duration: 2.2,
      },
    ]

    let currentIndex = 0
    const interval = setInterval(() => {
      if (currentIndex < mockSteps.length) {
        setSteps((prev) => [...prev, mockSteps[currentIndex]])
        currentIndex++
      } else {
        clearInterval(interval)
        setLoading(false)
      }
    }, 800)
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

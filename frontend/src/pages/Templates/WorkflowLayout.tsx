/**
 * Workflow Execution Layout Template
 *
 * 用于任务执行和流程演示页面，如 Agent 工作区、数据分析等。
 * 特点：输入 + 执行 + 实时反馈 + 结果展示
 *
 * 使用示例：
 * ```tsx
 * export default function AgentPage() {
 *   const [input, setInput] = useState('')
 *   const [steps, setSteps] = useState<Step[]>([])
 *   const [loading, setLoading] = useState(false)
 *   const [result, setResult] = useState<Result | null>(null)
 *
 *   const handleExecute = async () => {
 *     setLoading(true)
 *     // ... execute logic
 *     setSteps([...steps])
 *     setResult(result)
 *     setLoading(false)
 *   }
 *
 *   return (
 *     <WorkflowLayout
 *       title="Agent 工作区"
 *       description="执行智能任务"
 *       input={{
 *         value: input,
 *         onChange: setInput,
 *         placeholder: '输入您的需求',
 *         multiline: true,
 *       }}
 *       onExecute={handleExecute}
 *       executeButtonText="执行"
 *       loading={loading}
 *       steps={steps}
 *       stepRenderer={(step) => <StepCard step={step} />}
 *       result={result}
 *       resultRenderer={(result) => <ResultCard result={result} />}
 *     />
 *   )
 * }
 * ```
 */

import {
  Button,
  Input,
  Spin,
  Empty,
  Space,
  Card,
  Typography,
  Alert,
} from 'antd'
import { CaretRightOutlined } from '@ant-design/icons'
import React from 'react'
import './WorkflowLayout.css'

interface WorkflowLayoutProps {
  /** 页面标题 */
  title: string
  /** 页面描述（可选） */
  description?: string
  /** 输入框配置 */
  input: {
    value: string
    onChange: (value: string) => void
    placeholder?: string
    multiline?: boolean
    disabled?: boolean
  }
  /** 执行按钮回调 */
  onExecute: () => void | Promise<void>
  /** 执行按钮文本，默认"执行" */
  executeButtonText?: string
  /** 是否加载中 */
  loading?: boolean
  /** 步骤列表 */
  steps?: any[]
  /** 步骤渲染函数 */
  stepRenderer?: (step: any, index: number) => React.ReactNode
  /** 结果数据 */
  result?: any
  /** 结果渲染函数 */
  resultRenderer?: (result: any) => React.ReactNode
  /** 错误信息 */
  error?: string | null
  /** 空状态提示 */
  emptyDescription?: string
  /** 自定义 header（可选） */
  header?: React.ReactNode
}

const { Title, Text, Paragraph } = Typography

/**
 * Workflow Execution Layout 组件
 *
 * 用于流程执行和结果展示的标准布局：
 * - 标题和描述
 * - 输入区域（text 或 textarea）
 * - 执行按钮
 * - 实时步骤显示（Timeline 风格）
 * - 结果展示区域
 * - 错误提示
 */
export default function WorkflowLayout({
  title,
  description,
  input,
  onExecute,
  executeButtonText = '执行',
  loading = false,
  steps = [],
  stepRenderer,
  result,
  resultRenderer,
  error,
  emptyDescription,
  header,
}: WorkflowLayoutProps) {
  const handleExecute = async () => {
    try {
      await onExecute()
    } catch (err) {
      console.error('Workflow execution error:', err)
    }
  }

  const InputComponent = input.multiline ? Input.TextArea : Input

  return (
    <div className="workflow-layout" style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
      {/* Header */}
      {header ? (
        header
      ) : (
        <div>
          <Title level={3} style={{ margin: 0 }}>
            {title}
          </Title>
          {description && (
            <Text type="secondary" style={{ fontSize: 14 }}>
              {description}
            </Text>
          )}
        </div>
      )}

      {/* Input Section */}
      <Card>
        <Space direction="vertical" style={{ width: '100%' }} size="middle">
          <InputComponent
            value={input.value}
            onChange={(e) => input.onChange(e.target.value)}
            placeholder={input.placeholder}
            disabled={input.disabled || loading}
            rows={input.multiline ? 4 : 1}
            style={{ fontSize: 14 }}
          />
          <Button
            type="primary"
            icon={<CaretRightOutlined />}
            onClick={handleExecute}
            loading={loading}
            disabled={!input.value.trim() || loading}
            size="large"
          >
            {executeButtonText}
          </Button>
        </Space>
      </Card>

      {/* Error Alert */}
      {error && <Alert message="Error" description={error} type="error" showIcon closable />}

      {/* Loading State */}
      {loading && (
        <div style={{ textAlign: 'center', padding: 60 }}>
          <Spin size="large" tip="Processing..." />
        </div>
      )}

      {/* Steps Section */}
      {!loading && steps.length > 0 && (
        <Card title="Execution Steps">
          <Space direction="vertical" style={{ width: '100%' }} size="small">
            {steps.map((step, index) =>
              stepRenderer ? (
                <div key={index}>{stepRenderer(step, index)}</div>
              ) : (
                <div
                  key={index}
                  style={{
                    padding: 12,
                    border: '1px solid #f0f0f0',
                    borderRadius: 4,
                  }}
                >
                  <Paragraph style={{ margin: 0 }}>{JSON.stringify(step)}</Paragraph>
                </div>
              )
            )}
          </Space>
        </Card>
      )}

      {/* Result Section */}
      {!loading && result && (
        <Card title="Result">
          {resultRenderer ? (
            resultRenderer(result)
          ) : (
            <Paragraph style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
              {typeof result === 'string' ? result : JSON.stringify(result, null, 2)}
            </Paragraph>
          )}
        </Card>
      )}

      {/* Empty State */}
      {!loading && steps.length === 0 && !result && (
        <Empty description={emptyDescription} style={{ marginTop: 60 }} />
      )}
    </div>
  )
}

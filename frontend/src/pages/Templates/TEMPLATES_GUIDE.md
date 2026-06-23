# 页面模板使用指南

本文档介绍如何使用系统提供的三个页面模板来快速构建常见的页面类型。

## 三种页面模板

### 1. SidebarContentLayout - 侧边栏内容布局

**适用场景**：
- 聊天对话类页面（Chat）
- 知识库浏览
- 任何需要左侧导航 + 右侧内容的页面

**特点**：
- 固定侧边栏（支持深色/浅色主题）
- 主区域分为 header + content + footer
- Content 区域自动 flex 填充
- 自动处理滚动

**使用示例**：

```tsx
// src/pages/MyPage/MyPage.tsx
import { useState } from 'react'
import SidebarContentLayout from '@/pages/Templates/SidebarContentLayout'
import MySidebar from './components/MySidebar'
import MyHeader from './components/MyHeader'
import MyContent from './components/MyContent'
import MyFooter from './components/MyFooter'

export default function MyPage() {
  const [selectedId, setSelectedId] = useState<string>()

  return (
    <SidebarContentLayout
      sidebar={<MySidebar selectedId={selectedId} onSelect={setSelectedId} />}
      header={<MyHeader selectedId={selectedId} />}
      content={<MyContent selectedId={selectedId} />}
      footer={<MyFooter />}
      sidebarWidth={280}
      sidebarTheme="light"
    />
  )
}
```

**Props 详解**：

| Prop | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| sidebar | ReactNode | - | 侧边栏内容（必需） |
| header | ReactNode | undefined | 主区域顶部 header |
| content | ReactNode | - | 主区域中间内容（必需） |
| footer | ReactNode | undefined | 主区域底部 footer |
| sidebarWidth | number | 280 | 侧边栏宽度（px） |
| sidebarTheme | 'light' \| 'dark' | 'light' | 侧边栏主题 |
| height | string \| number | calc(100vh - 112px) | 总体高度 |

---

### 2. TableManagementLayout - 表格管理布局

**适用场景**：
- 文档管理（DocumentPage）
- 用户管理
- 角色管理
- 权限管理
- 任何列表数据展示和操作

**特点**：
- 标题 + 工具栏
- 搜索和过滤功能
- 表格展示
- 自动分页处理

**使用示例**：

```tsx
// src/pages/Document/DocumentPage.tsx
import { useState } from 'react'
import { Button, Space } from 'antd'
import { PlusOutlined, DownloadOutlined } from '@ant-design/icons'
import TableManagementLayout from '@/pages/Templates/TableManagementLayout'
import { useDocuments } from '@/hooks/useDocument'
import type { Document } from '@/types/document'
import { uiText } from '@/config/uiText'

export default function DocumentPage() {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [searchText, setSearchText] = useState('')
  const [statusFilter, setStatusFilter] = useState<string | null>(null)

  const { data, isLoading } = useDocuments(page, pageSize, searchText, statusFilter)

  const columns = [
    {
      title: uiText.document.colName,
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: uiText.document.colSize,
      dataIndex: 'size',
      key: 'size',
      render: (size: number) => `${(size / 1024 / 1024).toFixed(2)} MB`,
    },
    {
      title: uiText.document.colStatus,
      dataIndex: 'status',
      key: 'status',
    },
    {
      title: uiText.document.colActions,
      key: 'actions',
      render: (_, record: Document) => (
        <Space>
          <Button type="link">编辑</Button>
          <Button type="link" danger>删除</Button>
        </Space>
      ),
    },
  ]

  return (
    <TableManagementLayout
      title={uiText.document.pageTitle}
      toolbar={
        <Space>
          <Button type="primary" icon={<PlusOutlined />}>
            {uiText.common.create}
          </Button>
          <Button icon={<DownloadOutlined />}>导出</Button>
        </Space>
      }
      filters={[
        {
          key: 'search',
          label: '搜索',
          type: 'input',
          placeholder: uiText.document.searchPlaceholder,
        },
        {
          key: 'status',
          label: '状态',
          type: 'select',
          options: [
            { label: uiText.document.pending, value: 'pending' },
            { label: uiText.document.processing, value: 'processing' },
            { label: uiText.document.completed, value: 'completed' },
          ],
        },
      ]}
      table={{
        columns,
        dataSource: data?.items || [],
        loading: isLoading,
        pagination: {
          current: page,
          pageSize,
          total: data?.total || 0,
          showSizeChanger: true,
          pageSizeOptions: ['10', '20', '50', '100'],
        },
      }}
      onFilterChange={(filters) => {
        setSearchText(filters.search || '')
        setStatusFilter(filters.status || null)
        setPage(1)
      }}
      onPaginationChange={(p, ps) => {
        setPage(p)
        setPageSize(ps)
      }}
    />
  )
}
```

**Props 详解**：

| Prop | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| title | string | - | 页面标题（必需） |
| toolbar | ReactNode | undefined | 工具栏（新增、导出等按钮） |
| filters | FilterItem[] | [] | 过滤器配置 |
| table | TableProps | - | Ant Design Table props（必需） |
| onFilterChange | (filters) => void | undefined | 过滤值变化回调 |
| onPaginationChange | (page, pageSize) => void | undefined | 分页变化回调 |

**FilterItem 类型**：

```typescript
interface FilterItem {
  key: string                                    // 过滤键
  label: string                                  // 过滤标签
  type: 'input' | 'select' | 'date' | 'daterange'  // 过滤器类型
  placeholder?: string                          // 占位符
  options?: Array<{ label: string; value: any }> // select 类型的选项
}
```

---

### 3. WorkflowLayout - 工作流执行布局

**适用场景**：
- Agent 工作区（AgentWorkspace）
- 数据分析和处理
- 任何需要输入 → 执行 → 实时反馈 → 结果展示的页面

**特点**：
- 输入框（支持单行/多行）
- 执行按钮
- 实时步骤显示
- 结果展示区域
- 错误提示

**使用示例**：

```tsx
// src/pages/Agent/AgentWorkspace.tsx
import { useState } from 'react'
import { Card, Tag, Typography } from 'antd'
import { CheckCircleOutlined, ThunderboltOutlined } from '@ant-design/icons'
import WorkflowLayout from '@/pages/Templates/WorkflowLayout'
import { agentApi, type AgentStep } from '@/api/agent'
import { uiText } from '@/config/uiText'

const { Text, Paragraph } = Typography

export default function AgentWorkspace() {
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(false)
  const [steps, setSteps] = useState<AgentStep[]>([])
  const [result, setResult] = useState<string>()
  const [error, setError] = useState<string>()

  const handleExecute = async () => {
    if (!input.trim()) return

    setLoading(true)
    setSteps([])
    setResult(undefined)
    setError(undefined)

    try {
      await agentApi.executeAgent(
        { input: input.trim(), maxIterations: 10 },
        (step) => setSteps((prev) => [...prev, step]),
        null,
        () => setLoading(false),
        (err) => {
          setError(err)
          setLoading(false)
        }
      )
    } catch (err) {
      setError(String(err))
      setLoading(false)
    }
  }

  const renderStep = (step: AgentStep, index: number) => {
    const icons = {
      thinking: <ThunderboltOutlined style={{ color: '#1890ff' }} />,
      action: <CheckCircleOutlined style={{ color: '#52c41a' }} />,
      observation: <Text code />,
      final: <CheckCircleOutlined style={{ color: '#faad14' }} />,
    }

    return (
      <Card
        key={index}
        size="small"
        style={{ marginBottom: 8 }}
        title={
          <Space>
            {icons[step.type as keyof typeof icons]}
            <Text strong>{step.type.toUpperCase()}</Text>
            <Tag>{step.timestamp}</Tag>
          </Space>
        }
      >
        <Paragraph style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
          {step.content}
        </Paragraph>
      </Card>
    )
  }

  return (
    <WorkflowLayout
      title={uiText.agent.pageTitle}
      description={uiText.agent.pageDescription}
      input={{
        value: input,
        onChange: setInput,
        placeholder: uiText.agent.inputPlaceholder,
        multiline: true,
        disabled: loading,
      }}
      onExecute={handleExecute}
      executeButtonText={uiText.agent.execute}
      loading={loading}
      steps={steps}
      stepRenderer={renderStep}
      result={result}
      resultRenderer={(res) => <Paragraph>{res}</Paragraph>}
      error={error}
      emptyDescription={uiText.agent.emptyDescription}
    />
  )
}
```

**Props 详解**：

| Prop | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| title | string | - | 页面标题（必需） |
| description | string | undefined | 页面描述 |
| input | InputConfig | - | 输入框配置（必需） |
| onExecute | () => Promise<void> | - | 执行按钮回调（必需） |
| executeButtonText | string | '执行' | 执行按钮文本 |
| loading | boolean | false | 是否加载中 |
| steps | any[] | [] | 步骤列表 |
| stepRenderer | (step, index) => ReactNode | undefined | 步骤渲染函数 |
| result | any | undefined | 结果数据 |
| resultRenderer | (result) => ReactNode | undefined | 结果渲染函数 |
| error | string \| null | undefined | 错误信息 |
| emptyDescription | string | undefined | 空状态提示 |
| header | ReactNode | undefined | 自定义 header |

**InputConfig 类型**：

```typescript
interface InputConfig {
  value: string
  onChange: (value: string) => void
  placeholder?: string
  multiline?: boolean
  disabled?: boolean
}
```

---

## 最佳实践

### ✅ 何时使用哪个模板

| 页面类型 | 推荐模板 | 示例 |
|---------|--------|------|
| 对话/消息 | SidebarContentLayout | Chat（消息 + 会话列表） |
| 列表管理 | TableManagementLayout | 文档、用户、角色、权限管理 |
| 任务执行 | WorkflowLayout | Agent 工作区、数据分析 |
| 自定义页面 | 组合使用 | Dashboard（多个卡片布局） |

### ✅ 通用设计原则

1. **始终使用 uiText 进行文案管理**
   ```tsx
   // ❌ 不推荐
   <Button>创建</Button>
   
   // ✅ 推荐
   <Button>{uiText.common.create}</Button>
   ```

2. **使用模板中的 Props 而非样式覆盖**
   ```tsx
   // ✅ 推荐
   <SidebarContentLayout sidebarWidth={300} sidebarTheme="dark" />
   
   // ❌ 不推荐
   <SidebarContentLayout style={{ --sidebar-width: '300px' }} />
   ```

3. **处理加载和错误状态**
   ```tsx
   const { data, isLoading, error } = useQuery(...)
   
   return (
     <TableManagementLayout
       table={{
         dataSource: data || [],
         loading: isLoading,
       }}
     />
   )
   ```

4. **遵循页面标准流程**
   - 获取数据（useQuery）
   - 处理操作（useMutation）
   - 渲染 UI（使用模板）
   - 显示反馈（notification）

### ✅ 性能优化建议

1. **使用 useMemo 缓存列表定义**
   ```tsx
   const columns = useMemo(() => [...], [locale])
   ```

2. **分离容器组件和展示组件**
   ```tsx
   // 容器：处理数据和逻辑
   export default function DocumentPageContainer() {
     const [page, setPage] = useState(1)
     return <DocumentPageView ... />
   }
   
   // 展示：只负责渲染
   function DocumentPageView(props) {
     return <TableManagementLayout ... />
   }
   ```

3. **避免在渲染函数中创建新的对象/函数**
   ```tsx
   // ❌ 不推荐
   <TableManagementLayout
     filters={[{ key: 'a', ... }, { key: 'b', ... }]}
   />
   
   // ✅ 推荐
   const filters = useMemo(() => [...], [])
   ```

---

## 常见问题

**Q: 如何在 WorkflowLayout 中实现流式输出？**

A: 通过 `setSteps((prev) => [...prev, newStep])` 逐步添加步骤：

```tsx
const handleExecute = async () => {
  for await (const step of executeSteps()) {
    setSteps((prev) => [...prev, step])
  }
}
```

**Q: 如何在 TableManagementLayout 中实现服务端搜索？**

A: 在 onFilterChange 回调中更新搜索状态，然后 useQuery hook 会自动重新获取：

```tsx
onFilterChange={(filters) => {
  setSearchText(filters.search)
  setPage(1)  // 重置页码
}}

const { data } = useDocuments(page, pageSize, searchText)
```

**Q: 能否在 SidebarContentLayout 中隐藏 header 或 footer？**

A: 可以，只需要不传递对应的 prop 即可：

```tsx
<SidebarContentLayout
  sidebar={sidebar}
  content={content}
  // 不传 header 和 footer 就不会显示
/>
```

---

## 延伸阅读

- [模块注册指南](./MODULES_GUIDE.md)
- [Ant Design Table 文档](https://ant.design/components/table/)
- [React Hooks 最佳实践](https://react.dev/reference/react)

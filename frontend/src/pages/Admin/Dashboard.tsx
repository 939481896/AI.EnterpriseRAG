import { Row, Col, Card, Statistic, Typography, Progress, Table } from 'antd'
import {
  UserOutlined,
  FileTextOutlined,
  MessageOutlined,
  ClockCircleOutlined,
  ArrowUpOutlined,
  ArrowDownOutlined,
} from '@ant-design/icons'
import ReactECharts from 'echarts-for-react'
import type { EChartsOption } from 'echarts'
import './Dashboard.css'

const { Title } = Typography

export default function Dashboard() {
  // Mock data
  const stats = {
    totalUsers: 156,
    totalDocuments: 1234,
    totalChats: 5678,
    avgResponseTime: 3.2,
    userGrowth: 12.5,
    documentGrowth: 8.3,
    chatGrowth: 15.2,
    responseTimeChange: -5.6,
  }

  // Line chart for API usage
  const usageChartOption: EChartsOption = {
    title: {
      text: 'API 使用趋势（最近7天）',
      left: 'center',
    },
    tooltip: {
      trigger: 'axis',
    },
    legend: {
      data: ['V0 接口', 'V1 接口'],
      bottom: 0,
    },
    xAxis: {
      type: 'category',
      data: ['周一', '周二', '周三', '周四', '周五', '周六', '周日'],
    },
    yAxis: {
      type: 'value',
    },
    series: [
      {
        name: 'V0 接口',
        type: 'line',
        smooth: true,
        data: [320, 302, 301, 334, 390, 330, 320],
      },
      {
        name: 'V1 接口',
        type: 'line',
        smooth: true,
        data: [120, 132, 201, 234, 290, 330, 410],
      },
    ],
  }

  // Pie chart for document distribution
  const docChartOption: EChartsOption = {
    title: {
      text: '文档类型分布',
      left: 'center',
    },
    tooltip: {
      trigger: 'item',
    },
    legend: {
      bottom: 0,
    },
    series: [
      {
        name: '文档类型',
        type: 'pie',
        radius: '60%',
        data: [
          { value: 540, name: 'PDF' },
          { value: 435, name: 'Word' },
          { value: 259, name: 'TXT' },
        ],
        emphasis: {
          itemStyle: {
            shadowBlur: 10,
            shadowOffsetX: 0,
            shadowColor: 'rgba(0, 0, 0, 0.5)',
          },
        },
      },
    ],
  }

  // Bar chart for response time
  const responseTimeOption: EChartsOption = {
    title: {
      text: '响应时间分布（秒）',
      left: 'center',
    },
    tooltip: {
      trigger: 'axis',
      axisPointer: {
        type: 'shadow',
      },
    },
    xAxis: {
      type: 'category',
      data: ['<1s', '1-2s', '2-3s', '3-5s', '5-10s', '>10s'],
    },
    yAxis: {
      type: 'value',
    },
    series: [
      {
        name: '请求数',
        type: 'bar',
        data: [120, 350, 280, 150, 80, 20],
        itemStyle: {
          color: '#1890ff',
        },
      },
    ],
  }

  // Top questions table
  const topQuestions = [
    { rank: 1, question: '房价下降的基本原则是什么？', count: 45 },
    { rank: 2, question: '如何进行文档上传？', count: 38 },
    { rank: 3, question: 'RAG V0 和 V1 有什么区别？', count: 32 },
    { rank: 4, question: '支持哪些文件格式？', count: 28 },
    { rank: 5, question: '如何管理会话历史？', count: 25 },
  ]

  return (
    <div className="dashboard">
      <Title level={3}>数据面板</Title>

      {/* Statistics Cards */}
      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="总用户数"
              value={stats.totalUsers}
              prefix={<UserOutlined />}
              suffix={
                <span style={{ fontSize: 14, color: '#52c41a' }}>
                  <ArrowUpOutlined /> {stats.userGrowth}%
                </span>
              }
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="总文档数"
              value={stats.totalDocuments}
              prefix={<FileTextOutlined />}
              suffix={
                <span style={{ fontSize: 14, color: '#52c41a' }}>
                  <ArrowUpOutlined /> {stats.documentGrowth}%
                </span>
              }
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="总对话数"
              value={stats.totalChats}
              prefix={<MessageOutlined />}
              suffix={
                <span style={{ fontSize: 14, color: '#52c41a' }}>
                  <ArrowUpOutlined /> {stats.chatGrowth}%
                </span>
              }
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="平均响应时间"
              value={stats.avgResponseTime}
              prefix={<ClockCircleOutlined />}
              suffix={
                <>
                  <span style={{ fontSize: 16 }}>s</span>
                  <span style={{ fontSize: 14, color: '#52c41a', marginLeft: 8 }}>
                    <ArrowDownOutlined /> {Math.abs(stats.responseTimeChange)}%
                  </span>
                </>
              }
              precision={1}
            />
          </Card>
        </Col>
      </Row>

      {/* Charts */}
      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} lg={12}>
          <Card>
            <ReactECharts option={usageChartOption} style={{ height: 350 }} />
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card>
            <ReactECharts option={docChartOption} style={{ height: 350 }} />
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} lg={12}>
          <Card>
            <ReactECharts option={responseTimeOption} style={{ height: 350 }} />
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card title="热门问题 TOP 5">
            <Table
              dataSource={topQuestions}
              rowKey="rank"
              pagination={false}
              size="small"
              columns={[
                {
                  title: '排名',
                  dataIndex: 'rank',
                  key: 'rank',
                  width: 60,
                },
                {
                  title: '问题',
                  dataIndex: 'question',
                  key: 'question',
                  ellipsis: true,
                },
                {
                  title: '次数',
                  dataIndex: 'count',
                  key: 'count',
                  width: 80,
                },
              ]}
            />
          </Card>
        </Col>
      </Row>
    </div>
  )
}

import { lazy, Suspense } from 'react'
import { Row, Col, Card, Statistic, Table } from 'antd'
import {
  UserOutlined,
  FileTextOutlined,
  MessageOutlined,
  ClockCircleOutlined,
  ArrowUpOutlined,
  ArrowDownOutlined,
} from '@ant-design/icons'
import { Spin } from 'antd'
import type { EChartsOption } from 'echarts'
import { uiText } from '@/config/uiText'

// Lazy-load chart renderer so non-chart admin interactions are not blocked by ECharts bootstrap.
const ReactECharts = lazy(() => import('echarts-for-react'))

export default function Dashboard() {
  // Mock metrics; replace with real analytics query when backend endpoint is ready.
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
      text: uiText.adminDashboard.apiTrend,
      left: 'center',
    },
    tooltip: {
      trigger: 'axis',
    },
    legend: {
      data: [uiText.adminDashboard.v0Api, uiText.adminDashboard.v1Api],
      bottom: 0,
    },
    xAxis: {
      type: 'category',
      data: [
        uiText.adminDashboard.monday,
        uiText.adminDashboard.tuesday,
        uiText.adminDashboard.wednesday,
        uiText.adminDashboard.thursday,
        uiText.adminDashboard.friday,
        uiText.adminDashboard.saturday,
        uiText.adminDashboard.sunday,
      ],
    },
    yAxis: {
      type: 'value',
    },
    series: [
      {
        name: uiText.adminDashboard.v0Api,
        type: 'line',
        smooth: true,
        data: [320, 302, 301, 334, 390, 330, 320],
      },
      {
        name: uiText.adminDashboard.v1Api,
        type: 'line',
        smooth: true,
        data: [120, 132, 201, 234, 290, 330, 410],
      },
    ],
  }

  // Pie chart for document distribution
  const docChartOption: EChartsOption = {
    title: {
      text: uiText.adminDashboard.docDistribution,
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
        name: uiText.adminDashboard.docType,
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
      text: uiText.adminDashboard.responseDistribution,
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
        name: uiText.adminDashboard.requestCount,
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
    { rank: 1, question: uiText.adminDashboard.topQuestion1, count: 45 },
    { rank: 2, question: uiText.adminDashboard.topQuestion2, count: 38 },
    { rank: 3, question: uiText.adminDashboard.topQuestion3, count: 32 },
    { rank: 4, question: uiText.adminDashboard.topQuestion4, count: 28 },
    { rank: 5, question: uiText.adminDashboard.topQuestion5, count: 25 },
  ]

  return (
    <div className="page-container">
      <div className="page-header">
        <h3>{uiText.adminDashboard.title}</h3>
      </div>

      {/* Statistics Cards */}
      <Row gutter={[16, 16]} className="section">
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title={uiText.adminDashboard.totalUsers}
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
              title={uiText.adminDashboard.totalDocuments}
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
              title={uiText.adminDashboard.totalChats}
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
              title={uiText.adminDashboard.avgResponseTime}
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
            <Suspense fallback={<div style={{ height: 350, display: 'flex', alignItems: 'center', justifyContent: 'center' }}><Spin /></div>}>
              <ReactECharts option={usageChartOption} style={{ height: 350 }} />
            </Suspense>
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card>
            <Suspense fallback={<div style={{ height: 350, display: 'flex', alignItems: 'center', justifyContent: 'center' }}><Spin /></div>}>
              <ReactECharts option={docChartOption} style={{ height: 350 }} />
            </Suspense>
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} lg={12}>
          <Card>
            <Suspense fallback={<div style={{ height: 350, display: 'flex', alignItems: 'center', justifyContent: 'center' }}><Spin /></div>}>
              <ReactECharts option={responseTimeOption} style={{ height: 350 }} />
            </Suspense>
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card title={uiText.adminDashboard.topQuestions}>
            <Table
              dataSource={topQuestions}
              rowKey="rank"
              pagination={false}
              size="small"
              columns={[
                {
                  title: uiText.adminDashboard.rank,
                  dataIndex: 'rank',
                  key: 'rank',
                  width: 60,
                },
                {
                  title: uiText.adminDashboard.question,
                  dataIndex: 'question',
                  key: 'question',
                  ellipsis: true,
                },
                {
                  title: uiText.adminDashboard.count,
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

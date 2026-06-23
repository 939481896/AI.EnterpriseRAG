/**
 * Table Management Layout Template
 *
 * 用于数据列表管理页面，如文档管理、用户管理、角色管理等。
 * 特点：搜索/过滤 + 表格 + 分页
 *
 * 使用示例：
 * ```tsx
 * export default function DocumentPage() {
 *   const [page, setPage] = useState(1)
 *   const [pageSize, setPageSize] = useState(20)
 *   const { data, isLoading } = useDocuments(page, pageSize)
 *
 *   return (
 *     <TableManagementLayout
 *       title="文档管理"
 *       toolbar={<DocumentUploadButton />}
 *       filters={[
 *         { key: 'search', label: '搜索', type: 'input', placeholder: '输入文档名称' },
 *         { key: 'status', label: '状态', type: 'select', options: statusOptions },
 *       ]}
 *       table={{
 *         columns: documentColumns,
 *         dataSource: data?.items || [],
 *         pagination: { current: page, pageSize, total: data?.total },
 *         loading: isLoading,
 *       }}
 *       onFilterChange={(filters) => { setSearchText(filters.search) }}
 *       onPaginationChange={(p, ps) => { setPage(p); setPageSize(ps) }}
 *     />
 *   )
 * }
 * ```
 */

import {
  Space,
  Input,
  Select,
  DatePicker,
  Table,
  Row,
  Col,
  Typography,
} from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import type { TableProps, TablePaginationConfig } from 'antd'
import React, { useState } from 'react'
import './TableManagementLayout.css'

interface FilterItem {
  key: string
  label: string
  type: 'input' | 'select' | 'date' | 'daterange'
  placeholder?: string
  options?: Array<{ label: string; value: any }>
}

interface TableManagementLayoutProps<T extends Record<string, any>> {
  /** 页面标题 */
  title: string
  /** 工具栏按钮（新增、导出等） */
  toolbar?: React.ReactNode
  /** 过滤器配置 */
  filters?: FilterItem[]
  /** 表格属性 */
  table: TableProps<T>
  /** 过滤值变化回调 */
  onFilterChange?: (filters: Record<string, any>) => void
  /** 分页变化回调 */
  onPaginationChange?: (page: number, pageSize: number) => void
}

const { Title } = Typography

/**
 * Table Management Layout 组件
 *
 * 标准的表格管理页面布局：
 * - 标题 + 工具栏
 * - 搜索和过滤区域
 * - 数据表格
 * - 自动分页处理
 */
export default function TableManagementLayout<T extends Record<string, any>>({
  title,
  toolbar,
  filters = [],
  table,
  onFilterChange,
  onPaginationChange,
}: TableManagementLayoutProps<T>) {
  const [filterValues, setFilterValues] = useState<Record<string, any>>({})

  const handleFilterChange = (key: string, value: any) => {
    const newValues = { ...filterValues, [key]: value }
    setFilterValues(newValues)
    onFilterChange?.(newValues)
  }

  const handlePaginationChange = (page: number, pageSize: number) => {
    onPaginationChange?.(page, pageSize)
  }

  const renderFilterInput = (filter: FilterItem) => {
    const value = filterValues[filter.key]

    switch (filter.type) {
      case 'input':
        return (
          <Input
            key={filter.key}
            placeholder={filter.placeholder}
            value={value}
            onChange={(e) => handleFilterChange(filter.key, e.target.value)}
            prefix={<SearchOutlined />}
            allowClear
          />
        )
      case 'select':
        return (
          <Select
            key={filter.key}
            placeholder={filter.placeholder}
            value={value}
            onChange={(v) => handleFilterChange(filter.key, v)}
            options={filter.options}
            allowClear
            style={{ width: '100%' }}
          />
        )
      case 'date':
        return (
          <DatePicker
            key={filter.key}
            placeholder={filter.placeholder}
            value={value}
            onChange={(date) => handleFilterChange(filter.key, date)}
          />
        )
      case 'daterange':
        return (
          <DatePicker.RangePicker
            key={filter.key}
            placeholder={[filter.placeholder || 'Start', 'End']}
            value={value}
            onChange={(dates) => handleFilterChange(filter.key, dates)}
          />
        )
      default:
        return null
    }
  }

  return (
    <div className="table-management-layout" style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
      {/* Header */}
      <div>
        <Row gutter={[16, 16]} align="middle" justify="space-between">
          <Col>
            <Title level={3} style={{ margin: 0 }}>
              {title}
            </Title>
          </Col>
          {toolbar && <Col>{toolbar}</Col>}
        </Row>
      </div>

      {/* Filters */}
      {filters.length > 0 && (
        <div className="table-filters" style={{ padding: '12px 16px', background: '#fafafa', borderRadius: 4 }}>
          <Space wrap size="middle">
            {filters.map((filter) => (
              <div key={filter.key} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <span style={{ fontSize: 14 }}>{filter.label}:</span>
                {renderFilterInput(filter)}
              </div>
            ))}
          </Space>
        </div>
      )}

      {/* Table */}
      <Table<T>
        {...table}
        onChange={(pagination: TablePaginationConfig) => {
          if (pagination.current && pagination.pageSize) {
            handlePaginationChange(pagination.current, pagination.pageSize)
          }
        }}
      />
    </div>
  )
}

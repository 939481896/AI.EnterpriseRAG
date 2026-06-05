import { useState } from 'react'
import {
  Button,
  Table,
  Space,
  Tag,
  Input,
  Select,
  Modal,
  message,
  Progress,
  Typography,
  Upload,
  Card,
} from 'antd'
import {
  UploadOutlined,
  SearchOutlined,
  EyeOutlined,
  DeleteOutlined,
  FileTextOutlined,
  FilePdfOutlined,
  FileWordOutlined,
  InboxOutlined,
} from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import { useDocuments, useUploadDocument, useDeleteDocument } from '@/hooks/useDocument'
import { Document, DocumentStatus } from '@/types/document'
import dayjs from 'dayjs'
import './DocumentPage.css'

const { Title, Text } = Typography
const { Dragger } = Upload

const statusColors = {
  [DocumentStatus.Pending]: 'default',
  [DocumentStatus.Processing]: 'processing',
  [DocumentStatus.Completed]: 'success',
  [DocumentStatus.Failed]: 'error',
}

const statusTexts = {
  [DocumentStatus.Pending]: '待处理',
  [DocumentStatus.Processing]: '处理中',
  [DocumentStatus.Completed]: '已完成',
  [DocumentStatus.Failed]: '失败',
}

export default function DocumentPage() {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [searchText, setSearchText] = useState('')
  const [statusFilter, setStatusFilter] = useState<DocumentStatus | null>(null)
  const [previewDoc, setPreviewDoc] = useState<Document | null>(null)

  const { data, isLoading, refetch } = useDocuments(page, pageSize)
  const { upload, uploadProgress, isUploading } = useUploadDocument()
  const deleteDocument = useDeleteDocument()

  const handleUpload = (file: File) => {
    const isValidSize = file.size <= 50 * 1024 * 1024 // 50MB
    const isValidType = ['.pdf', '.docx', '.doc', '.txt'].some(ext => 
      file.name.toLowerCase().endsWith(ext)
    )

    if (!isValidSize) {
      message.error('文件大小不能超过 50MB')
      return false
    }

    if (!isValidType) {
      message.error('仅支持 PDF、Word、TXT 格式')
      return false
    }

    upload(file)
    return false // Prevent default upload
  }

  const handleDelete = (doc: Document) => {
    Modal.confirm({
      title: '确认删除',
      content: `确定要删除文档 "${doc.name}" 吗？此操作不可恢复。`,
      okText: '删除',
      okType: 'danger',
      cancelText: '取消',
      onOk: () => {
        deleteDocument.mutate(doc.id)
      },
    })
  }

  const handlePreview = (doc: Document) => {
    setPreviewDoc(doc)
  }

  const getFileIcon = (fileType: string) => {
    if (fileType.includes('pdf')) return <FilePdfOutlined style={{ color: '#ff4d4f' }} />
    if (fileType.includes('word') || fileType.includes('doc')) return <FileWordOutlined style={{ color: '#1890ff' }} />
    return <FileTextOutlined style={{ color: '#52c41a' }} />
  }

  const columns: ColumnsType<Document> = [
    {
      title: '文档名称',
      dataIndex: 'name',
      key: 'name',
      ellipsis: true,
      render: (text, record) => (
        <Space>
          {getFileIcon(record.fileType)}
          <span>{text}</span>
        </Space>
      ),
    },
    {
      title: '大小',
      dataIndex: 'fileSize',
      key: 'fileSize',
      width: 100,
      render: (size: number) => {
        const kb = size / 1024
        const mb = kb / 1024
        return mb >= 1 ? `${mb.toFixed(2)} MB` : `${kb.toFixed(2)} KB`
      },
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (status: DocumentStatus) => (
        <Tag color={statusColors[status]}>
          {statusTexts[status]}
        </Tag>
      ),
    },
    {
      title: '上传时间',
      dataIndex: 'createTime',
      key: 'createTime',
      width: 180,
      render: (time: string) => dayjs(time).format('YYYY-MM-DD HH:mm:ss'),
    },
    {
      title: '操作',
      key: 'actions',
      width: 150,
      render: (_, record) => (
        <Space>
          <Button
            type="link"
            size="small"
            icon={<EyeOutlined />}
            onClick={() => handlePreview(record)}
            disabled={record.status !== DocumentStatus.Completed}
          >
            预览
          </Button>
          <Button
            type="link"
            size="small"
            danger
            icon={<DeleteOutlined />}
            onClick={() => handleDelete(record)}
          >
            删除
          </Button>
        </Space>
      ),
    },
  ]

  const filteredData = data?.items.filter((doc) => {
    const matchesSearch = doc.name.toLowerCase().includes(searchText.toLowerCase())
    const matchesStatus = statusFilter === null || doc.status === statusFilter
    return matchesSearch && matchesStatus
  })

  return (
    <div className="document-page">
      <div className="document-header">
        <Title level={3}>文档管理</Title>
        <Space>
          <Input
            placeholder="搜索文档名称"
            prefix={<SearchOutlined />}
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            style={{ width: 240 }}
          />
          <Select
            placeholder="筛选状态"
            style={{ width: 120 }}
            allowClear
            value={statusFilter}
            onChange={setStatusFilter}
            options={[
              { label: '待处理', value: DocumentStatus.Pending },
              { label: '处理中', value: DocumentStatus.Processing },
              { label: '已完成', value: DocumentStatus.Completed },
              { label: '失败', value: DocumentStatus.Failed },
            ]}
          />
        </Space>
      </div>

      <Card className="upload-card">
        <Dragger
          name="file"
          multiple={false}
          beforeUpload={handleUpload}
          showUploadList={false}
          disabled={isUploading}
        >
          <p className="ant-upload-drag-icon">
            <InboxOutlined />
          </p>
          <p className="ant-upload-text">点击或拖拽文件到此区域上传</p>
          <p className="ant-upload-hint">
            支持 PDF、Word、TXT 格式，单个文件最大 50MB
          </p>
        </Dragger>

        {Object.keys(uploadProgress).length > 0 && (
          <div className="upload-progress-container">
            {Object.entries(uploadProgress).map(([key, info]) => (
              <div key={key} className="upload-progress-item">
                <Text ellipsis style={{ flex: 1 }}>
                  {info.file.name}
                </Text>
                <Progress
                  percent={info.progress}
                  status={info.status === 'error' ? 'exception' : info.status === 'success' ? 'success' : 'active'}
                  style={{ flex: 1, marginLeft: 16 }}
                />
              </div>
            ))}
          </div>
        )}
      </Card>

      <Table
        columns={columns}
        dataSource={filteredData}
        rowKey="id"
        loading={isLoading}
        pagination={{
          current: page,
          pageSize,
          total: data?.total || 0,
          onChange: (newPage, newPageSize) => {
            setPage(newPage)
            setPageSize(newPageSize || 20)
          },
          showSizeChanger: true,
          showTotal: (total) => `共 ${total} 个文档`,
        }}
      />

      {/* Preview Modal */}
      <Modal
        title="文档预览"
        open={!!previewDoc}
        onCancel={() => setPreviewDoc(null)}
        width={800}
        footer={null}
      >
        {previewDoc && (
          <div>
            <Text strong>文档名称：</Text> {previewDoc.name}
            <br />
            <Text strong>文件大小：</Text> {(previewDoc.fileSize / 1024 / 1024).toFixed(2)} MB
            <br />
            <Text strong>上传时间：</Text> {dayjs(previewDoc.createTime).format('YYYY-MM-DD HH:mm:ss')}
            <br />
            <div style={{ marginTop: 16 }}>
              {/* TODO: Add document preview iframe or PDF viewer */}
              <Text type="secondary">文档预览功能开发中...</Text>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

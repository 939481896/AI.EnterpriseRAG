import { useState } from 'react'
import {
  Button,
  Table,
  Space,
  Tag,
  Input,
  Select,
  Modal,
  Progress,
  Typography,
  Upload,
  Card,
} from 'antd'
import {
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
import { uiText, formatText } from '@/config/uiText'
import { notification } from '@/services/notification'
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
  [DocumentStatus.Pending]: uiText.document.pending,
  [DocumentStatus.Processing]: uiText.document.processing,
  [DocumentStatus.Completed]: uiText.document.completed,
  [DocumentStatus.Failed]: uiText.document.failed,
}

export default function DocumentPage() {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [searchText, setSearchText] = useState('')
  const [statusFilter, setStatusFilter] = useState<DocumentStatus | null>(null)
  const [previewDoc, setPreviewDoc] = useState<Document | null>(null)

  const { data, isLoading } = useDocuments(page, pageSize)
  const { upload, uploadProgress, isUploading } = useUploadDocument()
  const deleteDocument = useDeleteDocument()

  const handleUpload = (file: File) => {
    const isValidSize = file.size <= 50 * 1024 * 1024 // 50MB
    const isValidType = ['.pdf', '.docx', '.doc', '.txt'].some(ext => 
      file.name.toLowerCase().endsWith(ext)
    )

    if (!isValidSize) {
      notification.error(uiText.document.oversizeError)
      return false
    }

    if (!isValidType) {
      notification.error(uiText.document.typeError)
      return false
    }

    upload(file)
    return false // Prevent default upload
  }

  const handleDelete = (doc: Document) => {
    Modal.confirm({
      title: uiText.document.confirmDeleteTitle,
      content: formatText(uiText.document.confirmDeleteContent, { name: doc.name }),
      okText: uiText.common.delete,
      okType: 'danger',
      cancelText: uiText.common.cancel,
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
      title: uiText.document.colName,
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
      title: uiText.document.colSize,
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
      title: uiText.document.colStatus,
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
      title: uiText.document.colUploadTime,
      dataIndex: 'createTime',
      key: 'createTime',
      width: 180,
      render: (time: string) => dayjs(time).format('YYYY-MM-DD HH:mm:ss'),
    },
    {
      title: uiText.document.colActions,
      key: 'actions',
      width: 150,
      render: (_, record) => (
        <Space>
          <Button
            type="link"
            size="small"
            icon={<EyeOutlined />}
            onClick={() => { handlePreview(record); }}
            disabled={record.status !== DocumentStatus.Completed}
          >
            {uiText.common.preview}
          </Button>
          <Button
            type="link"
            size="small"
            danger
            icon={<DeleteOutlined />}
            onClick={() => { handleDelete(record); }}
          >
            {uiText.common.delete}
          </Button>
        </Space>
      ),
    },
  ]

  const filteredData = data?.items.filter((doc: Document) => {
    const matchesSearch = doc.name.toLowerCase().includes(searchText.toLowerCase())
    const matchesStatus = statusFilter === null || doc.status === statusFilter
    return matchesSearch && matchesStatus
  })

  return (
    <div className="document-page">
      <div className="document-header">
        <Title level={3}>{uiText.document.pageTitle}</Title>
        <Space>
          <Input
            placeholder={uiText.document.searchPlaceholder}
            prefix={<SearchOutlined />}
            value={searchText}
            onChange={(e) => { setSearchText(e.target.value); }}
            style={{ width: 240 }}
          />
          <Select
            placeholder={uiText.document.filterStatus}
            style={{ width: 120 }}
            allowClear
            value={statusFilter}
            onChange={setStatusFilter}
            options={[
              { label: uiText.document.pending, value: DocumentStatus.Pending },
              { label: uiText.document.processing, value: DocumentStatus.Processing },
              { label: uiText.document.completed, value: DocumentStatus.Completed },
              { label: uiText.document.failed, value: DocumentStatus.Failed },
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
          <p className="ant-upload-text">{uiText.document.uploadHint}</p>
          <p className="ant-upload-hint">
            {uiText.document.uploadSupport}
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
          showTotal: (total) => formatText(uiText.document.totalDocs, { total }),
        }}
      />

      {/* Preview Modal */}
      <Modal
        title={uiText.document.previewTitle}
        open={!!previewDoc}
        onCancel={() => { setPreviewDoc(null); }}
        width={800}
        footer={null}
      >
        {previewDoc && (
          <div>
            <Text strong>{uiText.document.fieldName}</Text> {previewDoc.name}
            <br />
            <Text strong>{uiText.document.fieldSize}</Text> {(previewDoc.fileSize / 1024 / 1024).toFixed(2)} MB
            <br />
            <Text strong>{uiText.document.fieldUploadTime}</Text> {dayjs(previewDoc.createTime).format('YYYY-MM-DD HH:mm:ss')}
            <br />
            <div style={{ marginTop: 16 }}>
              {/* TODO: Add document preview iframe or PDF viewer */}
              <Text type="secondary">{uiText.document.previewDeveloping}</Text>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

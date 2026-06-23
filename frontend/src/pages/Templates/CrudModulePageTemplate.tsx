import { useMemo, useState } from 'react'
import { Button, Card, Form, Input, Modal, Popconfirm, Space, Table } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons'

/**
 * CRUD page template for new business modules.
 *
 * Copy this file to a concrete page and replace:
 * - Domain types (TemplateItem / TemplateFormInput)
 * - Query hooks and mutation hooks
 * - Column definitions and text labels
 */

type TemplateItem = {
  id: string
  name: string
  description?: string
}

type TemplateFormInput = {
  name: string
  description?: string
}

export default function CrudModulePageTemplate() {
  const [form] = Form.useForm<TemplateFormInput>()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingItem, setEditingItem] = useState<TemplateItem | null>(null)

  // Replace with real query hook.
  const items: TemplateItem[] = []
  const isLoading = false

  // Replace with real mutations.
  const createPending = false
  const updatePending = false
  const deletePending = false

  const handleCreate = () => {
    setEditingItem(null)
    form.resetFields()
    setIsModalOpen(true)
  }

  const handleEdit = (item: TemplateItem) => {
    setEditingItem(item)
    form.setFieldsValue({ name: item.name, description: item.description })
    setIsModalOpen(true)
  }

  const handleDelete = (_id: string) => {
    // Replace with delete mutation.
  }

  const handleSubmit = async () => {
    const values = await form.validateFields()
    if (editingItem) {
      // Replace with update mutation.
      void values
      return
    }
    // Replace with create mutation.
    void values
  }

  const columns = useMemo<ColumnsType<TemplateItem>>(
    () => [
      {
        title: '名称',
        dataIndex: 'name',
        key: 'name',
      },
      {
        title: '描述',
        dataIndex: 'description',
        key: 'description',
      },
      {
        title: '操作',
        key: 'actions',
        render: (_, record) => (
          <Space>
            <Button type="link" icon={<EditOutlined />} onClick={() => { handleEdit(record); }}>
              编辑
            </Button>
            <Popconfirm
              title="确认删除"
              description="确定删除该记录吗？"
              onConfirm={() => { handleDelete(record.id); }}
            >
              <Button type="link" danger icon={<DeleteOutlined />} loading={deletePending}>
                删除
              </Button>
            </Popconfirm>
          </Space>
        ),
      },
    ],
    [deletePending]
  )

  return (
    <div className="page-container">
      <div className="page-header">
        <h3>模块标题</h3>
        <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
          新建
        </Button>
      </div>

      <Card>
        <Table
          rowKey="id"
          columns={columns}
          dataSource={items}
          loading={isLoading}
          pagination={{ pageSize: 10, showSizeChanger: true }}
        />
      </Card>

      <Modal
        title={editingItem ? '编辑记录' : '新建记录'}
        open={isModalOpen}
        onOk={handleSubmit}
        onCancel={() => { setIsModalOpen(false); }}
        confirmLoading={createPending || updatePending}
      >
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="名称" rules={[{ required: true, message: '请输入名称' }]}>
            <Input placeholder="请输入名称" />
          </Form.Item>
          <Form.Item name="description" label="描述">
            <Input.TextArea rows={3} placeholder="请输入描述（可选）" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}

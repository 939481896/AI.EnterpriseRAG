import React, { useState } from 'react'
import {
  Card,
  Table,
  Button,
  Space,
  Modal,
  Form,
  Input,
  Tag,
  Popconfirm,
  Collapse,
} from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  usePermissions,
  useCreatePermission,
  useUpdatePermission,
  useDeletePermission,
} from '@/hooks/usePermission'
import { PermissionGuard } from '@/contexts/PermissionContext'
import type { Permission } from '@/api/permission'
import { uiText, formatText } from '@/config/uiText'
import { useLocaleStore } from '@/store/localeStore'

const { Panel } = Collapse

const PermissionManagement: React.FC = () => {
  const [form] = Form.useForm()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingPermission, setEditingPermission] = useState<Permission | null>(null)
  const locale = useLocaleStore((state) => state.locale)

  const { data: permissions = [], isLoading } = usePermissions()
  const createPermission = useCreatePermission()
  const updatePermission = useUpdatePermission()
  const deletePermission = useDeletePermission()

  // Group permissions by module (memoized)
  const groupedPermissions = React.useMemo(() => {
    return permissions.reduce<Record<string, Permission[]>>((acc, perm) => {
      const module = perm.code.split('.')[0] || uiText.adminRole.othersModule
      if (!acc[module]) {
        acc[module] = []
      }
      acc[module].push(perm)
      return acc
    }, {})
  }, [permissions, locale])

  const handleCreate = () => {
    setEditingPermission(null)
    form.resetFields()
    setIsModalOpen(true)
  }

  const handleEdit = (permission: Permission) => {
    setEditingPermission(permission)
    form.setFieldsValue({
      code: permission.code,
      name: permission.name,
      description: permission.description,
    })
    setIsModalOpen(true)
  }

  const handleDelete = async (permissionId: number) => {
    await deletePermission.mutateAsync(permissionId)
  }

  // Memoize columns to prevent infinite re-renders
  const columns: ColumnsType<Permission> = React.useMemo(() => [
    {
      title: uiText.adminPermission.code,
      dataIndex: 'code',
      key: 'code',
      render: (code: string) => <Tag color="blue">{code}</Tag>,
    },
    {
      title: uiText.adminPermission.name,
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: uiText.adminPermission.description,
      dataIndex: 'description',
      key: 'description',
    },
    {
      title: uiText.adminPermission.roleCount,
      dataIndex: 'roleCount',
      key: 'roleCount',
      render: (count: number) => count || 0,
    },
    {
      title: uiText.adminUser.actions,
      key: 'action',
      render: (_, record) => (
        <Space size="small">
          <PermissionGuard permission="permission.update">
            <Button
              type="link"
              size="small"
              icon={<EditOutlined />}
              onClick={() => { handleEdit(record); }}
            >
              {uiText.common.edit}
            </Button>
          </PermissionGuard>
          <PermissionGuard permission="permission.delete">
            <Popconfirm
              title={uiText.common.delete}
              description={formatText(uiText.adminPermission.deleteConfirm, { name: record.name })}
              onConfirm={() => handleDelete(record.id)}
            >
              <Button type="link" size="small" danger icon={<DeleteOutlined />}>
                {uiText.common.delete}
              </Button>
            </Popconfirm>
          </PermissionGuard>
        </Space>
      ),
    },
  ], [locale])

  const handleModalOk = async () => {
    try {
      const values = await form.validateFields()

      if (editingPermission) {
        await updatePermission.mutateAsync({
          permissionId: editingPermission.id,
          data: values,
        })
      } else {
        await createPermission.mutateAsync(values)
      }

      setIsModalOpen(false)
      form.resetFields()
    } catch (error) {
      console.error('Form validation failed:', error)
    }
  }

  const handleModalCancel = () => {
    setIsModalOpen(false)
    form.resetFields()
  }

  return (
    <div style={{ padding: '24px' }}>
      <Card
        title={uiText.adminPermission.title}
        extra={
          <PermissionGuard permission="permission.create">
            <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
              {uiText.adminPermission.create}
            </Button>
          </PermissionGuard>
        }
      >
        <Collapse defaultActiveKey={Object.keys(groupedPermissions)}>
          {Object.entries(groupedPermissions).map(([module, perms]) => (
            <Panel
              header={
                <Space>
                  <Tag color="cyan">{module}</Tag>
                  <span>{formatText(uiText.adminPermission.moduleCount, { count: perms.length })}</span>
                </Space>
              }
              key={module}
            >
              <Table
                columns={columns}
                dataSource={perms}
                loading={isLoading}
                rowKey="id"
                pagination={false}
                size="small"
              />
            </Panel>
          ))}
        </Collapse>
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        title={editingPermission ? uiText.adminPermission.edit : uiText.adminPermission.create}
        open={isModalOpen}
        onOk={handleModalOk}
        onCancel={handleModalCancel}
        confirmLoading={createPermission.isPending || updatePermission.isPending}
      >
        <Form form={form} layout="vertical" style={{ marginTop: 24 }}>
          <Form.Item
            name="code"
            label={uiText.adminPermission.code}
            rules={[
              { required: true, message: uiText.adminPermission.inputCode },
              {
                pattern: /^[a-z]+\.[a-z_]+$/,
                message: uiText.adminPermission.codePattern,
              },
            ]}
          >
            <Input placeholder={uiText.adminPermission.codePlaceholder} />
          </Form.Item>

          <Form.Item
            name="name"
            label={uiText.adminPermission.name}
            rules={[{ required: true, message: uiText.adminPermission.inputName }]}
          >
            <Input placeholder={uiText.adminPermission.namePlaceholder} />
          </Form.Item>

          <Form.Item name="description" label={uiText.adminPermission.description}>
            <Input.TextArea rows={3} placeholder={uiText.adminPermission.descriptionPlaceholder} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}

export default PermissionManagement

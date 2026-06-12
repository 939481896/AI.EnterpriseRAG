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

const { Panel } = Collapse

const PermissionManagement: React.FC = () => {
  const [form] = Form.useForm()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingPermission, setEditingPermission] = useState<Permission | null>(null)

  const { data: permissions = [], isLoading } = usePermissions()
  const createPermission = useCreatePermission()
  const updatePermission = useUpdatePermission()
  const deletePermission = useDeletePermission()

  // Group permissions by module (memoized)
  const groupedPermissions = React.useMemo(() => {
    return permissions.reduce((acc, perm) => {
      const module = perm.code.split('.')[0] || '其他'
      if (!acc[module]) {
        acc[module] = []
      }
      acc[module].push(perm)
      return acc
    }, {} as Record<string, Permission[]>)
  }, [permissions])

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
      title: '权限代码',
      dataIndex: 'code',
      key: 'code',
      render: (code: string) => <Tag color="blue">{code}</Tag>,
    },
    {
      title: '权限名称',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
    },
    {
      title: '关联角色数',
      dataIndex: 'roleCount',
      key: 'roleCount',
      render: (count: number) => count || 0,
    },
    {
      title: '操作',
      key: 'action',
      render: (_, record) => (
        <Space size="small">
          <PermissionGuard permission="permission.update">
            <Button
              type="link"
              size="small"
              icon={<EditOutlined />}
              onClick={() => handleEdit(record)}
            >
              编辑
            </Button>
          </PermissionGuard>
          <PermissionGuard permission="permission.delete">
            <Popconfirm
              title="确认删除"
              description={`确定要删除权限 "${record.name}" 吗？`}
              onConfirm={() => handleDelete(record.id)}
            >
              <Button type="link" size="small" danger icon={<DeleteOutlined />}>
                删除
              </Button>
            </Popconfirm>
          </PermissionGuard>
        </Space>
      ),
    },
  ], []) // Empty deps since handlers are stable

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
        title="权限管理"
        extra={
          <PermissionGuard permission="permission.create">
            <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
              新建权限
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
                  <span>{perms.length} 个权限</span>
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
        title={editingPermission ? '编辑权限' : '新建权限'}
        open={isModalOpen}
        onOk={handleModalOk}
        onCancel={handleModalCancel}
        confirmLoading={createPermission.isPending || updatePermission.isPending}
      >
        <Form form={form} layout="vertical" style={{ marginTop: 24 }}>
          <Form.Item
            name="code"
            label="权限代码"
            rules={[
              { required: true, message: '请输入权限代码' },
              {
                pattern: /^[a-z]+\.[a-z_]+$/,
                message: '格式：模块.操作，如 user.create',
              },
            ]}
          >
            <Input placeholder="如：user.create" />
          </Form.Item>

          <Form.Item
            name="name"
            label="权限名称"
            rules={[{ required: true, message: '请输入权限名称' }]}
          >
            <Input placeholder="如：创建用户" />
          </Form.Item>

          <Form.Item name="description" label="描述">
            <Input.TextArea rows={3} placeholder="权限描述（可选）" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}

export default PermissionManagement

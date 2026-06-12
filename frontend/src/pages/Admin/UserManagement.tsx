import React, { useState } from 'react'
import {
  Table,
  Button,
  Space,
  Modal,
  Form,
  Input,
  Switch,
  Typography,
  message,
  Popconfirm,
  Drawer,
  Checkbox,
  Tag,
} from 'antd'
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  UserOutlined,
  MailOutlined,
  PhoneOutlined,
  TeamOutlined,
  SafetyOutlined,
} from '@ant-design/icons'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type { ColumnsType } from 'antd/es/table'
import type { User } from '@/types/auth'
import { userApi } from '@/api/user'
import { useRoles, useUserRoles, useAssignUserRoles } from '@/hooks/usePermission'
import { PermissionGuard } from '@/contexts/PermissionContext'
import dayjs from 'dayjs'
import './UserManagement.css'

const { Title } = Typography

export default function UserManagement() {
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<User | null>(null)
  const [isRoleDrawerOpen, setIsRoleDrawerOpen] = useState(false)
  const [currentUser, setCurrentUser] = useState<User | null>(null)
  const [selectedRoleIds, setSelectedRoleIds] = useState<number[]>([])
  const [form] = Form.useForm()
  const queryClient = useQueryClient()

  // Fetch users
  const { data: users, isLoading } = useQuery({
    queryKey: ['users'],
    queryFn: async () => {
      const response = await userApi.getUsers(1, 100)
      return response.data?.items || []
    },
  })

  // Fetch all roles
  const { data: allRoles = [] } = useRoles()

  // Fetch user roles when drawer opens
  const { data: userRoles = [] } = useUserRoles(currentUser?.id || null)

  // Assign roles mutation
  const assignRoles = useAssignUserRoles()

  // Create user mutation
  const createUserMutation = useMutation({
    mutationFn: (values: any) => userApi.createUser(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      message.success('用户已添加')
      setIsModalOpen(false)
      form.resetFields()
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || '添加失败')
    },
  })

  // Update user mutation
  const updateUserMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) =>
      userApi.updateUser(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      message.success('用户信息已更新')
      setIsModalOpen(false)
      form.resetFields()
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || '更新失败')
    },
  })

  // Delete user mutation
  const deleteUserMutation = useMutation({
    mutationFn: (userId: string) => userApi.deleteUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      message.success('用户已删除')
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || '删除失败')
    },
  })

  // Toggle status mutation
  const toggleStatusMutation = useMutation({
    mutationFn: ({ userId, isActive }: { userId: string; isActive: boolean }) =>
      userApi.toggleUserStatus(userId, isActive),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      message.success(variables.isActive ? '用户已启用' : '用户已禁用')
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || '操作失败')
    },
  })

  const handleAdd = () => {
    setEditingUser(null)
    form.resetFields()
    setIsModalOpen(true)
  }

  const handleEdit = (user: User) => {
    setEditingUser(user)
    form.setFieldsValue(user)
    setIsModalOpen(true)
  }

  const handleDelete = (userId: string) => {
    deleteUserMutation.mutate(userId)
  }

  const handleToggleStatus = (userId: string, isActive: boolean) => {
    toggleStatusMutation.mutate({ userId, isActive })
  }

  const handleModalOk = async () => {
    try {
      const values = await form.validateFields()

      if (editingUser) {
        // Update user (exclude password)
        const { password, ...updateData } = values
        updateUserMutation.mutate({
          id: editingUser.id,
          data: updateData,
        })
      } else {
        // Create user
        createUserMutation.mutate(values)
      }
    } catch (error) {
      console.error('Validation failed:', error)
    }
  }

  const handleModalCancel = () => {
    setIsModalOpen(false)
    form.resetFields()
  }

  const handleAssignRoles = (user: User) => {
    setCurrentUser(user)
    setIsRoleDrawerOpen(true)
  }

  const handleRoleDrawerClose = () => {
    setIsRoleDrawerOpen(false)
    setCurrentUser(null)
    setSelectedRoleIds([])
  }

  const handleSaveRoles = async () => {
    if (!currentUser) return

    await assignRoles.mutateAsync({
      userId: Number(currentUser.id),
      roleIds: selectedRoleIds,
    })

    handleRoleDrawerClose()
  }

  // Update selected roles when user roles are loaded
  React.useEffect(() => {
    if (userRoles.length > 0) {
      setSelectedRoleIds(userRoles.map((r: any) => r.id))
    } else {
      setSelectedRoleIds([])
    }
  }, [userRoles])

  // Memoize columns to prevent infinite re-renders
  const columns: ColumnsType<User> = React.useMemo(() => [
    {
      title: '账号',
      dataIndex: 'account',
      key: 'account',
      render: (text) => (
        <Space>
          <UserOutlined />
          <span>{text}</span>
        </Space>
      ),
    },
    {
      title: '真实姓名',
      dataIndex: 'realName',
      key: 'realName',
    },
    {
      title: '邮箱',
      dataIndex: 'email',
      key: 'email',
      ellipsis: true,
    },
    {
      title: '部门',
      dataIndex: 'department',
      key: 'department',
    },
    {
      title: '状态',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (isActive: boolean, record) => (
        <Switch
          checked={isActive}
          onChange={(checked) => handleToggleStatus(record.id, checked)}
          checkedChildren="启用"
          unCheckedChildren="禁用"
          loading={toggleStatusMutation.isPending}
        />
      ),
    },
    {
      title: '创建时间',
      dataIndex: 'createTime',
      key: 'createTime',
      render: (time: string) => dayjs(time).format('YYYY-MM-DD'),
    },
    {
      title: '操作',
      key: 'actions',
      render: (_, record) => (
        <Space>
          <PermissionGuard permission="user.update">
            <Button
              type="link"
              size="small"
              icon={<SafetyOutlined />}
              onClick={() => handleAssignRoles(record)}
            >
              分配角色
            </Button>
          </PermissionGuard>
          <PermissionGuard permission="user.update">
            <Button
              type="link"
              size="small"
              icon={<EditOutlined />}
              onClick={() => handleEdit(record)}
            >
              编辑
            </Button>
          </PermissionGuard>
          <PermissionGuard permission="user.delete">
            <Popconfirm
              title="确认删除"
              description="确定要删除该用户吗？"
              onConfirm={() => handleDelete(record.id)}
              okText="删除"
              cancelText="取消"
              okType="danger"
            >
              <Button
                type="link"
                size="small"
                danger
                icon={<DeleteOutlined />}
                loading={deleteUserMutation.isPending}
              >
                删除
              </Button>
            </Popconfirm>
          </PermissionGuard>
        </Space>
      ),
    },
  ], [toggleStatusMutation.isPending, deleteUserMutation.isPending]) // Add dependencies for loading states

  return (
    <div className="page-container">
      <div className="page-header">
        <h3>用户管理</h3>
        <PermissionGuard permission="user.create">
          <Button type="primary" icon={<PlusOutlined />} onClick={handleAdd}>
            添加用户
          </Button>
        </PermissionGuard>
      </div>

      <Table
        columns={columns}
        dataSource={users}
        rowKey="id"
        loading={isLoading}
        pagination={{
          pageSize: 10,
          showSizeChanger: true,
          showTotal: (total) => `共 ${total} 个用户`,
        }}
      />

      <Modal
        title={editingUser ? '编辑用户' : '添加用户'}
        open={isModalOpen}
        onOk={handleModalOk}
        onCancel={handleModalCancel}
        width={600}
        okText="确定"
        cancelText="取消"
        confirmLoading={createUserMutation.isPending || updateUserMutation.isPending}
      >
        <Form form={form} layout="vertical" autoComplete="off">
          <Form.Item
            name="account"
            label="账号"
            rules={[
              { required: true, message: '请输入账号' },
              { min: 3, message: '账号至少3个字符' },
              { pattern: /^[a-zA-Z0-9_]+$/, message: '只能包含字母、数字、下划线' },
            ]}
          >
            <Input
              prefix={<UserOutlined />}
              placeholder="账号"
              disabled={!!editingUser}
            />
          </Form.Item>

          {!editingUser && (
            <Form.Item
              name="password"
              label="密码"
              rules={[
                { required: true, message: '请输入密码' },
                { min: 6, message: '密码至少6个字符' },
              ]}
            >
              <Input.Password placeholder="密码" />
            </Form.Item>
          )}

          <Form.Item
            name="realName"
            label="真实姓名"
            rules={[{ required: true, message: '请输入真实姓名' }]}
          >
            <Input prefix={<UserOutlined />} placeholder="真实姓名" />
          </Form.Item>

          <Form.Item
            name="email"
            label="邮箱"
            rules={[
              { required: true, message: '请输入邮箱' },
              { type: 'email', message: '邮箱格式不正确' },
            ]}
          >
            <Input prefix={<MailOutlined />} placeholder="邮箱地址" />
          </Form.Item>

          <Form.Item name="phone" label="手机号">
            <Input prefix={<PhoneOutlined />} placeholder="手机号（可选）" />
          </Form.Item>

          <Form.Item name="department" label="部门">
            <Input prefix={<TeamOutlined />} placeholder="所属部门（可选）" />
          </Form.Item>
        </Form>
      </Modal>

      {/* Role Assignment Drawer */}
      <Drawer
        title={`为 "${currentUser?.realName}" 分配角色`}
        placement="right"
        width={400}
        open={isRoleDrawerOpen}
        onClose={handleRoleDrawerClose}
        extra={
          <Space>
            <Button onClick={handleRoleDrawerClose}>取消</Button>
            <Button
              type="primary"
              onClick={handleSaveRoles}
              loading={assignRoles.isPending}
            >
              保存
            </Button>
          </Space>
        }
      >
        <div style={{ marginBottom: 16 }}>
          <Typography.Text type="secondary">
            当前已分配 {selectedRoleIds.length} 个角色
          </Typography.Text>
        </div>
        <Checkbox.Group
          style={{ width: '100%', display: 'flex', flexDirection: 'column', gap: '12px' }}
          value={selectedRoleIds}
          onChange={(checkedValues) => setSelectedRoleIds(checkedValues as number[])}
        >
          {allRoles.map((role: any) => (
            <Checkbox key={role.id} value={role.id}>
              <Space>
                <Tag color={role.roleCode === 'admin' ? 'red' : 'blue'}>{role.roleCode}</Tag>
                <span>{role.roleName}</span>
              </Space>
              {role.description && (
                <div style={{ marginLeft: 24, fontSize: 12, color: '#999' }}>
                  {role.description}
                </div>
              )}
            </Checkbox>
          ))}
        </Checkbox.Group>
      </Drawer>
    </div>
  )
}

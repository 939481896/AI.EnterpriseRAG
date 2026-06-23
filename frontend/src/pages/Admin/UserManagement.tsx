import React, { useState } from 'react'
import {
  Table,
  Button,
  Space,
  Modal,
  Form,
  Input,
  Switch,
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
import type { Role } from '@/api/permission'
import dayjs from 'dayjs'
import { uiText, formatText } from '@/config/uiText'
import { notification } from '@/services/notification'
import { getErrorMessage } from '@/types/error'
import { queryKeys } from '@/config/queryKeys'
import { useLocaleStore } from '@/store/localeStore'

type CreateUserInput = {
  account: string
  password: string
  realName: string
  email: string
  phone?: string
  department?: string
}

type UpdateUserInput = {
  realName: string
  email: string
  phone?: string
  department?: string
}

export default function UserManagement() {
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<User | null>(null)
  const [isRoleDrawerOpen, setIsRoleDrawerOpen] = useState(false)
  const [currentUser, setCurrentUser] = useState<User | null>(null)
  const [selectedRoleIds, setSelectedRoleIds] = useState<number[]>([])
  const [form] = Form.useForm()
  const locale = useLocaleStore((state) => state.locale)
  const queryClient = useQueryClient()

  // Fetch users
  const { data: users, isLoading } = useQuery({
    queryKey: queryKeys.user.list,
    queryFn: async () => {
      const response = await userApi.getUsers(1, 100)
      return response.data?.items || []
    },
  })

  // Fetch all roles
  const { data: allRoles = [] } = useRoles()

  // Fetch user roles when drawer opens
  const { data: userRoles = [] } = useUserRoles(currentUser?.id ? Number(currentUser.id) : null)

  // Assign roles mutation
  const assignRoles = useAssignUserRoles()

  // Create user mutation
  const createUserMutation = useMutation({
    meta: { silentError: true },
    mutationFn: (values: CreateUserInput) => userApi.createUser(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.user.list })
      notification.success(uiText.adminUser.userAdded)
      setIsModalOpen(false)
      form.resetFields()
    },
    onError: (error: unknown) => {
      notification.error(getErrorMessage(error) || uiText.adminUser.addFailed)
    },
  })

  // Update user mutation
  const updateUserMutation = useMutation({
    meta: { silentError: true },
    mutationFn: ({ id, data }: { id: string; data: UpdateUserInput }) =>
      userApi.updateUser(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.user.list })
      notification.success(uiText.adminUser.userUpdated)
      setIsModalOpen(false)
      form.resetFields()
    },
    onError: (error: unknown) => {
      notification.error(getErrorMessage(error) || uiText.adminUser.updateFailed)
    },
  })

  // Delete user mutation
  const deleteUserMutation = useMutation({
    meta: { silentError: true },
    mutationFn: (userId: string) => userApi.deleteUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.user.list })
      notification.success(uiText.adminUser.userDeleted)
    },
    onError: (error: unknown) => {
      notification.error(getErrorMessage(error) || uiText.adminUser.userDeleteFailed)
    },
  })

  // Toggle status mutation
  const toggleStatusMutation = useMutation({
    meta: { silentError: true },
    mutationFn: ({ userId, isActive }: { userId: string; isActive: boolean }) =>
      userApi.toggleUserStatus(userId, isActive),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.user.list })
      notification.success(variables.isActive ? uiText.adminUser.userEnabled : uiText.adminUser.userDisabled)
    },
    onError: (error: unknown) => {
      notification.error(getErrorMessage(error) || uiText.common.operationFailed)
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
        const { password: _password, ...updateData } = values
        if (!editingUser.id) {
          notification.error(uiText.common.operationFailed)
          return
        }

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
      setSelectedRoleIds(userRoles.map((r: Role) => r.id))
    } else {
      setSelectedRoleIds([])
    }
  }, [userRoles])

  // Memoize columns to prevent infinite re-renders
  const columns: ColumnsType<User> = React.useMemo(() => [
    {
      title: uiText.adminUser.account,
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
      title: uiText.adminUser.realName,
      dataIndex: 'realName',
      key: 'realName',
    },
    {
      title: uiText.adminUser.email,
      dataIndex: 'email',
      key: 'email',
      ellipsis: true,
    },
    {
      title: uiText.adminUser.department,
      dataIndex: 'department',
      key: 'department',
    },
    {
      title: uiText.adminUser.status,
      dataIndex: 'isActive',
      key: 'isActive',
      render: (isActive: boolean, record) => (
        <Switch
          checked={isActive}
          onChange={(checked) => record.id && handleToggleStatus(record.id, checked)}
          checkedChildren={uiText.adminUser.enable}
          unCheckedChildren={uiText.adminUser.disable}
          loading={toggleStatusMutation.isPending}
        />
      ),
    },
    {
      title: uiText.adminUser.createTime,
      dataIndex: 'createTime',
      key: 'createTime',
      render: (time: string) => dayjs(time).format('YYYY-MM-DD'),
    },
    {
      title: uiText.adminUser.actions,
      key: 'actions',
      render: (_, record) => (
        <Space>
          <PermissionGuard permission="user.update">
            <Button
              type="link"
              size="small"
              icon={<SafetyOutlined />}
              onClick={() => { handleAssignRoles(record); }}
            >
              {uiText.adminUser.assignRole}
            </Button>
          </PermissionGuard>
          <PermissionGuard permission="user.update">
            <Button
              type="link"
              size="small"
              icon={<EditOutlined />}
              onClick={() => { handleEdit(record); }}
            >
              {uiText.common.edit}
            </Button>
          </PermissionGuard>
          <PermissionGuard permission="user.delete">
            <Popconfirm
              title={uiText.adminUser.confirmDeleteTitle}
              description={uiText.adminUser.confirmDeleteContent}
              onConfirm={() => record.id && handleDelete(record.id)}
              okText={uiText.common.delete}
              cancelText={uiText.common.cancel}
              okType="danger"
            >
              <Button
                type="link"
                size="small"
                danger
                icon={<DeleteOutlined />}
                loading={deleteUserMutation.isPending}
              >
                {uiText.common.delete}
              </Button>
            </Popconfirm>
          </PermissionGuard>
        </Space>
      ),
    },
  ], [toggleStatusMutation.isPending, deleteUserMutation.isPending, locale])

  return (
    <div className="page-container">
      <div className="page-header">
        <h3>{uiText.adminUser.title}</h3>
        <PermissionGuard permission="user.create">
          <Button type="primary" icon={<PlusOutlined />} onClick={handleAdd}>
            {uiText.adminUser.addUser}
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
          showTotal: (total) => formatText(uiText.adminUser.totalUsers, { total }),
        }}
      />

      <Modal
        title={editingUser ? uiText.adminUser.editUser : uiText.adminUser.createUser}
        open={isModalOpen}
        onOk={handleModalOk}
        onCancel={handleModalCancel}
        width={600}
        okText={uiText.common.confirm}
        cancelText={uiText.common.cancel}
        confirmLoading={createUserMutation.isPending || updateUserMutation.isPending}
      >
        <Form form={form} layout="vertical" autoComplete="off">
          <Form.Item
            name="account"
            label={uiText.adminUser.account}
            rules={[
              { required: true, message: uiText.adminUser.inputAccount },
              { min: 3, message: uiText.adminUser.accountMin },
              { pattern: /^[a-zA-Z0-9_]+$/, message: uiText.adminUser.accountPattern },
            ]}
          >
            <Input
              prefix={<UserOutlined />}
              placeholder={uiText.adminUser.accountPlaceholder}
              disabled={!!editingUser}
            />
          </Form.Item>

          {!editingUser && (
            <Form.Item
              name="password"
              label={uiText.auth.password}
              rules={[
                { required: true, message: uiText.adminUser.inputPassword },
                { min: 6, message: uiText.adminUser.passwordMin },
              ]}
            >
              <Input.Password placeholder={uiText.adminUser.passwordPlaceholder} />
            </Form.Item>
          )}

          <Form.Item
            name="realName"
            label={uiText.adminUser.realName}
            rules={[{ required: true, message: uiText.adminUser.inputRealName }]}
          >
            <Input prefix={<UserOutlined />} placeholder={uiText.adminUser.realNamePlaceholder} />
          </Form.Item>

          <Form.Item
            name="email"
            label={uiText.adminUser.email}
            rules={[
              { required: true, message: uiText.adminUser.inputEmail },
              { type: 'email', message: uiText.adminUser.emailInvalid },
            ]}
          >
            <Input prefix={<MailOutlined />} placeholder={uiText.adminUser.emailPlaceholder} />
          </Form.Item>

          <Form.Item name="phone" label={uiText.auth.phone}>
            <Input prefix={<PhoneOutlined />} placeholder={uiText.adminUser.phonePlaceholder} />
          </Form.Item>

          <Form.Item name="department" label={uiText.adminUser.department}>
            <Input prefix={<TeamOutlined />} placeholder={uiText.adminUser.departmentPlaceholder} />
          </Form.Item>
        </Form>
      </Modal>

      {/* Role Assignment Drawer */}
      <Drawer
        title={formatText(uiText.adminUser.assignRoleDrawerTitle, { name: currentUser?.userName || currentUser?.account || '' })}
        placement="right"
        width={400}
        open={isRoleDrawerOpen}
        onClose={handleRoleDrawerClose}
        extra={
          <Space>
            <Button onClick={handleRoleDrawerClose}>{uiText.common.cancel}</Button>
            <Button
              type="primary"
              onClick={handleSaveRoles}
              loading={assignRoles.isPending}
            >
              {uiText.common.save}
            </Button>
          </Space>
        }
      >
        <div style={{ marginBottom: 16 }}>
          <span style={{ color: '#666' }}>
            {formatText(uiText.adminUser.assignedRoleCount, { count: selectedRoleIds.length })}
          </span>
        </div>
        <Checkbox.Group
          style={{ width: '100%', display: 'flex', flexDirection: 'column', gap: '12px' }}
          value={selectedRoleIds}
          onChange={(checkedValues) => { setSelectedRoleIds(checkedValues); }}
        >
          {allRoles.map((role: Role) => (
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

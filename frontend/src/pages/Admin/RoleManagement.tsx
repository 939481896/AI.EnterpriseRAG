import React, { useState, useEffect } from 'react'
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
  Drawer,
  Tree,
  message,
  Spin,
} from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined, KeyOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  useRoles,
  useRole,
  useCreateRole,
  useUpdateRole,
  useDeleteRole,
  useAssignRolePermissions,
  useGroupedPermissions,
} from '@/hooks/usePermission'
import { PermissionGuard } from '@/contexts/PermissionContext'
import type { Role } from '@/api/permission'

const RoleManagement: React.FC = () => {
  const [form] = Form.useForm()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [isPermissionDrawerOpen, setIsPermissionDrawerOpen] = useState(false)
  const [editingRole, setEditingRole] = useState<Role | null>(null)
  const [currentRole, setCurrentRole] = useState<Role | null>(null)
  const [currentRoleId, setCurrentRoleId] = useState<number | null>(null)
  const [selectedPermissions, setSelectedPermissions] = useState<number[]>([])

  const { data: roles = [], isLoading } = useRoles()
  const { data: roleDetail, isLoading: isLoadingRoleDetail } = useRole(currentRoleId)
  const { data: groupedPermissions = {}, isLoading: isLoadingPermissions } = useGroupedPermissions()
  const createRole = useCreateRole()
  const updateRole = useUpdateRole()
  const deleteRole = useDeleteRole()
  const assignPermissions = useAssignRolePermissions()

  // 当角色详情加载完成后，设置已选中的权限
  useEffect(() => {
    if (roleDetail && roleDetail.permissions) {
      const permissionIds = roleDetail.permissions.map((p: any) => p.id)
      console.log('Setting selected permissions from role detail:', permissionIds)
      setSelectedPermissions(permissionIds)
    }
  }, [roleDetail])

  // Convert grouped permissions to tree data with safety checks
  const permissionTreeData = React.useMemo(() => {
    if (!groupedPermissions || typeof groupedPermissions !== 'object') {
      console.warn('groupedPermissions is not an object:', groupedPermissions)
      return []
    }

    return Object.entries(groupedPermissions).map(([module, permissions]) => {
      // Ensure permissions is an array
      const permArray = Array.isArray(permissions) ? permissions : []

      return {
        title: module || '其他',
        key: module,
        selectable: false,
        children: permArray.map((p: any) => ({
          title: `${p.name} (${p.code})`,
          key: p.id,
          isLeaf: true,
        })),
      }
    })
  }, [groupedPermissions])

  const handleCreate = () => {
    setEditingRole(null)
    form.resetFields()
    setIsModalOpen(true)
  }

  const handleEdit = (role: Role) => {
    setEditingRole(role)
    form.setFieldsValue({
      roleName: role.roleName,
      roleCode: role.roleCode,
      description: role.description,
    })
    setIsModalOpen(true)
  }

  const handleDelete = async (roleId: number) => {
    await deleteRole.mutateAsync(roleId)
  }

  const handleAssignPermissions = (role: Role) => {
    console.log('Opening permission drawer for role:', role)

    setCurrentRole(role)
    setCurrentRoleId(role.id) // 触发加载角色详情
    setIsPermissionDrawerOpen(true)
  }

  // Memoize columns to prevent infinite re-renders
  const columns: ColumnsType<Role> = React.useMemo(() => [
    {
      title: '角色名称',
      dataIndex: 'roleName',
      key: 'roleName',
    },
    {
      title: '角色代码',
      dataIndex: 'roleCode',
      key: 'roleCode',
      render: (code: string) => <Tag color="blue">{code}</Tag>,
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
    },
    {
      title: '用户数',
      dataIndex: 'userCount',
      key: 'userCount',
      render: (count: number) => count || 0,
    },
    {
      title: '权限数',
      dataIndex: 'permissionCount',
      key: 'permissionCount',
      render: (count: number) => count || 0,
    },
    {
      title: '操作',
      key: 'action',
      render: (_, record) => (
        <Space size="small">
          <PermissionGuard permission="role.update">
            <Button
              type="link"
              size="small"
              icon={<KeyOutlined />}
              onClick={() => handleAssignPermissions(record)}
            >
              分配权限
            </Button>
          </PermissionGuard>
          <PermissionGuard permission="role.update">
            <Button
              type="link"
              size="small"
              icon={<EditOutlined />}
              onClick={() => handleEdit(record)}
              disabled={record.roleCode === 'admin'}
            >
              编辑
            </Button>
          </PermissionGuard>
          <PermissionGuard permission="role.delete">
            <Popconfirm
              title="确认删除"
              description={`确定要删除角色 "${record.roleName}" 吗？`}
              onConfirm={() => handleDelete(record.id)}
              disabled={record.roleCode === 'admin'}
            >
              <Button
                type="link"
                size="small"
                danger
                icon={<DeleteOutlined />}
                disabled={record.roleCode === 'admin'}
              >
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

      if (editingRole) {
        await updateRole.mutateAsync({
          roleId: editingRole.id,
          data: values,
        })
      } else {
        await createRole.mutateAsync(values)
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

  const handlePermissionDrawerClose = React.useCallback(() => {
    setIsPermissionDrawerOpen(false)
    setCurrentRole(null)
    setCurrentRoleId(null) // 清除角色ID，停止加载详情
    setSelectedPermissions([])
  }, [])

  const handleSavePermissions = async () => {
    if (!currentRole) return

    await assignPermissions.mutateAsync({
      roleId: currentRole.id,
      permissionIds: selectedPermissions,
    })

    handlePermissionDrawerClose()
  }

  return (
    <div style={{ padding: '24px' }}>
      <Card
        title="角色管理"
        extra={
          <PermissionGuard permission="role.create">
            <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
              新建角色
            </Button>
          </PermissionGuard>
        }
      >
        <Table
          columns={columns}
          dataSource={roles}
          loading={isLoading}
          rowKey="id"
          pagination={{
            pageSize: 10,
            showSizeChanger: true,
            showTotal: (total) => `共 ${total} 条`,
          }}
        />
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        title={editingRole ? '编辑角色' : '新建角色'}
        open={isModalOpen}
        onOk={handleModalOk}
        onCancel={handleModalCancel}
        confirmLoading={createRole.isPending || updateRole.isPending}
      >
        <Form form={form} layout="vertical" style={{ marginTop: 24 }}>
          <Form.Item
            name="roleName"
            label="角色名称"
            rules={[{ required: true, message: '请输入角色名称' }]}
          >
            <Input placeholder="如：部门经理" />
          </Form.Item>

          <Form.Item
            name="roleCode"
            label="角色代码"
            rules={[
              { required: true, message: '请输入角色代码' },
              { pattern: /^[a-z_]+$/, message: '仅支持小写字母和下划线' },
            ]}
          >
            <Input
              placeholder="如：dept_manager"
              disabled={!!editingRole && editingRole.roleCode === 'admin'}
            />
          </Form.Item>

          <Form.Item name="description" label="描述">
            <Input.TextArea rows={3} placeholder="角色描述（可选）" />
          </Form.Item>
        </Form>
      </Modal>

      {/* Permission Assignment Drawer */}
      <Drawer
        title={`为 "${currentRole?.roleName}" 分配权限`}
        placement="right"
        width={500}
        open={isPermissionDrawerOpen}
        onClose={handlePermissionDrawerClose}
        extra={
          <Space>
            <Button onClick={handlePermissionDrawerClose}>取消</Button>
            <Button
              type="primary"
              onClick={handleSavePermissions}
              loading={assignPermissions.isPending}
            >
              保存
            </Button>
          </Space>
        }
      >
        {isLoadingPermissions || isLoadingRoleDetail ? (
          <div style={{ textAlign: 'center', padding: '40px 0' }}>
            <Spin />
            <div style={{ marginTop: 16 }}>
              {isLoadingRoleDetail ? '加载角色权限中...' : '加载权限数据中...'}
            </div>
          </div>
        ) : permissionTreeData.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '40px 0', color: '#999' }}>
            <p>暂无权限数据</p>
            <p style={{ fontSize: 12 }}>请检查后端权限是否已初始化</p>
          </div>
        ) : (
          <>
            <div style={{ marginBottom: 16, color: '#666' }}>
              已选择 {selectedPermissions.length} 个权限
            </div>
            <Tree
              checkable
              defaultExpandAll
              treeData={permissionTreeData}
              checkedKeys={selectedPermissions}
              onCheck={(checkedKeys) => {
                // Filter out parent keys (module names)
                const leafKeys = (checkedKeys as number[]).filter((key) => typeof key === 'number')
                setSelectedPermissions(leafKeys)
              }}
            />
          </>
        )}
      </Drawer>
    </div>
  )
}

export default RoleManagement

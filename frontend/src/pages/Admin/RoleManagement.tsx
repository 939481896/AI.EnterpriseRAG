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
import type { Permission, Role } from '@/api/permission'
import { uiText, formatText } from '@/config/uiText'
import { useLocaleStore } from '@/store/localeStore'

const RoleManagement: React.FC = () => {
  const [form] = Form.useForm()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [isPermissionDrawerOpen, setIsPermissionDrawerOpen] = useState(false)
  const [editingRole, setEditingRole] = useState<Role | null>(null)
  const [currentRole, setCurrentRole] = useState<Role | null>(null)
  const [currentRoleId, setCurrentRoleId] = useState<number | null>(null)
  const [selectedPermissions, setSelectedPermissions] = useState<number[]>([])
  const locale = useLocaleStore((state) => state.locale)

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
      const permissionIds = roleDetail.permissions.map((p: Permission) => p.id)
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
        title: module || uiText.adminRole.othersModule,
        key: module,
        selectable: false,
        children: permArray.map((p: Permission) => ({
          title: `${p.name} (${p.code})`,
          key: p.id,
          isLeaf: true,
        })),
      }
    })
  }, [groupedPermissions, locale])

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
      title: uiText.adminRole.roleName,
      dataIndex: 'roleName',
      key: 'roleName',
    },
    {
      title: uiText.adminRole.roleCode,
      dataIndex: 'roleCode',
      key: 'roleCode',
      render: (code: string) => <Tag color="blue">{code}</Tag>,
    },
    {
      title: uiText.adminRole.description,
      dataIndex: 'description',
      key: 'description',
    },
    {
      title: uiText.adminRole.userCount,
      dataIndex: 'userCount',
      key: 'userCount',
      render: (count: number) => count || 0,
    },
    {
      title: uiText.adminRole.permissionCount,
      dataIndex: 'permissionCount',
      key: 'permissionCount',
      render: (count: number) => count || 0,
    },
    {
      title: uiText.adminUser.actions,
      key: 'action',
      render: (_, record) => (
        <Space size="small">
          <PermissionGuard permission="role.update">
            <Button
              type="link"
              size="small"
              icon={<KeyOutlined />}
              onClick={() => { handleAssignPermissions(record); }}
            >
              {uiText.adminRole.assignPermissions}
            </Button>
          </PermissionGuard>
          <PermissionGuard permission="role.update">
            <Button
              type="link"
              size="small"
              icon={<EditOutlined />}
              onClick={() => { handleEdit(record); }}
              disabled={record.roleCode === 'admin'}
            >
              {uiText.common.edit}
            </Button>
          </PermissionGuard>
          <PermissionGuard permission="role.delete">
            <Popconfirm
              title={uiText.common.delete}
              description={formatText(uiText.adminRole.deleteConfirm, { name: record.roleName })}
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
        title={uiText.adminRole.title}
        extra={
          <PermissionGuard permission="role.create">
            <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
              {uiText.adminRole.create}
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
            showTotal: (total) => formatText(uiText.adminRole.totalRows, { total }),
          }}
        />
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        title={editingRole ? uiText.adminRole.edit : uiText.adminRole.create}
        open={isModalOpen}
        onOk={handleModalOk}
        onCancel={handleModalCancel}
        confirmLoading={createRole.isPending || updateRole.isPending}
      >
        <Form form={form} layout="vertical" style={{ marginTop: 24 }}>
          <Form.Item
            name="roleName"
            label={uiText.adminRole.roleName}
            rules={[{ required: true, message: uiText.adminRole.inputRoleName }]}
          >
            <Input placeholder={uiText.adminRole.roleNamePlaceholder} />
          </Form.Item>

          <Form.Item
            name="roleCode"
            label={uiText.adminRole.roleCode}
            rules={[
              { required: true, message: uiText.adminRole.inputRoleCode },
              { pattern: /^[a-z_]+$/, message: uiText.adminRole.roleCodePattern },
            ]}
          >
            <Input
              placeholder={uiText.adminRole.roleCodePlaceholder}
              disabled={!!editingRole && editingRole.roleCode === 'admin'}
            />
          </Form.Item>

          <Form.Item name="description" label={uiText.adminRole.description}>
            <Input.TextArea rows={3} placeholder={uiText.adminRole.descriptionPlaceholder} />
          </Form.Item>
        </Form>
      </Modal>

      {/* Permission Assignment Drawer */}
      <Drawer
        title={formatText(uiText.adminRole.assignDrawerTitle, { name: currentRole?.roleName || '' })}
        placement="right"
        width={500}
        open={isPermissionDrawerOpen}
        onClose={handlePermissionDrawerClose}
        extra={
          <Space>
            <Button onClick={handlePermissionDrawerClose}>{uiText.common.cancel}</Button>
            <Button
              type="primary"
              onClick={handleSavePermissions}
              loading={assignPermissions.isPending}
            >
              {uiText.common.save}
            </Button>
          </Space>
        }
      >
        {isLoadingPermissions || isLoadingRoleDetail ? (
          <div style={{ textAlign: 'center', padding: '40px 0' }}>
            <Spin />
            <div style={{ marginTop: 16 }}>
              {isLoadingRoleDetail ? uiText.adminRole.loadingRolePermissions : uiText.adminRole.loadingPermissions}
            </div>
          </div>
        ) : permissionTreeData.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '40px 0', color: '#999' }}>
            <p>{uiText.adminRole.emptyPermissions}</p>
            <p style={{ fontSize: 12 }}>{uiText.adminRole.emptyPermissionsHint}</p>
          </div>
        ) : (
          <>
            <div style={{ marginBottom: 16, color: '#666' }}>
              {formatText(uiText.adminRole.selectedPermissionCount, { count: selectedPermissions.length })}
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

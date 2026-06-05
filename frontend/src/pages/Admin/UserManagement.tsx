import { useState } from 'react'
import {
  Table,
  Button,
  Space,
  Modal,
  Form,
  Input,
  Switch,
  Tag,
  Typography,
  message,
  Popconfirm,
} from 'antd'
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  UserOutlined,
  MailOutlined,
  PhoneOutlined,
  TeamOutlined,
} from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import type { User } from '@/types/auth'
import dayjs from 'dayjs'
import './UserManagement.css'

const { Title } = Typography

export default function UserManagement() {
  const [users, setUsers] = useState<User[]>([
    {
      id: '1',
      account: 'admin',
      realName: '管理员',
      email: 'admin@example.com',
      department: 'IT部门',
      isActive: true,
      createTime: '2024-01-01T00:00:00Z',
    },
    {
      id: '2',
      account: 'user001',
      realName: '张三',
      email: 'zhangsan@example.com',
      department: '产品部',
      isActive: true,
      createTime: '2024-02-15T00:00:00Z',
    },
    {
      id: '3',
      account: 'user002',
      realName: '李四',
      email: 'lisi@example.com',
      department: '研发部',
      isActive: false,
      createTime: '2024-03-20T00:00:00Z',
    },
  ])

  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<User | null>(null)
  const [form] = Form.useForm()

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
    setUsers(users.filter((u) => u.id !== userId))
    message.success('用户已删除')
  }

  const handleToggleStatus = (userId: string, isActive: boolean) => {
    setUsers(
      users.map((u) => (u.id === userId ? { ...u, isActive } : u))
    )
    message.success(isActive ? '用户已启用' : '用户已禁用')
  }

  const handleModalOk = async () => {
    try {
      const values = await form.validateFields()
      
      if (editingUser) {
        // Update user
        setUsers(
          users.map((u) =>
            u.id === editingUser.id ? { ...u, ...values } : u
          )
        )
        message.success('用户信息已更新')
      } else {
        // Add user
        const newUser: User = {
          id: Date.now().toString(),
          ...values,
          isActive: true,
          createTime: new Date().toISOString(),
        }
        setUsers([...users, newUser])
        message.success('用户已添加')
      }

      setIsModalOpen(false)
      form.resetFields()
    } catch (error) {
      console.error('Validation failed:', error)
    }
  }

  const handleModalCancel = () => {
    setIsModalOpen(false)
    form.resetFields()
  }

  const columns: ColumnsType<User> = [
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
          <Button
            type="link"
            size="small"
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
          >
            编辑
          </Button>
          <Popconfirm
            title="确认删除"
            description="确定要删除该用户吗？"
            onConfirm={() => handleDelete(record.id)}
            okText="删除"
            cancelText="取消"
            okType="danger"
          >
            <Button type="link" size="small" danger icon={<DeleteOutlined />}>
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ]

  return (
    <div className="user-management">
      <div className="user-management-header">
        <Title level={3}>用户管理</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={handleAdd}>
          添加用户
        </Button>
      </div>

      <Table
        columns={columns}
        dataSource={users}
        rowKey="id"
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
      >
        <Form
          form={form}
          layout="vertical"
          autoComplete="off"
        >
          <Form.Item
            name="account"
            label="账号"
            rules={[
              { required: true, message: '请输入账号' },
              { min: 3, message: '账号至少3个字符' },
              { pattern: /^[a-zA-Z0-9_]+$/, message: '只能包含字母、数字、下划线' },
            ]}
          >
            <Input prefix={<UserOutlined />} placeholder="账号" disabled={!!editingUser} />
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
    </div>
  )
}

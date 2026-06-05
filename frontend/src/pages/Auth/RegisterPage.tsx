import { Form, Input, Button, Card, Typography, message } from 'antd'
import { UserOutlined, LockOutlined, MailOutlined, PhoneOutlined, TeamOutlined } from '@ant-design/icons'
import { Link, useNavigate } from 'react-router-dom'
import { useRegister } from '@/hooks/useAuth'
import type { RegisterRequest } from '@/types/auth'
import './AuthPages.css'

const { Title, Text } = Typography

export default function RegisterPage() {
  const [form] = Form.useForm()
  const navigate = useNavigate()
  const register = useRegister()

  const handleSubmit = async (values: RegisterRequest) => {
    try {
      await register.mutateAsync(values)
      message.success('注册成功，请登录')
      navigate('/login')
    } catch (error: any) {
      message.error(error.response?.data?.message || '注册失败')
    }
  }

  return (
    <div className="auth-container">
      <div className="auth-background" />
      <Card className="auth-card">
        <div className="auth-header">
          <Title level={2}>创建账号</Title>
          <Text type="secondary">加入企业 RAG 系统</Text>
        </div>

        <Form
          form={form}
          name="register"
          onFinish={handleSubmit}
          autoComplete="off"
          size="large"
          layout="vertical"
          scrollToFirstError
        >
          <Form.Item
            name="account"
            label="账号"
            rules={[
              { required: true, message: '请输入账号' },
              { min: 3, message: '账号至少3个字符' },
              { max: 50, message: '账号最多50个字符' },
              { pattern: /^[a-zA-Z0-9_]+$/, message: '只能包含字母、数字、下划线' },
            ]}
          >
            <Input prefix={<UserOutlined />} placeholder="账号" />
          </Form.Item>

          <Form.Item
            name="password"
            label="密码"
            rules={[
              { required: true, message: '请输入密码' },
              { min: 6, message: '密码至少6个字符' },
              { 
                pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]/,
                message: '密码必须包含大小写字母、数字和特殊字符'
              },
            ]}
            hasFeedback
          >
            <Input.Password prefix={<LockOutlined />} placeholder="密码" />
          </Form.Item>

          <Form.Item
            name="confirmPassword"
            label="确认密码"
            dependencies={['password']}
            hasFeedback
            rules={[
              { required: true, message: '请确认密码' },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('password') === value) {
                    return Promise.resolve()
                  }
                  return Promise.reject(new Error('两次输入的密码不一致'))
                },
              }),
            ]}
          >
            <Input.Password prefix={<LockOutlined />} placeholder="确认密码" />
          </Form.Item>

          <Form.Item
            name="realName"
            label="真实姓名"
            rules={[
              { required: true, message: '请输入真实姓名' },
              { max: 50, message: '姓名最多50个字符' },
            ]}
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

          <Form.Item
            name="phone"
            label="手机号"
            rules={[
              { pattern: /^1[3-9]\d{9}$/, message: '手机号格式不正确' },
            ]}
          >
            <Input prefix={<PhoneOutlined />} placeholder="手机号（可选）" />
          </Form.Item>

          <Form.Item
            name="department"
            label="部门"
          >
            <Input prefix={<TeamOutlined />} placeholder="所属部门（可选）" />
          </Form.Item>

          <Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              block
              loading={register.isPending}
            >
              注册
            </Button>
          </Form.Item>

          <div className="auth-footer">
            <Text type="secondary">
              已有账号？ <Link to="/login">立即登录</Link>
            </Text>
          </div>
        </Form>
      </Card>
    </div>
  )
}

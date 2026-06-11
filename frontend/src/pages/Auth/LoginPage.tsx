import React from 'react'
import { Form, Input, Button, Card, Typography } from 'antd'
import { UserOutlined, LockOutlined } from '@ant-design/icons'
import { Link, useNavigate } from 'react-router-dom'
import { useLogin } from '@/hooks/useAuth'
import type { LoginRequest } from '@/types/auth'
import './AuthPages.css'

const { Title, Text } = Typography

export default function LoginPage() {
  const [form] = Form.useForm()
  const navigate = useNavigate()
  const login = useLogin()

  const handleSubmit = async (values: LoginRequest) => {
    try {
      await login.mutateAsync(values)
      // 只有登录成功才会执行到这里
      navigate('/chat')
    } catch (error) {
      // 错误已经在 useLogin hook 中处理并显示
      // 不需要任何额外操作，更不要导航
      console.error('登录失败:', error)
    }
  }

  return (
    <div className="auth-container">
      <div className="auth-background" />
      <Card className="auth-card">
        <div className="auth-header">
          <Title level={2}>企业级 RAG 系统</Title>
          <Text type="secondary">智能问答，知识赋能</Text>
        </div>

        <Form
          form={form}
          name="login"
          onFinish={handleSubmit}
          autoComplete="off"
          size="large"
          layout="vertical"
        >
          <Form.Item
            name="account"
            rules={[{ required: true, message: '请输入账号' }]}
          >
            <Input
              prefix={<UserOutlined />}
              placeholder="账号"
              autoComplete="username"
            />
          </Form.Item>

          <Form.Item
            name="password"
            rules={[{ required: true, message: '请输入密码' }]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder="密码"
              autoComplete="current-password"
            />
          </Form.Item>
                  <Form.Item
                    name="tenantId"
                    rules={[{ required: true, message: '请输入租户' }]}
                    initialValue="default"
                  >
                    <Input
                      prefix={<UserOutlined />}
                      placeholder="租户ID"
                    />
                  </Form.Item>
          <Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              block
              loading={login.isPending}
            >
              登录
            </Button>
          </Form.Item>

          <div className="auth-footer">
            <Text type="secondary">
              还没有账号？ <Link to="/register">立即注册</Link>
            </Text>
          </div>
        </Form>

        <div className="auth-demo-hint">
          <Text type="secondary" style={{ fontSize: 12 }}>
            💡 演示账号: admin / Admin@123
          </Text>
        </div>
      </Card>
    </div>
  )
}

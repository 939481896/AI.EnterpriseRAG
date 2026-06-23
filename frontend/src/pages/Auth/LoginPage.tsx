import { Form, Input, Button, Card, Typography, Select } from 'antd'
import { UserOutlined, LockOutlined, GlobalOutlined } from '@ant-design/icons'
import { Link, useNavigate } from 'react-router-dom'
import { useCallback } from 'react'
import { useLogin } from '@/hooks/useAuth'
import type { LoginRequest } from '@/types/auth'
import { uiText } from '@/config/uiText'
import { type AppLocale, useLocaleStore } from '@/store/localeStore'
import './AuthPages.css'

const { Title, Text } = Typography

export default function LoginPage() {
  const [form] = Form.useForm()
  const navigate = useNavigate()
  const login = useLogin()
  // ✅ Use a selector to prevent unnecessary re-renders when locale changes
  const locale = useLocaleStore((state) => state.locale)
  const setLocale = useLocaleStore((state) => state.setLocale)

  // ✅ Memoize handleSubmit to prevent recreation on re-renders
  const handleSubmit = useCallback(async (values: LoginRequest) => {
    try {
      await login.mutateAsync(values)
      // 只有登录成功才会执行到这里
      form.resetFields() // 清除表单字段
      navigate('/chat')
    } catch (error) {
      // 清除密码字段，保留账号字段方便重试
      // 错误已由 useLogin hook 的 notification 处理
      form.setFieldsValue({ password: '' })
      console.error('登录请求失败:', error)
    }
  }, [login, form, navigate])

  return (
    <div className="auth-container">
      <div className="auth-background" />
      <div className="auth-language-switch">
        <Select
          size="small"
          value={locale}
          onChange={(value) => { setLocale(value as AppLocale); }}
          style={{ width: 150 }}
          options={[
            { value: 'zh-CN', label: `${uiText.common.chinese} (中文)` },
            { value: 'en-US', label: `${uiText.common.english} (English)` },
          ]}
          suffixIcon={<GlobalOutlined />}
        />
      </div>
      <Card className="auth-card">
        <div className="auth-header">
          <Title level={2}>{uiText.auth.loginTitle}</Title>
          <Text type="secondary">{uiText.auth.loginSubtitle}</Text>
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
            rules={[{ required: true, message: uiText.auth.inputAccount }]}
          >
            <Input
              prefix={<UserOutlined />}
              placeholder={uiText.auth.account}
              autoComplete="username"
            />
          </Form.Item>

          <Form.Item
            name="password"
            rules={[{ required: true, message: uiText.auth.inputPassword }]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder={uiText.auth.password}
              autoComplete="current-password"
            />
          </Form.Item>
                  <Form.Item
                    name="tenantId"
                    rules={[{ required: true, message: uiText.auth.inputTenant }]}
                    initialValue="default"
                  >
                    <Input
                      prefix={<UserOutlined />}
                      placeholder={uiText.auth.tenantId}
                    />
                  </Form.Item>
          <Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              block
              loading={login.isPending}
            >
              {uiText.auth.login}
            </Button>
          </Form.Item>

          <div className="auth-footer">
            <Text type="secondary">
              {uiText.auth.noAccount} <Link to="/register">{uiText.auth.goRegister}</Link>
            </Text>
          </div>
        </Form>

        <div className="auth-demo-hint">
          <Text type="secondary" style={{ fontSize: 12 }}>
            💡 {uiText.auth.demoHint}
          </Text>
        </div>
      </Card>
    </div>
  )
}

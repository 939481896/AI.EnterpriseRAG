import { Form, Input, Button, Card, Typography, Select } from 'antd'
import { UserOutlined, LockOutlined, MailOutlined, PhoneOutlined, TeamOutlined, GlobalOutlined } from '@ant-design/icons'
import { Link, useNavigate } from 'react-router-dom'
import { useRegister } from '@/hooks/useAuth'
import type { RegisterRequest } from '@/types/auth'
import { uiText } from '@/config/uiText'
import { notification } from '@/services/notification'
import { getErrorMessage } from '@/types/error'
import { type AppLocale, useLocaleStore } from '@/store/localeStore'
import './AuthPages.css'

const { Title, Text } = Typography

export default function RegisterPage() {
  const [form] = Form.useForm()
  const navigate = useNavigate()
  const register = useRegister()
  const { locale, setLocale } = useLocaleStore()

  const handleSubmit = async (values: RegisterRequest) => {
    try {
      await register.mutateAsync(values)
      notification.success(uiText.auth.registerSuccess)
      navigate('/login')
    } catch (error: unknown) {
      notification.error(getErrorMessage(error) || uiText.auth.registerFailed)
    }
  }

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
          <Title level={2}>{uiText.auth.createAccount}</Title>
          <Text type="secondary">{uiText.auth.registerSubtitle}</Text>
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
            label={uiText.auth.account}
            rules={[
              { required: true, message: uiText.auth.inputAccount },
              { min: 3, message: uiText.auth.accountMin },
              { max: 50, message: uiText.auth.accountMax },
              { pattern: /^[a-zA-Z0-9_]+$/, message: uiText.auth.accountPattern },
            ]}
          >
            <Input prefix={<UserOutlined />} placeholder={uiText.auth.account} />
          </Form.Item>

          <Form.Item
            name="password"
            label={uiText.auth.password}
            rules={[
              { required: true, message: uiText.auth.inputPassword },
              { min: 6, message: uiText.auth.passwordMin },
              { 
                pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]/,
                message: uiText.auth.passwordStrong,
              },
            ]}
            hasFeedback
          >
            <Input.Password prefix={<LockOutlined />} placeholder={uiText.auth.password} />
          </Form.Item>

          <Form.Item
            name="confirmPassword"
            label={uiText.auth.confirmPassword}
            dependencies={['password']}
            hasFeedback
            rules={[
              { required: true, message: uiText.auth.inputConfirmPassword },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('password') === value) {
                    return Promise.resolve()
                  }
                  return Promise.reject(new Error(uiText.auth.passwordMismatch))
                },
              }),
            ]}
          >
            <Input.Password prefix={<LockOutlined />} placeholder={uiText.auth.confirmPassword} />
          </Form.Item>

          <Form.Item
            name="realName"
            label={uiText.auth.realName}
            rules={[
              { required: true, message: uiText.auth.inputRealName },
              { max: 50, message: uiText.auth.realNameMax },
            ]}
          >
            <Input prefix={<UserOutlined />} placeholder={uiText.auth.realName} />
          </Form.Item>

          <Form.Item
            name="email"
            label={uiText.auth.email}
            rules={[
              { required: true, message: uiText.auth.inputEmail },
              { type: 'email', message: uiText.auth.emailInvalid },
            ]}
          >
            <Input prefix={<MailOutlined />} placeholder={uiText.auth.email} />
          </Form.Item>

          <Form.Item
            name="phone"
            label={uiText.auth.phone}
            rules={[
              { pattern: /^1[3-9]\d{9}$/, message: uiText.auth.phoneInvalid },
            ]}
          >
            <Input prefix={<PhoneOutlined />} placeholder={uiText.auth.optionalPhone} />
          </Form.Item>

          <Form.Item
            name="department"
            label={uiText.auth.department}
          >
            <Input prefix={<TeamOutlined />} placeholder={uiText.auth.optionalDepartment} />
          </Form.Item>

          <Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              block
              loading={register.isPending}
            >
              {uiText.auth.register}
            </Button>
          </Form.Item>

          <div className="auth-footer">
            <Text type="secondary">
              {uiText.auth.hasAccount} <Link to="/login">{uiText.auth.goLogin}</Link>
            </Text>
          </div>
        </Form>
      </Card>
    </div>
  )
}

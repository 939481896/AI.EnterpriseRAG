import { useState, useMemo } from 'react'
import React from 'react'
import { Layout, Menu, Avatar, Dropdown, Button, Space, Select } from 'antd'
import {
  UserOutlined,
  SettingOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  GlobalOutlined,
} from '@ant-design/icons'
import { useNavigate, useLocation } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { useLogout } from '@/hooks/useAuth'
import { usePermissionContext } from '@/contexts/PermissionContext'
import { uiText } from '@/config/uiText'
import { type AppLocale, useLocaleStore } from '@/store/localeStore'
import { getMenuConfigs } from '@/config/modules'
import './AppLayout.css'

const { Header, Sider, Content } = Layout

interface AppLayoutProps {
  children: React.ReactNode
}

export default function AppLayout({ children }: AppLayoutProps) {
  const [collapsed, setCollapsed] = useState(false)
  const navigate = useNavigate()
  const location = useLocation()
  const { user } = useAuthStore()
  const { locale, setLocale } = useLocaleStore()
  const logout = useLogout()
  const { hasPermission } = usePermissionContext()

  // 根据权限过滤菜单项（从模块注册表动态生成）
  const menuItems = useMemo(() => {
    // 获取翻译文本的辅助函数
    const getTextByKey = (key: string): string => {
      const parts = key.split('.')
      let current: any = uiText
      for (const part of parts) {
        current = current?.[part]
      }
      return typeof current === 'string' ? current : key
    }

    // 从模块注册表获取菜单配置
    const moduleMenus = getMenuConfigs(hasPermission)

    // 将菜单配置转换为 Ant Design 菜单项格式
    const transformed = moduleMenus.map((item) => {
      const IconComponent = item.iconComponent
      return {
        key: item.key,
        icon: IconComponent ? <IconComponent /> : undefined,
        label: getTextByKey(item.label as string),
        children: item.children
          ? item.children.map((child) => ({
              key: child.key,
              label: getTextByKey(child.label),
            }))
          : undefined,
      }
    })

    return transformed
  }, [hasPermission, locale])

  const userMenuItems = [
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: uiText.layout.profile,
    },
    {
      key: 'settings',
      icon: <SettingOutlined />,
      label: uiText.layout.settings,
    },
    {
      type: 'divider' as const,
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: uiText.layout.logout,
      danger: true,
    },
  ]

  const handleMenuClick = ({ key }: { key: string }) => {
    navigate(key)
  }

  const handleUserMenuClick = ({ key }: { key: string }) => {
    if (key === 'logout') {
      logout()
      navigate('/login')
    }
  }

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider
        trigger={null}
        collapsible
        collapsed={collapsed}
        style={{
          overflow: 'auto',
          height: '100vh',
          position: 'fixed',
          left: 0,
          top: 0,
          bottom: 0,
        }}
      >
        <div className="logo">
          {!collapsed && <h2 style={{ color: '#fff', margin: '16px' }}>{uiText.layout.appTitle}</h2>}
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[location.pathname]}
          items={menuItems}
          onClick={handleMenuClick}
        />
      </Sider>
      
      <Layout style={{ marginLeft: collapsed ? 80 : 200, transition: 'all 0.2s' }}>
        <Header
          style={{
            padding: '0 24px',
            background: '#fff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            borderBottom: '1px solid #f0f0f0',
          }}
        >
          <Button
            type="text"
            icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={() => { setCollapsed(!collapsed); }}
            style={{ fontSize: '16px', width: 64, height: 64 }}
          />
          
          <Space>
            <Select
              size="small"
              value={locale}
              onChange={(value) => { setLocale(value as AppLocale); }}
              style={{ width: 140 }}
              options={[
                { value: 'zh-CN', label: `${uiText.common.chinese} (中文)` },
                { value: 'en-US', label: `${uiText.common.english} (English)` },
              ]}
              suffixIcon={<GlobalOutlined />}
            />
            <span style={{ marginRight: 16 }}>
              {uiText.layout.welcome}<strong>{user?.userName || user?.account}</strong>
            </span>
            <Dropdown
              menu={{ items: userMenuItems, onClick: handleUserMenuClick }}
              placement="bottomRight"
            >
              <Avatar
                style={{ backgroundColor: '#1890ff', cursor: 'pointer' }}
                icon={<UserOutlined />}
              />
            </Dropdown>
          </Space>
        </Header>
        
        <Content
          style={{
            margin: '24px 16px',
            padding: 24,
            minHeight: 280,
            background: '#fff',
            borderRadius: 8,
          }}
        >
          {children}
        </Content>
      </Layout>
    </Layout>
  )
}

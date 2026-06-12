import { useState, useMemo } from 'react'
import { Layout, Menu, Avatar, Dropdown, Button, Space } from 'antd'
import {
  MessageOutlined,
  FileTextOutlined,
  RobotOutlined,
  DashboardOutlined,
  UserOutlined,
  SettingOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
} from '@ant-design/icons'
import { useNavigate, useLocation } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { useLogout } from '@/hooks/useAuth'
import { usePermissionContext } from '@/contexts/PermissionContext'
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
  const logout = useLogout()
  const { hasPermission, permissions, isLoading } = usePermissionContext()

  // 根据权限过滤菜单项
  const menuItems = useMemo(() => {
    const allMenuItems = [
      {
        key: '/chat',
        icon: <MessageOutlined />,
        label: '智能问答',
        permission: 'menu.chat',
      },
      {
        key: '/documents',
        icon: <FileTextOutlined />,
        label: '文档管理',
        permission: 'menu.document',
      },
      {
        key: '/agent',
        icon: <RobotOutlined />,
        label: 'Agent 工作区',
        permission: 'menu.agent',
      },
      {
        key: 'admin',
        icon: <DashboardOutlined />,
        label: '管理后台',
        permission: 'menu.admin',
        children: [
          {
            key: '/admin/dashboard',
            label: '数据面板',
            permission: 'menu.admin',
          },
          {
            key: '/admin/users',
            label: '用户管理',
            permission: 'menu.user',
          },
          {
            key: '/admin/roles',
            label: '角色管理',
            permission: 'menu.role',
          },
          {
            key: '/admin/permissions',
            label: '权限管理',
            permission: 'menu.permission',
          },
        ],
      },
    ]

    // 如果权限还在加载，显示所有菜单（避免闪烁）
    if (isLoading) {
      return allMenuItems.map(item => {
        if (item.children) {
          return {
            ...item,
            children: item.children.map(({ permission, ...child }) => child),
          }
        }
        const { permission, ...menuItem } = item
        return menuItem
      })
    }

    // 过滤菜单项
    const filtered = allMenuItems
      .filter(item => !item.permission || hasPermission(item.permission))
      .map(item => {
        if (item.children) {
          const filteredChildren = item.children.filter(
            child => !child.permission || hasPermission(child.permission)
          )
          // 如果所有子菜单都被过滤掉，则不显示父菜单
          if (filteredChildren.length === 0) return null

          return {
            ...item,
            children: filteredChildren.map(({ permission, ...child }) => child),
          }
        }
        // 移除 permission 属性，因为 Menu 组件不需要它
        const { permission, ...menuItem } = item
        return menuItem
      })
      .filter(Boolean) // 移除 null 项

    return filtered
  }, [hasPermission, permissions, isLoading])

  const userMenuItems = [
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: '个人信息',
    },
    {
      key: 'settings',
      icon: <SettingOutlined />,
      label: '系统设置',
    },
    {
      type: 'divider' as const,
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: '退出登录',
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
          {!collapsed && <h2 style={{ color: '#fff', margin: '16px' }}>企业 RAG</h2>}
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
            onClick={() => setCollapsed(!collapsed)}
            style={{ fontSize: '16px', width: 64, height: 64 }}
          />
          
          <Space>
            <span style={{ marginRight: 16 }}>
              欢迎，<strong>{user?.realName || user?.account}</strong>
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

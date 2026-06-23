/**
 * Sidebar Content Layout Template
 *
 * 用于有侧边栏的页面，如聊天对话、知识库浏览等。
 * 特点：左侧导航/列表，右侧主内容区域
 *
 * 使用示例：
 * ```tsx
 * export default function MyPage() {
 *   return (
 *     <SidebarContentLayout
 *       sidebar={<MySidebar />}
 *       header={<MyHeader />}
 *       content={<MyContent />}
 *       footer={<MyFooter />}
 *       sidebarWidth={280}
 *     />
 *   )
 * }
 * ```
 */

import { Layout } from 'antd'
import React from 'react'
import './SidebarContentLayout.css'

interface SidebarContentLayoutProps {
  /** 侧边栏内容 */
  sidebar: React.ReactNode
  /** 主区域顶部 header（可选） */
  header?: React.ReactNode
  /** 主区域内容（填充 flex） */
  content: React.ReactNode
  /** 主区域底部 footer（可选） */
  footer?: React.ReactNode
  /** 侧边栏宽度，默认 280 */
  sidebarWidth?: number
  /** 侧边栏主题，默认 light */
  sidebarTheme?: 'light' | 'dark'
  /** 总体高度，默认 calc(100vh - 112px) */
  height?: string | number
}

const { Sider, Content } = Layout

/**
 * Sidebar Content Layout 组件
 *
 * 管理侧边栏和主内容区域的布局，自动处理：
 * - 两列布局（侧边栏 + 主区域）
 * - 主区域内部的三行布局（header + content + footer）
 * - 固定侧边栏宽度，可配置
 * - 内容区域 flex 填充
 */
export default function SidebarContentLayout({
  sidebar,
  header,
  content,
  footer,
  sidebarWidth = 280,
  sidebarTheme = 'light',
  height = 'calc(100vh - 112px)',
}: SidebarContentLayoutProps) {
  return (
    <Layout style={{ height, background: '#fff' }}>
      {/* Left Sidebar */}
      <Sider
        width={sidebarWidth}
        theme={sidebarTheme}
        style={{
          borderRight: '1px solid #f0f0f0',
          height: '100%',
          overflow: 'hidden',
        }}
      >
        {sidebar}
      </Sider>

      {/* Main Content Area */}
      <Layout>
        {/* Header */}
        {header && (
          <div
            className="sidebar-layout-header"
            style={{
              padding: '12px 24px',
              borderBottom: '1px solid #f0f0f0',
            }}
          >
            {header}
          </div>
        )}

        {/* Content */}
        <Content
          style={{
            display: 'flex',
            flexDirection: 'column',
            flex: 1,
            overflowY: 'auto',
          }}
        >
          {content}
        </Content>

        {/* Footer */}
        {footer && (
          <div
            className="sidebar-layout-footer"
            style={{
              padding: '12px 24px',
              borderTop: '1px solid #f0f0f0',
              background: '#fafafa',
            }}
          >
            {footer}
          </div>
        )}
      </Layout>
    </Layout>
  )
}

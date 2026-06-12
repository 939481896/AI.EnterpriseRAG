# 🚀 Tailwind CSS 快速开始

## ⚡ 30 秒快速安装

```powershell
# 1. 进入 frontend 目录
cd frontend

# 2. 运行安装脚本（Windows PowerShell）
.\install-tailwind.ps1

# 或手动安装
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
```

---

## 📝 配置文件（自动生成）

已为您准备好以下文件：

- ✅ `tailwind.config.js` - Tailwind 配置（已映射现有设计 Token）
- ✅ `postcss.config.js` - PostCSS 配置
- ✅ `.vscode/settings.json` - VSCode IntelliSense 配置
- ✅ `.vscode/extensions.json` - 推荐扩展

---

## 🎨 更新 main.tsx

```typescript
// src/main.tsx
import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import App from './App'

// ✨ 添加这一行（必须在其他样式之前）
import 'tailwindcss/tailwind.css'

// Ant Design 样式
import 'antd/dist/reset.css'

// 全局样式（可以逐步废弃）
import './styles/global.css'

// ... 其余代码保持不变
```

---

## 🔄 迁移 ChatPage

### 替换 `src/pages/Chat/ChatPage.tsx`

```typescript
// 将 ChatPage.tailwind.tsx 的内容复制到 ChatPage.tsx

// 主要变化：
// ❌ 删除：import './ChatPage.css'
// ✅ 使用：className="flex h-screen bg-white"

// 旧代码
<Layout style={{ height: 'calc(100vh - 112px)', background: '#fff' }}>

// 新代码  
<Layout className="h-[calc(100vh-112px)] bg-white">
```

### 核心改动对照

| 功能 | 旧方式 | Tailwind 方式 |
|------|--------|---------------|
| 高度 | `style={{ height: 'calc(100vh - 112px)' }}` | `className="h-[calc(100vh-112px)]"` |
| 边框 | `style={{ borderRight: '1px solid #f0f0f0' }}` | `className="border-r border-gray-200"` |
| 内边距 | `style={{ padding: '24px' }}` | `className="p-6"` |
| Flex | `style={{ display: 'flex', flexDirection: 'column' }}` | `className="flex flex-col"` |
| 颜色 | `style={{ color: '#8c8c8c' }}` | `className="text-gray-500"` |

---

## ✅ 验证安装

```bash
# 1. 启动开发服务器
npm run dev

# 2. 打开浏览器
http://localhost:5173

# 3. 检查元素（F12）
# 应该看到 Tailwind 类名：
# <div class="flex h-screen bg-white">
```

---

## 🎯 常用类名速查

### 布局
```html
<!-- Flex 布局 -->
<div class="flex items-center justify-between gap-4">

<!-- Grid 布局 -->
<div class="grid grid-cols-3 gap-4">

<!-- 响应式 -->
<div class="flex-col md:flex-row">
```

### 间距
```html
<!-- 内边距 -->
<div class="p-6 px-4 py-2">

<!-- 外边距 -->
<div class="m-4 mx-auto my-2">

<!-- Gap -->
<div class="flex gap-2">
```

### 颜色
```html
<!-- 背景 -->
<div class="bg-white bg-gray-50 bg-primary">

<!-- 文本 -->
<span class="text-gray-500 text-primary">

<!-- 边框 -->
<div class="border border-gray-200">
```

### 尺寸
```html
<!-- 宽度 -->
<div class="w-full w-1/2 w-64">

<!-- 高度 -->
<div class="h-screen h-full h-64">
```

---

## 🔧 VSCode 设置

### 1. 安装扩展

在 VSCode 中搜索并安装：
- **Tailwind CSS IntelliSense** (必装)
- Prettier (可选)

### 2. 启用 IntelliSense

配置已自动生成在 `.vscode/settings.json`

**特性：**
- ✅ 自动补全类名
- ✅ 鼠标悬停显示 CSS
- ✅ 类名排序
- ✅ 颜色预览

---

## 📚 下一步

### 逐步迁移其他页面

1. ✅ ChatPage（已完成示例）
2. Dashboard
3. UserManagement
4. DocumentPage
5. 组件（SessionSidebar, ChatMessage）

### 迁移策略

**推荐：并行运行**
```typescript
// 保留旧 CSS，逐步添加 Tailwind
import './OldPage.css'  // 暂时保留

export default function Page() {
  return (
    <div className="flex">  {/* 新增 Tailwind */}
      <div className="old-style">  {/* 旧样式仍然工作 */}
        {/* ... */}
      </div>
    </div>
  )
}
```

---

## 🎨 设计 Token 映射

Tailwind 配置已映射您的现有设计系统：

| 变量 | Tailwind 类 |
|------|-------------|
| `--color-primary` | `text-primary`, `bg-primary` |
| `--space-lg` | `p-lg`, `m-lg`, `gap-lg` |
| `--font-size-base` | `text-base` |
| `--radius-base` | `rounded-base` |
| `--shadow-sm` | `shadow-sm` |

---

## 🆘 常见问题

### Q1: Tailwind 样式不生效？

**A:** 检查导入顺序

```typescript
// ✅ 正确顺序
import 'tailwindcss/tailwind.css'  // 1. Tailwind
import 'antd/dist/reset.css'       // 2. Ant Design
import './styles/global.css'       // 3. 自定义样式
```

### Q2: 与 Ant Design 冲突？

**A:** 已配置 `preflight: false`

```javascript
// tailwind.config.js
corePlugins: {
  preflight: false,  // ⚠️ 必须
}
```

### Q3: IntelliSense 不工作？

**A:** 
1. 安装 Tailwind CSS IntelliSense 扩展
2. 重启 VSCode
3. 检查 `.vscode/settings.json`

### Q4: 类名太长怎么办？

**A:** 使用 classnames 库

```typescript
import cn from 'classnames'

<div className={cn(
  'flex items-center',
  isActive && 'bg-primary text-white',
  'hover:shadow-md'
)}>
```

---

## 📖 学习资源

### 官方文档
- [Tailwind CSS Docs](https://tailwindcss.com/docs)
- [Tailwind Play](https://play.tailwindcss.com/)

### 速查表
- [Tailwind Cheat Sheet](https://nerdcave.com/tailwind-cheat-sheet)

---

## ✅ 检查清单

安装完成后，确认以下内容：

- [ ] ✅ Tailwind CSS 已安装
- [ ] ✅ tailwind.config.js 已配置
- [ ] ✅ postcss.config.js 已配置
- [ ] ✅ main.tsx 已更新导入
- [ ] ✅ VSCode 扩展已安装
- [ ] ✅ 开发服务器正常运行
- [ ] ✅ ChatPage 显示正常
- [ ] ✅ IntelliSense 工作正常

---

## 🎉 完成！

现在您可以：

1. ✅ 使用 Tailwind 类名
2. ✅ 享受自动补全
3. ✅ 快速开发页面
4. ✅ 逐步迁移旧代码

**查看完整指南：** `TAILWIND_MIGRATION_GUIDE.md`

**查看示例代码：** `frontend/src/pages/Chat/ChatPage.tailwind.tsx`

🚀 **开始使用 Tailwind CSS 吧！**

# 🎨 Tailwind CSS 迁移指南 - Step by Step

## 📦 第一步：安装依赖

在 `frontend` 目录下运行：

```bash
cd frontend

# 安装 Tailwind CSS 及其依赖
npm install -D tailwindcss postcss autoprefixer

# 初始化配置文件
npx tailwindcss init -p
```

这会创建两个文件：
- `tailwind.config.js` - Tailwind 配置
- `postcss.config.js` - PostCSS 配置

---

## ⚙️ 第二步：配置 Tailwind

### 1. 更新 `tailwind.config.js`

```javascript
/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      // 映射现有的设计 Token
      colors: {
        primary: {
          DEFAULT: '#1890ff',
          light: '#40a9ff',
          lighter: '#91d5ff',
          lightest: '#e6f7ff',
          dark: '#096dd9',
        },
        success: '#52c41a',
        warning: '#faad14',
        error: '#ff4d4f',
        info: '#1890ff',
      },
      spacing: {
        'xs': '4px',
        'sm': '8px',
        'md': '12px',
        'base': '16px',
        'lg': '24px',
        'xl': '32px',
        '2xl': '48px',
        '3xl': '64px',
      },
      fontSize: {
        'xs': '12px',
        'sm': '13px',
        'base': '14px',
        'md': '16px',
        'lg': '18px',
        'xl': '20px',
        '2xl': '24px',
        '3xl': '32px',
      },
      borderRadius: {
        'xs': '2px',
        'sm': '4px',
        'base': '6px',
        'md': '8px',
        'lg': '12px',
        'xl': '16px',
      },
      boxShadow: {
        'xs': '0 1px 2px rgba(0, 0, 0, 0.05)',
        'sm': '0 2px 4px rgba(0, 0, 0, 0.08)',
        'base': '0 4px 8px rgba(0, 0, 0, 0.1)',
        'md': '0 8px 16px rgba(0, 0, 0, 0.12)',
        'lg': '0 12px 24px rgba(0, 0, 0, 0.15)',
      },
    },
  },
  plugins: [],
  // 与 Ant Design 共存的重要配置
  corePlugins: {
    preflight: false, // 禁用 Tailwind 的基础样式重置，避免与 Ant Design 冲突
  },
}
```

### 2. 确认 `postcss.config.js`

```javascript
export default {
  plugins: {
    tailwindcss: {},
    autoprefixer: {},
  },
}
```

---

## 🎨 第三步：导入 Tailwind

### 更新 `src/main.tsx`

```typescript
import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import App from './App'

// ✨ 导入 Tailwind CSS (必须在其他样式之前)
import 'tailwindcss/tailwind.css'

// Ant Design 样式
import 'antd/dist/reset.css'

// 全局样式 (现在可以逐步废弃)
import './styles/global.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
})

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </QueryClientProvider>
  </React.StrictMode>
)
```

---

## 🔄 第四步：迁移 ChatPage

### 完整迁移后的代码

查看 `ChatPage.tailwind.tsx` 文件（已生成）

---

## 📊 迁移对照表

### 常用样式映射

| 旧 CSS | Tailwind 等价 |
|--------|---------------|
| `padding: 24px` | `p-6` 或 `p-lg` |
| `margin-bottom: 16px` | `mb-4` 或 `mb-base` |
| `display: flex` | `flex` |
| `align-items: center` | `items-center` |
| `justify-content: space-between` | `justify-between` |
| `gap: 8px` | `gap-2` 或 `gap-sm` |
| `background: #fafafa` | `bg-gray-50` |
| `border: 1px solid #f0f0f0` | `border border-gray-200` |
| `border-radius: 6px` | `rounded-base` |
| `color: #8c8c8c` | `text-gray-500` |
| `font-weight: 500` | `font-medium` |

### 响应式映射

| 旧 CSS | Tailwind |
|--------|----------|
| `@media (max-width: 768px) { padding: 16px }` | `p-6 md:p-4` |
| `@media (max-width: 768px) { display: none }` | `hidden md:block` |

---

## 🎯 第五步：逐步迁移

### 建议顺序

1. ✅ **ChatPage** (已完成，见示例)
2. Dashboard
3. UserManagement
4. DocumentPage
5. 组件 (SessionSidebar, ChatMessage)

### 迁移策略

#### 策略 A: 并行运行（推荐）
```typescript
// 保留旧 CSS，同时使用 Tailwind
import './ChatPage.css'  // 暂时保留

export default function ChatPage() {
  return (
    <div className="flex h-screen">  {/* Tailwind */}
      {/* ... */}
    </div>
  )
}
```

#### 策略 B: 逐步替换
1. 新功能用 Tailwind
2. 重构时替换旧样式
3. 最后删除 CSS 文件

---

## 🔧 第六步：VSCode 配置

### 安装扩展

```json
// .vscode/extensions.json
{
  "recommendations": [
    "bradlc.vscode-tailwindcss",
    "esbenp.prettier-vscode"
  ]
}
```

### 配置 IntelliSense

```json
// .vscode/settings.json
{
  "tailwindCSS.experimental.classRegex": [
    ["className\\s*=\\s*['\"]([^'\"]*)['\"]"]
  ],
  "editor.quickSuggestions": {
    "strings": true
  }
}
```

---

## 🎨 第七步：与 Ant Design 共存

### 重要配置

```javascript
// tailwind.config.js
export default {
  // ...
  corePlugins: {
    preflight: false, // ⚠️ 必须禁用，避免与 Ant Design 冲突
  },
}
```

### 样式优先级

```typescript
// 正确：Tailwind 不会覆盖 Ant Design
<Button className="mt-4">  {/* ✅ 只影响外边距 */}
  点击
</Button>

// 错误：可能冲突
<Button className="bg-blue-500">  {/* ❌ 可能不生效 */}
  点击
</Button>
```

### 最佳实践

```typescript
// 用 Tailwind 包裹 Ant Design
<div className="flex gap-4">
  <Button type="primary">按钮 1</Button>
  <Button>按钮 2</Button>
</div>
```

---

## ✅ 第八步：验证

### 检查清单

- [ ] Tailwind 配置正确
- [ ] PostCSS 配置正确
- [ ] main.tsx 导入顺序正确
- [ ] VSCode 扩展已安装
- [ ] IntelliSense 工作正常
- [ ] 页面显示正常
- [ ] 响应式工作正常
- [ ] 与 Ant Design 无冲突

### 测试命令

```bash
# 开发模式
npm run dev

# 生产构建
npm run build

# 检查 Tailwind 是否生效
# 打开浏览器开发者工具，检查元素类名
```

---

## 🚀 第九步：开始使用

### 示例 1：简单布局

```typescript
// 旧方式
<div className="page-container">
  <div className="page-header">
    <h3>标题</h3>
  </div>
</div>

// Tailwind 方式
<div className="p-6">
  <div className="flex items-center justify-between mb-6">
    <h3 className="text-xl font-semibold">标题</h3>
  </div>
</div>
```

### 示例 2：响应式

```typescript
// 旧方式
<div className="document-header">
  {/* CSS 文件中定义响应式 */}
</div>

// Tailwind 方式
<div className="flex flex-col md:flex-row items-start md:items-center gap-4">
  {/* 自动响应式 */}
</div>
```

### 示例 3：状态样式

```typescript
// 动态类名
<button
  className={`
    px-4 py-2 rounded-md
    ${isActive ? 'bg-primary text-white' : 'bg-gray-100 text-gray-700'}
    hover:shadow-md transition-all
  `}
>
  按钮
</button>
```

---

## 📚 常用 Tailwind 类名

### 布局
```
flex, flex-col, grid
items-center, items-start, items-end
justify-center, justify-between, justify-end
gap-2, gap-4, gap-6
```

### 间距
```
p-4, px-6, py-2, pt-4
m-4, mx-auto, my-4, mt-2
space-x-2, space-y-4
```

### 尺寸
```
w-full, w-1/2, w-64
h-screen, h-full, h-64
max-w-4xl, min-h-screen
```

### 文本
```
text-sm, text-base, text-lg, text-xl
font-normal, font-medium, font-semibold, font-bold
text-gray-500, text-primary
text-center, text-left, text-right
```

### 背景和边框
```
bg-white, bg-gray-50, bg-primary
border, border-2, border-gray-200
rounded, rounded-md, rounded-lg, rounded-full
shadow, shadow-md, shadow-lg
```

### 响应式
```
md:flex, lg:grid
md:hidden, lg:block
md:w-1/2, lg:w-1/3
```

---

## 🎓 学习资源

### 官方文档
- [Tailwind CSS Docs](https://tailwindcss.com/docs)
- [Tailwind Play](https://play.tailwindcss.com/) - 在线练习

### 速查表
- [Tailwind Cheat Sheet](https://nerdcave.com/tailwind-cheat-sheet)

### VSCode 插件
- Tailwind CSS IntelliSense
- Tailwind Documentation

---

## ⚠️ 注意事项

### 1. Preflight 禁用
```javascript
// tailwind.config.js
corePlugins: {
  preflight: false, // ⚠️ 必须
}
```

### 2. 类名顺序
```typescript
// ❌ 错误：可能被覆盖
<div className="p-4 p-6">  // p-6 生效

// ✅ 正确：使用条件类名
<div className={isLarge ? 'p-6' : 'p-4'}>
```

### 3. 动态类名
```typescript
// ❌ 错误：不会被编译
<div className={`text-${color}-500`}>

// ✅ 正确：使用完整类名
<div className={color === 'red' ? 'text-red-500' : 'text-blue-500'}>
```

### 4. 与 Ant Design 混用
```typescript
// ✅ 正确：Tailwind 用于布局，Ant Design 用于组件
<div className="flex gap-4 p-6">
  <Button type="primary">Ant Design</Button>
</div>
```

---

## 📈 迁移进度跟踪

### 待迁移文件

- [ ] ChatPage.tsx ✅ (已完成示例)
- [ ] Dashboard.tsx
- [ ] UserManagement.tsx
- [ ] DocumentPage.tsx
- [ ] AgentWorkspace.tsx
- [ ] SessionSidebar.tsx
- [ ] ChatMessage.tsx
- [ ] AppLayout.tsx

### 可删除文件（迁移完成后）

- [ ] src/styles/variables.css
- [ ] src/styles/components.css
- [ ] src/pages/Chat/ChatPage.css
- [ ] src/pages/Admin/Dashboard.css
- [ ] 等...

---

## 🎉 总结

### 完成步骤

1. ✅ 安装 Tailwind CSS
2. ✅ 配置 tailwind.config.js
3. ✅ 更新 main.tsx
4. ✅ 迁移第一个组件
5. ✅ 验证工作正常

### 下一步

1. 逐步迁移其他页面
2. 删除旧 CSS 文件
3. 享受 Tailwind 的便利！

---

**🚀 现在您可以开始使用 Tailwind CSS 了！**

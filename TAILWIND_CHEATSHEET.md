# 🎨 CSS → Tailwind 样式对照表

## 📐 布局 (Layout)

| CSS | Tailwind |
|-----|----------|
| `display: flex` | `flex` |
| `display: inline-flex` | `inline-flex` |
| `display: grid` | `grid` |
| `display: block` | `block` |
| `display: inline-block` | `inline-block` |
| `display: none` | `hidden` |
| `flex-direction: column` | `flex-col` |
| `flex-direction: row` | `flex-row` |
| `flex-wrap: wrap` | `flex-wrap` |

---

## 📍 Flexbox 对齐

| CSS | Tailwind |
|-----|----------|
| `align-items: center` | `items-center` |
| `align-items: flex-start` | `items-start` |
| `align-items: flex-end` | `items-end` |
| `align-items: stretch` | `items-stretch` |
| `justify-content: center` | `justify-center` |
| `justify-content: space-between` | `justify-between` |
| `justify-content: space-around` | `justify-around` |
| `justify-content: flex-end` | `justify-end` |
| `gap: 8px` | `gap-2` |
| `gap: 16px` | `gap-4` |
| `gap: 24px` | `gap-6` |

---

## 📏 间距 (Spacing)

### Padding

| CSS | Tailwind |
|-----|----------|
| `padding: 4px` | `p-1` |
| `padding: 8px` | `p-2` |
| `padding: 12px` | `p-3` |
| `padding: 16px` | `p-4` |
| `padding: 24px` | `p-6` |
| `padding: 32px` | `p-8` |
| `padding-left: 16px` | `pl-4` |
| `padding-right: 16px` | `pr-4` |
| `padding-top: 16px` | `pt-4` |
| `padding-bottom: 16px` | `pb-4` |
| `padding: 16px 24px` | `px-6 py-4` |

### Margin

| CSS | Tailwind |
|-----|----------|
| `margin: 16px` | `m-4` |
| `margin: 24px` | `m-6` |
| `margin-top: 16px` | `mt-4` |
| `margin-bottom: 16px` | `mb-4` |
| `margin-left: 16px` | `ml-4` |
| `margin-right: 16px` | `mr-4` |
| `margin: 0 auto` | `mx-auto` |
| `margin-top: auto` | `mt-auto` |

### 自定义间距（项目特定）

| CSS | Tailwind |
|-----|----------|
| `padding: var(--space-xs)` → `4px` | `p-xs` |
| `padding: var(--space-sm)` → `8px` | `p-sm` |
| `padding: var(--space-md)` → `12px` | `p-md` |
| `padding: var(--space-base)` → `16px` | `p-base` |
| `padding: var(--space-lg)` → `24px` | `p-lg` |
| `padding: var(--space-xl)` → `32px` | `p-xl` |

---

## 🎨 颜色 (Colors)

### 背景色

| CSS | Tailwind |
|-----|----------|
| `background: #ffffff` | `bg-white` |
| `background: #fafafa` | `bg-gray-50` |
| `background: #f5f5f5` | `bg-gray-100` |
| `background: #1890ff` | `bg-primary` |
| `background: var(--color-primary)` | `bg-primary` |
| `background: var(--bg-light)` | `bg-gray-50` |

### 文本色

| CSS | Tailwind |
|-----|----------|
| `color: #262626` | `text-gray-800` 或 `text-text-primary` |
| `color: #8c8c8c` | `text-gray-500` 或 `text-text-secondary` |
| `color: #bfbfbf` | `text-gray-400` 或 `text-text-tertiary` |
| `color: #1890ff` | `text-primary` |
| `color: #52c41a` | `text-success` |
| `color: #ff4d4f` | `text-error` |

### 边框色

| CSS | Tailwind |
|-----|----------|
| `border: 1px solid #d9d9d9` | `border border-gray-300` |
| `border: 1px solid #f0f0f0` | `border border-gray-200` |
| `border-bottom: 1px solid #f0f0f0` | `border-b border-gray-200` |

---

## 📐 尺寸 (Sizing)

### 宽度

| CSS | Tailwind |
|-----|----------|
| `width: 100%` | `w-full` |
| `width: 50%` | `w-1/2` |
| `width: 33.33%` | `w-1/3` |
| `width: 25%` | `w-1/4` |
| `width: 280px` | `w-70` |
| `width: auto` | `w-auto` |
| `max-width: 1200px` | `max-w-7xl` |

### 高度

| CSS | Tailwind |
|-----|----------|
| `height: 100%` | `h-full` |
| `height: 100vh` | `h-screen` |
| `height: calc(100vh - 112px)` | `h-[calc(100vh-112px)]` |
| `height: auto` | `h-auto` |
| `min-height: 100vh` | `min-h-screen` |

---

## 🔤 文本 (Typography)

### 字体大小

| CSS | Tailwind |
|-----|----------|
| `font-size: 12px` | `text-xs` |
| `font-size: 14px` | `text-sm` 或 `text-base` |
| `font-size: 16px` | `text-md` |
| `font-size: 18px` | `text-lg` |
| `font-size: 20px` | `text-xl` |
| `font-size: 24px` | `text-2xl` |

### 字重

| CSS | Tailwind |
|-----|----------|
| `font-weight: 400` | `font-normal` |
| `font-weight: 500` | `font-medium` |
| `font-weight: 600` | `font-semibold` |
| `font-weight: 700` | `font-bold` |

### 对齐

| CSS | Tailwind |
|-----|----------|
| `text-align: left` | `text-left` |
| `text-align: center` | `text-center` |
| `text-align: right` | `text-right` |

---

## 🔘 边框 (Borders)

### 边框样式

| CSS | Tailwind |
|-----|----------|
| `border: 1px solid` | `border` |
| `border: 2px solid` | `border-2` |
| `border-top: 1px solid` | `border-t` |
| `border-bottom: 1px solid` | `border-b` |
| `border-left: 1px solid` | `border-l` |
| `border-right: 1px solid` | `border-r` |

### 圆角

| CSS | Tailwind |
|-----|----------|
| `border-radius: 2px` | `rounded-xs` |
| `border-radius: 4px` | `rounded-sm` |
| `border-radius: 6px` | `rounded` 或 `rounded-base` |
| `border-radius: 8px` | `rounded-md` |
| `border-radius: 12px` | `rounded-lg` |
| `border-radius: 16px` | `rounded-xl` |
| `border-radius: 9999px` | `rounded-full` |

---

## 🌑 阴影 (Shadows)

| CSS | Tailwind |
|-----|----------|
| `box-shadow: 0 1px 2px rgba(0,0,0,0.05)` | `shadow-xs` |
| `box-shadow: 0 2px 4px rgba(0,0,0,0.08)` | `shadow-sm` |
| `box-shadow: 0 4px 8px rgba(0,0,0,0.1)` | `shadow` 或 `shadow-base` |
| `box-shadow: 0 8px 16px rgba(0,0,0,0.12)` | `shadow-md` |
| `box-shadow: 0 12px 24px rgba(0,0,0,0.15)` | `shadow-lg` |

---

## 📱 响应式 (Responsive)

### 断点

| CSS | Tailwind |
|-----|----------|
| `@media (min-width: 640px)` | `sm:` |
| `@media (min-width: 768px)` | `md:` |
| `@media (min-width: 1024px)` | `lg:` |
| `@media (min-width: 1280px)` | `xl:` |

### 示例

| CSS | Tailwind |
|-----|----------|
| 移动端隐藏 | `hidden md:block` |
| 桌面端隐藏 | `block md:hidden` |
| 响应式 Flex 方向 | `flex-col md:flex-row` |
| 响应式间距 | `p-4 md:p-6 lg:p-8` |

---

## 🎭 状态 (States)

### Hover

| CSS | Tailwind |
|-----|----------|
| `:hover { background: #f5f5f5 }` | `hover:bg-gray-100` |
| `:hover { color: #1890ff }` | `hover:text-primary` |
| `:hover { transform: scale(1.1) }` | `hover:scale-110` |

### Focus

| CSS | Tailwind |
|-----|----------|
| `:focus { outline: none }` | `focus:outline-none` |
| `:focus { ring: 2px }` | `focus:ring-2` |

### Active

| CSS | Tailwind |
|-----|----------|
| `:active { transform: scale(0.95) }` | `active:scale-95` |

---

## 📦 位置 (Position)

| CSS | Tailwind |
|-----|----------|
| `position: relative` | `relative` |
| `position: absolute` | `absolute` |
| `position: fixed` | `fixed` |
| `position: sticky` | `sticky` |
| `top: 0` | `top-0` |
| `right: 0` | `right-0` |
| `bottom: 0` | `bottom-0` |
| `left: 0` | `left-0` |
| `z-index: 10` | `z-10` |
| `z-index: 50` | `z-50` |

---

## 🌊 溢出 (Overflow)

| CSS | Tailwind |
|-----|----------|
| `overflow: hidden` | `overflow-hidden` |
| `overflow: auto` | `overflow-auto` |
| `overflow: scroll` | `overflow-scroll` |
| `overflow-x: auto` | `overflow-x-auto` |
| `overflow-y: auto` | `overflow-y-auto` |

---

## ⏱️ 过渡 (Transitions)

| CSS | Tailwind |
|-----|----------|
| `transition: all 0.2s` | `transition-all duration-200` |
| `transition: color 0.3s` | `transition-colors duration-300` |
| `transition: transform 0.2s` | `transition-transform duration-200` |

---

## 🎨 项目特定映射

### 您的设计系统 → Tailwind

| 变量 | 值 | Tailwind |
|------|-----|----------|
| `--color-primary` | `#1890ff` | `bg-primary`, `text-primary` |
| `--space-xs` | `4px` | `p-xs`, `m-xs`, `gap-xs` |
| `--space-sm` | `8px` | `p-sm`, `m-sm`, `gap-sm` |
| `--space-md` | `12px` | `p-md`, `m-md`, `gap-md` |
| `--space-base` | `16px` | `p-base`, `m-base`, `gap-base` |
| `--space-lg` | `24px` | `p-lg`, `m-lg`, `gap-lg` |
| `--font-size-base` | `14px` | `text-base` |
| `--radius-base` | `6px` | `rounded-base` |

---

## 🔄 完整示例对照

### 示例 1: 页面容器

**旧方式：**
```css
.page-container {
  padding: 24px;
}

@media (max-width: 768px) {
  .page-container {
    padding: 16px;
  }
}
```

**Tailwind：**
```html
<div class="p-6 md:p-4">
```

---

### 示例 2: Flex 布局

**旧方式：**
```css
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  gap: 16px;
}
```

**Tailwind：**
```html
<div class="flex items-center justify-between mb-6 gap-4">
```

---

### 示例 3: 卡片

**旧方式：**
```css
.card {
  background: #ffffff;
  border: 1px solid #f0f0f0;
  border-radius: 8px;
  padding: 24px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.08);
}
```

**Tailwind：**
```html
<div class="bg-white border border-gray-200 rounded-md p-6 shadow-sm">
```

---

### 示例 4: ChatPage 实际迁移

**旧方式：**
```typescript
<Layout style={{ 
  height: 'calc(100vh - 112px)', 
  background: '#fff' 
}}>
  <Sider
    width={280}
    theme="light"
    style={{
      borderRight: '1px solid #f0f0f0',
      height: '100%',
      overflow: 'hidden',
    }}
  >
```

**Tailwind：**
```typescript
<Layout className="h-[calc(100vh-112px)] bg-white">
  <Sider
    width={280}
    theme="light"
    className="border-r border-gray-200 h-full overflow-hidden"
  >
```

---

## 💡 实用技巧

### 任意值

```html
<!-- 自定义宽度 -->
<div class="w-[280px]">

<!-- 自定义高度 -->
<div class="h-[calc(100vh-112px)]">

<!-- 自定义颜色 -->
<div class="bg-[#1890ff]">
```

### 组合类名

```typescript
import cn from 'classnames'

<div className={cn(
  'flex items-center gap-2',
  isActive && 'bg-primary text-white',
  isDisabled && 'opacity-50 cursor-not-allowed'
)}>
```

---

## 📚 更多资源

- [Tailwind CSS 官方文档](https://tailwindcss.com/docs)
- [Tailwind CSS Cheat Sheet](https://nerdcave.com/tailwind-cheat-sheet)
- [Tailwind Play](https://play.tailwindcss.com/)

---

**💡 提示：保存此文档，迁移时随时查阅！**

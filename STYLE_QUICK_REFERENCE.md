# 🎨 样式系统速查表

## 📏 间距 (Spacing)

```css
--space-xs: 4px
--space-sm: 8px
--space-md: 12px
--space-base: 16px
--space-lg: 24px
--space-xl: 32px
--space-2xl: 48px
--space-3xl: 64px
```

**工具类:**
```html
<div class="mt-lg mb-base p-md gap-sm">
```

---

## 🎨 颜色 (Colors)

### 主色
```css
--color-primary: #1890ff
--color-primary-light: #40a9ff
--color-primary-lighter: #91d5ff
--color-primary-lightest: #e6f7ff
```

### 语义色
```css
--color-success: #52c41a  /* 成功 */
--color-warning: #faad14  /* 警告 */
--color-error: #ff4d4f    /* 错误 */
--color-info: #1890ff     /* 信息 */
```

### 文本色
```css
--text-primary: #262626     /* 主文本 */
--text-secondary: #8c8c8c   /* 次要文本 */
--text-tertiary: #bfbfbf    /* 辅助文本 */
```

---

## 🔤 字体 (Typography)

### 字号
```css
--font-size-xs: 12px
--font-size-sm: 13px
--font-size-base: 14px
--font-size-md: 16px
--font-size-lg: 18px
--font-size-xl: 20px
--font-size-2xl: 24px
--font-size-3xl: 32px
```

### 字重
```css
--font-weight-normal: 400
--font-weight-medium: 500
--font-weight-semibold: 600
--font-weight-bold: 700
```

---

## 🔘 圆角 (Border Radius)

```css
--radius-xs: 2px
--radius-sm: 4px
--radius-base: 6px
--radius-md: 8px
--radius-lg: 12px
--radius-xl: 16px
--radius-full: 9999px
```

---

## 🌑 阴影 (Shadows)

```css
--shadow-xs: 0 1px 2px rgba(0, 0, 0, 0.05)
--shadow-sm: 0 2px 4px rgba(0, 0, 0, 0.08)
--shadow-base: 0 4px 8px rgba(0, 0, 0, 0.1)
--shadow-md: 0 8px 16px rgba(0, 0, 0, 0.12)
--shadow-lg: 0 12px 24px rgba(0, 0, 0, 0.15)
```

---

## 📦 布局组件

### 页面容器
```html
<div class="page-container">
  <!-- 自动设置 padding: 24px (桌面) / 16px (移动) -->
</div>
```

### 页面标题栏
```html
<div class="page-header">
  <h3>标题</h3>
  <Button>操作</Button>
</div>
```

### 卡片
```html
<div class="card">
  <div class="card-header">
    <h4 class="card-title">卡片标题</h4>
  </div>
  <div class="card-body">
    内容
  </div>
</div>
```

---

## 🎯 工具类 (Utilities)

### Flexbox
```html
<div class="flex items-center justify-between gap-sm">
<div class="flex-col items-start gap-md">
```

### 间距
```html
<div class="mt-lg mb-base p-md">
<div class="mx-auto">  <!-- 水平居中 -->
```

### 文本
```html
<span class="text-center text-primary font-bold">
<p class="text-secondary font-medium">
```

### 显示/隐藏
```html
<div class="hide-mobile">  <!-- 移动端隐藏 -->
<div class="show-mobile">  <!-- 仅移动端显示 -->
```

---

## 🎬 动画类

```html
<div class="animate-fade-in">
<div class="animate-fade-in-up">
<div class="animate-slide-in-right">
<div class="animate-spin">
<div class="animate-pulse">
```

---

## 💬 消息组件

### 消息气泡
```html
<div class="message-bubble user-bubble">
<div class="message-bubble assistant-bubble">
```

### 状态徽章
```html
<span class="status-badge status-success">成功</span>
<span class="status-badge status-warning">警告</span>
<span class="status-badge status-error">错误</span>
<span class="status-badge status-info">信息</span>
```

---

## 🎨 空状态

```html
<div class="empty-state">
  <div class="empty-state-icon">📄</div>
  <div class="empty-state-title">暂无数据</div>
  <div class="empty-state-description">描述文字</div>
</div>
```

---

## 🔍 表单组件

```html
<div class="form-group">
  <label class="form-label">标签</label>
  <input />
  <div class="form-helper">提示文字</div>
  <div class="form-error">错误信息</div>
</div>
```

---

## 📱 响应式断点

```css
@media (max-width: 480px)  { /* xs */ }
@media (max-width: 576px)  { /* sm */ }
@media (max-width: 768px)  { /* md */ }
@media (max-width: 992px)  { /* lg */ }
@media (max-width: 1200px) { /* xl */ }
```

---

## ⚡ 快速示例

### 标准页面结构
```tsx
<div className="page-container">
  <div className="page-header">
    <h3>页面标题</h3>
    <Button type="primary">操作</Button>
  </div>
  
  <div className="card">
    <div className="card-header">
      <h4 className="card-title">卡片标题</h4>
    </div>
    <div className="card-body">
      <!-- 内容 -->
    </div>
  </div>
</div>
```

### Flexbox 布局
```tsx
<div className="flex items-center justify-between gap-md">
  <div className="flex items-center gap-sm">
    <Icon />
    <span>文本</span>
  </div>
  <Button>操作</Button>
</div>
```

### 加载状态
```tsx
{loading ? (
  <div className="loading-overlay">
    <Spin size="large" />
  </div>
) : (
  <Content />
)}
```

---

## 🎯 最佳实践

### ✅ 推荐
```css
/* 使用设计 Token */
padding: var(--space-lg);
color: var(--text-primary);

/* 使用全局类 */
<div className="page-container">
<div className="flex items-center gap-sm">
```

### ❌ 避免
```css
/* 避免硬编码 */
padding: 24px;
color: #262626;

/* 避免重复写通用样式 */
.my-container {
  padding: 24px;
  display: flex;
}
```

---

## 📚 常用组合

### 居中容器
```html
<div class="flex items-center justify-center">
```

### 垂直堆叠
```html
<div class="flex-col gap-md">
```

### 响应式卡片网格
```css
.grid-layout {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: var(--space-lg);
}
```

### 列表项
```html
<div class="list-item">
  <div class="flex items-center gap-sm">
    <Icon />
    <span>内容</span>
  </div>
  <Button size="small">操作</Button>
</div>
```

---

## 🔗 相关文档

- 📖 [完整架构文档](./FRONTEND_STYLE_ARCHITECTURE.md)
- 📁 [样式文件位置](./frontend/src/styles/)
- 🎨 [设计变量](./frontend/src/styles/variables.css)
- 🧩 [组件样式](./frontend/src/styles/components.css)

---

**💡 提示:** 保存此文件到书签，开发时随时查阅！

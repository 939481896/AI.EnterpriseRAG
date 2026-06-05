# 🎨 Enterprise RAG Frontend

企业级 RAG 智能问答系统 - 前端项目

## 📦 技术栈

- **框架**: React 18 + TypeScript
- **UI 库**: Ant Design 5.x
- **路由**: React Router v6
- **状态管理**: Zustand
- **HTTP 客户端**: Axios
- **数据请求**: TanStack Query (React Query)
- **Markdown**: react-markdown
- **构建工具**: Vite

## 🚀 快速开始

### 1. 安装依赖

```bash
cd frontend
npm install
```

### 2. 环境配置

创建 `.env` 文件：

```env
VITE_API_URL=http://localhost:5000
```

### 3. 启动开发服务器

```bash
npm run dev
```

访问: http://localhost:3000

### 4. 构建生产版本

```bash
npm run build
```

输出目录: `dist/`

## 📁 项目结构

```
frontend/
├── public/                 # 静态资源
├── src/
│   ├── api/               # API 接口定义
│   │   ├── client.ts      # Axios 配置
│   │   ├── auth.ts        # 认证接口
│   │   ├── chat.ts        # 聊天接口
│   │   └── document.ts    # 文档接口
│   │
│   ├── components/        # 可复用组件
│   │   ├── Layout/        # 布局组件
│   │   ├── Chat/          # 聊天组件
│   │   └── Document/      # 文档组件
│   │
│   ├── pages/             # 页面组件
│   │   ├── Auth/          # 登录/注册
│   │   ├── Chat/          # 聊天页面
│   │   ├── Document/      # 文档管理
│   │   ├── Agent/         # Agent 工作区
│   │   └── Admin/         # 管理后台
│   │
│   ├── hooks/             # 自定义 Hooks
│   │   ├── useAuth.ts
│   │   ├── useChat.ts
│   │   └── useDocument.ts
│   │
│   ├── store/             # Zustand 状态管理
│   │   ├── authStore.ts
│   │   └── chatStore.ts
│   │
│   ├── types/             # TypeScript 类型定义
│   │   ├── auth.ts
│   │   ├── chat.ts
│   │   └── document.ts
│   │
│   ├── styles/            # 全局样式
│   │   └── global.css
│   │
│   ├── App.tsx            # 根组件
│   └── main.tsx           # 入口文件
│
├── index.html
├── package.json
├── tsconfig.json
└── vite.config.ts
```

## 🎯 主要功能

### 1. 用户认证
- ✅ JWT Token 登录/注册
- ✅ 自动 Token 刷新
- ✅ 路由守卫

### 2. 智能问答
- ✅ RAG V0/V1 版本切换
- ✅ 实时消息展示
- ✅ Markdown 渲染
- ✅ 引用来源展示
- ✅ 会话管理

### 3. 文档管理
- ✅ 拖拽上传
- ✅ 批量上传（进度条）
- ✅ 文档预览
- ✅ 分类管理

### 4. Agent 工作区
- ✅ 工具执行日志
- ✅ 实时思考过程
- ✅ 结果可视化

### 5. 管理后台
- ✅ 数据统计面板
- ✅ 用户管理
- ✅ 权限配置

## 🔌 API 集成

### 后端接口配置

后端 API 地址在 `.env` 文件中配置：

```env
VITE_API_URL=http://localhost:5000
```

### API 调用示例

```typescript
// 登录
import { authApi } from '@/api/auth'

const response = await authApi.login({
  account: 'admin',
  password: 'password'
})

// 发送聊天消息
import { chatApi } from '@/api/chat'

const response = await chatApi.sendMessageV1({
  userId: 'admin',
  question: '房价下降的基本原则是什么？'
})
```

## 🎨 自定义主题

在 `src/main.tsx` 中修改 Ant Design 主题：

```typescript
<ConfigProvider
  theme={{
    token: {
      colorPrimary: '#1890ff',    // 主色
      borderRadius: 6,            // 圆角
      fontSize: 14,               // 字号
    },
  }}
>
```

## 📱 响应式设计

项目已适配移动端，主要断点：

```css
/* 平板 */
@media (max-width: 768px) {
  /* ... */
}

/* 手机 */
@media (max-width: 480px) {
  /* ... */
}
```

## 🚢 部署

### Docker 部署

```dockerfile
FROM node:18-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=builder /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

### Vercel/Netlify 部署

1. 推送代码到 GitHub
2. 连接到 Vercel/Netlify
3. 设置环境变量 `VITE_API_URL`
4. 自动部署

## 🧪 测试

```bash
# 运行测试
npm test

# 代码检查
npm run lint

# 格式化代码
npm run format
```

## 📝 开发规范

### 命名规范
- **组件**: PascalCase (`ChatMessage.tsx`)
- **Hooks**: camelCase with `use` prefix (`useChat.ts`)
- **工具函数**: camelCase (`formatDate.ts`)
- **常量**: UPPER_SNAKE_CASE (`API_BASE_URL`)

### 代码风格
- 使用 ESLint + Prettier
- 提交前自动格式化
- 遵循 Airbnb React 规范

## 🔧 故障排查

### 1. API 请求失败

检查后端服务是否启动：
```bash
# 后端应在 http://localhost:5000 运行
curl http://localhost:5000/api/health
```

### 2. CORS 错误

确保后端已配置 CORS：
```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

### 3. Token 过期

检查 JWT 配置：
- Token 有效期: 默认 1 小时
- 自动刷新: 已实现
- 401 状态: 自动跳转登录

## 📚 相关文档

- [React 官方文档](https://react.dev/)
- [Ant Design 文档](https://ant.design/)
- [TanStack Query 文档](https://tanstack.com/query/)
- [Zustand 文档](https://zustand-demo.pmnd.rs/)

## 🤝 贡献指南

1. Fork 项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

## 📄 许可证

MIT License

---

**开发团队**: AI.EnterpriseRAG  
**技术支持**: support@example.com  
**最后更新**: 2025-01-XX

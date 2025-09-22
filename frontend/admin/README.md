# Maple Blog 管理后台

基于 React 19 + TypeScript + Vite 构建的现代化管理后台系统。

## 技术栈

### 核心框架
- **React 19** - 最新版本的 React，支持并发特性
- **TypeScript** - 类型安全的 JavaScript
- **Vite** - 快速的构建工具

### UI 组件库
- **Ant Design Pro** - 企业级 UI 设计组件库
- **Ant Design** - React UI 组件库
- **@ant-design/icons** - 图标库

### 状态管理
- **Zustand** - 轻量级状态管理
- **TanStack Query** - 服务器状态管理
- **React Hook Form** - 表单状态管理

### 数据可视化
- **ECharts** - 专业的数据可视化图表库
- **echarts-for-react** - ECharts 的 React 封装

### 工具库
- **Axios** - HTTP 客户端
- **Day.js** - 日期处理库
- **Lodash-es** - 实用工具函数
- **ahooks** - React Hooks 工具库

## 项目结构

```
frontend/admin/
├── public/                 # 静态资源
├── src/
│   ├── components/        # 通用组件
│   │   ├── ui/           # 基础UI组件
│   │   ├── layout/       # 布局组件
│   │   └── common/       # 通用业务组件
│   ├── pages/            # 页面组件
│   │   ├── dashboard/    # 仪表盘
│   │   ├── users/        # 用户管理
│   │   ├── content/      # 内容管理
│   │   ├── analytics/    # 数据分析
│   │   ├── system/       # 系统管理
│   │   ├── profile/      # 个人资料
│   │   └── error/        # 错误页面
│   ├── layouts/          # 布局模板
│   ├── router/           # 路由配置
│   ├── stores/           # 状态管理
│   ├── services/         # API 服务
│   ├── hooks/            # 自定义 Hooks
│   ├── utils/            # 工具函数
│   ├── types/            # TypeScript 类型定义
│   ├── App.tsx           # 根组件
│   └── main.tsx          # 应用入口
├── index.html            # HTML 模板
├── vite.config.ts        # Vite 配置
├── tsconfig.json         # TypeScript 配置
├── package.json          # 项目依赖
└── README.md            # 项目说明
```

## 开发指南

### 环境要求
- Node.js >= 18.0.0
- npm >= 9.0.0

### 安装依赖
```bash
npm install
```

### 开发模式
```bash
npm run dev
```
启动开发服务器，默认端口 3001

### 构建生产版本
```bash
npm run build
```

### 预览生产版本
```bash
npm run preview
```

### 代码检查
```bash
npm run lint
npm run lint:fix
```

### 类型检查
```bash
npm run type-check
```

### 运行测试
```bash
npm test
npm run test:ui
npm run test:coverage
```

## 核心特性

### 🚀 现代化架构
- React 19 并发特性
- TypeScript 严格模式
- Vite 快速构建
- ESM 模块化

### 🎨 专业 UI
- Ant Design Pro 组件
- 响应式设计
- 深色模式支持
- 自定义主题

### 📊 数据可视化
- ECharts 图表集成
- 实时数据更新
- 交互式图表
- 多种图表类型

### 🔐 权限控制
- RBAC 权限模型
- 路由守卫
- 组件级权限
- 细粒度控制

### ⚡ 性能优化
- 代码分割
- 懒加载
- 虚拟滚动
- 缓存优化

### 🛠 开发体验
- TypeScript 类型安全
- ESLint 代码规范
- 热更新开发
- 调试工具集成

## 环境配置

### 开发环境变量
复制 `.env.development` 文件并根据需要修改：

```env
# API配置
VITE_API_BASE_URL=http://localhost:5000
VITE_API_PREFIX=/api

# 应用配置
VITE_APP_TITLE=Maple Blog 管理后台
VITE_APP_VERSION=1.0.0
```

### API 代理配置
开发环境会自动代理 `/api` 请求到后端服务器。

## 部署说明

### 构建生产版本
```bash
npm run build
```

构建产物将生成在 `dist` 目录中。

### Nginx 配置示例
```nginx
server {
    listen 80;
    server_name admin.yourdomain.com;
    root /path/to/dist;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api {
        proxy_pass http://backend-server;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

## 常见问题

### Q: 如何添加新页面？
1. 在 `src/pages` 中创建页面组件
2. 在 `src/router/index.tsx` 中添加路由配置
3. 在布局组件中添加菜单项

### Q: 如何自定义主题？
修改 `src/App.tsx` 中的主题配置。

### Q: 如何添加新的 API 接口？
在 `src/services` 中创建对应的服务文件。

### Q: 如何处理权限控制？
使用 `usePermissions` Hook 或 `PermissionGuard` 组件。

## 贡献指南

1. Fork 本仓库
2. 创建特性分支
3. 提交更改
4. 发起 Pull Request

## 许可证

MIT License
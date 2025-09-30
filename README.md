# 🍁 Maple Blog

[![Auto Release](https://github.com/Await-d/maple-blog/actions/workflows/auto-release-pipeline.yml/badge.svg)](https://github.com/Await-d/maple-blog/actions/workflows/auto-release-pipeline.yml)
[![PR Check](https://github.com/Await-d/maple-blog/actions/workflows/pr-lint-check.yml/badge.svg)](https://github.com/Await-d/maple-blog/actions/workflows/pr-lint-check.yml)
[![Docker Pulls](https://img.shields.io/docker/pulls/await2719/maple-blog-backend)](https://hub.docker.com/r/await2719/maple-blog-backend)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

一个现代化的AI博客系统，基于React 19和.NET 10构建，支持多数据库扩展。

## 📋 项目概述

Maple Blog是一个功能完整的博客系统，专为AI相关内容设计，但同时具备传统博客的所有核心功能。采用前后端分离架构，支持多种数据库后端，开发环境使用SQLite零配置启动。

### ✨ 核心特性

- 🚀 **现代技术栈** - React 19 + .NET 10 + TypeScript
- 🗄️ **多数据库支持** - SQLite/PostgreSQL/SQL Server/MySQL/Oracle
- 🔧 **零配置开发** - SQLite默认，一键启动
- 📱 **响应式设计** - 移动端友好
- 🔍 **全文搜索** - 强大的内容搜索功能
- 💬 **评论系统** - 支持嵌套回复和审核
- 👤 **用户管理** - 角色权限控制
- 📊 **管理后台** - 完善的内容管理界面
- 🤖 **AI扩展** - 预留AI功能接口
- 🐳 **容器化部署** - Docker支持

## 🏗️ 项目结构

```
maple-blog/
├── 📁 frontend/              # React 19 前端应用
│   ├── src/
│   │   ├── components/       # UI组件
│   │   │   ├── ui/          # 基础UI组件
│   │   │   ├── layout/      # 布局组件
│   │   │   └── common/      # 通用组件
│   │   ├── pages/           # 页面组件
│   │   │   ├── home/        # 首页
│   │   │   ├── blog/        # 博客相关页面
│   │   │   ├── auth/        # 认证页面
│   │   │   ├── admin/       # 管理后台
│   │   │   └── archive/     # 归档页面
│   │   ├── features/        # 功能模块
│   │   │   ├── blog/        # 博客功能
│   │   │   ├── auth/        # 认证功能
│   │   │   ├── admin/       # 管理功能
│   │   │   └── search/      # 搜索功能
│   │   ├── stores/          # Zustand状态管理
│   │   ├── services/        # TanStack Query API服务
│   │   ├── hooks/           # 自定义Hooks
│   │   ├── utils/           # 工具函数
│   │   ├── types/           # TypeScript类型定义
│   │   └── assets/          # 静态资源
│   └── public/              # 公共资源
├── 📁 backend/              # .NET 10 后端API
│   ├── src/
│   │   ├── MapleBlog.Domain/         # 领域层
│   │   │   ├── Entities/            # 实体类
│   │   │   ├── ValueObjects/        # 值对象
│   │   │   ├── Interfaces/          # 领域接口
│   │   │   └── Enums/              # 枚举类型
│   │   ├── MapleBlog.Application/    # 应用层
│   │   │   ├── Services/            # 应用服务
│   │   │   ├── DTOs/               # 数据传输对象
│   │   │   ├── Mappings/           # 对象映射
│   │   │   ├── Validators/         # 数据验证
│   │   │   └── Interfaces/         # 应用接口
│   │   ├── MapleBlog.Infrastructure/ # 基础设施层
│   │   │   ├── Data/               # 数据访问
│   │   │   ├── Repositories/       # 仓储实现
│   │   │   ├── Services/           # 基础设施服务
│   │   │   └── Configurations/     # 配置类
│   │   └── MapleBlog.API/           # API层
│   │       ├── Controllers/         # API控制器
│   │       ├── Middleware/          # 中间件
│   │       ├── Configuration/       # API配置
│   │       └── Extensions/          # 扩展方法
│   └── tests/                       # 测试项目
│       ├── MapleBlog.UnitTests/     # 单元测试
│       └── MapleBlog.IntegrationTests/ # 集成测试
├── 📁 docs/                 # 项目文档
├── 📁 data/                 # 数据文件(SQLite等)
├── 📁 scripts/              # 脚本文件
├── 🐳 docker-compose.yml    # Docker编排文件
├── 📄 README.md             # 项目说明
└── 📄 .gitignore           # Git忽略文件
```

## 🚀 快速开始

### 环境要求

- **Node.js** 18+
- **.NET** 8.0+
- **Git**
- **Docker** (可选)

### 本地开发

1. **克隆项目**
   ```bash
   git clone <repository-url>
   cd maple-blog
   ```

2. **启动后端API**
   ```bash
   cd backend/src/MapleBlog.API
   dotnet restore
   dotnet run
   ```
   > 后端将在 http://localhost:5000 启动，使用SQLite数据库

3. **启动前端应用**
   ```bash
   cd frontend
   npm install
   npm run dev
   ```
   > 前端将在 http://localhost:3000 启动

4. **访问应用**
   - 前端应用: http://localhost:3000
   - API文档: http://localhost:5000/swagger

### Docker部署

在运行任何 Docker Compose 之前，请先通过环境变量提供外部数据库与 Redis 的连接信息：

```bash
export POSTGRES_CONNECTION_STRING="Host=your-db-host;Port=5432;Database=maple_blog;Username=postgres;Password=***"
export REDIS_CONNECTION_STRING="your-redis-host:6379"

# 如部署管理后台，还需额外指定
export ADMIN_POSTGRES_CONNECTION_STRING="Host=your-db-host;Port=5432;Database=maple_blog_admin;Username=postgres;Password=***"
export ADMIN_REDIS_CONNECTION_STRING="your-redis-host:6379"
```

然后在项目根目录执行对应的 Compose 命令：

```bash
# 开发环境（默认使用 SQLite，可按需覆盖 DATABASE_PROVIDER 等变量）
docker-compose up -d

# 生产环境
docker-compose -f docker-compose.prod.yml up -d

# 管理后台（可选）
docker-compose -f docker-compose.admin.yml up -d
```

## 🛠️ 技术栈

### 前端技术栈
- **React 19** - 核心框架 (含React Compiler)
- **TypeScript** - 类型安全
- **Vite** - 构建工具
- **Zustand** - 状态管理
- **TanStack Query** - 服务端状态管理
- **React Router v6** - 路由管理
- **Tailwind CSS** - 样式框架
- **React Hook Form** - 表单处理
- **Framer Motion** - 动画效果

### 后端技术栈
- **.NET 10** - 核心框架
- **ASP.NET Core Web API** - API框架
- **Entity Framework Core** - ORM
- **AutoMapper** - 对象映射
- **FluentValidation** - 数据验证
- **JWT Bearer** - 身份认证
- **Serilog** - 日志记录
- **Swagger/OpenAPI** - API文档

### 数据库支持
- **SQLite** (开发默认)
- **PostgreSQL** (生产推荐)
- **SQL Server**
- **MySQL**
- **Oracle**

## 📚 功能模块

### 核心功能
- [x] 用户注册和登录
- [x] 文章发布和编辑
- [x] 分类和标签管理
- [x] 评论系统
- [x] 全文搜索
- [x] 归档功能
- [x] 响应式设计

### 管理功能
- [x] 内容管理
- [x] 用户管理
- [x] 权限控制
- [x] 统计分析

### AI功能 (计划中)
- [ ] AI内容生成
- [ ] 智能推荐
- [ ] 自动标签
- [ ] 内容分析

## 🔧 开发指南

- 详细的工程规范请见 [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md)
- 前端组件使用示例（模态框、评论系统）迁移至 [`docs/examples/`](docs/examples) 目录，供 Storybook 与文档引用

## ✅ 前端验证流程

- **单元测试**：`cd frontend && npx vitest run src/services/home/__tests__/homeApi.test.ts`
  - 如在受限环境遇到 WebSocket 端口警告，可忽略，测试仍会完成
- **关键表单自测**：
  - Newsletter 订阅：验证空邮箱/非法邮箱的错误提示，以及成功提示文案
  - 个人资料页：头像上传、偏好切换、密码修改及账户删除均具备防重入与错误反馈
- 更多验证说明参阅 [`docs/frontend_verification.md`](docs/frontend_verification.md)

## 📖 API文档

API文档在项目启动后可通过以下地址访问：
- Swagger UI: http://localhost:5000/swagger
- OpenAPI JSON: http://localhost:5000/swagger/v1/swagger.json

## 🤝 贡献指南

1. Fork 项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情

## 🙏 致谢

感谢所有为这个项目做出贡献的开发者们！

---

**🍁 Maple Blog** - 让写作更简单，让分享更有趣

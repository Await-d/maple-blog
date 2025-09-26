# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目规则 (Project Rules)

**三大核心规则 (THREE CORE RULES):**
- 🚫 **严禁使用临时方案** - 绝对不允许任何临时解决方案、占位符代码或临时性实现
- 🚫 **严禁使用模拟方案** - 绝对禁止模拟数据、假数据、测试桩或任何形式的模拟实现
- 🚫 **严禁使用简化方案** - 绝对不允许简化逻辑、减少功能或降低复杂度的实现方案

**重要规则 (CRITICAL RULES):**
- ✅ **完整功能实现** - 必须对所有功能进行完整的、生产就绪的功能实现
- ❌ **不生成测试文档** - 不需要生成测试相关的 .md 文档文件
- 🗑️ **自动删除测试文件** - 测试完成后必须自动删除测试文件
- 💯 **质量优先** - 所有代码必须通过类型检查、代码规范检查和测试
- 🎯 **实际可用** - 所有实现的功能必须在生产环境中实际可用，不允许演示级别的代码

## 项目概述

Maple Blog 是一个基于 React 19 和 .NET 10 构建的现代化 AI 驱动博客系统，支持多种数据库后端。采用前后端分离架构，使用 SQLite 实现零配置开发。

## 命令

### 开发命令

**后端 (.NET 10 API):**
```bash
cd backend/src/MapleBlog.API
dotnet restore                    # 安装依赖
dotnet run                        # 启动开发服务器 (http://localhost:5000)
dotnet build                      # 构建项目
dotnet test                       # 运行测试
dotnet ef database update         # 应用数据库迁移
```

**前端 (React 19):**
```bash
cd frontend
npm install                       # 安装依赖
npm run dev                       # 启动开发服务器 (http://localhost:3000)
npm run build                     # 生产环境构建
npm run preview                   # 预览生产构建
npm test                          # 运行测试
npm run lint                      # 运行 ESLint
npm run typecheck                 # 运行 TypeScript 类型检查
```

### Docker 命令
```bash
docker-compose up -d              # 启动开发环境
docker-compose -f docker-compose.prod.yml up -d  # 启动生产环境
docker-compose down               # 停止所有服务
```

### 数据库命令
```bash
# Entity Framework 迁移
cd backend/src/MapleBlog.API
dotnet ef migrations add <MigrationName>
dotnet ef database update
dotnet ef database drop --force   # 重置数据库
```

## 架构

### 技术栈
- **前端**: React 19 + TypeScript + Vite + Tailwind CSS + Zustand + TanStack Query
- **后端**: .NET 10 + ASP.NET Core Web API + Entity Framework Core
- **数据库**: SQLite (开发) / PostgreSQL (生产) / SQL Server / MySQL / Oracle
- **身份验证**: JWT Bearer 令牌
- **缓存**: Redis
- **验证**: FluentValidation
- **日志**: Serilog

### 项目结构
```
maple-blog/
├── frontend/                     # React 19 前端应用
│   ├── src/
│   │   ├── components/          # UI 组件 (ui/layout/common)
│   │   ├── pages/              # 页面组件 (home/blog/auth/admin/archive)
│   │   ├── features/           # 功能模块 (blog/auth/admin/search)
│   │   ├── stores/             # Zustand 状态管理
│   │   ├── services/           # TanStack Query API 服务
│   │   ├── hooks/              # 自定义 React hooks
│   │   ├── utils/              # 工具函数
│   │   └── types/              # TypeScript 类型定义
├── backend/                     # .NET 10 后端 API
│   ├── src/
│   │   ├── MapleBlog.Domain/           # 领域层 (实体, 值对象)
│   │   ├── MapleBlog.Application/      # 应用层 (服务, DTOs)
│   │   ├── MapleBlog.Infrastructure/   # 基础设施层 (数据访问)
│   │   └── MapleBlog.API/             # API 层 (控制器, 中间件)
│   └── tests/                          # 测试项目
├── docs/                        # 项目文档
├── data/                        # 数据库文件 (SQLite)
└── scripts/                     # 实用脚本
```

### 整洁架构分层
- **领域层**: 实体、值对象、领域逻辑、接口
- **应用层**: 服务、DTOs、验证、应用逻辑
- **基础设施层**: 数据访问、仓储、外部服务
- **API层**: 控制器、中间件、配置

### 数据库设计
系统使用 Entity Framework Core 的 Code First 方法。主要实体：
- **Users**: 身份验证和用户管理
- **Posts**: 包含标题、内容、分类、标签的博客文章
- **Categories**: 分层内容组织
- **Tags**: 内容标签系统
- **Comments**: 支持审核的嵌套评论系统

### 身份验证与授权
- JWT Bearer 令牌身份验证
- 基于角色的授权 (管理员、作者、用户)
- 使用 BCrypt 的安全密码哈希
- 令牌刷新机制

## 开发指南

### 代码风格
- **前端**: 使用函数组件和 hooks，TypeScript 严格模式，ESLint + Prettier
- **后端**: 遵循 C# 约定，async/await 模式，依赖注入
- **命名**: 类/方法使用 PascalCase，变量使用 camelCase，文件使用 kebab-case

### 状态管理
- **全局状态**: 使用 Zustand 管理客户端状态
- **服务器状态**: 使用 TanStack Query 进行 API 数据缓存和同步
- **表单**: 使用 React Hook Form 提升性能和验证

### API 设计
- 遵循约定模式的 RESTful 端点
- 一致的响应格式，包含成功/错误结构
- 全面的错误处理和验证
- 在 `/swagger` 可访问 OpenAPI/Swagger 文档

### 测试策略
- **前端**: Jest + React Testing Library 进行单元/集成测试
- **后端**: xUnit 进行单元测试，WebApplicationFactory 进行集成测试
- 为关键业务逻辑提供测试覆盖

### 数据库策略
- SQLite 用于零配置开发
- 生产环境推荐使用 PostgreSQL
- 通过抽象支持多种数据库提供商
- 使用 Entity Framework 迁移管理架构

## 常见任务

### 添加新功能
1. 在领域层设计实体
2. 在基础设施层创建仓储
3. 在应用层实现服务
4. 在 API 层添加控制器
5. 创建前端组件和页面
6. 使用 TanStack Query 添加 API 集成

### 数据库变更
1. 在领域层修改实体
2. 创建迁移: `dotnet ef migrations add <Name>`
3. 更新数据库: `dotnet ef database update`

### 任务完成检查清单 (Task Completion Checklist)

**每次完成开发任务后，必须执行以下命令:**

**前端任务:**
```bash
cd frontend
npm run typecheck                 # TypeScript 类型检查
npm run lint                      # ESLint 代码规范检查
npm test                          # 运行测试
```

**后端任务:**
```bash
cd backend
dotnet build                      # 构建验证
dotnet test                       # 运行所有测试
```

**质量要求:**
- 🚫 **绝不** 使用临时方案、模拟方案或简化方案
- 🚫 **绝不** 提交测试失败的代码
- 🚫 **绝不** 提交有 TypeScript 错误的代码
- 🚫 **绝不** 提交有 ESLint 错误的代码
- ✅ **必须** 在提交前本地测试通过
- 🗑️ **必须** 完成后删除测试文件 (按项目规则)
- ✅ **必须** 完整功能实现 - 达到生产环境标准
- 🎯 **必须** 确保所有功能在生产环境中实际可用

### 测试策略
- **前端**: Vitest + React Testing Library 进行单元/集成测试
- **后端**: xUnit 进行单元测试，WebApplicationFactory 进行集成测试
- **单个测试**: `dotnet test --filter TestName` 或 `npm test -- --grep "test name"`
- 为关键业务逻辑提供测试覆盖

## 环境配置

### 开发环境设置
1. 复制 `.env.template` 为 `.env` 并配置
2. 使用 `.env.development` 进行本地开发设置
3. SQLite 数据库在首次运行时自动创建
4. Redis 对开发是可选的 (缓存禁用回退)

### 生产部署
- 使用 PostgreSQL 数据库
- 配置 Redis 进行缓存
- 设置安全的 JWT 密钥
- 启用 HTTPS
- 使用 Docker Compose 进行容器化部署

## 关键文件
- `README.md`: 项目概述和快速开始指南
- `docs/DEVELOPMENT.md`: 详细开发说明
- `docs/ARCHITECTURE.md`: 全面的技术架构
- `docker-compose.yml`: 开发环境设置
- `docker-compose.prod.yml`: 生产部署配置

---

## 🚨 重要提醒 (IMPORTANT REMINDERS)

**开发规范 (Development Standards):**
1. **严格遵循三大核心规则** - 绝不允许临时方案、模拟方案或简化方案
2. **完整实现** - 任何功能必须完整实现，达到生产环境标准
3. **质量优先** - 所有代码提交前必须通过类型检查、lint 检查和测试
4. **测试管理** - 不创建测试文档，测试完成后自动删除测试文件
5. **架构遵循** - 严格遵循清洁架构分层和现有代码模式
6. **命令执行** - 完成任务后必须运行相应的验证命令

**提交前检查 (Pre-commit Checklist):**
- [ ] 前端: `npm run typecheck && npm run lint && npm test` 
- [ ] 后端: `dotnet build && dotnet test`
- [ ] 数据库变更已创建迁移
- [ ] 所有测试通过
- [ ] 临时测试文件已删除
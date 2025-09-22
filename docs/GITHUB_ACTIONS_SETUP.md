# GitHub Actions 配置指南

本文档说明如何为 Maple Blog 项目配置 GitHub Actions CI/CD 流水线。

## 必需的 Secrets 配置

为了使 GitHub Actions 能够正常工作，您需要在 GitHub 仓库设置中配置以下 Secrets：

### 1. Docker Hub 配置 (可选但推荐)

如果您希望自动推送 Docker 镜像到 Docker Hub：

1. 前往您的 GitHub 仓库
2. 点击 **Settings** → **Secrets and variables** → **Actions**
3. 点击 **New repository secret**
4. 添加以下 secrets：

   - **DOCKERHUB_TOKEN**
     - 名称：`DOCKERHUB_TOKEN`
     - 值：您的 Docker Hub 访问令牌
     - 获取方式：
       1. 登录 [Docker Hub](https://hub.docker.com/)
       2. 前往 **Account Settings** → **Security**
       3. 点击 **New Access Token**
       4. 命名为 `github-actions` 或类似名称
       5. 复制生成的令牌

   - **DOCKERHUB_USERNAME** (可选)
     - 名称：`DOCKERHUB_USERNAME`
     - 值：您的 Docker Hub 用户名
     - 如果不设置，默认使用 `await2719`

### 2. Telegram 通知配置 (可选)

如果您希望在发布新版本时收到 Telegram 通知：

1. 添加以下 secrets：

   - **TELEGRAM_BOT_TOKEN**
     - 名称：`TELEGRAM_BOT_TOKEN`
     - 值：您的 Telegram Bot Token
     - 获取方式：
       1. 在 Telegram 中搜索 `@BotFather`
       2. 发送 `/newbot` 创建新机器人
       3. 按照提示设置名称和用户名
       4. 复制生成的 token

   - **TELEGRAM_CHAT_ID**
     - 名称：`TELEGRAM_CHAT_ID`
     - 值：您的 Telegram 聊天 ID
     - 获取方式：
       1. 向您的机器人发送任意消息
       2. 访问 `https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates`
       3. 找到 `"chat":{"id":` 后面的数字

## 工作流说明

### 1. 自动发布流水线 (auto-release-pipeline.yml)

**触发条件：** 推送到 master 或 main 分支

**功能：**
- 自动计算版本号（基于提交信息）
- 构建 .NET 10 后端
- 构建 React 前端（如果存在）
- 生成 changelog
- 创建 GitHub Release
- 构建并推送 Docker 镜像（需要配置 DOCKERHUB_TOKEN）
- 发送 Telegram 通知（如果配置）

**版本号规则：**
- `feat!:` 或 `BREAKING CHANGE` → 主版本号 +1
- `feat:` → 次版本号 +1
- 其他 → 修订号 +1

### 2. PR 检查流水线 (pr-lint-check.yml)

**触发条件：** Pull Request 创建或更新

**功能：**
- 前端代码检查（ESLint、TypeScript）
- 后端构建和测试（.NET）
- Docker 构建验证
- 安全扫描（Trivy）
- 提交规范检查
- PR 大小检查
- 依赖漏洞检查

## 本地测试

在推送前，您可以本地验证：

```bash
# 后端测试
cd backend
dotnet build
dotnet test

# 前端测试（如果有）
cd frontend
npm run lint
npm run typecheck
npm run build
```

## 注意事项

1. **无 Docker Hub Token：** 如果未配置 DOCKERHUB_TOKEN，工作流仍会成功，但不会推送镜像到 Docker Hub
2. **无前端代码：** 工作流会自动检测并跳过前端相关步骤
3. **首次运行：** 首次运行会创建 v1.0.0 版本

## 故障排除

### 问题：Docker Hub 登录失败
**解决：** 检查 DOCKERHUB_TOKEN 是否正确配置

### 问题：前端构建失败（React 19 兼容性）
**解决：** 工作流已配置使用 `--legacy-peer-deps`

### 问题：git-cliff 下载失败
**解决：** 工作流会自动获取最新版本

## 相关链接

- [GitHub Actions 文档](https://docs.github.com/en/actions)
- [Docker Hub](https://hub.docker.com/)
- [Conventional Commits](https://www.conventionalcommits.org/)
# 🔧 Maple Blog 开发指南

## 📋 目录

1. [环境准备](#环境准备)
2. [快速开始](#快速开始)
3. [开发流程](#开发流程)
4. [代码规范](#代码规范)
5. [测试指南](#测试指南)
6. [调试指南](#调试指南)
7. [部署指南](#部署指南)
8. [常见问题](#常见问题)

---

## 🛠️ 环境准备

### 必需软件

| 软件 | 版本要求 | 说明 |
|------|----------|------|
| **Node.js** | ≥ 18.0.0 | JavaScript运行时 |
| **.NET SDK** | ≥ 8.0 | .NET开发工具包 |
| **Git** | ≥ 2.30 | 版本控制工具 |
| **Docker** | ≥ 20.0 | 容器化工具(可选) |
| **Visual Studio Code** | 最新版 | 推荐编辑器 |

### 推荐扩展 (VS Code)

```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.vscode-dotnet-runtime",
    "bradlc.vscode-tailwindcss",
    "esbenp.prettier-vscode",
    "ms-vscode.vscode-typescript-next",
    "formulahendry.auto-rename-tag",
    "ms-vscode.vscode-json",
    "humao.rest-client",
    "ms-docker.docker"
  ]
}
```

### 数据库工具 (可选)

- **SQLite Browser** - SQLite数据库管理
- **pgAdmin** - PostgreSQL管理工具
- **Redis Desktop Manager** - Redis管理工具

---

## 🚀 快速开始

### 1. 克隆项目

```bash
git clone <repository-url>
cd maple-blog
```

### 2. 环境配置

复制环境变量文件：
```bash
cp .env.template .env
cp .env.development .env.local
```

编辑 `.env` 文件，配置必要的环境变量。

### 3. 安装依赖

**前端依赖：**
```bash
cd frontend
npm install
```

**后端依赖：**
```bash
cd backend/src/MapleBlog.API
dotnet restore
```

### 4. 数据库初始化

**方式一：自动初始化 (推荐)**
```bash
cd backend/src/MapleBlog.API
dotnet run
```
首次运行会自动创建SQLite数据库和初始数据。

**方式二：手动初始化**
```bash
cd backend/src/MapleBlog.API
dotnet ef database update
```

### 5. 启动开发服务器

**启动后端API：**
```bash
cd backend/src/MapleBlog.API
dotnet run
```
访问: http://localhost:5000

**启动前端应用：**
```bash
cd frontend
npm run dev
```
访问: http://localhost:3000

### 6. 使用Docker (可选)

**开发环境：**
```bash
docker-compose up -d
```

**生产环境：**
```bash
docker-compose -f docker-compose.prod.yml up -d
```

---

## 🔄 开发流程

### 分支策略

```
main (主分支)
├── develop (开发分支)
├── feature/功能名称 (功能分支)
├── bugfix/问题描述 (修复分支)
└── hotfix/紧急修复 (热修复分支)
```

### 开发步骤

1. **创建功能分支**
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/article-management
   ```

2. **编写代码**
   - 遵循代码规范
   - 编写单元测试
   - 提交前运行测试

3. **提交代码**
   ```bash
   git add .
   git commit -m "feat: add article management functionality"
   git push origin feature/article-management
   ```

4. **创建Pull Request**
   - 描述清楚变更内容
   - 关联相关Issue
   - 请求代码审查

5. **代码审查和合并**
   - 修复审查意见
   - 合并到develop分支
   - 删除功能分支

### 提交信息规范

使用 [Conventional Commits](https://www.conventionalcommits.org/) 规范：

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

**类型说明：**
- `feat`: 新功能
- `fix`: 错误修复
- `docs`: 文档更新
- `style`: 代码格式调整
- `refactor`: 代码重构
- `test`: 测试相关
- `chore`: 构建工具或辅助工具的变动

**示例：**
```bash
git commit -m "feat(blog): add article search functionality"
git commit -m "fix(auth): resolve JWT token expiration issue"
git commit -m "docs: update API documentation"
```

---

## 📐 代码规范

### 前端代码规范

**TypeScript/React：**
```typescript
// 使用函数式组件和Hooks
const BlogPost: React.FC<BlogPostProps> = ({ post }) => {
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = useCallback(async (data: PostFormData) => {
    setIsLoading(true);
    try {
      await blogApi.updatePost(post.id, data);
    } catch (error) {
      console.error('Failed to update post:', error);
    } finally {
      setIsLoading(false);
    }
  }, [post.id]);

  return (
    <article className="prose max-w-none">
      <h1>{post.title}</h1>
      <div dangerouslySetInnerHTML={{ __html: post.content }} />
    </article>
  );
};
```

**命名规范：**
- 组件：PascalCase (`BlogPost`)
- 变量和函数：camelCase (`isLoading`, `handleSubmit`)
- 常量：UPPER_SNAKE_CASE (`API_BASE_URL`)
- 文件名：kebab-case (`blog-post.tsx`)

### 后端代码规范

**C#/.NET：**
```csharp
// 使用异步编程模式
public class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PostService> _logger;

    public PostService(
        IPostRepository postRepository,
        IMapper mapper,
        ILogger<PostService> logger)
    {
        _postRepository = postRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PostDto> GetPostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(id, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException($"Post with id {id} not found");
        }

        return _mapper.Map<PostDto>(post);
    }
}
```

**命名规范：**
- 类：PascalCase (`PostService`)
- 方法：PascalCase (`GetPostAsync`)
- 属性：PascalCase (`CreatedAt`)
- 私有字段：_camelCase (`_postRepository`)
- 常量：PascalCase (`MaxFileSize`)

### 样式规范

**Tailwind CSS：**
```jsx
// 使用组合类，保持可读性
<div className="
  flex flex-col gap-4
  p-6 bg-white rounded-lg shadow-md
  hover:shadow-lg transition-shadow duration-200
">
  <h2 className="text-xl font-semibold text-gray-900">
    {title}
  </h2>
  <p className="text-gray-600 leading-relaxed">
    {content}
  </p>
</div>
```

---

## 🧪 测试指南

### 前端测试

**单元测试 (Jest + React Testing Library)：**
```typescript
// BlogPost.test.tsx
import { render, screen } from '@testing-library/react';
import { BlogPost } from './BlogPost';

describe('BlogPost', () => {
  const mockPost = {
    id: '1',
    title: 'Test Post',
    content: 'Test content',
    createdAt: new Date().toISOString(),
  };

  test('renders post title and content', () => {
    render(<BlogPost post={mockPost} />);

    expect(screen.getByText('Test Post')).toBeInTheDocument();
    expect(screen.getByText('Test content')).toBeInTheDocument();
  });
});
```

**集成测试：**
```typescript
// BlogApi.test.ts
import { blogApi } from './BlogApi';

describe('BlogApi', () => {
  test('should fetch posts successfully', async () => {
    const posts = await blogApi.getPosts();
    expect(posts).toBeDefined();
    expect(Array.isArray(posts.data)).toBe(true);
  });
});
```

### 后端测试

**单元测试 (xUnit)：**
```csharp
// PostServiceTests.cs
public class PostServiceTests
{
    private readonly Mock<IPostRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PostService _service;

    public PostServiceTests()
    {
        _mockRepository = new Mock<IPostRepository>();
        _mockMapper = new Mock<IMapper>();
        _service = new PostService(_mockRepository.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetPostAsync_ExistingId_ReturnsPost()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = new Post { Id = postId, Title = "Test Post" };
        var postDto = new PostDto { Id = postId, Title = "Test Post" };

        _mockRepository.Setup(r => r.GetByIdAsync(postId, default))
                      .ReturnsAsync(post);
        _mockMapper.Setup(m => m.Map<PostDto>(post))
                   .Returns(postDto);

        // Act
        var result = await _service.GetPostAsync(postId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(postId, result.Id);
        Assert.Equal("Test Post", result.Title);
    }
}
```

**集成测试：**
```csharp
// PostControllerIntegrationTests.cs
public class PostControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PostControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPosts_ReturnsSuccessAndCorrectContentType()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/posts");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8",
                     response.Content.Headers.ContentType?.ToString());
    }
}
```

### 运行测试

**前端测试：**
```bash
cd frontend
npm test                 # 运行测试
npm run test:watch      # 监视模式
npm run test:coverage   # 覆盖率报告
```

**后端测试：**
```bash
cd backend
dotnet test                              # 运行所有测试
dotnet test --collect:"XPlat Code Coverage"  # 覆盖率报告
```

---

## 🐛 调试指南

### 前端调试

**Chrome DevTools：**
1. 打开开发者工具 (F12)
2. 使用 Sources 面板设置断点
3. 使用 Console 面板查看日志
4. 使用 Network 面板检查API调用

**VS Code调试：**
```json
// .vscode/launch.json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch Chrome",
      "type": "chrome",
      "request": "launch",
      "url": "http://localhost:3000",
      "webRoot": "${workspaceFolder}/frontend/src"
    }
  ]
}
```

### 后端调试

**VS Code调试：**
```json
// .vscode/launch.json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/backend/src/MapleBlog.API/bin/Debug/net8.0/MapleBlog.API.dll",
      "args": [],
      "cwd": "${workspaceFolder}/backend/src/MapleBlog.API",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

**日志调试：**
```csharp
// 使用Serilog进行结构化日志
public async Task<IActionResult> GetPost(Guid id)
{
    _logger.LogInformation("Fetching post with id: {PostId}", id);

    try
    {
        var post = await _postService.GetPostAsync(id);
        _logger.LogInformation("Successfully retrieved post: {PostTitle}", post.Title);
        return Ok(post);
    }
    catch (NotFoundException ex)
    {
        _logger.LogWarning("Post not found: {PostId}", id);
        return NotFound();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching post: {PostId}", id);
        return StatusCode(500);
    }
}
```

---

## 🚀 部署指南

### 本地部署

**前端构建：**
```bash
cd frontend
npm run build
npm run preview  # 预览构建结果
```

**后端发布：**
```bash
cd backend/src/MapleBlog.API
dotnet publish -c Release -o ./publish
```

### Docker部署

**构建镜像：**
```bash
# 前端镜像
docker build -t maple-blog-frontend ./frontend

# 后端镜像
docker build -t maple-blog-backend ./backend
```

**运行容器：**
```bash
# 开发环境
docker-compose up -d

# 生产环境
docker-compose -f docker-compose.prod.yml up -d
```

### 云部署

**使用Docker Swarm：**
```bash
# 初始化集群
docker swarm init

# 部署服务
docker stack deploy -c docker-compose.prod.yml maple-blog
```

**使用Kubernetes：**
```bash
# 应用配置
kubectl apply -f k8s/

# 查看状态
kubectl get pods -n maple-blog
```

---

## ❓ 常见问题

### Q1: 数据库连接失败

**问题：** 无法连接到数据库

**解决方案：**
1. 检查连接字符串配置
2. 确认数据库服务是否运行
3. 检查防火墙设置
4. 验证用户权限

### Q2: 前端API调用失败

**问题：** CORS错误或API调用失败

**解决方案：**
1. 检查后端CORS配置
2. 确认API URL配置
3. 检查网络连接
4. 查看浏览器控制台错误

### Q3: JWT Token失效

**问题：** 认证失败，提示token无效

**解决方案：**
1. 检查JWT密钥配置
2. 确认token是否过期
3. 验证token格式
4. 检查时钟同步

### Q4: 文件上传失败

**问题：** 文件上传到服务器失败

**解决方案：**
1. 检查文件大小限制
2. 确认文件类型允许
3. 检查磁盘空间
4. 验证文件路径权限

### Q5: Docker构建失败

**问题：** Docker镜像构建错误

**解决方案：**
1. 清理Docker缓存
2. 检查Dockerfile语法
3. 确认依赖版本
4. 查看构建日志

---

## 📚 参考资源

- [React 19 官方文档](https://react.dev/)
- [.NET 8 官方文档](https://docs.microsoft.com/en-us/dotnet/)
- [TypeScript 官方文档](https://www.typescriptlang.org/)
- [Tailwind CSS 官方文档](https://tailwindcss.com/)
- [Docker 官方文档](https://docs.docker.com/)

---

## 🤝 贡献指南

1. Fork 项目到个人仓库
2. 创建功能分支
3. 提交代码并编写测试
4. 确保所有测试通过
5. 提交Pull Request
6. 等待代码审查和合并

---

**Happy Coding! 🎉**
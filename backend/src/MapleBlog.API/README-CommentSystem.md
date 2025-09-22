# 评论系统集成指南

## 概述

本指南介绍如何将评论系统集成到Maple Blog API中。评论系统包含完整的CRUD操作、实时通信、智能审核、通知系统等功能。

## 系统架构

### 核心组件

- **应用层服务**
  - `CommentService` - 评论CRUD和业务逻辑
  - `CommentModerationService` - 审核管理服务
  - `CommentNotificationService` - 通知服务

- **基础设施服务**
  - `CommentCacheService` - 缓存服务
  - `AIContentModerationService` - AI内容审核
  - `SensitiveWordFilter` - 敏感词过滤

- **API控制器**
  - `CommentsController` - 评论REST API
  - `CommentModerationController` - 审核管理API

- **实时通信**
  - `CommentHub` - SignalR评论中心

## 集成步骤

### 1. 服务注册

在`Program.cs`或`Startup.cs`中添加评论系统服务：

```csharp
using MapleBlog.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 添加评论系统服务
builder.Services.AddCommentServices(builder.Configuration);
builder.Services.AddCommentSignalR();
builder.Services.ConfigureCommentOptions(builder.Configuration);
builder.Services.AddCommentCors(builder.Configuration);

// 添加AutoMapper
builder.Services.AddAutoMapper(typeof(CommentProfile));

var app = builder.Build();

// 配置中间件
app.UseCommentSystem(app.Environment);
```

### 2. 数据库配置

确保已执行评论系统相关的数据库迁移：

```bash
dotnet ef migrations add CommentSystem
dotnet ef database update
```

### 3. 配置文件

将`Configuration/comment-system.json`中的配置合并到主配置文件中：

```json
{
  "ContentModeration": {
    "EnableAI": false,
    "SpamThreshold": 0.7,
    "ToxicityThreshold": 0.8,
    "EnableSensitiveWordFilter": true
  },
  "CommentCache": {
    "Enabled": true,
    "DefaultExpirationMinutes": 15
  },
  "Notifications": {
    "EnableRealtime": true,
    "EnableEmail": false
  }
}
```

## API端点

### 评论管理

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | `/api/comments` | 获取评论列表 |
| GET | `/api/comments/{id}` | 获取单个评论 |
| POST | `/api/comments` | 创建评论 |
| PUT | `/api/comments/{id}` | 更新评论 |
| DELETE | `/api/comments/{id}` | 删除评论 |
| GET | `/api/comments/tree/{postId}` | 获取文章评论树 |
| GET | `/api/comments/user/{userId}` | 获取用户评论 |
| GET | `/api/comments/search` | 搜索评论 |

### 评论互动

| 方法 | 路径 | 描述 |
|------|------|------|
| POST | `/api/comments/{id}/like` | 点赞评论 |
| DELETE | `/api/comments/{id}/like` | 取消点赞 |
| POST | `/api/comments/{id}/report` | 举报评论 |

### 统计信息

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | `/api/comments/stats/post/{postId}` | 文章评论统计 |
| GET | `/api/comments/stats/user/{userId}` | 用户评论统计 |
| GET | `/api/comments/popular` | 热门评论 |

### 审核管理（需要管理员权限）

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | `/api/admin/comments/moderation-queue` | 审核队列 |
| POST | `/api/admin/comments/moderate` | 批量审核 |
| POST | `/api/admin/comments/{id}/approve` | 批准评论 |
| POST | `/api/admin/comments/{id}/reject` | 拒绝评论 |
| GET | `/api/admin/comments/reports` | 举报列表 |
| POST | `/api/admin/comments/reports/process` | 处理举报 |

## SignalR实时通信

### 连接到评论中心

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/comment")
    .build();

// 启动连接
connection.start().then(() => {
    console.log("Connected to CommentHub");
});
```

### 加入文章评论组

```javascript
// 加入特定文章的评论组
connection.invoke("JoinPostGroup", postId);

// 监听该文章的评论事件
connection.on("CommentCreated", (comment) => {
    // 处理新评论
    addCommentToUI(comment);
});

connection.on("CommentUpdated", (comment) => {
    // 处理评论更新
    updateCommentInUI(comment);
});

connection.on("CommentDeleted", (commentInfo) => {
    // 处理评论删除
    removeCommentFromUI(commentInfo.commentId);
});
```

### 实时输入状态

```javascript
// 用户开始输入
connection.invoke("StartTyping", postId, parentId);

// 用户停止输入
connection.invoke("StopTyping", postId, parentId);

// 监听其他用户输入状态
connection.on("UserStartedTyping", (typingInfo) => {
    showTypingIndicator(typingInfo);
});

connection.on("UserStoppedTyping", (typingInfo) => {
    hideTypingIndicator(typingInfo);
});
```

### 通知系统

```javascript
// 获取未读通知数量
connection.invoke("GetUnreadNotificationCount");

// 监听新通知
connection.on("NewNotification", (notification) => {
    showNotification(notification);
});

// 监听未读数量更新
connection.on("UnreadNotificationCount", (count) => {
    updateNotificationBadge(count);
});

// 标记通知为已读
connection.invoke("MarkNotificationAsRead", notificationId);
```

## 前端集成建议

### React组件结构

```
src/
├── components/
│   ├── comments/
│   │   ├── CommentList.tsx
│   │   ├── CommentItem.tsx
│   │   ├── CommentForm.tsx
│   │   ├── CommentTree.tsx
│   │   └── CommentActions.tsx
│   ├── moderation/
│   │   ├── ModerationQueue.tsx
│   │   ├── ModerationPanel.tsx
│   │   └── ReportManager.tsx
│   └── notifications/
│       ├── NotificationCenter.tsx
│       ├── NotificationItem.tsx
│       └── NotificationSettings.tsx
└── hooks/
    ├── useComments.ts
    ├── useCommentHub.ts
    ├── useNotifications.ts
    └── useModeration.ts
```

### 状态管理（Zustand）

```typescript
interface CommentStore {
  comments: Comment[];
  loading: boolean;
  addComment: (comment: Comment) => void;
  updateComment: (id: string, comment: Partial<Comment>) => void;
  removeComment: (id: string) => void;
  loadComments: (postId: string) => Promise<void>;
}

const useCommentStore = create<CommentStore>((set, get) => ({
  comments: [],
  loading: false,
  // ... 实现
}));
```

### API服务（TanStack Query）

```typescript
// 获取评论列表
export const useComments = (postId: string) => {
  return useQuery({
    queryKey: ['comments', postId],
    queryFn: () => commentApi.getComments(postId),
    staleTime: 5 * 60 * 1000, // 5分钟
  });
};

// 创建评论
export const useCreateComment = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: commentApi.createComment,
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries(['comments', variables.postId]);
    },
  });
};
```

## 性能优化

### 缓存策略

1. **Redis缓存**（生产环境推荐）
```json
{
  "CommentCache": {
    "RedisConnectionString": "localhost:6379"
  }
}
```

2. **内存缓存**（开发环境）
```csharp
services.AddMemoryCache(options =>
{
    options.SizeLimit = 100;
});
```

### 分页优化

- 使用游标分页替代传统分页（大数据量）
- 实现无限滚动加载
- 预加载热门评论

### 数据库优化

- 确保评论表有适当的索引
- 使用读写分离（如果需要）
- 定期清理软删除的数据

## 安全考虑

### 输入验证

- 所有用户输入都经过验证和清理
- HTML内容进行XSS防护
- 文件上传（如果支持）需要严格验证

### 权限控制

- 基于JWT的身份认证
- 角色基础的权限管理
- API限流和防爬虫

### 内容审核

- AI自动审核可疑内容
- 敏感词过滤
- 人工审核工作流
- 用户举报机制

## 监控和日志

### 日志记录

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddFile("Logs/comment-system-{Date}.log");
});
```

### 监控指标

- 评论创建/更新频率
- 审核队列长度
- 缓存命中率
- SignalR连接数
- API响应时间

## 故障排除

### 常见问题

1. **SignalR连接失败**
   - 检查CORS配置
   - 确认JWT令牌有效
   - 检查防火墙设置

2. **缓存不工作**
   - 验证Redis连接
   - 检查缓存键命名
   - 确认过期时间设置

3. **通知不发送**
   - 检查通知服务配置
   - 验证用户通知设置
   - 确认邮件/推送服务配置

### 调试技巧

- 启用详细日志记录
- 使用SignalR调试工具
- 监控数据库查询性能
- 检查API响应时间

## 部署注意事项

### 环境变量

```bash
# 生产环境
export COMMENT_AI_ENABLED=true
export COMMENT_REDIS_CONNECTION="your-redis-connection"
export NOTIFICATION_EMAIL_ENABLED=true
```

### Docker配置

```dockerfile
# 确保包含敏感词文件
COPY Data/sensitive-words.txt /app/Data/
```

### 负载均衡

- SignalR需要启用粘性会话
- 使用Redis作为SignalR背板
- 考虑水平扩展方案

---

本指南涵盖了评论系统的主要集成要点。如需更详细的实现细节，请参考源代码中的注释和单元测试。
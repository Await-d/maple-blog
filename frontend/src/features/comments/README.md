# Maple Blog 评论系统

现代化的React评论系统，支持实时通信、富文本编辑、移动端优化等功能。

## 核心特性

### 🚀 实时功能
- **WebSocket通信**: 基于SignalR的实时评论、点赞、回复同步
- **输入状态**: 实时显示用户正在输入的状态
- **在线统计**: 实时显示在线用户数量
- **通知系统**: 即时推送评论通知和@提及

### 📱 移动端优化
- **响应式设计**: 自适应桌面、平板、手机等不同屏幕
- **触摸友好**: 针对触摸设备优化的界面和交互
- **虚拟键盘**: 智能处理移动端虚拟键盘弹出
- **滑动手势**: 支持滑动关闭、长按等手势操作

### ✏️ 富文本编辑
- **Markdown支持**: 完整的Markdown语法支持
- **实时预览**: 编辑时实时预览渲染效果
- **@提及功能**: 智能用户提及和自动完成
- **表情符号**: 内置表情选择器
- **图片上传**: 支持拖拽和粘贴上传图片

### 🎛️ 高级功能
- **嵌套回复**: 支持多级回复，可配置最大深度
- **智能排序**: 时间、热度、点赞数等多种排序方式
- **虚拟滚动**: 大量评论时的性能优化
- **草稿保存**: 自动保存未提交的评论内容
- **权限控制**: 精细的用户权限和操作控制

## 快速开始

### 基本使用

```tsx
import React from 'react';
import CommentSystem from './features/comments/CommentSystem';

function BlogPost() {
  return (
    <div>
      {/* 文章内容 */}
      <article>
        <h1>文章标题</h1>
        <p>文章内容...</p>
      </article>

      {/* 评论系统 */}
      <CommentSystem
        postId="blog-post-123"
        showStats={true}
        showNotifications={true}
        enableRealtime={true}
      />
    </div>
  );
}
```

### 配置选项

```tsx
<CommentSystem
  // 必需参数
  postId="unique-post-id"

  // 可选参数
  initialSort={CommentSortOrder.CreatedAtDesc}  // 初始排序方式
  showStats={true}                              // 显示统计信息
  showNotifications={true}                      // 显示通知徽章
  maxDepth={3}                                  // 最大嵌套深度
  enableRealtime={true}                         // 启用实时功能
  autoRefresh={false}                           // 自动刷新
  refreshInterval={30000}                       // 刷新间隔(毫秒)
  className="custom-styles"                     // 自定义样式类
/>
```

## 组件架构

### 核心组件

- **CommentSystem**: 主容器组件，整合所有功能
- **CommentList**: 评论列表，支持虚拟滚动和嵌套显示
- **CommentItem**: 单个评论项，包含所有交互功能
- **CommentForm**: 桌面端评论表单，支持富文本编辑
- **MobileCommentForm**: 移动端模态框表单
- **CommentActions**: 评论操作按钮（点赞、回复、编辑等）

### 支持组件

- **CommentStats**: 评论统计信息展示
- **NotificationBadge**: 通知徽章和通知列表
- **UserAvatar**: 用户头像组件
- **CommentEditor**: 富文本编辑器
- **EmojiPicker**: 表情符号选择器
- **MentionSuggestions**: @提及建议列表

### Hook 工具

- **useCommentStore**: Zustand状态管理
- **useCommentSocket**: WebSocket连接管理
- **useCommentNotifications**: 通知系统管理
- **useResponsive**: 响应式设计检测
- **useTouchGestures**: 触摸手势处理

## 状态管理

评论系统使用Zustand进行状态管理，提供以下功能：

```tsx
import { useCommentStore } from './stores/commentStore';

function MyComponent() {
  const {
    // 数据
    comments,
    stats,
    notifications,

    // 状态
    loading,
    errors,

    // 操作方法
    actions: {
      loadComments,
      createComment,
      updateComment,
      deleteComment,
      likeComment,
      // ... 更多操作
    }
  } = useCommentStore();
}
```

## API 服务

### 评论 API

```tsx
import { commentApi } from './services/commentApi';

// 创建评论
const newComment = await commentApi.createComment({
  postId: 'post-123',
  content: '评论内容',
  mentionedUsers: ['user-456']
});

// 获取评论列表
const comments = await commentApi.getComments({
  postId: 'post-123',
  page: 1,
  pageSize: 20,
  sortOrder: CommentSortOrder.CreatedAtDesc
});
```

### WebSocket 连接

```tsx
import { commentSocket } from './services/commentSocket';

// 连接并加入文章组
await commentSocket.connect();
await commentSocket.joinPostGroup('post-123');

// 监听事件
commentSocket.on('CommentCreated', (comment) => {
  console.log('新评论:', comment);
});
```

## 样式定制

评论系统使用Tailwind CSS构建，支持深度定制：

```css
/* 自定义评论系统样式 */
.comment-system {
  /* 主容器样式 */
}

.comment-item {
  /* 评论项样式 */
}

.comment-form {
  /* 表单样式 */
}

/* 深色模式适配 */
.dark .comment-system {
  /* 深色模式样式 */
}
```

## 性能优化

### 虚拟滚动

对于大量评论，自动启用虚拟滚动：

```tsx
<CommentSystem
  postId="popular-post"
  enableVirtualScroll={true}  // 自动根据屏幕尺寸决定
/>
```

### 懒加载

评论图片和头像支持懒加载：

```tsx
// 自动启用，无需额外配置
<img loading="lazy" src={avatarUrl} alt="用户头像" />
```

### 缓存策略

- 评论数据使用智能缓存
- 实时更新时增量同步
- 草稿内容本地存储

## 安全特性

- **XSS防护**: DOMPurify清理用户输入
- **内容审核**: 支持AI和人工审核
- **速率限制**: 客户端和服务端双重限制
- **举报系统**: 用户举报和管理员审核

## 无障碍支持

- **键盘导航**: 完整的键盘操作支持
- **屏幕阅读器**: ARIA标签和语义化HTML
- **高对比度**: 支持系统高对比度模式
- **字体缩放**: 适应系统字体大小设置

## 浏览器支持

- Chrome 80+
- Firefox 75+
- Safari 13+
- Edge 80+
- 移动端浏览器

## 依赖项

主要依赖：
- React 19+
- TypeScript 4.9+
- Tailwind CSS 3.0+
- Zustand 4.0+
- TanStack Query 4.0+
- @microsoft/signalr 6.0+
- date-fns 2.0+
- dompurify 2.4+
- marked 4.0+

## 开发和贡献

### 本地开发

```bash
# 安装依赖
npm install

# 启动开发服务器
npm run dev

# 运行测试
npm test
```

### 构建

```bash
# 构建生产版本
npm run build

# 预览构建结果
npm run preview
```

## 许可证

MIT License - 详见 LICENSE 文件
# 评论系统使用指南

> 过往的 `CommentSystemExample` 组件已从代码库中移除，以下文档展示常见嵌入方式，方便在 Storybook 或开发文档中引用。

## 基础嵌入

```tsx
import CommentSystem from '@/features/comments/CommentSystem';
import { CommentSortOrder } from '@/types/comment';

export function ArticleComments({ postId }: { postId: string }) {
  return (
    <CommentSystem
      postId={postId}
      initialSort={CommentSortOrder.CreatedAtDesc}
      showStats
      showNotifications
      maxDepth={3}
      enableRealtime
    />
  );
}
```

## 侧边栏紧凑模式

```tsx
export function SidebarComments({ postId }: { postId: string }) {
  return (
    <div className="max-w-sm text-sm">
      <CommentSystem
        postId={postId}
        initialSort={CommentSortOrder.HotScore}
        showStats={false}
        showNotifications={false}
        maxDepth={2}
        enableRealtime
      />
    </div>
  );
}
```

## 动态排序示例

```tsx
import { useState } from 'react';

export function SortableComments({ postId }: { postId: string }) {
  const [sortOrder, setSortOrder] = useState(CommentSortOrder.CreatedAtDesc);

  return (
    <>
      <select
        value={sortOrder}
        onChange={(event) => setSortOrder(event.target.value as CommentSortOrder)}
        className="mb-4 rounded-md border px-3 py-2"
      >
        <option value={CommentSortOrder.CreatedAtDesc}>最新优先</option>
        <option value={CommentSortOrder.LikeCountDesc}>最多点赞</option>
        <option value={CommentSortOrder.ReplyCountDesc}>最多回复</option>
        <option value={CommentSortOrder.HotScore}>热度排序</option>
      </select>

      <CommentSystem
        postId={postId}
        initialSort={sortOrder}
        showStats
        showNotifications
        maxDepth={3}
        enableRealtime
      />
    </>
  );
}
```

> 如果需要只读展示，可在上层根据权限隐藏评论表单或直接使用 `showStats` 等只读信息。通过将示例迁移到文档，我们避免了在公共包中导出演示组件，同时保留了清晰的参考代码，降低维护成本并符合 KISS/YAGNI 原则。

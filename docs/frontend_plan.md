# Frontend Delivery Plan

> 本计划用于跟踪 Maple Blog 前端缺失功能的迭代交付。完成任务后请在对应复选框打勾（`[x]`），并提交更新。

## 阶段 1 · 基础设施与观测
- [x] 集成统一错误上报服务（`src/services/errorReporting.ts` + `src/main.tsx` 错误边界）
- [x] 建立 `useNotification` 或复用 `toastService`，替换评论/模态等临时提示
- [x] 完成 Newsletter 订阅 API 封装并接入 `Footer`

## 阶段 2 · 评论系统闭环
- [x] 为 `commentSocket.ts` 增强重连、错误处理、日志与状态回调
- [x] 评论交互（点赞、收藏、删除、图片上传等）接入实际 API 并移除 TODO
- [x] 更新/新增评论相关测试覆盖关键路径

## 阶段 3 · 博客与归档数据
- [x] 在 `blogApi` 提供分类、热门标签等接口并接入到 `BlogPage`
- [x] `CategoryArchive` 真实加载分类文章并持久化至状态
- [x] `BlogPostPage` 点赞/收藏调用后端，含乐观更新与失败回滚

## 阶段 4 · 个性化与用户资料
- [x] `usePersonalization` 记录浏览互动并调用后端接口
- [x] `UserProfilePage` 替换 mock 数据，改用 TanStack Query + 实际 API
- [x] Newsletter/个人设置等表单加入错误提示与防重入控制

## 阶段 5 · 示例与文档整理
- [x] 将示例组件（`ModalExamples`、`CommentSystemExample` 等）迁移至文档或 Storybook
- [x] 清理 placeholder 资源与临时注释，更新 README/开发文档
- [x] 确认前述改动的 E2E/集成验证流程并记录

# 前端验证与测试指南

## 自动化测试

- **单元测试**：
  ```bash
  cd frontend
  npx vitest run src/services/home/__tests__/homeApi.test.ts
  ```
  > 在某些沙箱环境会提示 `listen EPERM` 的 WebSocket 端口告警，不影响测试结果。

- **Lint / 格式**（如需）：
  ```bash
  cd frontend
  npm run lint
  ```

## 手动验证

### Newsletter 订阅

1. 打开页脚订阅表单，输入空邮箱 → 应显示表单错误并阻止提交。
2. 输入非法邮箱（如 `test@invalid`）→ 显示有效性提示。
3. 输入合法邮箱 → 出现加载状态，成功后自动清空表单并显示成功提示。

### 用户资料页

1. 修改基本信息并保存 → 按钮进入 loading 状态，提交成功后出现成功提示。
2. 上传头像 → 上传过程中按钮禁用；失败时有 toast 错误提示并保留原头像。
3. 调整偏好（如主题、语言）→ 发生错误时恢复原值并提示。
4. 密码修改 → 验证空/不匹配/长度不足的错误提示；提交时防重入。
5. 删除账户 → 需输入密码确认，过程中按钮禁用；错误时提示并保留对话框。

## 文档与示例

- 模态框示例：`docs/examples/modal-usage.md`
- 评论系统示例：`docs/examples/comment-system-usage.md`

> 若增加新功能，请同步更新此文档，保持验证步骤可追踪。

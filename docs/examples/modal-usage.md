# Modal 使用示例

> 这些示例源自先前内置的 `ModalExamples` 组件，如今迁移到文档以避免在生产包中携带演示代码。

## 基本模态框

```tsx
import { useState } from 'react';
import Modal from '@/components/ui/Modal';

export function BasicModalExample() {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="rounded-md bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
      >
        打开模态框
      </button>

      <Modal
        isOpen={open}
        onClose={() => setOpen(false)}
        title="示例模态框"
        size="md"
      >
        <p className="text-sm text-gray-600">
          这里可以放置表单、说明文本或任意自定义内容。
        </p>
      </Modal>
    </>
  );
}
```

## 确认操作

```tsx
import { useState } from 'react';
import { ConfirmationModal } from '@/components/ui/Modal';

export function DeleteConfirmButton() {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="rounded-md bg-red-600 px-4 py-2 text-white hover:bg-red-700"
      >
        删除项目
      </button>

      <ConfirmationModal
        isOpen={open}
        onClose={() => setOpen(false)}
        onConfirm={() => {
          // TODO: 执行删除逻辑
          setOpen(false);
        }}
        title="确定要删除吗？"
        message="此操作不可撤销，请谨慎确认。"
        confirmText="删除"
        cancelText="取消"
        variant="destructive"
      />
    </>
  );
}
```

## 提示信息

```tsx
import { useState } from 'react';
import { AlertModal } from '@/components/ui/Modal';

export function ActionSuccessNotice() {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="rounded-md bg-green-600 px-4 py-2 text-white hover:bg-green-700"
      >
        显示成功提示
      </button>

      <AlertModal
        isOpen={open}
        onClose={() => setOpen(false)}
        title="操作成功"
        message="已完成所选操作，您可以继续其他任务。"
        type="success"
      />
    </>
  );
}
```

> 在 Storybook 中添加这些示例可复用以上片段。保持示例与生产组件解耦，能避免演示代码进入最终构建，并使维护成本更低（KISS、YAGNI）。

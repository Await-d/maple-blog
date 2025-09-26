/**
 * 评论编辑器组件
 * 支持纯文本和富文本编辑，Markdown预览
 */

import React, { forwardRef, useImperativeHandle, useRef, useCallback, useMemo } from 'react';
import { CommentEditorConfig } from '../../../types/comment';
import RichTextRenderer from '../../../components/common/RichTextRenderer';

interface CommentEditorProps {
  content: string;
  onChange: (content: string) => void;
  placeholder?: string;
  autoFocus?: boolean;
  isPreviewMode?: boolean;
  compact?: boolean;
  config: CommentEditorConfig;
  className?: string;
  disabled?: boolean;
}

export interface CommentEditorRef {
  focus: () => void;
  blur: () => void;
  insertText: (text: string) => void;
  getSelection: () => { start: number; end: number };
  setSelection: (start: number, end: number) => void;
}

const CommentEditor = forwardRef<CommentEditorRef, CommentEditorProps>(({
  content,
  onChange,
  placeholder = '写下你的想法...',
  autoFocus = false,
  isPreviewMode = false,
  compact = false,
  config,
  className = '',
  disabled = false
}, ref) => {
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // 暴露方法给父组件
  useImperativeHandle(ref, () => ({
    focus: () => {
      textareaRef.current?.focus();
    },
    blur: () => {
      textareaRef.current?.blur();
    },
    insertText: (text: string) => {
      const textarea = textareaRef.current;
      if (!textarea) return;

      const start = textarea.selectionStart;
      const end = textarea.selectionEnd;
      const newContent = content.slice(0, start) + text + content.slice(end);

      onChange(newContent);

      // 设置光标位置到插入文本之后
      setTimeout(() => {
        textarea.setSelectionRange(start + text.length, start + text.length);
        textarea.focus();
      }, 0);
    },
    getSelection: () => {
      const textarea = textareaRef.current;
      return {
        start: textarea?.selectionStart || 0,
        end: textarea?.selectionEnd || 0
      };
    },
    setSelection: (start: number, end: number) => {
      const textarea = textareaRef.current;
      if (textarea) {
        textarea.setSelectionRange(start, end);
        textarea.focus();
      }
    }
  }));

  // 处理内容变化
  const handleChange = useCallback((e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const newContent = e.target.value;

    // 长度限制
    if (newContent.length <= config.maxLength) {
      onChange(newContent);
    }
  }, [onChange, config.maxLength]);

  // 处理键盘事件
  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    // Ctrl+Enter 或 Cmd+Enter 提交
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
      const form = textareaRef.current?.closest('form');
      if (form) {
        e.preventDefault();
        const submitButton = form.querySelector('button[type="submit"]') as HTMLButtonElement;
        if (submitButton && !submitButton.disabled) {
          submitButton.click();
        }
      }
    }

    // Tab 键处理
    if (e.key === 'Tab' && config.allowMarkdown) {
      e.preventDefault();
      const textarea = textareaRef.current!;
      const start = textarea.selectionStart;
      const end = textarea.selectionEnd;

      // 插入制表符或缩进
      const newContent = content.slice(0, start) + '    ' + content.slice(end);
      onChange(newContent);

      // 设置光标位置
      setTimeout(() => {
        textarea.setSelectionRange(start + 4, start + 4);
      }, 0);
    }

    // 智能列表
    if (e.key === 'Enter' && config.allowMarkdown) {
      const textarea = textareaRef.current!;
      const start = textarea.selectionStart;
      const lines = content.slice(0, start).split('\n');
      const currentLine = lines[lines.length - 1];

      // 检查是否是列表项
      const listMatch = currentLine.match(/^(\s*)([-*+]|\d+\.)\s/);
      if (listMatch) {
        e.preventDefault();
        const indent = listMatch[1];
        const marker = listMatch[2];

        let newMarker = marker;
        if (/\d+\./.test(marker)) {
          // 数字列表，递增
          const num = parseInt(marker);
          newMarker = `${num + 1}.`;
        }

        const newContent = content.slice(0, start) + `\n${indent}${newMarker} ` + content.slice(start);
        onChange(newContent);

        // 设置光标位置
        setTimeout(() => {
          const newPos = start + indent.length + newMarker.length + 2;
          textarea.setSelectionRange(newPos, newPos);
        }, 0);
      }
    }
  }, [content, onChange, config.allowMarkdown]);

  // 处理粘贴事件
  const handlePaste = useCallback((e: React.ClipboardEvent<HTMLTextAreaElement>) => {
    if (!config.allowImageUpload) return;

    const items = e.clipboardData.items;
    for (let i = 0; i < items.length; i++) {
      const item = items[i];
      if (item.type.startsWith('image/')) {
        e.preventDefault();

        const file = item.getAsFile();
        if (file) {
          // TODO: Implement image upload flow - can pass uploadImage function via props
        }
        break;
      }
    }
  }, [config.allowImageUpload]);

  // 自动调整高度
  const handleTextareaRef = useCallback((element: HTMLTextAreaElement | null) => {
    if (element) {
      textareaRef.current = element;

      const adjustHeight = () => {
        element.style.height = 'auto';
        const maxHeight = compact ? 200 : 400;
        element.style.height = Math.min(element.scrollHeight, maxHeight) + 'px';
      };

      adjustHeight();
      element.addEventListener('input', adjustHeight);

      return () => {
        element.removeEventListener('input', adjustHeight);
      };
    }
  }, [compact]);

  // 预览内容
  const previewContent = useMemo(() => {
    if (!isPreviewMode || !content.trim()) {
      return null;
    }

    return <RichTextRenderer content={content} />;
  }, [content, isPreviewMode]);

  // 计算样式
  const textareaClasses = `
    w-full resize-none border-0 focus:ring-0 focus:outline-none
    bg-transparent text-gray-900 dark:text-gray-100
    ${compact ? 'text-sm' : 'text-base'}
    ${compact ? 'min-h-[80px]' : 'min-h-[120px]'}
    placeholder-gray-400 dark:placeholder-gray-500
    ${className}
  `;

  if (isPreviewMode) {
    return (
      <div className={`p-4 ${compact ? 'min-h-[80px]' : 'min-h-[120px]'}`}>
        {content.trim() ? (
          <div className="prose prose-sm max-w-none text-gray-700 dark:text-gray-300 prose-blue dark:prose-invert">
            {previewContent}
          </div>
        ) : (
          <div className="text-gray-400 dark:text-gray-500 italic">
            {placeholder}
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="relative">
      <textarea
        ref={handleTextareaRef}
        value={content}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        onPaste={handlePaste}
        placeholder={placeholder}
        disabled={disabled}
        autoFocus={autoFocus}
        className={textareaClasses}
        style={{
          minHeight: compact ? '80px' : '120px',
          maxHeight: compact ? '200px' : '400px'
        }}
        spellCheck="false"
        autoComplete="off"
        autoCorrect="off"
        autoCapitalize="off"
      />

      {/* 长度警告 */}
      {content.length > config.maxLength * 0.9 && (
        <div className="absolute bottom-2 right-2 text-xs text-orange-500 dark:text-orange-400 bg-white dark:bg-gray-800 px-2 py-1 rounded shadow-sm">
          还可输入 {config.maxLength - content.length} 字符
        </div>
      )}

      {/* 超出长度提示 */}
      {content.length > config.maxLength && (
        <div className="absolute bottom-2 right-2 text-xs text-red-500 dark:text-red-400 bg-white dark:bg-gray-800 px-2 py-1 rounded shadow-sm">
          超出 {content.length - config.maxLength} 字符
        </div>
      )}
    </div>
  );
});

CommentEditor.displayName = 'CommentEditor';

export default CommentEditor;
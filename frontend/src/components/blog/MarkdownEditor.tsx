import React, { useState, useCallback, useRef, useEffect } from 'react';
import {
  BoldIcon,
  ItalicIcon,
  LinkIcon,
  PhotoIcon,
  CodeBracketIcon,
  ListBulletIcon,
  NumberedListIcon,
  EyeIcon
} from '@heroicons/react/24/outline';

interface MarkdownEditorProps {
  value: string
  onChange: (value: string) => void
  placeholder?: string
  height?: string
  enablePreview?: boolean
  onImageUpload?: (file: File) => Promise<string>
  disabled?: boolean
  'data-testid'?: string
}

interface ToolbarButton {
  icon: React.ComponentType<{ className?: string }>
  label: string
  action: () => void
  shortcut?: string
}

export const MarkdownEditor: React.FC<MarkdownEditorProps> = ({
  value,
  onChange,
  placeholder = 'Write your post content in Markdown...',
  height = '400px',
  enablePreview = true,
  onImageUpload,
  disabled = false,
  'data-testid': testId = 'markdown-editor'
}) => {
  const [isPreviewMode, setIsPreviewMode] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Auto-resize textarea
  useEffect(() => {
    const textarea = textareaRef.current;
    if (textarea) {
      textarea.style.height = 'auto';
      textarea.style.height = `${textarea.scrollHeight}px`;
    }
  }, [value]);

  const insertText = useCallback((before: string, after: string = '', placeholder: string = '') => {
    const textarea = textareaRef.current;
    if (!textarea || disabled) return;

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const selectedText = value.substring(start, end);
    const textToInsert = selectedText || placeholder;

    const newValue =
      value.substring(0, start) +
      before +
      textToInsert +
      after +
      value.substring(end);

    onChange(newValue);

    // Set cursor position
    setTimeout(() => {
      const newStart = start + before.length;
      const newEnd = newStart + textToInsert.length;
      textarea.setSelectionRange(newStart, newEnd);
      textarea.focus();
    }, 0);
  }, [value, onChange, disabled]);

  const handleImageUpload = useCallback(async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file || !onImageUpload) return;

    try {
      setIsUploading(true);
      const imageUrl = await onImageUpload(file);
      insertText(`![${file.name}](${imageUrl})`);
    } catch (error) {
      console.error('Image upload failed:', error);
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  }, [insertText, onImageUpload]);

  const handleKeyDown = useCallback((event: React.KeyboardEvent) => {
    if (disabled) return;

    // Handle keyboard shortcuts
    if (event.ctrlKey || event.metaKey) {
      switch (event.key) {
        case 'b':
          event.preventDefault();
          insertText('**', '**', 'bold text');
          break;
        case 'i':
          event.preventDefault();
          insertText('*', '*', 'italic text');
          break;
        case 'k':
          event.preventDefault();
          insertText('[', '](url)', 'link text');
          break;
      }
    }

    // Handle tab for indentation
    if (event.key === 'Tab') {
      event.preventDefault();
      const textarea = event.currentTarget as HTMLTextAreaElement;
      const start = textarea.selectionStart;
      const end = textarea.selectionEnd;

      const newValue = value.substring(0, start) + '  ' + value.substring(end);
      onChange(newValue);

      setTimeout(() => {
        textarea.setSelectionRange(start + 2, start + 2);
      }, 0);
    }
  }, [insertText, onChange, value, disabled]);

  const toolbarButtons: ToolbarButton[] = [
    {
      icon: BoldIcon,
      label: 'Bold',
      action: () => insertText('**', '**', 'bold text'),
      shortcut: 'Ctrl+B'
    },
    {
      icon: ItalicIcon,
      label: 'Italic',
      action: () => insertText('*', '*', 'italic text'),
      shortcut: 'Ctrl+I'
    },
    {
      icon: LinkIcon,
      label: 'Link',
      action: () => insertText('[', '](url)', 'link text'),
      shortcut: 'Ctrl+K'
    },
    {
      icon: PhotoIcon,
      label: 'Image',
      action: () => fileInputRef.current?.click()
    },
    {
      icon: CodeBracketIcon,
      label: 'Code',
      action: () => insertText('`', '`', 'code')
    },
    {
      icon: ListBulletIcon,
      label: 'Bullet List',
      action: () => insertText('- ', '', 'list item')
    },
    {
      icon: NumberedListIcon,
      label: 'Numbered List',
      action: () => insertText('1. ', '', 'list item')
    }
  ];

  const renderPreview = () => {
    // Enhanced Markdown preview with proper rendering
    if (!value) {
      return (
        <div
          className="prose max-w-none p-4 bg-gray-50 rounded-md min-h-full flex items-center justify-center text-gray-500"
          data-testid="markdown-preview"
        >
          Nothing to preview
        </div>
      );
    }

    // Basic markdown rendering for preview
    // In a real implementation, you would use a library like react-markdown
    const processMarkdown = (text: string) => {
      return text
        // Headers
        .replace(/^### (.*$)/gim, '<h3 class="text-lg font-semibold mt-4 mb-2">$1</h3>')
        .replace(/^## (.*$)/gim, '<h2 class="text-xl font-bold mt-6 mb-3">$1</h2>')
        .replace(/^# (.*$)/gim, '<h1 class="text-2xl font-bold mt-8 mb-4">$1</h1>')
        // Bold and Italic
        .replace(/\*\*(.*)\*\*/gim, '<strong class="font-bold">$1</strong>')
        .replace(/\*(.*)\*/gim, '<em class="italic">$1</em>')
        // Links
        .replace(/\[([^\]]+)\]\(([^)]+)\)/gim, '<a href="$2" class="text-blue-600 hover:text-blue-800 underline" target="_blank" rel="noopener noreferrer">$1</a>')
        // Code inline
        .replace(/`([^`]+)`/gim, '<code class="bg-gray-200 px-1 py-0.5 rounded text-sm font-mono">$1</code>')
        // Line breaks
        .replace(/\n/gim, '<br/>');
    };

    return (
      <div
        className="prose prose-sm max-w-none p-4 bg-gray-50 rounded-md min-h-full"
        data-testid="markdown-preview"
        dangerouslySetInnerHTML={{ __html: processMarkdown(value) }}
      />
    );
  };

  return (
    <div className="border border-gray-300 rounded-lg overflow-hidden" data-testid={testId}>
      {/* Toolbar */}
      <div className="flex items-center justify-between px-3 py-2 bg-gray-50 border-b border-gray-200">
        <div className="flex items-center space-x-1">
          {toolbarButtons.map((button, index) => (
            <button
              key={index}
              type="button"
              onClick={button.action}
              disabled={disabled || (button.icon === PhotoIcon && isUploading)}
              className={`
                p-2 rounded hover:bg-gray-200 disabled:opacity-50 disabled:cursor-not-allowed
                ${isUploading && button.icon === PhotoIcon ? 'animate-pulse' : ''}
              `}
              title={`${button.label}${button.shortcut ? ` (${button.shortcut})` : ''}`}
              data-testid={`toolbar-${button.label.toLowerCase().replace(' ', '-')}`}
            >
              <button.icon className="w-4 h-4" />
            </button>
          ))}
        </div>

        {enablePreview && (
          <button
            type="button"
            onClick={() => setIsPreviewMode(!isPreviewMode)}
            className={`
              flex items-center space-x-2 px-3 py-1 rounded text-sm
              ${isPreviewMode ? 'bg-blue-100 text-blue-700' : 'hover:bg-gray-200'}
            `}
            data-testid="preview-toggle"
          >
            <EyeIcon className="w-4 h-4" />
            <span>{isPreviewMode ? 'Edit' : 'Preview'}</span>
          </button>
        )}
      </div>

      {/* Content Area */}
      <div className="relative">
        {isPreviewMode ? (
          renderPreview()
        ) : (
          <textarea
            ref={textareaRef}
            value={value}
            onChange={(e) => onChange(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder={placeholder}
            disabled={disabled}
            className={`
              w-full p-4 border-0 outline-none resize-none font-mono text-sm
              disabled:bg-gray-50 disabled:cursor-not-allowed
              ${height ? '' : 'min-h-[400px]'}
            `}
            style={{ height: height }}
            data-testid="markdown-textarea"
          />
        )}

        {/* Character count */}
        <div className="absolute bottom-2 right-3 text-xs text-gray-400 bg-white px-2 rounded">
          {value.length} characters
        </div>
      </div>

      {/* Hidden file input for image uploads */}
      {onImageUpload && (
        <input
          ref={fileInputRef}
          type="file"
          accept="image/*"
          onChange={handleImageUpload}
          className="hidden"
          data-testid="image-upload-input"
        />
      )}
    </div>
  );
};

export default MarkdownEditor;
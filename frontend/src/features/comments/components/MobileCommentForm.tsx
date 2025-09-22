// @ts-nocheck
/**
 * 移动端评论表单组件
 * 专门为移动设备优化的评论表单
 * 支持滑动操作、触摸友好、虚拟键盘适配
 */

import React, { useState, useRef, useEffect, useCallback } from 'react';
import { createPortal } from 'react-dom';
import { useCommentStore } from '../../../stores/commentStore';
import { CommentFormData } from '../../../types/comment';
import { useAuth } from '../../../hooks/useAuth';
import CommentEditor from './CommentEditor';
import EmojiPicker from './EmojiPicker';
import MentionSuggestions from './MentionSuggestions';

interface MobileCommentFormProps {
  postId: string;
  parentId?: string;
  initialContent?: string;
  placeholder?: string;
  isEditing?: boolean;
  commentId?: string;
  onCancel?: () => void;
  onSubmitSuccess?: (comment: any) => void;
  isOpen: boolean;
  onClose: () => void;
}

const MobileCommentForm: React.FC<MobileCommentFormProps> = ({
  postId,
  parentId,
  initialContent = '',
  placeholder,
  isEditing = false,
  commentId,
  onCancel,
  onSubmitSuccess,
  isOpen,
  onClose
}) => {
  const { user } = useAuth();
  const { actions, loadingStates, config } = useCommentStore();

  // 表单状态
  const [formData, setFormData] = useState<CommentFormData>({
    content: initialContent,
    mentionedUsers: [],
    parentId
  });

  // UI状态
  const [showEmojiPicker, setShowEmojiPicker] = useState(false);
  const [showMentions, setShowMentions] = useState(false);
  const [mentionQuery, setMentionQuery] = useState('');
  const [mentionPosition, setMentionPosition] = useState(0);
  const [keyboardHeight, setKeyboardHeight] = useState(0);

  // Refs
  const modalRef = useRef<HTMLDivElement>(null);
  const editorRef = useRef<any>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // 检测虚拟键盘
  useEffect(() => {
    if (!isOpen) return;

    const handleResize = () => {
      if (window.visualViewport) {
        const keyboardHeight = window.innerHeight - window.visualViewport.height;
        setKeyboardHeight(Math.max(0, keyboardHeight));
      }
    };

    if (window.visualViewport) {
      window.visualViewport.addEventListener('resize', handleResize);
      return () => window.visualViewport?.removeEventListener('resize', handleResize);
    }

    // 降级方案：监听 window resize
    const handleWindowResize = () => {
      // 简单的键盘检测：当窗口高度显著减少时认为键盘打开
      const currentHeight = window.innerHeight;
      const originalHeight = window.screen.height;
      const heightDiff = originalHeight - currentHeight;

      if (heightDiff > 200) {
        setKeyboardHeight(heightDiff);
      } else {
        setKeyboardHeight(0);
      }
    };

    window.addEventListener('resize', handleWindowResize);
    return () => window.removeEventListener('resize', handleWindowResize);
  }, [isOpen]);

  // 防止背景滚动
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
      // iOS Safari 需要额外设置
      document.documentElement.style.overflow = 'hidden';

      return () => {
        document.body.style.overflow = '';
        document.documentElement.style.overflow = '';
      };
    }
  }, [isOpen]);

  // 处理内容变化
  const handleContentChange = useCallback((content: string) => {
    setFormData(prev => ({ ...prev, content }));

    // 检测 @提及
    const atIndex = content.lastIndexOf('@');
    if (atIndex !== -1) {
      const afterAt = content.slice(atIndex + 1);
      if (afterAt.length > 0 && !afterAt.includes(' ')) {
        setMentionQuery(afterAt);
        setMentionPosition(atIndex);
        setShowMentions(true);
      } else {
        setShowMentions(false);
      }
    } else {
      setShowMentions(false);
    }
  }, []);

  // 处理提及选择
  const handleMentionSelect = useCallback((user: any) => {
    const { content } = formData;
    const beforeMention = content.slice(0, mentionPosition);
    const afterMention = content.slice(mentionPosition + mentionQuery.length + 1);
    const newContent = `${beforeMention}@${user.username} ${afterMention}`;

    setFormData(prev => ({
      ...prev,
      content: newContent,
      mentionedUsers: [...prev.mentionedUsers.filter(id => id !== user.id), user.id]
    }));

    setShowMentions(false);
    editorRef.current?.focus();
  }, [formData, mentionPosition, mentionQuery]);

  // 处理表情选择
  const handleEmojiSelect = useCallback((emoji: string) => {
    const currentContent = formData.content;
    const newContent = currentContent + emoji;

    setFormData(prev => ({ ...prev, content: newContent }));
    setShowEmojiPicker(false);
    editorRef.current?.focus();
  }, [formData.content]);

  // 表单提交
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.content.trim() || !user) {
      return;
    }

    try {
      let result;

      if (isEditing && commentId) {
        await actions.updateComment(commentId, {
          content: formData.content,
          mentionedUsers: formData.mentionedUsers
        });
        result = { success: true };
      } else {
        result = await actions.createComment(formData, postId);
      }

      if (result) {
        // 清除表单
        setFormData({
          content: '',
          mentionedUsers: [],
          parentId
        });

        // 回调
        if (onSubmitSuccess) {
          onSubmitSuccess(result);
        }

        onClose();
      }
    } catch (error) {
      console.error('Submit comment failed:', error);
    }
  };

  // 关闭处理
  const handleClose = () => {
    if (onCancel) {
      onCancel();
    }
    onClose();
  };

  // 滑动关闭手势
  const [dragStartY, setDragStartY] = useState<number | null>(null);
  const [dragCurrentY, setDragCurrentY] = useState<number | null>(null);

  const handleTouchStart = (e: React.TouchEvent) => {
    setDragStartY(e.touches[0].clientY);
  };

  const handleTouchMove = (e: React.TouchEvent) => {
    if (dragStartY !== null) {
      setDragCurrentY(e.touches[0].clientY);
    }
  };

  const handleTouchEnd = () => {
    if (dragStartY !== null && dragCurrentY !== null) {
      const dragDistance = dragCurrentY - dragStartY;
      // 向下滑动超过100px时关闭
      if (dragDistance > 100) {
        handleClose();
      }
    }
    setDragStartY(null);
    setDragCurrentY(null);
  };

  const isLoading = loadingStates[isEditing ? `updating_${commentId}` : 'creating'] || false;
  const canSubmit = formData.content.trim().length > 0 && user && !isLoading;

  const finalPlaceholder = placeholder ||
    (parentId ? '写下你的回复...' : '分享你的想法...');

  if (!isOpen) return null;

  const modalStyle = {
    transform: dragCurrentY !== null && dragStartY !== null
      ? `translateY(${Math.max(0, dragCurrentY - dragStartY)}px)`
      : 'translateY(0)',
    paddingBottom: keyboardHeight > 0 ? `${Math.max(16, keyboardHeight - 100)}px` : '16px'
  };

  return createPortal(
    <div className="fixed inset-0 z-50 bg-black bg-opacity-50 flex items-end">
      <div
        ref={modalRef}
        className="w-full bg-white dark:bg-gray-800 rounded-t-xl shadow-2xl transform transition-transform duration-300 max-h-[90vh] flex flex-col"
        style={modalStyle}
        onTouchStart={handleTouchStart}
        onTouchMove={handleTouchMove}
        onTouchEnd={handleTouchEnd}
      >
        {/* 拖动指示器 */}
        <div className="flex justify-center py-2 bg-gray-50 dark:bg-gray-750 rounded-t-xl">
          <div className="w-8 h-1 bg-gray-300 dark:bg-gray-600 rounded-full"></div>
        </div>

        {/* 头部 */}
        <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-700">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
            {isEditing ? '编辑评论' : (parentId ? '回复评论' : '发表评论')}
          </h3>

          <button
            onClick={handleClose}
            className="p-2 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300 rounded-lg"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* 表单内容 */}
        <form onSubmit={handleSubmit} className="flex flex-col flex-1 overflow-hidden">
          {/* 编辑器区域 */}
          <div className="flex-1 overflow-y-auto">
            <div className="p-4">
              <CommentEditor
                ref={editorRef}
                content={formData.content}
                onChange={handleContentChange}
                placeholder={finalPlaceholder}
                autoFocus={true}
                compact={true}
                config={config}
                className="min-h-[120px] text-base" // 移动端更大的文字
              />

              {/* @提及建议 */}
              {showMentions && (
                <MentionSuggestions
                  query={mentionQuery}
                  onSelect={handleMentionSelect}
                  onClose={() => setShowMentions(false)}
                  className="mt-2"
                />
              )}

              {/* 表情选择器 */}
              {showEmojiPicker && (
                <div className="mt-2">
                  <EmojiPicker
                    onEmojiSelect={handleEmojiSelect}
                    onClose={() => setShowEmojiPicker(false)}
                  />
                </div>
              )}
            </div>
          </div>

          {/* 工具栏 */}
          <div className="border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-750">
            <div className="flex items-center justify-between px-4 py-3">
              {/* 左侧工具按钮 */}
              <div className="flex items-center space-x-3">
                {/* 表情按钮 */}
                <button
                  type="button"
                  onClick={() => setShowEmojiPicker(!showEmojiPicker)}
                  className="p-2 text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 rounded-lg touch-manipulation"
                >
                  <span className="text-xl">😀</span>
                </button>

                {/* @提及按钮 */}
                <button
                  type="button"
                  onClick={() => {
                    const newContent = formData.content + '@';
                    setFormData(prev => ({ ...prev, content: newContent }));
                    editorRef.current?.focus();
                  }}
                  className="p-2 text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 rounded-lg touch-manipulation"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 12a4 4 0 10-8 0 4 4 0 008 0zm0 0v1.5a2.5 2.5 0 005 0V12a9 9 0 10-9 9m4.5-1.206a8.959 8.959 0 01-4.5 1.207" />
                  </svg>
                </button>

                {/* 字数统计 */}
                <span className={`text-xs ${
                  formData.content.length > config.maxLength * 0.8
                    ? 'text-orange-500'
                    : formData.content.length > config.maxLength
                    ? 'text-red-500'
                    : 'text-gray-400 dark:text-gray-500'
                }`}>
                  {formData.content.length}/{config.maxLength}
                </span>
              </div>

              {/* 右侧操作按钮 */}
              <div className="flex items-center space-x-2">
                <button
                  type="button"
                  onClick={handleClose}
                  className="px-4 py-2 text-sm text-gray-700 dark:text-gray-300 border border-gray-300 dark:border-gray-600 rounded-lg touch-manipulation"
                >
                  取消
                </button>

                <button
                  type="submit"
                  disabled={!canSubmit}
                  className="px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed rounded-lg transition-colors touch-manipulation flex items-center space-x-2"
                >
                  {isLoading && (
                    <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                  )}
                  <span>
                    {isLoading
                      ? (isEditing ? '更新中...' : '发布中...')
                      : (isEditing ? '更新' : '发布')
                    }
                  </span>
                </button>
              </div>
            </div>
          </div>
        </form>
      </div>
    </div>,
    document.body
  );
};

export default MobileCommentForm;
/**
 * 评论表单组件
 * 支持创建新评论、回复评论、编辑评论
 * 包含富文本编辑、@提及、图片上传、实时预览等功能
 */

import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useCommentStore } from '../../../stores/commentStore';
import { useCommentSocket } from '../../../services/commentSocket';
import { CommentFormData, Comment, CommentAuthor } from '../../../types/comment';
import { CommentEditorRef } from './CommentEditor';
import { useAuth } from '../../../hooks/useAuth';
import CommentEditor from './CommentEditor';
import MentionSuggestions from './MentionSuggestions';
import ImageUploadModal from './ImageUploadModal';
import EmojiPicker from './EmojiPicker';
import { uploadApi } from '../../../services/uploadApi';
import { toastService } from '../../../services/toastService';
import { errorReporter } from '../../../services/errorReporting';

interface CommentFormProps {
  postId: string;
  parentId?: string;
  initialContent?: string;
  placeholder?: string;
  autoFocus?: boolean;
  isEditing?: boolean;
  commentId?: string;
  onCancel?: () => void;
  onSubmitSuccess?: (comment: Comment) => void;
  className?: string;
  compact?: boolean;
  showPreview?: boolean;
}

const CommentForm: React.FC<CommentFormProps> = ({
  postId,
  parentId,
  initialContent = '',
  placeholder,
  autoFocus = false,
  isEditing = false,
  commentId,
  onCancel,
  onSubmitSuccess,
  className = '',
  compact = false,
  showPreview = true
}) => {
  const { user } = useAuth();
  const { actions, loadingStates, config } = useCommentStore();
  const commentSocket = useCommentSocket();

  // 表单状态
  const [formData, setFormData] = useState<CommentFormData>({
    content: initialContent,
    mentionedUsers: [],
    parentId
  });

  // UI状态
  const [isPreviewMode, setIsPreviewMode] = useState(false);
  const [showEmojiPicker, setShowEmojiPicker] = useState(false);
  const [showImageUpload, setShowImageUpload] = useState(false);
  const [showMentions, setShowMentions] = useState(false);
  const [mentionQuery, setMentionQuery] = useState('');
  const [mentionPosition, setMentionPosition] = useState(0);

  // Refs
  const formRef = useRef<HTMLFormElement>(null);
  const editorRef = useRef<CommentEditorRef>(null);
  const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // 草稿键
  const draftKey = parentId ? `${postId}_${parentId}` : postId;

  // 加载草稿
  useEffect(() => {
    if (!isEditing && !initialContent) {
      const draft = actions.loadDraft(draftKey);
      if (draft) {
        setFormData(draft);
      }
    }
  }, [draftKey, isEditing, initialContent, actions]);

  // 自动保存草稿
  useEffect(() => {
    if (!isEditing && formData.content.trim()) {
      const timeout = setTimeout(() => {
        actions.saveDraft(draftKey, formData);
      }, 1000);

      return () => clearTimeout(timeout);
    }
  }, [formData, draftKey, isEditing, actions]);

  // 处理输入变化
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

    // 发送输入状态
    if (commentSocket.isConnected) {
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }

      commentSocket.startTyping(postId, parentId);

      typingTimeoutRef.current = setTimeout(() => {
        commentSocket.stopTyping(postId, parentId);
      }, 2000);
    }
  }, [postId, parentId, commentSocket]);

  // 处理提及选择
  const handleMentionSelect = useCallback((user: CommentAuthor) => {
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

  // 处理图片上传
  const handleImageInsert = useCallback((imageUrl: string, alt?: string) => {
    const markdownImage = `![${alt || ''}](${imageUrl})`;
    const newContent = formData.content + '\n' + markdownImage;

    setFormData(prev => ({ ...prev, content: newContent }));
    setShowImageUpload(false);
    editorRef.current?.focus();
  }, [formData.content]);

  const handleInlineImageUpload = useCallback(async (file: File) => {
    const toastId = toastService.loading('图片上传中…', {
      groupId: `comment-inline-upload-${postId}`,
    });

    try {
      const result = await uploadApi.uploadImage(file);
      handleImageInsert(
        result.url,
        result.filename ? result.filename.replace(/\.[^/.]+$/, '') : undefined
      );
      toastService.completeLoading(toastId, '图片上传成功');
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      toastService.failLoading(toastId, err.message || '图片上传失败，请稍后重试');
      errorReporter.captureError(err, {
        component: 'CommentForm',
        action: 'inlineImageUpload',
        handled: true,
        extra: {
          postId,
          parentId,
          fileName: file.name,
        },
      });
    }
  }, [handleImageInsert, parentId, postId]);

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

        // 清除草稿
        actions.clearDraft(draftKey);

        // 停止输入状态
        if (commentSocket.isConnected) {
          commentSocket.stopTyping(postId, parentId);
        }

        // 回调
        if (onSubmitSuccess && 'id' in result) {
          onSubmitSuccess(result);
        }

        if (onCancel) {
          onCancel();
        }
      }
    } catch (error) {
      console.error('Submit comment failed:', error);
    }
  };

  // 取消操作
  const handleCancel = () => {
    if (!isEditing) {
      // 保存为草稿
      if (formData.content.trim()) {
        actions.saveDraft(draftKey, formData);
      }
    }

    // 停止输入状态
    if (commentSocket.isConnected) {
      commentSocket.stopTyping(postId, parentId);
    }

    if (onCancel) {
      onCancel();
    }
  };

  // 清空表单
  const handleClear = () => {
    setFormData({
      content: '',
      mentionedUsers: [],
      parentId
    });
    actions.clearDraft(draftKey);
  };

  const isLoading = loadingStates[isEditing ? `updating_${commentId}` : 'creating'] || false;
  const canSubmit = formData.content.trim().length > 0 && user && !isLoading;

  const finalPlaceholder = placeholder ||
    (parentId ? '写下你的回复...' : '分享你的想法...') +
    (config.allowMarkdown ? ' (支持 Markdown)' : '');

  return (
    <form
      ref={formRef}
      onSubmit={handleSubmit}
      className={`comment-form ${className} ${compact ? 'compact' : ''}`}
      id={!parentId ? 'comment-form' : undefined}
    >
      <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 shadow-sm">
        {/* 工具栏 */}
        <div className="flex items-center justify-between px-4 py-2 border-b border-gray-200 dark:border-gray-700">
          <div className="flex items-center space-x-2">
            {/* 格式化按钮 */}
            {config.allowMarkdown && (
              <div className="flex items-center space-x-1">
                <button
                  type="button"
                  onClick={() => editorRef.current?.insertText('**粗体**')}
                  className="p-1.5 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 rounded hover:bg-gray-100 dark:hover:bg-gray-700"
                  title="粗体"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 4v16M10 4v16M14 8h4M14 16h4" />
                  </svg>
                </button>

                <button
                  type="button"
                  onClick={() => editorRef.current?.insertText('*斜体*')}
                  className="p-1.5 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 rounded hover:bg-gray-100 dark:hover:bg-gray-700"
                  title="斜体"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 4l4 16M6 8l12 0M4 16l12 0" />
                  </svg>
                </button>

                <button
                  type="button"
                  onClick={() => editorRef.current?.insertText('[链接文本](链接地址)')}
                  className="p-1.5 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 rounded hover:bg-gray-100 dark:hover:bg-gray-700"
                  title="插入链接"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
                  </svg>
                </button>

                <button
                  type="button"
                  onClick={() => editorRef.current?.insertText('```\n代码\n```')}
                  className="p-1.5 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 rounded hover:bg-gray-100 dark:hover:bg-gray-700"
                  title="插入代码"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" />
                  </svg>
                </button>

                <div className="w-px h-4 bg-gray-300 dark:bg-gray-600"></div>
              </div>
            )}

            {/* 表情按钮 */}
            {config.allowEmoji && (
              <div className="relative">
                <button
                  type="button"
                  onClick={() => setShowEmojiPicker(!showEmojiPicker)}
                  className="p-1.5 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 rounded hover:bg-gray-100 dark:hover:bg-gray-700"
                  title="插入表情"
                >
                  😀
                </button>
                {showEmojiPicker && (
                  <EmojiPicker
                    onEmojiSelect={handleEmojiSelect}
                    onClose={() => setShowEmojiPicker(false)}
                  />
                )}
              </div>
            )}

            {/* 图片上传按钮 */}
            {config.allowImageUpload && (
              <button
                type="button"
                onClick={() => setShowImageUpload(true)}
                className="p-1.5 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 rounded hover:bg-gray-100 dark:hover:bg-gray-700"
                title="插入图片"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
              </button>
            )}
          </div>

          <div className="flex items-center space-x-2">
            {/* 预览切换 */}
            {showPreview && config.allowMarkdown && (
              <button
                type="button"
                onClick={() => setIsPreviewMode(!isPreviewMode)}
                className={`px-2 py-1 text-sm rounded transition-colors ${
                  isPreviewMode
                    ? 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300'
                    : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'
                }`}
              >
                {isPreviewMode ? '编辑' : '预览'}
              </button>
            )}

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
        </div>

        {/* 编辑器 */}
        <div className="relative">
          <CommentEditor
            ref={editorRef}
            content={formData.content}
            onChange={handleContentChange}
            placeholder={finalPlaceholder}
            autoFocus={autoFocus}
            isPreviewMode={isPreviewMode}
            compact={compact}
            config={config}
            onImagePaste={config.allowImageUpload ? handleInlineImageUpload : undefined}
          />

          {/* @提及建议 */}
          {showMentions && (
            <MentionSuggestions
              query={mentionQuery}
              onSelect={handleMentionSelect}
              onClose={() => setShowMentions(false)}
            />
          )}
        </div>

        {/* 操作按钮 */}
        <div className="flex items-center justify-between px-4 py-3 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-750 rounded-b-lg">
          <div className="flex items-center space-x-2 text-sm text-gray-500 dark:text-gray-400">
            {config.allowMarkdown && (
              <span>支持 Markdown</span>
            )}
            {config.allowMention && (
              <span>支持 @提及</span>
            )}
          </div>

          <div className="flex items-center space-x-2">
            {/* 清空按钮 */}
            {formData.content.trim() && (
              <button
                type="button"
                onClick={handleClear}
                className="px-3 py-1.5 text-sm text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200"
              >
                清空
              </button>
            )}

            {/* 取消按钮 */}
            {onCancel && (
              <button
                type="button"
                onClick={handleCancel}
                className="px-4 py-2 text-sm text-gray-700 dark:text-gray-300 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
              >
                取消
              </button>
            )}

            {/* 提交按钮 */}
            <button
              type="submit"
              disabled={!canSubmit}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed rounded-lg transition-colors flex items-center space-x-2"
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
                  : (isEditing ? '更新评论' : (parentId ? '发布回复' : '发布评论'))
                }
              </span>
            </button>
          </div>
        </div>
      </div>

      {/* 图片上传模态框 */}
      {showImageUpload && (
        <ImageUploadModal
          onImageInsert={handleImageInsert}
          onClose={() => setShowImageUpload(false)}
        />
      )}
    </form>
  );
};

export default CommentForm;

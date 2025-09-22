// @ts-nocheck
/**
 * 单个评论项组件
 * 支持嵌套显示、互动操作、编辑等功能
 */

import React, { useState, useMemo } from 'react';
import { formatDistanceToNow } from 'date-fns';
import { zhCN } from 'date-fns/locale';
import { Comment, CommentStatus } from '../../../types/comment';
import UserAvatar from '../../../components/common/UserAvatar';
import CommentActions from './CommentActions';
import CommentForm from './CommentForm';
import RichTextRenderer from '../../../components/common/RichTextRenderer';
import StatusBadge from './StatusBadge';

interface CommentItemProps {
  comment: Comment;
  depth: number;
  maxDepth: number;
  hasReplies: boolean;
  isExpanded: boolean;
  onToggleReplies: (commentId: string) => void;
  onReply: (commentId: string) => void;
  onEdit: (commentId: string) => void;
  onDelete: (commentId: string) => Promise<void>;
  onLike: (commentId: string) => Promise<void>;
  onUnlike: (commentId: string) => Promise<void>;
  onReport: (commentId: string, reason: string, description?: string) => Promise<void>;
  isReplying: boolean;
  isEditing: boolean;
  postId: string;
  className?: string;
}

const CommentItem: React.FC<CommentItemProps> = ({
  comment,
  depth,
  maxDepth,
  hasReplies,
  isExpanded,
  onToggleReplies,
  onReply,
  onEdit,
  onDelete,
  onLike,
  onUnlike,
  onReport,
  isReplying,
  isEditing,
  postId,
  className = ''
}) => {
  const [showActions, setShowActions] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [showFullContent, setShowFullContent] = useState(false);
  const [reportDialogOpen, setReportDialogOpen] = useState(false);

  // 计算缩进样式
  const indentStyle = useMemo(() => {
    const baseIndent = depth * 24; // 每级缩进24px
    return {
      marginLeft: `${Math.min(baseIndent, maxDepth * 24)}px`
    };
  }, [depth, maxDepth]);

  // 格式化时间
  const relativeTime = useMemo(() => {
    return formatDistanceToNow(new Date(comment.createdAt), {
      addSuffix: true,
      locale: zhCN
    });
  }, [comment.createdAt]);

  // 截断长内容
  const shouldTruncate = comment.content.length > 300;
  const displayContent = shouldTruncate && !showFullContent
    ? comment.content.slice(0, 300) + '...'
    : comment.content;

  // 删除处理
  const handleDelete = async () => {
    if (window.confirm('确定要删除这条评论吗？')) {
      setIsDeleting(true);
      try {
        await onDelete(comment.id);
      } catch (error) {
        console.error('Delete failed:', error);
      } finally {
        setIsDeleting(false);
      }
    }
  };

  // 点赞处理
  const handleLikeToggle = async () => {
    try {
      if (comment.isLiked) {
        await onUnlike(comment.id);
      } else {
        await onLike(comment.id);
      }
    } catch (error) {
      console.error('Like toggle failed:', error);
    }
  };

  // 取消回复
  const handleCancelReply = () => {
    onReply('');
  };

  // 取消编辑
  const handleCancelEdit = () => {
    onEdit('');
  };

  // 处理回复
  const handleToggleReplies = () => {
    onToggleReplies(comment.id);
  };

  // 判断是否显示连接线
  const showConnector = depth > 0;
  const isLastReply = false; // 这个需要从父组件传入

  // 内容审核状态样式
  const getStatusStyle = (status: CommentStatus) => {
    switch (status) {
      case CommentStatus.Pending:
        return 'bg-yellow-50 border-yellow-200 dark:bg-yellow-900/20 dark:border-yellow-800';
      case CommentStatus.Rejected:
        return 'bg-red-50 border-red-200 dark:bg-red-900/20 dark:border-red-800';
      case CommentStatus.Hidden:
        return 'bg-gray-50 border-gray-200 dark:bg-gray-800/50 dark:border-gray-700';
      case CommentStatus.Spam:
        return 'bg-red-100 border-red-300 dark:bg-red-900/30 dark:border-red-700';
      default:
        return 'bg-white border-gray-200 dark:bg-gray-800 dark:border-gray-700';
    }
  };

  return (
    <article
      className={`comment-item relative ${className}`}
      style={indentStyle}
      onMouseEnter={() => setShowActions(true)}
      onMouseLeave={() => setShowActions(false)}
    >
      {/* 连接线 */}
      {showConnector && (
        <div className="absolute left-0 top-0 bottom-0 w-px bg-gray-200 dark:bg-gray-700">
          <div className="absolute top-6 left-0 w-4 h-px bg-gray-200 dark:bg-gray-700"></div>
          {!isLastReply && (
            <div className="absolute top-6 left-0 bottom-0 w-px bg-gray-200 dark:bg-gray-700"></div>
          )}
        </div>
      )}

      <div className={`
        comment-content rounded-lg border p-4 transition-all duration-200
        ${getStatusStyle(comment.status)}
        ${showActions ? 'shadow-md' : 'shadow-sm'}
        ${isDeleting ? 'opacity-50 pointer-events-none' : ''}
      `}>
        {/* 评论头部 */}
        <div className="comment-header flex items-start justify-between mb-3">
          <div className="flex items-center space-x-3">
            <UserAvatar
              user={comment.author}
              size="sm"
              showStatus={true}
            />

            <div className="comment-meta">
              <div className="flex items-center space-x-2">
                <span className="font-medium text-gray-900 dark:text-gray-100">
                  {comment.author?.displayName || comment.author?.username}
                </span>

                {comment.author?.role && comment.author.role !== 'User' && (
                  <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium
                                   bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200">
                    {comment.author.role}
                  </span>
                )}

                {comment.author?.isVip && (
                  <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium
                                   bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200">
                    VIP
                  </span>
                )}
              </div>

              <div className="flex items-center space-x-2 text-sm text-gray-500 dark:text-gray-400 mt-1">
                <time dateTime={comment.createdAt} title={new Date(comment.createdAt).toLocaleString()}>
                  {relativeTime}
                </time>

                {comment.updatedAt !== comment.createdAt && (
                  <>
                    <span>·</span>
                    <span className="text-xs">已编辑</span>
                  </>
                )}

                {comment.status !== CommentStatus.Approved && (
                  <>
                    <span>·</span>
                    <StatusBadge status={comment.status} />
                  </>
                )}
              </div>
            </div>
          </div>

          {/* 快速操作 */}
          <div className={`comment-quick-actions transition-opacity duration-200 ${
            showActions ? 'opacity-100' : 'opacity-0'
          }`}>
            <CommentActions
              comment={comment}
              onReply={() => onReply(comment.id)}
              onEdit={() => onEdit(comment.id)}
              onDelete={handleDelete}
              onLike={handleLikeToggle}
              onReport={() => setReportDialogOpen(true)}
              compact={true}
            />
          </div>
        </div>

        {/* 评论内容 */}
        <div className="comment-body mb-3">
          {isEditing ? (
            <CommentForm
              postId={postId}
              parentId={comment.parentId}
              initialContent={comment.content}
              isEditing={true}
              commentId={comment.id}
              onCancel={handleCancelEdit}
              className="mt-3"
            />
          ) : (
            <>
              <div className="prose prose-sm max-w-none text-gray-700 dark:text-gray-300
                              prose-blue dark:prose-invert">
                <RichTextRenderer content={comment.renderedContent || displayContent} />
              </div>

              {shouldTruncate && (
                <button
                  onClick={() => setShowFullContent(!showFullContent)}
                  className="text-blue-600 dark:text-blue-400 text-sm hover:underline mt-2"
                >
                  {showFullContent ? '收起' : '展开'}
                </button>
              )}
            </>
          )}
        </div>

        {/* 评论操作栏 */}
        <div className="comment-actions flex items-center justify-between">
          <div className="flex items-center space-x-4">
            <CommentActions
              comment={comment}
              onReply={() => onReply(comment.id)}
              onEdit={() => onEdit(comment.id)}
              onDelete={handleDelete}
              onLike={handleLikeToggle}
              onReport={() => setReportDialogOpen(true)}
              compact={false}
            />

            {/* 回复数和展开控制 */}
            {hasReplies && (
              <button
                onClick={handleToggleReplies}
                className="flex items-center space-x-1 text-sm text-gray-500 dark:text-gray-400
                           hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
              >
                <svg
                  className={`w-4 h-4 transition-transform duration-200 ${
                    isExpanded ? 'rotate-180' : ''
                  }`}
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M19 9l-7 7-7-7"
                  />
                </svg>
                <span>
                  {isExpanded ? '收起' : '展开'} {comment.replyCount} 条回复
                </span>
              </button>
            )}

            {/* 深度限制提示 */}
            {depth >= maxDepth && hasReplies && !isExpanded && (
              <span className="text-xs text-gray-400 dark:text-gray-500">
                回复层级已达上限
              </span>
            )}
          </div>

          {/* 点赞数 */}
          {comment.likeCount > 0 && (
            <div className="flex items-center space-x-1 text-sm text-gray-500 dark:text-gray-400">
              <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path d="M2 10.5a1.5 1.5 0 113 0v6a1.5 1.5 0 01-3 0v-6zM6 10.333v5.43a2 2 0 001.106 1.79l.05.025A4 4 0 008.943 18h5.416a2 2 0 001.962-1.608l1.2-6A2 2 0 0015.56 8H12V4a2 2 0 00-2-2 1 1 0 00-1 1v.667a4 4 0 01-.8 2.4L6.8 7.933a4 4 0 00-.8 2.4z" />
              </svg>
              <span>{comment.likeCount}</span>
            </div>
          )}
        </div>

        {/* 回复表单 */}
        {isReplying && (
          <CommentForm
            postId={postId}
            parentId={comment.id}
            onCancel={handleCancelReply}
            placeholder={`回复 @${comment.author?.displayName || comment.author?.username}`}
            className="mt-4"
            autoFocus={true}
          />
        )}
      </div>

      {/* 举报对话框 */}
      {reportDialogOpen && (
        <ReportDialog
          comment={comment}
          onReport={async (reason, description) => {
            await onReport(comment.id, reason, description);
            setReportDialogOpen(false);
          }}
          onCancel={() => setReportDialogOpen(false)}
        />
      )}
    </article>
  );
};

// 简单的举报对话框组件
const ReportDialog: React.FC<{
  comment: Comment;
  onReport: (reason: string, description?: string) => Promise<void>;
  onCancel: () => void;
}> = ({ comment, onReport, onCancel }) => {
  const [reason, setReason] = useState('');
  const [description, setDescription] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reason.trim()) return;

    setSubmitting(true);
    try {
      await onReport(reason, description.trim() || undefined);
    } finally {
      setSubmitting(false);
    }
  };

  const reasons = [
    { value: 'Spam', label: '垃圾信息' },
    { value: 'Harassment', label: '骚扰辱骂' },
    { value: 'Inappropriate', label: '不当内容' },
    { value: 'OffTopic', label: '偏离主题' },
    { value: 'Other', label: '其他' }
  ];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black bg-opacity-50">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl w-full max-w-md">
        <div className="p-6">
          <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-4">
            举报评论
          </h3>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                举报原因
              </label>
              <select
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                className="w-full p-2 border border-gray-300 dark:border-gray-600 rounded-md
                           bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
                required
              >
                <option value="">请选择举报原因</option>
                {reasons.map(r => (
                  <option key={r.value} value={r.value}>{r.label}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                详细说明（可选）
              </label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                className="w-full p-2 border border-gray-300 dark:border-gray-600 rounded-md
                           bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
                placeholder="请详细说明举报原因..."
              />
            </div>

            <div className="flex justify-end space-x-3">
              <button
                type="button"
                onClick={onCancel}
                disabled={submitting}
                className="px-4 py-2 text-gray-700 dark:text-gray-300 hover:bg-gray-100
                           dark:hover:bg-gray-700 rounded-md transition-colors"
              >
                取消
              </button>
              <button
                type="submit"
                disabled={!reason.trim() || submitting}
                className="px-4 py-2 bg-red-600 hover:bg-red-700 disabled:bg-gray-400
                           text-white rounded-md transition-colors disabled:cursor-not-allowed"
              >
                {submitting ? '提交中...' : '提交举报'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

export default CommentItem;
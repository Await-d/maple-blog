/**
 * 评论操作按钮组件
 * 包含点赞、回复、编辑、删除、举报等操作
 */

import React, { useState } from 'react';
import { Comment } from '../../../types/comment';
import { useAuth } from '../../../hooks/useAuth';
import { UserRole } from '../../../types/auth';

interface CommentActionsProps {
  comment: Comment;
  onReply: () => void;
  onEdit: () => void;
  onDelete: () => Promise<void>;
  onLike: () => Promise<void>;
  onReport: () => void;
  compact?: boolean;
  className?: string;
}

const CommentActions: React.FC<CommentActionsProps> = ({
  comment,
  onReply,
  onEdit,
  onDelete,
  onLike,
  onReport,
  compact = false,
  className = ''
}) => {
  const { user, isAuthenticated } = useAuth();
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  const handleLike = async () => {
    if (!isAuthenticated) {
      // TODO: Replace with proper UI notification for login required
      return;
    }

    setActionLoading('like');
    try {
      await onLike();
    } finally {
      setActionLoading(null);
    }
  };

  const handleDelete = async () => {
    setActionLoading('delete');
    try {
      await onDelete();
    } finally {
      setActionLoading(null);
    }
  };

  const canEdit = comment.canEdit && user?.id === comment.authorId;
  const canDelete = comment.canDelete && (user?.id === comment.authorId || user?.role === UserRole.Admin);

  return (
    <div className={`comment-actions flex items-center space-x-1 ${className}`}>
      {/* 点赞按钮 */}
      <button
        onClick={handleLike}
        disabled={actionLoading === 'like'}
        className={`
          inline-flex items-center space-x-1 px-2 py-1 text-sm rounded transition-colors
          ${comment.isLiked
            ? 'text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20'
            : 'text-gray-500 dark:text-gray-400 hover:text-red-600 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20'
          }
          ${actionLoading === 'like' ? 'opacity-50 cursor-not-allowed' : ''}
          ${compact ? 'text-xs px-1.5 py-0.5' : ''}
        `}
        title={comment.isLiked ? '取消点赞' : '点赞'}
      >
        <svg
          className={`${compact ? 'w-3 h-3' : 'w-4 h-4'} ${actionLoading === 'like' ? 'animate-pulse' : ''}`}
          fill={comment.isLiked ? 'currentColor' : 'none'}
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"
          />
        </svg>
        {!compact && comment.likeCount > 0 && (
          <span className="font-medium">{comment.likeCount}</span>
        )}
      </button>

      {/* 回复按钮 */}
      {isAuthenticated && (
        <button
          onClick={onReply}
          className={`
            inline-flex items-center space-x-1 px-2 py-1 text-sm rounded transition-colors
            text-gray-500 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20
            ${compact ? 'text-xs px-1.5 py-0.5' : ''}
          `}
          title="回复"
        >
          <svg className={`${compact ? 'w-3 h-3' : 'w-4 h-4'}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6"
            />
          </svg>
          {!compact && <span>回复</span>}
        </button>
      )}

      {/* 编辑按钮 */}
      {canEdit && (
        <button
          onClick={onEdit}
          className={`
            inline-flex items-center space-x-1 px-2 py-1 text-sm rounded transition-colors
            text-gray-500 dark:text-gray-400 hover:text-green-600 dark:hover:text-green-400 hover:bg-green-50 dark:hover:bg-green-900/20
            ${compact ? 'text-xs px-1.5 py-0.5' : ''}
          `}
          title="编辑"
        >
          <svg className={`${compact ? 'w-3 h-3' : 'w-4 h-4'}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
            />
          </svg>
          {!compact && <span>编辑</span>}
        </button>
      )}

      {/* 删除按钮 */}
      {canDelete && (
        <button
          onClick={handleDelete}
          disabled={actionLoading === 'delete'}
          className={`
            inline-flex items-center space-x-1 px-2 py-1 text-sm rounded transition-colors
            text-gray-500 dark:text-gray-400 hover:text-red-600 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20
            ${actionLoading === 'delete' ? 'opacity-50 cursor-not-allowed' : ''}
            ${compact ? 'text-xs px-1.5 py-0.5' : ''}
          `}
          title="删除"
        >
          <svg
            className={`${compact ? 'w-3 h-3' : 'w-4 h-4'} ${actionLoading === 'delete' ? 'animate-pulse' : ''}`}
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
            />
          </svg>
          {!compact && <span>删除</span>}
        </button>
      )}

      {/* 举报按钮 */}
      {isAuthenticated && user?.id !== comment.authorId && (
        <button
          onClick={onReport}
          className={`
            inline-flex items-center space-x-1 px-2 py-1 text-sm rounded transition-colors
            text-gray-500 dark:text-gray-400 hover:text-orange-600 dark:hover:text-orange-400 hover:bg-orange-50 dark:hover:bg-orange-900/20
            ${compact ? 'text-xs px-1.5 py-0.5' : ''}
          `}
          title="举报"
        >
          <svg className={`${compact ? 'w-3 h-3' : 'w-4 h-4'}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M3 21v-4a4 4 0 014-4h5c.88 0 1.7-.26 2.4-.72L21 9a2 2 0 000-4l-6.6 3.28c-.7.46-1.52.72-2.4.72H7a4 4 0 00-4 4v8"
            />
          </svg>
          {!compact && <span>举报</span>}
        </button>
      )}

      {/* 分享按钮 (如果需要) */}
      <button
        onClick={() => {
          const url = `${window.location.origin}${window.location.pathname}#comment-${comment.id}`;
          if (navigator.share) {
            navigator.share({
              title: '评论分享',
              text: comment.content.slice(0, 100) + '...',
              url
            });
          } else {
            navigator.clipboard.writeText(url);
            // TODO: Replace with proper UI notification for copy success
          }
        }}
        className={`
          inline-flex items-center space-x-1 px-2 py-1 text-sm rounded transition-colors
          text-gray-500 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20
          ${compact ? 'text-xs px-1.5 py-0.5' : ''}
        `}
        title="分享链接"
      >
        <svg className={`${compact ? 'w-3 h-3' : 'w-4 h-4'}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.367 2.684 3 3 0 00-5.367-2.684z"
          />
        </svg>
        {!compact && <span>分享</span>}
      </button>
    </div>
  );
};

export default CommentActions;
// @ts-nocheck
/**
 * 评论列表组件
 * 支持嵌套回复、虚拟滚动、排序、筛选等功能
 */

import React, { useEffect, useMemo, useState, useCallback, useRef } from 'react';
import { useCommentStore, useComments } from '../../../stores/commentStore';
import { useCommentSocket } from '../../../services/commentSocket';
import { CommentSortOrder, CommentStatus } from '../../../types/comment';
import CommentItem from './CommentItem';
import CommentSkeleton from './CommentSkeleton';
import EmptyComments from './EmptyComments';
import SortControls from './SortControls';
import TypingIndicator from './TypingIndicator';

interface CommentListProps {
  postId: string;
  className?: string;
  maxDepth?: number;
  showSortControls?: boolean;
  enableVirtualScroll?: boolean;
  pageSize?: number;
  autoRefresh?: boolean;
  refreshInterval?: number;
}

interface FlattenedComment {
  comment: any;
  depth: number;
  hasReplies: boolean;
  isLastReply: boolean;
}


const CommentList: React.FC<CommentListProps> = ({
  postId,
  className = '',
  maxDepth = 3,
  showSortControls = true,
  enableVirtualScroll = false,
  pageSize = 20,
  autoRefresh = false,
  refreshInterval = 30000
}) => {
  const { comments, loading, pagination, stats, actions } = useComments(postId);
  const { config, typingUsers, onlineUserCounts, replyingTo, editingComment } = useCommentStore();
  const [expandedReplies, setExpandedReplies] = useState<Set<string>>(new Set());
  const [sortOrder, setSortOrder] = useState(config.sortOrder);
  const [statusFilter, setStatusFilter] = useState<CommentStatus[]>([CommentStatus.Approved]);
  const autoRefreshRef = useRef<NodeJS.Timeout | null>(null);

  // 初始化评论数据和实时连接
  useEffect(() => {
    const initializeComments = async () => {
      await actions.loadComments(postId, {
        pageSize,
        sortOrder,
        includeStatus: statusFilter
      });

      // 初始化实时功能
      actions.initializeRealtime(postId);
    };

    initializeComments();

    // 自动刷新
    if (autoRefresh) {
      autoRefreshRef.current = setInterval(() => {
        actions.refreshComments(postId);
      }, refreshInterval);
    }

    return () => {
      if (autoRefreshRef.current) {
        clearInterval(autoRefreshRef.current);
      }
      actions.cleanupRealtime();
    };
  }, [postId, pageSize, sortOrder, statusFilter, autoRefresh, refreshInterval]);

  // 排序变化处理
  const handleSortChange = useCallback(async (newSortOrder: CommentSortOrder) => {
    setSortOrder(newSortOrder);
    actions.updateConfig({ sortOrder: newSortOrder });
    await actions.loadComments(postId, {
      sortOrder: newSortOrder,
      includeStatus: statusFilter,
      pageSize
    });
  }, [postId, pageSize, statusFilter, actions]);

  // 状态筛选变化处理
  const handleStatusFilterChange = useCallback(async (newStatusFilter: CommentStatus[]) => {
    setStatusFilter(newStatusFilter);
    await actions.loadComments(postId, {
      sortOrder,
      includeStatus: newStatusFilter,
      pageSize
    });
  }, [postId, pageSize, sortOrder, actions]);

  // 展开/收起回复
  const toggleReplies = useCallback((commentId: string) => {
    setExpandedReplies(prev => {
      const next = new Set(prev);
      if (next.has(commentId)) {
        next.delete(commentId);
      } else {
        next.add(commentId);
      }
      return next;
    });
  }, []);

  // 扁平化评论树，用于虚拟滚动
  const flattenedComments = useMemo(() => {
    const result: FlattenedComment[] = [];

    const flatten = (comments: any[], depth = 0, parentExpanded = true) => {
      comments.forEach((comment, index) => {
        const hasReplies = comment.replies && comment.replies.length > 0;
        const isLastReply = index === comments.length - 1;
        const isExpanded = expandedReplies.has(comment.id);

        if (parentExpanded && depth <= maxDepth) {
          result.push({
            comment,
            depth,
            hasReplies,
            isLastReply
          });

          // 递归处理回复
          if (hasReplies && isExpanded && depth < maxDepth) {
            flatten(comment.replies, depth + 1, true);
          }
        }
      });
    };

    flatten(comments);
    return result;
  }, [comments, expandedReplies, maxDepth]);


  // 获取当前文章的输入用户
  const currentTypingUsers = useMemo(() => {
    return typingUsers.filter(user => user.postId === postId);
  }, [typingUsers, postId]);

  const onlineCount = onlineUserCounts[postId] || 0;

  if (loading && comments.length === 0) {
    return (
      <div className={`space-y-4 ${className}`}>
        {Array.from({ length: 3 }).map((_, index) => (
          <CommentSkeleton key={index} />
        ))}
      </div>
    );
  }

  if (!loading && comments.length === 0) {
    return (
      <div className={className}>
        <EmptyComments postId={postId} />
      </div>
    );
  }

  return (
    <div className={`comment-list ${className}`}>
      {/* 标题和统计 */}
      <div className="comment-list-header mb-6">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
            评论 {stats && `(${stats.totalCount})`}
          </h3>

          {onlineCount > 0 && (
            <div className="flex items-center text-sm text-gray-500 dark:text-gray-400">
              <div className="w-2 h-2 bg-green-500 rounded-full mr-2 animate-pulse"></div>
              {onlineCount} 人在线
            </div>
          )}
        </div>

        {/* 排序控制 */}
        {showSortControls && (
          <SortControls
            currentSort={sortOrder}
            onSortChange={handleSortChange}
            statusFilter={statusFilter}
            onStatusFilterChange={handleStatusFilterChange}
          />
        )}
      </div>

      {/* 输入指示器 */}
      {currentTypingUsers.length > 0 && (
        <TypingIndicator users={currentTypingUsers} className="mb-4" />
      )}

      {/* 评论列表 */}
      <div className="comment-list-content">
        <div className="space-y-4">
          {flattenedComments.map((item, index) => (
            <CommentItem
              key={item.comment.id}
              comment={item.comment}
              depth={item.depth}
              maxDepth={maxDepth}
              hasReplies={item.hasReplies}
              isExpanded={expandedReplies.has(item.comment.id)}
              onToggleReplies={toggleReplies}
              onReply={(commentId) => actions.setReplyingTo(commentId)}
              onEdit={(commentId) => actions.setEditingComment(commentId)}
              onDelete={actions.deleteComment}
              onLike={actions.likeComment}
              onUnlike={actions.unlikeComment}
              onReport={actions.reportComment}
              isReplying={replyingTo === item.comment.id}
              isEditing={editingComment === item.comment.id}
              postId={postId}
            />
          ))}
        </div>

        {/* 加载更多按钮 */}
        {pagination?.hasMore && !enableVirtualScroll && (
          <div className="flex justify-center mt-8">
            <button
              onClick={() => actions.loadMoreComments(postId)}
              disabled={loading}
              className="px-6 py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400
                         text-white rounded-lg transition-colors duration-200
                         disabled:cursor-not-allowed"
            >
              {loading ? '加载中...' : '加载更多评论'}
            </button>
          </div>
        )}

        {/* 加载中指示器 */}
        {loading && comments.length > 0 && (
          <div className="flex justify-center mt-4">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          </div>
        )}
      </div>
    </div>
  );
};

export default CommentList;
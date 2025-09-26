/**
 * 评论统计信息组件
 * 显示评论数、参与者数、最新评论等统计信息
 */

import React, { useMemo } from 'react';
import { formatDistanceToNow } from 'date-fns';
import { zhCN } from 'date-fns/locale';
import { CommentStats as CommentStatsType } from '../../../types/comment';
import UserAvatar from '../../../components/common/UserAvatar';

interface CommentStatsProps {
  stats: CommentStatsType;
  onlineCount?: number;
  className?: string;
  compact?: boolean;
  showLatestComment?: boolean;
}

const CommentStats: React.FC<CommentStatsProps> = ({
  stats,
  onlineCount,
  className = '',
  compact = false,
  showLatestComment = true
}) => {
  // 格式化最新评论时间
  const latestCommentTime = useMemo(() => {
    if (!stats.latestCommentAt) return null;

    return formatDistanceToNow(new Date(stats.latestCommentAt), {
      addSuffix: true,
      locale: zhCN
    });
  }, [stats.latestCommentAt]);

  // 计算参与度百分比 (示例计算)
  const engagementRate = useMemo(() => {
    if (stats.totalCount === 0) return 0;
    // 简单的参与度计算：回复数 / 总评论数
    return Math.round((stats.replyCount / stats.totalCount) * 100);
  }, [stats.totalCount, stats.replyCount]);

  if (compact) {
    return (
      <div className={`comment-stats-compact flex items-center space-x-4 text-sm text-gray-500 dark:text-gray-400 ${className}`}>
        <span className="flex items-center space-x-1">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
          </svg>
          <span>{stats.totalCount}</span>
        </span>

        <span className="flex items-center space-x-1">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z" />
          </svg>
          <span>{stats.participantCount}</span>
        </span>

        {onlineCount && onlineCount > 0 && (
          <span className="flex items-center space-x-1">
            <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
            <span>{onlineCount}</span>
          </span>
        )}
      </div>
    );
  }

  return (
    <div className={`comment-stats bg-gray-50 dark:bg-gray-800/50 rounded-lg p-4 ${className}`}>
      {/* 统计标题 */}
      <h4 className="text-sm font-medium text-gray-900 dark:text-gray-100 mb-3">
        讨论统计
      </h4>

      {/* 统计网格 */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
        {/* 总评论数 */}
        <div className="text-center">
          <div className="flex items-center justify-center w-10 h-10 bg-blue-100 dark:bg-blue-900/20 rounded-lg mx-auto mb-2">
            <svg className="w-5 h-5 text-blue-600 dark:text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
            </svg>
          </div>
          <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">
            {stats.totalCount.toLocaleString()}
          </div>
          <div className="text-xs text-gray-500 dark:text-gray-400">
            总评论
          </div>
        </div>

        {/* 根评论数 */}
        <div className="text-center">
          <div className="flex items-center justify-center w-10 h-10 bg-green-100 dark:bg-green-900/20 rounded-lg mx-auto mb-2">
            <svg className="w-5 h-5 text-green-600 dark:text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 8h10M7 12h4m1 8l-4-4H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-3l-4 4z" />
            </svg>
          </div>
          <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">
            {stats.rootCommentCount.toLocaleString()}
          </div>
          <div className="text-xs text-gray-500 dark:text-gray-400">
            主评论
          </div>
        </div>

        {/* 回复数 */}
        <div className="text-center">
          <div className="flex items-center justify-center w-10 h-10 bg-purple-100 dark:bg-purple-900/20 rounded-lg mx-auto mb-2">
            <svg className="w-5 h-5 text-purple-600 dark:text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6" />
            </svg>
          </div>
          <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">
            {stats.replyCount.toLocaleString()}
          </div>
          <div className="text-xs text-gray-500 dark:text-gray-400">
            回复数
          </div>
        </div>

        {/* 参与者数 */}
        <div className="text-center">
          <div className="flex items-center justify-center w-10 h-10 bg-orange-100 dark:bg-orange-900/20 rounded-lg mx-auto mb-2">
            <svg className="w-5 h-5 text-orange-600 dark:text-orange-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z" />
            </svg>
          </div>
          <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">
            {stats.participantCount.toLocaleString()}
          </div>
          <div className="text-xs text-gray-500 dark:text-gray-400">
            参与者
          </div>
        </div>
      </div>

      {/* 参与度指标 */}
      <div className="mb-4">
        <div className="flex items-center justify-between text-sm text-gray-600 dark:text-gray-400 mb-1">
          <span>讨论活跃度</span>
          <span>{engagementRate}%</span>
        </div>
        <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2">
          <div
            className="bg-blue-600 dark:bg-blue-400 h-2 rounded-full transition-all duration-300"
            style={{ width: `${Math.min(engagementRate, 100)}%` }}
          ></div>
        </div>
      </div>

      {/* 在线状态 */}
      {onlineCount && onlineCount > 0 && (
        <div className="flex items-center justify-between py-2 px-3 bg-green-50 dark:bg-green-900/10 rounded-lg mb-4">
          <div className="flex items-center space-x-2">
            <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
            <span className="text-sm text-green-700 dark:text-green-400">
              当前在线
            </span>
          </div>
          <span className="text-sm font-medium text-green-700 dark:text-green-400">
            {onlineCount} 人
          </span>
        </div>
      )}

      {/* 最新评论信息 */}
      {showLatestComment && stats.latestCommentAt && stats.latestCommentAuthor && (
        <div className="border-t border-gray-200 dark:border-gray-700 pt-3">
          <div className="text-xs text-gray-500 dark:text-gray-400 mb-2">
            最新评论
          </div>
          <div className="flex items-center space-x-2">
            <UserAvatar
              user={stats.latestCommentAuthor}
              size="xs"
              showStatus={false}
            />
            <div className="flex-1 min-w-0">
              <div className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
                {stats.latestCommentAuthor.displayName}
              </div>
              <div className="text-xs text-gray-500 dark:text-gray-400">
                {latestCommentTime}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* 快速操作 */}
      <div className="mt-4 pt-3 border-t border-gray-200 dark:border-gray-700">
        <div className="flex items-center justify-between text-xs">
          <button
            onClick={() => {
              const commentForm = document.getElementById('comment-form');
              commentForm?.scrollIntoView({ behavior: 'smooth' });
              const textarea = commentForm?.querySelector('textarea');
              textarea?.focus();
            }}
            className="text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-200 transition-colors"
          >
            参与讨论 →
          </button>

          <button
            onClick={() => {
              window.scrollTo({ top: 0, behavior: 'smooth' });
            }}
            className="text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 transition-colors"
          >
            回到顶部
          </button>
        </div>
      </div>
    </div>
  );
};

export default CommentStats;
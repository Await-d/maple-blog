// @ts-nocheck
/**
 * 评论系统主组件
 * 整合所有评论功能，提供响应式设计和移动端优化
 */

import React, { useState, useEffect, useMemo } from 'react';
import { useCommentStore } from '../../stores/commentStore';
import { useAuth } from '../../hooks/useAuth';
import { useResponsive } from '../../hooks/useResponsive';
import CommentList from './components/CommentList';
import CommentForm from './components/CommentForm';
import MobileCommentForm from './components/MobileCommentForm';
import CommentStats from './components/CommentStats';
import NotificationBadge from './components/NotificationBadge';
import { CommentSortOrder } from '../../types/comment';

interface CommentSystemProps {
  postId: string;
  initialSort?: CommentSortOrder;
  showStats?: boolean;
  showNotifications?: boolean;
  className?: string;
  maxDepth?: number;
  enableRealtime?: boolean;
  autoRefresh?: boolean;
  refreshInterval?: number;
}

const CommentSystem: React.FC<CommentSystemProps> = ({
  postId,
  initialSort = CommentSortOrder.CreatedAtDesc,
  showStats = true,
  showNotifications = true,
  className = '',
  maxDepth = 3,
  enableRealtime = true,
  autoRefresh = false,
  refreshInterval = 30000
}) => {
  const { isAuthenticated } = useAuth();
  const responsive = useResponsive();
  const { stats, onlineUserCounts, replyingTo, actions } = useCommentStore();

  // 移动端表单状态
  const [mobileFormOpen, setMobileFormOpen] = useState(false);
  const [mobileFormConfig, setMobileFormConfig] = useState({
    parentId: undefined as string | undefined,
    initialContent: '',
    isEditing: false,
    commentId: undefined as string | undefined
  });

  // 根据响应式配置调整组件行为
  const commentConfig = useMemo(() => responsive.getCommentConfig(), [responsive]);

  // 初始化评论系统
  useEffect(() => {
    if (enableRealtime) {
      actions.initializeRealtime(postId);
    }

    return () => {
      if (enableRealtime) {
        actions.cleanupRealtime();
      }
    };
  }, [postId, enableRealtime, actions]);

  // 处理移动端回复
  const handleMobileReply = (commentId?: string) => {
    setMobileFormConfig({
      parentId: commentId,
      initialContent: '',
      isEditing: false,
      commentId: undefined
    });
    setMobileFormOpen(true);
  };

  // 处理移动端编辑
  const handleMobileEdit = (commentId: string, content: string) => {
    setMobileFormConfig({
      parentId: undefined,
      initialContent: content,
      isEditing: true,
      commentId
    });
    setMobileFormOpen(true);
  };

  // 处理桌面端回复
  const handleDesktopReply = (commentId?: string) => {
    actions.setReplyingTo(commentId || '');
  };

  // 处理桌面端编辑
  const handleDesktopEdit = (commentId: string) => {
    actions.setEditingComment(commentId);
  };

  // 处理表单提交成功
  const handleSubmitSuccess = () => {
    if (commentConfig.useMobileForm) {
      setMobileFormOpen(false);
    }
  };

  // 获取当前文章统计
  const currentStats = stats[postId];
  const onlineCount = onlineUserCounts[postId];

  return (
    <div className={`comment-system space-y-6 ${className}`}>
      {/* 顶部工具栏 */}
      <div className="flex items-center justify-between">
        {/* 统计信息 (紧凑模式) */}
        {showStats && currentStats && commentConfig.compact && (
          <CommentStats
            stats={currentStats}
            onlineCount={onlineCount}
            compact={true}
            showLatestComment={false}
          />
        )}

        {/* 通知徽章 */}
        {showNotifications && isAuthenticated && (
          <NotificationBadge
            position={commentConfig.position}
            maxItems={commentConfig.maxNotifications}
            compact={commentConfig.compact}
          />
        )}
      </div>

      {/* 主要内容区域 */}
      <div className={`grid gap-6 ${
        responsive.isDesktop && showStats && !commentConfig.compact
          ? 'lg:grid-cols-3'
          : 'grid-cols-1'
      }`}>
        {/* 评论列表和表单 */}
        <div className={
          responsive.isDesktop && showStats && !commentConfig.compact
            ? 'lg:col-span-2'
            : 'col-span-1'
        }>
          {/* 桌面端评论表单 */}
          {isAuthenticated && !commentConfig.useMobileForm && (
            <div className="mb-6">
              <CommentForm
                postId={postId}
                placeholder="分享你的想法..."
                autoFocus={commentConfig.autoFocus}
                showPreview={!commentConfig.compactToolbar}
                compact={commentConfig.compact}
              />
            </div>
          )}

          {/* 移动端评论按钮 */}
          {isAuthenticated && commentConfig.useMobileForm && (
            <div className="mb-6">
              <button
                onClick={() => handleMobileReply()}
                className="w-full p-4 text-left bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-sm hover:shadow-md transition-shadow"
              >
                <div className="flex items-center space-x-3">
                  <div className="w-8 h-8 bg-blue-100 dark:bg-blue-900/20 rounded-full flex items-center justify-center">
                    <svg className="w-4 h-4 text-blue-600 dark:text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                    </svg>
                  </div>
                  <span className="text-gray-500 dark:text-gray-400">分享你的想法...</span>
                </div>
              </button>
            </div>
          )}

          {/* 评论列表 */}
          <CommentList
            postId={postId}
            maxDepth={Math.min(maxDepth, commentConfig.maxDepth)}
            enableVirtualScroll={commentConfig.enableVirtualScroll}
            pageSize={commentConfig.pageSize}
            autoRefresh={autoRefresh}
            refreshInterval={refreshInterval}
          />
        </div>

        {/* 侧边栏统计 (桌面端) */}
        {responsive.isDesktop && showStats && !commentConfig.compact && currentStats && (
          <div className="lg:col-span-1">
            <div className="sticky top-4">
              <CommentStats
                stats={currentStats}
                onlineCount={onlineCount}
                compact={false}
                showLatestComment={true}
              />
            </div>
          </div>
        )}
      </div>

      {/* 移动端评论表单模态框 */}
      {commentConfig.useMobileForm && (
        <MobileCommentForm
          postId={postId}
          parentId={mobileFormConfig.parentId}
          initialContent={mobileFormConfig.initialContent}
          isEditing={mobileFormConfig.isEditing}
          commentId={mobileFormConfig.commentId}
          isOpen={mobileFormOpen}
          onClose={() => setMobileFormOpen(false)}
          onCancel={() => setMobileFormOpen(false)}
          onSubmitSuccess={handleSubmitSuccess}
        />
      )}

      {/* 底部浮动统计 (移动端) */}
      {responsive.isMobile && showStats && currentStats && (
        <div className="fixed bottom-4 left-4 right-4 z-40">
          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 p-3">
            <CommentStats
              stats={currentStats}
              onlineCount={onlineCount}
              compact={true}
              showLatestComment={false}
              className="text-center"
            />
          </div>
        </div>
      )}

      {/* 性能监控 (开发模式) */}
      {process.env.NODE_ENV === 'development' && (
        <div className="fixed bottom-4 right-4 bg-gray-900 text-white p-2 rounded text-xs z-50 opacity-50 hover:opacity-100 transition-opacity">
          <div>屏幕: {responsive.breakpoint}</div>
          <div>设备: {responsive.isMobile ? 'Mobile' : responsive.isTablet ? 'Tablet' : 'Desktop'}</div>
          <div>触摸: {responsive.isTouchDevice ? 'Yes' : 'No'}</div>
          {currentStats && <div>评论: {currentStats.totalCount}</div>}
        </div>
      )}
    </div>
  );
};

export default CommentSystem;
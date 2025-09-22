// @ts-nocheck
/**
 * 评论系统入口文件
 * 导出所有公共组件、Hook和工具
 */

// 主要组件
export { default as CommentSystem } from './CommentSystem';
export { default as CommentList } from './components/CommentList';
export { default as CommentItem } from './components/CommentItem';
export { default as CommentForm } from './components/CommentForm';
export { default as MobileCommentForm } from './components/MobileCommentForm';
export { default as CommentActions } from './components/CommentActions';
export { default as CommentStatsComponent } from './components/CommentStats';
export { default as NotificationBadge } from './components/NotificationBadge';

// 编辑器组件
export { default as CommentEditor } from './components/CommentEditor';
export { default as EmojiPicker } from './components/EmojiPicker';
export { default as MentionSuggestions } from './components/MentionSuggestions';
export { default as ImageUploadModal } from './components/ImageUploadModal';

// 辅助组件
export { default as CommentSkeleton } from './components/CommentSkeleton';
export { default as EmptyComments } from './components/EmptyComments';
export { default as SortControls } from './components/SortControls';
export { default as TypingIndicator } from './components/TypingIndicator';
export { default as StatusBadge } from './components/StatusBadge';

// 状态管理
export {
  useCommentStore,
  useComments,
  useCommentTree,
  useCommentActions
} from '../../stores/commentStore';

// API 服务
export { commentApi, safeCommentApi } from '../../services/commentApi';
export { commentSocket, useCommentSocket } from '../../services/commentSocket';

// Hook 工具
export { default as useCommentNotifications } from '../../hooks/useCommentNotifications';
export { default as useResponsive } from '../../hooks/useResponsive';
export { default as useTouchGestures } from '../../hooks/useTouchGestures';

// 类型定义
export type {
  Comment,
  CommentAuthor,
  CommentFormData,
  CommentQuery,
  CommentPagedResult,
  CommentStats,
  CommentNotification,
  CommentCreateRequest,
  CommentUpdateRequest,
  CommentReportRequest,
  CommentEditorConfig,
  CommentListConfig,
  TypingUser,
  CommentSocketEvents,
  ApiResponse,
  CommentError
} from '../../types/comment';

export {
  CommentStatus,
  CommentSortOrder,
  CommentNotificationType,
  CommentReportReason
} from '../../types/comment';

// Import specific type for internal usage
import type { Comment as CommentType } from '../../types/comment';

// 使用示例
export {
  BasicCommentExample,
  CompactCommentExample,
  EmbeddedCommentExample,
  ReadOnlyCommentExample,
  FullFeaturedExample
} from './example/CommentSystemExample';

// 版本信息
export const VERSION = '1.0.0';

// 默认配置
export const DEFAULT_COMMENT_CONFIG = {
  maxLength: 2000,
  allowMarkdown: true,
  allowEmoji: true,
  allowMention: true,
  allowImageUpload: true,
  maxDepth: 3,
  pageSize: 20,
  autoRefresh: false,
  refreshInterval: 30000,
  showAvatars: true,
  showTimestamps: true,
  showStats: true,
  showActions: true,
  enableVirtualScroll: false,
  placeholder: '分享你的想法...'
};

// 工具函数
export const CommentUtils = {
  /**
   * 格式化评论数量
   */
  formatCount: (count: number): string => {
    if (count < 1000) return count.toString();
    if (count < 10000) return `${(count / 1000).toFixed(1)}k`;
    if (count < 1000000) return `${(count / 10000).toFixed(1)}w`;
    return `${(count / 1000000).toFixed(1)}m`;
  },

  /**
   * 检测是否为移动设备
   */
  isMobileDevice: (): boolean => {
    return /Mobi|Android|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
  },

  /**
   * 检测是否支持触摸
   */
  isTouchDevice: (): boolean => {
    return 'ontouchstart' in window || navigator.maxTouchPoints > 0;
  },

  /**
   * 生成评论链接
   */
  generateCommentLink: (postId: string, commentId: string): string => {
    return `${window.location.origin}${window.location.pathname}?postId=${postId}#comment-${commentId}`;
  },

  /**
   * 计算评论热度分数
   */
  calculateHotScore: (comment: CommentType): number => {
    const ageInHours = (Date.now() - new Date(comment.createdAt).getTime()) / (1000 * 60 * 60);
    const gravity = 1.8; // 重力系数，控制衰减速度

    return (comment.likeCount + comment.replyCount + 1) / Math.pow(ageInHours + 2, gravity);
  },

  /**
   * 截断文本
   */
  truncateText: (text: string, maxLength: number): string => {
    if (text.length <= maxLength) return text;
    return text.slice(0, maxLength - 3) + '...';
  },

  /**
   * 验证评论内容
   */
  validateContent: (content: string): { valid: boolean; errors: string[] } => {
    const errors: string[] = [];

    if (!content.trim()) {
      errors.push('评论内容不能为空');
    }

    if (content.length > 2000) {
      errors.push('评论内容不能超过2000字符');
    }

    if (content.trim().length < 1) {
      errors.push('评论内容至少需要1个字符');
    }

    // 简单的垃圾内容检测
    if (/(.)\1{20,}/.test(content)) {
      errors.push('请不要重复输入相同字符');
    }

    return {
      valid: errors.length === 0,
      errors
    };
  }
};
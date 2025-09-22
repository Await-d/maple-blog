// @ts-nocheck
/**
 * 评论系统类型定义
 * 对应后端CommentDto等DTO结构
 */

export enum CommentStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Hidden = 'Hidden',
  Spam = 'Spam'
}

export enum CommentSortOrder {
  CreatedAtAsc = 'CreatedAtAsc',
  CreatedAtDesc = 'CreatedAtDesc',
  LikeCountDesc = 'LikeCountDesc',
  ReplyCountDesc = 'ReplyCountDesc',
  HotScore = 'HotScore'
}

export interface CommentAuthor {
  id: string;
  username: string;
  displayName: string;
  avatarUrl?: string;
  role: string;
  isVip: boolean;
}

export interface Comment {
  id: string;
  postId: string;
  authorId: string;
  author?: CommentAuthor;
  parentId?: string;
  content: string;
  renderedContent: string;
  status: CommentStatus;
  depth: number;
  threadPath: string;
  likeCount: number;
  replyCount: number;
  isLiked: boolean;
  canEdit: boolean;
  canDelete: boolean;
  createdAt: string;
  updatedAt: string;
  replies: Comment[];
}

export interface CommentCreateRequest {
  postId: string;
  parentId?: string;
  content: string;
  mentionedUsers: string[];
  clientInfo?: CommentClientInfo;
}

export interface CommentUpdateRequest {
  content: string;
  mentionedUsers: string[];
}

export interface CommentClientInfo {
  ipAddress?: string;
  userAgent?: string;
  referer?: string;
}

export interface CommentQuery {
  postId: string;
  parentId?: string;
  sortOrder?: CommentSortOrder;
  page?: number;
  pageSize?: number;
  rootOnly?: boolean;
  includeStatus?: CommentStatus[];
}

export interface CommentPagedResult {
  comments: Comment[];
  totalCount: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CommentStats {
  postId: string;
  totalCount: number;
  rootCommentCount: number;
  replyCount: number;
  participantCount: number;
  latestCommentAt?: string;
  latestCommentAuthor?: CommentAuthor;
}

export interface UserCommentStats {
  userId: string;
  totalCount: number;
  approvedCount: number;
  pendingCount: number;
  likeCount: number;
  averageLikes: number;
  mostLikedCommentId?: string;
}

export interface CommentReportRequest {
  reason: CommentReportReason;
  description?: string;
}

export enum CommentReportReason {
  Spam = 'Spam',
  Harassment = 'Harassment',
  Inappropriate = 'Inappropriate',
  OffTopic = 'OffTopic',
  Other = 'Other'
}

export interface CommentNotification {
  id: string;
  userId: string;
  type: CommentNotificationType;
  commentId: string;
  postId: string;
  triggeredByUserId: string;
  triggeredByUser?: CommentAuthor;
  title: string;
  content: string;
  url: string;
  isRead: boolean;
  createdAt: string;
}

export enum CommentNotificationType {
  CommentReply = 'CommentReply',
  CommentMention = 'CommentMention',
  CommentLiked = 'CommentLiked',
  CommentApproved = 'CommentApproved',
  CommentRejected = 'CommentRejected'
}

// SignalR 事件类型
export interface CommentSocketEvents {
  CommentCreated: (comment: Comment) => void;
  CommentUpdated: (comment: Comment) => void;
  CommentDeleted: (data: { commentId: string }) => void;
  CommentLiked: (data: { commentId: string; userId: string }) => void;
  CommentUnliked: (data: { commentId: string; userId: string }) => void;
  CommentApproved: (data: { commentId: string }) => void;
  CommentRejected: (data: { commentId: string }) => void;
  CommentHidden: (data: { commentId: string }) => void;
  CommentRestored: (data: { commentId: string }) => void;
  CommentMarkedAsSpam: (data: { commentId: string }) => void;
  UserStartedTyping: (data: { userId: string; userName: string; postId: string; parentId?: string; timestamp: string }) => void;
  UserStoppedTyping: (data: { userId: string; postId: string; parentId?: string }) => void;
  CommentStats: (stats: CommentStats) => void;
  OnlineUserCount: (data: { postId: string; count: number }) => void;
  NewNotification: (notification: CommentNotification) => void;
  UnreadNotificationCount: (count: number) => void;
  RecentNotifications: (notifications: CommentNotification[]) => void;
  NotificationMarkedAsRead: (notificationId: string) => void;
  ModerationStats: (stats: any) => void;
  CommentsModerated: (data: any) => void;
  Error: (message: string) => void;
  JoinedPostGroup: (postId: string) => void;
  LeftPostGroup: (postId: string) => void;
  JoinedModerationGroup: () => void;
  LeftModerationGroup: () => void;
}

export interface TypingUser {
  userId: string;
  userName: string;
  postId: string;
  parentId?: string;
  timestamp: string;
}

export interface CommentFormData {
  content: string;
  mentionedUsers: string[];
  parentId?: string;
}

export interface CommentEditorConfig {
  maxLength: number;
  allowMarkdown: boolean;
  allowEmoji: boolean;
  allowMention: boolean;
  allowImageUpload: boolean;
  placeholder: string;
  autoFocus: boolean;
  showToolbar: boolean;
}

export interface CommentListConfig {
  sortOrder: CommentSortOrder;
  pageSize: number;
  maxDepth: number;
  showAvatars: boolean;
  showTimestamps: boolean;
  showStats: boolean;
  showActions: boolean;
  enableVirtualScroll: boolean;
  autoRefresh: boolean;
  refreshInterval: number; // 毫秒
  // Editor configuration properties
  maxLength: number;
  allowMarkdown: boolean;
  allowEmoji: boolean;
  allowMention: boolean;
  allowImageUpload: boolean;
  placeholder: string;
  autoFocus: boolean;
  showToolbar: boolean;
}

export interface CommentModerationAction {
  commentIds: string[];
  action: 'approve' | 'reject' | 'hide' | 'restore' | 'spam';
  reason?: string;
}

export interface CommentSearchResult {
  comments: Comment[];
  totalCount: number;
  query: string;
  filters: {
    postId?: string;
    authorId?: string;
    status?: CommentStatus[];
  };
}

// API 响应包装器
export interface ApiResponse<T> {
  data?: T;
  message?: string;
  errors?: string[];
  success: boolean;
}

// 错误类型
export interface CommentError {
  type: 'validation' | 'permission' | 'network' | 'server' | 'unknown';
  message: string;
  details?: Record<string, any>;
}
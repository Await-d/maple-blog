// @ts-nocheck
/**
 * 评论状态管理 (Zustand Store)
 * 管理评论数据、UI状态、实时更新等
 */

import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import { immer } from 'zustand/middleware/immer';
import type {
  Comment,
  CommentQuery,
  CommentPagedResult,
  CommentStats,
  CommentFormData,
  CommentListConfig,
  TypingUser,
  CommentNotification,
  CommentError
} from '../types/comment';
import { CommentSortOrder } from '../types/comment';
import { commentApi } from '../services/commentApi';
import { commentSocket } from '../services/commentSocket';

interface CommentState {
  // 评论数据
  comments: Record<string, Comment>; // commentId -> Comment
  commentsByPost: Record<string, string[]>; // postId -> commentId[]
  commentTrees: Record<string, Comment[]>; // postId -> Comment tree
  replyChains: Record<string, string[]>; // parentId -> replyId[]

  // 分页和加载状态
  pagination: Record<string, {
    page: number;
    totalPages: number;
    totalCount: number;
    hasMore: boolean;
    loading: boolean;
  }>; // postId -> pagination info

  // UI状态
  loadingStates: Record<string, boolean>; // operation -> loading
  errors: CommentError[];
  selectedComment: string | null;
  replyingTo: string | null;
  editingComment: string | null;

  // 表单状态
  drafts: Record<string, CommentFormData>; // postId|parentId -> draft
  uploadingImages: string[];

  // 统计信息
  stats: Record<string, CommentStats>; // postId -> stats

  // 实时状态
  typingUsers: TypingUser[];
  onlineUserCounts: Record<string, number>; // postId -> count

  // 通知
  notifications: CommentNotification[];
  unreadNotificationCount: number;

  // 配置
  config: CommentListConfig;

  // 操作方法
  actions: {
    // 数据获取
    loadComments: (postId: string, query?: Partial<CommentQuery>) => Promise<void>;
    loadCommentTree: (postId: string, maxDepth?: number) => Promise<void>;
    loadMoreComments: (postId: string) => Promise<void>;
    refreshComments: (postId: string) => Promise<void>;
    loadStats: (postId: string) => Promise<void>;

    // 评论CRUD
    createComment: (data: CommentFormData, postId: string) => Promise<Comment | null>;
    updateComment: (commentId: string, data: Partial<CommentFormData>) => Promise<void>;
    deleteComment: (commentId: string) => Promise<void>;

    // 互动操作
    likeComment: (commentId: string) => Promise<void>;
    unlikeComment: (commentId: string) => Promise<void>;
    reportComment: (commentId: string, reason: string, description?: string) => Promise<void>;

    // UI状态管理
    setReplyingTo: (commentId: string | null) => void;
    setEditingComment: (commentId: string | null) => void;
    setSelectedComment: (commentId: string | null) => void;

    // 草稿管理
    saveDraft: (key: string, data: CommentFormData) => void;
    loadDraft: (key: string) => CommentFormData | null;
    clearDraft: (key: string) => void;

    // 配置管理
    updateConfig: (config: Partial<CommentListConfig>) => void;
    setSortOrder: (sortOrder: CommentSortOrder) => void;

    // 实时功能
    initializeRealtime: (postId: string) => void;
    cleanupRealtime: () => void;

    // 错误处理
    addError: (error: CommentError) => void;
    clearErrors: () => void;
    removeError: (index: number) => void;

    // 重置状态
    resetPostComments: (postId: string) => void;
    resetAll: () => void;
  };
}

const defaultConfig: CommentListConfig = {
  sortOrder: CommentSortOrder.CreatedAtDesc,
  pageSize: 20,
  maxDepth: 3,
  showAvatars: true,
  showTimestamps: true,
  showStats: true,
  showActions: true,
  enableVirtualScroll: false,
  autoRefresh: false,
  refreshInterval: 30000,
  // Editor configuration
  maxLength: 2000,
  allowMarkdown: true,
  allowEmoji: true,
  allowMention: true,
  allowImageUpload: false,
  placeholder: '写下你的评论...',
  autoFocus: false,
  showToolbar: true
};

export const useCommentStore = create<CommentState>()(
  devtools(
    immer((set, get) => ({
      // 初始状态
      comments: {},
      commentsByPost: {},
      commentTrees: {},
      replyChains: {},
      pagination: {},
      loadingStates: {},
      errors: [],
      selectedComment: null,
      replyingTo: null,
      editingComment: null,
      drafts: {},
      uploadingImages: [],
      stats: {},
      typingUsers: [],
      onlineUserCounts: {},
      notifications: [],
      unreadNotificationCount: 0,
      config: defaultConfig,

      actions: {
        // 加载评论列表
        loadComments: async (postId: string, queryOptions?: Partial<CommentQuery>) => {
          const { config, pagination } = get();

          const query: CommentQuery = {
            postId,
            page: 1,
            pageSize: config.pageSize,
            sortOrder: config.sortOrder,
            ...queryOptions
          };

          set(state => {
            state.loadingStates[`comments_${postId}`] = true;
          });

          try {
            const result = await commentApi.getComments(query);

            set(state => {
              // 更新评论数据
              result.comments.forEach(comment => {
                state.comments[comment.id] = comment;
              });

              // 更新文章评论索引
              state.commentsByPost[postId] = result.comments.map(c => c.id);

              // 更新分页信息
              state.pagination[postId] = {
                page: result.currentPage,
                totalPages: result.totalPages,
                totalCount: result.totalCount,
                hasMore: result.hasNextPage,
                loading: false
              };

              state.loadingStates[`comments_${postId}`] = false;
            });

            // 加载统计信息
            get().actions.loadStats(postId);

          } catch (error: any) {
            set(state => {
              state.loadingStates[`comments_${postId}`] = false;
              state.errors.push({
                type: 'network',
                message: error.message || '加载评论失败'
              });
            });
          }
        },

        // 加载评论树
        loadCommentTree: async (postId: string, maxDepth = 3) => {
          set(state => {
            state.loadingStates[`tree_${postId}`] = true;
          });

          try {
            const tree = await commentApi.getCommentTree(postId, maxDepth);

            set(state => {
              // 递归提取所有评论
              const extractComments = (comments: Comment[]) => {
                comments.forEach(comment => {
                  state.comments[comment.id] = comment;
                  if (comment.replies.length > 0) {
                    extractComments(comment.replies);
                  }
                });
              };

              extractComments(tree);
              state.commentTrees[postId] = tree;
              state.loadingStates[`tree_${postId}`] = false;
            });

          } catch (error: any) {
            set(state => {
              state.loadingStates[`tree_${postId}`] = false;
              state.errors.push({
                type: 'network',
                message: error.message || '加载评论树失败'
              });
            });
          }
        },

        // 加载更多评论
        loadMoreComments: async (postId: string) => {
          const { pagination } = get();
          const currentPagination = pagination[postId];

          if (!currentPagination || !currentPagination.hasMore || currentPagination.loading) {
            return;
          }

          set(state => {
            state.pagination[postId].loading = true;
          });

          try {
            const query: CommentQuery = {
              postId,
              page: currentPagination.page + 1,
              pageSize: get().config.pageSize,
              sortOrder: get().config.sortOrder
            };

            const result = await commentApi.getComments(query);

            set(state => {
              // 添加新评论
              result.comments.forEach(comment => {
                state.comments[comment.id] = comment;
              });

              // 更新文章评论索引
              const existingIds = state.commentsByPost[postId] || [];
              const newIds = result.comments.map(c => c.id);
              state.commentsByPost[postId] = [...existingIds, ...newIds];

              // 更新分页信息
              state.pagination[postId] = {
                page: result.currentPage,
                totalPages: result.totalPages,
                totalCount: result.totalCount,
                hasMore: result.hasNextPage,
                loading: false
              };
            });

          } catch (error: any) {
            set(state => {
              state.pagination[postId].loading = false;
              state.errors.push({
                type: 'network',
                message: error.message || '加载更多评论失败'
              });
            });
          }
        },

        // 刷新评论
        refreshComments: async (postId: string) => {
          await get().actions.loadComments(postId);
        },

        // 创建评论
        createComment: async (data: CommentFormData, postId: string): Promise<Comment | null> => {
          set(state => {
            state.loadingStates['creating'] = true;
          });

          try {
            const request = {
              postId,
              parentId: data.parentId,
              content: data.content,
              mentionedUsers: data.mentionedUsers
            };

            const newComment = await commentApi.createComment(request);

            set(state => {
              // 添加新评论到状态
              state.comments[newComment.id] = newComment;

              // 更新文章评论索引
              const existingIds = state.commentsByPost[postId] || [];

              if (newComment.parentId) {
                // 回复评论：添加到父评论的回复链
                const replyIds = state.replyChains[newComment.parentId] || [];
                state.replyChains[newComment.parentId] = [newComment.id, ...replyIds];
              } else {
                // 根评论：添加到文章评论列表的开头
                state.commentsByPost[postId] = [newComment.id, ...existingIds];
              }

              // 更新统计
              if (state.stats[postId]) {
                state.stats[postId].totalCount++;
                if (!newComment.parentId) {
                  state.stats[postId].rootCommentCount++;
                } else {
                  state.stats[postId].replyCount++;
                }
              }

              // 清除草稿
              const draftKey = data.parentId || postId;
              delete state.drafts[draftKey];

              // 清除回复状态
              state.replyingTo = null;
              state.loadingStates['creating'] = false;
            });

            return newComment;

          } catch (error: any) {
            set(state => {
              state.loadingStates['creating'] = false;
              state.errors.push({
                type: 'validation',
                message: error.message || '发布评论失败'
              });
            });
            return null;
          }
        },

        // 更新评论
        updateComment: async (commentId: string, data: Partial<CommentFormData>) => {
          set(state => {
            state.loadingStates[`updating_${commentId}`] = true;
          });

          try {
            const updateData = {
              content: data.content!,
              mentionedUsers: data.mentionedUsers || []
            };

            const updatedComment = await commentApi.updateComment(commentId, updateData);

            set(state => {
              state.comments[commentId] = updatedComment;
              state.editingComment = null;
              state.loadingStates[`updating_${commentId}`] = false;
            });

          } catch (error: any) {
            set(state => {
              state.loadingStates[`updating_${commentId}`] = false;
              state.errors.push({
                type: 'validation',
                message: error.message || '更新评论失败'
              });
            });
          }
        },

        // 删除评论
        deleteComment: async (commentId: string) => {
          const comment = get().comments[commentId];
          if (!comment) return;

          set(state => {
            state.loadingStates[`deleting_${commentId}`] = true;
          });

          try {
            await commentApi.deleteComment(commentId);

            set(state => {
              // 移除评论
              delete state.comments[commentId];

              // 更新索引
              if (comment.parentId) {
                const replyIds = state.replyChains[comment.parentId] || [];
                state.replyChains[comment.parentId] = replyIds.filter((id: string) => id !== commentId);
              } else {
                const postIds = state.commentsByPost[comment.postId] || [];
                state.commentsByPost[comment.postId] = postIds.filter((id: string) => id !== commentId);
              }

              // 更新统计
              if (state.stats[comment.postId]) {
                state.stats[comment.postId].totalCount--;
                if (!comment.parentId) {
                  state.stats[comment.postId].rootCommentCount--;
                } else {
                  state.stats[comment.postId].replyCount--;
                }
              }

              state.loadingStates[`deleting_${commentId}`] = false;
              state.selectedComment = null;
            });

          } catch (error: any) {
            set(state => {
              state.loadingStates[`deleting_${commentId}`] = false;
              state.errors.push({
                type: 'permission',
                message: error.message || '删除评论失败'
              });
            });
          }
        },

        // 点赞评论
        likeComment: async (commentId: string) => {
          try {
            await commentApi.likeComment(commentId);

            set(state => {
              const comment = state.comments[commentId];
              if (comment) {
                comment.likeCount++;
                comment.isLiked = true;
              }
            });

          } catch (error: any) {
            set(state => {
              state.errors.push({
                type: 'network',
                message: error.message || '点赞失败'
              });
            });
          }
        },

        // 取消点赞
        unlikeComment: async (commentId: string) => {
          try {
            await commentApi.unlikeComment(commentId);

            set(state => {
              const comment = state.comments[commentId];
              if (comment) {
                comment.likeCount = Math.max(0, comment.likeCount - 1);
                comment.isLiked = false;
              }
            });

          } catch (error: any) {
            set(state => {
              state.errors.push({
                type: 'network',
                message: error.message || '取消点赞失败'
              });
            });
          }
        },

        // 举报评论
        reportComment: async (commentId: string, reason: string, description?: string) => {
          try {
            await commentApi.reportComment(commentId, { reason: reason as any, description });
          } catch (error: any) {
            set(state => {
              state.errors.push({
                type: 'network',
                message: error.message || '举报失败'
              });
            });
          }
        },

        // 加载统计信息
        loadStats: async (postId: string) => {
          try {
            const stats = await commentApi.getCommentStats(postId);
            set(state => {
              state.stats[postId] = stats;
            });
          } catch (error) {
            console.error('Failed to load comment stats:', error);
          }
        },

        // UI状态管理
        setReplyingTo: (commentId: string | null) => {
          set(state => {
            state.replyingTo = commentId;
          });
        },

        setEditingComment: (commentId: string | null) => {
          set(state => {
            state.editingComment = commentId;
          });
        },

        setSelectedComment: (commentId: string | null) => {
          set(state => {
            state.selectedComment = commentId;
          });
        },

        // 草稿管理
        saveDraft: (key: string, data: CommentFormData) => {
          set(state => {
            state.drafts[key] = data;
          });
        },

        loadDraft: (key: string): CommentFormData | null => {
          return get().drafts[key] || null;
        },

        clearDraft: (key: string) => {
          set(state => {
            delete state.drafts[key];
          });
        },

        // 配置管理
        updateConfig: (newConfig: Partial<CommentListConfig>) => {
          set(state => {
            state.config = { ...state.config, ...newConfig };
          });
        },

        setSortOrder: (sortOrder: CommentSortOrder) => {
          set(state => {
            state.config.sortOrder = sortOrder;
          });
        },

        // 实时功能初始化
        initializeRealtime: (postId: string) => {
          // 连接WebSocket
          commentSocket.connect();
          commentSocket.joinPostGroup(postId);

          // 绑定事件监听器
          const handleCommentCreated = (comment: Comment) => {
            if (comment.postId === postId) {
              set(state => {
                state.comments[comment.id] = comment;

                if (comment.parentId) {
                  const replyIds = state.replyChains[comment.parentId] || [];
                  state.replyChains[comment.parentId] = [comment.id, ...replyIds];
                } else {
                  const existingIds = state.commentsByPost[postId] || [];
                  state.commentsByPost[postId] = [comment.id, ...existingIds];
                }

                if (state.stats[postId]) {
                  state.stats[postId].totalCount++;
                }
              });
            }
          };

          const handleCommentUpdated = (comment: Comment) => {
            if (comment.postId === postId) {
              set(state => {
                state.comments[comment.id] = comment;
              });
            }
          };

          const handleCommentDeleted = ({ commentId }: { commentId: string }) => {
            set(state => {
              const comment = state.comments[commentId];
              if (comment && comment.postId === postId) {
                delete state.comments[commentId];

                if (comment.parentId) {
                  const replyIds = state.replyChains[comment.parentId] || [];
                  state.replyChains[comment.parentId] = replyIds.filter((id: string) => id !== commentId);
                } else {
                  const postCommentIds = state.commentsByPost[postId] || [];
                  state.commentsByPost[postId] = postCommentIds.filter((id: string) => id !== commentId);
                }

                if (state.stats[postId]) {
                  state.stats[postId].totalCount--;
                }
              }
            });
          };

          const handleCommentLiked = ({ commentId, userId }: { commentId: string; userId: string }) => {
            set(state => {
              const comment = state.comments[commentId];
              if (comment) {
                comment.likeCount++;
              }
            });
          };

          const handleCommentUnliked = ({ commentId, userId }: { commentId: string; userId: string }) => {
            set(state => {
              const comment = state.comments[commentId];
              if (comment) {
                comment.likeCount = Math.max(0, comment.likeCount - 1);
              }
            });
          };

          const handleUserStartedTyping = (typingInfo: any) => {
            if (typingInfo.postId === postId) {
              set(state => {
                const existing = state.typingUsers.find(
                  (u: any) => u.userId === typingInfo.userId &&
                       u.postId === typingInfo.postId &&
                       u.parentId === typingInfo.parentId
                );
                if (!existing) {
                  state.typingUsers.push(typingInfo);
                }
              });
            }
          };

          const handleUserStoppedTyping = (typingInfo: any) => {
            if (typingInfo.postId === postId) {
              set(state => {
                state.typingUsers = state.typingUsers.filter(
                  (u: any) => !(u.userId === typingInfo.userId &&
                         u.postId === typingInfo.postId &&
                         u.parentId === typingInfo.parentId)
                );
              });
            }
          };

          const handleCommentStats = (stats: CommentStats) => {
            if (stats.postId === postId) {
              set(state => {
                state.stats[postId] = stats;
              });
            }
          };

          const handleOnlineUserCount = ({ postId: countPostId, count }: { postId: string; count: number }) => {
            if (countPostId === postId) {
              set(state => {
                state.onlineUserCounts[postId] = count;
              });
            }
          };

          // 绑定所有事件
          commentSocket.on('CommentCreated', handleCommentCreated);
          commentSocket.on('CommentUpdated', handleCommentUpdated);
          commentSocket.on('CommentDeleted', handleCommentDeleted);
          commentSocket.on('CommentLiked', handleCommentLiked);
          commentSocket.on('CommentUnliked', handleCommentUnliked);
          commentSocket.on('UserStartedTyping', handleUserStartedTyping);
          commentSocket.on('UserStoppedTyping', handleUserStoppedTyping);
          commentSocket.on('CommentStats', handleCommentStats);
          commentSocket.on('OnlineUserCount', handleOnlineUserCount);

          // 获取初始统计信息
          commentSocket.getCommentStats(postId);
          commentSocket.getOnlineUserCount(postId);
        },

        // 清理实时功能
        cleanupRealtime: () => {
          // 这里应该清理事件监听器，但由于SignalR的限制，我们只能断开连接
          // 在实际应用中，可能需要维护一个监听器注册表
        },

        // 错误处理
        addError: (error: CommentError) => {
          set(state => {
            state.errors.push(error);
          });
        },

        clearErrors: () => {
          set(state => {
            state.errors = [];
          });
        },

        removeError: (index: number) => {
          set(state => {
            state.errors.splice(index, 1);
          });
        },

        // 重置状态
        resetPostComments: (postId: string) => {
          set(state => {
            // 移除该文章的所有评论
            const commentIds = state.commentsByPost[postId] || [];
            commentIds.forEach((id: string) => {
              delete state.comments[id];
            });

            delete state.commentsByPost[postId];
            delete state.commentTrees[postId];
            delete state.pagination[postId];
            delete state.stats[postId];
            delete state.onlineUserCounts[postId];

            // 清理该文章的回复链
            Object.keys(state.replyChains).forEach(parentId => {
              if (state.comments[parentId]?.postId === postId) {
                delete state.replyChains[parentId];
              }
            });

            // 清理该文章的草稿
            Object.keys(state.drafts).forEach(key => {
              if (key === postId || key.startsWith(`${postId}_`)) {
                delete state.drafts[key];
              }
            });
          });
        },

        resetAll: () => {
          set(state => {
            Object.assign(state, {
              comments: {},
              commentsByPost: {},
              commentTrees: {},
              replyChains: {},
              pagination: {},
              loadingStates: {},
              errors: [],
              selectedComment: null,
              replyingTo: null,
              editingComment: null,
              drafts: {},
              uploadingImages: [],
              stats: {},
              typingUsers: [],
              onlineUserCounts: {},
              notifications: [],
              unreadNotificationCount: 0
            });
          });
        }
      }
    })),
    { name: 'comment-store' }
  )
);

// 导出便捷的hook
export const useComments = (postId: string) => {
  const store = useCommentStore();
  const comments = store.commentsByPost[postId]?.map(id => store.comments[id]) || [];
  const loading = store.loadingStates[`comments_${postId}`] || false;
  const pagination = store.pagination[postId];
  const stats = store.stats[postId];

  return {
    comments,
    loading,
    pagination,
    stats,
    actions: store.actions
  };
};

export const useCommentTree = (postId: string) => {
  const store = useCommentStore();
  const tree = store.commentTrees[postId] || [];
  const loading = store.loadingStates[`tree_${postId}`] || false;

  return {
    tree,
    loading,
    actions: store.actions
  };
};

export const useCommentActions = () => {
  const store = useCommentStore();
  return store.actions;
};
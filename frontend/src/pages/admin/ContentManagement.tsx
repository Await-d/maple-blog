/**
 * ContentManagement - 内容管理页面
 * 提供文章和评论的全面管理功能，包括审核、编辑、删除等操作
 * 完全去除模拟数据，使用真实API集成
 */

import React, { useState, useMemo, useCallback } from 'react';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@/components/ui/tabs';
import UserAvatar from '@/components/common/UserAvatar';
import { Helmet } from '@/components/common/DocumentHead';
import { ToastProvider } from '../../components/admin/ToastNotifications';
// Real API Integration
import {
  useAdminPostQueries,
  useAdminCommentQueries,
  useAdminMutations,
  AdminPost,
  AdminComment,
  PostFilters,
  CommentFilters,
  handleAdminApiError,
} from '../../services/admin/adminApi';
import { useNotifications } from '../../services/admin/notificationService';
import {
  FileText,
  MessageSquare,
  Search,
  Filter,
  MoreHorizontal,
  Edit,
  Trash2,
  Eye,
  EyeOff,
  Star,
  StarOff,
  CheckCircle,
  XCircle,
  Flag,
  Archive,
  Plus,
  Download,
  Grid3x3,
  List,
  AlertTriangle,
  CheckSquare,
  Square,
  RotateCcw,
  Settings,
  TrendingUp,
  Activity,
  MessageCircle,
  Reply,
  Globe,
  ExternalLink,
} from 'lucide-react';

// Enhanced Filter Types with Pagination
interface ContentFilters extends PostFilters {
  featured?: boolean;
  tag?: string;
}

interface AdminCommentFilters extends CommentFilters {
  post?: string;
}

// Pagination Constants
const DEFAULT_PAGE_SIZE = 20;
const PAGE_SIZE_OPTIONS = [10, 20, 50, 100];

const ContentManagement: React.FC = () => {
  // Real API Integration - No Mock Data
  const notifications = useNotifications();

  // State management
  const [activeTab, setActiveTab] = useState('posts');
  const [selectedPosts, setSelectedPosts] = useState<Set<string>>(new Set());
  const [selectedComments, setSelectedComments] = useState<Set<string>>(new Set());
  const [showPreviewModal, setShowPreviewModal] = useState(false);
  const [showBulkModal, setShowBulkModal] = useState(false);
  const [showReplyModal, setShowReplyModal] = useState(false);
  const [replyContent, setReplyContent] = useState('');
  const [replyingToComment, setReplyingToComment] = useState<AdminComment | null>(null);
  const [previewPost, setPreviewPost] = useState<AdminPost | null>(null);
  const [viewMode, setViewMode] = useState<'table' | 'card'>('table');

  // Filters with Pagination
  const [postFilters, setPostFilters] = useState<ContentFilters>({
    search: '',
    status: '',
    author: '',
    category: '',
    dateFrom: '',
    dateTo: '',
    featured: undefined,
    tag: '',
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
    sortBy: 'publishDate',
    sortOrder: 'desc',
  });

  const [commentFilters, setCommentFilters] = useState<AdminCommentFilters>({
    search: '',
    status: '',
    author: '',
    post: '',
    dateFrom: '',
    dateTo: '',
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
    sortBy: 'createdDate',
    sortOrder: 'desc',
  });

  const [showFilters, setShowFilters] = useState(false);
  const [bulkAction, setBulkAction] = useState('');

  // Real API Queries - Replace Mock Data
  const postQueries = useAdminPostQueries();
  const commentQueries = useAdminCommentQueries();
  const mutations = useAdminMutations();

  // Fetch Posts with Real API
  const {
    data: postsResponse,
    isLoading: postsLoading,
    error: postsError,
    refetch: refetchPosts
  } = postQueries.usePostsList(postFilters);

  // Fetch Comments with Real API
  const {
    data: commentsResponse,
    isLoading: commentsLoading,
    error: commentsError,
    refetch: refetchComments
  } = commentQueries.useCommentsList(commentFilters);

  // Fetch Statistics with Real API
  const {
    data: postStats
  } = postQueries.usePostStatistics();

  const {
    data: commentStats
  } = commentQueries.useCommentStatistics();

  // Extract data from responses
  const posts = postsResponse?.posts || [];
  const comments = commentsResponse?.comments || [];
  const loading = postsLoading || commentsLoading;

  // Error Handling
  React.useEffect(() => {
    if (postsError) {
      notifications.error(
        'Failed to load posts',
        handleAdminApiError(postsError)
      );
    }
  }, [postsError, notifications]);

  React.useEffect(() => {
    if (commentsError) {
      notifications.error(
        'Failed to load comments',
        handleAdminApiError(commentsError)
      );
    }
  }, [commentsError, notifications]);

  // Server-side filtering and pagination
  const filteredPosts = posts; // Already filtered on server
  const filteredComments = comments; // Already filtered on server

  // Pagination handlers
  const handlePostPageChange = useCallback((page: number) => {
    setPostFilters(prev => ({ ...prev, page }));
    setSelectedPosts(new Set()); // Clear selection on page change
  }, []);

  const handleCommentPageChange = useCallback((page: number) => {
    setCommentFilters(prev => ({ ...prev, page }));
    setSelectedComments(new Set()); // Clear selection on page change
  }, []);

  const handlePostPageSizeChange = useCallback((pageSize: number) => {
    setPostFilters(prev => ({ ...prev, pageSize, page: 1 }));
    setSelectedPosts(new Set());
  }, []);

  const handleCommentPageSizeChange = useCallback((pageSize: number) => {
    setCommentFilters(prev => ({ ...prev, pageSize, page: 1 }));
    setSelectedComments(new Set());
  }, []);

  // Real-time statistics from API
  const realPostStats = useMemo(() => {
    return postStats || {
      total: 0,
      published: 0,
      drafts: 0,
      archived: 0,
      featured: 0,
      totalViews: 0,
      totalComments: 0,
      avgSeoScore: 0,
    };
  }, [postStats]);

  const realCommentStats = useMemo(() => {
    return commentStats || {
      total: 0,
      approved: 0,
      pending: 0,
      rejected: 0,
      spam: 0,
      todayCount: 0,
      weeklyGrowth: 0,
    };
  }, [commentStats]);

  // Confirmation helper using notification service
  const showConfirm = useCallback((message: string, onConfirm: () => void) => {
    notifications.showConfirmation(
      'Confirm Action',
      message,
      onConfirm
    );
  }, [notifications]);

  // Utility functions
  const getStatusColor = (status: string, type: 'post' | 'comment' = 'post') => {
    const statusColors = {
      post: {
        'Published': 'bg-green-100 text-green-800',
        'Draft': 'bg-yellow-100 text-yellow-800',
        'Archived': 'bg-gray-100 text-gray-800',
      } as Record<string, string>,
      comment: {
        'Approved': 'bg-green-100 text-green-800',
        'Pending': 'bg-yellow-100 text-yellow-800',
        'Rejected': 'bg-red-100 text-red-800',
        'Spam': 'bg-red-100 text-red-800',
      } as Record<string, string>,
    };
    return statusColors[type][status] || 'bg-gray-100 text-gray-800';
  };

  const getSeoColor = (score: number) => {
    if (score >= 90) return 'text-green-600';
    if (score >= 70) return 'text-yellow-600';
    return 'text-red-600';
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  const formatDateTime = (dateString: string) => {
    return new Date(dateString).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  // Real Post Action Handlers - No Mock Operations
  const handlePostAction = useCallback(async (action: string, post: AdminPost) => {
    try {
      switch (action) {
        case 'toggleStatus':
          await mutations.togglePostStatusMutation.mutateAsync(post.id);
          notifications.success('Post status updated successfully');
          break;

        case 'toggleFeatured':
          await mutations.togglePostFeaturedMutation.mutateAsync(post.id);
          notifications.success(
            post.featured ? 'Post removed from featured' : 'Post featured successfully'
          );
          break;

        case 'archive':
          await mutations.archivePostMutation.mutateAsync(post.id);
          notifications.success('Post archived successfully');
          break;

        case 'delete':
          showConfirm(
            `Are you sure you want to delete "${post.title}"? This action cannot be undone.`,
            async () => {
              try {
                await mutations.deletePostMutation.mutateAsync(post.id);
                notifications.success('Post deleted successfully');
                setSelectedPosts(prev => {
                  const newSet = new Set(prev);
                  newSet.delete(post.id);
                  return newSet;
                });
              } catch (error) {
                notifications.error('Failed to delete post', handleAdminApiError(error));
              }
            }
          );
          break;

        case 'preview':
          setPreviewPost(post);
          setShowPreviewModal(true);
          break;

        case 'edit':
          // Navigate to edit page
          window.location.href = `/admin/posts/${post.id}/edit`;
          break;

        default:
          notifications.warning('Unknown action', `Action "${action}" is not supported`);
          break;
      }
    } catch (error) {
      notifications.error(
        `Failed to ${action} post`,
        handleAdminApiError(error)
      );
    }
  }, [mutations, notifications, showConfirm]);

  // Real Comment Action Handlers - No Mock Operations
  const handleCommentAction = useCallback(async (action: string, comment: AdminComment) => {
    try {
      switch (action) {
        case 'approve':
          await mutations.approveCommentMutation.mutateAsync(comment.id);
          notifications.success('Comment approved successfully');
          break;

        case 'reject':
          await mutations.rejectCommentMutation.mutateAsync(comment.id);
          notifications.success('Comment rejected successfully');
          break;

        case 'spam':
          await mutations.markCommentAsSpamMutation.mutateAsync(comment.id);
          notifications.success('Comment marked as spam');
          break;

        case 'delete':
          showConfirm(
            'Are you sure you want to delete this comment? This action cannot be undone.',
            async () => {
              try {
                await mutations.deleteCommentMutation.mutateAsync(comment.id);
                notifications.success('Comment deleted successfully');
                setSelectedComments(prev => {
                  const newSet = new Set(prev);
                  newSet.delete(comment.id);
                  return newSet;
                });
              } catch (error) {
                notifications.error('Failed to delete comment', handleAdminApiError(error));
              }
            }
          );
          break;

        case 'reply':
          // Open reply dialog
          setReplyingToComment(comment);
          setReplyContent('');
          setShowReplyModal(true);
          break;

        default:
          notifications.warning('Unknown action', `Action "${action}" is not supported`);
          break;
      }
    } catch (error) {
      notifications.error(
        `Failed to ${action} comment`,
        handleAdminApiError(error)
      );
    }
  }, [mutations, notifications, showConfirm]);

  // Real Bulk Action Handler - No Mock Operations
  const handleBulkAction = useCallback(async () => {
    const selectedItems = activeTab === 'posts' ? selectedPosts : selectedComments;
    if (selectedItems.size === 0) {
      notifications.warning('No items selected', 'Please select items to perform bulk actions');
      return;
    }

    const itemIds = Array.from(selectedItems);
    const itemCount = itemIds.length;

    try {
      if (activeTab === 'posts') {
        const operation = {
          action: bulkAction as 'publish' | 'archive' | 'delete' | 'feature' | 'unfeature',
          postIds: itemIds,
        };

        if (bulkAction === 'delete') {
          showConfirm(
            `Delete ${itemCount} selected posts? This action cannot be undone.`,
            async () => {
              try {
                const result = await mutations.bulkPostOperationMutation.mutateAsync(operation);
                notifications.showBulkOperationResult('post deletion', result);
                setSelectedPosts(new Set());
              } catch (error) {
                notifications.error('Bulk delete failed', handleAdminApiError(error));
              }
            }
          );
        } else {
          const result = await mutations.bulkPostOperationMutation.mutateAsync(operation);
          notifications.showBulkOperationResult(`post ${bulkAction}`, result);
          setSelectedPosts(new Set());
        }
      } else {
        const operation = {
          action: bulkAction as 'approve' | 'reject' | 'spam' | 'delete',
          commentIds: itemIds,
        };

        if (bulkAction === 'delete') {
          showConfirm(
            `Delete ${itemCount} selected comments? This action cannot be undone.`,
            async () => {
              try {
                const result = await mutations.bulkCommentOperationMutation.mutateAsync(operation);
                notifications.showBulkOperationResult('comment deletion', result);
                setSelectedComments(new Set());
              } catch (error) {
                notifications.error('Bulk delete failed', handleAdminApiError(error));
              }
            }
          );
        } else {
          const result = await mutations.bulkCommentOperationMutation.mutateAsync(operation);
          notifications.showBulkOperationResult(`comment ${bulkAction}`, result);
          setSelectedComments(new Set());
        }
      }
    } catch (error) {
      notifications.error(
        `Bulk ${bulkAction} failed`,
        handleAdminApiError(error)
      );
    }

    setShowBulkModal(false);
    setBulkAction('');
  }, [activeTab, selectedPosts, selectedComments, bulkAction, mutations, notifications, showConfirm]);

  // Clear Filters Handler
  const clearFilters = useCallback(() => {
    if (activeTab === 'posts') {
      setPostFilters({
        search: '',
        status: '',
        author: '',
        category: '',
        dateFrom: '',
        dateTo: '',
        featured: undefined,
        tag: '',
        page: 1,
        pageSize: DEFAULT_PAGE_SIZE,
        sortBy: 'publishDate',
        sortOrder: 'desc',
      });
    } else {
      setCommentFilters({
        search: '',
        status: '',
        author: '',
        post: '',
        dateFrom: '',
        dateTo: '',
        page: 1,
        pageSize: DEFAULT_PAGE_SIZE,
        sortBy: 'createdDate',
        sortOrder: 'desc',
      });
    }
    notifications.info('Filters cleared');
  }, [activeTab, notifications]);

  // Refresh Data Handler
  const _handleRefreshData = useCallback(() => {
    if (activeTab === 'posts') {
      refetchPosts();
      notifications.info('Posts refreshed');
    } else {
      refetchComments();
      notifications.info('Comments refreshed');
    }
  }, [activeTab, refetchPosts, refetchComments, notifications]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-600">Loading content...</p>
        </div>
      </div>
    );
  }

  return (
    <ToastProvider>
      <Helmet>
        <title>内容管理 - Maple Blog</title>
        <meta name="description" content="管理博客文章和评论，包括审核、编辑、删除等功能。" />
        <meta name="robots" content="noindex, nofollow" />
      </Helmet>

      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold flex items-center">
              <FileText className="w-8 h-8 mr-3 text-blue-600" />
              Content Management
            </h1>
            <p className="text-gray-600">
              Manage blog posts and comments across the platform
            </p>
          </div>
          <div className="flex items-center space-x-2">
            <Button variant="outline" size="sm">
              <Download className="mr-2 h-4 w-4" />
              Export
            </Button>
            <Button size="sm">
              <Plus className="mr-2 h-4 w-4" />
              New Post
            </Button>
          </div>
        </div>

        {/* Stats Overview */}
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Total Posts</CardTitle>
              <FileText className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{realPostStats.total}</div>
              <p className="text-xs text-muted-foreground">
                {realPostStats.published} published, {realPostStats.drafts} drafts
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Total Comments</CardTitle>
              <MessageSquare className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{realCommentStats.total}</div>
              <p className="text-xs text-muted-foreground">
                {realCommentStats.pending} pending review
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Total Views</CardTitle>
              <TrendingUp className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{realPostStats.totalViews.toLocaleString()}</div>
              <p className="text-xs text-muted-foreground">
                Across all published posts
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Engagement</CardTitle>
              <Activity className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">
                {((realCommentStats.total / realPostStats.published || 0) * 100).toFixed(1)}%
              </div>
              <p className="text-xs text-muted-foreground">
                Comments per published post
              </p>
            </CardContent>
          </Card>
        </div>

        {/* Main Content */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle>Content Overview</CardTitle>
              <div className="flex items-center space-x-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowFilters(!showFilters)}
                >
                  <Filter className="mr-2 h-4 w-4" />
                  Filters
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setViewMode(viewMode === 'table' ? 'card' : 'table')}
                >
                  {viewMode === 'table' ? (
                    <Grid3x3 className="mr-2 h-4 w-4" />
                  ) : (
                    <List className="mr-2 h-4 w-4" />
                  )}
                  {viewMode === 'table' ? 'Card View' : 'Table View'}
                </Button>
                {((activeTab === 'posts' && selectedPosts.size > 0) || 
                  (activeTab === 'comments' && selectedComments.size > 0)) && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setShowBulkModal(true)}
                  >
                    <Settings className="mr-2 h-4 w-4" />
                    Bulk Actions ({activeTab === 'posts' ? selectedPosts.size : selectedComments.size})
                  </Button>
                )}
              </div>
            </div>
          </CardHeader>

          <CardContent>
            <Tabs value={activeTab} onValueChange={setActiveTab}>
              <TabsList>
                <TabsTrigger value="posts">
                  Posts ({realPostStats.total})
                </TabsTrigger>
                <TabsTrigger value="comments">
                  Comments ({realCommentStats.total})
                </TabsTrigger>
              </TabsList>

              {/* Filters */}
              {showFilters && (
                <div className="mt-4 p-4 border rounded-lg bg-gray-50">
                  {activeTab === 'posts' ? (
                    <div className="grid gap-4 md:grid-cols-3 lg:grid-cols-4">
                      <Input
                        placeholder="Search posts..."
                        value={postFilters.search}
                        onChange={(e) => setPostFilters(prev => ({ ...prev, search: e.target.value }))}
                        leftIcon={<Search className="h-4 w-4" />}
                      />
                      <select
                        className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        value={postFilters.status}
                        onChange={(e) => setPostFilters(prev => ({ ...prev, status: e.target.value }))}
                      >
                        <option value="">All Statuses</option>
                        <option value="Published">Published</option>
                        <option value="Draft">Draft</option>
                        <option value="Archived">Archived</option>
                      </select>
                      <Input
                        placeholder="Author..."
                        value={postFilters.author}
                        onChange={(e) => setPostFilters(prev => ({ ...prev, author: e.target.value }))}
                      />
                      <Input
                        placeholder="Category..."
                        value={postFilters.category}
                        onChange={(e) => setPostFilters(prev => ({ ...prev, category: e.target.value }))}
                      />
                      <Input
                        type="date"
                        placeholder="From Date"
                        value={postFilters.dateFrom}
                        onChange={(e) => setPostFilters(prev => ({ ...prev, dateFrom: e.target.value }))}
                      />
                      <Input
                        type="date"
                        placeholder="To Date"
                        value={postFilters.dateTo}
                        onChange={(e) => setPostFilters(prev => ({ ...prev, dateTo: e.target.value }))}
                      />
                      <select
                        className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        value={postFilters.featured === undefined ? '' : postFilters.featured.toString()}
                        onChange={(e) => setPostFilters(prev => ({ 
                          ...prev, 
                          featured: e.target.value === '' ? undefined : e.target.value === 'true' 
                        }))}
                      >
                        <option value="">All Posts</option>
                        <option value="true">Featured</option>
                        <option value="false">Not Featured</option>
                      </select>
                      <Button variant="outline" onClick={clearFilters}>
                        <RotateCcw className="mr-2 h-4 w-4" />
                        Clear
                      </Button>
                    </div>
                  ) : (
                    <div className="grid gap-4 md:grid-cols-3 lg:grid-cols-4">
                      <Input
                        placeholder="Search comments..."
                        value={commentFilters.search}
                        onChange={(e) => setCommentFilters(prev => ({ ...prev, search: e.target.value }))}
                        leftIcon={<Search className="h-4 w-4" />}
                      />
                      <select
                        className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        value={commentFilters.status}
                        onChange={(e) => setCommentFilters(prev => ({ ...prev, status: e.target.value }))}
                      >
                        <option value="">All Statuses</option>
                        <option value="Approved">Approved</option>
                        <option value="Pending">Pending</option>
                        <option value="Rejected">Rejected</option>
                        <option value="Spam">Spam</option>
                      </select>
                      <Input
                        placeholder="Comment author..."
                        value={commentFilters.author}
                        onChange={(e) => setCommentFilters(prev => ({ ...prev, author: e.target.value }))}
                      />
                      <Input
                        placeholder="Post title..."
                        value={commentFilters.post}
                        onChange={(e) => setCommentFilters(prev => ({ ...prev, post: e.target.value }))}
                      />
                      <Input
                        type="date"
                        placeholder="From Date"
                        value={commentFilters.dateFrom}
                        onChange={(e) => setCommentFilters(prev => ({ ...prev, dateFrom: e.target.value }))}
                      />
                      <Input
                        type="date"
                        placeholder="To Date"
                        value={commentFilters.dateTo}
                        onChange={(e) => setCommentFilters(prev => ({ ...prev, dateTo: e.target.value }))}
                      />
                      <div></div>
                      <Button variant="outline" onClick={clearFilters}>
                        <RotateCcw className="mr-2 h-4 w-4" />
                        Clear
                      </Button>
                    </div>
                  )}
                  
                  {(postFilters.search || commentFilters.search) && (
                    <div className="mt-4 p-3 bg-blue-50 rounded-lg">
                      <p className="text-sm text-blue-700">
                        {activeTab === 'posts' ? (
                          postsResponse ? (
                            <>Showing {postsResponse.posts.length} of {postsResponse.total} posts on page {postsResponse.page}</>
                          ) : (
                            'Loading posts...'
                          )
                        ) : (
                          commentsResponse ? (
                            <>Showing {commentsResponse.comments.length} of {commentsResponse.total} comments on page {commentsResponse.page}</>
                          ) : (
                            'Loading comments...'
                          )
                        )}
                        {activeTab === 'posts' && postFilters.search && ` matching "${postFilters.search}"`}
                        {activeTab === 'comments' && commentFilters.search && ` matching "${commentFilters.search}"`}
                      </p>
                    </div>
                  )}
                </div>
              )}

              {/* Posts Tab */}
              <TabsContent value="posts" className="mt-6">
                {viewMode === 'table' ? (
                  <div className="overflow-x-auto">
                    <table className="w-full table-auto">
                      <thead>
                        <tr className="border-b">
                          <th className="text-left py-2 px-2">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => {
                                if (selectedPosts.size === filteredPosts.length) {
                                  setSelectedPosts(new Set());
                                } else {
                                  setSelectedPosts(new Set(filteredPosts.map(p => p.id)));
                                }
                              }}
                              className="p-0 h-auto"
                            >
                              {selectedPosts.size === filteredPosts.length && filteredPosts.length > 0 ? (
                                <CheckSquare className="h-4 w-4" />
                              ) : (
                                <Square className="h-4 w-4" />
                              )}
                            </Button>
                          </th>
                          <th className="text-left py-2 px-4">Title</th>
                          <th className="text-left py-2 px-4">Author</th>
                          <th className="text-left py-2 px-4">Status</th>
                          <th className="text-left py-2 px-4">Category</th>
                          <th className="text-left py-2 px-4">Published</th>
                          <th className="text-left py-2 px-4">Stats</th>
                          <th className="text-left py-2 px-4">SEO</th>
                          <th className="text-left py-2 px-4">Actions</th>
                        </tr>
                      </thead>
                      <tbody>
                        {filteredPosts.map((post) => (
                          <tr key={post.id} className="border-b hover:bg-gray-50">
                            <td className="py-3 px-2">
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => {
                                  const newSelected = new Set(selectedPosts);
                                  if (newSelected.has(post.id)) {
                                    newSelected.delete(post.id);
                                  } else {
                                    newSelected.add(post.id);
                                  }
                                  setSelectedPosts(newSelected);
                                }}
                                className="p-0 h-auto"
                              >
                                {selectedPosts.has(post.id) ? (
                                  <CheckSquare className="h-4 w-4 text-blue-600" />
                                ) : (
                                  <Square className="h-4 w-4" />
                                )}
                              </Button>
                            </td>
                            <td className="py-3 px-4">
                              <div className="flex items-center space-x-2">
                                <div>
                                  <div className="font-medium text-sm">{post.title}</div>
                                  <div className="text-xs text-gray-500">/{post.slug}</div>
                                  {post.featured && (
                                    <Star className="h-3 w-3 text-yellow-500 mt-1" />
                                  )}
                                </div>
                              </div>
                            </td>
                            <td className="py-3 px-4">
                              <div className="flex items-center space-x-2">
                                <UserAvatar
                                  user={{
                                    id: post.author.id,
                                    displayName: post.author.name,
                                    username: post.author.name.toLowerCase().replace(' ', ''),
                                    avatarUrl: post.author.avatar,
                                    role: 'Author',
                                    isVip: false,
                                  }}
                                  size="sm"
                                  showStatus={false}
                                  showRole={false}
                                />
                                <span className="text-sm">{post.author.name}</span>
                              </div>
                            </td>
                            <td className="py-3 px-4">
                              <Badge className={getStatusColor(post.status, 'post')}>
                                {post.status}
                              </Badge>
                            </td>
                            <td className="py-3 px-4">
                              <span className="text-sm">{post.category.name}</span>
                            </td>
                            <td className="py-3 px-4">
                              <div className="text-sm">
                                {formatDate(post.publishDate)}
                              </div>
                              <div className="text-xs text-gray-500">
                                Modified: {formatDate(post.lastModified)}
                              </div>
                            </td>
                            <td className="py-3 px-4">
                              <div className="text-sm">
                                <div className="flex items-center space-x-1">
                                  <Eye className="h-3 w-3" />
                                  <span>{post.viewCount.toLocaleString()}</span>
                                </div>
                                <div className="flex items-center space-x-1 text-xs text-gray-500">
                                  <MessageCircle className="h-3 w-3" />
                                  <span>{post.commentCount}</span>
                                </div>
                              </div>
                            </td>
                            <td className="py-3 px-4">
                              <div className={`text-sm font-medium ${getSeoColor(post.seoScore)}`}>
                                {post.seoScore}/100
                              </div>
                            </td>
                            <td className="py-3 px-4">
                              <div className="flex items-center space-x-1">
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handlePostAction('preview', post)}
                                  className="p-1 h-auto"
                                  title="Preview"
                                >
                                  <Eye className="h-4 w-4" />
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handlePostAction('edit', post)}
                                  className="p-1 h-auto"
                                  title="Edit"
                                >
                                  <Edit className="h-4 w-4" />
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handlePostAction('toggleStatus', post)}
                                  className="p-1 h-auto"
                                  title={post.status === 'Published' ? 'Unpublish' : 'Publish'}
                                >
                                  {post.status === 'Published' ? (
                                    <EyeOff className="h-4 w-4" />
                                  ) : (
                                    <Globe className="h-4 w-4" />
                                  )}
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handlePostAction('toggleFeatured', post)}
                                  className="p-1 h-auto"
                                  title={post.featured ? 'Remove from Featured' : 'Feature'}
                                >
                                  {post.featured ? (
                                    <StarOff className="h-4 w-4" />
                                  ) : (
                                    <Star className="h-4 w-4" />
                                  )}
                                </Button>
                                <div className="relative group">
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    className="p-1 h-auto"
                                  >
                                    <MoreHorizontal className="h-4 w-4" />
                                  </Button>
                                  <div className="absolute right-0 top-8 w-48 bg-white border rounded-md shadow-lg hidden group-hover:block z-10">
                                    <div className="py-1">
                                      <button
                                        className="block w-full text-left px-3 py-2 text-sm hover:bg-gray-50"
                                        onClick={() => handlePostAction('archive', post)}
                                      >
                                        <Archive className="inline-block w-4 h-4 mr-2" />
                                        Archive Post
                                      </button>
                                      <button
                                        className="block w-full text-left px-3 py-2 text-sm hover:bg-gray-50"
                                        onClick={() => window.open(`/blog/${post.slug}`, '_blank')}
                                      >
                                        <ExternalLink className="inline-block w-4 h-4 mr-2" />
                                        View Live
                                      </button>
                                      <button
                                        className="block w-full text-left px-3 py-2 text-sm hover:bg-gray-50 text-red-600"
                                        onClick={() => handlePostAction('delete', post)}
                                      >
                                        <Trash2 className="inline-block w-4 h-4 mr-2" />
                                        Delete Post
                                      </button>
                                    </div>
                                  </div>
                                </div>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>

                    {filteredPosts.length === 0 && (
                      <div className="text-center py-12">
                        <FileText className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                        <h3 className="text-lg font-medium text-gray-900 mb-2">No posts found</h3>
                        <p className="text-gray-500">Try adjusting your filters or search terms</p>
                      </div>
                    )}
                  </div>
                ) : (
                  // Card View for Posts
                  <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                    {filteredPosts.map((post) => (
                      <Card key={post.id} className="relative">
                        <CardHeader className="pb-3">
                          <div className="flex items-center justify-between">
                            <Badge className={getStatusColor(post.status, 'post')}>
                              {post.status}
                            </Badge>
                            <div className="flex items-center space-x-2">
                              {post.featured && (
                                <Star className="h-4 w-4 text-yellow-500" />
                              )}
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => {
                                  const newSelected = new Set(selectedPosts);
                                  if (newSelected.has(post.id)) {
                                    newSelected.delete(post.id);
                                  } else {
                                    newSelected.add(post.id);
                                  }
                                  setSelectedPosts(newSelected);
                                }}
                                className="p-1 h-auto"
                              >
                                {selectedPosts.has(post.id) ? (
                                  <CheckSquare className="h-4 w-4 text-blue-600" />
                                ) : (
                                  <Square className="h-4 w-4" />
                                )}
                              </Button>
                            </div>
                          </div>
                          <CardTitle className="text-base leading-tight">
                            {post.title}
                          </CardTitle>
                          <CardDescription className="text-sm">
                            {post.excerpt}
                          </CardDescription>
                        </CardHeader>
                        <CardContent className="pt-0 space-y-3">
                          <div className="flex items-center space-x-2">
                            <UserAvatar
                              user={{
                                id: post.author.id,
                                displayName: post.author.name,
                                username: post.author.name.toLowerCase().replace(' ', ''),
                                avatarUrl: post.author.avatar,
                                role: 'Author',
                                isVip: false,
                              }}
                              size="sm"
                              showStatus={false}
                              showRole={false}
                            />
                            <div className="flex-1">
                              <div className="text-sm font-medium">{post.author.name}</div>
                              <div className="text-xs text-gray-500">{post.category.name}</div>
                            </div>
                          </div>
                          
                          <div className="flex flex-wrap gap-1">
                            {post.tags.slice(0, 3).map(tag => (
                              <Badge key={tag.id} variant="outline" className="text-xs">
                                #{tag.name}
                              </Badge>
                            ))}
                            {post.tags.length > 3 && (
                              <Badge variant="outline" className="text-xs">
                                +{post.tags.length - 3} more
                              </Badge>
                            )}
                          </div>
                          
                          <div className="grid grid-cols-3 gap-2 text-xs text-center">
                            <div>
                              <div className="font-medium">{post.viewCount.toLocaleString()}</div>
                              <div className="text-gray-500">Views</div>
                            </div>
                            <div>
                              <div className="font-medium">{post.commentCount}</div>
                              <div className="text-gray-500">Comments</div>
                            </div>
                            <div>
                              <div className={`font-medium ${getSeoColor(post.seoScore)}`}>
                                {post.seoScore}/100
                              </div>
                              <div className="text-gray-500">SEO</div>
                            </div>
                          </div>
                          
                          <div className="text-xs text-gray-500">
                            Published: {formatDate(post.publishDate)}
                          </div>
                          
                          <div className="flex justify-center space-x-1 pt-2">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handlePostAction('preview', post)}
                              className="p-1 h-auto"
                              title="Preview"
                            >
                              <Eye className="h-4 w-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handlePostAction('edit', post)}
                              className="p-1 h-auto"
                              title="Edit"
                            >
                              <Edit className="h-4 w-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handlePostAction('toggleStatus', post)}
                              className="p-1 h-auto"
                              title={post.status === 'Published' ? 'Unpublish' : 'Publish'}
                            >
                              {post.status === 'Published' ? (
                                <EyeOff className="h-4 w-4" />
                              ) : (
                                <Globe className="h-4 w-4" />
                              )}
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handlePostAction('toggleFeatured', post)}
                              className="p-1 h-auto"
                              title={post.featured ? 'Remove from Featured' : 'Feature'}
                            >
                              {post.featured ? (
                                <StarOff className="h-4 w-4" />
                              ) : (
                                <Star className="h-4 w-4" />
                              )}
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handlePostAction('delete', post)}
                              className="p-1 h-auto text-red-500"
                              title="Delete"
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </div>
                        </CardContent>
                      </Card>
                    ))}

                    {filteredPosts.length === 0 && (
                      <div className="col-span-full text-center py-12">
                        <FileText className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                        <h3 className="text-lg font-medium text-gray-900 mb-2">No posts found</h3>
                        <p className="text-gray-500">Try adjusting your filters or search terms</p>
                      </div>
                    )}
                  </div>
                )}
              </TabsContent>

              {/* Comments Tab */}
              <TabsContent value="comments" className="mt-6">
                {viewMode === 'table' ? (
                  <div className="overflow-x-auto">
                    <table className="w-full table-auto">
                      <thead>
                        <tr className="border-b">
                          <th className="text-left py-2 px-2">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => {
                                if (selectedComments.size === filteredComments.length) {
                                  setSelectedComments(new Set());
                                } else {
                                  setSelectedComments(new Set(filteredComments.map(c => c.id)));
                                }
                              }}
                              className="p-0 h-auto"
                            >
                              {selectedComments.size === filteredComments.length && filteredComments.length > 0 ? (
                                <CheckSquare className="h-4 w-4" />
                              ) : (
                                <Square className="h-4 w-4" />
                              )}
                            </Button>
                          </th>
                          <th className="text-left py-2 px-4">Comment</th>
                          <th className="text-left py-2 px-4">Author</th>
                          <th className="text-left py-2 px-4">Post</th>
                          <th className="text-left py-2 px-4">Status</th>
                          <th className="text-left py-2 px-4">Date</th>
                          <th className="text-left py-2 px-4">Actions</th>
                        </tr>
                      </thead>
                      <tbody>
                        {filteredComments.map((comment) => (
                          <tr key={comment.id} className="border-b hover:bg-gray-50">
                            <td className="py-3 px-2">
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => {
                                  const newSelected = new Set(selectedComments);
                                  if (newSelected.has(comment.id)) {
                                    newSelected.delete(comment.id);
                                  } else {
                                    newSelected.add(comment.id);
                                  }
                                  setSelectedComments(newSelected);
                                }}
                                className="p-0 h-auto"
                              >
                                {selectedComments.has(comment.id) ? (
                                  <CheckSquare className="h-4 w-4 text-blue-600" />
                                ) : (
                                  <Square className="h-4 w-4" />
                                )}
                              </Button>
                            </td>
                            <td className="py-3 px-4 max-w-xs">
                              <div className="text-sm">
                                {comment.content.length > 100 
                                  ? `${comment.content.substring(0, 100)}...` 
                                  : comment.content
                                }
                              </div>
                              {comment.parentId && (
                                <div className="text-xs text-gray-500 mt-1 flex items-center">
                                  <Reply className="h-3 w-3 mr-1" />
                                  Reply to comment
                                </div>
                              )}
                            </td>
                            <td className="py-3 px-4">
                              <div className="flex items-center space-x-2">
                                <UserAvatar
                                  user={{
                                    id: comment.author.id,
                                    displayName: comment.author.name,
                                    username: comment.author.name.toLowerCase().replace(' ', ''),
                                    avatarUrl: comment.author.avatar,
                                    role: 'User',
                                    isVip: false,
                                  }}
                                  size="sm"
                                  showStatus={false}
                                  showRole={false}
                                />
                                <div>
                                  <div className="text-sm font-medium">{comment.author.name}</div>
                                  <div className="text-xs text-gray-500">{comment.author.email}</div>
                                </div>
                              </div>
                            </td>
                            <td className="py-3 px-4">
                              <div className="text-sm">
                                <div className="font-medium truncate max-w-xs">
                                  {comment.post.title}
                                </div>
                                <div className="text-xs text-gray-500">/{comment.post.slug}</div>
                              </div>
                            </td>
                            <td className="py-3 px-4">
                              <Badge className={getStatusColor(comment.status, 'comment')}>
                                {comment.status}
                              </Badge>
                            </td>
                            <td className="py-3 px-4">
                              <div className="text-sm">
                                {formatDateTime(comment.createdDate)}
                              </div>
                              <div className="text-xs text-gray-500">
                                {comment.ipAddress}
                              </div>
                            </td>
                            <td className="py-3 px-4">
                              <div className="flex items-center space-x-1">
                                {comment.status === 'Pending' && (
                                  <>
                                    <Button
                                      variant="ghost"
                                      size="sm"
                                      onClick={() => handleCommentAction('approve', comment)}
                                      className="p-1 h-auto"
                                      title="Approve"
                                    >
                                      <CheckCircle className="h-4 w-4 text-green-600" />
                                    </Button>
                                    <Button
                                      variant="ghost"
                                      size="sm"
                                      onClick={() => handleCommentAction('reject', comment)}
                                      className="p-1 h-auto"
                                      title="Reject"
                                    >
                                      <XCircle className="h-4 w-4 text-red-600" />
                                    </Button>
                                  </>
                                )}
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handleCommentAction('reply', comment)}
                                  className="p-1 h-auto"
                                  title="Reply"
                                >
                                  <Reply className="h-4 w-4" />
                                </Button>
                                <div className="relative group">
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    className="p-1 h-auto"
                                  >
                                    <MoreHorizontal className="h-4 w-4" />
                                  </Button>
                                  <div className="absolute right-0 top-8 w-48 bg-white border rounded-md shadow-lg hidden group-hover:block z-10">
                                    <div className="py-1">
                                      <button
                                        className="block w-full text-left px-3 py-2 text-sm hover:bg-gray-50"
                                        onClick={() => handleCommentAction('spam', comment)}
                                      >
                                        <Flag className="inline-block w-4 h-4 mr-2" />
                                        Mark as Spam
                                      </button>
                                      <button
                                        className="block w-full text-left px-3 py-2 text-sm hover:bg-gray-50"
                                        onClick={() => window.open(`/blog/${comment.post.slug}#comment-${comment.id}`, '_blank')}
                                      >
                                        <ExternalLink className="inline-block w-4 h-4 mr-2" />
                                        View in Context
                                      </button>
                                      <button
                                        className="block w-full text-left px-3 py-2 text-sm hover:bg-gray-50 text-red-600"
                                        onClick={() => handleCommentAction('delete', comment)}
                                      >
                                        <Trash2 className="inline-block w-4 h-4 mr-2" />
                                        Delete Comment
                                      </button>
                                    </div>
                                  </div>
                                </div>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>

                    {filteredComments.length === 0 && (
                      <div className="text-center py-12">
                        <MessageSquare className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                        <h3 className="text-lg font-medium text-gray-900 mb-2">No comments found</h3>
                        <p className="text-gray-500">Try adjusting your filters or search terms</p>
                      </div>
                    )}
                  </div>
                ) : (
                  // Card View for Comments
                  <div className="space-y-4">
                    {filteredComments.map((comment) => (
                      <Card key={comment.id} className="relative">
                        <CardHeader className="pb-3">
                          <div className="flex items-center justify-between">
                            <div className="flex items-center space-x-2">
                              <UserAvatar
                                user={{
                                  id: comment.author.id,
                                  displayName: comment.author.name,
                                  username: comment.author.name.toLowerCase().replace(' ', ''),
                                  avatarUrl: comment.author.avatar,
                                  role: 'User',
                                  isVip: false,
                                }}
                                size="sm"
                                showStatus={false}
                                showRole={false}
                              />
                              <div>
                                <div className="text-sm font-medium">{comment.author.name}</div>
                                <div className="text-xs text-gray-500">{formatDateTime(comment.createdDate)}</div>
                              </div>
                            </div>
                            <div className="flex items-center space-x-2">
                              <Badge className={getStatusColor(comment.status, 'comment')}>
                                {comment.status}
                              </Badge>
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => {
                                  const newSelected = new Set(selectedComments);
                                  if (newSelected.has(comment.id)) {
                                    newSelected.delete(comment.id);
                                  } else {
                                    newSelected.add(comment.id);
                                  }
                                  setSelectedComments(newSelected);
                                }}
                                className="p-1 h-auto"
                              >
                                {selectedComments.has(comment.id) ? (
                                  <CheckSquare className="h-4 w-4 text-blue-600" />
                                ) : (
                                  <Square className="h-4 w-4" />
                                )}
                              </Button>
                            </div>
                          </div>
                        </CardHeader>
                        <CardContent className="pt-0 space-y-3">
                          {comment.parentId && (
                            <div className="text-xs text-gray-500 flex items-center bg-gray-50 px-2 py-1 rounded">
                              <Reply className="h-3 w-3 mr-1" />
                              Reply to another comment
                            </div>
                          )}
                          
                          <div className="text-sm">
                            {comment.content}
                          </div>
                          
                          <div className="text-xs text-gray-500 border-t pt-2">
                            <div>On post: <span className="font-medium">{comment.post.title}</span></div>
                            <div>IP: {comment.ipAddress}</div>
                            <div>Email: {comment.author.email}</div>
                          </div>
                          
                          <div className="flex justify-center space-x-2 pt-2">
                            {comment.status === 'Pending' && (
                              <>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handleCommentAction('approve', comment)}
                                  className="text-green-600"
                                >
                                  <CheckCircle className="h-4 w-4 mr-1" />
                                  Approve
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handleCommentAction('reject', comment)}
                                  className="text-red-600"
                                >
                                  <XCircle className="h-4 w-4 mr-1" />
                                  Reject
                                </Button>
                              </>
                            )}
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleCommentAction('reply', comment)}
                            >
                              <Reply className="h-4 w-4 mr-1" />
                              Reply
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleCommentAction('spam', comment)}
                              className="text-orange-600"
                            >
                              <Flag className="h-4 w-4 mr-1" />
                              Spam
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleCommentAction('delete', comment)}
                              className="text-red-600"
                            >
                              <Trash2 className="h-4 w-4 mr-1" />
                              Delete
                            </Button>
                          </div>
                        </CardContent>
                      </Card>
                    ))}

                    {filteredComments.length === 0 && (
                      <div className="text-center py-12">
                        <MessageSquare className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                        <h3 className="text-lg font-medium text-gray-900 mb-2">No comments found</h3>
                        <p className="text-gray-500">Try adjusting your filters or search terms</p>
                      </div>
                    )}
                  </div>
                )}
              </TabsContent>
            </Tabs>
          </CardContent>
        </Card>

        {/* Preview Modal */}
        <Dialog open={showPreviewModal} onOpenChange={setShowPreviewModal}>
          <DialogContent className="max-w-4xl max-h-[80vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle>Post Preview</DialogTitle>
              <DialogDescription>
                Preview how this post will appear to readers
              </DialogDescription>
            </DialogHeader>
            {previewPost && (
              <div className="space-y-4">
                <div className="border-b pb-4">
                  <h1 className="text-2xl font-bold mb-2">{previewPost.title}</h1>
                  <div className="flex items-center space-x-4 text-sm text-gray-600">
                    <div className="flex items-center space-x-2">
                      <UserAvatar
                        user={{
                          id: previewPost.author.id,
                          displayName: previewPost.author.name,
                          username: previewPost.author.name.toLowerCase().replace(' ', ''),
                          avatarUrl: previewPost.author.avatar,
                          role: 'Author',
                          isVip: false,
                        }}
                        size="sm"
                        showStatus={false}
                        showRole={false}
                      />
                      <span>{previewPost.author.name}</span>
                    </div>
                    <span>•</span>
                    <span>{formatDate(previewPost.publishDate)}</span>
                    <span>•</span>
                    <Badge>{previewPost.category.name}</Badge>
                    {previewPost.featured && (
                      <>
                        <span>•</span>
                        <Star className="h-4 w-4 text-yellow-500" />
                        <span>Featured</span>
                      </>
                    )}
                  </div>
                </div>
                
                <div className="prose max-w-none">
                  <div className="text-lg text-gray-600 mb-4">{previewPost.excerpt}</div>
                  <div>{previewPost.content}</div>
                </div>
                
                <div className="border-t pt-4">
                  <div className="flex flex-wrap gap-2">
                    {previewPost.tags.map(tag => (
                      <Badge key={tag.id} variant="outline">
                        #{tag.name}
                      </Badge>
                    ))}
                  </div>
                </div>
                
                <div className="border-t pt-4 text-sm text-gray-600">
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                    <div>
                      <div className="font-medium">Views</div>
                      <div>{previewPost.viewCount.toLocaleString()}</div>
                    </div>
                    <div>
                      <div className="font-medium">Comments</div>
                      <div>{previewPost.commentCount}</div>
                    </div>
                    <div>
                      <div className="font-medium">SEO Score</div>
                      <div className={getSeoColor(previewPost.seoScore)}>
                        {previewPost.seoScore}/100
                      </div>
                    </div>
                    <div>
                      <div className="font-medium">Status</div>
                      <Badge className={getStatusColor(previewPost.status, 'post')}>
                        {previewPost.status}
                      </Badge>
                    </div>
                  </div>
                </div>
              </div>
            )}
            <DialogFooter>
              <Button variant="outline" onClick={() => setShowPreviewModal(false)}>
                Close
              </Button>
              {previewPost && (
                <Button onClick={() => handlePostAction('edit', previewPost)}>
                  Edit Post
                </Button>
              )}
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Reply to Comment Modal */}
        <Dialog open={showReplyModal} onOpenChange={setShowReplyModal}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Reply to Comment</DialogTitle>
              <DialogDescription>
                Write a reply to {replyingToComment?.author.name}&apos;s comment
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4">
              {replyingToComment && (
                <div className="p-3 bg-gray-50 rounded-lg border">
                  <p className="text-sm text-gray-600 mb-2">
                    <strong>{replyingToComment.author.name}:</strong>
                  </p>
                  <p className="text-sm">{replyingToComment.content}</p>
                </div>
              )}
              <div>
                <label className="block text-sm font-medium mb-2">Your Reply</label>
                <textarea
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  rows={4}
                  value={replyContent}
                  onChange={(e) => setReplyContent(e.target.value)}
                  placeholder="Enter your reply..."
                />
              </div>
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => setShowReplyModal(false)}>
                Cancel
              </Button>
              <Button
                onClick={async () => {
                  if (!replyingToComment || !replyContent.trim()) {
                    notifications.warning('Please enter a reply');
                    return;
                  }
                  try {
                    await mutations.replyToCommentMutation.mutateAsync({
                      id: replyingToComment.id,
                      content: replyContent.trim()
                    });
                    notifications.success('Reply sent successfully');
                    setShowReplyModal(false);
                    setReplyContent('');
                    setReplyingToComment(null);
                  } catch (error) {
                    notifications.error('Failed to send reply', handleAdminApiError(error));
                  }
                }}
                disabled={!replyContent.trim() || mutations.replyToCommentMutation.isPending}
              >
                {mutations.replyToCommentMutation.isPending ? 'Sending...' : 'Send Reply'}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Bulk Actions Modal */}
        <Dialog open={showBulkModal} onOpenChange={setShowBulkModal}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Bulk Actions</DialogTitle>
              <DialogDescription>
                Perform actions on {activeTab === 'posts' ? selectedPosts.size : selectedComments.size} selected {activeTab}.
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium mb-2">Action</label>
                <select 
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={bulkAction}
                  onChange={(e) => setBulkAction(e.target.value)}
                >
                  <option value="">Select an action</option>
                  {activeTab === 'posts' ? (
                    <>
                      <option value="publish">Publish Posts</option>
                      <option value="archive">Archive Posts</option>
                      <option value="feature">Feature Posts</option>
                      <option value="unfeature">Remove from Featured</option>
                      <option value="delete">Delete Posts</option>
                    </>
                  ) : (
                    <>
                      <option value="approve">Approve Comments</option>
                      <option value="reject">Reject Comments</option>
                      <option value="spam">Mark as Spam</option>
                      <option value="delete">Delete Comments</option>
                    </>
                  )}
                </select>
              </div>
              
              {bulkAction === 'delete' && (
                <Alert>
                  <AlertTriangle className="h-4 w-4" />
                  <AlertDescription>
                    This action cannot be undone. Selected {activeTab} will be permanently deleted.
                  </AlertDescription>
                </Alert>
              )}
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => setShowBulkModal(false)}>
                Cancel
              </Button>
              <Button
                variant={bulkAction === 'delete' ? 'destructive' : 'default'}
                onClick={handleBulkAction}
                disabled={!bulkAction || mutations.bulkPostOperationMutation.isPending || mutations.bulkCommentOperationMutation.isPending}
              >
                {mutations.bulkPostOperationMutation.isPending || mutations.bulkCommentOperationMutation.isPending
                  ? 'Processing...'
                  : 'Confirm Action'
                }
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Pagination Controls */}
        {(postsResponse?.totalPages || 0) > 1 && activeTab === 'posts' && (
          <Card className="mt-4">
            <CardContent className="pt-6">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-2">
                  <span className="text-sm text-gray-600">
                    Page {postsResponse?.page} of {postsResponse?.totalPages}
                  </span>
                  <select
                    className="px-2 py-1 border rounded text-sm"
                    value={postFilters.pageSize}
                    onChange={(e) => handlePostPageSizeChange(Number(e.target.value))}
                  >
                    {PAGE_SIZE_OPTIONS.map(size => (
                      <option key={size} value={size}>{size} per page</option>
                    ))}
                  </select>
                </div>
                <div className="flex items-center space-x-2">
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={!postsResponse?.hasPreviousPage || loading}
                    onClick={() => handlePostPageChange((postFilters.page || 1) - 1)}
                  >
                    Previous
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={!postsResponse?.hasNextPage || loading}
                    onClick={() => handlePostPageChange((postFilters.page || 1) + 1)}
                  >
                    Next
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>
        )}

        {(commentsResponse?.totalPages || 0) > 1 && activeTab === 'comments' && (
          <Card className="mt-4">
            <CardContent className="pt-6">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-2">
                  <span className="text-sm text-gray-600">
                    Page {commentsResponse?.page} of {commentsResponse?.totalPages}
                  </span>
                  <select
                    className="px-2 py-1 border rounded text-sm"
                    value={commentFilters.pageSize}
                    onChange={(e) => handleCommentPageSizeChange(Number(e.target.value))}
                  >
                    {PAGE_SIZE_OPTIONS.map(size => (
                      <option key={size} value={size}>{size} per page</option>
                    ))}
                  </select>
                </div>
                <div className="flex items-center space-x-2">
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={!commentsResponse?.hasPreviousPage || loading}
                    onClick={() => handleCommentPageChange((commentFilters.page || 1) - 1)}
                  >
                    Previous
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={!commentsResponse?.hasNextPage || loading}
                    onClick={() => handleCommentPageChange((commentFilters.page || 1) + 1)}
                  >
                    Next
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </ToastProvider>
  );
};

export default ContentManagement;
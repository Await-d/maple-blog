/**
 * ContentManagement - 内容管理页面
 * 提供文章和评论的全面管理功能，包括审核、编辑、删除等操作
 */

import React, { useState, useEffect, useMemo } from 'react';
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

// Data Types
interface AdminPost {
  id: string;
  title: string;
  slug: string;
  content: string;
  excerpt: string;
  status: 'Draft' | 'Published' | 'Archived';
  featured: boolean;
  author: {
    id: string;
    name: string;
    avatar?: string;
  };
  category: {
    id: string;
    name: string;
  };
  tags: Array<{
    id: string;
    name: string;
  }>;
  publishDate: string;
  lastModified: string;
  viewCount: number;
  commentCount: number;
  seoScore: number;
}

interface AdminComment {
  id: string;
  content: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Spam';
  author: {
    id: string;
    name: string;
    email: string;
    avatar?: string;
  };
  post: {
    id: string;
    title: string;
    slug: string;
  };
  parentId?: string;
  createdDate: string;
  ipAddress: string;
  userAgent: string;
}

interface ContentFilters {
  search: string;
  status: string;
  author: string;
  category: string;
  dateFrom: string;
  dateTo: string;
  featured?: boolean;
  tag?: string;
}

interface CommentFilters {
  search: string;
  status: string;
  author: string;
  post: string;
  dateFrom: string;
  dateTo: string;
}

// Generate mock data
const generateMockPosts = (): AdminPost[] => {
  const statuses: AdminPost['status'][] = ['Draft', 'Published', 'Archived'];
  const categories = [
    { id: '1', name: 'Technology' },
    { id: '2', name: 'Design' },
    { id: '3', name: 'Development' },
    { id: '4', name: 'Business' },
    { id: '5', name: 'Lifestyle' },
  ];
  const authors = [
    { id: '1', name: 'John Doe', avatar: 'https://i.pravatar.cc/100?img=1' },
    { id: '2', name: 'Jane Smith', avatar: 'https://i.pravatar.cc/100?img=2' },
    { id: '3', name: 'Mike Johnson', avatar: 'https://i.pravatar.cc/100?img=3' },
    { id: '4', name: 'Sarah Wilson', avatar: 'https://i.pravatar.cc/100?img=4' },
    { id: '5', name: 'Tom Brown', avatar: 'https://i.pravatar.cc/100?img=5' },
  ];
  const tags = [
    { id: '1', name: 'React' },
    { id: '2', name: 'JavaScript' },
    { id: '3', name: 'TypeScript' },
    { id: '4', name: 'CSS' },
    { id: '5', name: 'Node.js' },
    { id: '6', name: 'Design System' },
    { id: '7', name: 'UI/UX' },
    { id: '8', name: 'Backend' },
    { id: '9', name: 'Frontend' },
    { id: '10', name: 'Full Stack' },
  ];

  return Array.from({ length: 45 }, (_, index) => {
    const author = authors[Math.floor(Math.random() * authors.length)];
    const category = categories[Math.floor(Math.random() * categories.length)];
    const status = statuses[Math.floor(Math.random() * statuses.length)];
    const publishDate = new Date(2023 + Math.floor(Math.random() * 2), Math.floor(Math.random() * 12), Math.floor(Math.random() * 28));
    const lastModified = new Date(publishDate.getTime() + Math.random() * 30 * 24 * 60 * 60 * 1000);
    const postTags = tags.sort(() => 0.5 - Math.random()).slice(0, Math.floor(Math.random() * 4) + 1);
    
    return {
      id: `post-${index + 1}`,
      title: `Sample Blog Post ${index + 1}: Understanding Modern Web Development`,
      slug: `sample-blog-post-${index + 1}`,
      content: 'This is a comprehensive blog post about modern web development practices. It covers various topics including React, TypeScript, and modern development workflows. The content is rich and informative, providing valuable insights for developers at all levels.',
      excerpt: 'A brief overview of modern web development practices and technologies that every developer should know.',
      status,
      featured: Math.random() > 0.8,
      author,
      category,
      tags: postTags,
      publishDate: publishDate.toISOString(),
      lastModified: lastModified.toISOString(),
      viewCount: Math.floor(Math.random() * 5000) + 100,
      commentCount: Math.floor(Math.random() * 50),
      seoScore: Math.floor(Math.random() * 40) + 60,
    };
  });
};

const generateMockComments = (posts: AdminPost[]): AdminComment[] => {
  const statuses: AdminComment['status'][] = ['Pending', 'Approved', 'Rejected', 'Spam'];
  const commentAuthors = [
    { id: '1', name: 'Alice Johnson', email: 'alice@example.com', avatar: 'https://i.pravatar.cc/100?img=10' },
    { id: '2', name: 'Bob Smith', email: 'bob@example.com', avatar: 'https://i.pravatar.cc/100?img=11' },
    { id: '3', name: 'Carol White', email: 'carol@example.com', avatar: 'https://i.pravatar.cc/100?img=12' },
    { id: '4', name: 'David Brown', email: 'david@example.com', avatar: 'https://i.pravatar.cc/100?img=13' },
    { id: '5', name: 'Emma Davis', email: 'emma@example.com', avatar: 'https://i.pravatar.cc/100?img=14' },
  ];

  const commentTexts = [
    'Great article! Really helped me understand the concept better.',
    'Thanks for sharing this. Very informative and well-written.',
    'I have a question about the implementation details.',
    'This is exactly what I was looking for. Thank you!',
    'Could you please elaborate more on this topic?',
    'Excellent explanation. Keep up the good work!',
    'I disagree with some of the points mentioned here.',
    'This tutorial saved me hours of debugging. Much appreciated!',
    'Very detailed and comprehensive guide. Bookmarked!',
    'Looking forward to more content like this.',
  ];

  return Array.from({ length: 128 }, (_, index) => {
    const author = commentAuthors[Math.floor(Math.random() * commentAuthors.length)];
    const post = posts[Math.floor(Math.random() * posts.length)];
    const status = statuses[Math.floor(Math.random() * statuses.length)];
    const createdDate = new Date(Date.now() - Math.random() * 90 * 24 * 60 * 60 * 1000);
    const content = commentTexts[Math.floor(Math.random() * commentTexts.length)];
    
    return {
      id: `comment-${index + 1}`,
      content,
      status,
      author,
      post: {
        id: post.id,
        title: post.title,
        slug: post.slug,
      },
      parentId: Math.random() > 0.8 ? `comment-${Math.floor(Math.random() * index) + 1}` : undefined,
      createdDate: createdDate.toISOString(),
      ipAddress: `${Math.floor(Math.random() * 255)}.${Math.floor(Math.random() * 255)}.${Math.floor(Math.random() * 255)}.${Math.floor(Math.random() * 255)}`,
      userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
    };
  });
};

const ContentManagement: React.FC = () => {
  // State management
  const [posts, setPosts] = useState<AdminPost[]>([]);
  const [comments, setComments] = useState<AdminComment[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('posts');
  const [selectedPosts, setSelectedPosts] = useState<Set<string>>(new Set());
  const [selectedComments, setSelectedComments] = useState<Set<string>>(new Set());
  const [showPreviewModal, setShowPreviewModal] = useState(false);
  const [showBulkModal, setShowBulkModal] = useState(false);
  const [showConfirmModal, setShowConfirmModal] = useState(false);
  const [confirmActionState, setConfirmActionState] = useState<() => void>(() => {
    /* Default empty action */
  });
  const [confirmMessage, setConfirmMessage] = useState('');
  const [previewPost, setPreviewPost] = useState<AdminPost | null>(null);
  const [viewMode, setViewMode] = useState<'table' | 'card'>('table');

  // Filters
  const [postFilters, setPostFilters] = useState<ContentFilters>({
    search: '',
    status: '',
    author: '',
    category: '',
    dateFrom: '',
    dateTo: '',
    featured: undefined,
    tag: '',
  });

  const [commentFilters, setCommentFilters] = useState<CommentFilters>({
    search: '',
    status: '',
    author: '',
    post: '',
    dateFrom: '',
    dateTo: '',
  });

  const [showFilters, setShowFilters] = useState(false);
  const [bulkAction, setBulkAction] = useState('');

  // Load data
  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      try {
        await new Promise(resolve => setTimeout(resolve, 800));
        const mockPosts = generateMockPosts();
        const mockComments = generateMockComments(mockPosts);
        setPosts(mockPosts);
        setComments(mockComments);
      } catch (error) {
        console.error('Error loading content:', error);
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, []);

  // Filter posts
  const filteredPosts = useMemo(() => {
    let result = [...posts];

    if (postFilters.search) {
      const search = postFilters.search.toLowerCase();
      result = result.filter(post => 
        post.title.toLowerCase().includes(search) ||
        post.author.name.toLowerCase().includes(search) ||
        post.category.name.toLowerCase().includes(search) ||
        post.tags.some(tag => tag.name.toLowerCase().includes(search))
      );
    }

    if (postFilters.status) {
      result = result.filter(post => post.status === postFilters.status);
    }

    if (postFilters.author) {
      result = result.filter(post => post.author.name.includes(postFilters.author));
    }

    if (postFilters.category) {
      result = result.filter(post => post.category.name === postFilters.category);
    }

    if (postFilters.featured !== undefined) {
      result = result.filter(post => post.featured === postFilters.featured);
    }

    if (postFilters.dateFrom) {
      const fromDate = new Date(postFilters.dateFrom);
      result = result.filter(post => new Date(post.publishDate) >= fromDate);
    }

    if (postFilters.dateTo) {
      const toDate = new Date(postFilters.dateTo);
      result = result.filter(post => new Date(post.publishDate) <= toDate);
    }

    return result;
  }, [posts, postFilters]);

  // Filter comments
  const filteredComments = useMemo(() => {
    let result = [...comments];

    if (commentFilters.search) {
      const search = commentFilters.search.toLowerCase();
      result = result.filter(comment =>
        comment.content.toLowerCase().includes(search) ||
        comment.author.name.toLowerCase().includes(search) ||
        comment.post.title.toLowerCase().includes(search)
      );
    }

    if (commentFilters.status) {
      result = result.filter(comment => comment.status === commentFilters.status);
    }

    if (commentFilters.author) {
      result = result.filter(comment => comment.author.name.includes(commentFilters.author));
    }

    if (commentFilters.post) {
      result = result.filter(comment => comment.post.title.includes(commentFilters.post));
    }

    if (commentFilters.dateFrom) {
      const fromDate = new Date(commentFilters.dateFrom);
      result = result.filter(comment => new Date(comment.createdDate) >= fromDate);
    }

    if (commentFilters.dateTo) {
      const toDate = new Date(commentFilters.dateTo);
      result = result.filter(comment => new Date(comment.createdDate) <= toDate);
    }

    return result;
  }, [comments, commentFilters]);

  // Statistics
  const postStats = useMemo(() => {
    return {
      total: posts.length,
      published: posts.filter(p => p.status === 'Published').length,
      drafts: posts.filter(p => p.status === 'Draft').length,
      archived: posts.filter(p => p.status === 'Archived').length,
      featured: posts.filter(p => p.featured).length,
      totalViews: posts.reduce((sum, post) => sum + post.viewCount, 0),
      totalComments: posts.reduce((sum, post) => sum + post.commentCount, 0),
    };
  }, [posts]);

  const commentStats = useMemo(() => {
    return {
      total: comments.length,
      approved: comments.filter(c => c.status === 'Approved').length,
      pending: comments.filter(c => c.status === 'Pending').length,
      rejected: comments.filter(c => c.status === 'Rejected').length,
      spam: comments.filter(c => c.status === 'Spam').length,
    };
  }, [comments]);

  // Confirmation helper
  const showConfirm = (message: string, action: () => void) => {
    setConfirmMessage(message);
    setConfirmActionState(() => action);
    setShowConfirmModal(true);
  };

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

  // Action handlers
  const handlePostAction = async (action: string, post: AdminPost) => {
    // In production, this would make API calls

    switch (action) {
      case 'toggleStatus': {
        const newStatus = post.status === 'Published' ? 'Draft' : 'Published';
        setPosts(prev => prev.map(p =>
          p.id === post.id ? { ...p, status: newStatus } : p
        ));
        break;
      }
      case 'toggleFeatured':
        setPosts(prev => prev.map(p =>
          p.id === post.id ? { ...p, featured: !p.featured } : p
        ));
        break;
      case 'archive':
        setPosts(prev => prev.map(p =>
          p.id === post.id ? { ...p, status: 'Archived' as AdminPost['status'] } : p
        ));
        break;
      case 'delete':
        showConfirm(
          `Are you sure you want to delete "${post.title}"?`,
          () => setPosts(prev => prev.filter(p => p.id !== post.id))
        );
        break;
      case 'preview':
        setPreviewPost(post);
        setShowPreviewModal(true);
        break;
      case 'edit':
        // In production, this would navigate to the editor
        window.location.href = `/admin/posts/${post.id}/edit`;
        break;
      default:
        break;
    }
  };

  const handleCommentAction = async (action: string, comment: AdminComment) => {
    // In production, this would make API calls

    switch (action) {
      case 'approve':
        setComments(prev => prev.map(c =>
          c.id === comment.id ? { ...c, status: 'Approved' as AdminComment['status'] } : c
        ));
        break;
      case 'reject':
        setComments(prev => prev.map(c =>
          c.id === comment.id ? { ...c, status: 'Rejected' as AdminComment['status'] } : c
        ));
        break;
      case 'spam':
        setComments(prev => prev.map(c =>
          c.id === comment.id ? { ...c, status: 'Spam' as AdminComment['status'] } : c
        ));
        break;
      case 'delete':
        showConfirm(
          'Are you sure you want to delete this comment?',
          () => setComments(prev => prev.filter(c => c.id !== comment.id))
        );
        break;
      case 'reply':
        // In production, this would open a reply form
        setComments(prev => [...prev, {
          id: `reply-${Date.now()}`,
          content: 'Admin response to comment...',
          status: 'Approved' as AdminComment['status'],
          author: { id: 'admin', name: 'Admin', email: 'admin@example.com' },
          post: comment.post,
          parentId: comment.id,
          createdDate: new Date().toISOString(),
          ipAddress: '127.0.0.1',
          userAgent: 'Admin Panel',
        }]);
        break;
      default:
        break;
    }
  };

  const handleBulkAction = async () => {
    const selectedItems = activeTab === 'posts' ? selectedPosts : selectedComments;
    if (selectedItems.size === 0) return;

    // In production, this would make API calls
    
    if (activeTab === 'posts') {
      switch (bulkAction) {
        case 'delete':
          showConfirm(
            `Delete ${selectedItems.size} selected posts?`,
            () => {
              setPosts(prev => prev.filter(post => !selectedItems.has(post.id)));
              setSelectedPosts(new Set());
            }
          );
          break;
        case 'publish':
          setPosts(prev => prev.map(post => 
            selectedItems.has(post.id) ? { ...post, status: 'Published' as AdminPost['status'] } : post
          ));
          setSelectedPosts(new Set());
          break;
        case 'archive':
          setPosts(prev => prev.map(post => 
            selectedItems.has(post.id) ? { ...post, status: 'Archived' as AdminPost['status'] } : post
          ));
          setSelectedPosts(new Set());
          break;
        case 'feature':
          setPosts(prev => prev.map(post => 
            selectedItems.has(post.id) ? { ...post, featured: true } : post
          ));
          setSelectedPosts(new Set());
          break;
      }
    } else {
      switch (bulkAction) {
        case 'approve':
          setComments(prev => prev.map(comment => 
            selectedItems.has(comment.id) ? { ...comment, status: 'Approved' as AdminComment['status'] } : comment
          ));
          setSelectedComments(new Set());
          break;
        case 'reject':
          setComments(prev => prev.map(comment => 
            selectedItems.has(comment.id) ? { ...comment, status: 'Rejected' as AdminComment['status'] } : comment
          ));
          setSelectedComments(new Set());
          break;
        case 'delete':
          showConfirm(
            `Delete ${selectedItems.size} selected comments?`,
            () => {
              setComments(prev => prev.filter(comment => !selectedItems.has(comment.id)));
              setSelectedComments(new Set());
            }
          );
          break;
      }
    }
    
    setShowBulkModal(false);
  };

  const clearFilters = () => {
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
      });
    } else {
      setCommentFilters({
        search: '',
        status: '',
        author: '',
        post: '',
        dateFrom: '',
        dateTo: '',
      });
    }
  };

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
    <>
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
              <div className="text-2xl font-bold">{postStats.total}</div>
              <p className="text-xs text-muted-foreground">
                {postStats.published} published, {postStats.drafts} drafts
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Total Comments</CardTitle>
              <MessageSquare className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{commentStats.total}</div>
              <p className="text-xs text-muted-foreground">
                {commentStats.pending} pending review
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Total Views</CardTitle>
              <TrendingUp className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{postStats.totalViews.toLocaleString()}</div>
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
                {((commentStats.total / postStats.published || 0) * 100).toFixed(1)}%
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
                  Posts ({postStats.total})
                </TabsTrigger>
                <TabsTrigger value="comments">
                  Comments ({commentStats.total})
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
                        Showing {activeTab === 'posts' ? filteredPosts.length : filteredComments.length} of {activeTab === 'posts' ? posts.length : comments.length} {activeTab}
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
                disabled={!bulkAction}
              >
                Confirm Action
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Confirmation Modal */}
        <Dialog open={showConfirmModal} onOpenChange={setShowConfirmModal}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Confirm Action</DialogTitle>
              <DialogDescription>
                {confirmMessage}
              </DialogDescription>
            </DialogHeader>
            <DialogFooter>
              <Button variant="outline" onClick={() => setShowConfirmModal(false)}>
                Cancel
              </Button>
              <Button
                variant="destructive"
                onClick={() => {
                  confirmActionState();
                  setShowConfirmModal(false);
                }}
              >
                Confirm
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>
    </>
  );
};

export default ContentManagement;
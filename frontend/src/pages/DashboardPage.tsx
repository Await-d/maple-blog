/**
 * DashboardPage - 用户个人仪表板页面
 * 为认证用户提供个性化的仪表板体验，包含用户统计、活动动态和快速操作
 */

import React, { useEffect, useState } from 'react';
import { Helmet } from '@/components/common/DocumentHead';
import { useAuth } from '../hooks/useAuth';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/Button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import { Badge } from '../components/ui/badge';
import { LoadingSpinner } from '../components/ui/LoadingSpinner';
import { cn } from '../utils/cn';
import {
  User,
  FileText,
  MessageCircle,
  Eye,
  Heart,
  Plus,
  Edit,
  Settings,
  Activity,
  TrendingUp,
  Clock,
  Calendar
} from 'lucide-react';

// 用户统计数据类型
interface UserStats {
  postsCount: number;
  commentsCount: number;
  viewsCount: number;
  likesReceived: number;
  profileCompletion: number;
  joinDate: string;
  lastLoginDate: string;
}

// 活动项目类型
interface ActivityItem {
  id: string;
  type: 'post_created' | 'comment_made' | 'post_liked' | 'profile_updated';
  title: string;
  description: string;
  timestamp: string;
  relatedUrl?: string;
}

// 推荐文章类型
interface RecommendedPost {
  id: string;
  title: string;
  slug: string;
  excerpt: string;
  author: string;
  publishedAt: string;
  readTime: number;
  tags: string[];
}

// 页面属性接口
interface DashboardPageProps {
  className?: string;
}

export const DashboardPage: React.FC<DashboardPageProps> = ({ className }) => {
  const { user, isAuthenticated, loading: authLoading } = useAuth();
  const [stats, setStats] = useState<UserStats | null>(null);
  const [recentActivity, setRecentActivity] = useState<ActivityItem[]>([]);
  const [recommendations, setRecommendations] = useState<RecommendedPost[]>([]);
  const [loading, setLoading] = useState(true);

  // 模拟数据获取 - 在实际项目中应该从API获取
  useEffect(() => {
    const fetchDashboardData = async () => {
      if (!isAuthenticated || !user) return;

      try {
        setLoading(true);

        // 模拟API调用延迟
        await new Promise(resolve => setTimeout(resolve, 1000));

        // 模拟用户统计数据
        const userStats: UserStats = {
          postsCount: 12,
          commentsCount: 45,
          viewsCount: 1250,
          likesReceived: 89,
          profileCompletion: 85,
          joinDate: user.createdAt || '2024-01-15',
          lastLoginDate: new Date().toISOString()
        };

        // 模拟最近活动
        const activities: ActivityItem[] = [
          {
            id: '1',
            type: 'post_created',
            title: '发布了新文章',
            description: '《React 19 新特性详解》',
            timestamp: '2024-03-15T10:30:00Z',
            relatedUrl: '/blog/react-19-features'
          },
          {
            id: '2',
            type: 'comment_made',
            title: '发表了评论',
            description: '在《TypeScript 最佳实践》下评论',
            timestamp: '2024-03-14T16:45:00Z',
            relatedUrl: '/blog/typescript-best-practices'
          },
          {
            id: '3',
            type: 'profile_updated',
            title: '更新了个人资料',
            description: '添加了新的技能标签',
            timestamp: '2024-03-13T09:15:00Z',
            relatedUrl: '/profile'
          }
        ];

        // 模拟推荐文章
        const recommendedPosts: RecommendedPost[] = [
          {
            id: '1',
            title: '前端性能优化最佳实践',
            slug: 'frontend-performance-optimization',
            excerpt: '了解如何通过各种技术手段提升前端应用的性能表现...',
            author: 'Tech Writer',
            publishedAt: '2024-03-10T08:00:00Z',
            readTime: 8,
            tags: ['Performance', 'Frontend', 'Optimization']
          },
          {
            id: '2',
            title: 'Node.js 微服务架构设计',
            slug: 'nodejs-microservices-architecture',
            excerpt: '深入探讨如何使用Node.js构建可扩展的微服务架构...',
            author: 'Backend Expert',
            publishedAt: '2024-03-08T14:30:00Z',
            readTime: 12,
            tags: ['Node.js', 'Microservices', 'Architecture']
          }
        ];

        setStats(userStats);
        setRecentActivity(activities);
        setRecommendations(recommendedPosts);
      } catch (error) {
        console.error('Failed to load dashboard data:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
  }, [isAuthenticated, user]);

  // 格式化日期
  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('zh-CN', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  };

  // 格式化相对时间
  const formatRelativeTime = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffInHours = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60));

    if (diffInHours < 1) return '刚刚';
    if (diffInHours < 24) return `${diffInHours}小时前`;
    const diffInDays = Math.floor(diffInHours / 24);
    if (diffInDays < 7) return `${diffInDays}天前`;
    return formatDate(dateString);
  };

  // 获取活动图标
  const getActivityIcon = (type: ActivityItem['type']) => {
    switch (type) {
      case 'post_created':
        return <FileText className="h-4 w-4 text-blue-500" />;
      case 'comment_made':
        return <MessageCircle className="h-4 w-4 text-green-500" />;
      case 'post_liked':
        return <Heart className="h-4 w-4 text-red-500" />;
      case 'profile_updated':
        return <User className="h-4 w-4 text-purple-500" />;
      default:
        return <Activity className="h-4 w-4 text-gray-500" />;
    }
  };

  if (authLoading) {
    return <LoadingSpinner />;
  }

  if (!isAuthenticated || !user) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
            需要登录
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mb-6">
            请先登录以访问您的个人仪表板
          </p>
          <Button onClick={() => window.location.href = '/login'}>
            前往登录
          </Button>
        </div>
      </div>
    );
  }

  return (
    <>
      {/* SEO 元数据 */}
      <Helmet>
        <title>仪表板 - Maple Blog</title>
        <meta name="description" content="个人仪表板，查看您的文章统计、活动动态和个性化推荐内容。" />
        <meta name="robots" content="noindex, nofollow" />
      </Helmet>

      <div className={cn('min-h-screen bg-gray-50 dark:bg-gray-950', className)}>
        <div className="container-responsive py-8">
          <div className="max-w-7xl mx-auto">
            {/* 页面标题 */}
            <div className="mb-8">
              <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
                欢迎回来，{user.displayName}！
              </h1>
              <p className="text-lg text-gray-600 dark:text-gray-400">
                这里是您的个人仪表板，查看最新动态和管理您的内容
              </p>
            </div>

            {loading ? (
              <div className="flex items-center justify-center py-12">
                <LoadingSpinner />
              </div>
            ) : (
              <Tabs defaultValue="overview" className="space-y-6">
                <TabsList className="grid w-full grid-cols-3">
                  <TabsTrigger value="overview">概览</TabsTrigger>
                  <TabsTrigger value="activity">动态</TabsTrigger>
                  <TabsTrigger value="recommendations">推荐</TabsTrigger>
                </TabsList>

                {/* 概览标签页 */}
                <TabsContent value="overview" className="space-y-6">
                  {/* 统计卡片 */}
                  {stats && (
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                      <Card>
                        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                          <CardTitle className="text-sm font-medium">文章数量</CardTitle>
                          <FileText className="h-4 w-4 text-muted-foreground" />
                        </CardHeader>
                        <CardContent>
                          <div className="text-2xl font-bold">{stats.postsCount}</div>
                          <p className="text-xs text-muted-foreground">
                            累计发布文章
                          </p>
                        </CardContent>
                      </Card>

                      <Card>
                        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                          <CardTitle className="text-sm font-medium">评论数量</CardTitle>
                          <MessageCircle className="h-4 w-4 text-muted-foreground" />
                        </CardHeader>
                        <CardContent>
                          <div className="text-2xl font-bold">{stats.commentsCount}</div>
                          <p className="text-xs text-muted-foreground">
                            累计发表评论
                          </p>
                        </CardContent>
                      </Card>

                      <Card>
                        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                          <CardTitle className="text-sm font-medium">总浏览量</CardTitle>
                          <Eye className="h-4 w-4 text-muted-foreground" />
                        </CardHeader>
                        <CardContent>
                          <div className="text-2xl font-bold">{stats.viewsCount.toLocaleString()}</div>
                          <p className="text-xs text-muted-foreground">
                            文章总浏览次数
                          </p>
                        </CardContent>
                      </Card>

                      <Card>
                        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                          <CardTitle className="text-sm font-medium">获得点赞</CardTitle>
                          <Heart className="h-4 w-4 text-muted-foreground" />
                        </CardHeader>
                        <CardContent>
                          <div className="text-2xl font-bold">{stats.likesReceived}</div>
                          <p className="text-xs text-muted-foreground">
                            累计获得点赞
                          </p>
                        </CardContent>
                      </Card>
                    </div>
                  )}

                  {/* 快速操作 */}
                  <Card>
                    <CardHeader>
                      <CardTitle>快速操作</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                        <Button
                          className="h-20 flex flex-col items-center justify-center space-y-2"
                          onClick={() => window.location.href = '/blog/new'}
                        >
                          <Plus className="h-6 w-6" />
                          <span>写新文章</span>
                        </Button>
                        <Button
                          variant="outline"
                          className="h-20 flex flex-col items-center justify-center space-y-2"
                          onClick={() => window.location.href = '/profile'}
                        >
                          <Edit className="h-6 w-6" />
                          <span>编辑资料</span>
                        </Button>
                        <Button
                          variant="outline"
                          className="h-20 flex flex-col items-center justify-center space-y-2"
                          onClick={() => window.location.href = '/blog?author=' + user.userName}
                        >
                          <FileText className="h-6 w-6" />
                          <span>我的文章</span>
                        </Button>
                      </div>
                    </CardContent>
                  </Card>

                  {/* 账户信息 */}
                  {stats && (
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                      <Card>
                        <CardHeader>
                          <CardTitle>账户信息</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                          <div className="flex items-center justify-between">
                            <span className="text-sm font-medium">资料完成度</span>
                            <div className="flex items-center space-x-2">
                              <div className="w-20 h-2 bg-gray-200 rounded-full">
                                <div
                                  className="h-2 bg-green-500 rounded-full"
                                  style={{ width: `${stats.profileCompletion}%` }}
                                />
                              </div>
                              <span className="text-sm text-gray-600">{stats.profileCompletion}%</span>
                            </div>
                          </div>
                          <div className="flex items-center justify-between">
                            <span className="text-sm font-medium">加入时间</span>
                            <div className="flex items-center space-x-2">
                              <Calendar className="h-4 w-4 text-gray-400" />
                              <span className="text-sm text-gray-600">{formatDate(stats.joinDate)}</span>
                            </div>
                          </div>
                          <div className="flex items-center justify-between">
                            <span className="text-sm font-medium">最后登录</span>
                            <div className="flex items-center space-x-2">
                              <Clock className="h-4 w-4 text-gray-400" />
                              <span className="text-sm text-gray-600">{formatRelativeTime(stats.lastLoginDate)}</span>
                            </div>
                          </div>
                        </CardContent>
                      </Card>

                      <Card>
                        <CardHeader>
                          <CardTitle>设置快捷方式</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-3">
                          <Button
                            variant="ghost"
                            className="w-full justify-start"
                            onClick={() => window.location.href = '/profile'}
                          >
                            <User className="h-4 w-4 mr-2" />
                            个人资料设置
                          </Button>
                          <Button
                            variant="ghost"
                            className="w-full justify-start"
                            onClick={() => window.location.href = '/settings'}
                          >
                            <Settings className="h-4 w-4 mr-2" />
                            账户设置
                          </Button>
                          <Button
                            variant="ghost"
                            className="w-full justify-start"
                            onClick={() => window.location.href = '/analytics'}
                          >
                            <TrendingUp className="h-4 w-4 mr-2" />
                            数据统计
                          </Button>
                        </CardContent>
                      </Card>
                    </div>
                  )}
                </TabsContent>

                {/* 动态标签页 */}
                <TabsContent value="activity" className="space-y-6">
                  <Card>
                    <CardHeader>
                      <CardTitle>最近动态</CardTitle>
                    </CardHeader>
                    <CardContent>
                      {recentActivity.length > 0 ? (
                        <div className="space-y-4">
                          {recentActivity.map((activity) => (
                            <div key={activity.id} className="flex items-start space-x-3 p-3 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
                              <div className="flex-shrink-0 mt-1">
                                {getActivityIcon(activity.type)}
                              </div>
                              <div className="flex-1 min-w-0">
                                <p className="text-sm font-medium text-gray-900 dark:text-white">
                                  {activity.title}
                                </p>
                                <p className="text-sm text-gray-600 dark:text-gray-400">
                                  {activity.description}
                                </p>
                                <p className="text-xs text-gray-500 mt-1">
                                  {formatRelativeTime(activity.timestamp)}
                                </p>
                              </div>
                              {activity.relatedUrl && (
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => window.location.href = activity.relatedUrl!}
                                >
                                  查看
                                </Button>
                              )}
                            </div>
                          ))}
                        </div>
                      ) : (
                        <div className="text-center py-8">
                          <Activity className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                          <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">暂无动态</h3>
                          <p className="text-gray-500">您的活动动态将在这里显示</p>
                        </div>
                      )}
                    </CardContent>
                  </Card>
                </TabsContent>

                {/* 推荐标签页 */}
                <TabsContent value="recommendations" className="space-y-6">
                  <Card>
                    <CardHeader>
                      <CardTitle>为您推荐</CardTitle>
                    </CardHeader>
                    <CardContent>
                      {recommendations.length > 0 ? (
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                          {recommendations.map((post) => (
                            <div key={post.id} className="border rounded-lg p-4 hover:shadow-md transition-shadow">
                              <h3 className="font-semibold text-lg mb-2 line-clamp-2">
                                {post.title}
                              </h3>
                              <p className="text-gray-600 dark:text-gray-400 text-sm mb-3 line-clamp-3">
                                {post.excerpt}
                              </p>
                              <div className="flex items-center justify-between text-xs text-gray-500 mb-3">
                                <span>作者：{post.author}</span>
                                <span>阅读时间：{post.readTime}分钟</span>
                              </div>
                              <div className="flex items-center justify-between">
                                <div className="flex flex-wrap gap-1">
                                  {post.tags.slice(0, 3).map((tag) => (
                                    <Badge key={tag} variant="secondary" className="text-xs">
                                      {tag}
                                    </Badge>
                                  ))}
                                </div>
                                <Button
                                  size="sm"
                                  onClick={() => window.location.href = `/blog/${post.slug}`}
                                >
                                  阅读
                                </Button>
                              </div>
                            </div>
                          ))}
                        </div>
                      ) : (
                        <div className="text-center py-8">
                          <FileText className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                          <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">暂无推荐</h3>
                          <p className="text-gray-500">个性化推荐内容将在这里显示</p>
                        </div>
                      )}
                    </CardContent>
                  </Card>
                </TabsContent>
              </Tabs>
            )}
          </div>
        </div>
      </div>
    </>
  );
};

export default DashboardPage;
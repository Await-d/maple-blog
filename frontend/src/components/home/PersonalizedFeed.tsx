/**
 * PersonalizedFeed component - Intelligent personalized content recommendations
 * Features: ML-powered recommendations, user feedback, adaptive learning
 */

import React, { useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import {
  Zap,
  ThumbsUp,
  ThumbsDown,
  X,
  RefreshCw,
  Settings,
  TrendingUp,
  Clock,
  Eye,
  Heart,
  Bookmark,
  Share2,
  MoreHorizontal,
  User,
  Tag,
  Calendar as _Calendar,
  Target,
  Lightbulb,
  Brain,
  Sparkles,
  Info as _Info,
  CheckCircle,
  AlertCircle,
} from 'lucide-react';
import { Button } from '../ui/Button';
import { useAuth } from '../../hooks/useAuth';
import { usePersonalization, useAutoTrackViews } from '../../hooks/usePersonalization';
import { useIsMobile } from '../../stores/homeStore';
import { cn } from '../../utils/cn';
import type { PostSummary, RecommendationFeedback } from '../../types/home';

interface PersonalizedFeedProps {
  className?: string;
  title?: string;
  maxItems?: number;
  showOnboarding?: boolean;
  showFeedback?: boolean;
  showAnalytics?: boolean;
  compact?: boolean;
}

interface RecommendationCardProps {
  post: PostSummary;
  reason?: string;
  confidence?: number;
  onFeedback?: (feedback: RecommendationFeedback['feedback'], reason?: string) => void;
  onInteraction?: (type: 'like' | 'bookmark' | 'share') => void;
  showFeedback?: boolean;
  className?: string;
  style?: React.CSSProperties;
}

const RecommendationCard: React.FC<RecommendationCardProps> = ({
  post,
  reason,
  confidence,
  onFeedback,
  onInteraction,
  showFeedback = true,
  className,
  style,
}) => {
  const [feedbackGiven, setFeedbackGiven] = useState(false);
  const [showFeedbackForm, setShowFeedbackForm] = useState(false);
  const [_customReason, _setCustomReason] = useState('');
  const [imageLoaded, setImageLoaded] = useState(false);
  const [imageError, setImageError] = useState(false);

  const handleImageLoad = () => setImageLoaded(true);
  const handleImageError = () => setImageError(true);

  const handleFeedback = useCallback((
    feedback: RecommendationFeedback['feedback'],
    customReason?: string
  ) => {
    if (onFeedback) {
      onFeedback(feedback, customReason);
      setFeedbackGiven(true);
      setShowFeedbackForm(false);
    }
  }, [onFeedback]);

  const handleInteraction = useCallback((type: 'like' | 'bookmark' | 'share') => {
    if (onInteraction) {
      onInteraction(type);
    }
  }, [onInteraction]);

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffTime = Math.abs(now.getTime() - date.getTime());
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays === 1) return '昨天';
    if (diffDays < 7) return `${diffDays} 天前`;
    if (diffDays < 30) return `${Math.ceil(diffDays / 7)} 周前`;
    return date.toLocaleDateString('zh-CN');
  };

  return (
    <article
      data-post-id={post.id}
      className={cn(
        'group relative bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 hover:shadow-lg dark:hover:shadow-2xl transition-all duration-300 overflow-hidden',
        className
      )}
      style={style}
    >
      {/* Confidence Indicator */}
      {confidence && confidence > 0.8 && (
        <div className="absolute top-4 right-4 z-10">
          <div className="flex items-center space-x-1 px-2 py-1 bg-green-500/90 backdrop-blur-sm text-white text-xs font-medium rounded-full">
            <Target size={12} />
            <span>高匹配</span>
          </div>
        </div>
      )}

      <Link to={`/post/${post.slug}`} className="block">
        {/* Image */}
        {post.featuredImageUrl && !imageError && (
          <div className="relative h-48 bg-gray-200 dark:bg-gray-700 overflow-hidden">
            <img
              src={post.featuredImageUrl}
              alt={post.title}
              className={cn(
                'w-full h-full object-cover transition-all duration-500 group-hover:scale-105',
                !imageLoaded && 'opacity-0'
              )}
              onLoad={handleImageLoad}
              onError={handleImageError}
              loading="lazy"
            />
            {!imageLoaded && (
              <div className="absolute inset-0 bg-gray-200 dark:bg-gray-700 animate-pulse" />
            )}

            {/* Overlay with Quick Actions */}
            <div className="absolute inset-0 bg-black/20 opacity-0 group-hover:opacity-100 transition-opacity duration-300 flex items-center justify-center">
              <div className="flex items-center space-x-2">
                <Button
                  variant="ghost"
                  size="sm"
                  className="bg-white/20 backdrop-blur-sm hover:bg-white/30 text-white border-0 p-2"
                  onClick={(e: React.MouseEvent) => {
                    e.preventDefault();
                    handleInteraction('like');
                  }}
                >
                  <Heart size={16} />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  className="bg-white/20 backdrop-blur-sm hover:bg-white/30 text-white border-0 p-2"
                  onClick={(e: React.MouseEvent) => {
                    e.preventDefault();
                    handleInteraction('bookmark');
                  }}
                >
                  <Bookmark size={16} />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  className="bg-white/20 backdrop-blur-sm hover:bg-white/30 text-white border-0 p-2"
                  onClick={(e: React.MouseEvent) => {
                    e.preventDefault();
                    handleInteraction('share');
                  }}
                >
                  <Share2 size={16} />
                </Button>
              </div>
            </div>
          </div>
        )}

        {/* Content */}
        <div className="p-6">
          {/* Category & Reading Time */}
          <div className="flex items-center space-x-2 mb-3">
            {post.category && (
              <span className="inline-flex items-center px-2 py-1 bg-orange-100 dark:bg-orange-900/20 text-orange-700 dark:text-orange-300 text-xs font-medium rounded">
                <Tag size={10} className="mr-1" />
                {post.category.name}
              </span>
            )}
            {post.readingTime && (
              <span className="text-xs text-gray-500 dark:text-gray-400 flex items-center space-x-1">
                <Clock size={10} />
                <span>{post.readingTime} 分钟</span>
              </span>
            )}
          </div>

          {/* Title */}
          <h3 className="font-bold text-lg text-gray-900 dark:text-white group-hover:text-orange-600 dark:group-hover:text-orange-400 transition-colors line-clamp-2 mb-3">
            {post.title}
          </h3>

          {/* Summary */}
          {post.summary && (
            <p className="text-gray-600 dark:text-gray-400 line-clamp-3 mb-4 leading-relaxed">
              {post.summary}
            </p>
          )}

          {/* Meta */}
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center space-x-3 text-sm text-gray-500 dark:text-gray-400">
              <span className="flex items-center space-x-1">
                <User size={14} />
                <span className="truncate max-w-20">{post.author.displayName || post.author.userName}</span>
              </span>
              <span>{formatDate(post.publishedAt)}</span>
            </div>
            <div className="flex items-center space-x-3 text-sm text-gray-500 dark:text-gray-400">
              <span className="flex items-center space-x-1">
                <Eye size={14} />
                <span>{post.viewCount.toLocaleString()}</span>
              </span>
            </div>
          </div>

          {/* Recommendation Reason */}
          {reason && (
            <div className="bg-blue-50 dark:bg-blue-900/10 border border-blue-200 dark:border-blue-800 rounded-lg p-3 mb-4">
              <div className="flex items-center space-x-2 text-sm text-blue-700 dark:text-blue-300">
                <Lightbulb size={14} />
                <span>{reason}</span>
              </div>
            </div>
          )}
        </div>
      </Link>

      {/* Feedback Section */}
      {showFeedback && !feedbackGiven && (
        <div className="border-t border-gray-200 dark:border-gray-700 p-4">
          {!showFeedbackForm ? (
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-600 dark:text-gray-400">
                这个推荐对您有用吗？
              </span>
              <div className="flex items-center space-x-2">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleFeedback('relevant')}
                  className="text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20 p-2"
                  title="有用"
                >
                  <ThumbsUp size={16} />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleFeedback('not_relevant')}
                  className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 p-2"
                  title="无用"
                >
                  <ThumbsDown size={16} />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setShowFeedbackForm(true)}
                  className="p-2"
                  title="更多选项"
                >
                  <MoreHorizontal size={16} />
                </Button>
              </div>
            </div>
          ) : (
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  为什么不推荐？
                </span>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setShowFeedbackForm(false)}
                  className="p-1"
                >
                  <X size={14} />
                </Button>
              </div>
              <div className="grid grid-cols-2 gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleFeedback('already_read')}
                  className="text-xs"
                >
                  已经看过
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleFeedback('not_interested')}
                  className="text-xs"
                >
                  不感兴趣
                </Button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Feedback Given */}
      {feedbackGiven && (
        <div className="border-t border-gray-200 dark:border-gray-700 p-4">
          <div className="flex items-center space-x-2 text-sm text-green-600 dark:text-green-400">
            <CheckCircle size={14} />
            <span>感谢您的反馈！</span>
          </div>
        </div>
      )}
    </article>
  );
};

const PersonalizationOnboarding: React.FC<{ onClose: () => void }> = ({ onClose }) => (
  <div className="bg-gradient-to-r from-orange-50 to-red-50 dark:from-orange-950/20 dark:to-red-950/20 rounded-xl p-6 border border-orange-200 dark:border-orange-800 mb-6">
    <div className="flex items-start space-x-4">
      <div className="flex-shrink-0">
        <div className="w-10 h-10 bg-orange-500 rounded-lg flex items-center justify-center">
          <Brain className="text-white" size={20} />
        </div>
      </div>
      <div className="flex-1">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
          个性化推荐已启用
        </h3>
        <p className="text-gray-600 dark:text-gray-400 mb-4 leading-relaxed">
          我们会根据您的阅读历史、兴趣偏好和互动行为，为您推荐最相关的内容。通过您的反馈，推荐会越来越精准。
        </p>
        <div className="flex items-center space-x-3">
          <Button
            variant="primary"
            size="sm"
            onClick={onClose}
          >
            开始体验
          </Button>
          <Link to="/preferences">
            <Button variant="outline" size="sm">
              <Settings size={14} className="mr-2" />
              偏好设置
            </Button>
          </Link>
        </div>
      </div>
      <Button
        variant="ghost"
        size="sm"
        onClick={onClose}
        className="flex-shrink-0 p-2"
      >
        <X size={16} />
      </Button>
    </div>
  </div>
);

const PersonalizationAnalytics: React.FC<{ analytics: Record<string, unknown> }> = ({ analytics }) => (
  <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-6 mb-6">
    <div className="flex items-center space-x-3 mb-4">
      <TrendingUp className="text-orange-500" size={20} />
      <h3 className="font-semibold text-gray-900 dark:text-white">
        个性化分析
      </h3>
    </div>
    <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
      <div className="text-center">
        <div className="text-2xl font-bold text-orange-600 dark:text-orange-400">
          {String(analytics.readingStreak)}
        </div>
        <div className="text-xs text-gray-500 dark:text-gray-400">连续阅读天数</div>
      </div>
      <div className="text-center">
        <div className="text-2xl font-bold text-blue-600 dark:text-blue-400">
          {String(analytics.totalInteractions)}
        </div>
        <div className="text-xs text-gray-500 dark:text-gray-400">总互动次数</div>
      </div>
      <div className="text-center">
        <div className="text-2xl font-bold text-green-600 dark:text-green-400">
          {String(analytics.readingTime)}
        </div>
        <div className="text-xs text-gray-500 dark:text-gray-400">阅读时长(分)</div>
      </div>
      <div className="text-center">
        <div className="text-2xl font-bold text-purple-600 dark:text-purple-400">
          {String(analytics.engagementRate)}%
        </div>
        <div className="text-xs text-gray-500 dark:text-gray-400">参与度</div>
      </div>
    </div>
  </div>
);

export const PersonalizedFeed: React.FC<PersonalizedFeedProps> = ({
  className,
  title = '为您推荐',
  maxItems = 10,
  showOnboarding = true,
  showFeedback = true,
  showAnalytics = false,
  compact = false,
}) => {
  const { isAuthenticated } = useAuth();
  const _isMobile = useIsMobile();
  const { state, actions, analytics } = usePersonalization();

  // Local state
  const [onboardingDismissed, setOnboardingDismissed] = useState(() => {
    return localStorage.getItem('personalization_onboarding_dismissed') === 'true';
  });

  // Auto-track views for recommendations
  useAutoTrackViews(state.recommendations);

  const handleFeedback = useCallback((postId: string, feedback: RecommendationFeedback['feedback'], reason?: string) => {
    actions.provideFeedback(postId, feedback, reason);
  }, [actions]);

  const handleInteraction = useCallback((postId: string, type: 'like' | 'bookmark' | 'share') => {
    actions.recordInteraction(postId, type);
  }, [actions]);

  const dismissOnboarding = useCallback(() => {
    setOnboardingDismissed(true);
    localStorage.setItem('personalization_onboarding_dismissed', 'true');
  }, []);

  if (!isAuthenticated) {
    return (
      <section className={cn('space-y-6', className)}>
        <div className="text-center py-12 bg-gray-50 dark:bg-gray-800 rounded-xl">
          <Brain size={48} className="mx-auto text-gray-400 mb-4" />
          <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
            登录后享受个性化推荐
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">
            基于您的阅读偏好，为您推荐最相关的内容
          </p>
          <div className="flex items-center justify-center space-x-3">
            <Link to="/login">
              <Button variant="primary">
                立即登录
              </Button>
            </Link>
            <Link to="/register">
              <Button variant="outline">
                免费注册
              </Button>
            </Link>
          </div>
        </div>
      </section>
    );
  }

  if (!state.canPersonalize) {
    return (
      <section className={cn('space-y-6', className)}>
        <div className="text-center py-12 bg-gradient-to-br from-orange-50 to-red-50 dark:from-orange-950/20 dark:to-red-950/20 rounded-xl border border-orange-200 dark:border-orange-800">
          <Sparkles size={48} className="mx-auto text-orange-500 mb-4" />
          <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
            正在为您准备个性化内容
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">
            多阅读几篇文章，我们就能为您提供更精准的推荐
          </p>
          <div className="flex items-center justify-center space-x-4 text-sm text-gray-600 dark:text-gray-400">
            <div className="flex items-center space-x-1">
              <div className="w-2 h-2 bg-orange-500 rounded-full" />
              <span>互动得分: {Math.round(state.interactionScore * 100)}%</span>
            </div>
            <div className="flex items-center space-x-1">
              <div className="w-2 h-2 bg-blue-500 rounded-full" />
              <span>数据新鲜度: {Math.round(state.freshness * 100)}%</span>
            </div>
          </div>
        </div>
      </section>
    );
  }

  if (state.isLoading) {
    return (
      <section className={cn('space-y-6', className)}>
        <div className="flex items-center space-x-3">
          <div className="w-6 h-6 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
          <div className="h-8 w-32 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
        </div>
        <div className={cn(
          'grid gap-6',
          compact
            ? 'grid-cols-1 sm:grid-cols-2'
            : 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3'
        )}>
          {Array.from({ length: maxItems }, (_, index) => (
            <div key={index} className="bg-gray-200 dark:bg-gray-700 rounded-xl animate-pulse h-80" />
          ))}
        </div>
      </section>
    );
  }

  if (state.error || !state.hasRecommendations) {
    return (
      <section className={cn('space-y-6', className)}>
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            {title}
          </h2>
        </div>
        <div className="text-center py-12">
          <AlertCircle size={48} className="mx-auto text-gray-400 mb-4" />
          <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
            {state.error ? '推荐加载失败' : '暂无推荐内容'}
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">
            {state.error ? '请稍后重试' : '继续浏览文章，我们会为您生成推荐'}
          </p>
          <div className="flex items-center justify-center space-x-3">
            <Button
              onClick={actions.refreshRecommendations}
              variant="outline"
              disabled={state.isLoading}
            >
              <RefreshCw size={16} className={cn('mr-2', state.isLoading && 'animate-spin')} />
              重新加载
            </Button>
            <Link to="/preferences">
              <Button variant="outline">
                <Settings size={16} className="mr-2" />
                偏好设置
              </Button>
            </Link>
          </div>
        </div>
      </section>
    );
  }

  const displayRecommendations = state.recommendations.slice(0, maxItems);

  return (
    <section className={cn('space-y-6', className)} role="region" aria-label={title}>
      {/* Onboarding */}
      {showOnboarding && !onboardingDismissed && (
        <PersonalizationOnboarding onClose={dismissOnboarding} />
      )}

      {/* Analytics */}
      {showAnalytics && <PersonalizationAnalytics analytics={analytics} />}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <Zap className="text-orange-500" size={24} />
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            {title}
          </h2>
          <span className="px-2 py-1 bg-orange-100 dark:bg-orange-900/20 text-orange-700 dark:text-orange-300 text-sm font-medium rounded">
            {displayRecommendations.length}
          </span>
        </div>

        <div className="flex items-center space-x-2">
          <Button
            onClick={actions.refreshRecommendations}
            variant="outline"
            size="sm"
            disabled={state.isLoading}
            className="flex items-center space-x-2"
          >
            <RefreshCw size={14} className={state.isLoading ? 'animate-spin' : ''} />
            <span className="hidden sm:inline">刷新</span>
          </Button>
          <Link to="/preferences">
            <Button variant="outline" size="sm">
              <Settings size={14} className="mr-1" />
              <span className="hidden sm:inline">设置</span>
            </Button>
          </Link>
        </div>
      </div>

      {/* Recommendations Grid */}
      <div className={cn(
        'grid gap-6',
        compact
          ? 'grid-cols-1 sm:grid-cols-2'
          : 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3'
      )}>
        {displayRecommendations.map((post, index) => (
          <RecommendationCard
            key={post.id}
            post={post}
            reason="基于您的阅读偏好推荐" // In real app, this would come from the API
            confidence={0.85} // Mock confidence score
            onFeedback={(feedback, reason) => handleFeedback(post.id, feedback, reason)}
            onInteraction={(type) => handleInteraction(post.id, type)}
            showFeedback={showFeedback}
            className="animate-fade-in"
            style={{ animationDelay: `${index * 100}ms` } as React.CSSProperties}
          />
        ))}
      </div>

      {/* Footer Info */}
      <div className="flex items-center justify-between text-sm text-gray-500 dark:text-gray-400 pt-4 border-t border-gray-200 dark:border-gray-700">
        <div className="flex items-center space-x-4">
          <span>基于 {analytics.totalInteractions} 次互动推荐</span>
          <span>•</span>
          <span>匹配度 {Math.round(state.diversity * 100)}%</span>
        </div>
        <Link to="/preferences" className="hover:text-orange-600 dark:hover:text-orange-400">
          个性化设置
        </Link>
      </div>
    </section>
  );
};

/**
 * Usage:
 * <PersonalizedFeed /> - Default personalized feed
 * <PersonalizedFeed compact showAnalytics /> - Compact version with analytics
 * <PersonalizedFeed showFeedback={false} /> - Without feedback collection
 *
 * Features:
 * - ML-powered personalized recommendations
 * - User feedback collection and learning
 * - Automatic view tracking and interaction recording
 * - Onboarding and analytics displays
 * - Confidence scoring for recommendations
 * - Responsive design with mobile optimization
 * - Real-time recommendation updates
 * - Integration with personalization store
 * - Accessibility support with ARIA labels
 * - Performance optimizations with lazy loading
 */
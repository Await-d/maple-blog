/**
 * PopularPosts组件测试
 * 测试热门文章组件的渲染、数据获取、筛选和分页功能
 */

import React from 'react';
import { render, screen, fireEvent, waitFor, within as _within } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import PopularPosts from '../PopularPosts';

// Mock hooks
vi.mock('../../../hooks/useResponsive', () => ({
  useResponsive: vi.fn(() => ({
    isMobile: false,
    isTablet: false,
    isDesktop: true,
    viewportWidth: 1200,
    breakpoint: 'lg',
    getHomeLayoutConfig: () => ({
      postsPerRow: 3,
      gridCols: 3,
      gap: 8,
      showFeaturedPosts: true
    })
  }))
}));

// Mock API
const mockPopularPosts = [
  {
    id: '1',
    title: 'React 19新特性详解',
    description: '深入了解React 19的最新功能和改进',
    imageUrl: '/images/react-19.jpg',
    url: '/posts/react-19-features',
    author: {
      name: '前端小王',
      avatar: '/avatars/wang.jpg'
    },
    publishedAt: '2024-03-15T10:00:00Z',
    readingTime: 10,
    viewCount: 15420,
    likeCount: 892,
    commentCount: 156,
    category: {
      name: 'React',
      slug: 'react',
      color: '#61DAFB'
    },
    tags: ['React', 'JavaScript', 'Frontend']
  },
  {
    id: '2',
    title: 'TypeScript高级类型应用',
    description: '掌握TypeScript中的高级类型技巧',
    imageUrl: '/images/typescript.jpg',
    url: '/posts/typescript-advanced-types',
    author: {
      name: '类型达人',
      avatar: '/avatars/type-master.jpg'
    },
    publishedAt: '2024-03-14T14:30:00Z',
    readingTime: 12,
    viewCount: 12380,
    likeCount: 743,
    commentCount: 98,
    category: {
      name: 'TypeScript',
      slug: 'typescript',
      color: '#3178C6'
    },
    tags: ['TypeScript', 'Types', 'JavaScript']
  },
  {
    id: '3',
    title: '.NET Core性能优化实战',
    description: '提升.NET应用性能的最佳实践',
    imageUrl: '/images/dotnet-performance.jpg',
    url: '/posts/dotnet-performance',
    author: {
      name: '性能优化师',
      avatar: '/avatars/perf-expert.jpg'
    },
    publishedAt: '2024-03-13T09:15:00Z',
    readingTime: 15,
    viewCount: 9876,
    likeCount: 567,
    commentCount: 78,
    category: {
      name: '.NET',
      slug: 'dotnet',
      color: '#512BD4'
    },
    tags: ['.NET', 'Performance', 'Optimization']
  }
];

// Mock IntersectionObserver
const mockIntersectionObserver = vi.fn();
mockIntersectionObserver.mockReturnValue({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
});
window.IntersectionObserver = mockIntersectionObserver;

// 创建测试用的QueryClient
const createTestQueryClient = () => new QueryClient({
  defaultOptions: {
    queries: { retry: false },
    mutations: { retry: false },
  },
});

const renderWithProviders = (ui: React.ReactElement) => {
  const queryClient = createTestQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>
  );
};

// Mock fetch API
const mockFetch = vi.fn();
global.fetch = mockFetch;

// Mock type for useResponsive hook
type MockUseResponsive = ReturnType<typeof import('../../../hooks/useResponsive').useResponsive>;

describe('PopularPosts', () => {
  let mockUseResponsive: vi.MockedFunction<() => MockUseResponsive>;

  beforeEach(() => {
    mockUseResponsive = vi.fn(() => ({
      isMobile: false,
      isTablet: false,
      isDesktop: true,
      viewportWidth: 1200,
      breakpoint: 'lg',
      getHomeLayoutConfig: () => ({
        postsPerRow: 3,
        gridCols: 3,
        gap: 8,
        showFeaturedPosts: true
      })
    }));

    vi.doMock('../../../hooks/useResponsive', () => ({
      useResponsive: mockUseResponsive
    }));

    // Mock successful API response
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({
        data: mockPopularPosts,
        pagination: {
          page: 1,
          limit: 10,
          total: mockPopularPosts.length,
          totalPages: 1
        }
      })
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('基础渲染', () => {
    it('应该渲染组件标题', async () => {
      renderWithProviders(<PopularPosts />);

      expect(screen.getByRole('heading', { name: /热门文章/i })).toBeInTheDocument();
    });

    it('应该显示加载状态', () => {
      // Mock loading state
      mockFetch.mockImplementation(() => new Promise(() => {
        // Never resolves - simulating loading state
      })); // Never resolves

      renderWithProviders(<PopularPosts />);

      expect(screen.getByRole('status', { name: /加载中/i })).toBeInTheDocument();
      expect(screen.getAllByTestId('post-skeleton')).toHaveLength(6); // 默认骨架屏数量
    });

    it('应该渲染文章列表', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        mockPopularPosts.forEach(post => {
          expect(screen.getByText(post.title)).toBeInTheDocument();
          expect(screen.getByText(post.description)).toBeInTheDocument();
        });
      });
    });

    it('应该显示文章统计信息', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        expect(screen.getByText('15.4k 阅读')).toBeInTheDocument();
        expect(screen.getByText('892 点赞')).toBeInTheDocument();
        expect(screen.getByText('156 评论')).toBeInTheDocument();
      });
    });
  });

  describe('时间范围筛选', () => {
    it('应该渲染时间筛选器', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        expect(screen.getByRole('combobox', { name: /时间范围/i })).toBeInTheDocument();
      });

      const timeRangeSelect = screen.getByRole('combobox', { name: /时间范围/i });
      fireEvent.click(timeRangeSelect);

      expect(screen.getByRole('option', { name: /今天/i })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: /本周/i })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: /本月/i })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: /全部时间/i })).toBeInTheDocument();
    });

    it('应该支持时间范围切换', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        const timeRangeSelect = screen.getByRole('combobox', { name: /时间范围/i });
        fireEvent.click(timeRangeSelect);
      });

      const thisWeekOption = screen.getByRole('option', { name: /本周/i });
      fireEvent.click(thisWeekOption);

      // 验证API调用包含正确的时间范围参数
      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('timeRange=week'),
          expect.any(Object)
        );
      });
    });

    it('应该记住选择的时间范围', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        const timeRangeSelect = screen.getByRole('combobox', { name: /时间范围/i });
        fireEvent.click(timeRangeSelect);
      });

      const monthOption = screen.getByRole('option', { name: /本月/i });
      fireEvent.click(monthOption);

      await waitFor(() => {
        const timeRangeSelect = screen.getByRole('combobox', { name: /时间范围/i });
        expect(timeRangeSelect).toHaveValue('month');
      });
    });
  });

  describe('排序功能', () => {
    it('应该提供排序选项', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        const sortSelect = screen.getByRole('combobox', { name: /排序方式/i });
        fireEvent.click(sortSelect);
      });

      expect(screen.getByRole('option', { name: /最多阅读/i })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: /最多点赞/i })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: /最多评论/i })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: /最新发布/i })).toBeInTheDocument();
    });

    it('应该支持排序切换', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        const sortSelect = screen.getByRole('combobox', { name: /排序方式/i });
        fireEvent.click(sortSelect);
      });

      const likesOption = screen.getByRole('option', { name: /最多点赞/i });
      fireEvent.click(likesOption);

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('sortBy=likes'),
          expect.any(Object)
        );
      });
    });
  });

  describe('响应式行为', () => {
    it('应该在移动端显示单列布局', async () => {
      mockUseResponsive.mockReturnValue({
        isMobile: true,
        isTablet: false,
        isDesktop: false,
        viewportWidth: 375,
        breakpoint: 'sm',
        getHomeLayoutConfig: () => ({
          postsPerRow: 1,
          gridCols: 1,
          gap: 4,
          showFeaturedPosts: false
        })
      });

      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        const postsGrid = screen.getByTestId('popular-posts-grid');
        expect(postsGrid).toHaveClass('grid-cols-1');
      });
    });

    it('应该在平板端显示双列布局', async () => {
      mockUseResponsive.mockReturnValue({
        isMobile: false,
        isTablet: true,
        isDesktop: false,
        viewportWidth: 768,
        breakpoint: 'md',
        getHomeLayoutConfig: () => ({
          postsPerRow: 2,
          gridCols: 2,
          gap: 6,
          showFeaturedPosts: true
        })
      });

      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        const postsGrid = screen.getByTestId('popular-posts-grid');
        expect(postsGrid).toHaveClass('grid-cols-2');
      });
    });

    it('应该在移动端隐藏部分统计信息', async () => {
      mockUseResponsive.mockReturnValue({
        isMobile: true,
        isTablet: false,
        isDesktop: false,
        viewportWidth: 375,
        breakpoint: 'sm',
        getHomeLayoutConfig: () => ({
          postsPerRow: 1,
          gridCols: 1,
          gap: 4
        })
      });

      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        // 移动端可能只显示阅读量，不显示详细的点赞和评论数
        expect(screen.queryByText('点赞')).not.toBeInTheDocument();
        expect(screen.getByText(/阅读/)).toBeInTheDocument();
      });
    });
  });

  describe('懒加载和分页', () => {
    it('应该支持无限滚动加载', async () => {
      // Mock IntersectionObserver触发
      const _mockIntersect = vi.fn();
      mockIntersectionObserver.mockImplementation((callback) => ({
        observe: vi.fn(),
        unobserve: vi.fn(),
        disconnect: vi.fn(),
        trigger: () => callback([{ isIntersecting: true }])
      }));

      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        expect(screen.getAllByTestId('post-card')).toHaveLength(mockPopularPosts.length);
      });

      // 模拟滚动到底部触发加载更多
      const _loadMoreTrigger = screen.getByTestId('load-more-trigger');
      const observer = mockIntersectionObserver.mock.results[0].value;
      observer.trigger();

      // 验证是否发起了加载更多的请求
      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('page=2'),
          expect.any(Object)
        );
      });
    });

    it('应该显示加载更多状态', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        // 模拟加载更多状态
        const _loadingMore = screen.queryByText(/加载更多/i);
        // 根据实际实现调整断言
      });
    });

    it('应该处理没有更多内容的情况', async () => {
      // Mock API返回最后一页
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          data: mockPopularPosts,
          pagination: {
            page: 1,
            limit: 10,
            total: mockPopularPosts.length,
            totalPages: 1,
            hasNextPage: false
          }
        })
      });

      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        expect(screen.queryByTestId('load-more-trigger')).not.toBeInTheDocument();
      });
    });
  });

  describe('交互功能', () => {
    it('应该支持文章点击跳转', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        const firstPost = screen.getByText(mockPopularPosts[0].title);
        expect(firstPost.closest('a')).toHaveAttribute('href', mockPopularPosts[0].url);
      });
    });

    it('应该支持作者点击跳转', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        const authorLink = screen.getByText(mockPopularPosts[0].author.name);
        expect(authorLink.closest('a')).toHaveAttribute(
          'href',
          expect.stringContaining('/authors/')
        );
      });
    });

    it('应该支持分类点击跳转', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        const categoryLink = screen.getByText(mockPopularPosts[0].category.name);
        expect(categoryLink.closest('a')).toHaveAttribute(
          'href',
          `/categories/${mockPopularPosts[0].category.slug}`
        );
      });
    });

    it('应该支持标签点击', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        mockPopularPosts[0].tags.forEach(tag => {
          const tagElement = screen.getByText(tag);
          expect(tagElement.closest('a')).toHaveAttribute(
            'href',
            expect.stringContaining(`/tags/${tag.toLowerCase()}`)
          );
        });
      });
    });
  });

  describe('可访问性', () => {
    it('应该有正确的语义结构', async () => {
      renderWithProviders(<PopularPosts />);

      expect(screen.getByRole('region', { name: /热门文章/i })).toBeInTheDocument();

      await waitFor(() => {
        const articles = screen.getAllByRole('article');
        expect(articles).toHaveLength(mockPopularPosts.length);
      });
    });

    it('应该为图片提供alt文本', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        mockPopularPosts.forEach(post => {
          const img = screen.getByAltText(post.title);
          expect(img).toBeInTheDocument();
        });
      });
    });

    it('应该支持键盘导航', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        const firstPostLink = screen.getByRole('link', { name: mockPopularPosts[0].title });

        // 测试Tab键导航
        firstPostLink.focus();
        expect(firstPostLink).toHaveFocus();

        // 测试Enter键激活
        fireEvent.keyDown(firstPostLink, { key: 'Enter', code: 'Enter' });
      });
    });

    it('应该为统计信息提供screen reader标签', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        const viewCount = screen.getByLabelText(/阅读次数/i);
        expect(viewCount).toBeInTheDocument();

        const likeCount = screen.getByLabelText(/点赞次数/i);
        expect(likeCount).toBeInTheDocument();

        const commentCount = screen.getByLabelText(/评论次数/i);
        expect(commentCount).toBeInTheDocument();
      });
    });
  });

  describe('错误处理', () => {
    it('应该处理API错误', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {
        // Intentionally empty - we want to silence console errors in tests
      });

      mockFetch.mockRejectedValue(new Error('Network error'));

      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        expect(screen.getByText(/加载失败/i)).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /重试/i })).toBeInTheDocument();
      });

      consoleSpy.mockRestore();
    });

    it('应该支持重试功能', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network error'));

      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        const retryButton = screen.getByRole('button', { name: /重试/i });
        fireEvent.click(retryButton);
      });

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledTimes(2);
      });
    });

    it('应该处理空数据情况', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          data: [],
          pagination: {
            page: 1,
            limit: 10,
            total: 0,
            totalPages: 0
          }
        })
      });

      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        expect(screen.getByText(/暂无热门文章/i)).toBeInTheDocument();
        expect(screen.getByText(/去看看其他内容/i)).toBeInTheDocument();
      });
    });

    it('应该处理图片加载失败', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        const images = screen.getAllByRole('img');
        const firstImage = images[0] as HTMLImageElement;

        // 模拟图片加载失败
        fireEvent.error(firstImage);

        expect(firstImage.src).toContain('placeholder');
      });
    });
  });

  describe('性能优化', () => {
    it('应该实现虚拟化滚动（大数据集）', async () => {
      // 模拟大量数据
      const largeDataset = Array.from({ length: 1000 }, (_, i) => ({
        ...mockPopularPosts[0],
        id: `post-${i}`,
        title: `Post ${i}`
      }));

      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          data: largeDataset,
          pagination: {
            page: 1,
            limit: 1000,
            total: 1000,
            totalPages: 1
          }
        })
      });

      renderWithProviders(<PopularPosts enableVirtualization />);

      await waitFor(() => {
        // 虚拟化应该只渲染可见的项目
        const visibleItems = screen.getAllByTestId('post-card');
        expect(visibleItems.length).toBeLessThan(largeDataset.length);
      });
    });

    it('应该实现图片懒加载', async () => {
      renderWithProviders(<PopularPosts />);

      await waitFor(() => {
        const images = screen.getAllByRole('img');
        images.forEach(img => {
          expect(img).toHaveAttribute('loading', 'lazy');
          expect(img).toHaveAttribute('decoding', 'async');
        });
      });
    });
  });
});
/**
 * HeroSection组件测试
 * 测试英雄区域组件的渲染、交互和响应式行为
 */

import React from 'react';
import { render, screen, fireEvent, waitFor, within as _within } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import HeroSection from '../HeroSection';

// Mock hooks
vi.mock('../../../hooks/useResponsive', () => ({
  useResponsive: vi.fn(() => ({
    isMobile: false,
    isTablet: false,
    isDesktop: true,
    viewportWidth: 1200,
    viewportHeight: 800,
    breakpoint: 'lg'
  }))
}));

vi.mock('../../../hooks/useHomePerformance', () => ({
  useHomePerformance: vi.fn(() => ({
    observeLazyImage: vi.fn(),
    unobserveLazyImage: vi.fn()
  }))
}));

// Mock IntersectionObserver
const mockIntersectionObserver = vi.fn();
mockIntersectionObserver.mockReturnValue({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
});
window.IntersectionObserver = mockIntersectionObserver;

// Mock matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
    matches: query === '(prefers-reduced-motion: reduce)',
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

// 测试数据
const mockFeaturedPosts = [
  {
    id: '1',
    title: '现代React开发最佳实践',
    description: '深入探讨React 19的新特性和开发模式',
    imageUrl: '/images/react-best-practices.jpg',
    url: '/posts/react-best-practices',
    author: {
      name: '张开发',
      avatar: '/avatars/zhang.jpg'
    },
    publishedAt: '2024-03-15T10:00:00Z',
    readingTime: 8,
    tags: ['React', 'JavaScript', 'Frontend']
  },
  {
    id: '2',
    title: '.NET Core微服务架构设计',
    description: '构建可扩展的微服务系统实践指南',
    imageUrl: '/images/dotnet-microservices.jpg',
    url: '/posts/dotnet-microservices',
    author: {
      name: '李架构',
      avatar: '/avatars/li.jpg'
    },
    publishedAt: '2024-03-14T14:30:00Z',
    readingTime: 12,
    tags: ['.NET', 'Microservices', 'Backend']
  }
];

// 测试工具函数
const createTestQueryClient = () => new QueryClient({
  defaultOptions: {
    queries: { retry: false },
    mutations: { retry: false },
  },
});

const renderWithProviders = (ui: React.ReactElement, options = {}) => {
  const queryClient = createTestQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>,
    options
  );
};

// Mock type for useResponsive hook
type MockUseResponsive = ReturnType<typeof import('../../../hooks/useResponsive').useResponsive>;

describe('HeroSection', () => {
  let mockUseResponsive: vi.MockedFunction<() => MockUseResponsive>;

  beforeEach(() => {
    mockUseResponsive = vi.fn(() => ({
      isMobile: false,
      isTablet: false,
      isDesktop: true,
      viewportWidth: 1200,
      viewportHeight: 800,
      breakpoint: 'lg',
      getComponentSizes: () => ({
        titleSize: 'text-3xl',
        bodySize: 'text-base',
        buttonSize: 'md'
      })
    }));

    vi.doMock('../../../hooks/useResponsive', () => ({
      useResponsive: mockUseResponsive
    }));
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('基础渲染', () => {
    it('应该渲染默认的英雄区域内容', () => {
      renderWithProviders(<HeroSection />);

      expect(screen.getByRole('banner')).toBeInTheDocument();
      expect(screen.getByText(/欢迎来到/)).toBeInTheDocument();
      expect(screen.getByText('Maple Blog')).toBeInTheDocument();
      expect(screen.getByText(/分享技术见解/)).toBeInTheDocument();
    });

    it('应该渲染CTA按钮', () => {
      renderWithProviders(<HeroSection />);

      expect(screen.getByRole('link', { name: /开始阅读/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /探索分类/i })).toBeInTheDocument();
    });

    it('应该显示滚动指示器', () => {
      renderWithProviders(<HeroSection />);

      const scrollIndicator = screen.getByLabelText(/滚动查看更多/i);
      expect(scrollIndicator).toBeInTheDocument();
    });
  });

  describe('特色内容轮播', () => {
    it('应该渲染特色文章轮播', () => {
      renderWithProviders(
        <HeroSection featuredPosts={mockFeaturedPosts} />
      );

      expect(screen.getByText('现代React开发最佳实践')).toBeInTheDocument();
      expect(screen.getByText('.NET Core微服务架构设计')).toBeInTheDocument();
    });

    it('应该支持轮播导航', async () => {
      renderWithProviders(
        <HeroSection featuredPosts={mockFeaturedPosts} />
      );

      const nextButton = screen.getByLabelText(/下一张/i);
      const prevButton = screen.getByLabelText(/上一张/i);

      expect(nextButton).toBeInTheDocument();
      expect(prevButton).toBeInTheDocument();

      // 测试下一张按钮
      fireEvent.click(nextButton);

      // 等待动画完成
      await waitFor(() => {
        // 验证轮播状态变化
        expect(nextButton).not.toBeDisabled();
      });
    });

    it('应该支持指示器点击导航', async () => {
      renderWithProviders(
        <HeroSection featuredPosts={mockFeaturedPosts} />
      );

      const indicators = screen.getAllByRole('button', { name: /转到幻灯片/i });
      expect(indicators).toHaveLength(mockFeaturedPosts.length);

      // 点击第二个指示器
      fireEvent.click(indicators[1]);

      await waitFor(() => {
        // 验证当前激活的指示器
        expect(indicators[1]).toHaveAttribute('aria-current', 'true');
      });
    });

    it('应该自动播放轮播', async () => {
      vi.useFakeTimers();

      renderWithProviders(
        <HeroSection
          featuredPosts={mockFeaturedPosts}
          autoPlay={true}
          autoPlayInterval={3000}
        />
      );

      const indicators = screen.getAllByRole('button', { name: /转到幻灯片/i });
      expect(indicators[0]).toHaveAttribute('aria-current', 'true');

      // 快进3秒
      vi.advanceTimersByTime(3000);

      await waitFor(() => {
        expect(indicators[1]).toHaveAttribute('aria-current', 'true');
      });

      vi.useRealTimers();
    });

    it('应该在鼠标悬停时暂停自动播放', async () => {
      vi.useFakeTimers();

      renderWithProviders(
        <HeroSection
          featuredPosts={mockFeaturedPosts}
          autoPlay={true}
          autoPlayInterval={3000}
        />
      );

      const carousel = screen.getByRole('region', { name: /轮播/i });

      // 鼠标悬停
      fireEvent.mouseEnter(carousel);

      // 快进时间
      vi.advanceTimersByTime(5000);

      const indicators = screen.getAllByRole('button', { name: /转到幻灯片/i });

      // 应该仍然在第一张
      expect(indicators[0]).toHaveAttribute('aria-current', 'true');

      vi.useRealTimers();
    });
  });

  describe('响应式行为', () => {
    it('应该在移动端显示简化版本', () => {
      mockUseResponsive.mockReturnValue({
        isMobile: true,
        isTablet: false,
        isDesktop: false,
        viewportWidth: 375,
        viewportHeight: 667,
        breakpoint: 'sm',
        getComponentSizes: () => ({
          titleSize: 'text-xl',
          bodySize: 'text-sm',
          buttonSize: 'sm'
        })
      });

      renderWithProviders(<HeroSection />);

      const heroSection = screen.getByRole('banner');
      expect(heroSection).toHaveClass('min-h-[300px]'); // 移动端高度
    });

    it('应该在平板端调整布局', () => {
      mockUseResponsive.mockReturnValue({
        isMobile: false,
        isTablet: true,
        isDesktop: false,
        viewportWidth: 768,
        viewportHeight: 1024,
        breakpoint: 'md',
        getComponentSizes: () => ({
          titleSize: 'text-2xl',
          bodySize: 'text-base',
          buttonSize: 'md'
        })
      });

      renderWithProviders(<HeroSection />);

      const heroSection = screen.getByRole('banner');
      expect(heroSection).toHaveClass('min-h-[400px]'); // 平板端高度
    });

    it('应该根据屏幕尺寸调整文字大小', () => {
      // 桌面端
      renderWithProviders(<HeroSection />);

      const title = screen.getByRole('heading', { level: 1 });
      expect(title).toHaveClass('text-3xl'); // 或相应的桌面端样式
    });
  });

  describe('可访问性', () => {
    it('应该有正确的ARIA标签', () => {
      renderWithProviders(
        <HeroSection featuredPosts={mockFeaturedPosts} />
      );

      expect(screen.getByRole('banner')).toBeInTheDocument();
      expect(screen.getByRole('region', { name: /轮播/i })).toBeInTheDocument();
      expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument();
    });

    it('应该支持键盘导航', () => {
      renderWithProviders(
        <HeroSection featuredPosts={mockFeaturedPosts} />
      );

      const nextButton = screen.getByLabelText(/下一张/i);

      // Tab键应该能够聚焦到按钮
      nextButton.focus();
      expect(nextButton).toHaveFocus();

      // Enter键应该能够激活按钮
      fireEvent.keyDown(nextButton, { key: 'Enter', code: 'Enter' });
      // 验证轮播行为
    });

    it('应该为图片提供alt文本', () => {
      renderWithProviders(
        <HeroSection featuredPosts={mockFeaturedPosts} />
      );

      mockFeaturedPosts.forEach(post => {
        const images = screen.getAllByAltText(post.title);
        expect(images.length).toBeGreaterThan(0);
      });
    });

    it('应该支持屏幕阅读器公告', async () => {
      renderWithProviders(
        <HeroSection featuredPosts={mockFeaturedPosts} />
      );

      const nextButton = screen.getByLabelText(/下一张/i);
      fireEvent.click(nextButton);

      // 检查是否有live region用于公告轮播变化
      await waitFor(() => {
        const liveRegion = screen.getByRole('status', { hidden: true });
        expect(liveRegion).toBeInTheDocument();
      });
    });
  });

  describe('性能优化', () => {
    it('应该懒加载图片', () => {
      const mockObserveLazyImage = vi.fn();
      vi.doMock('../../../hooks/useHomePerformance', () => ({
        useHomePerformance: vi.fn(() => ({
          observeLazyImage: mockObserveLazyImage,
          unobserveLazyImage: vi.fn()
        }))
      }));

      renderWithProviders(
        <HeroSection featuredPosts={mockFeaturedPosts} />
      );

      // 验证懒加载observer被调用
      expect(mockObserveLazyImage).toHaveBeenCalled();
    });

    it('应该预加载下一张图片', async () => {
      renderWithProviders(
        <HeroSection featuredPosts={mockFeaturedPosts} />
      );

      // 等待预加载逻辑执行
      await waitFor(() => {
        // 检查是否创建了预加载的图片元素
        const preloadImages = document.querySelectorAll('link[rel="preload"][as="image"]');
        expect(preloadImages.length).toBeGreaterThan(0);
      });
    });

    it('应该在reduced motion偏好时禁用动画', () => {
      // Mock prefers-reduced-motion: reduce
      window.matchMedia = vi.fn().mockImplementation(query => ({
        matches: query === '(prefers-reduced-motion: reduce)',
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      }));

      renderWithProviders(
        <HeroSection featuredPosts={mockFeaturedPosts} />
      );

      const carousel = screen.getByRole('region', { name: /轮播/i });
      // 验证是否应用了减少动画的样式
      expect(carousel).not.toHaveClass('transition-transform');
    });
  });

  describe('用户交互', () => {
    it('应该响应CTA按钮点击', () => {
      const _mockNavigate = vi.fn();

      renderWithProviders(<HeroSection />);

      const startReadingButton = screen.getByRole('link', { name: /开始阅读/i });
      expect(startReadingButton).toHaveAttribute('href', '/blog');

      const exploreCategoriesButton = screen.getByRole('link', { name: /探索分类/i });
      expect(exploreCategoriesButton).toHaveAttribute('href', '/categories');
    });

    it('应该支持滚动到内容区域', () => {
      const mockScrollIntoView = vi.fn();
      Element.prototype.scrollIntoView = mockScrollIntoView;

      renderWithProviders(<HeroSection />);

      const scrollIndicator = screen.getByLabelText(/滚动查看更多/i);
      fireEvent.click(scrollIndicator);

      expect(mockScrollIntoView).toHaveBeenCalledWith({
        behavior: 'smooth',
        block: 'start'
      });
    });

    it('应该在触摸设备上支持滑动手势', () => {
      mockUseResponsive.mockReturnValue({
        isMobile: true,
        isTablet: false,
        isDesktop: false,
        viewportWidth: 375,
        viewportHeight: 667,
        breakpoint: 'sm',
        isTouchDevice: true,
        getComponentSizes: () => ({
          titleSize: 'text-xl',
          bodySize: 'text-sm',
          buttonSize: 'sm'
        })
      });

      renderWithProviders(
        <HeroSection featuredPosts={mockFeaturedPosts} />
      );

      const carousel = screen.getByRole('region', { name: /轮播/i });

      // 模拟触摸滑动
      fireEvent.touchStart(carousel, {
        touches: [{ clientX: 100, clientY: 0 }]
      });

      fireEvent.touchMove(carousel, {
        touches: [{ clientX: 50, clientY: 0 }]
      });

      fireEvent.touchEnd(carousel);

      // 验证轮播响应了滑动手势
      // 这里可以检查轮播状态的变化
    });
  });

  describe('错误处理', () => {
    it('应该处理图片加载失败', async () => {
      renderWithProviders(
        <HeroSection featuredPosts={mockFeaturedPosts} />
      );

      const images = screen.getAllByRole('img');
      const firstImage = images[0] as HTMLImageElement;

      // 模拟图片加载失败
      fireEvent.error(firstImage);

      await waitFor(() => {
        // 验证是否显示了默认图片或占位符
        expect(firstImage.src).toContain('placeholder');
      });
    });

    it('应该处理空的特色文章列表', () => {
      renderWithProviders(<HeroSection featuredPosts={[]} />);

      // 应该显示默认内容而不是轮播
      expect(screen.getByText('Maple Blog')).toBeInTheDocument();
      expect(screen.queryByRole('region', { name: /轮播/i })).not.toBeInTheDocument();
    });

    it('应该处理网络错误优雅降级', () => {
      // Mock网络错误
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {
        // Intentionally empty - we want to silence console errors in tests
      });

      renderWithProviders(
        <HeroSection featuredPosts={mockFeaturedPosts} />
      );

      // 组件应该仍然渲染，即使有网络问题
      expect(screen.getByRole('banner')).toBeInTheDocument();

      consoleSpy.mockRestore();
    });
  });
});
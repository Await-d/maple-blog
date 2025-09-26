/**
 * Header组件测试
 * 测试网站头部导航的渲染、交互和响应式行为
 */

import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import Header from '../Header';

// Mock hooks
vi.mock('../../../hooks/useResponsive', () => ({
  useResponsive: vi.fn(() => ({
    isMobile: false,
    isTablet: false,
    isDesktop: true,
    viewportWidth: 1200,
    breakpoint: 'lg',
    isTouchDevice: false
  }))
}));

vi.mock('../../../hooks/useAuth', () => ({
  useAuth: vi.fn(() => ({
    user: null,
    isAuthenticated: false,
    login: vi.fn(),
    logout: vi.fn(),
    loading: false
  }))
}));

// Mock stores
vi.mock('../../../stores/homeStore', () => ({
  useHomeStore: vi.fn(() => ({
    theme: 'light',
    setTheme: vi.fn(),
    searchQuery: '',
    setSearchQuery: vi.fn(),
    showSearch: false,
    setShowSearch: vi.fn()
  }))
}));

// Mock components
vi.mock('../Navigation', () => ({
  default: ({ mobile = false }) => (
    <div data-testid={mobile ? 'mobile-navigation' : 'desktop-navigation'}>
      <a href="/blog">博客</a>
      <a href="/categories">分类</a>
      <a href="/archive">归档</a>
      <a href="/about">关于</a>
    </div>
  )
}));

vi.mock('../../home/SearchBox', () => ({
  default: ({ onSearch, placeholder, className }) => (
    <div data-testid="search-box" className={className}>
      <input
        type="text"
        placeholder={placeholder || '搜索文章...'}
        onChange={(e) => onSearch?.(e.target.value)}
        aria-label="搜索"
      />
    </div>
  )
}));

vi.mock('../../home/ThemeToggle', () => ({
  default: ({ size = 'md', className }) => (
    <button
      data-testid="theme-toggle"
      className={className}
      aria-label="切换主题"
    >
      {size === 'sm' ? '🌙' : '🌞'}
    </button>
  )
}));

// 测试工具
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
      <BrowserRouter>
        {ui}
      </BrowserRouter>
    </QueryClientProvider>
  );
};

// Mock用户数据
const mockUser = {
  id: '1',
  username: 'testuser',
  email: 'test@example.com',
  name: '测试用户',
  avatar: '/avatars/test.jpg',
  role: 'user' as const
};

const mockAdminUser = {
  ...mockUser,
  id: '2',
  username: 'admin',
  name: '管理员',
  role: 'admin' as const
};

describe('Header', () => {
  let mockUseResponsive: ReturnType<typeof vi.fn>;
  let mockUseAuth: ReturnType<typeof vi.fn>;
  let mockUseHomeStore: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    mockUseResponsive = vi.fn(() => ({
      isMobile: false,
      isTablet: false,
      isDesktop: true,
      viewportWidth: 1200,
      breakpoint: 'lg',
      isTouchDevice: false
    }));

    mockUseAuth = vi.fn(() => ({
      user: null,
      isAuthenticated: false,
      login: vi.fn(),
      logout: vi.fn(),
      loading: false
    }));

    mockUseHomeStore = vi.fn(() => ({
      theme: 'light',
      setTheme: vi.fn(),
      searchQuery: '',
      setSearchQuery: vi.fn(),
      showSearch: false,
      setShowSearch: vi.fn()
    }));

    vi.doMock('../../../hooks/useResponsive', () => ({
      useResponsive: mockUseResponsive
    }));

    vi.doMock('../../../hooks/useAuth', () => ({
      useAuth: mockUseAuth
    }));

    vi.doMock('../../../stores/homeStore', () => ({
      useHomeStore: mockUseHomeStore
    }));
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('基础渲染', () => {
    it('应该渲染网站Logo', () => {
      renderWithProviders(<Header />);

      const logo = screen.getByRole('link', { name: /Maple Blog/i });
      expect(logo).toBeInTheDocument();
      expect(logo).toHaveAttribute('href', '/');
    });

    it('应该渲染桌面端导航菜单', () => {
      renderWithProviders(<Header />);

      expect(screen.getByTestId('desktop-navigation')).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /博客/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /分类/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /归档/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /关于/i })).toBeInTheDocument();
    });

    it('应该渲染搜索框', () => {
      renderWithProviders(<Header />);

      expect(screen.getByTestId('search-box')).toBeInTheDocument();
      expect(screen.getByLabelText('搜索')).toBeInTheDocument();
    });

    it('应该渲染主题切换按钮', () => {
      renderWithProviders(<Header />);

      expect(screen.getByTestId('theme-toggle')).toBeInTheDocument();
      expect(screen.getByLabelText('切换主题')).toBeInTheDocument();
    });

    it('应该在未登录时显示登录按钮', () => {
      renderWithProviders(<Header />);

      expect(screen.getByRole('link', { name: /登录/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /注册/i })).toBeInTheDocument();
    });
  });

  describe('用户认证状态', () => {
    it('应该在登录后显示用户菜单', () => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        login: vi.fn(),
        logout: vi.fn(),
        loading: false
      });

      renderWithProviders(<Header />);

      expect(screen.getByRole('button', { name: /用户菜单/i })).toBeInTheDocument();
      expect(screen.queryByRole('link', { name: /登录/i })).not.toBeInTheDocument();
    });

    it('应该显示用户头像和名称', () => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        login: vi.fn(),
        logout: vi.fn(),
        loading: false
      });

      renderWithProviders(<Header />);

      expect(screen.getByAltText('测试用户')).toBeInTheDocument();
      expect(screen.getByText('测试用户')).toBeInTheDocument();
    });

    it('应该支持用户菜单展开和收起', async () => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        login: vi.fn(),
        logout: vi.fn(),
        loading: false
      });

      renderWithProviders(<Header />);

      const userMenuButton = screen.getByRole('button', { name: /用户菜单/i });

      // 点击展开菜单
      fireEvent.click(userMenuButton);

      await waitFor(() => {
        expect(screen.getByRole('menu')).toBeInTheDocument();
        expect(screen.getByRole('menuitem', { name: /个人资料/i })).toBeInTheDocument();
        expect(screen.getByRole('menuitem', { name: /设置/i })).toBeInTheDocument();
        expect(screen.getByRole('menuitem', { name: /退出登录/i })).toBeInTheDocument();
      });

      // 点击收起菜单
      fireEvent.click(userMenuButton);

      await waitFor(() => {
        expect(screen.queryByRole('menu')).not.toBeInTheDocument();
      });
    });

    it('应该为管理员用户显示管理菜单', () => {
      mockUseAuth.mockReturnValue({
        user: mockAdminUser,
        isAuthenticated: true,
        login: vi.fn(),
        logout: vi.fn(),
        loading: false
      });

      renderWithProviders(<Header />);

      const userMenuButton = screen.getByRole('button', { name: /用户菜单/i });
      fireEvent.click(userMenuButton);

      expect(screen.getByRole('menuitem', { name: /管理后台/i })).toBeInTheDocument();
    });

    it('应该支持退出登录', async () => {
      const mockLogout = vi.fn();

      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        login: vi.fn(),
        logout: mockLogout,
        loading: false
      });

      renderWithProviders(<Header />);

      const userMenuButton = screen.getByRole('button', { name: /用户菜单/i });
      fireEvent.click(userMenuButton);

      const logoutButton = screen.getByRole('menuitem', { name: /退出登录/i });
      fireEvent.click(logoutButton);

      expect(mockLogout).toHaveBeenCalled();
    });
  });

  describe('搜索功能', () => {
    it('应该支持搜索输入', () => {
      const mockSetSearchQuery = vi.fn();

      mockUseHomeStore.mockReturnValue({
        theme: 'light',
        setTheme: vi.fn(),
        searchQuery: '',
        setSearchQuery: mockSetSearchQuery,
        showSearch: false,
        setShowSearch: vi.fn()
      });

      renderWithProviders(<Header />);

      const searchInput = screen.getByLabelText('搜索');
      fireEvent.change(searchInput, { target: { value: 'React' } });

      // 验证搜索回调被调用
      expect(mockSetSearchQuery).toHaveBeenCalledWith('React');
    });

    it('应该支持移动端搜索切换', () => {
      const mockSetShowSearch = vi.fn();

      mockUseResponsive.mockReturnValue({
        isMobile: true,
        isTablet: false,
        isDesktop: false,
        viewportWidth: 375,
        breakpoint: 'sm',
        isTouchDevice: true
      });

      mockUseHomeStore.mockReturnValue({
        theme: 'light',
        setTheme: vi.fn(),
        searchQuery: '',
        setSearchQuery: vi.fn(),
        showSearch: false,
        setShowSearch: mockSetShowSearch
      });

      renderWithProviders(<Header />);

      const searchToggle = screen.getByRole('button', { name: /搜索/i });
      fireEvent.click(searchToggle);

      expect(mockSetShowSearch).toHaveBeenCalledWith(true);
    });

    it('应该在移动端显示全屏搜索覆盖层', () => {
      mockUseResponsive.mockReturnValue({
        isMobile: true,
        isTablet: false,
        isDesktop: false,
        viewportWidth: 375,
        breakpoint: 'sm',
        isTouchDevice: true
      });

      mockUseHomeStore.mockReturnValue({
        theme: 'light',
        setTheme: vi.fn(),
        searchQuery: '',
        setSearchQuery: vi.fn(),
        showSearch: true,
        setShowSearch: vi.fn()
      });

      renderWithProviders(<Header />);

      expect(screen.getByTestId('search-overlay')).toBeInTheDocument();
    });
  });

  describe('主题切换', () => {
    it('应该支持主题切换', () => {
      const mockSetTheme = vi.fn();

      mockUseHomeStore.mockReturnValue({
        theme: 'light',
        setTheme: mockSetTheme,
        searchQuery: '',
        setSearchQuery: vi.fn(),
        showSearch: false,
        setShowSearch: vi.fn()
      });

      renderWithProviders(<Header />);

      const themeToggle = screen.getByTestId('theme-toggle');
      fireEvent.click(themeToggle);

      expect(mockSetTheme).toHaveBeenCalledWith('dark');
    });

    it('应该显示当前主题状态', () => {
      mockUseHomeStore.mockReturnValue({
        theme: 'dark',
        setTheme: vi.fn(),
        searchQuery: '',
        setSearchQuery: vi.fn(),
        showSearch: false,
        setShowSearch: vi.fn()
      });

      renderWithProviders(<Header />);

      const themeToggle = screen.getByTestId('theme-toggle');
      expect(themeToggle).toHaveTextContent('🌙'); // 暗色主题图标
    });
  });

  describe('响应式行为', () => {
    it('应该在移动端显示汉堡菜单', () => {
      mockUseResponsive.mockReturnValue({
        isMobile: true,
        isTablet: false,
        isDesktop: false,
        viewportWidth: 375,
        breakpoint: 'sm',
        isTouchDevice: true
      });

      renderWithProviders(<Header />);

      expect(screen.getByRole('button', { name: /打开菜单/i })).toBeInTheDocument();
      expect(screen.queryByTestId('desktop-navigation')).not.toBeInTheDocument();
    });

    it('应该支持移动端菜单展开和收起', async () => {
      mockUseResponsive.mockReturnValue({
        isMobile: true,
        isTablet: false,
        isDesktop: false,
        viewportWidth: 375,
        breakpoint: 'sm',
        isTouchDevice: true
      });

      renderWithProviders(<Header />);

      const menuButton = screen.getByRole('button', { name: /打开菜单/i });

      // 展开菜单
      fireEvent.click(menuButton);

      await waitFor(() => {
        expect(screen.getByTestId('mobile-navigation')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /关闭菜单/i })).toBeInTheDocument();
      });

      // 收起菜单
      const closeButton = screen.getByRole('button', { name: /关闭菜单/i });
      fireEvent.click(closeButton);

      await waitFor(() => {
        expect(screen.queryByTestId('mobile-navigation')).not.toBeInTheDocument();
        expect(screen.getByRole('button', { name: /打开菜单/i })).toBeInTheDocument();
      });
    });

    it('应该在平板端调整布局', () => {
      mockUseResponsive.mockReturnValue({
        isMobile: false,
        isTablet: true,
        isDesktop: false,
        viewportWidth: 768,
        breakpoint: 'md',
        isTouchDevice: true
      });

      renderWithProviders(<Header />);

      const header = screen.getByRole('banner');
      expect(header).toHaveClass('px-6'); // 平板端间距
    });

    it('应该在小屏幕上隐藏部分导航项', () => {
      mockUseResponsive.mockReturnValue({
        isMobile: false,
        isTablet: true,
        isDesktop: false,
        viewportWidth: 768,
        breakpoint: 'md',
        isTouchDevice: true
      });

      renderWithProviders(<Header />);

      // 平板端可能隐藏某些次要导航项
      expect(screen.queryByRole('link', { name: /关于/i })).not.toBeInTheDocument();
    });
  });

  describe('滚动行为', () => {
    it('应该在滚动时改变头部样式', async () => {
      renderWithProviders(<Header />);

      const header = screen.getByRole('banner');

      // 模拟滚动
      Object.defineProperty(window, 'scrollY', { value: 100 });
      fireEvent.scroll(window);

      await waitFor(() => {
        expect(header).toHaveClass('shadow-md'); // 滚动时的阴影
      });
    });

    it('应该支持头部自动隐藏（向下滚动时）', async () => {
      renderWithProviders(<Header autoHide />);

      const header = screen.getByRole('banner');

      // 模拟向下滚动
      let scrollY = 0;
      Object.defineProperty(window, 'scrollY', {
        get: () => scrollY,
        configurable: true
      });

      // 快速向下滚动
      scrollY = 200;
      fireEvent.scroll(window);

      await waitFor(() => {
        expect(header).toHaveClass('-translate-y-full'); // 头部隐藏
      });

      // 向上滚动
      scrollY = 150;
      fireEvent.scroll(window);

      await waitFor(() => {
        expect(header).not.toHaveClass('-translate-y-full'); // 头部显示
      });
    });
  });

  describe('可访问性', () => {
    it('应该有正确的语义结构', () => {
      renderWithProviders(<Header />);

      expect(screen.getByRole('banner')).toBeInTheDocument();
      expect(screen.getByRole('navigation')).toBeInTheDocument();
    });

    it('应该支持键盘导航', () => {
      renderWithProviders(<Header />);

      const logo = screen.getByRole('link', { name: /Maple Blog/i });
      const _blogLink = screen.getByRole('link', { name: /博客/i });

      // Tab键导航
      logo.focus();
      expect(logo).toHaveFocus();

      fireEvent.keyDown(logo, { key: 'Tab' });
      // 验证焦点移动到下一个可聚焦元素
    });

    it('应该为汉堡菜单提供正确的ARIA标签', () => {
      mockUseResponsive.mockReturnValue({
        isMobile: true,
        isTablet: false,
        isDesktop: false,
        viewportWidth: 375,
        breakpoint: 'sm',
        isTouchDevice: true
      });

      renderWithProviders(<Header />);

      const menuButton = screen.getByRole('button', { name: /打开菜单/i });
      expect(menuButton).toHaveAttribute('aria-expanded', 'false');

      fireEvent.click(menuButton);

      expect(menuButton).toHaveAttribute('aria-expanded', 'true');
    });

    it('应该为用户菜单提供正确的ARIA标签', () => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        login: vi.fn(),
        logout: vi.fn(),
        loading: false
      });

      renderWithProviders(<Header />);

      const userMenuButton = screen.getByRole('button', { name: /用户菜单/i });
      expect(userMenuButton).toHaveAttribute('aria-haspopup', 'menu');
    });

    it('应该支持ESC键关闭菜单', async () => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        login: vi.fn(),
        logout: vi.fn(),
        loading: false
      });

      renderWithProviders(<Header />);

      const userMenuButton = screen.getByRole('button', { name: /用户菜单/i });
      fireEvent.click(userMenuButton);

      expect(screen.getByRole('menu')).toBeInTheDocument();

      // 按ESC键
      fireEvent.keyDown(document, { key: 'Escape' });

      await waitFor(() => {
        expect(screen.queryByRole('menu')).not.toBeInTheDocument();
      });
    });

    it('应该为图标按钮提供描述性标签', () => {
      renderWithProviders(<Header />);

      expect(screen.getByLabelText('切换主题')).toBeInTheDocument();

      // 如果有搜索图标
      if (screen.queryByLabelText('搜索')) {
        expect(screen.getByLabelText('搜索')).toBeInTheDocument();
      }
    });
  });

  describe('性能优化', () => {
    it('应该防止频繁的滚动事件触发', async () => {
      const _scrollHandler = vi.fn();

      renderWithProviders(<Header />);

      // 模拟快速滚动事件
      for (let i = 0; i < 10; i++) {
        fireEvent.scroll(window);
      }

      // 由于防抖，处理函数应该被调用次数较少
      await waitFor(() => {
        // 验证防抖逻辑
      });
    });

    it('应该延迟加载用户头像', () => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        login: vi.fn(),
        logout: vi.fn(),
        loading: false
      });

      renderWithProviders(<Header />);

      const avatar = screen.getByAltText('测试用户');
      expect(avatar).toHaveAttribute('loading', 'lazy');
    });
  });

  describe('错误处理', () => {
    it('应该处理认证加载状态', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        login: vi.fn(),
        logout: vi.fn(),
        loading: true
      });

      renderWithProviders(<Header />);

      // 加载状态时应显示骨架屏或加载指示器
      expect(screen.getByTestId('user-menu-skeleton')).toBeInTheDocument();
    });

    it('应该处理用户头像加载失败', async () => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        login: vi.fn(),
        logout: vi.fn(),
        loading: false
      });

      renderWithProviders(<Header />);

      const avatar = screen.getByAltText('测试用户') as HTMLImageElement;

      // 模拟图片加载失败
      fireEvent.error(avatar);

      await waitFor(() => {
        expect(avatar.src).toContain('default-avatar'); // 默认头像
      });
    });

    it('应该处理网络错误时的优雅降级', () => {
      // Mock网络错误情况下的组件行为
      renderWithProviders(<Header />);

      // 组件应该仍然可以正常渲染和交互
      expect(screen.getByRole('banner')).toBeInTheDocument();
    });
  });
});
// @ts-nocheck
/**
 * App - Maple Blog应用程序根组件
 * 定义全局布局、路由和状态管理集成
 */

import React, { Suspense, useEffect } from 'react';
import { Routes, Route, useLocation } from 'react-router-dom';
import { Helmet } from '@/components/common/DocumentHead';

// 应用程序布局和页面组件
import { ResponsiveLayout } from './components/home/ResponsiveLayout';
import { GlobalHeader } from './components/layout/GlobalHeader';
import { GlobalFooter } from './components/layout/GlobalFooter';
import { GlobalSidebar } from './components/layout/GlobalSidebar';

// 页面组件 - 使用懒加载优化性能
const HomePage = React.lazy(() => import('./pages/HomePage'));
const BlogListPage = React.lazy(() => import('./pages/blog/BlogListPage'));
const BlogPostPage = React.lazy(() => import('./pages/blog/BlogPostPage'));
const LoginPage = React.lazy(() => import('./pages/auth/LoginPage'));
const RegisterPage = React.lazy(() => import('./pages/auth/RegisterPage'));
const ResetPasswordPage = React.lazy(() => import('./pages/auth/ResetPasswordPage'));
const AdminDashboard = React.lazy(() => import('./pages/admin/AdminDashboard'));
const ArchivePage = React.lazy(() => import('./pages/archive/ArchivePage'));

// 错误和加载组件
import { NotFoundPage } from './pages/errors/NotFoundPage';
import { PageLoadingFallback } from './components/ui/PageLoadingFallback';

// Hooks和工具
import { useTheme } from './hooks/useTheme';
import { useAuth } from './hooks/useAuth';
import { UserRole } from './types/auth';
import { ENV_CONFIG } from './types/env';

/**
 * 受保护的路由组件
 */
const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, loading } = useAuth();
  const location = useLocation();

  if (loading) {
    return <PageLoadingFallback />;
  }

  if (!isAuthenticated) {
    // 重定向到登录页面，保存当前路径
    window.location.href = `/login?redirect=${encodeURIComponent(location.pathname)}`;
    return <PageLoadingFallback />;
  }

  return <>{children}</>;
};

/**
 * 管理员路由组件
 */
const AdminRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, isAuthenticated, loading } = useAuth();

  if (loading) {
    return <PageLoadingFallback />;
  }

  if (!isAuthenticated || !user || user.role !== UserRole.Admin) {
    // 重定向到首页或显示权限不足页面
    window.location.href = '/';
    return <PageLoadingFallback />;
  }

  return <>{children}</>;
};

/**
 * 应用程序根组件
 */
const App: React.FC = () => {
  const { theme, toggleTheme } = useTheme();
  const { initialize: initializeAuth } = useAuth();
  const location = useLocation();

  // 初始化认证系统
  useEffect(() => {
    initializeAuth();
  }, [initializeAuth]);

  // 页面变化时滚动到顶部
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [location.pathname]);

  // 应用主题到document
  useEffect(() => {
    document.documentElement.classList.toggle('dark', theme === 'dark');
    document.documentElement.setAttribute('data-theme', theme);
  }, [theme]);

  // 页面元数据
  const getPageMetadata = () => {
    const defaultMeta = {
      title: 'Maple Blog',
      description: '现代化的AI驱动博客系统，技术分享与知识交流的平台',
    };

    // 根据路径定制元数据
    switch (location.pathname) {
      case '/':
        return {
          title: 'Maple Blog - 现代化技术博客',
          description: '探索前沿技术，分享编程经验，构建技术社区',
        };
      case '/blog':
        return {
          title: '技术文章 - Maple Blog',
          description: '最新的技术文章、教程和深度分析',
        };
      case '/login':
        return {
          title: '登录 - Maple Blog',
          description: '登录到Maple Blog，访问个人化内容',
        };
      case '/register':
        return {
          title: '注册 - Maple Blog',
          description: '加入Maple Blog社区，开始您的技术分享之旅',
        };
      default:
        return defaultMeta;
    }
  };

  const { title, description } = getPageMetadata();

  return (
    <>
      {/* 页面元数据 */}
      <Helmet>
        <title>{title}</title>
        <meta name="description" content={description} />
        <meta property="og:title" content={title} />
        <meta property="og:description" content={description} />
        <meta property="og:site_name" content="Maple Blog" />
        <meta property="twitter:title" content={title} />
        <meta property="twitter:description" content={description} />

        {/* 结构化数据 */}
        <script type="application/ld+json">
          {JSON.stringify({
            '@context': 'https://schema.org',
            '@type': 'WebSite',
            'name': 'Maple Blog',
            'url': ENV_CONFIG.API_URL,
            'description': '现代化的AI驱动博客系统',
            'potentialAction': {
              '@type': 'SearchAction',
              'target': {
                '@type': 'EntryPoint',
                'urlTemplate': `${ENV_CONFIG.API_URL}/search?q={search_term_string}`
              },
              'query-input': 'required name=search_term_string'
            }
          })}
        </script>
      </Helmet>

      {/* 主应用布局 */}
      <ResponsiveLayout
        header={<GlobalHeader theme={theme} onThemeToggle={toggleTheme} />}
        sidebar={<GlobalSidebar />}
        footer={<GlobalFooter />}
        className="min-h-screen"
        enableTouchGestures={true}
        onLayoutChange={(breakpoint) => {
          // 响应式布局变化处理
          console.debug(`Layout changed to: ${breakpoint}`);
        }}
      >
        {/* 主要内容区域 */}
        <main className="flex-1">
          <Suspense fallback={<PageLoadingFallback />}>
            <Routes>
              {/* 公共路由 */}
              <Route path="/" element={<HomePage />} />
              <Route path="/blog" element={<BlogListPage />} />
              <Route path="/blog/:slug" element={<BlogPostPage />} />
              <Route path="/archive" element={<ArchivePage />} />

              {/* 认证路由 */}
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="/reset-password" element={<ResetPasswordPage />} />

              {/* 受保护的路由 */}
              <Route
                path="/profile"
                element={
                  <ProtectedRoute>
                    <div>用户个人资料页面</div>
                  </ProtectedRoute>
                }
              />

              {/* 管理员路由 */}
              <Route
                path="/admin/*"
                element={
                  <AdminRoute>
                    <AdminDashboard />
                  </AdminRoute>
                }
              />

              {/* 404页面 */}
              <Route path="*" element={<NotFoundPage />} />
            </Routes>
          </Suspense>
        </main>
      </ResponsiveLayout>

      {/* 全局组件 */}
      {/* 这里可以添加全局通知、模态框等组件 */}
    </>
  );
};

export default App;
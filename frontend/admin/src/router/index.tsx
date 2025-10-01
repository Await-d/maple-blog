/* eslint-disable react-refresh/only-export-components */
import { Suspense, lazy } from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { Spin } from 'antd';
import AdminLayout from '@/layouts/AdminLayout';
import LoginPage from '@/pages/auth/Login';
import type { RouteConfig } from '@/types';

// 加载器组件
const PageLoader = () => (
  <div style={{
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    height: '400px'
  }}>
    <Spin size="large" tip="页面加载中..." />
  </div>
);

// 懒加载页面组件
const Dashboard = lazy(() => import('@/pages/Dashboard'));
const UserManagement = lazy(() => import('@/pages/users/UserManagement'));
const UserDetail = lazy(() => import('@/pages/users/UserDetail'));
const RoleManagement = lazy(() => import('@/pages/users/RoleManagement'));
const PostManagement = lazy(() => import('@/pages/Content/PostManagement'));
const PostEditor = lazy(() => import('@/pages/content/PostEditor'));
const CategoryManagement = lazy(() => import('@/pages/Content/CategoryManagement'));
const TagManagement = lazy(() => import('@/pages/content/TagManagement'));
const MediaLibrary = lazy(() => import('@/pages/Content/MediaLibrary'));
const Analytics = lazy(() => import('@/pages/Analytics'));
const ContentAnalytics = lazy(() => import('@/pages/Analytics/ContentAnalytics'));
const UserAnalytics = lazy(() => import('@/pages/Analytics/UserAnalytics'));
const SystemSettings = lazy(() => import('@/pages/System/Settings'));
const SystemMonitoring = lazy(() => import('@/pages/System/Monitoring'));
const SystemLogs = lazy(() => import('@/pages/System/Logs'));
const AuditLogs = lazy(() => import('@/pages/System/AuditLogs'));
const Profile = lazy(() => import('@/pages/profile/Profile'));
const NotFound = lazy(() => import('@/pages/error/NotFound'));

// 路由配置
export const routeConfigs: RouteConfig[] = [
  {
    path: '/',
    component: Dashboard,
    meta: {
      title: '仪表盘',
      requiresAuth: true,
      icon: '📊',
    },
  },
  {
    path: '/users',
    component: UserManagement,
    meta: {
      title: '用户管理',
      requiresAuth: true,
      permissions: ['user.read'],
      icon: '👥',
    },
    children: [
      {
        path: '/users/:id',
        component: UserDetail,
        meta: {
          title: '用户详情',
          requiresAuth: true,
          permissions: ['user.read'],
          hideInMenu: true,
        },
      },
    ],
  },
  {
    path: '/roles',
    component: RoleManagement,
    meta: {
      title: '角色管理',
      requiresAuth: true,
      permissions: ['role.read'],
      icon: '🔐',
    },
  },
  {
    path: '/content',
    component: PostManagement,
    meta: {
      title: '内容管理',
      requiresAuth: true,
      permissions: ['post.read'],
      icon: '📝',
    },
    children: [
      {
        path: '/content/posts',
        component: PostManagement,
        meta: {
          title: '文章管理',
          requiresAuth: true,
          permissions: ['post.read'],
        },
      },
      {
        path: '/content/posts/new',
        component: PostEditor,
        meta: {
          title: '新建文章',
          requiresAuth: true,
          permissions: ['post.create'],
          hideInMenu: true,
        },
      },
      {
        path: '/content/posts/:id/edit',
        component: PostEditor,
        meta: {
          title: '编辑文章',
          requiresAuth: true,
          permissions: ['post.update'],
          hideInMenu: true,
        },
      },
      {
        path: '/content/categories',
        component: CategoryManagement,
        meta: {
          title: '分类管理',
          requiresAuth: true,
          permissions: ['category.read'],
        },
      },
      {
        path: '/content/tags',
        component: TagManagement,
        meta: {
          title: '标签管理',
          requiresAuth: true,
          permissions: ['tag.read'],
        },
      },
      {
        path: '/content/media',
        component: MediaLibrary,
        meta: {
          title: '媒体库',
          requiresAuth: true,
          permissions: ['media.read'],
        },
      },
    ],
  },
  {
    path: '/analytics',
    component: Analytics,
    meta: {
      title: '数据分析',
      requiresAuth: true,
      permissions: ['analytics.read'],
      icon: '📈',
    },
    children: [
      {
        path: '/analytics/overview',
        component: Analytics,
        meta: {
          title: '概览',
          requiresAuth: true,
          permissions: ['analytics.read'],
        },
      },
      {
        path: '/analytics/content',
        component: ContentAnalytics,
        meta: {
          title: '内容分析',
          requiresAuth: true,
          permissions: ['analytics.read'],
        },
      },
      {
        path: '/analytics/users',
        component: UserAnalytics,
        meta: {
          title: '用户分析',
          requiresAuth: true,
          permissions: ['analytics.read'],
        },
      },
    ],
  },
  {
    path: '/system',
    component: SystemSettings,
    meta: {
      title: '系统管理',
      requiresAuth: true,
      permissions: ['system.read'],
      icon: '⚙️',
    },
    children: [
      {
        path: '/system/settings',
        component: SystemSettings,
        meta: {
          title: '系统设置',
          requiresAuth: true,
          permissions: ['system.update'],
        },
      },
      {
        path: '/system/monitoring',
        component: SystemMonitoring,
        meta: {
          title: '系统监控',
          requiresAuth: true,
          permissions: ['system.read'],
        },
      },
      {
        path: '/system/logs',
        component: SystemLogs,
        meta: {
          title: '系统日志',
          requiresAuth: true,
          permissions: ['system.read'],
        },
      },
      {
        path: '/system/audit',
        component: AuditLogs,
        meta: {
          title: '审计日志',
          requiresAuth: true,
          permissions: ['audit.read'],
        },
      },
    ],
  },
  {
    path: '/profile',
    component: Profile,
    meta: {
      title: '个人资料',
      requiresAuth: true,
      hideInMenu: true,
    },
  },
];

// 创建路由
export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/',
    element: <AdminLayout />,
    children: [
      {
        index: true,
        element: <Navigate to="/dashboard" replace />,
      },
      {
        path: 'dashboard',
        element: (
          <Suspense fallback={<PageLoader />}>
            <Dashboard />
          </Suspense>
        ),
      },
      {
        path: 'users',
        element: (
          <Suspense fallback={<PageLoader />}>
            <UserManagement />
          </Suspense>
        ),
      },
      {
        path: 'users/:id',
        element: (
          <Suspense fallback={<PageLoader />}>
            <UserDetail />
          </Suspense>
        ),
      },
      {
        path: 'roles',
        element: (
          <Suspense fallback={<PageLoader />}>
            <RoleManagement />
          </Suspense>
        ),
      },
      {
        path: 'content',
        children: [
          {
            index: true,
            element: <Navigate to="/content/posts" replace />,
          },
          {
            path: 'posts',
            element: (
              <Suspense fallback={<PageLoader />}>
                <PostManagement />
              </Suspense>
            ),
          },
          {
            path: 'posts/new',
            element: (
              <Suspense fallback={<PageLoader />}>
                <PostEditor />
              </Suspense>
            ),
          },
          {
            path: 'posts/:id/edit',
            element: (
              <Suspense fallback={<PageLoader />}>
                <PostEditor />
              </Suspense>
            ),
          },
          {
            path: 'categories',
            element: (
              <Suspense fallback={<PageLoader />}>
                <CategoryManagement />
              </Suspense>
            ),
          },
          {
            path: 'tags',
            element: (
              <Suspense fallback={<PageLoader />}>
                <TagManagement />
              </Suspense>
            ),
          },
          {
            path: 'media',
            element: (
              <Suspense fallback={<PageLoader />}>
                <MediaLibrary />
              </Suspense>
            ),
          },
        ],
      },
      {
        path: 'analytics',
        children: [
          {
            index: true,
            element: <Navigate to="/analytics/overview" replace />,
          },
          {
            path: 'overview',
            element: (
              <Suspense fallback={<PageLoader />}>
                <Analytics />
              </Suspense>
            ),
          },
          {
            path: 'content',
            element: (
              <Suspense fallback={<PageLoader />}>
                <ContentAnalytics />
              </Suspense>
            ),
          },
          {
            path: 'users',
            element: (
              <Suspense fallback={<PageLoader />}>
                <UserAnalytics />
              </Suspense>
            ),
          },
        ],
      },
      {
        path: 'system',
        children: [
          {
            index: true,
            element: <Navigate to="/system/settings" replace />,
          },
          {
            path: 'settings',
            element: (
              <Suspense fallback={<PageLoader />}>
                <SystemSettings />
              </Suspense>
            ),
          },
          {
            path: 'monitoring',
            element: (
              <Suspense fallback={<PageLoader />}>
                <SystemMonitoring />
              </Suspense>
            ),
          },
          {
            path: 'logs',
            element: (
              <Suspense fallback={<PageLoader />}>
                <SystemLogs />
              </Suspense>
            ),
          },
          {
            path: 'audit',
            element: (
              <Suspense fallback={<PageLoader />}>
                <AuditLogs />
              </Suspense>
            ),
          },
        ],
      },
      {
        path: 'profile',
        element: (
          <Suspense fallback={<PageLoader />}>
            <Profile />
          </Suspense>
        ),
      },
      {
        path: '*',
        element: (
          <Suspense fallback={<PageLoader />}>
            <NotFound />
          </Suspense>
        ),
      },
    ],
  },
]);

export default router;

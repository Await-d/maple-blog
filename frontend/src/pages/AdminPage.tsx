/**
 * AdminPage - Main admin entry point with dashboard overview
 * Provides comprehensive administrative interface with role-based access
 */

import React, { useCallback, useMemo } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Helmet } from 'react-helmet-async';
import { useLogger } from '@/utils/logger';
import { adminApi } from '@/services/admin/adminApi';
import { analytics } from '@/services/analytics';
import { LoadingSpinner } from '@/components/ui/LoadingSpinner';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { useAuth } from '@/hooks/useAuth';
import { ProtectedRoute } from '@/components/common/ProtectedRoute';
import type { SystemHealth } from '@/types/admin';
import { UserRole } from '@/types/admin';

interface AdminPageProps {
  className?: string;
}

interface NavigationItem {
  id: string;
  label: string;
  path: string;
  icon: string;
  badge?: string | number;
  requiredRole: UserRole;
  description: string;
}

const ADMIN_NAVIGATION: NavigationItem[] = [
  {
    id: 'dashboard',
    label: 'Dashboard',
    path: '/admin',
    icon: '📊',
    requiredRole: UserRole.Admin,
    description: 'Overview of system metrics and activity'
  },
  {
    id: 'content',
    label: 'Content Management',
    path: '/admin/content',
    icon: '📝',
    requiredRole: UserRole.Author,
    description: 'Manage blog posts, categories, and tags'
  },
  {
    id: 'users',
    label: 'User Management',
    path: '/admin/users',
    icon: '👥',
    requiredRole: UserRole.Admin,
    description: 'Manage user accounts and permissions'
  },
  {
    id: 'analytics',
    label: 'Analytics',
    path: '/admin/analytics',
    icon: '📈',
    requiredRole: UserRole.Admin,
    description: 'View site analytics and performance metrics'
  },
  {
    id: 'settings',
    label: 'System Settings',
    path: '/admin/settings',
    icon: '⚙️',
    requiredRole: UserRole.Admin,
    description: 'Configure system settings and preferences'
  },
  {
    id: 'seed-data',
    label: 'Seed Data',
    path: '/admin/seed-data',
    icon: '🌱',
    requiredRole: UserRole.Admin,
    description: 'Manage development and test data'
  }
];

export const AdminPage: React.FC<AdminPageProps> = ({ className = '' }) => {
  const location = useLocation();
  const navigate = useNavigate();
  const logger = useLogger('AdminPage');
  const { user, hasRole } = useAuth();

  // Check if user has admin access
  const hasAdminAccess = useMemo(() => {
    return user && (hasRole(UserRole.Admin) || hasRole(UserRole.Author) || hasRole(UserRole.Author));
  }, [user, hasRole]);

  // Filter navigation items based on user role
  const availableNavItems = useMemo(() => {
    if (!user) return [];
    return ADMIN_NAVIGATION.filter(item => hasRole(item.requiredRole));
  }, [user, hasRole]);

  // Determine if we're on dashboard (exact match)
  const isOnDashboard = location.pathname === '/admin';

  // Fetch admin dashboard data
  const {
    data: dashboardData,
    isLoading: dashboardLoading,
    error: dashboardError,
    refetch: refetchDashboard
  } = useQuery({
    queryKey: ['admin-dashboard'],
    queryFn: async () => {
      logger.startTimer('fetch_admin_dashboard');
      
      try {
        const [stats, health] = await Promise.all([
          adminApi.getStats(),
          adminApi.getSystemHealth()
        ]);
        
        logger.endTimer('fetch_admin_dashboard');

        return { stats, health };
      } catch (error) {
        logger.logApiError('GET', '/api/admin/dashboard', error as Error);
        throw error;
      }
    },
    enabled: isOnDashboard && Boolean(hasAdminAccess),
    staleTime: 2 * 60 * 1000, // 2 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
    retry: 3
  });

  // Track admin page access
  React.useEffect(() => {
    if (hasAdminAccess) {
      logger.logUserAction('admin_page_access', 'admin_navigation', {
        path: location.pathname,
        user_id: user?.id,
        user_role: user?.role
      });

      analytics.track('admin_page_view', 'AdminInterface');
    }
  }, [location.pathname, hasAdminAccess, user, hasRole, logger]);

  // Handle navigation
  const handleNavigation = useCallback((path: string, label: string) => {
    logger.logUserAction('admin_nav_click', 'admin_navigation', {
      target_path: path,
      nav_label: label
    });

    analytics.track('admin_navigation', 'AdminInterface', {
      targetPath: path,
      navLabel: label
    });

    navigate(path);
  }, [logger, navigate]);

  // Handle retry
  const handleRetry = useCallback(() => {
    logger.logUserAction('retry_admin_dashboard', 'error_recovery');
    refetchDashboard();
  }, [logger, refetchDashboard]);

  // Render access denied
  if (!hasAdminAccess) {
    return (
      <ProtectedRoute
        roles={[UserRole.Admin]}
        fallback={
          <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center">
            <div className="text-center">
              <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
                Access Denied
              </h1>
              <p className="text-gray-600 dark:text-gray-400 mb-6">
                You don&apos;t have permission to access the admin panel.
              </p>
              <Button onClick={() => navigate('/')}>
                Go Home
              </Button>
            </div>
          </div>
        }
      >
        <div />
      </ProtectedRoute>
    );
  }

  // Get system health status
  const getHealthStatus = (health?: SystemHealth) => {
    if (!health) return { status: 'unknown', color: 'gray' };
    
    const issues = Object.values(health).filter(metric => metric.status === 'error').length;
    const warnings = Object.values(health).filter(metric => metric.status === 'warning').length;
    
    if (issues > 0) return { status: 'critical', color: 'red' };
    if (warnings > 0) return { status: 'warning', color: 'yellow' };
    return { status: 'healthy', color: 'green' };
  };

  const healthStatus = getHealthStatus(dashboardData?.health);

  return (
    <div className={`min-h-screen bg-gray-50 dark:bg-gray-900 ${className}`}>
      <Helmet>
        <title>Admin Panel | Maple Blog</title>
        <meta name="description" content="Administrative interface for Maple Blog" />
        <meta name="robots" content="noindex, nofollow" />
      </Helmet>

      <div className="flex min-h-screen">
        {/* Sidebar Navigation */}
        <aside className="w-64 bg-white dark:bg-gray-800 shadow-sm border-r border-gray-200 dark:border-gray-700">
          <div className="p-6">
            <h1 className="text-xl font-bold text-gray-900 dark:text-white mb-2">
              Admin Panel
            </h1>
            <p className="text-sm text-gray-600 dark:text-gray-400">
              Welcome back, {user?.displayName}
            </p>
          </div>

          <nav className="px-4 pb-4">
            <ul className="space-y-2">
              {availableNavItems.map(item => {
                const isActive = location.pathname === item.path || 
                  (item.path !== '/admin' && location.pathname.startsWith(item.path));

                return (
                  <li key={item.id}>
                    <button
                      onClick={() => handleNavigation(item.path, item.label)}
                      className={`w-full text-left px-4 py-3 rounded-lg transition-colors flex items-center gap-3 ${
                        isActive
                          ? 'bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300 border border-blue-200 dark:border-blue-700'
                          : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
                      }`}
                    >
                      <span className="text-lg">{item.icon}</span>
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <span className="font-medium">{item.label}</span>
                          {item.badge && (
                            <Badge variant="secondary" className="text-xs">
                              {item.badge}
                            </Badge>
                          )}
                        </div>
                        <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                          {item.description}
                        </p>
                      </div>
                    </button>
                  </li>
                );
              })}
            </ul>
          </nav>

          {/* System Health Indicator */}
          {isOnDashboard && dashboardData?.health && (
            <div className="px-4 pb-4">
              <Separator className="mb-4" />
              <div className="flex items-center gap-2 text-sm">
                <div className={`w-2 h-2 rounded-full bg-${healthStatus.color}-500`} />
                <span className="text-gray-600 dark:text-gray-400">
                  System: {healthStatus.status}
                </span>
              </div>
            </div>
          )}
        </aside>

        {/* Main Content */}
        <main className="flex-1">
          {isOnDashboard ? (
            /* Dashboard Overview */
            <div className="p-8">
              <div className="mb-8">
                <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
                  Dashboard Overview
                </h2>
                <p className="text-gray-600 dark:text-gray-400">
                  Monitor your site&apos;s performance and manage administrative tasks
                </p>
              </div>

              {dashboardLoading ? (
                <div className="flex items-center justify-center py-12">
                  <LoadingSpinner size="lg" />
                </div>
              ) : dashboardError ? (
                <div className="flex flex-col items-center justify-center py-12">
                  <Alert className="mb-6 max-w-md">
                    <h3 className="font-semibold mb-2">Unable to Load Dashboard</h3>
                    <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
                      We&apos;re having trouble loading the admin dashboard data.
                    </p>
                    <Button onClick={handleRetry}>
                      Try Again
                    </Button>
                  </Alert>
                </div>
              ) : dashboardData ? (
                <div className="space-y-8">
                  {/* Quick Stats */}
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                    <div className="bg-white dark:bg-gray-800 rounded-lg p-6 shadow-sm">
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                            Total Posts
                          </p>
                          <p className="text-2xl font-bold text-gray-900 dark:text-white">
                            {dashboardData.stats.posts.total}
                          </p>
                        </div>
                        <span className="text-2xl">📝</span>
                      </div>
                      <div className="mt-4 text-sm">
                        <span className="text-green-600 dark:text-green-400">
                          +{dashboardData.stats.posts.thisWeek} this week
                        </span>
                      </div>
                    </div>

                    <div className="bg-white dark:bg-gray-800 rounded-lg p-6 shadow-sm">
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                            Total Users
                          </p>
                          <p className="text-2xl font-bold text-gray-900 dark:text-white">
                            {dashboardData.stats.users.total}
                          </p>
                        </div>
                        <span className="text-2xl">👥</span>
                      </div>
                      <div className="mt-4 text-sm">
                        <span className="text-green-600 dark:text-green-400">
                          +{dashboardData.stats.users.thisWeek} this week
                        </span>
                      </div>
                    </div>

                    <div className="bg-white dark:bg-gray-800 rounded-lg p-6 shadow-sm">
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                            Page Views
                          </p>
                          <p className="text-2xl font-bold text-gray-900 dark:text-white">
                            {dashboardData.stats.pageViews.total.toLocaleString()}
                          </p>
                        </div>
                        <span className="text-2xl">📈</span>
                      </div>
                      <div className="mt-4 text-sm">
                        <span className="text-green-600 dark:text-green-400">
                          +{dashboardData.stats.pageViews.thisWeek.toLocaleString()} this week
                        </span>
                      </div>
                    </div>

                    <div className="bg-white dark:bg-gray-800 rounded-lg p-6 shadow-sm">
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                            Comments
                          </p>
                          <p className="text-2xl font-bold text-gray-900 dark:text-white">
                            {dashboardData.stats.comments.total}
                          </p>
                        </div>
                        <span className="text-2xl">💬</span>
                      </div>
                      <div className="mt-4 text-sm">
                        <span className="text-green-600 dark:text-green-400">
                          +{dashboardData.stats.comments.thisWeek} this week
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* System Health */}
                  <div className="bg-white dark:bg-gray-800 rounded-lg p-6 shadow-sm">
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                      System Health
                    </h3>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                      {Object.entries(dashboardData.health).map(([key, metric]) => (
                        <div key={key} className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-700 rounded-lg">
                          <span className="text-sm font-medium text-gray-700 dark:text-gray-300 capitalize">
                            {key.replace(/([A-Z])/g, ' $1').trim()}
                          </span>
                          <Badge
                            variant={metric.status === 'healthy' ? 'default' : metric.status === 'warning' ? 'secondary' : 'destructive'}
                          >
                            {metric.value} {metric.unit}
                          </Badge>
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Quick Actions */}
                  <div className="bg-white dark:bg-gray-800 rounded-lg p-6 shadow-sm">
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                      Quick Actions
                    </h3>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                      <Button
                        onClick={() => handleNavigation('/admin/content/new', 'Create New Post')}
                        className="justify-start"
                      >
                        📝 Create New Post
                      </Button>
                      <Button
                        variant="outline"
                        onClick={() => handleNavigation('/admin/users', 'Manage Users')}
                        className="justify-start"
                      >
                        👥 Manage Users
                      </Button>
                      <Button
                        variant="outline"
                        onClick={() => handleNavigation('/admin/analytics', 'View Analytics')}
                        className="justify-start"
                      >
                        📊 View Analytics
                      </Button>
                    </div>
                  </div>
                </div>
              ) : null}
            </div>
          ) : (
            /* Nested Routes */
            <Outlet />
          )}
        </main>
      </div>
    </div>
  );
};

export default AdminPage;
// @ts-nocheck
/**
 * GlobalSidebar - 全局侧边栏组件
 * 提供导航、快速访问和工具链接
 */

import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  Home,
  FileText,
  Archive,
  User,
  Settings,
  Search,
  BookOpen,
  Tag,
  Calendar,
  TrendingUp
} from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import { UserRole } from '../../types/auth';
import { cn } from '../../utils/cn';

interface SidebarLinkProps {
  to: string;
  icon: React.ReactNode;
  label: string;
  isActive?: boolean;
  className?: string;
}

const SidebarLink: React.FC<SidebarLinkProps> = ({
  to,
  icon,
  label,
  isActive = false,
  className,
}) => (
  <Link
    to={to}
    className={cn(
      'flex items-center space-x-3 px-4 py-2 rounded-lg transition-colors',
      'hover:bg-gray-100 dark:hover:bg-gray-800',
      {
        'bg-blue-50 text-blue-700 dark:bg-blue-900/20 dark:text-blue-300': isActive,
        'text-gray-700 dark:text-gray-300': !isActive,
      },
      className
    )}
  >
    <span className="w-5 h-5">{icon}</span>
    <span className="font-medium">{label}</span>
  </Link>
);

interface SidebarSectionProps {
  title: string;
  children: React.ReactNode;
}

const SidebarSection: React.FC<SidebarSectionProps> = ({ title, children }) => (
  <div className="mb-6">
    <h3 className="px-4 mb-2 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">
      {title}
    </h3>
    <nav className="space-y-1">
      {children}
    </nav>
  </div>
);

export const GlobalSidebar: React.FC = () => {
  const location = useLocation();
  const { isAuthenticated, user } = useAuth();

  const isActivePath = (path: string) => location.pathname === path;

  return (
    <div className="h-full overflow-y-auto bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-700">
      <div className="p-4">
        {/* 主导航 */}
        <SidebarSection title="主导航">
          <SidebarLink
            to="/"
            icon={<Home />}
            label="首页"
            isActive={isActivePath('/')}
          />
          <SidebarLink
            to="/blog"
            icon={<FileText />}
            label="文章"
            isActive={isActivePath('/blog')}
          />
          <SidebarLink
            to="/archive"
            icon={<Archive />}
            label="归档"
            isActive={isActivePath('/archive')}
          />
        </SidebarSection>

        {/* 发现 */}
        <SidebarSection title="发现">
          <SidebarLink
            to="/search"
            icon={<Search />}
            label="搜索"
            isActive={isActivePath('/search')}
          />
          <SidebarLink
            to="/categories"
            icon={<BookOpen />}
            label="分类"
            isActive={isActivePath('/categories')}
          />
          <SidebarLink
            to="/tags"
            icon={<Tag />}
            label="标签"
            isActive={isActivePath('/tags')}
          />
          <SidebarLink
            to="/trending"
            icon={<TrendingUp />}
            label="热门"
            isActive={isActivePath('/trending')}
          />
        </SidebarSection>

        {/* 用户区域 */}
        {isAuthenticated ? (
          <SidebarSection title="个人">
            <SidebarLink
              to="/profile"
              icon={<User />}
              label="个人资料"
              isActive={isActivePath('/profile')}
            />
            <SidebarLink
              to="/settings"
              icon={<Settings />}
              label="设置"
              isActive={isActivePath('/settings')}
            />
            {user?.role === UserRole.Admin && (
              <SidebarLink
                to="/admin"
                icon={<Settings />}
                label="管理后台"
                isActive={location.pathname.startsWith('/admin')}
              />
            )}
          </SidebarSection>
        ) : (
          <SidebarSection title="账户">
            <SidebarLink
              to="/login"
              icon={<User />}
              label="登录"
              isActive={isActivePath('/login')}
            />
          </SidebarSection>
        )}

        {/* 快速访问 */}
        <SidebarSection title="快速访问">
          <div className="space-y-2">
            <div className="px-4 py-2 text-sm text-gray-600 dark:text-gray-400">
              <div className="flex items-center justify-between">
                <span>最近访问</span>
                <Calendar className="w-4 h-4" />
              </div>
            </div>
            {/* 这里可以添加最近访问的文章链接 */}
            <div className="px-4 py-2 text-xs text-gray-500 dark:text-gray-500">
              暂无最近访问
            </div>
          </div>
        </SidebarSection>
      </div>
    </div>
  );
};

export default GlobalSidebar;
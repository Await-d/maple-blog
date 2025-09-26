/**
 * Navigation component - Main site navigation with responsive design
 * Features: Active link highlighting, category dropdown, mobile-friendly layout
 */

import React, { useState, useRef, useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { ChevronDown, Home, Archive, Tag, User, BookOpen, Info } from 'lucide-react';
import { useCategoryStats } from '../../services/home/homeApi';
import { cn } from '../../utils/cn';

interface NavigationProps {
  mobile?: boolean;
  onNavigate?: () => void;
  className?: string;
}

interface NavItem {
  label: string;
  href: string;
  icon?: React.ReactNode;
  children?: NavItem[];
  external?: boolean;
}

export const Navigation: React.FC<NavigationProps> = ({
  mobile = false,
  onNavigate,
  className,
}) => {
  const location = useLocation();
  const { data: categories } = useCategoryStats(false);
  const [activeDropdown, setActiveDropdown] = useState<string | null>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setActiveDropdown(null);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Close dropdown on route change
  useEffect(() => {
    setActiveDropdown(null);
  }, [location.pathname]);

  // Main navigation items
  const navItems: NavItem[] = [
    {
      label: '首页',
      href: '/',
      icon: <Home size={16} />,
    },
    {
      label: '分类',
      href: '/categories',
      icon: <Tag size={16} />,
      children: categories?.slice(0, 8).map(category => ({
        label: category.name,
        href: `/category/${category.slug}`,
      })) || [],
    },
    {
      label: '归档',
      href: '/archive',
      icon: <Archive size={16} />,
    },
    {
      label: '作者',
      href: '/authors',
      icon: <User size={16} />,
    },
    {
      label: '关于',
      href: '/about',
      icon: <Info size={16} />,
    },
  ];

  const isActive = (href: string): boolean => {
    if (href === '/') {
      return location.pathname === '/';
    }
    return location.pathname.startsWith(href);
  };

  const handleItemClick = (_href: string) => {
    if (onNavigate) {
      onNavigate();
    }
    setActiveDropdown(null);
  };

  const toggleDropdown = (label: string) => {
    if (mobile) return; // No dropdowns on mobile
    setActiveDropdown(activeDropdown === label ? null : label);
  };

  if (mobile) {
    return (
      <nav className={cn('space-y-2', className)} role="navigation">
        {navItems.map((item) => (
          <div key={item.label}>
            <Link
              to={item.href}
              onClick={() => handleItemClick(item.href)}
              className={cn(
                'flex items-center space-x-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors',
                isActive(item.href)
                  ? 'bg-orange-100 text-orange-700 dark:bg-orange-900/20 dark:text-orange-300'
                  : 'text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800'
              )}
            >
              {item.icon}
              <span>{item.label}</span>
            </Link>

            {/* Show children as sub-items on mobile */}
            {item.children && item.children.length > 0 && (
              <div className="ml-6 mt-2 space-y-1">
                {item.children.slice(0, 5).map((child) => (
                  <Link
                    key={child.href}
                    to={child.href}
                    onClick={() => handleItemClick(child.href)}
                    className={cn(
                      'block px-3 py-1 text-sm rounded-md transition-colors',
                      isActive(child.href)
                        ? 'bg-orange-50 text-orange-600 dark:bg-orange-900/10 dark:text-orange-400'
                        : 'text-gray-600 hover:bg-gray-50 dark:text-gray-400 dark:hover:bg-gray-800'
                    )}
                  >
                    {child.label}
                  </Link>
                ))}
                {item.children.length > 5 && (
                  <Link
                    to={item.href}
                    onClick={() => handleItemClick(item.href)}
                    className="block px-3 py-1 text-sm text-orange-600 dark:text-orange-400 hover:underline"
                  >
                    查看全部 ({item.children.length})
                  </Link>
                )}
              </div>
            )}
          </div>
        ))}
      </nav>
    );
  }

  return (
    <nav className={cn('flex items-center justify-center space-x-1', className)} role="navigation">
      {navItems.map((item) => (
        <div key={item.label} className="relative" ref={item.children ? dropdownRef : undefined}>
          {item.children && item.children.length > 0 ? (
            <button
              onClick={() => toggleDropdown(item.label)}
              className={cn(
                'flex items-center space-x-1 px-3 py-2 rounded-lg text-sm font-medium transition-colors',
                isActive(item.href)
                  ? 'bg-orange-100 text-orange-700 dark:bg-orange-900/20 dark:text-orange-300'
                  : 'text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800'
              )}
              aria-expanded={activeDropdown === item.label}
              aria-haspopup="true"
            >
              <span>{item.label}</span>
              <ChevronDown
                size={14}
                className={cn(
                  'transition-transform duration-200',
                  activeDropdown === item.label && 'rotate-180'
                )}
              />
            </button>
          ) : (
            <Link
              to={item.href}
              onClick={() => handleItemClick(item.href)}
              className={cn(
                'flex items-center space-x-1 px-3 py-2 rounded-lg text-sm font-medium transition-colors',
                isActive(item.href)
                  ? 'bg-orange-100 text-orange-700 dark:bg-orange-900/20 dark:text-orange-300'
                  : 'text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800'
              )}
            >
              <span>{item.label}</span>
            </Link>
          )}

          {/* Dropdown Menu */}
          {item.children && item.children.length > 0 && activeDropdown === item.label && (
            <div className="absolute top-full left-0 mt-2 w-56 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 py-2 z-50">
              <div className="px-3 py-2 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide border-b border-gray-200 dark:border-gray-700">
                热门分类
              </div>

              {item.children.slice(0, 8).map((child) => (
                <Link
                  key={child.href}
                  to={child.href}
                  onClick={() => handleItemClick(child.href)}
                  className={cn(
                    'flex items-center justify-between px-3 py-2 text-sm transition-colors',
                    isActive(child.href)
                      ? 'bg-orange-50 text-orange-600 dark:bg-orange-900/10 dark:text-orange-400'
                      : 'text-gray-700 hover:bg-gray-50 dark:text-gray-300 dark:hover:bg-gray-700'
                  )}
                >
                  <span>{child.label}</span>
                  {categories?.find(cat => cat.slug === child.href.split('/').pop())?.postCount && (
                    <span className="text-xs text-gray-500 dark:text-gray-400">
                      {categories.find(cat => cat.slug === child.href.split('/').pop())?.postCount}
                    </span>
                  )}
                </Link>
              ))}

              {item.children.length > 8 && (
                <>
                  <hr className="my-2 border-gray-200 dark:border-gray-700" />
                  <Link
                    to={item.href}
                    onClick={() => handleItemClick(item.href)}
                    className="flex items-center space-x-2 px-3 py-2 text-sm text-orange-600 dark:text-orange-400 hover:bg-gray-50 dark:hover:bg-gray-700"
                  >
                    <BookOpen size={14} />
                    <span>查看全部分类</span>
                  </Link>
                </>
              )}
            </div>
          )}
        </div>
      ))}
    </nav>
  );
};

/**
 * Usage:
 * <Navigation /> - Desktop horizontal navigation
 * <Navigation mobile onNavigate={() => closeMobileMenu()} /> - Mobile vertical navigation
 *
 * Features:
 * - Responsive design with different layouts for mobile and desktop
 * - Active link highlighting based on current route
 * - Category dropdown with post counts
 * - Smooth hover effects and transitions
 * - Accessibility support with ARIA attributes
 * - Auto-close dropdown on route change or click outside
 * - Mobile-friendly with expandable sub-menus
 * - Integration with category data from API
 */
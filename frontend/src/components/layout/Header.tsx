// @ts-nocheck
/**
 * Header component - Main site header with navigation, search, and user controls
 * Features: Responsive design, theme toggle, search integration, user authentication
 */

import React, { useState, useEffect, useRef } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Search, Menu, X, Moon, Sun, Monitor, User, LogOut, Settings, Bell } from 'lucide-react';
import { Navigation } from './Navigation';
import { SearchBox } from '../common/SearchBox';
import { ThemeToggle } from '../common/ThemeToggle';
import { Button } from '../ui/Button';
import { useAuth } from '../../hooks/useAuth';
import { UserRole } from '../../types/auth';
import { useHomeStore, useIsMobile, useCurrentTheme } from '../../stores/homeStore';
import { cn } from '../../utils/cn';

interface HeaderProps {
  className?: string;
  transparent?: boolean;
  fixed?: boolean;
}

export const Header: React.FC<HeaderProps> = ({
  className,
  transparent = false,
  fixed = true,
}) => {
  const navigate = useNavigate();
  const { user, isAuthenticated, logout } = useAuth();
  const isMobile = useIsMobile();
  const currentTheme = useCurrentTheme();
  const { sidebarCollapsed, setSidebarCollapsed } = useHomeStore();

  // Local state
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const [scrolled, setScrolled] = useState(false);

  // Refs for click outside detection
  const userMenuRef = useRef<HTMLDivElement>(null);
  const notificationRef = useRef<HTMLDivElement>(null);
  const searchRef = useRef<HTMLDivElement>(null);

  // Handle scroll effect
  useEffect(() => {
    if (!fixed) return;

    const handleScroll = () => {
      setScrolled(window.scrollY > 10);
    };

    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, [fixed]);

  // Handle click outside for dropdowns
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setUserMenuOpen(false);
      }
      if (notificationRef.current && !notificationRef.current.contains(event.target as Node)) {
        setNotificationsOpen(false);
      }
      if (searchRef.current && !searchRef.current.contains(event.target as Node)) {
        setSearchOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Close mobile menu when switching to desktop
  useEffect(() => {
    if (!isMobile) {
      setMobileMenuOpen(false);
    }
  }, [isMobile]);

  // Handle keyboard navigation
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.ctrlKey || event.metaKey) {
        switch (event.key) {
          case 'k':
            event.preventDefault();
            setSearchOpen(true);
            break;
          case '/':
            event.preventDefault();
            setSearchOpen(true);
            break;
        }
      }
      if (event.key === 'Escape') {
        setSearchOpen(false);
        setUserMenuOpen(false);
        setNotificationsOpen(false);
        setMobileMenuOpen(false);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  const handleLogout = async () => {
    try {
      await logout();
      navigate('/');
      setUserMenuOpen(false);
    } catch (error) {
      console.error('Logout failed:', error);
    }
  };

  const toggleMobileMenu = () => {
    setMobileMenuOpen(!mobileMenuOpen);
  };

  const toggleSearch = () => {
    setSearchOpen(!searchOpen);
  };

  const toggleUserMenu = () => {
    setUserMenuOpen(!userMenuOpen);
  };

  const toggleNotifications = () => {
    setNotificationsOpen(!notificationsOpen);
  };

  const headerClasses = cn(
    'w-full border-b transition-all duration-200 z-50',
    {
      'fixed top-0 left-0 right-0': fixed,
      'bg-white/80 backdrop-blur-sm border-gray-200':
        !transparent || scrolled,
      'bg-transparent border-transparent':
        transparent && !scrolled,
      'shadow-sm': scrolled,
    },
    'dark:bg-gray-900/80 dark:border-gray-800',
    className
  );

  return (
    <header className={headerClasses}>
      <div className="container mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Logo and Brand */}
          <div className="flex items-center space-x-4">
            {isMobile && (
              <Button
                variant="ghost"
                size="sm"
                onClick={toggleMobileMenu}
                className="p-2"
                aria-label="Toggle mobile menu"
              >
                {mobileMenuOpen ? <X size={20} /> : <Menu size={20} />}
              </Button>
            )}

            <Link
              to="/"
              className="flex items-center space-x-2 group"
              aria-label="Maple Blog Home"
            >
              <div className="w-8 h-8 bg-gradient-to-br from-orange-500 to-red-600 rounded-lg flex items-center justify-center text-white font-bold text-sm transform group-hover:scale-105 transition-transform">
                M
              </div>
              <span className="text-xl font-bold text-gray-900 dark:text-white hidden sm:block">
                Maple Blog
              </span>
            </Link>
          </div>

          {/* Desktop Navigation */}
          {!isMobile && (
            <div className="flex-1 max-w-2xl mx-8">
              <Navigation />
            </div>
          )}

          {/* Right Side Actions */}
          <div className="flex items-center space-x-2">
            {/* Search */}
            <div className="relative" ref={searchRef}>
              <Button
                variant="ghost"
                size="sm"
                onClick={toggleSearch}
                className="p-2"
                aria-label="Search"
              >
                <Search size={20} />
              </Button>

              {searchOpen && (
                <div className="absolute right-0 top-full mt-2 w-80 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 p-4 z-50">
                  <SearchBox
                    onClose={() => setSearchOpen(false)}
                    autoFocus
                  />
                </div>
              )}
            </div>

            {/* Theme Toggle */}
            <ThemeToggle />

            {/* Notifications (for authenticated users) */}
            {isAuthenticated && (
              <div className="relative" ref={notificationRef}>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={toggleNotifications}
                  className="p-2 relative"
                  aria-label="Notifications"
                >
                  <Bell size={20} />
                  <span className="absolute -top-1 -right-1 w-3 h-3 bg-red-500 rounded-full text-xs"></span>
                </Button>

                {notificationsOpen && (
                  <div className="absolute right-0 top-full mt-2 w-80 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 z-50">
                    <div className="p-4 border-b border-gray-200 dark:border-gray-700">
                      <h3 className="font-semibold text-gray-900 dark:text-white">通知</h3>
                    </div>
                    <div className="p-4 text-sm text-gray-600 dark:text-gray-400 text-center">
                      暂无新通知
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* User Menu */}
            {isAuthenticated ? (
              <div className="relative" ref={userMenuRef}>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={toggleUserMenu}
                  className="flex items-center space-x-2 p-2"
                  aria-label="User menu"
                >
                  {user?.avatar ? (
                    <img
                      src={user.avatar}
                      alt={user.displayName || user.userName}
                      className="w-8 h-8 rounded-full object-cover"
                    />
                  ) : (
                    <div className="w-8 h-8 bg-gray-300 dark:bg-gray-600 rounded-full flex items-center justify-center">
                      <User size={16} />
                    </div>
                  )}
                  {!isMobile && (
                    <span className="text-sm font-medium text-gray-700 dark:text-gray-300 max-w-24 truncate">
                      {user?.displayName || user?.userName}
                    </span>
                  )}
                </Button>

                {userMenuOpen && (
                  <div className="absolute right-0 top-full mt-2 w-48 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 py-2 z-50">
                    <Link
                      to="/profile"
                      className="flex items-center space-x-2 px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
                      onClick={() => setUserMenuOpen(false)}
                    >
                      <User size={16} />
                      <span>个人资料</span>
                    </Link>
                    <Link
                      to="/settings"
                      className="flex items-center space-x-2 px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
                      onClick={() => setUserMenuOpen(false)}
                    >
                      <Settings size={16} />
                      <span>设置</span>
                    </Link>
                    {user?.role === UserRole.Admin && (
                      <Link
                        to="/admin"
                        className="flex items-center space-x-2 px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
                        onClick={() => setUserMenuOpen(false)}
                      >
                        <Settings size={16} />
                        <span>管理面板</span>
                      </Link>
                    )}
                    <hr className="my-2 border-gray-200 dark:border-gray-700" />
                    <button
                      onClick={handleLogout}
                      className="flex items-center space-x-2 w-full px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-gray-100 dark:hover:bg-gray-700"
                    >
                      <LogOut size={16} />
                      <span>退出登录</span>
                    </button>
                  </div>
                )}
              </div>
            ) : (
              <div className="flex items-center space-x-2">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => navigate('/login')}
                >
                  登录
                </Button>
                <Button
                  variant="primary"
                  size="sm"
                  onClick={() => navigate('/register')}
                >
                  注册
                </Button>
              </div>
            )}
          </div>
        </div>

        {/* Mobile Navigation */}
        {isMobile && mobileMenuOpen && (
          <div className="border-t border-gray-200 dark:border-gray-700 py-4">
            <Navigation
              mobile
              onNavigate={() => setMobileMenuOpen(false)}
            />
          </div>
        )}
      </div>
    </header>
  );
};

/**
 * Usage:
 * <Header /> - Standard header
 * <Header transparent /> - Transparent header (for hero sections)
 * <Header fixed={false} /> - Non-fixed header
 *
 * Features:
 * - Responsive design with mobile hamburger menu
 * - Integrated search with keyboard shortcuts (Ctrl+K, Ctrl+/)
 * - Theme toggle with system preference detection
 * - User authentication state display
 * - Notifications for authenticated users
 * - Smooth scroll effects and backdrop blur
 * - Accessibility support with ARIA labels and keyboard navigation
 * - Click outside to close dropdowns
 * - Auto-close mobile menu on desktop resize
 */
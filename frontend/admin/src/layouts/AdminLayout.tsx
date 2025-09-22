// @ts-nocheck
import React, { useState, useEffect } from 'react';
import { Outlet, useNavigate } from 'react-router-dom';
import {
  Layout,
  theme,
  ConfigProvider,
  App,
  FloatButton,
} from 'antd';
import {
  CustomerServiceOutlined,
  QuestionCircleOutlined,
} from '@ant-design/icons';
import { Helmet } from 'react-helmet-async';

import Sidebar from '@/components/layout/Sidebar';
import Header from '@/components/layout/Header';
import Breadcrumb from '@/components/layout/Breadcrumb';
import { useAdminStore, useUser, useCollapsed, useNotifications, useTheme } from '@/stores/adminStore';
import { env } from '@/utils';

const { Content } = Layout;

const AdminLayout: React.FC = () => {
  const navigate = useNavigate();
  const { token } = theme.useToken();

  const user = useUser();
  const collapsed = useCollapsed();
  const notifications = useNotifications();
  const currentTheme = useTheme();
  const { setCollapsed, setTheme, logout, clearNotifications } = useAdminStore();

  const [isMobile, setIsMobile] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(collapsed);

  // 响应式检测
  useEffect(() => {
    const checkMobile = () => {
      const mobile = window.innerWidth < 992;
      setIsMobile(mobile);
      
      // 移动端自动收起侧边栏
      if (mobile && !sidebarCollapsed) {
        setSidebarCollapsed(true);
      }
    };

    checkMobile();
    window.addEventListener('resize', checkMobile);
    return () => window.removeEventListener('resize', checkMobile);
  }, [sidebarCollapsed]);

  // 同步collapsed状态
  useEffect(() => {
    setSidebarCollapsed(collapsed);
  }, [collapsed]);

  // 处理侧边栏折叠
  const handleSidebarCollapse = (newCollapsed: boolean) => {
    setSidebarCollapsed(newCollapsed);
    setCollapsed(newCollapsed);
  };

  // 处理主题切换
  const handleThemeChange = (isDark: boolean) => {
    setTheme(isDark ? 'dark' : 'light');
  };

  // 处理通知相关操作
  const handleNotificationRead = (notificationId: string) => {
    // 这里应该调用API标记通知为已读
    console.log('Mark notification as read:', notificationId);
  };

  const handleNotificationClear = () => {
    clearNotifications();
  };

  // 格式化通知数据
  const formattedNotifications = notifications.map(notification => ({
    id: notification.id,
    title: notification.title,
    content: notification.content,
    type: notification.type as 'info' | 'success' | 'warning' | 'error',
    time: new Date(notification.createdAt).toLocaleString(),
    read: false, // 这里应该从服务器获取
  }));

  // 键盘快捷键
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Ctrl/Cmd + B 切换侧边栏
      if ((e.ctrlKey || e.metaKey) && e.key === 'b') {
        e.preventDefault();
        handleSidebarCollapse(!sidebarCollapsed);
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [sidebarCollapsed]);

  // 主题配置
  const themeConfig = {
    algorithm: currentTheme === 'dark' ? theme.darkAlgorithm : theme.defaultAlgorithm,
    token: {
      colorPrimary: '#1890ff',
      borderRadius: 6,
      wireframe: false,
      ...(currentTheme === 'dark' && {
        colorBgContainer: '#1f1f1f',
        colorBgElevated: '#262626',
        colorBgLayout: '#141414',
      }),
    },
    components: {
      Layout: {
        siderBg: currentTheme === 'dark' ? '#1f1f1f' : '#ffffff',
        headerBg: currentTheme === 'dark' ? '#1f1f1f' : '#ffffff',
        bodyBg: currentTheme === 'dark' ? '#141414' : '#f5f5f5',
      },
      Menu: {
        itemBg: 'transparent',
        itemSelectedBg: currentTheme === 'dark' ? '#1890ff' : '#1890ff',
        itemSelectedColor: '#ffffff',
        itemHoverBg: currentTheme === 'dark' ? 'rgba(255, 255, 255, 0.08)' : 'rgba(24, 144, 255, 0.08)',
        itemHoverColor: currentTheme === 'dark' ? '#ffffff' : '#1890ff',
      },
    },
  };

  return (
    <ConfigProvider theme={themeConfig}>
      <App>
        <>
          <Helmet>
            <title>{env.appTitle}</title>
            <meta name="description" content="专业的博客管理后台系统" />
            <meta name="theme-color" content={currentTheme === 'dark' ? '#1f1f1f' : '#ffffff'} />
          </Helmet>

          <Layout className="admin-layout" style={{ minHeight: '100vh' }}>
            {/* 侧边栏 */}
            <Sidebar
              collapsed={sidebarCollapsed}
              onCollapse={handleSidebarCollapse}
              className={isMobile ? 'mobile-sidebar' : ''}
            />

            {/* 移动端遮罩层 */}
            {isMobile && !sidebarCollapsed && (
              <div
                className="sidebar-mask"
                onClick={() => handleSidebarCollapse(true)}
                style={{
                  position: 'fixed',
                  top: 0,
                  left: 0,
                  right: 0,
                  bottom: 0,
                  background: 'rgba(0, 0, 0, 0.45)',
                  zIndex: 1000,
                }}
              />
            )}

            <Layout style={{ marginLeft: isMobile ? 0 : undefined }}>
              {/* 顶部导航 */}
              <Header
                collapsed={sidebarCollapsed}
                onCollapse={handleSidebarCollapse}
                user={user ? {
                  id: user.id,
                  username: user.username,
                  displayName: user.displayName,
                  email: user.email,
                  avatar: user.avatar,
                  role: user.roles[0]?.name,
                } : undefined}
                notifications={formattedNotifications}
                onNotificationRead={handleNotificationRead}
                onNotificationClear={handleNotificationClear}
                isDarkMode={currentTheme === 'dark'}
                onThemeChange={handleThemeChange}
              />

              {/* 面包屑导航 */}
              <div
                style={{
                  background: token.colorBgContainer,
                  padding: '12px 24px',
                  borderBottom: `1px solid ${token.colorBorderSecondary}`,
                }}
              >
                <Breadcrumb showIcon={!isMobile} />
              </div>

              {/* 内容区域 */}
              <Content
                className="admin-layout-content"
                style={{
                  padding: isMobile ? '16px 12px' : '24px',
                  background: token.colorBgLayout,
                  minHeight: 'calc(100vh - 64px - 57px)', // 减去header和breadcrumb高度
                  overflow: 'auto',
                }}
              >
                <div
                  className="content-wrapper"
                  style={{
                    background: token.colorBgContainer,
                    borderRadius: token.borderRadius,
                    padding: isMobile ? '16px' : '24px',
                    minHeight: '100%',
                    boxShadow: '0 2px 8px 0 rgba(29, 35, 41, 0.05)',
                  }}
                >
                  <div className="fade-in">
                    <Outlet />
                  </div>
                </div>
              </Content>
            </Layout>
          </Layout>

          {/* 浮动按钮 */}
          <FloatButton.Group
            trigger="hover"
            type="primary"
            style={{ right: 24, bottom: 24 }}
            icon={<CustomerServiceOutlined />}
          >
            <FloatButton
              icon={<QuestionCircleOutlined />}
              tooltip="帮助文档"
              onClick={() => console.log('打开帮助文档')}
            />
            <FloatButton
              icon={<CustomerServiceOutlined />}
              tooltip="在线客服"
              onClick={() => console.log('打开在线客服')}
            />
          </FloatButton.Group>

          {/* 全局样式 */}
          <style>{`
            .admin-layout {
              position: relative;
            }

            .fade-in {
              animation: fadeIn 0.3s ease-in-out;
            }

            @keyframes fadeIn {
              from {
                opacity: 0;
                transform: translateY(8px);
              }
              to {
                opacity: 1;
                transform: translateY(0);
              }
            }

            /* 移动端侧边栏样式 */
            .mobile-sidebar {
              position: fixed !important;
              z-index: 1001;
              height: 100vh;
              left: 0;
              top: 0;
            }

            /* 滚动条样式 */
            .admin-layout-content::-webkit-scrollbar {
              width: 6px;
            }

            .admin-layout-content::-webkit-scrollbar-track {
              background: ${token.colorBgLayout};
            }

            .admin-layout-content::-webkit-scrollbar-thumb {
              background: ${token.colorBorderSecondary};
              border-radius: 3px;
            }

            .admin-layout-content::-webkit-scrollbar-thumb:hover {
              background: ${token.colorBorder};
            }

            /* 响应式设计 */
            @media (max-width: 768px) {
              .content-wrapper {
                margin: 0 !important;
                border-radius: 0 !important;
                padding: 16px !important;
              }
            }

            /* 暗色主题下的特殊样式 */
            [data-theme="dark"] .admin-layout {
              background: #141414;
            }

            [data-theme="dark"] .sidebar-mask {
              background: rgba(0, 0, 0, 0.65);
            }

            /* 打印样式 */
            @media print {
              .admin-layout .ant-layout-sider,
              .admin-layout .ant-layout-header,
              .ant-float-btn-group {
                display: none !important;
              }

              .admin-layout-content {
                margin: 0 !important;
                padding: 0 !important;
              }

              .content-wrapper {
                box-shadow: none !important;
                border-radius: 0 !important;
              }
            }

            /* 无障碍支持 */
            @media (prefers-reduced-motion: reduce) {
              .fade-in {
                animation: none;
              }

              .admin-layout *,
              .admin-layout *::before,
              .admin-layout *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
              }
            }
          `}</style>
        </>
      </App>
    </ConfigProvider>
  );
};

export default AdminLayout;
import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  Layout,
  Menu,
  Typography,
  theme,
  Tooltip,
} from 'antd';
import {
  DashboardOutlined,
  UserOutlined,
  FileTextOutlined,
  BarChartOutlined,
  SettingOutlined,
  SafetyOutlined,
  AuditOutlined,
  TeamOutlined,
  TagsOutlined,
  FolderOutlined,
  FileImageOutlined,
  PieChartOutlined,
  LineChartOutlined,
  MonitorOutlined,
  FileSearchOutlined,
  GlobalOutlined,
} from '@ant-design/icons';
import type { MenuProps } from 'antd';

const { Sider } = Layout;
const { Text } = Typography;

// 菜单项类型
type MenuItem = Required<MenuProps>['items'][number];

// 创建菜单项的辅助函数
function getItem(
  label: React.ReactNode,
  key: React.Key,
  icon?: React.ReactNode,
  children?: MenuItem[],
  type?: 'group'
): MenuItem {
  return {
    key,
    icon,
    children,
    label,
    type,
  } as MenuItem;
}

// 菜单配置
const menuItems: MenuItem[] = [
  getItem('仪表盘', '/dashboard', <DashboardOutlined />),
  
  getItem('用户管理', 'users-group', <UserOutlined />, [
    getItem('用户列表', '/users', <TeamOutlined />),
    getItem('角色管理', '/roles', <SafetyOutlined />),
  ]),
  
  getItem('内容管理', 'content-group', <FileTextOutlined />, [
    getItem('文章管理', '/content/posts', <FileTextOutlined />),
    getItem('分类管理', '/content/categories', <FolderOutlined />),
    getItem('标签管理', '/content/tags', <TagsOutlined />),
    getItem('媒体库', '/content/media', <FileImageOutlined />),
  ]),
  
  getItem('数据分析', 'analytics-group', <BarChartOutlined />, [
    getItem('概览', '/analytics/overview', <PieChartOutlined />),
    getItem('内容分析', '/analytics/content', <LineChartOutlined />),
    getItem('用户分析', '/analytics/users', <UserOutlined />),
  ]),
  
  getItem('系统管理', 'system-group', <SettingOutlined />, [
    getItem('系统设置', '/system/settings', <GlobalOutlined />),
    getItem('系统监控', '/system/monitoring', <MonitorOutlined />),
    getItem('系统日志', '/system/logs', <FileSearchOutlined />),
    getItem('审计日志', '/system/audit', <AuditOutlined />),
  ]),
];

interface SidebarProps {
  collapsed: boolean;
  onCollapse: (collapsed: boolean) => void;
  className?: string;
}

const Sidebar: React.FC<SidebarProps> = ({
  collapsed,
  onCollapse,
  className = '',
}) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { token } = theme.useToken();

  const [selectedKeys, setSelectedKeys] = useState<string[]>([]);
  const [openKeys, setOpenKeys] = useState<string[]>([]);

  // 根据当前路径设置菜单选中状态
  useEffect(() => {
    const pathname = location.pathname;
    setSelectedKeys([pathname]);

    // 设置展开的菜单项
    const findParentKey = (path: string): string | undefined => {
      if (path.startsWith('/users') || path.startsWith('/roles')) {
        return 'users-group';
      }
      if (path.startsWith('/content')) {
        return 'content-group';
      }
      if (path.startsWith('/analytics')) {
        return 'analytics-group';
      }
      if (path.startsWith('/system')) {
        return 'system-group';
      }
      return undefined;
    };

    const parentKey = findParentKey(pathname);
    if (parentKey && !collapsed) {
      setOpenKeys([parentKey]);
    }
  }, [location.pathname, collapsed]);

  // 处理菜单点击
  const handleMenuClick: MenuProps['onClick'] = ({ key }) => {
    // 只有叶子节点才进行导航
    if (typeof key === 'string' && key.startsWith('/')) {
      navigate(key);
    }
  };

  // 处理菜单展开/收起
  const handleOpenChange = (keys: string[]) => {
    if (!collapsed) {
      setOpenKeys(keys);
    }
  };

  // 侧边栏折叠时，清空展开的菜单
  useEffect(() => {
    if (collapsed) {
      setOpenKeys([]);
    }
  }, [collapsed]);

  return (
    <Sider
      trigger={null}
      collapsible
      collapsed={collapsed}
      onCollapse={onCollapse}
      width={280}
      collapsedWidth={64}
      className={`admin-sidebar ${className}`}
      style={{
        background: token.colorBgContainer,
        borderRight: `1px solid ${token.colorBorderSecondary}`,
        boxShadow: '2px 0 8px 0 rgba(29, 35, 41, 0.05)',
        transition: 'all 0.2s cubic-bezier(0.645, 0.045, 0.355, 1)',
      }}
      breakpoint="lg"
      onBreakpoint={(broken) => {
        // 响应式断点处理
        if (broken) {
          onCollapse(true);
        }
      }}
    >
      {/* Logo 区域 */}
      <div
        className="sidebar-logo"
        style={{
          height: 64,
          padding: collapsed ? '16px 12px' : '16px 24px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: collapsed ? 'center' : 'flex-start',
          borderBottom: `1px solid ${token.colorBorderSecondary}`,
          background: token.colorBgContainer,
          transition: 'all 0.2s',
        }}
      >
        {collapsed ? (
          <Tooltip title="Maple Blog 管理后台" placement="right">
            <Text
              style={{
                fontSize: '28px',
                lineHeight: 1,
                display: 'block',
              }}
            >
              🍁
            </Text>
          </Tooltip>
        ) : (
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <Text
              style={{
                fontSize: '28px',
                lineHeight: 1,
              }}
            >
              🍁
            </Text>
            <div>
              <Text
                style={{
                  fontSize: '18px',
                  fontWeight: 600,
                  color: token.colorPrimary,
                  lineHeight: 1.2,
                  display: 'block',
                }}
              >
                Maple Blog
              </Text>
              <Text
                style={{
                  fontSize: '12px',
                  color: token.colorTextSecondary,
                  lineHeight: 1,
                }}
              >
                管理后台
              </Text>
            </div>
          </div>
        )}
      </div>

      {/* 菜单区域 */}
      <div
        className="sidebar-menu"
        style={{
          height: 'calc(100vh - 64px)',
          overflow: 'hidden auto',
          padding: '8px 0',
        }}
      >
        <Menu
          mode="inline"
          selectedKeys={selectedKeys}
          openKeys={openKeys}
          items={menuItems}
          onClick={handleMenuClick}
          onOpenChange={handleOpenChange}
          inlineCollapsed={collapsed}
          style={{
            border: 'none',
            background: 'transparent',
          }}
          theme="light"
          // 自定义样式
          className="sidebar-menu-content"
        />
      </div>

      {/* 底部信息 */}
      {!collapsed && (
        <div
          style={{
            position: 'absolute',
            bottom: 0,
            left: 0,
            right: 0,
            padding: '16px 24px',
            borderTop: `1px solid ${token.colorBorderSecondary}`,
            background: token.colorBgContainer,
          }}
        >
          <Text
            style={{
              fontSize: '12px',
              color: token.colorTextTertiary,
              display: 'block',
              textAlign: 'center',
            }}
          >
            Maple Blog v1.0.0
          </Text>
        </div>
      )}

      {/* 自定义样式 */}
      <style>{`
        .sidebar-menu-content .ant-menu-item,
        .sidebar-menu-content .ant-menu-submenu-title {
          margin: 2px 8px;
          border-radius: 6px;
          height: 42px;
          line-height: 42px;
          transition: all 0.2s;
        }

        .sidebar-menu-content .ant-menu-item:hover,
        .sidebar-menu-content .ant-menu-submenu-title:hover {
          background-color: ${token.colorPrimaryBg};
          color: ${token.colorPrimary};
        }

        .sidebar-menu-content .ant-menu-item-selected {
          background-color: ${token.colorPrimary};
          color: ${token.colorWhite};
          font-weight: 500;
        }

        .sidebar-menu-content .ant-menu-item-selected .anticon,
        .sidebar-menu-content .ant-menu-item-selected .ant-menu-title-content {
          color: ${token.colorWhite};
        }

        .sidebar-menu-content .ant-menu-submenu-selected > .ant-menu-submenu-title {
          color: ${token.colorPrimary};
          background-color: ${token.colorPrimaryBg};
        }

        .sidebar-menu-content .ant-menu-item .anticon,
        .sidebar-menu-content .ant-menu-submenu-title .anticon {
          font-size: 16px;
          min-width: 16px;
        }

        .admin-sidebar.ant-layout-sider-collapsed .sidebar-menu-content .ant-menu-item,
        .admin-sidebar.ant-layout-sider-collapsed .sidebar-menu-content .ant-menu-submenu-title {
          margin: 2px 12px;
          padding-left: 16px !important;
          padding-right: 16px !important;
        }

        /* 响应式设计 */
        @media (max-width: 992px) {
          .admin-sidebar {
            position: fixed !important;
            z-index: 1001;
            height: 100vh;
          }
        }

        /* 滚动条样式 */
        .sidebar-menu::-webkit-scrollbar {
          width: 4px;
        }

        .sidebar-menu::-webkit-scrollbar-track {
          background: transparent;
        }

        .sidebar-menu::-webkit-scrollbar-thumb {
          background: ${token.colorBorderSecondary};
          border-radius: 2px;
        }

        .sidebar-menu::-webkit-scrollbar-thumb:hover {
          background: ${token.colorBorder};
        }
      `}</style>
    </Sider>
  );
};

export default Sidebar;
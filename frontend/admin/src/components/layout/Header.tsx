// @ts-nocheck
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Layout,
  Button,
  Avatar,
  Dropdown,
  Badge,
  Space,
  Typography,
  theme,
  Switch,
  Tooltip,
  Drawer,
  List,
  Tag,
  Divider,
  App,
} from 'antd';
import {
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  BellOutlined,
  LogoutOutlined,
  ProfileOutlined,
  SafetyOutlined,
  UserOutlined,
  SettingOutlined,
  SunOutlined,
  MoonOutlined,
  FullscreenOutlined,
  FullscreenExitOutlined,
  QuestionCircleOutlined,
  SearchOutlined,
  GlobalOutlined,
} from '@ant-design/icons';
import type { MenuProps } from 'antd';

const { Header: AntHeader } = Layout;
const { Text } = Typography;

interface Notification {
  id: string;
  title: string;
  content: string;
  type: 'info' | 'success' | 'warning' | 'error';
  time: string;
  read: boolean;
}

interface HeaderProps {
  collapsed: boolean;
  onCollapse: (collapsed: boolean) => void;
  user?: {
    id: string;
    username: string;
    displayName?: string;
    email?: string;
    avatar?: string;
    role?: string;
  };
  notifications?: Notification[];
  onNotificationRead?: (notificationId: string) => void;
  onNotificationClear?: () => void;
  isDarkMode?: boolean;
  onThemeChange?: (dark: boolean) => void;
  className?: string;
}

const Header: React.FC<HeaderProps> = ({
  collapsed,
  onCollapse,
  user,
  notifications = [],
  onNotificationRead,
  onNotificationClear,
  isDarkMode = false,
  onThemeChange,
  className = '',
}) => {
  const navigate = useNavigate();
  const { token } = theme.useToken();
  const { message } = App.useApp();

  const [notificationDrawerOpen, setNotificationDrawerOpen] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);

  // 未读通知数量
  const unreadCount = notifications.filter(n => !n.read).length;

  // 全屏切换
  const toggleFullscreen = () => {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen().then(() => {
        setIsFullscreen(true);
        message.success('已进入全屏模式');
      }).catch(() => {
        message.error('全屏模式不可用');
      });
    } else {
      document.exitFullscreen().then(() => {
        setIsFullscreen(false);
        message.success('已退出全屏模式');
      });
    }
  };

  // 用户下拉菜单
  const userMenuItems: MenuProps['items'] = [
    {
      key: 'profile',
      icon: <ProfileOutlined />,
      label: '个人资料',
      onClick: () => navigate('/profile'),
    },
    {
      key: 'security',
      icon: <SafetyOutlined />,
      label: '安全设置',
      onClick: () => navigate('/profile/security'),
    },
    {
      key: 'settings',
      icon: <SettingOutlined />,
      label: '偏好设置',
      onClick: () => navigate('/profile/settings'),
    },
    {
      type: 'divider',
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: '退出登录',
      danger: true,
      onClick: () => {
        // 这里应该调用logout方法
        message.success('已退出登录');
        navigate('/login');
      },
    },
  ];

  // 通知项渲染
  const renderNotificationItem = (item: Notification) => (
    <List.Item
      key={item.id}
      style={{
        cursor: 'pointer',
        background: item.read ? 'transparent' : token.colorPrimaryBg,
        padding: '12px 16px',
        borderRadius: '6px',
        margin: '4px 0',
      }}
      onClick={() => {
        if (!item.read && onNotificationRead) {
          onNotificationRead(item.id);
        }
      }}
    >
      <List.Item.Meta
        title={
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Text strong={!item.read}>{item.title}</Text>
            <Tag color={
              item.type === 'error' ? 'red' :
              item.type === 'warning' ? 'orange' :
              item.type === 'success' ? 'green' : 'blue'
            }>
              {item.type}
            </Tag>
          </div>
        }
        description={
          <div>
            <Text style={{ color: token.colorTextSecondary }}>{item.content}</Text>
            <br />
            <Text style={{ fontSize: '12px', color: token.colorTextTertiary }}>{item.time}</Text>
          </div>
        }
      />
    </List.Item>
  );

  // 键盘快捷键处理
  React.useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Ctrl/Cmd + K 打开搜索
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        message.info('搜索功能开发中...');
      }
      // F11 全屏切换
      if (e.key === 'F11') {
        e.preventDefault();
        toggleFullscreen();
      }
      // Ctrl/Cmd + Shift + L 切换主题
      if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'L') {
        e.preventDefault();
        onThemeChange?.(!isDarkMode);
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isDarkMode, onThemeChange]);

  // 监听全屏状态变化
  React.useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
    };

    document.addEventListener('fullscreenchange', handleFullscreenChange);
    return () => document.removeEventListener('fullscreenchange', handleFullscreenChange);
  }, []);

  return (
    <>
      <AntHeader
        className={`admin-header ${className}`}
        style={{
          background: token.colorBgContainer,
          borderBottom: `1px solid ${token.colorBorderSecondary}`,
          padding: '0 24px',
          height: 64,
          lineHeight: '64px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          position: 'sticky',
          top: 0,
          zIndex: 1000,
          boxShadow: '0 2px 8px 0 rgba(29, 35, 41, 0.05)',
        }}
      >
        {/* 左侧区域 */}
        <div style={{ display: 'flex', alignItems: 'center', flex: 1 }}>
          {/* 折叠按钮 */}
          <Tooltip title={`${collapsed ? '展开' : '收起'}侧边栏 (Ctrl+B)`}>
            <Button
              type="text"
              icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
              onClick={() => onCollapse(!collapsed)}
              style={{
                fontSize: '16px',
                width: 48,
                height: 48,
                marginRight: 16,
              }}
            />
          </Tooltip>

          {/* 搜索按钮 */}
          <Tooltip title="全局搜索 (Ctrl+K)">
            <Button
              type="text"
              icon={<SearchOutlined />}
              onClick={() => message.info('搜索功能开发中...')}
              style={{
                fontSize: '16px',
                width: 48,
                height: 48,
                marginRight: 8,
              }}
            />
          </Tooltip>
        </div>

        {/* 右侧操作区 */}
        <Space >
          {/* 语言切换 */}
          <Tooltip title="语言切换">
            <Button
              type="text"
              icon={<GlobalOutlined />}
              onClick={() => message.info('多语言功能开发中...')}
              style={{
                fontSize: '16px',
                width: 48,
                height: 48,
              }}
            />
          </Tooltip>

          {/* 主题切换 */}
          <Tooltip title={`切换到${isDarkMode ? '亮色' : '暗色'}主题 (Ctrl+Shift+L)`}>
            <Button
              type="text"
              icon={isDarkMode ? <SunOutlined /> : <MoonOutlined />}
              onClick={() => onThemeChange?.(!isDarkMode)}
              style={{
                fontSize: '16px',
                width: 48,
                height: 48,
              }}
            />
          </Tooltip>

          {/* 全屏切换 */}
          <Tooltip title={`${isFullscreen ? '退出' : '进入'}全屏 (F11)`}>
            <Button
              type="text"
              icon={isFullscreen ? <FullscreenExitOutlined /> : <FullscreenOutlined />}
              onClick={toggleFullscreen}
              style={{
                fontSize: '16px',
                width: 48,
                height: 48,
              }}
            />
          </Tooltip>

          {/* 帮助 */}
          <Tooltip title="帮助文档">
            <Button
              type="text"
              icon={<QuestionCircleOutlined />}
              onClick={() => message.info('帮助文档开发中...')}
              style={{
                fontSize: '16px',
                width: 48,
                height: 48,
              }}
            />
          </Tooltip>

          {/* 分割线 */}
          <Divider type="vertical" style={{ height: 32, margin: '0 8px' }} />

          {/* 通知 */}
          <Tooltip title="通知">
            <Badge count={unreadCount}  offset={[-2, 2]}>
              <Button
                type="text"
                icon={<BellOutlined />}
                onClick={() => setNotificationDrawerOpen(true)}
                style={{
                  fontSize: '16px',
                  width: 48,
                  height: 48,
                }}
              />
            </Badge>
          </Tooltip>

          {/* 用户信息 */}
          <Dropdown
            menu={{ items: userMenuItems }}
            placement="bottomRight"
            arrow
            trigger={['click']}
          >
            <div
              style={{
                cursor: 'pointer',
                padding: '8px 12px',
                borderRadius: '6px',
                transition: 'background-color 0.2s',
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
              }}
              className="user-dropdown"
            >
              <Avatar
                size={32}
                src={user?.avatar}
                icon={<UserOutlined />}
                style={{
                  backgroundColor: token.colorPrimary,
                }}
              />
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start' }}>
                <Text
                  style={{
                    fontSize: '14px',
                    fontWeight: 500,
                    lineHeight: 1.2,
                    maxWidth: 120,
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    whiteSpace: 'nowrap',
                  }}
                >
                  {user?.displayName || user?.username || '管理员'}
                </Text>
                {user?.role && (
                  <Text
                    style={{
                      fontSize: '12px',
                      color: token.colorTextSecondary,
                      lineHeight: 1,
                    }}
                  >
                    {user.role}
                  </Text>
                )}
              </div>
            </div>
          </Dropdown>
        </Space>
      </AntHeader>

      {/* 通知抽屉 */}
      <Drawer
        title={
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span>通知中心</span>
            {notifications.length > 0 && (
              <Button
                type="link"
                
                onClick={onNotificationClear}
              >
                全部清除
              </Button>
            )}
          </div>
        }
        open={notificationDrawerOpen}
        onClose={() => setNotificationDrawerOpen(false)}
        placement="right"
        width={400}
        bodyStyle={{ padding: 0 }}
      >
        {notifications.length === 0 ? (
          <div
            style={{
              textAlign: 'center',
              padding: '40px 20px',
              color: token.colorTextSecondary,
            }}
          >
            <BellOutlined style={{ fontSize: '48px', marginBottom: '16px' }} />
            <div>暂无通知</div>
          </div>
        ) : (
          <List
            dataSource={notifications}
            renderItem={renderNotificationItem}
            style={{ padding: '8px' }}
          />
        )}
      </Drawer>

      {/* 自定义样式 */}
      <style>{`
        .user-dropdown:hover {
          background-color: ${token.colorBgTextHover} !important;
        }

        .admin-header .ant-btn:hover {
          background-color: ${token.colorBgTextHover};
        }

        /* 响应式设计 */
        @media (max-width: 768px) {
          .admin-header {
            padding: 0 16px !important;
          }
          
          .user-dropdown > div:last-child {
            display: none;
          }
        }

        @media (max-width: 576px) {
          .admin-header .ant-space > .ant-space-item:nth-child(-n+3) {
            display: none;
          }
        }
      `}</style>
    </>
  );
};

export default Header;
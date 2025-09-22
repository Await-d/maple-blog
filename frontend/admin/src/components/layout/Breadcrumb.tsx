// @ts-nocheck
import React from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { Breadcrumb as AntBreadcrumb, theme, Typography } from 'antd';
import {
  HomeOutlined,
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
  ProfileOutlined,
  EditOutlined,
  PlusOutlined,
} from '@ant-design/icons';

const { Text } = Typography;

// 路由配置映射
const routeConfig: Record<string, {
  title: string;
  icon?: React.ReactNode;
  parent?: string;
}> = {
  '/': {
    title: '首页',
    icon: <HomeOutlined />,
  },
  '/dashboard': {
    title: '仪表盘',
    icon: <DashboardOutlined />,
  },
  
  // 用户管理
  '/users': {
    title: '用户管理',
    icon: <UserOutlined />,
    parent: '/dashboard',
  },
  '/users/list': {
    title: '用户列表',
    icon: <TeamOutlined />,
    parent: '/users',
  },
  '/users/create': {
    title: '创建用户',
    icon: <PlusOutlined />,
    parent: '/users',
  },
  '/users/edit': {
    title: '编辑用户',
    icon: <EditOutlined />,
    parent: '/users',
  },
  '/roles': {
    title: '角色管理',
    icon: <SafetyOutlined />,
    parent: '/users',
  },
  '/roles/create': {
    title: '创建角色',
    icon: <PlusOutlined />,
    parent: '/roles',
  },
  '/roles/edit': {
    title: '编辑角色',
    icon: <EditOutlined />,
    parent: '/roles',
  },

  // 内容管理
  '/content': {
    title: '内容管理',
    icon: <FileTextOutlined />,
    parent: '/dashboard',
  },
  '/content/posts': {
    title: '文章管理',
    icon: <FileTextOutlined />,
    parent: '/content',
  },
  '/content/posts/create': {
    title: '创建文章',
    icon: <PlusOutlined />,
    parent: '/content/posts',
  },
  '/content/posts/edit': {
    title: '编辑文章',
    icon: <EditOutlined />,
    parent: '/content/posts',
  },
  '/content/categories': {
    title: '分类管理',
    icon: <FolderOutlined />,
    parent: '/content',
  },
  '/content/categories/create': {
    title: '创建分类',
    icon: <PlusOutlined />,
    parent: '/content/categories',
  },
  '/content/categories/edit': {
    title: '编辑分类',
    icon: <EditOutlined />,
    parent: '/content/categories',
  },
  '/content/tags': {
    title: '标签管理',
    icon: <TagsOutlined />,
    parent: '/content',
  },
  '/content/tags/create': {
    title: '创建标签',
    icon: <PlusOutlined />,
    parent: '/content/tags',
  },
  '/content/tags/edit': {
    title: '编辑标签',
    icon: <EditOutlined />,
    parent: '/content/tags',
  },
  '/content/media': {
    title: '媒体库',
    icon: <FileImageOutlined />,
    parent: '/content',
  },

  // 数据分析
  '/analytics': {
    title: '数据分析',
    icon: <BarChartOutlined />,
    parent: '/dashboard',
  },
  '/analytics/overview': {
    title: '分析概览',
    icon: <PieChartOutlined />,
    parent: '/analytics',
  },
  '/analytics/content': {
    title: '内容分析',
    icon: <LineChartOutlined />,
    parent: '/analytics',
  },
  '/analytics/users': {
    title: '用户分析',
    icon: <UserOutlined />,
    parent: '/analytics',
  },

  // 系统管理
  '/system': {
    title: '系统管理',
    icon: <SettingOutlined />,
    parent: '/dashboard',
  },
  '/system/settings': {
    title: '系统设置',
    icon: <GlobalOutlined />,
    parent: '/system',
  },
  '/system/monitoring': {
    title: '系统监控',
    icon: <MonitorOutlined />,
    parent: '/system',
  },
  '/system/logs': {
    title: '系统日志',
    icon: <FileSearchOutlined />,
    parent: '/system',
  },
  '/system/audit': {
    title: '审计日志',
    icon: <AuditOutlined />,
    parent: '/system',
  },

  // 个人中心
  '/profile': {
    title: '个人中心',
    icon: <ProfileOutlined />,
    parent: '/dashboard',
  },
  '/profile/security': {
    title: '安全设置',
    icon: <SafetyOutlined />,
    parent: '/profile',
  },
  '/profile/settings': {
    title: '偏好设置',
    icon: <SettingOutlined />,
    parent: '/profile',
  },
};

interface BreadcrumbProps {
  className?: string;
  showIcon?: boolean;
  separator?: string | React.ReactNode;
  maxItems?: number;
}

const Breadcrumb: React.FC<BreadcrumbProps> = ({
  className = '',
  showIcon = true,
  separator = '/',
  maxItems = 10,
}) => {
  const location = useLocation();
  const navigate = useNavigate();
  const { token } = theme.useToken();

  // 生成面包屑路径
  const generateBreadcrumbItems = () => {
    const pathname = location.pathname;
    const pathSegments = pathname.split('/').filter(Boolean);
    
    // 构建完整路径
    const paths: string[] = ['/dashboard']; // 总是从仪表盘开始
    
    if (pathname !== '/dashboard') {
      let currentPath = '';
      pathSegments.forEach(segment => {
        currentPath += `/${segment}`;
        paths.push(currentPath);
      });
    }

    // 去重并获取配置
    const uniquePaths = Array.from(new Set(paths));
    
    // 生成面包屑项目
    const items = uniquePaths.map((path, index) => {
      const config = routeConfig[path];
      const isLast = index === uniquePaths.length - 1;
      const isClickable = !isLast && path !== '/';

      // 处理动态路由参数（如 /users/123/edit）
      let displayConfig = config;
      if (!config && path.includes('/')) {
        // 尝试匹配模式路由
        const segments = path.split('/');
        for (let i = segments.length; i > 0; i--) {
          const pattern = segments.slice(0, i).join('/');
          if (routeConfig[pattern]) {
            displayConfig = routeConfig[pattern];
            break;
          }
        }
        
        // 如果还是没找到，使用最后一个路径段作为标题
        if (!displayConfig) {
          const lastSegment = segments[segments.length - 1];
          displayConfig = {
            title: lastSegment.charAt(0).toUpperCase() + lastSegment.slice(1),
          };
        }
      }

      if (!displayConfig) {
        return null;
      }

      return {
        key: path,
        title: (
          <span
            style={{
              color: isLast ? token.colorText : token.colorTextSecondary,
              cursor: isClickable ? 'pointer' : 'default',
              display: 'flex',
              alignItems: 'center',
              gap: '4px',
              fontWeight: isLast ? 500 : 400,
            }}
            onClick={() => isClickable && navigate(path)}
          >
            {showIcon && displayConfig.icon}
            {displayConfig.title}
          </span>
        ),
      };
    }).filter(Boolean);

    // 限制显示数量
    if (items.length > maxItems) {
      return [
        items[0], // 首页
        {
          key: 'ellipsis',
          title: <Text type="secondary">...</Text>,
        },
        ...items.slice(-maxItems + 2), // 保留最后几项
      ];
    }

    return items;
  };

  const breadcrumbItems = generateBreadcrumbItems();

  if (breadcrumbItems.length <= 1) {
    return null; // 不显示只有一项的面包屑
  }

  return (
    <div className={`admin-breadcrumb ${className}`}>
      <AntBreadcrumb
        items={breadcrumbItems}
        separator={separator}
        style={{
          fontSize: '14px',
          lineHeight: '22px',
        }}
      />

      {/* 自定义样式 */}
      <style>{`
        .admin-breadcrumb .ant-breadcrumb {
          display: flex;
          align-items: center;
          flex-wrap: wrap;
        }

        .admin-breadcrumb .ant-breadcrumb-link {
          display: flex;
          align-items: center;
          transition: color 0.2s;
        }

        .admin-breadcrumb .ant-breadcrumb-link:hover {
          color: ${token.colorPrimary} !important;
        }

        .admin-breadcrumb .ant-breadcrumb-separator {
          margin: 0 8px;
          color: ${token.colorTextTertiary};
        }

        /* 响应式设计 */
        @media (max-width: 768px) {
          .admin-breadcrumb {
            display: none;
          }
        }

        @media (max-width: 576px) {
          .admin-breadcrumb .ant-breadcrumb-link span {
            max-width: 100px;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
          }
        }
      `}</style>
    </div>
  );
};

export default Breadcrumb;
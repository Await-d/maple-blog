import React, { useMemo, useEffect, useState } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { Result, Button, Spin } from 'antd';
import { LockOutlined, ReloadOutlined } from '@ant-design/icons';
import { usePermissions } from '@/hooks/usePermissions';
import { useAdminStore } from '@/stores/adminStore';
import {
  GuardMode,
  PermissionGuardConfig,
  PermissionGuardProps,
  AccessDeniedProps
} from './permission-types';


// 访问拒绝组件
export const AccessDenied: React.FC<AccessDeniedProps> = ({
  reason = '您没有访问此页面的权限',
  permissions = [],
  onRetry,
  showDetails = false,
  redirectTo = '/admin/dashboard'
}) => {
  const location = useLocation();

  return (
    <div className="flex items-center justify-center min-h-[400px] p-8">
      <Result
        status="403"
        title="访问被拒绝"
        subTitle={reason}
        icon={<LockOutlined className="text-red-500" />}
        extra={[
          <Button type="primary" key="home" onClick={() => window.location.href = redirectTo}>
            返回首页
          </Button>,
          onRetry && (
            <Button key="retry" icon={<ReloadOutlined />} onClick={onRetry}>
              重试
            </Button>
          )
        ].filter(Boolean)}
      >
        {showDetails && permissions.length > 0 && (
          <div className="mt-4 p-4 bg-gray-50 rounded-lg">
            <h4 className="text-sm font-medium text-gray-900 mb-2">所需权限：</h4>
            <ul className="text-sm text-gray-600 space-y-1">
              {permissions.map((permission, index) => (
                <li key={index} className="flex items-center">
                  <span className="w-2 h-2 bg-red-400 rounded-full mr-2"></span>
                  {permission}
                </li>
              ))}
            </ul>
          </div>
        )}
        
        {showDetails && (
          <div className="mt-4 text-xs text-gray-500">
            <p>当前页面: {location.pathname}</p>
            <p>时间: {new Date().toLocaleString()}</p>
          </div>
        )}
      </Result>
    </div>
  );
};

// 权限加载组件
export const PermissionLoading: React.FC<{ message?: string }> = ({ 
  message = '正在验证权限...' 
}) => (
  <div className="flex items-center justify-center min-h-[200px] p-8">
    <div className="text-center">
      <Spin size="large" />
      <p className="mt-4 text-gray-600">{message}</p>
    </div>
  </div>
);

// 主要权限守卫组件
export const PermissionGuard: React.FC<PermissionGuardProps> = ({
  children,
  permissions = [],
  roles = [],
  resources = [],
  mode = 'all',
  strict = false,
  guardMode = GuardMode.RENDER,
  fallback,
  redirectTo = '/admin/403',
  customCheck,
  onAccessDenied,
  showAccessDenied = true,
  loading,
  debug = false,
  className,
  style
}) => {
  const {
    can,
    canAll,
    canAny,
    hasRole,
    hasAnyRole,
    hasAllRoles,
    checkResourcePermission,
    isAuthenticated,
    user,
    trace
  } = usePermissions({ strict, logAccess: debug });

  const { addNotification } = useAdminStore();
  const location = useLocation();
  const [isChecking, setIsChecking] = useState(true);
  const [hasAccess, setHasAccess] = useState(false);
  const [denialReason, setDenialReason] = useState<string>('');

  // 权限检查逻辑
  const checkAccess = useMemo(() => {
    if (!isAuthenticated) {
      return { hasAccess: false, reason: '用户未登录' };
    }

    const checks: boolean[] = [];
    let reason = '';

    try {
      // 权限检查
      if (permissions && permissions.length > 0) {
        const permArray = Array.isArray(permissions) ? permissions : [permissions];
        const permCheck = mode === 'all' ? canAll(permArray) : canAny(permArray);
        checks.push(permCheck);
        
        if (!permCheck) {
          reason += `缺少权限: ${permArray.join(', ')}; `;
        }
      }

      // 角色检查
      if (roles && roles.length > 0) {
        const roleArray = Array.isArray(roles) ? roles : [roles];
        const roleCheck = mode === 'all' ? hasAllRoles(roleArray) : hasAnyRole(roleArray);
        checks.push(roleCheck);
        
        if (!roleCheck) {
          reason += `缺少角色: ${roleArray.join(', ')}; `;
        }
      }

      // 资源权限检查
      if (resources && resources.length > 0) {
        const resourceChecks = resources.map(({ resource, level }) => 
          checkResourcePermission(resource, level)
        );
        const resourceCheck = mode === 'all' ? 
          resourceChecks.every(Boolean) : 
          resourceChecks.some(Boolean);
        checks.push(resourceCheck);
        
        if (!resourceCheck) {
          const resourceNames = resources.map(r => `${r.resource}(${PermissionLevel[r.level]})`);
          reason += `缺少资源权限: ${resourceNames.join(', ')}; `;
        }
      }

      // 自定义检查
      if (customCheck) {
        const customResult = customCheck({ can, hasRole, user, permissions: permissions });
        checks.push(customResult);
        
        if (!customResult) {
          reason += '自定义权限检查失败; ';
        }
      }

      // 如果没有任何检查，默认允许访问
      if (checks.length === 0) {
        return { hasAccess: true, reason: '' };
      }

      // 根据模式确定最终结果
      const finalAccess = mode === 'all' ? checks.every(Boolean) : checks.some(Boolean);
      
      return { 
        hasAccess: finalAccess, 
        reason: finalAccess ? '' : reason.trim().replace(/;$/, '')
      };
    } catch (error) {
      console.error('权限检查错误:', error);
      return { hasAccess: false, reason: '权限检查出现异常' };
    }
  }, [
    isAuthenticated, permissions, roles, resources, mode, customCheck,
    can, canAll, canAny, hasRole, hasAnyRole, hasAllRoles, 
    checkResourcePermission, user
  ]);

  // 执行权限检查
  useEffect(() => {
    const performCheck = async () => {
      setIsChecking(true);
      
      // 模拟异步检查（如果需要）
      await new Promise(resolve => setTimeout(resolve, 100));
      
      const { hasAccess: access, reason } = checkAccess;
      
      setHasAccess(access);
      setDenialReason(reason);
      setIsChecking(false);

      // 调试信息
      if (debug) {
        console.group('🛡️ 权限守卫检查');
        console.log('页面:', location.pathname);
        console.log('用户:', user?.username);
        console.log('权限要求:', { permissions, roles, resources });
        console.log('检查结果:', access);
        console.log('拒绝原因:', reason);
        
        if (permissions) {
          const permArray = Array.isArray(permissions) ? permissions : [permissions];
          permArray.forEach(perm => {
            console.log(`权限追踪 [${perm}]:`, trace(perm));
          });
        }
        
        console.groupEnd();
      }

      // 访问拒绝回调
      if (!access && onAccessDenied) {
        onAccessDenied(reason);
      }

      // 显示通知
      if (!access && showAccessDenied) {
        addNotification({
          type: 'warning',
          title: '访问被拒绝',
          description: reason,
          duration: 5000
        });
      }
    };

    performCheck();
  }, [checkAccess, debug, location.pathname, user, onAccessDenied, showAccessDenied, addNotification, trace, permissions, resources, roles]);

  // 加载状态
  if (isChecking) {
    return loading ? <>{loading}</> : <PermissionLoading />;
  }

  // 未登录处理
  if (!isAuthenticated) {
    if (guardMode === GuardMode.REDIRECT) {
      return <Navigate to="/admin/login" state={{ from: location }} replace />;
    }
    
    return (
      <AccessDenied
        reason="请先登录"
        redirectTo="/admin/login"
        showDetails={debug}
      />
    );
  }

  // 权限检查失败处理
  if (!hasAccess) {
    switch (guardMode) {
      case GuardMode.REDIRECT:
        return <Navigate to={redirectTo} state={{ reason: denialReason, from: location }} replace />;
      
      case GuardMode.HIDE:
        return null;
      
      case GuardMode.REPLACE:
        return fallback ? <>{fallback}</> : null;
      
      case GuardMode.RENDER:
      default:
        if (fallback) {
          return <>{fallback}</>;
        }
        
        return (
          <AccessDenied
            reason={denialReason}
            permissions={Array.isArray(permissions) ? permissions : [permissions].filter(Boolean)}
            showDetails={debug}
            onRetry={() => {
              setIsChecking(true);
              setTimeout(() => setIsChecking(false), 1000);
            }}
          />
        );
    }
  }

  // 权限检查通过，渲染子组件
  return (
    <div className={className} style={style}>
      {children}
    </div>
  );
};

// 角色守卫组件
export const RoleGuard: React.FC<{
  children: React.ReactNode;
  roles: string | string[];
  mode?: 'all' | 'any';
  fallback?: React.ReactNode;
}> = ({ children, roles, mode = 'any', fallback }) => (
  <PermissionGuard roles={roles} mode={mode} fallback={fallback}>
    {children}
  </PermissionGuard>
);

// 资源守卫组件
export const ResourceGuard: React.FC<{
  children: React.ReactNode;
  resource: string;
  level: PermissionLevel;
  fallback?: React.ReactNode;
}> = ({ children, resource, level, fallback }) => (
  <PermissionGuard 
    resources={[{ resource, level }]} 
    fallback={fallback}
  >
    {children}
  </PermissionGuard>
);

// 管理员守卫组件
export const AdminGuard: React.FC<{
  children: React.ReactNode;
  fallback?: React.ReactNode;
}> = ({ children, fallback }) => (
  <PermissionGuard 
    roles={['Admin', 'SuperAdmin']} 
    mode="any"
    fallback={fallback}
  >
    {children}
  </PermissionGuard>
);

// 超级管理员守卫组件
export const SuperAdminGuard: React.FC<{
  children: React.ReactNode;
  fallback?: React.ReactNode;
}> = ({ children, fallback }) => (
  <PermissionGuard 
    roles="SuperAdmin"
    fallback={fallback}
  >
    {children}
  </PermissionGuard>
);

// 高阶组件版本
export const withPermission = <P extends object>(
  Component: React.ComponentType<P>,
  config: PermissionGuardConfig
) => {
  const WrappedComponent = (props: P) => (
    <PermissionGuard {...config}>
      <Component {...props} />
    </PermissionGuard>
  );

  WrappedComponent.displayName = `withPermission(${Component.displayName || Component.name})`;
  return WrappedComponent;
};

// 权限条件渲染组件
export const PermissionRender: React.FC<{
  permission?: string | string[];
  role?: string | string[];
  resource?: { resource: string; level: PermissionLevel };
  children: React.ReactNode;
  fallback?: React.ReactNode;
  mode?: 'all' | 'any';
}> = ({ permission, role, resource, children, fallback, mode = 'any' }) => {
  const { can, hasRole, checkResourcePermission } = usePermissions();

  const hasAccess = useMemo(() => {
    const checks: boolean[] = [];

    if (permission) {
      const perms = Array.isArray(permission) ? permission : [permission];
      if (mode === 'all') {
        checks.push(perms.every(p => can(p)));
      } else {
        checks.push(perms.some(p => can(p)));
      }
    }

    if (role) {
      const roles = Array.isArray(role) ? role : [role];
      if (mode === 'all') {
        checks.push(roles.every(r => hasRole(r)));
      } else {
        checks.push(roles.some(r => hasRole(r)));
      }
    }

    if (resource) {
      checks.push(checkResourcePermission(resource.resource, resource.level));
    }

    return checks.length > 0 ? checks.every(Boolean) : true;
  }, [permission, role, resource, mode, can, hasRole, checkResourcePermission]);

  if (!hasAccess) {
    return fallback ? <>{fallback}</> : null;
  }

  return <>{children}</>;
};

export default PermissionGuard;
/* eslint-disable react-refresh/only-export-components */
import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { useAdminStore } from '@/stores/adminStore';
import type { User, Role } from '@/types';

// 权限检查结果类型
export interface PermissionCheck {
  hasPermission: boolean;
  missingPermissions: string[];
  reason?: string;
}

// 权限级别
export enum PermissionLevel {
  NONE = 0,
  READ = 1,
  WRITE = 2,
  DELETE = 3,
  ADMIN = 4
}

// 资源权限类型
export interface ResourcePermission {
  resource: string;
  level: PermissionLevel;
  conditions?: Record<string, unknown>;
}

// 权限上下文状态
export interface PermissionContextState {
  // 基础权限检查
  hasPermission: (permission: string | string[]) => boolean;
  hasAllPermissions: (permissions: string[]) => boolean;
  hasAnyPermission: (permissions: string[]) => boolean;
  
  // 角色检查
  hasRole: (role: string | string[]) => boolean;
  hasAllRoles: (roles: string[]) => boolean;
  hasAnyRole: (roles: string[]) => boolean;
  
  // 高级权限检查
  checkPermission: (permission: string | string[]) => PermissionCheck;
  checkResourcePermission: (resource: string, level: PermissionLevel, conditions?: Record<string, unknown>) => boolean;
  
  // 层级权限检查
  hasHierarchicalPermission: (permission: string, resource?: string) => boolean;
  canAccessResource: (resource: string, action: string) => boolean;
  
  // 权限信息
  getUserPermissions: () => string[];
  getUserRoles: () => Role[];
  getPermissionLevel: (resource: string) => PermissionLevel;
  
  // 权限调试
  debugPermissions: () => void;
  getPermissionTrace: (permission: string) => string[];
  
  // 权限缓存
  refreshPermissions: () => Promise<void>;
  clearPermissionCache: () => void;
  
  // 权限状态
  isLoading: boolean;
  isAuthenticated: boolean;
  user: User | null;
  lastUpdated: Date | null;
}

// 权限上下文
const PermissionContext = createContext<PermissionContextState | null>(null);

// 权限提供者属性
export interface PermissionProviderProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;
  onPermissionDenied?: (permission: string, reason: string) => void;
  enableDebug?: boolean;
}

// 权限缓存
const permissionCache = new Map<string, { result: boolean; timestamp: number }>();
// const CACHE_DURATION = 5 * 60 * 1000; // 5分钟缓存

// 权限提供者组件
export const PermissionProvider: React.FC<PermissionProviderProps> = ({
  children,
  fallback,
  onPermissionDenied,
  enableDebug = false
}) => {
  const { user, permissions } = useAdminStore();
  const [isLoading, setIsLoading] = useState(false);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  // 基础权限检查
  const hasPermission = useMemo(() => (permission: string | string[]): boolean => {
    if (!user || !permissions.length) return false;

    if (Array.isArray(permission)) {
      return permission.some(p => permissions.includes(p));
    }

    return permissions.includes(permission);
  }, [user, permissions]);

  const hasAllPermissions = useMemo(() => (requiredPermissions: string[]): boolean => {
    if (!user || !permissions.length) return false;
    return requiredPermissions.every(p => permissions.includes(p));
  }, [user, permissions]);

  const hasAnyPermission = useMemo(() => (requiredPermissions: string[]): boolean => {
    if (!user || !permissions.length) return false;
    return requiredPermissions.some(p => permissions.includes(p));
  }, [user, permissions]);

  // 角色检查
  const hasRole = useMemo(() => (role: string | string[]): boolean => {
    if (!user?.roles) return false;

    const userRoles = user.roles.map(r => r.name);

    if (Array.isArray(role)) {
      return role.some(r => userRoles.includes(r));
    }

    return userRoles.includes(role);
  }, [user]);

  const hasAllRoles = useMemo(() => (roles: string[]): boolean => {
    if (!user?.roles) return false;
    const userRoles = user.roles.map(r => r.name);
    return roles.every(r => userRoles.includes(r));
  }, [user]);

  const hasAnyRole = useMemo(() => (roles: string[]): boolean => {
    if (!user?.roles) return false;
    const userRoles = user.roles.map(r => r.name);
    return roles.some(r => userRoles.includes(r));
  }, [user]);

  // 高级权限检查
  const checkPermission = useMemo(() => (permission: string | string[]): PermissionCheck => {
    if (!user) {
      return {
        hasPermission: false,
        missingPermissions: Array.isArray(permission) ? permission : [permission],
        reason: '用户未登录'
      };
    }

    if (!permissions.length) {
      return {
        hasPermission: false,
        missingPermissions: Array.isArray(permission) ? permission : [permission],
        reason: '用户无任何权限'
      };
    }

    const requiredPermissions = Array.isArray(permission) ? permission : [permission];
    const missingPermissions = requiredPermissions.filter(p => !permissions.includes(p));

    return {
      hasPermission: missingPermissions.length === 0,
      missingPermissions,
      reason: missingPermissions.length > 0 ? `缺少权限: ${missingPermissions.join(', ')}` : undefined
    };
  }, [user, permissions]);

  // 资源权限检查
  const checkResourcePermission = useMemo(() => (
    resource: string, 
    level: PermissionLevel, 
    _conditions?: Record<string, unknown>
  ): boolean => {
    if (!user || !permissions.length) return false;

    // 根据权限级别检查
    switch (level) {
      case PermissionLevel.READ:
        return hasAnyPermission([`${resource}.read`, `${resource}.write`, `${resource}.delete`, `${resource}.admin`]);
      case PermissionLevel.WRITE:
        return hasAnyPermission([`${resource}.write`, `${resource}.delete`, `${resource}.admin`]);
      case PermissionLevel.DELETE:
        return hasAnyPermission([`${resource}.delete`, `${resource}.admin`]);
      case PermissionLevel.ADMIN:
        return hasPermission(`${resource}.admin`);
      default:
        return false;
    }
  }, [user, permissions, hasAnyPermission, hasPermission]);

  // 层级权限检查
  const hasHierarchicalPermission = useMemo(() => (permission: string, resource?: string): boolean => {
    if (!user || !permissions.length) return false;

    // 检查超级管理员权限
    if (hasRole('SuperAdmin') || hasPermission('system.admin')) {
      return true;
    }

    // 检查具体权限
    if (hasPermission(permission)) {
      return true;
    }

    // 检查父级权限
    const parts = permission.split('.');
    for (let i = parts.length - 1; i > 0; i--) {
      const parentPermission = parts.slice(0, i).join('.') + '.admin';
      if (hasPermission(parentPermission)) {
        return true;
      }
    }

    // 检查资源所有者权限
    if (resource && user.id) {
      const ownerPermission = `${resource}.owner.${user.id}`;
      if (hasPermission(ownerPermission)) {
        return true;
      }
    }

    return false;
  }, [user, permissions, hasRole, hasPermission]);

  // 资源访问检查
  const canAccessResource = useMemo(() => (resource: string, action: string): boolean => {
    const permission = `${resource}.${action}`;
    return hasHierarchicalPermission(permission, resource);
  }, [hasHierarchicalPermission]);

  // 获取权限信息
  const getUserPermissions = useMemo(() => (): string[] => {
    return permissions;
  }, [permissions]);

  const getUserRoles = useMemo(() => (): Role[] => {
    return user?.roles || [];
  }, [user]);

  const getPermissionLevel = useMemo(() => (resource: string): PermissionLevel => {
    if (checkResourcePermission(resource, PermissionLevel.ADMIN)) {
      return PermissionLevel.ADMIN;
    }
    if (checkResourcePermission(resource, PermissionLevel.DELETE)) {
      return PermissionLevel.DELETE;
    }
    if (checkResourcePermission(resource, PermissionLevel.WRITE)) {
      return PermissionLevel.WRITE;
    }
    if (checkResourcePermission(resource, PermissionLevel.READ)) {
      return PermissionLevel.READ;
    }
    return PermissionLevel.NONE;
  }, [checkResourcePermission]);

  // 权限调试
  const debugPermissions = useMemo(() => (): void => {
    if (!enableDebug) return;

    console.group('🔐 权限调试信息');
    console.log('当前用户:', user);
    console.log('用户角色:', user?.roles);
    console.log('用户权限:', permissions);
    console.log('权限缓存:', permissionCache);
    console.log('最后更新:', lastUpdated);
    console.groupEnd();
  }, [user, permissions, lastUpdated, enableDebug]);

  const getPermissionTrace = useMemo(() => (permission: string): string[] => {
    const trace: string[] = [];

    if (!user) {
      trace.push('❌ 用户未登录');
      return trace;
    }

    trace.push(`👤 当前用户: ${user.username} (${user.email})`);
    
    if (user.roles.length > 0) {
      trace.push(`🏷️ 用户角色: ${user.roles.map(r => r.name).join(', ')}`);
    }

    if (hasPermission(permission)) {
      trace.push(`✅ 拥有权限: ${permission}`);
    } else {
      trace.push(`❌ 缺少权限: ${permission}`);
      
      // 检查相关权限
      const relatedPermissions = permissions.filter(p => 
        p.includes(permission.split('.')[0]) || permission.includes(p.split('.')[0])
      );
      
      if (relatedPermissions.length > 0) {
        trace.push(`🔍 相关权限: ${relatedPermissions.join(', ')}`);
      }
    }

    return trace;
  }, [user, permissions, hasPermission]);

  // 权限刷新
  const refreshPermissions = useMemo(() => async (): Promise<void> => {
    if (!user) return;

    setIsLoading(true);
    try {
      // 这里应该调用API刷新权限
      // const response = await api.getUserPermissions(user.id);
      // setPermissions(response.permissions);
      
      // 清除缓存
      permissionCache.clear();
      setLastUpdated(new Date());
    } catch (error) {
      console.error('刷新权限失败:', error);
    } finally {
      setIsLoading(false);
    }
  }, [user]);

  const clearPermissionCache = useMemo(() => (): void => {
    permissionCache.clear();
    if (enableDebug) {
      console.log('🗑️ 权限缓存已清除');
    }
  }, [enableDebug]);

  // 权限变更监听
  useEffect(() => {
    if (user && permissions.length > 0) {
      setLastUpdated(new Date());
    }
  }, [user, permissions]);

  // 权限拒绝回调
  useEffect(() => {
    if (onPermissionDenied && user) {
      // 可以在这里监听权限检查失败事件
    }
  }, [onPermissionDenied, user]);

  const contextValue: PermissionContextState = {
    // 基础权限检查
    hasPermission,
    hasAllPermissions,
    hasAnyPermission,
    
    // 角色检查
    hasRole,
    hasAllRoles,
    hasAnyRole,
    
    // 高级权限检查
    checkPermission,
    checkResourcePermission,
    
    // 层级权限检查
    hasHierarchicalPermission,
    canAccessResource,
    
    // 权限信息
    getUserPermissions,
    getUserRoles,
    getPermissionLevel,
    
    // 权限调试
    debugPermissions,
    getPermissionTrace,
    
    // 权限缓存
    refreshPermissions,
    clearPermissionCache,
    
    // 权限状态
    isLoading,
    isAuthenticated: !!user,
    user,
    lastUpdated
  };

  if (!user && fallback) {
    return <>{fallback}</>;
  }

  return (
    <PermissionContext.Provider value={contextValue}>
      {children}
    </PermissionContext.Provider>
  );
};

// 权限Hook
export const usePermissionContext = (): PermissionContextState => {
  const context = useContext(PermissionContext);
  
  if (!context) {
    throw new Error('usePermissionContext must be used within a PermissionProvider');
  }
  
  return context;
};

// 便捷权限Hooks
export const useCanAccess = (permission: string | string[]) => {
  const { hasPermission } = usePermissionContext();
  return hasPermission(permission);
};

export const useCanAccessAll = (permissions: string[]) => {
  const { hasAllPermissions } = usePermissionContext();
  return hasAllPermissions(permissions);
};

export const useCanAccessAny = (permissions: string[]) => {
  const { hasAnyPermission } = usePermissionContext();
  return hasAnyPermission(permissions);
};

export const useHasRoleAccess = (role: string | string[]) => {
  const { hasRole } = usePermissionContext();
  return hasRole(role);
};

export const useResourceAccess = (resource: string, level: PermissionLevel = PermissionLevel.READ) => {
  const { checkResourcePermission } = usePermissionContext();
  return checkResourcePermission(resource, level);
};

export const usePermissionCheck = (permission: string | string[]) => {
  const { checkPermission } = usePermissionContext();
  return checkPermission(permission);
};

// 错误边界组件
export class PermissionErrorBoundary extends React.Component<
  { children: React.ReactNode; fallback?: React.ReactNode },
  { hasError: boolean }
> {
  constructor(props: { children: React.ReactNode; fallback?: React.ReactNode }) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(): { hasError: boolean } {
    return { hasError: true };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('权限系统错误:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return this.props.fallback || (
        <div className="p-4 text-center text-red-600">
          <h3>权限系统错误</h3>
          <p>权限检查出现异常，请刷新页面重试。</p>
        </div>
      );
    }

    return this.props.children;
  }
}

export default PermissionContext;
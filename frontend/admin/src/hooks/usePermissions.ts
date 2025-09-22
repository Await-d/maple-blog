// @ts-nocheck
import { useMemo, useCallback, useRef, useEffect } from 'react';
import { usePermissionContext, PermissionLevel } from '@/contexts/PermissionContext';
import { useAdminStore } from '@/stores/adminStore';
import type { User, Role } from '@/types';

// 权限检查选项
export interface PermissionCheckOptions {
  strict?: boolean; // 严格模式，所有权限都必须满足
  fallback?: boolean; // 回退值，当权限检查失败时返回的默认值
  cache?: boolean; // 是否缓存结果
  onDenied?: (permission: string, reason: string) => void; // 权限拒绝回调
  logAccess?: boolean; // 是否记录访问日志
}

// 批量权限检查结果
export interface BatchPermissionResult {
  hasAll: boolean;
  hasAny: boolean;
  granted: string[];
  denied: string[];
  details: Record<string, boolean>;
}

// 权限Hook返回类型
export interface UsePermissionsReturn {
  // 基础权限检查
  can: (permission: string | string[], options?: PermissionCheckOptions) => boolean;
  cannot: (permission: string | string[], options?: PermissionCheckOptions) => boolean;
  canAll: (permissions: string[], options?: PermissionCheckOptions) => boolean;
  canAny: (permissions: string[], options?: PermissionCheckOptions) => boolean;
  
  // 角色检查
  hasRole: (role: string | string[]) => boolean;
  hasAnyRole: (roles: string[]) => boolean;
  hasAllRoles: (roles: string[]) => boolean;
  
  // 资源权限检查
  canRead: (resource: string) => boolean;
  canWrite: (resource: string) => boolean;
  canDelete: (resource: string) => boolean;
  canAdmin: (resource: string) => boolean;
  getResourceLevel: (resource: string) => PermissionLevel;
  
  // 高级权限检查
  canAccess: (resource: string, action: string) => boolean;
  canModify: (resource: string, data?: any) => boolean;
  canCreate: (resource: string, data?: any) => boolean;
  
  // 批量权限检查
  checkBatch: (permissions: string[]) => BatchPermissionResult;
  checkResources: (resources: string[], level: PermissionLevel) => Record<string, boolean>;
  checkResourcePermission: (resource: string, level: PermissionLevel) => boolean;
  
  // 条件权限检查
  canIf: (condition: boolean, permission: string | string[]) => boolean;
  canWhen: (predicate: () => boolean, permission: string | string[]) => boolean;
  
  // 权限信息
  permissions: string[];
  roles: Role[];
  user: User | null;
  isAuthenticated: boolean;
  
  // 调试和分析
  debug: () => void;
  trace: (permission: string) => string[];
  analyze: () => PermissionAnalysis;
  
  // 缓存管理
  clearCache: () => void;
  refreshPermissions: () => Promise<void>;
}

// 权限分析结果
export interface PermissionAnalysis {
  totalPermissions: number;
  categorizedPermissions: Record<string, string[]>;
  roleHierarchy: string[];
  missingCriticalPermissions: string[];
  redundantPermissions: string[];
  securityRisk: 'low' | 'medium' | 'high';
}

// 权限缓存
const permissionCache = new Map<string, { result: boolean; timestamp: number; ttl: number }>();

// 关键权限列表
const CRITICAL_PERMISSIONS = [
  'system.admin',
  'user.admin',
  'role.admin',
  'permission.admin',
  'system.settings',
  'audit.admin'
];

/**
 * 增强的权限管理Hook
 * 提供全面的权限检查、角色管理和安全控制功能
 */
export const usePermissions = (options: PermissionCheckOptions = {}): UsePermissionsReturn => {
  const context = usePermissionContext();
  const { user } = useAdminStore();
  const accessLogRef = useRef<Array<{ permission: string; granted: boolean; timestamp: Date; reason?: string }>>([]);

  const {
    hasPermission,
    hasAllPermissions,
    hasAnyPermission,
    hasRole: contextHasRole,
    hasAllRoles: contextHasAllRoles,
    hasAnyRole: contextHasAnyRole,
    checkResourcePermission,
    canAccessResource,
    getPermissionLevel,
    getUserPermissions,
    getUserRoles,
    debugPermissions,
    getPermissionTrace,
    clearPermissionCache,
    refreshPermissions
  } = context;

  // 记录访问日志
  const logAccess = useCallback((permission: string, granted: boolean, reason?: string) => {
    if (options.logAccess) {
      accessLogRef.current.push({
        permission,
        granted,
        timestamp: new Date(),
        reason
      });

      // 限制日志大小
      if (accessLogRef.current.length > 1000) {
        accessLogRef.current = accessLogRef.current.slice(-500);
      }
    }
  }, [options.logAccess]);

  // 缓存键生成
  const getCacheKey = useCallback((permission: string | string[], userId?: string): string => {
    const permStr = Array.isArray(permission) ? permission.sort().join(',') : permission;
    return `${userId || 'anonymous'}:${permStr}`;
  }, []);

  // 缓存检查
  const checkCache = useCallback((key: string, ttl: number = 300000): boolean | null => {
    if (!options.cache) return null;

    const cached = permissionCache.get(key);
    if (cached && Date.now() - cached.timestamp < (cached.ttl || ttl)) {
      return cached.result;
    }

    permissionCache.delete(key);
    return null;
  }, [options.cache]);

  // 设置缓存
  const setCache = useCallback((key: string, result: boolean, ttl: number = 300000) => {
    if (options.cache) {
      permissionCache.set(key, {
        result,
        timestamp: Date.now(),
        ttl
      });
    }
  }, [options.cache]);

  // 基础权限检查
  const can = useCallback((permission: string | string[], checkOptions?: PermissionCheckOptions): boolean => {
    const opts = { ...options, ...checkOptions };
    const cacheKey = getCacheKey(permission, user?.id);
    
    // 检查缓存
    const cached = checkCache(cacheKey);
    if (cached !== null) {
      logAccess(Array.isArray(permission) ? permission.join(',') : permission, cached, 'cached');
      return cached;
    }

    const result = opts.strict 
      ? (Array.isArray(permission) ? hasAllPermissions(permission) : hasPermission(permission))
      : hasPermission(permission);

    // 设置缓存
    setCache(cacheKey, result);

    // 记录访问
    logAccess(Array.isArray(permission) ? permission.join(',') : permission, result);

    // 权限拒绝回调
    if (!result && (opts.onDenied || options.onDenied)) {
      const callback = opts.onDenied || options.onDenied;
      const permStr = Array.isArray(permission) ? permission.join(', ') : permission;
      callback?.(permStr, `缺少权限: ${permStr}`);
    }

    return result;
  }, [hasPermission, hasAllPermissions, options, user?.id, getCacheKey, checkCache, setCache, logAccess]);

  const cannot = useCallback((permission: string | string[], checkOptions?: PermissionCheckOptions): boolean => {
    return !can(permission, checkOptions);
  }, [can]);

  const canAll = useCallback((permissions: string[], checkOptions?: PermissionCheckOptions): boolean => {
    const cacheKey = getCacheKey(permissions, user?.id);
    const cached = checkCache(cacheKey);
    
    if (cached !== null) {
      logAccess(permissions.join(','), cached, 'cached');
      return cached;
    }

    const result = hasAllPermissions(permissions);
    setCache(cacheKey, result);
    logAccess(permissions.join(','), result);

    if (!result && checkOptions?.onDenied) {
      checkOptions.onDenied(permissions.join(', '), '缺少部分权限');
    }

    return result;
  }, [hasAllPermissions, user?.id, getCacheKey, checkCache, setCache, logAccess]);

  const canAny = useCallback((permissions: string[], checkOptions?: PermissionCheckOptions): boolean => {
    const cacheKey = getCacheKey(permissions, user?.id);
    const cached = checkCache(cacheKey);
    
    if (cached !== null) {
      logAccess(permissions.join(','), cached, 'cached');
      return cached;
    }

    const result = hasAnyPermission(permissions);
    setCache(cacheKey, result);
    logAccess(permissions.join(','), result);

    if (!result && checkOptions?.onDenied) {
      checkOptions.onDenied(permissions.join(', '), '不具备任何所需权限');
    }

    return result;
  }, [hasAnyPermission, user?.id, getCacheKey, checkCache, setCache, logAccess]);

  // 角色检查
  const hasRole = useCallback((role: string | string[]): boolean => {
    return contextHasRole(role);
  }, [contextHasRole]);

  const hasAnyRole = useCallback((roles: string[]): boolean => {
    return contextHasAnyRole(roles);
  }, [contextHasAnyRole]);

  const hasAllRoles = useCallback((roles: string[]): boolean => {
    return contextHasAllRoles(roles);
  }, [contextHasAllRoles]);

  // 资源权限检查
  const canRead = useCallback((resource: string): boolean => {
    return checkResourcePermission(resource, PermissionLevel.READ);
  }, [checkResourcePermission]);

  const canWrite = useCallback((resource: string): boolean => {
    return checkResourcePermission(resource, PermissionLevel.write);
  }, [checkResourcePermission]);

  const canDelete = useCallback((resource: string): boolean => {
    return checkResourcePermission(resource, PermissionLevel.delete);
  }, [checkResourcePermission]);

  const canAdmin = useCallback((resource: string): boolean => {
    return checkResourcePermission(resource, PermissionLevel.admin);
  }, [checkResourcePermission]);

  const getResourceLevel = useCallback((resource: string): PermissionLevel => {
    return getPermissionLevel(resource);
  }, [getPermissionLevel]);

  // 高级权限检查
  const canAccess = useCallback((resource: string, action: string): boolean => {
    return canAccessResource(resource, action);
  }, [canAccessResource]);

  const canModify = useCallback((resource: string, data?: any): boolean => {
    // 基础写权限检查
    if (!canWrite(resource)) return false;

    // 数据所有权检查
    if (data && data.authorId && user?.id && data.authorId !== user.id) {
      // 检查是否有管理员权限或特殊权限
      return canAdmin(resource) || can(`${resource}.modify.others`);
    }

    return true;
  }, [canWrite, canAdmin, can, user?.id]);

  const canCreate = useCallback((resource: string, data?: any): boolean => {
    // 基础创建权限检查
    if (!can(`${resource}.create`)) return false;

    // 额外的业务逻辑检查可以在这里添加
    // 例如：配额检查、时间限制等

    return true;
  }, [can]);

  // 批量权限检查
  const checkBatch = useCallback((permissions: string[]): BatchPermissionResult => {
    const details: Record<string, boolean> = {};
    const granted: string[] = [];
    const denied: string[] = [];

    permissions.forEach(permission => {
      const hasPermissionResult = can(permission);
      details[permission] = hasPermissionResult;
      
      if (hasPermissionResult) {
        granted.push(permission);
      } else {
        denied.push(permission);
      }
    });

    return {
      hasAll: denied.length === 0,
      hasAny: granted.length > 0,
      granted,
      denied,
      details
    };
  }, [can]);

  const checkResources = useCallback((resources: string[], level: PermissionLevel): Record<string, boolean> => {
    const result: Record<string, boolean> = {};
    
    resources.forEach(resource => {
      result[resource] = checkResourcePermission(resource, level);
    });

    return result;
  }, [checkResourcePermission]);

  // 条件权限检查
  const canIf = useCallback((condition: boolean, permission: string | string[]): boolean => {
    return condition ? can(permission) : false;
  }, [can]);

  const canWhen = useCallback((predicate: () => boolean, permission: string | string[]): boolean => {
    try {
      return predicate() ? can(permission) : false;
    } catch (error) {
      console.error('条件权限检查错误:', error);
      return false;
    }
  }, [can]);

  // 权限信息
  const permissions = useMemo(() => getUserPermissions(), [getUserPermissions]);
  const roles = useMemo(() => getUserRoles(), [getUserRoles]);
  const isAuthenticated = useMemo(() => !!user, [user]);

  // 调试和分析
  const debug = useCallback(() => {
    debugPermissions();
    
    if (accessLogRef.current.length > 0) {
      console.group('📊 权限访问日志');
      console.table(accessLogRef.current.slice(-20)); // 显示最近20条
      console.groupEnd();
    }
  }, [debugPermissions]);

  const trace = useCallback((permission: string): string[] => {
    return getPermissionTrace(permission);
  }, [getPermissionTrace]);

  const analyze = useCallback((): PermissionAnalysis => {
    const categorized: Record<string, string[]> = {};
    
    // 按类别分组权限
    permissions.forEach(permission => {
      const category = permission.split('.')[0] || 'other';
      if (!categorized[category]) {
        categorized[category] = [];
      }
      categorized[category].push(permission);
    });

    // 检查缺失的关键权限
    const missingCritical = CRITICAL_PERMISSIONS.filter(perm => !permissions.includes(perm));

    // 检查冗余权限（这里简化处理）
    const redundant: string[] = [];

    // 评估安全风险
    let securityRisk: 'low' | 'medium' | 'high' = 'low';
    if (permissions.includes('system.admin') || permissions.includes('*')) {
      securityRisk = 'high';
    } else if (missingCritical.length === 0) {
      securityRisk = 'medium';
    }

    return {
      totalPermissions: permissions.length,
      categorizedPermissions: categorized,
      roleHierarchy: roles.map(role => role.name).sort(),
      missingCriticalPermissions: missingCritical,
      redundantPermissions: redundant,
      securityRisk
    };
  }, [permissions, roles]);

  // 缓存管理
  const clearCache = useCallback(() => {
    clearPermissionCache();
    permissionCache.clear();
  }, [clearPermissionCache]);

  // 清理访问日志
  useEffect(() => {
    const cleanup = () => {
      // 保留最近100条记录
      if (accessLogRef.current.length > 100) {
        accessLogRef.current = accessLogRef.current.slice(-100);
      }
    };

    const interval = setInterval(cleanup, 300000); // 5分钟清理一次
    return () => clearInterval(interval);
  }, []);

  return {
    // 基础权限检查
    can,
    cannot,
    canAll,
    canAny,
    
    // 角色检查
    hasRole,
    hasAnyRole,
    hasAllRoles,
    
    // 资源权限检查
    canRead,
    canWrite,
    canDelete,
    canAdmin,
    getResourceLevel,
    checkResourcePermission,
    
    // 高级权限检查
    canAccess,
    canModify,
    canCreate,
    
    // 批量权限检查
    checkBatch,
    checkResources,
    
    // 条件权限检查
    canIf,
    canWhen,
    
    // 权限信息
    permissions,
    roles,
    user,
    isAuthenticated,
    
    // 调试和分析
    debug,
    trace,
    analyze,
    
    // 缓存管理
    clearCache,
    refreshPermissions
  };
};

// 便捷Hook - 检查单个权限
export const useCanAccess = (permission: string | string[], options?: PermissionCheckOptions) => {
  const { can } = usePermissions(options);
  return can(permission, options);
};

// 便捷Hook - 检查资源权限
export const useResourcePermission = (resource: string, level: PermissionLevel = PermissionLevel.read) => {
  const { checkResources } = usePermissions();
  return useMemo(() => checkResources([resource], level)[resource], [checkResources, resource, level]);
};

// 便捷Hook - 角色检查
export const useRoleCheck = (role: string | string[]) => {
  const { hasRole } = usePermissions();
  return hasRole(role);
};

// 便捷Hook - 批量权限检查
export const useBatchPermissions = (permissions: string[], options?: PermissionCheckOptions) => {
  const { checkBatch } = usePermissions(options);
  return useMemo(() => checkBatch(permissions), [checkBatch, permissions]);
};

export default usePermissions;
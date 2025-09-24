import type { User, Role } from '@/types';

// 权限常量定义
export const PERMISSION_CONSTANTS = {
  // 系统权限
  SYSTEM_ADMIN: 'system.admin',
  SYSTEM_SETTINGS: 'system.settings',
  SYSTEM_MONITOR: 'system.monitor',
  SYSTEM_LOGS: 'system.logs',
  
  // 用户权限
  USER_READ: 'user.read',
  USER_WRITE: 'user.write',
  USER_DELETE: 'user.delete',
  USER_ADMIN: 'user.admin',
  
  // 内容权限
  CONTENT_READ: 'content.read',
  CONTENT_WRITE: 'content.write',
  CONTENT_DELETE: 'content.delete',
  CONTENT_ADMIN: 'content.admin',
  CONTENT_PUBLISH: 'content.publish',
  
  // 角色权限
  ROLE_READ: 'role.read',
  ROLE_WRITE: 'role.write',
  ROLE_DELETE: 'role.delete',
  ROLE_ADMIN: 'role.admin',
  
  // 审计权限
  AUDIT_READ: 'audit.read',
  AUDIT_ADMIN: 'audit.admin',
  
  // 分析权限
  ANALYTICS_READ: 'analytics.read',
  ANALYTICS_ADMIN: 'analytics.admin'
} as const;

// 角色常量定义
export const ROLE_CONSTANTS = {
  SUPER_ADMIN: 'SuperAdmin',
  ADMIN: 'Admin',
  EDITOR: 'Editor',
  AUTHOR: 'Author',
  VIEWER: 'Viewer'
} as const;

// 权限级别定义
export enum PermissionLevel {
  NONE = 0,
  READ = 1,
  WRITE = 2,
  DELETE = 3,
  ADMIN = 4
}

// 权限分组
export const PERMISSION_GROUPS = {
  SYSTEM: [
    PERMISSION_CONSTANTS.SYSTEM_ADMIN,
    PERMISSION_CONSTANTS.SYSTEM_SETTINGS,
    PERMISSION_CONSTANTS.SYSTEM_MONITOR,
    PERMISSION_CONSTANTS.SYSTEM_LOGS
  ],
  USER_MANAGEMENT: [
    PERMISSION_CONSTANTS.USER_READ,
    PERMISSION_CONSTANTS.USER_WRITE,
    PERMISSION_CONSTANTS.USER_DELETE,
    PERMISSION_CONSTANTS.USER_ADMIN
  ],
  CONTENT_MANAGEMENT: [
    PERMISSION_CONSTANTS.CONTENT_READ,
    PERMISSION_CONSTANTS.CONTENT_WRITE,
    PERMISSION_CONSTANTS.CONTENT_DELETE,
    PERMISSION_CONSTANTS.CONTENT_ADMIN,
    PERMISSION_CONSTANTS.CONTENT_PUBLISH
  ],
  ROLE_MANAGEMENT: [
    PERMISSION_CONSTANTS.ROLE_READ,
    PERMISSION_CONSTANTS.ROLE_WRITE,
    PERMISSION_CONSTANTS.ROLE_DELETE,
    PERMISSION_CONSTANTS.ROLE_ADMIN
  ],
  AUDIT: [
    PERMISSION_CONSTANTS.AUDIT_READ,
    PERMISSION_CONSTANTS.AUDIT_ADMIN
  ],
  ANALYTICS: [
    PERMISSION_CONSTANTS.ANALYTICS_READ,
    PERMISSION_CONSTANTS.ANALYTICS_ADMIN
  ]
} as const;

// 权限层次结构
export const PERMISSION_HIERARCHY = {
  [PERMISSION_CONSTANTS.SYSTEM_ADMIN]: Object.values(PERMISSION_CONSTANTS),
  [PERMISSION_CONSTANTS.USER_ADMIN]: PERMISSION_GROUPS.USER_MANAGEMENT,
  [PERMISSION_CONSTANTS.CONTENT_ADMIN]: PERMISSION_GROUPS.CONTENT_MANAGEMENT,
  [PERMISSION_CONSTANTS.ROLE_ADMIN]: PERMISSION_GROUPS.ROLE_MANAGEMENT,
  [PERMISSION_CONSTANTS.AUDIT_ADMIN]: PERMISSION_GROUPS.AUDIT,
  [PERMISSION_CONSTANTS.ANALYTICS_ADMIN]: PERMISSION_GROUPS.ANALYTICS
} as const;

// 权限依赖关系
export const PERMISSION_DEPENDENCIES = {
  [PERMISSION_CONSTANTS.USER_WRITE]: [PERMISSION_CONSTANTS.USER_READ],
  [PERMISSION_CONSTANTS.USER_DELETE]: [PERMISSION_CONSTANTS.USER_READ, PERMISSION_CONSTANTS.USER_WRITE],
  [PERMISSION_CONSTANTS.CONTENT_WRITE]: [PERMISSION_CONSTANTS.CONTENT_READ],
  [PERMISSION_CONSTANTS.CONTENT_DELETE]: [PERMISSION_CONSTANTS.CONTENT_READ, PERMISSION_CONSTANTS.CONTENT_WRITE],
  [PERMISSION_CONSTANTS.CONTENT_PUBLISH]: [PERMISSION_CONSTANTS.CONTENT_READ, PERMISSION_CONSTANTS.CONTENT_WRITE],
  [PERMISSION_CONSTANTS.ROLE_WRITE]: [PERMISSION_CONSTANTS.ROLE_READ],
  [PERMISSION_CONSTANTS.ROLE_DELETE]: [PERMISSION_CONSTANTS.ROLE_READ, PERMISSION_CONSTANTS.ROLE_WRITE]
} as const;

interface CacheItem {
  data: unknown;
  timestamp: number;
  ttl: number;
}

interface PermissionSummary {
  total: number;
  byLevel: Record<string, number>;
  byResource: Record<string, number>;
  highRisk: string[];
}

interface RoleValidationResult {
  valid: boolean;
  errors: string[];
  warnings: string[];
}

interface AuditResult {
  summary: PermissionSummary;
  issues: string[];
  recommendations: string[];
}

interface PermissionReport {
  totalUsers: number;
  permissionDistribution: Record<string, number>;
  riskAssessment: string;
  recommendations: string[];
}

// 权限工具类
export class PermissionUtils {
  /**
   * 检查权限字符串格式是否正确
   */
  static validatePermissionFormat(permission: string): boolean {
    const pattern = /^[a-z]+(\.[a-z]+)*$/;
    return pattern.test(permission);
  }

  /**
   * 标准化权限名称
   */
  static normalizePermission(permission: string): string {
    return permission.toLowerCase().trim();
  }

  /**
   * 解析权限字符串
   */
  static parsePermission(permission: string): { 
    resource: string; 
    action: string; 
    scope?: string; 
  } {
    const parts = permission.split('.');
    
    if (parts.length < 2) {
      throw new Error(`无效的权限格式: ${permission}`);
    }

    return {
      resource: parts[0],
      action: parts[1],
      scope: parts[2]
    };
  }

  /**
   * 构建权限字符串
   */
  static buildPermission(resource: string, action: string, scope?: string): string {
    const parts = [resource, action];
    if (scope) parts.push(scope);
    return parts.join('.');
  }

  /**
   * 检查权限是否匹配模式
   */
  static matchesPattern(permission: string, pattern: string): boolean {
    // 支持通配符匹配
    const regexPattern = pattern
      .replace(/\./g, '\\.')
      .replace(/\*/g, '.*')
      .replace(/\?/g, '.');
    
    const regex = new RegExp(`^${regexPattern}$`);
    return regex.test(permission);
  }

  /**
   * 获取权限的父级权限列表
   */
  static getParentPermissions(permission: string): string[] {
    const parts = permission.split('.');
    const parents: string[] = [];
    
    for (let i = parts.length - 1; i > 0; i--) {
      const parent = parts.slice(0, i).join('.') + '.admin';
      parents.push(parent);
    }
    
    return parents;
  }

  /**
   * 获取权限的子级权限列表
   */
  static getChildPermissions(permission: string, allPermissions: string[]): string[] {
    return allPermissions.filter(perm => 
      perm.startsWith(permission + '.') && perm !== permission
    );
  }

  /**
   * 检查权限依赖关系
   */
  static checkDependencies(permission: string, userPermissions: string[]): {
    satisfied: boolean;
    missing: string[];
  } {
    const dependencies = PERMISSION_DEPENDENCIES[permission as keyof typeof PERMISSION_DEPENDENCIES] || [];
    const missing = dependencies.filter(dep => !userPermissions.includes(dep));
    
    return {
      satisfied: missing.length === 0,
      missing
    };
  }

  /**
   * 计算有效权限（包括继承的权限）
   */
  static calculateEffectivePermissions(userPermissions: string[]): string[] {
    const effective = new Set(userPermissions);
    
    // 添加继承的权限
    userPermissions.forEach(permission => {
      const inherited = PERMISSION_HIERARCHY[permission as keyof typeof PERMISSION_HIERARCHY];
      if (inherited) {
        inherited.forEach(perm => effective.add(perm));
      }
    });
    
    return Array.from(effective);
  }

  /**
   * 权限差集计算
   */
  static permissionDiff(
    currentPermissions: string[], 
    targetPermissions: string[]
  ): {
    toAdd: string[];
    toRemove: string[];
    unchanged: string[];
  } {
    const current = new Set(currentPermissions);
    const target = new Set(targetPermissions);
    
    return {
      toAdd: targetPermissions.filter(perm => !current.has(perm)),
      toRemove: currentPermissions.filter(perm => !target.has(perm)),
      unchanged: currentPermissions.filter(perm => target.has(perm))
    };
  }

  /**
   * 权限排序
   */
  static sortPermissions(permissions: string[]): string[] {
    return permissions.sort((a, b) => {
      const aLevel = this.getPermissionLevel(a);
      const bLevel = this.getPermissionLevel(b);
      
      if (aLevel !== bLevel) {
        return bLevel - aLevel; // 高级权限在前
      }
      
      return a.localeCompare(b); // 字母顺序
    });
  }

  /**
   * 获取权限级别
   */
  static getPermissionLevel(permission: string): number {
    if (permission.includes('.admin')) return 4;
    if (permission.includes('.delete')) return 3;
    if (permission.includes('.write') || permission.includes('.create') || permission.includes('.update')) return 2;
    if (permission.includes('.read')) return 1;
    return 0;
  }

  /**
   * 权限分组
   */
  static groupPermissions(permissions: string[]): Record<string, string[]> {
    const groups: Record<string, string[]> = {};
    
    permissions.forEach(permission => {
      const resource = permission.split('.')[0];
      if (!groups[resource]) {
        groups[resource] = [];
      }
      groups[resource].push(permission);
    });
    
    return groups;
  }

  /**
   * 检查权限冲突
   */
  static checkPermissionConflicts(permissions: string[]): string[] {
    const conflicts: string[] = [];
    const permissionSet = new Set(permissions);
    
    // 检查相互排斥的权限
    const exclusiveGroups = [
      ['user.read_only', 'user.write'],
      ['content.draft_only', 'content.publish']
    ];
    
    exclusiveGroups.forEach(group => {
      const conflicting = group.filter(perm => permissionSet.has(perm));
      if (conflicting.length > 1) {
        conflicts.push(`冲突权限: ${conflicting.join(', ')}`);
      }
    });
    
    return conflicts;
  }

  /**
   * 生成权限摘要
   */
  static generatePermissionSummary(permissions: string[]): PermissionSummary {
    const byLevel: Record<string, number> = { admin: 0, delete: 0, write: 0, read: 0, other: 0 };
    const byResource: Record<string, number> = {};
    const highRisk: string[] = [];
    
    permissions.forEach(permission => {
      // 按级别统计
      const level = this.getPermissionLevel(permission);
      if (level === 4) byLevel.admin++;
      else if (level === 3) byLevel.delete++;
      else if (level === 2) byLevel.write++;
      else if (level === 1) byLevel.read++;
      else byLevel.other++;
      
      // 按资源统计
      const resource = permission.split('.')[0];
      byResource[resource] = (byResource[resource] || 0) + 1;
      
      // 高风险权限
      if (permission.includes('admin') || permission.includes('system') || permission === '*') {
        highRisk.push(permission);
      }
    });
    
    return {
      total: permissions.length,
      byLevel,
      byResource,
      highRisk
    };
  }
}

// 角色工具类
export class RoleUtils {
  /**
   * 获取角色层次结构
   */
  static getRoleHierarchy(): Record<string, number> {
    return {
      [ROLE_CONSTANTS.SUPER_ADMIN]: 5,
      [ROLE_CONSTANTS.ADMIN]: 4,
      [ROLE_CONSTANTS.EDITOR]: 3,
      [ROLE_CONSTANTS.AUTHOR]: 2,
      [ROLE_CONSTANTS.VIEWER]: 1
    };
  }

  /**
   * 比较角色级别
   */
  static compareRoles(role1: string, role2: string): number {
    const hierarchy = this.getRoleHierarchy();
    const level1 = hierarchy[role1] || 0;
    const level2 = hierarchy[role2] || 0;
    return level1 - level2;
  }

  /**
   * 检查角色是否有权限访问另一个角色
   */
  static canManageRole(managerRole: string, targetRole: string): boolean {
    return this.compareRoles(managerRole, targetRole) > 0;
  }

  /**
   * 获取角色的默认权限
   */
  static getDefaultPermissions(roleName: string): string[] {
    const rolePermissions: Record<string, string[]> = {
      [ROLE_CONSTANTS.SUPER_ADMIN]: Object.values(PERMISSION_CONSTANTS),
      [ROLE_CONSTANTS.ADMIN]: [
        ...PERMISSION_GROUPS.USER_MANAGEMENT,
        ...PERMISSION_GROUPS.CONTENT_MANAGEMENT,
        ...PERMISSION_GROUPS.ROLE_MANAGEMENT.filter(p => !p.includes('admin')),
        PERMISSION_CONSTANTS.AUDIT_READ,
        PERMISSION_CONSTANTS.ANALYTICS_READ
      ],
      [ROLE_CONSTANTS.EDITOR]: [
        PERMISSION_CONSTANTS.USER_READ,
        ...PERMISSION_GROUPS.CONTENT_MANAGEMENT.filter(p => !p.includes('delete')),
        PERMISSION_CONSTANTS.ANALYTICS_READ
      ],
      [ROLE_CONSTANTS.AUTHOR]: [
        PERMISSION_CONSTANTS.CONTENT_READ,
        PERMISSION_CONSTANTS.CONTENT_WRITE
      ],
      [ROLE_CONSTANTS.VIEWER]: [
        PERMISSION_CONSTANTS.CONTENT_READ,
        PERMISSION_CONSTANTS.USER_READ,
        PERMISSION_CONSTANTS.ANALYTICS_READ
      ]
    };
    
    return rolePermissions[roleName] || [];
  }

  /**
   * 验证角色配置
   */
  static validateRoleConfiguration(role: Role): RoleValidationResult {
    const errors: string[] = [];
    const warnings: string[] = [];
    
    // 检查角色名称
    if (!role.name || role.name.trim() === '') {
      errors.push('角色名称不能为空');
    }
    
    // 检查权限格式
    role.permissions.forEach(permission => {
      if (!PermissionUtils.validatePermissionFormat(permission.code)) {
        errors.push(`权限格式错误: ${permission.code}`);
      }
    });
    
    // 检查权限依赖
    const userPermissions = role.permissions.map(p => p.code);
    userPermissions.forEach(permission => {
      const deps = PermissionUtils.checkDependencies(permission, userPermissions);
      if (!deps.satisfied) {
        warnings.push(`权限 ${permission} 缺少依赖: ${deps.missing.join(', ')}`);
      }
    });
    
    // 检查权限冲突
    const conflicts = PermissionUtils.checkPermissionConflicts(userPermissions);
    conflicts.forEach(conflict => warnings.push(conflict));
    
    return {
      valid: errors.length === 0,
      errors,
      warnings
    };
  }
}

// 权限表达式解析器
export class PermissionExpressionParser {
  /**
   * 解析权限表达式
   * 支持格式: "user.read OR content.write", "admin.* AND NOT temp.*"
   */
  static parse(expression: string): (permissions: string[]) => boolean {
    // 简化实现，支持基本的 AND, OR, NOT 操作
    const normalizedExpr = expression
      .replace(/\bAND\b/gi, '&&')
      .replace(/\bOR\b/gi, '||')
      .replace(/\bNOT\b/gi, '!');
    
    return (permissions: string[]): boolean => {
      try {
        // 替换权限检查
        let evaluableExpr = normalizedExpr;
        
        // 提取所有权限模式
        const patterns = evaluableExpr.match(/[a-z]+\.[a-z*?]+/g) || [];
        
        patterns.forEach(pattern => {
          const hasPermission = permissions.some(perm => 
            PermissionUtils.matchesPattern(perm, pattern)
          );
          evaluableExpr = evaluableExpr.replace(pattern, hasPermission.toString());
        });
        
        // 安全地评估表达式
        return new Function('return ' + evaluableExpr)();
      } catch (error) {
        console.error('权限表达式解析错误:', error);
        return false;
      }
    };
  }
}

// 权限缓存管理器
export class PermissionCacheManager {
  private static cache = new Map<string, CacheItem>();
  
  /**
   * 设置缓存
   */
  static set(key: string, data: unknown, ttl: number = 300000): void {
    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      ttl
    });
  }
  
  /**
   * 获取缓存
   */
  static get<T>(key: string): T | null {
    const item = this.cache.get(key);
    
    if (!item) return null;
    
    if (Date.now() - item.timestamp > item.ttl) {
      this.cache.delete(key);
      return null;
    }
    
    return item.data as T;
  }
  
  /**
   * 清除缓存
   */
  static clear(pattern?: string): void {
    if (!pattern) {
      this.cache.clear();
      return;
    }
    
    const regex = new RegExp(pattern);
    Array.from(this.cache.keys()).forEach(key => {
      if (regex.test(key)) {
        this.cache.delete(key);
      }
    });
  }
  
  /**
   * 获取缓存统计
   */
  static getStats(): {
    size: number;
    expired: number;
    memoryUsage: number;
  } {
    let expired = 0;
    const now = Date.now();
    
    this.cache.forEach((item) => {
      if (now - item.timestamp > item.ttl) {
        expired++;
      }
    });
    
    return {
      size: this.cache.size,
      expired,
      memoryUsage: JSON.stringify(Array.from(this.cache.entries())).length
    };
  }
}

// 权限审计工具
export class PermissionAuditor {
  /**
   * 审计用户权限
   */
  static auditUserPermissions(user: User): AuditResult {
    const permissions = user.roles.flatMap(role => role.permissions.map(p => p.code));
    const summary = PermissionUtils.generatePermissionSummary(permissions);
    const issues: string[] = [];
    const recommendations: string[] = [];
    
    // 检查过度权限
    if (summary.highRisk.length > 0) {
      issues.push(`用户拥有 ${summary.highRisk.length} 个高风险权限`);
    }
    
    // 检查权限冲突
    const conflicts = PermissionUtils.checkPermissionConflicts(permissions);
    issues.push(...conflicts);
    
    // 建议
    if (summary.total > 50) {
      recommendations.push('考虑简化权限结构，权限过多可能导致管理困难');
    }
    
    if (summary.byLevel.admin > 5) {
      recommendations.push('管理员权限过多，建议仔细审查必要性');
    }
    
    return { summary, issues, recommendations };
  }
  
  /**
   * 生成权限报告
   */
  static generateReport(users: User[]): PermissionReport {
    const permissionCounts: Record<string, number> = {};
    let highRiskUsers = 0;
    
    users.forEach(user => {
      const audit = this.auditUserPermissions(user);
      
      if (audit.issues.length > 0) {
        highRiskUsers++;
      }
      
      user.roles.forEach(role => {
        role.permissions.forEach(permission => {
          permissionCounts[permission.code] = (permissionCounts[permission.code] || 0) + 1;
        });
      });
    });
    
    const riskPercentage = (highRiskUsers / users.length) * 100;
    let riskAssessment = 'low';
    if (riskPercentage > 20) riskAssessment = 'high';
    else if (riskPercentage > 10) riskAssessment = 'medium';
    
    const recommendations: string[] = [];
    if (highRiskUsers > 0) {
      recommendations.push(`${highRiskUsers} 个用户存在权限风险，需要审查`);
    }
    
    return {
      totalUsers: users.length,
      permissionDistribution: permissionCounts,
      riskAssessment,
      recommendations
    };
  }
}

// 便捷函数
export const hasPermission = (userPermissions: string[], requiredPermission: string): boolean => {
  return PermissionUtils.calculateEffectivePermissions(userPermissions).includes(requiredPermission);
};

export const hasAnyPermission = (userPermissions: string[], requiredPermissions: string[]): boolean => {
  const effective = PermissionUtils.calculateEffectivePermissions(userPermissions);
  return requiredPermissions.some(perm => effective.includes(perm));
};

export const hasAllPermissions = (userPermissions: string[], requiredPermissions: string[]): boolean => {
  const effective = PermissionUtils.calculateEffectivePermissions(userPermissions);
  return requiredPermissions.every(perm => effective.includes(perm));
};

export const canManageUser = (managerRoles: string[], targetRoles: string[]): boolean => {
  const managerLevel = Math.max(...managerRoles.map(role => RoleUtils.getRoleHierarchy()[role] || 0));
  const targetLevel = Math.max(...targetRoles.map(role => RoleUtils.getRoleHierarchy()[role] || 0));
  return managerLevel > targetLevel;
};

export default {
  PermissionUtils,
  RoleUtils,
  PermissionExpressionParser,
  PermissionCacheManager,
  PermissionAuditor,
  PERMISSION_CONSTANTS,
  ROLE_CONSTANTS,
  PERMISSION_GROUPS,
  hasPermission,
  hasAnyPermission,
  hasAllPermissions,
  canManageUser
};
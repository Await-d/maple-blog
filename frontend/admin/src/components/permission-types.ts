import React from 'react';
import { PermissionLevel } from '@/contexts/PermissionContext';

// 权限守卫模式
export enum GuardMode {
  REDIRECT = 'redirect',      // 重定向到指定页面
  RENDER = 'render',          // 渲染替代内容
  HIDE = 'hide',              // 隐藏组件
  REPLACE = 'replace'         // 替换为指定组件
}

// 权限守卫配置
export interface PermissionGuardConfig {
  // 权限要求
  permissions?: string | string[];
  roles?: string | string[];
  resources?: { resource: string; level: PermissionLevel }[];

  // 检查模式
  mode?: 'all' | 'any';       // all: 所有权限都必须满足, any: 满足任意权限即可
  strict?: boolean;           // 严格模式

  // 守卫行为
  guardMode?: GuardMode;
  fallback?: React.ReactNode;
  redirectTo?: string;

  // 自定义检查
  customCheck?: (permissions: unknown) => boolean;

  // 错误处理
  onAccessDenied?: (reason: string) => void;
  showAccessDenied?: boolean;

  // 加载状态
  loading?: React.ReactNode;

  // 调试
  debug?: boolean;
}

// 权限守卫属性
export interface PermissionGuardProps extends PermissionGuardConfig {
  children: React.ReactNode;
  className?: string;
  style?: React.CSSProperties;
}

// 访问拒绝页面属性
export interface AccessDeniedProps {
  reason?: string;
  permissions?: string[];
  onRetry?: () => void;
  showDetails?: boolean;
  redirectTo?: string;
}
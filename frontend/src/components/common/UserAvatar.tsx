/**
 * 用户头像组件
 * 支持多种尺寸、在线状态显示、角色标识等
 */

import React, { useState } from 'react';
import { CommentAuthor } from '../../types/comment';

interface UserAvatarProps {
  user?: CommentAuthor | null;
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  showStatus?: boolean;
  showRole?: boolean;
  className?: string;
  onClick?: () => void;
}

const UserAvatar: React.FC<UserAvatarProps> = ({
  user,
  size = 'md',
  showStatus = false,
  showRole = false,
  className = '',
  onClick
}) => {
  const [imageError, setImageError] = useState(false);

  // 尺寸映射
  const sizeClasses = {
    xs: 'w-6 h-6 text-xs',
    sm: 'w-8 h-8 text-sm',
    md: 'w-10 h-10 text-base',
    lg: 'w-12 h-12 text-lg',
    xl: 'w-16 h-16 text-xl'
  };

  // 状态指示器尺寸
  const statusSizes = {
    xs: 'w-1.5 h-1.5 -bottom-0 -right-0',
    sm: 'w-2 h-2 -bottom-0.5 -right-0.5',
    md: 'w-2.5 h-2.5 -bottom-0.5 -right-0.5',
    lg: 'w-3 h-3 -bottom-1 -right-1',
    xl: 'w-4 h-4 -bottom-1 -right-1'
  };

  // 生成用户名首字母
  const getInitials = (name?: string) => {
    if (!name) return '?';
    return name
      .split(' ')
      .map(n => n[0])
      .join('')
      .slice(0, 2)
      .toUpperCase();
  };

  // 生成背景色（基于用户名）
  const getBackgroundColor = (name?: string) => {
    if (!name) return 'bg-gray-500';

    const colors = [
      'bg-red-500',
      'bg-yellow-500',
      'bg-green-500',
      'bg-blue-500',
      'bg-indigo-500',
      'bg-purple-500',
      'bg-pink-500',
      'bg-teal-500',
      'bg-orange-500',
      'bg-cyan-500'
    ];

    const index = name.charCodeAt(0) % colors.length;
    return colors[index];
  };

  // 角色颜色映射
  const getRoleColor = (role?: string) => {
    switch (role?.toLowerCase()) {
      case 'admin':
        return 'bg-red-500 text-white';
      case 'moderator':
        return 'bg-orange-500 text-white';
      case 'author':
        return 'bg-blue-500 text-white';
      case 'vip':
        return 'bg-yellow-500 text-black';
      default:
        return 'bg-gray-500 text-white';
    }
  };

  const displayName = user?.displayName || user?.username || 'Unknown';
  const avatarClasses = `
    ${sizeClasses[size]}
    relative inline-flex items-center justify-center rounded-full overflow-hidden
    ${onClick ? 'cursor-pointer hover:opacity-80 transition-opacity' : ''}
    ${className}
  `;

  return (
    <div className={avatarClasses} onClick={onClick} title={displayName}>
      {/* 头像图片或首字母 */}
      {user?.avatarUrl && !imageError ? (
        <img
          src={user.avatarUrl}
          alt={`${displayName}的头像`}
          className="w-full h-full object-cover"
          onError={() => setImageError(true)}
          loading="lazy"
        />
      ) : (
        <div
          className={`
            w-full h-full flex items-center justify-center font-semibold text-white
            ${getBackgroundColor(displayName)}
          `}
        >
          {getInitials(displayName)}
        </div>
      )}

      {/* 在线状态指示器 */}
      {showStatus && (
        <div
          className={`
            absolute border-2 border-white dark:border-gray-800 rounded-full
            bg-green-400 ${statusSizes[size]}
          `}
          title="在线"
        />
      )}

      {/* 角色标识 */}
      {showRole && user?.role && user.role !== 'User' && (
        <div
          className={`
            absolute -top-1 -right-1 text-xs px-1 py-0.5 rounded-full font-bold
            ${getRoleColor(user.role)}
          `}
          style={{ fontSize: size === 'xs' ? '0.6rem' : '0.7rem' }}
        >
          {user.role === 'Admin' ? 'A' : user.role === 'Moderator' ? 'M' : user.role[0]}
        </div>
      )}

      {/* VIP标识 */}
      {user?.isVip && (
        <div
          className="absolute -top-1 -right-1 bg-yellow-500 text-black text-xs px-1 py-0.5 rounded-full font-bold"
          style={{ fontSize: size === 'xs' ? '0.5rem' : '0.6rem' }}
          title="VIP用户"
        >
          ⭐
        </div>
      )}
    </div>
  );
};

export default UserAvatar;
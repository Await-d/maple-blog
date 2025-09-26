/**
 * 输入状态指示器组件
 * 显示正在输入评论的用户
 */

import React from 'react';
import { TypingUser } from '../../../types/comment';

interface TypingIndicatorProps {
  users: TypingUser[];
  className?: string;
  maxDisplay?: number;
}

const TypingIndicator: React.FC<TypingIndicatorProps> = ({
  users,
  className = '',
  maxDisplay = 3
}) => {
  if (users.length === 0) return null;

  const displayUsers = users.slice(0, maxDisplay);
  const remainingCount = users.length - maxDisplay;

  const formatUserList = () => {
    if (displayUsers.length === 1) {
      return `${displayUsers[0].userName} 正在输入`;
    }

    if (displayUsers.length === 2) {
      return `${displayUsers[0].userName} 和 ${displayUsers[1].userName} 正在输入`;
    }

    if (displayUsers.length === 3) {
      if (remainingCount > 0) {
        return `${displayUsers[0].userName}、${displayUsers[1].userName} 等 ${users.length} 人正在输入`;
      }
      return `${displayUsers[0].userName}、${displayUsers[1].userName} 和 ${displayUsers[2].userName} 正在输入`;
    }

    return `${users.length} 人正在输入`;
  };

  return (
    <div className={`typing-indicator flex items-center space-x-2 text-sm text-gray-500 dark:text-gray-400 ${className}`}>
      {/* 动画点 */}
      <div className="flex space-x-1">
        <div className="w-2 h-2 bg-blue-500 rounded-full animate-bounce [animation-delay:-0.3s]"></div>
        <div className="w-2 h-2 bg-blue-500 rounded-full animate-bounce [animation-delay:-0.15s]"></div>
        <div className="w-2 h-2 bg-blue-500 rounded-full animate-bounce"></div>
      </div>

      {/* 用户信息 */}
      <span className="italic">
        {formatUserList()}...
      </span>
    </div>
  );
};

export default TypingIndicator;
/**
 * @提及用户建议组件
 * 在输入 @ 符号后显示用户建议列表
 */

import React, { useState, useEffect, useRef } from 'react';
import { userApi } from '../../../services/userApi';
import UserAvatar from '../../../components/common/UserAvatar';

interface User {
  id: string;
  username: string;
  displayName: string;
  avatarUrl?: string;
  role: string;
  isVip: boolean;
}

interface MentionSuggestionsProps {
  query: string;
  onSelect: (user: User) => void;
  onClose: () => void;
  className?: string;
}

const MentionSuggestions: React.FC<MentionSuggestionsProps> = ({
  query,
  onSelect,
  onClose,
  className = ''
}) => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const containerRef = useRef<HTMLDivElement>(null);

  // 搜索用户
  useEffect(() => {
    const searchUsers = async () => {
      if (query.length < 1) {
        setUsers([]);
        return;
      }

      setLoading(true);
      try {
        // 这里应该调用实际的用户搜索API
        const result = await userApi.searchUsers(query, 10);
        setUsers(result || []);
        setSelectedIndex(0);
      } catch (error) {
        console.error('Search users failed:', error);
        setUsers([]);
      } finally {
        setLoading(false);
      }
    };

    const debounceTimer = setTimeout(searchUsers, 300);
    return () => clearTimeout(debounceTimer);
  }, [query]);

  // 键盘导航
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      switch (e.key) {
        case 'ArrowDown':
          e.preventDefault();
          setSelectedIndex(prev => Math.min(prev + 1, users.length - 1));
          break;
        case 'ArrowUp':
          e.preventDefault();
          setSelectedIndex(prev => Math.max(prev - 1, 0));
          break;
        case 'Enter':
          e.preventDefault();
          if (users[selectedIndex]) {
            onSelect(users[selectedIndex]);
          }
          break;
        case 'Escape':
          e.preventDefault();
          onClose();
          break;
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [users, selectedIndex, onSelect, onClose]);

  // 点击外部关闭
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        onClose();
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [onClose]);

  if (!query || (users.length === 0 && !loading)) {
    return null;
  }

  return (
    <div
      ref={containerRef}
      className={`absolute z-50 mt-1 w-64 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg ${className}`}
    >
      {/* 标题 */}
      <div className="px-3 py-2 text-xs font-medium text-gray-500 dark:text-gray-400 border-b border-gray-200 dark:border-gray-700">
        提及用户
      </div>

      {/* 加载状态 */}
      {loading && (
        <div className="px-3 py-4 text-center">
          <div className="inline-flex items-center space-x-2 text-sm text-gray-500 dark:text-gray-400">
            <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            <span>搜索用户中...</span>
          </div>
        </div>
      )}

      {/* 用户列表 */}
      {!loading && users.length > 0 && (
        <div className="max-h-60 overflow-y-auto">
          {users.map((user, index) => (
            <button
              key={user.id}
              onClick={() => onSelect(user)}
              className={`
                w-full px-3 py-2 text-left flex items-center space-x-3
                hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors
                ${index === selectedIndex ? 'bg-blue-50 dark:bg-blue-900/20' : ''}
              `}
              onMouseEnter={() => setSelectedIndex(index)}
            >
              <UserAvatar user={user} size="xs" />

              <div className="flex-1 min-w-0">
                <div className="flex items-center space-x-2">
                  <span className="font-medium text-gray-900 dark:text-gray-100 truncate">
                    {user.displayName}
                  </span>

                  {user.role !== 'User' && (
                    <span className="inline-flex items-center px-1.5 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200">
                      {user.role}
                    </span>
                  )}

                  {user.isVip && (
                    <span className="inline-flex items-center px-1.5 py-0.5 rounded text-xs font-medium bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200">
                      VIP
                    </span>
                  )}
                </div>

                <div className="text-sm text-gray-500 dark:text-gray-400 truncate">
                  @{user.username}
                </div>
              </div>
            </button>
          ))}
        </div>
      )}

      {/* 空状态 */}
      {!loading && users.length === 0 && query.length > 0 && (
        <div className="px-3 py-4 text-center text-sm text-gray-500 dark:text-gray-400">
          没有找到匹配的用户
        </div>
      )}

      {/* 快捷键提示 */}
      <div className="px-3 py-2 text-xs text-gray-400 dark:text-gray-500 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-750 rounded-b-lg">
        ↑↓ 选择 · Enter 确认 · Esc 取消
      </div>
    </div>
  );
};

export default MentionSuggestions;
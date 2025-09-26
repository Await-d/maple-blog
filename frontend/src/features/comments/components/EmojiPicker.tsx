/**
 * 表情选择器组件
 * 提供常用表情的快速选择
 */

import React, { useState, useRef, useEffect } from 'react';

interface EmojiPickerProps {
  onEmojiSelect: (emoji: string) => void;
  onClose: () => void;
  className?: string;
}

const EmojiPicker: React.FC<EmojiPickerProps> = ({
  onEmojiSelect,
  onClose,
  className = ''
}) => {
  const [selectedCategory, setSelectedCategory] = useState('smileys');
  const containerRef = useRef<HTMLDivElement>(null);

  // 表情数据
  const emojiCategories = {
    smileys: {
      name: '笑脸',
      icon: '😀',
      emojis: [
        '😀', '😃', '😄', '😁', '😆', '😅', '😂', '🤣',
        '😊', '😇', '🙂', '🙃', '😉', '😌', '😍', '🥰',
        '😘', '😗', '😙', '😚', '😋', '😛', '😝', '😜',
        '🤪', '🤨', '🧐', '🤓', '😎', '🤩', '🥳', '😏'
      ]
    },
    emotions: {
      name: '情感',
      icon: '😢',
      emojis: [
        '😒', '😞', '😔', '😟', '😕', '🙁', '☹️', '😣',
        '😖', '😫', '😩', '🥺', '😢', '😭', '😤', '😠',
        '😡', '🤬', '🤯', '😳', '🥵', '🥶', '😱', '😨',
        '😰', '😥', '😓', '🤗', '🤔', '🤭', '🤫', '🤥'
      ]
    },
    gestures: {
      name: '手势',
      icon: '👍',
      emojis: [
        '👍', '👎', '👌', '🤌', '🤏', '✌️', '🤞', '🤟',
        '🤘', '🤙', '👈', '👉', '👆', '🖕', '👇', '☝️',
        '👋', '🤚', '🖐️', '✋', '🖖', '👏', '🙌', '🤲',
        '🤝', '🙏', '✊', '👊', '🤛', '🤜', '👎', '👍'
      ]
    },
    objects: {
      name: '物品',
      icon: '❤️',
      emojis: [
        '❤️', '🧡', '💛', '💚', '💙', '💜', '🖤', '🤍',
        '🤎', '💔', '❣️', '💕', '💞', '💓', '💗', '💖',
        '💘', '💝', '💟', '☮️', '✝️', '☪️', '🕉️', '☸️',
        '✡️', '🔯', '🕎', '☯️', '☦️', '🛐', '⛎', '♈'
      ]
    }
  };

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

  // ESC键关闭
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  const handleEmojiClick = (emoji: string) => {
    onEmojiSelect(emoji);
  };

  const currentEmojis = emojiCategories[selectedCategory as keyof typeof emojiCategories]?.emojis || [];

  return (
    <div
      ref={containerRef}
      className={`absolute z-50 mt-1 w-80 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg ${className}`}
    >
      {/* 标题 */}
      <div className="px-3 py-2 text-sm font-medium text-gray-900 dark:text-gray-100 border-b border-gray-200 dark:border-gray-700">
        选择表情
      </div>

      {/* 分类选择 */}
      <div className="flex border-b border-gray-200 dark:border-gray-700">
        {Object.entries(emojiCategories).map(([key, category]) => (
          <button
            key={key}
            onClick={() => setSelectedCategory(key)}
            className={`
              flex-1 px-3 py-2 text-center text-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors
              ${selectedCategory === key
                ? 'bg-blue-50 dark:bg-blue-900/20 border-b-2 border-blue-500'
                : ''
              }
            `}
            title={category.name}
          >
            {category.icon}
          </button>
        ))}
      </div>

      {/* 表情网格 */}
      <div className="p-2 max-h-64 overflow-y-auto">
        <div className="grid grid-cols-8 gap-1">
          {currentEmojis.map((emoji, index) => (
            <button
              key={`${emoji}-${index}`}
              onClick={() => handleEmojiClick(emoji)}
              className="w-8 h-8 text-lg hover:bg-gray-100 dark:hover:bg-gray-700 rounded transition-colors flex items-center justify-center"
              title={emoji}
            >
              {emoji}
            </button>
          ))}
        </div>
      </div>

      {/* 底部提示 */}
      <div className="px-3 py-2 text-xs text-gray-400 dark:text-gray-500 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-750 rounded-b-lg text-center">
        点击选择表情
      </div>
    </div>
  );
};

export default EmojiPicker;
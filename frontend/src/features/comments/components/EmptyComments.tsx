/**
 * 空评论状态组件
 * 当没有评论时显示
 */

import React from 'react';

interface EmptyCommentsProps {
  postId: string;
  className?: string;
  showCallToAction?: boolean;
}

const EmptyComments: React.FC<EmptyCommentsProps> = ({
  postId: _postId,
  className = '',
  showCallToAction = true
}) => {
  return (
    <div className={`text-center py-12 ${className}`}>
      <div className="mx-auto w-24 h-24 mb-4 text-gray-300 dark:text-gray-600">
        <svg
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          className="w-full h-full"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={1}
            d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"
          />
        </svg>
      </div>

      <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">
        还没有评论
      </h3>

      <p className="text-gray-500 dark:text-gray-400 mb-6 max-w-md mx-auto">
        成为第一个分享想法的人！你的评论可以帮助其他读者更好地理解这篇文章。
      </p>

      {showCallToAction && (
        <div className="space-y-3">
          <button
            onClick={() => {
              // 滚动到评论表单
              const commentForm = document.getElementById('comment-form');
              commentForm?.scrollIntoView({ behavior: 'smooth', block: 'center' });

              // 聚焦到评论表单
              const textarea = commentForm?.querySelector('textarea');
              textarea?.focus();
            }}
            className="inline-flex items-center px-4 py-2 bg-blue-600 hover:bg-blue-700
                       text-white font-medium rounded-lg transition-colors duration-200"
          >
            <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"
              />
            </svg>
            写下第一条评论
          </button>

          <div className="text-sm text-gray-400 dark:text-gray-500">
            <p>💡 小贴士：</p>
            <ul className="mt-2 space-y-1 text-left max-w-md mx-auto">
              <li>• 分享你的见解和想法</li>
              <li>• 提出相关问题</li>
              <li>• 保持友善和尊重</li>
              <li>• 支持 Markdown 格式</li>
            </ul>
          </div>
        </div>
      )}
    </div>
  );
};

export default EmptyComments;
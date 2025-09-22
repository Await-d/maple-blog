// @ts-nocheck
/**
 * 评论系统使用示例
 * 展示如何在不同场景下使用评论系统
 */

import React from 'react';
import CommentSystem from '../CommentSystem';
import { CommentSortOrder } from '../../../types/comment';

// 基本使用示例
export const BasicCommentExample: React.FC = () => {
  return (
    <div className="max-w-4xl mx-auto p-6">
      <h1 className="text-2xl font-bold mb-6">基本评论系统示例</h1>

      {/* 文章内容示例 */}
      <article className="prose prose-lg max-w-none mb-8">
        <h2>示例文章标题</h2>
        <p>
          这里是文章内容。用户可以在下方的评论系统中进行讨论，
          支持实时通知、嵌套回复、富文本编辑等功能。
        </p>
        <p>
          评论系统会根据设备类型自动适配，在移动设备上提供触摸友好的界面，
          在桌面设备上提供完整的功能体验。
        </p>
      </article>

      {/* 基本评论系统 */}
      <CommentSystem
        postId="example-post-1"
        initialSort={CommentSortOrder.CreatedAtDesc}
        showStats={true}
        showNotifications={true}
        maxDepth={3}
        enableRealtime={true}
        autoRefresh={false}
      />
    </div>
  );
};

// 紧凑模式示例（适合侧边栏等空间较小的场景）
export const CompactCommentExample: React.FC = () => {
  return (
    <div className="max-w-sm mx-auto p-4">
      <h2 className="text-lg font-semibold mb-4">紧凑评论系统</h2>

      <CommentSystem
        postId="example-post-2"
        initialSort={CommentSortOrder.HotScore}
        showStats={false}
        showNotifications={false}
        maxDepth={2}
        className="text-sm"
        enableRealtime={true}
      />
    </div>
  );
};

// 嵌入式评论系统（用于卡片等组件内）
export const EmbeddedCommentExample: React.FC = () => {
  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 max-w-2xl mx-auto">
      <div className="mb-4">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
          产品讨论卡片
        </h3>
        <p className="text-gray-600 dark:text-gray-400 text-sm mt-1">
          分享你对这个产品的看法
        </p>
      </div>

      <CommentSystem
        postId="product-discussion-1"
        initialSort={CommentSortOrder.LikeCountDesc}
        showStats={true}
        showNotifications={false}
        maxDepth={2}
        enableRealtime={true}
        autoRefresh={true}
        refreshInterval={60000} // 1分钟刷新一次
      />
    </div>
  );
};

// 只读评论系统（用于展示历史讨论等）
export const ReadOnlyCommentExample: React.FC = () => {
  return (
    <div className="max-w-4xl mx-auto p-6">
      <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4 mb-6">
        <div className="flex items-center">
          <svg className="w-5 h-5 text-yellow-600 dark:text-yellow-400 mr-2" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
          </svg>
          <p className="text-yellow-800 dark:text-yellow-200 text-sm">
            这是一个历史讨论，已关闭新的评论提交。
          </p>
        </div>
      </div>

      {/* 只读模式需要通过props控制，这里仅作为示例 */}
      <CommentSystem
        postId="archived-post-1"
        initialSort={CommentSortOrder.CreatedAtAsc}
        showStats={true}
        showNotifications={false}
        maxDepth={5}
        enableRealtime={false}
        autoRefresh={false}
      />
    </div>
  );
};

// 完整功能展示页面
export const FullFeaturedExample: React.FC = () => {
  const [currentSort, setCurrentSort] = React.useState(CommentSortOrder.CreatedAtDesc);

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="max-w-6xl mx-auto p-6">
        <header className="text-center mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">
            Maple Blog 评论系统
          </h1>
          <p className="text-gray-600 dark:text-gray-400">
            现代化的实时评论系统，支持移动端优化、富文本编辑、实时通知等功能
          </p>
        </header>

        {/* 功能特性展示 */}
        <div className="grid md:grid-cols-3 gap-6 mb-8">
          <div className="bg-white dark:bg-gray-800 rounded-lg p-6 shadow-sm">
            <div className="w-12 h-12 bg-blue-100 dark:bg-blue-900/20 rounded-lg flex items-center justify-center mb-4">
              <svg className="w-6 h-6 text-blue-600 dark:text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
              </svg>
            </div>
            <h3 className="font-semibold text-gray-900 dark:text-gray-100 mb-2">实时评论</h3>
            <p className="text-gray-600 dark:text-gray-400 text-sm">
              基于SignalR的实时通信，评论、点赞、回复即时同步
            </p>
          </div>

          <div className="bg-white dark:bg-gray-800 rounded-lg p-6 shadow-sm">
            <div className="w-12 h-12 bg-green-100 dark:bg-green-900/20 rounded-lg flex items-center justify-center mb-4">
              <svg className="w-6 h-6 text-green-600 dark:text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 18h.01M8 21h8a2 2 0 002-2V5a2 2 0 00-2-2H8a2 2 0 00-2 2v14a2 2 0 002 2z" />
              </svg>
            </div>
            <h3 className="font-semibold text-gray-900 dark:text-gray-100 mb-2">移动优化</h3>
            <p className="text-gray-600 dark:text-gray-400 text-sm">
              触摸友好的界面，虚拟键盘适配，滑动手势操作
            </p>
          </div>

          <div className="bg-white dark:bg-gray-800 rounded-lg p-6 shadow-sm">
            <div className="w-12 h-12 bg-purple-100 dark:bg-purple-900/20 rounded-lg flex items-center justify-center mb-4">
              <svg className="w-6 h-6 text-purple-600 dark:text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
              </svg>
            </div>
            <h3 className="font-semibold text-gray-900 dark:text-gray-100 mb-2">富文本编辑</h3>
            <p className="text-gray-600 dark:text-gray-400 text-sm">
              Markdown支持，@提及，表情符号，图片上传
            </p>
          </div>
        </div>

        {/* 排序控制示例 */}
        <div className="bg-white dark:bg-gray-800 rounded-lg p-4 mb-6 shadow-sm">
          <div className="flex items-center justify-between">
            <h3 className="font-semibold text-gray-900 dark:text-gray-100">排序方式：</h3>
            <select
              value={currentSort}
              onChange={(e) => setCurrentSort(e.target.value as CommentSortOrder)}
              className="px-3 py-1 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
            >
              <option value={CommentSortOrder.CreatedAtDesc}>最新优先</option>
              <option value={CommentSortOrder.CreatedAtAsc}>最早优先</option>
              <option value={CommentSortOrder.LikeCountDesc}>最多点赞</option>
              <option value={CommentSortOrder.ReplyCountDesc}>最多回复</option>
              <option value={CommentSortOrder.HotScore}>热度排序</option>
            </select>
          </div>
        </div>

        {/* 主评论系统 */}
        <CommentSystem
          postId="full-featured-example"
          initialSort={currentSort}
          showStats={true}
          showNotifications={true}
          maxDepth={3}
          enableRealtime={true}
          autoRefresh={false}
          className="bg-white dark:bg-gray-800 rounded-lg shadow-sm"
        />
      </div>
    </div>
  );
};

export default FullFeaturedExample;
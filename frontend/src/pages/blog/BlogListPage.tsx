/**
 * BlogListPage - 博客文章列表页面
 * 展示所有博客文章的列表视图，支持分页、搜索和筛选
 */

import React from 'react';
import { Helmet } from '@/components/common/DocumentHead';

export const BlogListPage: React.FC = () => {
  return (
    <>
      <Helmet>
        <title>博客文章 - Maple Blog</title>
        <meta name="description" content="浏览所有博客文章，发现有趣的内容和见解。" />
      </Helmet>

      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container-responsive py-8">
          <div className="max-w-6xl mx-auto">
            {/* 页面标题 */}
            <div className="mb-8">
              <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
                博客文章
              </h1>
              <p className="text-lg text-gray-600 dark:text-gray-400">
                探索我们的精选文章和见解
              </p>
            </div>

            {/* 占位内容 */}
            <div className="text-center py-16">
              <div className="inline-flex items-center justify-center w-24 h-24 bg-blue-100 dark:bg-blue-900 rounded-full mb-6">
                <span className="text-blue-600 dark:text-blue-400 text-2xl font-bold">📝</span>
              </div>
              <h2 className="text-2xl font-semibold text-gray-900 dark:text-white mb-4">
                博客列表功能开发中
              </h2>
              <p className="text-gray-600 dark:text-gray-400 max-w-md mx-auto">
                我们正在构建一个功能丰富的博客列表页面，包括搜索、分类筛选和分页功能。
              </p>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default BlogListPage;
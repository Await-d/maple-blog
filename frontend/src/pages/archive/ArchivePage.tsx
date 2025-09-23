// @ts-nocheck
/**
 * ArchivePage - 文章归档页面
 * 按时间、分类、标签等维度展示文章归档
 */

import React from 'react';
import { Helmet } from '@/components/common/DocumentHead';
import { Archive, Calendar, Tag, Folder } from 'lucide-react';

export const ArchivePage: React.FC = () => {
  return (
    <>
      <Helmet>
        <title>文章归档 - Maple Blog</title>
        <meta name="description" content="按时间、分类和标签浏览历史文章归档。" />
      </Helmet>

      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container-responsive py-8">
          <div className="max-w-6xl mx-auto">
            {/* 页面标题 */}
            <div className="mb-8">
              <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2 flex items-center">
                <Archive className="w-8 h-8 mr-3 text-blue-600 dark:text-blue-400" />
                文章归档
              </h1>
              <p className="text-lg text-gray-600 dark:text-gray-400">
                按时间线、分类和标签浏览历史文章
              </p>
            </div>

            {/* 归档类型选择 */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-xl font-semibold text-gray-900 dark:text-white">
                    时间归档
                  </h3>
                  <Calendar className="w-8 h-8 text-blue-600 dark:text-blue-400" />
                </div>
                <p className="text-gray-600 dark:text-gray-400 text-sm mb-4">
                  按发布时间浏览文章
                </p>
                <div className="text-2xl font-bold text-blue-600 dark:text-blue-400">
                  12 个月
                </div>
              </div>

              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-xl font-semibold text-gray-900 dark:text-white">
                    分类归档
                  </h3>
                  <Folder className="w-8 h-8 text-green-600 dark:text-green-400" />
                </div>
                <p className="text-gray-600 dark:text-gray-400 text-sm mb-4">
                  按文章分类浏览
                </p>
                <div className="text-2xl font-bold text-green-600 dark:text-green-400">
                  8 个分类
                </div>
              </div>

              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-xl font-semibold text-gray-900 dark:text-white">
                    标签归档
                  </h3>
                  <Tag className="w-8 h-8 text-purple-600 dark:text-purple-400" />
                </div>
                <p className="text-gray-600 dark:text-gray-400 text-sm mb-4">
                  按标签浏览相关文章
                </p>
                <div className="text-2xl font-bold text-purple-600 dark:text-purple-400">
                  24 个标签
                </div>
              </div>
            </div>

            {/* 占位内容 */}
            <div className="text-center py-16">
              <div className="inline-flex items-center justify-center w-24 h-24 bg-purple-100 dark:bg-purple-900 rounded-full mb-6">
                <Archive className="w-12 h-12 text-purple-600 dark:text-purple-400" />
              </div>
              <h2 className="text-2xl font-semibold text-gray-900 dark:text-white mb-4">
                归档功能开发中
              </h2>
              <p className="text-gray-600 dark:text-gray-400 max-w-md mx-auto">
                我们正在构建智能的文章归档系统，支持多维度浏览和快速检索功能。
              </p>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default ArchivePage;
/**
 * BlogPostPage - 博客文章详情页面
 * 展示单篇博客文章的完整内容，包括评论和相关文章
 */

import React from 'react';
import { useParams } from 'react-router-dom';
import { Helmet } from '@/components/common/DocumentHead';

export const BlogPostPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();

  return (
    <>
      <Helmet>
        <title>文章详情 - Maple Blog</title>
        <meta name="description" content="阅读完整的博客文章内容，参与讨论和互动。" />
      </Helmet>

      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container-responsive py-8">
          <div className="max-w-4xl mx-auto">
            {/* 占位内容 */}
            <div className="text-center py-16">
              <div className="inline-flex items-center justify-center w-24 h-24 bg-green-100 dark:bg-green-900 rounded-full mb-6">
                <span className="text-green-600 dark:text-green-400 text-2xl font-bold">📄</span>
              </div>
              <h2 className="text-2xl font-semibold text-gray-900 dark:text-white mb-4">
                文章详情页面开发中
              </h2>
              <p className="text-gray-600 dark:text-gray-400 max-w-md mx-auto mb-4">
                我们正在构建一个功能丰富的文章阅读页面，包括富文本渲染、评论系统和社交分享功能。
              </p>
              {id && (
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  文章ID: {id}
                </p>
              )}
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default BlogPostPage;
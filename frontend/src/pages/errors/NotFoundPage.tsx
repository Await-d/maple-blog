// @ts-nocheck
/**
 * NotFoundPage - 404错误页面
 * 提供友好的错误提示和导航建议
 */

import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Helmet } from '@/components/common/DocumentHead';
import { Home, ArrowLeft, Search, FileText } from 'lucide-react';

export const NotFoundPage: React.FC = () => {
  const navigate = useNavigate();

  const handleGoBack = () => {
    if (window.history.length > 1) {
      navigate(-1);
    } else {
      navigate('/');
    }
  };

  return (
    <>
      <Helmet>
        <title>页面未找到 - Maple Blog</title>
        <meta name="description" content="抱歉，您访问的页面不存在。请检查链接或返回首页。" />
        <meta name="robots" content="noindex, nofollow" />
      </Helmet>

      <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-950 px-4">
        <div className="max-w-lg w-full text-center">
          {/* 404图标 */}
          <div className="mb-8">
            <div className="inline-flex items-center justify-center w-24 h-24 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full shadow-lg mb-6">
              <span className="text-white font-bold text-4xl">404</span>
            </div>
            <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-2">
              页面未找到
            </h1>
            <p className="text-lg text-gray-600 dark:text-gray-400">
              抱歉，您访问的页面不存在或已被移动
            </p>
          </div>

          {/* 错误信息 */}
          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 mb-8">
            <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
              可能的原因：
            </h2>
            <ul className="text-left space-y-2 text-gray-600 dark:text-gray-400">
              <li className="flex items-center">
                <span className="w-2 h-2 bg-blue-500 rounded-full mr-3"></span>
                URL地址输入错误
              </li>
              <li className="flex items-center">
                <span className="w-2 h-2 bg-blue-500 rounded-full mr-3"></span>
                页面已被删除或移动
              </li>
              <li className="flex items-center">
                <span className="w-2 h-2 bg-blue-500 rounded-full mr-3"></span>
                链接已过期
              </li>
              <li className="flex items-center">
                <span className="w-2 h-2 bg-blue-500 rounded-full mr-3"></span>
                您没有访问权限
              </li>
            </ul>
          </div>

          {/* 操作按钮 */}
          <div className="space-y-4">
            <div className="flex flex-col sm:flex-row gap-3 justify-center">
              <button
                onClick={handleGoBack}
                className="inline-flex items-center justify-center px-6 py-3 bg-gray-600 hover:bg-gray-700 text-white font-medium rounded-lg transition-colors"
              >
                <ArrowLeft className="w-5 h-5 mr-2" />
                返回上页
              </button>

              <Link
                to="/"
                className="inline-flex items-center justify-center px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg transition-colors"
              >
                <Home className="w-5 h-5 mr-2" />
                回到首页
              </Link>
            </div>

            <div className="flex flex-col sm:flex-row gap-3 justify-center">
              <Link
                to="/blog"
                className="inline-flex items-center justify-center px-6 py-3 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 font-medium rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
              >
                <FileText className="w-5 h-5 mr-2" />
                浏览文章
              </Link>

              <Link
                to="/search"
                className="inline-flex items-center justify-center px-6 py-3 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 font-medium rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
              >
                <Search className="w-5 h-5 mr-2" />
                搜索内容
              </Link>
            </div>
          </div>

          {/* 帮助信息 */}
          <div className="mt-8 p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
            <p className="text-sm text-blue-700 dark:text-blue-300">
              <strong>需要帮助？</strong> 如果您认为这是一个错误，请
              <Link
                to="/contact"
                className="underline hover:no-underline ml-1"
              >
                联系我们
              </Link>
            </p>
          </div>

          {/* 装饰元素 */}
          <div className="mt-8 flex justify-center space-x-2">
            <div className="w-2 h-2 bg-blue-400 rounded-full animate-bounce"></div>
            <div className="w-2 h-2 bg-purple-400 rounded-full animate-bounce delay-75"></div>
            <div className="w-2 h-2 bg-blue-400 rounded-full animate-bounce delay-150"></div>
          </div>
        </div>
      </div>
    </>
  );
};

export default NotFoundPage;
// @ts-nocheck
/**
 * PageLoadingFallback - 页面级加载后备组件
 * 在页面组件懒加载时显示的加载界面
 */

import React from 'react';
import { LoadingSpinner } from './LoadingSpinner';
import { cn } from '../../utils/cn';

interface PageLoadingFallbackProps {
  message?: string;
  className?: string;
  showLogo?: boolean;
}

export const PageLoadingFallback: React.FC<PageLoadingFallbackProps> = ({
  message = '页面加载中...',
  className,
  showLogo = true,
}) => {
  return (
    <div
      className={cn(
        'min-h-screen flex flex-col items-center justify-center',
        'bg-gray-50 dark:bg-gray-950',
        className
      )}
    >
      <div className="text-center space-y-6">
        {/* Logo区域 */}
        {showLogo && (
          <div className="mb-8">
            <div className="w-16 h-16 mx-auto bg-gradient-to-br from-blue-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
              <span className="text-white font-bold text-xl">M</span>
            </div>
            <h1 className="mt-4 text-2xl font-bold text-gray-900 dark:text-white">
              Maple Blog
            </h1>
          </div>
        )}

        {/* 加载动画 */}
        <div className="space-y-4">
          <LoadingSpinner size="lg" color="primary" />
          <p className="text-gray-600 dark:text-gray-400 font-medium">
            {message}
          </p>
        </div>

        {/* 加载进度条 */}
        <div className="w-64 mx-auto">
          <div className="h-1 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
            <div className="h-full bg-gradient-to-r from-blue-500 to-purple-600 rounded-full animate-pulse"></div>
          </div>
        </div>

        {/* 提示文本 */}
        <div className="max-w-md mx-auto text-center">
          <p className="text-sm text-gray-500 dark:text-gray-500">
            正在为您准备最佳的阅读体验...
          </p>
        </div>
      </div>

      {/* 底部装饰 */}
      <div className="absolute bottom-8 left-1/2 transform -translate-x-1/2">
        <div className="flex space-x-2">
          <div className="w-2 h-2 bg-blue-400 rounded-full animate-bounce"></div>
          <div className="w-2 h-2 bg-purple-400 rounded-full animate-bounce delay-75"></div>
          <div className="w-2 h-2 bg-blue-400 rounded-full animate-bounce delay-150"></div>
        </div>
      </div>
    </div>
  );
};

export default PageLoadingFallback;
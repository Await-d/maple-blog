/**
 * React应用程序主入口点
 * 初始化应用程序，配置提供者，并渲染根组件
 */

import React from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { HelmetProvider } from '@/components/common/DocumentHead';

import App from './App';
import './styles/globals.css';

// 导入环境配置和验证
import { validateEnvironment, isDevelopment } from './types/env';

// React Query配置
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5分钟
      gcTime: 10 * 60 * 1000, // 10分钟 (替代cacheTime)
      retry: (failureCount, error) => {
        // 不重试4xx错误
        const httpError = error as Error & { status?: number };
        if (httpError?.status && httpError.status >= 400 && httpError.status < 500) {
          return false;
        }
        // 最多重试3次
        return failureCount < 3;
      },
      refetchOnWindowFocus: false,
      refetchOnMount: true,
      refetchOnReconnect: true,
    },
    mutations: {
      retry: 1,
    },
  },
});

// 错误边界组件
class ErrorBoundary extends React.Component<
  { children: React.ReactNode },
  { hasError: boolean; error?: Error }
> {
  constructor(props: { children: React.ReactNode }) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    // TODO: Replace with proper error reporting service (e.g., Sentry)
    // For now, keeping critical error logging for debugging
    console.error('应用程序错误:', error);
    console.error('错误信息:', errorInfo);

    // 这里可以集成错误报告服务
    // 例如: Sentry.captureException(error, { extra: errorInfo });
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="error-boundary">
          <h1 className="text-2xl font-bold text-red-600 mb-4">
            应用程序遇到错误
          </h1>
          <p className="text-gray-700 mb-4">
            抱歉，应用程序遇到了意外错误。请刷新页面重试。
          </p>
          {isDevelopment() && this.state.error && (
            <details className="mt-4 p-4 bg-gray-100 rounded">
              <summary className="cursor-pointer font-semibold">
                错误详情 (开发模式)
              </summary>
              <pre className="mt-2 text-sm overflow-auto">
                {this.state.error.stack}
              </pre>
            </details>
          )}
          <button
            onClick={() => window.location.reload()}
            className="mt-4 px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors"
          >
            刷新页面
          </button>
        </div>
      );
    }

    return this.props.children;
  }
}

// 应用程序初始化
async function initializeApp() {
  try {
    // 验证环境变量
    validateEnvironment();

    // 检查浏览器兼容性
    if (!window.fetch) {
      throw new Error('此浏览器不支持现代Web标准，请更新您的浏览器');
    }

    // 初始化分析追踪（如果启用）
    if (isDevelopment()) {
      // Development mode initialized - React Query DevTools enabled
    }

    return true;
  } catch (error) {
    // TODO: Replace with proper error reporting service
    return false;
  }
}

// 渲染应用程序
async function renderApp() {
  const container = document.getElementById('root');
  if (!container) {
    throw new Error('找不到根容器元素');
  }

  const root = createRoot(container);

  // 初始化检查
  const initSuccess = await initializeApp();
  if (!initSuccess) {
    root.render(
      <div className="error-boundary">
        <h1>应用程序初始化失败</h1>
        <p>请检查控制台获取更多信息，或刷新页面重试。</p>
        <button onClick={() => window.location.reload()}>
          刷新页面
        </button>
      </div>
    );
    return;
  }

  // 渲染完整应用
  root.render(
    <React.StrictMode>
      <ErrorBoundary>
        <HelmetProvider>
          <QueryClientProvider client={queryClient}>
            <BrowserRouter>
              <App />
              {isDevelopment() && (
                <ReactQueryDevtools initialIsOpen={false} />
              )}
            </BrowserRouter>
          </QueryClientProvider>
        </HelmetProvider>
      </ErrorBoundary>
    </React.StrictMode>
  );
}

// 启动应用
renderApp().catch((error) => {
  // TODO: Replace with proper error reporting service (e.g., Sentry)
  // For now, keeping critical error logging for debugging
  console.error('应用程序渲染失败:', error);

  // 显示fallback错误UI
  const container = document.getElementById('root');
  if (container) {
    container.innerHTML = `
      <div style="text-align: center; padding: 2rem; font-family: system-ui, sans-serif;">
        <h1 style="color: #dc2626; margin-bottom: 1rem;">应用程序启动失败</h1>
        <p style="color: #6b7280; margin-bottom: 1rem;">应用程序无法正常启动，请检查控制台获取更多信息。</p>
        <button
          onclick="window.location.reload()"
          style="padding: 0.5rem 1rem; background: #3b82f6; color: white; border: none; border-radius: 0.25rem; cursor: pointer;"
        >
          刷新页面
        </button>
      </div>
    `;
  }
});

// 开发时热重载支持
if (isDevelopment() && import.meta.hot) {
  import.meta.hot.accept('./App', () => {
    // Hot reload: App component updated
  });
}
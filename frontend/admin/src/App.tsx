// @ts-nocheck
import React from 'react';
import { RouterProvider } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { ConfigProvider, App as AntdApp, theme as antdTheme } from 'antd';
import { HelmetProvider } from 'react-helmet-async';
import zhCN from 'antd/locale/zh_CN';
import dayjs from 'dayjs';
import 'dayjs/locale/zh-cn';

import router from '@/router';
import { useTheme } from '@/stores/adminStore';
import { env } from '@/utils';

import 'antd/dist/reset.css';
import './App.css';

// 配置 dayjs 中文
dayjs.locale('zh-cn');

// 创建 QueryClient
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (failureCount, error: any) => {
        // 对于认证错误不重试
        if (error?.status === 401 || error?.status === 403) {
          return false;
        }
        return failureCount < 3;
      },
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
      staleTime: 5 * 60 * 1000, // 5分钟
      gcTime: 10 * 60 * 1000, // 10分钟
      refetchOnWindowFocus: false,
      refetchOnReconnect: true,
    },
    mutations: {
      retry: false,
    },
  },
});

// 主题配置
const getThemeConfig = (theme: 'light' | 'dark') => ({
  token: {
    colorPrimary: '#1890ff',
    colorSuccess: '#52c41a',
    colorWarning: '#faad14',
    colorError: '#ff4d4f',
    colorInfo: '#1890ff',
    borderRadius: 6,
    fontSize: 14,
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, "Noto Sans", sans-serif',
  },
  algorithm: theme === 'dark' ? antdTheme.darkAlgorithm : antdTheme.defaultAlgorithm,
  components: {
    Layout: {
      headerBg: theme === 'dark' ? '#001529' : '#ffffff',
      siderBg: theme === 'dark' ? '#001529' : '#ffffff',
    },
    Menu: {
      darkItemBg: '#001529',
      darkItemSelectedBg: '#1890ff',
      darkItemHoverBg: '#002140',
    },
    Button: {
      borderRadius: 6,
    },
    Card: {
      borderRadius: 8,
    },
    Table: {
      borderRadius: 8,
    },
    Modal: {
      borderRadius: 8,
    },
    Drawer: {
      borderRadius: 8,
    },
  },
});

const AppContent: React.FC = () => {
  const theme = useTheme();

  return (
    <HelmetProvider>
      <ConfigProvider
        locale={zhCN}
        theme={getThemeConfig(theme)}
        componentSize="middle"
      >
        <AntdApp
          notification={{
            placement: 'topRight',
            duration: 4.5,
            maxCount: 5,
          }}
          message={{
            duration: 3,
            maxCount: 3,
          }}
        >
          <QueryClientProvider client={queryClient}>
            <RouterProvider router={router} />
            {env.isDev && (
              <ReactQueryDevtools
                initialIsOpen={false}
                position="bottom-right" as any
              />
            )}
          </QueryClientProvider>
        </AntdApp>
      </ConfigProvider>
    </HelmetProvider>
  );
};

const App: React.FC = () => {
  return <AppContent />;
};

export default App;
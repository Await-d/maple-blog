// @ts-nocheck
import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';

// 获取 DOM 元素
const rootElement = document.getElementById('root');

if (!rootElement) {
  throw new Error('Root element not found');
}

// 创建 React 根
const root = ReactDOM.createRoot(rootElement);

// 渲染应用
root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

// 开发环境热更新
if (import.meta.env.DEV && import.meta.hot) {
  import.meta.hot.accept();
}
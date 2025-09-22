// @ts-nocheck
import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse, AxiosError } from 'axios';
import { message } from 'antd';
import { env, storageUtils, errorUtils } from '@/utils';
import type { BaseResponse, ApiError } from '@/types';

// 创建axios实例
const createAxiosInstance = (): AxiosInstance => {
  const instance = axios.create({
    baseURL: env.apiBaseUrl + env.apiPrefix,
    timeout: 30000,
    headers: {
      'Content-Type': 'application/json',
    },
  });

  // 请求拦截器
  instance.interceptors.request.use(
    (config) => {
      // 添加认证token
      const token = storageUtils.get('access_token');
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }

      // 添加请求ID用于追踪
      config.headers['X-Request-ID'] = generateRequestId();

      // 开发环境日志
      if (env.isDev) {
        console.log('🚀 Request:', {
          method: config.method?.toUpperCase(),
          url: config.url,
          data: config.data,
          params: config.params,
        });
      }

      return config;
    },
    (error) => {
      errorUtils.log(error, 'Request Interceptor');
      return Promise.reject(error);
    }
  );

  // 响应拦截器
  instance.interceptors.response.use(
    (response: AxiosResponse<BaseResponse>) => {
      // 开发环境日志
      if (env.isDev) {
        console.log('✅ Response:', {
          status: response.status,
          url: response.config.url,
          data: response.data,
        });
      }

      const { data } = response;

      // 检查业务状态
      if (!data.success) {
        const error: ApiError = {
          message: data.message || '请求失败',
          code: data.code?.toString(),
          status: response.status,
        };
        throw error;
      }

      return response;
    },
    async (error: AxiosError<BaseResponse>) => {
      const { response, config } = error;

      // 开发环境日志
      if (env.isDev) {
        console.error('❌ Response Error:', {
          status: response?.status,
          url: config?.url,
          message: response?.data?.message || error.message,
        });
      }

      // 处理不同的HTTP状态码
      if (response) {
        switch (response.status) {
          case 401:
            await handleUnauthorized();
            break;
          case 403:
            message.error('没有权限访问该资源');
            break;
          case 404:
            message.error('请求的资源不存在');
            break;
          case 429:
            message.error('请求过于频繁，请稍后再试');
            break;
          case 500:
            message.error('服务器内部错误');
            break;
          default:
            message.error(response.data?.message || '请求失败');
        }
      } else {
        // 网络错误
        message.error('网络连接失败，请检查网络状态');
      }

      const apiError: ApiError = {
        message: response?.data?.message || error.message || '未知错误',
        code: response?.data?.code?.toString(),
        status: response?.status,
        details: response?.data,
      };

      return Promise.reject(apiError);
    }
  );

  return instance;
};

// 处理401未授权
const handleUnauthorized = async () => {
  const refreshToken = storageUtils.get('refresh_token');

  if (refreshToken) {
    try {
      // 尝试刷新token
      const response = await axios.post(`${env.apiBaseUrl}${env.apiPrefix}/auth/refresh`, {
        refreshToken,
      });

      const { token, refreshToken: newRefreshToken } = response.data.data;
      storageUtils.set('access_token', token);
      storageUtils.set('refresh_token', newRefreshToken);

      // 重新发起原请求
      window.location.reload();
    } catch {
      // 刷新失败，清除token并跳转到登录页
      clearAuthData();
      redirectToLogin();
    }
  } else {
    clearAuthData();
    redirectToLogin();
  }
};

// 清除认证数据
const clearAuthData = () => {
  storageUtils.remove('access_token');
  storageUtils.remove('refresh_token');
  storageUtils.remove('user_info');
};

// 跳转到登录页
const redirectToLogin = () => {
  const currentPath = window.location.pathname;
  if (currentPath !== '/login') {
    window.location.href = `/login?redirect=${encodeURIComponent(currentPath)}`;
  }
};

// 生成请求ID
const generateRequestId = (): string => {
  return `req_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
};

// 创建API实例
export const api = createAxiosInstance();

// API请求方法封装
export class ApiService {
  // GET请求
  static async get<T = any>(
    url: string,
    params?: Record<string, any>,
    config?: AxiosRequestConfig
  ): Promise<T> {
    const response = await api.get<BaseResponse<T>>(url, { params, ...config });
    return response.data.data;
  }

  // POST请求
  static async post<T = any>(
    url: string,
    data?: any,
    config?: AxiosRequestConfig
  ): Promise<T> {
    const response = await api.post<BaseResponse<T>>(url, data, config);
    return response.data.data;
  }

  // PUT请求
  static async put<T = any>(
    url: string,
    data?: any,
    config?: AxiosRequestConfig
  ): Promise<T> {
    const response = await api.put<BaseResponse<T>>(url, data, config);
    return response.data.data;
  }

  // PATCH请求
  static async patch<T = any>(
    url: string,
    data?: any,
    config?: AxiosRequestConfig
  ): Promise<T> {
    const response = await api.patch<BaseResponse<T>>(url, data, config);
    return response.data.data;
  }

  // DELETE请求
  static async delete<T = any>(
    url: string,
    config?: AxiosRequestConfig
  ): Promise<T> {
    const response = await api.delete<BaseResponse<T>>(url, config);
    return response.data.data;
  }

  // 文件上传
  static async upload<T = any>(
    url: string,
    file: File,
    onProgress?: (progress: number) => void,
    config?: AxiosRequestConfig
  ): Promise<T> {
    const formData = new FormData();
    formData.append('file', file);

    const response = await api.post<BaseResponse<T>>(url, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: (progressEvent) => {
        if (onProgress && progressEvent.total) {
          const progress = Math.round((progressEvent.loaded * 100) / progressEvent.total);
          onProgress(progress);
        }
      },
      ...config,
    });

    return response.data.data;
  }

  // 文件下载
  static async download(
    url: string,
    filename?: string,
    config?: AxiosRequestConfig
  ): Promise<void> {
    const response = await api.get(url, {
      responseType: 'blob',
      ...config,
    });

    const blob = new Blob([response.data]);
    const downloadUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = filename || 'download';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(downloadUrl);
  }

  // 批量请求
  static async batch<T = any>(
    requests: Array<() => Promise<any>>
  ): Promise<T[]> {
    const results = await Promise.allSettled(requests.map(request => request()));
    return results.map(result => {
      if (result.status === 'fulfilled') {
        return result.value;
      } else {
        throw result.reason;
      }
    });
  }
}

export default ApiService;
import { apiClient } from './api/client';
import type { ApiResponse } from '../types/common';

export interface NewsletterSubscribeRequest {
  email: string;
  name?: string;
  source?: string;
  tags?: string[];
  metadata?: Record<string, unknown>;
}

export interface NewsletterSubscribeResult {
  email: string;
  status: 'subscribed' | 'pending' | 'duplicate';
  subscribedAt?: string;
  message?: string;
}

export const newsletterApi = {
  async subscribe(payload: NewsletterSubscribeRequest): Promise<NewsletterSubscribeResult> {
    const response = await apiClient.post<ApiResponse<NewsletterSubscribeResult>>(
      '/api/newsletter/subscribe',
      payload
    );

    if (!response.data.success) {
      throw new Error(response.data.message || '订阅失败，请稍后重试');
    }

    return (
      response.data.data ?? {
        email: payload.email,
        status: 'pending',
        message: response.data.message,
      }
    );
  },
};

type NewsletterApi = typeof newsletterApi;
export type { NewsletterApi };

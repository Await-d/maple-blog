// @ts-nocheck
/**
 * File Upload API Service
 */

import { apiClient } from './api/client';

export interface UploadResponse {
  url: string;
  filename: string;
  size: number;
  mimeType: string;
}

export const uploadApi = {
  /**
   * Upload a single file
   */
  async uploadFile(file: File, onProgress?: (progress: number) => void): Promise<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post<UploadResponse>('/upload/file', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: (progressEvent) => {
        if (progressEvent.total && onProgress) {
          const progress = Math.round((progressEvent.loaded * 100) / progressEvent.total);
          onProgress(progress);
        }
      },
    });

    return response.data;
  },

  /**
   * Upload an image with automatic optimization
   */
  async uploadImage(file: File, options?: {
    maxWidth?: number;
    maxHeight?: number;
    quality?: number;
    onProgress?: (progress: number) => void;
  }): Promise<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    if (options?.maxWidth) formData.append('maxWidth', options.maxWidth.toString());
    if (options?.maxHeight) formData.append('maxHeight', options.maxHeight.toString());
    if (options?.quality) formData.append('quality', options.quality.toString());

    const response = await apiClient.post<UploadResponse>('/upload/image', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: (progressEvent) => {
        if (progressEvent.total && options?.onProgress) {
          const progress = Math.round((progressEvent.loaded * 100) / progressEvent.total);
          options.onProgress(progress);
        }
      },
    });

    return response.data;
  },

  /**
   * Delete an uploaded file
   */
  async deleteFile(fileUrl: string): Promise<void> {
    await apiClient.delete('/upload/file', {
      params: { url: fileUrl },
    });
  },

  /**
   * Get upload limits and allowed file types
   */
  async getUploadConfig(): Promise<{
    maxFileSize: number;
    allowedTypes: string[];
    maxImageWidth: number;
    maxImageHeight: number;
  }> {
    const response = await apiClient.get('/upload/config');
    return response.data;
  },
};

export default uploadApi;
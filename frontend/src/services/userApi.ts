// @ts-nocheck
/**
 * User API Service
 */

import { apiClient } from './api/client';

export interface User {
  id: string;
  username: string;
  email: string;
  avatar?: string;
  bio?: string;
  displayName: string;
  isVip: boolean;
  role: 'User' | 'Author' | 'Admin';
  createdAt: string;
  updatedAt: string;
}

export interface UserProfile extends User {
  firstName?: string;
  lastName?: string;
  phone?: string;
  website?: string;
  location?: string;
  socialLinks?: {
    twitter?: string;
    facebook?: string;
    linkedin?: string;
    github?: string;
  };
  preferences?: {
    emailNotifications: boolean;
    pushNotifications: boolean;
    theme: 'light' | 'dark' | 'auto';
    language: string;
  };
}

export interface UpdateProfileRequest {
  firstName?: string;
  lastName?: string;
  bio?: string;
  avatar?: string;
  website?: string;
  location?: string;
  socialLinks?: {
    twitter?: string;
    facebook?: string;
    linkedin?: string;
    github?: string;
  };
}

export const userApi = {
  /**
   * Get current user profile
   */
  async getCurrentUser(): Promise<UserProfile> {
    const response = await apiClient.get<UserProfile>('/users/me');
    return response.data;
  },

  /**
   * Get user by ID
   */
  async getUserById(userId: string): Promise<User> {
    const response = await apiClient.get<User>(`/users/${userId}`);
    return response.data;
  },

  /**
   * Update current user profile
   */
  async updateProfile(data: UpdateProfileRequest): Promise<UserProfile> {
    const response = await apiClient.put<UserProfile>('/users/me', data);
    return response.data;
  },

  /**
   * Update user avatar
   */
  async updateAvatar(file: File): Promise<{ avatarUrl: string }> {
    const formData = new FormData();
    formData.append('avatar', file);

    const response = await apiClient.post<{ avatarUrl: string }>('/users/me/avatar', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  /**
   * Change password
   */
  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await apiClient.post('/users/me/password', {
      currentPassword,
      newPassword,
    });
  },

  /**
   * Update user preferences
   */
  async updatePreferences(preferences: UserProfile['preferences']): Promise<void> {
    await apiClient.put('/users/me/preferences', preferences);
  },

  /**
   * Search users
   */
  async searchUsers(query: string, limit = 10): Promise<User[]> {
    const response = await apiClient.get<User[]>('/users/search', {
      params: { q: query, limit },
    });
    return response.data;
  },

  /**
   * Get user suggestions for mentions
   */
  async getMentionSuggestions(query: string): Promise<Array<{ id: string; username: string; avatar?: string }>> {
    const response = await apiClient.get<Array<{ id: string; username: string; avatar?: string }>>('/users/mentions', {
      params: { q: query },
    });
    return response.data;
  },

  /**
   * Delete user account
   */
  async deleteAccount(password: string): Promise<void> {
    await apiClient.delete('/users/me', {
      data: { password },
    });
  },
};

export default userApi;
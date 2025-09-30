import { apiClient } from './api/client';
import type { UserProfile } from '../types/auth';

export type UpdateProfileRequest = Partial<Pick<
  UserProfile,
  'displayName' | 'bio' | 'location' | 'website' | 'birthday' | 'timezone'
>> & {
  socialLinks?: UserProfile['socialLinks'];
  avatar?: string;
};

export type UpdatePreferencesRequest = Partial<UserProfile['preferences']>;

export interface UserSearchResult {
  id: string;
  username: string;
  displayName: string;
  avatar?: string;
  role: 'User' | 'Author' | 'Admin';
  isVip?: boolean;
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
  async getUserById(userId: string): Promise<UserProfile> {
    const response = await apiClient.get<UserProfile>(`/users/${userId}`);
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
  async searchUsers(query: string, limit = 10): Promise<UserSearchResult[]> {
    const response = await apiClient.get<UserSearchResult[]>('/users/search', {
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

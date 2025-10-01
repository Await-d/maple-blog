import { ApiService } from './api';
import type {
  PaginatedResponse,
  User,
  UserListQuery,
  CreateUserInput,
  UpdateUserInput,
  UserActivityLog,
  UserSession,
  UserStatus,
} from '@/types';

class UserService {
  private baseUrl = '/users';

  async getUsers(params: UserListQuery = {}): Promise<PaginatedResponse<User>> {
    const query: Record<string, unknown> = {
      page: params.page,
      pageSize: params.pageSize,
      sortBy: params.sortBy,
      sortOrder: params.sortOrder,
      search: params.search,
    };

    if (params.status) {
      query.status = params.status;
    }

    if (params.roleId) {
      query.roleId = params.roleId;
    }

    if (params.startDate) {
      query.startDate = params.startDate;
    }

    if (params.endDate) {
      query.endDate = params.endDate;
    }

    // 清理空值
    Object.keys(query).forEach((key) => {
      if (query[key] === undefined || query[key] === '') {
        delete query[key];
      }
    });

    return ApiService.get<PaginatedResponse<User>>(this.baseUrl, query);
  }

  async getUserById(userId: string): Promise<User> {
    return ApiService.get<User>(`${this.baseUrl}/${userId}`);
  }

  async createUser(payload: CreateUserInput): Promise<User> {
    return ApiService.post<User>(this.baseUrl, payload);
  }

  async updateUser(userId: string, payload: UpdateUserInput): Promise<User> {
    return ApiService.put<User>(`${this.baseUrl}/${userId}`, payload);
  }

  async deleteUser(userId: string): Promise<void> {
    await ApiService.delete(`${this.baseUrl}/${userId}`);
  }

  async deleteUsers(userIds: string[]): Promise<void> {
    await ApiService.post(`${this.baseUrl}/bulk-delete`, { userIds });
  }

  async updateUserStatus(userId: string, status: UserStatus): Promise<User> {
    return ApiService.patch<User>(`${this.baseUrl}/${userId}/status`, { status });
  }

  async assignRoles(userId: string, roleIds: string[]): Promise<User> {
    return ApiService.post<User>(`${this.baseUrl}/${userId}/roles`, { roleIds });
  }

  async resetPassword(userId: string, password: string): Promise<void> {
    await ApiService.post(`${this.baseUrl}/${userId}/reset-password`, { password });
  }

  async getActivities(userId: string): Promise<UserActivityLog[]> {
    return ApiService.get<UserActivityLog[]>(`${this.baseUrl}/${userId}/activities`);
  }

  async getSessions(userId: string): Promise<UserSession[]> {
    return ApiService.get<UserSession[]>(`${this.baseUrl}/${userId}/sessions`);
  }
}

export const userService = new UserService();
export default userService;

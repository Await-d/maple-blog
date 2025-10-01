import { ApiService } from './api';
import type {
  Role,
  Permission,
  CreateRoleInput,
  UpdateRoleInput,
  QueryParams,
  PaginatedResponse,
} from '@/types';

class RoleService {
  private baseUrl = '/roles';

  async getRoles(params?: QueryParams): Promise<Role[]> {
    return ApiService.get<Role[]>(this.baseUrl, params);
  }

  async getRolesWithPagination(params?: QueryParams): Promise<PaginatedResponse<Role>> {
    return ApiService.get<PaginatedResponse<Role>>(`${this.baseUrl}/list`, params);
  }

  async getRoleById(roleId: string): Promise<Role> {
    return ApiService.get<Role>(`${this.baseUrl}/${roleId}`);
  }

  async createRole(payload: CreateRoleInput): Promise<Role> {
    return ApiService.post<Role>(this.baseUrl, payload);
  }

  async updateRole(roleId: string, payload: UpdateRoleInput): Promise<Role> {
    return ApiService.put<Role>(`${this.baseUrl}/${roleId}`, payload);
  }

  async deleteRole(roleId: string): Promise<void> {
    await ApiService.delete(`${this.baseUrl}/${roleId}`);
  }

  async assignPermissions(roleId: string, permissionIds: string[]): Promise<Role> {
    return ApiService.post<Role>(`${this.baseUrl}/${roleId}/permissions`, { permissionIds });
  }

  async getPermissions(): Promise<Permission[]> {
    return ApiService.get<Permission[]>(`${this.baseUrl}/permissions`);
  }
}

export const roleService = new RoleService();
export default roleService;

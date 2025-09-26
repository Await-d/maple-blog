import { create } from 'zustand';
import { devtools, persist } from 'zustand/middleware';
import { immer } from 'zustand/middleware/immer';
import type {
  Role,
  Permission,
  QueryParams
} from '@/types';

export interface PermissionState {
  // Permissions data
  permissions: Permission[];
  permissionCategories: string[];
  selectedPermissions: string[];

  // Roles data
  roles: Role[];
  selectedRoles: string[];
  currentRole: Role | null;

  // Role-Permission matrix for visualization
  rolePermissionMatrix: Record<string, Record<string, boolean>>;

  // UI state
  loading: boolean;
  error: string | null;

  // Permissions form
  permissionFormVisible: boolean;
  permissionFormMode: 'create' | 'edit' | 'view';
  permissionFormData: Partial<Permission> | null;

  // Role form
  roleFormVisible: boolean;
  roleFormMode: 'create' | 'edit' | 'view';
  roleFormData: Partial<Role> | null;

  // Role permission assignment
  rolePermissionVisible: boolean;
  rolePermissionData: {
    roleId: string;
    permissionIds: string[];
  } | null;

  // User role assignment
  userRoleVisible: boolean;
  userRoleData: {
    userId: string;
    roleIds: string[];
  } | null;

  // Bulk operations
  bulkPermissionUpdate: {
    roleIds: string[];
    permissionIds: string[];
    action: 'grant' | 'revoke';
  } | null;

  // Query and filtering
  permissionQuery: QueryParams & {
    category?: string;
    isActive?: boolean;
  };

  roleQuery: QueryParams & {
    level?: number;
    isBuiltIn?: boolean;
  };

  // Actions
  setPermissions: (permissions: Permission[]) => void;
  addPermission: (permission: Permission) => void;
  updatePermission: (id: string, permission: Partial<Permission>) => void;
  removePermission: (id: string) => void;

  setRoles: (roles: Role[]) => void;
  addRole: (role: Role) => void;
  updateRole: (id: string, role: Partial<Role>) => void;
  removeRole: (id: string) => void;

  setCurrentRole: (role: Role | null) => void;

  // Selection management
  setSelectedPermissions: (permissionIds: string[]) => void;
  togglePermissionSelection: (permissionId: string) => void;
  setSelectedRoles: (roleIds: string[]) => void;
  toggleRoleSelection: (roleId: string) => void;
  clearSelections: () => void;

  // Matrix management
  updateRolePermissionMatrix: () => void;
  setRolePermission: (roleId: string, permissionId: string, granted: boolean) => void;

  // Form management
  openPermissionForm: (mode: 'create' | 'edit' | 'view', permission?: Permission) => void;
  closePermissionForm: () => void;
  setPermissionFormData: (data: Partial<Permission>) => void;

  openRoleForm: (mode: 'create' | 'edit' | 'view', role?: Role) => void;
  closeRoleForm: () => void;
  setRoleFormData: (data: Partial<Role>) => void;

  // Assignment management
  openRolePermissionAssignment: (roleId: string, permissionIds: string[]) => void;
  closeRolePermissionAssignment: () => void;
  updateRolePermissions: (roleId: string, permissionIds: string[]) => void;

  openUserRoleAssignment: (userId: string, roleIds: string[]) => void;
  closeUserRoleAssignment: () => void;
  updateUserRoles: (userId: string, roleIds: string[]) => void;

  // Bulk operations
  startBulkPermissionUpdate: (roleIds: string[], permissionIds: string[], action: 'grant' | 'revoke') => void;
  completeBulkPermissionUpdate: () => void;

  // Query management
  setPermissionQuery: (query: Partial<PermissionState['permissionQuery']>) => void;
  setRoleQuery: (query: Partial<PermissionState['roleQuery']>) => void;

  // Loading and error states
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;

  // Computed getters
  getPermissionsByCategory: (category: string) => Permission[];
  getRolesByLevel: (level: number) => Role[];
  getUserPermissions: (userId: string) => Permission[];
  getRolePermissions: (roleId: string) => Permission[];
  getPermissionCategories: () => string[];

  // Permission checks
  hasRolePermission: (roleId: string, permissionCode: string) => boolean;
  hasUserPermission: (userId: string, permissionCode: string) => boolean;
  canAssignRole: (userId: string, roleId: string) => boolean;

  reset: () => void;
}

const initialState = {
  permissions: [],
  permissionCategories: [],
  selectedPermissions: [],

  roles: [],
  selectedRoles: [],
  currentRole: null,

  rolePermissionMatrix: {},

  loading: false,
  error: null,

  permissionFormVisible: false,
  permissionFormMode: 'create' as const,
  permissionFormData: null,

  roleFormVisible: false,
  roleFormMode: 'create' as const,
  roleFormData: null,

  rolePermissionVisible: false,
  rolePermissionData: null,

  userRoleVisible: false,
  userRoleData: null,

  bulkPermissionUpdate: null,

  permissionQuery: {
    page: 1,
    pageSize: 50,
    sortBy: 'category',
    sortOrder: 'asc' as const,
    search: '',
  },

  roleQuery: {
    page: 1,
    pageSize: 50,
    sortBy: 'level',
    sortOrder: 'asc' as const,
    search: '',
  },
};

export const usePermissionStore = create<PermissionState>()(
  devtools(
    persist(
      immer((set, get) => ({
        ...initialState,

        setPermissions: (permissions) => {
          set((state) => {
            state.permissions = permissions;
            state.permissionCategories = [...new Set(permissions.map(p => p.category))];
            state.error = null;
          });
          get().updateRolePermissionMatrix();
        },

        addPermission: (permission) => {
          set((state) => {
            state.permissions.push(permission);
            if (!state.permissionCategories.includes(permission.category)) {
              state.permissionCategories.push(permission.category);
            }
          });
          get().updateRolePermissionMatrix();
        },

        updatePermission: (id, permissionData) => {
          set((state) => {
            const index = state.permissions.findIndex(p => p.id === id);
            if (index !== -1) {
              state.permissions[index] = { ...state.permissions[index], ...permissionData };

              // Update categories if needed
              const newCategory = permissionData.category;
              if (newCategory && !state.permissionCategories.includes(newCategory)) {
                state.permissionCategories.push(newCategory);
              }
            }
          });
          get().updateRolePermissionMatrix();
        },

        removePermission: (id) => {
          set((state) => {
            state.permissions = state.permissions.filter(p => p.id !== id);
            state.selectedPermissions = state.selectedPermissions.filter(pid => pid !== id);

            // Update categories
            state.permissionCategories = [...new Set(state.permissions.map(p => p.category))];
          });
          get().updateRolePermissionMatrix();
        },

        setRoles: (roles) => {
          set((state) => {
            state.roles = roles;
            state.error = null;
          });
          get().updateRolePermissionMatrix();
        },

        addRole: (role) => {
          set((state) => {
            state.roles.push(role);
          });
          get().updateRolePermissionMatrix();
        },

        updateRole: (id, roleData) => {
          set((state) => {
            const index = state.roles.findIndex(r => r.id === id);
            if (index !== -1) {
              state.roles[index] = { ...state.roles[index], ...roleData };
            }

            if (state.currentRole?.id === id) {
              state.currentRole = { ...state.currentRole, ...roleData };
            }
          });
          get().updateRolePermissionMatrix();
        },

        removeRole: (id) => {
          set((state) => {
            state.roles = state.roles.filter(r => r.id !== id);
            state.selectedRoles = state.selectedRoles.filter(rid => rid !== id);

            if (state.currentRole?.id === id) {
              state.currentRole = null;
            }
          });
          get().updateRolePermissionMatrix();
        },

        setCurrentRole: (role) => {
          set((state) => {
            state.currentRole = role;
          });
        },

        setSelectedPermissions: (permissionIds) => {
          set((state) => {
            state.selectedPermissions = permissionIds;
          });
        },

        togglePermissionSelection: (permissionId) => {
          set((state) => {
            const index = state.selectedPermissions.indexOf(permissionId);
            if (index > -1) {
              state.selectedPermissions.splice(index, 1);
            } else {
              state.selectedPermissions.push(permissionId);
            }
          });
        },

        setSelectedRoles: (roleIds) => {
          set((state) => {
            state.selectedRoles = roleIds;
          });
        },

        toggleRoleSelection: (roleId) => {
          set((state) => {
            const index = state.selectedRoles.indexOf(roleId);
            if (index > -1) {
              state.selectedRoles.splice(index, 1);
            } else {
              state.selectedRoles.push(roleId);
            }
          });
        },

        clearSelections: () => {
          set((state) => {
            state.selectedPermissions = [];
            state.selectedRoles = [];
          });
        },

        updateRolePermissionMatrix: () => {
          set((state) => {
            const matrix: Record<string, Record<string, boolean>> = {};

            state.roles.forEach(role => {
              matrix[role.id] = {};
              state.permissions.forEach(permission => {
                matrix[role.id][permission.id] = role.permissions.some(p => p.id === permission.id);
              });
            });

            state.rolePermissionMatrix = matrix;
          });
        },

        setRolePermission: (roleId, permissionId, granted) => {
          set((state) => {
            if (!state.rolePermissionMatrix[roleId]) {
              state.rolePermissionMatrix[roleId] = {};
            }
            state.rolePermissionMatrix[roleId][permissionId] = granted;
          });
        },

        openPermissionForm: (mode, permission) => {
          set((state) => {
            state.permissionFormVisible = true;
            state.permissionFormMode = mode;
            state.permissionFormData = permission || null;
          });
        },

        closePermissionForm: () => {
          set((state) => {
            state.permissionFormVisible = false;
            state.permissionFormData = null;
          });
        },

        setPermissionFormData: (data) => {
          set((state) => {
            state.permissionFormData = { ...state.permissionFormData, ...data };
          });
        },

        openRoleForm: (mode, role) => {
          set((state) => {
            state.roleFormVisible = true;
            state.roleFormMode = mode;
            state.roleFormData = role || null;
          });
        },

        closeRoleForm: () => {
          set((state) => {
            state.roleFormVisible = false;
            state.roleFormData = null;
          });
        },

        setRoleFormData: (data) => {
          set((state) => {
            state.roleFormData = { ...state.roleFormData, ...data };
          });
        },

        openRolePermissionAssignment: (roleId, permissionIds) => {
          set((state) => {
            state.rolePermissionVisible = true;
            state.rolePermissionData = { roleId, permissionIds };
          });
        },

        closeRolePermissionAssignment: () => {
          set((state) => {
            state.rolePermissionVisible = false;
            state.rolePermissionData = null;
          });
        },

        updateRolePermissions: (roleId, permissionIds) => {
          set((state) => {
            const roleIndex = state.roles.findIndex(r => r.id === roleId);
            if (roleIndex !== -1) {
              const permissions = state.permissions.filter(p => permissionIds.includes(p.id));
              state.roles[roleIndex].permissions = permissions;
            }
          });
          get().updateRolePermissionMatrix();
        },

        openUserRoleAssignment: (userId, roleIds) => {
          set((state) => {
            state.userRoleVisible = true;
            state.userRoleData = { userId, roleIds };
          });
        },

        closeUserRoleAssignment: () => {
          set((state) => {
            state.userRoleVisible = false;
            state.userRoleData = null;
          });
        },

        updateUserRoles: (userId, roleIds) => {
          // This would typically trigger an API call to update user roles
          console.log(`Updating user ${userId} with roles:`, roleIds);
        },

        startBulkPermissionUpdate: (roleIds, permissionIds, action) => {
          set((state) => {
            state.bulkPermissionUpdate = { roleIds, permissionIds, action };
          });
        },

        completeBulkPermissionUpdate: () => {
          set((state) => {
            state.bulkPermissionUpdate = null;
          });
        },

        setPermissionQuery: (query) => {
          set((state) => {
            state.permissionQuery = { ...state.permissionQuery, ...query };
          });
        },

        setRoleQuery: (query) => {
          set((state) => {
            state.roleQuery = { ...state.roleQuery, ...query };
          });
        },

        setLoading: (loading) => {
          set((state) => {
            state.loading = loading;
          });
        },

        setError: (error) => {
          set((state) => {
            state.error = error;
          });
        },

        getPermissionsByCategory: (category) => {
          const state = get();
          return state.permissions.filter(p => p.category === category);
        },

        getRolesByLevel: (level) => {
          const state = get();
          return state.roles.filter(r => r.level === level);
        },

        getUserPermissions: (_userId) => {
          // This would typically come from user data or API
          // For now, return empty array
          return [];
        },

        getRolePermissions: (roleId) => {
          const state = get();
          const role = state.roles.find(r => r.id === roleId);
          return role?.permissions || [];
        },

        getPermissionCategories: () => {
          const state = get();
          return state.permissionCategories;
        },

        hasRolePermission: (roleId, permissionCode) => {
          const state = get();
          const role = state.roles.find(r => r.id === roleId);
          return role?.permissions.some(p => p.code === permissionCode) || false;
        },

        hasUserPermission: (_userId, _permissionCode) => {
          // This would typically check user's roles and their permissions
          // For now, return false
          return false;
        },

        canAssignRole: (_userId, _roleId) => {
          // This would implement business logic for role assignment
          // For now, return true
          return true;
        },

        reset: () => {
          set(() => initialState);
        },
      })),
      {
        name: 'permission-store',
        partialize: (state) => ({
          permissionQuery: state.permissionQuery,
          roleQuery: state.roleQuery,
        }),
      }
    ),
    {
      name: 'permission-store',
      enabled: process.env.NODE_ENV === 'development',
    }
  )
);

// Selectors
export const usePermissions = () => usePermissionStore((state) => state.permissions);
export const useRoles = () => usePermissionStore((state) => state.roles);
export const useCurrentRole = () => usePermissionStore((state) => state.currentRole);
export const useSelectedPermissions = () => usePermissionStore((state) => state.selectedPermissions);
export const useSelectedRoles = () => usePermissionStore((state) => state.selectedRoles);
export const useRolePermissionMatrix = () => usePermissionStore((state) => state.rolePermissionMatrix);
export const usePermissionCategories = () => usePermissionStore((state) => state.permissionCategories);
export const usePermissionLoading = () => usePermissionStore((state) => state.loading);
export const usePermissionError = () => usePermissionStore((state) => state.error);

export const usePermissionForm = () => usePermissionStore((state) => ({
  visible: state.permissionFormVisible,
  mode: state.permissionFormMode,
  data: state.permissionFormData,
}));

export const useRoleForm = () => usePermissionStore((state) => ({
  visible: state.roleFormVisible,
  mode: state.roleFormMode,
  data: state.roleFormData,
}));

export const useRolePermissionAssignment = () => usePermissionStore((state) => ({
  visible: state.rolePermissionVisible,
  data: state.rolePermissionData,
}));

export const useUserRoleAssignment = () => usePermissionStore((state) => ({
  visible: state.userRoleVisible,
  data: state.userRoleData,
}));

// Computed selectors
export const usePermissionStats = () => {
  return usePermissionStore((state) => {
    const totalPermissions = state.permissions.length;
    const activePermissions = state.permissions.filter(p => p.isActive).length;
    const categories = state.permissionCategories.length;

    return {
      totalPermissions,
      activePermissions,
      inactivePermissions: totalPermissions - activePermissions,
      categories,
    };
  });
};

export const useRoleStats = () => {
  return usePermissionStore((state) => {
    const totalRoles = state.roles.length;
    const builtInRoles = state.roles.filter(r => r.isBuiltIn).length;
    const customRoles = totalRoles - builtInRoles;

    return {
      totalRoles,
      builtInRoles,
      customRoles,
    };
  });
};
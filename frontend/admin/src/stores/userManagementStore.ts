import { create } from 'zustand';
import { devtools, persist } from 'zustand/middleware';
import { immer } from 'zustand/middleware/immer';
import type {
  User,
  QueryParams,
  PaginatedResponse
} from '@/types';
import { UserStatus } from '@/types';

export interface UserManagementState {
  // Users data
  users: User[];
  selectedUsers: string[];
  currentUser: User | null;

  // Pagination and filtering
  pagination: {
    current: number;
    pageSize: number;
    total: number;
    totalPages: number;
  };

  // Query state
  query: QueryParams & {
    status?: UserStatus;
    roleId?: string;
    dateRange?: [string, string];
  };

  // UI state
  loading: boolean;
  actionLoading: Record<string, boolean>; // Track loading state for specific actions
  error: string | null;

  // Bulk operations
  bulkOperationProgress: {
    total: number;
    completed: number;
    failed: number;
    isRunning: boolean;
  };

  // User form state
  userFormVisible: boolean;
  userFormMode: 'create' | 'edit' | 'view';
  userFormData: Partial<User> | null;

  // Role assignment
  roleAssignmentVisible: boolean;
  roleAssignmentUser: User | null;

  // User details
  userDetailsVisible: boolean;
  selectedUserDetails: User | null;

  // Actions
  setUsers: (response: PaginatedResponse<User>) => void;
  addUser: (user: User) => void;
  updateUser: (id: string, user: Partial<User>) => void;
  removeUser: (id: string) => void;
  removeUsers: (ids: string[]) => void;

  setSelectedUsers: (userIds: string[]) => void;
  toggleUserSelection: (userId: string) => void;
  selectAllUsers: () => void;
  clearSelection: () => void;

  setCurrentUser: (user: User | null) => void;

  // Pagination
  setPagination: (pagination: Partial<UserManagementState['pagination']>) => void;
  changePage: (page: number, pageSize?: number) => void;

  // Query
  setQuery: (query: Partial<UserManagementState['query']>) => void;
  updateFilter: (key: string, value: unknown) => void;
  clearFilters: () => void;

  // Loading states
  setLoading: (loading: boolean) => void;
  setActionLoading: (action: string, loading: boolean) => void;
  setError: (error: string | null) => void;

  // Bulk operations
  startBulkOperation: (total: number) => void;
  updateBulkProgress: (completed: number, failed: number) => void;
  completeBulkOperation: () => void;

  // User form
  openUserForm: (mode: 'create' | 'edit' | 'view', user?: User) => void;
  closeUserForm: () => void;
  setUserFormData: (data: Partial<User>) => void;

  // Role assignment
  openRoleAssignment: (user: User) => void;
  closeRoleAssignment: () => void;

  // User details
  openUserDetails: (user: User) => void;
  closeUserDetails: () => void;

  // Computed actions
  getFilteredUsers: () => User[];
  getUserById: (id: string) => User | undefined;
  getUsersByRole: (roleId: string) => User[];
  getUsersByStatus: (status: UserStatus) => User[];

  reset: () => void;
}

const initialState = {
  users: [],
  selectedUsers: [],
  currentUser: null,

  pagination: {
    current: 1,
    pageSize: 20,
    total: 0,
    totalPages: 0,
  },

  query: {
    page: 1,
    pageSize: 20,
    sortBy: 'createdAt',
    sortOrder: 'desc' as const,
    search: '',
  },

  loading: false,
  actionLoading: {},
  error: null,

  bulkOperationProgress: {
    total: 0,
    completed: 0,
    failed: 0,
    isRunning: false,
  },

  userFormVisible: false,
  userFormMode: 'create' as const,
  userFormData: null,

  roleAssignmentVisible: false,
  roleAssignmentUser: null,

  userDetailsVisible: false,
  selectedUserDetails: null,
};

export const useUserManagementStore = create<UserManagementState>()(
  devtools(
    persist(
      immer((set, get) => ({
        ...initialState,

        setUsers: (response) => {
          set((state) => {
            state.users = response.data;
            state.pagination = response.pagination;
            state.error = null;
          });
        },

        addUser: (user) => {
          set((state) => {
            state.users.unshift(user);
            state.pagination.total += 1;
          });
        },

        updateUser: (id, userData) => {
          set((state) => {
            const index = state.users.findIndex(u => u.id === id);
            if (index !== -1) {
              state.users[index] = { ...state.users[index], ...userData };
            }

            // Update current user if it's being edited
            if (state.currentUser?.id === id) {
              state.currentUser = { ...state.currentUser, ...userData };
            }
          });
        },

        removeUser: (id) => {
          set((state) => {
            state.users = state.users.filter(u => u.id !== id);
            state.selectedUsers = state.selectedUsers.filter(uid => uid !== id);
            state.pagination.total -= 1;

            if (state.currentUser?.id === id) {
              state.currentUser = null;
            }
          });
        },

        removeUsers: (ids) => {
          set((state) => {
            state.users = state.users.filter(u => !ids.includes(u.id));
            state.selectedUsers = state.selectedUsers.filter(uid => !ids.includes(uid));
            state.pagination.total -= ids.length;
          });
        },

        setSelectedUsers: (userIds) => {
          set((state) => {
            state.selectedUsers = userIds;
          });
        },

        toggleUserSelection: (userId) => {
          set((state) => {
            const index = state.selectedUsers.indexOf(userId);
            if (index > -1) {
              state.selectedUsers.splice(index, 1);
            } else {
              state.selectedUsers.push(userId);
            }
          });
        },

        selectAllUsers: () => {
          set((state) => {
            state.selectedUsers = state.users.map(u => u.id);
          });
        },

        clearSelection: () => {
          set((state) => {
            state.selectedUsers = [];
          });
        },

        setCurrentUser: (user) => {
          set((state) => {
            state.currentUser = user;
          });
        },

        setPagination: (pagination) => {
          set((state) => {
            state.pagination = { ...state.pagination, ...pagination };
          });
        },

        changePage: (page, pageSize) => {
          set((state) => {
            state.pagination.current = page;
            if (pageSize) {
              state.pagination.pageSize = pageSize;
            }
            state.query.page = page;
            if (pageSize) {
              state.query.pageSize = pageSize;
            }
          });
        },

        setQuery: (query) => {
          set((state) => {
            state.query = { ...state.query, ...query };
          });
        },

        updateFilter: (key, value) => {
          set((state) => {
            (state.query as Record<string, unknown>)[key] = value;
            // Reset to first page when filtering
            state.query.page = 1;
            state.pagination.current = 1;
          });
        },

        clearFilters: () => {
          set((state) => {
            state.query = {
              page: 1,
              pageSize: state.query.pageSize || 10,
              sortBy: 'createdAt',
              sortOrder: 'desc',
              search: '',
            };
            state.pagination.current = 1;
          });
        },

        setLoading: (loading) => {
          set((state) => {
            state.loading = loading;
          });
        },

        setActionLoading: (action, loading) => {
          set((state) => {
            if (loading) {
              state.actionLoading[action] = true;
            } else {
              delete state.actionLoading[action];
            }
          });
        },

        setError: (error) => {
          set((state) => {
            state.error = error;
          });
        },

        startBulkOperation: (total) => {
          set((state) => {
            state.bulkOperationProgress = {
              total,
              completed: 0,
              failed: 0,
              isRunning: true,
            };
          });
        },

        updateBulkProgress: (completed, failed) => {
          set((state) => {
            state.bulkOperationProgress.completed = completed;
            state.bulkOperationProgress.failed = failed;
          });
        },

        completeBulkOperation: () => {
          set((state) => {
            state.bulkOperationProgress.isRunning = false;
          });
        },

        openUserForm: (mode, user) => {
          set((state) => {
            state.userFormVisible = true;
            state.userFormMode = mode;
            state.userFormData = user || null;
          });
        },

        closeUserForm: () => {
          set((state) => {
            state.userFormVisible = false;
            state.userFormData = null;
          });
        },

        setUserFormData: (data) => {
          set((state) => {
            state.userFormData = { ...state.userFormData, ...data };
          });
        },

        openRoleAssignment: (user) => {
          set((state) => {
            state.roleAssignmentVisible = true;
            state.roleAssignmentUser = user;
          });
        },

        closeRoleAssignment: () => {
          set((state) => {
            state.roleAssignmentVisible = false;
            state.roleAssignmentUser = null;
          });
        },

        openUserDetails: (user) => {
          set((state) => {
            state.userDetailsVisible = true;
            state.selectedUserDetails = user;
          });
        },

        closeUserDetails: () => {
          set((state) => {
            state.userDetailsVisible = false;
            state.selectedUserDetails = null;
          });
        },

        getFilteredUsers: () => {
          const state = get();
          let filtered = [...state.users];

          // Apply filters
          if (state.query.search) {
            const searchLower = state.query.search.toLowerCase();
            filtered = filtered.filter(user =>
              user.username.toLowerCase().includes(searchLower) ||
              user.email.toLowerCase().includes(searchLower) ||
              user.displayName?.toLowerCase().includes(searchLower)
            );
          }

          if (state.query.status) {
            filtered = filtered.filter(user => user.status === state.query.status);
          }

          if (state.query.roleId) {
            filtered = filtered.filter(user =>
              user.roles.some(role => role.id === state.query.roleId)
            );
          }

          return filtered;
        },

        getUserById: (id) => {
          const state = get();
          return state.users.find(user => user.id === id);
        },

        getUsersByRole: (roleId) => {
          const state = get();
          return state.users.filter(user =>
            user.roles.some(role => role.id === roleId)
          );
        },

        getUsersByStatus: (status) => {
          const state = get();
          return state.users.filter(user => user.status === status);
        },

        reset: () => {
          set(() => initialState);
        },
      })),
      {
        name: 'user-management-store',
        partialize: (state) => ({
          pagination: state.pagination,
          query: state.query,
        }),
      }
    ),
    {
      name: 'user-management-store',
      enabled: process.env.NODE_ENV === 'development',
    }
  )
);

// Selectors
export const useUsers = () => useUserManagementStore((state) => state.users);
export const useSelectedUsers = () => useUserManagementStore((state) => state.selectedUsers);
export const useCurrentUser = () => useUserManagementStore((state) => state.currentUser);
export const usePagination = () => useUserManagementStore((state) => state.pagination);
export const useQuery = () => useUserManagementStore((state) => state.query);
export const useUserManagementLoading = () => useUserManagementStore((state) => state.loading);
export const useUserManagementError = () => useUserManagementStore((state) => state.error);
export const useBulkOperationProgress = () => useUserManagementStore((state) => state.bulkOperationProgress);
export const useUserForm = () => useUserManagementStore((state) => ({
  visible: state.userFormVisible,
  mode: state.userFormMode,
  data: state.userFormData,
}));
export const useRoleAssignment = () => useUserManagementStore((state) => ({
  visible: state.roleAssignmentVisible,
  user: state.roleAssignmentUser,
}));
export const useUserDetails = () => useUserManagementStore((state) => ({
  visible: state.userDetailsVisible,
  user: state.selectedUserDetails,
}));

// Computed selectors
export const useSelectedUsersCount = () => {
  return useUserManagementStore((state) => state.selectedUsers.length);
};

export const useHasSelectedUsers = () => {
  return useUserManagementStore((state) => state.selectedUsers.length > 0);
};

export const useUserStats = () => {
  return useUserManagementStore((state) => {
    const users = state.users;
    const total = users.length;
    const active = users.filter(u => u.status === UserStatus.Active).length;
    const inactive = users.filter(u => u.status === UserStatus.Inactive).length;
    const banned = users.filter(u => u.status === UserStatus.Banned).length;
    const pending = users.filter(u => u.status === UserStatus.Pending).length;

    return {
      total,
      active,
      inactive,
      banned,
      pending,
      activePercentage: total ? Math.round((active / total) * 100) : 0,
    };
  });
};
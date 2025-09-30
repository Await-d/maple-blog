/**
 * PermissionManagement - Complete role and permission management system
 * Provides enterprise-grade permission matrix, role management, and user assignment functionality
 */

import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Shield,
  Plus,
  Edit2,
  Trash2,
  Save,
  X,
  Search,
  Download,
  AlertTriangle,
  Check,
} from 'lucide-react';
import { Modal, useModal, ConfirmationModal } from '../ui/Modal';
import { Button } from '../ui/Button';
import { Input } from '../ui/Input';
import { cn } from '../../utils/cn';
import { apiClient } from '../../services/api/client';
import type {
  Role,
  Permission,
  PermissionMatrix,
  UserRoleAssignment,
  RoleCreateRequest,
  RoleUpdateRequest,
  PermissionCategory
} from '../../types/admin';
import type { User, ApiResponse, PaginatedResponse } from '../../types/common';

interface PermissionManagementProps {
  className?: string;
}

interface PermissionManagementState {
  matrix: PermissionMatrix | null;
  userAssignments: UserRoleAssignment[];
  users: User[];
  selectedRole: Role | null;
  selectedUser: User | null;
  isLoading: boolean;
  error: string | null;
  isDirty: boolean;
  searchTerm: string;
  selectedCategory: PermissionCategory | 'all';
  showInactiveRoles: boolean;
}

// Role form component
interface RoleFormProps {
  role?: Role;
  permissions: Permission[];
  onSubmit: (data: RoleCreateRequest | RoleUpdateRequest) => Promise<void>;
  onCancel: () => void;
  isLoading: boolean;
}

const RoleForm: React.FC<RoleFormProps> = ({ role, permissions, onSubmit, onCancel, isLoading }) => {
  const [formData, setFormData] = useState({
    name: role?.name || '',
    displayName: role?.displayName || '',
    description: role?.description || '',
    parentRoleId: role?.parentRoleId || '',
    permissionIds: role?.permissions?.map(p => p.id) || []
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [selectedCategory, setSelectedCategory] = useState<PermissionCategory | 'all'>('all');

  const categorizedPermissions = useMemo(() => {
    const categories: Record<PermissionCategory | 'all', Permission[]> = {
      'all': permissions,
      'Content Management': [],
      'User Management': [],
      'System Administration': [],
      'Analytics & Reporting': [],
      'Security & Moderation': [],
      'Media Management': [],
      'Settings & Configuration': []
    };

    permissions.forEach(permission => {
      if (categories[permission.category as PermissionCategory]) {
        categories[permission.category as PermissionCategory].push(permission);
      }
    });

    return categories;
  }, [permissions]);

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) newErrors.name = 'Name is required';
    if (!formData.displayName.trim()) newErrors.displayName = 'Display name is required';
    if (formData.permissionIds.length === 0) newErrors.permissions = 'At least one permission is required';

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    try {
      if (role) {
        await onSubmit({
          displayName: formData.displayName,
          description: formData.description,
          parentRoleId: formData.parentRoleId || undefined,
          permissionIds: formData.permissionIds
        } as RoleUpdateRequest);
      } else {
        await onSubmit({
          name: formData.name,
          displayName: formData.displayName,
          description: formData.description,
          parentRoleId: formData.parentRoleId || undefined,
          permissionIds: formData.permissionIds
        } as RoleCreateRequest);
      }
    } catch (error) {
      console.error('Form submission error:', error);
    }
  };

  const handlePermissionToggle = (permissionId: string) => {
    setFormData(prev => ({
      ...prev,
      permissionIds: prev.permissionIds.includes(permissionId)
        ? prev.permissionIds.filter(id => id !== permissionId)
        : [...prev.permissionIds, permissionId]
    }));
  };

  const displayedPermissions = selectedCategory === 'all'
    ? permissions
    : categorizedPermissions[selectedCategory];

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div>
          <label htmlFor="name" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Role Name *
          </label>
          <Input
            id="name"
            type="text"
            value={formData.name}
            onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
            disabled={!!role || isLoading}
            className={errors.name ? 'border-red-500' : ''}
          />
          {errors.name && <p className="mt-1 text-sm text-red-600">{errors.name}</p>}
        </div>

        <div>
          <label htmlFor="displayName" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Display Name *
          </label>
          <Input
            id="displayName"
            type="text"
            value={formData.displayName}
            onChange={(e) => setFormData(prev => ({ ...prev, displayName: e.target.value }))}
            disabled={isLoading}
            className={errors.displayName ? 'border-red-500' : ''}
          />
          {errors.displayName && <p className="mt-1 text-sm text-red-600">{errors.displayName}</p>}
        </div>
      </div>

      <div>
        <label htmlFor="description" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          Description
        </label>
        <textarea
          id="description"
          rows={3}
          value={formData.description}
          onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
          disabled={isLoading}
          className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white"
        />
      </div>

      <div>
        <div className="flex items-center justify-between mb-4">
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
            Permissions * ({formData.permissionIds.length} selected)
          </label>
          <select
            value={selectedCategory}
            onChange={(e) => setSelectedCategory(e.target.value as PermissionCategory | 'all')}
            className="px-3 py-1 border border-gray-300 dark:border-gray-600 rounded-md text-sm dark:bg-gray-700"
          >
            <option value="all">All Categories</option>
            {Object.keys(categorizedPermissions).filter(cat => cat !== 'all').map(category => (
              <option key={category} value={category}>{category}</option>
            ))}
          </select>
        </div>

        <div className="max-h-64 overflow-y-auto border border-gray-300 dark:border-gray-600 rounded-md p-4 space-y-2">
          {displayedPermissions.map(permission => (
            <div key={permission.id} className="flex items-start space-x-3 p-2 hover:bg-gray-50 dark:hover:bg-gray-700 rounded">
              <input
                type="checkbox"
                id={`permission-${permission.id}`}
                checked={formData.permissionIds.includes(permission.id)}
                onChange={() => handlePermissionToggle(permission.id)}
                disabled={isLoading}
                className="mt-1 h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
              />
              <div className="flex-1 min-w-0">
                <label
                  htmlFor={`permission-${permission.id}`}
                  className="block text-sm font-medium text-gray-900 dark:text-white cursor-pointer"
                >
                  {permission.displayName}
                </label>
                <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                  {permission.description}
                </p>
                <div className="flex items-center space-x-2 mt-1">
                  <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200">
                    {permission.action}
                  </span>
                  <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">
                    {permission.resource}
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>
        {errors.permissions && <p className="mt-2 text-sm text-red-600">{errors.permissions}</p>}
      </div>

      <div className="flex justify-end space-x-3 pt-6 border-t border-gray-200 dark:border-gray-600">
        <Button
          type="button"
          variant="outline"
          onClick={onCancel}
          disabled={isLoading}
        >
          Cancel
        </Button>
        <Button
          type="submit"
          loading={isLoading}
          className="inline-flex items-center"
        >
          <Save className="w-4 h-4 mr-2" />
          {role ? 'Update Role' : 'Create Role'}
        </Button>
      </div>
    </form>
  );
};

// Permission matrix component
interface PermissionMatrixProps {
  matrix: PermissionMatrix;
  onPermissionToggle: (roleId: string, permissionId: string) => void;
  selectedRole: Role | null;
  selectedCategory: PermissionCategory | 'all';
  searchTerm: string;
}

const PermissionMatrix: React.FC<PermissionMatrixProps> = ({
  matrix,
  onPermissionToggle,
  selectedRole,
  selectedCategory,
  searchTerm
}) => {
  const filteredPermissions = useMemo(() => {
    let permissions = matrix.permissions;

    if (selectedCategory !== 'all') {
      permissions = permissions.filter(p => p.category === selectedCategory);
    }

    if (searchTerm) {
      permissions = permissions.filter(p =>
        p.displayName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        p.description.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }

    return permissions;
  }, [matrix.permissions, selectedCategory, searchTerm]);

  const filteredRoles = useMemo(() => {
    if (selectedRole) return [selectedRole];
    return matrix.roles.filter(role => !role.isSystemRole);
  }, [matrix.roles, selectedRole]);

  return (
    <div className="overflow-x-auto">
      <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-600">
        <thead className="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th className="sticky left-0 bg-gray-50 dark:bg-gray-800 px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider z-10">
              Permission
            </th>
            {filteredRoles.map(role => (
              <th
                key={role.id}
                className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider"
              >
                <div className="flex flex-col items-center">
                  <span>{role.displayName}</span>
                  <span className="text-xs font-normal text-gray-400 mt-1">
                    {role.permissions.length} permissions
                  </span>
                </div>
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-600">
          {filteredPermissions.map((permission, index) => (
            <tr
              key={permission.id}
              className={cn(
                index % 2 === 0 ? 'bg-white dark:bg-gray-900' : 'bg-gray-50 dark:bg-gray-800'
              )}
            >
              <td className="sticky left-0 bg-inherit px-6 py-4 whitespace-nowrap z-10 border-r border-gray-200 dark:border-gray-600">
                <div>
                  <div className="text-sm font-medium text-gray-900 dark:text-white">
                    {permission.displayName}
                  </div>
                  <div className="text-sm text-gray-500 dark:text-gray-400">
                    {permission.description}
                  </div>
                  <div className="flex items-center space-x-2 mt-1">
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200">
                      {permission.action}
                    </span>
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">
                      {permission.resource}
                    </span>
                  </div>
                </div>
              </td>
              {filteredRoles.map(role => {
                const hasPermission = matrix.assignments[role.id]?.includes(permission.id);
                return (
                  <td key={`${role.id}-${permission.id}`} className="px-6 py-4 text-center">
                    <button
                      type="button"
                      onClick={() => onPermissionToggle(role.id, permission.id)}
                      className={cn(
                        'inline-flex items-center justify-center w-8 h-8 rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500',
                        hasPermission
                          ? 'bg-green-100 text-green-600 hover:bg-green-200 dark:bg-green-900 dark:text-green-400'
                          : 'bg-gray-100 text-gray-400 hover:bg-gray-200 dark:bg-gray-700 dark:text-gray-500'
                      )}
                      aria-label={`${hasPermission ? 'Revoke' : 'Grant'} ${permission.displayName} for ${role.displayName}`}
                    >
                      {hasPermission ? (
                        <Check className="w-4 h-4" />
                      ) : (
                        <X className="w-4 h-4" />
                      )}
                    </button>
                  </td>
                );
              })}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

// Main Permission Management component
export const PermissionManagement: React.FC<PermissionManagementProps> = ({ className }) => {
  const [state, setState] = useState<PermissionManagementState>({
    matrix: null,
    userAssignments: [],
    users: [],
    selectedRole: null,
    selectedUser: null,
    isLoading: false,
    error: null,
    isDirty: false,
    searchTerm: '',
    selectedCategory: 'all',
    showInactiveRoles: false
  });

  const createRoleModal = useModal();
  const editRoleModal = useModal();
  const deleteRoleModal = useModal();
  const _userAssignmentModal = useModal();

  // Load initial data
  const loadData = useCallback(async () => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));

    try {
      const [matrixResponse, usersResponse, assignmentsResponse] = await Promise.all([
        apiClient.get<ApiResponse<PermissionMatrix>>('/api/admin/permissions/matrix'),
        apiClient.get<ApiResponse<PaginatedResponse<User>>>('/api/admin/users'),
        apiClient.get<ApiResponse<UserRoleAssignment[]>>('/api/admin/permissions/user-assignments')
      ]);

      setState(prev => ({
        ...prev,
        matrix: matrixResponse.data.data!,
        users: usersResponse.data.data!.data,
        userAssignments: assignmentsResponse.data.data!,
        isLoading: false
      }));
    } catch (error) {
      console.error('Failed to load permission data:', error);
      setState(prev => ({
        ...prev,
        error: 'Failed to load permission data',
        isLoading: false
      }));
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Permission toggle handler
  const handlePermissionToggle = useCallback(async (roleId: string, permissionId: string) => {
    if (!state.matrix) return;

    const hasPermission = state.matrix.assignments[roleId]?.includes(permissionId);

    try {
      if (hasPermission) {
        await apiClient.delete(`/api/admin/permissions/roles/${roleId}/permissions/${permissionId}`);
      } else {
        await apiClient.post(`/api/admin/permissions/roles/${roleId}/permissions/${permissionId}`);
      }

      // Update local state
      setState(prev => {
        if (!prev.matrix) return prev;

        const newAssignments = { ...prev.matrix.assignments };
        if (hasPermission) {
          newAssignments[roleId] = newAssignments[roleId]?.filter(id => id !== permissionId) || [];
        } else {
          newAssignments[roleId] = [...(newAssignments[roleId] || []), permissionId];
        }

        return {
          ...prev,
          matrix: {
            ...prev.matrix,
            assignments: newAssignments
          },
          isDirty: true
        };
      });
    } catch (error) {
      console.error('Failed to toggle permission:', error);
      setState(prev => ({ ...prev, error: 'Failed to update permission' }));
    }
  }, [state.matrix]);

  // Role management handlers
  const handleCreateRole = useCallback(async (data: RoleCreateRequest | RoleUpdateRequest) => {
    try {
      const response = await apiClient.post<ApiResponse<Role>>('/api/admin/permissions/roles', data as RoleCreateRequest);
      const newRole = response.data.data!;

      setState(prev => {
        if (!prev.matrix) return prev;

        return {
          ...prev,
          matrix: {
            ...prev.matrix,
            roles: [...prev.matrix.roles, newRole],
            assignments: { ...prev.matrix.assignments, [newRole.id]: data.permissionIds }
          }
        };
      });

      createRoleModal.closeModal();
    } catch (error) {
      console.error('Failed to create role:', error);
      throw error;
    }
  }, [createRoleModal]);

  const handleUpdateRole = useCallback(async (data: RoleUpdateRequest) => {
    if (!state.selectedRole) return;

    try {
      const response = await apiClient.put<ApiResponse<Role>>(
        `/api/admin/permissions/roles/${state.selectedRole.id}`,
        data
      );
      const updatedRole = response.data.data!;

      setState(prev => {
        if (!prev.matrix) return prev;

        return {
          ...prev,
          matrix: {
            ...prev.matrix,
            roles: prev.matrix.roles.map(role =>
              role.id === updatedRole.id ? updatedRole : role
            ),
            assignments: { ...prev.matrix.assignments, [updatedRole.id]: data.permissionIds }
          },
          selectedRole: updatedRole
        };
      });

      editRoleModal.closeModal();
    } catch (error) {
      console.error('Failed to update role:', error);
      throw error;
    }
  }, [state.selectedRole, editRoleModal]);

  const handleDeleteRole = useCallback(async () => {
    if (!state.selectedRole) return;

    try {
      await apiClient.delete(`/api/admin/permissions/roles/${state.selectedRole.id}`);

      setState(prev => {
        if (!prev.matrix) return prev;

        const newAssignments = { ...prev.matrix.assignments };
        delete newAssignments[prev.selectedRole!.id];

        return {
          ...prev,
          matrix: {
            ...prev.matrix,
            roles: prev.matrix.roles.filter(role => role.id !== prev.selectedRole!.id),
            assignments: newAssignments
          },
          selectedRole: null
        };
      });

      deleteRoleModal.closeModal();
    } catch (error) {
      console.error('Failed to delete role:', error);
      setState(prev => ({ ...prev, error: 'Failed to delete role' }));
    }
  }, [state.selectedRole, deleteRoleModal]);

  // Export/Import handlers
  const handleExport = useCallback(async () => {
    try {
      const response = await apiClient.get('/api/admin/permissions/export', {
        responseType: 'blob'
      });

      const blob = new Blob([response.data as BlobPart], { type: 'application/json' });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.style.display = 'none';
      a.href = url;
      a.download = `permissions-${new Date().toISOString().split('T')[0]}.json`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (error) {
      console.error('Failed to export permissions:', error);
      setState(prev => ({ ...prev, error: 'Failed to export permissions' }));
    }
  }, []);

  if (state.isLoading && !state.matrix) {
    return (
      <div className={cn('flex items-center justify-center h-64', className)}>
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (state.error && !state.matrix) {
    return (
      <div className={cn('text-center py-8', className)}>
        <AlertTriangle className="mx-auto h-12 w-12 text-red-500 mb-4" />
        <p className="text-red-600 mb-4">{state.error}</p>
        <Button onClick={loadData}>Retry</Button>
      </div>
    );
  }

  if (!state.matrix) {
    return null;
  }

  return (
    <div className={cn('space-y-6', className)}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center">
            <Shield className="w-6 h-6 mr-3 text-blue-600" />
            Permission Management
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Manage roles and permissions across the system
          </p>
        </div>
        <div className="flex items-center space-x-3">
          <Button
            variant="outline"
            onClick={handleExport}
            className="inline-flex items-center"
          >
            <Download className="w-4 h-4 mr-2" />
            Export
          </Button>
          <Button
            onClick={createRoleModal.openModal}
            className="inline-flex items-center"
          >
            <Plus className="w-4 h-4 mr-2" />
            Create Role
          </Button>
        </div>
      </div>

      {/* Controls */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between space-y-4 sm:space-y-0 sm:space-x-4">
        <div className="flex items-center space-x-4">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
            <Input
              type="text"
              placeholder="Search permissions..."
              value={state.searchTerm}
              onChange={(e) => setState(prev => ({ ...prev, searchTerm: e.target.value }))}
              className="pl-10 w-64"
            />
          </div>
          <select
            value={state.selectedCategory}
            onChange={(e) => setState(prev => ({ ...prev, selectedCategory: e.target.value as PermissionCategory | 'all' }))}
            className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md dark:bg-gray-700 dark:text-white"
          >
            <option value="all">All Categories</option>
            <option value="Content Management">Content Management</option>
            <option value="User Management">User Management</option>
            <option value="System Administration">System Administration</option>
            <option value="Analytics & Reporting">Analytics & Reporting</option>
            <option value="Security & Moderation">Security & Moderation</option>
            <option value="Media Management">Media Management</option>
            <option value="Settings & Configuration">Settings & Configuration</option>
          </select>
        </div>

        <div className="flex items-center space-x-4">
          {state.selectedRole && (
            <div className="flex items-center space-x-2">
              <span className="text-sm text-gray-600 dark:text-gray-400">
                Editing: {state.selectedRole.displayName}
              </span>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setState(prev => ({ ...prev, selectedRole: null }))}
              >
                <X className="w-4 h-4" />
              </Button>
            </div>
          )}
        </div>
      </div>

      {/* Role List */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        <div className="lg:col-span-1">
          <div className="bg-white dark:bg-gray-800 rounded-lg shadow">
            <div className="p-4 border-b border-gray-200 dark:border-gray-600">
              <h3 className="text-lg font-medium text-gray-900 dark:text-white">Roles</h3>
            </div>
            <div className="p-4 space-y-2 max-h-96 overflow-y-auto">
              {state.matrix.roles.map(role => (
                <div
                  key={role.id}
                  className={cn(
                    'flex items-center justify-between p-3 rounded-lg cursor-pointer transition-colors',
                    state.selectedRole?.id === role.id
                      ? 'bg-blue-50 border-blue-200 dark:bg-blue-900 dark:border-blue-700'
                      : 'hover:bg-gray-50 dark:hover:bg-gray-700'
                  )}
                  onClick={() => setState(prev => ({ ...prev, selectedRole: role }))}
                >
                  <div>
                    <div className="text-sm font-medium text-gray-900 dark:text-white">
                      {role.displayName}
                    </div>
                    <div className="text-xs text-gray-500 dark:text-gray-400">
                      {role.permissions.length} permissions
                    </div>
                    {role.isSystemRole && (
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200 mt-1">
                        System
                      </span>
                    )}
                  </div>
                  {!role.isSystemRole && (
                    <div className="flex items-center space-x-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={(e) => {
                          e.stopPropagation();
                          setState(prev => ({ ...prev, selectedRole: role }));
                          editRoleModal.openModal();
                        }}
                        className="h-6 w-6"
                      >
                        <Edit2 className="w-3 h-3" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={(e) => {
                          e.stopPropagation();
                          setState(prev => ({ ...prev, selectedRole: role }));
                          deleteRoleModal.openModal();
                        }}
                        className="h-6 w-6 text-red-600 hover:text-red-700"
                      >
                        <Trash2 className="w-3 h-3" />
                      </Button>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Permission Matrix */}
        <div className="lg:col-span-3">
          <div className="bg-white dark:bg-gray-800 rounded-lg shadow">
            <div className="p-4 border-b border-gray-200 dark:border-gray-600">
              <h3 className="text-lg font-medium text-gray-900 dark:text-white">Permission Matrix</h3>
            </div>
            <div className="p-4">
              <PermissionMatrix
                matrix={state.matrix}
                onPermissionToggle={handlePermissionToggle}
                selectedRole={state.selectedRole}
                selectedCategory={state.selectedCategory}
                searchTerm={state.searchTerm}
              />
            </div>
          </div>
        </div>
      </div>

      {/* Create Role Modal */}
      <Modal
        isOpen={createRoleModal.isOpen}
        onClose={createRoleModal.closeModal}
        title="Create New Role"
        size="lg"
      >
        <RoleForm
          permissions={state.matrix.permissions}
          onSubmit={handleCreateRole}
          onCancel={createRoleModal.closeModal}
          isLoading={false}
        />
      </Modal>

      {/* Edit Role Modal */}
      <Modal
        isOpen={editRoleModal.isOpen}
        onClose={editRoleModal.closeModal}
        title="Edit Role"
        size="lg"
      >
        {state.selectedRole && (
          <RoleForm
            role={state.selectedRole}
            permissions={state.matrix.permissions}
            onSubmit={handleUpdateRole}
            onCancel={editRoleModal.closeModal}
            isLoading={false}
          />
        )}
      </Modal>

      {/* Delete Role Confirmation */}
      <ConfirmationModal
        isOpen={deleteRoleModal.isOpen}
        onClose={deleteRoleModal.closeModal}
        onConfirm={handleDeleteRole}
        title="Delete Role"
        message={`Are you sure you want to delete the role "${state.selectedRole?.displayName}"? This action cannot be undone and will remove all associated permissions.`}
        confirmText="Delete"
        variant="destructive"
      />

      {/* Error Alert */}
      {state.error && (
        <div className="fixed top-4 right-4 bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded z-50">
          <div className="flex items-center">
            <AlertTriangle className="w-4 h-4 mr-2" />
            {state.error}
            <button
              onClick={() => setState(prev => ({ ...prev, error: null }))}
              className="ml-4 text-red-500 hover:text-red-700"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default PermissionManagement;
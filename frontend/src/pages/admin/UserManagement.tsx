import React, { useState, useEffect } from 'react';
import {
  Card,
  CardContent,
  CardDescription as _CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { adminUserApi, type AdminUser, type CreateUserRequest, type UpdateUserRequest, type UserFilters, type UserSortConfig, type BulkOperation, type UserStatistics } from '@/services/adminUserApi';
import { toastService } from '@/services/toastService';
import { ConfirmationDialog, BulkActionConfirmation } from '@/components/common/ConfirmationDialog';
import { UserManagementSkeleton, StatsCardSkeleton } from '@/components/common/SkeletonLoader';
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@/components/ui/tabs';
import { Separator as _Separator } from '@/components/ui/separator';
import UserAvatar from '@/components/common/UserAvatar';
import {
  Users,
  Search,
  Filter,
  MoreHorizontal,
  Edit,
  Trash2,
  UserCheck,
  UserX,
  Key,
  Plus,
  Download,
  Upload as _Upload,
  Grid3x3,
  List,
  Calendar,
  Mail,
  Shield,
  Activity,
  CheckSquare,
  Square,
  RotateCcw,
  Eye,
  EyeOff as _EyeOff,
  Settings,
  AlertTriangle,
  CheckCircle,
  Clock as _Clock,
  Ban,
} from 'lucide-react';

// All types are now imported from adminUserApi service

const UserManagement: React.FC = () => {
  // State management
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [viewMode, setViewMode] = useState<'table' | 'card'>('table');
  const [selectedUsers, setSelectedUsers] = useState<Set<string>>(new Set());
  const [showFilters, setShowFilters] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showBulkActionModal, setShowBulkActionModal] = useState(false);
  const [editingUser, setEditingUser] = useState<AdminUser | null>(null);
  const [bulkOperation, setBulkOperation] = useState<BulkOperation>('delete');

  // Confirmation states
  const [confirmation, setConfirmation] = useState<{
    open: boolean;
    type?: string;
    data?: AdminUser;
    title?: string;
    message?: string;
    onConfirm?: () => void | Promise<void>;
    onCancel?: () => void;
  }>({ open: false });

  const [bulkConfirmation, setBulkConfirmation] = useState<{
    open: boolean;
    action?: string;
    itemCount?: number;
    itemType?: string;
    severity?: 'info' | 'warning' | 'danger';
    onConfirm?: () => void | Promise<void>;
    onCancel?: () => void;
  }>({ open: false });

  const [userDetailsModal, setUserDetailsModal] = useState<{
    open: boolean;
    user?: AdminUser;
  }>({ open: false });

  // Additional state for enterprise features
  const [error, setError] = useState<string | null>(null);
  const [statistics, setStatistics] = useState<UserStatistics | null>(null);

  // Filters state
  const [filters, setFilters] = useState<UserFilters>({
    search: '',
    role: '',
    status: '',
    emailVerified: undefined,
    twoFactorEnabled: undefined,
    isOnline: undefined,
    registrationDateFrom: '',
    registrationDateTo: '',
    lastLoginFrom: '',
    lastLoginTo: '',
  });

  // Sort and pagination state
  const [sortConfig, setSortConfig] = useState<UserSortConfig>({
    field: 'registrationDate',
    direction: 'desc'
  });
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 20,
    total: 0,
    totalPages: 0,
  });

  // Real API integration - Load users with comprehensive error handling
  const loadUsers = React.useCallback(async (showLoading = true) => {
    try {
      if (showLoading) setLoading(true);
      setError(null);

      const response = await adminUserApi.getUsers(
        filters,
        sortConfig,
        { page: pagination.page, pageSize: pagination.pageSize }
      );

      setUsers(response.data);
      setPagination(prev => ({
        ...prev,
        total: response.pagination.total,
        totalPages: response.pagination.totalPages,
      }));

      toastService.success('User data loaded successfully', {
        groupId: 'user-load',
      });

    } catch (error) {
      console.error('Failed to load users:', error);
      const errorMessage = error instanceof Error ? error.message : 'Failed to load users';
      setError(errorMessage);
      
      toastService.error('Failed to load users', {
        groupId: 'user-load',
        actions: [
          {
            id: 'retry',
            label: 'Retry',
            variant: 'primary',
          }
        ],
        onAction: (toast, actionId) => {
          if (actionId === 'retry') {
            loadUsers(true);
            toastService.hide(toast.id);
          }
        }
      });
    } finally {
      setLoading(false);
    }
  }, [filters, sortConfig, pagination.page, pagination.pageSize]);

  // Load statistics
  const loadStatistics = React.useCallback(async () => {
    try {
      const stats = await adminUserApi.getUserStatistics();
      setStatistics(stats);
    } catch (error) {
      console.error('Failed to load statistics:', error);
      toastService.warning('Statistics temporarily unavailable');
    }
  }, []);

  // Initial data load
  useEffect(() => {
    loadUsers();
    loadStatistics();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Reload users when filters, sort, or pagination changes
  useEffect(() => {
    const timeoutId = setTimeout(() => {
      loadUsers(false); // Don't show loading for filter changes
    }, 300); // Debounce

    return () => clearTimeout(timeoutId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filters, sortConfig, pagination.page, pagination.pageSize]);

  // Server-side filtering and pagination - no client-side processing needed
  // All filtering, sorting, and pagination is handled by the API

  // Computed values for template compatibility
  const filteredAndSortedUsers = users; // Server handles filtering and sorting
  const paginatedUsers = users; // Server handles pagination

  // Utility functions
  const getStatusColor = (status: AdminUser['status']) => {
    switch (status) {
      case 'Active':
        return 'bg-green-100 text-green-800';
      case 'Inactive':
        return 'bg-gray-100 text-gray-800';
      case 'Suspended':
        return 'bg-yellow-100 text-yellow-800';
      case 'Banned':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const getRoleColor = (role: AdminUser['primaryRole']) => {
    switch (role) {
      case 'Admin':
        return 'bg-purple-100 text-purple-800';
      case 'Author':
        return 'bg-blue-100 text-blue-800';
      case 'User':
        return 'bg-gray-100 text-gray-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  const formatDateTime = (dateString: string) => {
    return new Date(dateString).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  // Action handlers
  const handleSort = (field: keyof AdminUser) => {
    setSortConfig(prev => ({
      field,
      direction: prev.field === field && prev.direction === 'asc' ? 'desc' : 'asc',
    }));
  };

  const handleSelectUser = (userId: string) => {
    setSelectedUsers(prev => {
      const newSelected = new Set(prev);
      if (newSelected.has(userId)) {
        newSelected.delete(userId);
      } else {
        newSelected.add(userId);
      }
      return newSelected;
    });
  };

  const handleSelectAll = () => {
    if (selectedUsers.size === users.length && users.length > 0) {
      setSelectedUsers(new Set());
    } else {
      setSelectedUsers(new Set(users.map(user => user.id)));
    }
  };

  const handleUserAction = async (action: string, user: AdminUser) => {
    switch (action) {
      case 'edit':
        setEditingUser(user);
        break;
      case 'delete':
        setConfirmation({
          open: true,
          type: 'deleteUser',
          data: user,
          title: 'Delete User',
          message: `Are you sure you want to delete user "${user.displayName}"? This action cannot be undone.`,
          onConfirm: async () => {
            try {
              await adminUserApi.deleteUser(user.id, {
                anonymizeData: true,
                reason: 'Admin deletion via user management interface'
              });
              
              // Optimistic update
              setUsers(prev => prev.filter(u => u.id !== user.id));
              toastService.success(`User "${user.displayName}" deleted successfully`);
              
              // Reload data
              await loadUsers(false);
              await loadStatistics();
            } catch (error) {
              console.error('Failed to delete user:', error);
              toastService.error('Failed to delete user');
            }
            setConfirmation({ open: false });
          },
          onCancel: () => setConfirmation({ open: false })
        });
        break;
      case 'toggleStatus': {
        const newStatus = user.status === 'Active' ? 'Inactive' : 'Active';
        try {
          await adminUserApi.updateUser(user.id, { status: newStatus });
          setUsers(prev => prev.map(u =>
            u.id === user.id ? { ...u, status: newStatus } : u
          ));
          toastService.success(`User status updated to ${newStatus}`);
        } catch (error) {
          console.error('Failed to update user status:', error);
          toastService.error('Failed to update user status');
        }
        break;
      }
      case 'resetPassword':
        setConfirmation({
          open: true,
          type: 'resetPassword',
          data: user,
          title: 'Reset Password',
          message: `Are you sure you want to reset the password for "${user.displayName}"? A new password will be sent via email.`,
          onConfirm: async () => {
            try {
              await adminUserApi.resetUserPassword(user.id, {
                userId: user.id,
                temporaryPassword: true,
                requireChangeOnLogin: true,
                sendEmail: true,
                expiryHours: 24,
              });
              toastService.success(`Password reset email sent to ${user.email}`);
            } catch (error) {
              console.error('Failed to reset password:', error);
              toastService.error('Failed to reset password');
            }
            setConfirmation({ open: false });
          },
          onCancel: () => setConfirmation({ open: false })
        });
        break;
      case 'suspend':
        try {
          await adminUserApi.updateUser(user.id, { status: 'Suspended' });
          setUsers(prev => prev.map(u => 
            u.id === user.id ? { ...u, status: 'Suspended' as AdminUser['status'] } : u
          ));
          toastService.success(`User ${user.displayName} suspended`);
        } catch (error) {
          console.error('Failed to suspend user:', error);
          toastService.error('Failed to suspend user');
        }
        break;
      case 'unsuspend':
        try {
          await adminUserApi.updateUser(user.id, { status: 'Active' });
          setUsers(prev => prev.map(u => 
            u.id === user.id ? { ...u, status: 'Active' as AdminUser['status'] } : u
          ));
          toastService.success(`User ${user.displayName} unsuspended`);
        } catch (error) {
          console.error('Failed to unsuspend user:', error);
          toastService.error('Failed to unsuspend user');
        }
        break;
      case 'revokeSessions':
        try {
          const result = await adminUserApi.revokeUserSessions(
            user.id,
            'Admin action via user management interface'
          );
          toastService.success(`Revoked ${result.revokedSessions} active sessions`);
        } catch (error) {
          console.error('Failed to revoke sessions:', error);
          toastService.error('Failed to revoke user sessions');
        }
        break;
      default:
        break;
    }
  };

  const handleBulkAction = async () => {
    if (selectedUsers.size === 0) return;
    
    switch (bulkOperation) {
      case 'delete':
        setBulkConfirmation({
          open: true,
          action: 'delete',
          itemCount: selectedUsers.size,
          itemType: 'users',
          severity: 'danger',
          onConfirm: async () => {
            try {
              const userIds = Array.from(selectedUsers);
              const result = await adminUserApi.performBulkOperation({
                userIds,
                operation: 'delete',
                reason: 'Bulk delete via admin interface',
                notifyUsers: true,
              });

              if (result.failureCount > 0) {
                toastService.warning(
                  `Bulk operation completed with ${result.failureCount} failures`,
                  {
                    title: 'Partial Success',
                    actions: [{ id: 'details', label: 'View Details', variant: 'primary' }]
                  }
                );
              } else {
                toastService.success(`Successfully deleted ${result.successCount} users`);
              }

              setSelectedUsers(new Set());
              await loadUsers(false);
              await loadStatistics();
            } catch (error) {
              console.error('Bulk operation failed:', error);
              toastService.error('Bulk operation failed');
            }
            setBulkConfirmation({ open: false });
          },
          onCancel: () => setBulkConfirmation({ open: false })
        });
        break;
      case 'activate':
      case 'deactivate':
      case 'suspend':
      case 'unsuspend':
      case 'verify-email':
      case 'reset-password':
        try {
          const userIds = Array.from(selectedUsers);
          const result = await adminUserApi.performBulkOperation({
            userIds,
            operation: bulkOperation,
            reason: `Bulk ${bulkOperation} via admin interface`,
            notifyUsers: true,
          });

          if (result.failureCount > 0) {
            toastService.warning(
              `Bulk operation completed with ${result.failureCount} failures`,
              {
                title: 'Partial Success',
              }
            );
          } else {
            toastService.success(
              `Successfully ${bulkOperation}d ${result.successCount} users`
            );
          }

          setSelectedUsers(new Set());
          await loadUsers(false);
          await loadStatistics();
        } catch (error) {
          console.error('Bulk operation failed:', error);
          toastService.error('Bulk operation failed');
        }
        break;
      default:
        break;
    }
    
    setShowBulkActionModal(false);
  };

  const handleExport = () => {
    const csvContent = [
      ['Username', 'Email', 'Display Name', 'Role', 'Status', 'Registration Date', 'Last Login', 'Post Count', 'Comment Count'].join(','),
      ...filteredAndSortedUsers.map(user => [
        user.username,
        user.email,
        user.displayName,
        user.primaryRole,
        user.status,
        formatDate(user.registrationDate),
        user.lastLogin ? formatDateTime(user.lastLogin) : 'Never',
        user.postCount.toString(),
        user.commentCount.toString(),
      ].join(',')),
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `users_${new Date().toISOString().split('T')[0]}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const clearFilters = () => {
    setFilters({
      search: '',
      role: '',
      status: '',
      emailVerified: undefined,
      twoFactorEnabled: undefined,
      isOnline: undefined,
      registrationDateFrom: '',
      registrationDateTo: '',
      lastLoginFrom: '',
      lastLoginTo: '',
    });
  };

  if (loading && users.length === 0) {
    return <UserManagementSkeleton viewMode={viewMode} showFilters={showFilters} />;
  }

  if (error && users.length === 0) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center space-y-4">
          <AlertTriangle className="h-12 w-12 text-red-500 mx-auto" />
          <div>
            <h3 className="text-lg font-semibold text-gray-900">Failed to Load Users</h3>
            <p className="text-gray-600 mt-2">{error}</p>
          </div>
          <Button onClick={() => loadUsers(true)} variant="outline">
            <RotateCcw className="mr-2 h-4 w-4" />
            Retry
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">User Management</h1>
          <p className="text-gray-600">
            Manage users, roles, and permissions across the platform
          </p>
        </div>
        <div className="flex items-center space-x-2">
          <Button onClick={handleExport} variant="outline" size="sm">
            <Download className="mr-2 h-4 w-4" />
            Export
          </Button>
          <Button onClick={() => setShowCreateModal(true)} size="sm">
            <Plus className="mr-2 h-4 w-4" />
            Add User
          </Button>
        </div>
      </div>

      {/* Stats Cards */}
      {statistics ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Total Users</CardTitle>
              <Users className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{statistics.totalUsers}</div>
              <p className="text-xs text-muted-foreground">
                {statistics.activeUsers} active
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Online Now</CardTitle>
              <Activity className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{statistics.onlineUsers}</div>
              <p className="text-xs text-muted-foreground">
                {statistics.totalUsers > 0 ? Math.round((statistics.onlineUsers / statistics.totalUsers) * 100) : 0}% of total
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">New This Month</CardTitle>
              <Calendar className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{statistics.newUsersThisMonth}</div>
              <p className="text-xs text-muted-foreground">
                Last 30 days
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Verification Rate</CardTitle>
              <Mail className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">
                {statistics.totalUsers > 0 ? Math.round((statistics.verifiedUsers / statistics.totalUsers) * 100) : 0}%
              </div>
              <p className="text-xs text-muted-foreground">
                Email verified
              </p>
            </CardContent>
          </Card>
        </div>
      ) : (
        <StatsCardSkeleton count={4} />
      )}

      {/* Filters and Controls */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="text-lg">User Filters & Controls</CardTitle>
            <div className="flex items-center space-x-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowFilters(!showFilters)}
              >
                <Filter className="mr-2 h-4 w-4" />
                Filters
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setViewMode(viewMode === 'table' ? 'card' : 'table')}
              >
                {viewMode === 'table' ? (
                  <Grid3x3 className="mr-2 h-4 w-4" />
                ) : (
                  <List className="mr-2 h-4 w-4" />
                )}
                {viewMode === 'table' ? 'Card View' : 'Table View'}
              </Button>
              {selectedUsers.size > 0 && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowBulkActionModal(true)}
                >
                  <Settings className="mr-2 h-4 w-4" />
                  Bulk Actions ({selectedUsers.size})
                </Button>
              )}
            </div>
          </div>
        </CardHeader>

        {showFilters && (
          <CardContent>
            <div className="grid gap-4 md:grid-cols-3 lg:grid-cols-4">
              <div>
                <Input
                  placeholder="Search users..."
                  value={filters.search}
                  onChange={(e) => setFilters(prev => ({ ...prev, search: e.target.value }))}
                  leftIcon={<Search className="h-4 w-4" />}
                />
              </div>

              <div>
                <select
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  value={filters.role}
                  onChange={(e) => setFilters(prev => ({ ...prev, role: e.target.value }))}
                >
                  <option value="">All Roles</option>
                  <option value="Admin">Admin</option>
                  <option value="Author">Author</option>
                  <option value="User">User</option>
                </select>
              </div>

              <div>
                <select
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  value={filters.status}
                  onChange={(e) => setFilters(prev => ({ ...prev, status: e.target.value }))}
                >
                  <option value="">All Statuses</option>
                  <option value="Active">Active</option>
                  <option value="Inactive">Inactive</option>
                  <option value="Suspended">Suspended</option>
                  <option value="Banned">Banned</option>
                </select>
              </div>

              <div>
                <select
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  value={filters.emailVerified === undefined ? '' : filters.emailVerified.toString()}
                  onChange={(e) => setFilters(prev => ({
                    ...prev,
                    emailVerified: e.target.value === '' ? undefined : e.target.value === 'true'
                  }))}
                >
                  <option value="">All Verification</option>
                  <option value="true">Verified</option>
                  <option value="false">Unverified</option>
                </select>
              </div>

              <div>
                <Input
                  type="date"
                  placeholder="From Date"
                  value={filters.registrationDateFrom}
                  onChange={(e) => setFilters(prev => ({ ...prev, registrationDateFrom: e.target.value }))}
                />
              </div>

              <div>
                <Input
                  type="date"
                  placeholder="To Date"
                  value={filters.registrationDateTo}
                  onChange={(e) => setFilters(prev => ({ ...prev, registrationDateTo: e.target.value }))}
                />
              </div>

              <div>
                <select
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  value={filters.isOnline === undefined ? '' : filters.isOnline.toString()}
                  onChange={(e) => setFilters(prev => ({
                    ...prev,
                    isOnline: e.target.value === '' ? undefined : e.target.value === 'true'
                  }))}
                >
                  <option value="">All Users</option>
                  <option value="true">Online</option>
                  <option value="false">Offline</option>
                </select>
              </div>

              <div>
                <Button
                  variant="outline"
                  onClick={clearFilters}
                  className="w-full"
                >
                  <RotateCcw className="mr-2 h-4 w-4" />
                  Clear
                </Button>
              </div>
            </div>

            {(filters.search || filters.role || filters.status || filters.emailVerified !== null) && (
              <div className="mt-4 p-3 bg-blue-50 rounded-lg">
                <p className="text-sm text-blue-700">
                  Showing {filteredAndSortedUsers.length} of {users.length} users
                  {filters.search && ` matching "${filters.search}"`}
                  {filters.role && ` with role "${filters.role}"`}
                  {filters.status && ` with status "${filters.status}"`}
                </p>
              </div>
            )}
          </CardContent>
        )}
      </Card>

      {/* User List */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            <span>Users ({filteredAndSortedUsers.length})</span>
            {selectedUsers.size > 0 && (
              <span className="text-sm font-normal text-blue-600">
                {selectedUsers.size} selected
              </span>
            )}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {viewMode === 'table' ? (
            <div className="overflow-x-auto">
              <table className="w-full table-auto">
                <thead>
                  <tr className="border-b">
                    <th className="text-left py-2 px-2">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={handleSelectAll}
                        className="p-0 h-auto"
                      >
                        {selectedUsers.size === paginatedUsers.length && paginatedUsers.length > 0 ? (
                          <CheckSquare className="h-4 w-4" />
                        ) : (
                          <Square className="h-4 w-4" />
                        )}
                      </Button>
                    </th>
                    <th className="text-left py-2 px-4 cursor-pointer hover:bg-gray-50" onClick={() => handleSort('username')}>
                      <div className="flex items-center">
                        User
                        {sortConfig.field === 'username' && (
                          <span className="ml-1 text-xs">
                            {sortConfig.direction === 'asc' ? '↑' : '↓'}
                          </span>
                        )}
                      </div>
                    </th>
                    <th className="text-left py-2 px-4 cursor-pointer hover:bg-gray-50" onClick={() => handleSort('primaryRole')}>
                      <div className="flex items-center">
                        Role
                        {sortConfig.field === 'primaryRole' && (
                          <span className="ml-1 text-xs">
                            {sortConfig.direction === 'asc' ? '↑' : '↓'}
                          </span>
                        )}
                      </div>
                    </th>
                    <th className="text-left py-2 px-4 cursor-pointer hover:bg-gray-50" onClick={() => handleSort('status')}>
                      <div className="flex items-center">
                        Status
                        {sortConfig.field === 'status' && (
                          <span className="ml-1 text-xs">
                            {sortConfig.direction === 'asc' ? '↑' : '↓'}
                          </span>
                        )}
                      </div>
                    </th>
                    <th className="text-left py-2 px-4 cursor-pointer hover:bg-gray-50" onClick={() => handleSort('registrationDate')}>
                      <div className="flex items-center">
                        Joined
                        {sortConfig.field === 'registrationDate' && (
                          <span className="ml-1 text-xs">
                            {sortConfig.direction === 'asc' ? '↑' : '↓'}
                          </span>
                        )}
                      </div>
                    </th>
                    <th className="text-left py-2 px-4 cursor-pointer hover:bg-gray-50" onClick={() => handleSort('lastLogin')}>
                      <div className="flex items-center">
                        Last Login
                        {sortConfig.field === 'lastLogin' && (
                          <span className="ml-1 text-xs">
                            {sortConfig.direction === 'asc' ? '↑' : '↓'}
                          </span>
                        )}
                      </div>
                    </th>
                    <th className="text-left py-2 px-4">Content</th>
                    <th className="text-left py-2 px-4">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {paginatedUsers.map((user) => (
                    <tr key={user.id} className="border-b hover:bg-gray-50">
                      <td className="py-3 px-2">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleSelectUser(user.id)}
                          className="p-0 h-auto"
                        >
                          {selectedUsers.has(user.id) ? (
                            <CheckSquare className="h-4 w-4 text-blue-600" />
                          ) : (
                            <Square className="h-4 w-4" />
                          )}
                        </Button>
                      </td>
                      <td className="py-3 px-4">
                        <div className="flex items-center space-x-3">
                          <div className="relative">
                            <UserAvatar
                              user={{
                                id: user.id,
                                displayName: user.displayName,
                                username: user.username,
                                avatarUrl: user.avatar,
                                role: user.primaryRole,
                                isVip: false,
                              }}
                              size="md"
                              showStatus={false}
                              showRole={false}
                            />
                            {user.isOnline && (
                              <div className="absolute -bottom-0.5 -right-0.5 w-3 h-3 bg-green-400 border-2 border-white rounded-full"></div>
                            )}
                          </div>
                          <div>
                            <div className="font-medium">{user.displayName}</div>
                            <div className="text-sm text-gray-500">@{user.username}</div>
                            <div className="text-xs text-gray-400">{user.email}</div>
                          </div>
                        </div>
                      </td>
                      <td className="py-3 px-4">
                        <Badge className={getRoleColor(user.primaryRole)}>
                          {user.primaryRole}
                        </Badge>
                      </td>
                      <td className="py-3 px-4">
                        <div className="flex items-center space-x-2">
                          <Badge className={getStatusColor(user.status)}>
                            {user.status}
                          </Badge>
                          {user.emailVerified && (
                            <CheckCircle className="h-4 w-4 text-green-500" />
                          )}
                          {user.twoFactorEnabled && (
                            <Shield className="h-4 w-4 text-blue-500" />
                          )}
                        </div>
                      </td>
                      <td className="py-3 px-4">
                        <div className="text-sm">
                          {formatDate(user.registrationDate)}
                        </div>
                        <div className="text-xs text-gray-500">
                          {user.accountAge}
                        </div>
                      </td>
                      <td className="py-3 px-4">
                        <div className="text-sm">
                          {user.lastLogin ? formatDateTime(user.lastLogin) : 'Never'}
                        </div>
                        {user.lastLogin && (
                          <div className="text-xs text-gray-500">
                            {user.loginCount} logins
                          </div>
                        )}
                      </td>
                      <td className="py-3 px-4">
                        <div className="text-sm">
                          <div>{user.postCount} posts</div>
                          <div className="text-xs text-gray-500">{user.commentCount} comments</div>
                        </div>
                      </td>
                      <td className="py-3 px-4">
                        <div className="flex items-center space-x-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleUserAction('edit', user)}
                            className="p-1 h-auto"
                            title="Edit User"
                          >
                            <Edit className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleUserAction('toggleStatus', user)}
                            className="p-1 h-auto"
                            title={user.status === 'Active' ? 'Deactivate' : 'Activate'}
                          >
                            {user.status === 'Active' ? (
                              <UserX className="h-4 w-4" />
                            ) : (
                              <UserCheck className="h-4 w-4" />
                            )}
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleUserAction('resetPassword', user)}
                            className="p-1 h-auto"
                            title="Reset Password"
                          >
                            <Key className="h-4 w-4" />
                          </Button>
                          <div className="relative group">
                            <Button
                              variant="ghost"
                              size="sm"
                              className="p-1 h-auto"
                            >
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                            <div className="absolute right-0 top-8 w-48 bg-white border rounded-md shadow-lg hidden group-hover:block z-10">
                              <div className="py-1">
                                <button
                                  className="block w-full text-left px-3 py-2 text-sm hover:bg-gray-50"
                                  onClick={() => handleUserAction('suspend', user)}
                                >
                                  <Ban className="inline-block w-4 h-4 mr-2" />
                                  Suspend User
                                </button>
                                <button
                                  className="block w-full text-left px-3 py-2 text-sm hover:bg-gray-50"
                                  onClick={() => setUserDetailsModal({ open: true, user })}
                                >
                                  <Eye className="inline-block w-4 h-4 mr-2" />
                                  View Details
                                </button>
                                <button
                                  className="block w-full text-left px-3 py-2 text-sm hover:bg-gray-50 text-red-600"
                                  onClick={() => handleUserAction('delete', user)}
                                >
                                  <Trash2 className="inline-block w-4 h-4 mr-2" />
                                  Delete User
                                </button>
                              </div>
                            </div>
                          </div>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>

              {paginatedUsers.length === 0 && (
                <div className="text-center py-12">
                  <Users className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                  <h3 className="text-lg font-medium text-gray-900 mb-2">No users found</h3>
                  <p className="text-gray-500">Try adjusting your filters or search terms</p>
                </div>
              )}
            </div>
          ) : (
            // Card View
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
              {paginatedUsers.map((user) => (
                <Card key={user.id} className="relative">
                  <CardHeader className="pb-3">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-x-3">
                        <div className="relative">
                          <UserAvatar
                            user={{
                              id: user.id,
                              displayName: user.displayName,
                              username: user.username,
                              avatarUrl: user.avatar,
                              role: user.primaryRole,
                              isVip: false,
                            }}
                            size="md"
                            showStatus={false}
                            showRole={false}
                          />
                          {user.isOnline && (
                            <div className="absolute -bottom-0.5 -right-0.5 w-3 h-3 bg-green-400 border-2 border-white rounded-full"></div>
                          )}
                        </div>
                        <div className="flex-1">
                          <h4 className="font-medium text-sm">{user.displayName}</h4>
                          <p className="text-xs text-gray-500">@{user.username}</p>
                        </div>
                      </div>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleSelectUser(user.id)}
                        className="p-1 h-auto"
                      >
                        {selectedUsers.has(user.id) ? (
                          <CheckSquare className="h-4 w-4 text-blue-600" />
                        ) : (
                          <Square className="h-4 w-4" />
                        )}
                      </Button>
                    </div>
                  </CardHeader>
                  <CardContent className="pt-0 space-y-3">
                    <div className="flex items-center justify-between">
                      <Badge className={getRoleColor(user.primaryRole)} variant="secondary">
                        {user.primaryRole}
                      </Badge>
                      <Badge className={getStatusColor(user.status)} variant="secondary">
                        {user.status}
                      </Badge>
                    </div>
                    
                    <div className="space-y-2 text-xs text-gray-600">
                      <div className="flex justify-between">
                        <span>Joined:</span>
                        <span>{formatDate(user.registrationDate)}</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Last Login:</span>
                        <span>{user.lastLogin ? formatDate(user.lastLogin) : 'Never'}</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Posts:</span>
                        <span>{user.postCount}</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Comments:</span>
                        <span>{user.commentCount}</span>
                      </div>
                    </div>

                    <div className="flex items-center justify-center space-x-1 pt-2">
                      {user.emailVerified && (
                        <CheckCircle className="h-4 w-4 text-green-500" />
                      )}
                      {user.twoFactorEnabled && (
                        <Shield className="h-4 w-4 text-blue-500" />
                      )}
                    </div>

                    <div className="flex justify-center space-x-1 pt-2">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleUserAction('edit', user)}
                        className="p-1 h-auto"
                        title="Edit User"
                      >
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleUserAction('toggleStatus', user)}
                        className="p-1 h-auto"
                        title={user.status === 'Active' ? 'Deactivate' : 'Activate'}
                      >
                        {user.status === 'Active' ? (
                          <UserX className="h-4 w-4" />
                        ) : (
                          <UserCheck className="h-4 w-4" />
                        )}
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleUserAction('resetPassword', user)}
                        className="p-1 h-auto"
                        title="Reset Password"
                      >
                        <Key className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleUserAction('delete', user)}
                        className="p-1 h-auto text-red-500"
                        title="Delete User"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              ))}

              {paginatedUsers.length === 0 && (
                <div className="col-span-full text-center py-12">
                  <Users className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                  <h3 className="text-lg font-medium text-gray-900 mb-2">No users found</h3>
                  <p className="text-gray-500">Try adjusting your filters or search terms</p>
                </div>
              )}
            </div>
          )}

          {/* Pagination */}
          {pagination.totalPages && pagination.totalPages > 1 && (
            <div className="flex items-center justify-between mt-6 pt-6 border-t">
              <div className="text-sm text-gray-700">
                Showing {(pagination.page - 1) * pagination.pageSize + 1} to{' '}
                {Math.min(pagination.page * pagination.pageSize, pagination.total || 0)} of{' '}
                {pagination.total || 0} users
              </div>
              <div className="flex items-center space-x-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPagination(prev => ({ ...prev, page: prev.page - 1 }))}
                  disabled={pagination.page === 1}
                >
                  Previous
                </Button>
                <div className="flex items-center space-x-1">
                  {Array.from({ length: Math.min(5, pagination.totalPages || 0) }, (_, i) => {
                    const pageNumber = Math.max(1, pagination.page - 2) + i;
                    if (pageNumber > (pagination.totalPages || 0)) return null;
                    
                    return (
                      <Button
                        key={pageNumber}
                        variant={pagination.page === pageNumber ? 'default' : 'ghost'}
                        size="sm"
                        onClick={() => setPagination(prev => ({ ...prev, page: pageNumber }))}
                        className="px-3"
                      >
                        {pageNumber}
                      </Button>
                    );
                  })}
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPagination(prev => ({ ...prev, page: prev.page + 1 }))}
                  disabled={pagination.page === (pagination.totalPages || 0)}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Create User Modal */}
      <Dialog open={showCreateModal} onOpenChange={setShowCreateModal}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Create New User</DialogTitle>
            <DialogDescription>
              Add a new user to the system with the specified details.
            </DialogDescription>
          </DialogHeader>
          <form id="create-user-form" className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <Input name="firstName" label="First Name" required />
              <Input name="lastName" label="Last Name" required />
            </div>
            <Input name="username" label="Username" required />
            <Input name="displayName" label="Display Name" required />
            <Input name="email" label="Email" type="email" required />
            <Input name="password" label="Password" type="password" required />
            <div>
              <label className="block text-sm font-medium mb-2">Role</label>
              <select name="primaryRole" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500">
                <option value="User">User</option>
                <option value="Author">Author</option>
                <option value="Admin">Admin</option>
              </select>
            </div>
            <div className="flex items-center space-x-2">
              <input type="checkbox" name="emailVerified" id="emailVerified" className="rounded" />
              <label htmlFor="emailVerified" className="text-sm">Email Verified</label>
            </div>
            <div className="flex items-center space-x-2">
              <input type="checkbox" name="sendWelcomeEmail" id="sendWelcomeEmail" className="rounded" />
              <label htmlFor="sendWelcomeEmail" className="text-sm">Send Welcome Email</label>
            </div>
          </form>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowCreateModal(false)}>
              Cancel
            </Button>
            <Button onClick={async () => {
              try {
                const formData = new FormData(document.querySelector('#create-user-form') as HTMLFormElement);
                
                const createRequest: CreateUserRequest = {
                  username: formData.get('username') as string,
                  email: formData.get('email') as string,
                  firstName: formData.get('firstName') as string,
                  lastName: formData.get('lastName') as string,
                  password: formData.get('password') as string,
                  primaryRole: formData.get('primaryRole') as 'Admin' | 'Author' | 'User',
                  emailVerified: formData.get('emailVerified') === 'on',
                  sendWelcomeEmail: formData.get('sendWelcomeEmail') === 'on',
                };

                // Validate required fields
                if (!createRequest.username || !createRequest.email || !createRequest.firstName || 
                    !createRequest.lastName || !createRequest.password) {
                  toastService.error('Please fill in all required fields');
                  return;
                }

                // Check email availability
                const emailCheck = await adminUserApi.checkEmailAvailability(createRequest.email);
                if (!emailCheck.available) {
                  toastService.error('Email address is already in use');
                  return;
                }

                // Check username availability
                const usernameCheck = await adminUserApi.checkUsernameAvailability(createRequest.username);
                if (!usernameCheck.available) {
                  toastService.error('Username is already taken');
                  return;
                }

                const newUser = await adminUserApi.createUser(createRequest);
                
                // Optimistic update
                setUsers(prev => [newUser, ...prev]);
                setShowCreateModal(false);

                toastService.success(`User "${newUser.displayName}" created successfully`, {
                  actions: [{
                    id: 'view',
                    label: 'View User',
                    variant: 'primary',
                  }],
                  onAction: (toast, actionId) => {
                    if (actionId === 'view') {
                      setEditingUser(newUser);
                      toastService.hide(toast.id);
                    }
                  }
                });

                // Reload to get fresh data
                await loadUsers(false);
                await loadStatistics();

              } catch (error) {
                console.error('Failed to create user:', error);
                toastService.error('Failed to create user');
              }
            }}>
              Create User
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Bulk Actions Modal */}
      <Dialog open={showBulkActionModal} onOpenChange={setShowBulkActionModal}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Bulk Actions</DialogTitle>
            <DialogDescription>
              Perform actions on {selectedUsers.size} selected users.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-2">Action</label>
              <select 
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={bulkOperation}
                onChange={(e) => setBulkOperation(e.target.value as BulkOperation)}
              >
                <option value="delete">Delete Users</option>
                <option value="activate">Activate Users</option>
                <option value="deactivate">Deactivate Users</option>
                <option value="suspend">Suspend Users</option>
                <option value="unsuspend">Unsuspend Users</option>
                <option value="verify-email">Verify Emails</option>
                <option value="reset-password">Reset Passwords</option>
              </select>
            </div>
            
            {bulkOperation === 'delete' && (
              <Alert>
                <AlertTriangle className="h-4 w-4" />
                <AlertDescription>
                  This action cannot be undone. Selected users will be permanently deleted.
                </AlertDescription>
              </Alert>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowBulkActionModal(false)}>
              Cancel
            </Button>
            <Button 
              variant={bulkOperation === 'delete' ? 'destructive' : 'default'}
              onClick={handleBulkAction}
            >
              Confirm Action
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit User Modal */}
      {editingUser && (
        <Dialog open={!!editingUser} onOpenChange={() => setEditingUser(null)}>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle>Edit User: {editingUser.displayName}</DialogTitle>
              <DialogDescription>
                Update user information and settings.
              </DialogDescription>
            </DialogHeader>
            
            <form id="edit-user-form">
              <Tabs defaultValue="basic" className="space-y-4">
                <TabsList>
                  <TabsTrigger value="basic">Basic Info</TabsTrigger>
                  <TabsTrigger value="security">Security</TabsTrigger>
                  <TabsTrigger value="activity">Activity</TabsTrigger>
                </TabsList>

                <TabsContent value="basic" className="space-y-4">
                  <div className="grid gap-4 md:grid-cols-2">
                    <Input name="firstName" label="First Name" defaultValue={editingUser.firstName} />
                    <Input name="lastName" label="Last Name" defaultValue={editingUser.lastName} />
                  </div>
                  <Input name="username" label="Username" defaultValue={editingUser.username} />
                  <Input name="email" label="Email" type="email" defaultValue={editingUser.email} />
                  <Input name="displayName" label="Display Name" defaultValue={editingUser.displayName} />
                  <div>
                    <label className="block text-sm font-medium mb-2">Primary Role</label>
                    <select
                      name="primaryRole"
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      defaultValue={editingUser.primaryRole}
                    >
                      <option value="User">User</option>
                      <option value="Author">Author</option>
                      <option value="Admin">Admin</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-2">Status</label>
                    <select
                      name="status"
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      defaultValue={editingUser.status}
                    >
                      <option value="Active">Active</option>
                      <option value="Inactive">Inactive</option>
                      <option value="Suspended">Suspended</option>
                      <option value="Banned">Banned</option>
                    </select>
                  </div>
                </TabsContent>
              
              <TabsContent value="security" className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="flex items-center space-x-2">
                    <input 
                      type="checkbox" 
                      id="emailVerified" 
                      className="rounded" 
                      defaultChecked={editingUser.emailVerified}
                    />
                    <label htmlFor="emailVerified" className="text-sm">Email Verified</label>
                  </div>
                  <div className="flex items-center space-x-2">
                    <input 
                      type="checkbox" 
                      id="twoFactorEnabled" 
                      className="rounded" 
                      defaultChecked={editingUser.twoFactorEnabled}
                    />
                    <label htmlFor="twoFactorEnabled" className="text-sm">Two-Factor Authentication</label>
                  </div>
                </div>
                <div className="space-y-2">
                  <label className="block text-sm font-medium">Failed Login Attempts</label>
                  <div className="text-sm text-gray-600">{editingUser.failedLoginAttempts}</div>
                </div>
                <div className="space-y-2">
                  <label className="block text-sm font-medium">Total Logins</label>
                  <div className="text-sm text-gray-600">{editingUser.loginCount}</div>
                </div>
                {editingUser.lockoutEnd && (
                  <div className="space-y-2">
                    <label className="block text-sm font-medium">Lockout Until</label>
                    <div className="text-sm text-gray-600">{formatDateTime(editingUser.lockoutEnd)}</div>
                  </div>
                )}
                <div className="space-y-2">
                  <label className="block text-sm font-medium">Last Known IP</label>
                  <div className="text-sm text-gray-600">{editingUser.ipAddress}</div>
                </div>
                <div className="space-y-2">
                  <label className="block text-sm font-medium">Location</label>
                  <div className="text-sm text-gray-600">{editingUser.location}</div>
                </div>
              </TabsContent>
              
              <TabsContent value="activity" className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <label className="block text-sm font-medium">Registration Date</label>
                    <div className="text-sm text-gray-600">{formatDateTime(editingUser.registrationDate)}</div>
                  </div>
                  <div className="space-y-2">
                    <label className="block text-sm font-medium">Last Login</label>
                    <div className="text-sm text-gray-600">
                      {editingUser.lastLogin ? formatDateTime(editingUser.lastLogin) : 'Never'}
                    </div>
                  </div>
                  <div className="space-y-2">
                    <label className="block text-sm font-medium">Last Activity</label>
                    <div className="text-sm text-gray-600">{formatDateTime(editingUser.lastActivity)}</div>
                  </div>
                  <div className="space-y-2">
                    <label className="block text-sm font-medium">Account Age</label>
                    <div className="text-sm text-gray-600">{editingUser.accountAge}</div>
                  </div>
                  <div className="space-y-2">
                    <label className="block text-sm font-medium">Posts Created</label>
                    <div className="text-sm text-gray-600">{editingUser.postCount}</div>
                  </div>
                  <div className="space-y-2">
                    <label className="block text-sm font-medium">Comments Made</label>
                    <div className="text-sm text-gray-600">{editingUser.commentCount}</div>
                  </div>
                  <div className="space-y-2">
                    <label className="block text-sm font-medium">Profile Completion</label>
                    <div className="text-sm text-gray-600">{editingUser.profileCompletion}%</div>
                  </div>
                  <div className="space-y-2">
                    <label className="block text-sm font-medium">Online Status</label>
                    <div className="text-sm text-gray-600">
                      {editingUser.isOnline ? 'Online' : 'Offline'}
                    </div>
                  </div>
                </div>
                <div className="space-y-2">
                  <label className="block text-sm font-medium">Admin Notes</label>
                  <textarea 
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                    rows={3}
                    defaultValue={editingUser.notes || ''}
                    placeholder="Add notes about this user..."
                  />
                </div>
              </TabsContent>
            </Tabs>
            </form>

            <DialogFooter>
              <Button variant="outline" onClick={() => setEditingUser(null)}>
                Cancel
              </Button>
              <Button onClick={async () => {
                try {
                  if (!editingUser) return;

                  const formData = new FormData(document.querySelector('#edit-user-form') as HTMLFormElement);
                  
                  const updateRequest: UpdateUserRequest = {
                    firstName: formData.get('firstName') as string,
                    lastName: formData.get('lastName') as string,
                    email: formData.get('email') as string,
                    displayName: formData.get('displayName') as string,
                    primaryRole: formData.get('primaryRole') as 'Admin' | 'Author' | 'User',
                    status: formData.get('status') as 'Active' | 'Inactive' | 'Suspended' | 'Banned',
                    emailVerified: (document.getElementById('emailVerified') as HTMLInputElement)?.checked,
                    twoFactorEnabled: (document.getElementById('twoFactorEnabled') as HTMLInputElement)?.checked,
                    notes: formData.get('notes') as string,
                  };

                  // Check email availability if email changed
                  if (updateRequest.email && updateRequest.email !== editingUser.email) {
                    const emailCheck = await adminUserApi.checkEmailAvailability(
                      updateRequest.email, 
                      editingUser.id
                    );
                    if (!emailCheck.available) {
                      toastService.error('Email address is already in use');
                      return;
                    }
                  }

                  const updatedUser = await adminUserApi.updateUser(editingUser.id, updateRequest);
                  
                  // Optimistic update
                  setUsers(prev => prev.map(user => 
                    user.id === editingUser.id ? updatedUser : user
                  ));
                  
                  setEditingUser(null);
                  toastService.success(`User "${updatedUser.displayName}" updated successfully`);

                  // Reload to get fresh data
                  await loadUsers(false);

                } catch (error) {
                  console.error('Failed to update user:', error);
                  toastService.error('Failed to update user');
                }
              }}>
                Save Changes
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}

      {/* Confirmation Dialog */}
      <ConfirmationDialog
        open={confirmation.open}
        onOpenChange={(open) => setConfirmation({ open })}
        title={confirmation.title || '确认操作'}
        description={confirmation.message || '确定要执行此操作吗？'}
        severity={confirmation.type === 'deleteUser' ? 'danger' : 'warning'}
        confirmAction={{
          label: confirmation.type === 'deleteUser' ? '删除' : '确认',
          variant: confirmation.type === 'deleteUser' ? 'destructive' : 'default',
          onClick: confirmation.onConfirm || (() => { /* noop */ })
        }}
        cancelAction={{
          label: '取消',
          onClick: confirmation.onCancel || (() => { /* noop */ })
        }}
      />

      {/* Bulk Action Confirmation Dialog */}
      <BulkActionConfirmation
        open={bulkConfirmation.open}
        onOpenChange={(open) => setBulkConfirmation({ open })}
        action={bulkConfirmation.action || 'delete'}
        itemCount={bulkConfirmation.itemCount || 0}
        itemType={bulkConfirmation.itemType || 'items'}
        severity={bulkConfirmation.severity || 'warning'}
        onConfirm={bulkConfirmation.onConfirm || (() => { /* noop */ })}
        onCancel={bulkConfirmation.onCancel || (() => { /* noop */ })}
      />

      {/* User Details Modal */}
      {userDetailsModal.user && (
        <Dialog open={userDetailsModal.open} onOpenChange={(open) => setUserDetailsModal({ open })}>
          <DialogContent className="max-w-4xl">
            <DialogHeader>
              <DialogTitle className="flex items-center gap-3">
                <div className="relative">
                  <UserAvatar
                    user={{
                      id: userDetailsModal.user.id,
                      displayName: userDetailsModal.user.displayName,
                      username: userDetailsModal.user.username,
                      avatarUrl: userDetailsModal.user.avatar,
                      role: userDetailsModal.user.primaryRole,
                      isVip: false,
                    }}
                    size="lg"
                    showStatus={false}
                    showRole={false}
                  />
                  {userDetailsModal.user.isOnline && (
                    <div className="absolute -bottom-1 -right-1 w-4 h-4 bg-green-400 border-2 border-white rounded-full"></div>
                  )}
                </div>
                <div>
                  <h2 className="text-xl font-semibold">{userDetailsModal.user.displayName}</h2>
                  <p className="text-sm text-gray-500">@{userDetailsModal.user.username}</p>
                </div>
              </DialogTitle>
              <DialogDescription>
                完整的用户信息和活动记录
              </DialogDescription>
            </DialogHeader>

            <Tabs defaultValue="overview" className="space-y-4">
              <TabsList className="grid w-full grid-cols-4">
                <TabsTrigger value="overview">概览</TabsTrigger>
                <TabsTrigger value="activity">活动</TabsTrigger>
                <TabsTrigger value="security">安全</TabsTrigger>
                <TabsTrigger value="content">内容</TabsTrigger>
              </TabsList>

              <TabsContent value="overview" className="space-y-6">
                <div className="grid gap-6 md:grid-cols-2">
                  <Card>
                    <CardHeader>
                      <CardTitle className="text-base">基本信息</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-gray-500">邮箱</span>
                        <div className="flex items-center gap-2">
                          <span className="text-sm">{userDetailsModal.user.email}</span>
                          {userDetailsModal.user.emailVerified && (
                            <CheckCircle className="h-4 w-4 text-green-500" />
                          )}
                        </div>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-gray-500">状态</span>
                        <Badge className={getStatusColor(userDetailsModal.user.status)}>
                          {userDetailsModal.user.status}
                        </Badge>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-gray-500">角色</span>
                        <Badge className={getRoleColor(userDetailsModal.user.primaryRole)}>
                          {userDetailsModal.user.primaryRole}
                        </Badge>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-gray-500">注册时间</span>
                        <span className="text-sm">{formatDateTime(userDetailsModal.user.registrationDate)}</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-gray-500">账户年龄</span>
                        <span className="text-sm">{userDetailsModal.user.accountAge}</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-gray-500">资料完整度</span>
                        <span className="text-sm">{userDetailsModal.user.profileCompletion}%</span>
                      </div>
                    </CardContent>
                  </Card>

                  <Card>
                    <CardHeader>
                      <CardTitle className="text-base">统计信息</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-gray-500">文章数量</span>
                        <span className="text-sm font-bold">{userDetailsModal.user.postCount}</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-gray-500">评论数量</span>
                        <span className="text-sm font-bold">{userDetailsModal.user.commentCount}</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-gray-500">总登录次数</span>
                        <span className="text-sm font-bold">{userDetailsModal.user.loginCount}</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-gray-500">最后登录</span>
                        <span className="text-sm">
                          {userDetailsModal.user.lastLogin ? formatDateTime(userDetailsModal.user.lastLogin) : '从未'}
                        </span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-gray-500">最后活动</span>
                        <span className="text-sm">{formatDateTime(userDetailsModal.user.lastActivity)}</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium text-gray-500">在线状态</span>
                        <div className="flex items-center gap-2">
                          <span className={`inline-block w-2 h-2 rounded-full ${userDetailsModal.user.isOnline ? 'bg-green-400' : 'bg-gray-400'}`}></span>
                          <span className="text-sm">{userDetailsModal.user.isOnline ? '在线' : '离线'}</span>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                </div>

                {userDetailsModal.user.notes && (
                  <Card>
                    <CardHeader>
                      <CardTitle className="text-base">管理员备注</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <p className="text-sm text-gray-700">{userDetailsModal.user.notes}</p>
                    </CardContent>
                  </Card>
                )}
              </TabsContent>

              <TabsContent value="activity" className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <Card>
                    <CardHeader>
                      <CardTitle className="text-base">登录记录</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-3">
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium">总登录次数</span>
                        <span className="text-lg font-bold text-blue-600">{userDetailsModal.user.loginCount}</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium">失败登录尝试</span>
                        <span className={`text-lg font-bold ${userDetailsModal.user.failedLoginAttempts > 0 ? 'text-red-600' : 'text-green-600'}`}>
                          {userDetailsModal.user.failedLoginAttempts}
                        </span>
                      </div>
                      {userDetailsModal.user.lockoutEnd && (
                        <div className="flex items-center justify-between">
                          <span className="text-sm font-medium">锁定至</span>
                          <span className="text-sm text-red-600">{formatDateTime(userDetailsModal.user.lockoutEnd)}</span>
                        </div>
                      )}
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium">最后登录IP</span>
                        <span className="text-sm font-mono">{userDetailsModal.user.ipAddress}</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium">位置</span>
                        <span className="text-sm">{userDetailsModal.user.location}</span>
                      </div>
                    </CardContent>
                  </Card>

                  <Card>
                    <CardHeader>
                      <CardTitle className="text-base">内容活动</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-3">
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium">发布的文章</span>
                        <span className="text-lg font-bold text-purple-600">{userDetailsModal.user.postCount}</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium">发表的评论</span>
                        <span className="text-lg font-bold text-orange-600">{userDetailsModal.user.commentCount}</span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium">创建者</span>
                        <span className="text-sm">{userDetailsModal.user.createdBy || '系统'}</span>
                      </div>
                    </CardContent>
                  </Card>
                </div>
              </TabsContent>

              <TabsContent value="security" className="space-y-4">
                <Card>
                  <CardHeader>
                    <CardTitle className="text-base">安全设置</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium">邮箱验证</span>
                      <div className="flex items-center gap-2">
                        {userDetailsModal.user.emailVerified ? (
                          <>
                            <CheckCircle className="h-4 w-4 text-green-500" />
                            <span className="text-sm text-green-600">已验证</span>
                          </>
                        ) : (
                          <>
                            <AlertTriangle className="h-4 w-4 text-yellow-500" />
                            <span className="text-sm text-yellow-600">未验证</span>
                          </>
                        )}
                      </div>
                    </div>
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium">双因素认证</span>
                      <div className="flex items-center gap-2">
                        {userDetailsModal.user.twoFactorEnabled ? (
                          <>
                            <Shield className="h-4 w-4 text-green-500" />
                            <span className="text-sm text-green-600">已启用</span>
                          </>
                        ) : (
                          <>
                            <Shield className="h-4 w-4 text-gray-400" />
                            <span className="text-sm text-gray-600">未启用</span>
                          </>
                        )}
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </TabsContent>

              <TabsContent value="content" className="space-y-4">
                <div className="text-center py-8">
                  <p className="text-gray-500">内容管理功能将在后续版本中提供</p>
                </div>
              </TabsContent>
            </Tabs>

            <DialogFooter>
              <Button onClick={() => setUserDetailsModal({ open: false })}>
                关闭
              </Button>
              <Button
                onClick={() => userDetailsModal.user && handleUserAction('edit', userDetailsModal.user)}
                disabled={!userDetailsModal.user}
              >
                编辑用户
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
};

export default UserManagement;
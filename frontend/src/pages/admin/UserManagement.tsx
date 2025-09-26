import React, { useState, useEffect, useMemo } from 'react';
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

// Extended User interface for admin management
interface AdminUser {
  id: string;
  username: string;
  email: string;
  displayName: string;
  firstName: string;
  lastName: string;
  avatar?: string;
  status: 'Active' | 'Inactive' | 'Suspended' | 'Banned';
  roles: string[];
  primaryRole: 'Admin' | 'Author' | 'User';
  lastLogin?: string;
  registrationDate: string;
  postCount: number;
  commentCount: number;
  emailVerified: boolean;
  twoFactorEnabled: boolean;
  isOnline: boolean;
  lastActivity: string;
  profileCompletion: number;
  accountAge: string;
  loginCount: number;
  failedLoginAttempts: number;
  lockoutEnd?: string;
  ipAddress?: string;
  location?: string;
  createdBy?: string;
  notes?: string;
}

// Filter interface
interface UserFilters {
  search: string;
  role: string;
  status: string;
  emailVerified: boolean | null;
  twoFactorEnabled: boolean | null;
  isOnline: boolean | null;
  registrationDateFrom: string;
  registrationDateTo: string;
  lastLoginFrom: string;
  lastLoginTo: string;
}

// Sort configuration
interface SortConfig {
  key: keyof AdminUser | null;
  direction: 'asc' | 'desc';
}

// Pagination configuration
interface PaginationConfig {
  page: number;
  itemsPerPage: number;
  totalItems: number;
  totalPages: number;
}

// Bulk operation types
type BulkOperation = 'delete' | 'activate' | 'deactivate' | 'suspend' | 'unsuspend' | 'verify-email' | 'reset-password';

// Mock data for demonstration
const generateMockUsers = (): AdminUser[] => {
  const statuses: AdminUser['status'][] = ['Active', 'Inactive', 'Suspended', 'Banned'];
  const roles: AdminUser['primaryRole'][] = ['Admin', 'Author', 'User'];
  const locations = ['New York, US', 'London, UK', 'Tokyo, JP', 'Berlin, DE', 'Sydney, AU', 'Toronto, CA'];
  
  return Array.from({ length: 127 }, (_, index) => {
    const firstName = `User${index + 1}`;
    const lastName = `Last${index + 1}`;
    const username = `user${index + 1}`;
    const status = statuses[Math.floor(Math.random() * statuses.length)];
    const role = roles[Math.floor(Math.random() * roles.length)];
    const registrationDate = new Date(2020 + Math.floor(Math.random() * 4), Math.floor(Math.random() * 12), Math.floor(Math.random() * 28));
    const lastLogin = Math.random() > 0.3 ? new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000) : undefined;
    const isOnline = Math.random() > 0.7;
    
    return {
      id: `user-${index + 1}`,
      username,
      email: `${username}@example.com`,
      displayName: `${firstName} ${lastName}`,
      firstName,
      lastName,
      avatar: Math.random() > 0.5 ? `https://i.pravatar.cc/100?img=${index + 1}` : undefined,
      status,
      roles: role === 'Admin' ? ['Admin', 'Author', 'User'] : role === 'Author' ? ['Author', 'User'] : ['User'],
      primaryRole: role,
      lastLogin: lastLogin?.toISOString(),
      registrationDate: registrationDate.toISOString(),
      postCount: Math.floor(Math.random() * 50),
      commentCount: Math.floor(Math.random() * 200),
      emailVerified: Math.random() > 0.2,
      twoFactorEnabled: Math.random() > 0.7,
      isOnline,
      lastActivity: new Date(Date.now() - Math.random() * 7 * 24 * 60 * 60 * 1000).toISOString(),
      profileCompletion: Math.floor(Math.random() * 40) + 60,
      accountAge: `${Math.floor((Date.now() - registrationDate.getTime()) / (1000 * 60 * 60 * 24))} days`,
      loginCount: Math.floor(Math.random() * 1000) + 50,
      failedLoginAttempts: Math.floor(Math.random() * 5),
      lockoutEnd: status === 'Suspended' && Math.random() > 0.5 ? new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString() : undefined,
      ipAddress: `${Math.floor(Math.random() * 255)}.${Math.floor(Math.random() * 255)}.${Math.floor(Math.random() * 255)}.${Math.floor(Math.random() * 255)}`,
      location: locations[Math.floor(Math.random() * locations.length)],
      createdBy: index < 10 ? 'System' : 'admin',
      notes: Math.random() > 0.8 ? 'Test user account created for demonstration' : undefined,
    };
  });
};

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

  // Filters state
  const [filters, setFilters] = useState<UserFilters>({
    search: '',
    role: '',
    status: '',
    emailVerified: null,
    twoFactorEnabled: null,
    isOnline: null,
    registrationDateFrom: '',
    registrationDateTo: '',
    lastLoginFrom: '',
    lastLoginTo: '',
  });

  // Sort and pagination state
  const [sortConfig, setSortConfig] = useState<SortConfig>({ key: 'registrationDate', direction: 'desc' });
  const [pagination, setPagination] = useState<PaginationConfig>({
    page: 1,
    itemsPerPage: 20,
    totalItems: 0,
    totalPages: 0,
  });

  // Load users on component mount
  useEffect(() => {
    const loadUsers = async () => {
      setLoading(true);
      try {
        // Simulate API delay
        await new Promise(resolve => setTimeout(resolve, 1000));
        const mockUsers = generateMockUsers();
        setUsers(mockUsers);
        setPagination(prev => ({
          ...prev,
          totalItems: mockUsers.length,
          totalPages: Math.ceil(mockUsers.length / prev.itemsPerPage),
        }));
      } catch (error) {
        // TODO: Implement proper error handling with toast notification
      } finally {
        setLoading(false);
      }
    };

    loadUsers();
  }, []);

  // Filter and sort users
  const filteredAndSortedUsers = useMemo(() => {
    let result = [...users];

    // Apply filters
    if (filters.search) {
      const searchLower = filters.search.toLowerCase();
      result = result.filter(user => 
        user.username.toLowerCase().includes(searchLower) ||
        user.email.toLowerCase().includes(searchLower) ||
        user.displayName.toLowerCase().includes(searchLower)
      );
    }

    if (filters.role) {
      result = result.filter(user => user.primaryRole === filters.role);
    }

    if (filters.status) {
      result = result.filter(user => user.status === filters.status);
    }

    if (filters.emailVerified !== null) {
      result = result.filter(user => user.emailVerified === filters.emailVerified);
    }

    if (filters.twoFactorEnabled !== null) {
      result = result.filter(user => user.twoFactorEnabled === filters.twoFactorEnabled);
    }

    if (filters.isOnline !== null) {
      result = result.filter(user => user.isOnline === filters.isOnline);
    }

    if (filters.registrationDateFrom) {
      const fromDate = new Date(filters.registrationDateFrom);
      result = result.filter(user => new Date(user.registrationDate) >= fromDate);
    }

    if (filters.registrationDateTo) {
      const toDate = new Date(filters.registrationDateTo);
      result = result.filter(user => new Date(user.registrationDate) <= toDate);
    }

    // Apply sorting
    if (sortConfig.key) {
      result.sort((a, b) => {
        const aValue = a[sortConfig.key!];
        const bValue = b[sortConfig.key!];

        if (aValue === null || aValue === undefined) return 1;
        if (bValue === null || bValue === undefined) return -1;

        if (typeof aValue === 'string' && typeof bValue === 'string') {
          const comparison = aValue.localeCompare(bValue);
          return sortConfig.direction === 'asc' ? comparison : -comparison;
        }

        if (typeof aValue === 'number' && typeof bValue === 'number') {
          const comparison = aValue - bValue;
          return sortConfig.direction === 'asc' ? comparison : -comparison;
        }

        // Date comparison
        if (typeof aValue === 'string' && typeof bValue === 'string' && 
            (sortConfig.key === 'registrationDate' || sortConfig.key === 'lastLogin' || sortConfig.key === 'lastActivity')) {
          const aDate = new Date(aValue).getTime();
          const bDate = new Date(bValue).getTime();
          const comparison = aDate - bDate;
          return sortConfig.direction === 'asc' ? comparison : -comparison;
        }

        return 0;
      });
    }

    return result;
  }, [users, filters, sortConfig]);

  // Paginated users
  const paginatedUsers = useMemo(() => {
    const startIndex = (pagination.page - 1) * pagination.itemsPerPage;
    const endIndex = startIndex + pagination.itemsPerPage;
    return filteredAndSortedUsers.slice(startIndex, endIndex);
  }, [filteredAndSortedUsers, pagination.page, pagination.itemsPerPage]);

  // Update pagination when filters change
  useEffect(() => {
    setPagination(prev => ({
      ...prev,
      page: 1,
      totalItems: filteredAndSortedUsers.length,
      totalPages: Math.ceil(filteredAndSortedUsers.length / prev.itemsPerPage),
    }));
  }, [filteredAndSortedUsers.length, pagination.itemsPerPage]);

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
  const handleSort = (key: keyof AdminUser) => {
    setSortConfig(prev => ({
      key,
      direction: prev.key === key && prev.direction === 'asc' ? 'desc' : 'asc',
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
    if (selectedUsers.size === paginatedUsers.length) {
      setSelectedUsers(new Set());
    } else {
      setSelectedUsers(new Set(paginatedUsers.map(user => user.id)));
    }
  };

  const handleUserAction = async (action: string, user: AdminUser) => {
    // Here you would make API calls to perform the action
    
    switch (action) {
      case 'edit':
        setEditingUser(user);
        break;
      case 'delete':
        // TODO: Implement proper confirmation dialog
        setUsers(prev => prev.filter(u => u.id !== user.id));
        break;
      case 'toggleStatus': {
        const newStatus = user.status === 'Active' ? 'Inactive' : 'Active';
        setUsers(prev => prev.map(u =>
          u.id === user.id ? { ...u, status: newStatus } : u
        ));
        break;
      }
      case 'resetPassword':
        // TODO: Show toast notification instead of alert
        break;
      case 'suspend':
        setUsers(prev => prev.map(u => 
          u.id === user.id ? { ...u, status: 'Suspended' as AdminUser['status'] } : u
        ));
        break;
      case 'unsuspend':
        setUsers(prev => prev.map(u => 
          u.id === user.id ? { ...u, status: 'Active' as AdminUser['status'] } : u
        ));
        break;
      default:
        break;
    }
  };

  const handleBulkAction = async () => {
    if (selectedUsers.size === 0) return;
    // Here you would make API calls to perform bulk actions
    switch (bulkOperation) {
      case 'delete':
        // TODO: Implement proper confirmation dialog
        setUsers(prev => prev.filter(user => !selectedUsers.has(user.id)));
        setSelectedUsers(new Set());
        break;
      case 'activate':
        setUsers(prev => prev.map(user => 
          selectedUsers.has(user.id) ? { ...user, status: 'Active' as AdminUser['status'] } : user
        ));
        setSelectedUsers(new Set());
        break;
      case 'deactivate':
        setUsers(prev => prev.map(user => 
          selectedUsers.has(user.id) ? { ...user, status: 'Inactive' as AdminUser['status'] } : user
        ));
        setSelectedUsers(new Set());
        break;
      case 'suspend':
        setUsers(prev => prev.map(user => 
          selectedUsers.has(user.id) ? { ...user, status: 'Suspended' as AdminUser['status'] } : user
        ));
        setSelectedUsers(new Set());
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
      emailVerified: null,
      twoFactorEnabled: null,
      isOnline: null,
      registrationDateFrom: '',
      registrationDateTo: '',
      lastLoginFrom: '',
      lastLoginTo: '',
    });
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-600">Loading users...</p>
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
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Users</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{users.length}</div>
            <p className="text-xs text-muted-foreground">
              {users.filter(u => u.status === 'Active').length} active
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Online Now</CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {users.filter(u => u.isOnline).length}
            </div>
            <p className="text-xs text-muted-foreground">
              {Math.round((users.filter(u => u.isOnline).length / users.length) * 100)}% of total
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">New This Month</CardTitle>
            <Calendar className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {users.filter(u => {
                const regDate = new Date(u.registrationDate);
                const now = new Date();
                const thirtyDaysAgo = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
                return regDate >= thirtyDaysAgo;
              }).length}
            </div>
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
              {Math.round((users.filter(u => u.emailVerified).length / users.length) * 100)}%
            </div>
            <p className="text-xs text-muted-foreground">
              Email verified
            </p>
          </CardContent>
        </Card>
      </div>

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
                  value={filters.emailVerified === null ? '' : filters.emailVerified.toString()}
                  onChange={(e) => setFilters(prev => ({ 
                    ...prev, 
                    emailVerified: e.target.value === '' ? null : e.target.value === 'true' 
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
                  value={filters.isOnline === null ? '' : filters.isOnline.toString()}
                  onChange={(e) => setFilters(prev => ({ 
                    ...prev, 
                    isOnline: e.target.value === '' ? null : e.target.value === 'true' 
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
                        {sortConfig.key === 'username' && (
                          <span className="ml-1 text-xs">
                            {sortConfig.direction === 'asc' ? '↑' : '↓'}
                          </span>
                        )}
                      </div>
                    </th>
                    <th className="text-left py-2 px-4 cursor-pointer hover:bg-gray-50" onClick={() => handleSort('primaryRole')}>
                      <div className="flex items-center">
                        Role
                        {sortConfig.key === 'primaryRole' && (
                          <span className="ml-1 text-xs">
                            {sortConfig.direction === 'asc' ? '↑' : '↓'}
                          </span>
                        )}
                      </div>
                    </th>
                    <th className="text-left py-2 px-4 cursor-pointer hover:bg-gray-50" onClick={() => handleSort('status')}>
                      <div className="flex items-center">
                        Status
                        {sortConfig.key === 'status' && (
                          <span className="ml-1 text-xs">
                            {sortConfig.direction === 'asc' ? '↑' : '↓'}
                          </span>
                        )}
                      </div>
                    </th>
                    <th className="text-left py-2 px-4 cursor-pointer hover:bg-gray-50" onClick={() => handleSort('registrationDate')}>
                      <div className="flex items-center">
                        Joined
                        {sortConfig.key === 'registrationDate' && (
                          <span className="ml-1 text-xs">
                            {sortConfig.direction === 'asc' ? '↑' : '↓'}
                          </span>
                        )}
                      </div>
                    </th>
                    <th className="text-left py-2 px-4 cursor-pointer hover:bg-gray-50" onClick={() => handleSort('lastLogin')}>
                      <div className="flex items-center">
                        Last Login
                        {sortConfig.key === 'lastLogin' && (
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
                                  onClick={() => {/* TODO: Implement user details view */}}
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
          {pagination.totalPages > 1 && (
            <div className="flex items-center justify-between mt-6 pt-6 border-t">
              <div className="text-sm text-gray-700">
                Showing {(pagination.page - 1) * pagination.itemsPerPage + 1} to{' '}
                {Math.min(pagination.page * pagination.itemsPerPage, pagination.totalItems)} of{' '}
                {pagination.totalItems} users
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
                  {Array.from({ length: Math.min(5, pagination.totalPages) }, (_, i) => {
                    const pageNumber = Math.max(1, pagination.page - 2) + i;
                    if (pageNumber > pagination.totalPages) return null;
                    
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
                  disabled={pagination.page === pagination.totalPages}
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
          <div className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <Input label="First Name" required />
              <Input label="Last Name" required />
            </div>
            <Input label="Username" required />
            <Input label="Email" type="email" required />
            <Input label="Password" type="password" required />
            <div>
              <label className="block text-sm font-medium mb-2">Role</label>
              <select className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500">
                <option value="User">User</option>
                <option value="Author">Author</option>
                <option value="Admin">Admin</option>
              </select>
            </div>
            <div className="flex items-center space-x-2">
              <input type="checkbox" id="emailVerified" className="rounded" />
              <label htmlFor="emailVerified" className="text-sm">Email Verified</label>
            </div>
            <div className="flex items-center space-x-2">
              <input type="checkbox" id="sendWelcomeEmail" className="rounded" />
              <label htmlFor="sendWelcomeEmail" className="text-sm">Send Welcome Email</label>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowCreateModal(false)}>
              Cancel
            </Button>
            <Button onClick={() => {
              // TODO: Implement user creation functionality
              setShowCreateModal(false);
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
            
            <Tabs defaultValue="basic" className="space-y-4">
              <TabsList>
                <TabsTrigger value="basic">Basic Info</TabsTrigger>
                <TabsTrigger value="security">Security</TabsTrigger>
                <TabsTrigger value="activity">Activity</TabsTrigger>
              </TabsList>
              
              <TabsContent value="basic" className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <Input label="First Name" defaultValue={editingUser.firstName} />
                  <Input label="Last Name" defaultValue={editingUser.lastName} />
                </div>
                <Input label="Username" defaultValue={editingUser.username} />
                <Input label="Email" type="email" defaultValue={editingUser.email} />
                <Input label="Display Name" defaultValue={editingUser.displayName} />
                <div>
                  <label className="block text-sm font-medium mb-2">Primary Role</label>
                  <select 
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
            
            <DialogFooter>
              <Button variant="outline" onClick={() => setEditingUser(null)}>
                Cancel
              </Button>
              <Button onClick={() => {
                // TODO: Implement user update functionality
                setEditingUser(null);
              }}>
                Save Changes
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
};

export default UserManagement;
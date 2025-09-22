// @ts-nocheck
import React, { useEffect, useState, useMemo, useCallback } from 'react';
import {
  Card,
  Typography,
  Button,
  Space,
  Table,
  Input,
  Select,
  Tag,
  Avatar,
  Dropdown,
  Modal,
  Progress,
  Tooltip,
  DatePicker,
  Statistic,
  Row,
  Col,
  message,
  Drawer,
  Badge,
  Divider,
  Switch,
  Alert,
  Popconfirm,
} from 'antd';
import {
  PlusOutlined,
  SearchOutlined,
  FilterOutlined,
  ExportOutlined,
  ImportOutlined,
  MoreOutlined,
  UserOutlined,
  EditOutlined,
  DeleteOutlined,
  SettingOutlined,
  SafetyOutlined,
  MailOutlined,
  PhoneOutlined,
  CalendarOutlined,
  EyeOutlined,
  UserSwitchOutlined,
  TeamOutlined,
  ExclamationCircleOutlined,
  ReloadOutlined,
  DownloadOutlined,
  UploadOutlined,
  BlockOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  ClockCircleOutlined,
  WarningOutlined,
} from '@ant-design/icons';
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table';
import type { DropdownProps } from 'antd/es/dropdown';
import type { RangePickerProps } from 'antd/es/date-picker';
import dayjs from 'dayjs';
import {
  useUserManagementStore,
  useUsers,
  useSelectedUsers,
  usePagination,
  useQuery,
  useUserManagementLoading,
  useUserStats,
  useBulkOperationProgress,
  useHasSelectedUsers,
} from '@/stores/userManagementStore';
import type { User, UserStatus, Role } from '@/types';
import UserForm from '@/components/users/UserForm';
import RoleSelector from '@/components/users/RoleSelector';
import UserDetail from './UserDetail';

const { Title, Text } = Typography;
const { Option } = Select;
const { RangePicker } = DatePicker;
const { confirm } = Modal;

const UserManagement: React.FC = () => {
  const [searchValue, setSearchValue] = useState('');
  const [selectedStatus, setSelectedStatus] = useState<UserStatus | undefined>();
  const [selectedRole, setSelectedRole] = useState<string | undefined>();
  const [dateRange, setDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs] | null>(null);
  const [showFilters, setShowFilters] = useState(false);
  const [exportLoading, setExportLoading] = useState(false);
  const [importLoading, setImportLoading] = useState(false);

  // Store hooks
  const users = useUsers();
  const selectedUsers = useSelectedUsers();
  const pagination = usePagination();
  const query = useQuery();
  const loading = useUserManagementLoading();
  const userStats = useUserStats();
  const bulkProgress = useBulkOperationProgress();
  const hasSelectedUsers = useHasSelectedUsers();

  const {
    setUsers,
    setSelectedUsers,
    toggleUserSelection,
    selectAllUsers,
    clearSelection,
    changePage,
    updateFilter,
    clearFilters,
    setLoading,
    setError,
    openUserForm,
    openUserDetails,
    openRoleAssignment,
    updateUser,
    removeUser,
    removeUsers,
    startBulkOperation,
    updateBulkProgress,
    completeBulkOperation,
    setActionLoading,
  } = useUserManagementStore();

  // API service for users
  const userService = {
    async getUsers(params: any = {}) {
      const queryString = new URLSearchParams(params).toString();
      const response = await fetch(`/api/users?${queryString}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('authToken')}`
        }
      });

      if (!response.ok) {
        throw new Error('Failed to fetch users');
      }

      return response.json();
    }
  };

  // Load users on component mount
  useEffect(() => {
    loadUsers();
  }, [query]);

  const loadUsers = useCallback(async () => {
    try {
      setLoading(true);
      const result = await userService.getUsers(query);
      setUsers(result);
    } catch (error) {
      setError('加载用户列表失败');
      message.error('加载用户列表失败');
      // Set empty response on error
      setUsers({
        success: false,
        data: [],
        pagination: {
          current: 1,
          pageSize: 20,
          total: 0,
          totalPages: 0,
        },
      });
    } finally {
      setLoading(false);
    }
  }, [query, setUsers, setLoading, setError]);

  // Handle search
  const handleSearch = useCallback((value: string) => {
    setSearchValue(value);
    updateFilter('search', value);
  }, [updateFilter]);

  // Handle status filter
  const handleStatusFilter = useCallback((status: UserStatus | undefined) => {
    setSelectedStatus(status);
    updateFilter('status', status);
  }, [updateFilter]);

  // Handle role filter
  const handleRoleFilter = useCallback((roleId: string | undefined) => {
    setSelectedRole(roleId);
    updateFilter('roleId', roleId);
  }, [updateFilter]);

  // Handle date range filter
  const handleDateRangeFilter: RangePickerProps['onChange'] = useCallback((dates) => {
    setDateRange(dates);
    if (dates) {
      updateFilter('dateRange', [dates[0]?.toISOString(), dates[1]?.toISOString()]);
    } else {
      updateFilter('dateRange', undefined);
    }
  }, [updateFilter]);

  // Clear all filters
  const handleClearFilters = useCallback(() => {
    setSearchValue('');
    setSelectedStatus(undefined);
    setSelectedRole(undefined);
    setDateRange(null);
    clearFilters();
  }, [clearFilters]);

  // Handle table pagination change
  const handleTableChange = useCallback((
    paginationConfig: TablePaginationConfig,
  ) => {
    changePage(paginationConfig.current || 1, paginationConfig.pageSize);
  }, [changePage]);

  // Handle row selection
  const rowSelection = {
    selectedRowKeys: selectedUsers,
    onChange: setSelectedUsers,
    onSelect: (record: User, selected: boolean) => {
      toggleUserSelection(record.id);
    },
    onSelectAll: (selected: boolean, selectedRows: User[], changeRows: User[]) => {
      if (selected) {
        selectAllUsers();
      } else {
        clearSelection();
      }
    },
  };

  // Get status display
  const getStatusDisplay = (status: UserStatus) => {
    const statusConfig = {
      active: { color: 'success', icon: <CheckCircleOutlined />, text: '活跃' },
      inactive: { color: 'default', icon: <CloseCircleOutlined />, text: '非活跃' },
      banned: { color: 'error', icon: <BlockOutlined />, text: '已封禁' },
      pending: { color: 'warning', icon: <ClockCircleOutlined />, text: '待激活' },
    };

    const config = statusConfig[status];
    return (
      <Tag color={config.color} icon={config.icon}>
        {config.text}
      </Tag>
    );
  };

  // Handle user actions
  const handleViewUser = useCallback((user: User) => {
    openUserDetails(user);
  }, [openUserDetails]);

  const handleEditUser = useCallback((user: User) => {
    openUserForm('edit', user);
  }, [openUserForm]);

  const handleDeleteUser = useCallback((user: User) => {
    confirm({
      title: '确认删除用户',
      content: `确定要删除用户 "${user.displayName || user.username}" 吗？此操作不可恢复。`,
      icon: <ExclamationCircleOutlined />,
      okText: '确认删除',
      okType: 'danger',
      cancelText: '取消',
      onOk: async () => {
        try {
          setActionLoading(`delete-${user.id}`, true);
          // Simulate API call
          await new Promise(resolve => setTimeout(resolve, 1000));
          removeUser(user.id);
          message.success('用户删除成功');
        } catch (error) {
          message.error('删除用户失败');
        } finally {
          setActionLoading(`delete-${user.id}`, false);
        }
      },
    });
  }, [removeUser, setActionLoading]);

  const handleToggleUserStatus = useCallback(async (user: User) => {
    const newStatus = user.status === 'active' ? 'inactive' : 'active';
    try {
      setActionLoading(`toggle-${user.id}`, true);
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 500));
      updateUser(user.id, { status: newStatus });
      message.success(`用户状态已${newStatus === 'active' ? '激活' : '禁用'}`);
    } catch (error) {
      message.error('更新用户状态失败');
    } finally {
      setActionLoading(`toggle-${user.id}`, false);
    }
  }, [updateUser, setActionLoading]);

  const handleAssignRoles = useCallback((user: User) => {
    openRoleAssignment(user);
  }, [openRoleAssignment]);

  // Bulk operations
  const handleBulkDelete = useCallback(() => {
    const selectedCount = selectedUsers.length;
    confirm({
      title: '批量删除用户',
      content: `确定要删除选中的 ${selectedCount} 个用户吗？此操作不可恢复。`,
      icon: <ExclamationCircleOutlined />,
      okText: '确认删除',
      okType: 'danger',
      cancelText: '取消',
      onOk: async () => {
        try {
          startBulkOperation(selectedCount);
          let completed = 0;
          let failed = 0;

          for (const userId of selectedUsers) {
            try {
              // Simulate API call
              await new Promise(resolve => setTimeout(resolve, 200));
              completed++;
            } catch {
              failed++;
            }
            updateBulkProgress(completed, failed);
          }

          removeUsers(selectedUsers);
          completeBulkOperation();
          clearSelection();
          message.success(`批量删除完成，成功 ${completed} 个，失败 ${failed} 个`);
        } catch (error) {
          message.error('批量删除失败');
          completeBulkOperation();
        }
      },
    });
  }, [selectedUsers, startBulkOperation, updateBulkProgress, removeUsers, completeBulkOperation, clearSelection]);

  const handleBulkActivate = useCallback(async () => {
    const selectedCount = selectedUsers.length;
    try {
      startBulkOperation(selectedCount);
      let completed = 0;
      let failed = 0;

      for (const userId of selectedUsers) {
        try {
          await new Promise(resolve => setTimeout(resolve, 200));
          updateUser(userId, { status: 'active' as UserStatus });
          completed++;
        } catch {
          failed++;
        }
        updateBulkProgress(completed, failed);
      }

      completeBulkOperation();
      clearSelection();
      message.success(`批量激活完成，成功 ${completed} 个，失败 ${failed} 个`);
    } catch (error) {
      message.error('批量激活失败');
      completeBulkOperation();
    }
  }, [selectedUsers, startBulkOperation, updateBulkProgress, updateUser, completeBulkOperation, clearSelection]);

  const handleBulkDeactivate = useCallback(async () => {
    const selectedCount = selectedUsers.length;
    try {
      startBulkOperation(selectedCount);
      let completed = 0;
      let failed = 0;

      for (const userId of selectedUsers) {
        try {
          await new Promise(resolve => setTimeout(resolve, 200));
          updateUser(userId, { status: 'inactive' as UserStatus });
          completed++;
        } catch {
          failed++;
        }
        updateBulkProgress(completed, failed);
      }

      completeBulkOperation();
      clearSelection();
      message.success(`批量禁用完成，成功 ${completed} 个，失败 ${failed} 个`);
    } catch (error) {
      message.error('批量禁用失败');
      completeBulkOperation();
    }
  }, [selectedUsers, startBulkOperation, updateBulkProgress, updateUser, completeBulkOperation, clearSelection]);

  // Export/Import operations
  const handleExport = useCallback(async () => {
    try {
      setExportLoading(true);
      // Simulate export
      await new Promise(resolve => setTimeout(resolve, 2000));
      message.success('用户数据导出成功');
    } catch (error) {
      message.error('导出失败');
    } finally {
      setExportLoading(false);
    }
  }, []);

  const handleImport = useCallback(async () => {
    try {
      setImportLoading(true);
      // Simulate import
      await new Promise(resolve => setTimeout(resolve, 2000));
      message.success('用户数据导入成功');
      loadUsers(); // Reload data
    } catch (error) {
      message.error('导入失败');
    } finally {
      setImportLoading(false);
    }
  }, [loadUsers]);

  // Action menu for each user
  const getActionMenu = (user: User): DropdownProps['menu'] => ({
    items: [
      {
        key: 'view',
        icon: <EyeOutlined />,
        label: '查看详情',
        onClick: () => handleViewUser(user),
      },
      {
        key: 'edit',
        icon: <EditOutlined />,
        label: '编辑用户',
        onClick: () => handleEditUser(user),
      },
      {
        key: 'roles',
        icon: <UserSwitchOutlined />,
        label: '分配角色',
        onClick: () => handleAssignRoles(user),
      },
      {
        key: 'divider1',
        type: 'divider',
      },
      {
        key: 'toggle',
        icon: user.status === 'active' ? <CloseCircleOutlined /> : <CheckCircleOutlined />,
        label: user.status === 'active' ? '禁用用户' : '激活用户',
        onClick: () => handleToggleUserStatus(user),
      },
      {
        key: 'divider2',
        type: 'divider',
      },
      {
        key: 'delete',
        icon: <DeleteOutlined />,
        label: '删除用户',
        danger: true,
        onClick: () => handleDeleteUser(user),
      },
    ],
  });

  // Bulk actions menu
  const bulkActionsMenu: DropdownProps['menu'] = {
    items: [
      {
        key: 'activate',
        icon: <CheckCircleOutlined />,
        label: '批量激活',
        onClick: handleBulkActivate,
      },
      {
        key: 'deactivate',
        icon: <CloseCircleOutlined />,
        label: '批量禁用',
        onClick: handleBulkDeactivate,
      },
      {
        key: 'divider',
        type: 'divider',
      },
      {
        key: 'delete',
        icon: <DeleteOutlined />,
        label: '批量删除',
        danger: true,
        onClick: handleBulkDelete,
      },
    ],
  };

  // Table columns
  const columns: ColumnsType<User> = [
    {
      title: '用户信息',
      key: 'userInfo',
      width: 200,
      fixed: 'left',
      render: (_, user) => (
        <div className="flex items-center space-x-3">
          <Avatar
            size={40}
            src={user.avatar}
            icon={<UserOutlined />}
            className="flex-shrink-0"
          />
          <div className="min-w-0 flex-1">
            <div className="font-medium text-gray-900 truncate">
              {user.displayName || user.username}
            </div>
            <div className="text-sm text-gray-500 truncate">
              {user.email}
            </div>
          </div>
        </div>
      ),
    },
    {
      title: '用户名',
      dataIndex: 'username',
      key: 'username',
      width: 120,
      sorter: true,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (status: UserStatus) => getStatusDisplay(status),
      filters: [
        { text: '活跃', value: 'active' },
        { text: '非活跃', value: 'inactive' },
        { text: '已封禁', value: 'banned' },
        { text: '待激活', value: 'pending' },
      ],
    },
    {
      title: '角色',
      dataIndex: 'roles',
      key: 'roles',
      width: 200,
      render: (roles: Role[]) => (
        <div className="space-y-1">
          {roles.map(role => (
            <Tag key={role.id} color="blue">
              {role.name}
            </Tag>
          ))}
        </div>
      ),
    },
    {
      title: '最后登录',
      dataIndex: 'lastLoginAt',
      key: 'lastLoginAt',
      width: 120,
      sorter: true,
      render: (date: string) => (
        <div className="text-sm">
          {date ? dayjs(date).format('MM-DD HH:mm') : '从未登录'}
        </div>
      ),
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 120,
      sorter: true,
      render: (date: string) => (
        <div className="text-sm">
          {dayjs(date).format('YYYY-MM-DD')}
        </div>
      ),
    },
    {
      title: '操作',
      key: 'actions',
      width: 80,
      fixed: 'right',
      render: (_, user) => (
        <Dropdown menu={getActionMenu(user)} trigger={['click']}>
          <Button type="text" icon={<MoreOutlined />} />
        </Dropdown>
      ),
    },
  ];

  return (
    <div className="user-management">
      {/* Page Header */}
      <div className="page-header">
        <div>
          <Title level={2} className="page-title">
            用户管理
          </Title>
          <Text className="page-description">
            管理系统用户，包括用户信息、角色权限、状态控制等
          </Text>
        </div>
        <Space>
          <Button
            icon={<ImportOutlined />}
            loading={importLoading}
            onClick={handleImport}
          >
            导入用户
          </Button>
          <Button
            icon={<ExportOutlined />}
            loading={exportLoading}
            onClick={handleExport}
          >
            导出用户
          </Button>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => openUserForm('create')}
          >
            新增用户
          </Button>
        </Space>
      </div>

      {/* Statistics Cards */}
      <Row gutter={[16, 16]} className="mb-6">
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="总用户数"
              value={userStats.total}
              prefix={<TeamOutlined />}
              valueStyle={{ color: '#1890ff' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="活跃用户"
              value={userStats.active}
              suffix={`/${userStats.total}`}
              prefix={<CheckCircleOutlined />}
              valueStyle={{ color: '#52c41a' }}
            />
            <div className="mt-2">
              <Text type="secondary">{userStats.activePercentage}% 活跃率</Text>
            </div>
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="非活跃用户"
              value={userStats.inactive}
              prefix={<CloseCircleOutlined />}
              valueStyle={{ color: '#faad14' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="已封禁用户"
              value={userStats.banned}
              prefix={<WarningOutlined />}
              valueStyle={{ color: '#ff4d4f' }}
            />
          </Card>
        </Col>
      </Row>

      {/* Filters */}
      <Card className="mb-4">
        <div className="flex flex-wrap items-center gap-4 mb-4">
          <Input.Search
            placeholder="搜索用户名、邮箱或显示名"
            value={searchValue}
            onChange={(e) => setSearchValue(e.target.value)}
            onSearch={handleSearch}
            style={{ width: 300 }}
            allowClear
          />
          
          <Select
            placeholder="选择状态"
            value={selectedStatus}
            onChange={handleStatusFilter}
            style={{ width: 120 }}
            allowClear
          >
            <Option value="active">活跃</Option>
            <Option value="inactive">非活跃</Option>
            <Option value="banned">已封禁</Option>
            <Option value="pending">待激活</Option>
          </Select>

          <Select
            placeholder="选择角色"
            value={selectedRole}
            onChange={handleRoleFilter}
            style={{ width: 150 }}
            allowClear
          >
            <Option value="1">Administrator</Option>
            <Option value="2">Editor</Option>
            <Option value="3">Author</Option>
            <Option value="4">User</Option>
          </Select>

          <Button
            icon={<FilterOutlined />}
            onClick={() => setShowFilters(!showFilters)}
          >
            更多筛选
          </Button>

          <Button onClick={handleClearFilters}>
            清除筛选
          </Button>

          <Button
            icon={<ReloadOutlined />}
            onClick={loadUsers}
            loading={loading}
          >
            刷新
          </Button>
        </div>

        {showFilters && (
          <div className="flex flex-wrap items-center gap-4 pt-4 border-t border-gray-200">
            <div className="flex items-center gap-2">
              <Text>创建时间:</Text>
              <RangePicker
                value={dateRange}
                onChange={handleDateRangeFilter}
                format="YYYY-MM-DD"
              />
            </div>
          </div>
        )}
      </Card>

      {/* Batch Operations */}
      {hasSelectedUsers && (
        <Card className="mb-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <Text strong>
                已选择 {selectedUsers.length} 个用户
              </Text>
              <Dropdown menu={bulkActionsMenu} trigger={['click']}>
                <Button>
                  批量操作 <MoreOutlined />
                </Button>
              </Dropdown>
              <Button onClick={clearSelection}>
                取消选择
              </Button>
            </div>

            {bulkProgress.isRunning && (
              <div className="flex items-center gap-4">
                <Progress
                  percent={Math.round(((bulkProgress.completed + bulkProgress.failed) / bulkProgress.total) * 100)}
                  
                  style={{ width: 100 }}
                />
                <Text type="secondary">
                  {bulkProgress.completed + bulkProgress.failed} / {bulkProgress.total}
                </Text>
              </div>
            )}
          </div>
        </Card>
      )}

      {/* Users Table */}
      <Card>
        <Table<User>
          columns={columns}
          dataSource={users}
          rowKey="id"
          loading={loading}
          pagination={{
            current: pagination.current,
            pageSize: pagination.pageSize,
            total: pagination.total,
            showSizeChanger: true,
            showQuickJumper: true,
            showTotal: (total, range) =>
              `第 ${range[0]}-${range[1]} 条，共 ${total} 条`,
            pageSizeOptions: ['10', '20', '50', '100'],
          }}
          rowSelection={rowSelection}
          onChange={handleTableChange}
          scroll={{ x: 1200 }}
          size="middle"
        />
      </Card>

      {/* User Form Modal */}
      <UserForm />

      {/* Role Assignment Modal */}
      <RoleSelector />

      {/* User Detail Drawer */}
      <UserDetail />
    </div>
  );
};

export default UserManagement;
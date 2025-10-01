import React, { useEffect, useState } from 'react';
import {
  Button,
  Card,
  DatePicker,
  Dropdown,
  Input,
  MenuProps,
  Popconfirm,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
  message,
} from 'antd';
import { PlusOutlined, ReloadOutlined, TeamOutlined, DeleteOutlined, EyeOutlined, EditOutlined, KeyOutlined } from '@ant-design/icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import dayjs, { Dayjs } from 'dayjs';
import UserForm from '@/components/users/UserForm';
import RoleSelector from '@/components/users/RoleSelector';
import UserDetailsDrawer from '@/components/users/UserDetailsDrawer';
import { usePagination, useQuery as useUserQuery, useUserManagementStore, useUsers } from '@/stores/userManagementStore';
import type { User, Role, UserStatus } from '@/types';
import userService from '@/services/user.service';
import roleService from '@/services/role.service';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;
const { Option } = Select;

const statusMeta: Record<UserStatus, { label: string; color: string }> = {
  active: { label: '活跃', color: 'green' },
  inactive: { label: '未激活', color: 'default' },
  banned: { label: '禁用', color: 'red' },
  pending: { label: '待审核', color: 'orange' },
};

const UserManagement: React.FC = () => {
  const queryClient = useQueryClient();
  const users = useUsers();
  const pagination = usePagination();
  const query = useUserQuery();
  const [searchValue, setSearchValue] = useState(query.search ?? '');

  useEffect(() => {
    setSearchValue(query.search ?? '');
  }, [query.search]);

  const {
    setUsers,
    setQuery,
    changePage,
    setSelectedUsers,
    selectedUsers,
    openUserForm,
    openRoleAssignment,
    openUserDetails,
    clearSelection,
    removeUser,
    removeUsers,
    setError,
  } = useUserManagementStore();

  const { data: roles = [] } = useQuery({
    queryKey: ['admin-roles'],
    queryFn: () => roleService.getRoles(),
    staleTime: 300_000,
  });

  const usersQuery = useQuery({
    queryKey: ['admin-users', query],
    queryFn: () => userService.getUsers(query),
    keepPreviousData: true,
    onSuccess: (response) => {
      setUsers(response);
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '获取用户列表失败';
      setError(errMsg);
      message.error(errMsg);
    },
  });

  const deleteUserMutation = useMutation({
    mutationFn: (userId: string) => userService.deleteUser(userId),
    onSuccess: (_, userId) => {
      removeUser(userId);
      message.success('已删除用户');
      queryClient.invalidateQueries({ queryKey: ['admin-users'] });
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '删除用户失败';
      message.error(errMsg);
    },
  });

  const bulkDeleteMutation = useMutation({
    mutationFn: (userIds: string[]) => userService.deleteUsers(userIds),
    onSuccess: (_, userIds) => {
      removeUsers(userIds);
      clearSelection();
      message.success('已批量删除选中用户');
      queryClient.invalidateQueries({ queryKey: ['admin-users'] });
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '批量删除失败';
      message.error(errMsg);
    },
  });

  const resetPasswordMutation = useMutation({
    mutationFn: ({ id, password }: { id: string; password: string }) => userService.resetPassword(id, password),
    onSuccess: () => message.success('已发送密码重置请求'),
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '重置密码失败';
      message.error(errMsg);
    },
  });

  const handleSearch = (value: string) => {
    setQuery({ search: value, page: 1 });
  };

  const handleStatusChange = (value: UserStatus | undefined) => {
    setQuery({ status: value, page: 1 });
  };

  const handleRoleFilterChange = (value: string | undefined) => {
    setQuery({ roleId: value, page: 1 });
  };

  const handleDateRangeChange = (value: [Dayjs, Dayjs] | null) => {
    if (!value) {
      setQuery({ startDate: undefined, endDate: undefined });
      return;
    }
    setQuery({
      startDate: value[0].startOf('day').toISOString(),
      endDate: value[1].endOf('day').toISOString(),
      page: 1,
    });
  };

  const handleTableChange = (paginationConfig: { current?: number; pageSize?: number }) => {
    changePage(paginationConfig.current ?? 1, paginationConfig.pageSize);
  };

  const handleDeleteUser = (userId: string) => {
    deleteUserMutation.mutate(userId);
  };

  const handleResetPassword = (userId: string) => {
    resetPasswordMutation.mutate({ id: userId, password: 'Temp@1234' });
  };

  const bulkMenuItems: MenuProps['items'] = [
    {
      key: 'delete',
      icon: <DeleteOutlined />,
      label: (
        <Popconfirm
          title="确认删除选中用户?"
          onConfirm={() => bulkDeleteMutation.mutate(selectedUsers)}
          okText="确定"
          cancelText="取消"
          disabled={selectedUsers.length === 0}
        >
          <span>批量删除</span>
        </Popconfirm>
      ),
      disabled: selectedUsers.length === 0,
    },
  ];

  const columns = [
    {
      title: '用户',
      dataIndex: 'username',
      key: 'username',
      render: (_: string, record: User) => (
        <Space direction="vertical" size={2}>
          <Text strong>{record.displayName || record.username}</Text>
          <Text type="secondary">{record.email}</Text>
        </Space>
      ),
    },
    {
      title: '角色',
      dataIndex: 'roles',
      key: 'roles',
      render: (roles: Role[]) => (
        <Space size={[4, 4]} wrap>
          {roles.map((role) => (
            <Tag color={role.isBuiltIn ? 'blue' : 'geekblue'} key={role.id}>
              {role.name}
            </Tag>
          ))}
        </Space>
      ),
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      render: (status: UserStatus) => {
        const meta = statusMeta[status] ?? { label: status, color: 'default' };
        return <Tag color={meta.color}>{meta.label}</Tag>;
      },
    },
    {
      title: '最近登录',
      dataIndex: 'lastLoginAt',
      key: 'lastLoginAt',
      render: (value?: string) => (value ? dayjs(value).format('YYYY-MM-DD HH:mm') : '未记录'),
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (value: string) => dayjs(value).format('YYYY-MM-DD HH:mm'),
    },
    {
      title: '操作',
      key: 'actions',
      width: 220,
      render: (_: unknown, record: User) => (
        <Space size="small">
          <Tooltip title="查看详情">
            <Button type="link" icon={<EyeOutlined />} onClick={() => openUserDetails(record)}>
              详情
            </Button>
          </Tooltip>
          <Tooltip title="编辑">
            <Button type="link" icon={<EditOutlined />} onClick={() => openUserForm('edit', record)}>
              编辑
            </Button>
          </Tooltip>
          <Tooltip title="分配角色">
            <Button type="link" icon={<TeamOutlined />} onClick={() => openRoleAssignment(record)}>
              角色
            </Button>
          </Tooltip>
          <Tooltip title="重置密码">
            <Button
              type="link"
              icon={<KeyOutlined />}
              onClick={() => handleResetPassword(record.id)}
              loading={resetPasswordMutation.isLoading}
            >
              重置
            </Button>
          </Tooltip>
          <Popconfirm
            title="确定删除该用户?"
            okText="删除"
            cancelText="取消"
            onConfirm={() => handleDeleteUser(record.id)}
          >
            <Button type="link" danger icon={<DeleteOutlined />}>
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div className="page-header">
        <div>
          <Title level={2} className="page-title">
            用户管理
          </Title>
          <Text className="page-description">
            管理后台用户、角色分配及账号状态
          </Text>
        </div>
        <Space>
          <Button icon={<ReloadOutlined />} onClick={() => usersQuery.refetch()} loading={usersQuery.isFetching}>
            刷新
          </Button>
          <Dropdown menu={{ items: bulkMenuItems }} trigger={['click']}>
            <Button disabled={selectedUsers.length === 0}>
              批量操作
            </Button>
          </Dropdown>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => openUserForm('create')}>
            新增用户
          </Button>
        </Space>
      </div>

      <Card className="page-container" bordered={false}>
        <Space direction="vertical" size={16} className="w-full">
          <Space wrap>
            <Input.Search
              placeholder="搜索用户名/邮箱"
              allowClear
              value={searchValue}
              onChange={(event) => {
                const value = event.target.value;
                setSearchValue(value);
                if (value === '') {
                  handleSearch('');
                }
              }}
              onSearch={handleSearch}
              style={{ width: 240 }}
            />
            <Select
              allowClear
              placeholder="按状态筛选"
              style={{ width: 160 }}
              value={query.status}
              onChange={handleStatusChange}
            >
              {Object.entries(statusMeta).map(([value, meta]) => (
                <Option key={value} value={value}>
                  {meta.label}
                </Option>
              ))}
            </Select>
            <Select
              allowClear
              placeholder="按角色筛选"
              style={{ width: 200 }}
              value={query.roleId}
              onChange={handleRoleFilterChange}
            >
              {roles.map((role) => (
                <Option key={role.id} value={role.id}>
                  {role.name}
                </Option>
              ))}
            </Select>
            <RangePicker
              value={query.startDate && query.endDate ? [dayjs(query.startDate), dayjs(query.endDate)] : null}
              onChange={handleDateRangeChange}
              allowEmpty={[true, true]}
            />
          </Space>

          <Table<User>
            rowKey="id"
            loading={usersQuery.isLoading}
            dataSource={users}
            columns={columns}
            rowSelection={{
              selectedRowKeys: selectedUsers,
              onChange: (keys) => setSelectedUsers(keys as string[]),
            }}
            pagination={{
              current: pagination.current,
              pageSize: pagination.pageSize,
              total: pagination.total,
              showSizeChanger: true,
              showTotal: (total) => `共 ${total} 位用户`,
            }}
            onChange={handleTableChange}
          />
        </Space>
      </Card>

      <UserForm />
      <RoleSelector />
      <UserDetailsDrawer />
    </div>
  );
};

export default UserManagement;

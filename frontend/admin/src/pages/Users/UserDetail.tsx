// @ts-nocheck
import React, { useState, useMemo } from 'react';
import {
  Drawer,
  Card,
  Typography,
  Avatar,
  Tag,
  Descriptions,
  Tabs,
  Timeline,
  Table,
  Badge,
  Progress,
  Space,
  Button,
  Tooltip,
  Statistic,
  Row,
  Col,
  Alert,
  Divider,
  Empty,
  List,
  Rate,
  Switch,
  message,
} from 'antd';
import {
  UserOutlined,
  MailOutlined,
  PhoneOutlined,
  CalendarOutlined,
  SafetyOutlined,
  HistoryOutlined,
  FileTextOutlined,
  CommentOutlined,
  HeartOutlined,
  EyeOutlined,
  EditOutlined,
  SettingOutlined,
  ShieldOutlined,
  ClockCircleOutlined,
  CheckCircleOutlined,
  ExclamationCircleOutlined,
  WarningOutlined,
  LoginOutlined,
  LogoutOutlined,
  GlobalOutlined,
  MobileOutlined,
  DesktopOutlined,
  TeamOutlined,
  CrownOutlined,
  StarOutlined,
  TrophyOutlined,
  ThunderboltOutlined,
  FireOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import {
  useUserDetails,
  useUserManagementStore,
} from '@/stores/userManagementStore';
import type { User, UserStatus, Role, Activity, AuditLog } from '@/types';

dayjs.extend(relativeTime);

const { Title, Text, Paragraph } = Typography;
const { TabPane } = Tabs;

interface UserDetailProps {}

const UserDetail: React.FC<UserDetailProps> = () => {
  const { visible, user } = useUserDetails();
  const { closeUserDetails, openUserForm, openRoleAssignment } = useUserManagementStore();
  
  const [activeTab, setActiveTab] = useState('basic');

  // Mock data for user activities and audit logs
  const mockActivities = useMemo(() => [
    {
      id: '1',
      type: 'user_login' as const,
      description: '用户登录系统',
      userId: user?.id || '',
      user: user!,
      createdAt: '2024-01-15T10:30:00Z',
    },
    {
      id: '2',
      type: 'post_create' as const,
      description: '创建了新文章《React 19 新特性解析》',
      userId: user?.id || '',
      user: user!,
      entityType: 'post',
      entityId: 'post_123',
      createdAt: '2024-01-15T09:15:00Z',
    },
    {
      id: '3',
      type: 'post_update' as const,
      description: '更新了文章《前端性能优化实践》',
      userId: user?.id || '',
      user: user!,
      entityType: 'post',
      entityId: 'post_124',
      createdAt: '2024-01-14T16:45:00Z',
    },
  ], [user]);

  const mockAuditLogs = useMemo(() => [
    {
      id: '1',
      action: 'USER_LOGIN',
      entityType: 'user',
      entityId: user?.id || '',
      userId: user?.id || '',
      user: user!,
      ipAddress: '192.168.1.100',
      userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
      createdAt: '2024-01-15T10:30:00Z',
    },
    {
      id: '2',
      action: 'USER_PROFILE_UPDATE',
      entityType: 'user',
      entityId: user?.id || '',
      userId: user?.id || '',
      user: user!,
      ipAddress: '192.168.1.100',
      userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
      changes: {
        displayName: { old: 'Old Name', new: 'New Name' },
        email: { old: 'old@email.com', new: 'new@email.com' },
      },
      createdAt: '2024-01-14T15:20:00Z',
    },
  ], [user]);

  const mockUserStats = useMemo(() => ({
    postsCount: 15,
    commentsCount: 42,
    likesReceived: 186,
    viewsTotal: 12500,
    loginCount: 89,
    lastActiveDate: '2024-01-15T10:30:00Z',
    memberSince: user?.createdAt || '2024-01-01T00:00:00Z',
    averageRating: 4.8,
    badges: ['早期用户', '活跃作者', '优质内容'],
  }), [user]);

  if (!user || !visible) {
    return null;
  }

  // Get status configuration
  const getStatusConfig = (status: UserStatus) => {
    const configs = {
      active: {
        color: 'success',
        icon: <CheckCircleOutlined />,
        text: '活跃',
        description: '用户状态正常，可以正常使用系统功能',
      },
      inactive: {
        color: 'default',
        icon: <ClockCircleOutlined />,
        text: '非活跃',
        description: '用户长时间未登录，账号状态为非活跃',
      },
      banned: {
        color: 'error',
        icon: <ExclamationCircleOutlined />,
        text: '已封禁',
        description: '用户违反了使用条款，账号已被封禁',
      },
      pending: {
        color: 'warning',
        icon: <WarningOutlined />,
        text: '待激活',
        description: '用户注册后尚未激活邮箱',
      },
    };
    return configs[status];
  };

  // Role hierarchy colors
  const getRoleColor = (level: number) => {
    const colors = {
      1: 'red',      // Administrator
      2: 'orange',   // Editor
      3: 'blue',     // Author
      4: 'green',    // User
    };
    return colors[level as keyof typeof colors] || 'default';
  };

  // Activities table columns
  const activitiesColumns: ColumnsType<typeof mockActivities[0]> = [
    {
      title: '活动类型',
      dataIndex: 'type',
      key: 'type',
      width: 120,
      render: (type) => {
        const typeConfig = {
          user_login: { icon: <LoginOutlined />, text: '登录', color: 'blue' },
          user_register: { icon: <UserOutlined />, text: '注册', color: 'green' },
          post_create: { icon: <FileTextOutlined />, text: '创建文章', color: 'cyan' },
          post_update: { icon: <EditOutlined />, text: '更新文章', color: 'orange' },
          post_delete: { icon: <DeleteOutlined />, text: '删除文章', color: 'red' },
          comment_create: { icon: <CommentOutlined />, text: '发表评论', color: 'purple' },
        };
        const config = typeConfig[type as keyof typeof typeConfig];
        return config ? (
          <Tag icon={config.icon} color={config.color}>
            {config.text}
          </Tag>
        ) : type;
      },
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
    },
    {
      title: '时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 150,
      render: (date) => (
        <div>
          <div>{dayjs(date).format('YYYY-MM-DD HH:mm')}</div>
          <Text type="secondary" style={{ fontSize: '12px' }}>
            {dayjs(date).fromNow()}
          </Text>
        </div>
      ),
    },
  ];

  // Audit logs table columns
  const auditColumns: ColumnsType<typeof mockAuditLogs[0]> = [
    {
      title: '操作',
      dataIndex: 'action',
      key: 'action',
      width: 150,
      render: (action) => (
        <Tag color="blue">{action.replace(/_/g, ' ')}</Tag>
      ),
    },
    {
      title: 'IP地址',
      dataIndex: 'ipAddress',
      key: 'ipAddress',
      width: 130,
    },
    {
      title: '设备信息',
      dataIndex: 'userAgent',
      key: 'userAgent',
      ellipsis: true,
      render: (userAgent) => {
        const isMobile = userAgent.includes('Mobile');
        return (
          <Tooltip title={userAgent}>
            <span>
              {isMobile ? <MobileOutlined /> : <DesktopOutlined />}
              {isMobile ? ' 移动设备' : ' 桌面设备'}
            </span>
          </Tooltip>
        );
      },
    },
    {
      title: '时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 150,
      render: (date) => dayjs(date).format('YYYY-MM-DD HH:mm:ss'),
    },
  ];

  const statusConfig = getStatusConfig(user.status);

  return (
    <Drawer
      title={
        <div className="flex items-center gap-3">
          <Avatar size={40} src={user.avatar} icon={<UserOutlined />} />
          <div>
            <div className="font-semibold">
              {user.displayName || user.username}
            </div>
            <div className="text-sm text-gray-500">
              用户详细信息
            </div>
          </div>
        </div>
      }
      placement="right"
      width={800}
      open={visible}
      onClose={closeUserDetails}
      extra={
        <Space>
          <Button
            icon={<EditOutlined />}
            onClick={() => {
              closeUserDetails();
              openUserForm('edit', user);
            }}
          >
            编辑
          </Button>
          <Button
            icon={<SettingOutlined />}
            onClick={() => {
              closeUserDetails();
              openRoleAssignment(user);
            }}
          >
            角色管理
          </Button>
        </Space>
      }
    >
      <div className="space-y-6">
        {/* User Status Alert */}
        {user.status !== 'active' && (
          <Alert
            type={statusConfig.color === 'error' ? 'error' : 'warning'}
            message={`用户状态：${statusConfig.text}`}
            description={statusConfig.description}
            showIcon
            icon={statusConfig.icon}
          />
        )}

        <Tabs activeKey={activeTab} onChange={setActiveTab}>
          {/* Basic Information */}
          <TabPane tab="基本信息" key="basic">
            <div className="space-y-6">
              {/* User Profile Card */}
              <Card>
                <div className="flex items-start gap-6">
                  <Avatar size={80} src={user.avatar} icon={<UserOutlined />} />
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <Title level={4} className="mb-0">
                        {user.displayName || user.username}
                      </Title>
                      <Tag
                        color={statusConfig.color}
                        icon={statusConfig.icon}
                      >
                        {statusConfig.text}
                      </Tag>
                    </div>
                    <div className="flex flex-wrap gap-2 mb-3">
                      {user.roles.map(role => (
                        <Tag
                          key={role.id}
                          color={getRoleColor(role.level)}
                          icon={role.level === 1 ? <CrownOutlined /> : <TeamOutlined />}
                        >
                          {role.name}
                        </Tag>
                      ))}
                    </div>
                    <div className="space-y-1 text-sm text-gray-600">
                      <div className="flex items-center gap-2">
                        <MailOutlined />
                        {user.email}
                      </div>
                      <div className="flex items-center gap-2">
                        <CalendarOutlined />
                        注册于 {dayjs(user.createdAt).format('YYYY年MM月DD日')}
                      </div>
                      {user.lastLoginAt && (
                        <div className="flex items-center gap-2">
                          <ClockCircleOutlined />
                          最后登录 {dayjs(user.lastLoginAt).fromNow()}
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </Card>

              {/* User Statistics */}
              <Card title="用户统计" >
                <Row gutter={[16, 16]}>
                  <Col xs={12} sm={8} lg={6}>
                    <Statistic
                      title="发表文章"
                      value={mockUserStats.postsCount}
                      prefix={<FileTextOutlined />}
                      valueStyle={{ color: '#1890ff' }}
                    />
                  </Col>
                  <Col xs={12} sm={8} lg={6}>
                    <Statistic
                      title="评论数量"
                      value={mockUserStats.commentsCount}
                      prefix={<CommentOutlined />}
                      valueStyle={{ color: '#52c41a' }}
                    />
                  </Col>
                  <Col xs={12} sm={8} lg={6}>
                    <Statistic
                      title="获得点赞"
                      value={mockUserStats.likesReceived}
                      prefix={<HeartOutlined />}
                      valueStyle={{ color: '#eb2f96' }}
                    />
                  </Col>
                  <Col xs={12} sm={8} lg={6}>
                    <Statistic
                      title="总阅读量"
                      value={mockUserStats.viewsTotal}
                      prefix={<EyeOutlined />}
                      valueStyle={{ color: '#722ed1' }}
                    />
                  </Col>
                  <Col xs={12} sm={8} lg={6}>
                    <Statistic
                      title="登录次数"
                      value={mockUserStats.loginCount}
                      prefix={<LoginOutlined />}
                      valueStyle={{ color: '#fa8c16' }}
                    />
                  </Col>
                  <Col xs={12} sm={8} lg={6}>
                    <div>
                      <div className="text-sm text-gray-500 mb-1">内容评分</div>
                      <Rate disabled value={mockUserStats.averageRating} allowHalf />
                      <div className="text-lg font-semibold">
                        {mockUserStats.averageRating}
                      </div>
                    </div>
                  </Col>
                </Row>
              </Card>

              {/* User Badges */}
              <Card title="用户徽章" >
                <div className="flex flex-wrap gap-2">
                  {mockUserStats.badges.map((badge, index) => (
                    <Tag
                      key={index}
                      color="gold"
                      icon={<TrophyOutlined />}
                      className="px-3 py-1"
                    >
                      {badge}
                    </Tag>
                  ))}
                </div>
              </Card>

              {/* Detailed Information */}
              <Card title="详细信息" >
                <Descriptions column={2} >
                  <Descriptions.Item label="用户ID">
                    {user.id}
                  </Descriptions.Item>
                  <Descriptions.Item label="用户名">
                    {user.username}
                  </Descriptions.Item>
                  <Descriptions.Item label="显示名称">
                    {user.displayName || '未设置'}
                  </Descriptions.Item>
                  <Descriptions.Item label="邮箱地址">
                    {user.email}
                  </Descriptions.Item>
                  <Descriptions.Item label="账号状态">
                    <Tag color={statusConfig.color} icon={statusConfig.icon}>
                      {statusConfig.text}
                    </Tag>
                  </Descriptions.Item>
                  <Descriptions.Item label="注册时间">
                    {dayjs(user.createdAt).format('YYYY-MM-DD HH:mm:ss')}
                  </Descriptions.Item>
                  <Descriptions.Item label="更新时间">
                    {dayjs(user.updatedAt).format('YYYY-MM-DD HH:mm:ss')}
                  </Descriptions.Item>
                  <Descriptions.Item label="最后登录">
                    {user.lastLoginAt 
                      ? dayjs(user.lastLoginAt).format('YYYY-MM-DD HH:mm:ss')
                      : '从未登录'
                    }
                  </Descriptions.Item>
                </Descriptions>
              </Card>
            </div>
          </TabPane>

          {/* Roles & Permissions */}
          <TabPane tab="角色权限" key="roles">
            <div className="space-y-4">
              {user.roles.map(role => (
                <Card
                  key={role.id}
                  
                  title={
                    <div className="flex items-center gap-2">
                      <Tag
                        color={getRoleColor(role.level)}
                        icon={role.level === 1 ? <CrownOutlined /> : <TeamOutlined />}
                      >
                        {role.name}
                      </Tag>
                      <Text type="secondary">级别 {role.level}</Text>
                    </div>
                  }
                >
                  <div className="mb-3">
                    <Text type="secondary">{role.description}</Text>
                  </div>
                  
                  {role.permissions && role.permissions.length > 0 ? (
                    <div>
                      <Text strong className="block mb-2">权限列表：</Text>
                      <div className="flex flex-wrap gap-1">
                        {role.permissions.map(permission => (
                          <Tag key={permission.id} className="mb-1">
                            {permission.name}
                          </Tag>
                        ))}
                      </div>
                    </div>
                  ) : (
                    <Empty
                      image={Empty.PRESENTED_IMAGE_SIMPLE}
                      description="暂无权限配置"
                      className="py-4"
                    />
                  )}
                </Card>
              ))}
            </div>
          </TabPane>

          {/* Activity History */}
          <TabPane tab="活动历史" key="activities">
            <Card>
              <Table
                columns={activitiesColumns}
                dataSource={mockActivities}
                rowKey="id"
                
                pagination={{
                  pageSize: 10,
                  showSizeChanger: true,
                  showQuickJumper: true,
                }}
              />
            </Card>
          </TabPane>

          {/* Security Logs */}
          <TabPane tab="安全日志" key="security">
            <Card>
              <Table
                columns={auditColumns}
                dataSource={mockAuditLogs}
                rowKey="id"
                
                pagination={{
                  pageSize: 10,
                  showSizeChanger: true,
                  showQuickJumper: true,
                }}
                expandable={{
                  expandedRowRender: (record) => (
                    record.changes ? (
                      <div className="p-4 bg-gray-50 rounded">
                        <Text strong>变更详情：</Text>
                        <pre className="mt-2 text-sm">
                          {JSON.stringify(record.changes, null, 2)}
                        </pre>
                      </div>
                    ) : null
                  ),
                  rowExpandable: (record) => !!record.changes,
                }}
              />
            </Card>
          </TabPane>
        </Tabs>
      </div>
    </Drawer>
  );
};

export default UserDetail;
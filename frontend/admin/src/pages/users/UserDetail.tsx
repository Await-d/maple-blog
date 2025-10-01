import React, { useMemo } from 'react';
import {
  Card,
  Col,
  Descriptions,
  List,
  Row,
  Space,
  Spin,
  Statistic,
  Tag,
  Typography,
  message,
} from 'antd';
import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import dayjs from 'dayjs';
import userService from '@/services/user.service';
import type { User, UserActivityLog, UserStatus } from '@/types';
import { useUserManagementStore } from '@/stores/userManagementStore';

const { Title, Text } = Typography;

const statusMeta: Record<UserStatus, { label: string; color: string }> = {
  active: { label: '活跃', color: 'green' },
  inactive: { label: '未激活', color: 'default' },
  banned: { label: '禁用', color: 'red' },
  pending: { label: '待审核', color: 'orange' },
};

const UserDetail: React.FC = () => {
  const params = useParams();
  const userId = params.id as string;
  const { updateUser } = useUserManagementStore();

  const userQuery = useQuery({
    queryKey: ['admin-user', userId],
    queryFn: () => userService.getUserById(userId),
    enabled: Boolean(userId),
    onSuccess: (user) => updateUser(user.id, user),
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '加载用户信息失败';
      message.error(errMsg);
    },
  });

  const activityQuery = useQuery({
    queryKey: ['admin-user-activities', userId],
    queryFn: () => userService.getActivities(userId),
    enabled: Boolean(userId),
  });

  const user = userQuery.data as User | undefined;

  const statusTag = useMemo(() => {
    if (!user) return null;
    const meta = statusMeta[user.status] ?? { label: user.status, color: 'default' };
    return <Tag color={meta.color}>{meta.label}</Tag>;
  }, [user]);

  if (userQuery.isLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <Spin tip="加载用户信息中..." />
      </div>
    );
  }

  if (!user) {
    return (
      <Card className="page-container">
        <Title level={3}>未找到用户</Title>
        <Text type="secondary">无法加载用户详情，请返回列表后重试。</Text>
      </Card>
    );
  }

  return (
    <Space direction="vertical" size={24} className="w-full">
      <div className="page-header">
        <div>
          <Title level={2} className="page-title">
            用户详情
          </Title>
          <Text className="page-description">查看用户基本信息、角色及操作记录</Text>
        </div>
        {statusTag}
      </div>

      <Row gutter={[16, 16]}>
        <Col xs={24} lg={16}>
          <Card title="基本信息" bordered={false}>
            <Descriptions column={2} labelStyle={{ minWidth: 120 }}>
              <Descriptions.Item label="用户 ID">{user.id}</Descriptions.Item>
              <Descriptions.Item label="用户名">{user.username}</Descriptions.Item>
              <Descriptions.Item label="显示名称">{user.displayName}</Descriptions.Item>
              <Descriptions.Item label="邮箱">{user.email}</Descriptions.Item>
              <Descriptions.Item label="最近登录">
                {user.lastLoginAt ? dayjs(user.lastLoginAt).format('YYYY-MM-DD HH:mm:ss') : '未记录'}
              </Descriptions.Item>
              <Descriptions.Item label="创建时间">{dayjs(user.createdAt).format('YYYY-MM-DD HH:mm:ss')}</Descriptions.Item>
              <Descriptions.Item label="更新时间">{dayjs(user.updatedAt).format('YYYY-MM-DD HH:mm:ss')}</Descriptions.Item>
            </Descriptions>
          </Card>

          <Card title="角色与权限" bordered={false} className="mt-4">
            {user.roles.length > 0 ? (
              <Space size={[8, 8]} wrap>
                {user.roles.map((role) => (
                  <Tag color={role.isBuiltIn ? 'blue' : 'geekblue'} key={role.id}>
                    {role.name}
                  </Tag>
                ))}
              </Space>
            ) : (
              <Text type="secondary">尚未分配角色</Text>
            )}
          </Card>
        </Col>

        <Col xs={24} lg={8}>
          <Card title="账号统计" bordered={false}>
            <Row gutter={[16, 16]}>
              <Col span={12}>
                <Statistic title="角色数量" value={user.roles.length} />
              </Col>
              <Col span={12}>
                <Statistic title="账号状态" value={statusMeta[user.status]?.label ?? user.status} />
              </Col>
              <Col span={12}>
                <Statistic title="创建天数" value={dayjs().diff(dayjs(user.createdAt), 'day')} suffix="天" />
              </Col>
              <Col span={12}>
                <Statistic title="距上次登录" value={user.lastLoginAt ? dayjs().diff(dayjs(user.lastLoginAt), 'day') : '--'} suffix={user.lastLoginAt ? '天' : ''} />
              </Col>
            </Row>
          </Card>
        </Col>
      </Row>

      <Card
        title="活动记录"
        bordered={false}
        extra={<Text type="secondary">最近 20 条</Text>}
      >
        <List<UserActivityLog>
          loading={activityQuery.isLoading}
          dataSource={activityQuery.data ?? []}
          locale={{ emptyText: '暂无活动记录' }}
          renderItem={(item) => (
            <List.Item key={item.id}>
              <List.Item.Meta
                title={item.description}
                description={
                  <Space direction="vertical" size={0}>
                    <Text type="secondary">{dayjs(item.createdAt).format('YYYY-MM-DD HH:mm:ss')}</Text>
                    {item.ip && <Text type="secondary">IP: {item.ip}</Text>}
                    {item.userAgent && <Text type="secondary">UA: {item.userAgent}</Text>}
                  </Space>
                }
              />
            </List.Item>
          )}
        />
      </Card>
    </Space>
  );
};

export default UserDetail;

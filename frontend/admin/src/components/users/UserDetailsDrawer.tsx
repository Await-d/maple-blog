import React, { useMemo } from 'react';
import { Drawer, Descriptions, Tag, Space, Typography, Avatar, Button, Divider, Statistic } from 'antd';
import { CalendarOutlined, MailOutlined, UserOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useUserDetails, useUserManagementStore } from '@/stores/userManagementStore';
import type { UserStatus } from '@/types';

const { Text } = Typography;

const statusMeta: Record<UserStatus, { label: string; color: string }> = {
  active: { label: '活跃', color: 'green' },
  inactive: { label: '未激活', color: 'default' },
  banned: { label: '已禁用', color: 'red' },
  pending: { label: '待审核', color: 'orange' },
};

const UserDetailsDrawer: React.FC = () => {
  const navigate = useNavigate();
  const { visible, user } = useUserDetails();
  const status = user ? statusMeta[user.status as UserStatus] : undefined;
  const closeUserDetails = useUserManagementStore((state) => state.closeUserDetails);

  const avatarContent = useMemo(() => {
    if (!user) return <UserOutlined />;
    if (user.avatar) {
      return <Avatar size={64} src={user.avatar} />;
    }
    return (
      <Avatar size={64} icon={<UserOutlined />}>
        {user.displayName?.[0] ?? user.username?.[0]?.toUpperCase()}
      </Avatar>
    );
  }, [user]);

  return (
    <Drawer
      width={420}
      title="用户详情"
      open={visible}
      onClose={closeUserDetails}
      destroyOnClose
      extra={user && (
        <Space>
          <Button
            type="link"
            onClick={() => {
              closeUserDetails();
              navigate(`/users/${user.id}`);
            }}
          >
            前往详情页
          </Button>
        </Space>
      )}
    >
      {user ? (
        <Space direction="vertical" size={24} className="w-full">
          <div className="flex flex-col items-center gap-3">
            {avatarContent}
            <Space direction="vertical" align="center" size={4}>
              <Text strong>{user.displayName || user.username}</Text>
              <Space size="small">
                {status && <Tag color={status.color}>{status.label}</Tag>}
                <Tag icon={<UserOutlined />}>ID: {user.id}</Tag>
              </Space>
            </Space>
          </div>

          <Descriptions column={1} size="small" bordered>
            <Descriptions.Item label="用户名">{user.username}</Descriptions.Item>
            <Descriptions.Item label="邮箱">
              <Space>
                <MailOutlined />
                <Text>{user.email}</Text>
              </Space>
            </Descriptions.Item>
            <Descriptions.Item label="最近登录">
              <Space>
                <CalendarOutlined />
                <Text>{user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : '未记录'}</Text>
              </Space>
            </Descriptions.Item>
            <Descriptions.Item label="创建时间">
              {new Date(user.createdAt).toLocaleString()}
            </Descriptions.Item>
          </Descriptions>

          <div>
            <Text strong>角色</Text>
            <Divider className="my-2" />
            <Space size={[8, 8]} wrap>
              {user.roles.length > 0 ? (
                user.roles.map((role) => (
                  <Tag key={role.id} color={role.isBuiltIn ? 'blue' : 'geekblue'}>
                    {role.name}
                  </Tag>
                ))
              ) : (
                <Text type="secondary">尚未分配角色</Text>
              )}
            </Space>
          </div>

          <div>
            <Text strong>统计信息</Text>
            <Divider className="my-2" />
            <Space size={16} wrap>
              <Statistic title="角色数量" value={user.roles.length} />
              <Statistic title="账号状态" value={status?.label ?? '未知'} />
            </Space>
          </div>
        </Space>
      ) : (
        <Text type="secondary">未找到用户信息</Text>
      )}
    </Drawer>
  );
};

export default UserDetailsDrawer;

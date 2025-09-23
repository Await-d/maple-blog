// @ts-nocheck
import React from 'react';
import { Card, Typography, Button, Space } from 'antd';
import { PlusOutlined } from '@ant-design/icons';

const { Title, Text } = Typography;

const UserManagement: React.FC = () => {
  return (
    <div>
      <div className="page-header">
        <div>
          <Title level={2} className="page-title">
            用户管理
          </Title>
          <Text className="page-description">
            管理系统用户，包括用户信息、角色权限等
          </Text>
        </div>
        <Space>
          <Button type="primary" icon={<PlusOutlined />}>
            新增用户
          </Button>
        </Space>
      </div>

      <Card className="page-container">
        <div style={{ textAlign: 'center', padding: '64px 0', color: '#999' }}>
          <div style={{ fontSize: '64px', marginBottom: '16px' }}>👥</div>
          <div>用户管理功能正在开发中...</div>
        </div>
      </Card>
    </div>
  );
};

export default UserManagement;
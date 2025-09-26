import React from 'react';
import { Card, Typography } from 'antd';

const { Title, Text } = Typography;

const RoleManagement: React.FC = () => {
  return (
    <Card className="page-container">
      <Title level={2}>角色管理</Title>
      <Text type="secondary">角色管理页面正在开发中...</Text>
    </Card>
  );
};

export default RoleManagement;
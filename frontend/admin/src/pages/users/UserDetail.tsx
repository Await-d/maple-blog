// @ts-nocheck
import React from 'react';
import { Card, Typography } from 'antd';

const { Title, Text } = Typography;

const UserDetail: React.FC = () => {
  return (
    <Card className="page-container">
      <Title level={2}>用户详情</Title>
      <Text type="secondary">用户详情页面正在开发中...</Text>
    </Card>
  );
};

export default UserDetail;
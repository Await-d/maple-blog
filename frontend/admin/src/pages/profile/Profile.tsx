// @ts-nocheck
import React from 'react';
import { Card, Typography } from 'antd';

const { Title, Text } = Typography;

const Profile: React.FC = () => {
  return (
    <Card className="page-container">
      <Title level={2}>Profile</Title>
      <Text type="secondary">Profile页面正在开发中...</Text>
    </Card>
  );
};

export default Profile;

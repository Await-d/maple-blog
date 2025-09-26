import React from 'react';
import { Card, Typography } from 'antd';

const { Title, Text } = Typography;

const SystemSettings: React.FC = () => {
  return (
    <Card className="page-container">
      <Title level={2}>SystemSettings</Title>
      <Text type="secondary">SystemSettings页面正在开发中...</Text>
    </Card>
  );
};

export default SystemSettings;

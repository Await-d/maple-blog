import React from 'react';
import { Card, Typography } from 'antd';

const { Title, Text } = Typography;

const AuditLogs: React.FC = () => {
  return (
    <Card className="page-container">
      <Title level={2}>AuditLogs</Title>
      <Text type="secondary">AuditLogs页面正在开发中...</Text>
    </Card>
  );
};

export default AuditLogs;

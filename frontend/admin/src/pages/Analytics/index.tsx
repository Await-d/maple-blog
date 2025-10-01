import React, { useMemo } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { Card, Tabs, Typography } from 'antd';

const { Title, Text } = Typography;

const tabItems = [
  {
    key: '/analytics/overview',
    label: '概览',
    description: '总体访问、趋势与实时指标',
  },
  {
    key: '/analytics/content',
    label: '内容分析',
    description: '内容表现、互动与渠道对比',
  },
  {
    key: '/analytics/users',
    label: '用户分析',
    description: '用户活跃度、留存与画像',
  },
];

const Analytics: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();

  const activeKey = useMemo(() => {
    const matchedTab = tabItems.find((item) => location.pathname.startsWith(item.key));
    return matchedTab?.key ?? '/analytics/overview';
  }, [location.pathname]);

  return (
    <div>
      <div className="page-header">
        <div>
          <Title level={2} className="page-title">
            数据分析
          </Title>
          <Text className="page-description">
            聚焦访问趋势、内容表现与用户行为洞察
          </Text>
        </div>
      </div>

      <Card className="page-container" bordered={false}>
        <Tabs
          defaultActiveKey="/analytics/overview"
          activeKey={activeKey}
          onChange={(key) => navigate(key)}
          items={tabItems.map((item) => ({
            key: item.key,
            label: item.label,
          }))}
        />
        <div className="mt-6">
          <Outlet />
        </div>
      </Card>
    </div>
  );
};

export default Analytics;

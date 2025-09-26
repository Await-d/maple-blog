import React from 'react';
import { Card, Row, Col, Typography, Space, Button } from 'antd';
import {
  UserOutlined,
  FileTextOutlined,
  EyeOutlined,
  MessageOutlined,
  ArrowUpOutlined,
  ArrowDownOutlined,
  BarChartOutlined,
  SettingOutlined,
} from '@ant-design/icons';

const { Title, Text } = Typography;

const Dashboard: React.FC = () => {
  // 模拟数据
  const stats = [
    {
      title: '总用户数',
      value: 1234,
      prefix: <UserOutlined />,
      suffix: '人',
      trend: 12.5,
      isPositive: true,
      color: '#1890ff',
    },
    {
      title: '文章总数',
      value: 567,
      prefix: <FileTextOutlined />,
      suffix: '篇',
      trend: 8.2,
      isPositive: true,
      color: '#52c41a',
    },
    {
      title: '总浏览量',
      value: 89012,
      prefix: <EyeOutlined />,
      suffix: '次',
      trend: -2.3,
      isPositive: false,
      color: '#faad14',
    },
    {
      title: '评论总数',
      value: 345,
      prefix: <MessageOutlined />,
      suffix: '条',
      trend: 15.8,
      isPositive: true,
      color: '#ff4d4f',
    },
  ];

  return (
    <div>
      <div className="page-header">
        <div>
          <Title level={2} className="page-title">
            仪表盘
          </Title>
          <Text className="page-description">
            欢迎回来！这里是您的数据概览
          </Text>
        </div>
        <Space>
          <Button type="primary">刷新数据</Button>
          <Button>导出报告</Button>
        </Space>
      </div>

      {/* 统计卡片 */}
      <Row gutter={[24, 24]} style={{ marginBottom: 24 }}>
        {stats.map((stat, index) => (
          <Col xs={24} sm={12} lg={6} key={index}>
            <Card
              className="stat-card"
              style={{
                background: stat.color,
                borderRadius: 12,
                border: 'none',
              }}
            >
              <Space direction="vertical" size={0} style={{ width: '100%' }}>
                <div
                  style={{
                    fontSize: 24,
                    color: 'rgba(255, 255, 255, 0.8)',
                  }}
                >
                  {stat.prefix}
                </div>
                <Text
                  style={{
                    color: 'rgba(255, 255, 255, 0.9)',
                    fontSize: 14,
                  }}
                >
                  {stat.title}
                </Text>
                <Text
                  style={{
                    color: '#fff',
                    fontSize: 28,
                    fontWeight: 'bold',
                    lineHeight: 1,
                  }}
                >
                  {stat.value.toLocaleString()}
                  <span style={{ fontSize: 14, marginLeft: 4 }}>
                    {stat.suffix}
                  </span>
                </Text>
                <div
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    color: 'rgba(255, 255, 255, 0.8)',
                    fontSize: 12,
                  }}
                >
                  {stat.isPositive ? (
                    <ArrowUpOutlined style={{ marginRight: 4 }} />
                  ) : (
                    <ArrowDownOutlined style={{ marginRight: 4 }} />
                  )}
                  {Math.abs(stat.trend)}% 比上月
                </div>
              </Space>
            </Card>
          </Col>
        ))}
      </Row>

      {/* 图表区域 */}
      <Row gutter={[24, 24]}>
        <Col xs={24} lg={16}>
          <Card
            title="访问趋势"
            extra={<Button type="link">查看详情</Button>}
            className="chart-container"
          >
            <div
              style={{
                height: 300,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                background: '#fafafa',
                borderRadius: 8,
                color: '#999',
              }}
            >
              图表组件将在后续实现
            </div>
          </Card>
        </Col>

        <Col xs={24} lg={8}>
          <Card
            title="最新动态"
            extra={<Button type="link">查看全部</Button>}
            className="chart-container"
          >
            <div style={{ height: 300 }}>
              <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                {[1, 2, 3, 4, 5].map((item) => (
                  <div
                    key={item}
                    style={{
                      padding: '12px 0',
                      borderBottom: '1px solid #f0f0f0',
                    }}
                  >
                    <div
                      style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'flex-start',
                      }}
                    >
                      <div style={{ flex: 1 }}>
                        <Text strong>用户 {item} 发表了新文章</Text>
                        <br />
                        <Text type="secondary" style={{ fontSize: 12 }}>
                          2小时前
                        </Text>
                      </div>
                    </div>
                  </div>
                ))}
              </Space>
            </div>
          </Card>
        </Col>
      </Row>

      {/* 快捷操作 */}
      <Card
        title="快捷操作"
        style={{ marginTop: 24 }}
        className="chart-container"
      >
        <Row gutter={[16, 16]}>
          <Col xs={24} sm={12} md={6}>
            <Button
              type="primary"
              size="large"
              icon={<FileTextOutlined />}
              block
            >
              创建文章
            </Button>
          </Col>
          <Col xs={24} sm={12} md={6}>
            <Button
              size="large"
              icon={<UserOutlined />}
              block
            >
              用户管理
            </Button>
          </Col>
          <Col xs={24} sm={12} md={6}>
            <Button
              size="large"
              icon={<BarChartOutlined />}
              block
            >
              数据分析
            </Button>
          </Col>
          <Col xs={24} sm={12} md={6}>
            <Button
              size="large"
              icon={<SettingOutlined />}
              block
            >
              系统设置
            </Button>
          </Col>
        </Row>
      </Card>
    </div>
  );
};

export default Dashboard;
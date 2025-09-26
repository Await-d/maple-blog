import React, { useState, useCallback } from 'react';
import {
  Row,
  Col,
  Card,
  Space,
  Button,
  Dropdown,
  Switch,
  InputNumber,
  Alert,
  Tag,
  Avatar,
  List,
  Typography,
  Progress,
  Divider,
} from 'antd';
import {
  ReloadOutlined,
  SettingOutlined,
  FullscreenOutlined,
  UserOutlined,
  FileTextOutlined,
  EyeOutlined,
  CommentOutlined,
  ClockCircleOutlined,
  CheckCircleOutlined,
  ExclamationCircleOutlined,
  CloseCircleOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import { StatCardVariants, StatCardFormatters } from '@/components/charts/StatCard';
import LineChart from '@/components/charts/LineChart';
import { useDashboard, useDashboardConfig } from '@/hooks/useDashboard';
import type { Activity, HealthCheck } from '@/types';

const { Text, Title } = Typography;

// Dashboard layout configurations
const DASHBOARD_LAYOUTS = {
  default: {
    statsCards: {
      xs: 24,
      sm: 12,
      md: 6,
      lg: 6,
      xl: 6,
    },
    charts: {
      xs: 24,
      sm: 24,
      md: 12,
      lg: 12,
      xl: 8,
    },
    activity: {
      xs: 24,
      sm: 24,
      md: 24,
      lg: 24,
      xl: 8,
    },
  },
  compact: {
    statsCards: {
      xs: 24,
      sm: 12,
      md: 8,
      lg: 6,
      xl: 4,
    },
    charts: {
      xs: 24,
      sm: 24,
      md: 12,
      lg: 8,
      xl: 8,
    },
    activity: {
      xs: 24,
      sm: 24,
      md: 12,
      lg: 8,
      xl: 8,
    },
  },
};

const Dashboard: React.FC = () => {
  const [currentLayout, setCurrentLayout] = useState<'default' | 'compact'>('default');
  // Removed unused state variables: isFullscreen, setIsFullscreen, hasPermission

  const {
    stats,
    metrics,
    healthCheck,
    activities,
    isLoading,
    isRefreshing,
    hasErrors,
    isConnected,
    refreshAll,
    exportData,
    getChartData,
    getTrendData,
  } = useDashboard();

  const {
    autoRefresh,
    refreshInterval,
    updateRefreshSettings,
  } = useDashboardConfig();

  const layout = DASHBOARD_LAYOUTS[currentLayout];

  // Handle refresh settings change
  const handleRefreshSettingsChange = useCallback((key: string, value: unknown) => {
    updateRefreshSettings({ [key]: value });
  }, [updateRefreshSettings]);

  // Handle export
  const handleExport = useCallback((format: 'json' | 'csv' | 'excel') => {
    exportData(format);
  }, [exportData]);

  // Render dashboard toolbar
  const renderToolbar = () => {
    const settingsMenu = {
      items: [
        {
          key: 'auto-refresh',
          label: (
            <div className="flex items-center justify-between w-48">
              <span>自动刷新</span>
              <Switch
                
                checked={autoRefresh}
                onChange={(checked) => handleRefreshSettingsChange('autoRefresh', checked)}
              />
            </div>
          ),
        },
        {
          key: 'refresh-interval',
          label: (
            <div className="flex items-center justify-between w-48">
              <span>刷新间隔(秒)</span>
              <InputNumber
                
                min={5}
                max={300}
                value={refreshInterval}
                onChange={(value) => handleRefreshSettingsChange('refreshInterval', value)}
                style={{ width: 80 }}
              />
            </div>
          ),
        },
        { type: 'divider' },
        {
          key: 'layout-default',
          label: '默认布局',
          onClick: () => setCurrentLayout('default'),
        },
        {
          key: 'layout-compact',
          label: '紧凑布局',
          onClick: () => setCurrentLayout('compact'),
        },
        { type: 'divider' },
        {
          key: 'export-json',
          label: '导出 JSON',
          onClick: () => handleExport('json'),
        },
        {
          key: 'export-csv',
          label: '导出 CSV',
          onClick: () => handleExport('csv'),
        },
        {
          key: 'export-excel',
          label: '导出 Excel',
          onClick: () => handleExport('excel'),
        },
      ],
    };

    return (
      <div className="flex items-center justify-between mb-6">
        <div>
          <Title level={2} className="mb-0">
            管理仪表盘
          </Title>
          <Text type="secondary">
            实时监控系统状态和关键指标
            {!isConnected && (
              <Tag color="orange" className="ml-2">
                离线模式
              </Tag>
            )}
          </Text>
        </div>

        <Space>
          <Button
            type="text"
            icon={<ReloadOutlined />}
            loading={isRefreshing}
            onClick={refreshAll}
          >
            刷新
          </Button>

          <Dropdown menu={settingsMenu} placement="bottomRight">
            <Button type="text" icon={<SettingOutlined />}>
              设置
            </Button>
          </Dropdown>

          <Button
            type="text"
            icon={<FullscreenOutlined />}
            onClick={() => {
              if (document.fullscreenElement) {
                document.exitFullscreen();
              } else {
                document.documentElement.requestFullscreen();
              }
            }}
          >
            全屏
          </Button>
        </Space>
      </div>
    );
  };

  // Render system health status
  const renderHealthStatus = () => {
    if (!healthCheck) return null;

    const getStatusConfig = (status: HealthCheck['status']) => {
      switch (status) {
        case 'healthy':
          return { color: 'success', icon: <CheckCircleOutlined />, text: '健康' };
        case 'degraded':
          return { color: 'warning', icon: <ExclamationCircleOutlined />, text: '降级' };
        case 'unhealthy':
          return { color: 'error', icon: <CloseCircleOutlined />, text: '异常' };
        default:
          return { color: 'default', icon: <ClockCircleOutlined />, text: '未知' };
      }
    };

    const statusConfig = getStatusConfig(healthCheck.status);

    return (
      <Alert
        type={statusConfig.color as 'success' | 'info' | 'warning' | 'error'}
        showIcon
        icon={statusConfig.icon}
        message={`系统状态：${statusConfig.text}`}
        description={
          <div className="mt-2">
            <Text type="secondary">
              检查时间：{dayjs(healthCheck.timestamp).format('YYYY-MM-DD HH:mm:ss')}
            </Text>
            <div className="mt-2 grid grid-cols-2 md:grid-cols-4 gap-2">
              {Object.entries(healthCheck.checks).map(([key, check]) => (
                <div key={key} className="flex items-center gap-2">
                  <div
                    className={`w-2 h-2 rounded-full ${
                      check.status === 'pass'
                        ? 'bg-green-500'
                        : check.status === 'warn'
                        ? 'bg-yellow-500'
                        : 'bg-red-500'
                    }`}
                  />
                  <Text style={{ fontSize: 12 }}>{key}</Text>
                </div>
              ))}
            </div>
          </div>
        }
        className="mb-6"
      />
    );
  };

  // Render statistics cards
  const renderStatsCards = () => {
    if (!stats) return null;

    const trendData = getTrendData();

    const cards = [
      {
        Component: StatCardVariants.UserStats,
        props: {
          title: '用户统计',
          value: stats.userStats.total,
          formatter: StatCardFormatters.number,
          trend: {
            value: trendData?.userTrend || 0,
            label: '较昨日',
          },
          tooltip: `活跃用户：${stats.userStats.active}，今日新增：${stats.userStats.newToday}`,
        },
      },
      {
        Component: StatCardVariants.ContentStats,
        props: {
          title: '内容统计',
          value: stats.contentStats.publishedPosts,
          suffix: '篇',
          trend: {
            value: trendData?.contentTrend || 0,
            label: '较昨日',
          },
          tooltip: `草稿：${stats.contentStats.drafts}，今日新增：${stats.contentStats.postsToday}`,
        },
      },
      {
        Component: StatCardVariants.SystemStats,
        props: {
          title: '总浏览量',
          value: stats.systemStats.viewsTotal,
          formatter: StatCardFormatters.number,
          tooltip: `今日浏览：${stats.systemStats.viewsToday}`,
        },
      },
      {
        Component: StatCardVariants.PerformanceStats,
        props: {
          title: '性能评分',
          value: stats.systemStats.performanceScore,
          precision: 1,
          suffix: '分',
          tooltip: '系统综合性能评分',
        },
      },
    ];

    return (
      <Row gutter={[16, 16]} className="mb-6">
        {cards.map((card, index) => (
          <Col key={index} {...layout.statsCards}>
            <card.Component
              {...card.props}
              loading={isLoading}
              size={currentLayout === 'compact' ? 'small' : 'default'}
            />
          </Col>
        ))}
      </Row>
    );
  };

  // Render charts section
  const renderCharts = () => {
    const charts = [
      {
        title: '流量趋势',
        type: 'traffic' as const,
        description: '网站访问量变化趋势',
      },
      {
        title: '系统性能',
        type: 'performance' as const,
        description: '系统资源使用情况',
      },
      {
        title: '用户增长',
        type: 'users' as const,
        description: '用户注册和活跃趋势',
      },
    ];

    return (
      <Row gutter={[16, 16]} className="mb-6">
        {charts.map((chart) => (
          <Col key={chart.type} {...layout.charts}>
            <LineChart
              title={chart.title}
              data={getChartData(chart.type)}
              loading={isLoading}
              height={currentLayout === 'compact' ? 250 : 300}
              onRefresh={refreshAll}
              showToolbar={true}
            />
          </Col>
        ))}
      </Row>
    );
  };

  // Render activity feed
  const renderActivityFeed = () => {
    if (!activities) return null;

    const getActivityIcon = (type: Activity['type']) => {
      switch (type) {
        case 'user_login':
        case 'user_register':
          return <UserOutlined className="text-blue-500" />;
        case 'post_create':
        case 'post_update':
        case 'post_delete':
          return <FileTextOutlined className="text-green-500" />;
        case 'comment_create':
          return <CommentOutlined className="text-orange-500" />;
        default:
          return <ClockCircleOutlined className="text-gray-500" />;
      }
    };

    return (
      <Col {...layout.activity}>
        <Card
          title="最近活动"
          bordered={false}
          className="h-full"
          extra={
            <Button type="text"  icon={<EyeOutlined />}>
              查看全部
            </Button>
          }
        >
          <List
            loading={isLoading}
            dataSource={activities.slice(0, 10)}
            renderItem={(activity) => (
              <List.Item>
                <List.Item.Meta
                  avatar={
                    <Avatar
                      
                      icon={getActivityIcon(activity.type)}
                      src={activity.user.avatar}
                    />
                  }
                  title={
                    <div className="flex items-center justify-between">
                      <Text>{activity.description}</Text>
                      <Text type="secondary" style={{ fontSize: 12 }}>
                        {dayjs(activity.createdAt).fromNow()}
                      </Text>
                    </div>
                  }
                  description={
                    <Text type="secondary" style={{ fontSize: 12 }}>
                      {activity.user.displayName || activity.user.username}
                    </Text>
                  }
                />
              </List.Item>
            )}
          />
        </Card>
      </Col>
    );
  };

  // Render system metrics
  const renderSystemMetrics = () => {
    if (!metrics) return null;

    return (
      <Row gutter={[16, 16]}>
        <Col span={24}>
          <Card title="系统监控" bordered={false}>
            <Row gutter={[16, 16]}>
              <Col xs={24} sm={12} md={6}>
                <div className="text-center">
                  <Progress
                    type="circle"
                    percent={metrics.cpu.usage}
                    size={80}
                    status={metrics.cpu.usage > 80 ? 'exception' : 'normal'}
                  />
                  <div className="mt-2">
                    <Text strong>CPU 使用率</Text>
                    <br />
                    <Text type="secondary">{metrics.cpu.cores} 核心</Text>
                  </div>
                </div>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <div className="text-center">
                  <Progress
                    type="circle"
                    percent={metrics.memory.usage}
                    size={80}
                    status={metrics.memory.usage > 80 ? 'exception' : 'normal'}
                  />
                  <div className="mt-2">
                    <Text strong>内存使用率</Text>
                    <br />
                    <Text type="secondary">
                      {StatCardFormatters.fileSize(metrics.memory.used)} /
                      {StatCardFormatters.fileSize(metrics.memory.total)}
                    </Text>
                  </div>
                </div>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <div className="text-center">
                  <Progress
                    type="circle"
                    percent={metrics.disk.usage}
                    size={80}
                    status={metrics.disk.usage > 90 ? 'exception' : 'normal'}
                  />
                  <div className="mt-2">
                    <Text strong>磁盘使用率</Text>
                    <br />
                    <Text type="secondary">
                      {StatCardFormatters.fileSize(metrics.disk.used)} /
                      {StatCardFormatters.fileSize(metrics.disk.total)}
                    </Text>
                  </div>
                </div>
              </Col>
              <Col xs={24} sm={12} md={6}>
                <div className="text-center">
                  <div className="mb-4">
                    <Text strong display="block">应用状态</Text>
                    <Text type="secondary">
                      运行时间：{StatCardFormatters.duration(metrics.application.uptime)}
                    </Text>
                  </div>
                  <Space direction="vertical" >
                    <div>
                      <Text>请求数：{StatCardFormatters.number(metrics.application.requestCount)}</Text>
                    </div>
                    <div>
                      <Text>错误数：{metrics.application.errorCount}</Text>
                    </div>
                    <div>
                      <Text>响应时间：{metrics.application.responseTime}ms</Text>
                    </div>
                  </Space>
                </div>
              </Col>
            </Row>
          </Card>
        </Col>
      </Row>
    );
  };

  // Handle errors
  if (hasErrors) {
    return (
      <div className="p-6">
        <Alert
          type="error"
          message="数据加载失败"
          description="无法获取仪表盘数据，请检查网络连接或稍后重试。"
          showIcon
          action={
            <Button  danger onClick={refreshAll}>
              重试
            </Button>
          }
        />
      </div>
    );
  }

  return (
    <div className="dashboard-page">
      {renderToolbar()}
      {renderHealthStatus()}
      {renderStatsCards()}
      {renderCharts()}
      <Row gutter={[16, 16]}>
        {renderActivityFeed()}
      </Row>
      <Divider />
      {renderSystemMetrics()}
    </div>
  );
};

export default Dashboard;
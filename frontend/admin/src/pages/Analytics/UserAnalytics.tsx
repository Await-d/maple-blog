import React, { useState } from 'react';
import { Row, Col, Card, Table, Tag, Space, DatePicker, Select, Button, Tabs, Statistic, Avatar, Typography, Progress, Timeline, List, Divider, Badge, Segmented, Alert } from 'antd';
import {
  TeamOutlined,
  UserAddOutlined,
  UserSwitchOutlined,
  FireOutlined,
  RiseOutlined,
  FallOutlined,
  ExportOutlined,
  EnvironmentOutlined
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import dayjs from 'dayjs';
import AnalyticsChart from '@/components/analytics/AnalyticsChart';
import { analyticsService } from '@/services/analytics.service';
import type {
  UserFlow,
  UserSegment,
  UserActivity,
  TimeRange
} from '@/types/analytics';

const { Title, Text, Paragraph } = Typography;
const { RangePicker } = DatePicker;
const { TabPane } = Tabs;

const UserAnalytics: React.FC = () => {
  const [timeRange, setTimeRange] = useState<TimeRange>('month');
  const [customDateRange, setCustomDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs]>([
    dayjs().subtract(30, 'days'),
    dayjs()
  ]);
  const [segmentType] = useState<'all' | 'new' | 'returning' | 'engaged'>('all');
  const [viewMode, setViewMode] = useState<'overview' | 'detailed' | 'cohorts'>('overview');

  // Fetch user behavior data
  const { data: userBehavior } = useQuery({
    queryKey: ['user-behavior', timeRange, customDateRange, segmentType],
    queryFn: () => analyticsService.getUserBehavior({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined,
      segment: segmentType === 'all' ? undefined : segmentType
    })
  });

  // Fetch user flow data
  const { data: userFlow, isLoading: flowLoading } = useQuery({
    queryKey: ['user-flow', timeRange, customDateRange],
    queryFn: () => analyticsService.getUserFlow({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Fetch user segments
  const { data: userSegments, isLoading: segmentsLoading } = useQuery({
    queryKey: ['user-segments', timeRange, customDateRange],
    queryFn: () => analyticsService.getUserSegments({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Fetch user activity patterns
  const { data: userActivity, isLoading: activityLoading } = useQuery({
    queryKey: ['user-activity', timeRange, customDateRange],
    queryFn: () => analyticsService.getUserActivity({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Fetch cohort data
  const { isLoading: cohortLoading } = useQuery({
    queryKey: ['user-cohorts', timeRange, customDateRange],
    queryFn: () => analyticsService.getUserCohorts({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Fetch retention data
  const { isLoading: retentionLoading } = useQuery({
    queryKey: ['user-retention', timeRange, customDateRange],
    queryFn: () => analyticsService.getUserRetention({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Fetch conversion funnel
  const { data: funnelData, isLoading: funnelLoading } = useQuery({
    queryKey: ['conversion-funnel', timeRange, customDateRange],
    queryFn: () => analyticsService.getConversionFunnel('user-journey', {
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Fetch geographic distribution
  const { data: geoData, isLoading: geoLoading } = useQuery({
    queryKey: ['user-geographic', timeRange, customDateRange],
    queryFn: () => analyticsService.getGeographicData({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Calculate user metrics
  const totalUsers = userBehavior?.find(b => b.metric === 'Total Users')?.value || 0;
  const newUsers = userBehavior?.find(b => b.metric === 'New Users')?.value || 0;
  const returningUsers = userBehavior?.find(b => b.metric === 'Returning Users')?.value || 0;
  const activeUsers = userBehavior?.find(b => b.metric === 'Active Users')?.value || 0;

  // Prepare chart data
  const segmentChartData = {
    items: userSegments?.map(segment => ({
      name: segment.name,
      value: segment.count,
      percentage: segment.percentage
    })) || []
  };

  const activityHeatmapData = {
    xAxis: ['00', '01', '02', '03', '04', '05', '06', '07', '08', '09', '10', '11',
             '12', '13', '14', '15', '16', '17', '18', '19', '20', '21', '22', '23'],
    yAxis: ['周一', '周二', '周三', '周四', '周五', '周六', '周日'],
    values: userActivity?.flatMap((activity, dayIndex) => {
      const days = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday'];
      return Array.from({ length: 24 }, (_, hourIndex) => {
        const dayKey = days[dayIndex];
        const value = activity[dayKey as keyof UserActivity] || 0;
        return [hourIndex, dayIndex, value];
      });
    }) || [],
    min: 0,
    max: 100
  };

  const funnelChartData = {
    items: funnelData?.map((stage, _index) => ({
      name: stage.stage,
      value: stage.conversionRate,
      users: stage.users,
      dropoff: stage.dropoff
    })) || []
  };

  const userFlowChartData = {
    categories: userFlow?.map(flow => flow.page) || [],
    series: [
      {
        name: '进入',
        data: userFlow?.map(flow => flow.entries) || [],
        color: '#52c41a'
      },
      {
        name: '退出',
        data: userFlow?.map(flow => flow.exits) || [],
        color: '#f5222d'
      },
      {
        name: '转化',
        data: userFlow?.map(flow => flow.conversions) || [],
        color: '#1890ff'
      }
    ]
  };

  // User segments table columns
  const segmentColumns = [
    {
      title: '用户群体',
      dataIndex: 'name',
      key: 'name',
      render: (name: string) => <Text strong>{name}</Text>
    },
    {
      title: '用户数',
      dataIndex: 'count',
      key: 'count',
      align: 'right' as const,
      render: (count: number) => count.toLocaleString()
    },
    {
      title: '占比',
      dataIndex: 'percentage',
      key: 'percentage',
      align: 'center' as const,
      render: (percentage: number) => (
        <Progress percent={percentage}  strokeColor="#1890ff" />
      )
    },
    {
      title: '平均年龄',
      key: 'avgAge',
      align: 'center' as const,
      render: (record: UserSegment) => record.characteristics.avgAge || '-'
    },
    {
      title: '平均会话数',
      key: 'avgSessions',
      align: 'right' as const,
      render: (record: UserSegment) =>
        record.characteristics.avgSessionsPerUser?.toFixed(2) || '-'
    },
    {
      title: '平均价值',
      key: 'avgValue',
      align: 'right' as const,
      render: (record: UserSegment) =>
        record.characteristics.avgValuePerUser ?
          `¥${record.characteristics.avgValuePerUser.toFixed(2)}` : '-'
    },
    {
      title: '趋势',
      dataIndex: 'trend',
      key: 'trend',
      align: 'center' as const,
      render: (trend: number) => (
        <Text type={trend > 0 ? 'success' : 'danger'}>
          {trend > 0 ? <RiseOutlined /> : <FallOutlined />}
          {Math.abs(trend).toFixed(1)}%
        </Text>
      )
    }
  ];

  // User flow table columns
  const flowColumns = [
    {
      title: '页面',
      dataIndex: 'page',
      key: 'page',
      render: (page: string) => <Text strong>{page}</Text>
    },
    {
      title: '进入次数',
      dataIndex: 'entries',
      key: 'entries',
      align: 'right' as const,
      render: (entries: number) => entries.toLocaleString()
    },
    {
      title: '退出次数',
      dataIndex: 'exits',
      key: 'exits',
      align: 'right' as const,
      render: (exits: number) => exits.toLocaleString()
    },
    {
      title: '流失率',
      dataIndex: 'dropoffs',
      key: 'dropoffs',
      align: 'center' as const,
      render: (dropoffs: number, record: UserFlow) => {
        const rate = record.entries > 0 ? (dropoffs / record.entries) * 100 : 0;
        return (
          <Text type={rate > 50 ? 'danger' : rate > 30 ? 'warning' : 'success'}>
            {rate.toFixed(1)}%
          </Text>
        );
      }
    },
    {
      title: '转化次数',
      dataIndex: 'conversions',
      key: 'conversions',
      align: 'right' as const,
      render: (conversions: number) => conversions.toLocaleString()
    },
    {
      title: '平均停留',
      dataIndex: 'avgTime',
      key: 'avgTime',
      align: 'right' as const,
      render: (time: number) => {
        const minutes = Math.floor(time / 60);
        const seconds = time % 60;
        return `${minutes}:${seconds.toString().padStart(2, '0')}`;
      }
    }
  ];

  // Handle export
  const handleExport = () => {
    analyticsService.exportData({
      format: 'excel',
      dateRange: {
        start: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : dayjs().subtract(30, 'days').format('YYYY-MM-DD'),
        end: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : dayjs().format('YYYY-MM-DD')
      },
      metrics: ['users', 'sessions', 'behavior', 'segments', 'retention'],
      dimensions: ['date', 'segment', 'device', 'location']
    }).then(blob => {
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `user-analytics-${dayjs().format('YYYY-MM-DD')}.xlsx`;
      link.click();
    });
  };

  return (
    <div className="user-analytics">
      {/* Header */}
      <div style={{ marginBottom: 24 }}>
        <Row justify="space-between" align="middle">
          <Col>
            <Title level={2} style={{ margin: 0 }}>
              用户分析
            </Title>
          </Col>
          <Col>
            <Space>
              <Segmented
                value={viewMode}
                onChange={setViewMode}
                options={[
                  { label: '概览', value: 'overview' },
                  { label: '详细', value: 'detailed' },
                  { label: '同期群', value: 'cohorts' }
                ]}
              />
              <Select
                value={timeRange}
                onChange={setTimeRange}
                style={{ width: 120 }}
                options={analyticsService.getTimeRangePresets()}
              />
              {timeRange === 'custom' && (
                <RangePicker
                  value={customDateRange}
                  onChange={(dates) => dates && setCustomDateRange(dates as [dayjs.Dayjs, dayjs.Dayjs])}
                />
              )}
              <Button icon={<ExportOutlined />} onClick={handleExport}>
                导出报表
              </Button>
            </Space>
          </Col>
        </Row>
      </div>

      {/* User Metrics Overview */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="总用户数"
              value={totalUsers}
              prefix={<TeamOutlined />}
              valueStyle={{ color: '#1890ff' }}
            />
            <div style={{ marginTop: 16 }}>
              <Text type="secondary">
                较上期 {' '}
                <Text type={userBehavior?.[0]?.change > 0 ? 'success' : 'danger'}>
                  {userBehavior?.[0]?.change > 0 ? '+' : ''}{userBehavior?.[0]?.change?.toFixed(1)}%
                </Text>
              </Text>
            </div>
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="新用户"
              value={newUsers}
              prefix={<UserAddOutlined />}
              valueStyle={{ color: '#52c41a' }}
            />
            <Progress
              percent={(newUsers / totalUsers) * 100}
              
              strokeColor="#52c41a"
              format={() => `${((newUsers / totalUsers) * 100).toFixed(1)}%`}
              style={{ marginTop: 16 }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="回访用户"
              value={returningUsers}
              prefix={<UserSwitchOutlined />}
              valueStyle={{ color: '#722ed1' }}
            />
            <Progress
              percent={(returningUsers / totalUsers) * 100}
              
              strokeColor="#722ed1"
              format={() => `${((returningUsers / totalUsers) * 100).toFixed(1)}%`}
              style={{ marginTop: 16 }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="活跃用户"
              value={activeUsers}
              prefix={<FireOutlined />}
              valueStyle={{ color: '#fa8c16' }}
            />
            <Badge
              status={activeUsers > 1000 ? 'processing' : 'default'}
              text={activeUsers > 1000 ? '高活跃度' : '正常活跃度'}
              style={{ marginTop: 16 }}
            />
          </Card>
        </Col>
      </Row>

      {/* Main Content */}
      <Tabs defaultActiveKey="behavior">
        <TabPane tab="用户行为" key="behavior">
          <Row gutter={[16, 16]}>
            {/* User Flow */}
            <Col span={24}>
              <Card title="用户流程分析" extra={<Button type="link">查看详情</Button>}>
                <AnalyticsChart
                  type="bar"
                  data={userFlowChartData}
                  height={350}
                  loading={flowLoading}
                />
              </Card>
            </Col>

            {/* Conversion Funnel */}
            <Col xs={24} lg={12}>
              <AnalyticsChart
                type="funnel"
                title="转化漏斗"
                subtitle="用户转化路径分析"
                data={funnelChartData}
                height={400}
                loading={funnelLoading}
              />
            </Col>

            {/* User Activity Heatmap */}
            <Col xs={24} lg={12}>
              <AnalyticsChart
                type="heatmap"
                title="用户活跃度热力图"
                subtitle="按时间和星期分布"
                data={activityHeatmapData}
                height={400}
                loading={activityLoading}
              />
            </Col>

            {/* User Flow Table */}
            <Col span={24}>
              <Card title="页面流转详情">
                <Table
                  columns={flowColumns}
                  dataSource={userFlow}
                  rowKey="page"
                  loading={flowLoading}
                  pagination={{
                    showSizeChanger: true,
                    showTotal: (total) => `共 ${total} 个页面`
                  }}
                />
              </Card>
            </Col>
          </Row>
        </TabPane>

        <TabPane tab="用户群体" key="segments">
          <Row gutter={[16, 16]}>
            {/* Segment Distribution */}
            <Col xs={24} lg={12}>
              <AnalyticsChart
                type="pie"
                title="用户群体分布"
                data={segmentChartData}
                height={400}
                loading={segmentsLoading}
              />
            </Col>

            {/* Segment Details */}
            <Col xs={24} lg={12}>
              <Card title="群体特征分析" loading={segmentsLoading}>
                <List
                  dataSource={userSegments?.slice(0, 5)}
                  renderItem={(segment) => (
                    <List.Item>
                      <List.Item.Meta
                        avatar={
                          <Avatar style={{ backgroundColor: '#1890ff' }}>
                            {segment.count > 1000 ? 'VIP' : segment.name.charAt(0)}
                          </Avatar>
                        }
                        title={segment.name}
                        description={
                          <Space direction="vertical" size={0}>
                            <Text type="secondary">
                              用户数: {segment.count.toLocaleString()} ({segment.percentage}%)
                            </Text>
                            <Space >
                              {segment.characteristics.topInterests?.slice(0, 3).map((interest, idx) => (
                                <Tag key={idx} color="blue">{interest}</Tag>
                              ))}
                            </Space>
                          </Space>
                        }
                      />
                      <div>
                        <Statistic
                          value={segment.characteristics.avgValuePerUser || 0}
                          prefix="¥"
                          precision={2}
                          valueStyle={{ fontSize: 16 }}
                        />
                      </div>
                    </List.Item>
                  )}
                />
              </Card>
            </Col>

            {/* Segments Table */}
            <Col span={24}>
              <Card title="用户群体详细数据">
                <Table
                  columns={segmentColumns}
                  dataSource={userSegments}
                  rowKey="name"
                  loading={segmentsLoading}
                  pagination={{
                    showSizeChanger: true,
                    showTotal: (total) => `共 ${total} 个群体`
                  }}
                />
              </Card>
            </Col>
          </Row>
        </TabPane>

        <TabPane tab="留存分析" key="retention">
          <Row gutter={[16, 16]}>
            {viewMode === 'cohorts' ? (
              <>
                {/* Cohort Analysis */}
                <Col span={24}>
                  <Card title="同期群分析" loading={cohortLoading}>
                    <Alert
                      message="同期群分析"
                      description="通过分析不同时间段注册的用户群体的行为模式，了解用户留存和生命周期价值"
                      type="info"
                      showIcon
                      style={{ marginBottom: 16 }}
                    />
                    <div style={{ overflowX: 'auto' }}>
                      <table style={{ width: '100%', minWidth: 800 }}>
                        <thead>
                          <tr style={{ backgroundColor: '#fafafa' }}>
                            <th style={{ padding: 12, textAlign: 'left' }}>注册时间</th>
                            <th style={{ padding: 12, textAlign: 'center' }}>用户数</th>
                            <th style={{ padding: 12, textAlign: 'center' }}>第1天</th>
                            <th style={{ padding: 12, textAlign: 'center' }}>第7天</th>
                            <th style={{ padding: 12, textAlign: 'center' }}>第14天</th>
                            <th style={{ padding: 12, textAlign: 'center' }}>第30天</th>
                            <th style={{ padding: 12, textAlign: 'center' }}>第60天</th>
                            <th style={{ padding: 12, textAlign: 'center' }}>第90天</th>
                          </tr>
                        </thead>
                        <tbody>
                          {[0, 1, 2, 3, 4, 5].map((week) => {
                            const baseRetention = 100 - week * 10;
                            return (
                              <tr key={week} style={{ borderBottom: '1px solid #f0f0f0' }}>
                                <td style={{ padding: 12 }}>
                                  {dayjs().subtract(week, 'week').format('YYYY-MM-DD')} 周
                                </td>
                                <td style={{ padding: 12, textAlign: 'center' }}>
                                  {(1000 - week * 100).toLocaleString()}
                                </td>
                                <td style={{ padding: 12, textAlign: 'center' }}>
                                  <Badge
                                    count={`${baseRetention}%`}
                                    style={{ backgroundColor: '#52c41a' }}
                                  />
                                </td>
                                <td style={{ padding: 12, textAlign: 'center' }}>
                                  <Badge
                                    count={`${Math.max(20, baseRetention - 20)}%`}
                                    style={{ backgroundColor: '#52c41a' }}
                                  />
                                </td>
                                <td style={{ padding: 12, textAlign: 'center' }}>
                                  <Badge
                                    count={`${Math.max(15, baseRetention - 35)}%`}
                                    style={{ backgroundColor: '#faad14' }}
                                  />
                                </td>
                                <td style={{ padding: 12, textAlign: 'center' }}>
                                  <Badge
                                    count={`${Math.max(10, baseRetention - 50)}%`}
                                    style={{ backgroundColor: '#faad14' }}
                                  />
                                </td>
                                <td style={{ padding: 12, textAlign: 'center' }}>
                                  <Badge
                                    count={`${Math.max(8, baseRetention - 60)}%`}
                                    style={{ backgroundColor: '#f5222d' }}
                                  />
                                </td>
                                <td style={{ padding: 12, textAlign: 'center' }}>
                                  <Badge
                                    count={`${Math.max(5, baseRetention - 70)}%`}
                                    style={{ backgroundColor: '#f5222d' }}
                                  />
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </table>
                    </div>
                  </Card>
                </Col>

                {/* Retention Curve */}
                <Col xs={24} lg={12}>
                  <AnalyticsChart
                    type="line"
                    title="留存率曲线"
                    subtitle="不同时间段的用户留存率变化"
                    data={{
                      categories: ['第1天', '第3天', '第7天', '第14天', '第30天', '第60天', '第90天'],
                      series: [
                        {
                          name: '本月注册',
                          data: [100, 75, 55, 40, 30, 20, 15],
                          showArea: true
                        },
                        {
                          name: '上月注册',
                          data: [100, 70, 50, 35, 25, 18, 12],
                          showArea: true
                        }
                      ]
                    }}
                    height={350}
                    loading={retentionLoading}
                  />
                </Col>

                {/* LTV Analysis */}
                <Col xs={24} lg={12}>
                  <Card title="用户生命周期价值 (LTV)" loading={retentionLoading}>
                    <Row gutter={[16, 16]}>
                      <Col span={12}>
                        <Statistic
                          title="30天 LTV"
                          value={580}
                          prefix="¥"
                          valueStyle={{ color: '#1890ff' }}
                        />
                      </Col>
                      <Col span={12}>
                        <Statistic
                          title="90天 LTV"
                          value={1280}
                          prefix="¥"
                          valueStyle={{ color: '#52c41a' }}
                        />
                      </Col>
                      <Col span={12}>
                        <Statistic
                          title="180天 LTV"
                          value={2150}
                          prefix="¥"
                          valueStyle={{ color: '#722ed1' }}
                        />
                      </Col>
                      <Col span={12}>
                        <Statistic
                          title="预测年度 LTV"
                          value={3800}
                          prefix="¥"
                          valueStyle={{ color: '#fa8c16' }}
                        />
                      </Col>
                    </Row>
                    <Divider />
                    <Timeline>
                      <Timeline.Item color="green">
                        新用户注册 - 平均获客成本 ¥50
                      </Timeline.Item>
                      <Timeline.Item color="blue">
                        首次购买 - 转化率 25%
                      </Timeline.Item>
                      <Timeline.Item color="orange">
                        复购用户 - 复购率 45%
                      </Timeline.Item>
                      <Timeline.Item color="red">
                        流失用户 - 90天流失率 85%
                      </Timeline.Item>
                    </Timeline>
                  </Card>
                </Col>
              </>
            ) : (
              <>
                {/* Standard Retention Metrics */}
                <Col xs={24} lg={12}>
                  <Card title="留存率指标">
                    <Row gutter={[16, 16]}>
                      <Col span={8}>
                        <Statistic
                          title="次日留存"
                          value={68.5}
                          suffix="%"
                          valueStyle={{ color: '#52c41a' }}
                        />
                      </Col>
                      <Col span={8}>
                        <Statistic
                          title="7日留存"
                          value={42.3}
                          suffix="%"
                          valueStyle={{ color: '#faad14' }}
                        />
                      </Col>
                      <Col span={8}>
                        <Statistic
                          title="30日留存"
                          value={25.8}
                          suffix="%"
                          valueStyle={{ color: '#f5222d' }}
                        />
                      </Col>
                    </Row>
                    <Divider />
                    <Paragraph type="secondary">
                      留存率反映了用户对产品的黏性和满意度。提高留存率是实现可持续增长的关键。
                    </Paragraph>
                  </Card>
                </Col>

                {/* Churn Analysis */}
                <Col xs={24} lg={12}>
                  <Card title="流失分析">
                    <List
                      dataSource={[
                        { reason: '功能不满足需求', percentage: 35, color: '#f5222d' },
                        { reason: '使用体验不佳', percentage: 28, color: '#fa8c16' },
                        { reason: '竞品更优', percentage: 20, color: '#faad14' },
                        { reason: '价格因素', percentage: 12, color: '#1890ff' },
                        { reason: '其他原因', percentage: 5, color: '#8c8c8c' }
                      ]}
                      renderItem={(item) => (
                        <List.Item>
                          <div style={{ width: '100%' }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                              <Text>{item.reason}</Text>
                              <Text strong>{item.percentage}%</Text>
                            </div>
                            <Progress
                              percent={item.percentage}
                              strokeColor={item.color}
                              showInfo={false}
                            />
                          </div>
                        </List.Item>
                      )}
                    />
                  </Card>
                </Col>
              </>
            )}
          </Row>
        </TabPane>

        <TabPane tab="地理分布" key="geographic">
          <Row gutter={[16, 16]}>
            {/* Geographic Map */}
            <Col span={24}>
              <Card title="用户地理分布" extra={<Button type="link">查看地图</Button>}>
                <Alert
                  message="地理分布分析"
                  description="了解用户的地理位置分布，帮助优化区域性运营策略"
                  type="info"
                  showIcon
                  style={{ marginBottom: 16 }}
                />
                <Row gutter={[16, 16]}>
                  {geoData?.slice(0, 12).map((geo, index) => (
                    <Col xs={12} sm={8} md={6} lg={4} key={index}>
                      <Card  hoverable>
                        <Statistic
                          title={
                            <Space>
                              <EnvironmentOutlined />
                              {geo.country}
                            </Space>
                          }
                          value={geo.visits}
                          valueStyle={{ fontSize: 16 }}
                        />
                        <Progress
                          percent={geo.percentage}
                          
                          strokeColor="#1890ff"
                          showInfo={false}
                        />
                        <div style={{ marginTop: 8 }}>
                          <Text type="secondary" style={{ fontSize: 12 }}>
                            跳出率: {geo.bounceRate.toFixed(1)}%
                          </Text>
                        </div>
                      </Card>
                    </Col>
                  ))}
                </Row>
              </Card>
            </Col>

            {/* Top Cities */}
            <Col xs={24} lg={12}>
              <Card title="TOP 城市">
                <List
                  dataSource={geoData?.slice(0, 10)}
                  renderItem={(item, index) => (
                    <List.Item>
                      <List.Item.Meta
                        avatar={
                          <Avatar style={{ backgroundColor: '#1890ff' }}>
                            {index + 1}
                          </Avatar>
                        }
                        title={`${item.country}${item.region ? ` - ${item.region}` : ''}`}
                        description={
                          <Space>
                            <Text type="secondary">访问: {item.visits.toLocaleString()}</Text>
                            <Text type="secondary">停留: {Math.floor(item.avgDuration / 60)}分钟</Text>
                          </Space>
                        }
                      />
                      <Text strong>{item.percentage.toFixed(1)}%</Text>
                    </List.Item>
                  )}
                />
              </Card>
            </Col>

            {/* Device by Region */}
            <Col xs={24} lg={12}>
              <AnalyticsChart
                type="bar"
                title="地区设备分布"
                data={{
                  categories: geoData?.slice(0, 5).map(g => g.country) || [],
                  series: [
                    {
                      name: '移动设备',
                      data: geoData?.slice(0, 5).map(() => Math.floor(Math.random() * 1000)) || []
                    },
                    {
                      name: '桌面设备',
                      data: geoData?.slice(0, 5).map(() => Math.floor(Math.random() * 800)) || []
                    },
                    {
                      name: '平板设备',
                      data: geoData?.slice(0, 5).map(() => Math.floor(Math.random() * 300)) || []
                    }
                  ]
                }}
                height={350}
                loading={geoLoading}
              />
            </Col>
          </Row>
        </TabPane>
      </Tabs>
    </div>
  );
};

export default UserAnalytics;
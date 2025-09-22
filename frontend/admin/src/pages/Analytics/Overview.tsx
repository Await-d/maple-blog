// @ts-nocheck
import React, { useState, useEffect } from 'react';
import { Row, Col, Card, Statistic, Space, DatePicker, Select, Button, Segmented, Typography, Tooltip, Badge, Progress, Divider } from 'antd';
import {
  EyeOutlined,
  UserOutlined,
  FileTextOutlined,
  RiseOutlined,
  FallOutlined,
  GlobalOutlined,
  MobileOutlined,
  DesktopOutlined,
  TabletOutlined,
  ClockCircleOutlined,
  ThunderboltOutlined,
  DashboardOutlined,
  BarChartOutlined,
  LineChartOutlined,
  PieChartOutlined,
  AreaChartOutlined,
  RadarChartOutlined,
  HeatMapOutlined,
  ReloadOutlined,
  ExportOutlined,
  SettingOutlined
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import dayjs from 'dayjs';
import AnalyticsChart from '@/components/analytics/AnalyticsChart';
import { analyticsService } from '@/services/analytics.service';
import type { AnalyticsOverview, TimeRange, TrafficData, TrafficSource, DeviceAnalytics, GeographicData, RealTimeData } from '@/types/analytics';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;

const Overview: React.FC = () => {
  const [timeRange, setTimeRange] = useState<TimeRange>('month');
  const [customDateRange, setCustomDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs]>([
    dayjs().subtract(30, 'days'),
    dayjs()
  ]);
  const [viewMode, setViewMode] = useState<'overview' | 'detailed'>('overview');
  const [chartType, setChartType] = useState<'line' | 'bar' | 'area'>('line');

  // Fetch overview data
  const { data: overviewData, isLoading: overviewLoading, refetch: refetchOverview } = useQuery({
    queryKey: ['analytics-overview', timeRange, customDateRange],
    queryFn: () => analyticsService.getOverview({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Fetch traffic data
  const { data: trafficData, isLoading: trafficLoading } = useQuery({
    queryKey: ['analytics-traffic', timeRange, customDateRange],
    queryFn: () => analyticsService.getTrafficData({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Fetch traffic sources
  const { data: trafficSources, isLoading: sourcesLoading } = useQuery({
    queryKey: ['analytics-sources', timeRange, customDateRange],
    queryFn: () => analyticsService.getTrafficSources({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Fetch device analytics
  const { data: deviceData, isLoading: deviceLoading } = useQuery({
    queryKey: ['analytics-devices', timeRange, customDateRange],
    queryFn: () => analyticsService.getDeviceAnalytics({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Fetch geographic data
  const { data: geoData, isLoading: geoLoading } = useQuery({
    queryKey: ['analytics-geographic', timeRange, customDateRange],
    queryFn: () => analyticsService.getGeographicData({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Fetch real-time data
  const { data: realTimeData, isLoading: realTimeLoading } = useQuery({
    queryKey: ['analytics-realtime'],
    queryFn: () => analyticsService.getRealTimeData(),
    refetchInterval: 30000 // Refresh every 30 seconds
  });

  // Format metric card data
  const metricCards = [
    {
      title: '总访问量',
      value: overviewData?.metrics.totalVisits || 0,
      trend: overviewData?.trends.visitsChange || 0,
      icon: <EyeOutlined />,
      color: '#1890ff',
      suffix: '次'
    },
    {
      title: '独立访客',
      value: overviewData?.metrics.uniqueVisitors || 0,
      trend: overviewData?.trends.visitorsChange || 0,
      icon: <UserOutlined />,
      color: '#52c41a',
      suffix: '人'
    },
    {
      title: '页面浏览量',
      value: overviewData?.metrics.pageViews || 0,
      trend: overviewData?.trends.pageViewsChange || 0,
      icon: <FileTextOutlined />,
      color: '#722ed1',
      suffix: '次'
    },
    {
      title: '平均停留时长',
      value: Math.floor((overviewData?.metrics.avgSessionDuration || 0) / 60),
      trend: overviewData?.trends.durationChange || 0,
      icon: <ClockCircleOutlined />,
      color: '#fa8c16',
      suffix: '分钟',
      precision: 1
    },
    {
      title: '跳出率',
      value: overviewData?.metrics.bounceRate || 0,
      trend: -(overviewData?.trends.pageViewsChange || 0), // Lower is better
      icon: <ThunderboltOutlined />,
      color: '#f5222d',
      suffix: '%',
      precision: 2,
      invertTrend: true
    },
    {
      title: '转化率',
      value: overviewData?.metrics.conversionRate || 0,
      trend: overviewData?.trends.pageViewsChange || 0,
      icon: <DashboardOutlined />,
      color: '#13c2c2',
      suffix: '%',
      precision: 2
    }
  ];

  // Prepare chart data
  const trafficChartData = {
    categories: trafficData?.map(d => dayjs(d.date).format('MM-DD')) || [],
    series: [
      {
        name: '访问量',
        data: trafficData?.map(d => d.visits) || [],
        color: '#1890ff'
      },
      {
        name: '独立访客',
        data: trafficData?.map(d => d.uniqueVisitors) || [],
        color: '#52c41a'
      },
      {
        name: '页面浏览量',
        data: trafficData?.map(d => d.pageViews) || [],
        color: '#722ed1'
      }
    ]
  };

  const sourceChartData = {
    items: trafficSources?.map(source => ({
      name: source.source,
      value: source.visits,
      itemStyle: { color: source.color }
    })) || []
  };

  const deviceChartData = {
    categories: deviceData?.map(d => d.device) || [],
    series: [{
      name: '设备分布',
      data: deviceData?.map(d => d.sessions) || []
    }]
  };

  const geoChartData = {
    items: geoData?.slice(0, 10).map(geo => ({
      name: geo.country,
      value: geo.visits
    })) || []
  };

  // Handle export
  const handleExport = () => {
    analyticsService.exportData({
      format: 'excel',
      dateRange: {
        start: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : dayjs().subtract(30, 'days').format('YYYY-MM-DD'),
        end: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : dayjs().format('YYYY-MM-DD')
      },
      metrics: ['visits', 'visitors', 'pageViews', 'bounceRate', 'avgDuration', 'conversionRate'],
      dimensions: ['date', 'source', 'device', 'country']
    }).then(blob => {
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `analytics-overview-${dayjs().format('YYYY-MM-DD')}.xlsx`;
      link.click();
    });
  };

  return (
    <div className="analytics-overview">
      {/* Header */}
      <div style={{ marginBottom: 24 }}>
        <Row justify="space-between" align="middle">
          <Col>
            <Title level={2} style={{ margin: 0 }}>
              数据分析总览
            </Title>
          </Col>
          <Col>
            <Space size="middle">
              <Segmented
                value={viewMode}
                onChange={setViewMode}
                options={[
                  { label: '概览模式', value: 'overview' },
                  { label: '详细模式', value: 'detailed' }
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
              <Button icon={<ReloadOutlined />} onClick={() => refetchOverview()}>
                刷新
              </Button>
              <Button icon={<ExportOutlined />} onClick={handleExport}>
                导出报表
              </Button>
              <Button icon={<SettingOutlined />} type="text">
                设置
              </Button>
            </Space>
          </Col>
        </Row>
      </div>

      {/* Real-time Stats Banner */}
      {realTimeData && (
        <Card style={{ marginBottom: 24, background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)' }}>
          <Row gutter={24} align="middle">
            <Col flex="none">
              <Badge status="processing" text={
                <Text style={{ color: '#fff', fontSize: 16, fontWeight: 'bold' }}>
                  实时数据
                </Text>
              } />
            </Col>
            <Col flex="auto">
              <Row gutter={32}>
                <Col>
                  <Statistic
                    title={<Text style={{ color: 'rgba(255,255,255,0.8)' }}>在线用户</Text>}
                    value={realTimeData.activeUsers}
                    valueStyle={{ color: '#fff', fontSize: 24 }}
                    suffix="人"
                  />
                </Col>
                <Col>
                  <Statistic
                    title={<Text style={{ color: 'rgba(255,255,255,0.8)' }}>活跃会话</Text>}
                    value={realTimeData.activeSessions}
                    valueStyle={{ color: '#fff', fontSize: 24 }}
                    suffix="个"
                  />
                </Col>
                <Col>
                  <Statistic
                    title={<Text style={{ color: 'rgba(255,255,255,0.8)' }}>每分钟页面浏览</Text>}
                    value={realTimeData.pageViewsPerMinute}
                    valueStyle={{ color: '#fff', fontSize: 24 }}
                    suffix="次"
                  />
                </Col>
              </Row>
            </Col>
            <Col flex="none">
              <Text style={{ color: 'rgba(255,255,255,0.8)', fontSize: 12 }}>
                最后更新: {dayjs().format('HH:mm:ss')}
              </Text>
            </Col>
          </Row>
        </Card>
      )}

      {/* Key Metrics */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        {metricCards.map((metric, index) => (
          <Col xs={24} sm={12} md={8} lg={4} key={index}>
            <Card hoverable>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 16 }}>
                <div style={{
                  width: 48,
                  height: 48,
                  borderRadius: 8,
                  background: `${metric.color}20`,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: 24,
                  color: metric.color
                }}>
                  {metric.icon}
                </div>
                <div style={{ textAlign: 'right' }}>
                  <div style={{
                    color: metric.invertTrend
                      ? (metric.trend > 0 ? '#f5222d' : '#52c41a')
                      : (metric.trend > 0 ? '#52c41a' : '#f5222d'),
                    fontSize: 14,
                    fontWeight: 'bold'
                  }}>
                    {metric.trend > 0 ? <RiseOutlined /> : <FallOutlined />}
                    {' '}
                    {Math.abs(metric.trend).toFixed(1)}%
                  </div>
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    较上期
                  </Text>
                </div>
              </div>
              <Statistic
                title={metric.title}
                value={metric.value}
                precision={metric.precision}
                suffix={metric.suffix}
                valueStyle={{ fontSize: 28, fontWeight: 'bold' }}
              />
            </Card>
          </Col>
        ))}
      </Row>

      {/* Charts Section */}
      <Row gutter={[16, 16]}>
        {/* Traffic Trend */}
        <Col xs={24} lg={16}>
          <AnalyticsChart
            type={chartType}
            title="流量趋势"
            subtitle="访问量、独立访客、页面浏览量变化趋势"
            data={trafficChartData}
            height={400}
            loading={trafficLoading}
            onRefresh={() => refetchOverview()}
          />
        </Col>

        {/* Traffic Sources */}
        <Col xs={24} lg={8}>
          <AnalyticsChart
            type="pie"
            title="流量来源"
            subtitle="各渠道流量占比"
            data={sourceChartData}
            height={400}
            loading={sourcesLoading}
          />
        </Col>

        {/* Device Distribution */}
        <Col xs={24} lg={12}>
          <AnalyticsChart
            type="bar"
            title="设备分布"
            subtitle="不同设备类型的访问情况"
            data={deviceChartData}
            height={350}
            loading={deviceLoading}
          />
        </Col>

        {/* Geographic Distribution */}
        <Col xs={24} lg={12}>
          <AnalyticsChart
            type="bar"
            title="地理分布 TOP10"
            subtitle="访问量最多的国家/地区"
            data={geoChartData}
            height={350}
            loading={geoLoading}
            customOptions={{
              xAxis: { type: 'value' },
              yAxis: { type: 'category', data: geoChartData.items?.map(i => i.name) || [] },
              series: [{
                type: 'bar',
                data: geoChartData.items?.map(i => i.value) || [],
                orientation: 'horizontal'
              }]
            }}
          />
        </Col>

        {/* Performance Metrics */}
        {viewMode === 'detailed' && (
          <>
            <Col xs={24} lg={12}>
              <Card title="页面性能指标" extra={<Button type="link">查看详情</Button>}>
                <Row gutter={[16, 16]}>
                  <Col span={12}>
                    <Text type="secondary">首次内容绘制 (FCP)</Text>
                    <div style={{ marginTop: 8 }}>
                      <Progress percent={85} strokeColor="#52c41a" format={() => '1.2s'} />
                    </div>
                  </Col>
                  <Col span={12}>
                    <Text type="secondary">最大内容绘制 (LCP)</Text>
                    <div style={{ marginTop: 8 }}>
                      <Progress percent={75} strokeColor="#faad14" format={() => '2.5s'} />
                    </div>
                  </Col>
                  <Col span={12}>
                    <Text type="secondary">首次输入延迟 (FID)</Text>
                    <div style={{ marginTop: 8 }}>
                      <Progress percent={95} strokeColor="#52c41a" format={() => '50ms'} />
                    </div>
                  </Col>
                  <Col span={12}>
                    <Text type="secondary">累积布局偏移 (CLS)</Text>
                    <div style={{ marginTop: 8 }}>
                      <Progress percent={90} strokeColor="#52c41a" format={() => '0.05'} />
                    </div>
                  </Col>
                </Row>
              </Card>
            </Col>

            <Col xs={24} lg={12}>
              <Card title="用户行为洞察" extra={<Button type="link">查看详情</Button>}>
                <Row gutter={[16, 16]}>
                  <Col span={8}>
                    <Statistic
                      title="新访客占比"
                      value={68.5}
                      precision={1}
                      suffix="%"
                      valueStyle={{ color: '#1890ff' }}
                    />
                  </Col>
                  <Col span={8}>
                    <Statistic
                      title="回访率"
                      value={31.5}
                      precision={1}
                      suffix="%"
                      valueStyle={{ color: '#52c41a' }}
                    />
                  </Col>
                  <Col span={8}>
                    <Statistic
                      title="平均页面深度"
                      value={4.2}
                      precision={1}
                      suffix="页"
                      valueStyle={{ color: '#722ed1' }}
                    />
                  </Col>
                </Row>
                <Divider />
                <div>
                  <Text type="secondary">用户参与度评分</Text>
                  <Progress
                    percent={78}
                    strokeColor={{
                      '0%': '#108ee9',
                      '100%': '#87d068'
                    }}
                    style={{ marginTop: 8 }}
                  />
                </div>
              </Card>
            </Col>
          </>
        )}
      </Row>

      {/* Top Content Performance */}
      {viewMode === 'detailed' && (
        <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
          <Col span={24}>
            <Card
              title="热门内容表现"
              extra={
                <Space>
                  <Select defaultValue="views" style={{ width: 120 }}>
                    <Select.Option value="views">按浏览量</Select.Option>
                    <Select.Option value="engagement">按互动率</Select.Option>
                    <Select.Option value="duration">按停留时长</Select.Option>
                  </Select>
                  <Button type="link">查看全部</Button>
                </Space>
              }
            >
              <div style={{ overflowX: 'auto' }}>
                <table style={{ width: '100%', minWidth: 600 }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid #f0f0f0' }}>
                      <th style={{ padding: '12px', textAlign: 'left' }}>页面标题</th>
                      <th style={{ padding: '12px', textAlign: 'right' }}>浏览量</th>
                      <th style={{ padding: '12px', textAlign: 'right' }}>独立访客</th>
                      <th style={{ padding: '12px', textAlign: 'right' }}>平均停留时长</th>
                      <th style={{ padding: '12px', textAlign: 'right' }}>跳出率</th>
                      <th style={{ padding: '12px', textAlign: 'right' }}>趋势</th>
                    </tr>
                  </thead>
                  <tbody>
                    {[1, 2, 3, 4, 5].map(i => (
                      <tr key={i} style={{ borderBottom: '1px solid #f0f0f0' }}>
                        <td style={{ padding: '12px' }}>
                          <Text>示例文章标题 {i}</Text>
                        </td>
                        <td style={{ padding: '12px', textAlign: 'right' }}>
                          <Text strong>{(10000 - i * 1500).toLocaleString()}</Text>
                        </td>
                        <td style={{ padding: '12px', textAlign: 'right' }}>
                          <Text>{(8000 - i * 1200).toLocaleString()}</Text>
                        </td>
                        <td style={{ padding: '12px', textAlign: 'right' }}>
                          <Text>{3 + i * 0.5}:20</Text>
                        </td>
                        <td style={{ padding: '12px', textAlign: 'right' }}>
                          <Text>{(20 + i * 5).toFixed(1)}%</Text>
                        </td>
                        <td style={{ padding: '12px', textAlign: 'right' }}>
                          <Text type={i % 2 === 0 ? 'success' : 'danger'}>
                            {i % 2 === 0 ? <RiseOutlined /> : <FallOutlined />}
                            {' '}
                            {(10 - i * 2).toFixed(1)}%
                          </Text>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </Card>
          </Col>
        </Row>
      )}
    </div>
  );
};

export default Overview;
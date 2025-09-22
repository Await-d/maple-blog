// @ts-nocheck
import React, { useState, useEffect } from 'react';
import {
  Card,
  Row,
  Col,
  Typography,
  Space,
  Button,
  Tabs,
  Select,
  Input,
  DatePicker,
  Switch,
  Badge,
  Tag,
  Statistic,
  Alert,
  Modal,
  Table,
  List,
  Tooltip,
  Dropdown,
  Menu,
  notification,
  Progress,
  Timeline,
  Empty,
} from 'antd';
import {
  FileTextOutlined,
  SearchOutlined,
  FilterOutlined,
  DownloadOutlined,
  ReloadOutlined,
  SettingOutlined,
  ClearOutlined,
  PlayCircleOutlined,
  PauseCircleOutlined,
  FullscreenOutlined,
  EyeOutlined,
  WarningOutlined,
  BugOutlined,
  InfoCircleOutlined,
  CloseCircleOutlined,
  CheckCircleOutlined,
  CloudDownloadOutlined,
  BarChartOutlined,
  PieChartOutlined,
  LineChartOutlined,
  DatabaseOutlined,
  ApiOutlined,
  SecurityScanOutlined,
  ThunderboltOutlined,
  UserOutlined,
  ClockCircleOutlined,
} from '@ant-design/icons';
import LogViewer from '../../components/logs/LogViewer';
import PieChart from '../../components/charts/PieChart';
import BarChart from '../../components/charts/BarChart';
import LineChart from '../../components/charts/LineChart';
import dayjs from 'dayjs';

const { Title, Text, Paragraph } = Typography;
const { TabPane } = Tabs;
const { Option } = Select;
const { RangePicker } = DatePicker;
const { Search } = Input;

interface LogAnalytics {
  totalLogs: number;
  errorRate: number;
  topSources: Array<{ name: string; count: number; percentage: number }>;
  logLevels: Array<{ level: string; count: number; color: string }>;
  hourlyDistribution: Array<{ hour: string; count: number }>;
  recentErrors: Array<{
    id: string;
    timestamp: string;
    level: string;
    source: string;
    message: string;
  }>;
}

interface LogExportConfig {
  format: 'json' | 'csv' | 'txt';
  levels: string[];
  sources: string[];
  dateRange: [string, string] | null;
  maxEntries: number;
}

const SystemLogs: React.FC = () => {
  const [activeTab, setActiveTab] = useState('viewer');
  const [loading, setLoading] = useState(false);
  const [streaming, setStreaming] = useState(true);
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [refreshInterval, setRefreshInterval] = useState(30);
  
  // Modals
  const [exportModalVisible, setExportModalVisible] = useState(false);
  const [analyticsModalVisible, setAnalyticsModalVisible] = useState(false);
  const [settingsModalVisible, setSettingsModalVisible] = useState(false);

  // Analytics data
  const [analytics, setAnalytics] = useState<LogAnalytics>({
    totalLogs: 15423,
    errorRate: 2.8,
    topSources: [
      { name: 'MapleBlog.API', count: 5432, percentage: 35.2 },
      { name: 'Authentication', count: 3421, percentage: 22.2 },
      { name: 'Database', count: 2856, percentage: 18.5 },
      { name: 'Cache', count: 1923, percentage: 12.5 },
      { name: 'BackgroundService', count: 1791, percentage: 11.6 },
    ],
    logLevels: [
      { level: 'Info', count: 8945, color: '#1890ff' },
      { level: 'Warning', count: 3421, color: '#faad14' },
      { level: 'Error', count: 2134, color: '#ff4d4f' },
      { level: 'Debug', count: 823, color: '#722ed1' },
      { level: 'Fatal', count: 100, color: '#a8071a' },
    ],
    hourlyDistribution: Array.from({ length: 24 }, (_, i) => ({
      hour: i.toString().padStart(2, '0') + ':00',
      count: Math.floor(Math.random() * 500) + 100 + Math.sin(i / 4) * 200,
    })),
    recentErrors: [
      {
        id: '1',
        timestamp: dayjs().subtract(5, 'minute').toISOString(),
        level: 'error',
        source: 'Database',
        message: 'Connection timeout after 30 seconds',
      },
      {
        id: '2',
        timestamp: dayjs().subtract(12, 'minute').toISOString(),
        level: 'error',
        source: 'MapleBlog.API',
        message: 'Validation failed for user input',
      },
      {
        id: '3',
        timestamp: dayjs().subtract(25, 'minute').toISOString(),
        level: 'fatal',
        source: 'Authentication',
        message: 'JWT validation service unavailable',
      },
      {
        id: '4',
        timestamp: dayjs().subtract(38, 'minute').toISOString(),
        level: 'error',
        source: 'Cache',
        message: 'Redis connection lost',
      },
      {
        id: '5',
        timestamp: dayjs().subtract(45, 'minute').toISOString(),
        level: 'error',
        source: 'MapleBlog.API',
        message: 'File upload failed - storage quota exceeded',
      },
    ],
  });

  // Export configuration
  const [exportConfig, setExportConfig] = useState<LogExportConfig>({
    format: 'json',
    levels: ['info', 'warn', 'error', 'fatal'],
    sources: [],
    dateRange: null,
    maxEntries: 10000,
  });

  // Log viewer settings
  const [logViewerSettings, setLogViewerSettings] = useState({
    realTime: true,
    autoRefresh: true,
    refreshInterval: 5000,
    maxEntries: 10000,
    height: 600,
  });

  // Auto-refresh analytics
  useEffect(() => {
    if (!autoRefresh) return;

    const interval = setInterval(() => {
      refreshAnalytics();
    }, refreshInterval * 1000);

    return () => clearInterval(interval);
  }, [autoRefresh, refreshInterval]);

  // Refresh analytics data
  const refreshAnalytics = async () => {
    setLoading(true);
    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      // Update analytics with some variation
      setAnalytics(prev => ({
        ...prev,
        totalLogs: prev.totalLogs + Math.floor(Math.random() * 50),
        errorRate: Math.max(0, prev.errorRate + (Math.random() - 0.5) * 0.5),
        hourlyDistribution: prev.hourlyDistribution.map(item => ({
          ...item,
          count: Math.max(0, item.count + Math.floor((Math.random() - 0.5) * 50)),
        })),
      }));

      notification.success({
        message: 'Analytics Updated',
        description: 'Log analytics data has been refreshed',
        duration: 2,
      });
    } catch (error) {
      console.error('Failed to refresh analytics:', error);
      notification.error({
        message: 'Refresh Failed',
        description: 'Unable to refresh analytics data',
      });
    } finally {
      setLoading(false);
    }
  };

  // Clear all logs
  const clearAllLogs = () => {
    Modal.confirm({
      title: 'Clear All Logs',
      content: 'Are you sure you want to clear all log entries? This action cannot be undone and will affect system monitoring.',
      okText: 'Clear All',
      okType: 'danger',
      onOk: () => {
        notification.success({
          message: 'Logs Cleared',
          description: 'All log entries have been cleared successfully',
        });
      },
    });
  };

  // Archive logs
  const archiveLogs = () => {
    Modal.confirm({
      title: 'Archive Logs',
      content: 'Archive logs older than 30 days? Archived logs will be moved to long-term storage.',
      onOk: async () => {
        try {
          // Simulate archive operation
          await new Promise(resolve => setTimeout(resolve, 2000));
          notification.success({
            message: 'Logs Archived',
            description: 'Older logs have been successfully archived',
          });
        } catch (error) {
          notification.error({
            message: 'Archive Failed',
            description: 'Failed to archive logs. Please try again.',
          });
        }
      },
    });
  };

  // Handle log export
  const handleLogExport = (entries: any[], format: string) => {
    const timestamp = dayjs().format('YYYY-MM-DD_HH-mm-ss');
    let content = '';
    let mimeType = 'text/plain';
    let extension = 'txt';

    switch (format) {
      case 'json':
        content = JSON.stringify(entries, null, 2);
        mimeType = 'application/json';
        extension = 'json';
        break;
      case 'csv':
        const headers = ['Timestamp', 'Level', 'Source', 'Message', 'User ID', 'Request ID'];
        const csvRows = [
          headers.join(','),
          ...entries.map(entry => [
            entry.timestamp,
            entry.level,
            entry.source,
            `"${entry.message.replace(/"/g, '""')}"`,
            entry.userId || '',
            entry.requestId || '',
          ].join(','))
        ];
        content = csvRows.join('\n');
        mimeType = 'text/csv';
        extension = 'csv';
        break;
      default:
        content = entries.map(entry => 
          `${entry.timestamp} [${entry.level.toUpperCase()}] ${entry.source}: ${entry.message}`
        ).join('\n');
        break;
    }

    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `logs_export_${timestamp}.${extension}`;
    link.click();
    URL.revokeObjectURL(url);

    notification.success({
      message: 'Export Complete',
      description: `${entries.length} log entries exported as ${format.toUpperCase()}`,
    });
  };

  // Get level icon
  const getLevelIcon = (level: string) => {
    switch (level.toLowerCase()) {
      case 'error':
      case 'fatal':
        return <CloseCircleOutlined style={{ color: '#ff4d4f' }} />;
      case 'warning':
      case 'warn':
        return <WarningOutlined style={{ color: '#faad14' }} />;
      case 'info':
        return <InfoCircleOutlined style={{ color: '#1890ff' }} />;
      case 'debug':
        return <BugOutlined style={{ color: '#722ed1' }} />;
      default:
        return <InfoCircleOutlined style={{ color: '#d9d9d9' }} />;
    }
  };

  // Get level color
  const getLevelColor = (level: string) => {
    switch (level.toLowerCase()) {
      case 'error':
      case 'fatal':
        return '#ff4d4f';
      case 'warning':
      case 'warn':
        return '#faad14';
      case 'info':
        return '#1890ff';
      case 'debug':
        return '#722ed1';
      default:
        return '#d9d9d9';
    }
  };

  return (
    <div className="system-logs">
      {/* Header */}
      <div style={{ marginBottom: 24 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <div>
            <Title level={2}>
              <Space>
                <FileTextOutlined />
                System Logs
              </Space>
            </Title>
            <Paragraph type="secondary">
              View, search, and analyze system logs in real-time. Monitor application behavior and troubleshoot issues.
            </Paragraph>
          </div>
          <Space>
            <Badge
              status={streaming ? 'processing' : 'default'}
              text={streaming ? 'Live' : 'Paused'}
            />
            <Switch
              checked={autoRefresh}
              onChange={setAutoRefresh}
              checkedChildren="Auto"
              unCheckedChildren="Manual"
            />
            <Select
              value={refreshInterval}
              onChange={setRefreshInterval}
              style={{ width: 120 }}
              disabled={!autoRefresh}
            >
              <Option value={10}>10s</Option>
              <Option value={30}>30s</Option>
              <Option value={60}>1m</Option>
              <Option value={300}>5m</Option>
            </Select>
            <Button
              icon={<ReloadOutlined />}
              loading={loading}
              onClick={refreshAnalytics}
            >
              Refresh
            </Button>
          </Space>
        </div>

        {/* Quick Stats */}
        <Row gutter={[16, 8]} style={{ marginTop: 16 }}>
          <Col span={6}>
            <Card >
              <Statistic
                title="Total Logs"
                value={analytics.totalLogs}
                prefix={<FileTextOutlined />}
                valueStyle={{ color: '#1890ff' }}
              />
            </Card>
          </Col>
          <Col span={6}>
            <Card >
              <Statistic
                title="Error Rate"
                value={analytics.errorRate}
                suffix="%"
                prefix={<WarningOutlined />}
                valueStyle={{ color: analytics.errorRate > 5 ? '#ff4d4f' : '#52c41a' }}
              />
            </Card>
          </Col>
          <Col span={6}>
            <Card >
              <Statistic
                title="Recent Errors"
                value={analytics.recentErrors.length}
                prefix={<CloseCircleOutlined />}
                valueStyle={{ color: analytics.recentErrors.length > 3 ? '#ff4d4f' : '#52c41a' }}
              />
            </Card>
          </Col>
          <Col span={6}>
            <Card >
              <Statistic
                title="Log Sources"
                value={analytics.topSources.length}
                prefix={<DatabaseOutlined />}
                valueStyle={{ color: '#722ed1' }}
              />
            </Card>
          </Col>
        </Row>
      </div>

      {/* Main Content */}
      <Tabs activeKey={activeTab} onChange={setActiveTab} type="card">
        {/* Log Viewer Tab */}
        <TabPane
          tab={
            <Space>
              <EyeOutlined />
              Log Viewer
              {streaming && <Badge status="processing" />}
            </Space>
          }
          key="viewer"
        >
          <Card
            title="Real-time Log Stream"
            extra={
              <Space>
                <Switch
                  checked={streaming}
                  onChange={setStreaming}
                  checkedChildren={<PlayCircleOutlined />}
                  unCheckedChildren={<PauseCircleOutlined />}
                />
                <Button
                  icon={<SettingOutlined />}
                  onClick={() => setSettingsModalVisible(true)}
                >
                  Settings
                </Button>
                <Dropdown
                  overlay={
                    <Menu>
                      <Menu.Item
                        key="export"
                        icon={<DownloadOutlined />}
                        onClick={() => setExportModalVisible(true)}
                      >
                        Export Logs
                      </Menu.Item>
                      <Menu.Item
                        key="clear"
                        icon={<ClearOutlined />}
                        onClick={clearAllLogs}
                      >
                        Clear All Logs
                      </Menu.Item>
                      <Menu.Item
                        key="archive"
                        icon={<CloudDownloadOutlined />}
                        onClick={archiveLogs}
                      >
                        Archive Old Logs
                      </Menu.Item>
                    </Menu>
                  }
                >
                  <Button icon={<SettingOutlined />}>
                    Actions
                  </Button>
                </Dropdown>
              </Space>
            }
            bodyStyle={{ padding: 0 }}
          >
            <LogViewer
              height={logViewerSettings.height}
              realTime={logViewerSettings.realTime}
              autoRefresh={logViewerSettings.autoRefresh}
              refreshInterval={logViewerSettings.refreshInterval}
              maxEntries={logViewerSettings.maxEntries}
              onExport={handleLogExport}
            />
          </Card>
        </TabPane>

        {/* Analytics Tab */}
        <TabPane
          tab={
            <Space>
              <BarChartOutlined />
              Analytics
            </Space>
          }
          key="analytics"
        >
          <Row gutter={[16, 16]}>
            {/* Log Level Distribution */}
            <Col span={12}>
              <Card title="Log Level Distribution" >
                <PieChart
                  data={analytics.logLevels.map(item => ({
                    name: item.level,
                    value: item.count,
                    color: item.color,
                  }))}
                  height={300}
                />
                <List
                  
                  dataSource={analytics.logLevels}
                  renderItem={(item) => (
                    <List.Item>
                      <Space style={{ width: '100%', justifyContent: 'space-between' }}>
                        <Space>
                          {getLevelIcon(item.level)}
                          <Text>{item.level}</Text>
                        </Space>
                        <Text strong>{item.count.toLocaleString()}</Text>
                      </Space>
                    </List.Item>
                  )}
                />
              </Card>
            </Col>

            {/* Top Log Sources */}
            <Col span={12}>
              <Card title="Top Log Sources" >
                <List
                  dataSource={analytics.topSources}
                  renderItem={(item) => (
                    <List.Item>
                      <div style={{ width: '100%' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                          <Text strong>{item.name}</Text>
                          <Text>{item.count.toLocaleString()} ({item.percentage}%)</Text>
                        </div>
                        <Progress
                          percent={item.percentage}
                          showInfo={false}
                          strokeColor={`hsl(${200 + item.percentage * 2}, 70%, 50%)`}
                        />
                      </div>
                    </List.Item>
                  )}
                />
              </Card>
            </Col>

            {/* Hourly Distribution */}
            <Col span={24}>
              <Card title="Hourly Log Distribution (Last 24 Hours)" >
                <BarChart
                  data={analytics.hourlyDistribution.map(item => ({
                    name: item.hour,
                    value: item.count,
                  }))}
                  height={300}
                  color="#1890ff"
                />
              </Card>
            </Col>

            {/* Recent Errors */}
            <Col span={24}>
              <Card title="Recent Errors" >
                <Timeline>
                  {analytics.recentErrors.map((error) => (
                    <Timeline.Item
                      key={error.id}
                      dot={getLevelIcon(error.level)}
                      color={getLevelColor(error.level)}
                    >
                      <div>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Space>
                            <Tag color={getLevelColor(error.level)}>
                              {error.level.toUpperCase()}
                            </Tag>
                            <Text strong>{error.source}</Text>
                          </Space>
                          <Text type="secondary" style={{ fontSize: '12px' }}>
                            {dayjs(error.timestamp).format('MM-DD HH:mm:ss')}
                          </Text>
                        </div>
                        <Paragraph style={{ margin: '4px 0 0 0', fontSize: '13px' }}>
                          {error.message}
                        </Paragraph>
                      </div>
                    </Timeline.Item>
                  ))}
                </Timeline>
                {analytics.recentErrors.length === 0 && (
                  <Empty
                    description="No recent errors"
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                  />
                )}
              </Card>
            </Col>
          </Row>
        </TabPane>

        {/* Search & Filter Tab */}
        <TabPane
          tab={
            <Space>
              <SearchOutlined />
              Search & Filter
            </Space>
          }
          key="search"
        >
          <Row gutter={[16, 16]}>
            <Col span={24}>
              <Card title="Advanced Log Search" >
                <Space direction="vertical" style={{ width: '100%' }}>
                  <Row gutter={[16, 16]}>
                    <Col span={12}>
                      <Text strong>Search Query:</Text>
                      <Search
                        placeholder="Search in log messages, sources, user IDs..."
                        allowClear
                        enterButton="Search"
                        size="large"
                      />
                    </Col>
                    <Col span={12}>
                      <Text strong>Date Range:</Text>
                      <RangePicker
                        showTime
                        style={{ width: '100%' }}
                        size="large"
                      />
                    </Col>
                  </Row>
                  
                  <Row gutter={[16, 16]}>
                    <Col span={8}>
                      <Text strong>Log Levels:</Text>
                      <Select
                        mode="multiple"
                        placeholder="Select log levels"
                        style={{ width: '100%' }}
                        options={[
                          { label: 'Trace', value: 'trace' },
                          { label: 'Debug', value: 'debug' },
                          { label: 'Info', value: 'info' },
                          { label: 'Warning', value: 'warn' },
                          { label: 'Error', value: 'error' },
                          { label: 'Fatal', value: 'fatal' },
                        ]}
                      />
                    </Col>
                    <Col span={8}>
                      <Text strong>Sources:</Text>
                      <Select
                        mode="multiple"
                        placeholder="Select log sources"
                        style={{ width: '100%' }}
                        options={analytics.topSources.map(source => ({
                          label: source.name,
                          value: source.name,
                        }))}
                      />
                    </Col>
                    <Col span={8}>
                      <Text strong>User ID:</Text>
                      <Input placeholder="Filter by user ID" />
                    </Col>
                  </Row>

                  <Row gutter={[16, 16]}>
                    <Col span={8}>
                      <Text strong>Request ID:</Text>
                      <Input placeholder="Filter by request ID" />
                    </Col>
                    <Col span={8}>
                      <Text strong>Response Time:</Text>
                      <Select placeholder="Filter by response time" style={{ width: '100%' }}>
                        <Option value="fast">Fast (&lt; 100ms)</Option>
                        <Option value="normal">Normal (100ms - 1s)</Option>
                        <Option value="slow">Slow (&gt; 1s)</Option>
                      </Select>
                    </Col>
                    <Col span={8}>
                      <Text strong>Has Errors:</Text>
                      <Select placeholder="Filter by error presence" style={{ width: '100%' }}>
                        <Option value="yes">With Errors</Option>
                        <Option value="no">Without Errors</Option>
                      </Select>
                    </Col>
                  </Row>

                  <div style={{ textAlign: 'center', marginTop: 16 }}>
                    <Space>
                      <Button type="primary" icon={<SearchOutlined />} size="large">
                        Search Logs
                      </Button>
                      <Button icon={<ClearOutlined />} size="large">
                        Clear Filters
                      </Button>
                      <Button icon={<DownloadOutlined />} size="large">
                        Export Results
                      </Button>
                    </Space>
                  </div>
                </Space>
              </Card>
            </Col>

            {/* Search Results */}
            <Col span={24}>
              <Card title="Search Results" >
                <Empty
                  description="Enter search criteria above to find specific log entries"
                  image={Empty.PRESENTED_IMAGE_SIMPLE}
                />
              </Card>
            </Col>
          </Row>
        </TabPane>

        {/* Configuration Tab */}
        <TabPane
          tab={
            <Space>
              <SettingOutlined />
              Configuration
            </Space>
          }
          key="config"
        >
          <Row gutter={[16, 16]}>
            <Col span={12}>
              <Card title="Log Retention Settings" >
                <Space direction="vertical" style={{ width: '100%' }}>
                  <div>
                    <Text strong>Retention Period:</Text>
                    <Select defaultValue="30" style={{ width: '100%', marginTop: 8 }}>
                      <Option value="7">7 days</Option>
                      <Option value="30">30 days</Option>
                      <Option value="90">90 days</Option>
                      <Option value="365">1 year</Option>
                      <Option value="forever">Forever</Option>
                    </Select>
                  </div>
                  <div>
                    <Text strong>Archive After:</Text>
                    <Select defaultValue="30" style={{ width: '100%', marginTop: 8 }}>
                      <Option value="7">7 days</Option>
                      <Option value="30">30 days</Option>
                      <Option value="90">90 days</Option>
                    </Select>
                  </div>
                  <div>
                    <Text strong>Max Log Size:</Text>
                    <Select defaultValue="100" style={{ width: '100%', marginTop: 8 }}>
                      <Option value="50">50 MB</Option>
                      <Option value="100">100 MB</Option>
                      <Option value="500">500 MB</Option>
                      <Option value="1000">1 GB</Option>
                    </Select>
                  </div>
                </Space>
              </Card>
            </Col>

            <Col span={12}>
              <Card title="Log Level Configuration" >
                <Space direction="vertical" style={{ width: '100%' }}>
                  <div>
                    <Text strong>Global Log Level:</Text>
                    <Select defaultValue="info" style={{ width: '100%', marginTop: 8 }}>
                      <Option value="trace">Trace</Option>
                      <Option value="debug">Debug</Option>
                      <Option value="info">Info</Option>
                      <Option value="warn">Warning</Option>
                      <Option value="error">Error</Option>
                      <Option value="fatal">Fatal</Option>
                    </Select>
                  </div>
                  <div>
                    <Text strong>Enable Debug Logging:</Text>
                    <br />
                    <Switch defaultChecked={false} style={{ marginTop: 8 }} />
                  </div>
                  <div>
                    <Text strong>Log Performance Metrics:</Text>
                    <br />
                    <Switch defaultChecked={true} style={{ marginTop: 8 }} />
                  </div>
                  <div>
                    <Text strong>Log User Actions:</Text>
                    <br />
                    <Switch defaultChecked={true} style={{ marginTop: 8 }} />
                  </div>
                </Space>
              </Card>
            </Col>

            <Col span={12}>
              <Card title="Export Settings" >
                <Space direction="vertical" style={{ width: '100%' }}>
                  <div>
                    <Text strong>Default Export Format:</Text>
                    <Select defaultValue="json" style={{ width: '100%', marginTop: 8 }}>
                      <Option value="json">JSON</Option>
                      <Option value="csv">CSV</Option>
                      <Option value="txt">Text</Option>
                    </Select>
                  </div>
                  <div>
                    <Text strong>Max Export Size:</Text>
                    <Select defaultValue="10000" style={{ width: '100%', marginTop: 8 }}>
                      <Option value="1000">1,000 entries</Option>
                      <Option value="10000">10,000 entries</Option>
                      <Option value="100000">100,000 entries</Option>
                      <Option value="unlimited">Unlimited</Option>
                    </Select>
                  </div>
                  <div>
                    <Text strong>Include Stack Traces:</Text>
                    <br />
                    <Switch defaultChecked={true} style={{ marginTop: 8 }} />
                  </div>
                  <div>
                    <Text strong>Include Context Data:</Text>
                    <br />
                    <Switch defaultChecked={true} style={{ marginTop: 8 }} />
                  </div>
                </Space>
              </Card>
            </Col>

            <Col span={12}>
              <Card title="Real-time Settings" >
                <Space direction="vertical" style={{ width: '100%' }}>
                  <div>
                    <Text strong>Auto Refresh Interval:</Text>
                    <Select defaultValue="5" style={{ width: '100%', marginTop: 8 }}>
                      <Option value="1">1 second</Option>
                      <Option value="5">5 seconds</Option>
                      <Option value="10">10 seconds</Option>
                      <Option value="30">30 seconds</Option>
                    </Select>
                  </div>
                  <div>
                    <Text strong>Max Live Entries:</Text>
                    <Select defaultValue="1000" style={{ width: '100%', marginTop: 8 }}>
                      <Option value="500">500</Option>
                      <Option value="1000">1,000</Option>
                      <Option value="5000">5,000</Option>
                      <Option value="10000">10,000</Option>
                    </Select>
                  </div>
                  <div>
                    <Text strong>Auto Scroll to Latest:</Text>
                    <br />
                    <Switch defaultChecked={true} style={{ marginTop: 8 }} />
                  </div>
                  <div>
                    <Text strong>Highlight Errors:</Text>
                    <br />
                    <Switch defaultChecked={true} style={{ marginTop: 8 }} />
                  </div>
                </Space>
              </Card>
            </Col>

            <Col span={24}>
              <div style={{ textAlign: 'center' }}>
                <Space>
                  <Button type="primary" size="large">
                    Save Configuration
                  </Button>
                  <Button size="large">
                    Reset to Defaults
                  </Button>
                </Space>
              </div>
            </Col>
          </Row>
        </TabPane>
      </Tabs>

      {/* Log Export Modal */}
      <Modal
        title="Export Logs"
        open={exportModalVisible}
        onCancel={() => setExportModalVisible(false)}
        footer={[
          <Button key="cancel" onClick={() => setExportModalVisible(false)}>
            Cancel
          </Button>,
          <Button key="export" type="primary" onClick={() => {
            setExportModalVisible(false);
            notification.success({
              message: 'Export Started',
              description: 'Log export has been initiated. Download will start shortly.',
            });
          }}>
            Export Logs
          </Button>,
        ]}
        width={600}
      >
        <Space direction="vertical" style={{ width: '100%' }}>
          <div>
            <Text strong>Export Format:</Text>
            <Select
              value={exportConfig.format}
              onChange={(format) => setExportConfig(prev => ({ ...prev, format }))}
              style={{ width: '100%', marginTop: 8 }}
            >
              <Option value="json">JSON</Option>
              <Option value="csv">CSV</Option>
              <Option value="txt">Plain Text</Option>
            </Select>
          </div>
          <div>
            <Text strong>Log Levels:</Text>
            <Select
              mode="multiple"
              value={exportConfig.levels}
              onChange={(levels) => setExportConfig(prev => ({ ...prev, levels }))}
              style={{ width: '100%', marginTop: 8 }}
              placeholder="Select log levels to export"
            >
              <Option value="trace">Trace</Option>
              <Option value="debug">Debug</Option>
              <Option value="info">Info</Option>
              <Option value="warn">Warning</Option>
              <Option value="error">Error</Option>
              <Option value="fatal">Fatal</Option>
            </Select>
          </div>
          <div>
            <Text strong>Log Sources:</Text>
            <Select
              mode="multiple"
              value={exportConfig.sources}
              onChange={(sources) => setExportConfig(prev => ({ ...prev, sources }))}
              style={{ width: '100%', marginTop: 8 }}
              placeholder="Select log sources to export"
            >
              {analytics.topSources.map(source => (
                <Option key={source.name} value={source.name}>{source.name}</Option>
              ))}
            </Select>
          </div>
          <div>
            <Text strong>Date Range:</Text>
            <RangePicker
              showTime
              value={exportConfig.dateRange ? [dayjs(exportConfig.dateRange[0]), dayjs(exportConfig.dateRange[1])] : null}
              onChange={(dates) => {
                setExportConfig(prev => ({
                  ...prev,
                  dateRange: dates ? [dates[0]!.toISOString(), dates[1]!.toISOString()] : null,
                }));
              }}
              style={{ width: '100%', marginTop: 8 }}
            />
          </div>
          <div>
            <Text strong>Max Entries:</Text>
            <Select
              value={exportConfig.maxEntries}
              onChange={(maxEntries) => setExportConfig(prev => ({ ...prev, maxEntries }))}
              style={{ width: '100%', marginTop: 8 }}
            >
              <Option value={1000}>1,000</Option>
              <Option value={10000}>10,000</Option>
              <Option value={50000}>50,000</Option>
              <Option value={100000}>100,000</Option>
            </Select>
          </div>
        </Space>
      </Modal>

      {/* Log Viewer Settings Modal */}
      <Modal
        title="Log Viewer Settings"
        open={settingsModalVisible}
        onCancel={() => setSettingsModalVisible(false)}
        footer={[
          <Button key="cancel" onClick={() => setSettingsModalVisible(false)}>
            Cancel
          </Button>,
          <Button key="save" type="primary" onClick={() => setSettingsModalVisible(false)}>
            Save Settings
          </Button>,
        ]}
      >
        <Space direction="vertical" style={{ width: '100%' }}>
          <div>
            <Text strong>Real-time Streaming:</Text>
            <br />
            <Switch
              checked={logViewerSettings.realTime}
              onChange={(realTime) => setLogViewerSettings(prev => ({ ...prev, realTime }))}
              style={{ marginTop: 8 }}
            />
          </div>
          <div>
            <Text strong>Auto Refresh:</Text>
            <br />
            <Switch
              checked={logViewerSettings.autoRefresh}
              onChange={(autoRefresh) => setLogViewerSettings(prev => ({ ...prev, autoRefresh }))}
              style={{ marginTop: 8 }}
            />
          </div>
          <div>
            <Text strong>Refresh Interval:</Text>
            <Select
              value={logViewerSettings.refreshInterval}
              onChange={(refreshInterval) => setLogViewerSettings(prev => ({ ...prev, refreshInterval }))}
              style={{ width: '100%', marginTop: 8 }}
            >
              <Option value={1000}>1 second</Option>
              <Option value={5000}>5 seconds</Option>
              <Option value={10000}>10 seconds</Option>
              <Option value={30000}>30 seconds</Option>
            </Select>
          </div>
          <div>
            <Text strong>Max Entries:</Text>
            <Select
              value={logViewerSettings.maxEntries}
              onChange={(maxEntries) => setLogViewerSettings(prev => ({ ...prev, maxEntries }))}
              style={{ width: '100%', marginTop: 8 }}
            >
              <Option value={1000}>1,000</Option>
              <Option value={5000}>5,000</Option>
              <Option value={10000}>10,000</Option>
              <Option value={50000}>50,000</Option>
            </Select>
          </div>
          <div>
            <Text strong>Viewer Height:</Text>
            <Select
              value={logViewerSettings.height}
              onChange={(height) => setLogViewerSettings(prev => ({ ...prev, height }))}
              style={{ width: '100%', marginTop: 8 }}
            >
              <Option value={400}>400px</Option>
              <Option value={600}>600px</Option>
              <Option value={800}>800px</Option>
              <Option value={1000}>1000px</Option>
            </Select>
          </div>
        </Space>
      </Modal>
    </div>
  );
};

export default SystemLogs;
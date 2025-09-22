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
  Alert,
  Statistic,
  Progress,
  Badge,
  Tag,
  Modal,
  Table,
  List,
  Avatar,
  Tooltip,
  Dropdown,
  Menu,
  Switch,
  Select,
  Input,
  DatePicker,
  notification,
  Divider,
  Timeline,
} from 'antd';
import {
  DashboardOutlined,
  MonitorOutlined,
  AlertOutlined,
  SettingOutlined,
  ReloadOutlined,
  BellOutlined,
  CloudServerOutlined,
  DatabaseOutlined,
  ThunderboltOutlined,
  HddOutlined,
  WifiOutlined,
  SafetyCertificateOutlined,
  BugOutlined,
  CheckCircleOutlined,
  ExclamationCircleOutlined,
  CloseCircleOutlined,
  SyncOutlined,
  EyeOutlined,
  EditOutlined,
  DeleteOutlined,
  PlusOutlined,
  DownloadOutlined,
  UploadOutlined,
  InfoCircleOutlined,
  WarningOutlined,
  UserOutlined,
  ClockCircleOutlined,
} from '@ant-design/icons';
import SystemStatus from '../../components/monitoring/SystemStatus';
import LineChart from '../../components/charts/LineChart';
import BarChart from '../../components/charts/BarChart';
import PieChart from '../../components/charts/PieChart';
import dayjs from 'dayjs';

const { Title, Text, Paragraph } = Typography;
const { TabPane } = Tabs;
const { Option } = Select;
const { RangePicker } = DatePicker;

interface AlertRule {
  id: string;
  name: string;
  description: string;
  metric: string;
  condition: 'greater_than' | 'less_than' | 'equals' | 'not_equals';
  threshold: number;
  severity: 'info' | 'warning' | 'error' | 'critical';
  enabled: boolean;
  channels: string[];
  createdAt: string;
  lastTriggered?: string;
  triggerCount: number;
}

interface NotificationChannel {
  id: string;
  name: string;
  type: 'email' | 'slack' | 'webhook' | 'sms';
  config: Record<string, any>;
  enabled: boolean;
  createdAt: string;
}

interface SystemAlert {
  id: string;
  type: 'info' | 'warning' | 'error' | 'critical';
  title: string;
  message: string;
  source: string;
  timestamp: string;
  acknowledged: boolean;
  resolved: boolean;
  assignee?: string;
  ruleId?: string;
}

const SystemMonitoring: React.FC = () => {
  const [activeTab, setActiveTab] = useState('overview');
  const [loading, setLoading] = useState(false);
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [refreshInterval, setRefreshInterval] = useState(30); // seconds
  const [selectedDateRange, setSelectedDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs] | null>(null);

  // Modals
  const [alertRuleModalVisible, setAlertRuleModalVisible] = useState(false);
  const [channelModalVisible, setChannelModalVisible] = useState(false);
  const [alertDetailsModalVisible, setAlertDetailsModalVisible] = useState(false);
  const [selectedAlert, setSelectedAlert] = useState<SystemAlert | null>(null);

  // Mock data
  const [alertRules, setAlertRules] = useState<AlertRule[]>([
    {
      id: '1',
      name: 'High CPU Usage',
      description: 'Alert when CPU usage exceeds 80%',
      metric: 'cpu_usage',
      condition: 'greater_than',
      threshold: 80,
      severity: 'warning',
      enabled: true,
      channels: ['email', 'slack'],
      createdAt: '2024-01-15T10:00:00Z',
      lastTriggered: '2024-01-20T14:30:00Z',
      triggerCount: 3,
    },
    {
      id: '2',
      name: 'Database Connection Failure',
      description: 'Alert when database connection fails',
      metric: 'db_connection',
      condition: 'equals',
      threshold: 0,
      severity: 'critical',
      enabled: true,
      channels: ['email', 'slack', 'sms'],
      createdAt: '2024-01-15T10:00:00Z',
      triggerCount: 0,
    },
    {
      id: '3',
      name: 'Low Disk Space',
      description: 'Alert when disk usage exceeds 90%',
      metric: 'disk_usage',
      condition: 'greater_than',
      threshold: 90,
      severity: 'error',
      enabled: true,
      channels: ['email'],
      createdAt: '2024-01-15T10:00:00Z',
      triggerCount: 1,
    },
  ]);

  const [notificationChannels, setNotificationChannels] = useState<NotificationChannel[]>([
    {
      id: 'email',
      name: 'Email Notifications',
      type: 'email',
      config: { recipients: ['admin@mapleblog.com', 'ops@mapleblog.com'] },
      enabled: true,
      createdAt: '2024-01-15T10:00:00Z',
    },
    {
      id: 'slack',
      name: 'Slack #alerts',
      type: 'slack',
      config: { webhook: 'https://hooks.slack.com/services/...', channel: '#alerts' },
      enabled: true,
      createdAt: '2024-01-15T10:00:00Z',
    },
    {
      id: 'sms',
      name: 'SMS Alerts',
      type: 'sms',
      config: { numbers: ['+1234567890'] },
      enabled: false,
      createdAt: '2024-01-15T10:00:00Z',
    },
  ]);

  const [systemAlerts, setSystemAlerts] = useState<SystemAlert[]>([
    {
      id: '1',
      type: 'warning',
      title: 'High CPU Usage Detected',
      message: 'CPU usage has been above 85% for the last 5 minutes',
      source: 'System Monitor',
      timestamp: '2024-01-20T14:30:00Z',
      acknowledged: false,
      resolved: false,
      ruleId: '1',
    },
    {
      id: '2',
      type: 'error',
      title: 'API Response Time Degradation',
      message: 'Average API response time has increased to 1.2s',
      source: 'Performance Monitor',
      timestamp: '2024-01-20T13:45:00Z',
      acknowledged: true,
      resolved: false,
      assignee: 'John Doe',
    },
    {
      id: '3',
      type: 'info',
      title: 'Scheduled Maintenance Reminder',
      message: 'Database maintenance scheduled for tonight at 2:00 AM',
      source: 'Maintenance Scheduler',
      timestamp: '2024-01-20T12:00:00Z',
      acknowledged: true,
      resolved: false,
    },
  ]);

  // Performance metrics data
  const [performanceMetrics, setPerformanceMetrics] = useState({
    responseTime: Array.from({ length: 24 }, (_, i) => ({
      time: dayjs().subtract(23 - i, 'hour').format('HH:mm'),
      value: Math.random() * 500 + 200 + Math.sin(i / 4) * 100,
    })),
    throughput: Array.from({ length: 24 }, (_, i) => ({
      time: dayjs().subtract(23 - i, 'hour').format('HH:mm'),
      value: Math.random() * 1000 + 500 + Math.sin(i / 6) * 200,
    })),
    errorRate: Array.from({ length: 24 }, (_, i) => ({
      time: dayjs().subtract(23 - i, 'hour').format('HH:mm'),
      value: Math.random() * 5 + Math.sin(i / 3) * 2,
    })),
  });

  // Auto-refresh effect
  useEffect(() => {
    if (!autoRefresh) return;

    const interval = setInterval(() => {
      refreshData();
    }, refreshInterval * 1000);

    return () => clearInterval(interval);
  }, [autoRefresh, refreshInterval]);

  // Refresh all data
  const refreshData = async () => {
    setLoading(true);
    try {
      // Simulate API calls
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      // Update performance metrics with new data points
      setPerformanceMetrics(prev => ({
        responseTime: [...prev.responseTime.slice(1), {
          time: dayjs().format('HH:mm'),
          value: Math.random() * 500 + 200,
        }],
        throughput: [...prev.throughput.slice(1), {
          time: dayjs().format('HH:mm'),
          value: Math.random() * 1000 + 500,
        }],
        errorRate: [...prev.errorRate.slice(1), {
          time: dayjs().format('HH:mm'),
          value: Math.random() * 5,
        }],
      }));

      notification.success({
        message: 'Data Refreshed',
        description: 'Monitoring data has been updated',
        duration: 2,
      });
    } catch (error) {
      console.error('Failed to refresh data:', error);
      notification.error({
        message: 'Refresh Failed',
        description: 'Unable to refresh monitoring data',
      });
    } finally {
      setLoading(false);
    }
  };

  // Handle alert acknowledgment
  const acknowledgeAlert = (alertId: string) => {
    setSystemAlerts(prev => prev.map(alert => 
      alert.id === alertId ? { ...alert, acknowledged: true } : alert
    ));
    notification.success({
      message: 'Alert Acknowledged',
      description: 'Alert has been marked as acknowledged',
    });
  };

  // Handle alert resolution
  const resolveAlert = (alertId: string) => {
    setSystemAlerts(prev => prev.map(alert => 
      alert.id === alertId ? { ...alert, resolved: true } : alert
    ));
    notification.success({
      message: 'Alert Resolved',
      description: 'Alert has been marked as resolved',
    });
  };

  // Toggle alert rule
  const toggleAlertRule = (ruleId: string) => {
    setAlertRules(prev => prev.map(rule => 
      rule.id === ruleId ? { ...rule, enabled: !rule.enabled } : rule
    ));
  };

  // Toggle notification channel
  const toggleNotificationChannel = (channelId: string) => {
    setNotificationChannels(prev => prev.map(channel => 
      channel.id === channelId ? { ...channel, enabled: !channel.enabled } : channel
    ));
  };

  // Get severity color
  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case 'info': return '#1890ff';
      case 'warning': return '#faad14';
      case 'error': return '#ff4d4f';
      case 'critical': return '#a8071a';
      default: return '#d9d9d9';
    }
  };

  // Get severity icon
  const getSeverityIcon = (severity: string) => {
    switch (severity) {
      case 'info': return <InfoCircleOutlined />;
      case 'warning': return <WarningOutlined />;
      case 'error': return <CloseCircleOutlined />;
      case 'critical': return <ExclamationCircleOutlined />;
      default: return <InfoCircleOutlined />;
    }
  };

  const unacknowledgedAlerts = systemAlerts.filter(alert => !alert.acknowledged && !alert.resolved);
  const activeAlerts = systemAlerts.filter(alert => !alert.resolved);

  return (
    <div className="system-monitoring">
      {/* Header */}
      <div style={{ marginBottom: 24 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <div>
            <Title level={2}>
              <Space>
                <MonitorOutlined />
                System Monitoring
                {unacknowledgedAlerts.length > 0 && (
                  <Badge count={unacknowledgedAlerts.length} />
                )}
              </Space>
            </Title>
            <Paragraph type="secondary">
              Monitor system performance, manage alerts, and track service health in real-time.
            </Paragraph>
          </div>
          <Space>
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
              onClick={refreshData}
            >
              Refresh
            </Button>
          </Space>
        </div>

        {/* Active alerts banner */}
        {unacknowledgedAlerts.length > 0 && (
          <Alert
            type="warning"
            message={`${unacknowledgedAlerts.length} unacknowledged alert(s) require attention`}
            description="Review and acknowledge active alerts to ensure system stability"
            showIcon
            closable
            action={
              <Button
                
                type="primary"
                onClick={() => setActiveTab('alerts')}
              >
                View Alerts
              </Button>
            }
            style={{ marginTop: 16 }}
          />
        )}
      </div>

      {/* Main Content */}
      <Tabs activeKey={activeTab} onChange={setActiveTab} type="card">
        {/* Overview Tab */}
        <TabPane
          tab={
            <Space>
              <DashboardOutlined />
              Overview
            </Space>
          }
          key="overview"
        >
          <Row gutter={[16, 16]}>
            {/* System Status Component */}
            <Col span={24}>
              <SystemStatus
                refreshInterval={refreshInterval * 1000}
                onAlert={(alert) => {
                  setSystemAlerts(prev => [alert, ...prev]);
                  notification.warning({
                    message: alert.title,
                    description: alert.message,
                    duration: 5,
                  });
                }}
              />
            </Col>

            {/* Performance Charts */}
            <Col span={8}>
              <Card title="API Response Time (24h)" >
                <LineChart
                  data={performanceMetrics.responseTime}
                  height={200}
                  color="#1890ff"
                  smooth
                />
                <div style={{ textAlign: 'center', marginTop: 8 }}>
                  <Statistic
                    value={performanceMetrics.responseTime[performanceMetrics.responseTime.length - 1]?.value || 0}
                    suffix="ms"
                    precision={0}
                    valueStyle={{ fontSize: '14px' }}
                  />
                </div>
              </Card>
            </Col>

            <Col span={8}>
              <Card title="Throughput (24h)" >
                <LineChart
                  data={performanceMetrics.throughput}
                  height={200}
                  color="#52c41a"
                  smooth
                />
                <div style={{ textAlign: 'center', marginTop: 8 }}>
                  <Statistic
                    value={performanceMetrics.throughput[performanceMetrics.throughput.length - 1]?.value || 0}
                    suffix="/min"
                    precision={0}
                    valueStyle={{ fontSize: '14px' }}
                  />
                </div>
              </Card>
            </Col>

            <Col span={8}>
              <Card title="Error Rate (24h)" >
                <LineChart
                  data={performanceMetrics.errorRate}
                  height={200}
                  color="#ff4d4f"
                  smooth
                />
                <div style={{ textAlign: 'center', marginTop: 8 }}>
                  <Statistic
                    value={performanceMetrics.errorRate[performanceMetrics.errorRate.length - 1]?.value || 0}
                    suffix="%"
                    precision={2}
                    valueStyle={{ fontSize: '14px' }}
                  />
                </div>
              </Card>
            </Col>
          </Row>
        </TabPane>

        {/* Alerts Tab */}
        <TabPane
          tab={
            <Space>
              <AlertOutlined />
              Alerts
              <Badge count={unacknowledgedAlerts.length} />
            </Space>
          }
          key="alerts"
        >
          <Row gutter={[16, 16]}>
            {/* Alert Summary */}
            <Col span={24}>
              <Row gutter={[16, 16]}>
                <Col span={6}>
                  <Card >
                    <Statistic
                      title="Active Alerts"
                      value={activeAlerts.length}
                      prefix={<AlertOutlined />}
                      valueStyle={{ color: activeAlerts.length > 0 ? '#ff4d4f' : '#52c41a' }}
                    />
                  </Card>
                </Col>
                <Col span={6}>
                  <Card >
                    <Statistic
                      title="Unacknowledged"
                      value={unacknowledgedAlerts.length}
                      prefix={<ExclamationCircleOutlined />}
                      valueStyle={{ color: unacknowledgedAlerts.length > 0 ? '#faad14' : '#52c41a' }}
                    />
                  </Card>
                </Col>
                <Col span={6}>
                  <Card >
                    <Statistic
                      title="Alert Rules"
                      value={alertRules.filter(rule => rule.enabled).length}
                      suffix={`/ ${alertRules.length}`}
                      prefix={<SettingOutlined />}
                    />
                  </Card>
                </Col>
                <Col span={6}>
                  <Card >
                    <Statistic
                      title="Notification Channels"
                      value={notificationChannels.filter(channel => channel.enabled).length}
                      suffix={`/ ${notificationChannels.length}`}
                      prefix={<BellOutlined />}
                    />
                  </Card>
                </Col>
              </Row>
            </Col>

            {/* Active Alerts List */}
            <Col span={24}>
              <Card
                title="Active Alerts"
                extra={
                  <Button
                    type="primary"
                    icon={<PlusOutlined />}
                    onClick={() => setAlertRuleModalVisible(true)}
                  >
                    New Alert Rule
                  </Button>
                }
              >
                <Table
                  dataSource={activeAlerts}
                  rowKey="id"
                  pagination={{ pageSize: 10 }}
                  
                  columns={[
                    {
                      title: 'Severity',
                      dataIndex: 'type',
                      key: 'type',
                      width: 100,
                      render: (type) => (
                        <Tag
                          icon={getSeverityIcon(type)}
                          color={getSeverityColor(type)}
                        >
                          {type.toUpperCase()}
                        </Tag>
                      ),
                    },
                    {
                      title: 'Alert',
                      dataIndex: 'title',
                      key: 'title',
                      render: (title, record) => (
                        <div>
                          <Text strong>{title}</Text>
                          <br />
                          <Text type="secondary" style={{ fontSize: '12px' }}>
                            {record.message}
                          </Text>
                        </div>
                      ),
                    },
                    {
                      title: 'Source',
                      dataIndex: 'source',
                      key: 'source',
                      width: 120,
                    },
                    {
                      title: 'Time',
                      dataIndex: 'timestamp',
                      key: 'timestamp',
                      width: 120,
                      render: (timestamp) => dayjs(timestamp).format('MM-DD HH:mm'),
                      sorter: (a, b) => dayjs(a.timestamp).unix() - dayjs(b.timestamp).unix(),
                    },
                    {
                      title: 'Status',
                      key: 'status',
                      width: 120,
                      render: (_, record) => (
                        <Space direction="vertical" >
                          {record.acknowledged && <Tag color="blue">ACKNOWLEDGED</Tag>}
                          {record.assignee && (
                            <Tag icon={<UserOutlined />}>{record.assignee}</Tag>
                          )}
                        </Space>
                      ),
                    },
                    {
                      title: 'Actions',
                      key: 'actions',
                      width: 150,
                      render: (_, record) => (
                        <Space>
                          <Button
                            
                            icon={<EyeOutlined />}
                            onClick={() => {
                              setSelectedAlert(record);
                              setAlertDetailsModalVisible(true);
                            }}
                          >
                            Details
                          </Button>
                          {!record.acknowledged && (
                            <Button
                              
                              onClick={() => acknowledgeAlert(record.id)}
                            >
                              ACK
                            </Button>
                          )}
                          {!record.resolved && (
                            <Button
                              
                              type="primary"
                              onClick={() => resolveAlert(record.id)}
                            >
                              Resolve
                            </Button>
                          )}
                        </Space>
                      ),
                    },
                  ]}
                />
              </Card>
            </Col>
          </Row>
        </TabPane>

        {/* Alert Rules Tab */}
        <TabPane
          tab={
            <Space>
              <SettingOutlined />
              Alert Rules
            </Space>
          }
          key="rules"
        >
          <Card
            title="Alert Rules Configuration"
            extra={
              <Button
                type="primary"
                icon={<PlusOutlined />}
                onClick={() => setAlertRuleModalVisible(true)}
              >
                Add Rule
              </Button>
            }
          >
            <Table
              dataSource={alertRules}
              rowKey="id"
              pagination={false}
              
              columns={[
                {
                  title: 'Rule Name',
                  dataIndex: 'name',
                  key: 'name',
                  render: (name, record) => (
                    <div>
                      <Text strong>{name}</Text>
                      <br />
                      <Text type="secondary" style={{ fontSize: '12px' }}>
                        {record.description}
                      </Text>
                    </div>
                  ),
                },
                {
                  title: 'Condition',
                  key: 'condition',
                  render: (_, record) => (
                    <Text code>
                      {record.metric} {record.condition.replace('_', ' ')} {record.threshold}
                    </Text>
                  ),
                },
                {
                  title: 'Severity',
                  dataIndex: 'severity',
                  key: 'severity',
                  render: (severity) => (
                    <Tag
                      icon={getSeverityIcon(severity)}
                      color={getSeverityColor(severity)}
                    >
                      {severity.toUpperCase()}
                    </Tag>
                  ),
                },
                {
                  title: 'Channels',
                  dataIndex: 'channels',
                  key: 'channels',
                  render: (channels) => (
                    <Space>
                      {channels.map((channel: string) => (
                        <Tag key={channel}>{channel}</Tag>
                      ))}
                    </Space>
                  ),
                },
                {
                  title: 'Triggers',
                  dataIndex: 'triggerCount',
                  key: 'triggerCount',
                  render: (count, record) => (
                    <div>
                      <Text>{count}</Text>
                      {record.lastTriggered && (
                        <>
                          <br />
                          <Text type="secondary" style={{ fontSize: '12px' }}>
                            Last: {dayjs(record.lastTriggered).format('MM-DD HH:mm')}
                          </Text>
                        </>
                      )}
                    </div>
                  ),
                },
                {
                  title: 'Status',
                  dataIndex: 'enabled',
                  key: 'enabled',
                  render: (enabled, record) => (
                    <Switch
                      checked={enabled}
                      onChange={() => toggleAlertRule(record.id)}
                    />
                  ),
                },
                {
                  title: 'Actions',
                  key: 'actions',
                  render: (_, record) => (
                    <Space>
                      <Button  icon={<EditOutlined />}>
                        Edit
                      </Button>
                      <Button  icon={<DeleteOutlined />} danger>
                        Delete
                      </Button>
                    </Space>
                  ),
                },
              ]}
            />
          </Card>
        </TabPane>

        {/* Notification Channels Tab */}
        <TabPane
          tab={
            <Space>
              <BellOutlined />
              Notifications
            </Space>
          }
          key="notifications"
        >
          <Card
            title="Notification Channels"
            extra={
              <Button
                type="primary"
                icon={<PlusOutlined />}
                onClick={() => setChannelModalVisible(true)}
              >
                Add Channel
              </Button>
            }
          >
            <List
              dataSource={notificationChannels}
              renderItem={(channel) => (
                <List.Item
                  actions={[
                    <Switch
                      key="toggle"
                      checked={channel.enabled}
                      onChange={() => toggleNotificationChannel(channel.id)}
                    />,
                    <Button key="edit"  icon={<EditOutlined />}>
                      Edit
                    </Button>,
                    <Button key="test" >
                      Test
                    </Button>,
                    <Button key="delete"  icon={<DeleteOutlined />} danger>
                      Delete
                    </Button>,
                  ]}
                >
                  <List.Item.Meta
                    avatar={
                      <Avatar
                        icon={
                          channel.type === 'email' ? <BellOutlined /> :
                          channel.type === 'slack' ? <WifiOutlined /> :
                          channel.type === 'webhook' ? <CloudServerOutlined /> :
                          <BellOutlined />
                        }
                        style={{
                          backgroundColor: channel.enabled ? '#52c41a' : '#d9d9d9',
                        }}
                      />
                    }
                    title={
                      <Space>
                        {channel.name}
                        <Tag>{channel.type.toUpperCase()}</Tag>
                        {!channel.enabled && <Tag color="red">DISABLED</Tag>}
                      </Space>
                    }
                    description={
                      <div>
                        <Text type="secondary">
                          Created: {dayjs(channel.createdAt).format('YYYY-MM-DD')}
                        </Text>
                        <br />
                        <Text code style={{ fontSize: '12px' }}>
                          {JSON.stringify(channel.config, null, 0)}
                        </Text>
                      </div>
                    }
                  />
                </List.Item>
              )}
            />
          </Card>
        </TabPane>
      </Tabs>

      {/* Alert Details Modal */}
      <Modal
        title={
          <Space>
            <AlertOutlined />
            Alert Details
            {selectedAlert && (
              <Tag
                icon={getSeverityIcon(selectedAlert.type)}
                color={getSeverityColor(selectedAlert.type)}
              >
                {selectedAlert.type.toUpperCase()}
              </Tag>
            )}
          </Space>
        }
        open={alertDetailsModalVisible}
        onCancel={() => setAlertDetailsModalVisible(false)}
        footer={[
          <Button key="close" onClick={() => setAlertDetailsModalVisible(false)}>
            Close
          </Button>,
          selectedAlert && !selectedAlert.acknowledged && (
            <Button
              key="ack"
              onClick={() => {
                acknowledgeAlert(selectedAlert.id);
                setAlertDetailsModalVisible(false);
              }}
            >
              Acknowledge
            </Button>
          ),
          selectedAlert && !selectedAlert.resolved && (
            <Button
              key="resolve"
              type="primary"
              onClick={() => {
                resolveAlert(selectedAlert.id);
                setAlertDetailsModalVisible(false);
              }}
            >
              Resolve
            </Button>
          ),
        ]}
        width={600}
      >
        {selectedAlert && (
          <div>
            <Row gutter={[16, 16]}>
              <Col span={12}>
                <Text strong>Title:</Text>
                <br />
                <Text>{selectedAlert.title}</Text>
              </Col>
              <Col span={12}>
                <Text strong>Source:</Text>
                <br />
                <Text>{selectedAlert.source}</Text>
              </Col>
              <Col span={12}>
                <Text strong>Timestamp:</Text>
                <br />
                <Text>{dayjs(selectedAlert.timestamp).format('YYYY-MM-DD HH:mm:ss')}</Text>
              </Col>
              <Col span={12}>
                <Text strong>Severity:</Text>
                <br />
                <Tag
                  icon={getSeverityIcon(selectedAlert.type)}
                  color={getSeverityColor(selectedAlert.type)}
                >
                  {selectedAlert.type.toUpperCase()}
                </Tag>
              </Col>
              {selectedAlert.assignee && (
                <Col span={12}>
                  <Text strong>Assignee:</Text>
                  <br />
                  <Text>{selectedAlert.assignee}</Text>
                </Col>
              )}
              {selectedAlert.ruleId && (
                <Col span={12}>
                  <Text strong>Alert Rule:</Text>
                  <br />
                  <Text>{alertRules.find(rule => rule.id === selectedAlert.ruleId)?.name || selectedAlert.ruleId}</Text>
                </Col>
              )}
            </Row>
            
            <Divider />
            
            <div>
              <Text strong>Message:</Text>
              <Paragraph style={{ marginTop: 8 }}>
                {selectedAlert.message}
              </Paragraph>
            </div>

            <div style={{ marginTop: 16 }}>
              <Text strong>Status:</Text>
              <div style={{ marginTop: 8 }}>
                <Space direction="vertical">
                  {selectedAlert.acknowledged && (
                    <Tag color="blue" icon={<CheckCircleOutlined />}>
                      ACKNOWLEDGED
                    </Tag>
                  )}
                  {selectedAlert.resolved && (
                    <Tag color="green" icon={<CheckCircleOutlined />}>
                      RESOLVED
                    </Tag>
                  )}
                  {!selectedAlert.acknowledged && !selectedAlert.resolved && (
                    <Tag color="red" icon={<ExclamationCircleOutlined />}>
                      ACTIVE
                    </Tag>
                  )}
                </Space>
              </div>
            </div>
          </div>
        )}
      </Modal>

      {/* Alert Rule Modal */}
      <Modal
        title="Add Alert Rule"
        open={alertRuleModalVisible}
        onCancel={() => setAlertRuleModalVisible(false)}
        footer={[
          <Button key="cancel" onClick={() => setAlertRuleModalVisible(false)}>
            Cancel
          </Button>,
          <Button key="save" type="primary">
            Save Rule
          </Button>,
        ]}
        width={600}
      >
        <Space direction="vertical" style={{ width: '100%' }}>
          <div>
            <Text strong>Rule Name:</Text>
            <Input placeholder="Enter rule name" />
          </div>
          <div>
            <Text strong>Description:</Text>
            <Input.TextArea placeholder="Enter rule description" rows={2} />
          </div>
          <Row gutter={16}>
            <Col span={8}>
              <Text strong>Metric:</Text>
              <Select placeholder="Select metric" style={{ width: '100%' }}>
                <Option value="cpu_usage">CPU Usage</Option>
                <Option value="memory_usage">Memory Usage</Option>
                <Option value="disk_usage">Disk Usage</Option>
                <Option value="response_time">Response Time</Option>
                <Option value="error_rate">Error Rate</Option>
              </Select>
            </Col>
            <Col span={8}>
              <Text strong>Condition:</Text>
              <Select placeholder="Select condition" style={{ width: '100%' }}>
                <Option value="greater_than">Greater than</Option>
                <Option value="less_than">Less than</Option>
                <Option value="equals">Equals</Option>
                <Option value="not_equals">Not equals</Option>
              </Select>
            </Col>
            <Col span={8}>
              <Text strong>Threshold:</Text>
              <Input placeholder="Enter threshold value" type="number" />
            </Col>
          </Row>
          <div>
            <Text strong>Severity:</Text>
            <Select placeholder="Select severity" style={{ width: '100%' }}>
              <Option value="info">Info</Option>
              <Option value="warning">Warning</Option>
              <Option value="error">Error</Option>
              <Option value="critical">Critical</Option>
            </Select>
          </div>
          <div>
            <Text strong>Notification Channels:</Text>
            <Select
              mode="multiple"
              placeholder="Select channels"
              style={{ width: '100%' }}
            >
              {notificationChannels
                .filter(channel => channel.enabled)
                .map(channel => (
                  <Option key={channel.id} value={channel.id}>
                    {channel.name}
                  </Option>
                ))}
            </Select>
          </div>
        </Space>
      </Modal>

      {/* Notification Channel Modal */}
      <Modal
        title="Add Notification Channel"
        open={channelModalVisible}
        onCancel={() => setChannelModalVisible(false)}
        footer={[
          <Button key="cancel" onClick={() => setChannelModalVisible(false)}>
            Cancel
          </Button>,
          <Button key="save" type="primary">
            Save Channel
          </Button>,
        ]}
        width={600}
      >
        <Space direction="vertical" style={{ width: '100%' }}>
          <div>
            <Text strong>Channel Name:</Text>
            <Input placeholder="Enter channel name" />
          </div>
          <div>
            <Text strong>Channel Type:</Text>
            <Select placeholder="Select channel type" style={{ width: '100%' }}>
              <Option value="email">Email</Option>
              <Option value="slack">Slack</Option>
              <Option value="webhook">Webhook</Option>
              <Option value="sms">SMS</Option>
            </Select>
          </div>
          <div>
            <Text strong>Configuration:</Text>
            <Input.TextArea
              placeholder="Enter channel configuration (JSON format)"
              rows={4}
            />
          </div>
        </Space>
      </Modal>
    </div>
  );
};

export default SystemMonitoring;
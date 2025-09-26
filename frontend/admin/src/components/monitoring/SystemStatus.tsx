import React, { useState, useEffect, useCallback } from 'react';
import {
  Card,
  Row,
  Col,
  Statistic,
  Progress,
  Badge,
  Tag,
  Space,
  Typography,
  List,
  Avatar,
  Button,
  Alert,
  Divider,
  Modal,
  Table,
  notification,
} from 'antd';
import {
  CheckCircleOutlined,
  ExclamationCircleOutlined,
  CloseCircleOutlined,
  SyncOutlined,
  DatabaseOutlined,
  CloudServerOutlined,
  ThunderboltOutlined,
  BellOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import LineChart from '../charts/LineChart';

const { Title, Text } = Typography;

interface SystemMetric {
  name: string;
  value: number;
  unit: string;
  status: 'healthy' | 'warning' | 'critical';
  trend: 'up' | 'down' | 'stable';
  threshold: {
    warning: number;
    critical: number;
  };
}

interface ServiceStatus {
  id: string;
  name: string;
  status: 'running' | 'stopped' | 'error' | 'starting';
  uptime: string;
  version: string;
  endpoint?: string;
  lastCheck: string;
  responseTime?: number;
  errorRate?: number;
}

interface Alert {
  id: string;
  type: 'info' | 'warning' | 'error' | 'critical';
  title: string;
  message: string;
  source: string;
  timestamp: string;
  acknowledged: boolean;
  resolved: boolean;
}

interface SystemStatusProps {
  refreshInterval?: number;
  onAlert?: (alert: Alert) => void;
}

const SystemStatus: React.FC<SystemStatusProps> = ({
  refreshInterval = 30000,
  onAlert,
}) => {
  const [loading, setLoading] = useState(false);
  const [lastUpdate, setLastUpdate] = useState<Date>(new Date());
  const [alertsVisible, setAlertsVisible] = useState(false);
  const [selectedService, setSelectedService] = useState<ServiceStatus | null>(null);

  // Mock system metrics data
  const [systemMetrics, setSystemMetrics] = useState<SystemMetric[]>([
    {
      name: 'CPU Usage',
      value: 45,
      unit: '%',
      status: 'healthy',
      trend: 'stable',
      threshold: { warning: 70, critical: 90 },
    },
    {
      name: 'Memory Usage',
      value: 62,
      unit: '%',
      status: 'healthy',
      trend: 'up',
      threshold: { warning: 80, critical: 95 },
    },
    {
      name: 'Disk Usage',
      value: 78,
      unit: '%',
      status: 'warning',
      trend: 'up',
      threshold: { warning: 75, critical: 90 },
    },
    {
      name: 'Network I/O',
      value: 25,
      unit: 'MB/s',
      status: 'healthy',
      trend: 'stable',
      threshold: { warning: 100, critical: 200 },
    },
  ]);

  // Mock services data
  const [services] = useState<ServiceStatus[]>([
    {
      id: 'api',
      name: 'MapleBlog API',
      status: 'running',
      uptime: '7d 14h 32m',
      version: '1.0.0',
      endpoint: 'https://api.mapleblog.com',
      lastCheck: new Date().toISOString(),
      responseTime: 145,
      errorRate: 0.2,
    },
    {
      id: 'db',
      name: 'PostgreSQL Database',
      status: 'running',
      uptime: '15d 8h 45m',
      version: '15.2',
      lastCheck: new Date().toISOString(),
      responseTime: 25,
      errorRate: 0,
    },
    {
      id: 'redis',
      name: 'Redis Cache',
      status: 'running',
      uptime: '12d 6h 12m',
      version: '7.0.8',
      lastCheck: new Date().toISOString(),
      responseTime: 5,
      errorRate: 0,
    },
    {
      id: 'nginx',
      name: 'Nginx Proxy',
      status: 'running',
      uptime: '20d 3h 15m',
      version: '1.22.1',
      lastCheck: new Date().toISOString(),
      responseTime: 12,
      errorRate: 0.1,
    },
    {
      id: 'elasticsearch',
      name: 'Elasticsearch',
      status: 'warning',
      uptime: '5d 12h 8m',
      version: '8.6.0',
      lastCheck: new Date().toISOString(),
      responseTime: 280,
      errorRate: 2.1,
    },
  ]);

  // Mock alerts data
  const [alerts, setAlerts] = useState<Alert[]>([
    {
      id: '1',
      type: 'warning',
      title: 'High Disk Usage',
      message: 'Disk usage has exceeded 75% threshold on primary storage',
      source: 'System Monitor',
      timestamp: new Date(Date.now() - 300000).toISOString(),
      acknowledged: false,
      resolved: false,
    },
    {
      id: '2',
      type: 'error',
      title: 'Elasticsearch Performance Degradation',
      message: 'Response time has increased significantly over the past hour',
      source: 'Service Monitor',
      timestamp: new Date(Date.now() - 600000).toISOString(),
      acknowledged: true,
      resolved: false,
    },
    {
      id: '3',
      type: 'info',
      title: 'Scheduled Maintenance',
      message: 'System maintenance window scheduled for tonight at 2:00 AM',
      source: 'Maintenance Scheduler',
      timestamp: new Date(Date.now() - 1800000).toISOString(),
      acknowledged: true,
      resolved: false,
    },
  ]);

  // Mock performance data for charts
  const [performanceData] = useState({
    cpu: Array.from({ length: 24 }, (_, i) => ({
      time: new Date(Date.now() - (23 - i) * 3600000).toISOString(),
      value: Math.random() * 50 + 20 + Math.sin(i / 4) * 10,
    })),
    memory: Array.from({ length: 24 }, (_, i) => ({
      time: new Date(Date.now() - (23 - i) * 3600000).toISOString(),
      value: Math.random() * 30 + 40 + Math.sin(i / 6) * 15,
    })),
    network: Array.from({ length: 24 }, (_, i) => ({
      time: new Date(Date.now() - (23 - i) * 3600000).toISOString(),
      value: Math.random() * 50 + 10,
    })),
  });

  // Auto-refresh data
  useEffect(() => {
    const interval = setInterval(() => {
      refreshSystemStatus();
    }, refreshInterval);

    return () => clearInterval(interval);
  }, [refreshInterval, refreshSystemStatus]);

  // Refresh system status
  const refreshSystemStatus = useCallback(async () => {
    setLoading(true);
    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      // Update metrics with some random variation
      setSystemMetrics(prev => prev.map(metric => ({
        ...metric,
        value: Math.max(0, Math.min(100, metric.value + (Math.random() - 0.5) * 5)),
      })));

      setLastUpdate(new Date());
      
      // Check for new alerts
      const newAlert = Math.random() < 0.1; // 10% chance of new alert
      if (newAlert && onAlert) {
        const alertTypes: Alert['type'][] = ['info', 'warning', 'error'];
        const randomAlert: Alert = {
          id: Date.now().toString(),
          type: alertTypes[Math.floor(Math.random() * alertTypes.length)],
          title: 'System Alert',
          message: 'This is a sample alert for demonstration',
          source: 'System Monitor',
          timestamp: new Date().toISOString(),
          acknowledged: false,
          resolved: false,
        };
        onAlert(randomAlert);
      }
    } catch (error) {
      console.error('Failed to refresh system status:', error);
      notification.error({
        message: 'Refresh Failed',
        description: 'Unable to refresh system status. Please try again.',
      });
    } finally {
      setLoading(false);
    }
  }, [onAlert]);

  // Get status color
  const getStatusColor = (status: string) => {
    switch (status) {
      case 'running':
      case 'healthy':
        return '#52c41a';
      case 'warning':
        return '#faad14';
      case 'error':
      case 'critical':
        return '#ff4d4f';
      case 'stopped':
        return '#d9d9d9';
      case 'starting':
        return '#1890ff';
      default:
        return '#d9d9d9';
    }
  };

  // Get status icon
  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'running':
      case 'healthy':
        return <CheckCircleOutlined style={{ color: '#52c41a' }} />;
      case 'warning':
        return <ExclamationCircleOutlined style={{ color: '#faad14' }} />;
      case 'error':
      case 'critical':
        return <CloseCircleOutlined style={{ color: '#ff4d4f' }} />;
      case 'starting':
        return <SyncOutlined spin style={{ color: '#1890ff' }} />;
      default:
        return <ExclamationCircleOutlined style={{ color: '#d9d9d9' }} />;
    }
  };

  // Get metric status based on thresholds
  const getMetricStatus = (metric: SystemMetric): 'healthy' | 'warning' | 'critical' => {
    if (metric.value >= metric.threshold.critical) return 'critical';
    if (metric.value >= metric.threshold.warning) return 'warning';
    return 'healthy';
  };

  // Handle alert acknowledgment
  const acknowledgeAlert = (alertId: string) => {
    setAlerts(prev => prev.map(alert => 
      alert.id === alertId ? { ...alert, acknowledged: true } : alert
    ));
  };

  // Handle alert resolution
  const resolveAlert = (alertId: string) => {
    setAlerts(prev => prev.map(alert => 
      alert.id === alertId ? { ...alert, resolved: true } : alert
    ));
  };

  const unacknowledgedAlerts = alerts.filter(alert => !alert.acknowledged && !alert.resolved);
  const activeAlerts = alerts.filter(alert => !alert.resolved);

  return (
    <div className="system-status">
      {/* Header with refresh controls */}
      <div style={{ marginBottom: 24, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <Title level={3} style={{ margin: 0 }}>System Status</Title>
          <Text type="secondary">
            Last updated: {lastUpdate.toLocaleTimeString()}
          </Text>
        </div>
        <Space>
          {unacknowledgedAlerts.length > 0 && (
            <Badge count={unacknowledgedAlerts.length}>
              <Button
                icon={<BellOutlined />}
                onClick={() => setAlertsVisible(true)}
              >
                Alerts
              </Button>
            </Badge>
          )}
          <Button
            icon={<ReloadOutlined />}
            loading={loading}
            onClick={refreshSystemStatus}
          >
            Refresh
          </Button>
        </Space>
      </div>

      {/* Active alerts banner */}
      {unacknowledgedAlerts.length > 0 && (
        <Alert
          type="warning"
          message={`${unacknowledgedAlerts.length} unacknowledged alert(s)`}
          description="Click to view and manage active alerts"
          showIcon
          closable
          action={
            <Button
              
              type="primary"
              onClick={() => setAlertsVisible(true)}
            >
              View Alerts
            </Button>
          }
          style={{ marginBottom: 16 }}
        />
      )}

      {/* System Metrics */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        {systemMetrics.map((metric) => {
          const status = getMetricStatus(metric);
          return (
            <Col key={metric.name} span={6}>
              <Card >
                <Statistic
                  title={metric.name}
                  value={metric.value}
                  suffix={metric.unit}
                  prefix={getStatusIcon(status)}
                  valueStyle={{ color: getStatusColor(status) }}
                />
                <div style={{ marginTop: 8 }}>
                  <Progress
                    percent={metric.value}
                    strokeColor={getStatusColor(status)}
                    
                    showInfo={false}
                  />
                  <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '12px', color: '#666' }}>
                    <span>Warning: {metric.threshold.warning}{metric.unit}</span>
                    <span>Critical: {metric.threshold.critical}{metric.unit}</span>
                  </div>
                </div>
              </Card>
            </Col>
          );
        })}
      </Row>

      {/* Services Status */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col span={24}>
          <Card
            title={
              <Space>
                <CloudServerOutlined />
                Services Status
              </Space>
            }
            
          >
            <List
              dataSource={services}
              renderItem={(service) => (
                <List.Item
                  actions={[
                    <Button
                      key="details"
                      onClick={() => setSelectedService(service)}
                    >
                      Details
                    </Button>,
                    service.endpoint && (
                      <Button key="open" type="link">
                        Open
                      </Button>
                    ),
                  ]}
                >
                  <List.Item.Meta
                    avatar={
                      <Avatar
                        icon={
                          service.name.includes('Database') ? <DatabaseOutlined /> :
                          service.name.includes('Cache') ? <ThunderboltOutlined /> :
                          <CloudServerOutlined />
                        }
                        style={{ backgroundColor: getStatusColor(service.status) }}
                      />
                    }
                    title={
                      <Space>
                        {service.name}
                        <Tag color={getStatusColor(service.status)}>
                          {service.status.toUpperCase()}
                        </Tag>
                        <Text type="secondary">v{service.version}</Text>
                      </Space>
                    }
                    description={
                      <Space direction="vertical" >
                        <Text type="secondary">Uptime: {service.uptime}</Text>
                        {service.responseTime && (
                          <Text type="secondary">Response: {service.responseTime}ms</Text>
                        )}
                        {service.errorRate !== undefined && (
                          <Text type="secondary">Error Rate: {service.errorRate}%</Text>
                        )}
                      </Space>
                    }
                  />
                </List.Item>
              )}
            />
          </Card>
        </Col>
      </Row>

      {/* Performance Charts */}
      <Row gutter={[16, 16]}>
        <Col span={8}>
          <Card title="CPU Usage (24h)" >
            <LineChart
              data={performanceData.cpu}
              height={200}
              color="#1890ff"
              smooth
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card title="Memory Usage (24h)" >
            <LineChart
              data={performanceData.memory}
              height={200}
              color="#52c41a"
              smooth
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card title="Network I/O (24h)" >
            <LineChart
              data={performanceData.network}
              height={200}
              color="#722ed1"
              smooth
            />
          </Card>
        </Col>
      </Row>

      {/* Alerts Modal */}
      <Modal
        title={
          <Space>
            <BellOutlined />
            System Alerts
            <Badge count={unacknowledgedAlerts.length} />
          </Space>
        }
        open={alertsVisible}
        onCancel={() => setAlertsVisible(false)}
        footer={null}
        width={800}
      >
        <Table
          dataSource={activeAlerts}
          rowKey="id"
          pagination={false}
          
          columns={[
            {
              title: 'Type',
              dataIndex: 'type',
              key: 'type',
              width: 80,
              render: (type) => {
                const colors = {
                  info: 'blue',
                  warning: 'orange',
                  error: 'red',
                  critical: 'red',
                };
                return <Tag color={colors[type]}>{type.toUpperCase()}</Tag>;
              },
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
              width: 100,
              render: (timestamp) => new Date(timestamp).toLocaleTimeString(),
            },
            {
              title: 'Status',
              key: 'status',
              width: 100,
              render: (_, record) => (
                <Space direction="vertical" >
                  {record.acknowledged && <Tag color="blue">ACK</Tag>}
                  {record.resolved && <Tag color="green">RESOLVED</Tag>}
                </Space>
              ),
            },
            {
              title: 'Actions',
              key: 'actions',
              width: 120,
              render: (_, record) => (
                <Space>
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
      </Modal>

      {/* Service Details Modal */}
      <Modal
        title={selectedService?.name}
        open={!!selectedService}
        onCancel={() => setSelectedService(null)}
        footer={null}
        width={600}
      >
        {selectedService && (
          <div>
            <Row gutter={[16, 16]}>
              <Col span={12}>
                <Statistic
                  title="Status"
                  value={selectedService.status.toUpperCase()}
                  prefix={getStatusIcon(selectedService.status)}
                  valueStyle={{ color: getStatusColor(selectedService.status) }}
                />
              </Col>
              <Col span={12}>
                <Statistic
                  title="Uptime"
                  value={selectedService.uptime}
                />
              </Col>
              <Col span={12}>
                <Statistic
                  title="Version"
                  value={selectedService.version}
                />
              </Col>
              <Col span={12}>
                <Statistic
                  title="Last Check"
                  value={new Date(selectedService.lastCheck).toLocaleTimeString()}
                />
              </Col>
              {selectedService.responseTime && (
                <Col span={12}>
                  <Statistic
                    title="Response Time"
                    value={selectedService.responseTime}
                    suffix="ms"
                  />
                </Col>
              )}
              {selectedService.errorRate !== undefined && (
                <Col span={12}>
                  <Statistic
                    title="Error Rate"
                    value={selectedService.errorRate}
                    suffix="%"
                    valueStyle={{
                      color: selectedService.errorRate > 1 ? '#ff4d4f' : '#52c41a'
                    }}
                  />
                </Col>
              )}
            </Row>
            {selectedService.endpoint && (
              <div style={{ marginTop: 16 }}>
                <Divider />
                <Text strong>Endpoint: </Text>
                <Text code>{selectedService.endpoint}</Text>
              </div>
            )}
          </div>
        )}
      </Modal>
    </div>
  );
};

export default SystemStatus;
// @ts-nocheck
import React, { useState, useCallback, useEffect } from 'react';
import {
  Card,
  Row,
  Col,
  Button,
  Space,
  Tabs,
  Alert,
  Spin,
  Typography,
  Divider,
  Modal,
  Input,
  Select,
  Table,
  Tag,
  Dropdown,
  Menu,
  message,
  Statistic,
  Progress,
  Timeline,
  List,
  Avatar,
  Tooltip,
  Badge,
} from 'antd';
import {
  SettingOutlined,
  SafetyCertificateOutlined,
  DatabaseOutlined,
  ApiOutlined,
  HistoryOutlined,
  CloudDownloadOutlined,
  UserOutlined,
  LockOutlined,
  ThunderboltOutlined,
  ExperimentOutlined,
  FileTextOutlined,
  BellOutlined,
  SyncOutlined,
  CheckCircleOutlined,
  ExclamationCircleOutlined,
  InfoCircleOutlined,
  EditOutlined,
  EyeOutlined,
  CopyOutlined,
  DeleteOutlined,
  CloudUploadOutlined,
  CloudDownloadOutlined,
  BranchesOutlined,
  DiffOutlined,
  SecurityScanOutlined,
} from '@ant-design/icons';
import ConfigEditor from '../../components/config/ConfigEditor';
import ConfigForm from '../../components/config/ConfigForm';
import useSystemConfig from '../../hooks/useSystemConfig';
import { SystemConfiguration, ConfigurationTemplate, ConfigurationAudit } from '../../types/systemConfig';

const { Title, Text, Paragraph } = Typography;
const { TabPane } = Tabs;
const { Option } = Select;
const { TextArea } = Input;

const SystemSettings: React.FC = () => {
  const [activeTab, setActiveTab] = useState('configuration');
  const [selectedTemplate, setSelectedTemplate] = useState<ConfigurationTemplate | null>(null);
  const [templateModalVisible, setTemplateModalVisible] = useState(false);
  const [auditModalVisible, setAuditModalVisible] = useState(false);
  const [conflictModalVisible, setConflictModalVisible] = useState(false);
  const [approvalModalVisible, setApprovalModalVisible] = useState(false);
  const [selectedAudit, setSelectedAudit] = useState<ConfigurationAudit | null>(null);

  const {
    configs,
    currentConfig,
    configHistory,
    validationErrors,
    isLoading,
    isSaving,
    isValidating,
    saveConfig,
    validateConfig,
    rollbackToVersion,
    createBackup,
    restoreFromBackup,
    compareVersions,
    applyTemplate,
    saveAsTemplate,
    resolveConflict,
    analyzeImpact,
  } = useSystemConfig();

  // Mock data for demonstration
  const [templates] = useState<ConfigurationTemplate[]>([
    {
      id: 'default',
      name: 'Default Configuration',
      description: 'Standard configuration for new installations',
      category: 'General',
      config: {},
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: 'production',
      name: 'Production Configuration',
      description: 'Optimized settings for production environments',
      category: 'Environment',
      config: {},
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: 'development',
      name: 'Development Configuration',
      description: 'Debug-friendly settings for development',
      category: 'Environment',
      config: {},
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
  ]);

  const [auditLogs] = useState<ConfigurationAudit[]>([
    {
      id: '1',
      action: 'UPDATE',
      configId: currentConfig?.id || '1',
      userId: 'admin',
      userName: 'System Administrator',
      changes: { siteName: { from: 'Old Name', to: 'New Name' } },
      timestamp: new Date().toISOString(),
      ip: '192.168.1.100',
      userAgent: 'Mozilla/5.0...',
    },
    {
      id: '2',
      action: 'ROLLBACK',
      configId: currentConfig?.id || '1',
      userId: 'admin',
      userName: 'System Administrator',
      changes: {},
      timestamp: new Date(Date.now() - 3600000).toISOString(),
      ip: '192.168.1.100',
      userAgent: 'Mozilla/5.0...',
    },
  ]);

  const [systemStats, setSystemStats] = useState({
    totalConfigurations: configs.length,
    activeConfiguration: currentConfig?.version || 'N/A',
    lastModified: currentConfig?.updatedAt || new Date().toISOString(),
    validationStatus: validationErrors.length === 0 ? 'Valid' : 'Invalid',
    backupCount: configHistory.length,
    templateCount: templates.length,
  });

  // Update stats when configs change
  useEffect(() => {
    setSystemStats({
      totalConfigurations: configs.length,
      activeConfiguration: currentConfig?.version || 'N/A',
      lastModified: currentConfig?.updatedAt || new Date().toISOString(),
      validationStatus: validationErrors.length === 0 ? 'Valid' : 'Invalid',
      backupCount: configHistory.length,
      templateCount: templates.length,
    });
  }, [configs, currentConfig, validationErrors, configHistory, templates]);

  // Handle template application
  const handleApplyTemplate = useCallback(async (templateId: string) => {
    try {
      await applyTemplate(templateId);
      message.success('Template applied successfully');
      setTemplateModalVisible(false);
    } catch (error) {
      console.error('Failed to apply template:', error);
      message.error('Failed to apply template');
    }
  }, [applyTemplate]);

  // Handle save as template
  const handleSaveAsTemplate = useCallback(async (name: string, description: string) => {
    try {
      await saveAsTemplate(name, description);
      message.success('Configuration saved as template');
      setTemplateModalVisible(false);
    } catch (error) {
      console.error('Failed to save template:', error);
      message.error('Failed to save template');
    }
  }, [saveAsTemplate]);

  // Render overview tab
  const renderOverviewTab = () => (
    <Row gutter={[16, 16]}>
      {/* System Status Cards */}
      <Col span={24}>
        <Row gutter={[16, 16]}>
          <Col span={6}>
            <Card>
              <Statistic
                title="Active Configuration"
                value={systemStats.activeConfiguration}
                prefix={<SettingOutlined />}
                valueStyle={{ color: '#1890ff' }}
              />
            </Card>
          </Col>
          <Col span={6}>
            <Card>
              <Statistic
                title="Validation Status"
                value={systemStats.validationStatus}
                prefix={
                  systemStats.validationStatus === 'Valid' ? (
                    <CheckCircleOutlined style={{ color: '#52c41a' }} />
                  ) : (
                    <ExclamationCircleOutlined style={{ color: '#ff4d4f' }} />
                  )
                }
                valueStyle={{
                  color: systemStats.validationStatus === 'Valid' ? '#52c41a' : '#ff4d4f',
                }}
              />
            </Card>
          </Col>
          <Col span={6}>
            <Card>
              <Statistic
                title="Total Backups"
                value={systemStats.backupCount}
                prefix={<CloudDownloadOutlined />}
                valueStyle={{ color: '#722ed1' }}
              />
            </Card>
          </Col>
          <Col span={6}>
            <Card>
              <Statistic
                title="Available Templates"
                value={systemStats.templateCount}
                prefix={<ExperimentOutlined />}
                valueStyle={{ color: '#fa8c16' }}
              />
            </Card>
          </Col>
        </Row>
      </Col>

      {/* Quick Actions */}
      <Col span={24}>
        <Card title="Quick Actions" >
          <Space wrap>
            <Button
              type="primary"
              icon={<EditOutlined />}
              onClick={() => setActiveTab('configuration')}
            >
              Edit Configuration
            </Button>
            <Button
              icon={<EyeOutlined />}
              onClick={() => setActiveTab('history')}
            >
              View History
            </Button>
            <Button
              icon={<CloudDownloadOutlined />}
              onClick={async () => {
                const description = `Manual backup - ${new Date().toLocaleString()}`;
                await createBackup(description);
              }}
            >
              Create Backup
            </Button>
            <Button
              icon={<ExperimentOutlined />}
              onClick={() => setTemplateModalVisible(true)}
            >
              Manage Templates
            </Button>
            <Button
              icon={<HistoryOutlined />}
              onClick={() => setAuditModalVisible(true)}
            >
              View Audit Log
            </Button>
            <Button
              icon={<SecurityScanOutlined />}
              onClick={() => setActiveTab('security')}
            >
              Security Settings
            </Button>
          </Space>
        </Card>
      </Col>

      {/* Current Configuration Summary */}
      <Col span={12}>
        <Card title="Current Configuration Summary" >
          {currentConfig ? (
            <List
              
              dataSource={[
                { label: 'Site Name', value: currentConfig.siteName || 'Not Set' },
                { label: 'Environment', value: currentConfig.environment || 'Not Set' },
                { label: 'Version', value: currentConfig.version || 'N/A' },
                { label: 'Last Modified', value: new Date(currentConfig.updatedAt).toLocaleString() },
                { label: 'Modified By', value: currentConfig.modifiedBy || 'System' },
              ]}
              renderItem={(item) => (
                <List.Item>
                  <Text strong>{item.label}:</Text> <Text>{item.value}</Text>
                </List.Item>
              )}
            />
          ) : (
            <Text type="secondary">No configuration loaded</Text>
          )}
        </Card>
      </Col>

      {/* Recent Changes */}
      <Col span={12}>
        <Card title="Recent Changes" >
          <Timeline >
            {configHistory.slice(0, 5).map((config, index) => (
              <Timeline.Item
                key={config.id}
                color={index === 0 ? 'green' : 'blue'}
                dot={index === 0 ? <CheckCircleOutlined /> : <HistoryOutlined />}
              >
                <Text strong>v{config.version}</Text>
                <br />
                <Text type="secondary">
                  {new Date(config.createdAt).toLocaleString()}
                </Text>
                <br />
                <Text>{config.description || 'Configuration update'}</Text>
              </Timeline.Item>
            ))}
          </Timeline>
        </Card>
      </Col>

      {/* Validation Status */}
      {validationErrors.length > 0 && (
        <Col span={24}>
          <Alert
            type="error"
            message={`Configuration has ${validationErrors.length} validation error(s)`}
            description={
              <List
                
                dataSource={validationErrors.slice(0, 3)}
                renderItem={(error) => (
                  <List.Item>
                    <strong>{error.field}</strong>: {error.message}
                  </List.Item>
                )}
              />
            }
            showIcon
            action={
              <Button
                
                type="primary"
                onClick={() => setActiveTab('configuration')}
              >
                Fix Issues
              </Button>
            }
          />
        </Col>
      )}
    </Row>
  );

  // Render configuration tab
  const renderConfigurationTab = () => (
    <ConfigEditor height={800} />
  );

  // Render history tab
  const renderHistoryTab = () => (
    <Card>
      <Table
        dataSource={configHistory}
        rowKey="id"
        pagination={{ pageSize: 10 }}
        columns={[
          {
            title: 'Version',
            dataIndex: 'version',
            key: 'version',
            render: (version, record) => (
              <Space>
                <Badge
                  status={record.isActive ? 'success' : 'default'}
                  text={`v${version}`}
                />
                {record.isActive && <Tag color="green">ACTIVE</Tag>}
              </Space>
            ),
          },
          {
            title: 'Description',
            dataIndex: 'description',
            key: 'description',
            ellipsis: true,
          },
          {
            title: 'Created',
            dataIndex: 'createdAt',
            key: 'createdAt',
            render: (date) => new Date(date).toLocaleString(),
            sorter: (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
          },
          {
            title: 'Modified By',
            dataIndex: 'modifiedBy',
            key: 'modifiedBy',
            render: (user) => (
              <Space>
                <Avatar  icon={<UserOutlined />} />
                {user || 'System'}
              </Space>
            ),
          },
          {
            title: 'Size',
            dataIndex: 'size',
            key: 'size',
            render: (size) => `${(size || 0)} KB`,
          },
          {
            title: 'Actions',
            key: 'actions',
            render: (_, record) => (
              <Space>
                <Button
                  
                  icon={<EyeOutlined />}
                  onClick={() => {
                    // View configuration details
                  }}
                >
                  View
                </Button>
                {!record.isActive && (
                  <Button
                    
                    icon={<SyncOutlined />}
                    onClick={() => rollbackToVersion(record.id)}
                  >
                    Restore
                  </Button>
                )}
                <Dropdown
                  overlay={
                    <Menu>
                      <Menu.Item
                        key="copy"
                        icon={<CopyOutlined />}
                      >
                        Duplicate
                      </Menu.Item>
                      <Menu.Item
                        key="compare"
                        icon={<DiffOutlined />}
                        onClick={() => {
                          if (currentConfig) {
                            compareVersions(currentConfig.id, record.id);
                          }
                        }}
                      >
                        Compare with Current
                      </Menu.Item>
                      <Menu.Item
                        key="export"
                        icon={<CloudDownloadOutlined />}
                      >
                        Export
                      </Menu.Item>
                      {!record.isActive && (
                        <Menu.Item
                          key="delete"
                          icon={<DeleteOutlined />}
                          danger
                        >
                          Delete
                        </Menu.Item>
                      )}
                    </Menu>
                  }
                >
                  <Button >More</Button>
                </Dropdown>
              </Space>
            ),
          },
        ]}
      />
    </Card>
  );

  // Render templates tab
  const renderTemplatesTab = () => (
    <Row gutter={[16, 16]}>
      <Col span={24}>
        <Card
          title="Configuration Templates"
          extra={
            <Space>
              <Button
                type="primary"
                icon={<CloudUploadOutlined />}
                onClick={() => setTemplateModalVisible(true)}
              >
                Save Current as Template
              </Button>
            </Space>
          }
        >
          <Row gutter={[16, 16]}>
            {templates.map((template) => (
              <Col key={template.id} span={8}>
                <Card
                  
                  title={template.name}
                  extra={
                    <Dropdown
                      overlay={
                        <Menu>
                          <Menu.Item
                            key="apply"
                            icon={<ThunderboltOutlined />}
                            onClick={() => handleApplyTemplate(template.id)}
                          >
                            Apply Template
                          </Menu.Item>
                          <Menu.Item
                            key="edit"
                            icon={<EditOutlined />}
                          >
                            Edit Template
                          </Menu.Item>
                          <Menu.Item
                            key="copy"
                            icon={<CopyOutlined />}
                          >
                            Duplicate
                          </Menu.Item>
                          <Menu.Item
                            key="export"
                            icon={<CloudDownloadOutlined />}
                          >
                            Export
                          </Menu.Item>
                          <Menu.Item
                            key="delete"
                            icon={<DeleteOutlined />}
                            danger
                          >
                            Delete
                          </Menu.Item>
                        </Menu>
                      }
                    >
                      <Button  type="text">•••</Button>
                    </Dropdown>
                  }
                >
                  <Paragraph ellipsis={{ rows: 2 }}>
                    {template.description}
                  </Paragraph>
                  <Space direction="vertical" style={{ width: '100%' }}>
                    <Tag color="blue">{template.category}</Tag>
                    <Text type="secondary" style={{ fontSize: '12px' }}>
                      Updated {new Date(template.updatedAt).toLocaleDateString()}
                    </Text>
                    <Button
                      type="primary"
                      
                      block
                      onClick={() => handleApplyTemplate(template.id)}
                    >
                      Apply Template
                    </Button>
                  </Space>
                </Card>
              </Col>
            ))}
          </Row>
        </Card>
      </Col>
    </Row>
  );

  // Render security tab
  const renderSecurityTab = () => (
    <Row gutter={[16, 16]}>
      <Col span={24}>
        <Alert
          type="info"
          message="Security Configuration"
          description="Configure security settings and access controls for the system configuration management."
          showIcon
          style={{ marginBottom: 16 }}
        />
      </Col>
      <Col span={12}>
        <Card title="Access Control" >
          <Space direction="vertical" style={{ width: '100%' }}>
            <div>
              <Text strong>Configuration Admin Role:</Text>
              <br />
              <Text type="secondary">Users who can modify system configuration</Text>
            </div>
            <div>
              <Text strong>Configuration Viewer Role:</Text>
              <br />
              <Text type="secondary">Users who can view configuration (read-only)</Text>
            </div>
            <div>
              <Text strong>Approval Required:</Text>
              <br />
              <Text type="secondary">Changes require approval before applying</Text>
            </div>
          </Space>
        </Card>
      </Col>
      <Col span={12}>
        <Card title="Audit & Compliance" >
          <Space direction="vertical" style={{ width: '100%' }}>
            <div>
              <Text strong>Change Tracking:</Text>
              <br />
              <Tag color="green">Enabled</Tag>
            </div>
            <div>
              <Text strong>Backup Retention:</Text>
              <br />
              <Text type="secondary">30 days</Text>
            </div>
            <div>
              <Text strong>Validation Required:</Text>
              <br />
              <Tag color="green">Enabled</Tag>
            </div>
          </Space>
        </Card>
      </Col>
    </Row>
  );

  if (isLoading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
        <div style={{ marginTop: 16 }}>
          <Text>Loading system configuration...</Text>
        </div>
      </div>
    );
  }

  return (
    <div className="system-settings">
      <div style={{ marginBottom: 24 }}>
        <Title level={2}>
          <Space>
            <SettingOutlined />
            System Settings
          </Space>
        </Title>
        <Paragraph type="secondary">
          Manage system configuration, templates, and security settings.
        </Paragraph>
      </div>

      <Tabs
        activeKey={activeTab}
        onChange={setActiveTab}
        type="card"
      >
        <TabPane
          tab={
            <Space>
              <InfoCircleOutlined />
              Overview
            </Space>
          }
          key="overview"
        >
          {renderOverviewTab()}
        </TabPane>

        <TabPane
          tab={
            <Space>
              <SettingOutlined />
              Configuration
            </Space>
          }
          key="configuration"
        >
          {renderConfigurationTab()}
        </TabPane>

        <TabPane
          tab={
            <Space>
              <HistoryOutlined />
              History
              <Badge count={configHistory.length}  />
            </Space>
          }
          key="history"
        >
          {renderHistoryTab()}
        </TabPane>

        <TabPane
          tab={
            <Space>
              <ExperimentOutlined />
              Templates
              <Badge count={templates.length}  />
            </Space>
          }
          key="templates"
        >
          {renderTemplatesTab()}
        </TabPane>

        <TabPane
          tab={
            <Space>
              <LockOutlined />
              Security
            </Space>
          }
          key="security"
        >
          {renderSecurityTab()}
        </TabPane>
      </Tabs>

      {/* Template Modal */}
      <Modal
        title="Save Configuration as Template"
        open={templateModalVisible}
        onCancel={() => setTemplateModalVisible(false)}
        onOk={() => {
          const nameInput = document.getElementById('template-name') as HTMLInputElement;
          const descInput = document.getElementById('template-desc') as HTMLTextAreaElement;
          if (nameInput?.value && descInput?.value) {
            handleSaveAsTemplate(nameInput.value, descInput.value);
          }
        }}
      >
        <Space direction="vertical" style={{ width: '100%' }}>
          <div>
            <Text strong>Template Name:</Text>
            <Input id="template-name" placeholder="Enter template name" />
          </div>
          <div>
            <Text strong>Description:</Text>
            <TextArea id="template-desc" rows={3} placeholder="Enter template description" />
          </div>
        </Space>
      </Modal>

      {/* Audit Modal */}
      <Modal
        title="Configuration Audit Log"
        open={auditModalVisible}
        onCancel={() => setAuditModalVisible(false)}
        footer={null}
        width={1000}
      >
        <Table
          dataSource={auditLogs}
          rowKey="id"
          pagination={{ pageSize: 10 }}
          
          columns={[
            {
              title: 'Action',
              dataIndex: 'action',
              key: 'action',
              render: (action) => (
                <Tag color={action === 'UPDATE' ? 'blue' : action === 'ROLLBACK' ? 'orange' : 'green'}>
                  {action}
                </Tag>
              ),
            },
            {
              title: 'User',
              dataIndex: 'userName',
              key: 'userName',
              render: (name, record) => (
                <Space>
                  <Avatar  icon={<UserOutlined />} />
                  {name}
                </Space>
              ),
            },
            {
              title: 'Timestamp',
              dataIndex: 'timestamp',
              key: 'timestamp',
              render: (timestamp) => new Date(timestamp).toLocaleString(),
            },
            {
              title: 'IP Address',
              dataIndex: 'ip',
              key: 'ip',
            },
            {
              title: 'Changes',
              dataIndex: 'changes',
              key: 'changes',
              render: (changes) => (
                <Text code>{Object.keys(changes).length} field(s)</Text>
              ),
            },
            {
              title: 'Actions',
              key: 'actions',
              render: (_, record) => (
                <Button
                  
                  onClick={() => setSelectedAudit(record)}
                >
                  View Details
                </Button>
              ),
            },
          ]}
        />
      </Modal>
    </div>
  );
};

export default SystemSettings;
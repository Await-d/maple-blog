// @ts-nocheck
import React, { useState, useCallback, useEffect } from 'react';
import {
  Card,
  Row,
  Col,
  Button,
  Space,
  // message,
  Modal,
  Input,
  Select,
  Table,
  Tag,
  Dropdown,
  Menu,
  Divider,
  Alert,
  Tabs,
  Tree,
  Typography,
  Progress,
} from 'antd';
import {
  SaveOutlined,
  ReloadOutlined,
  BranchesOutlined,
  HistoryOutlined,
  ImportOutlined,
  ExportOutlined,
  CheckCircleOutlined,
  SettingOutlined,
  CodeOutlined,
  ThunderboltOutlined,
  CloudUploadOutlined,
} from '@ant-design/icons';
// import { div } from '@monaco-editor/react';
import { SystemConfiguration, ConfigurationDiff } from '../../types/systemConfig';
import useSystemConfig from '../../hooks/useSystemConfig';
import ConfigForm from './ConfigForm';

const { Option } = Select;
// const { TextArea } = Input;
const { Text } = Typography;
const { TabPane } = Tabs;

interface ConfigEditorProps {
  className?: string;
  height?: number;
  defaultMode?: 'visual' | 'json' | 'tree';
}

const ConfigEditor: React.FC<ConfigEditorProps> = ({
  className,
  height = 600,
  defaultMode = 'visual',
}) => {
  const [mode, setMode] = useState<'visual' | 'json' | 'tree'>(defaultMode);
  const [selectedConfig, setSelectedConfig] = useState<SystemConfiguration | null>(null);
  const [editingConfig, setEditingConfig] = useState<Partial<SystemConfiguration>>({});
  const [isDirty, setIsDirty] = useState(false);
  const [showDiff, setShowDiff] = useState(false);
  // const [compareVersions, setCompareVersions] = useState<[string, string] | null>(null);
  const [diffData] = useState<ConfigurationDiff | null>(null);
  const [backupModalVisible, setBackupModalVisible] = useState(false);
  const [restoreModalVisible, setRestoreModalVisible] = useState(false);
  const [impactAnalysis, setImpactAnalysis] = useState<any>(null);
  const [isAnalyzing, setIsAnalyzing] = useState(false);

  // const editorRef = useRef<any>(null);

  const {
    configs,
    currentConfig,
    configHistory,
    validationErrors,
    // isLoading,
    isSaving,
    isValidating,
    saveConfig,
    validateConfig,
    // rollbackToVersion,
    createBackup,
    restoreFromBackup,
    // compareVersions: compareConfigVersions,
    // applyTemplate,
    // saveAsTemplate,
    analyzeImpact,
  } = useSystemConfig();

  // Initialize with current config
  useEffect(() => {
    if (currentConfig && !selectedConfig) {
      setSelectedConfig(currentConfig);
      setEditingConfig(currentConfig);
    }
  }, [currentConfig, selectedConfig]);

  // Handle config selection
  const handleConfigSelect = useCallback((configId: string) => {
    const config = configs.find(c => c.id === configId) || configHistory.find(c => c.id === configId);
    if (config) {
      setSelectedConfig(config);
      setEditingConfig(config);
      setIsDirty(false);
    }
  }, [configs, configHistory]);

  // Handle config change
  const handleConfigChange = useCallback((changes: Partial<SystemConfiguration>) => {
    setEditingConfig(prev => ({ ...prev, ...changes }));
    setIsDirty(true);
  }, []);

  // Handle JSON editor change
  // const handleJsonChange = useCallback((value: string | undefined) => {
  //   if (!value) return;
  //
  //   try {
  //     const parsed = JSON.parse(value);
  //     setEditingConfig(parsed);
  //     setIsDirty(true);
  //   } catch (error) {
  //     console.error('Invalid JSON:', error);
  //   }
  // }, []);

  // Save configuration
  const handleSave = useCallback(async () => {
    try {
      await saveConfig(editingConfig);
      setIsDirty(false);
    } catch (error) {
      console.error('Failed to save config:', error);
    }
  }, [editingConfig, saveConfig]);

  // Validate configuration
  const handleValidate = useCallback(async (): Promise<boolean> => {
    try {
      await validateConfig(editingConfig);
      return true;
    } catch (error) {
      console.error('Failed to validate config:', error);
      return false;
    }
  }, [editingConfig, validateConfig]);

  // Analyze impact
  const handleAnalyzeImpact = useCallback(async () => {
    try {
      setIsAnalyzing(true);
      const impact = await analyzeImpact(editingConfig);
      setImpactAnalysis(impact);
    } catch (error) {
      console.error('Failed to analyze impact:', error);
    } finally {
      setIsAnalyzing(false);
    }
  }, [editingConfig, analyzeImpact]);

  // Compare versions
  const handleCompareVersions = useCallback(async () => {
    try {
      // const diff = await compareConfigVersions(versionA, versionB);
      // setDiffData(diff);
      // setCompareVersions([versionA, versionB]);
      setShowDiff(true);
    } catch (error) {
      console.error('Failed to compare versions:', error);
    }
  }, []);

  // Rollback to version
  // const handleRollback = useCallback(async (versionId: string) => {
  //   try {
  //     await rollbackToVersion(versionId);
  //     message.success('Configuration rolled back successfully');
  //   } catch (error) {
  //     console.error('Failed to rollback:', error);
  //   }
  // }, [rollbackToVersion]);

  // Create backup
  const handleCreateBackup = useCallback(async (description: string) => {
    try {
      await createBackup(description);
      setBackupModalVisible(false);
    } catch (error) {
      console.error('Failed to create backup:', error);
    }
  }, [createBackup]);

  // Restore from backup
  const handleRestoreBackup = useCallback(async (backupId: string) => {
    try {
      await restoreFromBackup(backupId);
      setRestoreModalVisible(false);
    } catch (error) {
      console.error('Failed to restore backup:', error);
    }
  }, [restoreFromBackup]);

  // Reset configuration
  const handleReset = useCallback(() => {
    if (selectedConfig) {
      setEditingConfig(selectedConfig);
      setIsDirty(false);
    }
  }, [selectedConfig]);

  // Render mode tabs
  const renderModeTabs = () => (
    <Tabs
      activeKey={mode}
      onChange={(key) => setMode(key as any)}
      
      tabBarExtraContent={
        <Space>
          <Select
            value={selectedConfig?.id || null}
            onChange={handleConfigSelect}
            style={{ width: 200 }}
            placeholder="Select configuration"
          >
            <Option value={currentConfig?.id}>Current Configuration</Option>
            <Divider style={{ margin: '4px 0' }} />
            {configHistory.map(config => (
              <Option key={config.id} value={config.id}>
                v{config.version} - {new Date(config.createdAt).toLocaleDateString()}
              </Option>
            ))}
          </Select>
        </Space>
      }
    >
      <TabPane
        tab={
          <Space>
            <SettingOutlined />
            Visual Editor
          </Space>
        }
        key="visual"
      />
      <TabPane
        tab={
          <Space>
            <CodeOutlined />
            JSON Editor
          </Space>
        }
        key="json"
      />
      <TabPane
        tab={
          <Space>
            <BranchesOutlined />
            Tree View
          </Space>
        }
        key="tree"
      />
    </Tabs>
  );

  // Render action buttons
  const renderActions = () => (
    <Card  style={{ marginBottom: 16 }}>
      <Row justify="space-between" align="middle">
        <Col>
          <Space wrap>
            <Button
              type="primary"
              icon={<SaveOutlined />}
              onClick={handleSave}
              loading={isSaving}
              disabled={!isDirty}
            >
              Save
            </Button>
            <Button
              icon={<CheckCircleOutlined />}
              onClick={handleValidate}
              loading={isValidating}
            >
              Validate
            </Button>
            <Button
              icon={<ThunderboltOutlined />}
              onClick={handleAnalyzeImpact}
              loading={isAnalyzing}
            >
              Analyze Impact
            </Button>
            <Button
              icon={<ReloadOutlined />}
              onClick={handleReset}
              disabled={!isDirty}
            >
              Reset
            </Button>
          </Space>
        </Col>
        <Col>
          <Space>
            <Dropdown
              overlay={
                <Menu>
                  <Menu.Item
                    key="backup"
                    icon={<CloudUploadOutlined />}
                    onClick={() => setBackupModalVisible(true)}
                  >
                    Create Backup
                  </Menu.Item>
                  <Menu.Item
                    key="restore"
                    icon={<ImportOutlined />}
                    onClick={() => setRestoreModalVisible(true)}
                  >
                    Restore Backup
                  </Menu.Item>
                  <Menu.Divider />
                  <Menu.Item
                    key="export"
                    icon={<ExportOutlined />}
                    onClick={() => {
                      const dataStr = JSON.stringify(editingConfig, null, 2);
                      const dataUri = 'data:application/json;charset=utf-8,'+ encodeURIComponent(dataStr);
                      const linkElement = document.createElement('a');
                      linkElement.setAttribute('href', dataUri);
                      linkElement.setAttribute('download', `config-${Date.now()}.json`);
                      linkElement.click();
                    }}
                  >
                    Export Config
                  </Menu.Item>
                  <Menu.Item
                    key="history"
                    icon={<HistoryOutlined />}
                    onClick={() => setShowDiff(true)}
                  >
                    View History
                  </Menu.Item>
                </Menu>
              }
            >
              <Button icon={<SettingOutlined />}>
                More Actions
              </Button>
            </Dropdown>
            {isDirty && (
              <Tag color="orange">
                Unsaved Changes
              </Tag>
            )}
          </Space>
        </Col>
      </Row>
    </Card>
  );

  // Render validation status
  const renderValidationStatus = () => {
    if (validationErrors.length === 0) {
      return (
        <Alert
          type="success"
          message="Configuration is valid"
          icon={<CheckCircleOutlined />}
          style={{ marginBottom: 16 }}
          showIcon
        />
      );
    }

    return (
      <Alert
        type="error"
        message={`Found ${validationErrors.length} validation error(s)`}
        description={
          <ul style={{ margin: '8px 0 0 0', paddingLeft: 20 }}>
            {validationErrors.slice(0, 5).map((error, index) => (
              <li key={index}>
                <strong>{error.field}</strong>: {error.message}
              </li>
            ))}
            {validationErrors.length > 5 && (
              <li>... and {validationErrors.length - 5} more errors</li>
            )}
          </ul>
        }
        style={{ marginBottom: 16 }}
        showIcon
      />
    );
  };

  // Render impact analysis
  const renderImpactAnalysis = () => {
    if (!impactAnalysis) return null;

    return (
      <Card
        
        title="Impact Analysis"
        style={{ marginBottom: 16 }}
        extra={
          <Tag color={impactAnalysis.riskLevel === 'high' ? 'red' : impactAnalysis.riskLevel === 'medium' ? 'orange' : 'green'}>
            {impactAnalysis.riskLevel?.toUpperCase()} RISK
          </Tag>
        }
      >
        <Row gutter={16}>
          <Col span={8}>
            <Text strong>Affected Services:</Text>
            <div>
              {impactAnalysis.affectedServices?.map((service: string) => (
                <Tag key={service} style={{ margin: '2px' }}>
                  {service}
                </Tag>
              ))}
            </div>
          </Col>
          <Col span={8}>
            <Text strong>Performance Impact:</Text>
            <Progress
              percent={impactAnalysis.performanceImpact || 0}
              
              status={impactAnalysis.performanceImpact > 50 ? 'exception' : 'normal'}
            />
          </Col>
          <Col span={8}>
            <Text strong>Restart Required:</Text>
            <Tag color={impactAnalysis.requiresRestart ? 'red' : 'green'}>
              {impactAnalysis.requiresRestart ? 'YES' : 'NO'}
            </Tag>
          </Col>
        </Row>
        {impactAnalysis.warnings?.length > 0 && (
          <Alert
            type="warning"
            message="Warnings"
            description={
              <ul style={{ margin: 0, paddingLeft: 20 }}>
                {impactAnalysis.warnings.map((warning: string, index: number) => (
                  <li key={index}>{warning}</li>
                ))}
              </ul>
            }
            style={{ marginTop: 12 }}
            showIcon
          />
        )}
      </Card>
    );
  };

  // Render visual editor
  const renderVisualEditor = () => (
    <ConfigForm
      config={editingConfig as SystemConfiguration}
      onChange={handleConfigChange}
      onSave={handleSave}
      onValidate={handleValidate}
      showValidationResults={false}
    />
  );

  // Render JSON editor
  const renderJsonEditor = () => (
    <Card>
      <div style={{
        height: height - 200,
        border: '1px solid #d9d9d9',
        padding: '8px',
        overflow: 'auto',
        fontFamily: 'monospace'
      }}>
        <pre>{JSON.stringify(editingConfig, null, 2)}</pre>
      </div>
      {/* <JSONEditor
        height={height - 200}
        language="json"
        value={JSON.stringify(editingConfig, null, 2)}
        onChange={handleJsonChange}
        options={{
          minimap: { enabled: false },
          scrollBeyondLastLine: false,
          fontSize: 13,
          wordWrap: 'on',
          formatOnPaste: true,
          formatOnType: true,
        }}
        onMount={(editor) => {
          editorRef.current = editor;
        }}
      /> */}
    </Card>
  );

  // Render tree view
  const renderTreeView = () => {
    const convertToTreeData = (obj: any, prefix = ''): any[] => {
      return Object.entries(obj).map(([key, value]) => {
        const fullKey = prefix ? `${prefix}.${key}` : key;
        const node = {
          title: (
            <Space>
              <Text strong>{key}</Text>
              <Text type="secondary">
                {typeof value === 'object' && value !== null ? `{${Object.keys(value).length} items}` : String(value)}
              </Text>
            </Space>
          ),
          key: fullKey,
          children: typeof value === 'object' && value !== null ? convertToTreeData(value, fullKey) : undefined,
        };
        return node;
      });
    };

    const treeData = convertToTreeData(editingConfig);

    return (
      <Card>
        <Tree
          treeData={treeData}
          defaultExpandAll
          showLine
          height={height - 200}
          style={{ overflow: 'auto' }}
        />
      </Card>
    );
  };

  // Render main content
  const renderContent = () => {
    switch (mode) {
      case 'visual':
        return renderVisualEditor();
      case 'json':
        return renderJsonEditor();
      case 'tree':
        return renderTreeView();
      default:
        return renderVisualEditor();
    }
  };

  return (
    <div className={className}>
      {renderModeTabs()}
      {renderActions()}
      {renderValidationStatus()}
      {renderImpactAnalysis()}
      {renderContent()}

      {/* Backup Modal */}
      <Modal
        title="Create Configuration Backup"
        open={backupModalVisible}
        onCancel={() => setBackupModalVisible(false)}
        onOk={() => {
          const description = (document.getElementById('backup-description') as HTMLInputElement)?.value;
          if (description) {
            handleCreateBackup(description);
          }
        }}
      >
        <Input
          id="backup-description"
          placeholder="Enter backup description"
          style={{ width: '100%' }}
        />
      </Modal>

      {/* Restore Modal */}
      <Modal
        title="Restore from Backup"
        open={restoreModalVisible}
        onCancel={() => setRestoreModalVisible(false)}
        footer={null}
        width={800}
      >
        <Table
          dataSource={configHistory}
          rowKey="id"
          pagination={false}
          
          columns={[
            {
              title: 'Version',
              dataIndex: 'version',
              key: 'version',
            },
            {
              title: 'Created',
              dataIndex: 'createdAt',
              key: 'createdAt',
              render: (date) => new Date(date).toLocaleString(),
            },
            {
              title: 'Description',
              dataIndex: 'description',
              key: 'description',
            },
            {
              title: 'Actions',
              key: 'actions',
              render: (_, record) => (
                <Space>
                  <Button
                    
                    onClick={() => handleRestoreBackup(record.id)}
                  >
                    Restore
                  </Button>
                  <Button
                    
                    onClick={() => handleCompareVersions()}
                  >
                    Compare
                  </Button>
                </Space>
              ),
            },
          ]}
        />
      </Modal>

      {/* Diff Modal */}
      <Modal
        title="Configuration Differences"
        open={showDiff}
        onCancel={() => setShowDiff(false)}
        footer={null}
        width={1000}
      >
        {diffData && (
          <div>
            <Alert
              type="info"
              message={`Comparing ${diffData.fromVersion} with ${diffData.toVersion}`}
              style={{ marginBottom: 16 }}
            />
            <pre style={{ maxHeight: 400, overflow: 'auto', padding: 16, background: '#f5f5f5' }}>
              {JSON.stringify(diffData.changes, null, 2)}
            </pre>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default ConfigEditor;
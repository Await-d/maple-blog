import React, { useState, useCallback, useEffect } from 'react';
import {
  Form,
  Input,
  Switch,
  InputNumber,
  Select,
  Divider,
  Card,
  Collapse,
  Button,
  Space,
  Alert,
  Tooltip,
  Typography,
  Row,
  Col,
  Tag,
  Modal,
  Upload,
  message,
} from 'antd';
import {
  InfoCircleOutlined,
  SaveOutlined,
  ReloadOutlined,
  ExportOutlined,
  ImportOutlined,
  CheckOutlined,
  ExclamationCircleOutlined,
  EyeOutlined,
  // HistoryOutlined,
} from '@ant-design/icons';
import { SystemConfiguration, ConfigurationSection } from '../../types/systemConfig';
import useSystemConfig from '../../hooks/useSystemConfig';

const { Panel } = Collapse;
const { Option } = Select;
const { TextArea } = Input;
const { Text } = Typography;

interface ConfigFormProps {
  config?: SystemConfiguration;
  onChange?: (config: Partial<SystemConfiguration>) => void;
  onSave?: (config: SystemConfiguration) => Promise<void>;
  onValidate?: (config: Partial<SystemConfiguration>) => Promise<boolean>;
  readOnly?: boolean;
  showValidationResults?: boolean;
}

const ConfigForm: React.FC<ConfigFormProps> = ({
  config,
  onChange,
  onSave,
  onValidate,
  readOnly = false,
  showValidationResults = true,
}) => {
  const [form] = Form.useForm();
  const [activeSection, setActiveSection] = useState<string>('general');
  const [isValidating, setIsValidating] = useState(false);
  const [isDirty, setIsDirty] = useState(false);
  const [previewVisible, setPreviewVisible] = useState(false);
  const [importModalVisible, setImportModalVisible] = useState(false);

  const {
    validationErrors,
    isSaving,
    validateConfig,
    // analyzeImpact,
  } = useSystemConfig();

  // Form sections configuration
  const sections: ConfigurationSection[] = [
    {
      key: 'general',
      title: 'General Settings',
      description: 'Basic site configuration and branding',
      icon: 'setting',
      fields: [
        {
          key: 'siteName',
          label: 'Site Name',
          type: 'input',
          required: true,
          tooltip: 'The name of your website',
          validation: { max: 100 },
        },
        {
          key: 'siteDescription',
          label: 'Site Description',
          type: 'textarea',
          tooltip: 'Brief description of your website',
          validation: { max: 500 },
        },
        {
          key: 'siteUrl',
          label: 'Site URL',
          type: 'input',
          required: true,
          tooltip: 'The public URL of your website',
          validation: { pattern: /^https?:\/\/.+/ },
        },
        {
          key: 'language',
          label: 'Default Language',
          type: 'select',
          options: [
            { value: 'en', label: 'English' },
            { value: 'zh', label: '中文' },
            { value: 'ja', label: '日本語' },
          ],
          defaultValue: 'en',
        },
        {
          key: 'timezone',
          label: 'Timezone',
          type: 'select',
          options: [
            { value: 'UTC', label: 'UTC' },
            { value: 'Asia/Shanghai', label: 'Asia/Shanghai' },
            { value: 'America/New_York', label: 'America/New_York' },
          ],
          defaultValue: 'UTC',
        },
      ],
    },
    {
      key: 'features',
      title: 'Feature Switches',
      description: 'Enable or disable various site features',
      icon: 'experiment',
      fields: [
        {
          key: 'enableComments',
          label: 'Enable Comments',
          type: 'switch',
          tooltip: 'Allow users to comment on posts',
          defaultValue: true,
        },
        {
          key: 'enableRegistration',
          label: 'Enable User Registration',
          type: 'switch',
          tooltip: 'Allow new users to register',
          defaultValue: true,
        },
        {
          key: 'enableSearch',
          label: 'Enable Search',
          type: 'switch',
          tooltip: 'Enable site-wide search functionality',
          defaultValue: true,
        },
        {
          key: 'enableAnalytics',
          label: 'Enable Analytics',
          type: 'switch',
          tooltip: 'Enable site analytics tracking',
          defaultValue: false,
        },
        {
          key: 'enableCache',
          label: 'Enable Caching',
          type: 'switch',
          tooltip: 'Enable Redis caching for better performance',
          defaultValue: true,
        },
        {
          key: 'maintenanceMode',
          label: 'Maintenance Mode',
          type: 'switch',
          tooltip: 'Put the site in maintenance mode',
          defaultValue: false,
          warning: true,
        },
      ],
    },
    {
      key: 'content',
      title: 'Content Settings',
      description: 'Configure content display and management',
      icon: 'file-text',
      fields: [
        {
          key: 'postsPerPage',
          label: 'Posts Per Page',
          type: 'number',
          validation: { min: 1, max: 100 },
          defaultValue: 10,
          tooltip: 'Number of posts to display per page',
        },
        {
          key: 'allowedFileTypes',
          label: 'Allowed File Types',
          type: 'tags',
          tooltip: 'File extensions allowed for upload',
          defaultValue: ['jpg', 'jpeg', 'png', 'gif', 'pdf', 'doc', 'docx'],
        },
        {
          key: 'maxFileSize',
          label: 'Max File Size (MB)',
          type: 'number',
          validation: { min: 1, max: 100 },
          defaultValue: 10,
          tooltip: 'Maximum file size for uploads',
        },
        {
          key: 'enableAutoSave',
          label: 'Enable Auto Save',
          type: 'switch',
          defaultValue: true,
          tooltip: 'Automatically save drafts while editing',
        },
        {
          key: 'autoSaveInterval',
          label: 'Auto Save Interval (seconds)',
          type: 'number',
          validation: { min: 30, max: 600 },
          defaultValue: 60,
          tooltip: 'How often to auto-save drafts',
          dependsOn: 'enableAutoSave',
        },
      ],
    },
    {
      key: 'security',
      title: 'Security Settings',
      description: 'Configure security and access control',
      icon: 'security-scan',
      fields: [
        {
          key: 'sessionTimeout',
          label: 'Session Timeout (minutes)',
          type: 'number',
          validation: { min: 15, max: 1440 },
          defaultValue: 60,
          tooltip: 'How long user sessions last',
        },
        {
          key: 'enableTwoFactor',
          label: 'Enable Two-Factor Authentication',
          type: 'switch',
          defaultValue: false,
          tooltip: 'Require 2FA for admin accounts',
        },
        {
          key: 'passwordMinLength',
          label: 'Minimum Password Length',
          type: 'number',
          validation: { min: 6, max: 50 },
          defaultValue: 8,
          tooltip: 'Minimum required password length',
        },
        {
          key: 'enableCaptcha',
          label: 'Enable CAPTCHA',
          type: 'switch',
          defaultValue: false,
          tooltip: 'Enable CAPTCHA for forms',
        },
        {
          key: 'maxLoginAttempts',
          label: 'Max Login Attempts',
          type: 'number',
          validation: { min: 3, max: 20 },
          defaultValue: 5,
          tooltip: 'Maximum failed login attempts before lockout',
        },
      ],
    },
    {
      key: 'integrations',
      title: 'Third-party Integrations',
      description: 'Configure external service integrations',
      icon: 'api',
      fields: [
        {
          key: 'googleAnalyticsId',
          label: 'Google Analytics ID',
          type: 'input',
          tooltip: 'Google Analytics tracking ID',
          validation: { pattern: /^(UA-|G-).+/ },
        },
        {
          key: 'emailProvider',
          label: 'Email Provider',
          type: 'select',
          options: [
            { value: 'smtp', label: 'SMTP' },
            { value: 'sendgrid', label: 'SendGrid' },
            { value: 'mailgun', label: 'Mailgun' },
          ],
          defaultValue: 'smtp',
        },
        {
          key: 'smtpHost',
          label: 'SMTP Host',
          type: 'input',
          tooltip: 'SMTP server hostname',
          dependsOn: 'emailProvider',
          dependsOnValue: 'smtp',
        },
        {
          key: 'smtpPort',
          label: 'SMTP Port',
          type: 'number',
          validation: { min: 1, max: 65535 },
          defaultValue: 587,
          dependsOn: 'emailProvider',
          dependsOnValue: 'smtp',
        },
        {
          key: 'cdnEnabled',
          label: 'Enable CDN',
          type: 'switch',
          defaultValue: false,
          tooltip: 'Use CDN for static assets',
        },
        {
          key: 'cdnUrl',
          label: 'CDN URL',
          type: 'input',
          tooltip: 'CDN base URL',
          dependsOn: 'cdnEnabled',
        },
      ],
    },
  ];

  // Initialize form with config data
  useEffect(() => {
    if (config) {
      form.setFieldsValue(config);
    }
  }, [config, form]);

  // Handle form value changes
  const handleFormChange = useCallback((_changedValues: Record<string, unknown>, allValues: Record<string, unknown>) => {
    setIsDirty(true);
    onChange?.(allValues);
  }, [onChange]);

  // Validate configuration
  const handleValidate = useCallback(async () => {
    try {
      setIsValidating(true);
      const values = form.getFieldsValue();
      const isValid = await onValidate?.(values) ?? await validateConfig(values);

      if (isValid) {
        message.success('Configuration is valid');
      } else {
        message.warning('Configuration has validation issues');
      }
    } catch (error) {
      console.error('Validation error:', error);
      message.error('Failed to validate configuration');
    } finally {
      setIsValidating(false);
    }
  }, [form, onValidate, validateConfig]);

  // Save configuration
  const handleSave = useCallback(async () => {
    try {
      const values = await form.validateFields();
      await onSave?.(values as SystemConfiguration);
      setIsDirty(false);
      message.success('Configuration saved successfully');
    } catch (error: unknown) {
      console.error('Save error:', error);
      if ((error as { errorFields?: unknown[] }).errorFields) {
        message.error('Please fix form validation errors');
      } else {
        message.error('Failed to save configuration');
      }
    }
  }, [form, onSave]);

  // Reset form
  const handleReset = useCallback(() => {
    form.resetFields();
    setIsDirty(false);
    message.info('Form reset to original values');
  }, [form]);

  // Show preview
  const showPreview = useCallback(() => {
    setPreviewVisible(true);
  }, []);

  // Export configuration
  const handleExport = useCallback(() => {
    const values = form.getFieldsValue();
    const dataStr = JSON.stringify(values, null, 2);
    const dataUri = 'data:application/json;charset=utf-8,'+ encodeURIComponent(dataStr);

    const exportFileDefaultName = `config-${new Date().toISOString().split('T')[0]}.json`;

    const linkElement = document.createElement('a');
    linkElement.setAttribute('href', dataUri);
    linkElement.setAttribute('download', exportFileDefaultName);
    linkElement.click();
  }, [form]);

  // Import configuration
  const handleImport = useCallback((file: File) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const config = JSON.parse(e.target?.result as string);
        form.setFieldsValue(config);
        setIsDirty(true);
        message.success('Configuration imported successfully');
        setImportModalVisible(false);
      } catch (error) {
        message.error('Invalid configuration file');
      }
    };
    reader.readAsText(file);
    return false; // Prevent upload
  }, [form]);

  // Render form field
  const renderField = useCallback((field: {
    key: string;
    label: string;
    type: string;
    tooltip?: string;
    required?: boolean;
    validation?: Record<string, unknown>;
    options?: Array<{ label: string; value: unknown }>;
    dependsOn?: string;
    dependsOnValue?: unknown;
    warning?: string;
  }, section: string) => {
    const { key, label, type, tooltip, required, validation, options, dependsOn, dependsOnValue, warning, ...fieldProps } = field;

    // Check if field should be visible
    if (dependsOn) {
      const dependsOnFieldValue = form.getFieldValue(dependsOn);
      if (dependsOnValue && dependsOnFieldValue !== dependsOnValue) {
        return null;
      }
      if (!dependsOnValue && !dependsOnFieldValue) {
        return null;
      }
    }

    const rules = [];
    if (required) {
      rules.push({ required: true, message: `${label} is required` });
    }
    if (validation) {
      if (validation.max) {
        rules.push({ max: validation.max, message: `${label} must be less than ${validation.max} characters` });
      }
      if (validation.pattern) {
        rules.push({ pattern: validation.pattern, message: `${label} format is invalid` });
      }
    }

    const fieldId = `${section}_${key}`;
    const hasError = validationErrors.some(error => error.field === key);

    const labelWithTooltip = (
      <Space>
        <span style={{ color: warning ? '#ff4d4f' : undefined }}>
          {label}
          {warning && <ExclamationCircleOutlined style={{ color: '#ff4d4f', marginLeft: 4 }} />}
        </span>
        {tooltip && (
          <Tooltip title={tooltip}>
            <InfoCircleOutlined style={{ color: '#1890ff' }} />
          </Tooltip>
        )}
      </Space>
    );

    let fieldComponent;
    switch (type) {
      case 'input':
        fieldComponent = <Input disabled={readOnly} {...fieldProps} />;
        break;
      case 'textarea':
        fieldComponent = <TextArea rows={3} disabled={readOnly} {...fieldProps} />;
        break;
      case 'number':
        fieldComponent = <InputNumber style={{ width: '100%' }} disabled={readOnly} {...fieldProps} />;
        break;
      case 'switch':
        fieldComponent = <Switch disabled={readOnly} {...fieldProps} />;
        break;
      case 'select':
        fieldComponent = (
          <Select disabled={readOnly} {...fieldProps}>
            {options?.map((option: { label: string; value: unknown }) => (
              <Option key={option.value} value={option.value}>
                {option.label}
              </Option>
            ))}
          </Select>
        );
        break;
      case 'tags':
        fieldComponent = (
          <Select
            mode="tags"
            style={{ width: '100%' }}
            tokenSeparators={[',']}
            disabled={readOnly}
            {...fieldProps}
          />
        );
        break;
      default:
        fieldComponent = <Input disabled={readOnly} {...fieldProps} />;
    }

    return (
      <Form.Item
        key={fieldId}
        name={key}
        label={labelWithTooltip}
        rules={rules}
        validateStatus={hasError ? 'error' : ''}
        help={hasError ? validationErrors.find(error => error.field === key)?.message : undefined}
      >
        {fieldComponent}
      </Form.Item>
    );
  }, [form, validationErrors, readOnly]);

  // Render section
  const renderSection = useCallback((section: ConfigurationSection) => {
    return (
      <Card
        key={section.key}
        title={
          <Space>
            <span>{section.title}</span>
            {section.description && (
              <Text type="secondary" style={{ fontSize: '12px' }}>
                {section.description}
              </Text>
            )}
          </Space>
        }
        
        style={{ marginBottom: 16 }}
      >
        <Row gutter={[16, 0]}>
          {section.fields.map((field) => (
            <Col key={field.key} span={field.span || 24}>
              {renderField(field, section.key)}
            </Col>
          ))}
        </Row>
      </Card>
    );
  }, [renderField]);

  return (
    <div className="config-form">
      {/* Validation Errors Alert */}
      {showValidationResults && validationErrors.length > 0 && (
        <Alert
          type="error"
          message="Configuration Validation Errors"
          description={
            <ul style={{ margin: 0, paddingLeft: 20 }}>
              {validationErrors.map((error, index) => (
                <li key={index}>
                  <strong>{error.field}</strong>: {error.message}
                </li>
              ))}
            </ul>
          }
          style={{ marginBottom: 16 }}
          showIcon
        />
      )}

      {/* Form Actions */}
      {!readOnly && (
        <Card  style={{ marginBottom: 16 }}>
          <Space wrap>
            <Button
              type="primary"
              icon={<SaveOutlined />}
              onClick={handleSave}
              loading={isSaving}
              disabled={!isDirty}
            >
              Save Configuration
            </Button>
            <Button
              icon={<CheckOutlined />}
              onClick={handleValidate}
              loading={isValidating}
            >
              Validate
            </Button>
            <Button
              icon={<ReloadOutlined />}
              onClick={handleReset}
              disabled={!isDirty}
            >
              Reset
            </Button>
            <Divider type="vertical" />
            <Button icon={<EyeOutlined />} onClick={showPreview}>
              Preview
            </Button>
            <Button icon={<ExportOutlined />} onClick={handleExport}>
              Export
            </Button>
            <Button
              icon={<ImportOutlined />}
              onClick={() => setImportModalVisible(true)}
            >
              Import
            </Button>
          </Space>

          {isDirty && (
            <Tag color="orange" style={{ marginLeft: 16 }}>
              Unsaved Changes
            </Tag>
          )}
        </Card>
      )}

      {/* Configuration Form */}
      <Form
        form={form}
        layout="vertical"
        onValuesChange={handleFormChange}
        
      >
        <Collapse
          activeKey={activeSection}
          onChange={(key) => setActiveSection(Array.isArray(key) ? key[0] : key)}
        >
          {sections.map((section) => (
            <Panel
              key={section.key}
              header={
                <Space>
                  <span>{section.title}</span>
                  <Text type="secondary" style={{ fontSize: '12px' }}>
                    {section.description}
                  </Text>
                </Space>
              }
            >
              {renderSection(section)}
            </Panel>
          ))}
        </Collapse>
      </Form>

      {/* Preview Modal */}
      <Modal
        title="Configuration Preview"
        open={previewVisible}
        onCancel={() => setPreviewVisible(false)}
        footer={[
          <Button key="close" onClick={() => setPreviewVisible(false)}>
            Close
          </Button>,
        ]}
        width={800}
      >
        <pre style={{ maxHeight: 400, overflow: 'auto' }}>
          {JSON.stringify(form.getFieldsValue(), null, 2)}
        </pre>
      </Modal>

      {/* Import Modal */}
      <Modal
        title="Import Configuration"
        open={importModalVisible}
        onCancel={() => setImportModalVisible(false)}
        footer={null}
      >
        <Upload.Dragger
          accept=".json"
          beforeUpload={handleImport}
          showUploadList={false}
        >
          <p className="ant-upload-drag-icon">
            <ImportOutlined />
          </p>
          <p className="ant-upload-text">Click or drag JSON file to this area to import</p>
          <p className="ant-upload-hint">
            Support for a single JSON configuration file.
          </p>
        </Upload.Dragger>
      </Modal>
    </div>
  );
};

export default ConfigForm;
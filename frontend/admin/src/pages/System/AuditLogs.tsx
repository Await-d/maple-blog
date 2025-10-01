import React, { useMemo, useState } from 'react';
import {
  Badge,
  Button,
  Card,
  Col,
  DatePicker,
  Descriptions,
  Drawer,
  Empty,
  Input,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import { ReloadOutlined, EyeOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import dayjs, { type Dayjs } from 'dayjs';
import systemConfigService from '@/services/systemConfigService';
import type { ConfigurationAudit } from '@/types/systemConfig';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;
const { Option } = Select;

type AuditAction = ConfigurationAudit['action'] | 'ALL';

interface AuditFilters {
  action: AuditAction;
  search: string;
  dateRange: [Dayjs, Dayjs] | null;
}

const auditActionMeta: Record<ConfigurationAudit['action'], { label: string; color: string }> = {
  CREATE: { label: '创建配置', color: 'green' },
  UPDATE: { label: '更新配置', color: 'blue' },
  DELETE: { label: '删除配置', color: 'red' },
  ROLLBACK: { label: '回滚配置', color: 'orange' },
  BACKUP: { label: '备份配置', color: 'purple' },
  RESTORE: { label: '恢复配置', color: 'geekblue' },
  APPLY_TEMPLATE: { label: '应用模板', color: 'cyan' },
};

const initialFilters: AuditFilters = {
  action: 'ALL',
  search: '',
  dateRange: null,
};

const filterAuditLogs = (logs: ConfigurationAudit[], filters: AuditFilters) => {
  return logs.filter((log) => {
    const matchesAction = filters.action === 'ALL' || log.action === filters.action;

    const matchesSearch = !filters.search
      || log.configId.toLowerCase().includes(filters.search)
      || log.userName.toLowerCase().includes(filters.search)
      || log.userId.toLowerCase().includes(filters.search);

    const matchesDate = !filters.dateRange
      || dayjs(log.timestamp).isBetween(filters.dateRange[0], filters.dateRange[1], 'minute', '[]');

    return matchesAction && matchesSearch && matchesDate;
  });
};

const getChangeDescriptions = (changes: ConfigurationAudit['changes']) => {
  return Object.entries(changes).map(([key, value]) => (
    <Descriptions.Item key={key} label={key} span={3}>
      <div className="flex flex-col gap-2">
        {value.from !== undefined && (
          <div>
            <Text type="secondary">原始值：</Text>
            <Text code>{JSON.stringify(value.from)}</Text>
          </div>
        )}
        {value.to !== undefined && (
          <div>
            <Text type="secondary">更新值：</Text>
            <Text code>{JSON.stringify(value.to)}</Text>
          </div>
        )}
      </div>
    </Descriptions.Item>
  ));
};

const AuditLogs: React.FC = () => {
  const [filters, setFilters] = useState<AuditFilters>(initialFilters);
  const [selectedAudit, setSelectedAudit] = useState<ConfigurationAudit | null>(null);

  const { data: auditLogs = [], isLoading, refetch, isRefetching } = useQuery({
    queryKey: ['system-audit-logs'],
    queryFn: () => systemConfigService.getAuditLogs(),
    staleTime: 60_000,
  });

  const filteredLogs = useMemo(() => {
    const normalizedSearch = filters.search.trim().toLowerCase();
    return filterAuditLogs(auditLogs, { ...filters, search: normalizedSearch });
  }, [auditLogs, filters]);

  const handleFiltersChange = (partial: Partial<AuditFilters>) => {
    setFilters((prev) => ({ ...prev, ...partial }));
  };

  const handleResetFilters = () => {
    setFilters(initialFilters);
  };

  const columns = [
    {
      title: '时间',
      dataIndex: 'timestamp',
      key: 'timestamp',
      width: 200,
      render: (value: string) => dayjs(value).format('YYYY-MM-DD HH:mm:ss'),
    },
    {
      title: '动作',
      dataIndex: 'action',
      key: 'action',
      width: 140,
      render: (action: ConfigurationAudit['action']) => {
        const meta = auditActionMeta[action];
        return <Tag color={meta.color}>{meta.label}</Tag>;
      },
    },
    {
      title: '配置项',
      dataIndex: 'configId',
      key: 'configId',
      render: (configId: string) => <Text code>{configId}</Text>,
    },
    {
      title: '操作人',
      dataIndex: 'userName',
      key: 'userName',
      width: 160,
      render: (_: string, record: ConfigurationAudit) => (
        <div className="flex flex-col">
          <Text strong>{record.userName}</Text>
          <Text type="secondary">ID: {record.userId}</Text>
        </div>
      ),
    },
    {
      title: '来源',
      dataIndex: 'ip',
      key: 'ip',
      width: 160,
      render: (_: string, record: ConfigurationAudit) => (
        <div className="flex flex-col">
          <Text>{record.ip}</Text>
          <Text type="secondary">{record.userAgent}</Text>
        </div>
      ),
    },
    {
      title: '操作',
      key: 'actions',
      width: 120,
      render: (_: unknown, record: ConfigurationAudit) => (
        <Space size="small">
          <Tooltip title="查看详情">
            <Button
              type="link"
              size="small"
              icon={<EyeOutlined />}
              onClick={() => setSelectedAudit(record)}
            >
              详情
            </Button>
          </Tooltip>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div className="page-header">
        <div>
          <Title level={2} className="page-title">
            审计日志
          </Title>
          <Text className="page-description">
            跟踪系统配置的关键变更，确保可追踪性与合规性
          </Text>
        </div>
        <Space>
          <Button
            icon={<ReloadOutlined />}
            loading={isRefetching}
            onClick={() => refetch()}
          >
            刷新
          </Button>
        </Space>
      </div>

      <Card className="page-container" bordered={false}>
        <Row gutter={[16, 16]} className="mb-4">
          <Col xs={24} sm={24} md={12} lg={8}>
            <Text type="secondary" className="block mb-2">
              时间范围
            </Text>
            <RangePicker
              allowEmpty={[true, true]}
              showTime
              value={filters.dateRange}
              onChange={(value) => handleFiltersChange({ dateRange: value })}
              style={{ width: '100%' }}
              placeholder={['开始时间', '结束时间']}
            />
          </Col>
          <Col xs={24} sm={12} md={6} lg={5}>
            <Text type="secondary" className="block mb-2">
              操作类型
            </Text>
            <Select
              value={filters.action}
              onChange={(value: AuditAction) => handleFiltersChange({ action: value })}
              style={{ width: '100%' }}
            >
              <Option value="ALL">全部</Option>
              {Object.entries(auditActionMeta).map(([action, meta]) => (
                <Option key={action} value={action}>
                  {meta.label}
                </Option>
              ))}
            </Select>
          </Col>
          <Col xs={24} sm={12} md={6} lg={6}>
            <Text type="secondary" className="block mb-2">
              关键字
            </Text>
            <Input.Search
              allowClear
              placeholder="搜索配置ID / 用户"
              value={filters.search}
              onChange={(event) => handleFiltersChange({ search: event.target.value })}
              onSearch={(value) => handleFiltersChange({ search: value })}
            />
          </Col>
          <Col xs={24} sm={24} md={24} lg={5}>
            <Text type="secondary" className="block mb-2 invisible">
              操作
            </Text>
            <Space>
              <Button onClick={handleResetFilters}>重置</Button>
              <Badge count={filteredLogs.length} showZero>
                <div className="px-3 py-1 bg-gray-100 rounded text-gray-600">记录</div>
              </Badge>
            </Space>
          </Col>
        </Row>

        <Table<ConfigurationAudit>
          rowKey="id"
          loading={isLoading}
          dataSource={filteredLogs}
          columns={columns}
          pagination={{ pageSize: 10, showSizeChanger: true, showTotal: (total) => `共 ${total} 条` }}
          locale={{
            emptyText: <Empty description="暂无审计记录" />,
          }}
          onRow={(record) => ({
            onDoubleClick: () => setSelectedAudit(record),
          })}
        />
      </Card>

      <Drawer
        width={560}
        title="审计详情"
        open={Boolean(selectedAudit)}
        onClose={() => setSelectedAudit(null)}
      >
        {selectedAudit ? (
          <Space direction="vertical" size={24} className="w-full">
            <Descriptions column={3} bordered size="small">
              <Descriptions.Item label="操作人" span={3}>
                <Space size="small">
                  <Text strong>{selectedAudit.userName}</Text>
                  <Text type="secondary">ID: {selectedAudit.userId}</Text>
                </Space>
              </Descriptions.Item>
              <Descriptions.Item label="动作" span={3}>
                <Tag color={auditActionMeta[selectedAudit.action].color}>
                  {auditActionMeta[selectedAudit.action].label}
                </Tag>
              </Descriptions.Item>
              <Descriptions.Item label="配置项" span={3}>
                <Text code>{selectedAudit.configId}</Text>
              </Descriptions.Item>
              <Descriptions.Item label="操作时间" span={3}>
                {dayjs(selectedAudit.timestamp).format('YYYY-MM-DD HH:mm:ss')}
              </Descriptions.Item>
              <Descriptions.Item label="来源IP" span={3}>
                {selectedAudit.ip}
              </Descriptions.Item>
              <Descriptions.Item label="User Agent" span={3}>
                <Text type="secondary">{selectedAudit.userAgent}</Text>
              </Descriptions.Item>
              {selectedAudit.metadata?.reason && (
                <Descriptions.Item label="操作原因" span={3}>
                  {selectedAudit.metadata.reason}
                </Descriptions.Item>
              )}
              {selectedAudit.metadata?.approvalId && (
                <Descriptions.Item label="审批单号" span={3}>
                  <Text code>{selectedAudit.metadata.approvalId}</Text>
                </Descriptions.Item>
              )}
            </Descriptions>

            {Object.keys(selectedAudit.changes).length > 0 ? (
              <Card title="变更详情" size="small" bordered={false}>
                <Descriptions column={1} size="small">
                  {getChangeDescriptions(selectedAudit.changes)}
                </Descriptions>
              </Card>
            ) : (
              <Empty description="此操作未包含字段变更" />
            )}
          </Space>
        ) : (
          <Empty />
        )}
      </Drawer>
    </div>
  );
};

export default AuditLogs;

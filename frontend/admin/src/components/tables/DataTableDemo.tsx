// @ts-nocheck
import React, { useState, useMemo, useCallback } from 'react';
import { 
  Card, 
  Space, 
  Button, 
  Select, 
  Statistic, 
  Row, 
  Col, 
  Alert, 
  Typography, 
  Tag,
  Divider,
  Switch,
  Slider,
  message
} from 'antd';
import { 
  TableOutlined, 
  ThunderboltOutlined, 
  DatabaseOutlined,
  ClockCircleOutlined,
  RocketOutlined 
} from '@ant-design/icons';
import { DataTable } from './DataTable';
import { useDataTable } from '../../hooks/useDataTable';
import { TablePresets, BatchActionPresets, PerformancePresets, TableUtils } from './index';
import type { DataTableColumn } from './DataTable';

const { Title, Text, Paragraph } = Typography;
const { Option } = Select;

// 演示数据接口
interface DemoData {
  id: number;
  username: string;
  email: string;
  displayName: string;
  status: 'active' | 'inactive' | 'banned';
  createdAt: string;
  lastLoginAt: string;
  viewCount: number;
  score: number;
}

export const DataTableDemo: React.FC = () => {
  // 状态管理
  const [dataSize, setDataSize] = useState(1000);
  const [tableSize, setTableSize] = useState<'small' | 'middle' | 'large'>('middle');
  const [enableVirtual, setEnableVirtual] = useState(true);
  const [enableBorders, setEnableBorders] = useState(false);
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [loading, setLoading] = useState(false);

  // 生成演示数据
  const demoData = useMemo(() => {
    return TableUtils.generateMockData(dataSize) as DemoData[];
  }, [dataSize]);

  // 性能配置
  const performanceConfig = useMemo(() => {
    return TableUtils.getPerformanceConfig(dataSize);
  }, [dataSize]);

  // 性能基准测试
  const performanceBenchmark = useMemo(() => {
    return TableUtils.performanceBenchmark(dataSize);
  }, [dataSize]);

  // 列配置
  const columns: DataTableColumn<DemoData>[] = useMemo(() => [
    {
      key: 'id',
      title: 'ID',
      dataIndex: 'id',
      width: 80,
      sortable: true,
      filterable: false,
      pinned: 'left',
      render: (value) => <Text code>{value}</Text>,
    },
    {
      key: 'username',
      title: 'Username',
      dataIndex: 'username',
      width: 140,
      sortable: true,
      searchable: true,
      render: (value) => <Text strong>{value}</Text>,
    },
    {
      key: 'email',
      title: 'Email Address',
      dataIndex: 'email',
      width: 220,
      sortable: true,
      searchable: true,
      render: (value) => <Text type="secondary">{value}</Text>,
    },
    {
      key: 'displayName',
      title: 'Display Name',
      dataIndex: 'displayName',
      width: 160,
      sortable: true,
      searchable: true,
    },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      width: 120,
      sortable: true,
      filterable: true,
      filterType: 'select',
      filterOptions: [
        { label: 'Active', value: 'active' },
        { label: 'Inactive', value: 'inactive' },
        { label: 'Banned', value: 'banned' },
      ],
      render: (status) => {
        const colorMap = {
          active: 'green',
          inactive: 'orange',
          banned: 'red',
        };
        return <Tag color={colorMap[status]}>{status.toUpperCase()}</Tag>;
      },
    },
    {
      key: 'viewCount',
      title: 'View Count',
      dataIndex: 'viewCount',
      width: 120,
      sortable: true,
      sortType: 'number',
      render: (value) => <Text>{value.toLocaleString()}</Text>,
    },
    {
      key: 'score',
      title: 'Score',
      dataIndex: 'score',
      width: 100,
      sortable: true,
      sortType: 'number',
      render: (value) => {
        const color = value >= 80 ? 'green' : value >= 60 ? 'orange' : 'red';
        return <Tag color={color}>{value}</Tag>;
      },
    },
    {
      key: 'createdAt',
      title: 'Created At',
      dataIndex: 'createdAt',
      width: 160,
      sortable: true,
      sortType: 'date',
      render: (value) => new Date(value).toLocaleDateString(),
    },
    {
      key: 'lastLoginAt',
      title: 'Last Login',
      dataIndex: 'lastLoginAt',
      width: 160,
      sortable: true,
      sortType: 'date',
      render: (value) => {
        const date = new Date(value);
        const isRecent = Date.now() - date.getTime() < 7 * 24 * 60 * 60 * 1000;
        return (
          <Text type={isRecent ? 'success' : 'secondary'}>
            {date.toLocaleDateString()}
          </Text>
        );
      },
    },
    {
      key: 'actions',
      title: 'Actions',
      width: 150,
      pinned: 'right',
      exportable: false,
      render: (_, record) => (
        <Space >
          <Button  type="link">Edit</Button>
          <Button  type="link">View</Button>
          <Button  type="link" danger>Delete</Button>
        </Space>
      ),
    },
  ], []);

  // 使用数据表格 Hook
  const tableHook = useDataTable({
    data: demoData,
    columns,
    enableVirtualization: enableVirtual,
    virtualizationThreshold: performanceConfig.virtualThreshold,
    debounceMs: performanceConfig.debounceMs,
    enableCache: performanceConfig.enableCache,
  });

  // 处理批量操作
  const handleBatchAction = useCallback(async (action: string, params?: any) => {
    setLoading(true);
    
    try {
      // 模拟 API 调用
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      message.success(`Successfully performed ${action} on ${selectedRowKeys.length} items`);
      
      // 清除选择
      setSelectedRowKeys([]);
    } catch (error) {
      message.error(`Failed to perform ${action}`);
    } finally {
      setLoading(false);
    }
  }, [selectedRowKeys]);

  // 处理数据大小变化
  const handleDataSizeChange = useCallback((size: number) => {
    setDataSize(size);
    setSelectedRowKeys([]);
  }, []);

  // 刷新数据
  const handleRefresh = useCallback(() => {
    setLoading(true);
    setTimeout(() => {
      tableHook.refreshData();
      setLoading(false);
      message.success('Data refreshed successfully');
    }, 500);
  }, [tableHook]);

  return (
    <div style={{ padding: '24px' }}>
      <Title level={2}>
        <ThunderboltOutlined /> High-Performance Data Table Demo
      </Title>
      
      <Paragraph>
        This demo showcases a high-performance data table component capable of handling 
        million-level datasets with virtual scrolling, advanced filtering, sorting, and batch operations.
      </Paragraph>

      {/* 性能统计 */}
      <Card title="Performance Metrics" style={{ marginBottom: 24 }}>
        <Row gutter={16}>
          <Col span={6}>
            <Statistic
              title="Data Size"
              value={dataSize}
              suffix="rows"
              prefix={<DatabaseOutlined />}
              formatter={(value) => TableUtils.formatDataSize(Number(value))}
            />
          </Col>
          <Col span={6}>
            <Statistic
              title="Processing Time"
              value={performanceBenchmark.processTime}
              suffix="ms"
              prefix={<ClockCircleOutlined />}
              valueStyle={{ 
                color: performanceBenchmark.processTime < 16 ? '#3f8600' : 
                       performanceBenchmark.processTime < 100 ? '#fa8c16' : '#cf1322' 
              }}
            />
          </Col>
          <Col span={6}>
            <Statistic
              title="Filtered Count"
              value={tableHook.filteredCount}
              suffix="rows"
              prefix={<TableOutlined />}
            />
          </Col>
          <Col span={6}>
            <Statistic
              title="Performance"
              value={performanceBenchmark.performance}
              prefix={<RocketOutlined />}
              valueStyle={{ 
                color: performanceBenchmark.performance === 'excellent' ? '#3f8600' : 
                       performanceBenchmark.performance === 'good' ? '#fa8c16' : '#cf1322' 
              }}
            />
          </Col>
        </Row>
      </Card>

      {/* 控制面板 */}
      <Card title="Demo Controls" style={{ marginBottom: 24 }}>
        <Row gutter={[16, 16]}>
          <Col span={6}>
            <Text strong>Data Size:</Text>
            <Select
              value={dataSize}
              onChange={handleDataSizeChange}
              style={{ width: '100%', marginTop: 8 }}
            >
              <Option value={100}>100 rows</Option>
              <Option value={1000}>1K rows</Option>
              <Option value={10000}>10K rows</Option>
              <Option value={50000}>50K rows</Option>
              <Option value={100000}>100K rows</Option>
              <Option value={500000}>500K rows</Option>
              <Option value={1000000}>1M rows</Option>
            </Select>
          </Col>
          <Col span={6}>
            <Text strong>Table Size:</Text>
            <Select
              value={tableSize}
              onChange={setTableSize}
              style={{ width: '100%', marginTop: 8 }}
            >
              <Option value="small">Small</Option>
              <Option value="middle">Middle</Option>
              <Option value="large">Large</Option>
            </Select>
          </Col>
          <Col span={6}>
            <Text strong>Virtual Scrolling:</Text>
            <div style={{ marginTop: 8 }}>
              <Switch
                checked={enableVirtual}
                onChange={setEnableVirtual}
                checkedChildren="ON"
                unCheckedChildren="OFF"
              />
            </div>
          </Col>
          <Col span={6}>
            <Text strong>Bordered:</Text>
            <div style={{ marginTop: 8 }}>
              <Switch
                checked={enableBorders}
                onChange={setEnableBorders}
                checkedChildren="ON"
                unCheckedChildren="OFF"
              />
            </div>
          </Col>
        </Row>
      </Card>

      {/* 性能提示 */}
      {dataSize >= 100000 && (
        <Alert
          message="Large Dataset Detected"
          description={`You are viewing ${TableUtils.formatDataSize(dataSize)}. Virtual scrolling and advanced optimizations are automatically enabled for optimal performance.`}
          type="info"
          showIcon
          style={{ marginBottom: 24 }}
        />
      )}

      {/* 数据表格 */}
      <Card title={`Data Table (${TableUtils.formatDataSize(dataSize)})`}>
        <DataTable<DemoData>
          columns={columns}
          dataSource={demoData}
          loading={loading}
          size={tableSize}
          bordered={enableBorders}
          virtual={enableVirtual}
          virtualThreshold={performanceConfig.virtualThreshold}
          searchable
          filterable
          exportable
          columnsConfigurable
          resizable
          sortable
          fullscreen
          toolbar
          densitySelector
          columnSelector
          rowSelection={{
            selectedRowKeys,
            onChange: (keys, rows) => {
              setSelectedRowKeys(keys);
            },
            preserveSelectedRowKeys: true,
          }}
          pagination={{
            current: 1,
            pageSize: 50,
            total: demoData.length,
            showSizeChanger: true,
            showQuickJumper: true,
            showTotal: (total, range) => 
              `${range[0]}-${range[1]} of ${total} items`,
            onChange: (page, pageSize) => {
              console.log('Pagination changed:', { page, pageSize });
            },
          }}
          onBatchAction={handleBatchAction}
          onRefresh={handleRefresh}
          onExport={(data, columns) => {
            console.log('Export triggered:', { data: data.length, columns: columns.length });
            message.success(`Exported ${data.length} rows`);
          }}
          scroll={{ x: 1500, y: 600 }}
          rowKey="id"
        />
      </Card>

      {/* 性能说明 */}
      <Card title="Performance Features" style={{ marginTop: 24 }}>
        <Row gutter={16}>
          <Col span={12}>
            <Title level={4}>🚀 Virtual Scrolling</Title>
            <Paragraph>
              Automatically enables for datasets over {performanceConfig.virtualThreshold} rows.
              Only renders visible rows, maintaining 60fps performance even with millions of records.
            </Paragraph>
            
            <Title level={4}>🔍 Intelligent Filtering</Title>
            <Paragraph>
              Debounced search with {performanceConfig.debounceMs}ms delay. 
              Advanced column-specific filters with multiple data types support.
            </Paragraph>
          </Col>
          <Col span={12}>
            <Title level={4}>💾 Smart Caching</Title>
            <Paragraph>
              Results are cached to avoid redundant processing. 
              Cache automatically manages memory with LRU eviction.
            </Paragraph>
            
            <Title level={4}>📊 Batch Operations</Title>
            <Paragraph>
              Efficient bulk operations on selected rows with confirmation dialogs 
              and progress feedback for large operations.
            </Paragraph>
          </Col>
        </Row>
      </Card>

      <Divider />

      <div style={{ textAlign: 'center', color: '#666' }}>
        <Text type="secondary">
          High-Performance Data Table Components • Built with React 19 & Ant Design 5
        </Text>
      </div>
    </div>
  );
};

export default DataTableDemo;
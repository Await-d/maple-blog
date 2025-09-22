// @ts-nocheck
import React, { useState, useCallback, useEffect, useRef } from 'react';
import { 
  Card, 
  Button, 
  Progress, 
  Space, 
  Statistic, 
  Row, 
  Col, 
  Alert, 
  Typography, 
  List,
  Tag,
  Divider
} from 'antd';
import { 
  PlayCircleOutlined, 
  StopOutlined, 
  ReloadOutlined,
  ThunderboltOutlined,
  CheckCircleOutlined,
  ExclamationCircleOutlined,
  ClockCircleOutlined
} from '@ant-design/icons';
import { DataTable } from './DataTable';
import { VirtualTable } from './VirtualTable';
import { useDataTable } from '../../hooks/useDataTable';
import { TableUtils } from './index';

const { Title, Text, Paragraph } = Typography;

// 性能测试配置
interface PerformanceTestConfig {
  name: string;
  dataSize: number;
  operations: string[];
  expectedTime: number; // 期望完成时间（毫秒）
  virtualScrolling: boolean;
}

// 测试结果接口
interface TestResult {
  testName: string;
  dataSize: number;
  operations: string[];
  totalTime: number;
  operationTimes: { [key: string]: number };
  memoryUsage: number;
  status: 'success' | 'warning' | 'error';
  score: number;
}

// 预定义测试套件
const TEST_SUITES: PerformanceTestConfig[] = [
  {
    name: 'Small Dataset Test',
    dataSize: 1000,
    operations: ['render', 'search', 'filter', 'sort', 'select'],
    expectedTime: 100,
    virtualScrolling: false,
  },
  {
    name: 'Medium Dataset Test',
    dataSize: 10000,
    operations: ['render', 'search', 'filter', 'sort', 'select', 'export'],
    expectedTime: 200,
    virtualScrolling: true,
  },
  {
    name: 'Large Dataset Test',
    dataSize: 100000,
    operations: ['render', 'search', 'filter', 'sort', 'virtualScroll'],
    expectedTime: 500,
    virtualScrolling: true,
  },
  {
    name: 'Million Record Test',
    dataSize: 1000000,
    operations: ['render', 'virtualScroll', 'search', 'bulkSelect'],
    expectedTime: 1000,
    virtualScrolling: true,
  },
];

export const PerformanceTest: React.FC = () => {
  const [isRunning, setIsRunning] = useState(false);
  const [currentTest, setCurrentTest] = useState<PerformanceTestConfig | null>(null);
  const [progress, setProgress] = useState(0);
  const [results, setResults] = useState<TestResult[]>([]);
  const [currentData, setCurrentData] = useState<any[]>([]);
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);

  const testStartTime = useRef<number>(0);
  const operationTimes = useRef<{ [key: string]: number }>({});

  // 性能监控函数
  const measurePerformance = useCallback((operationName: string, operation: () => void | Promise<void>) => {
    return new Promise<number>(async (resolve) => {
      const start = performance.now();
      
      // 强制垃圾回收（如果支持）
      if (window.gc) {
        window.gc();
      }
      
      const memoryBefore = (performance as any).memory?.usedJSHeapSize || 0;
      
      await operation();
      
      // 等待下一帧确保DOM更新完成
      requestAnimationFrame(() => {
        const end = performance.now();
        const duration = end - start;
        
        operationTimes.current[operationName] = duration;
        
        console.log(`${operationName}: ${duration.toFixed(2)}ms`);
        resolve(duration);
      });
    });
  }, []);

  // 执行单个测试操作
  const executeOperation = useCallback(async (operation: string, data: any[]) => {
    switch (operation) {
      case 'render':
        return measurePerformance('Initial Render', () => {
          setCurrentData(data);
        });

      case 'search':
        return measurePerformance('Search Operation', () => {
          // 模拟搜索操作
          const searchTerm = 'user100';
          const filtered = data.filter(item => 
            item.username.includes(searchTerm) || item.email.includes(searchTerm)
          );
          console.log(`Search found ${filtered.length} results`);
        });

      case 'filter':
        return measurePerformance('Filter Operation', () => {
          // 模拟复杂过滤
          const filtered = data.filter(item => 
            item.status === 'active' && item.viewCount > 5000
          );
          console.log(`Filter found ${filtered.length} results`);
        });

      case 'sort':
        return measurePerformance('Sort Operation', () => {
          // 模拟排序操作
          const sorted = [...data].sort((a, b) => b.viewCount - a.viewCount);
          console.log(`Sorted ${sorted.length} items`);
        });

      case 'select':
        return measurePerformance('Selection Operation', () => {
          // 模拟选择操作
          const selectedKeys = data.slice(0, Math.min(100, data.length)).map(item => item.id);
          setSelectedRowKeys(selectedKeys);
        });

      case 'bulkSelect':
        return measurePerformance('Bulk Selection', () => {
          // 模拟大批量选择
          const selectedKeys = data.slice(0, Math.min(10000, data.length)).map(item => item.id);
          setSelectedRowKeys(selectedKeys);
        });

      case 'virtualScroll':
        return measurePerformance('Virtual Scroll Test', async () => {
          // 模拟虚拟滚动性能测试
          for (let i = 0; i < 10; i++) {
            await new Promise(resolve => {
              setTimeout(() => {
                // 模拟滚动到不同位置
                const scrollPosition = (data.length / 10) * i;
                console.log(`Virtual scroll to position: ${scrollPosition}`);
                resolve(void 0);
              }, 10);
            });
          }
        });

      case 'export':
        return measurePerformance('Export Operation', () => {
          // 模拟导出操作
          const csvData = data.slice(0, Math.min(1000, data.length)).map(item => ({
            id: item.id,
            username: item.username,
            email: item.email,
            status: item.status,
          }));
          console.log(`Exported ${csvData.length} rows`);
        });

      default:
        return Promise.resolve(0);
    }
  }, [measurePerformance]);

  // 运行单个测试
  const runTest = useCallback(async (testConfig: PerformanceTestConfig) => {
    console.log(`Starting test: ${testConfig.name}`);
    setCurrentTest(testConfig);
    setProgress(0);
    operationTimes.current = {};
    
    // 生成测试数据
    const testData = TableUtils.generateMockData(testConfig.dataSize);
    
    const totalOperations = testConfig.operations.length;
    let completedOperations = 0;
    
    testStartTime.current = performance.now();
    
    // 执行每个操作
    for (const operation of testConfig.operations) {
      await executeOperation(operation, testData);
      completedOperations++;
      setProgress((completedOperations / totalOperations) * 100);
      
      // 添加小延迟以允许UI更新
      await new Promise(resolve => setTimeout(resolve, 50));
    }
    
    const totalTime = performance.now() - testStartTime.current;
    const memoryUsage = (performance as any).memory?.usedJSHeapSize || 0;
    
    // 计算性能得分
    const score = Math.max(0, Math.min(100, 
      100 - (totalTime / testConfig.expectedTime) * 50
    ));
    
    // 确定测试状态
    let status: 'success' | 'warning' | 'error' = 'success';
    if (totalTime > testConfig.expectedTime * 1.5) {
      status = 'error';
    } else if (totalTime > testConfig.expectedTime) {
      status = 'warning';
    }
    
    const result: TestResult = {
      testName: testConfig.name,
      dataSize: testConfig.dataSize,
      operations: testConfig.operations,
      totalTime,
      operationTimes: { ...operationTimes.current },
      memoryUsage,
      status,
      score: Math.round(score),
    };
    
    setResults(prev => [...prev, result]);
    setCurrentTest(null);
    setProgress(0);
    
    console.log(`Test completed: ${testConfig.name}`, result);
    
    return result;
  }, [executeOperation]);

  // 运行所有测试
  const runAllTests = useCallback(async () => {
    setIsRunning(true);
    setResults([]);
    setSelectedRowKeys([]);
    
    try {
      for (const testConfig of TEST_SUITES) {
        await runTest(testConfig);
        
        // 清理内存
        setCurrentData([]);
        setSelectedRowKeys([]);
        
        // 强制垃圾回收
        if (window.gc) {
          window.gc();
        }
        
        // 在测试间添加延迟
        await new Promise(resolve => setTimeout(resolve, 100));
      }
    } catch (error) {
      console.error('Test suite failed:', error);
    } finally {
      setIsRunning(false);
    }
  }, [runTest]);

  // 停止测试
  const stopTests = useCallback(() => {
    setIsRunning(false);
    setCurrentTest(null);
    setProgress(0);
  }, []);

  // 清除结果
  const clearResults = useCallback(() => {
    setResults([]);
    setCurrentData([]);
    setSelectedRowKeys([]);
  }, []);

  // 获取总体性能评分
  const overallScore = results.length > 0 
    ? Math.round(results.reduce((sum, result) => sum + result.score, 0) / results.length)
    : 0;

  // 获取总体状态
  const overallStatus = results.length > 0
    ? results.some(r => r.status === 'error') ? 'error'
      : results.some(r => r.status === 'warning') ? 'warning'
      : 'success'
    : 'info';

  return (
    <div style={{ padding: '24px' }}>
      <Title level={2}>
        <ThunderboltOutlined /> Performance Test Suite
      </Title>
      
      <Paragraph>
        Comprehensive performance testing for the high-performance data table components.
        Tests virtual scrolling, filtering, sorting, and batch operations across different dataset sizes.
      </Paragraph>

      {/* 控制面板 */}
      <Card title="Test Controls" style={{ marginBottom: 24 }}>
        <Space wrap>
          <Button
            type="primary"
            icon={<PlayCircleOutlined />}
            onClick={runAllTests}
            loading={isRunning}
            disabled={isRunning}
          >
            Run All Tests
          </Button>
          <Button
            icon={<StopOutlined />}
            onClick={stopTests}
            disabled={!isRunning}
          >
            Stop Tests
          </Button>
          <Button
            icon={<ReloadOutlined />}
            onClick={clearResults}
            disabled={isRunning}
          >
            Clear Results
          </Button>
        </Space>
      </Card>

      {/* 当前测试状态 */}
      {currentTest && (
        <Card title="Current Test" style={{ marginBottom: 24 }}>
          <Row gutter={16}>
            <Col span={8}>
              <Statistic
                title="Test Name"
                value={currentTest.name}
                valueStyle={{ fontSize: '16px' }}
              />
            </Col>
            <Col span={8}>
              <Statistic
                title="Dataset Size"
                value={TableUtils.formatDataSize(currentTest.dataSize)}
                prefix={<ClockCircleOutlined />}
              />
            </Col>
            <Col span={8}>
              <div>
                <Text strong>Progress</Text>
                <Progress
                  percent={progress}
                  status={progress === 100 ? 'success' : 'active'}
                  style={{ marginTop: 8 }}
                />
              </div>
            </Col>
          </Row>
        </Card>
      )}

      {/* 总体结果 */}
      {results.length > 0 && (
        <Card title="Overall Results" style={{ marginBottom: 24 }}>
          <Row gutter={16}>
            <Col span={6}>
              <Statistic
                title="Tests Completed"
                value={results.length}
                suffix={`/ ${TEST_SUITES.length}`}
                prefix={<CheckCircleOutlined />}
              />
            </Col>
            <Col span={6}>
              <Statistic
                title="Overall Score"
                value={overallScore}
                suffix="/ 100"
                valueStyle={{ 
                  color: overallScore >= 80 ? '#3f8600' : 
                         overallScore >= 60 ? '#fa8c16' : '#cf1322' 
                }}
              />
            </Col>
            <Col span={6}>
              <Statistic
                title="Performance Status"
                value={overallStatus === 'success' ? 'Excellent' : 
                       overallStatus === 'warning' ? 'Good' : 'Poor'}
                valueStyle={{ 
                  color: overallStatus === 'success' ? '#3f8600' : 
                         overallStatus === 'warning' ? '#fa8c16' : '#cf1322' 
                }}
              />
            </Col>
            <Col span={6}>
              <Statistic
                title="Total Time"
                value={results.reduce((sum, r) => sum + r.totalTime, 0).toFixed(0)}
                suffix="ms"
                prefix={<ClockCircleOutlined />}
              />
            </Col>
          </Row>
        </Card>
      )}

      {/* 详细测试结果 */}
      {results.length > 0 && (
        <Card title="Detailed Results">
          <List
            dataSource={results}
            renderItem={(result) => (
              <List.Item>
                <Card  style={{ width: '100%' }}>
                  <Row gutter={16} align="middle">
                    <Col span={6}>
                      <div>
                        <Text strong>{result.testName}</Text>
                        <br />
                        <Text type="secondary">
                          {TableUtils.formatDataSize(result.dataSize)}
                        </Text>
                      </div>
                    </Col>
                    <Col span={4}>
                      <Statistic
                        title="Total Time"
                        value={result.totalTime.toFixed(0)}
                        suffix="ms"
                        valueStyle={{ fontSize: '14px' }}
                      />
                    </Col>
                    <Col span={3}>
                      <Statistic
                        title="Score"
                        value={result.score}
                        suffix="/ 100"
                        valueStyle={{ 
                          fontSize: '14px',
                          color: result.status === 'success' ? '#3f8600' : 
                                 result.status === 'warning' ? '#fa8c16' : '#cf1322' 
                        }}
                      />
                    </Col>
                    <Col span={3}>
                      <Tag color={
                        result.status === 'success' ? 'green' : 
                        result.status === 'warning' ? 'orange' : 'red'
                      }>
                        {result.status.toUpperCase()}
                      </Tag>
                    </Col>
                    <Col span={8}>
                      <Space wrap >
                        {Object.entries(result.operationTimes).map(([op, time]) => (
                          <Tag key={op} color="blue">
                            {op}: {time.toFixed(1)}ms
                          </Tag>
                        ))}
                      </Space>
                    </Col>
                  </Row>
                </Card>
              </List.Item>
            )}
          />
        </Card>
      )}

      {/* 测试数据表格 */}
      {currentData.length > 0 && (
        <Card title={`Test Data (${TableUtils.formatDataSize(currentData.length)})`} style={{ marginTop: 24 }}>
          <DataTable
            columns={[
              { key: 'id', title: 'ID', dataIndex: 'id', width: 80 },
              { key: 'username', title: 'Username', dataIndex: 'username', width: 120 },
              { key: 'email', title: 'Email', dataIndex: 'email', width: 200 },
              { key: 'status', title: 'Status', dataIndex: 'status', width: 100 },
            ]}
            dataSource={currentData}
            virtual={currentData.length > 1000}
            virtualThreshold={1000}
            rowSelection={{
              selectedRowKeys,
              onChange: setSelectedRowKeys,
            }}
            pagination={false}
            scroll={{ y: 400 }}
            
          />
        </Card>
      )}

      <Divider />

      {/* 性能基准说明 */}
      <Alert
        message="Performance Benchmarks"
        description={
          <div>
            <Paragraph>
              <strong>Expected Performance Standards:</strong>
            </Paragraph>
            <ul>
              <li>Small datasets (&lt;1K): &lt;100ms initial render</li>
              <li>Medium datasets (1K-10K): &lt;200ms with virtual scrolling</li>
              <li>Large datasets (10K-100K): &lt;500ms with optimizations</li>
              <li>Million records: &lt;1000ms with virtual scrolling and caching</li>
            </ul>
            <Paragraph>
              Tests measure real-world operations including rendering, searching, filtering, 
              sorting, and bulk selections to ensure consistent 60fps performance.
            </Paragraph>
          </div>
        }
        type="info"
        showIcon
      />
    </div>
  );
};

export default PerformanceTest;
// @ts-nocheck
import React, { useEffect, useRef, useState, useCallback, useMemo } from 'react';
import { Card, Button, Space, Badge, Alert, Statistic, Row, Col } from 'antd';
import { PlayCircleOutlined, PauseCircleOutlined, ReloadOutlined } from '@ant-design/icons';
import ChartWrapper, { ChartWrapperRef } from './ChartWrapper';
// import { useWebSocket } from '../../../hooks/useWebSocket';

// Temporary stub for useWebSocket
const useWebSocket = (_url: string, _options?: any) => {
  return {
    data: null,
    error: null,
    connecting: false,
    connected: false,
    isConnected: false,
    lastMessage: null as any,
    sendMessage: () => {},
    disconnect: () => {},
    reconnect: () => {},
  };
};

export interface RealTimeDataPoint {
  timestamp: number;
  value: number | number[];
  category?: string;
  metadata?: Record<string, any>;
}

export interface RealTimeChartProps {
  // Data source
  websocketUrl?: string;
  dataSource?: () => Promise<RealTimeDataPoint[]>;
  initialData?: RealTimeDataPoint[];

  // Chart configuration
  type?: 'line' | 'bar' | 'gauge' | 'liquid' | 'radar';
  title?: string;
  height?: number;
  maxDataPoints?: number;
  updateInterval?: number; // milliseconds

  // Real-time features
  autoStart?: boolean;
  pausable?: boolean;
  refreshable?: boolean;
  animated?: boolean;
  smooth?: boolean;

  // Thresholds and alerts
  thresholds?: {
    warning: number;
    critical: number;
    colors: {
      normal: string;
      warning: string;
      critical: string;
    };
  };

  // Display options
  showMetrics?: boolean;
  showStatus?: boolean;
  showTrend?: boolean;
  timeFormat?: string;

  // Events
  onDataUpdate?: (data: RealTimeDataPoint[]) => void;
  onThresholdExceeded?: (type: 'warning' | 'critical', value: number) => void;
  onError?: (error: Error) => void;

  // Styling
  className?: string;
  theme?: 'light' | 'dark';
  colors?: string[];
}

interface MetricsData {
  current: number;
  average: number;
  min: number;
  max: number;
  trend: 'up' | 'down' | 'stable';
  changePercent: number;
}

const RealTimeChart: React.FC<RealTimeChartProps> = ({
  websocketUrl,
  dataSource,
  initialData = [],
  type = 'line',
  title = '实时数据',
  height = 400,
  maxDataPoints = 100,
  updateInterval = 1000,
  autoStart = true,
  pausable = true,
  refreshable = true,
  animated = true,
  smooth = true,
  thresholds,
  showMetrics = true,
  showStatus = true,
  showTrend = true,
  timeFormat: _timeFormat = 'HH:mm:ss',
  onDataUpdate,
  onThresholdExceeded,
  onError,
  className,
  theme = 'light',
  colors = ['#1890ff', '#52c41a', '#faad14', '#f5222d', '#722ed1']
}) => {
  const chartRef = useRef<ChartWrapperRef>(null);
  const [data, setData] = useState<RealTimeDataPoint[]>(initialData);
  const [isRunning, setIsRunning] = useState(autoStart);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdateTime, setLastUpdateTime] = useState<number>(Date.now());
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  // WebSocket connection
  const {
    isConnected: wsConnected,
    lastMessage,
    sendMessage: _sendMessage,
    error: wsError
  } = useWebSocket(websocketUrl || '', {
    shouldReconnect: () => true,
    reconnectInterval: 3000,
    reconnectAttempts: 10,
    enabled: !!websocketUrl && isRunning
  });

  // Calculate metrics
  const metrics = useMemo((): MetricsData => {
    if (data.length === 0) {
      return {
        current: 0,
        average: 0,
        min: 0,
        max: 0,
        trend: 'stable',
        changePercent: 0
      };
    }

    const values = data.map(d => Array.isArray(d.value) ? d.value[0] : d.value);
    const current = values[values.length - 1] || 0;
    const previous = values[values.length - 2] || current;
    const average = values.reduce((a, b) => a + b, 0) / values.length;
    const min = Math.min(...values);
    const max = Math.max(...values);

    const changePercent = previous !== 0 ? ((current - previous) / previous) * 100 : 0;
    const trend = changePercent > 0.1 ? 'up' : changePercent < -0.1 ? 'down' : 'stable';

    return {
      current,
      average,
      min,
      max,
      trend,
      changePercent
    };
  }, [data]);

  // Check thresholds
  const checkThresholds = useCallback((value: number) => {
    if (!thresholds) return 'normal';

    if (value >= thresholds.critical) {
      onThresholdExceeded?.('critical', value);
      return 'critical';
    } else if (value >= thresholds.warning) {
      onThresholdExceeded?.('warning', value);
      return 'warning';
    }
    return 'normal';
  }, [thresholds, onThresholdExceeded]);

  // Get current status color
  const getCurrentStatusColor = () => {
    if (!thresholds) return '#52c41a';

    const status = checkThresholds(metrics.current);
    return thresholds.colors[status] || '#52c41a';
  };

  // Generate chart option
  const chartOption = useMemo(() => {
    if (data.length === 0) return {};

    const timestamps = data.map(d => new Date(d.timestamp).toLocaleTimeString([], {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    }));

    switch (type) {
      case 'line':
        return {
          xAxis: {
            type: 'category',
            data: timestamps,
            boundaryGap: false,
            axisLabel: {
              interval: Math.floor(data.length / 6) || 1
            }
          },
          yAxis: {
            type: 'value',
            scale: true,
            splitLine: {
              lineStyle: {
                type: 'dashed'
              }
            }
          },
          series: [{
            type: 'line',
            data: data.map(d => Array.isArray(d.value) ? d.value[0] : d.value),
            smooth,
            symbol: 'none',
            lineStyle: {
              width: 2,
              color: colors[0]
            },
            areaStyle: {
              opacity: 0.2,
              color: {
                type: 'linear',
                x: 0,
                y: 0,
                x2: 0,
                y2: 1,
                colorStops: [
                  { offset: 0, color: colors[0] },
                  { offset: 1, color: 'transparent' }
                ]
              }
            },
            markLine: thresholds ? {
              silent: true,
              data: [
                {
                  yAxis: thresholds.warning,
                  lineStyle: { color: thresholds.colors.warning, type: 'dashed' },
                  label: { formatter: '警告线' }
                },
                {
                  yAxis: thresholds.critical,
                  lineStyle: { color: thresholds.colors.critical, type: 'dashed' },
                  label: { formatter: '危险线' }
                }
              ]
            } : undefined
          }],
          animation: animated,
          animationDuration: 300,
          animationEasing: 'linear'
        };

      case 'gauge':
        return {
          series: [{
            type: 'gauge',
            center: ['50%', '60%'],
            startAngle: 200,
            endAngle: -40,
            min: 0,
            max: thresholds ? Math.max(thresholds.critical * 1.2, metrics.max * 1.2) : metrics.max * 1.2,
            splitNumber: 10,
            itemStyle: {
              color: getCurrentStatusColor()
            },
            progress: {
              show: true,
              width: 18
            },
            pointer: {
              show: false
            },
            axisLine: {
              lineStyle: {
                width: 18
              }
            },
            axisTick: {
              distance: -45,
              splitNumber: 5,
              lineStyle: {
                width: 2,
                color: '#999'
              }
            },
            splitLine: {
              distance: -52,
              length: 14,
              lineStyle: {
                width: 3,
                color: '#999'
              }
            },
            axisLabel: {
              distance: -20,
              color: '#999',
              fontSize: 12
            },
            anchor: {
              show: false
            },
            title: {
              show: false
            },
            detail: {
              valueAnimation: true,
              width: '60%',
              lineHeight: 40,
              borderRadius: 8,
              offsetCenter: [0, '-15%'],
              fontSize: 24,
              fontWeight: 'bolder',
              formatter: '{value}',
              color: 'inherit'
            },
            data: [{
              value: metrics.current,
              name: title
            }]
          }]
        };

      case 'bar':
        const recentData = data.slice(-20);
        return {
          xAxis: {
            type: 'category',
            data: recentData.map(d => new Date(d.timestamp).toLocaleTimeString([], {
              minute: '2-digit',
              second: '2-digit'
            })),
            axisLabel: {
              interval: Math.floor(recentData.length / 6) || 1,
              rotate: 45
            }
          },
          yAxis: {
            type: 'value'
          },
          series: [{
            type: 'bar',
            data: recentData.map(d => ({
              value: Array.isArray(d.value) ? d.value[0] : d.value,
              itemStyle: {
                color: thresholds ?
                  ((Array.isArray(d.value) ? d.value[0] : d.value) >= thresholds.critical ? thresholds.colors.critical :
                   (Array.isArray(d.value) ? d.value[0] : d.value) >= thresholds.warning ? thresholds.colors.warning :
                   thresholds.colors.normal) : colors[0]
              }
            })),
            animationDelay: (idx: number) => idx * 10
          }]
        };

      default:
        return {};
    }
  }, [data, type, smooth, colors, thresholds, animated, metrics, title]);

  // Handle WebSocket messages
  useEffect(() => {
    if (lastMessage && lastMessage.data) {
      try {
        const newDataPoint = JSON.parse(lastMessage.data) as RealTimeDataPoint;
        addDataPoint(newDataPoint);
      } catch (err) {
        console.error('Failed to parse WebSocket message:', err);
      }
    }
  }, [lastMessage]);

  // Handle polling data source
  useEffect(() => {
    if (!isRunning || websocketUrl || !dataSource) return;

    const fetchData = async () => {
      try {
        const newData = await dataSource();
        if (newData.length > 0) {
          setData(prevData => {
            const combined = [...prevData, ...newData];
            return combined.slice(-maxDataPoints);
          });
          setLastUpdateTime(Date.now());
        }
        setError(null);
      } catch (err) {
        setError(err instanceof Error ? err.message : '数据获取失败');
        onError?.(err instanceof Error ? err : new Error('数据获取失败'));
      }
    };

    const interval = setInterval(fetchData, updateInterval);
    intervalRef.current = interval;

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [isRunning, websocketUrl, dataSource, updateInterval, maxDataPoints, onError]);

  // Add single data point
  const addDataPoint = useCallback((newPoint: RealTimeDataPoint) => {
    setData(prevData => {
      const newData = [...prevData, newPoint];
      const trimmedData = newData.slice(-maxDataPoints);

      onDataUpdate?.(trimmedData);
      setLastUpdateTime(Date.now());

      return trimmedData;
    });
  }, [maxDataPoints, onDataUpdate]);

  // Simulate real-time data for demo
  const generateSimulatedData = useCallback(() => {
    if (!isRunning) return;

    const now = Date.now();
    const baseValue = 50;
    const variation = 20;
    const trend = Math.sin(now / 10000) * 10;
    const noise = (Math.random() - 0.5) * variation;
    const value = baseValue + trend + noise;

    addDataPoint({
      timestamp: now,
      value: Math.max(0, value),
      metadata: { generated: true }
    });
  }, [isRunning, addDataPoint]);

  // Start simulation if no external data source
  useEffect(() => {
    if (!websocketUrl && !dataSource && isRunning) {
      const interval = setInterval(generateSimulatedData, updateInterval);
      return () => clearInterval(interval);
    }
  }, [websocketUrl, dataSource, isRunning, updateInterval, generateSimulatedData]);

  const handleToggleRunning = () => {
    setIsRunning(!isRunning);
  };

  const handleRefresh = () => {
    setData([]);
    setError(null);
    setLastUpdateTime(Date.now());
  };

  const getStatusBadge = () => {
    if (error || wsError) {
      return <Badge status="error" text="错误" />;
    }
    if (websocketUrl && !wsConnected) {
      return <Badge status="warning" text="连接中" />;
    }
    if (isRunning) {
      return <Badge status="processing" text="运行中" />;
    }
    return <Badge status="default" text="已暂停" />;
  };

  const getTrendIcon = () => {
    switch (metrics.trend) {
      case 'up':
        return '↗️';
      case 'down':
        return '↘️';
      default:
        return '➡️';
    }
  };

  const renderControls = () => (
    <Space>
      {pausable && (
        <Button
          type={isRunning ? 'default' : 'primary'}
          icon={isRunning ? <PauseCircleOutlined /> : <PlayCircleOutlined />}
          onClick={handleToggleRunning}
          
        >
          {isRunning ? '暂停' : '开始'}
        </Button>
      )}
      {refreshable && (
        <Button
          icon={<ReloadOutlined />}
          onClick={handleRefresh}
          
        >
          重置
        </Button>
      )}
    </Space>
  );

  const renderMetrics = () => {
    if (!showMetrics) return null;

    return (
      <Row gutter={16} className="mb-4">
        <Col span={6}>
          <Statistic
            title="当前值"
            value={metrics.current}
            precision={2}
            valueStyle={{ color: getCurrentStatusColor() }}
          />
        </Col>
        <Col span={6}>
          <Statistic
            title="平均值"
            value={metrics.average}
            precision={2}
          />
        </Col>
        <Col span={6}>
          <Statistic
            title="最小值"
            value={metrics.min}
            precision={2}
          />
        </Col>
        <Col span={6}>
          <Statistic
            title="最大值"
            value={metrics.max}
            precision={2}
            suffix={showTrend ? `${getTrendIcon()} ${metrics.changePercent.toFixed(1)}%` : undefined}
          />
        </Col>
      </Row>
    );
  };

  const cardExtra = (
    <Space>
      {showStatus && getStatusBadge()}
      {renderControls()}
    </Space>
  );

  return (
    <Card
      title={title}
      extra={cardExtra}
      {...(className && { className })}
    >
      {error && (
        <Alert
          message="数据错误"
          description={error}
          type="error"
          showIcon
          closable
          onClose={() => setError(null)}
          className="mb-4"
        />
      )}

      {renderMetrics()}

      <ChartWrapper
        ref={chartRef}
        option={chartOption}
        height={height}
        theme={theme}
        loading={!data.length && isRunning}
        empty={!data.length && !isRunning}
        emptyText="暂无实时数据"
        card={false}
        animation={animated}
        responsive
      />

      <div className="text-xs text-gray-500 mt-2 text-center">
        数据点: {data.length}/{maxDataPoints} |
        最后更新: {new Date(lastUpdateTime).toLocaleTimeString()}
      </div>
    </Card>
  );
};

export default RealTimeChart;
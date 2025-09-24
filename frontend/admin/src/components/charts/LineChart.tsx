import React, { useMemo, useRef, useEffect, useState } from 'react';
import { Card, Select, Space, Button, Tooltip, Spin } from 'antd';
import {
  FullscreenOutlined,
  DownloadOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import ReactECharts from 'echarts-for-react';
import type { EChartsOption } from 'echarts';
import classNames from 'classnames';
// Remove file-saver dependency - using direct download method instead

export interface LineChartData {
  labels: string[];
  datasets: Array<{
    label: string;
    data: number[];
    borderColor?: string;
    backgroundColor?: string;
    borderWidth?: number;
    fill?: boolean;
    tension?: number;
    pointRadius?: number;
    pointHoverRadius?: number;
  }>;
}

export interface LineChartProps {
  title?: string;
  data: LineChartData | null;
  loading?: boolean;
  height?: number;
  className?: string;
  style?: React.CSSProperties;
  showToolbar?: boolean;
  showLegend?: boolean;
  showGrid?: boolean;
  smooth?: boolean;
  theme?: 'light' | 'dark';
  colors?: string[];
  onRefresh?: () => void;
  onExport?: (format: 'png' | 'jpg' | 'svg') => void;
  chartOptions?: Partial<EChartsOption>;
}

const LineChart: React.FC<LineChartProps> = ({
  title,
  data,
  loading = false,
  height = 300,
  className,
  style,
  showToolbar = true,
  showLegend = true,
  showGrid = true,
  smooth = true,
  theme = 'light',
  colors = [
    '#1890ff',
    '#52c41a',
    '#faad14',
    '#f5222d',
    '#722ed1',
    '#fa8c16',
    '#13c2c2',
    '#eb2f96',
  ],
  onRefresh,
  onExport,
  chartOptions = {},
}) => {
  const chartRef = useRef<ReactECharts>(null);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [selectedTimeRange, setSelectedTimeRange] = useState('7d');

  // Time range options
  const timeRangeOptions = [
    { label: '最近7天', value: '7d' },
    { label: '最近30天', value: '30d' },
    { label: '最近3个月', value: '3m' },
    { label: '最近一年', value: '1y' },
  ];

  // Generate ECharts option
  const chartOption = useMemo((): EChartsOption => {
    if (!data || !data.datasets.length) {
      return {
        title: {
          text: '暂无数据',
          left: 'center',
          top: 'middle',
          textStyle: {
            color: theme === 'dark' ? '#fff' : '#999',
            fontSize: 14,
          },
        },
      };
    }

    const series = data.datasets.map((dataset, index) => ({
      name: dataset.label,
      type: 'line' as const,
      data: dataset.data,
      smooth: smooth,
      symbol: 'circle',
      symbolSize: dataset.pointRadius || 4,
      lineStyle: {
        width: dataset.borderWidth || 2,
        color: dataset.borderColor || colors[index % colors.length],
      },
      itemStyle: {
        color: dataset.borderColor || colors[index % colors.length],
      },
      ...(dataset.fill && { areaStyle: {
        color: {
          type: 'linear' as const,
          x: 0,
          y: 0,
          x2: 0,
          y2: 1,
          colorStops: [
            {
              offset: 0,
              color: dataset.backgroundColor || `${colors[index % colors.length]}20`,
            },
            {
              offset: 1,
              color: dataset.backgroundColor || `${colors[index % colors.length]}05`,
            },
          ],
        } as any,
      }}),
    }));

    const option: EChartsOption = {
      grid: {
        left: '3%',
        right: '4%',
        bottom: '3%',
        containLabel: true,
        show: showGrid,
        borderColor: theme === 'dark' ? '#333' : '#e8e8e8',
      },
      xAxis: {
        type: 'category',
        data: data.labels,
        axisLine: {
          lineStyle: {
            color: theme === 'dark' ? '#555' : '#d9d9d9',
          },
        },
        axisLabel: {
          color: theme === 'dark' ? '#ccc' : '#666',
        },
        splitLine: {
          show: false,
        },
      },
      yAxis: {
        type: 'value',
        axisLine: {
          show: false,
        },
        axisTick: {
          show: false,
        },
        axisLabel: {
          color: theme === 'dark' ? '#ccc' : '#666',
        },
        splitLine: {
          show: showGrid,
          lineStyle: {
            color: theme === 'dark' ? '#333' : '#f0f0f0',
            type: 'dashed',
          },
        },
      },
      series,
      ...(showLegend && { legend: {
        top: 10,
        left: 'center',
        textStyle: {
          color: theme === 'dark' ? '#ccc' : '#666',
        },
      }}),
      tooltip: {
        trigger: 'axis',
        backgroundColor: theme === 'dark' ? '#001529' : '#fff',
        borderColor: theme === 'dark' ? '#333' : '#d9d9d9',
        textStyle: {
          color: theme === 'dark' ? '#fff' : '#666',
        },
        axisPointer: {
          type: 'cross',
          crossStyle: {
            color: theme === 'dark' ? '#555' : '#999',
          },
        },
      },
      backgroundColor: 'transparent',
      ...chartOptions,
    };

    return option;
  }, [data, theme, showGrid, showLegend, smooth, colors, chartOptions]);

  // Handle fullscreen toggle
  const handleFullscreen = () => {
    if (!document.fullscreenElement) {
      const chartElement = chartRef.current?.getEchartsInstance().getDom().parentElement;
      if (chartElement) {
        chartElement.requestFullscreen?.();
        setIsFullscreen(true);
      }
    } else {
      document.exitFullscreen();
      setIsFullscreen(false);
    }
  };

  // Handle export
  const handleExport = (format: 'png' | 'jpg' | 'svg') => {
    const chartInstance = chartRef.current?.getEchartsInstance();
    if (!chartInstance) return;

    if (onExport) {
      onExport(format);
      return;
    }

    // Default export implementation
    const canvas = chartInstance.getDataURL({
      type: format === 'jpg' ? 'jpeg' : format,
      pixelRatio: 2,
      backgroundColor: theme === 'dark' ? '#001529' : '#fff',
    });

    // Convert data URL to blob and download
    const link = document.createElement('a');
    link.download = `chart-${Date.now()}.${format}`;
    link.href = canvas;
    link.click();
  };

  // Handle chart resize
  const handleResize = () => {
    const chartInstance = chartRef.current?.getEchartsInstance();
    if (chartInstance) {
      chartInstance.resize();
    }
  };

  // Listen for fullscreen changes
  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
    };

    document.addEventListener('fullscreenchange', handleFullscreenChange);
    return () => {
      document.removeEventListener('fullscreenchange', handleFullscreenChange);
    };
  }, []);

  // Resize chart when container size changes
  useEffect(() => {
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  // Render toolbar
  const renderToolbar = () => {
    if (!showToolbar) return null;

    return (
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-3">
          {title && (
            <h3 className="text-lg font-semibold mb-0">{title}</h3>
          )}
        </div>

        <Space>
          <Select
            value={selectedTimeRange}
            onChange={setSelectedTimeRange}
            options={timeRangeOptions}
            
            style={{ width: 100 }}
          />

          {onRefresh && (
            <Tooltip title="刷新">
              <Button
                type="text"
                
                icon={<ReloadOutlined />}
                onClick={onRefresh}
              />
            </Tooltip>
          )}

          <Tooltip title="导出">
            <Button
              type="text"
              
              icon={<DownloadOutlined />}
              onClick={() => handleExport('png')}
            />
          </Tooltip>

          <Tooltip title="全屏">
            <Button
              type="text"
              
              icon={<FullscreenOutlined />}
              onClick={handleFullscreen}
            />
          </Tooltip>
        </Space>
      </div>
    );
  };

  return (
    <Card
      className={classNames('line-chart-card', className)}
      style={style || {}}
      bordered={false}
      styles={{
        body: { padding: 16 },
      }}
    >
      {renderToolbar()}

      <div
        className="relative"
        style={{ height: isFullscreen ? '80vh' : height }}
      >
        {loading ? (
          <div className="flex items-center justify-center h-full">
            <Spin size="large" />
          </div>
        ) : (
          <ReactECharts
            ref={chartRef}
            option={chartOption}
            style={{ height: '100%', width: '100%' }}
            opts={{
              renderer: 'canvas',
            }}
            theme={theme}
            notMerge={true}
            lazyUpdate={true}
          />
        )}
      </div>
    </Card>
  );
};

// Predefined chart configurations
export const LineChartPresets = {
  // Traffic chart preset
  traffic: {
    colors: ['#1890ff', '#52c41a', '#faad14'],
    smooth: true,
    showGrid: true,
    chartOptions: {
      grid: { bottom: '15%' },
    },
  },

  // Performance chart preset
  performance: {
    colors: ['#ff4d4f', '#faad14', '#52c41a', '#1890ff'],
    smooth: false,
    showGrid: true,
    chartOptions: {
      yAxis: {
        max: 100,
        axisLabel: {
          formatter: '{value}%',
        },
      },
    },
  },

  // Revenue chart preset
  revenue: {
    colors: ['#52c41a', '#1890ff'],
    smooth: true,
    showGrid: true,
    chartOptions: {
      yAxis: {
        axisLabel: {
          formatter: '¥{value}',
        },
      },
    },
  },
};

export default LineChart;
// @ts-nocheck
import React, { useMemo, useRef, useEffect, useState } from 'react';
import { Card, Space, Button, Tooltip, Spin } from 'antd';
import {
  FullscreenOutlined,
  DownloadOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import ReactECharts from 'echarts-for-react';
import type { EChartsOption } from 'echarts';
import classNames from 'classnames';

export interface BarChartData {
  labels: string[];
  datasets: Array<{
    label: string;
    data: number[];
    backgroundColor?: string | string[];
    borderColor?: string | string[];
    borderWidth?: number;
  }>;
}

export interface BarChartProps {
  title?: string;
  data: BarChartData | null;
  loading?: boolean;
  height?: number;
  className?: string;
  style?: React.CSSProperties;
  showToolbar?: boolean;
  showLegend?: boolean;
  showGrid?: boolean;
  horizontal?: boolean;
  stacked?: boolean;
  theme?: 'light' | 'dark';
  colors?: string[];
  onRefresh?: () => void;
  onExport?: (format: 'png' | 'jpg' | 'svg') => void;
  chartOptions?: Partial<EChartsOption>;
}

const BarChart: React.FC<BarChartProps> = ({
  title,
  data,
  loading = false,
  height = 300,
  className,
  style,
  showToolbar = true,
  showLegend = true,
  showGrid = true,
  horizontal = false,
  stacked = false,
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
      type: 'bar' as const,
      data: dataset.data,
      ...(stacked ? { stack: 'total' } : {}),
      itemStyle: {
        color: Array.isArray(dataset.backgroundColor)
          ? dataset.backgroundColor[0] || colors[index % colors.length]
          : dataset.backgroundColor || colors[index % colors.length],
        borderColor: Array.isArray(dataset.borderColor)
          ? dataset.borderColor[0] || colors[index % colors.length]
          : dataset.borderColor || colors[index % colors.length],
        borderWidth: dataset.borderWidth || 0,
      },
      emphasis: {
        itemStyle: {
          shadowBlur: 10,
          shadowOffsetX: 0,
          shadowColor: 'rgba(0, 0, 0, 0.5)',
        },
      },
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
      xAxis: horizontal ? {
        type: 'value',
        axisLine: {
          lineStyle: {
            color: theme === 'dark' ? '#555' : '#d9d9d9',
          },
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
      } : {
        type: 'category',
        data: data.labels,
        axisLine: {
          lineStyle: {
            color: theme === 'dark' ? '#555' : '#d9d9d9',
          },
        },
        axisLabel: {
          color: theme === 'dark' ? '#ccc' : '#666',
          interval: 0,
          rotate: data.labels.some(label => label.length > 6) ? 45 : 0,
        },
        splitLine: {
          show: false,
        },
      },
      yAxis: horizontal ? {
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
      } : {
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
          type: 'shadow',
        },
      },
      backgroundColor: 'transparent',
      ...chartOptions,
    };

    return option;
  }, [data, theme, showGrid, showLegend, horizontal, stacked, colors, chartOptions]);

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
    link.download = `bar-chart-${Date.now()}.${format}`;
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
      className={classNames('bar-chart-card', className)}
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

export default BarChart;
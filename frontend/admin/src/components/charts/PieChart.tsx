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

export interface PieChartData {
  labels: string[];
  datasets: Array<{
    data: number[];
    backgroundColor?: string[];
    borderColor?: string[];
    borderWidth?: number;
  }>;
}

export interface PieChartProps {
  title?: string;
  data: PieChartData | null;
  loading?: boolean;
  height?: number;
  className?: string;
  style?: React.CSSProperties;
  showToolbar?: boolean;
  showLegend?: boolean;
  donut?: boolean;
  showLabels?: boolean;
  labelType?: 'name' | 'value' | 'percent' | 'name-value' | 'name-percent';
  theme?: 'light' | 'dark';
  colors?: string[];
  onRefresh?: () => void;
  onExport?: (format: 'png' | 'jpg' | 'svg') => void;
  chartOptions?: Partial<EChartsOption>;
}

const PieChart: React.FC<PieChartProps> = ({
  title,
  data,
  loading = false,
  height = 300,
  className,
  style,
  showToolbar = true,
  showLegend = true,
  donut = false,
  showLabels = true,
  labelType = 'name-percent',
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
    if (!data || !data.datasets.length || !data.datasets[0].data.length) {
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

    const dataset = data.datasets[0];
    const total = dataset.data.reduce((sum, value) => sum + value, 0);

    const seriesData = data.labels.map((label, index) => ({
      name: label,
      value: dataset.data[index],
      itemStyle: {
        color: dataset.backgroundColor?.[index] || colors[index % colors.length],
        borderColor: dataset.borderColor?.[index] || '#fff',
        borderWidth: dataset.borderWidth || 1,
      },
    }));

    // Generate label formatter based on type
    const getLabelFormatter = () => {
      switch (labelType) {
        case 'name':
          return '{b}';
        case 'value':
          return '{c}';
        case 'percent':
          return '{d}%';
        case 'name-value':
          return '{b}: {c}';
        case 'name-percent':
          return '{b}: {d}%';
        default:
          return '{b}: {d}%';
      }
    };

    const option: EChartsOption = {
      series: [
        {
          type: 'pie',
          radius: donut ? ['40%', '70%'] : '70%',
          center: ['50%', '50%'],
          data: seriesData,
          emphasis: {
            itemStyle: {
              shadowBlur: 10,
              shadowOffsetX: 0,
              shadowColor: 'rgba(0, 0, 0, 0.5)',
            },
          },
          label: showLabels ? {
            formatter: getLabelFormatter(),
            color: theme === 'dark' ? '#ccc' : '#666',
          } : {
            show: false,
          },
          labelLine: {
            show: showLabels,
            lineStyle: {
              color: theme === 'dark' ? '#555' : '#999',
            },
          },
          avoidLabelOverlap: true,
        },
      ],
      ...(showLegend && { legend: {
        orient: 'vertical',
        left: 'right',
        top: 'center',
        textStyle: {
          color: theme === 'dark' ? '#ccc' : '#666',
        },
        formatter: (name: string) => {
          const index = data.labels.indexOf(name);
          const value = dataset.data[index];
          const percent = ((value / total) * 100).toFixed(1);
          return `${name} (${percent}%)`;
        },
      }}),
      tooltip: {
        trigger: 'item',
        backgroundColor: theme === 'dark' ? '#001529' : '#fff',
        borderColor: theme === 'dark' ? '#333' : '#d9d9d9',
        textStyle: {
          color: theme === 'dark' ? '#fff' : '#666',
        },
        formatter: (params: any) => {
          return `${params.name}<br/>${params.value} (${params.percent}%)`;
        },
      },
      backgroundColor: 'transparent',
      ...chartOptions,
    };

    return option;
  }, [data, theme, donut, showLabels, showLegend, labelType, colors, chartOptions]);

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
    link.download = `pie-chart-${Date.now()}.${format}`;
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
      className={classNames('pie-chart-card', className)}
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

export default PieChart;
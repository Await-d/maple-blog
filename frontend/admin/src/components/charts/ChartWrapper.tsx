import React, { forwardRef, useImperativeHandle, useRef, useMemo, useCallback } from 'react';
import { Card, Spin, Empty, Button, Dropdown, message } from 'antd';
import { DownloadOutlined, FullscreenOutlined, ReloadOutlined, SettingOutlined } from '@ant-design/icons';
import ReactEChartsCore from 'echarts-for-react/lib/core';
import * as echarts from 'echarts/core';
import {
  BarChart,
  LineChart,
  PieChart,
  ScatterChart,
  RadarChart,
  TreeChart,
  TreemapChart,
  SunburstChart,
  BoxplotChart,
  CandlestickChart,
  HeatmapChart,
  MapChart,
  ParallelChart,
  LinesChart,
  GraphChart,
  SankeyChart,
  FunnelChart,
  GaugeChart,
  PictorialBarChart,
  ThemeRiverChart
} from 'echarts/charts';
import {
  GridComponent,
  PolarComponent,
  RadarComponent,
  GeoComponent,
  SingleAxisComponent,
  ParallelComponent,
  CalendarComponent,
  GraphicComponent,
  ToolboxComponent,
  TooltipComponent,
  AxisPointerComponent,
  BrushComponent,
  TitleComponent,
  TimelineComponent,
  MarkPointComponent,
  MarkLineComponent,
  MarkAreaComponent,
  LegendComponent,
  DataZoomComponent,
  DataZoomInsideComponent,
  DataZoomSliderComponent,
  VisualMapComponent,
  VisualMapContinuousComponent,
  VisualMapPiecewiseComponent,
  AriaComponent,
  TransformComponent
} from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import { UniversalTransition } from 'echarts/features';
import classNames from 'classnames';

// Register ECharts components
echarts.use([
  // Charts
  BarChart,
  LineChart,
  PieChart,
  ScatterChart,
  RadarChart,
  TreeChart,
  TreemapChart,
  SunburstChart,
  BoxplotChart,
  CandlestickChart,
  HeatmapChart,
  MapChart,
  ParallelChart,
  LinesChart,
  GraphChart,
  SankeyChart,
  FunnelChart,
  GaugeChart,
  PictorialBarChart,
  ThemeRiverChart,

  // Components
  GridComponent,
  PolarComponent,
  RadarComponent,
  GeoComponent,
  SingleAxisComponent,
  ParallelComponent,
  CalendarComponent,
  GraphicComponent,
  ToolboxComponent,
  TooltipComponent,
  AxisPointerComponent,
  BrushComponent,
  TitleComponent,
  TimelineComponent,
  MarkPointComponent,
  MarkLineComponent,
  MarkAreaComponent,
  LegendComponent,
  DataZoomComponent,
  DataZoomInsideComponent,
  DataZoomSliderComponent,
  VisualMapComponent,
  VisualMapContinuousComponent,
  VisualMapPiecewiseComponent,
  AriaComponent,
  TransformComponent,

  // Renderer & Features
  CanvasRenderer,
  UniversalTransition
]);

export interface ChartWrapperProps {
  // Chart configuration
  option: echarts.EChartsOption;
  type?: 'line' | 'bar' | 'pie' | 'scatter' | 'radar' | 'heatmap' | 'tree' | 'treemap' | 'sunburst' | 'gauge' | 'funnel' | 'sankey' | 'graph' | 'custom';
  theme?: 'light' | 'dark' | string;

  // Layout & Style
  height?: number | string;
  width?: number | string;
  className?: string;
  style?: React.CSSProperties;

  // Loading & Error states
  loading?: boolean;
  error?: string | null;
  empty?: boolean;
  emptyText?: string;

  // Card wrapper
  card?: boolean;
  title?: React.ReactNode;
  extra?: React.ReactNode;
  bodyStyle?: React.CSSProperties;

  // Features
  toolbar?: boolean;
  exportable?: boolean;
  refreshable?: boolean;
  fullscreen?: boolean;
  configurable?: boolean;

  // Events
  onChartReady?: (chart: echarts.ECharts) => void;
  onEvents?: Record<string, (params: unknown) => void>;
  onRefresh?: () => void;
  onExport?: (format: 'png' | 'svg') => void;
  onFullscreen?: () => void;
  onConfigure?: () => void;

  // Animation
  animation?: boolean;
  animationDuration?: number;
  animationEasing?: string;

  // Responsive
  responsive?: boolean;
  autoResize?: boolean;

  // Data update
  notMerge?: boolean;
  lazyUpdate?: boolean;
  silent?: boolean;
}

export interface ChartWrapperRef {
  chart: echarts.ECharts | null;
  exportChart: (format: 'png' | 'svg') => void;
  resize: () => void;
  dispatchAction: (payload: unknown) => void;
  getOption: () => echarts.EChartsOption;
  setOption: (option: echarts.EChartsOption, notMerge?: boolean, lazyUpdate?: boolean) => void;
}

const ChartWrapper = forwardRef<ChartWrapperRef, ChartWrapperProps>(({
  option,
  type = 'line',
  theme = 'light',
  height = 400,
  width = '100%',
  className,
  style,
  loading = false,
  error = null,
  empty = false,
  emptyText = '暂无数据',
  card = true,
  title,
  extra,
  bodyStyle,
  toolbar = true,
  exportable = true,
  refreshable = true,
  fullscreen = false,
  configurable = false,
  onChartReady,
  onEvents = {},
  onRefresh,
  onExport,
  onFullscreen,
  onConfigure,
  animation = true,
  animationDuration = 1000,
  animationEasing = 'cubicOut',
  responsive = true,
  autoResize: _autoResize = true,
  notMerge = false,
  lazyUpdate = false,
  silent: _silent = false
}, ref) => {
  const chartRef = useRef<ReactEChartsCore>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  // Enhanced chart option with animation and responsive features
  const enhancedOption = useMemo(() => {
    if (!option) return {};

    const baseOption = {
      ...option,
      animation,
      animationDuration,
      animationEasing,
      backgroundColor: 'transparent',
      textStyle: {
        fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
      }
    };

    // Add default grid and responsive settings
    if (responsive && (type === 'line' || type === 'bar' || type === 'scatter')) {
      baseOption.grid = {
        left: '3%',
        right: '4%',
        bottom: '3%',
        top: '10%',
        containLabel: true,
        ...baseOption.grid
      };
    }

    // Add default tooltip
    if (!baseOption.tooltip) {
      baseOption.tooltip = {
        trigger: type === 'pie' ? 'item' : 'axis',
        backgroundColor: 'rgba(50, 50, 50, 0.95)',
        borderColor: 'rgba(50, 50, 50, 0.95)',
        textStyle: {
          color: '#fff'
        },
        extraCssText: 'box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15); border-radius: 8px;'
      };
    }

    // Add default legend for multi-series charts
    if (!baseOption.legend && option.series && Array.isArray(option.series) && option.series.length > 1) {
      baseOption.legend = {
        top: 'top',
        type: 'scroll',
        itemGap: 20,
        textStyle: {
          fontSize: 12
        }
      };
    }

    return baseOption;
  }, [option, animation, animationDuration, animationEasing, responsive, type]);

  // Chart methods
  const exportChart = useCallback((format: 'png' | 'svg' = 'png') => {
    const chart = chartRef.current?.getEchartsInstance();
    if (!chart) return;

    try {
      let dataURL: string;

      if (format === 'svg') {
        dataURL = chart.renderToSVGString();
        const blob = new Blob([dataURL], { type: 'image/svg+xml' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `chart.${format}`;
        link.click();
        URL.revokeObjectURL(url);
      } else {
        dataURL = chart.getDataURL({
          type: format,
          pixelRatio: 2,
          backgroundColor: '#fff'
        });
        const link = document.createElement('a');
        link.href = dataURL;
        link.download = `chart.${format}`;
        link.click();
      }

      message.success('图表导出成功');
      onExport?.(format);
    } catch (error) {
      console.error('Chart export failed:', error);
      message.error('图表导出失败');
    }
  }, [onExport]);

  const resizeChart = () => {
    const chart = chartRef.current?.getEchartsInstance();
    if (chart) {
      chart.resize();
    }
  };

  // Expose chart methods via ref
  useImperativeHandle(ref, () => ({
    chart: chartRef.current?.getEchartsInstance() || null,
    exportChart,
    resize: resizeChart,
    dispatchAction: (payload: unknown) => {
      const chart = chartRef.current?.getEchartsInstance();
      if (chart) {
        chart.dispatchAction(payload);
      }
    },
    getOption: () => {
      const chart = chartRef.current?.getEchartsInstance();
      return chart?.getOption() || null;
    },
    setOption: (newOption: echarts.EChartsOption, merge?: boolean, lazy?: boolean) => {
      const chart = chartRef.current?.getEchartsInstance();
      if (chart) {
        chart.setOption(newOption, !merge, lazy);
      }
    }
  }), [exportChart]);

  // Handle chart ready
  const handleChartReady = (chart: echarts.ECharts) => {
    // Register events
    Object.entries(onEvents).forEach(([eventName, handler]) => {
      chart.on(eventName, handler);
    });

    onChartReady?.(chart);
  };

  // Toolbar actions
  const toolbarItems = [
    refreshable && {
      key: 'refresh',
      icon: <ReloadOutlined />,
      label: '刷新',
      onClick: onRefresh
    },
    exportable && {
      key: 'export',
      icon: <DownloadOutlined />,
      label: '导出',
      children: [
        {
          key: 'png',
          label: 'PNG图片',
          onClick: () => exportChart('png')
        },
        {
          key: 'svg',
          label: 'SVG矢量图',
          onClick: () => exportChart('svg')
        }
      ]
    },
    fullscreen && {
      key: 'fullscreen',
      icon: <FullscreenOutlined />,
      label: '全屏',
      onClick: onFullscreen
    },
    configurable && {
      key: 'configure',
      icon: <SettingOutlined />,
      label: '配置',
      onClick: onConfigure
    }
  ].filter(Boolean);

  const renderToolbar = () => {
    if (!toolbar || toolbarItems.length === 0) return null;

    return (
      <div className="flex items-center gap-2">
        {toolbarItems.map((item: { key: string; label: string; icon?: React.ReactNode; onClick?: () => void }) => {
          if (item.children) {
            return (
              <Dropdown
                key={item.key}
                menu={{ items: item.children }}
                trigger={['click']}
              >
                <Button
                  type="text"
                  
                  icon={item.icon}
                  title={item.label}
                />
              </Dropdown>
            );
          }

          return (
            <Button
              key={item.key}
              type="text"
              
              icon={item.icon}
              title={item.label}
              onClick={item.onClick}
            />
          );
        })}
      </div>
    );
  };

  const renderChart = () => {
    if (loading) {
      return (
        <div
          className="flex items-center justify-center"
          style={{ height: typeof height === 'number' ? `${height}px` : height }}
        >
          <Spin size="large" />
        </div>
      );
    }

    if (error) {
      return (
        <div
          className="flex flex-col items-center justify-center text-red-500"
          style={{ height: typeof height === 'number' ? `${height}px` : height }}
        >
          <div className="text-lg mb-2">图表加载失败</div>
          <div className="text-sm opacity-75">{error}</div>
          {refreshable && (
            <Button
              type="primary"
              
              onClick={onRefresh}
              className="mt-4"
            >
              重试
            </Button>
          )}
        </div>
      );
    }

    if (empty || !enhancedOption || Object.keys(enhancedOption).length === 0) {
      return (
        <div
          className="flex items-center justify-center"
          style={{ height: typeof height === 'number' ? `${height}px` : height }}
        >
          <Empty description={emptyText} />
        </div>
      );
    }

    return (
      <ReactEChartsCore
        ref={chartRef}
        echarts={echarts}
        option={enhancedOption}
        theme={theme}
        style={{
          height: typeof height === 'number' ? `${height}px` : height,
          width: typeof width === 'number' ? `${width}px` : width
        }}
        onChartReady={handleChartReady}
        notMerge={notMerge}
        lazyUpdate={lazyUpdate}
        onEvents={onEvents}
        opts={{ renderer: 'canvas' }}
      />
    );
  };

  const chartContent = (
    <div
      ref={containerRef}
      className={classNames('chart-wrapper', className)}
      style={style}
    >
      {renderChart()}
    </div>
  );

  if (!card) {
    return chartContent;
  }

  return (
    <Card
      title={title}
      extra={extra || renderToolbar()}
      bodyStyle={{
        padding: 16,
        ...bodyStyle
      }}
      className={classNames('chart-card', className)}
      style={style || {}}
    >
      {chartContent}
    </Card>
  );
});

ChartWrapper.displayName = 'ChartWrapper';

export default ChartWrapper;
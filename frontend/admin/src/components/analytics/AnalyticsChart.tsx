import React, { useMemo, useRef } from 'react';
import ReactECharts from 'echarts-for-react';
import type { EChartsOption } from 'echarts';
import { Card, Empty, Button, Space, Dropdown, Tooltip } from 'antd';
import {
  DownloadOutlined,
  ExpandOutlined,
  ReloadOutlined,
  MoreOutlined
} from '@ant-design/icons';
import type { ChartData, ChartInstance, TooltipParam } from './chart-types';

interface AnalyticsChartProps {
  type: 'line' | 'bar' | 'pie' | 'scatter' | 'heatmap' | 'radar' | 'funnel' | 'gauge' | 'map' | 'treemap' | 'sunburst' | 'sankey';
  title?: string;
  subtitle?: string;
  data: ChartData;
  loading?: boolean;
  height?: number | string;
  onRefresh?: () => void;
  onExport?: (format: 'png' | 'svg' | 'pdf') => void;
  onFullscreen?: () => void;
  customOptions?: EChartsOption;
  showToolbox?: boolean;
  showLegend?: boolean;
  theme?: 'light' | 'dark';
  responsive?: boolean;
  animation?: boolean;
  className?: string;
}

const AnalyticsChart: React.FC<AnalyticsChartProps> = ({
  type,
  title,
  subtitle,
  data,
  loading = false,
  height = 400,
  onRefresh,
  onExport,
  onFullscreen,
  customOptions,
  showToolbox = true,
  showLegend = true,
  theme = 'light',
  animation = true,
  className
}) => {
  const chartRef = useRef<ChartInstance>(null);

  // Generate chart options based on type
  const chartOptions = useMemo((): EChartsOption => {
    const baseOptions: EChartsOption = {
      backgroundColor: theme === 'dark' ? '#1f1f1f' : 'transparent',
      animation,
      animationDuration: 1000,
      animationEasing: 'cubicOut',
      title: {
        text: title || '',
        subtext: subtitle || '',
        left: 'left',
        textStyle: {
          color: theme === 'dark' ? '#fff' : '#333',
          fontSize: 16,
          fontWeight: 'bold'
        },
        subtextStyle: {
          color: theme === 'dark' ? '#aaa' : '#666',
          fontSize: 12
        }
      },
      tooltip: {
        trigger: type === 'pie' || type === 'funnel' ? 'item' : 'axis',
        backgroundColor: theme === 'dark' ? 'rgba(0, 0, 0, 0.8)' : 'rgba(255, 255, 255, 0.95)',
        borderColor: theme === 'dark' ? '#333' : '#ddd',
        textStyle: {
          color: theme === 'dark' ? '#fff' : '#333'
        },
        formatter: (params: TooltipParam | TooltipParam[]) => {
          if (Array.isArray(params)) {
            let result = `<div style="font-weight: bold; margin-bottom: 8px;">${params[0].axisValue}</div>`;
            params.forEach((item: TooltipParam) => {
              result += `
                <div style="display: flex; align-items: center; margin: 4px 0;">
                  <span style="display: inline-block; width: 10px; height: 10px; background: ${item.color}; border-radius: 50%; margin-right: 8px;"></span>
                  <span style="flex: 1;">${item.seriesName}:</span>
                  <span style="font-weight: bold; margin-left: 8px;">${item.value}</span>
                </div>
              `;
            });
            return result;
          }
          return `${params.name}: ${params.value}`;
        }
      },
      ...(showLegend && { legend: {
        type: 'scroll',
        orient: 'horizontal',
        bottom: 0,
        textStyle: {
          color: theme === 'dark' ? '#aaa' : '#666'
        }
      }}),
      ...(showToolbox && { toolbox: {
        feature: {
          dataZoom: {
            yAxisIndex: 'none',
            title: {
              zoom: '区域缩放',
              back: '缩放还原'
            }
          },
          dataView: {
            title: '数据视图',
            lang: ['数据视图', '关闭', '刷新']
          },
          magicType: {
            type: ['line', 'bar', 'stack'],
            title: {
              line: '切换为折线图',
              bar: '切换为柱状图',
              stack: '切换为堆叠'
            }
          },
          restore: {
            title: '还原'
          },
          saveAsImage: {
            title: '保存为图片',
            pixelRatio: 2
          }
        },
        iconStyle: {
          borderColor: theme === 'dark' ? '#aaa' : '#666'
        }
      }}),
      grid: {
        left: '3%',
        right: '4%',
        bottom: showLegend ? '15%' : '3%',
        top: title ? '15%' : '10%',
        containLabel: true
      }
    };

    // Type-specific options
    switch (type) {
      case 'line':
        return {
          ...baseOptions,
          xAxis: {
            type: 'category',
            data: data.categories || [],
            boundaryGap: false,
            axisLine: {
              lineStyle: {
                color: theme === 'dark' ? '#555' : '#ddd'
              }
            },
            axisLabel: {
              color: theme === 'dark' ? '#aaa' : '#666',
              rotate: data.categories?.length > 10 ? 45 : 0
            },
            splitLine: {
              show: true,
              lineStyle: {
                color: theme === 'dark' ? '#333' : '#f0f0f0',
                type: 'dashed'
              }
            }
          },
          yAxis: {
            type: 'value',
            axisLine: {
              lineStyle: {
                color: theme === 'dark' ? '#555' : '#ddd'
              }
            },
            axisLabel: {
              color: theme === 'dark' ? '#aaa' : '#666',
              formatter: (value: number) => {
                if (value >= 1000000) return `${(value / 1000000).toFixed(1)}M`;
                if (value >= 1000) return `${(value / 1000).toFixed(1)}K`;
                return value.toString();
              }
            },
            splitLine: {
              lineStyle: {
                color: theme === 'dark' ? '#333' : '#f0f0f0',
                type: 'dashed'
              }
            }
          },
          series: data.series?.map((s) => ({
            ...s,
            type: 'line',
            smooth: true,
            symbol: 'circle',
            symbolSize: 6,
            lineStyle: {
              width: 2,
              shadowColor: 'rgba(0, 0, 0, 0.1)',
              shadowBlur: 10,
              shadowOffsetY: 5
            },
            areaStyle: s.showArea ? {
              opacity: 0.1
            } : undefined,
            emphasis: {
              focus: 'series',
              itemStyle: {
                borderColor: '#fff',
                borderWidth: 2
              }
            }
          })) || [],
          ...customOptions
        };

      case 'bar':
        return {
          ...baseOptions,
          xAxis: {
            type: 'category',
            data: data.categories || [],
            axisLine: {
              lineStyle: {
                color: theme === 'dark' ? '#555' : '#ddd'
              }
            },
            axisLabel: {
              color: theme === 'dark' ? '#aaa' : '#666',
              rotate: data.categories?.length > 10 ? 45 : 0
            }
          },
          yAxis: {
            type: 'value',
            axisLine: {
              lineStyle: {
                color: theme === 'dark' ? '#555' : '#ddd'
              }
            },
            axisLabel: {
              color: theme === 'dark' ? '#aaa' : '#666'
            },
            splitLine: {
              lineStyle: {
                color: theme === 'dark' ? '#333' : '#f0f0f0',
                type: 'dashed'
              }
            }
          },
          series: data.series?.map((s) => ({
            ...s,
            type: 'bar',
            barWidth: '60%',
            itemStyle: {
              borderRadius: [4, 4, 0, 0],
              shadowColor: 'rgba(0, 0, 0, 0.1)',
              shadowBlur: 10,
              shadowOffsetY: 5
            },
            emphasis: {
              itemStyle: {
                shadowColor: 'rgba(0, 0, 0, 0.2)',
                shadowBlur: 20,
                shadowOffsetY: 10
              }
            }
          })) || [],
          ...customOptions
        };

      case 'pie':
        return {
          ...baseOptions,
          series: [{
            type: 'pie',
            radius: ['40%', '70%'],
            center: ['50%', '50%'],
            data: data.items || [],
            label: {
              show: true,
              formatter: '{b}\n{d}%',
              color: theme === 'dark' ? '#aaa' : '#666'
            },
            labelLine: {
              show: true,
              lineStyle: {
                color: theme === 'dark' ? '#555' : '#ddd'
              }
            },
            itemStyle: {
              borderRadius: 10,
              borderColor: theme === 'dark' ? '#1f1f1f' : '#fff',
              borderWidth: 2,
              shadowBlur: 10,
              shadowOffsetX: 0,
              shadowOffsetY: 0,
              shadowColor: 'rgba(0, 0, 0, 0.2)'
            },
            emphasis: {
              itemStyle: {
                shadowBlur: 20,
                shadowOffsetX: 0,
                shadowOffsetY: 0,
                shadowColor: 'rgba(0, 0, 0, 0.3)'
              },
              label: {
                show: true,
                fontSize: 14,
                fontWeight: 'bold'
              }
            }
          }],
          ...customOptions
        };

      case 'heatmap':
        return {
          ...baseOptions,
          xAxis: {
            type: 'category',
            data: data.xAxis || [],
            splitArea: {
              show: true
            },
            axisLabel: {
              color: theme === 'dark' ? '#aaa' : '#666'
            }
          },
          yAxis: {
            type: 'category',
            data: data.yAxis || [],
            splitArea: {
              show: true
            },
            axisLabel: {
              color: theme === 'dark' ? '#aaa' : '#666'
            }
          },
          visualMap: {
            min: data.min || 0,
            max: data.max || 100,
            calculable: true,
            orient: 'horizontal',
            left: 'center',
            bottom: showLegend ? '10%' : '5%',
            inRange: {
              color: ['#313695', '#4575b4', '#74add1', '#abd9e9', '#e0f3f8', '#ffffbf', '#fee090', '#fdae61', '#f46d43', '#d73027', '#a50026']
            },
            textStyle: {
              color: theme === 'dark' ? '#aaa' : '#666'
            }
          },
          series: [{
            type: 'heatmap',
            data: data.values || [],
            label: {
              show: true,
              color: theme === 'dark' ? '#fff' : '#333'
            },
            emphasis: {
              itemStyle: {
                shadowBlur: 10,
                shadowColor: 'rgba(0, 0, 0, 0.5)'
              }
            }
          }],
          ...customOptions
        };

      case 'funnel':
        return {
          ...baseOptions,
          series: [{
            type: 'funnel',
            left: '10%',
            top: 60,
            bottom: 60,
            width: '80%',
            min: 0,
            max: 100,
            minSize: '0%',
            maxSize: '100%',
            sort: 'descending',
            gap: 2,
            label: {
              show: true,
              position: 'inside',
              formatter: '{b}\n{c}%',
              color: '#fff'
            },
            labelLine: {
              length: 10,
              lineStyle: {
                width: 1,
                type: 'solid'
              }
            },
            itemStyle: {
              borderColor: theme === 'dark' ? '#1f1f1f' : '#fff',
              borderWidth: 1,
              shadowBlur: 10,
              shadowOffsetX: 0,
              shadowOffsetY: 5,
              shadowColor: 'rgba(0, 0, 0, 0.2)'
            },
            emphasis: {
              label: {
                fontSize: 16,
                fontWeight: 'bold'
              }
            },
            data: data.items || []
          }],
          ...customOptions
        };

      case 'radar':
        return {
          ...baseOptions,
          radar: {
            indicator: data.indicators || [],
            shape: 'polygon',
            splitNumber: 5,
            axisName: {
              color: theme === 'dark' ? '#aaa' : '#666'
            },
            splitLine: {
              lineStyle: {
                color: theme === 'dark' ? '#333' : '#ddd',
                type: 'dashed'
              }
            },
            splitArea: {
              show: true,
              areaStyle: {
                color: theme === 'dark' ? ['rgba(255,255,255,0.05)', 'rgba(255,255,255,0.1)'] : ['rgba(0,0,0,0.01)', 'rgba(0,0,0,0.02)']
              }
            },
            axisLine: {
              lineStyle: {
                color: theme === 'dark' ? '#555' : '#ddd'
              }
            }
          },
          series: [{
            type: 'radar',
            data: data.series || [],
            symbol: 'circle',
            symbolSize: 6,
            lineStyle: {
              width: 2
            },
            areaStyle: {
              opacity: 0.2
            },
            emphasis: {
              lineStyle: {
                width: 3
              },
              areaStyle: {
                opacity: 0.4
              }
            }
          }],
          ...customOptions
        };

      case 'gauge':
        return {
          ...baseOptions,
          series: [{
            type: 'gauge',
            startAngle: 180,
            endAngle: 0,
            min: data.min || 0,
            max: data.max || 100,
            splitNumber: 10,
            radius: '80%',
            center: ['50%', '75%'],
            axisLine: {
              lineStyle: {
                width: 20,
                color: [
                  [0.3, '#67e0e3'],
                  [0.7, '#37a2da'],
                  [1, '#fd666d']
                ]
              }
            },
            axisTick: {
              distance: -25,
              length: 8,
              lineStyle: {
                color: '#fff',
                width: 2
              }
            },
            axisLabel: {
              distance: -35,
              color: theme === 'dark' ? '#aaa' : '#666',
              fontSize: 12
            },
            pointer: {
              length: '75%',
              width: 6,
              itemStyle: {
                color: 'auto'
              }
            },
            title: {
              offsetCenter: [0, '-10%'],
              color: theme === 'dark' ? '#fff' : '#333',
              fontSize: 14
            },
            detail: {
              valueAnimation: true,
              formatter: '{value}%',
              color: 'auto',
              fontSize: 24,
              offsetCenter: [0, '10%']
            },
            data: [{
              value: data.value || 0,
              name: data.name || ''
            }]
          }],
          ...customOptions
        };

      default:
        return { ...baseOptions, ...customOptions };
    }
  }, [type, data, title, subtitle, showToolbox, showLegend, theme, animation, customOptions]);

  // Handle chart actions
  const handleExport = (format: 'png' | 'svg' | 'pdf') => {
    if (chartRef.current) {
      const instance = chartRef.current.getEchartsInstance();
      const url = instance.getDataURL({
        type: format === 'pdf' ? 'png' : format,
        pixelRatio: 2,
        backgroundColor: theme === 'dark' ? '#1f1f1f' : '#fff'
      });

      if (format === 'pdf') {
        // Would need additional library for PDF export
        console.log('PDF export requires additional implementation');
      } else {
        const link = document.createElement('a');
        link.download = `chart-${Date.now()}.${format}`;
        link.href = url;
        link.click();
      }
    }
    onExport?.(format);
  };

  const menuItems = [
    {
      key: 'export-png',
      label: '导出为PNG',
      icon: <DownloadOutlined />,
      onClick: () => handleExport('png')
    },
    {
      key: 'export-svg',
      label: '导出为SVG',
      icon: <DownloadOutlined />,
      onClick: () => handleExport('svg')
    },
    {
      key: 'fullscreen',
      label: '全屏查看',
      icon: <ExpandOutlined />,
      onClick: onFullscreen || (() => {})
    }
  ];

  return (
    <Card
      {...(className && { className })}
      bodyStyle={{ padding: 0, height: typeof height === 'number' ? `${height}px` : height }}
      loading={loading}
    >
      {!loading && data ? (
        <div style={{ position: 'relative', height: '100%' }}>
          <ReactECharts
            ref={chartRef}
            option={chartOptions}
            style={{ height: '100%' }}
            theme={theme}
            opts={{ renderer: 'svg' }}
            notMerge
            lazyUpdate
          />
          {(onRefresh || onExport || onFullscreen) && (
            <Space
              style={{
                position: 'absolute',
                top: 10,
                right: 10,
                zIndex: 10
              }}
            >
              {onRefresh && (
                <Tooltip title="刷新数据">
                  <Button
                    type="text"
                    icon={<ReloadOutlined />}
                    onClick={onRefresh}
                    
                  />
                </Tooltip>
              )}
              <Dropdown menu={{ items: menuItems }} placement="bottomRight">
                <Button
                  type="text"
                  icon={<MoreOutlined />}
                  
                />
              </Dropdown>
            </Space>
          )}
        </div>
      ) : (
        <Empty description="暂无数据" style={{ padding: '50px 0' }} />
      )}
    </Card>
  );
};

export default AnalyticsChart;
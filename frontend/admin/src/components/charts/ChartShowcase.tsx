// @ts-nocheck
import React, { useState, useRef, useEffect } from 'react';
import { Card, Tabs, Button, Space, Select, Switch, Row, Col, Typography, Divider } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';
import ChartWrapper, { ChartWrapperRef } from './ChartWrapper';
import MultiChart, { ChartConfig } from './MultiChart';
import RealTimeChart from './RealTimeChart';
import HeatMapChart from './HeatMapChart';
import TreeMapChart from './TreeMapChart';
import { CHART_TEMPLATES, COLOR_PALETTES } from '../../utils/chartUtils';

const { Title, Paragraph } = Typography;
const { TabPane } = Tabs;

interface ChartShowcaseProps {
  className?: string;
}

const ChartShowcase: React.FC<ChartShowcaseProps> = ({ className }) => {
  const [theme, setTheme] = useState<'light' | 'dark'>('light');
  const [animated, setAnimated] = useState(true);
  const [autoRefresh, setAutoRefresh] = useState(false);
  const chartRef = useRef<ChartWrapperRef>(null);

  // Generate sample data
  const generateLineData = () => {
    const categories = ['周一', '周二', '周三', '周四', '周五', '周六', '周日'];
    const series = [
      {
        name: '访问量',
        type: 'line',
        data: categories.map(() => Math.floor(Math.random() * 1000) + 100),
        smooth: true
      },
      {
        name: '订单量',
        type: 'line',
        data: categories.map(() => Math.floor(Math.random() * 500) + 50),
        smooth: true
      }
    ];

    return {
      xAxis: { type: 'category', data: categories },
      yAxis: { type: 'value' },
      series,
      legend: { data: series.map(s => s.name) }
    };
  };

  const generateBarData = () => {
    const categories = ['产品A', '产品B', '产品C', '产品D', '产品E'];
    return {
      xAxis: { type: 'category', data: categories },
      yAxis: { type: 'value' },
      series: [{
        name: '销量',
        type: 'bar',
        data: categories.map(() => Math.floor(Math.random() * 800) + 200),
        itemStyle: {
          color: (params: any) => {
            const colors = COLOR_PALETTES.business;
            return colors[params.dataIndex % colors.length];
          }
        }
      }]
    };
  };

  const generatePieData = () => {
    const data = [
      { value: 335, name: '直接访问' },
      { value: 310, name: '邮件营销' },
      { value: 234, name: '联盟广告' },
      { value: 135, name: '视频广告' },
      { value: 1548, name: '搜索引擎' }
    ];

    return {
      series: [{
        name: '访问来源',
        type: 'pie',
        radius: ['40%', '70%'],
        data,
        emphasis: {
          itemStyle: {
            shadowBlur: 10,
            shadowOffsetX: 0,
            shadowColor: 'rgba(0, 0, 0, 0.5)'
          }
        },
        label: {
          formatter: '{b}: {c} ({d}%)'
        }
      }],
      legend: {
        orient: 'vertical',
        left: 'left',
        data: data.map(item => item.name)
      }
    };
  };

  const generateScatterData = () => {
    const data = Array.from({ length: 50 }, () => [
      Math.random() * 100,
      Math.random() * 100,
      Math.random() * 50 + 10
    ]);

    return {
      xAxis: { type: 'value', scale: true },
      yAxis: { type: 'value', scale: true },
      series: [{
        type: 'scatter',
        symbolSize: (data: number[]) => data[2],
        data,
        itemStyle: {
          color: 'rgba(24, 144, 255, 0.8)'
        }
      }]
    };
  };

  const generateHeatMapData = () => {
    const hours = ['12a', '1a', '2a', '3a', '4a', '5a', '6a', '7a', '8a', '9a', '10a', '11a',
                   '12p', '1p', '2p', '3p', '4p', '5p', '6p', '7p', '8p', '9p', '10p', '11p'];
    const days = ['周六', '周五', '周四', '周三', '周二', '周一', '周日'];

    const data = [];
    for (let i = 0; i < days.length; i++) {
      for (let j = 0; j < hours.length; j++) {
        data.push({
          x: j,
          y: i,
          value: Math.floor(Math.random() * 300)
        });
      }
    }

    return data;
  };

  const generateTreeMapData = () => {
    return [
      {
        name: '技术部',
        value: 40,
        children: [
          { name: '前端', value: 15 },
          { name: '后端', value: 20 },
          { name: '移动端', value: 5 }
        ]
      },
      {
        name: '产品部',
        value: 30,
        children: [
          { name: '产品经理', value: 20 },
          { name: 'UI设计', value: 10 }
        ]
      },
      {
        name: '运营部',
        value: 20,
        children: [
          { name: '市场', value: 12 },
          { name: '推广', value: 8 }
        ]
      },
      {
        name: '财务部',
        value: 10
      }
    ];
  };

  // Multi-chart configuration
  const multiChartConfigs: ChartConfig[] = [
    {
      id: 'line-chart',
      title: '趋势分析',
      option: generateLineData(),
      span: 12,
      order: 0,
      visible: true,
      height: 300,
      refreshable: true,
      exportable: true
    },
    {
      id: 'bar-chart',
      title: '产品销量',
      option: generateBarData(),
      span: 12,
      order: 1,
      visible: true,
      height: 300,
      refreshable: true,
      exportable: true
    },
    {
      id: 'pie-chart',
      title: '流量来源',
      option: generatePieData(),
      span: 8,
      order: 2,
      visible: true,
      height: 350,
      refreshable: true,
      exportable: true
    },
    {
      id: 'scatter-chart',
      title: '数据分布',
      option: generateScatterData(),
      span: 16,
      order: 3,
      visible: true,
      height: 350,
      refreshable: true,
      exportable: true
    }
  ];

  const [multiCharts, setMultiCharts] = useState<ChartConfig[]>(multiChartConfigs);

  // Refresh chart data
  const refreshChartData = () => {
    const newCharts = multiCharts.map(chart => ({
      ...chart,
      option: (() => {
        switch (chart.id) {
          case 'line-chart':
            return generateLineData();
          case 'bar-chart':
            return generateBarData();
          case 'pie-chart':
            return generatePieData();
          case 'scatter-chart':
            return generateScatterData();
          default:
            return chart.option;
        }
      })()
    }));
    setMultiCharts(newCharts);
  };

  // Auto refresh effect
  useEffect(() => {
    if (!autoRefresh) return;

    const interval = setInterval(refreshChartData, 5000);
    return () => clearInterval(interval);
  }, [autoRefresh, multiCharts]);

  return (
    <div className={className}>
      <Card>
        <Title level={2}>图表可视化组件库展示</Title>
        <Paragraph>
          这是一个基于 ECharts 的综合图表组件库，支持多种图表类型、实时数据更新、主题切换等功能。
        </Paragraph>

        {/* Controls */}
        <Row gutter={16} className="mb-6">
          <Col>
            <Space>
              <span>主题：</span>
              <Select
                value={theme}
                onChange={setTheme}
                options={[
                  { label: '浅色', value: 'light' },
                  { label: '深色', value: 'dark' }
                ]}
              />
            </Space>
          </Col>
          <Col>
            <Space>
              <span>动画：</span>
              <Switch checked={animated} onChange={setAnimated} />
            </Space>
          </Col>
          <Col>
            <Space>
              <span>自动刷新：</span>
              <Switch checked={autoRefresh} onChange={setAutoRefresh} />
            </Space>
          </Col>
          <Col>
            <Button
              icon={<ReloadOutlined />}
              onClick={refreshChartData}
            >
              刷新数据
            </Button>
          </Col>
        </Row>

        <Tabs defaultActiveKey="basic">
          <TabPane tab="基础图表" key="basic">
            <Row gutter={[16, 16]}>
              <Col span={12}>
                <ChartWrapper
                  ref={chartRef}
                  title="基础折线图"
                  option={generateLineData()}
                  theme={theme}
                  height={300}
                  animation={animated}
                  exportable
                  refreshable
                  onRefresh={() => {
                    const chart = chartRef.current;
                    if (chart) {
                      chart.setOption(generateLineData());
                    }
                  }}
                />
              </Col>
              <Col span={12}>
                <ChartWrapper
                  title="基础柱状图"
                  option={generateBarData()}
                  theme={theme}
                  height={300}
                  animation={animated}
                  exportable
                  refreshable
                />
              </Col>
              <Col span={12}>
                <ChartWrapper
                  title="饼图"
                  option={generatePieData()}
                  theme={theme}
                  height={300}
                  animation={animated}
                  exportable
                  refreshable
                />
              </Col>
              <Col span={12}>
                <ChartWrapper
                  title="散点图"
                  option={generateScatterData()}
                  theme={theme}
                  height={300}
                  animation={animated}
                  exportable
                  refreshable
                />
              </Col>
            </Row>
          </TabPane>

          <TabPane tab="高级图表" key="advanced">
            <Row gutter={[16, 16]}>
              <Col span={24}>
                <HeatMapChart
                  title="热力图 - 用户活跃度"
                  data={generateHeatMapData()}
                  xAxisData={['12a', '1a', '2a', '3a', '4a', '5a', '6a', '7a', '8a', '9a', '10a', '11a',
                             '12p', '1p', '2p', '3p', '4p', '5p', '6p', '7p', '8p', '9p', '10p', '11p']}
                  yAxisData={['周六', '周五', '周四', '周三', '周二', '周一', '周日']}
                  theme={theme}
                  height={300}
                  colorScale="viridis"
                  showLabel={false}
                  exportable
                />
              </Col>
              <Col span={24}>
                <TreeMapChart
                  title="树图 - 组织架构"
                  data={generateTreeMapData()}
                  theme={theme}
                  height={400}
                  roam="move"
                  nodeClick="zoomToNode"
                  exportable
                />
              </Col>
            </Row>
          </TabPane>

          <TabPane tab="实时图表" key="realtime">
            <Row gutter={[16, 16]}>
              <Col span={12}>
                <RealTimeChart
                  title="实时数据 - 折线图"
                  type="line"
                  height={350}
                  theme={theme}
                  maxDataPoints={50}
                  updateInterval={2000}
                  autoStart
                  animated={animated}
                  smooth
                  showMetrics
                  showStatus
                  thresholds={{
                    warning: 60,
                    critical: 80,
                    colors: {
                      normal: '#52c41a',
                      warning: '#faad14',
                      critical: '#f5222d'
                    }
                  }}
                />
              </Col>
              <Col span={12}>
                <RealTimeChart
                  title="实时数据 - 仪表盘"
                  type="gauge"
                  height={350}
                  theme={theme}
                  maxDataPoints={1}
                  updateInterval={1500}
                  autoStart
                  animated={animated}
                  showMetrics={false}
                  showStatus
                  thresholds={{
                    warning: 70,
                    critical: 90,
                    colors: {
                      normal: '#52c41a',
                      warning: '#faad14',
                      critical: '#f5222d'
                    }
                  }}
                />
              </Col>
            </Row>
          </TabPane>

          <TabPane tab="多图表仪表盘" key="dashboard">
            <MultiChart
              charts={multiCharts}
              onChartsChange={setMultiCharts}
              theme={theme}
              animated={animated}
              editable
              refreshable
              exportable
              onRefreshAll={refreshChartData}
            />
          </TabPane>

          <TabPane tab="模板示例" key="templates">
            <Row gutter={[16, 16]}>
              {Object.entries(CHART_TEMPLATES).map(([key, template]) => (
                <Col span={12} key={key}>
                  <ChartWrapper
                    title={template.title?.text || key}
                    option={template}
                    theme={theme}
                    height={300}
                    animation={animated}
                    exportable
                  />
                </Col>
              ))}
            </Row>
          </TabPane>
        </Tabs>

        <Divider />

        <Title level={3}>使用说明</Title>
        <Paragraph>
          <ul>
            <li><strong>ChartWrapper</strong>: 通用图表包装器组件，支持所有 ECharts 图表类型</li>
            <li><strong>MultiChart</strong>: 多图表仪表盘组件，支持拖拽排序和配置编辑</li>
            <li><strong>RealTimeChart</strong>: 实时数据图表组件，支持 WebSocket 和轮询数据更新</li>
            <li><strong>HeatMapChart</strong>: 热力图组件，支持多种颜色方案</li>
            <li><strong>TreeMapChart</strong>: 树图组件，支持层级数据展示</li>
            <li><strong>ChartUtils</strong>: 图表工具类，提供颜色生成、数据处理等功能</li>
          </ul>
        </Paragraph>

        <Paragraph>
          所有组件都支持主题切换、动画效果、数据导出、响应式设计等特性。
        </Paragraph>
      </Card>
    </div>
  );
};

export default ChartShowcase;
// Export all chart components
export { default as StatCard, StatCardVariants, StatCardFormatters } from './StatCard';
export type { StatCardProps } from './StatCard';

export { default as LineChart, LineChartPresets } from './LineChart';
export type { LineChartProps, LineChartData } from './LineChart';

export { default as BarChart } from './BarChart';
export type { BarChartProps, BarChartData } from './BarChart';

export { default as PieChart } from './PieChart';
export type { PieChartProps, PieChartData } from './PieChart';

// New comprehensive chart components
export { default as ChartWrapper } from './ChartWrapper';
export type { ChartWrapperProps, ChartWrapperRef } from './ChartWrapper';

export { default as MultiChart } from './MultiChart';
export type { MultiChartProps, ChartConfig } from './MultiChart';

export { default as RealTimeChart } from './RealTimeChart';
export type { RealTimeChartProps, RealTimeDataPoint } from './RealTimeChart';

export { default as HeatMapChart } from './HeatMapChart';
export type { HeatMapChartProps, HeatMapData } from './HeatMapChart';

export { default as TreeMapChart } from './TreeMapChart';
export type { TreeMapChartProps, TreeMapData } from './TreeMapChart';

export { default as ChartShowcase } from './ChartShowcase';

// Chart utilities and helpers
export { default as ChartUtils, COLOR_PALETTES, CHART_THEMES, CHART_CONFIGS, CHART_TEMPLATES } from '../../utils/chartUtils';

// Common chart data types
export interface ChartDataset {
  label: string;
  data: number[];
  backgroundColor?: string | string[];
  borderColor?: string | string[];
  borderWidth?: number;
}

// Common chart utilities and types
export interface ChartColors {
  primary: string;
  secondary: string;
  success: string;
  warning: string;
  error: string;
  info: string;
}

export const defaultChartColors: ChartColors = {
  primary: '#1890ff',
  secondary: '#722ed1',
  success: '#52c41a',
  warning: '#faad14',
  error: '#f5222d',
  info: '#13c2c2',
};

export const chartColorPalettes = {
  default: [
    '#1890ff',
    '#52c41a',
    '#faad14',
    '#f5222d',
    '#722ed1',
    '#fa8c16',
    '#13c2c2',
    '#eb2f96',
  ],
  business: [
    '#2f54eb',
    '#722ed1',
    '#13c2c2',
    '#52c41a',
    '#faad14',
    '#fa8c16',
    '#f5222d',
    '#eb2f96',
  ],
  tech: [
    '#1890ff',
    '#13c2c2',
    '#52c41a',
    '#722ed1',
    '#faad14',
    '#fa8c16',
    '#f5222d',
    '#eb2f96',
  ],
  pastel: [
    '#91d5ff',
    '#b7eb8f',
    '#ffe7ba',
    '#ffadd2',
    '#d3adf7',
    '#ffd591',
    '#ff9c6e',
    '#ffc069',
  ],
};

// Chart theme configurations
export const chartThemes = {
  light: {
    backgroundColor: '#ffffff',
    textColor: '#666666',
    gridColor: '#f0f0f0',
    axisColor: '#d9d9d9',
  },
  dark: {
    backgroundColor: '#001529',
    textColor: '#cccccc',
    gridColor: '#333333',
    axisColor: '#555555',
  },
};

// Common chart options
export const commonChartOptions = {
  grid: {
    left: '3%',
    right: '4%',
    bottom: '3%',
    containLabel: true,
  },
  tooltip: {
    trigger: 'axis',
    axisPointer: {
      type: 'cross',
    },
  },
  legend: {
    top: 10,
    left: 'center',
  },
};

// Utility functions
export const formatChartData = {
  // Convert raw data to chart format
  toLineChart: (data: Record<string, number[]>, labels: string[]): { labels: string[]; datasets: Array<{ label: string; data: number[]; borderColor: string; backgroundColor: string }> } => ({
    labels,
    datasets: Object.entries(data).map(([key, values], index) => ({
      label: key,
      data: values,
      borderColor: chartColorPalettes.default[index % chartColorPalettes.default.length],
      backgroundColor: `${chartColorPalettes.default[index % chartColorPalettes.default.length]}20`,
    })),
  }),

  toBarChart: (data: Record<string, number[]>, labels: string[]): { labels: string[]; datasets: Array<{ label: string; data: number[]; backgroundColor: string }> } => ({
    labels,
    datasets: Object.entries(data).map(([key, values], index) => ({
      label: key,
      data: values,
      backgroundColor: chartColorPalettes.default[index % chartColorPalettes.default.length],
    })),
  }),

  toPieChart: (data: Record<string, number>): { labels: string[]; datasets: Array<{ data: number[]; backgroundColor: string[] }> } => ({
    labels: Object.keys(data),
    datasets: [{
      data: Object.values(data),
      backgroundColor: chartColorPalettes.default,
    }],
  }),
};

// Chart responsive configurations
export const responsiveBreakpoints = {
  xs: 480,
  sm: 576,
  md: 768,
  lg: 992,
  xl: 1200,
  xxl: 1600,
};

export const getResponsiveHeight = (breakpoint: keyof typeof responsiveBreakpoints): number => {
  const heights = {
    xs: 200,
    sm: 250,
    md: 300,
    lg: 350,
    xl: 400,
    xxl: 450,
  };
  return heights[breakpoint];
};

// Export utility for chart downloads
export const downloadChart = (
  chartRef: React.RefObject<{ saveAsImage?: (options: { name?: string; type?: string }) => void; getDataURL?: () => string }>,
  filename: string,
  format: 'png' | 'jpg' | 'svg' = 'png'
) => {
  if (!chartRef.current) return;

  const chartInstance = chartRef.current.getEchartsInstance();
  if (!chartInstance) return;

  const canvas = chartInstance.getDataURL({
    type: format === 'jpg' ? 'jpeg' : format,
    pixelRatio: 2,
    backgroundColor: '#ffffff',
  });

  const link = document.createElement('a');
  link.download = `${filename}.${format}`;
  link.href = canvas;
  link.click();
};
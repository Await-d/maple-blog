import * as echarts from 'echarts';

// RGB Color interface
interface RGBColor {
  r: number;
  g: number;
  b: number;
}

interface ChartTooltipParams {
  seriesName: string;
  data: number[];
}

interface HeatmapTooltipParams {
  data: [string, string, number];
}

interface ChartOption {
  [key: string]: unknown;
  grid?: {
    left?: string;
    right?: string;
    bottom?: string;
    top?: string;
    height?: string;
    containLabel?: boolean;
    backgroundColor?: string;
    borderColor?: string;
  };
  legend?: {
    textStyle?: { color: string };
    type?: string;
    orient?: string;
    bottom?: number | string;
    left?: string;
    data?: string[];
  };
  tooltip?: {
    trigger?: string;
    backgroundColor?: string;
    borderColor?: string;
    textStyle?: { color: string };
    axisPointer?: {
      type?: string;
      label?: { backgroundColor: string };
    };
    position?: string;
    formatter?: string | ((params: ChartTooltipParams | HeatmapTooltipParams) => string);
  };
  title?: {
    text?: string;
    left?: string;
    textStyle?: { color: string };
    subtextStyle?: { color: string };
  };
  xAxis?: {
    type?: string;
    data?: string[];
    axisLine?: { lineStyle: { color: string } };
    axisLabel?: { color: string };
    splitLine?: { lineStyle: { color: string } };
  };
  yAxis?: {
    type?: string;
    axisLine?: { lineStyle: { color: string } };
    axisLabel?: { color: string };
    splitLine?: { lineStyle: { color: string } };
  };
  series?: Array<{
    name?: string;
    type?: string;
    data?: number[] | Array<{ value: number; name: string }>;
    radius?: string;
  }>;
  animation?: boolean;
  animationType?: string;
  animationEasing?: string;
  animationDuration?: number;
  animationDelay?: (idx: number) => number;
  toolbox?: {
    feature?: {
      saveAsImage?: { show: boolean };
      dataZoom?: { show: boolean };
      restore?: { show: boolean };
    };
  };
}

interface TimeSeriesDataPoint {
  date: string;
  value: number;
  timestamp: number;
}

// Color palettes for different themes
export const COLOR_PALETTES = {
  default: ['#1890ff', '#52c41a', '#faad14', '#f5222d', '#722ed1', '#fa8c16', '#13c2c2', '#eb2f96'],
  business: ['#0050b3', '#096dd9', '#1890ff', '#40a9ff', '#69c0ff', '#91d5ff', '#bae7ff', '#e6f7ff'],
  warm: ['#fa541c', '#fa8c16', '#faad14', '#fadb14', '#a0d911', '#52c41a', '#13c2c2', '#1890ff'],
  cool: ['#722ed1', '#9254de', '#b37feb', '#d3adf7', '#efdbff', '#f9f0ff', '#1890ff', '#40a9ff'],
  monochrome: ['#262626', '#434343', '#595959', '#8c8c8c', '#bfbfbf', '#d9d9d9', '#f0f0f0', '#fafafa'],
  vibrant: ['#ff4d4f', '#ff7a45', '#ffa940', '#ffec3d', '#bae637', '#73d13d', '#36cfc9', '#40a9ff']
};

// Chart themes configuration
export const CHART_THEMES = {
  light: {
    backgroundColor: 'transparent',
    textStyle: {
      color: '#333',
      fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif'
    },
    grid: {
      backgroundColor: 'transparent',
      borderColor: '#f0f0f0'
    },
    tooltip: {
      backgroundColor: 'rgba(255, 255, 255, 0.95)',
      borderColor: '#d9d9d9',
      textStyle: { color: '#333' }
    },
    legend: {
      textStyle: { color: '#333' }
    },
    categoryAxis: {
      axisLine: { lineStyle: { color: '#d9d9d9' } },
      axisLabel: { color: '#666' },
      splitLine: { lineStyle: { color: '#f0f0f0' } }
    },
    valueAxis: {
      axisLine: { lineStyle: { color: '#d9d9d9' } },
      axisLabel: { color: '#666' },
      splitLine: { lineStyle: { color: '#f0f0f0' } }
    }
  },
  dark: {
    backgroundColor: 'transparent',
    textStyle: {
      color: '#fff',
      fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif'
    },
    grid: {
      backgroundColor: 'transparent',
      borderColor: '#434343'
    },
    tooltip: {
      backgroundColor: 'rgba(0, 0, 0, 0.85)',
      borderColor: '#434343',
      textStyle: { color: '#fff' }
    },
    legend: {
      textStyle: { color: '#fff' }
    },
    categoryAxis: {
      axisLine: { lineStyle: { color: '#434343' } },
      axisLabel: { color: '#bfbfbf' },
      splitLine: { lineStyle: { color: '#303030' } }
    },
    valueAxis: {
      axisLine: { lineStyle: { color: '#434343' } },
      axisLabel: { color: '#bfbfbf' },
      splitLine: { lineStyle: { color: '#303030' } }
    }
  }
};

// Common chart configurations
export const CHART_CONFIGS = {
  line: {
    animation: true,
    animationDuration: 1000,
    animationEasing: 'cubicOut',
    grid: {
      left: '3%',
      right: '4%',
      bottom: '3%',
      top: '10%',
      containLabel: true
    },
    tooltip: {
      trigger: 'axis',
      axisPointer: {
        type: 'cross',
        label: { backgroundColor: '#6a7985' }
      }
    },
    toolbox: {
      feature: {
        saveAsImage: { show: true },
        dataZoom: { show: true },
        restore: { show: true }
      }
    }
  },
  bar: {
    animation: true,
    animationDuration: 1000,
    animationDelay: (idx: number) => idx * 50,
    grid: {
      left: '3%',
      right: '4%',
      bottom: '3%',
      top: '10%',
      containLabel: true
    },
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'shadow' }
    }
  },
  pie: {
    animation: true,
    animationType: 'scale',
    animationEasing: 'elasticOut',
    tooltip: {
      trigger: 'item',
      formatter: '{a} <br/>{b}: {c} ({d}%)'
    },
    legend: {
      type: 'scroll',
      bottom: 10,
      data: []
    }
  },
  scatter: {
    animation: true,
    animationDuration: 1000,
    grid: {
      left: '3%',
      right: '7%',
      bottom: '3%',
      top: '10%',
      containLabel: true
    },
    tooltip: {
      trigger: 'item',
      formatter: (params: ChartTooltipParams) => {
        return `${params.seriesName}<br/>${params.data[0]} : ${params.data[1]}`;
      }
    }
  },
  heatmap: {
    animation: true,
    tooltip: {
      position: 'top',
      formatter: (params: HeatmapTooltipParams) => {
        return `${params.data[0]} - ${params.data[1]}: ${params.data[2]}`;
      }
    },
    grid: {
      height: '50%',
      top: '10%'
    }
  }
};

// Utility functions
export class ChartUtils {
  /**
   * Generate color palette based on count
   */
  static generateColors(count: number, palette: keyof typeof COLOR_PALETTES = 'default'): string[] {
    const colors = COLOR_PALETTES[palette];
    if (count <= colors.length) {
      return colors.slice(0, count);
    }

    // Generate additional colors by interpolating
    const result = [...colors];
    while (result.length < count) {
      const baseIndex = result.length % colors.length;
      const baseColor = colors[baseIndex];
      const variation = Math.floor((result.length - colors.length) / colors.length) + 1;
      const newColor = this.adjustColorBrightness(baseColor, -0.1 * variation);
      result.push(newColor);
    }

    return result;
  }

  /**
   * Adjust color brightness
   */
  static adjustColorBrightness(color: string, amount: number): string {
    const usePound = color[0] === '#';
    const col = usePound ? color.slice(1) : color;

    const num = parseInt(col, 16);
    let r = (num >> 16) + amount * 255;
    let g = (num >> 8 & 0x00FF) + amount * 255;
    let b = (num & 0x0000FF) + amount * 255;

    r = Math.max(Math.min(255, r), 0);
    g = Math.max(Math.min(255, g), 0);
    b = Math.max(Math.min(255, b), 0);

    return (usePound ? '#' : '') + (0x1000000 + r * 0x10000 + g * 0x100 + b).toString(16).slice(1);
  }

  /**
   * Convert RGB to Hex
   */
  static rgbToHex(rgb: RGBColor): string {
    const r = Math.round(rgb.r);
    const g = Math.round(rgb.g);
    const b = Math.round(rgb.b);
    return `#${((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1)}`;
  }

  /**
   * Generate gradient colors
   */
  static generateGradient(startColor: string, endColor: string, steps: number): string[] {
    const start = this.hexToRgb(startColor);
    const end = this.hexToRgb(endColor);

    if (!start || !end) return [startColor, endColor];

    const colors = [];
    for (let i = 0; i < steps; i++) {
      const ratio = i / (steps - 1);
      const r = Math.round(start.r + (end.r - start.r) * ratio);
      const g = Math.round(start.g + (end.g - start.g) * ratio);
      const b = Math.round(start.b + (end.b - start.b) * ratio);
      colors.push(this.rgbToHex({ r, g, b }));
    }

    return colors;
  }

  /**
   * Convert hex to RGB
   */
  static hexToRgb(hex: string): { r: number; g: number; b: number } | null {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    return result ? {
      r: parseInt(result[1], 16),
      g: parseInt(result[2], 16),
      b: parseInt(result[3], 16)
    } : null;
  }

  /**
   * Format number with appropriate units
   */
  static formatNumber(value: number, precision: number = 2): string {
    if (value >= 1000000000) {
      return (value / 1000000000).toFixed(precision) + 'B';
    } else if (value >= 1000000) {
      return (value / 1000000).toFixed(precision) + 'M';
    } else if (value >= 1000) {
      return (value / 1000).toFixed(precision) + 'K';
    }
    return value.toFixed(precision);
  }

  /**
   * Format percentage
   */
  static formatPercentage(value: number, total: number, precision: number = 1): string {
    const percentage = (value / total) * 100;
    return percentage.toFixed(precision) + '%';
  }

  /**
   * Generate time series data
   */
  static generateTimeSeriesData(
    startDate: Date,
    endDate: Date,
    interval: 'hour' | 'day' | 'week' | 'month',
    valueGenerator: (date: Date, index: number) => number
  ): TimeSeriesDataPoint[] {
    const data: TimeSeriesDataPoint[] = [];
    const current = new Date(startDate);
    let index = 0;

    while (current <= endDate) {
      data.push({
        date: current.toISOString(),
        value: valueGenerator(current, index),
        timestamp: current.getTime()
      });

      switch (interval) {
        case 'hour':
          current.setHours(current.getHours() + 1);
          break;
        case 'day':
          current.setDate(current.getDate() + 1);
          break;
        case 'week':
          current.setDate(current.getDate() + 7);
          break;
        case 'month':
          current.setMonth(current.getMonth() + 1);
          break;
      }
      index++;
    }

    return data;
  }

  /**
   * Calculate moving average
   */
  static calculateMovingAverage(data: number[], window: number): number[] {
    if (window <= 0 || window > data.length) return data;

    const result = [];
    for (let i = 0; i < data.length; i++) {
      const start = Math.max(0, i - window + 1);
      const end = i + 1;
      const slice = data.slice(start, end);
      const average = slice.reduce((a, b) => a + b, 0) / slice.length;
      result.push(average);
    }

    return result;
  }

  /**
   * Detect trend in data
   */
  static detectTrend(data: number[]): 'up' | 'down' | 'stable' {
    if (data.length < 2) return 'stable';

    const recent = data.slice(-Math.min(5, data.length));
    const start = recent[0];
    const end = recent[recent.length - 1];
    const change = (end - start) / start;

    if (change > 0.05) return 'up';
    if (change < -0.05) return 'down';
    return 'stable';
  }

  /**
   * Create responsive chart option
   */
  static createResponsiveOption(baseOption: ChartOption, containerWidth: number): ChartOption {
    const option = { ...baseOption };

    // Adjust based on container width
    if (containerWidth < 576) {
      // Mobile
      option.grid = {
        ...option.grid,
        left: '5%',
        right: '5%',
        top: '15%',
        bottom: '15%'
      };
      option.legend = {
        ...option.legend,
        type: 'scroll',
        orient: 'horizontal',
        bottom: 0
      };
    } else if (containerWidth < 768) {
      // Tablet
      option.grid = {
        ...option.grid,
        left: '4%',
        right: '4%',
        top: '12%',
        bottom: '10%'
      };
    }

    return option;
  }

  /**
   * Export chart as image
   */
  static async exportChart(
    chart: echarts.ECharts,
    filename: string = 'chart',
    format: 'png' | 'svg' = 'png',
    options?: {
      pixelRatio?: number;
      backgroundColor?: string;
      excludeComponents?: string[];
    }
  ): Promise<void> {
    try {
      let dataURL: string;

      if (format === 'svg') {
        dataURL = chart.renderToSVGString();
        const blob = new Blob([dataURL], { type: 'image/svg+xml' });
        const url = URL.createObjectURL(blob);
        this.downloadFile(url, `${filename}.svg`);
        URL.revokeObjectURL(url);
      } else {
        dataURL = chart.getDataURL({
          type: format,
          pixelRatio: options?.pixelRatio || 2,
          backgroundColor: options?.backgroundColor || '#fff',
          ...(options?.excludeComponents && { excludeComponents: options.excludeComponents })
        });
        this.downloadFile(dataURL, `${filename}.${format}`);
      }
    } catch (error) {
      console.error('Export failed:', error);
      throw error;
    }
  }

  /**
   * Download file from data URL
   */
  private static downloadFile(dataURL: string, filename: string): void {
    const link = document.createElement('a');
    link.href = dataURL;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  /**
   * Merge chart options deeply
   */
  static mergeOptions(target: ChartOption, ...sources: ChartOption[]): ChartOption {
    if (!sources.length) return target;
    const source = sources.shift();
    
    if (!source) return target;

    if (this.isObject(target) && this.isObject(source)) {
      for (const key in source) {
        if (this.isObject(source[key])) {
          if (!target[key]) Object.assign(target, { [key]: {} });
          this.mergeOptions(target[key] as ChartOption, source[key] as ChartOption);
        } else {
          Object.assign(target, { [key]: source[key] });
        }
      }
    }

    return this.mergeOptions(target, ...sources);
  }

  /**
   * Check if value is object
   */
  private static isObject(item: unknown): item is Record<string, unknown> {
    return item !== null && typeof item === 'object' && !Array.isArray(item);
  }

  /**
   * Generate chart theme based on brand colors
   */
  static generateCustomTheme(primaryColor: string, name: string = 'custom'): void {
    const colors = this.generateColors(8, 'default');
    colors[0] = primaryColor;

    const theme = {
      color: colors,
      backgroundColor: 'transparent',
      textStyle: {},
      title: {
        textStyle: { color: primaryColor },
        subtextStyle: { color: '#999' }
      },
      line: {
        itemStyle: { borderWidth: 1 },
        lineStyle: { width: 2 },
        symbolSize: 4,
        symbol: 'emptyCircle',
        smooth: false
      },
      radar: {
        itemStyle: { borderWidth: 1 },
        lineStyle: { width: 2 },
        symbolSize: 4,
        symbol: 'emptyCircle',
        smooth: false
      },
      bar: {
        itemStyle: {
          barBorderWidth: 0,
          barBorderColor: '#ccc'
        }
      },
      pie: {
        itemStyle: {
          borderWidth: 0,
          borderColor: '#ccc'
        }
      },
      scatter: {
        itemStyle: {
          borderWidth: 0,
          borderColor: '#ccc'
        }
      },
      boxplot: {
        itemStyle: {
          borderWidth: 0,
          borderColor: '#ccc'
        }
      },
      parallel: {
        itemStyle: {
          borderWidth: 0,
          borderColor: '#ccc'
        }
      },
      sankey: {
        itemStyle: {
          borderWidth: 0,
          borderColor: '#ccc'
        }
      },
      funnel: {
        itemStyle: {
          borderWidth: 0,
          borderColor: '#ccc'
        }
      },
      gauge: {
        itemStyle: {
          borderWidth: 0,
          borderColor: '#ccc'
        }
      }
    };

    echarts.registerTheme(name, theme);
  }
}

// Pre-defined chart templates
export const CHART_TEMPLATES = {
  basicLine: {
    title: { text: '基础折线图' },
    xAxis: { type: 'category', data: ['周一', '周二', '周三', '周四', '周五', '周六', '周日'] },
    yAxis: { type: 'value' },
    series: [{ data: [150, 230, 224, 218, 135, 147, 260], type: 'line' }]
  },

  basicBar: {
    title: { text: '基础柱状图' },
    xAxis: { type: 'category', data: ['周一', '周二', '周三', '周四', '周五', '周六', '周日'] },
    yAxis: { type: 'value' },
    series: [{ data: [120, 200, 150, 80, 70, 110, 130], type: 'bar' }]
  },

  basicPie: {
    title: { text: '基础饼图', left: 'center' },
    tooltip: { trigger: 'item' },
    legend: { orient: 'vertical', left: 'left' },
    series: [{
      name: '访问来源',
      type: 'pie',
      radius: '50%',
      data: [
        { value: 1048, name: '搜索引擎' },
        { value: 735, name: '直接访问' },
        { value: 580, name: '邮件营销' },
        { value: 484, name: '联盟广告' },
        { value: 300, name: '视频广告' }
      ]
    }]
  },

  gaugeChart: {
    title: { text: '仪表盘图' },
    series: [{
      name: 'Pressure',
      type: 'gauge',
      detail: { formatter: '{value}' },
      data: [{ value: 50, name: 'SCORE' }]
    }]
  }
};

export default ChartUtils;
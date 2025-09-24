export interface ChartDataPoint {
  name: string;
  value: number | string;
  [key: string]: unknown;
}

export interface ChartData {
  labels?: string[];
  datasets?: ChartDataPoint[];
  categories?: string[];
  series?: ChartSeries[];
  [key: string]: unknown;
}

export interface TooltipParam {
  axisValue?: string;
  color: string;
  seriesName: string;
  value: number | string;
  name: string;
  [key: string]: unknown;
}

export interface ChartSeries {
  name: string;
  type?: string;
  data: (number | string | ChartDataPoint)[];
  [key: string]: unknown;
}

export interface ChartInstance {
  getEchartsInstance(): {
    getDataURL(options?: {
      type?: 'png' | 'svg';
      pixelRatio?: number;
      backgroundColor?: string;
      excludeComponents?: string[];
    }): string;
    saveAsImage(options?: {
      type?: 'png' | 'svg';
      name?: string;
    }): void;
    resize(options?: {
      width?: number | string;
      height?: number | string;
    }): void;
    setOption(option: unknown, notMerge?: boolean, lazyUpdate?: boolean): void;
  };
}
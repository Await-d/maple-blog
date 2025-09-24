import React, { useMemo } from 'react';
import ChartWrapper, { ChartWrapperProps } from './ChartWrapper';

export interface HeatMapData {
  x: string | number;
  y: string | number;
  value: number;
  label?: string;
}

export interface HeatMapChartProps extends Omit<ChartWrapperProps, 'option' | 'type'> {
  data: HeatMapData[];
  xAxisData?: string[] | number[];
  yAxisData?: string[] | number[];
  visualMap?: {
    min?: number;
    max?: number;
    calculable?: boolean;
    orient?: 'horizontal' | 'vertical';
    left?: string | number;
    bottom?: string | number;
    inRange?: {
      color?: string[];
    };
  };
  colorScale?: 'viridis' | 'plasma' | 'warm' | 'cool' | 'custom';
  customColors?: string[];
  showLabel?: boolean;
  cellBorderWidth?: number;
  cellBorderColor?: string;
}

const HeatMapChart: React.FC<HeatMapChartProps> = ({
  data,
  xAxisData,
  yAxisData,
  visualMap,
  colorScale = 'viridis',
  customColors,
  showLabel = false,
  cellBorderWidth = 1,
  cellBorderColor = '#fff',
  ...chartProps
}) => {
  const option = useMemo(() => {
    if (!data.length) return {};

    // Auto-generate axis data if not provided
    const autoXAxisData = xAxisData || [...new Set(data.map(d => d.x))].sort();
    const autoYAxisData = yAxisData || [...new Set(data.map(d => d.y))].sort();

    // Transform data for ECharts heatmap format
    const seriesData = data.map(item => [
      autoXAxisData.indexOf(item.x),
      autoYAxisData.indexOf(item.y),
      item.value,
      item.label || item.value
    ]);

    // Calculate min/max values for visual map
    const values = data.map(d => d.value);
    const minValue = Math.min(...values);
    const maxValue = Math.max(...values);

    // Define color scales
    const colorScales = {
      viridis: ['#440154', '#482777', '#3f4a8a', '#31678e', '#26838f', '#1f9d8a', '#6cce5a', '#b6de2b', '#fee825'],
      plasma: ['#0c0786', '#40039c', '#6a00a7', '#8f0da4', '#b12a90', '#cc4678', '#e16462', '#f2844b', '#fca636', '#fcce25'],
      warm: ['#800080', '#a0146e', '#c0285c', '#e03c4a', '#ff5038', '#ff6426', '#ff7814', '#ff8c02', '#ffa000'],
      cool: ['#000080', '#0d47a1', '#1976d2', '#1e88e5', '#2196f3', '#42a5f5', '#64b5f6', '#90caf9', '#bbdefb']
    };

    let colors = customColors;
    if (!colors) {
      colors = colorScales[colorScale as keyof typeof colorScales] || colorScales.viridis;
    }

    return {
      grid: {
        height: '50%',
        top: '10%'
      },
      xAxis: {
        type: 'category',
        data: autoXAxisData,
        splitArea: {
          show: true
        },
        axisLabel: {
          interval: 0,
          rotate: autoXAxisData.length > 10 ? 45 : 0
        }
      },
      yAxis: {
        type: 'category',
        data: autoYAxisData,
        splitArea: {
          show: true
        }
      },
      visualMap: {
        min: visualMap?.min ?? minValue,
        max: visualMap?.max ?? maxValue,
        calculable: visualMap?.calculable ?? true,
        orient: visualMap?.orient ?? 'horizontal',
        left: visualMap?.left ?? 'center',
        bottom: visualMap?.bottom ?? '15%',
        inRange: {
          color: colors,
          ...visualMap?.inRange
        }
      },
      series: [{
        name: '热力图',
        type: 'heatmap',
        data: seriesData,
        label: {
          show: showLabel,
          color: '#000'
        },
        itemStyle: {
          borderWidth: cellBorderWidth,
          borderColor: cellBorderColor
        },
        emphasis: {
          itemStyle: {
            shadowBlur: 10,
            shadowColor: 'rgba(0, 0, 0, 0.5)'
          }
        }
      }],
      tooltip: {
        position: 'top',
        formatter: (params: { data: [number, number, number, string?] }) => {
          const [xIndex, yIndex, value, label] = params.data;
          const xLabel = autoXAxisData[xIndex];
          const yLabel = autoYAxisData[yIndex];
          return `${yLabel} - ${xLabel}<br/>值: ${label || value}`;
        }
      }
    };
  }, [data, xAxisData, yAxisData, visualMap, colorScale, customColors, showLabel, cellBorderWidth, cellBorderColor]);

  return (
    <ChartWrapper
      {...chartProps}
      option={option}
      type="heatmap"
    />
  );
};

export default HeatMapChart;
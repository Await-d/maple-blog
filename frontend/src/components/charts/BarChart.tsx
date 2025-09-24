/**
 * BarChart - A lightweight, responsive bar chart component using SVG
 */

import React, { useMemo } from 'react';
import { ChartDataPoint } from '@/types/analytics';

interface BarChartProps {
  data: ChartDataPoint[];
  width?: number;
  height?: number;
  className?: string;
  orientation?: 'vertical' | 'horizontal';
  showValues?: boolean;
  showGrid?: boolean;
  color?: string;
  colors?: string[];
}

export const BarChart: React.FC<BarChartProps> = ({
  data,
  width = 600,
  height = 300,
  className = '',
  orientation = 'vertical',
  showValues = true,
  showGrid = true,
  color = '#3B82F6',
  colors
}) => {
  const [hoveredBar, setHoveredBar] = React.useState<{
    x: number;
    y: number;
    data: ChartDataPoint;
  } | null>(null);

  const { bars, gridLines, maxValue } = useMemo(() => {
    if (!data.length) return { bars: [], gridLines: [], maxValue: 0 };

    const values = data.map(d => d.value);
    const maxValue = Math.max(...values);
    const padding = { top: 20, right: 20, bottom: 60, left: 60 };

    const chartWidth = width - padding.left - padding.right;
    const chartHeight = height - padding.top - padding.bottom;

    let bars: Array<{
      x: number;
      y: number;
      width: number;
      height: number;
      data: ChartDataPoint;
      color: string;
    }> = [];

    if (orientation === 'vertical') {
      const barWidth = chartWidth / data.length * 0.8;
      const barSpacing = chartWidth / data.length * 0.2;

      bars = data.map((item, i) => {
        const barHeight = (item.value / maxValue) * chartHeight;
        return {
          x: padding.left + i * (barWidth + barSpacing) + barSpacing / 2,
          y: padding.top + chartHeight - barHeight,
          width: barWidth,
          height: barHeight,
          data: item,
          color: colors ? colors[i % colors.length] : (item.color || color)
        };
      });
    } else {
      const barHeight = chartHeight / data.length * 0.8;
      const barSpacing = chartHeight / data.length * 0.2;

      bars = data.map((item, i) => {
        const barWidth = (item.value / maxValue) * chartWidth;
        return {
          x: padding.left,
          y: padding.top + i * (barHeight + barSpacing) + barSpacing / 2,
          width: barWidth,
          height: barHeight,
          data: item,
          color: colors ? colors[i % colors.length] : (item.color || color)
        };
      });
    }

    // Grid lines
    const gridLines = showGrid ? (
      orientation === 'vertical' 
        ? Array.from({ length: 5 }, (_, i) => {
            const y = padding.top + (chartHeight / 4) * i;
            const value = maxValue - (maxValue / 4) * i;
            return { 
              x1: padding.left, 
              y1: y, 
              x2: width - padding.right, 
              y2: y,
              label: Math.round(value).toLocaleString() 
            };
          })
        : Array.from({ length: 5 }, (_, i) => {
            const x = padding.left + (chartWidth / 4) * i;
            const value = (maxValue / 4) * i;
            return { 
              x1: x, 
              y1: padding.top, 
              x2: x, 
              y2: height - padding.bottom,
              label: Math.round(value).toLocaleString() 
            };
          })
    ) : [];

    return { bars, gridLines, maxValue };
  }, [data, width, height, orientation, showGrid, color, colors]);

  if (!data.length) {
    return (
      <div className={`flex items-center justify-center bg-gray-50 dark:bg-gray-800 rounded-lg ${className}`} 
           style={{ width, height }}>
        <p className="text-gray-500 dark:text-gray-400">No data available</p>
      </div>
    );
  }

  return (
    <div className={`relative ${className}`}>
      <svg width={width} height={height} className="overflow-visible">
        {/* Grid lines */}
        {showGrid && (
          <g className="opacity-30">
            {gridLines.map((line, i) => (
              <line
                key={`grid-${i}`}
                x1={line.x1}
                y1={line.y1}
                x2={line.x2}
                y2={line.y2}
                stroke="currentColor"
                strokeWidth="1"
                className="text-gray-300 dark:text-gray-600"
              />
            ))}
          </g>
        )}

        {/* Grid labels */}
        {showGrid && gridLines.map((line, i) => (
          <text
            key={`grid-label-${i}`}
            x={orientation === 'vertical' ? 55 : line.x1}
            y={orientation === 'vertical' ? line.y1 + 4 : height - 25}
            textAnchor={orientation === 'vertical' ? 'end' : 'middle'}
            className="text-xs fill-gray-600 dark:fill-gray-400"
          >
            {line.label}
          </text>
        ))}

        {/* Bars */}
        {bars.map((bar, i) => (
          <g key={`bar-${i}`}>
            <rect
              x={bar.x}
              y={bar.y}
              width={bar.width}
              height={bar.height}
              fill={bar.color}
              className="cursor-pointer hover:opacity-80 transition-opacity"
              onMouseEnter={() => setHoveredBar({
                x: bar.x + bar.width / 2,
                y: bar.y,
                data: bar.data
              })}
              onMouseLeave={() => setHoveredBar(null)}
            />
            
            {/* Value labels */}
            {showValues && (
              <text
                x={orientation === 'vertical' ? bar.x + bar.width / 2 : bar.x + bar.width + 5}
                y={orientation === 'vertical' ? bar.y - 5 : bar.y + bar.height / 2 + 4}
                textAnchor={orientation === 'vertical' ? 'middle' : 'start'}
                className="text-xs font-medium fill-gray-700 dark:fill-gray-300"
              >
                {bar.data.value.toLocaleString()}
              </text>
            )}
          </g>
        ))}

        {/* Category labels */}
        {data.map((item, i) => (
          <text
            key={`label-${i}`}
            x={orientation === 'vertical' ? bars[i].x + bars[i].width / 2 : 55}
            y={orientation === 'vertical' ? height - 25 : bars[i].y + bars[i].height / 2 + 4}
            textAnchor={orientation === 'vertical' ? 'middle' : 'end'}
            className="text-sm fill-gray-600 dark:fill-gray-400"
          >
            {item.name.length > 12 ? `${item.name.slice(0, 12)}...` : item.name}
          </text>
        ))}
      </svg>

      {/* Tooltip */}
      {hoveredBar && (
        <div
          className="absolute z-10 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg p-3 pointer-events-none"
          style={{
            left: Math.min(hoveredBar.x + 10, width - 150),
            top: Math.max(hoveredBar.y - 80, 10)
          }}
        >
          <p className="text-sm font-medium text-gray-900 dark:text-white">
            {hoveredBar.data.name}
          </p>
          <p className="text-lg font-semibold text-blue-600 dark:text-blue-400">
            {hoveredBar.data.value.toLocaleString()}
          </p>
          {hoveredBar.data.percentage && (
            <p className="text-xs text-gray-600 dark:text-gray-400">
              {hoveredBar.data.percentage.toFixed(1)}%
            </p>
          )}
        </div>
      )}
    </div>
  );
};
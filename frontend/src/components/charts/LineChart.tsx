/**
 * LineChart - A lightweight, responsive line chart component using SVG
 */

import React, { useMemo } from 'react';
import { LineChartData } from '@/types/analytics';

interface LineChartProps {
  data: LineChartData[];
  width?: number;
  height?: number;
  className?: string;
  showGrid?: boolean;
  showTooltip?: boolean;
  showLegend?: boolean;
  colors?: string[];
}

export const LineChart: React.FC<LineChartProps> = ({
  data,
  width = 600,
  height = 300,
  className = '',
  showGrid = true,
  showTooltip = true,
  showLegend = true,
  colors = ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6']
}) => {
  const [hoveredPoint, setHoveredPoint] = React.useState<{
    x: number;
    y: number;
    data: { name: string; value: number; date: string };
  } | null>(null);

  const { lines, gridLines } = useMemo(() => {
    if (!data.length) return { 
      xScale: [], 
      yScale: () => 0, 
      lines: [], 
      gridLines: { vertical: [], horizontal: [] }, 
      allDataPoints: [] 
    };

    // Get all data points for consistent scaling
    const allPoints = data.flatMap(series => series.data);
    const xValues = [...new Set(allPoints.map(p => p.x))].sort();
    const yValues = allPoints.map(p => p.y);
    
    const minY = Math.min(...yValues);
    const maxY = Math.max(...yValues);
    const yPadding = (maxY - minY) * 0.1 || 1;

    const padding = { top: 20, right: 20, bottom: 40, left: 60 };
    const chartWidth = width - padding.left - padding.right;
    const chartHeight = height - padding.top - padding.bottom;

    // Create scales
    const xStep = chartWidth / (xValues.length - 1 || 1);
    const xScale = xValues.map((_, i) => padding.left + i * xStep);
    const yScaleValue = (y: number) => 
      padding.top + chartHeight - ((y - minY + yPadding) / (maxY - minY + 2 * yPadding)) * chartHeight;

    // Generate lines
    const lines = data.map((series, seriesIndex) => {
      const points = series.data.map((point) => ({
        x: xScale[xValues.indexOf(point.x)],
        y: yScaleValue(point.y),
        data: { name: series.name, value: point.y, date: point.x }
      }));

      const pathData = points.reduce((path, point, i) => {
        return path + (i === 0 ? `M ${point.x} ${point.y}` : ` L ${point.x} ${point.y}`);
      }, '');

      return {
        ...series,
        pathData,
        points,
        color: series.color || colors[seriesIndex % colors.length]
      };
    });

    // Grid lines
    const gridLines = showGrid ? {
      vertical: xScale.map((x, i) => ({ x, label: xValues[i] })),
      horizontal: Array.from({ length: 5 }, (_, i) => {
        const y = padding.top + (chartHeight / 4) * i;
        const value = maxY + yPadding - ((y - padding.top) / chartHeight) * (maxY - minY + 2 * yPadding);
        return { y, label: Math.round(value).toLocaleString() };
      })
    } : { vertical: [], horizontal: [] };

    return { lines, gridLines };
  }, [data, width, height, showGrid, colors]);

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
            {gridLines.horizontal.map((line, i) => (
              <line
                key={`h-${i}`}
                x1={60}
                y1={line.y}
                x2={width - 20}
                y2={line.y}
                stroke="currentColor"
                strokeWidth="1"
                className="text-gray-300 dark:text-gray-600"
              />
            ))}
            {gridLines.vertical.map((line, i) => (
              <line
                key={`v-${i}`}
                x1={line.x}
                y1={20}
                x2={line.x}
                y2={height - 40}
                stroke="currentColor"
                strokeWidth="1"
                className="text-gray-300 dark:text-gray-600"
              />
            ))}
          </g>
        )}

        {/* Y-axis labels */}
        {gridLines.horizontal.map((line, i) => (
          <text
            key={`y-label-${i}`}
            x={55}
            y={line.y + 4}
            textAnchor="end"
            className="text-xs fill-gray-600 dark:fill-gray-400"
          >
            {line.label}
          </text>
        ))}

        {/* X-axis labels */}
        {gridLines.vertical.map((line, i) => i % Math.ceil(gridLines.vertical.length / 6) === 0 && (
          <text
            key={`x-label-${i}`}
            x={line.x}
            y={height - 20}
            textAnchor="middle"
            className="text-xs fill-gray-600 dark:fill-gray-400"
          >
            {new Date(line.label).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}
          </text>
        ))}

        {/* Lines */}
        {lines.map((line, i) => (
          <g key={`line-${i}`}>
            <path
              d={line.pathData}
              fill="none"
              stroke={line.color}
              strokeWidth="2"
              className="drop-shadow-sm"
            />
            {/* Data points */}
            {line.points.map((point, j) => (
              <circle
                key={`point-${i}-${j}`}
                cx={point.x}
                cy={point.y}
                r="4"
                fill={line.color}
                className="cursor-pointer hover:r-6 transition-all"
                onMouseEnter={() => showTooltip && setHoveredPoint({ x: point.x, y: point.y, data: point.data })}
                onMouseLeave={() => setHoveredPoint(null)}
              />
            ))}
          </g>
        ))}
      </svg>

      {/* Tooltip */}
      {hoveredPoint && (
        <div
          className="absolute z-10 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg p-3 pointer-events-none"
          style={{
            left: Math.min(hoveredPoint.x + 10, width - 150),
            top: Math.max(hoveredPoint.y - 60, 10)
          }}
        >
          <p className="text-sm font-medium text-gray-900 dark:text-white">
            {hoveredPoint.data.name}
          </p>
          <p className="text-xs text-gray-600 dark:text-gray-400">
            {new Date(hoveredPoint.data.date).toLocaleDateString()}
          </p>
          <p className="text-lg font-semibold text-blue-600 dark:text-blue-400">
            {hoveredPoint.data.value.toLocaleString()}
          </p>
        </div>
      )}

      {/* Legend */}
      {showLegend && lines.length > 1 && (
        <div className="flex flex-wrap justify-center mt-4 gap-4">
          {lines.map((line, i) => (
            <div key={`legend-${i}`} className="flex items-center gap-2">
              <div
                className="w-3 h-3 rounded-full"
                style={{ backgroundColor: line.color }}
              />
              <span className="text-sm text-gray-600 dark:text-gray-400">
                {line.name}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
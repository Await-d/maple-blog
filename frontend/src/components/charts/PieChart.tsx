/**
 * PieChart - A lightweight, responsive pie chart component using SVG
 */

import React, { useMemo } from 'react';
import { ChartDataPoint } from '@/types/analytics';

interface PieChartProps {
  data: ChartDataPoint[];
  width?: number;
  height?: number;
  className?: string;
  showLabels?: boolean;
  showLegend?: boolean;
  showPercentages?: boolean;
  innerRadius?: number;
  colors?: string[];
}

export const PieChart: React.FC<PieChartProps> = ({
  data,
  width = 300,
  height = 300,
  className = '',
  showLabels = true,
  showLegend = true,
  showPercentages = true,
  innerRadius = 0,
  colors = ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899', '#14B8A6', '#F97316']
}) => {
  const [hoveredSlice, setHoveredSlice] = React.useState<{
    x: number;
    y: number;
    data: ChartDataPoint;
  } | null>(null);

  const { slices, total, center } = useMemo(() => {
    if (!data.length) return { slices: [], total: 0, center: { x: 0, y: 0 }, radius: 0 };

    const total = data.reduce((sum, item) => sum + item.value, 0);
    const center = { x: width / 2, y: height / 2 };
    const radius = Math.min(width, height) / 2 - 20;

    let currentAngle = -Math.PI / 2; // Start from top

    const slices = data.map((item, i) => {
      const percentage = (item.value / total) * 100;
      const angle = (item.value / total) * 2 * Math.PI;
      const startAngle = currentAngle;
      const endAngle = currentAngle + angle;

      // Calculate path for slice
      const x1 = center.x + Math.cos(startAngle) * radius;
      const y1 = center.y + Math.sin(startAngle) * radius;
      const x2 = center.x + Math.cos(endAngle) * radius;
      const y2 = center.y + Math.sin(endAngle) * radius;

      const largeArcFlag = angle > Math.PI ? 1 : 0;

      let pathData = `M ${center.x} ${center.y} L ${x1} ${y1} A ${radius} ${radius} 0 ${largeArcFlag} 1 ${x2} ${y2} Z`;
      
      // For donut chart
      if (innerRadius > 0) {
        const innerX1 = center.x + Math.cos(startAngle) * innerRadius;
        const innerY1 = center.y + Math.sin(startAngle) * innerRadius;
        const innerX2 = center.x + Math.cos(endAngle) * innerRadius;
        const innerY2 = center.y + Math.sin(endAngle) * innerRadius;

        pathData = `M ${x1} ${y1} A ${radius} ${radius} 0 ${largeArcFlag} 1 ${x2} ${y2} L ${innerX2} ${innerY2} A ${innerRadius} ${innerRadius} 0 ${largeArcFlag} 0 ${innerX1} ${innerY1} Z`;
      }

      // Label position
      const labelAngle = startAngle + angle / 2;
      const labelRadius = radius + (innerRadius > 0 ? 0 : 20);
      const labelX = center.x + Math.cos(labelAngle) * labelRadius;
      const labelY = center.y + Math.sin(labelAngle) * labelRadius;

      currentAngle += angle;

      return {
        ...item,
        pathData,
        percentage,
        color: item.color || colors[i % colors.length],
        labelPosition: { x: labelX, y: labelY },
        centerPosition: {
          x: center.x + Math.cos(labelAngle) * (radius - innerRadius) / 2,
          y: center.y + Math.sin(labelAngle) * (radius - innerRadius) / 2
        }
      };
    });

    return { slices, total, center };
  }, [data, width, height, innerRadius, colors]);

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
        {/* Slices */}
        {slices.map((slice, i) => (
          <g key={`slice-${i}`}>
            <path
              d={slice.pathData}
              fill={slice.color}
              className="cursor-pointer hover:opacity-80 transition-opacity"
              onMouseEnter={() => setHoveredSlice({
                x: slice.centerPosition.x,
                y: slice.centerPosition.y,
                data: slice
              })}
              onMouseLeave={() => setHoveredSlice(null)}
            />
            
            {/* Labels */}
            {showLabels && slice.percentage > 5 && (
              <text
                x={slice.labelPosition.x}
                y={slice.labelPosition.y}
                textAnchor={slice.labelPosition.x > center.x ? 'start' : 'end'}
                className="text-xs font-medium fill-gray-700 dark:fill-gray-300"
              >
                {showPercentages ? `${slice.percentage.toFixed(1)}%` : slice.name}
              </text>
            )}
          </g>
        ))}

        {/* Center label for donut chart */}
        {innerRadius > 0 && (
          <g>
            <text
              x={center.x}
              y={center.y - 8}
              textAnchor="middle"
              className="text-lg font-bold fill-gray-900 dark:fill-white"
            >
              {total.toLocaleString()}
            </text>
            <text
              x={center.x}
              y={center.y + 12}
              textAnchor="middle"
              className="text-sm fill-gray-600 dark:fill-gray-400"
            >
              Total
            </text>
          </g>
        )}
      </svg>

      {/* Tooltip */}
      {hoveredSlice && (
        <div
          className="absolute z-10 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg p-3 pointer-events-none"
          style={{
            left: Math.min(hoveredSlice.x + 10, width - 150),
            top: Math.max(hoveredSlice.y - 60, 10)
          }}
        >
          <p className="text-sm font-medium text-gray-900 dark:text-white">
            {hoveredSlice.data.name}
          </p>
          <p className="text-lg font-semibold text-blue-600 dark:text-blue-400">
            {hoveredSlice.data.value.toLocaleString()}
          </p>
          <p className="text-xs text-gray-600 dark:text-gray-400">
            {hoveredSlice.data.percentage?.toFixed(1)}% of total
          </p>
        </div>
      )}

      {/* Legend */}
      {showLegend && (
        <div className="flex flex-wrap justify-center mt-4 gap-3">
          {slices.map((slice, i) => (
            <div key={`legend-${i}`} className="flex items-center gap-2">
              <div
                className="w-3 h-3 rounded-full"
                style={{ backgroundColor: slice.color }}
              />
              <span className="text-sm text-gray-600 dark:text-gray-400">
                {slice.name}
              </span>
              {showPercentages && (
                <span className="text-xs text-gray-500 dark:text-gray-500">
                  ({slice.percentage.toFixed(1)}%)
                </span>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
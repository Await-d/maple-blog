import React, { useMemo } from 'react';
import ChartWrapper, { ChartWrapperProps } from './ChartWrapper';
import { ChartUtils } from '../../utils/chartUtils';

export interface TreeMapData {
  name: string;
  value: number;
  children?: TreeMapData[];
  itemStyle?: {
    color?: string;
    borderColor?: string;
    borderWidth?: number;
  };
  label?: {
    show?: boolean;
    position?: string;
    formatter?: string | ((params: TreeMapTooltipParams) => string);
  };
}

interface TreeMapTooltipParams {
  data: {
    value: number;
    treePathInfo: Array<{ name: string }>;
  };
}

interface TreeMapLevel {
  itemStyle?: {
    borderColor?: string;
    borderWidth?: number;
    gapWidth?: number;
    color?: string;
    textStyle?: {
      color?: string;
    };
  };
  upperLabel?: {
    show?: boolean;
  };
  emphasis?: {
    itemStyle?: {
      borderColor?: string;
    };
  };
  label?: {
    show?: boolean;
  };
}

export interface TreeMapChartProps extends Omit<ChartWrapperProps, 'option' | 'type'> {
  data: TreeMapData[];
  squareRatio?: number;
  leafDepth?: number;
  drillDownIcon?: string;
  roam?: boolean | 'scale' | 'move';
  nodeClick?: 'zoomToNode' | 'link' | false;
  zoomToNodeRatio?: number;
  levels?: TreeMapLevel[];
  breadcrumb?: {
    show?: boolean;
    height?: number;
    left?: string | number;
    top?: string | number;
    right?: string | number;
    bottom?: string | number;
    emptyItemWidth?: number;
    itemStyle?: {
      color?: string;
      textStyle?: {
        color?: string;
      };
    };
    emphasis?: {
      itemStyle?: {
        color?: string;
      };
    };
  };
  colorSaturation?: [number, number];
  colorAlpha?: [number, number];
  colorMappingBy?: 'value' | 'index' | 'id';
  visibleMin?: number;
  childrenVisibleMin?: number;
}

const TreeMapChart: React.FC<TreeMapChartProps> = ({
  data,
  squareRatio = 0.75,
  leafDepth,
  drillDownIcon = '▶',
  roam = false,
  nodeClick = 'zoomToNode',
  zoomToNodeRatio = 0.1,
  levels = [],
  breadcrumb,
  colorSaturation,
  colorAlpha,
  colorMappingBy = 'value',
  visibleMin = 10,
  childrenVisibleMin,
  ...chartProps
}) => {
  const option = useMemo(() => {
    if (!data.length) return {};

    // Generate colors for different levels
    const colors = ChartUtils.generateColors(10, 'default');

    // Default levels configuration
    const defaultLevels: TreeMapLevel[] = [
      {
        itemStyle: {
          borderColor: '#777',
          borderWidth: 0,
          gapWidth: 1
        },
        upperLabel: {
          show: false
        }
      },
      {
        itemStyle: {
          borderColor: '#555',
          borderWidth: 5,
          gapWidth: 1
        },
        emphasis: {
          itemStyle: {
            borderColor: '#ddd'
          }
        }
      },
      {
        itemStyle: {
          borderColor: '#333',
          borderWidth: 5,
          gapWidth: 1
        },
        emphasis: {
          itemStyle: {
            borderColor: '#999'
          }
        }
      }
    ];

    // Process data to add colors if not specified
    const processData = (nodes: TreeMapData[], level: number = 0): TreeMapData[] => {
      return nodes.map((node, index) => ({
        ...node,
        itemStyle: {
          color: colors[index % colors.length],
          ...node.itemStyle
        },
        ...(node.children && { children: processData(node.children, level + 1) })
      }));
    };

    const processedData = processData(data);

    return {
      series: [{
        name: '树图',
        type: 'treemap',
        data: processedData,
        squareRatio,
        leafDepth,
        drillDownIcon,
        roam,
        nodeClick,
        zoomToNodeRatio,
        levels: levels.length > 0 ? levels : defaultLevels,
        breadcrumb: breadcrumb || {
          show: true,
          height: 22,
          left: 'center',
          top: 'bottom',
          emptyItemWidth: 25,
          itemStyle: {
            color: 'rgba(0,0,0,0.7)',
            textStyle: {
              color: 'rgba(255,255,255,1)'
            }
          },
          emphasis: {
            itemStyle: {
              color: 'rgba(0,0,0,0.9)'
            }
          }
        },
        label: {
          show: true,
          formatter: '{b}',
          color: '#fff',
          fontSize: 12
        },
        upperLabel: {
          show: true,
          height: 20,
          formatter: '{b}',
          color: '#fff'
        },
        itemStyle: {
          borderColor: '#fff',
          borderWidth: 1
        },
        emphasis: {
          label: {
            show: true
          },
          itemStyle: {
            borderColor: '#333'
          },
          upperLabel: {
            show: true
          }
        },
        colorSaturation,
        colorAlpha,
        colorMappingBy,
        visibleMin,
        childrenVisibleMin: childrenVisibleMin || visibleMin * 0.1
      }],
      tooltip: {
        trigger: 'item',
        formatter: (params: TreeMapTooltipParams) => {
          const { value, treePathInfo } = params.data;
          const path = treePathInfo.map((item) => item.name).join(' > ');
          return `${path}<br/>值: ${value}`;
        }
      }
    };
  }, [
    data,
    squareRatio,
    leafDepth,
    drillDownIcon,
    roam,
    nodeClick,
    zoomToNodeRatio,
    levels,
    breadcrumb,
    colorSaturation,
    colorAlpha,
    colorMappingBy,
    visibleMin,
    childrenVisibleMin
  ]);

  return (
    <ChartWrapper
      {...chartProps}
      option={option}
      type="treemap"
    />
  );
};

export default TreeMapChart;
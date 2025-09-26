import React, { useState, useCallback, useRef, useEffect } from 'react';
import { Row, Col, Button, Modal, Select, Switch, Slider, Space, Divider, message } from 'antd';
import { SettingOutlined, ExpandOutlined, SyncOutlined, DownloadOutlined, PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import ChartWrapper, { ChartWrapperProps, ChartWrapperRef } from './ChartWrapper';
import { DndProvider, useDrag, useDrop } from 'react-dnd';
import { HTML5Backend } from 'react-dnd-html5-backend';
import classNames from 'classnames';

// ECharts option interface - simplified version for common chart types
interface EChartsOption {
  xAxis?: Record<string, unknown>;
  yAxis?: Record<string, unknown>;
  series?: Record<string, unknown>[];
  title?: Record<string, unknown>;
  legend?: Record<string, unknown>;
  tooltip?: Record<string, unknown>;
  grid?: Record<string, unknown>;
  dataZoom?: Record<string, unknown>[];
  animation?: boolean;
  [key: string]: unknown;
}

export interface ChartConfig extends Omit<ChartWrapperProps, 'option'> {
  id: string;
  title: string;
  option: EChartsOption;
  span: number; // Grid span (1-24)
  order: number;
  visible: boolean;
}

export interface MultiChartProps {
  charts: ChartConfig[];
  gutter?: [number, number];
  className?: string;
  style?: React.CSSProperties;
  editable?: boolean;
  refreshable?: boolean;
  exportable?: boolean;
  onChartsChange?: (charts: ChartConfig[]) => void;
  onChartUpdate?: (chartId: string, option: EChartsOption) => void;
  onRefreshAll?: () => void;
  layout?: 'grid' | 'masonry';
  theme?: 'light' | 'dark';
  animated?: boolean;
}

interface DragItem {
  id: string;
  index: number;
  type: string;
}

const DraggableChart: React.FC<{
  chart: ChartConfig;
  index: number;
  moveChart: (dragIndex: number, hoverIndex: number) => void;
  onEdit: (chart: ChartConfig) => void;
  onDelete: (chartId: string) => void;
  onToggleVisibility: (chartId: string) => void;
  editable: boolean;
  theme: string;
}> = ({ chart, index, moveChart, onEdit, onDelete, onToggleVisibility, editable, theme }) => {
  const ref = useRef<HTMLDivElement>(null);
  const chartRef = useRef<ChartWrapperRef>(null);

  const [{ handlerId }, drop] = useDrop<DragItem, void, { handlerId: string | symbol | null }>({
    accept: 'chart',
    collect(monitor) {
      return {
        handlerId: monitor.getHandlerId(),
      };
    },
    hover(item: DragItem, monitor: import('react-dnd').DropTargetMonitor) {
      if (!ref.current) return;

      const dragIndex = item.index;
      const hoverIndex = index;

      if (dragIndex === hoverIndex) return;

      const hoverBoundingRect = ref.current.getBoundingClientRect();
      const hoverMiddleY = (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;
      const clientOffset = monitor.getClientOffset();
      const hoverClientY = (clientOffset?.y || 0) - hoverBoundingRect.top;

      if (dragIndex < hoverIndex && hoverClientY < hoverMiddleY) return;
      if (dragIndex > hoverIndex && hoverClientY > hoverMiddleY) return;

      moveChart(dragIndex, hoverIndex);
      item.index = hoverIndex;
    },
  });

  const [{ isDragging }, drag] = useDrag({
    type: 'chart',
    item: () => ({ id: chart.id, index }),
    collect: (monitor) => ({
      isDragging: monitor.isDragging(),
    }),
  });

  const opacity = isDragging ? 0.4 : 1;
  drag(drop(ref));

  const handleExport = () => {
    chartRef.current?.exportChart('png');
  };

  const chartActions = editable ? (
    <Space >
      <Button
        type="text"
        
        icon={<SettingOutlined />}
        onClick={() => onEdit(chart)}
        title="编辑图表"
      />
      <Button
        type="text"
        
        icon={<ExpandOutlined />}
        onClick={handleExport}
        title="导出图表"
      />
      <Switch
        
        checked={chart.visible}
        onChange={() => onToggleVisibility(chart.id)}
        title="显示/隐藏"
      />
      <Button
        type="text"
        
        danger
        icon={<DeleteOutlined />}
        onClick={() => onDelete(chart.id)}
        title="删除图表"
      />
    </Space>
  ) : (
    <Button
      type="text"
      
      icon={<DownloadOutlined />}
      onClick={handleExport}
      title="导出图表"
    />
  );

  if (!chart.visible) {
    return null;
  }

  return (
    <Col
      ref={ref}
      span={chart.span}
      style={{ opacity }}
      data-handler-id={handlerId}
      className={classNames('multi-chart-item', {
        'dragging': isDragging,
        'draggable': editable
      })}
    >
      <ChartWrapper
        ref={chartRef}
        {...chart}
        theme={theme}
        extra={chartActions}
        className={classNames(chart.className, {
          'editable-chart': editable
        })}
      />
    </Col>
  );
};

const ChartEditor: React.FC<{
  chart: ChartConfig | null;
  visible: boolean;
  onSave: (chart: ChartConfig) => void;
  onCancel: () => void;
}> = ({ chart, visible, onSave, onCancel }) => {
  const [editingChart, setEditingChart] = useState<ChartConfig | null>(null);

  useEffect(() => {
    if (chart) {
      setEditingChart({ ...chart });
    }
  }, [chart]);

  const handleSave = () => {
    if (editingChart) {
      onSave(editingChart);
    }
  };

  const handleSpanChange = (value: number) => {
    if (editingChart) {
      setEditingChart({ ...editingChart, span: value });
    }
  };

  const handleHeightChange = (value: number) => {
    if (editingChart) {
      setEditingChart({ ...editingChart, height: value });
    }
  };

  const handleThemeChange = (value: string) => {
    if (editingChart) {
      setEditingChart({ ...editingChart, theme: value as 'light' | 'dark' });
    }
  };

  if (!editingChart) return null;

  return (
    <Modal
      title="编辑图表"
      open={visible}
      onOk={handleSave}
      onCancel={onCancel}
      width={600}
      okText="保存"
      cancelText="取消"
    >
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-2">图表标题</label>
          <input
            type="text"
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
            value={editingChart.title}
            onChange={(e) => setEditingChart({ ...editingChart, title: e.target.value })}
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-2">网格跨度 (1-24)</label>
          <Slider
            min={1}
            max={24}
            value={editingChart.span}
            onChange={handleSpanChange}
            marks={{
              6: '6',
              8: '8',
              12: '12',
              16: '16',
              24: '24'
            }}
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-2">图表高度 (px)</label>
          <Slider
            min={200}
            max={800}
            step={50}
            value={typeof editingChart.height === 'number' ? editingChart.height : 400}
            onChange={handleHeightChange}
            marks={{
              200: '200px',
              400: '400px',
              600: '600px',
              800: '800px'
            }}
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-2">主题</label>
          <Select
            value={editingChart.theme || 'light'}
            onChange={handleThemeChange}
            options={[
              { label: '浅色主题', value: 'light' },
              { label: '深色主题', value: 'dark' }
            ]}
            className="w-full"
          />
        </div>

        <Divider />

        <div>
          <label className="block text-sm font-medium mb-2">图表功能</label>
          <Space direction="vertical">
            <label className="flex items-center">
              <input
                type="checkbox"
                checked={editingChart.refreshable}
                onChange={(e) => setEditingChart({ ...editingChart, refreshable: e.target.checked })}
                className="mr-2"
              />
              可刷新
            </label>
            <label className="flex items-center">
              <input
                type="checkbox"
                checked={editingChart.exportable}
                onChange={(e) => setEditingChart({ ...editingChart, exportable: e.target.checked })}
                className="mr-2"
              />
              可导出
            </label>
            <label className="flex items-center">
              <input
                type="checkbox"
                checked={editingChart.animation}
                onChange={(e) => setEditingChart({ ...editingChart, animation: e.target.checked })}
                className="mr-2"
              />
              动画效果
            </label>
          </Space>
        </div>
      </div>
    </Modal>
  );
};

const MultiChart: React.FC<MultiChartProps> = ({
  charts: initialCharts,
  gutter = [16, 16],
  className,
  style,
  editable = false,
  refreshable = true,
  exportable = true,
  onChartsChange,
  onChartUpdate: _onChartUpdate,
  onRefreshAll,
  layout = 'grid',
  theme = 'light',
  animated = true
}) => {
  const [charts, setCharts] = useState<ChartConfig[]>(initialCharts);
  const [editingChart, setEditingChart] = useState<ChartConfig | null>(null);
  const [editorVisible, setEditorVisible] = useState(false);

  // Update local state when props change
  useEffect(() => {
    setCharts(initialCharts);
  }, [initialCharts]);

  // Notify parent of changes
  const updateCharts = useCallback((newCharts: ChartConfig[]) => {
    setCharts(newCharts);
    onChartsChange?.(newCharts);
  }, [onChartsChange]);

  const moveChart = useCallback((dragIndex: number, hoverIndex: number) => {
    const newCharts = [...charts];
    const draggedChart = newCharts[dragIndex];
    newCharts.splice(dragIndex, 1);
    newCharts.splice(hoverIndex, 0, draggedChart);

    // Update order property
    newCharts.forEach((chart, index) => {
      chart.order = index;
    });

    updateCharts(newCharts);
  }, [charts, updateCharts]);

  const handleEditChart = (chart: ChartConfig) => {
    setEditingChart(chart);
    setEditorVisible(true);
  };

  const handleSaveChart = (updatedChart: ChartConfig) => {
    const newCharts = charts.map(chart =>
      chart.id === updatedChart.id ? updatedChart : chart
    );
    updateCharts(newCharts);
    setEditorVisible(false);
    setEditingChart(null);
    message.success('图表配置已保存');
  };

  const handleDeleteChart = (chartId: string) => {
    const newCharts = charts.filter(chart => chart.id !== chartId);
    updateCharts(newCharts);
    message.success('图表已删除');
  };

  const handleToggleVisibility = (chartId: string) => {
    const newCharts = charts.map(chart =>
      chart.id === chartId ? { ...chart, visible: !chart.visible } : chart
    );
    updateCharts(newCharts);
  };

  const handleRefreshAll = () => {
    onRefreshAll?.();
    message.success('所有图表已刷新');
  };

  const handleExportAll = () => {
    // Export all visible charts as a single image
    message.info('导出所有图表功能开发中...');
  };

  const handleAddChart = () => {
    const newChart: ChartConfig = {
      id: `chart_${Date.now()}`,
      title: '新图表',
      option: {
        xAxis: { type: 'category', data: ['A', 'B', 'C'] },
        yAxis: { type: 'value' },
        series: [{ data: [120, 200, 150], type: 'bar' }]
      },
      span: 12,
      order: charts.length,
      visible: true,
      height: 400,
      theme: theme as 'light' | 'dark',
      refreshable: true,
      exportable: true,
      animation: true
    };

    updateCharts([...charts, newChart]);
    message.success('新图表已添加');
  };

  const sortedCharts = [...charts].sort((a, b) => a.order - b.order);

  const renderToolbar = () => (
    <div className="flex items-center justify-between mb-4">
      <div className="text-lg font-semibold">
        多图表仪表盘 ({charts.filter(c => c.visible).length}/{charts.length})
      </div>
      <Space>
        {refreshable && (
          <Button
            icon={<SyncOutlined />}
            onClick={handleRefreshAll}
          >
            刷新所有
          </Button>
        )}
        {exportable && (
          <Button
            icon={<DownloadOutlined />}
            onClick={handleExportAll}
          >
            导出所有
          </Button>
        )}
        {editable && (
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={handleAddChart}
          >
            添加图表
          </Button>
        )}
      </Space>
    </div>
  );

  const renderCharts = () => (
    <Row gutter={gutter} className={classNames({ 'editable-mode': editable })}>
      {sortedCharts.map((chart, index) => (
        <DraggableChart
          key={chart.id}
          chart={chart}
          index={index}
          moveChart={moveChart}
          onEdit={handleEditChart}
          onDelete={handleDeleteChart}
          onToggleVisibility={handleToggleVisibility}
          editable={editable}
          theme={theme}
        />
      ))}
    </Row>
  );

  return (
    <DndProvider backend={HTML5Backend}>
      <div className={classNames('multi-chart-container', className)} style={style}>
        {renderToolbar()}

        <div className={classNames('charts-grid', {
          'masonry-layout': layout === 'masonry',
          'animated': animated
        })}>
          {renderCharts()}
        </div>

        <ChartEditor
          chart={editingChart}
          visible={editorVisible}
          onSave={handleSaveChart}
          onCancel={() => {
            setEditorVisible(false);
            setEditingChart(null);
          }}
        />
      </div>

      <style>{`
        .multi-chart-container {
          min-height: 100vh;
        }

        .charts-grid.animated .multi-chart-item {
          transition: all 0.3s ease;
        }

        .multi-chart-item.dragging {
          transform: rotate(5deg);
          box-shadow: 0 8px 16px rgba(0, 0, 0, 0.2);
        }

        .multi-chart-item.draggable {
          cursor: move;
        }

        .editable-chart {
          border: 2px dashed transparent;
          transition: border-color 0.2s ease;
        }

        .editable-mode .editable-chart:hover {
          border-color: #1890ff;
        }

        .masonry-layout {
          column-count: auto;
          column-width: 400px;
          column-gap: 16px;
        }

        .masonry-layout .multi-chart-item {
          break-inside: avoid;
          margin-bottom: 16px;
        }
      `}</style>
    </DndProvider>
  );
};

export default MultiChart;
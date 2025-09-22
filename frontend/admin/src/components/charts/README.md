# Chart Visualization Component Library

A comprehensive chart visualization library for the admin dashboard, built with ECharts and React.

## Components

### 1. ChartWrapper (`ChartWrapper.tsx`)
Universal chart wrapper component that provides a consistent API for all chart types.

**Features:**
- Support for all ECharts chart types (line, bar, pie, scatter, heatmap, treemap, etc.)
- Built-in toolbar with export, refresh, fullscreen capabilities
- Theme switching (light/dark)
- Animation controls
- Responsive design
- Error handling and loading states
- TypeScript support

**Usage:**
```tsx
import { ChartWrapper } from '@/components/charts';

<ChartWrapper
  title="Sales Chart"
  option={chartOption}
  theme="light"
  height={400}
  exportable
  refreshable
  onRefresh={() => refetchData()}
/>
```

### 2. MultiChart (`MultiChart.tsx`)
Multi-chart dashboard component with drag-and-drop layout management.

**Features:**
- Drag and drop chart reordering
- Individual chart configuration
- Grid layout with responsive columns
- Chart visibility toggle
- Batch operations (refresh all, export all)
- Real-time chart editing
- Persistent layout configuration

**Usage:**
```tsx
import { MultiChart, ChartConfig } from '@/components/charts';

const charts: ChartConfig[] = [
  {
    id: 'chart1',
    title: 'Revenue',
    option: revenueChartOption,
    span: 12,
    order: 0,
    visible: true
  }
];

<MultiChart
  charts={charts}
  editable
  onChartsChange={setCharts}
  theme="light"
/>
```

### 3. RealTimeChart (`RealTimeChart.tsx`)
Real-time data visualization with WebSocket and polling support.

**Features:**
- WebSocket integration for live updates
- Polling fallback for REST APIs
- Multiple chart types (line, bar, gauge)
- Threshold monitoring with alerts
- Performance metrics display
- Configurable update intervals
- Data point limits
- Pause/resume functionality

**Usage:**
```tsx
import { RealTimeChart } from '@/components/charts';

<RealTimeChart
  title="System Metrics"
  type="line"
  websocketUrl="ws://localhost:3001/metrics"
  maxDataPoints={100}
  updateInterval={1000}
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
```

### 4. HeatMapChart (`HeatMapChart.tsx`)
Specialized heatmap component for matrix data visualization.

**Features:**
- Multiple color scales (viridis, plasma, warm, cool)
- Custom color mapping
- Configurable visual map
- Cell border styling
- Automatic axis generation
- Label display options

**Usage:**
```tsx
import { HeatMapChart } from '@/components/charts';

<HeatMapChart
  data={heatmapData}
  colorScale="viridis"
  showLabel={false}
  title="User Activity Heatmap"
/>
```

### 5. TreeMapChart (`TreeMapChart.tsx`)
Hierarchical data visualization using treemap layout.

**Features:**
- Hierarchical data support
- Drill-down navigation
- Breadcrumb navigation
- Custom styling per level
- Interactive zoom
- Color mapping options

**Usage:**
```tsx
import { TreeMapChart } from '@/components/charts';

<TreeMapChart
  data={hierarchicalData}
  roam="move"
  nodeClick="zoomToNode"
  title="Organization Structure"
/>
```

### 6. ChartShowcase (`ChartShowcase.tsx`)
Comprehensive demo component showcasing all chart capabilities.

## Utilities

### ChartUtils (`../../utils/chartUtils.ts`)
Comprehensive utility class with helper functions:

- **Color Management**: Generate palettes, adjust brightness, color interpolation
- **Data Processing**: Time series generation, moving averages, trend detection
- **Export Functions**: Image export with various formats
- **Responsive Design**: Dynamic option generation based on container size
- **Theme Generation**: Custom theme creation

## Key Features

### 🎨 Theme Support
- Light and dark themes
- Custom theme generation
- Automatic color palette selection

### 📊 Chart Types
- Line charts with smooth curves
- Bar charts with animations
- Pie charts with emphasis effects
- Scatter plots with bubble sizing
- Heatmaps with color mapping
- Treemaps with hierarchical data
- Gauge charts for KPIs
- Real-time streaming charts

### 🚀 Performance
- Optimized rendering with ECharts
- Virtual scrolling for large datasets
- Efficient data updates
- Memory management for real-time charts

### 📱 Responsive Design
- Mobile-first approach
- Breakpoint-aware layouts
- Container-based sizing
- Touch-friendly interactions

### 🔧 Developer Experience
- TypeScript definitions
- Comprehensive prop interfaces
- Error boundaries
- Hot reload support

## Installation

Dependencies are already included in the project:
- `echarts`: Core charting library
- `echarts-for-react`: React wrapper
- `react-dnd`: Drag and drop functionality
- `antd`: UI components

## Best Practices

### Performance
```tsx
// Use React.memo for expensive chart components
const OptimizedChart = React.memo(ChartWrapper);

// Debounce real-time updates
const debouncedUpdate = useDebouncedCallback(updateChart, 100);
```

### Data Management
```tsx
// Use proper data structures
interface ChartData {
  timestamp: number;
  value: number;
  metadata?: Record<string, any>;
}

// Implement data validation
const validateChartData = (data: unknown): data is ChartData[] => {
  // validation logic
};
```

### Error Handling
```tsx
<ChartWrapper
  option={chartOption}
  error={error?.message}
  loading={isLoading}
  empty={!data?.length}
  onRefresh={() => refetch()}
/>
```

## Examples

See `ChartShowcase.tsx` for comprehensive examples of all components and features.

## Contributing

When adding new chart types or features:

1. Follow existing TypeScript patterns
2. Add proper error handling
3. Include loading and empty states
4. Write comprehensive prop documentation
5. Add to the main index export
6. Update this README
// @ts-nocheck
import React, { useMemo, useCallback, useState, useRef, useEffect } from 'react';
import { Table, Space, Button, Input, Select, Tooltip, Dropdown, Checkbox, Empty, Spin } from 'antd';
import {
  SearchOutlined,
  FilterOutlined,
  ColumnHeightOutlined,
  SettingOutlined,
  DownloadOutlined,
  ReloadOutlined,
  FullscreenOutlined,
  FullscreenExitOutlined,
} from '@ant-design/icons';
import type { TableProps, ColumnsType, TableRowSelection } from 'antd/es/table';
import type { SizeType } from 'antd/es/config-provider/SizeContext';
import classNames from 'classnames';
import { useDataTable } from '../../hooks/useDataTable';
import { VirtualTable } from './VirtualTable';
import { TableActions } from './TableActions';
import type { TableColumn, QueryParams } from '../../types';

// 扩展的列配置接口
export interface DataTableColumn<T = any> extends TableColumn<T> {
  searchable?: boolean;
  filterable?: boolean;
  exportable?: boolean;
  resizable?: boolean;
  hidden?: boolean;
  pinned?: 'left' | 'right';
  aggregation?: 'sum' | 'avg' | 'count' | 'min' | 'max';
  cellRenderer?: (value: any, record: T, index: number) => React.ReactNode;
  headerRenderer?: () => React.ReactNode;
  filterType?: 'text' | 'select' | 'date' | 'number' | 'range';
  filterOptions?: Array<{ label: string; value: any }>;
  sortType?: 'string' | 'number' | 'date';
}

export interface DataTableProps<T = any> {
  // 基础属性
  columns: DataTableColumn<T>[];
  dataSource: T[];
  loading?: boolean;
  className?: string;
  style?: React.CSSProperties;
  
  // 分页配置
  pagination?: {
    current: number;
    pageSize: number;
    total: number;
    showSizeChanger?: boolean;
    showQuickJumper?: boolean;
    showTotal?: (total: number, range: [number, number]) => string;
    onChange: (page: number, pageSize: number) => void;
  } | false;
  
  // 选择配置
  rowSelection?: {
    selectedRowKeys: React.Key[];
    onChange: (selectedRowKeys: React.Key[], selectedRows: T[]) => void;
    onSelect?: (record: T, selected: boolean, selectedRows: T[], nativeEvent: Event) => void;
    onSelectAll?: (selected: boolean, selectedRows: T[], changeRows: T[]) => void;
    getCheckboxProps?: (record: T) => object;
    type?: 'checkbox' | 'radio';
    fixed?: boolean;
    columnWidth?: string | number;
    preserveSelectedRowKeys?: boolean;
    hideSelectAll?: boolean;
    checkStrictly?: boolean;
  };
  
  // 功能配置
  searchable?: boolean;
  filterable?: boolean;
  exportable?: boolean;
  columnsConfigurable?: boolean;
  resizable?: boolean;
  sortable?: boolean;
  
  // 虚拟滚动配置
  virtual?: boolean;
  virtualThreshold?: number; // 超过多少行启用虚拟滚动
  scroll?: { x?: number; y?: number };
  
  // 表格配置
  size?: SizeType;
  bordered?: boolean;
  showHeader?: boolean;
  tableLayout?: 'auto' | 'fixed';
  
  // 事件回调
  onRow?: (record: T, index?: number) => React.HTMLAttributes<any>;
  onHeaderRow?: (columns: ColumnsType<T>, index?: number) => React.HTMLAttributes<any>;
  onSearch?: (value: string) => void;
  onFilter?: (filters: Record<string, any>) => void;
  onSort?: (field: string, order: 'asc' | 'desc' | null) => void;
  onExport?: (data: T[], columns: DataTableColumn<T>[]) => void;
  onRefresh?: () => void;
  onColumnConfigChange?: (columns: DataTableColumn<T>[]) => void;
  
  // 自定义渲染
  title?: React.ReactNode;
  footer?: React.ReactNode;
  summary?: (data: readonly T[]) => React.ReactNode;
  expandedRowRender?: (record: T, index: number, indent: number, expanded: boolean) => React.ReactNode;
  expandIcon?: (props: any) => React.ReactNode;
  expandRowByClick?: boolean;
  
  // 性能优化
  rowKey?: string | ((record: T) => string);
  showSorterTooltip?: boolean;
  locale?: object;
  
  // 高级功能
  fullscreen?: boolean;
  toolbar?: boolean;
  densitySelector?: boolean;
  columnSelector?: boolean;
}

export const DataTable = <T extends Record<string, any>>(props: DataTableProps<T>) => {
  const {
    columns: propColumns,
    dataSource,
    loading = false,
    className,
    style,
    pagination,
    rowSelection,
    searchable = true,
    filterable = true,
    exportable = true,
    columnsConfigurable = true,
    resizable = true,
    sortable = true,
    virtual = false,
    virtualThreshold = 1000,
    scroll,
    size = 'middle',
    bordered = false,
    showHeader = true,
    tableLayout = 'auto',
    onRow,
    onHeaderRow,
    onSearch,
    onFilter,
    onSort,
    onExport,
    onRefresh,
    onColumnConfigChange,
    title,
    footer,
    summary,
    expandedRowRender,
    expandIcon,
    expandRowByClick,
    rowKey = 'id',
    showSorterTooltip = false,
    locale,
    fullscreen = false,
    toolbar = true,
    densitySelector = true,
    columnSelector = true,
    ...restProps
  } = props;

  // 状态管理
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [tableSize, setTableSize] = useState<SizeType>(size);
  const [searchValue, setSearchValue] = useState('');
  const [filters, setFilters] = useState<Record<string, any>>({});
  const [sortConfig, setSortConfig] = useState<{ field: string; order: 'asc' | 'desc' } | null>(null);
  const [columnsConfig, setColumnsConfig] = useState<DataTableColumn<T>[]>(propColumns);
  
  const tableRef = useRef<HTMLDivElement>(null);

  // 使用自定义hook进行数据管理
  const {
    processedData,
    visibleColumns,
    selectedRowKeys,
    searchData,
    filterData,
    sortData,
    exportData,
    toggleColumnVisibility,
    resetColumns,
    isProcessing,
  } = useDataTable({
    data: dataSource,
    columns: columnsConfig,
    searchValue,
    filters,
    sortConfig,
    rowSelection,
  });

  // 全屏功能
  const toggleFullscreen = useCallback(() => {
    if (!document.fullscreenElement) {
      tableRef.current?.requestFullscreen();
      setIsFullscreen(true);
    } else {
      document.exitFullscreen();
      setIsFullscreen(false);
    }
  }, []);

  // 监听全屏状态变化
  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
    };
    
    document.addEventListener('fullscreenchange', handleFullscreenChange);
    return () => document.removeEventListener('fullscreenchange', handleFullscreenChange);
  }, []);

  // 处理搜索
  const handleSearch = useCallback((value: string) => {
    setSearchValue(value);
    onSearch?.(value);
  }, [onSearch]);

  // 处理过滤
  const handleFilter = useCallback((field: string, value: any) => {
    const newFilters = { ...filters, [field]: value };
    setFilters(newFilters);
    onFilter?.(newFilters);
  }, [filters, onFilter]);

  // 处理排序
  const handleSort = useCallback((sorter: any) => {
    const { field, order } = sorter || {};
    const newSortConfig = field && order ? { field, order } : null;
    setSortConfig(newSortConfig);
    onSort?.(field, order);
  }, [onSort]);

  // 处理列配置变化
  const handleColumnConfigChange = useCallback((newColumns: DataTableColumn<T>[]) => {
    setColumnsConfig(newColumns);
    onColumnConfigChange?.(newColumns);
  }, [onColumnConfigChange]);

  // 处理导出
  const handleExport = useCallback(() => {
    const exportColumns = visibleColumns.filter(col => col.exportable !== false);
    exportData(processedData, exportColumns);
    onExport?.(processedData, exportColumns);
  }, [processedData, visibleColumns, exportData, onExport]);

  // 转换列配置
  const antdColumns: ColumnsType<T> = useMemo(() => {
    return visibleColumns.map((col) => {
      const antdCol: any = {
        key: col.key,
        title: col.headerRenderer ? col.headerRenderer() : col.title,
        dataIndex: col.dataIndex || col.key,
        width: col.width,
        fixed: col.pinned,
        sorter: sortable && col.sortable !== false,
        render: col.cellRenderer || col.render,
        ellipsis: true,
      };

      // 添加过滤功能
      if (filterable && col.filterable) {
        if (col.filterType === 'select' && col.filterOptions) {
          antdCol.filters = col.filterOptions.map(option => ({
            text: option.label,
            value: option.value,
          }));
          antdCol.onFilter = (value: any, record: T) => {
            const fieldValue = record[col.dataIndex || col.key];
            return fieldValue === value;
          };
        } else {
          antdCol.filterDropdown = ({ setSelectedKeys, selectedKeys, confirm, clearFilters }: any) => (
            <div style={{ padding: 8 }}>
              <Input
                placeholder={`Search ${col.title}`}
                value={selectedKeys[0]}
                onChange={(e) => setSelectedKeys(e.target.value ? [e.target.value] : [])}
                onPressEnter={() => confirm()}
                style={{ marginBottom: 8, display: 'block' }}
              />
              <Space>
                <Button
                  type="primary"
                  onClick={() => confirm()}
                  icon={<SearchOutlined />}
                  
                  style={{ width: 90 }}
                >
                  Search
                </Button>
                <Button onClick={clearFilters}  style={{ width: 90 }}>
                  Reset
                </Button>
              </Space>
            </div>
          );
          antdCol.filterIcon = (filtered: boolean) => (
            <SearchOutlined style={{ color: filtered ? '#1890ff' : undefined }} />
          );
          antdCol.onFilter = (value: any, record: T) => {
            const fieldValue = record[col.dataIndex || col.key];
            return fieldValue?.toString().toLowerCase().includes(value.toLowerCase());
          };
        }
      }

      return antdCol;
    });
  }, [visibleColumns, sortable, filterable]);

  // 工具栏配置
  const toolbarItems = useMemo(() => {
    const items = [];

    if (searchable) {
      items.push(
        <Input
          key="search"
          placeholder="Search..."
          prefix={<SearchOutlined />}
          value={searchValue}
          onChange={(e) => handleSearch(e.target.value)}
          style={{ width: 200 }}
          allowClear
        />
      );
    }

    if (densitySelector) {
      items.push(
        <Select
          key="density"
          value={tableSize}
          onChange={setTableSize}
          style={{ width: 100 }}
          options={[
            { label: 'Large', value: 'large' },
            { label: 'Middle', value: 'middle' },
            { label: 'Small', value: 'small' },
          ]}
        />
      );
    }

    if (onRefresh) {
      items.push(
        <Tooltip key="refresh" title="Refresh">
          <Button icon={<ReloadOutlined />} onClick={onRefresh} />
        </Tooltip>
      );
    }

    if (exportable) {
      items.push(
        <Tooltip key="export" title="Export">
          <Button icon={<DownloadOutlined />} onClick={handleExport} />
        </Tooltip>
      );
    }

    if (fullscreen) {
      items.push(
        <Tooltip key="fullscreen" title={isFullscreen ? 'Exit Fullscreen' : 'Fullscreen'}>
          <Button
            icon={isFullscreen ? <FullscreenExitOutlined /> : <FullscreenOutlined />}
            onClick={toggleFullscreen}
          />
        </Tooltip>
      );
    }

    return items;
  }, [
    searchable,
    densitySelector,
    exportable,
    fullscreen,
    searchValue,
    tableSize,
    isFullscreen,
    handleSearch,
    handleExport,
    onRefresh,
    toggleFullscreen,
  ]);

  // 列选择器
  const columnSelector = useMemo(() => {
    if (!columnSelector) return null;

    const items = columnsConfig.map((col) => ({
      key: col.key,
      label: (
        <Checkbox
          checked={!col.hidden}
          onChange={(e) => {
            const newColumns = columnsConfig.map(c =>
              c.key === col.key ? { ...c, hidden: !e.target.checked } : c
            );
            handleColumnConfigChange(newColumns);
          }}
        >
          {col.title}
        </Checkbox>
      ),
    }));

    return (
      <Dropdown
        menu={{
          items: [
            ...items,
            { type: 'divider' },
            {
              key: 'reset',
              label: (
                <Button type="link" onClick={() => handleColumnConfigChange(propColumns)}>
                  Reset Columns
                </Button>
              ),
            },
          ],
        }}
        trigger={['click']}
      >
        <Button icon={<SettingOutlined />}>Columns</Button>
      </Dropdown>
    );
  }, [columnsConfig, columnSelector, handleColumnConfigChange, propColumns]);

  // 决定使用哪种表格组件
  const TableComponent = virtual || (dataSource.length > virtualThreshold) ? VirtualTable : Table;

  // 表格属性
  const tableProps: TableProps<T> = {
    columns: antdColumns,
    dataSource: processedData,
    loading: loading || isProcessing,
    pagination,
    rowSelection: rowSelection ? {
      ...rowSelection,
      selectedRowKeys: selectedRowKeys,
    } : undefined,
    size: tableSize,
    bordered,
    showHeader,
    tableLayout,
    onRow,
    onHeaderRow,
    onChange: (paginationInfo, filtersInfo, sorter) => {
      handleSort(Array.isArray(sorter) ? sorter[0] : sorter);
    },
    title: title ? () => title : undefined,
    footer: footer ? () => footer : undefined,
    summary,
    expandedRowRender,
    expandIcon,
    expandRowByClick,
    rowKey,
    showSorterTooltip,
    locale,
    scroll: scroll || (virtual ? { y: 400 } : undefined),
    ...restProps,
  };

  return (
    <div
      ref={tableRef}
      className={classNames(
        'data-table',
        {
          'data-table-fullscreen': isFullscreen,
          'data-table-bordered': bordered,
        },
        className
      )}
      style={style}
    >
      {/* 工具栏 */}
      {toolbar && (toolbarItems.length > 0 || columnSelector) && (
        <div className="data-table-toolbar" style={{ marginBottom: 16 }}>
          <Space wrap>
            {toolbarItems}
            {columnSelector}
          </Space>
        </div>
      )}

      {/* 表格操作栏 */}
      {rowSelection && selectedRowKeys.length > 0 && (
        <TableActions
          selectedCount={selectedRowKeys.length}
          totalCount={dataSource.length}
          onClearSelection={() => rowSelection.onChange([], [])}
        />
      )}

      {/* 表格主体 */}
      <div className="data-table-container">
        {dataSource.length === 0 && !loading ? (
          <Empty description="No data" />
        ) : (
          <TableComponent {...tableProps} />
        )}
      </div>

      {/* 自定义样式 */}
      <style jsx>{`
        .data-table {
          position: relative;
          background: white;
          border-radius: 6px;
        }
        
        .data-table-fullscreen {
          position: fixed;
          top: 0;
          left: 0;
          right: 0;
          bottom: 0;
          z-index: 9999;
          background: white;
          padding: 24px;
        }
        
        .data-table-bordered {
          border: 1px solid #f0f0f0;
        }
        
        .data-table-toolbar {
          display: flex;
          justify-content: space-between;
          align-items: center;
          flex-wrap: wrap;
          gap: 8px;
        }
        
        .data-table-container {
          overflow: auto;
        }
        
        .ant-table-thead > tr > th {
          position: sticky;
          top: 0;
          z-index: 1;
        }
        
        .ant-table-pagination {
          margin: 16px 0 0 0;
          text-align: right;
        }
        
        /* 响应式设计 */
        @media (max-width: 768px) {
          .data-table-toolbar {
            flex-direction: column;
            align-items: stretch;
          }
          
          .ant-table-pagination {
            text-align: center;
          }
        }
      `}</style>
    </div>
  );
};

export default DataTable;
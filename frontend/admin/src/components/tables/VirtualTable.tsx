/* eslint-disable react-refresh/only-export-components */
import React, { useMemo, useCallback, useRef, useState } from 'react';
import { Table, Empty, Spin } from 'antd';
import type { TableProps, ColumnsType } from 'antd/es/table';
import { List } from 'react-window';
import type { ListChildComponentProps } from 'react-window';
import ResizeObserver from 'rc-resize-observer';
import classNames from 'classnames';

// 虚拟滚动表格行高配置
interface VirtualTableProps<T = Record<string, unknown>> extends Omit<TableProps<T>, 'pagination'> {
  itemHeight?: number; // 固定行高
  estimatedItemHeight?: number; // 预估行高（动态高度时使用）
  overscan?: number; // 预渲染行数
  threshold?: number; // 启用虚拟滚动的最小行数
  maxHeight?: number; // 表格最大高度
  onScroll?: (scrollTop: number) => void;
  onReachEnd?: () => void; // 滚动到底部时触发
  enableInfiniteLoading?: boolean; // 是否启用无限加载
  loadingMore?: boolean; // 是否正在加载更多
}

// 虚拟滚动表格行组件
const VirtualTableRow: React.FC<{
  index: number;
  style: React.CSSProperties;
  data: {
    columns: ColumnsType<Record<string, unknown>>;
    dataSource: Record<string, unknown>[];
    rowKey: string | ((record: Record<string, unknown>) => string);
    onRow?: (record: Record<string, unknown>, index?: number) => React.HTMLAttributes<HTMLElement>;
    rowClassName?: string | ((record: Record<string, unknown>, index: number) => string);
    rowSelection?: Record<string, unknown>;
    expandedRowRender?: (record: Record<string, unknown>, index: number, indent: number, expanded: boolean) => React.ReactNode;
    expandRowByClick?: boolean;
  };
}> = ({ index, style, data }) => {
  const {
    columns,
    dataSource,
    rowKey,
    onRow,
    rowClassName,
    rowSelection,
    expandedRowRender,
  } = data;

  const record = dataSource[index];
  if (!record) return null;

  const key = typeof rowKey === 'function' ? rowKey(record) : record[rowKey];
  const rowProps = onRow?.(record, index) || {};
  const className = typeof rowClassName === 'function' ? rowClassName(record, index) : rowClassName;

  return (
    <div
      style={style}
      className={classNames('virtual-table-row', className)}
      {...rowProps}
    >
      <div className="virtual-table-row-content">
        {/* 选择框 */}
        {rowSelection && (
          <div className="virtual-table-cell virtual-table-selection-cell">
            <input
              type={rowSelection.type || 'checkbox'}
              checked={rowSelection.selectedRowKeys?.includes(key)}
              onChange={(e) => {
                if (rowSelection.onSelect) {
                  rowSelection.onSelect(record, e.target.checked, [], e.nativeEvent);
                }
              }}
            />
          </div>
        )}

        {/* 数据列 */}
        {columns.map((col: Record<string, unknown>, colIndex) => {
          const value = record[col.dataIndex] || record[col.key];
          const cellContent = col.render 
            ? col.render(value, record, index)
            : value;

          return (
            <div
              key={col.key || colIndex}
              className="virtual-table-cell"
              style={{
                width: col.width || 'auto',
                minWidth: col.minWidth || 50,
                textAlign: col.align || 'left',
              }}
              title={typeof cellContent === 'string' ? cellContent : undefined}
            >
              {cellContent}
            </div>
          );
        })}
      </div>

      {/* 展开行 */}
      {expandedRowRender && (
        <div className="virtual-table-expanded-row">
          {expandedRowRender(record, index, 0, true)}
        </div>
      )}
    </div>
  );
};

// 虚拟滚动表格头组件
const VirtualTableHeader: React.FC<{
  columns: ColumnsType<Record<string, unknown>>;
  rowSelection?: Record<string, unknown>;
}> = ({ columns, rowSelection }) => {
  return (
    <div className="virtual-table-header">
      {/* 选择框列头 */}
      {rowSelection && (
        <div className="virtual-table-header-cell virtual-table-selection-cell">
          {rowSelection.type !== 'radio' && !rowSelection.hideSelectAll && (
            <input
              type="checkbox"
              checked={
                rowSelection.selectedRowKeys?.length === rowSelection.dataSource?.length &&
                rowSelection.selectedRowKeys?.length > 0
              }
              {...(
                rowSelection.selectedRowKeys?.length > 0 &&
                rowSelection.selectedRowKeys?.length < rowSelection.dataSource?.length
                  ? { ref: (input: HTMLInputElement | null) => {
                      if (input) input.indeterminate = true;
                    }}
                  : {}
              )}
              onChange={(e) => {
                if (rowSelection.onSelectAll) {
                  rowSelection.onSelectAll(e.target.checked, [], []);
                }
              }}
            />
          )}
        </div>
      )}

      {/* 数据列头 */}
      {columns.map((col: Record<string, unknown>, index) => (
        <div
          key={col.key || index}
          className={classNames('virtual-table-header-cell', {
            'virtual-table-header-sortable': col.sorter,
          })}
          style={{
            width: col.width || 'auto',
            minWidth: col.minWidth || 50,
            textAlign: col.align || 'left',
          }}
        >
          <div className="virtual-table-header-content">
            {col.title}
            {col.sorter && (
              <div className="virtual-table-sort-icon">
                ↕
              </div>
            )}
          </div>
        </div>
      ))}
    </div>
  );
};

export const VirtualTable = <T extends Record<string, unknown>>(props: VirtualTableProps<T>) => {
  const {
    dataSource = [],
    columns = [],
    loading = false,
    itemHeight = 54,
  
    overscan = 5,
    threshold = 100,
    maxHeight = 600,
    scroll,
    rowKey = 'id',
    onRow,
    rowClassName,
    rowSelection,
    expandedRowRender,
    expandRowByClick,
    onScroll,
    onReachEnd,
    enableInfiniteLoading = false,
    loadingMore = false,
    ...restProps
  } = props;

  const [containerHeight, setContainerHeight] = useState(maxHeight);

  const listRef = useRef<List>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  // 计算行高（支持动态高度）
  const getItemHeight = useCallback((_index: number) => {
    // 可以根据具体数据计算动态高度
    // 这里简化为固定高度
    return itemHeight;
  }, [itemHeight]);

  // 滚动事件处理
  const handleScroll = useCallback(({ scrollTop: newScrollTop }: { scrollTop: number }) => {

    onScroll?.(newScrollTop);

    // 无限加载逻辑
    if (enableInfiniteLoading && onReachEnd && !loadingMore) {
      const scrollHeight = dataSource.length * itemHeight;
      const containerHeight = listRef.current?.props.height || 0;
      const scrollBottom = newScrollTop + containerHeight;
      
      // 距离底部50px时触发加载
      if (scrollHeight - scrollBottom < 50) {
        onReachEnd();
      }
    }
  }, [onScroll, onReachEnd, enableInfiniteLoading, loadingMore, dataSource.length, itemHeight]);

  // 容器尺寸变化监听
  const handleResize = useCallback(({ height }: { height: number }) => {
    const newHeight = Math.min(height, maxHeight);
    setContainerHeight(newHeight);
  }, [maxHeight]);



  // 准备传递给行组件的数据
  const rowData = useMemo(() => ({
    columns,
    dataSource,
    rowKey,
    onRow,
    rowClassName,
    rowSelection: rowSelection ? {
      ...rowSelection,
      dataSource,
    } : undefined,
    expandedRowRender,
    expandRowByClick,
  }), [columns, dataSource, rowKey, onRow, rowClassName, rowSelection, expandedRowRender, expandRowByClick]);

  // 渲染虚拟行
  const VirtualRow = useCallback(({ index, style }: ListChildComponentProps) => (
    <VirtualTableRow
      index={index}
      style={style}
      data={rowData}
    />
  ), [rowData]);

  // 如果数据量小于阈值，使用原生Table组件
  if (dataSource.length < threshold) {
    return (
      <Table
        {...restProps}
        dataSource={dataSource}
        columns={columns}
        loading={loading}
        rowKey={rowKey}
        onRow={onRow}
        rowClassName={rowClassName}
        rowSelection={rowSelection}
        expandedRowRender={expandedRowRender}
        expandRowByClick={expandRowByClick}
        pagination={false}
        scroll={scroll}
      />
    );
  }

  if (loading && dataSource.length === 0) {
    return (
      <div className="virtual-table-loading">
        <Spin size="large" />
      </div>
    );
  }

  if (dataSource.length === 0) {
    return <Empty description="No data" />;
  }

  return (
    <div className="virtual-table-container" ref={containerRef}>
      {/* 表头 */}
      <VirtualTableHeader columns={columns} rowSelection={rowSelection} />

      {/* 虚拟滚动内容 */}
      <ResizeObserver onResize={handleResize}>
        <div className="virtual-table-body" style={{ height: containerHeight }}>
          <List
            ref={listRef}
            height={containerHeight}
            itemCount={dataSource.length}
            itemSize={getItemHeight}
            overscanCount={overscan}
            onScroll={handleScroll}
            itemData={rowData}
            width="100%"
          >
            {VirtualRow}
          </List>
        </div>
      </ResizeObserver>

      {/* 加载更多指示器 */}
      {enableInfiniteLoading && loadingMore && (
        <div className="virtual-table-loading-more">
          <Spin  />
          <span style={{ marginLeft: 8 }}>Loading more...</span>
        </div>
      )}

      <style>{`
        .virtual-table-container {
          border: 1px solid #f0f0f0;
          border-radius: 6px;
          overflow: hidden;
          background: white;
        }

        .virtual-table-header {
          display: flex;
          background: #fafafa;
          border-bottom: 1px solid #f0f0f0;
          font-weight: 600;
          position: sticky;
          top: 0;
          z-index: 10;
        }

        .virtual-table-header-cell {
          padding: 16px;
          border-right: 1px solid #f0f0f0;
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
          min-height: 54px;
          display: flex;
          align-items: center;
        }

        .virtual-table-header-cell:last-child {
          border-right: none;
        }

        .virtual-table-header-sortable {
          cursor: pointer;
          user-select: none;
        }

        .virtual-table-header-sortable:hover {
          background: #f5f5f5;
        }

        .virtual-table-header-content {
          display: flex;
          align-items: center;
          justify-content: space-between;
          width: 100%;
        }

        .virtual-table-sort-icon {
          margin-left: 4px;
          opacity: 0.5;
        }

        .virtual-table-selection-cell {
          width: 60px;
          min-width: 60px;
          text-align: center;
        }

        .virtual-table-body {
          overflow: auto;
        }

        .virtual-table-row {
          border-bottom: 1px solid #f0f0f0;
          transition: background-color 0.2s;
        }

        .virtual-table-row:hover {
          background: #fafafa;
        }

        .virtual-table-row-content {
          display: flex;
          align-items: center;
          min-height: 54px;
        }

        .virtual-table-cell {
          padding: 16px;
          border-right: 1px solid #f0f0f0;
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
          display: flex;
          align-items: center;
        }

        .virtual-table-cell:last-child {
          border-right: none;
        }

        .virtual-table-expanded-row {
          padding: 16px;
          background: #fafafa;
          border-top: 1px solid #f0f0f0;
        }

        .virtual-table-loading {
          display: flex;
          justify-content: center;
          align-items: center;
          height: 200px;
        }

        .virtual-table-loading-more {
          display: flex;
          justify-content: center;
          align-items: center;
          padding: 16px;
          border-top: 1px solid #f0f0f0;
          background: #fafafa;
        }

        /* 响应式设计 */
        @media (max-width: 768px) {
          .virtual-table-header-cell,
          .virtual-table-cell {
            padding: 8px;
            font-size: 12px;
          }

          .virtual-table-row-content {
            min-height: 48px;
          }
        }

        /* 滚动条样式 */
        .virtual-table-body::-webkit-scrollbar {
          width: 8px;
          height: 8px;
        }

        .virtual-table-body::-webkit-scrollbar-track {
          background: #f0f0f0;
          border-radius: 4px;
        }

        .virtual-table-body::-webkit-scrollbar-thumb {
          background: #d9d9d9;
          border-radius: 4px;
        }

        .virtual-table-body::-webkit-scrollbar-thumb:hover {
          background: #bfbfbf;
        }

        /* 选中状态 */
        .virtual-table-row.selected {
          background: #e6f7ff;
        }

        .virtual-table-row.selected:hover {
          background: #bae7ff;
        }

        /* 加载状态 */
        .virtual-table-row.loading {
          opacity: 0.6;
          pointer-events: none;
        }

        /* 错误状态 */
        .virtual-table-row.error {
          background: #fff2f0;
          color: #ff4d4f;
        }

        /* 禁用状态 */
        .virtual-table-row.disabled {
          opacity: 0.5;
          pointer-events: none;
        }
      `}</style>
    </div>
  );
};

// 导出相关工具函数和类型
export type { VirtualTableProps };

// 性能优化工具函数
export const VirtualTableUtils = {
  // 计算可见行范围
  getVisibleRowRange: (scrollTop: number, containerHeight: number, itemHeight: number) => {
    const startIndex = Math.floor(scrollTop / itemHeight);
    const endIndex = Math.min(
      startIndex + Math.ceil(containerHeight / itemHeight),
      Number.MAX_SAFE_INTEGER
    );
    return { startIndex, endIndex };
  },

  // 计算总高度
  getTotalHeight: (itemCount: number, itemHeight: number) => {
    return itemCount * itemHeight;
  },

  // 优化数据处理
  memoizeRowData: function<T>(data: T[], keyExtractor: (item: T) => string) {
    const cache = new Map();
    return data.map(item => {
      const key = keyExtractor(item);
      if (!cache.has(key)) {
        cache.set(key, { ...item, _key: key });
      }
      return cache.get(key);
    });
  }
};

export default VirtualTable;
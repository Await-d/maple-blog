// 高性能数据表格组件库
// High-Performance Data Table Components Library

// 主要组件导出
export { DataTable, type DataTableProps, type DataTableColumn } from './DataTable';
export { VirtualTable, type VirtualTableProps, VirtualTableUtils } from './VirtualTable';
export { TableActions, type TableActionsProps, type BatchActionConfig, withTableActions } from './TableActions';

// Hook 导出
export { useDataTable, type UseDataTableOptions, type UseDataTableResult } from '../../hooks/useDataTable';

// 预设配置和工具
export const TablePresets = {
  // 用户管理表格列配置
  userTableColumns: [
    {
      key: 'id',
      title: 'ID',
      dataIndex: 'id',
      width: 80,
      sortable: true,
      filterable: false,
      exportable: true,
    },
    {
      key: 'username',
      title: 'Username',
      dataIndex: 'username',
      width: 120,
      sortable: true,
      searchable: true,
      exportable: true,
    },
    {
      key: 'email',
      title: 'Email',
      dataIndex: 'email',
      width: 200,
      sortable: true,
      searchable: true,
      exportable: true,
    },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      width: 100,
      sortable: true,
      filterable: true,
      filterType: 'select',
      filterOptions: [
        { label: 'Active', value: 'active' },
        { label: 'Inactive', value: 'inactive' },
        { label: 'Banned', value: 'banned' },
      ],
      exportable: true,
    },
    {
      key: 'createdAt',
      title: 'Created At',
      dataIndex: 'createdAt',
      width: 150,
      sortable: true,
      sortType: 'date',
      exportable: true,
    },
    {
      key: 'actions',
      title: 'Actions',
      width: 120,
      fixed: 'right',
      exportable: false,
    },
  ],

  // 文章管理表格列配置
  postTableColumns: [
    {
      key: 'id',
      title: 'ID',
      dataIndex: 'id',
      width: 80,
      sortable: true,
      exportable: true,
    },
    {
      key: 'title',
      title: 'Title',
      dataIndex: 'title',
      width: 250,
      sortable: true,
      searchable: true,
      exportable: true,
    },
    {
      key: 'author',
      title: 'Author',
      dataIndex: 'author',
      width: 120,
      sortable: true,
      filterable: true,
      exportable: true,
    },
    {
      key: 'status',
      title: 'Status',
      dataIndex: 'status',
      width: 100,
      sortable: true,
      filterable: true,
      filterType: 'select',
      filterOptions: [
        { label: 'Draft', value: 'draft' },
        { label: 'Published', value: 'published' },
        { label: 'Archived', value: 'archived' },
      ],
      exportable: true,
    },
    {
      key: 'viewCount',
      title: 'Views',
      dataIndex: 'viewCount',
      width: 100,
      sortable: true,
      sortType: 'number',
      exportable: true,
    },
    {
      key: 'publishedAt',
      title: 'Published At',
      dataIndex: 'publishedAt',
      width: 150,
      sortable: true,
      sortType: 'date',
      exportable: true,
    },
    {
      key: 'actions',
      title: 'Actions',
      width: 120,
      fixed: 'right',
      exportable: false,
    },
  ],

  // 系统日志表格列配置
  auditLogColumns: [
    {
      key: 'id',
      title: 'ID',
      dataIndex: 'id',
      width: 80,
      sortable: true,
      exportable: true,
    },
    {
      key: 'action',
      title: 'Action',
      dataIndex: 'action',
      width: 120,
      sortable: true,
      filterable: true,
      searchable: true,
      exportable: true,
    },
    {
      key: 'user',
      title: 'User',
      dataIndex: 'user',
      width: 120,
      sortable: true,
      filterable: true,
      exportable: true,
    },
    {
      key: 'entityType',
      title: 'Entity Type',
      dataIndex: 'entityType',
      width: 120,
      sortable: true,
      filterable: true,
      exportable: true,
    },
    {
      key: 'ipAddress',
      title: 'IP Address',
      dataIndex: 'ipAddress',
      width: 130,
      sortable: true,
      searchable: true,
      exportable: true,
    },
    {
      key: 'createdAt',
      title: 'Created At',
      dataIndex: 'createdAt',
      width: 150,
      sortable: true,
      sortType: 'date',
      exportable: true,
    },
  ],
};

// 批量操作预设
export const BatchActionPresets = {
  // 用户管理批量操作
  userBatchActions: [
    {
      key: 'activate',
      label: 'Activate Users',
      icon: '🟢',
      needConfirm: true,
      confirmTitle: 'Activate Selected Users',
      confirmContent: 'Are you sure you want to activate the selected users?',
    },
    {
      key: 'deactivate',
      label: 'Deactivate Users',
      icon: '🔴',
      needConfirm: true,
      confirmTitle: 'Deactivate Selected Users',
      confirmContent: 'Are you sure you want to deactivate the selected users?',
    },
    {
      key: 'ban',
      label: 'Ban Users',
      icon: '🚫',
      danger: true,
      needConfirm: true,
      confirmTitle: 'Ban Selected Users',
      confirmContent: 'Are you sure you want to ban the selected users? This action requires careful consideration.',
    },
    {
      key: 'sendEmail',
      label: 'Send Email',
      icon: '📧',
      requireInput: true,
      inputType: 'textarea',
      inputPlaceholder: 'Enter email message...',
    },
  ],

  // 内容管理批量操作
  contentBatchActions: [
    {
      key: 'publish',
      label: 'Publish Posts',
      icon: '📤',
      needConfirm: true,
      confirmTitle: 'Publish Selected Posts',
      confirmContent: 'Are you sure you want to publish the selected posts?',
    },
    {
      key: 'unpublish',
      label: 'Unpublish Posts',
      icon: '📥',
      needConfirm: true,
      confirmTitle: 'Unpublish Selected Posts',
      confirmContent: 'Are you sure you want to unpublish the selected posts?',
    },
    {
      key: 'archive',
      label: 'Archive Posts',
      icon: '📦',
      needConfirm: true,
      confirmTitle: 'Archive Selected Posts',
      confirmContent: 'Are you sure you want to archive the selected posts?',
    },
    {
      key: 'categorize',
      label: 'Set Category',
      icon: '🏷️',
      requireInput: true,
      inputType: 'select',
      inputPlaceholder: 'Select category...',
    },
  ],
};

// 性能配置预设
export const PerformancePresets = {
  // 小数据集配置 (< 1000 条)
  small: {
    virtual: false,
    virtualThreshold: 1000,
    debounceMs: 150,
    enableCache: true,
    itemHeight: 54,
    overscan: 5,
  },

  // 中等数据集配置 (1000 - 10000 条)
  medium: {
    virtual: true,
    virtualThreshold: 1000,
    debounceMs: 200,
    enableCache: true,
    itemHeight: 54,
    overscan: 10,
  },

  // 大数据集配置 (10000 - 100000 条)
  large: {
    virtual: true,
    virtualThreshold: 500,
    debounceMs: 300,
    enableCache: true,
    itemHeight: 48,
    overscan: 15,
  },

  // 超大数据集配置 (> 100000 条)
  extraLarge: {
    virtual: true,
    virtualThreshold: 100,
    debounceMs: 500,
    enableCache: true,
    itemHeight: 40,
    overscan: 20,
    enableInfiniteLoading: true,
  },
};

// 实用工具函数
export const TableUtils = {
  // 生成测试数据
  generateMockData: (count: number = 1000) => {
    const data = [];
    const statuses = ['active', 'inactive', 'banned'];
    const names = ['Alice', 'Bob', 'Charlie', 'Diana', 'Eve', 'Frank', 'Grace', 'Henry'];
    
    for (let i = 1; i <= count; i++) {
      data.push({
        id: i,
        username: `user${i}`,
        email: `user${i}@example.com`,
        displayName: names[i % names.length] + ` ${i}`,
        status: statuses[i % statuses.length],
        createdAt: new Date(Date.now() - Math.random() * 365 * 24 * 60 * 60 * 1000).toISOString(),
        lastLoginAt: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString(),
        viewCount: Math.floor(Math.random() * 10000),
        score: Math.floor(Math.random() * 100),
      });
    }
    
    return data;
  },

  // 获取性能配置
  getPerformanceConfig: (dataLength: number) => {
    if (dataLength < 1000) return PerformancePresets.small;
    if (dataLength < 10000) return PerformancePresets.medium;
    if (dataLength < 100000) return PerformancePresets.large;
    return PerformancePresets.extraLarge;
  },

  // 格式化数据大小
  formatDataSize: (count: number): string => {
    if (count < 1000) return `${count} rows`;
    if (count < 1000000) return `${(count / 1000).toFixed(1)}K rows`;
    return `${(count / 1000000).toFixed(1)}M rows`;
  },

  // 性能基准测试
  performanceBenchmark: (dataLength: number) => {
    const start = performance.now();
    
    // 模拟数据处理
    const mockProcessing = () => {
      const data = Array.from({ length: dataLength }, (_, i) => ({ id: i, value: Math.random() }));
      return data.filter(item => item.value > 0.5).sort((a, b) => a.value - b.value);
    };
    
    const result = mockProcessing();
    const end = performance.now();
    
    return {
      processTime: Math.round((end - start) * 100) / 100,
      processedCount: result.length,
      originalCount: dataLength,
      performance: end - start < 16 ? 'excellent' : end - start < 100 ? 'good' : 'needs optimization',
    };
  },
};

// TypeScript 类型导出
export type {
  SortConfig,
  FilterConfig,
  SelectionConfig,
} from '../../hooks/useDataTable';

// 默认导出
export default {
  DataTable,
  VirtualTable,
  TableActions,
  useDataTable,
  TablePresets,
  BatchActionPresets,
  PerformancePresets,
  TableUtils,
};
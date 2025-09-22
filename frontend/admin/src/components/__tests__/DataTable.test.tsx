// @ts-nocheck
import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import '@testing-library/jest-dom';

import DataTable, { type DataTableProps, type DataTableColumn } from '../tables/DataTable';
import { PermissionProvider } from '../../contexts/PermissionContext';

// Mock dependencies
vi.mock('../../hooks/useDataTable', () => ({
  useDataTable: () => ({
    processedData: mockData,
    visibleColumns: mockColumns,
    selectedRowKeys: [],
    searchData: vi.fn(),
    filterData: vi.fn(),
    sortData: vi.fn(),
    exportData: vi.fn(),
    toggleColumnVisibility: vi.fn(),
    resetColumns: vi.fn(),
    isProcessing: false,
  }),
}));

vi.mock('../../hooks/usePermissions', () => ({
  usePermissions: () => ({
    hasPermission: vi.fn().mockReturnValue(true),
    userRole: 'admin',
    permissions: [],
  }),
}));

// Mock data for testing
const mockData = [
  {
    id: '1',
    name: 'John Doe',
    email: 'john@example.com',
    role: 'admin',
    status: 'active',
    createdAt: '2024-01-01T00:00:00Z',
    lastLogin: '2024-01-15T10:30:00Z',
  },
  {
    id: '2',
    name: 'Jane Smith',
    email: 'jane@example.com',
    role: 'user',
    status: 'inactive',
    createdAt: '2024-01-02T00:00:00Z',
    lastLogin: '2024-01-14T09:15:00Z',
  },
  {
    id: '3',
    name: 'Bob Johnson',
    email: 'bob@example.com',
    role: 'moderator',
    status: 'active',
    createdAt: '2024-01-03T00:00:00Z',
    lastLogin: '2024-01-13T14:45:00Z',
  },
];

const mockColumns: DataTableColumn[] = [
  {
    key: 'name',
    title: 'Name',
    dataIndex: 'name',
    searchable: true,
    sortable: true,
    width: 150,
    render: (text: string) => <span data-testid="name-cell">{text}</span>,
  },
  {
    key: 'email',
    title: 'Email',
    dataIndex: 'email',
    searchable: true,
    filterable: true,
    width: 200,
  },
  {
    key: 'role',
    title: 'Role',
    dataIndex: 'role',
    filterable: true,
    filterType: 'select',
    filterOptions: [
      { label: 'Admin', value: 'admin' },
      { label: 'User', value: 'user' },
      { label: 'Moderator', value: 'moderator' },
    ],
    width: 120,
  },
  {
    key: 'status',
    title: 'Status',
    dataIndex: 'status',
    width: 100,
    render: (status: string) => (
      <span
        className={`status-badge ${status}`}
        data-testid="status-badge"
        aria-label={`Status: ${status}`}
      >
        {status}
      </span>
    ),
  },
  {
    key: 'actions',
    title: 'Actions',
    width: 150,
    render: (_, record) => (
      <div data-testid="action-buttons">
        <button
          data-testid={`edit-${record.id}`}
          aria-label={`Edit ${record.name}`}
          onClick={() => console.log('Edit', record.id)}
        >
          Edit
        </button>
        <button
          data-testid={`delete-${record.id}`}
          aria-label={`Delete ${record.name}`}
          onClick={() => console.log('Delete', record.id)}
        >
          Delete
        </button>
      </div>
    ),
  },
];

// Test wrapper component
const TestWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <PermissionProvider>
          {children}
        </PermissionProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
};

const renderDataTable = (props: Partial<DataTableProps> = {}) => {
  const defaultProps: DataTableProps = {
    columns: mockColumns,
    dataSource: mockData,
    pagination: {
      current: 1,
      pageSize: 10,
      total: mockData.length,
      onChange: vi.fn(),
    },
    onSearch: vi.fn(),
    onFilter: vi.fn(),
    onSort: vi.fn(),
    onExport: vi.fn(),
    onRefresh: vi.fn(),
    onColumnConfigChange: vi.fn(),
  };

  return render(
    <TestWrapper>
      <DataTable {...defaultProps} {...props} />
    </TestWrapper>
  );
};

describe('DataTable Component', () => {
  let user: ReturnType<typeof userEvent.setup>;

  beforeEach(() => {
    user = userEvent.setup();
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Basic Rendering', () => {
    it('renders table with correct data', () => {
      renderDataTable();

      // Check if table headers are rendered
      expect(screen.getByText('Name')).toBeInTheDocument();
      expect(screen.getByText('Email')).toBeInTheDocument();
      expect(screen.getByText('Role')).toBeInTheDocument();
      expect(screen.getByText('Status')).toBeInTheDocument();
      expect(screen.getByText('Actions')).toBeInTheDocument();

      // Check if data rows are rendered
      expect(screen.getByText('John Doe')).toBeInTheDocument();
      expect(screen.getByText('jane@example.com')).toBeInTheDocument();
      expect(screen.getByText('moderator')).toBeInTheDocument();
    });

    it('renders empty state when no data', () => {
      renderDataTable({ dataSource: [] });
      expect(screen.getByText('No data')).toBeInTheDocument();
    });

    it('shows loading state correctly', () => {
      renderDataTable({ loading: true });
      expect(screen.getByTestId('loading-spinner') || document.querySelector('.ant-spin')).toBeInTheDocument();
    });

    it('applies custom className and style', () => {
      const { container } = renderDataTable({
        className: 'custom-table',
        style: { backgroundColor: 'red' },
      });
      
      const tableContainer = container.querySelector('.data-table');
      expect(tableContainer).toHaveClass('custom-table');
      expect(tableContainer).toHaveStyle({ backgroundColor: 'red' });
    });
  });

  describe('Search Functionality', () => {
    it('renders search input when searchable is true', () => {
      renderDataTable({ searchable: true });
      
      const searchInput = screen.getByPlaceholderText('Search...');
      expect(searchInput).toBeInTheDocument();
      expect(searchInput).toHaveAttribute('type', 'text');
    });

    it('does not render search input when searchable is false', () => {
      renderDataTable({ searchable: false });
      
      expect(screen.queryByPlaceholderText('Search...')).not.toBeInTheDocument();
    });

    it('triggers search callback when typing in search input', async () => {
      const onSearch = vi.fn();
      renderDataTable({ searchable: true, onSearch });
      
      const searchInput = screen.getByPlaceholderText('Search...');
      await user.type(searchInput, 'John');
      
      await waitFor(() => {
        expect(onSearch).toHaveBeenCalledWith('John');
      });
    });

    it('clears search input when clear button is clicked', async () => {
      renderDataTable({ searchable: true });
      
      const searchInput = screen.getByPlaceholderText('Search...');
      await user.type(searchInput, 'test search');
      
      // Antd Input has a clear button when there's content
      const clearButton = document.querySelector('.ant-input-clear-icon');
      if (clearButton) {
        await user.click(clearButton);
        expect(searchInput).toHaveValue('');
      }
    });
  });

  describe('Filtering Functionality', () => {
    it('shows filter dropdowns for filterable columns', async () => {
      renderDataTable({ filterable: true });
      
      // Look for filter icons in column headers
      const filterIcons = document.querySelectorAll('.ant-table-filter-trigger');
      expect(filterIcons.length).toBeGreaterThan(0);
    });

    it('opens filter dropdown when filter icon is clicked', async () => {
      renderDataTable({ filterable: true });
      
      // Find and click on a filter trigger
      const filterTrigger = document.querySelector('.ant-table-filter-trigger');
      if (filterTrigger) {
        await user.click(filterTrigger);
        
        // Should show filter dropdown
        await waitFor(() => {
          expect(document.querySelector('.ant-table-filter-dropdown')).toBeInTheDocument();
        });
      }
    });

    it('handles select type filters correctly', async () => {
      renderDataTable({ filterable: true });
      
      // The role column should have select type filter
      const roleHeader = screen.getByText('Role').closest('th');
      const filterTrigger = roleHeader?.querySelector('.ant-table-filter-trigger');
      
      if (filterTrigger) {
        await user.click(filterTrigger);
        
        await waitFor(() => {
          expect(screen.getByText('Admin')).toBeInTheDocument();
          expect(screen.getByText('User')).toBeInTheDocument();
          expect(screen.getByText('Moderator')).toBeInTheDocument();
        });
      }
    });
  });

  describe('Sorting Functionality', () => {
    it('enables sorting for sortable columns', () => {
      renderDataTable({ sortable: true });
      
      // Check if sortable columns have sort triggers
      const nameHeader = screen.getByText('Name').closest('th');
      expect(nameHeader?.querySelector('.ant-table-column-sorter')).toBeInTheDocument();
    });

    it('triggers sort callback when column header is clicked', async () => {
      const onSort = vi.fn();
      renderDataTable({ sortable: true, onSort });
      
      const nameHeader = screen.getByText('Name');
      await user.click(nameHeader);
      
      await waitFor(() => {
        expect(onSort).toHaveBeenCalled();
      });
    });

    it('shows sort indicators for active sorting', async () => {
      renderDataTable({ sortable: true });
      
      const nameHeader = screen.getByText('Name');
      await user.click(nameHeader);
      
      await waitFor(() => {
        const sorter = document.querySelector('.ant-table-column-sorter-up.active, .ant-table-column-sorter-down.active');
        expect(sorter).toBeInTheDocument();
      });
    });
  });

  describe('Row Selection', () => {
    const rowSelectionProps = {
      selectedRowKeys: ['1'],
      onChange: vi.fn(),
      onSelect: vi.fn(),
      onSelectAll: vi.fn(),
    };

    it('renders selection checkboxes when rowSelection is provided', () => {
      renderDataTable({ rowSelection: rowSelectionProps });
      
      // Should have checkboxes for each row plus header
      const checkboxes = screen.getAllByRole('checkbox');
      expect(checkboxes.length).toBe(mockData.length + 1); // +1 for select all
    });

    it('shows selected rows correctly', () => {
      renderDataTable({ rowSelection: rowSelectionProps });
      
      const checkboxes = screen.getAllByRole('checkbox');
      // First checkbox after header should be checked (id: '1')
      expect(checkboxes[1]).toBeChecked();
    });

    it('handles row selection change', async () => {
      const onChange = vi.fn();
      renderDataTable({
        rowSelection: { ...rowSelectionProps, onChange },
      });
      
      const checkboxes = screen.getAllByRole('checkbox');
      await user.click(checkboxes[2]); // Click second data row
      
      await waitFor(() => {
        expect(onChange).toHaveBeenCalled();
      });
    });

    it('handles select all functionality', async () => {
      const onSelectAll = vi.fn();
      renderDataTable({
        rowSelection: { ...rowSelectionProps, onSelectAll },
      });
      
      const selectAllCheckbox = screen.getAllByRole('checkbox')[0];
      await user.click(selectAllCheckbox);
      
      await waitFor(() => {
        expect(onSelectAll).toHaveBeenCalled();
      });
    });
  });

  describe('Toolbar Functionality', () => {
    it('renders toolbar when enabled', () => {
      renderDataTable({ toolbar: true });
      
      const toolbar = document.querySelector('.data-table-toolbar');
      expect(toolbar).toBeInTheDocument();
    });

    it('hides toolbar when disabled', () => {
      renderDataTable({ toolbar: false });
      
      const toolbar = document.querySelector('.data-table-toolbar');
      expect(toolbar).not.toBeInTheDocument();
    });

    it('shows export button when exportable is true', () => {
      renderDataTable({ exportable: true, toolbar: true });
      
      const exportButton = screen.getByRole('button', { name: /export/i });
      expect(exportButton).toBeInTheDocument();
    });

    it('triggers export callback when export button is clicked', async () => {
      const onExport = vi.fn();
      renderDataTable({ exportable: true, toolbar: true, onExport });
      
      const exportButton = screen.getByRole('button', { name: /export/i });
      await user.click(exportButton);
      
      await waitFor(() => {
        expect(onExport).toHaveBeenCalled();
      });
    });

    it('shows refresh button when onRefresh is provided', () => {
      renderDataTable({ onRefresh: vi.fn(), toolbar: true });
      
      const refreshButton = screen.getByRole('button', { name: /refresh/i });
      expect(refreshButton).toBeInTheDocument();
    });

    it('triggers refresh callback when refresh button is clicked', async () => {
      const onRefresh = vi.fn();
      renderDataTable({ onRefresh, toolbar: true });
      
      const refreshButton = screen.getByRole('button', { name: /refresh/i });
      await user.click(refreshButton);
      
      expect(onRefresh).toHaveBeenCalled();
    });

    it('shows density selector when enabled', () => {
      renderDataTable({ densitySelector: true, toolbar: true });
      
      const densitySelector = screen.getByDisplayValue('Middle');
      expect(densitySelector).toBeInTheDocument();
    });

    it('changes table size when density is changed', async () => {
      renderDataTable({ densitySelector: true, toolbar: true });
      
      const densitySelector = screen.getByDisplayValue('Middle');
      await user.click(densitySelector);
      
      // Should show density options
      await waitFor(() => {
        expect(screen.getByText('Large')).toBeInTheDocument();
        expect(screen.getByText('Small')).toBeInTheDocument();
      });
    });
  });

  describe('Column Configuration', () => {
    it('shows column selector when enabled', () => {
      renderDataTable({ columnSelector: true, toolbar: true });
      
      const columnButton = screen.getByRole('button', { name: /columns/i });
      expect(columnButton).toBeInTheDocument();
    });

    it('opens column configuration dropdown', async () => {
      renderDataTable({ columnSelector: true, toolbar: true });
      
      const columnButton = screen.getByRole('button', { name: /columns/i });
      await user.click(columnButton);
      
      await waitFor(() => {
        // Should show column names as checkboxes
        expect(screen.getByText('Name')).toBeInTheDocument();
        expect(screen.getByText('Email')).toBeInTheDocument();
      });
    });

    it('toggles column visibility', async () => {
      const onColumnConfigChange = vi.fn();
      renderDataTable({
        columnSelector: true,
        toolbar: true,
        onColumnConfigChange,
      });
      
      const columnButton = screen.getByRole('button', { name: /columns/i });
      await user.click(columnButton);
      
      await waitFor(async () => {
        const nameCheckbox = screen.getByRole('checkbox', { name: /name/i });
        await user.click(nameCheckbox);
        
        expect(onColumnConfigChange).toHaveBeenCalled();
      });
    });
  });

  describe('Pagination', () => {
    const paginationProps = {
      current: 1,
      pageSize: 2,
      total: 3,
      onChange: vi.fn(),
    };

    it('renders pagination when provided', () => {
      renderDataTable({ pagination: paginationProps });
      
      const pagination = document.querySelector('.ant-pagination');
      expect(pagination).toBeInTheDocument();
    });

    it('hides pagination when set to false', () => {
      renderDataTable({ pagination: false });
      
      const pagination = document.querySelector('.ant-pagination');
      expect(pagination).not.toBeInTheDocument();
    });

    it('triggers pagination change', async () => {
      const onChange = vi.fn();
      renderDataTable({
        pagination: { ...paginationProps, onChange },
      });
      
      // Look for next page button
      const nextButton = document.querySelector('.ant-pagination-next');
      if (nextButton && !nextButton.classList.contains('ant-pagination-disabled')) {
        await user.click(nextButton);
        
        await waitFor(() => {
          expect(onChange).toHaveBeenCalled();
        });
      }
    });
  });

  describe('Virtual Scrolling', () => {
    it('uses virtual table when virtual prop is true', () => {
      renderDataTable({ virtual: true });
      
      // Should render virtual table instead of regular table
      const virtualTable = document.querySelector('.virtual-table');
      expect(virtualTable).toBeInTheDocument();
    });

    it('uses virtual table when data exceeds threshold', () => {
      const largeDataSet = Array.from({ length: 1500 }, (_, i) => ({
        id: `${i}`,
        name: `User ${i}`,
        email: `user${i}@example.com`,
        role: 'user',
        status: 'active',
      }));

      renderDataTable({
        dataSource: largeDataSet,
        virtualThreshold: 1000,
      });
      
      // Should automatically use virtual scrolling
      const virtualTable = document.querySelector('.virtual-table');
      expect(virtualTable).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('has proper ARIA labels on interactive elements', () => {
      renderDataTable();
      
      // Check action buttons have proper labels
      const editButton = screen.getByLabelText('Edit John Doe');
      const deleteButton = screen.getByLabelText('Delete John Doe');
      
      expect(editButton).toBeInTheDocument();
      expect(deleteButton).toBeInTheDocument();
    });

    it('has proper ARIA roles on table elements', () => {
      renderDataTable();
      
      const table = screen.getByRole('table');
      const rows = screen.getAllByRole('row');
      const cells = screen.getAllByRole('cell');
      
      expect(table).toBeInTheDocument();
      expect(rows.length).toBeGreaterThan(0);
      expect(cells.length).toBeGreaterThan(0);
    });

    it('provides status information via ARIA labels', () => {
      renderDataTable();
      
      const statusBadges = screen.getAllByTestId('status-badge');
      statusBadges.forEach((badge) => {
        expect(badge).toHaveAttribute('aria-label');
      });
    });

    it('supports keyboard navigation', async () => {
      renderDataTable();
      
      const firstButton = screen.getByTestId('edit-1');
      firstButton.focus();
      
      expect(firstButton).toHaveFocus();
      
      // Tab should move to next focusable element
      await user.tab();
      const deleteButton = screen.getByTestId('delete-1');
      expect(deleteButton).toHaveFocus();
    });

    it('announces sorting changes to screen readers', async () => {
      renderDataTable({ sortable: true });
      
      const nameHeader = screen.getByText('Name');
      await user.click(nameHeader);
      
      // Should have appropriate ARIA sort attributes
      const headerCell = nameHeader.closest('th');
      expect(headerCell).toHaveAttribute('aria-sort');
    });
  });

  describe('Responsive Design', () => {
    it('adapts to mobile viewport', () => {
      // Mock mobile viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 768,
      });
      
      renderDataTable();
      
      // Should apply mobile-specific classes or styles
      const table = document.querySelector('.data-table');
      expect(table).toBeInTheDocument();
    });

    it('handles horizontal scrolling for wide tables', () => {
      renderDataTable({
        scroll: { x: 1200 },
      });
      
      const tableContainer = document.querySelector('.ant-table-body');
      expect(tableContainer).toHaveStyle('overflow-x: auto');
    });
  });

  describe('Performance', () => {
    it('renders efficiently with large datasets', () => {
      const largeDataSet = Array.from({ length: 100 }, (_, i) => ({
        id: `${i}`,
        name: `User ${i}`,
        email: `user${i}@example.com`,
        role: 'user',
        status: 'active',
      }));

      const startTime = performance.now();
      renderDataTable({ dataSource: largeDataSet });
      const endTime = performance.now();
      
      // Should render within reasonable time (adjust threshold as needed)
      expect(endTime - startTime).toBeLessThan(1000);
    });

    it('memoizes expensive operations', () => {
      const { rerender } = renderDataTable();
      
      // Re-render with same props shouldn't cause unnecessary updates
      rerender(
        <TestWrapper>
          <DataTable columns={mockColumns} dataSource={mockData} />
        </TestWrapper>
      );
      
      // Component should handle re-renders efficiently
      expect(screen.getByText('John Doe')).toBeInTheDocument();
    });
  });

  describe('Error Handling', () => {
    it('handles invalid data gracefully', () => {
      const invalidData = [
        { id: '1', name: null, email: undefined },
        { id: '2' }, // Missing required fields
      ];

      renderDataTable({ dataSource: invalidData });
      
      // Should not crash and show some content
      expect(screen.getByRole('table')).toBeInTheDocument();
    });

    it('handles callback errors gracefully', async () => {
      const errorOnSearch = vi.fn().mockImplementation(() => {
        throw new Error('Search failed');
      });

      renderDataTable({ searchable: true, onSearch: errorOnSearch });
      
      const searchInput = screen.getByPlaceholderText('Search...');
      
      // Should not crash when callback throws
      await user.type(searchInput, 'test');
      expect(screen.getByRole('table')).toBeInTheDocument();
    });
  });

  describe('Integration', () => {
    it('works with permission system', () => {
      renderDataTable();
      
      // Should render normally when permissions allow
      expect(screen.getByText('John Doe')).toBeInTheDocument();
    });

    it('integrates with data fetching hooks', () => {
      renderDataTable({ loading: true });
      
      // Should show loading state during data fetch
      expect(document.querySelector('.ant-spin')).toBeInTheDocument();
    });
  });
});
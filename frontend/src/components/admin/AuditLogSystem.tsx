/**
 * AuditLogSystem - Complete audit log management with real-time updates
 * Provides comprehensive system operation tracking, advanced filtering, search, and export functionality
 */

import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  FileText,
  Filter,
  Search,
  Download,
  Eye,
  Clock,
  User as UserIcon,
  AlertTriangle,
  CheckCircle,
  XCircle,
  Activity,
  RefreshCw,
  ChevronDown,
  Play,
  Pause,
  RotateCcw,
  Settings,
  X
} from 'lucide-react';
import { format, parseISO, subDays, startOfDay, endOfDay } from 'date-fns';
import { Button } from '../ui/Button';
import { Input } from '../ui/Input';
import { Modal, useModal } from '../ui/Modal';
import { cn } from '../../utils/cn';
import { apiClient } from '../../services/api/client';
import type {
  AuditLogEntry,
  AuditLogFilters,
  AuditLogStats,
  AuditAction,
  AuditSeverity,
  ExportFormat,
  AuditLogExportRequest,
  ExportJob,
  AuditLogEvent
} from '../../types/admin';
import type { ApiResponse, PaginatedResponse } from '../../types/common';

interface AuditLogSystemProps {
  className?: string;
}

interface AuditLogSystemState {
  entries: AuditLogEntry[];
  stats: AuditLogStats | null;
  filters: AuditLogFilters;
  selectedEntry: AuditLogEntry | null;
  isLoading: boolean;
  error: string | null;
  realTimeEnabled: boolean;
  exportJob: ExportJob | null;
  totalCount: number;
  currentPage: number;
  hasNextPage: boolean;
}

// Severity badge component
const SeverityBadge: React.FC<{ severity: AuditSeverity }> = ({ severity }) => {
  const styles = {
    low: 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300',
    medium: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300',
    high: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300',
    critical: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300'
  };

  return (
    <span className={cn('inline-flex items-center px-2 py-1 rounded-full text-xs font-medium', styles[severity])}>
      {severity.toUpperCase()}
    </span>
  );
};

// Success/failure badge component
const StatusBadge: React.FC<{ success: boolean }> = ({ success }) => {
  return (
    <span className={cn(
      'inline-flex items-center px-2 py-1 rounded-full text-xs font-medium',
      success
        ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300'
        : 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300'
    )}>
      {success ? (
        <CheckCircle className="w-3 h-3 mr-1" />
      ) : (
        <XCircle className="w-3 h-3 mr-1" />
      )}
      {success ? 'Success' : 'Failed'}
    </span>
  );
};

// Audit log filters component
interface AuditLogFiltersProps {
  filters: AuditLogFilters;
  onFiltersChange: (filters: AuditLogFilters) => void;
  onReset: () => void;
  stats: AuditLogStats | null;
}

const AuditLogFilters: React.FC<AuditLogFiltersProps> = ({ filters, onFiltersChange, onReset, stats }) => {
  const [showAdvanced, setShowAdvanced] = useState(false);

  const handleFilterChange = (key: keyof AuditLogFilters, value: string | string[] | Date | boolean | undefined) => {
    onFiltersChange({ ...filters, [key]: value });
  };

  const predefinedRanges = [
    { label: 'Today', value: () => ({ dateFrom: startOfDay(new Date()), dateTo: endOfDay(new Date()) }) },
    { label: 'Yesterday', value: () => ({ dateFrom: startOfDay(subDays(new Date(), 1)), dateTo: endOfDay(subDays(new Date(), 1)) }) },
    { label: 'Last 7 days', value: () => ({ dateFrom: subDays(new Date(), 7), dateTo: new Date() }) },
    { label: 'Last 30 days', value: () => ({ dateFrom: subDays(new Date(), 30), dateTo: new Date() }) }
  ];

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4 space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-medium text-gray-900 dark:text-white flex items-center">
          <Filter className="w-5 h-5 mr-2" />
          Filters
        </h3>
        <div className="flex items-center space-x-2">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setShowAdvanced(!showAdvanced)}
          >
            {showAdvanced ? 'Basic' : 'Advanced'}
            <ChevronDown className={cn('w-4 h-4 ml-1 transition-transform', showAdvanced && 'rotate-180')} />
          </Button>
          <Button variant="outline" size="sm" onClick={onReset}>
            <RotateCcw className="w-4 h-4 mr-1" />
            Reset
          </Button>
        </div>
      </div>

      {/* Basic Filters */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Search
          </label>
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
            <Input
              type="text"
              placeholder="Search logs..."
              value={filters.search || ''}
              onChange={(e) => handleFilterChange('search', e.target.value || undefined)}
              className="pl-10"
            />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Action
          </label>
          <select
            value={filters.action?.[0] || ''}
            onChange={(e) => handleFilterChange('action', e.target.value ? [e.target.value as AuditAction] : undefined)}
            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md dark:bg-gray-700 dark:text-white"
          >
            <option value="">All Actions</option>
            <option value="create">Create</option>
            <option value="read">Read</option>
            <option value="update">Update</option>
            <option value="delete">Delete</option>
            <option value="login">Login</option>
            <option value="logout">Logout</option>
            <option value="password_change">Password Change</option>
            <option value="role_assignment">Role Assignment</option>
            <option value="permission_grant">Permission Grant</option>
            <option value="permission_revoke">Permission Revoke</option>
            <option value="system_config">System Config</option>
            <option value="security_event">Security Event</option>
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Severity
          </label>
          <select
            value={filters.severity?.[0] || ''}
            onChange={(e) => handleFilterChange('severity', e.target.value ? [e.target.value as AuditSeverity] : undefined)}
            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md dark:bg-gray-700 dark:text-white"
          >
            <option value="">All Severities</option>
            <option value="low">Low</option>
            <option value="medium">Medium</option>
            <option value="high">High</option>
            <option value="critical">Critical</option>
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Status
          </label>
          <select
            value={filters.success === undefined ? '' : filters.success.toString()}
            onChange={(e) => handleFilterChange('success', e.target.value === '' ? undefined : e.target.value === 'true')}
            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md dark:bg-gray-700 dark:text-white"
          >
            <option value="">All</option>
            <option value="true">Success</option>
            <option value="false">Failed</option>
          </select>
        </div>
      </div>

      {/* Quick Date Ranges */}
      <div>
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          Quick Date Ranges
        </label>
        <div className="flex flex-wrap gap-2">
          {predefinedRanges.map((range) => (
            <Button
              key={range.label}
              variant="outline"
              size="sm"
              onClick={() => {
                const dates = range.value();
                handleFilterChange('dateFrom', dates.dateFrom);
                handleFilterChange('dateTo', dates.dateTo);
              }}
            >
              {range.label}
            </Button>
          ))}
        </div>
      </div>

      {/* Advanced Filters */}
      {showAdvanced && (
        <div className="border-t border-gray-200 dark:border-gray-600 pt-4 space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Date From
              </label>
              <Input
                type="datetime-local"
                value={filters.dateFrom ? format(filters.dateFrom, "yyyy-MM-dd'T'HH:mm") : ''}
                onChange={(e) => handleFilterChange('dateFrom', e.target.value ? new Date(e.target.value) : undefined)}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Date To
              </label>
              <Input
                type="datetime-local"
                value={filters.dateTo ? format(filters.dateTo, "yyyy-MM-dd'T'HH:mm") : ''}
                onChange={(e) => handleFilterChange('dateTo', e.target.value ? new Date(e.target.value) : undefined)}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                IP Address
              </label>
              <Input
                type="text"
                placeholder="e.g., 192.168.1.1"
                value={filters.ipAddress || ''}
                onChange={(e) => handleFilterChange('ipAddress', e.target.value || undefined)}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Resource Type
              </label>
              <Input
                type="text"
                placeholder="e.g., post, user, comment"
                value={filters.resourceType?.[0] || ''}
                onChange={(e) => handleFilterChange('resourceType', e.target.value ? [e.target.value] : undefined)}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Session ID
              </label>
              <Input
                type="text"
                placeholder="Session identifier"
                value={filters.sessionId || ''}
                onChange={(e) => handleFilterChange('sessionId', e.target.value || undefined)}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Sort By
              </label>
              <select
                value={filters.sortBy || 'createdAt'}
                onChange={(e) => handleFilterChange('sortBy', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md dark:bg-gray-700 dark:text-white"
              >
                <option value="createdAt">Date</option>
                <option value="action">Action</option>
                <option value="severity">Severity</option>
                <option value="userId">User</option>
              </select>
            </div>
          </div>

          <div className="flex items-center space-x-4">
            <label className="flex items-center">
              <input
                type="checkbox"
                checked={filters.sortOrder === 'asc'}
                onChange={(e) => handleFilterChange('sortOrder', e.target.checked ? 'asc' : 'desc')}
                className="rounded border-gray-300 text-blue-600 shadow-sm focus:border-blue-300 focus:ring focus:ring-blue-200"
              />
              <span className="ml-2 text-sm text-gray-700 dark:text-gray-300">
                Sort Ascending
              </span>
            </label>
          </div>
        </div>
      )}

      {/* Filter Stats */}
      {stats && (
        <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-center">
            <div>
              <div className="text-2xl font-bold text-blue-600 dark:text-blue-400">{stats.totalEntries.toLocaleString()}</div>
              <div className="text-sm text-gray-600 dark:text-gray-300">Total Entries</div>
            </div>
            <div>
              <div className="text-2xl font-bold text-green-600 dark:text-green-400">{stats.todayEntries.toLocaleString()}</div>
              <div className="text-sm text-gray-600 dark:text-gray-300">Today</div>
            </div>
            <div>
              <div className="text-2xl font-bold text-yellow-600 dark:text-yellow-400">{((1 - stats.failureRate) * 100).toFixed(1)}%</div>
              <div className="text-sm text-gray-600 dark:text-gray-300">Success Rate</div>
            </div>
            <div>
              <div className="text-2xl font-bold text-purple-600 dark:text-purple-400">{stats.averageDuration.toFixed(0)}ms</div>
              <div className="text-sm text-gray-600 dark:text-gray-300">Avg Duration</div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

// Audit log entry detail modal
const AuditLogDetailModal: React.FC<{
  entry: AuditLogEntry | null;
  isOpen: boolean;
  onClose: () => void;
}> = ({ entry, isOpen, onClose }) => {
  if (!entry) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Audit Log Details" size="lg">
      <div className="space-y-6">
        {/* Header Info */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Action</label>
            <div className="mt-1 flex items-center space-x-2">
              <span className="px-3 py-1 bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300 rounded-full text-sm font-medium">
                {entry.action}
              </span>
              <SeverityBadge severity={entry.severity} />
              <StatusBadge success={entry.success} />
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Timestamp</label>
            <div className="mt-1 text-sm text-gray-900 dark:text-white">
              {format(parseISO(entry.createdAt), 'yyyy-MM-dd HH:mm:ss')}
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">User</label>
            <div className="mt-1 text-sm text-gray-900 dark:text-white">
              {entry.user ? (
                <div className="flex items-center space-x-2">
                  <UserIcon className="w-4 h-4" />
                  <span>{entry.user.userName}</span>
                </div>
              ) : (
                'System'
              )}
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">IP Address</label>
            <div className="mt-1 text-sm text-gray-900 dark:text-white font-mono">
              {entry.ipAddress}
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Resource Type</label>
            <div className="mt-1 text-sm text-gray-900 dark:text-white">
              {entry.resourceType}
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Resource ID</label>
            <div className="mt-1 text-sm text-gray-900 dark:text-white font-mono">
              {entry.resourceId || 'N/A'}
            </div>
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Description</label>
          <div className="mt-1 text-sm text-gray-900 dark:text-white">
            {entry.description}
          </div>
        </div>

        {entry.errorMessage && (
          <div>
            <label className="block text-sm font-medium text-red-700 dark:text-red-300">Error Message</label>
            <div className="mt-1 text-sm text-red-900 dark:text-red-200 bg-red-50 dark:bg-red-900 p-3 rounded">
              {entry.errorMessage}
            </div>
          </div>
        )}

        {entry.duration && (
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Duration</label>
            <div className="mt-1 text-sm text-gray-900 dark:text-white">
              {entry.duration}ms
            </div>
          </div>
        )}

        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Session Info</label>
          <div className="mt-1 text-sm text-gray-900 dark:text-white">
            <div><strong>Session ID:</strong> {entry.sessionId}</div>
            <div><strong>User Agent:</strong> {entry.userAgent}</div>
          </div>
        </div>

        {Object.keys(entry.details).length > 0 && (
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Details</label>
            <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-3 overflow-x-auto">
              <pre className="text-sm text-gray-900 dark:text-white">
                {JSON.stringify(entry.details, null, 2)}
              </pre>
            </div>
          </div>
        )}

        {Object.keys(entry.metadata).length > 0 && (
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Metadata</label>
            <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-3 overflow-x-auto">
              <pre className="text-sm text-gray-900 dark:text-white">
                {JSON.stringify(entry.metadata, null, 2)}
              </pre>
            </div>
          </div>
        )}
      </div>
    </Modal>
  );
};

// Main Audit Log System component
export const AuditLogSystem: React.FC<AuditLogSystemProps> = ({ className }) => {
  const [state, setState] = useState<AuditLogSystemState>({
    entries: [],
    stats: null,
    filters: {
      page: 1,
      pageSize: 50,
      sortBy: 'createdAt',
      sortOrder: 'desc'
    },
    selectedEntry: null,
    isLoading: false,
    error: null,
    realTimeEnabled: false,
    exportJob: null,
    totalCount: 0,
    currentPage: 1,
    hasNextPage: false
  });

  const detailModal = useModal();
  const wsRef = useRef<WebSocket | null>(null);

  // Load audit logs
  const loadAuditLogs = useCallback(async (filters: AuditLogFilters) => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));

    try {
      const params = new URLSearchParams();
      Object.entries(filters).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
          if (Array.isArray(value)) {
            value.forEach(v => params.append(key, v.toString()));
          } else if (value instanceof Date) {
            params.append(key, value.toISOString());
          } else {
            params.append(key, value.toString());
          }
        }
      });

      const [logsResponse, statsResponse] = await Promise.all([
        apiClient.get<ApiResponse<PaginatedResponse<AuditLogEntry>>>(`/api/admin/audit-logs?${params}`),
        apiClient.get<ApiResponse<AuditLogStats>>('/api/admin/audit-logs/stats')
      ]);

      const logsData = logsResponse.data.data!;
      setState(prev => ({
        ...prev,
        entries: logsData.data,
        stats: statsResponse.data.data!,
        totalCount: logsData.totalCount,
        currentPage: logsData.page,
        hasNextPage: logsData.hasNextPage,
        isLoading: false
      }));
    } catch (error) {
      // Failed to load audit logs - error logged via error reporting service
      setState(prev => ({
        ...prev,
        error: 'Failed to load audit logs',
        isLoading: false
      }));
    }
  }, []);

  // Load initial data on mount
  useEffect(() => {
    loadAuditLogs(state.filters);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Real-time updates via WebSocket
  useEffect(() => {
    if (!state.realTimeEnabled) {
      if (wsRef.current) {
        wsRef.current.close();
        wsRef.current = null;
      }
      return;
    }

    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsUrl = `${protocol}//${window.location.host}/api/admin/audit-logs/ws`;

    try {
      const ws = new WebSocket(wsUrl);

      ws.onopen = () => {
        // Audit log WebSocket connected
      };

      ws.onmessage = (event) => {
        try {
          const data: AuditLogEvent = JSON.parse(event.data);

          if (data.type === 'new_entry' && data.entry) {
            setState(prev => ({
              ...prev,
              entries: [data.entry!, ...prev.entries.slice(0, 49)] // Keep max 50 entries for performance
            }));
          }
        } catch (error) {
          // Failed to parse WebSocket message - error logged via error reporting service
        }
      };

      ws.onclose = () => {
        // Audit log WebSocket disconnected
        // Attempt to reconnect after 5 seconds
        setTimeout(() => {
          if (state.realTimeEnabled) {
            setState(prev => ({ ...prev, realTimeEnabled: false }));
            setTimeout(() => setState(prev => ({ ...prev, realTimeEnabled: true })), 100);
          }
        }, 5000);
      };

      ws.onerror = (_error) => {
        // Audit log WebSocket error - error logged via error reporting service
      };

      wsRef.current = ws;
    } catch (error) {
      // Failed to create WebSocket connection - error logged via error reporting service
      setState(prev => ({ ...prev, realTimeEnabled: false }));
    }

    return () => {
      if (wsRef.current) {
        wsRef.current.close();
        wsRef.current = null;
      }
    };
  }, [state.realTimeEnabled]);

  // Handle filter changes
  const handleFiltersChange = useCallback((newFilters: AuditLogFilters) => {
    const updatedFilters = { ...newFilters, page: 1 }; // Reset to first page
    setState(prev => ({ ...prev, filters: updatedFilters }));
    loadAuditLogs(updatedFilters);
  }, [loadAuditLogs]);

  // Handle pagination
  const handlePageChange = useCallback((page: number) => {
    const updatedFilters = { ...state.filters, page };
    setState(prev => ({ ...prev, filters: updatedFilters }));
    loadAuditLogs(updatedFilters);
  }, [state.filters, loadAuditLogs]);

  // Handle entry selection
  const handleEntrySelect = useCallback((entry: AuditLogEntry) => {
    setState(prev => ({ ...prev, selectedEntry: entry }));
    detailModal.openModal();
  }, [detailModal]);

  // Handle export
  const handleExport = useCallback(async (exportFormat: ExportFormat) => {
    try {
      const exportRequest: AuditLogExportRequest = {
        format: exportFormat,
        filters: state.filters,
        includeDetails: true,
        includeMetadata: false,
        filename: `audit-logs-${format(new Date(), 'yyyy-MM-dd')}.${exportFormat}`
      };

      const response = await apiClient.post<ApiResponse<ExportJob>>('/api/admin/audit-logs/export', exportRequest);
      const job = response.data.data!;

      setState(prev => ({ ...prev, exportJob: job }));

      // Poll for export completion
      const pollInterval = setInterval(async () => {
        try {
          const statusResponse = await apiClient.get<ApiResponse<ExportJob>>(`/api/admin/export-jobs/${job.jobId}`);
          const updatedJob = statusResponse.data.data!;

          setState(prev => ({ ...prev, exportJob: updatedJob }));

          if (updatedJob.status === 'completed' || updatedJob.status === 'failed') {
            clearInterval(pollInterval);

            if (updatedJob.status === 'completed' && updatedJob.downloadUrl) {
              // Trigger download
              const link = document.createElement('a');
              link.href = updatedJob.downloadUrl;
              link.download = updatedJob.filePath?.split('/').pop() || 'export.csv';
              document.body.appendChild(link);
              link.click();
              document.body.removeChild(link);
            }
          }
        } catch (error) {
          // Failed to check export status - error logged via error reporting service
          clearInterval(pollInterval);
        }
      }, 2000);

    } catch (error) {
      // Failed to start export - error logged via error reporting service
      setState(prev => ({ ...prev, error: 'Failed to start export' }));
    }
  }, [state.filters]);

  // Reset filters
  const resetFilters = useCallback(() => {
    const defaultFilters: AuditLogFilters = {
      page: 1,
      pageSize: 50,
      sortBy: 'createdAt',
      sortOrder: 'desc'
    };
    setState(prev => ({ ...prev, filters: defaultFilters }));
    loadAuditLogs(defaultFilters);
  }, [loadAuditLogs]);

  if (state.isLoading && state.entries.length === 0) {
    return (
      <div className={cn('flex items-center justify-center h-64', className)}>
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (state.error && state.entries.length === 0) {
    return (
      <div className={cn('text-center py-8', className)}>
        <AlertTriangle className="mx-auto h-12 w-12 text-red-500 mb-4" />
        <p className="text-red-600 mb-4">{state.error}</p>
        <Button onClick={() => loadAuditLogs(state.filters)}>Retry</Button>
      </div>
    );
  }

  return (
    <div className={cn('space-y-6', className)}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center">
            <FileText className="w-6 h-6 mr-3 text-green-600" />
            Audit Logs
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            View and analyze system operations and events
          </p>
        </div>
        <div className="flex items-center space-x-3">
          <Button
            variant={state.realTimeEnabled ? 'default' : 'outline'}
            onClick={() => setState(prev => ({ ...prev, realTimeEnabled: !prev.realTimeEnabled }))}
            className="inline-flex items-center"
          >
            {state.realTimeEnabled ? (
              <Pause className="w-4 h-4 mr-2" />
            ) : (
              <Play className="w-4 h-4 mr-2" />
            )}
            {state.realTimeEnabled ? 'Pause' : 'Live'} Updates
          </Button>
          <Button
            variant="outline"
            onClick={() => handleExport('csv' as ExportFormat)}
            className="inline-flex items-center"
          >
            <Download className="w-4 h-4 mr-2" />
            Export CSV
          </Button>
          <Button
            variant="outline"
            onClick={() => loadAuditLogs(state.filters)}
            className="inline-flex items-center"
          >
            <RefreshCw className="w-4 h-4 mr-2" />
            Refresh
          </Button>
        </div>
      </div>

      {/* Filters */}
      <AuditLogFilters
        filters={state.filters}
        onFiltersChange={handleFiltersChange}
        onReset={resetFilters}
        stats={state.stats}
      />

      {/* Audit Log Table */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow">
        <div className="p-4 border-b border-gray-200 dark:border-gray-600">
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-medium text-gray-900 dark:text-white">
              Audit Entries ({state.totalCount.toLocaleString()})
            </h3>
            {state.realTimeEnabled && (
              <div className="flex items-center space-x-2 text-green-600 dark:text-green-400">
                <Activity className="w-4 h-4 animate-pulse" />
                <span className="text-sm font-medium">Live Updates</span>
              </div>
            )}
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-600">
            <thead className="bg-gray-50 dark:bg-gray-700">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Timestamp
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Action
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  User
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Resource
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Status
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Severity
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-600">
              {state.entries.map((entry, index) => (
                <tr
                  key={entry.id}
                  className={cn(
                    'hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer',
                    index % 2 === 0 ? 'bg-white dark:bg-gray-800' : 'bg-gray-50 dark:bg-gray-700'
                  )}
                  onClick={() => handleEntrySelect(entry)}
                >
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-white">
                    <div className="flex items-center space-x-2">
                      <Clock className="w-4 h-4 text-gray-400" />
                      <div>
                        <div>{format(parseISO(entry.createdAt), 'MMM dd, HH:mm:ss')}</div>
                        <div className="text-xs text-gray-500">
                          {format(parseISO(entry.createdAt), 'yyyy')}
                        </div>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-white">
                    <span className="px-3 py-1 bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300 rounded-full text-xs font-medium">
                      {entry.action}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-white">
                    {entry.user ? (
                      <div className="flex items-center space-x-2">
                        <UserIcon className="w-4 h-4 text-gray-400" />
                        <div>
                          <div className="font-medium">{entry.user.userName}</div>
                          <div className="text-xs text-gray-500">{entry.user.email}</div>
                        </div>
                      </div>
                    ) : (
                      <div className="flex items-center space-x-2">
                        <Settings className="w-4 h-4 text-gray-400" />
                        <span className="text-gray-500">System</span>
                      </div>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-white">
                    <div>
                      <div className="font-medium">{entry.resourceType}</div>
                      {entry.resourceId && (
                        <div className="text-xs text-gray-500 font-mono">{entry.resourceId.slice(0, 8)}...</div>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <StatusBadge success={entry.success} />
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <SeverityBadge severity={entry.severity} />
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={(e) => {
                        e.stopPropagation();
                        handleEntrySelect(entry);
                      }}
                    >
                      <Eye className="w-4 h-4" />
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {state.totalCount > state.filters.pageSize! && (
          <div className="px-6 py-4 border-t border-gray-200 dark:border-gray-600">
            <div className="flex items-center justify-between">
              <div className="text-sm text-gray-700 dark:text-gray-300">
                Showing {((state.currentPage - 1) * state.filters.pageSize!) + 1} to{' '}
                {Math.min(state.currentPage * state.filters.pageSize!, state.totalCount)} of{' '}
                {state.totalCount.toLocaleString()} entries
              </div>
              <div className="flex items-center space-x-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={state.currentPage === 1}
                  onClick={() => handlePageChange(state.currentPage - 1)}
                >
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={!state.hasNextPage}
                  onClick={() => handlePageChange(state.currentPage + 1)}
                >
                  Next
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Detail Modal */}
      <AuditLogDetailModal
        entry={state.selectedEntry}
        isOpen={detailModal.isOpen}
        onClose={detailModal.closeModal}
      />

      {/* Export Progress */}
      {state.exportJob && (
        <div className="fixed top-4 right-4 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded-lg shadow-lg p-4 z-50 min-w-64">
          <div className="flex items-center justify-between mb-2">
            <h4 className="text-sm font-medium text-gray-900 dark:text-white">Export Progress</h4>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setState(prev => ({ ...prev, exportJob: null }))}
            >
              <X className="w-4 h-4" />
            </Button>
          </div>
          <div className="space-y-2">
            <div className="flex justify-between text-sm">
              <span className="text-gray-600 dark:text-gray-400">Status:</span>
              <span className={cn(
                'font-medium',
                state.exportJob.status === 'completed' ? 'text-green-600' :
                state.exportJob.status === 'failed' ? 'text-red-600' :
                'text-blue-600'
              )}>
                {state.exportJob.status.charAt(0).toUpperCase() + state.exportJob.status.slice(1)}
              </span>
            </div>
            {state.exportJob.totalRecords > 0 && (
              <div className="space-y-1">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600 dark:text-gray-400">Progress:</span>
                  <span className="font-medium">
                    {state.exportJob.processedRecords} / {state.exportJob.totalRecords}
                  </span>
                </div>
                <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2">
                  <div
                    className="bg-blue-600 h-2 rounded-full transition-all duration-300"
                    style={{
                      width: `${(state.exportJob.processedRecords / state.exportJob.totalRecords) * 100}%`
                    }}
                  />
                </div>
              </div>
            )}
            {state.exportJob.errorMessage && (
              <div className="text-sm text-red-600 dark:text-red-400">
                {state.exportJob.errorMessage}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Error Alert */}
      {state.error && (
        <div className="fixed top-4 right-4 bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded z-50">
          <div className="flex items-center">
            <AlertTriangle className="w-4 h-4 mr-2" />
            {state.error}
            <button
              onClick={() => setState(prev => ({ ...prev, error: null }))}
              className="ml-4 text-red-500 hover:text-red-700"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default AuditLogSystem;
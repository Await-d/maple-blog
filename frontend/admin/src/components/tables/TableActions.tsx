// @ts-nocheck
import React, { useMemo, useCallback, useState } from 'react';
import {
  Space,
  Button,
  Dropdown,
  Modal,
  message,
  Popconfirm,
  Tooltip,
  Badge,
  Typography,
  Input,
  Select,
  Divider,
  Tag,
} from 'antd';
import {
  DeleteOutlined,
  EditOutlined,
  ExportOutlined,
  MoreOutlined,
  BulkUpdateOutlined,
  SendOutlined,
  CopyOutlined,
  ArchiveBoxOutlined,
  EyeOutlined,
  EyeInvisibleOutlined,
  LockOutlined,
  UnlockOutlined,
  StarOutlined,
  StarFilled,
  CheckCircleOutlined,
  CloseCircleOutlined,
  ExclamationCircleOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import type { MenuProps } from 'antd';

const { Text } = Typography;
const { TextArea } = Input;

// 批量操作类型
export type BatchAction = 
  | 'delete'
  | 'export'
  | 'archive'
  | 'publish'
  | 'unpublish'
  | 'enable'
  | 'disable'
  | 'approve'
  | 'reject'
  | 'lock'
  | 'unlock'
  | 'favorite'
  | 'unfavorite'
  | 'copy'
  | 'move'
  | 'tag'
  | 'untag'
  | 'assign'
  | 'unassign'
  | 'custom';

// 批量操作配置
export interface BatchActionConfig {
  key: BatchAction | string;
  label: string;
  icon?: React.ReactNode;
  danger?: boolean;
  disabled?: boolean;
  tooltip?: string;
  needConfirm?: boolean;
  confirmTitle?: string;
  confirmContent?: string;
  requireInput?: boolean;
  inputPlaceholder?: string;
  inputType?: 'text' | 'textarea' | 'select';
  inputOptions?: Array<{ label: string; value: any }>;
  visible?: boolean;
}

// 表格操作属性
export interface TableActionsProps {
  selectedCount: number;
  totalCount: number;
  loading?: boolean;
  disabled?: boolean;
  
  // 批量操作配置
  batchActions?: BatchActionConfig[];
  defaultBatchActions?: boolean;
  
  // 事件回调
  onClearSelection?: () => void;
  onBatchAction?: (action: string, params?: any) => Promise<void> | void;
  onRefresh?: () => void;
  
  // 自定义渲染
  prefix?: React.ReactNode;
  suffix?: React.ReactNode;
  
  // 样式
  className?: string;
  style?: React.CSSProperties;
}

// 预定义批量操作
const DEFAULT_BATCH_ACTIONS: BatchActionConfig[] = [
  {
    key: 'delete',
    label: 'Delete',
    icon: <DeleteOutlined />,
    danger: true,
    needConfirm: true,
    confirmTitle: 'Delete Selected Items',
    confirmContent: 'Are you sure you want to delete the selected items? This action cannot be undone.',
  },
  {
    key: 'export',
    label: 'Export',
    icon: <ExportOutlined />,
  },
  {
    key: 'archive',
    label: 'Archive',
    icon: <ArchiveBoxOutlined />,
    needConfirm: true,
    confirmTitle: 'Archive Selected Items',
    confirmContent: 'Are you sure you want to archive the selected items?',
  },
  {
    key: 'publish',
    label: 'Publish',
    icon: <CheckCircleOutlined />,
  },
  {
    key: 'unpublish',
    label: 'Unpublish',
    icon: <CloseCircleOutlined />,
  },
  {
    key: 'enable',
    label: 'Enable',
    icon: <EyeOutlined />,
  },
  {
    key: 'disable',
    label: 'Disable',
    icon: <EyeInvisibleOutlined />,
  },
  {
    key: 'lock',
    label: 'Lock',
    icon: <LockOutlined />,
  },
  {
    key: 'unlock',
    label: 'Unlock',
    icon: <UnlockOutlined />,
  },
  {
    key: 'copy',
    label: 'Copy',
    icon: <CopyOutlined />,
  },
  {
    key: 'tag',
    label: 'Add Tag',
    icon: <Tag />,
    requireInput: true,
    inputType: 'select',
    inputPlaceholder: 'Select tags to add',
  },
];

export const TableActions: React.FC<TableActionsProps> = ({
  selectedCount,
  totalCount,
  loading = false,
  disabled = false,
  batchActions,
  defaultBatchActions = true,
  onClearSelection,
  onBatchAction,
  onRefresh,
  prefix,
  suffix,
  className,
  style,
}) => {
  const [modalVisible, setModalVisible] = useState(false);
  const [currentAction, setCurrentAction] = useState<BatchActionConfig | null>(null);
  const [inputValue, setInputValue] = useState<any>('');
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  // 合并批量操作配置
  const allBatchActions = useMemo(() => {
    const actions = defaultBatchActions ? [...DEFAULT_BATCH_ACTIONS] : [];
    if (batchActions) {
      return [...actions, ...batchActions];
    }
    return actions;
  }, [batchActions, defaultBatchActions]);

  // 可见的批量操作
  const visibleBatchActions = useMemo(() => {
    return allBatchActions.filter(action => action.visible !== false);
  }, [allBatchActions]);

  // 处理批量操作
  const handleBatchAction = useCallback(async (actionConfig: BatchActionConfig, params?: any) => {
    if (disabled || loading) return;

    const { key, needConfirm, requireInput } = actionConfig;

    try {
      setActionLoading(key);

      // 需要输入的操作
      if (requireInput) {
        setCurrentAction(actionConfig);
        setInputValue('');
        setModalVisible(true);
        return;
      }

      // 需要确认的操作
      if (needConfirm) {
        Modal.confirm({
          title: actionConfig.confirmTitle || 'Confirm Action',
          content: actionConfig.confirmContent || `Are you sure you want to perform this action on ${selectedCount} items?`,
          onOk: async () => {
            await onBatchAction?.(key, params);
            message.success(`Successfully performed ${actionConfig.label.toLowerCase()} on ${selectedCount} items`);
          },
        });
        return;
      }

      // 直接执行操作
      await onBatchAction?.(key, params);
      message.success(`Successfully performed ${actionConfig.label.toLowerCase()} on ${selectedCount} items`);
    } catch (error) {
      console.error(`Error performing ${actionConfig.label}:`, error);
      message.error(`Failed to perform ${actionConfig.label.toLowerCase()}`);
    } finally {
      setActionLoading(null);
    }
  }, [disabled, loading, selectedCount, onBatchAction]);

  // 处理模态框确认
  const handleModalOk = useCallback(async () => {
    if (!currentAction) return;

    try {
      await onBatchAction?.(currentAction.key, inputValue);
      message.success(`Successfully performed ${currentAction.label.toLowerCase()} on ${selectedCount} items`);
      setModalVisible(false);
      setCurrentAction(null);
      setInputValue('');
    } catch (error) {
      console.error(`Error performing ${currentAction.label}:`, error);
      message.error(`Failed to perform ${currentAction.label.toLowerCase()}`);
    }
  }, [currentAction, inputValue, selectedCount, onBatchAction]);

  // 生成下拉菜单项
  const menuItems: MenuProps['items'] = useMemo(() => {
    return visibleBatchActions.map((action) => ({
      key: action.key,
      label: (
        <span>
          {action.icon}
          <span style={{ marginLeft: 8 }}>{action.label}</span>
        </span>
      ),
      disabled: action.disabled || loading,
      danger: action.danger,
      onClick: () => handleBatchAction(action),
    }));
  }, [visibleBatchActions, loading, handleBatchAction]);

  // 主要批量操作（前3个）
  const primaryActions = visibleBatchActions.slice(0, 3);
  const secondaryActions = visibleBatchActions.slice(3);

  // 渲染输入组件
  const renderInput = () => {
    if (!currentAction) return null;

    const { inputType = 'text', inputPlaceholder, inputOptions } = currentAction;

    switch (inputType) {
      case 'textarea':
        return (
          <TextArea
            placeholder={inputPlaceholder}
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
            rows={4}
          />
        );
      case 'select':
        return (
          <Select
            placeholder={inputPlaceholder}
            value={inputValue}
            onChange={setInputValue}
            style={{ width: '100%' }}
            mode={inputOptions ? 'multiple' : undefined}
            options={inputOptions}
          />
        );
      default:
        return (
          <Input
            placeholder={inputPlaceholder}
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
          />
        );
    }
  };

  if (selectedCount === 0) {
    return null;
  }

  return (
    <div className={`table-actions ${className || ''}`} style={style}>
      <div className="table-actions-content">
        {/* 前缀内容 */}
        {prefix}

        {/* 选择信息 */}
        <div className="table-actions-info">
          <Badge count={selectedCount} color="#1890ff" />
          <Text style={{ marginLeft: 8 }}>
            Selected {selectedCount} of {totalCount} items
          </Text>
          {onClearSelection && (
            <Button
              type="link"
              
              onClick={onClearSelection}
              style={{ marginLeft: 8 }}
            >
              Clear
            </Button>
          )}
        </div>

        <Divider type="vertical" />

        {/* 批量操作按钮 */}
        <Space wrap>
          {/* 主要操作按钮 */}
          {primaryActions.map((action) => (
            <Tooltip key={action.key} title={action.tooltip}>
              <Button
                type={action.danger ? 'primary' : 'default'}
                danger={action.danger}
                icon={action.icon}
                loading={actionLoading === action.key}
                disabled={action.disabled || loading || disabled}
                onClick={() => handleBatchAction(action)}
              >
                {action.label}
              </Button>
            </Tooltip>
          ))}

          {/* 更多操作下拉菜单 */}
          {secondaryActions.length > 0 && (
            <Dropdown
              menu={{ items: menuItems.slice(3) }}
              trigger={['click']}
              disabled={loading || disabled}
            >
              <Button icon={<MoreOutlined />}>
                More Actions
              </Button>
            </Dropdown>
          )}

          {/* 刷新按钮 */}
          {onRefresh && (
            <Tooltip title="Refresh">
              <Button
                icon={<ReloadOutlined />}
                onClick={onRefresh}
                loading={loading}
                disabled={disabled}
              />
            </Tooltip>
          )}
        </Space>

        {/* 后缀内容 */}
        {suffix}
      </div>

      {/* 输入模态框 */}
      <Modal
        title={currentAction?.label}
        open={modalVisible}
        onOk={handleModalOk}
        onCancel={() => {
          setModalVisible(false);
          setCurrentAction(null);
          setInputValue('');
        }}
        confirmLoading={actionLoading === currentAction?.key}
      >
        <div style={{ marginBottom: 16 }}>
          <Text>
            This action will be applied to {selectedCount} selected items.
          </Text>
        </div>
        {renderInput()}
      </Modal>

      <style jsx>{`
        .table-actions {
          background: #fafafa;
          border: 1px solid #d9d9d9;
          border-radius: 6px;
          padding: 12px 16px;
          margin-bottom: 16px;
          display: flex;
          align-items: center;
          justify-content: space-between;
        }

        .table-actions-content {
          display: flex;
          align-items: center;
          width: 100%;
          gap: 12px;
        }

        .table-actions-info {
          display: flex;
          align-items: center;
          flex: 1;
        }

        .table-actions-info .ant-badge {
          line-height: 1;
        }

        /* 响应式设计 */
        @media (max-width: 768px) {
          .table-actions {
            flex-direction: column;
            align-items: stretch;
            gap: 12px;
          }

          .table-actions-content {
            flex-direction: column;
            align-items: stretch;
          }

          .table-actions-info {
            justify-content: center;
          }

          .ant-space {
            justify-content: center;
          }
        }

        /* 动画效果 */
        .table-actions {
          animation: slideDown 0.3s ease-out;
        }

        @keyframes slideDown {
          from {
            opacity: 0;
            transform: translateY(-10px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }

        /* 主题适配 */
        .table-actions.dark {
          background: #141414;
          border-color: #434343;
          color: #fff;
        }

        /* 加载状态 */
        .table-actions.loading {
          opacity: 0.7;
          pointer-events: none;
        }

        /* 危险操作样式 */
        .table-actions .ant-btn-dangerous {
          border-color: #ff4d4f;
        }

        .table-actions .ant-btn-dangerous:hover {
          background: #ff4d4f;
          border-color: #ff4d4f;
          color: white;
        }
      `}</style>
    </div>
  );
};

// 高阶组件：为表格添加批量操作功能
export const withTableActions = <T extends Record<string, any>>(
  TableComponent: React.ComponentType<any>
) => {
  return React.forwardRef<any, any>((props, ref) => {
    const { rowSelection, ...restProps } = props;
    const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);

    const enhancedRowSelection = useMemo(() => {
      if (!rowSelection) return undefined;

      return {
        ...rowSelection,
        selectedRowKeys,
        onChange: (keys: React.Key[], rows: T[]) => {
          setSelectedRowKeys(keys);
          rowSelection.onChange?.(keys, rows);
        },
      };
    }, [rowSelection, selectedRowKeys]);

    const handleClearSelection = useCallback(() => {
      setSelectedRowKeys([]);
      rowSelection?.onChange?.([], []);
    }, [rowSelection]);

    return (
      <>
        <TableActions
          selectedCount={selectedRowKeys.length}
          totalCount={props.dataSource?.length || 0}
          onClearSelection={handleClearSelection}
          {...props.tableActionsProps}
        />
        <TableComponent
          {...restProps}
          ref={ref}
          rowSelection={enhancedRowSelection}
        />
      </>
    );
  });
};

export default TableActions;
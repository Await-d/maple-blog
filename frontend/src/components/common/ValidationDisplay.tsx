import React, { useState, useEffect } from 'react';
import { createRoot, Root } from 'react-dom/client';

// 精确的验证值类型定义（递归支持任意深度）
export type ValidationValue =
  | string
  | number
  | boolean
  | null
  | undefined
  | Date
  | ValidationValue[]
  | { [key: string]: ValidationValue };

export interface ValidationError {
  field: string;
  message: string;
  code?: string;
  value?: ValidationValue;
}

export interface ValidationResult {
  valid: boolean;
  errors: ValidationError[];
  warnings?: ValidationError[];
  summary?: {
    totalErrors: number;
    totalWarnings: number;
    criticalErrors: number;
  };
  metadata?: {
    validatedAt: string;
    validatorVersion?: string;
    processingTime?: number;
  };
}

export interface ValidationDisplayOptions {
  title?: string;
  showSummary?: boolean;
  showMetadata?: boolean;
  groupByField?: boolean;
  showWarnings?: boolean;
  maxHeight?: number;
  width?: number;
  position?: 'center' | 'top-right' | 'bottom-right';
  autoClose?: number; // Auto close after N seconds
  onClose?: () => void;
  onRetry?: () => void;
  onIgnoreWarnings?: () => void;
}

interface ValidationDisplayState {
  id: string;
  result: ValidationResult;
  options: Required<ValidationDisplayOptions>;
  onClose: () => void;
}

class ValidationDisplayService {
  private activeDisplays: Map<string, ValidationDisplayState> = new Map();
  private container: HTMLDivElement | null = null;
  private root: Root | null = null;
  private nextId = 1;

  constructor() {
    this.initializeContainer();
  }

  /**
   * Display validation results
   */
  show(
    result: ValidationResult,
    options: ValidationDisplayOptions = {}
  ): string {
    const id = `validation_${this.nextId++}`;

    const defaultOptions: Partial<ValidationDisplayOptions> = {
      title: '验证结果',
      showSummary: true,
      showMetadata: false,
      groupByField: true,
      showWarnings: true,
      maxHeight: 500,
      width: 600,
      position: 'center',
      autoClose: 0,
      onClose: () => { /* noop - default handler */ }
    };

    const displayOptions: Required<ValidationDisplayOptions> = {
      title: '验证结果',
      showSummary: true,
      showMetadata: false,
      groupByField: true,
      showWarnings: true,
      maxHeight: 500,
      width: 600,
      position: 'center',
      autoClose: 0,
      onClose: () => { /* noop - default handler */ },
      onRetry: () => { /* noop - parent handles retry */ },
      onIgnoreWarnings: () => { /* noop - parent handles ignore */ },
      ...defaultOptions,
      ...options
    };

    const display: ValidationDisplayState = {
      id,
      result,
      options: displayOptions,
      onClose: () => this.close(id)
    };

    this.activeDisplays.set(id, display);

    // Auto-close if specified
    if (displayOptions.autoClose && displayOptions.autoClose > 0) {
      setTimeout(() => {
        this.close(id);
      }, displayOptions.autoClose * 1000);
    }

    this.render();
    return id;
  }

  /**
   * Show validation errors in a compact format
   */
  showErrors(errors: ValidationError[], title: string = '验证错误'): string {
    const result: ValidationResult = {
      valid: false,
      errors,
      summary: {
        totalErrors: errors.length,
        totalWarnings: 0,
        criticalErrors: errors.filter(e => e.code === 'critical').length
      }
    };

    return this.show(result, {
      title,
      showWarnings: false,
      groupByField: true
    });
  }

  /**
   * Show validation summary with statistics
   */
  showSummary(result: ValidationResult, onRetry?: () => void): string {
    return this.show(result, {
      title: result.valid ? '验证通过' : '验证失败',
      showSummary: true,
      showMetadata: true,
      onRetry
    });
  }

  /**
   * Close a specific display
   */
  close(id: string): void {
    const display = this.activeDisplays.get(id);
    if (display) {
      display.options.onClose();
      this.activeDisplays.delete(id);
      this.render();
    }
  }

  /**
   * Close all displays
   */
  closeAll(): void {
    this.activeDisplays.clear();
    this.render();
  }

  /**
   * Get active displays
   */
  getActiveDisplays(): ValidationDisplayState[] {
    return Array.from(this.activeDisplays.values());
  }

  private initializeContainer(): void {
    if (typeof document === 'undefined') return;

    this.container = document.createElement('div');
    this.container.id = 'validation-display-container';
    this.container.style.cssText = `
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      z-index: 10002;
      pointer-events: none;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    `;

    document.body.appendChild(this.container);
    this.root = createRoot(this.container);
  }

  private render(): void {
    if (!this.root) return;

    const displays = Array.from(this.activeDisplays.values());

    this.root.render(React.createElement(ValidationDisplayContainer, {
      displays,
      onClose: (id: string) => this.close(id)
    }));
  }
}

// React Components
interface ValidationDisplayContainerProps {
  displays: ValidationDisplayState[];
  onClose: (id: string) => void;
}

// eslint-disable-next-line react-refresh/only-export-components
const ValidationDisplayContainer = ({ displays, onClose }: ValidationDisplayContainerProps) => {
  if (displays.length === 0) return null;

  return React.createElement('div', {
    style: {
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      pointerEvents: 'auto'
    }
  }, displays.map(display =>
    React.createElement(ValidationDisplayComponent, {
      key: display.id,
      display,
      onClose
    })
  ));
};
ValidationDisplayContainer.displayName = 'ValidationDisplayContainer';

interface ValidationDisplayComponentProps {
  display: ValidationDisplayState;
  onClose: (id: string) => void;
}

// eslint-disable-next-line react-refresh/only-export-components
const ValidationDisplayComponent = ({ display, onClose }: ValidationDisplayComponentProps) => {
  const { id, result, options } = display;
  const [activeTab, setActiveTab] = useState<'errors' | 'warnings' | 'summary'>('errors');

  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose(id);
      }
    };

    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [id, onClose]);

  const handleBackdropClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) {
      onClose(id);
    }
  };

  const handleRetry = () => {
    if (options.onRetry) {
      options.onRetry();
    }
    onClose(id);
  };

  const handleIgnoreWarnings = () => {
    if (options.onIgnoreWarnings) {
      options.onIgnoreWarnings();
    }
    onClose(id);
  };

  const getPositionStyles = (): React.CSSProperties => {
    switch (options.position) {
      case 'top-right':
        return {
          position: 'absolute',
          top: '20px',
          right: '20px'
        };
      case 'bottom-right':
        return {
          position: 'absolute',
          bottom: '20px',
          right: '20px'
        };
      case 'center':
      default:
        return {
          position: 'absolute',
          top: '50%',
          left: '50%',
          transform: 'translate(-50%, -50%)'
        };
    }
  };

  const hasErrors = result.errors.length > 0;
  const hasWarnings = (result.warnings?.length || 0) > 0;

  return React.createElement('div', {
    className: 'validation-display-backdrop',
    onClick: handleBackdropClick,
    style: {
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      backgroundColor: 'rgba(0, 0, 0, 0.5)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      padding: '20px'
    }
  }, React.createElement('div', {
    className: `validation-display validation-${result.valid ? 'success' : 'error'}`,
    onClick: (e: React.MouseEvent) => e.stopPropagation(),
    style: {
      backgroundColor: 'white',
      borderRadius: '8px',
      border: `2px solid ${result.valid ? '#10B981' : '#EF4444'}`,
      boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
      width: '100%',
      maxWidth: `${options.width}px`,
      maxHeight: '90vh',
      display: 'flex',
      flexDirection: 'column',
      ...getPositionStyles()
    }
  }, [
    // Header
    React.createElement('div', {
      key: 'header',
      style: {
        padding: '20px 24px 16px',
        borderBottom: '1px solid #E5E7EB',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between'
      }
    }, [
      React.createElement('div', {
        key: 'title-section',
        style: { display: 'flex', alignItems: 'center', gap: '12px' }
      }, [
        React.createElement('div', {
          key: 'icon',
          style: {
            fontSize: '24px',
            color: result.valid ? '#10B981' : '#EF4444'
          }
        }, result.valid ? '✓' : '✕'),
        React.createElement('h3', {
          key: 'title',
          style: {
            margin: 0,
            fontSize: '18px',
            fontWeight: '600',
            color: '#111827'
          }
        }, options.title)
      ]),
      React.createElement('button', {
        key: 'close',
        onClick: () => onClose(id),
        style: {
          background: 'none',
          border: 'none',
          color: '#9CA3AF',
          cursor: 'pointer',
          fontSize: '20px',
          lineHeight: 1,
          padding: '4px'
        }
      }, '×')
    ]),

    // Summary (if enabled)
    options.showSummary && result.summary && React.createElement('div', {
      key: 'summary',
      style: {
        padding: '16px 24px',
        backgroundColor: result.valid ? '#F0FDF4' : '#FEF2F2',
        borderBottom: '1px solid #E5E7EB',
        display: 'flex',
        gap: '24px',
        fontSize: '14px'
      }
    }, [
      React.createElement('div', {
        key: 'errors',
        style: { display: 'flex', alignItems: 'center', gap: '8px' }
      }, [
        React.createElement('span', {
          key: 'label',
          style: { color: '#6B7280' }
        }, '错误:'),
        React.createElement('span', {
          key: 'count',
          style: {
            fontWeight: '600',
            color: result.summary.totalErrors > 0 ? '#EF4444' : '#10B981'
          }
        }, result.summary.totalErrors)
      ]),
      hasWarnings && React.createElement('div', {
        key: 'warnings',
        style: { display: 'flex', alignItems: 'center', gap: '8px' }
      }, [
        React.createElement('span', {
          key: 'label',
          style: { color: '#6B7280' }
        }, '警告:'),
        React.createElement('span', {
          key: 'count',
          style: {
            fontWeight: '600',
            color: '#F59E0B'
          }
        }, result.summary.totalWarnings)
      ]),
      result.summary.criticalErrors > 0 && React.createElement('div', {
        key: 'critical',
        style: { display: 'flex', alignItems: 'center', gap: '8px' }
      }, [
        React.createElement('span', {
          key: 'label',
          style: { color: '#6B7280' }
        }, '严重错误:'),
        React.createElement('span', {
          key: 'count',
          style: {
            fontWeight: '600',
            color: '#DC2626'
          }
        }, result.summary.criticalErrors)
      ])
    ]),

    // Tabs
    (hasErrors || hasWarnings) && React.createElement('div', {
      key: 'tabs',
      style: {
        padding: '0 24px',
        borderBottom: '1px solid #E5E7EB',
        display: 'flex',
        gap: '24px'
      }
    }, [
      hasErrors && React.createElement('button', {
        key: 'errors-tab',
        onClick: () => setActiveTab('errors'),
        style: {
          padding: '12px 0',
          border: 'none',
          background: 'none',
          fontSize: '14px',
          fontWeight: '500',
          color: activeTab === 'errors' ? '#EF4444' : '#6B7280',
          borderBottom: `2px solid ${activeTab === 'errors' ? '#EF4444' : 'transparent'}`,
          cursor: 'pointer'
        }
      }, `错误 (${result.errors.length})`),
      hasWarnings && options.showWarnings && React.createElement('button', {
        key: 'warnings-tab',
        onClick: () => setActiveTab('warnings'),
        style: {
          padding: '12px 0',
          border: 'none',
          background: 'none',
          fontSize: '14px',
          fontWeight: '500',
          color: activeTab === 'warnings' ? '#F59E0B' : '#6B7280',
          borderBottom: `2px solid ${activeTab === 'warnings' ? '#F59E0B' : 'transparent'}`,
          cursor: 'pointer'
        }
      }, `警告 (${result.warnings?.length || 0})`),
      options.showSummary && React.createElement('button', {
        key: 'summary-tab',
        onClick: () => setActiveTab('summary'),
        style: {
          padding: '12px 0',
          border: 'none',
          background: 'none',
          fontSize: '14px',
          fontWeight: '500',
          color: activeTab === 'summary' ? '#3B82F6' : '#6B7280',
          borderBottom: `2px solid ${activeTab === 'summary' ? '#3B82F6' : 'transparent'}`,
          cursor: 'pointer'
        }
      }, '详情')
    ]),

    // Content
    React.createElement('div', {
      key: 'content',
      style: {
        flex: 1,
        overflow: 'auto',
        maxHeight: `${options.maxHeight}px`,
        padding: '20px 24px'
      }
    }, [
      // Success message
      result.valid && React.createElement('div', {
        key: 'success',
        style: {
          textAlign: 'center',
          padding: '40px 20px',
          fontSize: '16px',
          color: '#10B981'
        }
      }, '验证通过！所有检查项目都已通过。'),

      // Errors tab
      activeTab === 'errors' && hasErrors && React.createElement(ErrorList, {
        key: 'errors',
        errors: result.errors,
        groupByField: options.groupByField,
        type: 'error'
      }),

      // Warnings tab
      activeTab === 'warnings' && hasWarnings && React.createElement(ErrorList, {
        key: 'warnings',
        errors: result.warnings || [],
        groupByField: options.groupByField,
        type: 'warning'
      }),

      // Summary tab
      activeTab === 'summary' && options.showSummary && React.createElement('div', {
        key: 'summary-content'
      }, [
        result.metadata && React.createElement('div', {
          key: 'metadata',
          style: {
            backgroundColor: '#F9FAFB',
            padding: '16px',
            borderRadius: '6px',
            fontSize: '13px',
            marginBottom: '20px'
          }
        }, [
          React.createElement('h4', {
            key: 'title',
            style: {
              margin: '0 0 8px 0',
              fontSize: '14px',
              fontWeight: '600',
              color: '#374151'
            }
          }, '验证元数据'),
          React.createElement('div', {
            key: 'details',
            style: { color: '#6B7280', lineHeight: '1.4' }
          }, [
            React.createElement('div', {
              key: 'time'
            }, `验证时间: ${new Date(result.metadata.validatedAt).toLocaleString()}`),
            result.metadata.processingTime && React.createElement('div', {
              key: 'processing-time'
            }, `处理时间: ${result.metadata.processingTime}ms`),
            result.metadata.validatorVersion && React.createElement('div', {
              key: 'version'
            }, `验证器版本: ${result.metadata.validatorVersion}`)
          ])
        ]),
        React.createElement('div', {
          key: 'summary-stats',
          style: {
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
            gap: '16px'
          }
        }, [
          React.createElement(StatCard, {
            key: 'total-issues',
            title: '总问题数',
            value: result.errors.length + (result.warnings?.length || 0),
            color: result.errors.length > 0 ? '#EF4444' : '#F59E0B'
          }),
          React.createElement(StatCard, {
            key: 'errors',
            title: '错误',
            value: result.errors.length,
            color: '#EF4444'
          }),
          hasWarnings && React.createElement(StatCard, {
            key: 'warnings',
            title: '警告',
            value: result.warnings?.length || 0,
            color: '#F59E0B'
          }),
          result.summary?.criticalErrors && React.createElement(StatCard, {
            key: 'critical',
            title: '严重错误',
            value: result.summary.criticalErrors,
            color: '#DC2626'
          })
        ])
      ])
    ]),

    // Footer
    React.createElement('div', {
      key: 'footer',
      style: {
        padding: '16px 24px 20px',
        borderTop: '1px solid #E5E7EB',
        display: 'flex',
        justifyContent: 'flex-end',
        gap: '12px'
      }
    }, [
      hasWarnings && !hasErrors && React.createElement('button', {
        key: 'ignore-warnings',
        onClick: handleIgnoreWarnings,
        style: {
          padding: '8px 16px',
          border: '1px solid #F59E0B',
          borderRadius: '6px',
          backgroundColor: 'white',
          color: '#F59E0B',
          fontSize: '14px',
          fontWeight: '500',
          cursor: 'pointer'
        }
      }, '忽略警告'),
      hasErrors && options.onRetry && options.onRetry.toString() !== '() => {}' && React.createElement('button', {
        key: 'retry',
        onClick: handleRetry,
        style: {
          padding: '8px 16px',
          border: 'none',
          borderRadius: '6px',
          backgroundColor: '#3B82F6',
          color: 'white',
          fontSize: '14px',
          fontWeight: '500',
          cursor: 'pointer'
        }
      }, '重新验证'),
      React.createElement('button', {
        key: 'close',
        onClick: () => onClose(id),
        style: {
          padding: '8px 16px',
          border: '1px solid #D1D5DB',
          borderRadius: '6px',
          backgroundColor: 'white',
          color: '#374151',
          fontSize: '14px',
          fontWeight: '500',
          cursor: 'pointer'
        }
      }, '关闭')
    ])
  ]));
};
ValidationDisplayComponent.displayName = 'ValidationDisplayComponent';

// Helper Components
interface ErrorListProps {
  errors: ValidationError[];
  groupByField: boolean;
  type: 'error' | 'warning';
}

// eslint-disable-next-line react-refresh/only-export-components
const ErrorList = ({ errors, groupByField, type }: ErrorListProps) => {
  if (errors.length === 0) {
    return React.createElement('div', {
      style: {
        textAlign: 'center',
        padding: '40px 20px',
        color: '#6B7280',
        fontSize: '14px'
      }
    }, `没有${type === 'error' ? '错误' : '警告'}`);
  }

  const color = type === 'error' ? '#EF4444' : '#F59E0B';
  const bgColor = type === 'error' ? '#FEF2F2' : '#FFFBEB';

  if (groupByField) {
    const grouped = errors.reduce((acc, error) => {
      if (!acc[error.field]) {
        acc[error.field] = [];
      }
      acc[error.field].push(error);
      return acc;
    }, {} as Record<string, ValidationError[]>);

    return React.createElement('div', {
      style: { display: 'flex', flexDirection: 'column', gap: '16px' }
    }, Object.entries(grouped).map(([field, fieldErrors]) =>
      React.createElement('div', {
        key: field,
        style: {
          border: `1px solid ${color}`,
          borderRadius: '6px',
          overflow: 'hidden'
        }
      }, [
        React.createElement('div', {
          key: 'header',
          style: {
            padding: '12px 16px',
            backgroundColor: bgColor,
            borderBottom: `1px solid ${color}`,
            fontWeight: '600',
            fontSize: '14px',
            color: '#374151'
          }
        }, field || '通用'),
        React.createElement('div', {
          key: 'errors',
          style: { padding: '12px 16px' }
        }, fieldErrors.map((error, index) =>
          React.createElement('div', {
            key: index,
            style: {
              padding: '8px 0',
              borderBottom: index < fieldErrors.length - 1 ? '1px solid #E5E7EB' : 'none',
              fontSize: '13px',
              lineHeight: '1.4',
              color: '#374151'
            }
          }, [
            React.createElement('div', {
              key: 'message',
              style: { marginBottom: '4px' }
            }, error.message),
            error.code && React.createElement('div', {
              key: 'code',
              style: {
                fontSize: '11px',
                color: '#6B7280',
                fontFamily: 'monospace',
                backgroundColor: '#F3F4F6',
                padding: '2px 6px',
                borderRadius: '3px',
                display: 'inline-block'
              }
            }, error.code)
          ])
        ))
      ])
    ));
  } else {
    return React.createElement('div', {
      style: { display: 'flex', flexDirection: 'column', gap: '8px' }
    }, errors.map((error, index) =>
      React.createElement('div', {
        key: index,
        style: {
          padding: '12px 16px',
          border: `1px solid ${color}`,
          borderRadius: '6px',
          backgroundColor: bgColor,
          fontSize: '13px',
          lineHeight: '1.4'
        }
      }, [
        React.createElement('div', {
          key: 'field',
          style: {
            fontWeight: '600',
            color: color,
            marginBottom: '4px'
          }
        }, error.field),
        React.createElement('div', {
          key: 'message',
          style: {
            color: '#374151',
            marginBottom: error.code ? '6px' : 0
          }
        }, error.message),
        error.code && React.createElement('div', {
          key: 'code',
          style: {
            fontSize: '11px',
            color: '#6B7280',
            fontFamily: 'monospace',
            backgroundColor: 'white',
            padding: '4px 8px',
            borderRadius: '3px',
            border: '1px solid #E5E7EB',
            display: 'inline-block'
          }
        }, error.code)
      ])
    ));
  }
};
ErrorList.displayName = 'ErrorList';

interface StatCardProps {
  title: string;
  value: number;
  color: string;
}

// eslint-disable-next-line react-refresh/only-export-components
const StatCard = ({ title, value, color }: StatCardProps) => {
  return React.createElement('div', {
    style: {
      padding: '16px',
      border: `1px solid ${color}`,
      borderRadius: '6px',
      backgroundColor: 'white',
      textAlign: 'center'
    }
  }, [
    React.createElement('div', {
      key: 'value',
      style: {
        fontSize: '24px',
        fontWeight: '700',
        color,
        marginBottom: '4px'
      }
    }, value),
    React.createElement('div', {
      key: 'title',
      style: {
        fontSize: '12px',
        color: '#6B7280',
        textTransform: 'uppercase',
        letterSpacing: '0.05em'
      }
    }, title)
  ]);
};
StatCard.displayName = 'StatCard';

// Create singleton instance
export const validationDisplay = new ValidationDisplayService();

// Export convenience functions
export const showValidation = validationDisplay.show.bind(validationDisplay);
export const showValidationErrors = validationDisplay.showErrors.bind(validationDisplay);
export const showValidationSummary = validationDisplay.showSummary.bind(validationDisplay);

export default validationDisplay;
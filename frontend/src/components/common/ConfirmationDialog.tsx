/**
 * Confirmation Dialog Component
 * 
 * Accessible confirmation dialog for critical user actions with
 * customizable severity levels and action buttons.
 */

import React from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/Button';
import { Alert, AlertDescription } from '@/components/ui/alert';
import {
  AlertTriangle,
  Trash2,
  UserX,
  Shield,
  Clock,
  Info,
  CheckCircle,
  XCircle,
} from 'lucide-react';

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

export interface ConfirmationAction {
  label: string;
  variant?: 'default' | 'destructive' | 'outline' | 'secondary' | 'ghost' | 'link';
  loading?: boolean;
  disabled?: boolean;
  autoFocus?: boolean;
  onClick: () => void | Promise<void>;
}

export interface ConfirmationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: string;
  severity?: 'info' | 'warning' | 'danger' | 'success';
  icon?: React.ReactNode;
  children?: React.ReactNode;
  confirmAction: ConfirmationAction;
  cancelAction?: ConfirmationAction;
  additionalActions?: ConfirmationAction[];
  showAlert?: boolean;
  alertMessage?: string;
  loading?: boolean;
  className?: string;
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl';
  preventClose?: boolean;
  requireConfirmation?: {
    text: string;
    placeholder?: string;
    caseSensitive?: boolean;
  };
  countdown?: {
    seconds: number;
    message?: string;
  };
}

// ============================================================================
// CONFIRMATION DIALOG COMPONENT
// ============================================================================

export const ConfirmationDialog: React.FC<ConfirmationDialogProps> = ({
  open,
  onOpenChange,
  title,
  description,
  severity = 'warning',
  icon,
  children,
  confirmAction,
  cancelAction,
  additionalActions = [],
  showAlert = false,
  alertMessage,
  loading = false,
  className,
  maxWidth = 'md',
  preventClose = false,
  requireConfirmation,
  countdown,
}) => {
  const [confirmationText, setConfirmationText] = React.useState('');
  const [countdownSeconds, setCountdownSeconds] = React.useState(countdown?.seconds || 0);

  // Handle countdown timer
  React.useEffect(() => {
    if (countdown && open && countdownSeconds > 0) {
      const timer = setTimeout(() => {
        setCountdownSeconds(prev => prev - 1);
      }, 1000);

      return () => clearTimeout(timer);
    }
  }, [countdown, open, countdownSeconds]);

  // Reset countdown when dialog opens
  React.useEffect(() => {
    if (open && countdown) {
      setCountdownSeconds(countdown.seconds);
    }
  }, [open, countdown]);

  // Reset confirmation text when dialog closes
  React.useEffect(() => {
    if (!open) {
      setConfirmationText('');
    }
  }, [open]);

  // Check if confirmation text matches required text
  const isConfirmationValid = React.useMemo(() => {
    if (!requireConfirmation) return true;
    
    const userText = requireConfirmation.caseSensitive 
      ? confirmationText 
      : confirmationText.toLowerCase();
    const requiredText = requireConfirmation.caseSensitive 
      ? requireConfirmation.text 
      : requireConfirmation.text.toLowerCase();
    
    return userText === requiredText;
  }, [confirmationText, requireConfirmation]);

  // Check if countdown has finished
  const isCountdownFinished = !countdown || countdownSeconds <= 0;

  // Get severity configuration
  const severityConfig = {
    info: {
      color: 'blue',
      defaultIcon: <Info className="h-6 w-6" />,
      alertVariant: 'default' as const,
    },
    success: {
      color: 'green',
      defaultIcon: <CheckCircle className="h-6 w-6" />,
      alertVariant: 'default' as const,
    },
    warning: {
      color: 'yellow',
      defaultIcon: <AlertTriangle className="h-6 w-6" />,
      alertVariant: 'default' as const,
    },
    danger: {
      color: 'red',
      defaultIcon: <XCircle className="h-6 w-6" />,
      alertVariant: 'destructive' as const,
    },
  };

  const config = severityConfig[severity];
  const displayIcon = icon || config.defaultIcon;

  const handleConfirm = async () => {
    try {
      await confirmAction.onClick();
    } catch (error) {
      console.error('Confirmation action failed:', error);
    }
  };

  const handleCancel = async () => {
    if (cancelAction) {
      try {
        await cancelAction.onClick();
      } catch (error) {
        console.error('Cancel action failed:', error);
      }
    } else {
      onOpenChange(false);
    }
  };

  const handleOpenChange = (newOpen: boolean) => {
    if (preventClose && loading) return;
    onOpenChange(newOpen);
  };

  const maxWidthClasses = {
    sm: 'max-w-sm',
    md: 'max-w-md',
    lg: 'max-w-lg',
    xl: 'max-w-xl',
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className={`${maxWidthClasses[maxWidth]} ${className || ''}`}>
        <DialogHeader>
          <DialogTitle className="flex items-center space-x-3">
            {displayIcon && (
              <div className={`text-${config.color}-600 dark:text-${config.color}-400`}>
                {displayIcon}
              </div>
            )}
            <span>{title}</span>
          </DialogTitle>
          <DialogDescription className="text-left">
            {description}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {/* Alert Message */}
          {showAlert && alertMessage && (
            <Alert variant={config.alertVariant}>
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>{alertMessage}</AlertDescription>
            </Alert>
          )}

          {/* Countdown Timer */}
          {countdown && countdownSeconds > 0 && (
            <Alert>
              <Clock className="h-4 w-4" />
              <AlertDescription>
                {countdown.message || `Please wait ${countdownSeconds} seconds before confirming.`}
              </AlertDescription>
            </Alert>
          )}

          {/* Additional Content */}
          {children}

          {/* Confirmation Text Input */}
          {requireConfirmation && (
            <div className="space-y-2">
              <label 
                htmlFor="confirmation-text" 
                className="text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Type &quot;{requireConfirmation.text}&quot; to confirm:
              </label>
              <input
                id="confirmation-text"
                type="text"
                value={confirmationText}
                onChange={(e) => setConfirmationText(e.target.value)}
                placeholder={requireConfirmation.placeholder || requireConfirmation.text}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent dark:border-gray-600 dark:bg-gray-700 dark:text-white"
                disabled={loading}
                autoComplete="off"
                spellCheck={false}
              />
              {!isConfirmationValid && confirmationText.length > 0 && (
                <p className="text-sm text-red-600 dark:text-red-400">
                  Confirmation text does not match.
                </p>
              )}
            </div>
          )}
        </div>

        <DialogFooter className="flex justify-end space-x-2">
          {/* Additional Actions */}
          {additionalActions.map((action, index) => (
            <Button
              key={index}
              variant={action.variant || 'outline'}
              disabled={action.disabled || loading}
              onClick={action.onClick}
              className="order-1"
            >
              {action.loading && (
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2" />
              )}
              {action.label}
            </Button>
          ))}

          {/* Cancel Button */}
          <Button
            variant={cancelAction?.variant || 'outline'}
            disabled={cancelAction?.disabled || loading}
            onClick={handleCancel}
            className="order-2"
          >
            {cancelAction?.loading && (
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2" />
            )}
            {cancelAction?.label || 'Cancel'}
          </Button>

          {/* Confirm Button */}
          <Button
            variant={confirmAction.variant || (severity === 'danger' ? 'destructive' : 'default')}
            disabled={
              confirmAction.disabled || 
              loading || 
              !isConfirmationValid || 
              !isCountdownFinished
            }
            onClick={handleConfirm}
            autoFocus={confirmAction.autoFocus}
            className="order-3"
          >
            {confirmAction.loading && (
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2" />
            )}
            {confirmAction.label}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

// ============================================================================
// PRESET CONFIRMATION DIALOGS
// ============================================================================

export interface DeleteConfirmationProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title?: string;
  itemName: string;
  itemType?: string;
  onConfirm: () => void | Promise<void>;
  onCancel?: () => void;
  loading?: boolean;
  requireConfirmation?: boolean;
  additionalWarning?: string;
}

export const DeleteConfirmation: React.FC<DeleteConfirmationProps> = ({
  open,
  onOpenChange,
  title,
  itemName,
  itemType = 'item',
  onConfirm,
  onCancel,
  loading = false,
  requireConfirmation = false,
  additionalWarning,
}) => {
  const confirmationText = requireConfirmation ? 'DELETE' : undefined;

  return (
    <ConfirmationDialog
      open={open}
      onOpenChange={onOpenChange}
      title={title || `Delete ${itemType}`}
      description={`Are you sure you want to delete "${itemName}"? This action cannot be undone.`}
      severity="danger"
      icon={<Trash2 className="h-6 w-6" />}
      confirmAction={{
        label: loading ? 'Deleting...' : 'Delete',
        variant: 'destructive',
        loading,
        onClick: onConfirm,
      }}
      cancelAction={onCancel ? { label: 'Cancel', onClick: onCancel } : undefined}
      showAlert={!!additionalWarning}
      alertMessage={additionalWarning}
      requireConfirmation={confirmationText ? {
        text: confirmationText,
        placeholder: 'Type DELETE to confirm',
        caseSensitive: true,
      } : undefined}
      loading={loading}
    />
  );
};

export interface BulkActionConfirmationProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  action: string;
  itemCount: number;
  itemType?: string;
  onConfirm: () => void | Promise<void>;
  onCancel?: () => void;
  loading?: boolean;
  severity?: 'info' | 'warning' | 'danger';
  additionalInfo?: string;
}

export const BulkActionConfirmation: React.FC<BulkActionConfirmationProps> = ({
  open,
  onOpenChange,
  action,
  itemCount,
  itemType = 'items',
  onConfirm,
  onCancel,
  loading = false,
  severity = 'warning',
  additionalInfo,
}) => {
  const getIcon = () => {
    switch (action.toLowerCase()) {
      case 'delete':
        return <Trash2 className="h-6 w-6" />;
      case 'suspend':
      case 'deactivate':
        return <UserX className="h-6 w-6" />;
      case 'activate':
      case 'unsuspend':
        return <CheckCircle className="h-6 w-6" />;
      default:
        return <Shield className="h-6 w-6" />;
    }
  };

  const isDangerous = ['delete', 'suspend', 'ban'].includes(action.toLowerCase());
  const finalSeverity = isDangerous ? 'danger' : severity;

  return (
    <ConfirmationDialog
      open={open}
      onOpenChange={onOpenChange}
      title={`Bulk ${action}`}
      description={`Are you sure you want to ${action.toLowerCase()} ${itemCount} ${itemType}?`}
      severity={finalSeverity}
      icon={getIcon()}
      confirmAction={{
        label: loading ? `${action}ing...` : `${action} ${itemCount} ${itemType}`,
        variant: isDangerous ? 'destructive' : 'default',
        loading,
        onClick: onConfirm,
      }}
      cancelAction={onCancel ? { label: 'Cancel', onClick: onCancel } : undefined}
      showAlert={isDangerous || !!additionalInfo}
      alertMessage={
        additionalInfo || 
        (isDangerous ? 'This action may have permanent consequences.' : undefined)
      }
      loading={loading}
    />
  );
};

// ============================================================================
// UTILITY FUNCTIONS
// ============================================================================

/**
 * Simple confirmation dialog utility
 * Returns a promise that resolves to true if confirmed, false if cancelled
 */
// eslint-disable-next-line react-refresh/only-export-components
export const confirmSimple = (
  title: string,
  message: string,
  options: {
    confirmLabel?: string;
    cancelLabel?: string;
    severity?: 'info' | 'warning' | 'danger' | 'success';
    requireConfirmation?: boolean;
  } = {}
): Promise<boolean> => {
  return new Promise((resolve) => {
    let isResolved = false;

    const handleConfirm = () => {
      if (!isResolved) {
        isResolved = true;
        resolve(true);
      }
    };

    const handleCancel = () => {
      if (!isResolved) {
        isResolved = true;
        resolve(false);
      }
    };

    // Create a temporary container for the dialog
    const container = document.createElement('div');
    document.body.appendChild(container);

    // Import React and render the dialog
    import('react').then(React => {
      import('react-dom/client').then(ReactDOM => {
        const root = ReactDOM.createRoot(container);

        const DialogWrapper = () => {
          const [open, setOpen] = React.useState(true);

          const handleOpenChange = (newOpen: boolean) => {
            setOpen(newOpen);
            if (!newOpen && !isResolved) {
              handleCancel();
              cleanup();
            }
          };

          const cleanup = () => {
            setTimeout(() => {
              root.unmount();
              document.body.removeChild(container);
            }, 100);
          };

          const onConfirm = () => {
            handleConfirm();
            setOpen(false);
            cleanup();
          };

          const onCancel = () => {
            handleCancel();
            setOpen(false);
            cleanup();
          };

          return React.createElement(ConfirmationDialog, {
            open,
            onOpenChange: handleOpenChange,
            title,
            description: message,
            severity: options.severity || 'warning',
            confirmAction: {
              label: options.confirmLabel || 'Confirm',
              onClick: onConfirm,
              variant: options.severity === 'danger' ? 'destructive' : 'default'
            },
            cancelAction: {
              label: options.cancelLabel || 'Cancel',
              onClick: onCancel
            },
            requireConfirmation: options.requireConfirmation ? {
              text: 'CONFIRM',
              placeholder: 'Type CONFIRM to proceed',
              caseSensitive: true
            } : undefined
          });
        };

        root.render(React.createElement(DialogWrapper));
      });
    });
  });
};

// ============================================================================
// EXPORT DEFAULT
// ============================================================================

export default ConfirmationDialog;
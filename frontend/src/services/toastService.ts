/**
 * Toast Notification Service
 * 
 * Enterprise-grade toast notification system with queuing, persistence,
 * and accessibility features.
 */

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

export interface ToastOptions {
  id?: string;
  title?: string;
  message: string;
  type?: 'success' | 'error' | 'warning' | 'info';
  duration?: number; // in milliseconds, 0 for persistent
  closable?: boolean;
  actions?: ToastAction[];
  persistent?: boolean;
  priority?: 'low' | 'normal' | 'high' | 'critical';
  groupId?: string; // For grouping related notifications
  metadata?: Record<string, unknown>;
  onShow?: (toast: Toast) => void;
  onHide?: (toast: Toast) => void;
  onClick?: (toast: Toast) => void;
  onAction?: (toast: Toast, actionId: string) => void;
}

export interface ToastAction {
  id: string;
  label: string;
  variant?: 'primary' | 'secondary' | 'danger';
  disabled?: boolean;
  loading?: boolean;
}

export interface Toast {
  id: string;
  title: string;
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
  duration: number;
  closable: boolean;
  actions: ToastAction[];
  persistent: boolean;
  priority: 'low' | 'normal' | 'high' | 'critical';
  groupId: string;
  metadata: Record<string, unknown>;
  createdAt: Date;
  updatedAt: Date;
  showCount: number;
  dismissed: boolean;
  expired: boolean;
  onShow?: (toast: Toast) => void;
  onHide?: (toast: Toast) => void;
  onClick?: (toast: Toast) => void;
  onAction?: (toast: Toast, actionId: string) => void;
}

export interface ToastServiceConfig {
  maxToasts?: number;
  defaultDuration?: number;
  position?: 'top-left' | 'top-right' | 'bottom-left' | 'bottom-right' | 'top-center' | 'bottom-center';
  enablePersistence?: boolean;
  persistenceKey?: string;
  enableGrouping?: boolean;
  enableSounds?: boolean;
  respectReducedMotion?: boolean;
  enableAnalytics?: boolean;
}

// ============================================================================
// TOAST SERVICE CLASS
// ============================================================================

class ToastService {
  private toasts: Map<string, Toast> = new Map();
  private subscribers: Set<(toasts: Toast[]) => void> = new Set();
  private config: Required<ToastServiceConfig>;
  private timeouts: Map<string, NodeJS.Timeout> = new Map();
  private soundContext?: AudioContext;
  private sounds: Map<string, AudioBuffer> = new Map();
  private analytics: Array<{ event: string; toastId: string; timestamp: Date }> = [];

  constructor(config: ToastServiceConfig = {}) {
    this.config = {
      maxToasts: config.maxToasts ?? 5,
      defaultDuration: config.defaultDuration ?? 5000,
      position: config.position ?? 'top-right',
      enablePersistence: config.enablePersistence ?? false,
      persistenceKey: config.persistenceKey ?? 'toast_service_state',
      enableGrouping: config.enableGrouping ?? true,
      enableSounds: config.enableSounds ?? false,
      respectReducedMotion: config.respectReducedMotion ?? true,
      enableAnalytics: config.enableAnalytics ?? false,
    };

    this.initialize();
  }

  private initialize(): void {
    // Load persisted toasts
    if (this.config.enablePersistence && typeof window !== 'undefined') {
      this.loadPersistedToasts();
    }

    // Initialize audio context for sounds
    if (this.config.enableSounds && typeof window !== 'undefined') {
      this.initializeSounds();
    }

    // Set up persistence save on page unload
    if (this.config.enablePersistence && typeof window !== 'undefined') {
      window.addEventListener('beforeunload', () => {
        this.persistToasts();
      });
    }
  }

  private generateId(): string {
    return `toast_${Date.now()}_${Math.random().toString(36).substring(2)}`;
  }

  private loadPersistedToasts(): void {
    try {
      const stored = localStorage.getItem(this.config.persistenceKey);
      if (stored) {
        interface PersistedToast {
          id: string;
          title: string;
          message: string;
          type: 'success' | 'error' | 'warning' | 'info';
          duration: number;
          closable: boolean;
          actions: ToastAction[];
          persistent: boolean;
          priority: 'low' | 'normal' | 'high' | 'critical';
          groupId: string;
          metadata: Record<string, unknown>;
          createdAt: string;
          updatedAt: string;
          showCount: number;
          dismissed: boolean;
          expired: boolean;
        }
        const persistedToasts = JSON.parse(stored) as PersistedToast[];

        persistedToasts
          .filter(toast => toast.persistent && !toast.dismissed)
          .forEach(toastData => {
            const toast: Toast = {
              ...toastData,
              createdAt: new Date(toastData.createdAt),
              updatedAt: new Date(toastData.updatedAt),
            };
            
            this.toasts.set(toast.id, toast);
            this.scheduleAutoHide(toast);
          });

        this.notifySubscribers();
      }
    } catch (error) {
      console.warn('Failed to load persisted toasts:', error);
    }
  }

  private persistToasts(): void {
    try {
      const persistableToasts = Array.from(this.toasts.values())
        .filter(toast => toast.persistent && !toast.dismissed)
        .map(toast => ({
          ...toast,
          // Remove functions for JSON serialization
          onShow: undefined,
          onHide: undefined,
          onClick: undefined,
          onAction: undefined,
        }));

      localStorage.setItem(this.config.persistenceKey, JSON.stringify(persistableToasts));
    } catch (error) {
      console.warn('Failed to persist toasts:', error);
    }
  }

  private async initializeSounds(): Promise<void> {
    try {
      this.soundContext = new AudioContext();
      
      // Generate simple notification sounds
      const successSound = this.generateTone(800, 0.1, 'sine');
      const errorSound = this.generateTone(400, 0.2, 'sawtooth');
      const warningSound = this.generateTone(600, 0.15, 'triangle');
      const infoSound = this.generateTone(500, 0.1, 'sine');

      this.sounds.set('success', await successSound);
      this.sounds.set('error', await errorSound);
      this.sounds.set('warning', await warningSound);
      this.sounds.set('info', await infoSound);
    } catch (error) {
      console.warn('Failed to initialize toast sounds:', error);
    }
  }

  private async generateTone(frequency: number, duration: number, type: OscillatorType): Promise<AudioBuffer> {
    if (!this.soundContext) throw new Error('Audio context not initialized');

    const sampleRate = this.soundContext.sampleRate;
    const numSamples = sampleRate * duration;
    const buffer = this.soundContext.createBuffer(1, numSamples, sampleRate);
    const channelData = buffer.getChannelData(0);

    for (let i = 0; i < numSamples; i++) {
      const t = i / sampleRate;
      let sample: number;
      
      switch (type) {
        case 'sine':
          sample = Math.sin(2 * Math.PI * frequency * t);
          break;
        case 'sawtooth':
          sample = 2 * ((frequency * t) % 1) - 1;
          break;
        case 'triangle':
          sample = 2 * Math.abs(2 * ((frequency * t) % 1) - 1) - 1;
          break;
        default:
          sample = Math.sin(2 * Math.PI * frequency * t);
      }

      // Apply fade out to prevent clicking
      const fadeOut = Math.max(0, 1 - (t / duration) * 2);
      channelData[i] = sample * fadeOut * 0.1; // Low volume
    }

    return buffer;
  }

  private playSound(type: Toast['type']): void {
    if (!this.config.enableSounds || !this.soundContext || !this.sounds.has(type)) {
      return;
    }

    try {
      const buffer = this.sounds.get(type);
      if (buffer) {
        const source = this.soundContext.createBufferSource();
        source.buffer = buffer;
        source.connect(this.soundContext.destination);
        source.start();
      }
    } catch (error) {
      console.warn(`Failed to play toast sound for type ${type}:`, error);
    }
  }

  private scheduleAutoHide(toast: Toast): void {
    if (toast.duration > 0) {
      const timeout = setTimeout(() => {
        this.hide(toast.id, 'timeout');
      }, toast.duration);
      
      this.timeouts.set(toast.id, timeout);
    }
  }

  private clearTimeout(toastId: string): void {
    const timeout = this.timeouts.get(toastId);
    if (timeout) {
      clearTimeout(timeout);
      this.timeouts.delete(toastId);
    }
  }

  private enforceMaxToasts(): void {
    const toastArray = Array.from(this.toasts.values())
      .filter(toast => !toast.dismissed)
      .sort((a, b) => {
        // Sort by priority first, then by creation time
        const priorityOrder = { critical: 4, high: 3, normal: 2, low: 1 };
        const priorityDiff = priorityOrder[b.priority] - priorityOrder[a.priority];
        if (priorityDiff !== 0) return priorityDiff;
        return b.createdAt.getTime() - a.createdAt.getTime();
      });

    // Remove excess toasts (lowest priority, oldest first)
    const toastsToRemove = toastArray.slice(this.config.maxToasts);
    toastsToRemove.forEach(toast => {
      if (toast.priority !== 'critical') {
        this.hide(toast.id, 'overflow');
      }
    });
  }

  private notifySubscribers(): void {
    const activeToasts = Array.from(this.toasts.values())
      .filter(toast => !toast.dismissed)
      .sort((a, b) => {
        const priorityOrder = { critical: 4, high: 3, normal: 2, low: 1 };
        const priorityDiff = priorityOrder[b.priority] - priorityOrder[a.priority];
        if (priorityDiff !== 0) return priorityDiff;
        return b.createdAt.getTime() - a.createdAt.getTime();
      });

    this.subscribers.forEach(callback => {
      try {
        callback(activeToasts);
      } catch (error) {
        console.error('Error in toast subscriber callback:', error);
      }
    });
  }

  private trackAnalytics(event: string, toastId: string): void {
    if (this.config.enableAnalytics) {
      this.analytics.push({
        event,
        toastId,
        timestamp: new Date(),
      });

      // Keep only last 1000 events
      if (this.analytics.length > 1000) {
        this.analytics = this.analytics.slice(-1000);
      }
    }
  }

  // ========================================================================
  // PUBLIC API
  // ========================================================================

  /**
   * Show a toast notification
   */
  show(options: ToastOptions): string {
    const now = new Date();
    const toast: Toast = {
      id: options.id || this.generateId(),
      title: options.title || '',
      message: options.message,
      type: options.type || 'info',
      duration: options.duration ?? this.config.defaultDuration,
      closable: options.closable ?? true,
      actions: options.actions || [],
      persistent: options.persistent ?? false,
      priority: options.priority || 'normal',
      groupId: options.groupId || '',
      metadata: options.metadata || {},
      createdAt: now,
      updatedAt: now,
      showCount: 0,
      dismissed: false,
      expired: false,
      onShow: options.onShow,
      onHide: options.onHide,
      onClick: options.onClick,
      onAction: options.onAction,
    };

    // Handle grouping
    if (this.config.enableGrouping && toast.groupId) {
      // Remove existing toasts in the same group
      Array.from(this.toasts.values())
        .filter(existingToast => existingToast.groupId === toast.groupId && !existingToast.dismissed)
        .forEach(existingToast => this.hide(existingToast.id, 'grouped'));
    }

    this.toasts.set(toast.id, toast);
    this.enforceMaxToasts();
    this.scheduleAutoHide(toast);
    this.playSound(toast.type);

    // Update show count and call onShow callback
    toast.showCount++;
    toast.onShow?.(toast);

    this.trackAnalytics('show', toast.id);
    this.notifySubscribers();

    return toast.id;
  }

  /**
   * Hide a specific toast
   */
  hide(toastId: string, reason: 'user' | 'timeout' | 'overflow' | 'grouped' | 'programmatic' = 'programmatic'): boolean {
    const toast = this.toasts.get(toastId);
    if (!toast || toast.dismissed) return false;

    toast.dismissed = true;
    toast.updatedAt = new Date();
    toast.expired = reason === 'timeout';
    
    this.clearTimeout(toastId);
    toast.onHide?.(toast);

    this.trackAnalytics(`hide_${reason}`, toastId);
    this.notifySubscribers();

    // Clean up non-persistent toasts after animation time
    if (!toast.persistent) {
      setTimeout(() => {
        this.toasts.delete(toastId);
      }, 300); // Allow time for exit animation
    }

    return true;
  }

  /**
   * Hide all toasts
   */
  hideAll(reason: 'user' | 'programmatic' = 'programmatic'): number {
    let hiddenCount = 0;
    
    Array.from(this.toasts.values())
      .filter(toast => !toast.dismissed)
      .forEach(toast => {
        if (this.hide(toast.id, reason)) {
          hiddenCount++;
        }
      });

    return hiddenCount;
  }

  /**
   * Update an existing toast
   */
  update(toastId: string, updates: Partial<ToastOptions>): boolean {
    const toast = this.toasts.get(toastId);
    if (!toast || toast.dismissed) return false;

    // Clear existing timeout if duration is being updated
    if (updates.duration !== undefined) {
      this.clearTimeout(toastId);
    }

    // Update toast properties
    Object.assign(toast, {
      ...updates,
      id: toastId, // Ensure ID doesn't change
      updatedAt: new Date(),
    });

    // Reschedule auto-hide if duration was updated
    if (updates.duration !== undefined) {
      this.scheduleAutoHide(toast);
    }

    this.trackAnalytics('update', toastId);
    this.notifySubscribers();

    return true;
  }

  /**
   * Get a specific toast by ID
   */
  get(toastId: string): Toast | undefined {
    return this.toasts.get(toastId);
  }

  /**
   * Get all active toasts
   */
  getAll(): Toast[] {
    return Array.from(this.toasts.values()).filter(toast => !toast.dismissed);
  }

  /**
   * Subscribe to toast updates
   */
  subscribe(callback: (toasts: Toast[]) => void): () => void {
    this.subscribers.add(callback);
    
    // Immediately call with current toasts
    callback(this.getAll());
    
    // Return unsubscribe function
    return () => {
      this.subscribers.delete(callback);
    };
  }

  /**
   * Handle toast action clicks
   */
  handleAction(toastId: string, actionId: string): void {
    const toast = this.toasts.get(toastId);
    if (!toast || toast.dismissed) return;

    toast.onAction?.(toast, actionId);
    this.trackAnalytics(`action_${actionId}`, toastId);
  }

  /**
   * Handle toast clicks
   */
  handleClick(toastId: string): void {
    const toast = this.toasts.get(toastId);
    if (!toast || toast.dismissed) return;

    toast.onClick?.(toast);
    this.trackAnalytics('click', toastId);
  }

  /**
   * Clear all toasts and reset state
   */
  clear(): void {
    // Clear all timeouts
    this.timeouts.forEach(timeout => clearTimeout(timeout));
    this.timeouts.clear();

    // Clear toasts
    this.toasts.clear();
    this.analytics = [];

    // Clear persistence
    if (this.config.enablePersistence && typeof window !== 'undefined') {
      localStorage.removeItem(this.config.persistenceKey);
    }

    this.notifySubscribers();
  }

  /**
   * Get analytics data
   */
  getAnalytics(): Array<{ event: string; toastId: string; timestamp: Date }> {
    return [...this.analytics];
  }

  /**
   * Update service configuration
   */
  updateConfig(newConfig: Partial<ToastServiceConfig>): void {
    Object.assign(this.config, newConfig);
    
    // Reinitialize sounds if sound settings changed
    if (newConfig.enableSounds !== undefined && this.config.enableSounds) {
      this.initializeSounds();
    }
  }

  // ========================================================================
  // CONVENIENCE METHODS
  // ========================================================================

  success(message: string, options?: Omit<ToastOptions, 'message' | 'type'>): string {
    return this.show({ ...options, message, type: 'success' });
  }

  error(message: string, options?: Omit<ToastOptions, 'message' | 'type'>): string {
    return this.show({ 
      ...options, 
      message, 
      type: 'error',
      duration: options?.duration ?? 0, // Errors are persistent by default
      priority: options?.priority ?? 'high',
    });
  }

  warning(message: string, options?: Omit<ToastOptions, 'message' | 'type'>): string {
    return this.show({ 
      ...options, 
      message, 
      type: 'warning',
      priority: options?.priority ?? 'normal',
    });
  }

  info(message: string, options?: Omit<ToastOptions, 'message' | 'type'>): string {
    return this.show({ ...options, message, type: 'info' });
  }

  /**
   * Show loading toast with progress updates
   */
  loading(message: string, options?: Omit<ToastOptions, 'message' | 'type' | 'duration' | 'closable'>): string {
    return this.show({
      ...options,
      message,
      type: 'info',
      duration: 0, // Loading toasts don't auto-hide
      closable: false,
      actions: [
        {
          id: 'cancel',
          label: 'Cancel',
          variant: 'secondary',
        }
      ],
    });
  }

  /**
   * Update loading toast progress
   */
  updateProgress(toastId: string, progress: number, message?: string): boolean {
    const updates: Partial<ToastOptions> = {
      metadata: { progress: Math.max(0, Math.min(100, progress)) },
    };
    
    if (message) {
      updates.message = message;
    }

    return this.update(toastId, updates);
  }

  /**
   * Complete loading toast
   */
  completeLoading(toastId: string, successMessage: string, autoHide = true): string {
    this.hide(toastId, 'programmatic');
    
    return this.success(successMessage, {
      duration: autoHide ? this.config.defaultDuration : 0,
    });
  }

  /**
   * Fail loading toast
   */
  failLoading(toastId: string, errorMessage: string): string {
    this.hide(toastId, 'programmatic');
    
    return this.error(errorMessage);
  }
}

// ============================================================================
// SINGLETON INSTANCE
// ============================================================================

export const toastService = new ToastService({
  maxToasts: 5,
  defaultDuration: 5000,
  position: 'top-right',
  enablePersistence: true,
  enableGrouping: true,
  enableSounds: false,
  respectReducedMotion: true,
  enableAnalytics: true,
});

// Export the service class for custom instances
export { ToastService };

// Note: Types are already exported above, don't re-export them to avoid conflicts
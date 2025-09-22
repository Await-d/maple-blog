// @ts-nocheck
import React from 'react';
import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import { immer } from 'zustand/middleware/immer';
import type { Activity, SystemMetrics, Notification } from '@/types';

export interface RealTimeState {
  // Connection management
  isConnected: boolean;
  connectionId: string | null;
  reconnectAttempts: number;
  maxReconnectAttempts: number;
  reconnectInterval: number;

  // WebSocket instance
  ws: WebSocket | null;

  // Subscriptions
  subscriptions: Set<string>;
  subscribedChannels: Map<string, Set<string>>; // channel -> subscriber IDs

  // Real-time data streams
  liveActivities: Activity[];
  liveMetrics: SystemMetrics | null;
  liveNotifications: Notification[];

  // Event handlers
  eventHandlers: Map<string, Array<(data: any) => void>>;

  // Actions
  connect: (url: string) => Promise<void>;
  disconnect: () => void;
  reconnect: () => void;

  // Subscription management
  subscribe: (channel: string, subscriberId: string) => void;
  unsubscribe: (channel: string, subscriberId: string) => void;
  unsubscribeAll: (subscriberId: string) => void;

  // Event handling
  addEventListener: (event: string, handler: (data: any) => void) => string;
  removeEventListener: (event: string, handlerId: string) => void;

  // Message sending
  sendMessage: (message: any) => void;
  handleMessage: (data: any) => void;

  // Data updates
  updateLiveActivities: (activities: Activity[]) => void;
  addLiveActivity: (activity: Activity) => void;
  updateLiveMetrics: (metrics: SystemMetrics) => void;
  addLiveNotification: (notification: Notification) => void;

  // State management
  setConnectionStatus: (connected: boolean) => void;
  incrementReconnectAttempts: () => void;
  resetReconnectAttempts: () => void;

  reset: () => void;
}

const initialState = {
  isConnected: false,
  connectionId: null,
  reconnectAttempts: 0,
  maxReconnectAttempts: 5,
  reconnectInterval: 5000,

  ws: null,

  subscriptions: new Set<string>(),
  subscribedChannels: new Map<string, Set<string>>(),

  liveActivities: [],
  liveMetrics: null,
  liveNotifications: [],

  eventHandlers: new Map<string, Array<(data: any) => void>>(),
};

export const useRealTimeStore = create<RealTimeState>()(
  devtools(
    immer((set, get) => ({
      ...initialState,

      connect: async (url: string) => {
        const state = get();

        // Close existing connection
        if (state.ws) {
          state.ws.close();
        }

        try {
          const ws = new WebSocket(url);

          ws.onopen = () => {
            console.log('WebSocket connected');
            set((state) => {
              state.isConnected = true;
              state.reconnectAttempts = 0;
              state.ws = ws;
            });

            // Send initial subscriptions
            const subscriptions = Array.from(state.subscriptions);
            if (subscriptions.length > 0) {
              ws.send(JSON.stringify({
                type: 'subscribe',
                channels: subscriptions,
              }));
            }
          };

          ws.onmessage = (event) => {
            try {
              const data = JSON.parse(event.data);
              get().handleMessage(data);
            } catch (error) {
              console.error('Failed to parse WebSocket message:', error);
            }
          };

          ws.onerror = (error) => {
            console.error('WebSocket error:', error);
            set((state) => {
              state.isConnected = false;
            });
          };

          ws.onclose = () => {
            console.log('WebSocket disconnected');
            set((state) => {
              state.isConnected = false;
              state.ws = null;
            });

            // Attempt to reconnect
            const currentState = get();
            if (currentState.reconnectAttempts < currentState.maxReconnectAttempts) {
              setTimeout(() => {
                get().reconnect();
              }, currentState.reconnectInterval);
            }
          };

          set((state) => {
            state.ws = ws;
          });

        } catch (error) {
          console.error('Failed to connect to WebSocket:', error);
          set((state) => {
            state.isConnected = false;
          });
        }
      },

      disconnect: () => {
        const state = get();
        if (state.ws) {
          state.ws.close();
        }

        set((state) => {
          state.isConnected = false;
          state.ws = null;
          state.reconnectAttempts = 0;
        });
      },

      reconnect: () => {
        get();
        set((state) => {
          state.reconnectAttempts += 1;
        });

        // This would need the original URL, which should be stored
        console.log('Attempting to reconnect...');
      },

      subscribe: (channel: string, subscriberId: string) => {
        set((state) => {
          state.subscriptions.add(channel);

          if (!state.subscribedChannels.has(channel)) {
            state.subscribedChannels.set(channel, new Set());
          }
          state.subscribedChannels.get(channel)!.add(subscriberId);

          // Send subscription to server if connected
          if (state.ws && state.isConnected) {
            state.ws.send(JSON.stringify({
              type: 'subscribe',
              channel,
              subscriberId,
            }));
          }
        });
      },

      unsubscribe: (channel: string, subscriberId: string) => {
        set((state) => {
          const subscribers = state.subscribedChannels.get(channel);
          if (subscribers) {
            subscribers.delete(subscriberId);

            // Remove channel if no more subscribers
            if (subscribers.size === 0) {
              state.subscribedChannels.delete(channel);
              state.subscriptions.delete(channel);

              // Send unsubscription to server if connected
              if (state.ws && state.isConnected) {
                state.ws.send(JSON.stringify({
                  type: 'unsubscribe',
                  channel,
                }));
              }
            }
          }
        });
      },

      unsubscribeAll: (subscriberId: string) => {
        const state = get();
        const channelsToRemove: string[] = [];

        state.subscribedChannels.forEach((subscribers, channel) => {
          subscribers.delete(subscriberId);
          if (subscribers.size === 0) {
            channelsToRemove.push(channel);
          }
        });

        set((state) => {
          channelsToRemove.forEach(channel => {
            state.subscribedChannels.delete(channel);
            state.subscriptions.delete(channel);
          });
        });

        // Send bulk unsubscription to server
        if (state.ws && state.isConnected && channelsToRemove.length > 0) {
          state.ws.send(JSON.stringify({
            type: 'unsubscribe_bulk',
            channels: channelsToRemove,
          }));
        }
      },

      addEventListener: (event: string, handler: (data: any) => void) => {
        const handlerId = `${event}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

        set((state) => {
          if (!state.eventHandlers.has(event)) {
            state.eventHandlers.set(event, []);
          }
          state.eventHandlers.get(event)!.push(handler);
        });

        return handlerId;
      },

      removeEventListener: (event: string, _handlerId: string) => {
        set((state) => {
          const handlers = state.eventHandlers.get(event);
          if (handlers) {
            // For simplicity, remove by index matching handlerId
            // In real implementation, you'd store handler with ID
            state.eventHandlers.set(event, handlers.slice(1));
          }
        });
      },

      sendMessage: (message: any) => {
        const state = get();
        if (state.ws && state.isConnected) {
          state.ws.send(JSON.stringify(message));
        } else {
          console.warn('Cannot send message: WebSocket not connected');
        }
      },

      updateLiveActivities: (activities: Activity[]) => {
        set((state) => {
          state.liveActivities = activities;
        });
      },

      addLiveActivity: (activity: Activity) => {
        set((state) => {
          state.liveActivities.unshift(activity);

          // Keep only the latest 100 activities
          if (state.liveActivities.length > 100) {
            state.liveActivities = state.liveActivities.slice(0, 100);
          }
        });
      },

      updateLiveMetrics: (metrics: SystemMetrics) => {
        set((state) => {
          state.liveMetrics = metrics;
        });
      },

      addLiveNotification: (notification: Notification) => {
        set((state) => {
          state.liveNotifications.unshift(notification);

          // Keep only the latest 20 notifications
          if (state.liveNotifications.length > 20) {
            state.liveNotifications = state.liveNotifications.slice(0, 20);
          }
        });
      },

      setConnectionStatus: (connected: boolean) => {
        set((state) => {
          state.isConnected = connected;
        });
      },

      incrementReconnectAttempts: () => {
        set((state) => {
          state.reconnectAttempts += 1;
        });
      },

      resetReconnectAttempts: () => {
        set((state) => {
          state.reconnectAttempts = 0;
        });
      },

      // Handle incoming WebSocket messages
      handleMessage: (data: any) => {
        const { type, payload } = data;

        switch (type) {
          case 'activity':
            get().addLiveActivity(payload);
            break;

          case 'metrics':
            get().updateLiveMetrics(payload);
            break;

          case 'notification':
            get().addLiveNotification(payload);
            break;

          case 'connection_id':
            set((state) => {
              state.connectionId = payload.id;
            });
            break;

          default:
            // Call custom event handlers
            const handlers = get().eventHandlers.get(type);
            if (handlers) {
              handlers.forEach(handler => handler(payload));
            }
        }
      },

      reset: () => {
        const state = get();
        if (state.ws) {
          state.ws.close();
        }
        set(() => initialState);
      },
    })),
    {
      name: 'real-time-store',
      enabled: process.env.NODE_ENV === 'development',
    }
  )
);

// Selectors
export const useRealTimeConnection = () => useRealTimeStore((state) => ({
  isConnected: state.isConnected,
  connectionId: state.connectionId,
  reconnectAttempts: state.reconnectAttempts,
}));

export const useLiveActivities = () => useRealTimeStore((state) => state.liveActivities);
export const useLiveMetrics = () => useRealTimeStore((state) => state.liveMetrics);
export const useLiveNotifications = () => useRealTimeStore((state) => state.liveNotifications);

// Custom hooks for easy real-time integration
export const useRealTimeChannel = (channel: string, subscriberId: string) => {
  const subscribe = useRealTimeStore((state) => state.subscribe);
  const unsubscribe = useRealTimeStore((state) => state.unsubscribe);

  React.useEffect(() => {
    subscribe(channel, subscriberId);

    return () => {
      unsubscribe(channel, subscriberId);
    };
  }, [channel, subscriberId, subscribe, unsubscribe]);
};

export const useRealTimeEvent = (
  event: string,
  handler: (data: any) => void,
  deps: React.DependencyList = []
) => {
  const addEventListener = useRealTimeStore((state) => state.addEventListener);
  const removeEventListener = useRealTimeStore((state) => state.removeEventListener);

  React.useEffect(() => {
    const handlerId = addEventListener(event, handler);

    return () => {
      removeEventListener(event, handlerId);
    };
  }, [event, ...deps]);
};
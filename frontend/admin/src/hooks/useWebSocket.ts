import { useRef, useEffect, useState, useCallback } from 'react';

export interface WebSocketMessage {
  data: string;
  timestamp: number;
  type?: string;
}

export interface UseWebSocketOptions {
  shouldReconnect?: () => boolean;
  reconnectInterval?: number;
  reconnectAttempts?: number;
  enabled?: boolean;
  protocols?: string | string[];
  onOpen?: (event: Event) => void;
  onClose?: (event: CloseEvent) => void;
  onMessage?: (message: WebSocketMessage) => void;
  onError?: (error: Event) => void;
}

export interface UseWebSocketReturn {
  isConnected: boolean;
  lastMessage: WebSocketMessage | null;
  sendMessage: (message: string) => void;
  disconnect: () => void;
  connect: () => void;
  error: string | null;
  reconnectCount: number;
}

export const useWebSocket = (
  url: string,
  options: UseWebSocketOptions = {}
): UseWebSocketReturn => {
  const {
    shouldReconnect = () => true,
    reconnectInterval = 3000,
    reconnectAttempts = 5,
    enabled = true,
    protocols,
    onOpen,
    onClose,
    onMessage,
    onError
  } = options;

  const ws = useRef<WebSocket | null>(null);
  const reconnectTimer = useRef<NodeJS.Timeout | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [lastMessage, setLastMessage] = useState<WebSocketMessage | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [reconnectCount, setReconnectCount] = useState(0);

  const connect = useCallback(() => {
    if (!url || !enabled) return;

    try {
      if (ws.current?.readyState === WebSocket.OPEN) {
        ws.current.close();
      }

      ws.current = new WebSocket(url, protocols);

      ws.current.onopen = (event) => {
        setIsConnected(true);
        setError(null);
        setReconnectCount(0);
        onOpen?.(event);
      };

      ws.current.onclose = (event) => {
        setIsConnected(false);
        onClose?.(event);

        // Attempt to reconnect if conditions are met
        if (
          shouldReconnect() &&
          reconnectCount < reconnectAttempts &&
          enabled
        ) {
          reconnectTimer.current = setTimeout(() => {
            setReconnectCount(prev => prev + 1);
            connect();
          }, reconnectInterval);
        }
      };

      ws.current.onmessage = (event) => {
        const message: WebSocketMessage = {
          data: event.data,
          timestamp: Date.now()
        };
        setLastMessage(message);
        onMessage?.(message);
      };

      ws.current.onerror = (event) => {
        setError('WebSocket连接错误');
        onError?.(event);
      };
    } catch (err) {
      setError(err instanceof Error ? err.message : 'WebSocket连接失败');
    }
  }, [url, enabled, protocols, shouldReconnect, reconnectInterval, reconnectAttempts, reconnectCount, onOpen, onClose, onMessage, onError]);

  const disconnect = useCallback(() => {
    if (reconnectTimer.current) {
      clearTimeout(reconnectTimer.current);
    }

    if (ws.current) {
      ws.current.close(1000, 'Manual disconnect');
    }

    setIsConnected(false);
    setReconnectCount(0);
  }, []);

  const sendMessage = useCallback((message: string) => {
    if (ws.current?.readyState === WebSocket.OPEN) {
      ws.current.send(message);
    } else {
      console.warn('WebSocket is not connected');
    }
  }, []);

  useEffect(() => {
    if (enabled) {
      connect();
    } else {
      disconnect();
    }

    return () => {
      disconnect();
    };
  }, [enabled, connect, disconnect]);

  return {
    isConnected,
    lastMessage,
    sendMessage,
    disconnect,
    connect,
    error,
    reconnectCount
  };
};
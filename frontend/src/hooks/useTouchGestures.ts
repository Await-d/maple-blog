/**
 * 触摸手势Hook - 完整架构重构版本
 * 解决React事件和DOM事件类型不兼容的根本性架构问题
 * 提供双重接口：React组件接口和DOM元素接口
 */

import { useCallback, useRef } from 'react';

export interface TouchGestureConfig {
  // 滑动配置
  swipeThreshold?: number; // 滑动最小距离
  swipeVelocity?: number;  // 滑动最小速度

  // 长按配置
  longPressDelay?: number; // 长按延迟时间
  longPressMoveThreshold?: number; // 长按允许的移动距离

  // 双击配置
  doubleTapDelay?: number; // 双击间隔时间
  doubleTapDistance?: number; // 双击允许的位置偏移
}

// React组件接口 - 使用React合成事件
export interface ReactTouchGestureCallbacks {
  onSwipeLeft?: () => void;
  onSwipeRight?: () => void;
  onSwipeUp?: () => void;
  onSwipeDown?: () => void;
  onLongPress?: (event: React.TouchEvent) => void;
  onDoubleTap?: (event: React.TouchEvent) => void;
  onTap?: (event: React.TouchEvent) => void;
}

// DOM元素接口 - 使用原生DOM事件
export interface DOMTouchGestureCallbacks {
  onSwipeLeft?: () => void;
  onSwipeRight?: () => void;
  onSwipeUp?: () => void;
  onSwipeDown?: () => void;
  onLongPress?: (event: TouchEvent) => void;
  onDoubleTap?: (event: TouchEvent) => void;
  onTap?: (event: TouchEvent) => void;
}

interface TouchPoint {
  x: number;
  y: number;
  time: number;
}

// 事件适配器：将DOM事件转换为React事件格式
class EventAdapter {
  static createSyntheticTouchEvent(nativeEvent: TouchEvent): React.TouchEvent {
    // 创建模拟的React.TouchEvent对象
    const syntheticEvent = {
      touches: nativeEvent.touches,
      changedTouches: nativeEvent.changedTouches,
      targetTouches: nativeEvent.targetTouches,
      altKey: nativeEvent.altKey,
      ctrlKey: nativeEvent.ctrlKey,
      shiftKey: nativeEvent.shiftKey,
      metaKey: nativeEvent.metaKey,
      preventDefault: nativeEvent.preventDefault.bind(nativeEvent),
      stopPropagation: nativeEvent.stopPropagation.bind(nativeEvent),
      target: nativeEvent.target,
      currentTarget: nativeEvent.currentTarget,
      type: nativeEvent.type,
      bubbles: nativeEvent.bubbles,
      cancelable: nativeEvent.cancelable,
      timeStamp: nativeEvent.timeStamp,
      // React特有的属性
      nativeEvent: nativeEvent,
      isDefaultPrevented: () => nativeEvent.defaultPrevented,
      isPropagationStopped: () => false,
      persist: () => {
        // No-op: React 17+ removed event pooling, so persist() is not needed
      },
      getModifierState: (key: string) => {
        switch (key) {
          case 'Alt': return nativeEvent.altKey;
          case 'Control': return nativeEvent.ctrlKey;
          case 'Shift': return nativeEvent.shiftKey;
          case 'Meta': return nativeEvent.metaKey;
          default: return false;
        }
      },
      // 添加缺失的React.SyntheticEvent属性
      detail: 0,
      view: window,
      defaultPrevented: nativeEvent.defaultPrevented,
      eventPhase: nativeEvent.eventPhase,
      isTrusted: nativeEvent.isTrusted
    } as unknown as React.TouchEvent;

    return syntheticEvent;
  }

  static createSyntheticMouseEvent(nativeEvent: Event): React.MouseEvent {
    const mouseEvent = nativeEvent as MouseEvent;
    const syntheticEvent = {
      button: mouseEvent.button || 0,
      buttons: mouseEvent.buttons || 0,
      clientX: mouseEvent.clientX || 0,
      clientY: mouseEvent.clientY || 0,
      pageX: mouseEvent.pageX || 0,
      pageY: mouseEvent.pageY || 0,
      screenX: mouseEvent.screenX || 0,
      screenY: mouseEvent.screenY || 0,
      altKey: mouseEvent.altKey || false,
      ctrlKey: mouseEvent.ctrlKey || false,
      shiftKey: mouseEvent.shiftKey || false,
      metaKey: mouseEvent.metaKey || false,
      preventDefault: nativeEvent.preventDefault.bind(nativeEvent),
      stopPropagation: nativeEvent.stopPropagation.bind(nativeEvent),
      target: nativeEvent.target,
      currentTarget: nativeEvent.currentTarget,
      type: nativeEvent.type,
      bubbles: nativeEvent.bubbles,
      cancelable: nativeEvent.cancelable,
      timeStamp: nativeEvent.timeStamp,
      // React特有的属性
      nativeEvent: nativeEvent,
      isDefaultPrevented: () => nativeEvent.defaultPrevented,
      isPropagationStopped: () => false,
      persist: () => {
        // No-op: React 17+ removed event pooling, so persist() is not needed
      },
      getModifierState: (key: string) => {
        switch (key) {
          case 'Alt': return mouseEvent.altKey;
          case 'Control': return mouseEvent.ctrlKey;
          case 'Shift': return mouseEvent.shiftKey;
          case 'Meta': return mouseEvent.metaKey;
          default: return false;
        }
      }
    } as React.MouseEvent;

    return syntheticEvent;
  }
}

export const useTouchGestures = (
  config: TouchGestureConfig = {},
  reactCallbacks: ReactTouchGestureCallbacks = {},
  domCallbacks: DOMTouchGestureCallbacks = {}
) => {
  const {
    swipeThreshold = 50,
    swipeVelocity = 0.5,
    longPressDelay = 500,
    longPressMoveThreshold = 10,
    doubleTapDelay = 300,
    doubleTapDistance = 30
  } = config;

  // React callbacks
  const {
    onSwipeLeft: reactOnSwipeLeft,
    onSwipeRight: reactOnSwipeRight,
    onSwipeUp: reactOnSwipeUp,
    onSwipeDown: reactOnSwipeDown,
    onLongPress: reactOnLongPress,
    onDoubleTap: reactOnDoubleTap,
    onTap: reactOnTap
  } = reactCallbacks;

  // DOM callbacks
  const {
    onSwipeLeft: domOnSwipeLeft,
    onSwipeRight: domOnSwipeRight,
    onSwipeUp: domOnSwipeUp,
    onSwipeDown: domOnSwipeDown,
    onLongPress: domOnLongPress,
    onDoubleTap: domOnDoubleTap,
    onTap: domOnTap
  } = domCallbacks;

  // 状态引用
  const touchStartRef = useRef<TouchPoint | null>(null);
  const touchEndRef = useRef<TouchPoint | null>(null);
  const longPressTimerRef = useRef<NodeJS.Timeout | null>(null);
  const lastTapRef = useRef<TouchPoint | null>(null);
  const preventClickRef = useRef<boolean>(false);

  // 工具函数
  const calculateDistance = useCallback((point1: TouchPoint, point2: TouchPoint): number => {
    const dx = point2.x - point1.x;
    const dy = point2.y - point1.y;
    return Math.sqrt(dx * dx + dy * dy);
  }, []);

  const calculateVelocity = useCallback((point1: TouchPoint, point2: TouchPoint): number => {
    const distance = calculateDistance(point1, point2);
    const time = point2.time - point1.time;
    return time > 0 ? distance / time : 0;
  }, [calculateDistance]);

  const clearLongPressTimer = useCallback(() => {
    if (longPressTimerRef.current) {
      clearTimeout(longPressTimerRef.current);
      longPressTimerRef.current = null;
    }
  }, []);

  // React事件处理器 - 用于React组件
  const handleReactTouchStart = useCallback((event: React.TouchEvent) => {
    const touch = event.touches[0];
    const touchPoint: TouchPoint = {
      x: touch.clientX,
      y: touch.clientY,
      time: Date.now()
    };

    touchStartRef.current = touchPoint;
    touchEndRef.current = null;
    preventClickRef.current = false;

    // 开始长按计时器
    if (reactOnLongPress) {
      clearLongPressTimer();
      longPressTimerRef.current = setTimeout(() => {
        if (touchStartRef.current && !preventClickRef.current) {
          reactOnLongPress(event);
          preventClickRef.current = true;
        }
      }, longPressDelay);
    }
  }, [reactOnLongPress, longPressDelay, clearLongPressTimer]);

  const handleReactTouchMove = useCallback((event: React.TouchEvent) => {
    if (!touchStartRef.current) return;

    const touch = event.touches[0];
    const currentPoint: TouchPoint = {
      x: touch.clientX,
      y: touch.clientY,
      time: Date.now()
    };

    // 如果移动距离超过阈值，取消长按
    const distance = calculateDistance(touchStartRef.current, currentPoint);
    if (distance > longPressMoveThreshold) {
      clearLongPressTimer();
    }
  }, [calculateDistance, longPressMoveThreshold, clearLongPressTimer]);

  const handleReactTouchEnd = useCallback((event: React.TouchEvent) => {
    clearLongPressTimer();

    if (!touchStartRef.current) return;

    const touch = event.changedTouches[0];
    const touchPoint: TouchPoint = {
      x: touch.clientX,
      y: touch.clientY,
      time: Date.now()
    };

    touchEndRef.current = touchPoint;

    // 检测滑动手势
    const deltaX = touchPoint.x - touchStartRef.current.x;
    const deltaY = touchPoint.y - touchStartRef.current.y;
    const distance = calculateDistance(touchStartRef.current, touchPoint);
    const velocity = calculateVelocity(touchStartRef.current, touchPoint);

    if (distance >= swipeThreshold && velocity >= swipeVelocity) {
      if (Math.abs(deltaX) > Math.abs(deltaY)) {
        // 水平滑动
        if (deltaX > 0) {
          reactOnSwipeRight?.();
        } else {
          reactOnSwipeLeft?.();
        }
      } else {
        // 垂直滑动
        if (deltaY > 0) {
          reactOnSwipeDown?.();
        } else {
          reactOnSwipeUp?.();
        }
      }
    } else {
      // 可能是点击或双击
      if (lastTapRef.current) {
        const timeSinceLastTap = touchPoint.time - lastTapRef.current.time;
        const distanceFromLastTap = calculateDistance(lastTapRef.current, touchPoint);

        // 检测双击
        if (
          timeSinceLastTap <= doubleTapDelay &&
          distanceFromLastTap <= doubleTapDistance &&
          reactOnDoubleTap
        ) {
          reactOnDoubleTap(event);
          lastTapRef.current = null;
          preventClickRef.current = true;
          return;
        }
      }

      // 记录这次点击
      lastTapRef.current = touchPoint;

      // 延迟执行单击
      if (reactOnTap) {
        setTimeout(() => {
          if (lastTapRef.current === touchPoint && !preventClickRef.current) {
            reactOnTap(event);
          }
        }, doubleTapDelay);
      }
    }

    touchStartRef.current = null;
  }, [
    clearLongPressTimer,
    calculateDistance,
    calculateVelocity,
    swipeThreshold,
    swipeVelocity,
    doubleTapDelay,
    doubleTapDistance,
    reactOnSwipeLeft,
    reactOnSwipeRight,
    reactOnSwipeUp,
    reactOnSwipeDown,
    reactOnDoubleTap,
    reactOnTap
  ]);

  const handleReactTouchCancel = useCallback(() => {
    clearLongPressTimer();
    touchStartRef.current = null;
    touchEndRef.current = null;
    preventClickRef.current = false;
  }, [clearLongPressTimer]);

  const handleReactClick = useCallback((event: React.MouseEvent) => {
    if (preventClickRef.current) {
      event.preventDefault();
      event.stopPropagation();
      preventClickRef.current = false;
    }
  }, []);

  // DOM事件处理器 - 用于直接DOM绑定
  const handleDOMTouchStart = useCallback((event: TouchEvent) => {
    const touch = event.touches[0];
    const touchPoint: TouchPoint = {
      x: touch.clientX,
      y: touch.clientY,
      time: Date.now()
    };

    touchStartRef.current = touchPoint;
    touchEndRef.current = null;
    preventClickRef.current = false;

    // 开始长按计时器
    if (domOnLongPress) {
      clearLongPressTimer();
      longPressTimerRef.current = setTimeout(() => {
        if (touchStartRef.current && !preventClickRef.current) {
          domOnLongPress(event);
          preventClickRef.current = true;
        }
      }, longPressDelay);
    }
  }, [domOnLongPress, longPressDelay, clearLongPressTimer]);

  const handleDOMTouchMove = useCallback((event: TouchEvent) => {
    if (!touchStartRef.current) return;

    const touch = event.touches[0];
    const currentPoint: TouchPoint = {
      x: touch.clientX,
      y: touch.clientY,
      time: Date.now()
    };

    // 如果移动距离超过阈值，取消长按
    const distance = calculateDistance(touchStartRef.current, currentPoint);
    if (distance > longPressMoveThreshold) {
      clearLongPressTimer();
    }
  }, [calculateDistance, longPressMoveThreshold, clearLongPressTimer]);

  const handleDOMTouchEnd = useCallback((event: TouchEvent) => {
    clearLongPressTimer();

    if (!touchStartRef.current) return;

    const touch = event.changedTouches[0];
    const touchPoint: TouchPoint = {
      x: touch.clientX,
      y: touch.clientY,
      time: Date.now()
    };

    touchEndRef.current = touchPoint;

    // 检测滑动手势
    const deltaX = touchPoint.x - touchStartRef.current.x;
    const deltaY = touchPoint.y - touchStartRef.current.y;
    const distance = calculateDistance(touchStartRef.current, touchPoint);
    const velocity = calculateVelocity(touchStartRef.current, touchPoint);

    if (distance >= swipeThreshold && velocity >= swipeVelocity) {
      if (Math.abs(deltaX) > Math.abs(deltaY)) {
        // 水平滑动
        if (deltaX > 0) {
          domOnSwipeRight?.();
        } else {
          domOnSwipeLeft?.();
        }
      } else {
        // 垂直滑动
        if (deltaY > 0) {
          domOnSwipeDown?.();
        } else {
          domOnSwipeUp?.();
        }
      }
    } else {
      // 可能是点击或双击
      if (lastTapRef.current) {
        const timeSinceLastTap = touchPoint.time - lastTapRef.current.time;
        const distanceFromLastTap = calculateDistance(lastTapRef.current, touchPoint);

        // 检测双击
        if (
          timeSinceLastTap <= doubleTapDelay &&
          distanceFromLastTap <= doubleTapDistance &&
          domOnDoubleTap
        ) {
          domOnDoubleTap(event);
          lastTapRef.current = null;
          preventClickRef.current = true;
          return;
        }
      }

      // 记录这次点击
      lastTapRef.current = touchPoint;

      // 延迟执行单击
      if (domOnTap) {
        setTimeout(() => {
          if (lastTapRef.current === touchPoint && !preventClickRef.current) {
            domOnTap(event);
          }
        }, doubleTapDelay);
      }
    }

    touchStartRef.current = null;
  }, [
    clearLongPressTimer,
    calculateDistance,
    calculateVelocity,
    swipeThreshold,
    swipeVelocity,
    doubleTapDelay,
    doubleTapDistance,
    domOnSwipeLeft,
    domOnSwipeRight,
    domOnSwipeUp,
    domOnSwipeDown,
    domOnDoubleTap,
    domOnTap
  ]);

  const handleDOMTouchCancel = useCallback(() => {
    clearLongPressTimer();
    touchStartRef.current = null;
    touchEndRef.current = null;
    preventClickRef.current = false;
  }, [clearLongPressTimer]);

  const handleDOMClick = useCallback((event: Event) => {
    if (preventClickRef.current) {
      event.preventDefault();
      event.stopPropagation();
      preventClickRef.current = false;
    }
  }, []);

  // React组件事件处理器对象
  const reactTouchHandlers = {
    onTouchStart: handleReactTouchStart,
    onTouchMove: handleReactTouchMove,
    onTouchEnd: handleReactTouchEnd,
    onTouchCancel: handleReactTouchCancel,
    onClick: handleReactClick,
  };

  // DOM元素绑定函数
  const bindToElement = useCallback((element: HTMLElement) => {
    if (!element) return;

    element.addEventListener('touchstart', handleDOMTouchStart, { passive: false });
    element.addEventListener('touchmove', handleDOMTouchMove, { passive: false });
    element.addEventListener('touchend', handleDOMTouchEnd, { passive: false });
    element.addEventListener('touchcancel', handleDOMTouchCancel, { passive: false });
    element.addEventListener('click', handleDOMClick, { passive: false });

    return () => {
      element.removeEventListener('touchstart', handleDOMTouchStart);
      element.removeEventListener('touchmove', handleDOMTouchMove);
      element.removeEventListener('touchend', handleDOMTouchEnd);
      element.removeEventListener('touchcancel', handleDOMTouchCancel);
      element.removeEventListener('click', handleDOMClick);
    };
  }, [handleDOMTouchStart, handleDOMTouchMove, handleDOMTouchEnd, handleDOMTouchCancel, handleDOMClick]);

  return {
    // React组件接口
    touchHandlers: reactTouchHandlers,

    // DOM元素接口
    bindToElement,

    // 状态查询
    isGestureActive: touchStartRef.current !== null,
    preventClick: preventClickRef.current,

    // 事件适配器 - 供高级用法使用
    EventAdapter
  };
};

export default useTouchGestures;
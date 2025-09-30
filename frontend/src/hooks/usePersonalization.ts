/**
 * usePersonalization hook - Handle personalized content recommendations
 * Features: User behavior tracking, preference learning, recommendation algorithms
 */

import { useState, useEffect, useCallback, useMemo } from 'react';
import { useQuery as _useQuery, useMutation as _useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from './useAuth';
import { usePersonalizationActions, usePersonalization as usePersonalizationStore } from '../stores/homeStore';
import {
  usePersonalizedRecommendations,
  useRecordInteraction,
  HOME_QUERY_KEYS,
} from '../services/home/homeApi';
import { toastService } from '../services/toastService';
import { errorReporter } from '../services/errorReporting';
import type {
  PostSummary,
  PersonalizationSettings,
  UserInteraction,
  RecommendationFeedback,
} from '../types/home';

interface PersonalizationState {
  isLoading: boolean;
  error: string | null;
  recommendations: PostSummary[];
  hasRecommendations: boolean;
  canPersonalize: boolean;
  interactionScore: number;
  diversity: number;
  freshness: number;
}

interface PersonalizationActions {
  recordInteraction: (postId: string, type: UserInteraction['interactionType'], duration?: number) => Promise<void>;
  provideFeedback: (postId: string, feedback: RecommendationFeedback['feedback'], reason?: string) => Promise<void>;
  refreshRecommendations: () => Promise<void>;
  updatePreferences: (preferences: Partial<PersonalizationSettings>) => void;
  resetPersonalization: () => void;
  addInterest: (type: 'category' | 'tag' | 'author', id: string) => void;
  removeInterest: (type: 'category' | 'tag' | 'author', id: string) => void;
  setReadingGoals: (goals: { postsPerWeek?: number; categoriesOfInterest?: number }) => void;
}

interface UsePersonalizationReturn {
  state: PersonalizationState;
  actions: PersonalizationActions;
  settings: PersonalizationSettings | undefined;
  preferences: PersonalizationSettings | undefined;
  updatePreferences: (preferences: Partial<PersonalizationSettings>) => void;
  analytics: {
    totalInteractions: number;
    readingStreak: number;
    favoriteCategories: string[];
    readingTime: number;
    engagementRate: number;
  };
}

/**
 * Custom hook for managing user personalization and recommendations
 */
export const usePersonalization = (): UsePersonalizationReturn => {
  const { user, isAuthenticated } = useAuth();
  const queryClient = useQueryClient();
  const personalizationActions = usePersonalizationActions();
  const personalizationSettings = usePersonalizationStore();

  // Local state
  const [interactionHistory, setInteractionHistory] = useState<UserInteraction[]>([]);
  const [feedbackHistory, setFeedbackHistory] = useState<RecommendationFeedback[]>([]);
  const [error, setError] = useState<string | null>(null);

  // API hooks
  const {
    data: recommendations,
    isLoading: recommendationsLoading,
    error: recommendationsError,
    refetch: refetchRecommendations,
  } = usePersonalizedRecommendations(user?.id || null, 10);

  const recordInteractionMutation = useRecordInteraction(undefined, user?.id);

  // Load personalization data from localStorage on mount
  useEffect(() => {
    if (!isAuthenticated || !user) return;

    const storageKey = `personalization_${user.id}`;
    const stored = localStorage.getItem(storageKey);
    if (stored) {
      try {
        const data = JSON.parse(stored);
        setInteractionHistory(data.interactions || []);
        setFeedbackHistory(data.feedback || []);
      } catch (error) {
        console.warn('Failed to load personalization data:', error);
      }
    }
  }, [isAuthenticated, user]);

  // Save personalization data to localStorage
  const saveToStorage = useCallback(() => {
    if (!isAuthenticated || !user) return;

    const storageKey = `personalization_${user.id}`;
    const data = {
      interactions: interactionHistory.slice(-1000), // Keep last 1000 interactions
      feedback: feedbackHistory.slice(-100), // Keep last 100 feedback items
      lastSaved: new Date().toISOString(),
    };

    try {
      localStorage.setItem(storageKey, JSON.stringify(data));
    } catch (error) {
      console.warn('Failed to save personalization data:', error);
    }
  }, [isAuthenticated, user, interactionHistory, feedbackHistory]);

  // Auto-save periodically
  useEffect(() => {
    const interval = setInterval(saveToStorage, 30000); // Save every 30 seconds
    return () => clearInterval(interval);
  }, [saveToStorage]);

  useEffect(() => {
    saveToStorage();
  }, [interactionHistory, feedbackHistory, saveToStorage]);

  // Calculate analytics from interaction history
  const analytics = useMemo(() => {
    const now = new Date();
    const weekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
    const _recentInteractions = interactionHistory.filter(
      interaction => new Date(interaction.timestamp) > weekAgo
    );

    // Calculate reading streak (consecutive days with interactions)
    const _today = new Date().toDateString();
    let streak = 0;
    const currentDate = new Date();

    for (let i = 0; i < 30; i++) {
      const dateString = currentDate.toDateString();
      const hasInteraction = interactionHistory.some(
        interaction => new Date(interaction.timestamp).toDateString() === dateString
      );

      if (hasInteraction) {
        streak++;
      } else if (i > 0) {
        break; // Break streak if no interaction found
      }

      currentDate.setDate(currentDate.getDate() - 1);
    }

    // Calculate favorite categories from interactions
    const categoryInteractions: Record<string, number> = {};
    interactionHistory.forEach(_interaction => {
      // This would need category mapping from post data
      // For now, we'll use a mock implementation
    });

    // Calculate total reading time
    const totalReadingTime = interactionHistory
      .filter(interaction => interaction.interactionType === 'view' && interaction.duration)
      .reduce((total, interaction) => total + (interaction.duration || 0), 0);

    // Calculate engagement rate
    const viewCount = interactionHistory.filter(i => i.interactionType === 'view').length;
    const engagementCount = interactionHistory.filter(
      i => ['like', 'comment', 'share', 'bookmark'].includes(i.interactionType)
    ).length;
    const engagementRate = viewCount > 0 ? (engagementCount / viewCount) * 100 : 0;

    return {
      totalInteractions: interactionHistory.length,
      readingStreak: streak,
      favoriteCategories: Object.keys(categoryInteractions).slice(0, 5),
      readingTime: Math.round(totalReadingTime / 60), // Convert to minutes
      engagementRate: Math.round(engagementRate * 100) / 100,
    };
  }, [interactionHistory]);

  // Calculate personalization quality metrics
  const personalizationQuality = useMemo(() => {
    const hasEnoughData = interactionHistory.length >= 10;
    const hasRecentActivity = interactionHistory.some(
      interaction => new Date(interaction.timestamp) > new Date(Date.now() - 7 * 24 * 60 * 60 * 1000)
    );

    return {
      canPersonalize: isAuthenticated && hasEnoughData && hasRecentActivity,
      interactionScore: Math.min(interactionHistory.length / 50, 1), // 0-1 scale
      diversity: feedbackHistory.filter(f => f.feedback === 'relevant').length / Math.max(feedbackHistory.length, 1),
      freshness: hasRecentActivity ? 1 : 0,
    };
  }, [isAuthenticated, interactionHistory, feedbackHistory]);

  // Actions
  const recordInteraction = useCallback(async (
    postId: string,
    type: UserInteraction['interactionType'],
    duration?: number
  ) => {
    if (!isAuthenticated || !postId) return;

    const interaction: UserInteraction = {
      postId,
      interactionType: type,
      duration,
      timestamp: new Date().toISOString(),
    };

    try {
      await recordInteractionMutation.mutateAsync(interaction);
      setInteractionHistory(prev => [...prev, interaction]);
      setError(null);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to record interaction';
      setError(message);
      if (type !== 'view') {
        toastService.error(message);
      }
      errorReporter.captureError(err instanceof Error ? err : new Error(String(err)), {
        component: 'usePersonalization',
        action: 'recordInteraction',
        handled: true,
        extra: {
          postId,
          interactionType: type,
        },
      });
    }
  }, [isAuthenticated, recordInteractionMutation]);

  const provideFeedback = useCallback(async (
    postId: string,
    feedback: RecommendationFeedback['feedback'],
    reason?: string
  ) => {
    if (!isAuthenticated) return;

    const feedbackData: RecommendationFeedback = {
      postId,
      feedback,
      reason,
    };

    // Store feedback locally
    setFeedbackHistory(prev => [...prev, feedbackData]);

    // In a real app, this would be sent to the API
    // await provideFeedbackMutation.mutateAsync(feedbackData);

    // Refresh recommendations after feedback
    setTimeout(() => {
      refetchRecommendations();
    }, 1000);
  }, [isAuthenticated, refetchRecommendations]);

  const refreshRecommendations = useCallback(async () => {
    await refetchRecommendations();
  }, [refetchRecommendations]);

  const updatePreferences = useCallback((preferences: Partial<PersonalizationSettings>) => {
    personalizationActions.updatePersonalization(preferences);
  }, [personalizationActions]);

  const resetPersonalization = useCallback(() => {
    setInteractionHistory([]);
    setFeedbackHistory([]);
    setError(null);
    // Note: resetPersonalization method not available in store

    // Clear localStorage
    if (user) {
      const storageKey = `personalization_${user.id}`;
      localStorage.removeItem(storageKey);
      queryClient.removeQueries({
        queryKey: HOME_QUERY_KEYS.recommendations(user.id, 10),
      });
      queryClient.removeQueries({
        queryKey: HOME_QUERY_KEYS.homePagePersonalized(user.id),
      });
    }
  }, [user, queryClient]);

  const addInterest = useCallback((
    type: 'category' | 'tag' | 'author',
    id: string
  ) => {
    switch (type) {
      case 'category':
        personalizationActions.addPreferredCategory(id);
        break;
      case 'tag':
        personalizationActions.addPreferredTag(id);
        break;
      case 'author':
        personalizationActions.followAuthor(id);
        break;
    }
  }, [personalizationActions]);

  const removeInterest = useCallback((
    type: 'category' | 'tag' | 'author',
    id: string
  ) => {
    switch (type) {
      case 'category':
        personalizationActions.removePreferredCategory(id);
        break;
      case 'tag':
        personalizationActions.removePreferredTag(id);
        break;
      case 'author':
        personalizationActions.unfollowAuthor(id);
        break;
    }
  }, [personalizationActions]);

  const setReadingGoals = useCallback((_goals: {
    postsPerWeek?: number;
    categoriesOfInterest?: number;
  }) => {
    // Update user preferences with reading goals
    personalizationActions.updatePersonalization({
      // This would extend the PersonalizationSettings type to include goals
      ...personalizationSettings,
      updatedAt: new Date().toISOString(),
    } as PersonalizationSettings);
  }, [personalizationActions, personalizationSettings]);

  const state: PersonalizationState = {
    isLoading: recommendationsLoading,
    error: error || recommendationsError?.message || null,
    recommendations: recommendations || [],
    hasRecommendations: Boolean(recommendations?.length),
    ...personalizationQuality,
  };

  const actions: PersonalizationActions = {
    recordInteraction,
    provideFeedback,
    refreshRecommendations,
    updatePreferences,
    resetPersonalization,
    addInterest,
    removeInterest,
    setReadingGoals,
  };

  return {
    state,
    actions,
    settings: personalizationSettings,
    preferences: personalizationSettings,
    updatePreferences: actions.updatePreferences,
    analytics,
  };
};

/**
 * Hook for automatic interaction tracking
 * Automatically tracks view interactions when posts are visible
 */
export const useAutoTrackViews = (posts: PostSummary[]) => {
  const { isAuthenticated, user } = useAuth();
  const { mutate: recordView } = useRecordInteraction(
    {
      onError: (error, variables) => {
        errorReporter.captureError(error instanceof Error ? error : new Error(String(error)), {
          component: 'useAutoTrackViews',
          action: 'recordView',
          handled: true,
          extra: {
            postId: variables.postId,
            duration: variables.duration,
          },
        });
      },
    },
    user?.id
  );

  useEffect(() => {
    if (!isAuthenticated || posts.length === 0) {
      return;
    }

    const trackedPosts = new Set<string>();
    const exitObservers = new Map<Element, IntersectionObserver>();

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) {
            return;
          }

          const postId = entry.target.getAttribute('data-post-id');
          if (!postId || trackedPosts.has(postId)) {
            return;
          }

          trackedPosts.add(postId);
          const startTime = Date.now();

          const exitObserver = new IntersectionObserver(
            ([exitEntry]) => {
              if (exitEntry.isIntersecting) {
                return;
              }

              const duration = Math.round((Date.now() - startTime) / 1000);
              if (duration > 3) {
                recordView({
                  postId,
                  interactionType: 'view',
                  duration,
                  timestamp: new Date().toISOString(),
                });
              }

              const storedObserver = exitObservers.get(exitEntry.target);
              storedObserver?.disconnect();
              exitObservers.delete(exitEntry.target);
            },
            { threshold: 0 }
          );

          exitObservers.set(entry.target, exitObserver);
          exitObserver.observe(entry.target);
        });
      },
      {
        threshold: 0.5,
        rootMargin: '0px 0px -50px 0px',
      }
    );

    posts.forEach((post) => {
      const element = document.querySelector(`[data-post-id="${post.id}"]`);
      if (element) {
        observer.observe(element);
      }
    });

    return () => {
      observer.disconnect();
      exitObservers.forEach((exitObserver) => exitObserver.disconnect());
      exitObservers.clear();
    };
  }, [posts, isAuthenticated, recordView]);
};

export default usePersonalization;

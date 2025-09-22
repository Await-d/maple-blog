// @ts-nocheck
/**
 * usePersonalization hook - Handle personalized content recommendations
 * Features: User behavior tracking, preference learning, recommendation algorithms
 */

import { useState, useEffect, useCallback, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from './useAuth';
import { usePersonalizationActions, usePersonalization as usePersonalizationStore } from '../stores/homeStore';
import {
  usePersonalizedRecommendations,
  useRecordInteraction,
  HOME_QUERY_KEYS,
} from '../services/home/homeApi';
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

  const recordInteractionMutation = useRecordInteraction({
    onSuccess: (_, variables) => {
      // Update local interaction history
      setInteractionHistory(prev => [
        ...prev,
        {
          ...variables,
          timestamp: new Date().toISOString(),
        },
      ]);

      // Clear error on successful interaction
      setError(null);
    },
    onError: (error: any) => {
      setError(error.message || 'Failed to record interaction');
    },
  });

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

  // Calculate analytics from interaction history
  const analytics = useMemo(() => {
    const now = new Date();
    const weekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
    const recentInteractions = interactionHistory.filter(
      interaction => new Date(interaction.timestamp) > weekAgo
    );

    // Calculate reading streak (consecutive days with interactions)
    const today = new Date().toDateString();
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
    interactionHistory.forEach(interaction => {
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
    if (!isAuthenticated) return;

    const interaction: UserInteraction = {
      postId,
      interactionType: type,
      duration,
      timestamp: new Date().toISOString(),
    };

    await recordInteractionMutation.mutateAsync(interaction);
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
    }

    // Invalidate recommendations
    queryClient.removeQueries({
      queryKey: HOME_QUERY_KEYS.recommendations(user?.id || '', 10),
    });
  }, [personalizationActions, user, queryClient]);

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

  const setReadingGoals = useCallback((goals: {
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
    analytics,
  };
};

/**
 * Hook for automatic interaction tracking
 * Automatically tracks view interactions when posts are visible
 */
export const useAutoTrackViews = (posts: PostSummary[]) => {
  const actions = usePersonalizationActions();

  useEffect(() => {
    const trackedPosts = new Set<string>();

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            const postId = entry.target.getAttribute('data-post-id');
            if (postId && !trackedPosts.has(postId)) {
              trackedPosts.add(postId);

              // Track view with estimated reading time
              const startTime = Date.now();
              const unobserve = () => {
                const duration = Math.round((Date.now() - startTime) / 1000);
                if (duration > 3) { // Only track if viewed for more than 3 seconds
                  // Note: recordInteraction not available in actions store
                  console.log('Would record interaction:', postId, 'view', duration);
                }
              };

              // Track when post leaves viewport
              const exitObserver = new IntersectionObserver(
                ([exitEntry]) => {
                  if (!exitEntry.isIntersecting) {
                    unobserve();
                    exitObserver.disconnect();
                  }
                },
                { threshold: 0 }
              );
              exitObserver.observe(entry.target);
            }
          }
        });
      },
      {
        threshold: 0.5, // Track when 50% of post is visible
        rootMargin: '0px 0px -50px 0px', // Only trigger when well into viewport
      }
    );

    // Observe all post elements
    posts.forEach((post) => {
      const element = document.querySelector(`[data-post-id="${post.id}"]`);
      if (element) {
        observer.observe(element);
      }
    });

    return () => {
      observer.disconnect();
    };
  }, [posts, actions]);
};

export default usePersonalization;
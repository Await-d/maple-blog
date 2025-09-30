/**
 * Personalization Feature Index
 * Central exports for user personalization and recommendation functionality
 */

// Core personalization utilities
export const personalizationFeature = {
  name: 'personalization',
  version: '1.0.0',
  description: 'User personalization and recommendation system',
  components: {
    PersonalizedFeed: () => import('@/components/home/PersonalizedFeed').then(m => ({ default: m.PersonalizedFeed })),
    PreferencesPanel: () => import('@/components/common/PreferencesPanel').then(m => ({ default: m.PreferencesPanel }))
  },
  hooks: {
    usePersonalization: () => import('@/hooks/usePersonalization').then(m => m.usePersonalization)
  }
} as const;

// Content types for scoring
interface ContentTag {
  slug: string;
  name?: string;
}

interface ContentCategory {
  slug: string;
  name?: string;
}

interface ContentItem {
  category?: ContentCategory;
  tags?: ContentTag[];
  readingTime?: number;
  contentLength?: 'short' | 'medium' | 'long';
  publishedAt: string;
}

// Personalization types
export interface UserPreferences {
  // Reading preferences
  reading: {
    preferredCategories: string[];
    excludedCategories: string[];
    preferredTags: string[];
    excludedTags: string[];
    readingSpeed: 'slow' | 'average' | 'fast';
    contentLength: 'short' | 'medium' | 'long' | 'any';
    difficultyLevel: 'beginner' | 'intermediate' | 'advanced' | 'any';
  };
  
  // Display preferences
  display: {
    theme: 'light' | 'dark' | 'auto';
    fontSize: 'small' | 'medium' | 'large';
    fontFamily: 'system' | 'serif' | 'sans-serif';
    layout: 'compact' | 'comfortable' | 'spacious';
    showImages: boolean;
    autoplayVideos: boolean;
    reducedMotion: boolean;
  };
  
  // Notification preferences
  notifications: {
    email: {
      enabled: boolean;
      frequency: 'immediate' | 'daily' | 'weekly' | 'monthly';
      types: string[];
    };
    push: {
      enabled: boolean;
      types: string[];
    };
    inApp: {
      enabled: boolean;
      types: string[];
    };
  };
  
  // Privacy preferences
  privacy: {
    allowAnalytics: boolean;
    allowPersonalization: boolean;
    allowTargetedAds: boolean;
    shareReadingData: boolean;
    publicProfile: boolean;
  };
  
  // Language and localization
  localization: {
    language: string;
    timezone: string;
    dateFormat: 'ISO' | 'US' | 'EU';
    numberFormat: 'US' | 'EU' | 'space';
  };
}

export interface PersonalizationProfile {
  userId: string;
  preferences: UserPreferences;
  behaviors: {
    readingHistory: Array<{
      postId: string;
      readAt: string;
      readingTime: number;
      scrollDepth: number;
      engagement: 'low' | 'medium' | 'high';
    }>;
    searchHistory: Array<{
      query: string;
      searchedAt: string;
      clickedResults: string[];
    }>;
    interactionHistory: Array<{
      type: 'like' | 'comment' | 'share' | 'bookmark';
      postId: string;
      interactedAt: string;
    }>;
  };
  recommendations: {
    posts: string[];
    categories: string[];
    tags: string[];
    authors: string[];
    lastUpdated: string;
  };
  segments: string[]; // User segments for targeted content
}

export interface RecommendationEngine {
  // Content-based filtering
  getContentBasedRecommendations(userId: string, limit?: number): Promise<string[]>;
  
  // Collaborative filtering
  getCollaborativeRecommendations(userId: string, limit?: number): Promise<string[]>;
  
  // Hybrid recommendations
  getHybridRecommendations(userId: string, limit?: number): Promise<string[]>;
  
  // Trending content
  getTrendingContent(userId?: string, limit?: number): Promise<string[]>;
  
  // Similar users
  getSimilarUsers(userId: string, limit?: number): Promise<string[]>;
}

// Personalization configuration
export const personalizationConfig = {
  // Recommendation algorithms
  algorithms: {
    contentBased: {
      enabled: true,
      weight: 0.4,
      factors: {
        categories: 0.3,
        tags: 0.25,
        authors: 0.2,
        readingTime: 0.15,
        difficulty: 0.1
      }
    },
    collaborative: {
      enabled: true,
      weight: 0.35,
      minSimilarUsers: 5,
      maxSimilarUsers: 50,
      similarityThreshold: 0.3
    },
    trending: {
      enabled: true,
      weight: 0.15,
      timeWindow: 7, // days
      decayFactor: 0.8
    },
    diversity: {
      enabled: true,
      weight: 0.1,
      categorySpread: 0.7, // How diverse categories should be
      authorSpread: 0.6,
      topicSpread: 0.5
    }
  },
  
  // User segmentation
  segmentation: {
    enabled: true,
    segments: [
      {
        id: 'new_user',
        name: 'New User',
        criteria: {
          daysActive: { max: 7 },
          postsRead: { max: 10 }
        },
        strategy: 'trending_and_popular'
      },
      {
        id: 'casual_reader',
        name: 'Casual Reader',
        criteria: {
          postsPerWeek: { min: 1, max: 5 },
          avgReadingTime: { min: 60, max: 300 }
        },
        strategy: 'content_based'
      },
      {
        id: 'power_user',
        name: 'Power User',
        criteria: {
          postsPerWeek: { min: 10 },
          avgReadingTime: { min: 180 }
        },
        strategy: 'hybrid_advanced'
      },
      {
        id: 'tech_enthusiast',
        name: 'Tech Enthusiast',
        criteria: {
          preferredCategories: ['technology', 'programming', 'web-development']
        },
        strategy: 'topic_focused'
      },
      {
        id: 'content_creator',
        name: 'Content Creator',
        criteria: {
          hasPublishedPosts: true,
          commentsPerPost: { min: 3 }
        },
        strategy: 'creator_focused'
      }
    ]
  },
  
  // Personalization features
  features: {
    smartFeed: {
      enabled: true,
      refreshInterval: 6 * 60 * 60 * 1000, // 6 hours
      maxItems: 50,
      diversityBoost: true
    },
    readingProgress: {
      enabled: true,
      trackScrollDepth: true,
      trackReadingTime: true,
      estimateComprehension: false
    },
    smartNotifications: {
      enabled: true,
      optimalTiming: true,
      contentScoring: true,
      frequencyCapping: true
    },
    adaptiveUI: {
      enabled: true,
      learningRate: 0.1,
      features: ['layout', 'content_density', 'navigation']
    }
  },
  
  // Privacy and data handling
  privacy: {
    dataRetention: {
      readingHistory: 365, // days
      searchHistory: 90,
      interactionHistory: 730,
      preferences: 'indefinite'
    },
    anonymization: {
      enabled: true,
      delayDays: 30,
      excludeFields: ['userId', 'sessionId']
    },
    consent: {
      required: true,
      granular: true,
      withdrawalEnabled: true
    }
  },
  
  // Performance optimization
  performance: {
    caching: {
      recommendations: 60 * 60 * 1000, // 1 hour
      userProfiles: 30 * 60 * 1000, // 30 minutes
      trending: 15 * 60 * 1000, // 15 minutes
      similarities: 2 * 60 * 60 * 1000 // 2 hours
    },
    batchProcessing: {
      enabled: true,
      batchSize: 100,
      processingInterval: 5 * 60 * 1000 // 5 minutes
    },
    precomputation: {
      enabled: true,
      schedule: '0 2 * * *', // Daily at 2 AM
      models: ['collaborative', 'trending', 'similarities']
    }
  }
} as const;

// Personalization utilities
export const personalizationUtils = {
  /**
   * Calculate user similarity based on reading behavior
   */
  calculateUserSimilarity: (user1: PersonalizationProfile, user2: PersonalizationProfile): number => {
    const readPosts1 = new Set(user1.behaviors.readingHistory.map(h => h.postId));
    const readPosts2 = new Set(user2.behaviors.readingHistory.map(h => h.postId));
    
    const intersection = new Set([...readPosts1].filter(x => readPosts2.has(x)));
    const union = new Set([...readPosts1, ...readPosts2]);
    
    // Jaccard similarity
    return union.size > 0 ? intersection.size / union.size : 0;
  },
  
  /**
   * Score content relevance for a user
   */
  scoreContentRelevance: (content: ContentItem, profile: PersonalizationProfile): number => {
    let score = 0;
    const prefs = profile.preferences.reading;

    // Category preference scoring
    if (content.category && prefs.preferredCategories.includes(content.category.slug)) {
      score += 0.3;
    }
    if (content.category && prefs.excludedCategories.includes(content.category.slug)) {
      score -= 0.5;
    }

    // Tag preference scoring
    const contentTags = content.tags?.map((tag: ContentTag) => tag.slug) || [];
    const preferredTagMatches = contentTags.filter((tag: string) => prefs.preferredTags.includes(tag)).length;
    const excludedTagMatches = contentTags.filter((tag: string) => prefs.excludedTags.includes(tag)).length;
    
    score += (preferredTagMatches / Math.max(contentTags.length, 1)) * 0.2;
    score -= (excludedTagMatches / Math.max(contentTags.length, 1)) * 0.3;
    
    // Reading time preference
    const readingTime = content.readingTime || 5;
    const timeMatch = personalizationUtils.matchReadingTimePreference(readingTime, prefs.readingSpeed);
    score += timeMatch * 0.15;
    
    // Content length preference
    const lengthMatch = personalizationUtils.matchContentLengthPreference(content.contentLength || 'medium', prefs.contentLength);
    score += lengthMatch * 0.1;
    
    // Recency boost (newer content gets slight boost)
    const daysSincePublished = (Date.now() - new Date(content.publishedAt).getTime()) / (1000 * 60 * 60 * 24);
    const recencyScore = Math.exp(-daysSincePublished / 30) * 0.1; // Exponential decay over 30 days
    score += recencyScore;
    
    return Math.max(0, Math.min(1, score)); // Clamp between 0 and 1
  },
  
  /**
   * Match reading time preference
   */
  matchReadingTimePreference: (readingTime: number, preference: UserPreferences['reading']['readingSpeed']): number => {
    const speedRanges = {
      slow: { min: 0, max: 10, optimal: 5 },
      average: { min: 5, max: 20, optimal: 10 },
      fast: { min: 15, max: 60, optimal: 25 }
    };
    
    const range = speedRanges[preference];
    if (readingTime >= range.min && readingTime <= range.max) {
      // Calculate how close to optimal
      const distance = Math.abs(readingTime - range.optimal);
      const maxDistance = Math.max(range.optimal - range.min, range.max - range.optimal);
      return 1 - (distance / maxDistance);
    }
    
    return 0;
  },
  
  /**
   * Match content length preference
   */
  matchContentLengthPreference: (contentLength: string, preference: UserPreferences['reading']['contentLength']): number => {
    if (preference === 'any') return 1;
    return contentLength === preference ? 1 : 0.3;
  },
  
  /**
   * Generate default preferences based on user behavior
   */
  generateDefaultPreferences: (_behaviors: PersonalizationProfile['behaviors']): Partial<UserPreferences> => {
    // Note: Full implementation would analyze reading history to determine preferred categories and tags
    // This requires additional post metadata that's not available in the behavior data structure
    // For now, we return sensible defaults based on system recommendations

    return {
      reading: {
        preferredCategories: [],
        excludedCategories: [],
        preferredTags: [],
        excludedTags: [],
        readingSpeed: 'average',
        contentLength: 'any',
        difficultyLevel: 'any'
      },
      display: {
        theme: 'auto',
        fontSize: 'medium',
        fontFamily: 'system',
        layout: 'comfortable',
        showImages: true,
        autoplayVideos: false,
        reducedMotion: false
      },
      privacy: {
        allowAnalytics: true,
        allowPersonalization: true,
        allowTargetedAds: false,
        shareReadingData: false,
        publicProfile: false
      }
    };
  },
  
  /**
   * Update user segment based on current behavior
   */
  updateUserSegment: (profile: PersonalizationProfile): string[] => {
    const segments: string[] = [];
    const { readingHistory } = profile.behaviors;
    // Note: interactionHistory could be used for more sophisticated segmentation in future
    
    // Calculate metrics
    const daysActive = Math.floor(
      (Date.now() - new Date(readingHistory[0]?.readAt || Date.now()).getTime()) / (1000 * 60 * 60 * 24)
    );
    const postsRead = readingHistory.length;
    const avgReadingTime = readingHistory.reduce((sum, h) => sum + h.readingTime, 0) / readingHistory.length;
    const weeklyActivity = readingHistory.filter(h => 
      Date.now() - new Date(h.readAt).getTime() < 7 * 24 * 60 * 60 * 1000
    ).length;
    
    // Apply segmentation rules
    personalizationConfig.segmentation.segments.forEach(segment => {
      const { criteria } = segment;
      let matches = true;

      if ('daysActive' in criteria && criteria.daysActive) {
        if ('min' in criteria.daysActive && criteria.daysActive.min !== undefined && criteria.daysActive.min !== null && daysActive < (criteria.daysActive.min as number)) matches = false;
        if ('max' in criteria.daysActive && criteria.daysActive.max !== undefined && daysActive > criteria.daysActive.max) matches = false;
      }

      if ('postsRead' in criteria && criteria.postsRead) {
        if ('min' in criteria.postsRead && criteria.postsRead.min !== undefined && criteria.postsRead.min !== null && postsRead < (criteria.postsRead.min as number)) matches = false;
        if ('max' in criteria.postsRead && criteria.postsRead.max !== undefined && postsRead > criteria.postsRead.max) matches = false;
      }

      if ('postsPerWeek' in criteria && criteria.postsPerWeek) {
        if ('min' in criteria.postsPerWeek && criteria.postsPerWeek.min !== undefined && weeklyActivity < criteria.postsPerWeek.min) matches = false;
        if ('max' in criteria.postsPerWeek && criteria.postsPerWeek.max !== undefined && weeklyActivity > criteria.postsPerWeek.max) matches = false;
      }

      if ('avgReadingTime' in criteria && criteria.avgReadingTime) {
        if ('min' in criteria.avgReadingTime && criteria.avgReadingTime.min !== undefined && avgReadingTime < criteria.avgReadingTime.min) matches = false;
        if ('max' in criteria.avgReadingTime && criteria.avgReadingTime.max !== undefined && avgReadingTime > criteria.avgReadingTime.max) matches = false;
      }

      // Handle other criteria types
      if ('preferredCategories' in criteria && criteria.preferredCategories) {
        // This would need user's preferred categories to match
        // For now, we'll skip this check since it requires additional user data
      }

      if ('hasPublishedPosts' in criteria && criteria.hasPublishedPosts) {
        // This would need to check if user has published posts
        // For now, we'll skip this check since it requires additional user data
      }

      if ('commentsPerPost' in criteria && criteria.commentsPerPost) {
        // This would need to check user's commenting behavior
        // For now, we'll skip this check since it requires additional user data
      }

      if (matches) {
        segments.push(segment.id);
      }
    });
    
    return segments.length > 0 ? segments : ['new_user'];
  }
};

export default personalizationFeature;
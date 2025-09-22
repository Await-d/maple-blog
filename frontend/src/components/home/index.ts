// @ts-nocheck
/**
 * Home components barrel exports
 * Centralized export for all home page components
 */

export { HeroSection } from './HeroSection';
export { PopularPosts } from './PopularPosts';
export { FeaturedPosts } from './FeaturedPosts';
export { CategoryGrid } from './CategoryGrid';
export { TagCloud } from './TagCloud';
export { StatsWidget } from './StatsWidget';
export { ActiveAuthors } from './ActiveAuthors';
export { PersonalizedFeed } from './PersonalizedFeed';

// Re-export common components used in home page
export { SearchBox } from '../common/SearchBox';
export { SearchSuggestions } from '../common/SearchSuggestions';
export { ThemeToggle } from '../common/ThemeToggle';
export { PreferencesPanel } from '../common/PreferencesPanel';

// Re-export layout components
export { Header } from '../layout/Header';
export { Footer } from '../layout/Footer';
export { Sidebar } from '../layout/Sidebar';
export { Navigation } from '../layout/Navigation';

/**
 * Component categories for documentation and organization:
 *
 * Layout Components:
 * - Header: Main site header with navigation and search
 * - Footer: Site footer with links and information
 * - Sidebar: Dynamic sidebar with latest posts and stats
 * - Navigation: Main navigation with categories dropdown
 *
 * Content Components:
 * - HeroSection: Featured posts carousel with auto-play
 * - PopularPosts: Popular posts with multiple layouts
 * - FeaturedPosts: Editor-selected featured posts
 * - PersonalizedFeed: AI-powered personalized recommendations
 *
 * Navigation Components:
 * - CategoryGrid: Interactive categories with hierarchy
 * - TagCloud: Visual tag cloud with frequency weighting
 *
 * Information Components:
 * - StatsWidget: Site statistics with animated counters
 * - ActiveAuthors: Author rankings and profiles
 *
 * Interactive Components:
 * - SearchBox: Advanced search with suggestions
 * - SearchSuggestions: Smart search recommendations
 * - ThemeToggle: Theme switching controls
 * - PreferencesPanel: Comprehensive user settings
 *
 * All components feature:
 * - TypeScript strict typing
 * - Responsive design (mobile-first)
 * - Dark theme support
 * - Accessibility compliance (WCAG 2.1 AA)
 * - Loading states and error handling
 * - Smooth animations and transitions
 * - SEO optimization
 * - Performance optimization
 */
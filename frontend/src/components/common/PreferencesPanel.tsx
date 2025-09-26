/**
 * PreferencesPanel component - Comprehensive user preferences and settings
 * Features: Theme settings, layout preferences, content filters, accessibility options
 */

import React, { useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import {
  Settings,
  X,
  Save,
  RotateCcw,
  Palette,
  Layout,
  Filter,
  Accessibility,
  Bell,
  Shield,
  User,
  Monitor,
  Grid,
  List,
  Grid3X3,
  Heart,
  CheckCircle,
  AlertTriangle,
  Zap,
  Brain,
  Target as _Target,
  Globe as _Globe,
  Smartphone,
  Tablet,
  Laptop,
} from 'lucide-react';
import { Button } from '../ui/Button';
import { Input as _Input } from '../ui/Input';
import { Modal } from '../ui/Modal';
import { ThemeToggle } from './ThemeToggle';
import { useAuth } from '../../hooks/useAuth';
import {
  useHomeStore,
  usePersonalization,
  usePersonalizationActions,
  useAccessibilitySettings,
  useAccessibilityActions,
  useCurrentTheme,
  useCurrentLayout,
  useIsMobile,
} from '../../stores/homeStore';
import { useCategoryStats, useTagStats } from '../../services/home/homeApi';
import { cn } from '../../utils/cn';
import type { PersonalizationSettings, AccessibilitySettings } from '../../types/home';

interface PreferencesPanelProps {
  isOpen: boolean;
  onClose: () => void;
  className?: string;
  initialTab?: string;
}

interface PreferencesSectionProps {
  title: string;
  icon: React.ReactNode;
  children: React.ReactNode;
  className?: string;
}

interface ToggleSettingProps {
  label: string;
  description?: string;
  value: boolean;
  onChange: (value: boolean) => void;
  disabled?: boolean;
  className?: string;
}

interface SelectSettingProps {
  label: string;
  description?: string;
  value: string | number;
  options: Array<{ value: string | number; label: string; icon?: React.ReactNode }>;
  onChange: (value: string | number) => void;
  disabled?: boolean;
  className?: string;
}

interface MultiSelectSettingProps {
  label: string;
  description?: string;
  values: string[];
  options: Array<{ value: string; label: string; count?: number }>;
  onChange: (values: string[]) => void;
  maxSelections?: number;
  className?: string;
}

const PreferencesSection: React.FC<PreferencesSectionProps> = ({
  title,
  icon,
  children,
  className,
}) => (
  <div className={cn('space-y-4', className)}>
    <div className="flex items-center space-x-3">
      <span className="text-orange-500">{icon}</span>
      <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
        {title}
      </h3>
    </div>
    <div className="space-y-4 pl-8">
      {children}
    </div>
  </div>
);

const ToggleSetting: React.FC<ToggleSettingProps> = ({
  label,
  description,
  value,
  onChange,
  disabled = false,
  className,
}) => (
  <div className={cn('flex items-center justify-between space-x-4', className)}>
    <div className="flex-1">
      <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
        {label}
      </label>
      {description && (
        <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
          {description}
        </p>
      )}
    </div>
    <button
      onClick={() => onChange(!value)}
      disabled={disabled}
      className={cn(
        'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-orange-500 focus:ring-offset-2',
        value
          ? 'bg-orange-500'
          : 'bg-gray-200 dark:bg-gray-700',
        disabled && 'opacity-50 cursor-not-allowed'
      )}
      role="switch"
      aria-checked={value}
    >
      <span
        className={cn(
          'inline-block h-4 w-4 transform rounded-full bg-white shadow-lg transition-transform',
          value ? 'translate-x-6' : 'translate-x-1'
        )}
      />
    </button>
  </div>
);

const SelectSetting: React.FC<SelectSettingProps> = ({
  label,
  description,
  value,
  options,
  onChange,
  disabled = false,
  className,
}) => (
  <div className={cn('space-y-2', className)}>
    <div>
      <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
        {label}
      </label>
      {description && (
        <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
          {description}
        </p>
      )}
    </div>
    <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
      {options.map((option) => (
        <button
          key={option.value}
          onClick={() => onChange(option.value)}
          disabled={disabled}
          className={cn(
            'flex items-center space-x-2 px-3 py-2 rounded-lg border text-sm transition-colors',
            value === option.value
              ? 'border-orange-500 bg-orange-50 text-orange-700 dark:bg-orange-900/20 dark:text-orange-300 dark:border-orange-400'
              : 'border-gray-200 hover:border-gray-300 text-gray-700 dark:border-gray-700 dark:hover:border-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800',
            disabled && 'opacity-50 cursor-not-allowed'
          )}
        >
          {option.icon && <span>{option.icon}</span>}
          <span>{option.label}</span>
        </button>
      ))}
    </div>
  </div>
);

const MultiSelectSetting: React.FC<MultiSelectSettingProps> = ({
  label,
  description,
  values,
  options,
  onChange,
  maxSelections,
  className,
}) => {
  const handleToggle = (value: string) => {
    const isSelected = values.includes(value);
    if (isSelected) {
      onChange(values.filter(v => v !== value));
    } else if (!maxSelections || values.length < maxSelections) {
      onChange([...values, value]);
    }
  };

  return (
    <div className={cn('space-y-2', className)}>
      <div className="flex items-center justify-between">
        <div>
          <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
            {label}
          </label>
          {description && (
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              {description}
            </p>
          )}
        </div>
        {maxSelections && (
          <span className="text-xs text-gray-500 dark:text-gray-400">
            {values.length}/{maxSelections}
          </span>
        )}
      </div>
      <div className="flex flex-wrap gap-2">
        {options.map((option) => {
          const isSelected = values.includes(option.value);
          const isDisabled = Boolean(!isSelected && maxSelections && values.length >= maxSelections);

          return (
            <button
              key={option.value}
              onClick={() => handleToggle(option.value)}
              disabled={isDisabled}
              className={cn(
                'inline-flex items-center space-x-1 px-3 py-1.5 rounded-full text-sm transition-colors',
                isSelected
                  ? 'bg-orange-100 text-orange-700 dark:bg-orange-900/20 dark:text-orange-300'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200 dark:bg-gray-800 dark:text-gray-300 dark:hover:bg-gray-700',
                isDisabled && 'opacity-50 cursor-not-allowed'
              )}
            >
              <span>{option.label}</span>
              {option.count && (
                <span className="text-xs opacity-75">({option.count})</span>
              )}
            </button>
          );
        })}
      </div>
    </div>
  );
};

export const PreferencesPanel: React.FC<PreferencesPanelProps> = ({
  isOpen,
  onClose,
  className,
  initialTab = 'appearance',
}) => {
  const { isAuthenticated, user: _user } = useAuth();
  const isMobile = useIsMobile();
  const currentTheme = useCurrentTheme();
  const currentLayout = useCurrentLayout();

  // Store actions
  const { setLayoutMode, setTheme: _setTheme } = useHomeStore();
  const personalization = usePersonalization();
  const personalizationActions = usePersonalizationActions();
  const accessibility = useAccessibilitySettings();
  const accessibilityActions = useAccessibilityActions();

  // API data
  const { data: categories } = useCategoryStats();
  const { data: tags } = useTagStats(30, 2);

  // Local state
  const [activeTab, setActiveTab] = useState(initialTab);
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);
  const [tempSettings, setTempSettings] = useState({
    personalization: personalization,
    accessibility: accessibility,
  });

  const tabs = [
    { id: 'appearance', label: '外观', icon: <Palette size={16} /> },
    { id: 'layout', label: '布局', icon: <Layout size={16} /> },
    { id: 'content', label: '内容', icon: <Filter size={16} /> },
    { id: 'accessibility', label: '无障碍', icon: <Accessibility size={16} /> },
    { id: 'notifications', label: '通知', icon: <Bell size={16} /> },
    { id: 'privacy', label: '隐私', icon: <Shield size={16} /> },
  ];

  const handleSave = useCallback(() => {
    if (tempSettings.personalization) {
      personalizationActions.updatePersonalization(tempSettings.personalization);
    }
    accessibilityActions.updateAccessibility(tempSettings.accessibility);
    setHasUnsavedChanges(false);
  }, [tempSettings, personalizationActions, accessibilityActions]);

  const handleReset = useCallback(() => {
    setTempSettings({
      personalization: personalization,
      accessibility: accessibility,
    });
    setHasUnsavedChanges(false);
  }, [personalization, accessibility]);

  const updatePersonalizationSetting = useCallback((
    updates: Partial<PersonalizationSettings>
  ) => {
    setTempSettings(prev => ({
      ...prev,
      personalization: prev.personalization ? { ...prev.personalization, ...updates } : undefined,
    }));
    setHasUnsavedChanges(true);
  }, []);

  const updateAccessibilitySetting = useCallback((
    updates: Partial<AccessibilitySettings>
  ) => {
    setTempSettings(prev => ({
      ...prev,
      accessibility: { ...prev.accessibility, ...updates },
    }));
    setHasUnsavedChanges(true);
  }, []);

  if (!isOpen) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} size="lg" className={className}>
      <div className="flex flex-col h-full max-h-[80vh]">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-gray-200 dark:border-gray-700">
          <div className="flex items-center space-x-3">
            <Settings className="text-orange-500" size={24} />
            <h2 className="text-xl font-bold text-gray-900 dark:text-white">
              偏好设置
            </h2>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={onClose}
            className="p-2"
          >
            <X size={20} />
          </Button>
        </div>

        {/* Tabs */}
        <div className="flex space-x-1 p-4 bg-gray-50 dark:bg-gray-800 overflow-x-auto">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={cn(
                'flex items-center space-x-2 px-3 py-2 rounded-lg text-sm font-medium whitespace-nowrap transition-colors',
                activeTab === tab.id
                  ? 'bg-white text-orange-600 shadow-sm dark:bg-gray-700 dark:text-orange-400'
                  : 'text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-gray-200'
              )}
            >
              {tab.icon}
              <span>{tab.label}</span>
            </button>
          ))}
        </div>

        {/* Content */}
        <div className="flex-1 p-6 overflow-y-auto space-y-6">
          {/* Appearance Tab */}
          {activeTab === 'appearance' && (
            <div className="space-y-6">
              <PreferencesSection title="主题设置" icon={<Palette size={20} />}>
                <ThemeToggle variant="dropdown" />

                <div className="pt-4 border-t border-gray-200 dark:border-gray-700">
                  <div className="text-sm text-gray-600 dark:text-gray-400">
                    <p className="mb-2">当前主题: <strong>{currentTheme === 'light' ? '浅色' : currentTheme === 'dark' ? '深色' : '跟随系统'}</strong></p>
                    <p>主题设置会自动保存并在所有设备间同步。</p>
                  </div>
                </div>
              </PreferencesSection>

              <PreferencesSection title="显示设置" icon={<Monitor size={20} />}>
                <SelectSetting
                  label="界面缩放"
                  description="调整界面元素的大小"
                  value={accessibility.fontSizeMultiplier}
                  options={[
                    { value: 0.8, label: '小', icon: <Smartphone size={16} /> },
                    { value: 1.0, label: '标准', icon: <Laptop size={16} /> },
                    { value: 1.2, label: '大', icon: <Tablet size={16} /> },
                  ]}
                  onChange={(value) => updateAccessibilitySetting({ fontSizeMultiplier: value as number })}
                />

                <ToggleSetting
                  label="高对比度"
                  description="提高文字和背景的对比度"
                  value={accessibility.highContrast}
                  onChange={(value) => updateAccessibilitySetting({ highContrast: value })}
                />
              </PreferencesSection>
            </div>
          )}

          {/* Layout Tab */}
          {activeTab === 'layout' && (
            <div className="space-y-6">
              <PreferencesSection title="布局偏好" icon={<Layout size={20} />}>
                <SelectSetting
                  label="默认布局"
                  description="选择内容的默认显示方式"
                  value={currentLayout}
                  options={[
                    { value: 'grid', label: '网格', icon: <Grid3X3 size={16} /> },
                    { value: 'list', label: '列表', icon: <List size={16} /> },
                    { value: 'cards', label: '卡片', icon: <Grid size={16} /> },
                  ]}
                  onChange={(value) => setLayoutMode(value as 'grid' | 'list' | 'cards')}
                />

                <SelectSetting
                  label="每页文章数"
                  description="设置每页显示的文章数量"
                  value={personalization?.postsPerPage || 10}
                  options={[
                    { value: 5, label: '5篇' },
                    { value: 10, label: '10篇' },
                    { value: 15, label: '15篇' },
                    { value: 20, label: '20篇' },
                  ]}
                  onChange={(value) => updatePersonalizationSetting({ postsPerPage: value as number })}
                />

                <ToggleSetting
                  label="显示阅读时间"
                  description="在文章列表中显示预估阅读时间"
                  value={personalization?.showReadingTime || true}
                  onChange={(value) => updatePersonalizationSetting({ showReadingTime: value })}
                />

                <ToggleSetting
                  label="显示文章摘要"
                  description="在列表视图中显示文章摘要"
                  value={personalization?.showSummaries || true}
                  onChange={(value) => updatePersonalizationSetting({ showSummaries: value })}
                />
              </PreferencesSection>

              <PreferencesSection title="响应式设置" icon={<Smartphone size={20} />}>
                <div className="text-sm text-gray-600 dark:text-gray-400">
                  <p className="mb-2">当前设备: <strong>{isMobile ? '移动设备' : '桌面设备'}</strong></p>
                  <p>布局会根据设备屏幕大小自动调整。</p>
                </div>
              </PreferencesSection>
            </div>
          )}

          {/* Content Tab */}
          {activeTab === 'content' && isAuthenticated && (
            <div className="space-y-6">
              <PreferencesSection title="内容偏好" icon={<Heart size={20} />}>
                <MultiSelectSetting
                  label="关注的分类"
                  description="选择您感兴趣的文章分类（最多5个）"
                  values={personalization?.preferredCategories || []}
                  options={categories?.map(cat => ({
                    value: cat.id,
                    label: cat.name,
                    count: cat.postCount,
                  })) || []}
                  onChange={(values) => updatePersonalizationSetting({ preferredCategories: values })}
                  maxSelections={5}
                />

                <MultiSelectSetting
                  label="关注的标签"
                  description="选择您感兴趣的标签（最多10个）"
                  values={personalization?.preferredTags || []}
                  options={tags?.slice(0, 20).map(tag => ({
                    value: tag.id,
                    label: tag.name,
                    count: tag.postCount,
                  })) || []}
                  onChange={(values) => updatePersonalizationSetting({ preferredTags: values })}
                  maxSelections={10}
                />
              </PreferencesSection>

              <PreferencesSection title="个性化推荐" icon={<Brain size={20} />}>
                <div className="bg-blue-50 dark:bg-blue-900/10 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                  <div className="flex items-center space-x-2 mb-2">
                    <Zap className="text-blue-500" size={16} />
                    <h4 className="text-sm font-medium text-blue-900 dark:text-blue-100">
                      智能推荐系统
                    </h4>
                  </div>
                  <p className="text-sm text-blue-800 dark:text-blue-200 mb-3">
                    基于您的阅读历史和兴趣偏好，为您推荐最相关的内容。
                  </p>
                  <Link to="/personalization">
                    <Button variant="outline" size="sm">
                      查看推荐详情
                    </Button>
                  </Link>
                </div>
              </PreferencesSection>
            </div>
          )}

          {/* Content Tab - Not Authenticated */}
          {activeTab === 'content' && !isAuthenticated && (
            <div className="space-y-6">
              <div className="text-center py-8">
                <User size={48} className="mx-auto text-gray-400 mb-4" />
                <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                  登录后享受个性化体验
                </h3>
                <p className="text-gray-500 dark:text-gray-400 mb-4">
                  登录后可以设置内容偏好，获得个性化推荐
                </p>
                <div className="flex items-center justify-center space-x-3">
                  <Link to="/login">
                    <Button variant="primary">
                      立即登录
                    </Button>
                  </Link>
                  <Link to="/register">
                    <Button variant="outline">
                      免费注册
                    </Button>
                  </Link>
                </div>
              </div>
            </div>
          )}

          {/* Accessibility Tab */}
          {activeTab === 'accessibility' && (
            <div className="space-y-6">
              <PreferencesSection title="无障碍访问" icon={<Accessibility size={20} />}>
                <ToggleSetting
                  label="减少动画"
                  description="减少界面中的动画和过渡效果"
                  value={accessibility.reduceMotion}
                  onChange={(value) => updateAccessibilitySetting({ reduceMotion: value })}
                />

                <ToggleSetting
                  label="屏幕阅读器优化"
                  description="优化界面以便屏幕阅读器使用"
                  value={accessibility.screenReaderOptimized}
                  onChange={(value) => updateAccessibilitySetting({ screenReaderOptimized: value })}
                />

                <ToggleSetting
                  label="增强键盘导航"
                  description="改善键盘导航体验"
                  value={accessibility.enhancedKeyboardNav}
                  onChange={(value) => updateAccessibilitySetting({ enhancedKeyboardNav: value })}
                />
              </PreferencesSection>

              <PreferencesSection title="WCAG 兼容性" icon={<CheckCircle size={20} />}>
                <div className="text-sm text-gray-600 dark:text-gray-400">
                  <p className="mb-2">
                    本网站遵循 <strong>WCAG 2.1 AA</strong> 无障碍访问标准。
                  </p>
                  <p>
                    如果您在使用过程中遇到无障碍相关问题，请通过反馈渠道告知我们。
                  </p>
                </div>
              </PreferencesSection>
            </div>
          )}

          {/* Notifications Tab */}
          {activeTab === 'notifications' && (
            <div className="space-y-6">
              <PreferencesSection title="通知设置" icon={<Bell size={20} />}>
                <div className="text-center py-8">
                  <Bell size={48} className="mx-auto text-gray-400 mb-4" />
                  <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                    通知功能开发中
                  </h3>
                  <p className="text-gray-500 dark:text-gray-400">
                    稍后您将可以在这里管理所有通知偏好
                  </p>
                </div>
              </PreferencesSection>
            </div>
          )}

          {/* Privacy Tab */}
          {activeTab === 'privacy' && (
            <div className="space-y-6">
              <PreferencesSection title="隐私设置" icon={<Shield size={20} />}>
                <div className="text-center py-8">
                  <Shield size={48} className="mx-auto text-gray-400 mb-4" />
                  <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                    隐私设置开发中
                  </h3>
                  <p className="text-gray-500 dark:text-gray-400">
                    稍后您将可以在这里管理隐私相关设置
                  </p>
                </div>
              </PreferencesSection>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between p-6 border-t border-gray-200 dark:border-gray-700">
          <div className="flex items-center space-x-2 text-sm text-gray-500 dark:text-gray-400">
            {hasUnsavedChanges && (
              <>
                <AlertTriangle size={16} className="text-amber-500" />
                <span>有未保存的更改</span>
              </>
            )}
          </div>
          <div className="flex items-center space-x-3">
            <Button
              variant="ghost"
              onClick={handleReset}
              disabled={!hasUnsavedChanges}
            >
              <RotateCcw size={16} className="mr-2" />
              重置
            </Button>
            <Button
              variant="primary"
              onClick={handleSave}
              disabled={!hasUnsavedChanges}
            >
              <Save size={16} className="mr-2" />
              保存设置
            </Button>
          </div>
        </div>
      </div>
    </Modal>
  );
};

/**
 * Usage:
 * <PreferencesPanel isOpen={isOpen} onClose={handleClose} />
 * <PreferencesPanel isOpen={isOpen} onClose={handleClose} initialTab="content" />
 *
 * Features:
 * - Comprehensive user preference management
 * - Tabbed interface for organized settings
 * - Real-time preview of changes
 * - Accessibility settings with WCAG compliance
 * - Theme and layout customization
 * - Content filtering and personalization
 * - Responsive design for all screen sizes
 * - Integration with authentication system
 * - Persistent settings storage via Zustand
 * - Unsaved changes detection and warnings
 */

export default PreferencesPanel;
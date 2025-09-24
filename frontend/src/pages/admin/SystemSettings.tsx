/**
 * SystemSettings - 系统设置页面
 * 提供全面的系统配置管理界面
 */

import React, { useState, useEffect, useCallback } from 'react';
import { Helmet } from '@/components/common/DocumentHead';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Button } from '@/components/ui/Button';
import { Input, Textarea } from '@/components/ui/Input';
import { cn } from '@/utils/cn';
import {
  Settings,
  Save,
  Upload,
  Download,
  RotateCcw,
  Search,
  AlertTriangle,
  CheckCircle,
  Globe,
  FileText,
  Shield,
  Mail,
  Zap,
  Palette,
  X,
} from 'lucide-react';
import {
  SystemSettings as ISystemSettings,
  DEFAULT_SETTINGS,
  SettingsState,
  SettingsValidationError,
  VALIDATION_RULES,
} from '@/types/settings';

// 设置分类标签页配置
const SETTINGS_TABS = [
  { id: 'general', label: '常规设置', icon: Globe },
  { id: 'content', label: '内容设置', icon: FileText },
  { id: 'security', label: '用户与安全', icon: Shield },
  { id: 'email', label: '邮件设置', icon: Mail },
  { id: 'performance', label: '性能与缓存', icon: Zap },
  { id: 'appearance', label: '外观设置', icon: Palette },
];

export const SystemSettings: React.FC = () => {
  // 状态管理
  const [settingsState, setSettingsState] = useState<SettingsState>({
    settings: DEFAULT_SETTINGS,
    loading: true,
    saving: false,
    isDirty: false,
    errors: [],
    lastSaved: null,
    autoSaveEnabled: true,
  });

  const [activeTab, setActiveTab] = useState('general');
  const [searchQuery, setSearchQuery] = useState('');
  const [showUnsavedWarning, setShowUnsavedWarning] = useState(false);

  // 加载设置
  const loadSettings = useCallback(async () => {
    try {
      setSettingsState(prev => ({ ...prev, loading: true }));
      
      // 这里应该调用实际的API
      // const response = await settingsApi.getSettings();
      // const settings = response.data;
      
      // 模拟API调用延迟
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      // 使用默认设置作为模拟数据
      setSettingsState(prev => ({
        ...prev,
        settings: DEFAULT_SETTINGS,
        loading: false,
        isDirty: false,
        lastSaved: new Date(),
      }));
    } catch (error) {
      console.error('Failed to load settings:', error);
      setSettingsState(prev => ({
        ...prev,
        loading: false,
        errors: [{ field: 'general', message: '设置加载失败' }],
      }));
    }
  }, []);

  // 保存设置
  const saveSettings = useCallback(async () => {
    try {
      setSettingsState(prev => ({ ...prev, saving: true, errors: [] }));
      
      // 验证设置
      const validationErrors = validateSettings(settingsState.settings);
      if (validationErrors.length > 0) {
        setSettingsState(prev => ({
          ...prev,
          saving: false,
          errors: validationErrors,
        }));
        return;
      }

      // 这里应该调用实际的API
      // await settingsApi.updateSettings(settingsState.settings);
      
      // 模拟API调用延迟
      await new Promise(resolve => setTimeout(resolve, 1500));
      
      setSettingsState(prev => ({
        ...prev,
        saving: false,
        isDirty: false,
        lastSaved: new Date(),
      }));

      // 显示成功消息
      showSuccessMessage('设置已成功保存');
    } catch (error) {
      console.error('Failed to save settings:', error);
      setSettingsState(prev => ({
        ...prev,
        saving: false,
        errors: [{ field: 'general', message: '设置保存失败' }],
      }));
    }
  }, [settingsState.settings]);

  // 重置设置
  const resetSettings = useCallback(() => {
    if (window.confirm('确定要重置所有设置为默认值吗？此操作不可撤销。')) {
      setSettingsState(prev => ({
        ...prev,
        settings: DEFAULT_SETTINGS,
        isDirty: true,
        errors: [],
      }));
    }
  }, []);

  // 导出配置
  const exportSettings = useCallback(() => {
    const dataStr = JSON.stringify(settingsState.settings, null, 2);
    const dataBlob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(dataBlob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `maple-blog-settings-${new Date().toISOString().split('T')[0]}.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }, [settingsState.settings]);

  // 导入配置
  const importSettings = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const importedSettings = JSON.parse(e.target?.result as string);
        
        // 基本验证导入的设置结构
        if (validateImportedSettings(importedSettings)) {
          setSettingsState(prev => ({
            ...prev,
            settings: { ...DEFAULT_SETTINGS, ...importedSettings },
            isDirty: true,
            errors: [],
          }));
          showSuccessMessage('配置导入成功');
        } else {
          throw new Error('Invalid settings format');
        }
      } catch (error) {
        console.error('Failed to import settings:', error);
        setSettingsState(prev => ({
          ...prev,
          errors: [{ field: 'general', message: '配置文件格式无效' }],
        }));
      }
    };
    reader.readAsText(file);
    
    // 重置文件输入
    event.target.value = '';
  }, []);

  // 更新设置字段
  const updateSettings = useCallback(<T extends keyof ISystemSettings>(
    category: T,
    field: keyof ISystemSettings[T],
    value: any
  ) => {
    setSettingsState(prev => ({
      ...prev,
      settings: {
        ...prev.settings,
        [category]: {
          ...prev.settings[category],
          [field]: value,
        },
      },
      isDirty: true,
      errors: prev.errors.filter(error => error.field !== `${category}.${String(field)}`),
    }));
  }, []);

  // 自动保存功能
  useEffect(() => {
    if (!settingsState.autoSaveEnabled || !settingsState.isDirty) return;

    const autoSaveTimer = setTimeout(() => {
      saveSettings();
    }, 5000); // 5秒后自动保存

    return () => clearTimeout(autoSaveTimer);
  }, [settingsState.isDirty, settingsState.autoSaveEnabled, saveSettings]);

  // 页面离开警告
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (settingsState.isDirty) {
        e.preventDefault();
        e.returnValue = '';
      }
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, [settingsState.isDirty]);

  // 初始化加载
  useEffect(() => {
    loadSettings();
  }, [loadSettings]);

  // 过滤标签页
  const filteredTabs = SETTINGS_TABS.filter(tab =>
    searchQuery === '' || 
    tab.label.toLowerCase().includes(searchQuery.toLowerCase())
  );

  if (settingsState.loading) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-950 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-600 dark:text-gray-400">正在加载设置...</p>
        </div>
      </div>
    );
  }

  return (
    <>
      <Helmet>
        <title>系统设置 - Maple Blog</title>
        <meta name="description" content="系统配置和参数设置管理。" />
        <meta name="robots" content="noindex, nofollow" />
      </Helmet>

      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container-responsive py-8">
          <div className="max-w-7xl mx-auto">
            {/* 页面头部 */}
            <div className="mb-8">
              <div className="flex items-center justify-between">
                <div>
                  <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2 flex items-center">
                    <Settings className="w-8 h-8 mr-3 text-blue-600 dark:text-blue-400" />
                    系统设置
                  </h1>
                  <p className="text-lg text-gray-600 dark:text-gray-400">
                    管理网站的各项配置参数
                  </p>
                </div>

                {/* 操作按钮 */}
                <div className="flex items-center space-x-3">
                  {/* 搜索框 */}
                  <div className="relative">
                    <Input
                      type="text"
                      placeholder="搜索设置..."
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                      leftIcon={<Search className="w-4 h-4" />}
                      className="w-64"
                    />
                  </div>

                  {/* 导入配置 */}
                  <label className="cursor-pointer">
                    <input
                      type="file"
                      accept=".json"
                      onChange={importSettings}
                      className="hidden"
                    />
                    <Button variant="outline" size="sm" as="span">
                      <Upload className="w-4 h-4 mr-2" />
                      导入
                    </Button>
                  </label>

                  {/* 导出配置 */}
                  <Button variant="outline" size="sm" onClick={exportSettings}>
                    <Download className="w-4 h-4 mr-2" />
                    导出
                  </Button>

                  {/* 重置设置 */}
                  <Button variant="outline" size="sm" onClick={resetSettings}>
                    <RotateCcw className="w-4 h-4 mr-2" />
                    重置
                  </Button>

                  {/* 保存按钮 */}
                  <Button
                    onClick={saveSettings}
                    disabled={!settingsState.isDirty || settingsState.saving}
                    loading={settingsState.saving}
                    className="min-w-[100px]"
                  >
                    <Save className="w-4 h-4 mr-2" />
                    {settingsState.saving ? '保存中...' : '保存更改'}
                  </Button>
                </div>
              </div>

              {/* 状态指示 */}
              {settingsState.isDirty && (
                <div className="mt-4 flex items-center text-orange-600 dark:text-orange-400">
                  <AlertTriangle className="w-4 h-4 mr-2" />
                  <span className="text-sm">您有未保存的更改</span>
                </div>
              )}

              {settingsState.lastSaved && !settingsState.isDirty && (
                <div className="mt-4 flex items-center text-green-600 dark:text-green-400">
                  <CheckCircle className="w-4 h-4 mr-2" />
                  <span className="text-sm">
                    最后保存时间: {settingsState.lastSaved.toLocaleString()}
                  </span>
                </div>
              )}

              {/* 错误信息 */}
              {settingsState.errors.length > 0 && (
                <div className="mt-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
                  <div className="flex items-start">
                    <AlertTriangle className="w-5 h-5 text-red-600 dark:text-red-400 mt-0.5 mr-3 flex-shrink-0" />
                    <div className="flex-1">
                      <h3 className="text-sm font-medium text-red-800 dark:text-red-200 mb-2">
                        配置错误
                      </h3>
                      <ul className="text-sm text-red-700 dark:text-red-300 space-y-1">
                        {settingsState.errors.map((error, index) => (
                          <li key={index}>{error.message}</li>
                        ))}
                      </ul>
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setSettingsState(prev => ({ ...prev, errors: [] }))}
                    >
                      <X className="w-4 h-4" />
                    </Button>
                  </div>
                </div>
              )}
            </div>

            {/* 设置标签页 */}
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg">
              <Tabs value={activeTab} onValueChange={setActiveTab}>
                {/* 标签页列表 */}
                <div className="border-b border-gray-200 dark:border-gray-700">
                  <TabsList className="w-full justify-start p-0 h-auto bg-transparent">
                    <div className="flex overflow-x-auto">
                      {filteredTabs.map((tab) => {
                        const Icon = tab.icon;
                        return (
                          <TabsTrigger
                            key={tab.id}
                            value={tab.id}
                            className="flex items-center space-x-2 px-6 py-4 border-b-2 border-transparent data-[state=active]:border-blue-600 data-[state=active]:text-blue-600"
                          >
                            <Icon className="w-4 h-4" />
                            <span>{tab.label}</span>
                          </TabsTrigger>
                        );
                      })}
                    </div>
                  </TabsList>
                </div>

                {/* 标签页内容 */}
                <div className="p-8">
                  {/* 这里将渲染各个设置分类的内容组件 */}
                  <TabsContent value="general" className="mt-0">
                    <GeneralSettingsTab
                      settings={settingsState.settings.general}
                      onUpdate={(field, value) => updateSettings('general', field, value)}
                    />
                  </TabsContent>

                  <TabsContent value="content" className="mt-0">
                    <ContentSettingsTab
                      settings={settingsState.settings.content}
                      onUpdate={(field, value) => updateSettings('content', field, value)}
                    />
                  </TabsContent>

                  <TabsContent value="security" className="mt-0">
                    <SecuritySettingsTab
                      settings={settingsState.settings.security}
                      onUpdate={(field, value) => updateSettings('security', field, value)}
                    />
                  </TabsContent>

                  <TabsContent value="email" className="mt-0">
                    <EmailSettingsTab
                      settings={settingsState.settings.email}
                      onUpdate={(field, value) => updateSettings('email', field, value)}
                    />
                  </TabsContent>

                  <TabsContent value="performance" className="mt-0">
                    <PerformanceSettingsTab
                      settings={settingsState.settings.performance}
                      onUpdate={(field, value) => updateSettings('performance', field, value)}
                    />
                  </TabsContent>

                  <TabsContent value="appearance" className="mt-0">
                    <AppearanceSettingsTab
                      settings={settingsState.settings.appearance}
                      onUpdate={(field, value) => updateSettings('appearance', field, value)}
                    />
                  </TabsContent>
                </div>
              </Tabs>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

// 辅助函数
const validateSettings = (settings: ISystemSettings): SettingsValidationError[] => {
  const errors: SettingsValidationError[] = [];

  // 这里实现详细的验证逻辑
  // 根据 VALIDATION_RULES 进行验证

  if (!settings.general.siteTitle) {
    errors.push({ field: 'general.siteTitle', message: '站点标题不能为空' });
  }

  if (settings.general.contactEmail && !isValidEmail(settings.general.contactEmail)) {
    errors.push({ field: 'general.contactEmail', message: '邮箱格式不正确' });
  }

  // 添加更多验证规则...

  return errors;
};

const validateImportedSettings = (settings: any): boolean => {
  // 验证导入的设置格式是否正确
  return typeof settings === 'object' && settings !== null;
};

const isValidEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};

const showSuccessMessage = (message: string) => {
  // 这里可以集成 toast 通知组件
  console.log('Success:', message);
};

// 常规设置标签页组件
const GeneralSettingsTab: React.FC<{
  settings: ISystemSettings['general'];
  onUpdate: (field: keyof ISystemSettings['general'], value: any) => void;
}> = ({ settings, onUpdate }) => {
  const [logoPreview, setLogoPreview] = useState<string>('');
  const [faviconPreview, setFaviconPreview] = useState<string>('');

  const handleFileUpload = (field: 'logo' | 'favicon', file: File) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      const result = e.target?.result as string;
      onUpdate(field, result);
      if (field === 'logo') setLogoPreview(result);
      if (field === 'favicon') setFaviconPreview(result);
    };
    reader.readAsDataURL(file);
  };

  const handleKeywordChange = (keywords: string) => {
    const keywordArray = keywords.split(',').map(k => k.trim()).filter(k => k);
    onUpdate('keywords', keywordArray);
  };

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">常规设置</h2>
        <p className="text-gray-600 dark:text-gray-400 mb-8">
          配置网站的基本信息和联系方式。
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* 网站信息 */}
        <div className="space-y-6">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2">
            网站信息
          </h3>

          <Input
            label="网站标题"
            value={settings.siteTitle}
            onChange={(e) => onUpdate('siteTitle', e.target.value)}
            placeholder="输入网站标题"
            required
            helperText="显示在浏览器标签页和搜索结果中"
          />

          <Textarea
            label="网站描述"
            value={settings.siteDescription}
            onChange={(e) => onUpdate('siteDescription', e.target.value)}
            placeholder="输入网站描述"
            rows={3}
            helperText="用于SEO和社交分享的网站描述"
          />

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              关键词
            </label>
            <Input
              value={settings.keywords.join(', ')}
              onChange={(e) => handleKeywordChange(e.target.value)}
              placeholder="输入关键词，用逗号分隔"
              helperText="用于SEO的网站关键词"
            />
          </div>

          {/* 语言和时区 */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                默认语言
              </label>
              <select
                value={settings.language}
                onChange={(e) => onUpdate('language', e.target.value)}
                className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-blue-500 focus:border-blue-500"
              >
                <option value="zh-CN">中文（简体）</option>
                <option value="zh-TW">中文（繁體）</option>
                <option value="en">English</option>
                <option value="ja">日本語</option>
                <option value="ko">한국어</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                时区
              </label>
              <select
                value={settings.timezone}
                onChange={(e) => onUpdate('timezone', e.target.value)}
                className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-blue-500 focus:border-blue-500"
              >
                <option value="UTC">UTC</option>
                <option value="Asia/Shanghai">亚洲/上海</option>
                <option value="Asia/Tokyo">亚洲/东京</option>
                <option value="America/New_York">美洲/纽约</option>
                <option value="Europe/London">欧洲/伦敦</option>
              </select>
            </div>
          </div>

          {/* 日期时间格式 */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                日期格式
              </label>
              <select
                value={settings.dateFormat}
                onChange={(e) => onUpdate('dateFormat', e.target.value)}
                className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-blue-500 focus:border-blue-500"
              >
                <option value="YYYY-MM-DD">2024-01-01</option>
                <option value="MM/DD/YYYY">01/01/2024</option>
                <option value="DD/MM/YYYY">01/01/2024</option>
                <option value="YYYY年MM月DD日">2024年01月01日</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                时间格式
              </label>
              <select
                value={settings.timeFormat}
                onChange={(e) => onUpdate('timeFormat', e.target.value)}
                className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-blue-500 focus:border-blue-500"
              >
                <option value="HH:mm">24小时制</option>
                <option value="hh:mm A">12小时制</option>
              </select>
            </div>
          </div>
        </div>

        {/* 品牌资源和联系信息 */}
        <div className="space-y-6">
          {/* 品牌资源 */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2 mb-4">
              品牌资源
            </h3>

            {/* Logo 上传 */}
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  网站Logo
                </label>
                <div className="flex items-center space-x-4">
                  <div className="flex-shrink-0">
                    {(logoPreview || settings.logo) ? (
                      <img
                        src={logoPreview || settings.logo}
                        alt="Logo预览"
                        className="w-16 h-16 object-contain border border-gray-200 dark:border-gray-600 rounded"
                      />
                    ) : (
                      <div className="w-16 h-16 bg-gray-100 dark:bg-gray-700 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded flex items-center justify-center">
                        <span className="text-gray-400 text-xs">Logo</span>
                      </div>
                    )}
                  </div>
                  <div>
                    <input
                      type="file"
                      accept="image/*"
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        if (file) handleFileUpload('logo', file);
                      }}
                      className="hidden"
                      id="logo-upload"
                    />
                    <label
                      htmlFor="logo-upload"
                      className="inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 cursor-pointer"
                    >
                      <Upload className="w-4 h-4 mr-2" />
                      上传Logo
                    </label>
                  </div>
                </div>
                <p className="text-sm text-gray-500 mt-2">
                  推荐尺寸: 200x50px，支持PNG、JPG格式
                </p>
              </div>

              {/* Favicon 上传 */}
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  网站图标 (Favicon)
                </label>
                <div className="flex items-center space-x-4">
                  <div className="flex-shrink-0">
                    {(faviconPreview || settings.favicon) ? (
                      <img
                        src={faviconPreview || settings.favicon}
                        alt="Favicon预览"
                        className="w-8 h-8 object-contain border border-gray-200 dark:border-gray-600 rounded"
                      />
                    ) : (
                      <div className="w-8 h-8 bg-gray-100 dark:bg-gray-700 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded flex items-center justify-center">
                        <span className="text-gray-400 text-xs">ICO</span>
                      </div>
                    )}
                  </div>
                  <div>
                    <input
                      type="file"
                      accept="image/x-icon,image/png"
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        if (file) handleFileUpload('favicon', file);
                      }}
                      className="hidden"
                      id="favicon-upload"
                    />
                    <label
                      htmlFor="favicon-upload"
                      className="inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 cursor-pointer"
                    >
                      <Upload className="w-4 h-4 mr-2" />
                      上传图标
                    </label>
                  </div>
                </div>
                <p className="text-sm text-gray-500 mt-2">
                  推荐尺寸: 32x32px，支持ICO、PNG格式
                </p>
              </div>
            </div>
          </div>

          {/* 联系信息 */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2 mb-4">
              联系信息
            </h3>

            <div className="space-y-4">
              <Input
                label="联系邮箱"
                type="email"
                value={settings.contactEmail}
                onChange={(e) => onUpdate('contactEmail', e.target.value)}
                placeholder="contact@example.com"
                helperText="用于接收用户咨询和系统通知"
              />

              <Input
                label="联系电话"
                value={settings.contactPhone}
                onChange={(e) => onUpdate('contactPhone', e.target.value)}
                placeholder="+86 138 0000 0000"
                helperText="可选，用于紧急联系"
              />

              <Textarea
                label="联系地址"
                value={settings.address}
                onChange={(e) => onUpdate('address', e.target.value)}
                placeholder="输入完整地址"
                rows={3}
                helperText="可选，用于显示在联系页面"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

const ContentSettingsTab: React.FC<{
  settings: ISystemSettings['content'];
  onUpdate: (field: keyof ISystemSettings['content'], value: any) => void;
}> = ({ settings, onUpdate }) => {
  const handleImageFormatsChange = (formats: string) => {
    const formatArray = formats.split(',').map(f => f.trim().toLowerCase()).filter(f => f);
    onUpdate('allowedImageFormats', formatArray);
  };

  const toggleFeature = (field: keyof ISystemSettings['content'], currentValue: boolean) => {
    onUpdate(field, !currentValue);
  };

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">内容设置</h2>
        <p className="text-gray-600 dark:text-gray-400 mb-8">
          配置文章发布、评论审核和内容管理相关设置。
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* 文章设置 */}
        <div className="space-y-6">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2">
            文章设置
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                每页文章数
              </label>
              <Input
                type="number"
                value={settings.postsPerPage.toString()}
                onChange={(e) => onUpdate('postsPerPage', parseInt(e.target.value) || 10)}
                min="1"
                max="100"
                helperText="首页和分类页显示的文章数量"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                默认文章状态
              </label>
              <select
                value={settings.defaultPostStatus}
                onChange={(e) => onUpdate('defaultPostStatus', e.target.value)}
                className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-blue-500 focus:border-blue-500"
              >
                <option value="draft">草稿</option>
                <option value="published">发布</option>
              </select>
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              编辑器类型
            </label>
            <select
              value={settings.editorType}
              onChange={(e) => onUpdate('editorType', e.target.value)}
              className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="markdown">Markdown 编辑器</option>
              <option value="wysiwyg">富文本编辑器</option>
              <option value="both">两者都支持</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              自动归档天数
            </label>
            <Input
              type="number"
              value={settings.autoArchiveDays.toString()}
              onChange={(e) => onUpdate('autoArchiveDays', parseInt(e.target.value) || 365)}
              min="1"
              max="3650"
              helperText="超过指定天数的文章自动归档"
            />
          </div>

          {/* 功能开关 */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  启用分类功能
                </label>
                <p className="text-sm text-gray-500">允许为文章设置分类</p>
              </div>
              <button
                type="button"
                onClick={() => toggleFeature('enableCategories', settings.enableCategories)}
                className={cn(
                  'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                  settings.enableCategories ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                )}
              >
                <span
                  className={cn(
                    'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                    settings.enableCategories ? 'translate-x-6' : 'translate-x-1'
                  )}
                />
              </button>
            </div>

            <div className="flex items-center justify-between">
              <div>
                <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  启用标签功能
                </label>
                <p className="text-sm text-gray-500">允许为文章添加标签</p>
              </div>
              <button
                type="button"
                onClick={() => toggleFeature('enableTags', settings.enableTags)}
                className={cn(
                  'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                  settings.enableTags ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                )}
              >
                <span
                  className={cn(
                    'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                    settings.enableTags ? 'translate-x-6' : 'translate-x-1'
                  )}
                />
              </button>
            </div>

            <div className="flex items-center justify-between">
              <div>
                <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  启用社交分享
                </label>
                <p className="text-sm text-gray-500">在文章页面显示分享按钮</p>
              </div>
              <button
                type="button"
                onClick={() => toggleFeature('enableSocialSharing', settings.enableSocialSharing)}
                className={cn(
                  'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                  settings.enableSocialSharing ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                )}
              >
                <span
                  className={cn(
                    'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                    settings.enableSocialSharing ? 'translate-x-6' : 'translate-x-1'
                  )}
                />
              </button>
            </div>
          </div>
        </div>

        {/* 评论和媒体设置 */}
        <div className="space-y-6">
          {/* 评论设置 */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2 mb-4">
              评论设置
            </h3>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                评论审核模式
              </label>
              <select
                value={settings.commentModeration}
                onChange={(e) => onUpdate('commentModeration', e.target.value)}
                className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-blue-500 focus:border-blue-500"
              >
                <option value="auto">自动通过</option>
                <option value="manual">人工审核</option>
                <option value="disabled">禁用评论</option>
              </select>
              <p className="text-sm text-gray-500 mt-1">
                {settings.commentModeration === 'auto' && '评论会立即显示在网站上'}
                {settings.commentModeration === 'manual' && '评论需要管理员审核后才显示'}
                {settings.commentModeration === 'disabled' && '完全关闭评论功能'}
              </p>
            </div>
          </div>

          {/* 媒体设置 */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2 mb-4">
              媒体设置
            </h3>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  允许的图片格式
                </label>
                <Input
                  value={settings.allowedImageFormats.join(', ')}
                  onChange={(e) => handleImageFormatsChange(e.target.value)}
                  placeholder="jpg, jpeg, png, gif, webp"
                  helperText="支持的图片文件格式，用逗号分隔"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  最大图片大小 (MB)
                </label>
                <Input
                  type="number"
                  value={settings.maxImageSize.toString()}
                  onChange={(e) => onUpdate('maxImageSize', parseInt(e.target.value) || 5)}
                  min="1"
                  max="100"
                  step="0.1"
                  helperText="单个图片文件的最大允许大小"
                />
              </div>
            </div>
          </div>

          {/* SEO设置 */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2 mb-4">
              SEO设置
            </h3>

            <div className="flex items-center justify-between">
              <div>
                <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  启用SEO优化
                </label>
                <p className="text-sm text-gray-500">自动生成meta标签和结构化数据</p>
              </div>
              <button
                type="button"
                onClick={() => toggleFeature('enableSEO', settings.enableSEO)}
                className={cn(
                  'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                  settings.enableSEO ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                )}
              >
                <span
                  className={cn(
                    'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                    settings.enableSEO ? 'translate-x-6' : 'translate-x-1'
                  )}
                />
              </button>
            </div>
          </div>

          {/* 预览区域 */}
          <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4">
            <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-2">设置预览</h4>
            <div className="text-sm text-gray-600 dark:text-gray-400 space-y-1">
              <p>• 每页显示 {settings.postsPerPage} 篇文章</p>
              <p>• 评论模式: {
                settings.commentModeration === 'auto' ? '自动通过' :
                settings.commentModeration === 'manual' ? '人工审核' : '已禁用'
              }</p>
              <p>• 支持格式: {settings.allowedImageFormats.join(', ')}</p>
              <p>• 最大图片: {settings.maxImageSize}MB</p>
              <p>• SEO优化: {settings.enableSEO ? '已启用' : '已禁用'}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

const SecuritySettingsTab: React.FC<{
  settings: ISystemSettings['security'];
  onUpdate: (field: keyof ISystemSettings['security'], value: any) => void;
}> = ({ settings, onUpdate }) => {
  const toggleFeature = (field: keyof ISystemSettings['security'], currentValue: boolean) => {
    onUpdate(field, !currentValue);
  };

  const getPasswordStrengthDescription = () => {
    const requirements = [];
    if (settings.passwordMinLength >= 8) requirements.push(`至少${settings.passwordMinLength}位`);
    if (settings.requireNumbers) requirements.push('包含数字');
    if (settings.requireUppercase) requirements.push('包含大写字母');
    if (settings.requireSpecialChars) requirements.push('包含特殊字符');
    return requirements.length > 0 ? requirements.join('、') : '无要求';
  };

  const getSecurityLevel = () => {
    let score = 0;
    if (settings.passwordMinLength >= 8) score += 1;
    if (settings.passwordMinLength >= 12) score += 1;
    if (settings.requireNumbers) score += 1;
    if (settings.requireUppercase) score += 1;
    if (settings.requireSpecialChars) score += 1;
    if (settings.enable2FA) score += 2;
    if (settings.maxLoginAttempts <= 3) score += 1;

    if (score >= 6) return { level: '高', color: 'text-green-600', bgColor: 'bg-green-100' };
    if (score >= 4) return { level: '中等', color: 'text-yellow-600', bgColor: 'bg-yellow-100' };
    return { level: '低', color: 'text-red-600', bgColor: 'bg-red-100' };
  };

  const securityLevel = getSecurityLevel();

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">用户与安全设置</h2>
        <p className="text-gray-600 dark:text-gray-400 mb-8">
          配置用户注册、密码策略和账户安全相关设置。
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* 用户注册设置 */}
        <div className="space-y-6">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2">
            用户注册设置
          </h3>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              用户注册模式
            </label>
            <select
              value={settings.userRegistration}
              onChange={(e) => onUpdate('userRegistration', e.target.value)}
              className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="open">开放注册</option>
              <option value="invite">邀请注册</option>
              <option value="disabled">禁止注册</option>
            </select>
            <p className="text-sm text-gray-500 mt-1">
              {settings.userRegistration === 'open' && '任何人都可以注册账户'}
              {settings.userRegistration === 'invite' && '需要邀请码才能注册'}
              {settings.userRegistration === 'disabled' && '完全关闭用户注册'}
            </p>
          </div>

          {/* 密码策略 */}
          <div>
            <h4 className="text-md font-medium text-gray-900 dark:text-white mb-4">密码策略</h4>
            
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  最小密码长度
                </label>
                <Input
                  type="number"
                  value={settings.passwordMinLength.toString()}
                  onChange={(e) => onUpdate('passwordMinLength', parseInt(e.target.value) || 8)}
                  min="6"
                  max="64"
                  helperText="推荐至少8位以上"
                />
              </div>

              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                      要求包含数字
                    </label>
                    <p className="text-sm text-gray-500">密码必须包含至少一个数字</p>
                  </div>
                  <button
                    type="button"
                    onClick={() => toggleFeature('requireNumbers', settings.requireNumbers)}
                    className={cn(
                      'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                      settings.requireNumbers ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                    )}
                  >
                    <span
                      className={cn(
                        'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                        settings.requireNumbers ? 'translate-x-6' : 'translate-x-1'
                      )}
                    />
                  </button>
                </div>

                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                      要求包含大写字母
                    </label>
                    <p className="text-sm text-gray-500">密码必须包含至少一个大写字母</p>
                  </div>
                  <button
                    type="button"
                    onClick={() => toggleFeature('requireUppercase', settings.requireUppercase)}
                    className={cn(
                      'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                      settings.requireUppercase ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                    )}
                  >
                    <span
                      className={cn(
                        'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                        settings.requireUppercase ? 'translate-x-6' : 'translate-x-1'
                      )}
                    />
                  </button>
                </div>

                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                      要求包含特殊字符
                    </label>
                    <p className="text-sm text-gray-500">密码必须包含符号 (!@#$%^&*)</p>
                  </div>
                  <button
                    type="button"
                    onClick={() => toggleFeature('requireSpecialChars', settings.requireSpecialChars)}
                    className={cn(
                      'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                      settings.requireSpecialChars ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                    )}
                  >
                    <span
                      className={cn(
                        'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                        settings.requireSpecialChars ? 'translate-x-6' : 'translate-x-1'
                      )}
                    />
                  </button>
                </div>
              </div>
            </div>

            {/* 密码强度预览 */}
            <div className="mt-4 p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
              <p className="text-sm text-gray-600 dark:text-gray-400">
                <span className="font-medium">当前密码要求:</span> {getPasswordStrengthDescription()}
              </p>
            </div>
          </div>
        </div>

        {/* 会话和安全设置 */}
        <div className="space-y-6">
          {/* 会话管理 */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2 mb-4">
              会话管理
            </h3>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  会话超时 (分钟)
                </label>
                <Input
                  type="number"
                  value={settings.sessionTimeout.toString()}
                  onChange={(e) => onUpdate('sessionTimeout', parseInt(e.target.value) || 30)}
                  min="5"
                  max="1440"
                  helperText="用户无活动后自动登出的时间"
                />
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    启用双因子认证
                  </label>
                  <p className="text-sm text-gray-500">提供额外的账户安全保护</p>
                </div>
                <button
                  type="button"
                  onClick={() => toggleFeature('enable2FA', settings.enable2FA)}
                  className={cn(
                    'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                    settings.enable2FA ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                  )}
                >
                  <span
                    className={cn(
                      'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                      settings.enable2FA ? 'translate-x-6' : 'translate-x-1'
                    )}
                  />
                </button>
              </div>
            </div>
          </div>

          {/* 登录安全 */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2 mb-4">
              登录安全
            </h3>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  最大失败尝试次数
                </label>
                <Input
                  type="number"
                  value={settings.maxLoginAttempts.toString()}
                  onChange={(e) => onUpdate('maxLoginAttempts', parseInt(e.target.value) || 5)}
                  min="1"
                  max="20"
                  helperText="达到次数后账户将被锁定"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  锁定时长 (分钟)
                </label>
                <Input
                  type="number"
                  value={settings.lockoutDuration.toString()}
                  onChange={(e) => onUpdate('lockoutDuration', parseInt(e.target.value) || 15)}
                  min="1"
                  max="1440"
                  helperText="账户锁定的持续时间"
                />
              </div>
            </div>
          </div>

          {/* 法律页面链接 */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2 mb-4">
              法律文档
            </h3>

            <div className="space-y-4">
              <Input
                label="隐私政策链接"
                value={settings.privacyPolicyUrl}
                onChange={(e) => onUpdate('privacyPolicyUrl', e.target.value)}
                placeholder="https://example.com/privacy"
                helperText="隐私政策页面的URL"
              />

              <Input
                label="服务条款链接"
                value={settings.termsOfServiceUrl}
                onChange={(e) => onUpdate('termsOfServiceUrl', e.target.value)}
                placeholder="https://example.com/terms"
                helperText="服务条款页面的URL"
              />
            </div>
          </div>

          {/* 安全级别指示 */}
          <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4">
            <div className="flex items-center justify-between mb-3">
              <h4 className="text-sm font-medium text-gray-900 dark:text-white">安全级别</h4>
              <span className={cn(
                'px-2 py-1 rounded text-xs font-medium',
                securityLevel.color,
                securityLevel.bgColor,
                'dark:text-white dark:bg-opacity-20'
              )}>
                {securityLevel.level}
              </span>
            </div>
            <div className="text-sm text-gray-600 dark:text-gray-400 space-y-1">
              <p>• 会话超时: {settings.sessionTimeout} 分钟</p>
              <p>• 最大登录尝试: {settings.maxLoginAttempts} 次</p>
              <p>• 锁定时长: {settings.lockoutDuration} 分钟</p>
              <p>• 双因子认证: {settings.enable2FA ? '已启用' : '已禁用'}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

const EmailSettingsTab: React.FC<{
  settings: ISystemSettings['email'];
  onUpdate: (field: keyof ISystemSettings['email'], value: any) => void;
}> = ({ settings, onUpdate }) => {
  const [testEmailSending, setTestEmailSending] = useState(false);
  const [testEmailResult, setTestEmailResult] = useState<{ success: boolean; message: string } | null>(null);
  const [showPassword, setShowPassword] = useState(false);

  const toggleFeature = (field: keyof ISystemSettings['email'], currentValue: boolean) => {
    onUpdate(field, !currentValue);
  };

  const sendTestEmail = async () => {
    setTestEmailSending(true);
    setTestEmailResult(null);

    try {
      // 模拟测试邮件发送
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      // 这里应该调用实际的API
      // const result = await emailApi.sendTestEmail(settings);
      
      setTestEmailResult({
        success: true,
        message: '测试邮件已成功发送到配置的邮箱地址'
      });
    } catch (error) {
      setTestEmailResult({
        success: false,
        message: '邮件发送失败，请检查SMTP配置'
      });
    } finally {
      setTestEmailSending(false);
    }
  };

  const isEmailConfigured = () => {
    return settings.smtpServer && settings.smtpUsername && settings.smtpPassword && settings.senderEmail;
  };

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">邮件设置</h2>
        <p className="text-gray-600 dark:text-gray-400 mb-8">
          配置SMTP服务器和邮件通知相关设置。
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* SMTP配置 */}
        <div className="space-y-6">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2">
            SMTP配置
          </h3>

          <div className="space-y-4">
            <Input
              label="SMTP服务器"
              value={settings.smtpServer}
              onChange={(e) => onUpdate('smtpServer', e.target.value)}
              placeholder="smtp.example.com"
              required
              helperText="邮件服务商的SMTP服务器地址"
            />

            <div className="grid grid-cols-2 gap-4">
              <Input
                label="SMTP端口"
                type="number"
                value={settings.smtpPort.toString()}
                onChange={(e) => onUpdate('smtpPort', parseInt(e.target.value) || 587)}
                placeholder="587"
                helperText="通常为25、465或587"
              />

              <div className="flex items-center justify-between pt-6">
                <div>
                  <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    使用SSL/TLS
                  </label>
                  <p className="text-sm text-gray-500">启用安全连接</p>
                </div>
                <button
                  type="button"
                  onClick={() => toggleFeature('smtpSecure', settings.smtpSecure)}
                  className={cn(
                    'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                    settings.smtpSecure ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                  )}
                >
                  <span
                    className={cn(
                      'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                      settings.smtpSecure ? 'translate-x-6' : 'translate-x-1'
                    )}
                  />
                </button>
              </div>
            </div>

            <Input
              label="SMTP用户名"
              value={settings.smtpUsername}
              onChange={(e) => onUpdate('smtpUsername', e.target.value)}
              placeholder="username@example.com"
              required
              helperText="通常是您的邮箱地址"
            />

            <div className="relative">
              <Input
                label="SMTP密码"
                type={showPassword ? 'text' : 'password'}
                value={settings.smtpPassword}
                onChange={(e) => onUpdate('smtpPassword', e.target.value)}
                placeholder="输入邮箱密码或应用专用密码"
                required
                helperText="建议使用应用专用密码而不是账户密码"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-9 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
              >
                {showPassword ? (
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L6.111 6.111M14.12 14.12l3.767 3.768M14.12 14.12L6.111 6.111m7.12 8.009a3 3 0 01-4.243-4.243" />
                  </svg>
                ) : (
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                  </svg>
                )}
              </button>
            </div>

            {/* 测试邮件 */}
            <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
              <div className="flex items-start justify-between">
                <div>
                  <h4 className="text-sm font-medium text-blue-800 dark:text-blue-200 mb-1">
                    测试SMTP配置
                  </h4>
                  <p className="text-sm text-blue-700 dark:text-blue-300">
                    发送测试邮件验证配置是否正确
                  </p>
                </div>
                <Button
                  size="sm"
                  onClick={sendTestEmail}
                  disabled={!isEmailConfigured() || testEmailSending}
                  loading={testEmailSending}
                  className="min-w-[80px]"
                >
                  {testEmailSending ? '发送中...' : '发送测试'}
                </Button>
              </div>
              
              {testEmailResult && (
                <div className={cn(
                  'mt-3 p-2 rounded text-sm',
                  testEmailResult.success 
                    ? 'bg-green-100 dark:bg-green-900/20 text-green-800 dark:text-green-200'
                    : 'bg-red-100 dark:bg-red-900/20 text-red-800 dark:text-red-200'
                )}>
                  {testEmailResult.message}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* 邮件发送设置 */}
        <div className="space-y-6">
          {/* 发送方信息 */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2 mb-4">
              发送方信息
            </h3>

            <div className="space-y-4">
              <Input
                label="发送方名称"
                value={settings.senderName}
                onChange={(e) => onUpdate('senderName', e.target.value)}
                placeholder="Maple Blog"
                helperText="显示在邮件发送方的名称"
              />

              <Input
                label="发送方邮箱"
                type="email"
                value={settings.senderEmail}
                onChange={(e) => onUpdate('senderEmail', e.target.value)}
                placeholder="noreply@example.com"
                required
                helperText="用作邮件发送方地址"
              />
            </div>
          </div>

          {/* 通知设置 */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2 mb-4">
              通知设置
            </h3>

            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    新用户注册通知
                  </label>
                  <p className="text-sm text-gray-500">有新用户注册时发送邮件</p>
                </div>
                <button
                  type="button"
                  onClick={() => toggleFeature('notifyNewUser', settings.notifyNewUser)}
                  className={cn(
                    'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                    settings.notifyNewUser ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                  )}
                >
                  <span
                    className={cn(
                      'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                      settings.notifyNewUser ? 'translate-x-6' : 'translate-x-1'
                    )}
                  />
                </button>
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    新评论通知
                  </label>
                  <p className="text-sm text-gray-500">有新评论时发送邮件</p>
                </div>
                <button
                  type="button"
                  onClick={() => toggleFeature('notifyNewComment', settings.notifyNewComment)}
                  className={cn(
                    'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                    settings.notifyNewComment ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                  )}
                >
                  <span
                    className={cn(
                      'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                      settings.notifyNewComment ? 'translate-x-6' : 'translate-x-1'
                    )}
                  />
                </button>
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    新文章发布通知
                  </label>
                  <p className="text-sm text-gray-500">有新文章发布时发送邮件</p>
                </div>
                <button
                  type="button"
                  onClick={() => toggleFeature('notifyNewPost', settings.notifyNewPost)}
                  className={cn(
                    'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                    settings.notifyNewPost ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                  )}
                >
                  <span
                    className={cn(
                      'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                      settings.notifyNewPost ? 'translate-x-6' : 'translate-x-1'
                    )}
                  />
                </button>
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    邮箱验证
                  </label>
                  <p className="text-sm text-gray-500">要求用户验证邮箱地址</p>
                </div>
                <button
                  type="button"
                  onClick={() => toggleFeature('emailVerificationRequired', settings.emailVerificationRequired)}
                  className={cn(
                    'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                    settings.emailVerificationRequired ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                  )}
                >
                  <span
                    className={cn(
                      'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                      settings.emailVerificationRequired ? 'translate-x-6' : 'translate-x-1'
                    )}
                  />
                </button>
              </div>
            </div>
          </div>

          {/* 常用邮件服务商配置示例 */}
          <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4">
            <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-3">常用邮件服务商配置</h4>
            <div className="text-xs text-gray-600 dark:text-gray-400 space-y-2">
              <div>
                <span className="font-medium">Gmail:</span> smtp.gmail.com:587 (TLS)
              </div>
              <div>
                <span className="font-medium">QQ邮箱:</span> smtp.qq.com:587 (TLS)
              </div>
              <div>
                <span className="font-medium">163邮箱:</span> smtp.163.com:25/465
              </div>
              <div>
                <span className="font-medium">Outlook:</span> smtp-mail.outlook.com:587 (TLS)
              </div>
            </div>
          </div>

          {/* 配置状态 */}
          <div className={cn(
            'p-4 rounded-lg border',
            isEmailConfigured()
              ? 'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800'
              : 'bg-yellow-50 dark:bg-yellow-900/20 border-yellow-200 dark:border-yellow-800'
          )}>
            <div className="flex items-start">
              {isEmailConfigured() ? (
                <CheckCircle className="w-5 h-5 text-green-600 dark:text-green-400 mt-0.5 mr-3 flex-shrink-0" />
              ) : (
                <AlertTriangle className="w-5 h-5 text-yellow-600 dark:text-yellow-400 mt-0.5 mr-3 flex-shrink-0" />
              )}
              <div>
                <h4 className={cn(
                  'text-sm font-medium mb-1',
                  isEmailConfigured() 
                    ? 'text-green-800 dark:text-green-200'
                    : 'text-yellow-800 dark:text-yellow-200'
                )}>
                  {isEmailConfigured() ? '邮件配置完成' : '邮件配置不完整'}
                </h4>
                <p className={cn(
                  'text-sm',
                  isEmailConfigured()
                    ? 'text-green-700 dark:text-green-300'
                    : 'text-yellow-700 dark:text-yellow-300'
                )}>
                  {isEmailConfigured() 
                    ? '系统可以正常发送邮件通知'
                    : '请完成SMTP服务器和发送方邮箱的配置'
                  }
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

const PerformanceSettingsTab: React.FC<{
  settings: ISystemSettings['performance'];
  onUpdate: (field: keyof ISystemSettings['performance'], value: any) => void;
}> = ({ settings, onUpdate }) => {
  const [clearingCache, setClearingCache] = useState(false);
  const [cacheCleared, setCacheCleared] = useState(false);

  const toggleFeature = (field: keyof ISystemSettings['performance'], currentValue: boolean) => {
    onUpdate(field, !currentValue);
  };

  const clearCache = async () => {
    setClearingCache(true);
    setCacheCleared(false);

    try {
      // 模拟清理缓存
      await new Promise(resolve => setTimeout(resolve, 1500));
      
      // 这里应该调用实际的API
      // await cacheApi.clearAll();
      
      setCacheCleared(true);
      setTimeout(() => setCacheCleared(false), 3000);
    } catch (error) {
      console.error('Failed to clear cache:', error);
    } finally {
      setClearingCache(false);
    }
  };

  const formatCacheExpiration = (seconds: number) => {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    if (hours > 0) return `${hours}小时${minutes > 0 ? `${minutes}分钟` : ''}`;
    return `${minutes}分钟`;
  };

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">性能与缓存设置</h2>
        <p className="text-gray-600 dark:text-gray-400 mb-8">
          配置缓存策略、CDN和性能优化相关设置。
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* 缓存设置 */}
        <div className="space-y-6">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2">
            缓存设置
          </h3>

          <div className="flex items-center justify-between">
            <div>
              <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                启用缓存
              </label>
              <p className="text-sm text-gray-500">提高网站访问速度</p>
            </div>
            <button
              type="button"
              onClick={() => toggleFeature('enableCache', settings.enableCache)}
              className={cn(
                'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                settings.enableCache ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
              )}
            >
              <span
                className={cn(
                  'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                  settings.enableCache ? 'translate-x-6' : 'translate-x-1'
                )}
              />
            </button>
          </div>

          {settings.enableCache && (
            <div className="space-y-4 pl-4 border-l-2 border-blue-100 dark:border-blue-900">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  缓存类型
                </label>
                <select
                  value={settings.cacheType}
                  onChange={(e) => onUpdate('cacheType', e.target.value)}
                  className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-blue-500 focus:border-blue-500"
                >
                  <option value="memory">内存缓存</option>
                  <option value="redis">Redis缓存</option>
                  <option value="file">文件缓存</option>
                </select>
                <p className="text-sm text-gray-500 mt-1">
                  {settings.cacheType === 'memory' && '适合单机部署，重启后缓存丢失'}
                  {settings.cacheType === 'redis' && '适合分布式部署，性能最佳'}
                  {settings.cacheType === 'file' && '持久化缓存，适合小型网站'}
                </p>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  缓存过期时间
                </label>
                <div className="flex items-center space-x-3">
                  <Input
                    type="number"
                    value={settings.cacheExpiration.toString()}
                    onChange={(e) => onUpdate('cacheExpiration', parseInt(e.target.value) || 3600)}
                    min="60"
                    max="86400"
                    className="flex-1"
                  />
                  <span className="text-sm text-gray-500">秒</span>
                </div>
                <p className="text-sm text-gray-500 mt-1">
                  当前设置: {formatCacheExpiration(settings.cacheExpiration)}
                </p>
              </div>

              {/* 缓存管理 */}
              <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                <div className="flex items-start justify-between">
                  <div>
                    <h4 className="text-sm font-medium text-blue-800 dark:text-blue-200 mb-1">
                      缓存管理
                    </h4>
                    <p className="text-sm text-blue-700 dark:text-blue-300">
                      清理所有缓存数据，释放内存空间
                    </p>
                  </div>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={clearCache}
                    disabled={clearingCache}
                    loading={clearingCache}
                    className="min-w-[80px]"
                  >
                    {clearingCache ? '清理中...' : '清理缓存'}
                  </Button>
                </div>
                
                {cacheCleared && (
                  <div className="mt-3 p-2 rounded text-sm bg-green-100 dark:bg-green-900/20 text-green-800 dark:text-green-200">
                    缓存已成功清理
                  </div>
                )}
              </div>
            </div>
          )}

          {/* 维护模式 */}
          <div className="pt-4 border-t border-gray-200 dark:border-gray-700">
            <div className="flex items-center justify-between mb-4">
              <div>
                <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  维护模式
                </label>
                <p className="text-sm text-gray-500">暂停网站访问，显示维护页面</p>
              </div>
              <button
                type="button"
                onClick={() => toggleFeature('maintenanceMode', settings.maintenanceMode)}
                className={cn(
                  'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2',
                  settings.maintenanceMode ? 'bg-red-600' : 'bg-gray-200 dark:bg-gray-600'
                )}
              >
                <span
                  className={cn(
                    'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                    settings.maintenanceMode ? 'translate-x-6' : 'translate-x-1'
                  )}
                />
              </button>
            </div>

            {settings.maintenanceMode && (
              <div className="pl-4 border-l-2 border-red-100 dark:border-red-900">
                <Textarea
                  label="维护信息"
                  value={settings.maintenanceMessage}
                  onChange={(e) => onUpdate('maintenanceMessage', e.target.value)}
                  placeholder="网站正在维护中，请稍后再试..."
                  rows={3}
                  helperText="将显示给访问者的维护信息"
                />
              </div>
            )}
          </div>
        </div>

        {/* 优化和备份设置 */}
        <div className="space-y-6">
          {/* 性能优化 */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2 mb-4">
              性能优化
            </h3>

            <div className="space-y-4">
              <Input
                label="CDN地址"
                value={settings.cdnUrl}
                onChange={(e) => onUpdate('cdnUrl', e.target.value)}
                placeholder="https://cdn.example.com"
                helperText="用于加速静态资源访问的CDN地址"
              />

              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    启用图片优化
                  </label>
                  <p className="text-sm text-gray-500">自动压缩和优化上传的图片</p>
                </div>
                <button
                  type="button"
                  onClick={() => toggleFeature('enableImageOptimization', settings.enableImageOptimization)}
                  className={cn(
                    'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                    settings.enableImageOptimization ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                  )}
                >
                  <span
                    className={cn(
                      'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                      settings.enableImageOptimization ? 'translate-x-6' : 'translate-x-1'
                    )}
                  />
                </button>
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    启用Gzip压缩
                  </label>
                  <p className="text-sm text-gray-500">压缩响应内容，减少传输大小</p>
                </div>
                <button
                  type="button"
                  onClick={() => toggleFeature('enableGzipCompression', settings.enableGzipCompression)}
                  className={cn(
                    'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                    settings.enableGzipCompression ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
                  )}
                >
                  <span
                    className={cn(
                      'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                      settings.enableGzipCompression ? 'translate-x-6' : 'translate-x-1'
                    )}
                  />
                </button>
              </div>
            </div>
          </div>

          {/* 备份设置 */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2 mb-4">
              备份设置
            </h3>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                自动备份频率
              </label>
              <select
                value={settings.backupSchedule}
                onChange={(e) => onUpdate('backupSchedule', e.target.value)}
                className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-blue-500 focus:border-blue-500"
              >
                <option value="disabled">禁用备份</option>
                <option value="daily">每天备份</option>
                <option value="weekly">每周备份</option>
                <option value="monthly">每月备份</option>
              </select>
              <p className="text-sm text-gray-500 mt-1">
                {settings.backupSchedule === 'disabled' && '不会自动创建备份'}
                {settings.backupSchedule === 'daily' && '每天凌晨自动备份数据库和文件'}
                {settings.backupSchedule === 'weekly' && '每周日凌晨自动备份'}
                {settings.backupSchedule === 'monthly' && '每月1号凌晨自动备份'}
              </p>
            </div>
          </div>

          {/* 性能监控 */}
          <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4">
            <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-3">性能状态</h4>
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <span className="text-gray-500">缓存状态:</span>
                <span className={cn(
                  'ml-2 font-medium',
                  settings.enableCache ? 'text-green-600' : 'text-red-600'
                )}>
                  {settings.enableCache ? '已启用' : '已禁用'}
                </span>
              </div>
              <div>
                <span className="text-gray-500">缓存类型:</span>
                <span className="ml-2 font-medium text-gray-900 dark:text-white">
                  {settings.cacheType === 'memory' && '内存缓存'}
                  {settings.cacheType === 'redis' && 'Redis缓存'}
                  {settings.cacheType === 'file' && '文件缓存'}
                </span>
              </div>
              <div>
                <span className="text-gray-500">图片优化:</span>
                <span className={cn(
                  'ml-2 font-medium',
                  settings.enableImageOptimization ? 'text-green-600' : 'text-gray-600'
                )}>
                  {settings.enableImageOptimization ? '已启用' : '已禁用'}
                </span>
              </div>
              <div>
                <span className="text-gray-500">Gzip压缩:</span>
                <span className={cn(
                  'ml-2 font-medium',
                  settings.enableGzipCompression ? 'text-green-600' : 'text-gray-600'
                )}>
                  {settings.enableGzipCompression ? '已启用' : '已禁用'}
                </span>
              </div>
              <div>
                <span className="text-gray-500">备份频率:</span>
                <span className="ml-2 font-medium text-gray-900 dark:text-white">
                  {settings.backupSchedule === 'disabled' && '已禁用'}
                  {settings.backupSchedule === 'daily' && '每天'}
                  {settings.backupSchedule === 'weekly' && '每周'}
                  {settings.backupSchedule === 'monthly' && '每月'}
                </span>
              </div>
              <div>
                <span className="text-gray-500">维护模式:</span>
                <span className={cn(
                  'ml-2 font-medium',
                  settings.maintenanceMode ? 'text-red-600' : 'text-green-600'
                )}>
                  {settings.maintenanceMode ? '已启用' : '正常运行'}
                </span>
              </div>
            </div>
          </div>

          {/* 性能建议 */}
          <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
            <h4 className="text-sm font-medium text-blue-800 dark:text-blue-200 mb-2">性能优化建议</h4>
            <ul className="text-sm text-blue-700 dark:text-blue-300 space-y-1">
              {!settings.enableCache && (
                <li>• 启用缓存可以显著提高网站响应速度</li>
              )}
              {settings.enableCache && settings.cacheType === 'file' && (
                <li>• 考虑使用Redis缓存以获得更好的性能</li>
              )}
              {!settings.enableImageOptimization && (
                <li>• 启用图片优化可以减少页面加载时间</li>
              )}
              {!settings.enableGzipCompression && (
                <li>• 启用Gzip压缩可以减少带宽使用</li>
              )}
              {!settings.cdnUrl && (
                <li>• 配置CDN可以加速全球用户访问</li>
              )}
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
};

const AppearanceSettingsTab: React.FC<{
  settings: ISystemSettings['appearance'];
  onUpdate: (field: keyof ISystemSettings['appearance'], value: any) => void;
}> = ({ settings, onUpdate }) => {
  const [previewMode, setPreviewMode] = useState(false);

  const colorPresets = [
    { name: '经典蓝', primary: '#3b82f6', accent: '#10b981' },
    { name: '优雅紫', primary: '#8b5cf6', accent: '#f59e0b' },
    { name: '现代绿', primary: '#059669', accent: '#3b82f6' },
    { name: '暖橙色', primary: '#ea580c', accent: '#dc2626' },
    { name: '深邃黑', primary: '#1f2937', accent: '#6b7280' },
    { name: '玫瑰红', primary: '#e11d48', accent: '#f97316' },
  ];

  const fontOptions = [
    'Inter',
    'Roboto',
    'Open Sans',
    'Lato',
    'Montserrat',
    'Source Sans Pro',
    'Noto Sans',
    'System UI',
    '微软雅黑',
    'PingFang SC',
    'Helvetica Neue',
  ];

  const applyColorPreset = (preset: typeof colorPresets[0]) => {
    onUpdate('primaryColor', preset.primary);
    onUpdate('accentColor', preset.accent);
  };

  const getPreviewStyle = () => ({
    '--primary-color': settings.primaryColor,
    '--accent-color': settings.accentColor,
    fontFamily: settings.fontFamily,
    fontSize: `${settings.fontSize}px`,
  } as React.CSSProperties);

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">外观设置</h2>
        <p className="text-gray-600 dark:text-gray-400 mb-8">
          自定义网站的主题、颜色和布局样式。
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* 主题和颜色 */}
        <div className="space-y-6">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2">
            主题和颜色
          </h3>

          {/* 主题选择 */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">
              主题模式
            </label>
            <div className="grid grid-cols-3 gap-3">
              {[
                { value: 'light', label: '浅色', icon: '☀️' },
                { value: 'dark', label: '深色', icon: '🌙' },
                { value: 'auto', label: '跟随系统', icon: '🔄' }
              ].map((theme) => (
                <button
                  key={theme.value}
                  type="button"
                  onClick={() => onUpdate('theme', theme.value)}
                  className={cn(
                    'flex flex-col items-center justify-center p-4 border-2 rounded-lg transition-all',
                    settings.theme === theme.value
                      ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
                      : 'border-gray-200 dark:border-gray-600 hover:border-gray-300 dark:hover:border-gray-500'
                  )}
                >
                  <span className="text-2xl mb-2">{theme.icon}</span>
                  <span className="text-sm font-medium">{theme.label}</span>
                </button>
              ))}
            </div>
          </div>

          {/* 颜色设置 */}
          <div className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  主色调
                </label>
                <div className="flex items-center space-x-3">
                  <input
                    type="color"
                    value={settings.primaryColor}
                    onChange={(e) => onUpdate('primaryColor', e.target.value)}
                    className="w-12 h-10 border border-gray-300 dark:border-gray-600 rounded-md cursor-pointer"
                  />
                  <Input
                    value={settings.primaryColor}
                    onChange={(e) => onUpdate('primaryColor', e.target.value)}
                    placeholder="#3b82f6"
                    className="flex-1"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  强调色
                </label>
                <div className="flex items-center space-x-3">
                  <input
                    type="color"
                    value={settings.accentColor}
                    onChange={(e) => onUpdate('accentColor', e.target.value)}
                    className="w-12 h-10 border border-gray-300 dark:border-gray-600 rounded-md cursor-pointer"
                  />
                  <Input
                    value={settings.accentColor}
                    onChange={(e) => onUpdate('accentColor', e.target.value)}
                    placeholder="#10b981"
                    className="flex-1"
                  />
                </div>
              </div>
            </div>

            {/* 颜色预设 */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">
                颜色预设
              </label>
              <div className="grid grid-cols-2 md:grid-cols-3 gap-2">
                {colorPresets.map((preset, index) => (
                  <button
                    key={index}
                    type="button"
                    onClick={() => applyColorPreset(preset)}
                    className="flex items-center space-x-2 p-2 border border-gray-200 dark:border-gray-600 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
                  >
                    <div className="flex space-x-1">
                      <div
                        className="w-4 h-4 rounded-full border border-gray-300 dark:border-gray-600"
                        style={{ backgroundColor: preset.primary }}
                      />
                      <div
                        className="w-4 h-4 rounded-full border border-gray-300 dark:border-gray-600"
                        style={{ backgroundColor: preset.accent }}
                      />
                    </div>
                    <span className="text-xs font-medium">{preset.name}</span>
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* 字体设置 */}
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                字体系列
              </label>
              <select
                value={settings.fontFamily}
                onChange={(e) => onUpdate('fontFamily', e.target.value)}
                className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-blue-500 focus:border-blue-500"
                style={{ fontFamily: settings.fontFamily }}
              >
                {fontOptions.map((font) => (
                  <option key={font} value={font} style={{ fontFamily: font }}>
                    {font}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                基础字体大小: {settings.fontSize}px
              </label>
              <input
                type="range"
                min="10"
                max="24"
                step="1"
                value={settings.fontSize}
                onChange={(e) => onUpdate('fontSize', parseInt(e.target.value))}
                className="w-full h-2 bg-gray-200 dark:bg-gray-600 rounded-lg appearance-none cursor-pointer"
              />
              <div className="flex justify-between text-xs text-gray-500 mt-1">
                <span>小 (10px)</span>
                <span>中 (16px)</span>
                <span>大 (24px)</span>
              </div>
            </div>
          </div>
        </div>

        {/* 布局和自定义 */}
        <div className="space-y-6">
          {/* 布局设置 */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2 mb-4">
              布局设置
            </h3>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  头部样式
                </label>
                <select
                  value={settings.headerStyle}
                  onChange={(e) => onUpdate('headerStyle', e.target.value)}
                  className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-blue-500 focus:border-blue-500"
                >
                  <option value="minimal">简约式</option>
                  <option value="standard">标准式</option>
                  <option value="featured">特色式</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  侧边栏位置
                </label>
                <select
                  value={settings.sidebarPosition}
                  onChange={(e) => onUpdate('sidebarPosition', e.target.value)}
                  className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-blue-500 focus:border-blue-500"
                >
                  <option value="left">左侧</option>
                  <option value="right">右侧</option>
                  <option value="disabled">禁用</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  Logo位置
                </label>
                <select
                  value={settings.logoPosition}
                  onChange={(e) => onUpdate('logoPosition', e.target.value)}
                  className="block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-blue-500 focus:border-blue-500"
                >
                  <option value="header">仅头部</option>
                  <option value="sidebar">仅侧边栏</option>
                  <option value="both">头部和侧边栏</option>
                </select>
              </div>
            </div>
          </div>

          {/* 自定义代码 */}
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2 mb-4">
              自定义代码
            </h3>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  自定义CSS
                </label>
                <Textarea
                  value={settings.customCSS}
                  onChange={(e) => onUpdate('customCSS', e.target.value)}
                  placeholder="/* 在这里添加您的自定义CSS */
.custom-style {
  color: var(--primary-color);
}"
                  rows={8}
                  className="font-mono text-sm"
                  helperText="添加自定义CSS样式以个性化外观"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  自定义HTML (头部)
                </label>
                <Textarea
                  value={settings.customHTML}
                  onChange={(e) => onUpdate('customHTML', e.target.value)}
                  placeholder="<!-- 在这里添加自定义HTML，如统计代码 -->
<!-- 此内容将插入到<head>标签中 -->"
                  rows={6}
                  className="font-mono text-sm"
                  helperText="添加到网站头部的自定义HTML代码"
                />
              </div>
            </div>
          </div>

          {/* 预览模式切换 */}
          <div className="flex items-center justify-between p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
            <div>
              <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                预览模式
              </label>
              <p className="text-sm text-gray-500">实时预览外观更改效果</p>
            </div>
            <button
              type="button"
              onClick={() => setPreviewMode(!previewMode)}
              className={cn(
                'relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                previewMode ? 'bg-blue-600' : 'bg-gray-200 dark:bg-gray-600'
              )}
            >
              <span
                className={cn(
                  'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                  previewMode ? 'translate-x-6' : 'translate-x-1'
                )}
              />
            </button>
          </div>
        </div>
      </div>

      {/* 样式预览 */}
      {previewMode && (
        <div className="mt-8 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">样式预览</h3>
          
          <div style={getPreviewStyle()} className="space-y-4">
            {/* 头部预览 */}
            <div
              className="p-4 rounded-lg border"
              style={{
                backgroundColor: settings.primaryColor + '10',
                borderColor: settings.primaryColor + '30',
              }}
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3">
                  <div
                    className="w-8 h-8 rounded"
                    style={{ backgroundColor: settings.primaryColor }}
                  />
                  <h4 className="font-semibold" style={{ color: settings.primaryColor }}>
                    Maple Blog
                  </h4>
                </div>
                <nav className="flex space-x-4">
                  <a href="#" className="hover:underline">首页</a>
                  <a href="#" className="hover:underline">文章</a>
                  <a href="#" className="hover:underline">关于</a>
                </nav>
              </div>
            </div>

            {/* 内容预览 */}
            <div className="space-y-3">
              <h2 className="text-xl font-bold text-gray-900 dark:text-white">
                示例文章标题
              </h2>
              <p className="text-gray-600 dark:text-gray-400">
                这是一段示例文本，用于展示当前字体和字号设置的效果。
                您可以在上面调整各种外观设置，并在此预览区域查看实时效果。
              </p>
              <button
                className="px-4 py-2 rounded-md text-white"
                style={{ backgroundColor: settings.accentColor }}
              >
                强调按钮
              </button>
            </div>

            {/* 卡片预览 */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="border border-gray-200 dark:border-gray-700 rounded-lg p-4">
                <h3 className="font-medium text-gray-900 dark:text-white mb-2">卡片标题</h3>
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  卡片内容示例，展示边框、间距和文字效果。
                </p>
                <div className="mt-3 flex items-center space-x-2">
                  <span
                    className="inline-block w-2 h-2 rounded-full"
                    style={{ backgroundColor: settings.primaryColor }}
                  />
                  <span className="text-xs text-gray-500">标签示例</span>
                </div>
              </div>
              
              <div className="border border-gray-200 dark:border-gray-700 rounded-lg p-4">
                <h3 className="font-medium text-gray-900 dark:text-white mb-2">另一个卡片</h3>
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  更多内容展示当前的主题配置效果。
                </p>
                <div className="mt-3">
                  <span
                    className="text-xs px-2 py-1 rounded"
                    style={{
                      backgroundColor: settings.accentColor + '20',
                      color: settings.accentColor,
                    }}
                  >
                    彩色标签
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default SystemSettings;
// @ts-nocheck
import React, { useState, useCallback, useEffect, useRef, useMemo } from 'react';
import {
  // Card,
  Button,
  Space,
  Typography,
  Input,
  Select,
  Form,
  Row,
  Col,
  Tabs,
  Divider,
  Switch,
  // InputNumber,
  Upload,
  Image,
  Tag,
  // Modal,
  message,
  Tooltip,
  Progress,
  Alert,
  Badge,
  Drawer,
  Collapse,
  Slider,
  ColorPicker,
  DatePicker,
  // TimePicker,
  List,
  Avatar,
  Popover,
  // Checkbox,
  Radio,
  Statistic,
  // AutoComplete
} from 'antd';
import {
  BoldOutlined,
  ItalicOutlined,
  UnderlineOutlined,
  StrikethroughOutlined,
  AlignLeftOutlined,
  AlignCenterOutlined,
  AlignRightOutlined,
  OrderedListOutlined,
  UnorderedListOutlined,
  LinkOutlined,
  PictureOutlined,
  VideoCameraOutlined,
  CodeOutlined,
  TableOutlined,
  // FontSizeOutlined,
  FontColorsOutlined,
  // BgColorsOutlined,
  UndoOutlined,
  RedoOutlined,
  EyeOutlined,
  SaveOutlined,
  SendOutlined,
  FullscreenOutlined,
  FullscreenExitOutlined,
  SettingOutlined,
  HistoryOutlined,
  // CopyOutlined,
  // ScissorOutlined,
  // FormatPainterOutlined,
  DeleteOutlined,
  PlusOutlined,
  // EditOutlined,
  QuestionCircleOutlined,
  ThunderboltOutlined,
  // SearchOutlined,
  FileTextOutlined,
  TagsOutlined,
  // CalendarOutlined,
  // UserOutlined,
  // GlobalOutlined,
  EyeInvisibleOutlined,
  ClockCircleOutlined,
  CheckCircleOutlined,
  ExclamationCircleOutlined,
  InfoCircleOutlined,
  WarningOutlined
} from '@ant-design/icons';
import dayjs, { Dayjs } from 'dayjs';

const { Title, Text, Paragraph } = Typography;
const { TextArea } = Input;
const { Option } = Select;
const { TabPane } = Tabs;
const { Panel } = Collapse;

// Content editor interfaces
interface EditorContent {
  title: string;
  excerpt: string;
  content: string;
  categoryId: number | null;
  tags: string[];
  featuredImage: string | null;
  isPublished: boolean;
  publishDate: Dayjs | null;
  seoTitle: string;
  seoDescription: string;
  seoKeywords: string[];
  customUrl: string;
  allowComments: boolean;
  isFeatured: boolean;
  readingTime: number;
  wordCount: number;
}

interface EditorSettings {
  autoSave: boolean;
  autoSaveInterval: number;
  spellCheck: boolean;
  wordWrap: boolean;
  lineNumbers: boolean;
  fontSize: number;
  theme: 'light' | 'dark';
  showPreview: boolean;
  previewMode: 'side' | 'tab';
}

interface ContentVersion {
  id: number;
  content: string;
  timestamp: string;
  changes: string;
  wordCount: number;
}

interface SEOAnalysis {
  score: number;
  issues: Array<{
    type: 'error' | 'warning' | 'info';
    message: string;
    suggestion: string;
  }>;
  keywords: Array<{
    keyword: string;
    density: number;
    count: number;
  }>;
  readability: {
    score: number;
    level: string;
    suggestions: string[];
  };
}

// Mock data
const mockCategories = [
  { id: 1, name: '前端技术', color: '#1677ff' },
  { id: 2, name: '后端技术', color: '#52c41a' },
  { id: 3, name: '数据库', color: '#722ed1' },
  { id: 4, name: '系统架构', color: '#fa8c16' }
];

const mockTags = [
  'React', 'Vue.js', 'TypeScript', 'JavaScript', 'Node.js',
  '.NET', 'Python', 'Java', 'Docker', 'Kubernetes',
  'MongoDB', 'PostgreSQL', 'Redis', 'MySQL', 'SQLite'
];

const mockVersions: ContentVersion[] = [
  {
    id: 1,
    content: '初始版本的内容...',
    timestamp: '2024-01-20T10:30:00Z',
    changes: '创建文档',
    wordCount: 150
  },
  {
    id: 2,
    content: '修改后的内容...',
    timestamp: '2024-01-20T11:15:00Z',
    changes: '添加了技术细节和示例代码',
    wordCount: 350
  },
  {
    id: 3,
    content: '最新版本的内容...',
    timestamp: '2024-01-20T14:22:00Z',
    changes: '优化了文章结构，增加了图片',
    wordCount: 520
  }
];

interface ContentEditorProps {
  initialContent?: Partial<EditorContent>;
  onSave?: (content: EditorContent) => void;
  onPublish?: (content: EditorContent) => void;
  onPreview?: (content: EditorContent) => void;
  autosave?: boolean;
  readOnly?: boolean;
}

const ContentEditor: React.FC<ContentEditorProps> = ({
  initialContent,
  onSave,
  onPublish,
  onPreview,
  autosave = true,
  readOnly = false
}) => {
  const [form] = Form.useForm();
  const editorRef = useRef<HTMLTextAreaElement>(null);
  const [content, setContent] = useState<EditorContent>({
    title: '',
    excerpt: '',
    content: '',
    categoryId: null,
    tags: [],
    featuredImage: null,
    isPublished: false,
    publishDate: null,
    seoTitle: '',
    seoDescription: '',
    seoKeywords: [],
    customUrl: '',
    allowComments: true,
    isFeatured: false,
    readingTime: 0,
    wordCount: 0,
    ...initialContent
  });

  const [settings, setSettings] = useState<EditorSettings>({
    autoSave: true,
    autoSaveInterval: 30,
    spellCheck: true,
    wordWrap: true,
    lineNumbers: false,
    fontSize: 14,
    theme: 'light',
    showPreview: false,
    previewMode: 'side'
  });

  const [editorMode, setEditorMode] = useState<'edit' | 'preview' | 'split'>('edit');
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [versions, setVersions] = useState<ContentVersion[]>(mockVersions);
  const [versionDrawerVisible, setVersionDrawerVisible] = useState(false);
  const [settingsDrawerVisible, setSettingsDrawerVisible] = useState(false);
  const [seoDrawerVisible, setSeoDrawerVisible] = useState(false);
  const [mediaDrawerVisible, setMediaDrawerVisible] = useState(false);
  const [saving, setSaving] = useState(false);
  const [lastSaved, setLastSaved] = useState<Dayjs | null>(null);
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);

  // Calculate word count and reading time
  const wordStats = useMemo(() => {
    const text = content.content.replace(/<[^>]*>/g, ''); // Remove HTML tags
    const words = text.trim().split(/\s+/).filter(word => word.length > 0);
    const wordCount = words.length;
    const readingTime = Math.ceil(wordCount / 200); // Assuming 200 words per minute

    return { wordCount, readingTime };
  }, [content.content]);

  // SEO Analysis
  const seoAnalysis = useMemo((): SEOAnalysis => {
    const text = content.content.replace(/<[^>]*>/g, '').toLowerCase();
    const words = text.split(/\s+/).filter(word => word.length > 0);
    const totalWords = words.length;

    // Calculate keyword density
    const keywordCounts: { [key: string]: number } = {};
    content.seoKeywords.forEach(keyword => {
      const count = (text.match(new RegExp(keyword.toLowerCase(), 'g')) || []).length;
      keywordCounts[keyword] = count;
    });

    const keywords = content.seoKeywords.map(keyword => ({
      keyword,
      count: keywordCounts[keyword] || 0,
      density: totalWords > 0 ? ((keywordCounts[keyword] || 0) / totalWords) * 100 : 0
    }));

    // SEO Issues
    const issues: SEOAnalysis['issues'] = [];

    if (!content.title || content.title.length < 10) {
      issues.push({
        type: 'error',
        message: '标题过短',
        suggestion: '建议标题长度在10-60个字符之间'
      });
    }

    if (content.title && content.title.length > 60) {
      issues.push({
        type: 'warning',
        message: '标题过长',
        suggestion: '标题过长可能在搜索结果中被截断'
      });
    }

    if (!content.seoDescription || content.seoDescription.length < 50) {
      issues.push({
        type: 'warning',
        message: 'SEO描述过短',
        suggestion: '建议SEO描述长度在50-160个字符之间'
      });
    }

    if (content.seoKeywords.length === 0) {
      issues.push({
        type: 'info',
        message: '未设置关键词',
        suggestion: '添加3-5个相关关键词有助于SEO优化'
      });
    }

    if (!content.featuredImage) {
      issues.push({
        type: 'info',
        message: '未设置特色图片',
        suggestion: '特色图片有助于社交媒体分享和SEO'
      });
    }

    // Calculate overall score
    let score = 100;
    issues.forEach(issue => {
      if (issue.type === 'error') score -= 20;
      else if (issue.type === 'warning') score -= 10;
      else score -= 5;
    });

    return {
      score: Math.max(0, score),
      issues,
      keywords,
      readability: {
        score: 75,
        level: '适中',
        suggestions: ['考虑使用更多的子标题', '适当缩短段落长度']
      }
    };
  }, [content]);

  // Auto-save functionality
  useEffect(() => {
    if (!autosave || !settings.autoSave || readOnly) return;

    const interval = setInterval(() => {
      if (hasUnsavedChanges) {
        handleSave(true);
      }
    }, settings.autoSaveInterval * 1000);

    return () => clearInterval(interval);
  }, [hasUnsavedChanges, settings.autoSave, settings.autoSaveInterval, autosave, readOnly]);

  // Update content and mark as changed
  const updateContent = useCallback((updates: Partial<EditorContent>) => {
    setContent(prev => ({ ...prev, ...updates }));
    setHasUnsavedChanges(true);
  }, []);

  // Handle save
  const handleSave = useCallback(async (isAutoSave = false) => {
    if (readOnly) return;

    setSaving(true);
    try {
      const updatedContent = {
        ...content,
        wordCount: wordStats.wordCount,
        readingTime: wordStats.readingTime
      };

      await new Promise(resolve => setTimeout(resolve, 500)); // Simulate API call

      if (onSave) {
        onSave(updatedContent);
      }

      setLastSaved(dayjs());
      setHasUnsavedChanges(false);

      if (!isAutoSave) {
        message.success('内容已保存');
      }

      // Add to version history
      const newVersion: ContentVersion = {
        id: Date.now(),
        content: content.content,
        timestamp: new Date().toISOString(),
        changes: isAutoSave ? '自动保存' : '手动保存',
        wordCount: wordStats.wordCount
      };
      setVersions(prev => [newVersion, ...prev.slice(0, 9)]); // Keep last 10 versions

    } catch (error) {
      message.error('保存失败');
    } finally {
      setSaving(false);
    }
  }, [content, wordStats, onSave, readOnly]);

  // Handle publish
  const handlePublish = useCallback(async () => {
    if (readOnly) return;

    try {
      await form.validateFields();

      const updatedContent = {
        ...content,
        isPublished: true,
        publishDate: content.publishDate || dayjs(),
        wordCount: wordStats.wordCount,
        readingTime: wordStats.readingTime
      };

      if (onPublish) {
        onPublish(updatedContent);
      }

      message.success('文章已发布');
    } catch (error) {
      message.error('发布失败，请检查必填字段');
    }
  }, [content, wordStats, onPublish, form, readOnly]);

  // Handle preview
  const handlePreview = useCallback(() => {
    if (onPreview) {
      onPreview(content);
    } else {
      setEditorMode(editorMode === 'preview' ? 'edit' : 'preview');
    }
  }, [content, onPreview, editorMode]);

  // Toolbar component
  const EditorToolbar: React.FC = () => (
    <div className="border-b p-2 bg-white">
      <div className="flex justify-between items-center">
        <Space wrap>
          {/* Format buttons */}
          <Space.Compact>
            <Tooltip title="粗体 (Ctrl+B)">
              <Button  icon={<BoldOutlined />} />
            </Tooltip>
            <Tooltip title="斜体 (Ctrl+I)">
              <Button  icon={<ItalicOutlined />} />
            </Tooltip>
            <Tooltip title="下划线 (Ctrl+U)">
              <Button  icon={<UnderlineOutlined />} />
            </Tooltip>
            <Tooltip title="删除线">
              <Button  icon={<StrikethroughOutlined />} />
            </Tooltip>
          </Space.Compact>

          <Divider type="vertical" />

          {/* Alignment buttons */}
          <Space.Compact>
            <Tooltip title="左对齐">
              <Button  icon={<AlignLeftOutlined />} />
            </Tooltip>
            <Tooltip title="居中">
              <Button  icon={<AlignCenterOutlined />} />
            </Tooltip>
            <Tooltip title="右对齐">
              <Button  icon={<AlignRightOutlined />} />
            </Tooltip>
          </Space.Compact>

          <Divider type="vertical" />

          {/* List buttons */}
          <Space.Compact>
            <Tooltip title="有序列表">
              <Button  icon={<OrderedListOutlined />} />
            </Tooltip>
            <Tooltip title="无序列表">
              <Button  icon={<UnorderedListOutlined />} />
            </Tooltip>
          </Space.Compact>

          <Divider type="vertical" />

          {/* Insert buttons */}
          <Space.Compact>
            <Tooltip title="插入链接">
              <Button  icon={<LinkOutlined />} />
            </Tooltip>
            <Tooltip title="插入图片">
              <Button  icon={<PictureOutlined />} onClick={() => setMediaDrawerVisible(true)} />
            </Tooltip>
            <Tooltip title="插入视频">
              <Button  icon={<VideoCameraOutlined />} />
            </Tooltip>
            <Tooltip title="插入代码">
              <Button  icon={<CodeOutlined />} />
            </Tooltip>
            <Tooltip title="插入表格">
              <Button  icon={<TableOutlined />} />
            </Tooltip>
          </Space.Compact>

          <Divider type="vertical" />

          {/* Style buttons */}
          <Popover
            content={
              <div className="space-y-2">
                <div>
                  <Text strong>字体大小</Text>
                  <Slider
                    min={12}
                    max={24}
                    value={settings.fontSize}
                    onChange={value => setSettings(prev => ({ ...prev, fontSize: value }))}
                  />
                </div>
                <div>
                  <Text strong>字体颜色</Text>
                  <ColorPicker />
                </div>
                <div>
                  <Text strong>背景颜色</Text>
                  <ColorPicker />
                </div>
              </div>
            }
            trigger="click"
            title="文字样式"
          >
            <Button  icon={<FontColorsOutlined />} />
          </Popover>

          <Divider type="vertical" />

          {/* History buttons */}
          <Space.Compact>
            <Tooltip title="撤销 (Ctrl+Z)">
              <Button  icon={<UndoOutlined />} />
            </Tooltip>
            <Tooltip title="重做 (Ctrl+Y)">
              <Button  icon={<RedoOutlined />} />
            </Tooltip>
          </Space.Compact>
        </Space>

        <Space>
          {/* View mode toggle */}
          <Radio.Group
            
            value={editorMode}
            onChange={e => setEditorMode(e.target.value)}
          >
            <Radio.Button value="edit">编辑</Radio.Button>
            <Radio.Button value="split">分屏</Radio.Button>
            <Radio.Button value="preview">预览</Radio.Button>
          </Radio.Group>

          <Tooltip title="版本历史">
            <Button
              
              icon={<HistoryOutlined />}
              onClick={() => setVersionDrawerVisible(true)}
            />
          </Tooltip>

          <Tooltip title="编辑器设置">
            <Button
              
              icon={<SettingOutlined />}
              onClick={() => setSettingsDrawerVisible(true)}
            />
          </Tooltip>

          <Tooltip title={isFullscreen ? '退出全屏' : '全屏编辑'}>
            <Button
              
              icon={isFullscreen ? <FullscreenExitOutlined /> : <FullscreenOutlined />}
              onClick={() => setIsFullscreen(!isFullscreen)}
            />
          </Tooltip>
        </Space>
      </div>
    </div>
  );

  // Preview component
  const ContentPreview: React.FC = () => (
    <div className="p-6 bg-white">
      <article className="prose max-w-none">
        <header className="mb-6">
          <Title level={1}>{content.title || '请输入标题'}</Title>
          {content.excerpt && (
            <Paragraph className="text-lg text-gray-600 italic">
              {content.excerpt}
            </Paragraph>
          )}
          <div className="flex items-center gap-4 text-sm text-gray-500 mt-4">
            <span>预计阅读时间: {wordStats.readingTime} 分钟</span>
            <span>字数: {wordStats.wordCount}</span>
            {content.tags.length > 0 && (
              <Space>
                标签:
                {content.tags.map(tag => (
                  <Tag key={tag} >{tag}</Tag>
                ))}
              </Space>
            )}
          </div>
        </header>

        {content.featuredImage && (
          <div className="mb-6">
            <Image
              src={content.featuredImage}
              alt={content.title}
              className="w-full rounded-lg"
            />
          </div>
        )}

        <div
          className="content"
          dangerouslySetInnerHTML={{
            __html: content.content || '<p>开始写作吧...</p>'
          }}
        />
      </article>
    </div>
  );

  return (
    <div className={`content-editor ${isFullscreen ? 'fixed inset-0 z-50 bg-white' : ''}`}>
      <Form
        form={form}
        layout="vertical"
        initialValues={content}
        onValuesChange={(_, values) => updateContent(values)}
      >
        {/* Header */}
        <div className="border-b bg-white p-4">
          <div className="flex justify-between items-center">
            <div className="flex items-center gap-4">
              <Title level={4} className="mb-0">
                {content.title || '新建文章'}
              </Title>
              {hasUnsavedChanges && (
                <Badge status="warning" text="未保存" />
              )}
              {lastSaved && (
                <Text type="secondary" className="text-sm">
                  最后保存: {lastSaved.format('HH:mm:ss')}
                </Text>
              )}
            </div>

            <Space>
              {/* Status indicator */}
              <Tag color={content.isPublished ? 'green' : 'orange'}>
                {content.isPublished ? '已发布' : '草稿'}
              </Tag>

              {/* Action buttons */}
              <Button
                icon={<SaveOutlined />}
                loading={saving}
                onClick={() => handleSave()}
                disabled={readOnly}
              >
                保存
              </Button>

              <Button
                icon={<EyeOutlined />}
                onClick={handlePreview}
              >
                预览
              </Button>

              <Button
                type="primary"
                icon={<SendOutlined />}
                onClick={handlePublish}
                disabled={readOnly}
              >
                {content.isPublished ? '更新' : '发布'}
              </Button>
            </Space>
          </div>
        </div>

        <div className="flex flex-1 overflow-hidden">
          {/* Sidebar - Article settings */}
          <div className="w-80 border-r bg-gray-50 p-4 overflow-y-auto">
            <Tabs defaultActiveKey="basic" >
              <TabPane tab="基本设置" key="basic">
                <div className="space-y-4">
                  <Form.Item
                    label="文章标题"
                    name="title"
                    rules={[{ required: true, message: '请输入文章标题' }]}
                  >
                    <Input
                      placeholder="请输入文章标题"
                      showCount
                      maxLength={100}
                      disabled={readOnly}
                    />
                  </Form.Item>

                  <Form.Item
                    label="文章摘要"
                    name="excerpt"
                  >
                    <TextArea
                      placeholder="请输入文章摘要"
                      showCount
                      maxLength={200}
                      rows={3}
                      disabled={readOnly}
                    />
                  </Form.Item>

                  <Form.Item
                    label="分类"
                    name="categoryId"
                    rules={[{ required: true, message: '请选择文章分类' }]}
                  >
                    <Select
                      placeholder="选择分类"
                      disabled={readOnly}
                    >
                      {mockCategories.map(category => (
                        <Option key={category.id} value={category.id}>
                          <div className="flex items-center gap-2">
                            <div
                              className="w-3 h-3 rounded-full"
                              style={{ backgroundColor: category.color }}
                            />
                            {category.name}
                          </div>
                        </Option>
                      ))}
                    </Select>
                  </Form.Item>

                  <Form.Item
                    label="标签"
                    name="tags"
                  >
                    <Select
                      mode="tags"
                      placeholder="添加标签"
                      tokenSeparators={[',']}
                      disabled={readOnly}
                    >
                      {mockTags.map(tag => (
                        <Option key={tag} value={tag}>{tag}</Option>
                      ))}
                    </Select>
                  </Form.Item>

                  <Form.Item
                    label="特色图片"
                    name="featuredImage"
                  >
                    <div className="space-y-2">
                      {content.featuredImage ? (
                        <div className="relative">
                          <Image
                            src={content.featuredImage}
                            alt="特色图片"
                            className="w-full rounded"
                            height={120}
                          />
                          {!readOnly && (
                            <Button
                              
                              danger
                              icon={<DeleteOutlined />}
                              className="absolute top-2 right-2"
                              onClick={() => updateContent({ featuredImage: null })}
                            />
                          )}
                        </div>
                      ) : (
                        <Upload
                          listType="picture-card"
                          showUploadList={false}
                          beforeUpload={() => false}
                          onChange={(info) => {
                            if (info.file) {
                              const url = URL.createObjectURL(info.file as any);
                              updateContent({ featuredImage: url });
                            }
                          }}
                          disabled={readOnly}
                        >
                          <div>
                            <PlusOutlined />
                            <div style={{ marginTop: 8 }}>上传图片</div>
                          </div>
                        </Upload>
                      )}
                    </div>
                  </Form.Item>

                  <Divider />

                  <div className="space-y-3">
                    <Form.Item
                      label="允许评论"
                      name="allowComments"
                      valuePropName="checked"
                    >
                      <Switch disabled={readOnly} />
                    </Form.Item>

                    <Form.Item
                      label="设为精选"
                      name="isFeatured"
                      valuePropName="checked"
                    >
                      <Switch disabled={readOnly} />
                    </Form.Item>

                    <Form.Item
                      label="发布时间"
                      name="publishDate"
                    >
                      <DatePicker
                        showTime
                        placeholder="选择发布时间"
                        style={{ width: '100%' }}
                        disabled={readOnly}
                      />
                    </Form.Item>
                  </div>
                </div>
              </TabPane>

              <TabPane tab="SEO优化" key="seo">
                <div className="space-y-4">
                  {/* SEO Score */}
                  <div className="text-center">
                    <Progress
                      type="circle"
                      percent={seoAnalysis.score}
                      size={80}
                      strokeColor={
                        seoAnalysis.score >= 80 ? '#52c41a' :
                        seoAnalysis.score >= 60 ? '#faad14' : '#ff4d4f'
                      }
                    />
                    <div className="mt-2">
                      <Text strong>SEO 得分</Text>
                    </div>
                  </div>

                  <Divider />

                  <Form.Item
                    label="SEO 标题"
                    name="seoTitle"
                    extra="建议长度: 10-60个字符"
                  >
                    <Input
                      placeholder="输入SEO标题"
                      showCount
                      maxLength={60}
                      disabled={readOnly}
                    />
                  </Form.Item>

                  <Form.Item
                    label="SEO 描述"
                    name="seoDescription"
                    extra="建议长度: 50-160个字符"
                  >
                    <TextArea
                      placeholder="输入SEO描述"
                      showCount
                      maxLength={160}
                      rows={3}
                      disabled={readOnly}
                    />
                  </Form.Item>

                  <Form.Item
                    label="关键词"
                    name="seoKeywords"
                    extra="建议3-5个关键词"
                  >
                    <Select
                      mode="tags"
                      placeholder="添加关键词"
                      tokenSeparators={[',']}
                      disabled={readOnly}
                    />
                  </Form.Item>

                  <Form.Item
                    label="自定义URL"
                    name="customUrl"
                  >
                    <Input
                      placeholder="自定义URL路径"
                      addonBefore="/posts/"
                      disabled={readOnly}
                    />
                  </Form.Item>

                  <Divider />

                  {/* SEO Issues */}
                  <div>
                    <Text strong>SEO 建议</Text>
                    <div className="mt-2 space-y-2">
                      {seoAnalysis.issues.map((issue, index) => (
                        <Alert
                          key={index}
                          message={issue.message}
                          description={issue.suggestion}
                          type={issue.type === 'error' ? 'error' : issue.type === 'warning' ? 'warning' : 'info'}
                          
                          showIcon
                        />
                      ))}
                    </div>
                  </div>

                  {/* Keyword Analysis */}
                  {seoAnalysis.keywords.length > 0 && (
                    <div>
                      <Text strong>关键词密度</Text>
                      <div className="mt-2">
                        {seoAnalysis.keywords.map((keyword, index) => (
                          <div key={index} className="flex justify-between items-center mb-1">
                            <span>{keyword.keyword}</span>
                            <div className="flex items-center gap-2">
                              <span className="text-xs">{keyword.density.toFixed(1)}%</span>
                              <Progress
                                percent={Math.min(keyword.density * 20, 100)}
                                
                                showInfo={false}
                                strokeColor={
                                  keyword.density >= 2 && keyword.density <= 5 ? '#52c41a' : '#faad14'
                                }
                              />
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  <Button
                    block
                    icon={<ThunderboltOutlined />}
                    onClick={() => setSeoDrawerVisible(true)}
                  >
                    详细SEO分析
                  </Button>
                </div>
              </TabPane>

              <TabPane tab="统计信息" key="stats">
                <div className="space-y-4">
                  <Row gutter={16}>
                    <Col span={12}>
                      <Statistic
                        title="字数"
                        value={wordStats.wordCount}
                        prefix={<FileTextOutlined />}
                      />
                    </Col>
                    <Col span={12}>
                      <Statistic
                        title="阅读时间"
                        value={wordStats.readingTime}
                        suffix="分钟"
                        prefix={<ClockCircleOutlined />}
                      />
                    </Col>
                  </Row>

                  <Divider />

                  <div>
                    <Text strong>编辑历史</Text>
                    <div className="mt-2 space-y-2">
                      {versions.slice(0, 3).map(version => (
                        <div key={version.id} className="text-sm">
                          <div className="flex justify-between">
                            <span className="text-gray-600">{version.changes}</span>
                            <span className="text-gray-400">
                              {dayjs(version.timestamp).format('HH:mm')}
                            </span>
                          </div>
                          <div className="text-xs text-gray-400">
                            {version.wordCount} 字
                          </div>
                        </div>
                      ))}
                    </div>
                    <Button
                      type="link"
                      
                      className="p-0 mt-2"
                      onClick={() => setVersionDrawerVisible(true)}
                    >
                      查看完整历史
                    </Button>
                  </div>

                  <Divider />

                  <div>
                    <Text strong>可读性分析</Text>
                    <div className="mt-2">
                      <Progress
                        percent={seoAnalysis.readability.score}
                        strokeColor="#722ed1"
                      />
                      <div className="mt-1 text-sm text-gray-600">
                        等级: {seoAnalysis.readability.level}
                      </div>
                      <div className="mt-2 space-y-1">
                        {seoAnalysis.readability.suggestions.map((suggestion, index) => (
                          <div key={index} className="text-xs text-gray-500">
                            • {suggestion}
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              </TabPane>
            </Tabs>
          </div>

          {/* Main editor area */}
          <div className="flex-1 flex flex-col">
            <EditorToolbar />

            <div className="flex-1 flex overflow-hidden">
              {/* Editor */}
              {(editorMode === 'edit' || editorMode === 'split') && (
                <div className={`bg-white overflow-hidden ${editorMode === 'split' ? 'w-1/2 border-r' : 'w-full'}`}>
                  <Form.Item
                    name="content"
                    className="h-full mb-0"
                  >
                    <TextArea
                      ref={editorRef}
                      placeholder="开始写作..."
                      className="h-full resize-none border-0 focus:shadow-none"
                      style={{
                        fontSize: settings.fontSize,
                        lineHeight: 1.6,
                        fontFamily: 'Monaco, Consolas, "Courier New", monospace'
                      }}
                      disabled={readOnly}
                    />
                  </Form.Item>
                </div>
              )}

              {/* Preview */}
              {(editorMode === 'preview' || editorMode === 'split') && (
                <div className={`overflow-y-auto ${editorMode === 'split' ? 'w-1/2' : 'w-full'}`}>
                  <ContentPreview />
                </div>
              )}
            </div>

            {/* Footer status bar */}
            <div className="border-t bg-gray-50 px-4 py-2 text-sm text-gray-600">
              <div className="flex justify-between items-center">
                <div className="flex items-center gap-4">
                  <span>字数: {wordStats.wordCount}</span>
                  <span>阅读时间: {wordStats.readingTime} 分钟</span>
                  <span>SEO得分: {seoAnalysis.score}</span>
                  {hasUnsavedChanges && (
                    <Badge status="processing" text="自动保存中..." />
                  )}
                </div>
                <div className="flex items-center gap-2">
                  {lastSaved && (
                    <span>最后保存: {lastSaved.format('HH:mm:ss')}</span>
                  )}
                  <Button
                    type="link"
                    
                    icon={<QuestionCircleOutlined />}
                    onClick={() => message.info('快捷键帮助')}
                  >
                    帮助
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </Form>

      {/* Version History Drawer */}
      <Drawer
        title="版本历史"
        placement="right"
        width={400}
        onClose={() => setVersionDrawerVisible(false)}
        open={versionDrawerVisible}
      >
        <List
          dataSource={versions}
          renderItem={(version, index) => (
            <List.Item
              actions={[
                <Button
                  type="link"
                  
                  onClick={() => {
                    updateContent({ content: version.content });
                    setVersionDrawerVisible(false);
                    message.success('已恢复到此版本');
                  }}
                >
                  恢复
                </Button>
              ]}
            >
              <List.Item.Meta
                avatar={
                  <Avatar  className={index === 0 ? 'bg-green-500' : 'bg-gray-500'}>
                    {index + 1}
                  </Avatar>
                }
                title={
                  <div className="flex justify-between items-center">
                    <span>{version.changes}</span>
                    {index === 0 && <Badge status="success" text="当前" />}
                  </div>
                }
                description={
                  <div>
                    <div>{dayjs(version.timestamp).format('YYYY-MM-DD HH:mm:ss')}</div>
                    <div className="text-xs">字数: {version.wordCount}</div>
                  </div>
                }
              />
            </List.Item>
          )}
        />
      </Drawer>

      {/* Settings Drawer */}
      <Drawer
        title="编辑器设置"
        placement="right"
        width={350}
        onClose={() => setSettingsDrawerVisible(false)}
        open={settingsDrawerVisible}
      >
        <div className="space-y-6">
          <div>
            <Title level={5}>自动保存</Title>
            <div className="space-y-3">
              <div className="flex justify-between items-center">
                <Text>启用自动保存</Text>
                <Switch
                  checked={settings.autoSave}
                  onChange={checked => setSettings(prev => ({ ...prev, autoSave: checked }))}
                />
              </div>
              <div>
                <Text>保存间隔 (秒)</Text>
                <Slider
                  min={10}
                  max={300}
                  value={settings.autoSaveInterval}
                  onChange={value => setSettings(prev => ({ ...prev, autoSaveInterval: value }))}
                  marks={{ 10: '10s', 60: '1m', 180: '3m', 300: '5m' }}
                />
              </div>
            </div>
          </div>

          <Divider />

          <div>
            <Title level={5}>编辑器外观</Title>
            <div className="space-y-3">
              <div>
                <Text>字体大小</Text>
                <Slider
                  min={12}
                  max={24}
                  value={settings.fontSize}
                  onChange={value => setSettings(prev => ({ ...prev, fontSize: value }))}
                  marks={{ 12: '12', 16: '16', 20: '20', 24: '24' }}
                />
              </div>
              <div className="flex justify-between items-center">
                <Text>主题</Text>
                <Select
                  value={settings.theme}
                  onChange={value => setSettings(prev => ({ ...prev, theme: value }))}
                  style={{ width: 100 }}
                >
                  <Option value="light">浅色</Option>
                  <Option value="dark">深色</Option>
                </Select>
              </div>
            </div>
          </div>

          <Divider />

          <div>
            <Title level={5}>编辑功能</Title>
            <div className="space-y-3">
              <div className="flex justify-between items-center">
                <Text>拼写检查</Text>
                <Switch
                  checked={settings.spellCheck}
                  onChange={checked => setSettings(prev => ({ ...prev, spellCheck: checked }))}
                />
              </div>
              <div className="flex justify-between items-center">
                <Text>自动换行</Text>
                <Switch
                  checked={settings.wordWrap}
                  onChange={checked => setSettings(prev => ({ ...prev, wordWrap: checked }))}
                />
              </div>
              <div className="flex justify-between items-center">
                <Text>显示行号</Text>
                <Switch
                  checked={settings.lineNumbers}
                  onChange={checked => setSettings(prev => ({ ...prev, lineNumbers: checked }))}
                />
              </div>
            </div>
          </div>
        </div>
      </Drawer>

      {/* SEO Analysis Drawer */}
      <Drawer
        title="SEO详细分析"
        placement="right"
        width={500}
        onClose={() => setSeoDrawerVisible(false)}
        open={seoDrawerVisible}
      >
        <div className="space-y-6">
          {/* Overall Score */}
          <div className="text-center">
            <Progress
              type="circle"
              percent={seoAnalysis.score}
              size={120}
              strokeColor={
                seoAnalysis.score >= 80 ? '#52c41a' :
                seoAnalysis.score >= 60 ? '#faad14' : '#ff4d4f'
              }
            />
            <div className="mt-4">
              <Title level={4}>SEO 总得分</Title>
              <Text type="secondary">
                {seoAnalysis.score >= 80 ? '优秀' :
                 seoAnalysis.score >= 60 ? '良好' : '需要改进'}
              </Text>
            </div>
          </div>

          <Divider />

          {/* Issues */}
          <div>
            <Title level={5}>
              <ExclamationCircleOutlined className="mr-2" />
              待优化项目
            </Title>
            <div className="space-y-3">
              {seoAnalysis.issues.map((issue, index) => (
                <Alert
                  key={index}
                  message={issue.message}
                  description={issue.suggestion}
                  type={issue.type === 'error' ? 'error' : issue.type === 'warning' ? 'warning' : 'info'}
                  showIcon
                  action={
                    <Button  type="text">
                      修复
                    </Button>
                  }
                />
              ))}
            </div>
          </div>

          <Divider />

          {/* Keywords */}
          <div>
            <Title level={5}>
              <TagsOutlined className="mr-2" />
              关键词分析
            </Title>
            <div className="space-y-3">
              {seoAnalysis.keywords.map((keyword, index) => (
                <div key={index} className="border rounded p-3">
                  <div className="flex justify-between items-center mb-2">
                    <Text strong>{keyword.keyword}</Text>
                    <Tag color={
                      keyword.density >= 2 && keyword.density <= 5 ? 'green' :
                      keyword.density > 5 ? 'red' : 'orange'
                    }>
                      {keyword.density.toFixed(1)}%
                    </Tag>
                  </div>
                  <div className="text-sm text-gray-600 mb-2">
                    出现次数: {keyword.count}
                  </div>
                  <Progress
                    percent={Math.min(keyword.density * 20, 100)}
                    strokeColor={
                      keyword.density >= 2 && keyword.density <= 5 ? '#52c41a' :
                      keyword.density > 5 ? '#ff4d4f' : '#faad14'
                    }
                  />
                  <div className="text-xs text-gray-500 mt-1">
                    {keyword.density < 2 ? '密度偏低，建议增加关键词使用' :
                     keyword.density > 5 ? '密度过高，可能被视为关键词堆砌' :
                     '密度适中，有利于SEO'}
                  </div>
                </div>
              ))}
            </div>
          </div>

          <Divider />

          {/* Readability */}
          <div>
            <Title level={5}>
              <InfoCircleOutlined className="mr-2" />
              可读性分析
            </Title>
            <div className="space-y-3">
              <div>
                <div className="flex justify-between items-center mb-2">
                  <Text>可读性得分</Text>
                  <Text strong>{seoAnalysis.readability.score}</Text>
                </div>
                <Progress
                  percent={seoAnalysis.readability.score}
                  strokeColor="#722ed1"
                />
                <div className="text-sm text-gray-600 mt-1">
                  等级: {seoAnalysis.readability.level}
                </div>
              </div>

              <div>
                <Text strong>改进建议:</Text>
                <ul className="mt-2 space-y-1">
                  {seoAnalysis.readability.suggestions.map((suggestion, index) => (
                    <li key={index} className="text-sm text-gray-600">
                      • {suggestion}
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </div>
        </div>
      </Drawer>

      {/* Media Library Drawer */}
      <Drawer
        title="媒体库"
        placement="right"
        width={600}
        onClose={() => setMediaDrawerVisible(false)}
        open={mediaDrawerVisible}
      >
        <div className="text-center text-gray-500 py-20">
          <PictureOutlined style={{ fontSize: 48 }} />
          <div className="mt-4">媒体库功能开发中...</div>
        </div>
      </Drawer>
    </div>
  );
};

export default ContentEditor;
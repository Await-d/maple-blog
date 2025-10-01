import React, { useState, useCallback, useMemo, useEffect } from 'react';
import {
  Card,
  Table,
  Button,
  Space,
  Tag,
  Typography,
  Select,
  Input,
  DatePicker,
  Modal,
  message,
  Dropdown,
  Tooltip,
  Progress,
  Popconfirm,
  Form,
  Row,
  Col,
  Image,
  Drawer,
  Tabs,
  Statistic,
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  EyeOutlined,
  MoreOutlined,
  ExportOutlined,
  ImportOutlined,
  FilterOutlined,
  CalendarOutlined,
  FileTextOutlined,
  BarChartOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  ExclamationCircleOutlined,
  SendOutlined,
  CopyOutlined,
  ShareAltOutlined,
  DownloadOutlined,
  ReloadOutlined,
  CommentOutlined
} from '@ant-design/icons';
import dayjs, { Dayjs } from 'dayjs';
import { useNavigate } from 'react-router-dom';

const { Title, Text, Paragraph } = Typography;
const { Search } = Input;
const { RangePicker } = DatePicker;
const { Option } = Select;
const { TabPane } = Tabs;

// Post status configurations
const POST_STATUS = {
  DRAFT: { color: 'default', icon: <FileTextOutlined />, text: '草稿' },
  REVIEW: { color: 'warning', icon: <ClockCircleOutlined />, text: '待审核' },
  PUBLISHED: { color: 'success', icon: <CheckCircleOutlined />, text: '已发布' },
  ARCHIVED: { color: 'error', icon: <ExclamationCircleOutlined />, text: '已归档' },
  SCHEDULED: { color: 'processing', icon: <CalendarOutlined />, text: '定时发布' }
};

// API service for posts
const postService = {
  async getPosts(_params: Record<string, unknown> = {}) {
    const response = await fetch('/api/posts', {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('authToken')}`
      }
    });

    if (!response.ok) {
      throw new Error('Failed to fetch posts');
    }

    return response.json();
  },

  async updatePostStatus(postId: number, status: string) {
    const response = await fetch(`/api/posts/${postId}/status`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('authToken')}`
      },
      body: JSON.stringify({ status })
    });

    if (!response.ok) {
      throw new Error('Failed to update post status');
    }

    return response.json();
  },

  async deletePosts(postIds: number[]) {
    const response = await fetch('/api/posts/batch-delete', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('authToken')}`
      },
      body: JSON.stringify({ postIds })
    });

    if (!response.ok) {
      throw new Error('Failed to delete posts');
    }

    return response.json();
  }
};

interface Post {
  id: number;
  title: string;
  excerpt: string;
  author: { name: string; avatar: string };
  category: { id: number; name: string };
  tags: { id: number; name: string }[];
  status: keyof typeof POST_STATUS;
  views: number;
  comments: number;
  likes: number;
  shares: number;
  publishDate: string | null;
  createdAt: string;
  updatedAt: string;
  featured: boolean;
  seoScore: number;
  readingTime: number;
  wordCount: number;
  featuredImage: string | null;
}

interface PostFilters {
  search: string;
  status: string[];
  category: string[];
  author: string[];
  dateRange: [Dayjs, Dayjs] | null;
  featured: boolean | null;
  seoScoreRange: [number, number] | null;
}

const PostManagement: React.FC = () => {
  const navigate = useNavigate();
  const [posts, setPosts] = useState<Post[]>([]);
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [filters, setFilters] = useState<PostFilters>({
    search: '',
    status: [],
    category: [],
    author: [],
    dateRange: null,
    featured: null,
    seoScoreRange: null
  });
  const [filterDrawerVisible, setFilterDrawerVisible] = useState(false);
  const [postDetailVisible, setPostDetailVisible] = useState(false);
  const [selectedPost, setSelectedPost] = useState<Post | null>(null);
  const [batchActionModal, setBatchActionModal] = useState<{ visible: boolean; action: string }>({ visible: false, action: '' });
  const [loading, setLoading] = useState(false);
  const [sortConfig] = useState<{ field: string; direction: 'asc' | 'desc' }>({ field: 'updatedAt', direction: 'desc' });

  // Load posts data from API
  useEffect(() => {
    const loadPosts = async () => {
      try {
        setLoading(true);
        const data = await postService.getPosts();
        setPosts(data.data || []);
      } catch (error) {
        console.error('Failed to load posts:', error);
        message.error('加载文章数据失败');
        setPosts([]); // Set empty array on error
      } finally {
        setLoading(false);
      }
    };

    loadPosts();
  }, []);

  // Filter and sort posts
  const filteredPosts = useMemo(() => {
    let filtered = [...posts];

    // Text search
    if (filters.search) {
      const searchLower = filters.search.toLowerCase();
      filtered = filtered.filter(post =>
        post.title.toLowerCase().includes(searchLower) ||
        post.excerpt.toLowerCase().includes(searchLower) ||
        post.author.name.toLowerCase().includes(searchLower)
      );
    }

    // Status filter
    if (filters.status.length > 0) {
      filtered = filtered.filter(post => filters.status.includes(post.status));
    }

    // Category filter
    if (filters.category.length > 0) {
      filtered = filtered.filter(post => filters.category.includes(post.category.name));
    }

    // Author filter
    if (filters.author.length > 0) {
      filtered = filtered.filter(post => filters.author.includes(post.author.name));
    }

    // Date range filter
    if (filters.dateRange) {
      const [start, end] = filters.dateRange;
      filtered = filtered.filter(post => {
        const postDate = dayjs(post.createdAt);
        return postDate.isAfter(start) && postDate.isBefore(end);
      });
    }

    // Featured filter
    if (filters.featured !== null) {
      filtered = filtered.filter(post => post.featured === filters.featured);
    }

    // SEO score filter
    if (filters.seoScoreRange) {
      const [min, max] = filters.seoScoreRange;
      filtered = filtered.filter(post => post.seoScore >= min && post.seoScore <= max);
    }

    // Sort
    filtered.sort((a, b) => {
      let aValue: unknown = a[sortConfig.field as keyof Post];
      let bValue: unknown = b[sortConfig.field as keyof Post];

      if (sortConfig.field === 'author') {
        aValue = a.author.name;
        bValue = b.author.name;
      } else if (sortConfig.field === 'category') {
        aValue = a.category.name;
        bValue = b.category.name;
      }

      if (typeof aValue === 'string') {
        aValue = aValue.toLowerCase();
        bValue = bValue.toLowerCase();
      }

      if (sortConfig.direction === 'asc') {
        return aValue > bValue ? 1 : -1;
      } else {
        return aValue < bValue ? 1 : -1;
      }
    });

    return filtered;
  }, [posts, filters, sortConfig]);

  // Statistics
  const statistics = useMemo(() => {
    const total = posts.length;
    const published = posts.filter(p => p.status === 'PUBLISHED').length;
    const draft = posts.filter(p => p.status === 'DRAFT').length;
    const review = posts.filter(p => p.status === 'REVIEW').length;
    const totalViews = posts.reduce((sum, p) => sum + p.views, 0);
    const totalComments = posts.reduce((sum, p) => sum + p.comments, 0);
    const avgSeoScore = posts.length > 0 ? posts.reduce((sum, p) => sum + p.seoScore, 0) / posts.length : 0;

    return {
      total,
      published,
      draft,
      review,
      totalViews,
      totalComments,
      avgSeoScore: Math.round(avgSeoScore)
    };
  }, [posts]);

  // Handle post status change
  const handleStatusChange = useCallback(async (postId: number, newStatus: keyof typeof POST_STATUS) => {
    try {
      setLoading(true);
      await postService.updatePostStatus(postId, newStatus);

      setPosts(prev => prev.map(post =>
        post.id === postId
          ? {
              ...post,
              status: newStatus,
              publishDate: newStatus === 'PUBLISHED' ? new Date().toISOString() : post.publishDate,
              updatedAt: new Date().toISOString()
            }
          : post
      ));
      message.success(`文章状态已更新为${POST_STATUS[newStatus].text}`);
    } catch (error) {
      console.error('Failed to update post status:', error);
      message.error('更新文章状态失败');
    } finally {
      setLoading(false);
    }
  }, []);

  // Handle batch actions
  const handleBatchAction = useCallback((action: string) => {
    if (selectedRowKeys.length === 0) {
      message.warning('请先选择要操作的文章');
      return;
    }
    setBatchActionModal({ visible: true, action });
  }, [selectedRowKeys]);

  const confirmBatchAction = useCallback(async () => {
    try {
      setLoading(true);
      const action = batchActionModal.action;
      const count = selectedRowKeys.length;
      const postIds = selectedRowKeys.map(key => Number(key));

      if (action === 'delete') {
        await postService.deletePosts(postIds);
        setPosts(prev => prev.filter(post => !selectedRowKeys.includes(post.id)));
        setSelectedRowKeys([]);
        message.success(`已删除 ${count} 篇文章`);
      } else if (action === 'publish') {
        // Batch update status
        await Promise.all(postIds.map(id => postService.updatePostStatus(id, 'PUBLISHED')));
        setPosts(prev => prev.map(post =>
          selectedRowKeys.includes(post.id)
            ? { ...post, status: 'PUBLISHED' as keyof typeof POST_STATUS, publishDate: new Date().toISOString() }
            : post
        ));
        setSelectedRowKeys([]);
        message.success(`已发布 ${count} 篇文章`);
      } else if (action === 'archive') {
        // Batch update status
        await Promise.all(postIds.map(id => postService.updatePostStatus(id, 'ARCHIVED')));
        setPosts(prev => prev.map(post =>
          selectedRowKeys.includes(post.id)
            ? { ...post, status: 'ARCHIVED' as keyof typeof POST_STATUS }
            : post
        ));
        setSelectedRowKeys([]);
        message.success(`已归档 ${count} 篇文章`);
      }

      setBatchActionModal({ visible: false, action: '' });
    } catch (error) {
      console.error('Batch action failed:', error);
      message.error('批量操作失败');
    } finally {
      setLoading(false);
    }
  }, [batchActionModal.action, selectedRowKeys]);

  // Table columns
  const columns = [
    {
      title: '文章信息',
      key: 'info',
      width: 300,
      render: (record: Post) => (
        <div className="flex gap-3">
          {record.featuredImage && (
            <Image
              src={record.featuredImage}
              alt={record.title}
              width={60}
              height={40}
              className="rounded object-cover"
              fallback="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAMIAAADDCAYAAADQvc6UAAABRWlDQ1BJQ0MgUHJvZmlsZQAAKJFjYGASSSwoyGFhYGDIzSspCnJ3UoiIjFJgf8LAwSDCIMogwMCcmFxc4BgQ4ANUwgCjUcG3awyMIPqyLsis7PPOq3QdDFcvjV3jOD1boQVTPQrgSkktTgbSf4A4LbmgqISBgTEFyFYuLykAsTuAbJEioKOA7DkgdjqEvQHEToKwj4DVhAQ5A9k3gGyB5IxEoBmML4BsnSQk8XQkNtReEOBxcfXxUQg1Mjc0dyHgXNJBSWpFCYh2zi+oLMpMzyhRcASGUqqCZ16yno6CkYGRAQMDKMwhqj/fAIcloxgHQqxAjIHBEugw5sUIsSQpBobtQPdLciLEVJYzMPBHMDBsayhILEqEO4DxG0txmrERhM29nYGBddr//5/DGRjYNRkY/l7////39v///y4Dmn+LgeHANwDrkl1AuO+pmgAAADhlWElmTU0AKgAAAAgAAYdpAAQAAAABAAAAGgAAAAAAAqACAAQAAAABAAAAwqADAAQAAAABAAAAwwAAAAD9b/HnAAAHlklEQVR4Ae3dP3Ik1RUG8G+2gDcBMzkH2C5vAuQYbBdvYiEQY6vJDLshaBP7aQiKV9Xd06/fz9f/T+o2VRXd996rqnfP..."
            />
          )}
          <div className="flex-1 min-w-0">
            <div className="font-medium text-gray-900 truncate">{record.title}</div>
            <div className="text-sm text-gray-500 mt-1">{record.excerpt}</div>
            <div className="flex items-center gap-2 mt-2">
              <img src={record.author.avatar} alt={record.author.name} className="w-4 h-4 rounded-full" />
              <span className="text-xs text-gray-500">{record.author.name}</span>
              <span className="text-xs text-gray-400">·</span>
              <span className="text-xs text-gray-500">{dayjs(record.createdAt).format('MM-DD HH:mm')}</span>
            </div>
          </div>
        </div>
      )
    },
    {
      title: '分类/标签',
      key: 'category',
      width: 150,
      render: (record: Post) => (
        <div>
          <Tag color="blue">{record.category.name}</Tag>
          <div className="mt-1">
            {record.tags.map(tag => (
              <Tag key={tag.id} >{tag.name}</Tag>
            ))}
          </div>
        </div>
      )
    },
    {
      title: '状态',
      key: 'status',
      width: 100,
      render: (record: Post) => {
        return (
          <Select
            value={record.status}
            
            style={{ width: '100%' }}
            onChange={(value) => handleStatusChange(record.id, value)}
          >
            {Object.entries(POST_STATUS).map(([key, config]) => (
              <Option key={key} value={key}>
                <Tag color={config.color} icon={config.icon}>{config.text}</Tag>
              </Option>
            ))}
          </Select>
        );
      }
    },
    {
      title: '数据统计',
      key: 'stats',
      width: 120,
      render: (record: Post) => (
        <div className="text-xs">
          <div className="flex justify-between">
            <span className="text-gray-500">浏览:</span>
            <span>{record.views}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-500">评论:</span>
            <span>{record.comments}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-500">点赞:</span>
            <span>{record.likes}</span>
          </div>
        </div>
      )
    },
    {
      title: 'SEO',
      key: 'seo',
      width: 80,
      render: (record: Post) => (
        <div className="text-center">
          <Progress
            type="circle"
            size={40}
            percent={record.seoScore}
            format={percent => `${percent}`}
            strokeColor={record.seoScore >= 80 ? '#52c41a' : record.seoScore >= 60 ? '#faad14' : '#ff4d4f'}
          />
        </div>
      )
    },
    {
      title: '操作',
      key: 'actions',
      width: 120,
      render: (record: Post) => (
        <Space >
          <Tooltip title="查看详情">
            <Button
              type="text"
              
              icon={<EyeOutlined />}
              onClick={() => {
                setSelectedPost(record);
                setPostDetailVisible(true);
              }}
            />
          </Tooltip>
          <Tooltip title="编辑">
            <Button
              type="text"
              icon={<EditOutlined />}
              onClick={() => navigate(`/content/posts/${record.id}/edit`)}
            />
          </Tooltip>
          <Dropdown
            menu={{
              items: [
                {
                  key: 'copy',
                  icon: <CopyOutlined />,
                  label: '复制文章',
                  onClick: () => message.info('复制文章功能')
                },
                {
                  key: 'share',
                  icon: <ShareAltOutlined />,
                  label: '分享文章',
                  onClick: () => message.info('分享文章功能')
                },
                {
                  key: 'export',
                  icon: <DownloadOutlined />,
                  label: '导出文章',
                  onClick: () => message.info('导出文章功能')
                },
                {
                  type: 'divider'
                },
                {
                  key: 'delete',
                  icon: <DeleteOutlined />,
                  label: '删除文章',
                  danger: true,
                  onClick: () => {
                    Modal.confirm({
                      title: '确认删除',
                      content: '确定要删除这篇文章吗？此操作不可恢复。',
                      okType: 'danger',
                      onOk: () => {
                        setPosts(prev => prev.filter(p => p.id !== record.id));
                        message.success('文章已删除');
                      }
                    });
                  }
                }
              ]
            }}
            trigger={['click']}
          >
            <Button type="text"  icon={<MoreOutlined />} />
          </Dropdown>
        </Space>
      )
    }
  ];

  const rowSelection = {
    selectedRowKeys,
    onChange: setSelectedRowKeys,
    selections: [
      Table.SELECTION_ALL,
      Table.SELECTION_INVERT,
      Table.SELECTION_NONE,
      {
        key: 'published',
        text: '选择已发布',
        onSelect: (_changeableRowKeys: React.Key[]) => {
          const publishedKeys = posts
            .filter(post => post.status === 'PUBLISHED')
            .map(post => post.id);
          setSelectedRowKeys(publishedKeys);
        }
      }
    ]
  };

  return (
    <div className="space-y-6">
      {/* Statistics Cards */}
      <Row gutter={16}>
        <Col span={4}>
          <Card>
            <Statistic
              title="总文章数"
              value={statistics.total}
              prefix={<FileTextOutlined />}
              valueStyle={{ color: '#1677ff' }}
            />
          </Card>
        </Col>
        <Col span={4}>
          <Card>
            <Statistic
              title="已发布"
              value={statistics.published}
              prefix={<CheckCircleOutlined />}
              valueStyle={{ color: '#52c41a' }}
            />
          </Card>
        </Col>
        <Col span={4}>
          <Card>
            <Statistic
              title="草稿"
              value={statistics.draft}
              prefix={<FileTextOutlined />}
              valueStyle={{ color: '#faad14' }}
            />
          </Card>
        </Col>
        <Col span={4}>
          <Card>
            <Statistic
              title="待审核"
              value={statistics.review}
              prefix={<ClockCircleOutlined />}
              valueStyle={{ color: '#fa8c16' }}
            />
          </Card>
        </Col>
        <Col span={4}>
          <Card>
            <Statistic
              title="总浏览量"
              value={statistics.totalViews}
              prefix={<EyeOutlined />}
              valueStyle={{ color: '#722ed1' }}
            />
          </Card>
        </Col>
        <Col span={4}>
          <Card>
            <Statistic
              title="平均SEO"
              value={statistics.avgSeoScore}
              suffix="%"
              prefix={<BarChartOutlined />}
              valueStyle={{ color: statistics.avgSeoScore >= 80 ? '#52c41a' : '#faad14' }}
            />
          </Card>
        </Col>
      </Row>

      <Card>
        <div className="flex justify-between items-center mb-4">
          <Title level={3} className="mb-0">文章管理</Title>
          <Space>
            <Button icon={<PlusOutlined />} type="primary" onClick={() => navigate('/content/posts/new')}>
              新建文章
            </Button>
            <Button icon={<ImportOutlined />}>
              导入文章
            </Button>
            <Button icon={<ExportOutlined />}>
              导出文章
            </Button>
          </Space>
        </div>

        {/* Filters and Search */}
        <div className="flex gap-4 mb-4 flex-wrap">
          <Search
            placeholder="搜索文章标题、内容或作者"
            style={{ width: 300 }}
            value={filters.search}
            onChange={e => setFilters(prev => ({ ...prev, search: e.target.value }))}
            allowClear
          />
          <Select
            mode="multiple"
            placeholder="状态筛选"
            style={{ width: 200 }}
            value={filters.status}
            onChange={value => setFilters(prev => ({ ...prev, status: value }))}
            allowClear
          >
            {Object.entries(POST_STATUS).map(([key, config]) => (
              <Option key={key} value={key}>
                <Tag color={config.color} icon={config.icon}>{config.text}</Tag>
              </Option>
            ))}
          </Select>
          <Button
            icon={<FilterOutlined />}
            onClick={() => setFilterDrawerVisible(true)}
          >
            高级筛选
          </Button>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => {
              setFilters({
                search: '',
                status: [],
                category: [],
                author: [],
                dateRange: null,
                featured: null,
                seoScoreRange: null
              });
              message.success('筛选条件已重置');
            }}
          >
            重置筛选
          </Button>
        </div>

        {/* Batch Actions */}
        {selectedRowKeys.length > 0 && (
          <div className="bg-blue-50 border border-blue-200 rounded p-3 mb-4">
            <div className="flex items-center justify-between">
              <span className="text-blue-700">
                已选择 {selectedRowKeys.length} 篇文章
              </span>
              <Space>
                <Button
                  
                  icon={<SendOutlined />}
                  onClick={() => handleBatchAction('publish')}
                >
                  批量发布
                </Button>
                <Button
                  
                  icon={<ExclamationCircleOutlined />}
                  onClick={() => handleBatchAction('archive')}
                >
                  批量归档
                </Button>
                <Button
                  
                  icon={<ExportOutlined />}
                  onClick={() => handleBatchAction('export')}
                >
                  批量导出
                </Button>
                <Popconfirm
                  title="确认删除"
                  description={`确定要删除选中的 ${selectedRowKeys.length} 篇文章吗？`}
                  onConfirm={() => handleBatchAction('delete')}
                  okType="danger"
                >
                  <Button
                    
                    danger
                    icon={<DeleteOutlined />}
                  >
                    批量删除
                  </Button>
                </Popconfirm>
              </Space>
            </div>
          </div>
        )}

        {/* Posts Table */}
        <Table
          rowSelection={rowSelection}
          columns={columns}
          dataSource={filteredPosts}
          rowKey="id"
          pagination={{
            total: filteredPosts.length,
            pageSize: 10,
            showSizeChanger: true,
            showQuickJumper: true,
            showTotal: (total, range) => `第 ${range[0]}-${range[1]} 条，共 ${total} 条`
          }}
          scroll={{ x: 1200 }}
          loading={loading}
        />
      </Card>

      {/* Advanced Filter Drawer */}
      <Drawer
        title="高级筛选"
        placement="right"
        width={400}
        onClose={() => setFilterDrawerVisible(false)}
        open={filterDrawerVisible}
      >
        <Form layout="vertical">
          <Form.Item label="发布时间范围">
            <RangePicker
              style={{ width: '100%' }}
              value={filters.dateRange}
              onChange={value => setFilters(prev => ({ ...prev, dateRange: value }))}
            />
          </Form.Item>
          <Form.Item label="分类筛选">
            <Select
              mode="multiple"
              placeholder="选择分类"
              style={{ width: '100%' }}
              value={filters.category}
              onChange={value => setFilters(prev => ({ ...prev, category: value }))}
            >
              <Option value="前端技术">前端技术</Option>
              <Option value="系统架构">系统架构</Option>
              <Option value="数据库">数据库</Option>
            </Select>
          </Form.Item>
          <Form.Item label="作者筛选">
            <Select
              mode="multiple"
              placeholder="选择作者"
              style={{ width: '100%' }}
              value={filters.author}
              onChange={value => setFilters(prev => ({ ...prev, author: value }))}
            >
              <Option value="张三">张三</Option>
              <Option value="李四">李四</Option>
              <Option value="王五">王五</Option>
            </Select>
          </Form.Item>
          <Form.Item label="精选文章">
            <Select
              placeholder="选择精选状态"
              style={{ width: '100%' }}
              value={filters.featured}
              onChange={value => setFilters(prev => ({ ...prev, featured: value }))}
              allowClear
            >
              <Option value={true}>精选文章</Option>
              <Option value={false}>普通文章</Option>
            </Select>
          </Form.Item>
        </Form>
      </Drawer>

      {/* Post Detail Modal */}
      <Modal
        title="文章详情"
        open={postDetailVisible}
        onCancel={() => setPostDetailVisible(false)}
        footer={[
          <Button key="close" onClick={() => setPostDetailVisible(false)}>
            关闭
          </Button>,
          <Button key="edit" type="primary" icon={<EditOutlined />}>
            编辑文章
          </Button>
        ]}
        width={800}
      >
        {selectedPost && (
          <Tabs defaultActiveKey="info">
            <TabPane tab="基本信息" key="info">
              <div className="space-y-4">
                <div>
                  <Text strong>标题：</Text>
                  <Paragraph copyable>{selectedPost.title}</Paragraph>
                </div>
                <div>
                  <Text strong>摘要：</Text>
                  <Paragraph>{selectedPost.excerpt}</Paragraph>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <Text strong>作者：</Text>
                    <div className="flex items-center gap-2 mt-1">
                      <img src={selectedPost.author.avatar} alt={selectedPost.author.name} className="w-6 h-6 rounded-full" />
                      <span>{selectedPost.author.name}</span>
                    </div>
                  </div>
                  <div>
                    <Text strong>分类：</Text>
                    <div className="mt-1">
                      <Tag color="blue">{selectedPost.category.name}</Tag>
                    </div>
                  </div>
                </div>
                <div>
                  <Text strong>标签：</Text>
                  <div className="mt-1">
                    {selectedPost.tags.map(tag => (
                      <Tag key={tag.id}>{tag.name}</Tag>
                    ))}
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <Text strong>字数：</Text>
                    <span className="ml-2">{selectedPost.wordCount} 字</span>
                  </div>
                  <div>
                    <Text strong>阅读时间：</Text>
                    <span className="ml-2">{selectedPost.readingTime} 分钟</span>
                  </div>
                </div>
              </div>
            </TabPane>
            <TabPane tab="数据统计" key="stats">
              <Row gutter={16}>
                <Col span={6}>
                  <Statistic title="浏览量" value={selectedPost.views} prefix={<EyeOutlined />} />
                </Col>
                <Col span={6}>
                  <Statistic title="评论数" value={selectedPost.comments} prefix={<CommentOutlined />} />
                </Col>
                <Col span={6}>
                  <Statistic title="点赞数" value={selectedPost.likes} prefix={<CheckCircleOutlined />} />
                </Col>
                <Col span={6}>
                  <Statistic title="分享数" value={selectedPost.shares} prefix={<ShareAltOutlined />} />
                </Col>
              </Row>
              <div className="mt-6">
                <Text strong>SEO 得分：</Text>
                <div className="mt-2">
                  <Progress
                    percent={selectedPost.seoScore}
                    strokeColor={selectedPost.seoScore >= 80 ? '#52c41a' : selectedPost.seoScore >= 60 ? '#faad14' : '#ff4d4f'}
                  />
                </div>
              </div>
            </TabPane>
            <TabPane tab="时间信息" key="time">
              <div className="space-y-4">
                <div>
                  <Text strong>创建时间：</Text>
                  <div className="mt-1">{dayjs(selectedPost.createdAt).format('YYYY-MM-DD HH:mm:ss')}</div>
                </div>
                <div>
                  <Text strong>更新时间：</Text>
                  <div className="mt-1">{dayjs(selectedPost.updatedAt).format('YYYY-MM-DD HH:mm:ss')}</div>
                </div>
                {selectedPost.publishDate && (
                  <div>
                    <Text strong>发布时间：</Text>
                    <div className="mt-1">{dayjs(selectedPost.publishDate).format('YYYY-MM-DD HH:mm:ss')}</div>
                  </div>
                )}
              </div>
            </TabPane>
          </Tabs>
        )}
      </Modal>

      {/* Batch Action Confirmation Modal */}
      <Modal
        title="批量操作确认"
        open={batchActionModal.visible}
        onOk={confirmBatchAction}
        onCancel={() => setBatchActionModal({ visible: false, action: '' })}
        confirmLoading={loading}
        okType={batchActionModal.action === 'delete' ? 'danger' : 'primary'}
      >
        {batchActionModal.action === 'publish' && (
          <p>确定要发布选中的 {selectedRowKeys.length} 篇文章吗？</p>
        )}
        {batchActionModal.action === 'archive' && (
          <p>确定要归档选中的 {selectedRowKeys.length} 篇文章吗？</p>
        )}
        {batchActionModal.action === 'delete' && (
          <p>确定要删除选中的 {selectedRowKeys.length} 篇文章吗？此操作不可恢复。</p>
        )}
      </Modal>
    </div>
  );
};

export default PostManagement;

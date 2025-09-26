import React, { useState } from 'react';
import { Row, Col, Card, Table, Tag, Space, DatePicker, Select, Button, Tabs, Statistic, Avatar, Typography, Badge, Progress, Tooltip, Radio, Segmented, Divider } from 'antd';
import {
  FileTextOutlined,
  EyeOutlined,
  HeartOutlined,
  CommentOutlined,
  ShareAltOutlined,
  RiseOutlined,
  FallOutlined,
  TrophyOutlined,
  ThunderboltOutlined,
  TagOutlined,
  FolderOutlined,
  BarChartOutlined,
  PieChartOutlined,
  HeatMapOutlined,
  ExportOutlined
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import dayjs from 'dayjs';
import AnalyticsChart from '@/components/analytics/AnalyticsChart';
import { analyticsService } from '@/services/analytics.service';
import type { ContentMetrics, AuthorPerformance, TimeRange } from '@/types/analytics';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;
const { TabPane } = Tabs;

const ContentAnalytics: React.FC = () => {
  const [timeRange, setTimeRange] = useState<TimeRange>('month');
  const [customDateRange, setCustomDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs]>([
    dayjs().subtract(30, 'days'),
    dayjs()
  ]);
  const [contentType, setContentType] = useState<'all' | 'post' | 'page' | 'media'>('all');
  const [sortBy, setSortBy] = useState<'views' | 'engagement' | 'shares' | 'comments'>('views');
  const [viewMode, setViewMode] = useState<'table' | 'cards' | 'heatmap'>('table');
  const [selectedCategory, setSelectedCategory] = useState<string | undefined>();
  const [selectedAuthor, setSelectedAuthor] = useState<string | undefined>();

  // Fetch content metrics
  const { data: contentMetrics, isLoading: metricsLoading } = useQuery({
    queryKey: ['content-metrics', timeRange, customDateRange, contentType, selectedCategory, selectedAuthor],
    queryFn: () => analyticsService.getContentMetrics({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined,
      contentType: contentType === 'all' ? undefined : contentType,
      category: selectedCategory,
      author: selectedAuthor
    })
  });

  // Fetch content performance
  const { data: contentPerformance, isLoading: performanceLoading } = useQuery({
    queryKey: ['content-performance', timeRange, customDateRange],
    queryFn: () => analyticsService.getContentPerformance({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Fetch author performance
  const { data: authorPerformance, isLoading: authorLoading } = useQuery({
    queryKey: ['author-performance', timeRange, customDateRange],
    queryFn: () => analyticsService.getAuthorPerformance({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Fetch engagement metrics
  const { data: engagementData, isLoading: engagementLoading } = useQuery({
    queryKey: ['engagement-metrics', timeRange, customDateRange],
    queryFn: () => analyticsService.getEngagementMetrics({
      timeRange,
      startDate: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : undefined,
      endDate: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : undefined
    })
  });

  // Sort content metrics
  const sortedContent = contentMetrics ? [...contentMetrics].sort((a, b) => {
    switch (sortBy) {
      case 'views':
        return b.views - a.views;
      case 'engagement':
        return b.engagementScore - a.engagementScore;
      case 'shares':
        return b.shares - a.shares;
      case 'comments':
        return b.comments - a.comments;
      default:
        return 0;
    }
  }) : [];

  // Content table columns
  const contentColumns = [
    {
      title: '排名',
      key: 'rank',
      width: 60,
      fixed: 'left' as const,
      render: (_: unknown, __: unknown, index: number) => {
        const medals = ['🥇', '🥈', '🥉'];
        return index < 3 ? medals[index] : `#${index + 1}`;
      }
    },
    {
      title: '内容标题',
      dataIndex: 'title',
      key: 'title',
      width: 300,
      fixed: 'left' as const,
      render: (title: string, record: ContentMetrics) => (
        <Space direction="vertical" size={0}>
          <Text strong>{title}</Text>
          <Space >
            <Tag color={record.type === 'post' ? 'blue' : record.type === 'page' ? 'green' : 'orange'}>
              {record.type === 'post' ? '文章' : record.type === 'page' ? '页面' : '媒体'}
            </Tag>
            <Text type="secondary" style={{ fontSize: 12 }}>
              {dayjs(record.publishDate).format('YYYY-MM-DD')}
            </Text>
          </Space>
        </Space>
      )
    },
    {
      title: '作者',
      dataIndex: 'author',
      key: 'author',
      width: 120,
      render: (author: string) => (
        <Space>
          <Avatar  style={{ backgroundColor: '#1890ff' }}>
            {author.charAt(0).toUpperCase()}
          </Avatar>
          <Text>{author}</Text>
        </Space>
      )
    },
    {
      title: '浏览量',
      dataIndex: 'views',
      key: 'views',
      width: 100,
      align: 'right' as const,
      sorter: (a: ContentMetrics, b: ContentMetrics) => a.views - b.views,
      render: (views: number) => (
        <Text strong>{views.toLocaleString()}</Text>
      )
    },
    {
      title: '独立访客',
      dataIndex: 'uniqueViews',
      key: 'uniqueViews',
      width: 100,
      align: 'right' as const,
      render: (views: number) => views.toLocaleString()
    },
    {
      title: '平均停留时长',
      dataIndex: 'avgTimeOnPage',
      key: 'avgTimeOnPage',
      width: 120,
      align: 'right' as const,
      render: (time: number) => {
        const minutes = Math.floor(time / 60);
        const seconds = time % 60;
        return `${minutes}:${seconds.toString().padStart(2, '0')}`;
      }
    },
    {
      title: '跳出率',
      dataIndex: 'bounceRate',
      key: 'bounceRate',
      width: 100,
      align: 'right' as const,
      render: (rate: number) => (
        <Text type={rate > 70 ? 'danger' : rate > 50 ? 'warning' : 'success'}>
          {rate.toFixed(1)}%
        </Text>
      )
    },
    {
      title: '互动',
      key: 'engagement',
      width: 200,
      render: (record: ContentMetrics) => (
        <Space>
          <Tooltip title="点赞">
            <span><HeartOutlined /> {record.likes}</span>
          </Tooltip>
          <Tooltip title="评论">
            <span><CommentOutlined /> {record.comments}</span>
          </Tooltip>
          <Tooltip title="分享">
            <span><ShareAltOutlined /> {record.shares}</span>
          </Tooltip>
        </Space>
      )
    },
    {
      title: '参与度评分',
      dataIndex: 'engagementScore',
      key: 'engagementScore',
      width: 120,
      align: 'center' as const,
      render: (score: number) => (
        <Progress
          type="circle"
          percent={score}
          width={50}
          strokeColor={score > 80 ? '#52c41a' : score > 60 ? '#1890ff' : '#faad14'}
        />
      )
    },
    {
      title: '趋势',
      dataIndex: 'trend',
      key: 'trend',
      width: 80,
      align: 'center' as const,
      render: (trend: number) => (
        <Text type={trend > 0 ? 'success' : 'danger'}>
          {trend > 0 ? <RiseOutlined /> : <FallOutlined />}
          {Math.abs(trend).toFixed(1)}%
        </Text>
      )
    }
  ];

  // Author performance columns
  const authorColumns = [
    {
      title: '作者',
      key: 'author',
      render: (record: AuthorPerformance) => (
        <Space>
          <Avatar src={record.avatar} style={{ backgroundColor: '#1890ff' }}>
            {record.authorName.charAt(0).toUpperCase()}
          </Avatar>
          <Space direction="vertical" size={0}>
            <Text strong>{record.authorName}</Text>
            {record.followers && (
              <Text type="secondary" style={{ fontSize: 12 }}>
                {record.followers} 关注者
              </Text>
            )}
          </Space>
        </Space>
      )
    },
    {
      title: '发布文章',
      dataIndex: 'posts',
      key: 'posts',
      align: 'center' as const,
      render: (posts: number) => <Badge count={posts} showZero style={{ backgroundColor: '#52c41a' }} />
    },
    {
      title: '总浏览量',
      dataIndex: 'totalViews',
      key: 'totalViews',
      align: 'right' as const,
      render: (views: number) => <Text strong>{views.toLocaleString()}</Text>
    },
    {
      title: '平均浏览量',
      dataIndex: 'avgViews',
      key: 'avgViews',
      align: 'right' as const,
      render: (views: number) => views.toLocaleString()
    },
    {
      title: '总互动',
      dataIndex: 'totalEngagement',
      key: 'totalEngagement',
      align: 'right' as const,
      render: (engagement: number) => engagement.toLocaleString()
    },
    {
      title: '平均互动率',
      dataIndex: 'avgEngagement',
      key: 'avgEngagement',
      align: 'center' as const,
      render: (rate: number) => (
        <Progress percent={rate}  strokeColor="#1890ff" />
      )
    },
    {
      title: '最热文章',
      dataIndex: 'topPost',
      key: 'topPost',
      width: 200,
      ellipsis: true,
      render: (post: string) => (
        <Tooltip title={post}>
          <Text>{post}</Text>
        </Tooltip>
      )
    },
    {
      title: '趋势',
      dataIndex: 'trend',
      key: 'trend',
      align: 'center' as const,
      render: (trend: number) => (
        <Text type={trend > 0 ? 'success' : 'danger'}>
          {trend > 0 ? <RiseOutlined /> : <FallOutlined />}
          {Math.abs(trend).toFixed(1)}%
        </Text>
      )
    }
  ];

  // Prepare chart data
  const categoryChartData = {
    categories: contentPerformance?.categories?.map(c => c.category) || [],
    series: [
      {
        name: '文章数',
        data: contentPerformance?.categories?.map(c => c.posts) || []
      },
      {
        name: '总浏览量',
        data: contentPerformance?.categories?.map(c => c.totalViews) || []
      }
    ]
  };

  const engagementChartData = {
    indicators: [
      { name: '点赞', max: 10000 },
      { name: '评论', max: 5000 },
      { name: '分享', max: 3000 },
      { name: '收藏', max: 2000 },
      { name: '互动率', max: 100 }
    ],
    series: [
      {
        name: '互动数据',
        value: [
          engagementData?.likes || 0,
          engagementData?.comments || 0,
          engagementData?.shares || 0,
          engagementData?.saves || 0,
          (engagementData?.engagementRate || 0) * 100
        ]
      }
    ]
  };

  // Handle export
  const handleExport = () => {
    analyticsService.exportData({
      format: 'excel',
      dateRange: {
        start: timeRange === 'custom' ? customDateRange[0].format('YYYY-MM-DD') : dayjs().subtract(30, 'days').format('YYYY-MM-DD'),
        end: timeRange === 'custom' ? customDateRange[1].format('YYYY-MM-DD') : dayjs().format('YYYY-MM-DD')
      },
      metrics: ['views', 'uniqueViews', 'avgTimeOnPage', 'bounceRate', 'engagement'],
      dimensions: ['content', 'author', 'category', 'tags']
    }).then(blob => {
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `content-analytics-${dayjs().format('YYYY-MM-DD')}.xlsx`;
      link.click();
    });
  };

  return (
    <div className="content-analytics">
      {/* Header */}
      <div style={{ marginBottom: 24 }}>
        <Row justify="space-between" align="middle">
          <Col>
            <Title level={2} style={{ margin: 0 }}>
              内容分析
            </Title>
          </Col>
          <Col>
            <Space>
              <Select
                value={timeRange}
                onChange={setTimeRange}
                style={{ width: 120 }}
                options={analyticsService.getTimeRangePresets()}
              />
              {timeRange === 'custom' && (
                <RangePicker
                  value={customDateRange}
                  onChange={(dates) => dates && setCustomDateRange(dates as [dayjs.Dayjs, dayjs.Dayjs])}
                />
              )}
              <Button icon={<ExportOutlined />} onClick={handleExport}>
                导出报表
              </Button>
            </Space>
          </Col>
        </Row>
      </div>

      {/* Summary Stats */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="总内容数"
              value={contentMetrics?.length || 0}
              prefix={<FileTextOutlined />}
              valueStyle={{ color: '#1890ff' }}
            />
            <div style={{ marginTop: 16 }}>
              <Space>
                <Tag color="blue">文章 {sortedContent.filter(c => c.type === 'post').length}</Tag>
                <Tag color="green">页面 {sortedContent.filter(c => c.type === 'page').length}</Tag>
              </Space>
            </div>
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="总浏览量"
              value={sortedContent.reduce((sum, c) => sum + c.views, 0)}
              prefix={<EyeOutlined />}
              valueStyle={{ color: '#52c41a' }}
            />
            <div style={{ marginTop: 16 }}>
              <Text type="secondary">
                平均每篇: {Math.round(sortedContent.reduce((sum, c) => sum + c.views, 0) / (sortedContent.length || 1)).toLocaleString()}
              </Text>
            </div>
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="总互动数"
              value={engagementData?.totalInteractions || 0}
              prefix={<HeartOutlined />}
              valueStyle={{ color: '#722ed1' }}
            />
            <div style={{ marginTop: 16 }}>
              <Progress
                percent={engagementData?.engagementRate ? engagementData.engagementRate * 100 : 0}
                
                format={(percent) => `互动率 ${percent?.toFixed(1)}%`}
              />
            </div>
          </Card>
        </Col>
        <Col xs={24} sm={12} md={6}>
          <Card>
            <Statistic
              title="病毒传播指数"
              value={engagementData?.viralityScore || 0}
              prefix={<ThunderboltOutlined />}
              valueStyle={{ color: '#fa8c16' }}
              precision={2}
            />
            <div style={{ marginTop: 16 }}>
              <Badge
                status={engagementData?.viralityScore && engagementData.viralityScore > 5 ? 'processing' : 'default'}
                text={engagementData?.viralityScore && engagementData.viralityScore > 5 ? '高传播性' : '普通传播'}
              />
            </div>
          </Card>
        </Col>
      </Row>

      {/* Main Content Area */}
      <Tabs defaultActiveKey="content">
        <TabPane tab="内容表现" key="content">
          {/* Filters */}
          <Card style={{ marginBottom: 16 }}>
            <Row gutter={[16, 16]} align="middle">
              <Col flex="auto">
                <Space size="middle">
                  <Radio.Group value={contentType} onChange={(e) => setContentType(e.target.value)}>
                    <Radio.Button value="all">全部</Radio.Button>
                    <Radio.Button value="post">文章</Radio.Button>
                    <Radio.Button value="page">页面</Radio.Button>
                    <Radio.Button value="media">媒体</Radio.Button>
                  </Radio.Group>
                  <Select
                    placeholder="选择分类"
                    style={{ width: 150 }}
                    allowClear
                    value={selectedCategory}
                    onChange={setSelectedCategory}
                  >
                    {contentPerformance?.categories?.map(cat => (
                      <Select.Option key={cat.category} value={cat.category}>
                        {cat.category}
                      </Select.Option>
                    ))}
                  </Select>
                  <Select
                    placeholder="选择作者"
                    style={{ width: 150 }}
                    allowClear
                    value={selectedAuthor}
                    onChange={setSelectedAuthor}
                  >
                    {authorPerformance?.map(author => (
                      <Select.Option key={author.authorId} value={author.authorId}>
                        {author.authorName}
                      </Select.Option>
                    ))}
                  </Select>
                </Space>
              </Col>
              <Col>
                <Space>
                  <Select value={sortBy} onChange={setSortBy} style={{ width: 120 }}>
                    <Select.Option value="views">按浏览量</Select.Option>
                    <Select.Option value="engagement">按互动率</Select.Option>
                    <Select.Option value="shares">按分享数</Select.Option>
                    <Select.Option value="comments">按评论数</Select.Option>
                  </Select>
                  <Segmented
                    value={viewMode}
                    onChange={setViewMode}
                    options={[
                      { label: '表格', value: 'table', icon: <BarChartOutlined /> },
                      { label: '卡片', value: 'cards', icon: <PieChartOutlined /> },
                      { label: '热图', value: 'heatmap', icon: <HeatMapOutlined /> }
                    ]}
                  />
                </Space>
              </Col>
            </Row>
          </Card>

          {/* Content Table/Cards/Heatmap */}
          {viewMode === 'table' ? (
            <Card>
              <Table
                columns={contentColumns}
                dataSource={sortedContent}
                rowKey="contentId"
                loading={metricsLoading}
                pagination={{
                  showSizeChanger: true,
                  showTotal: (total) => `共 ${total} 条`,
                  pageSize: 20
                }}
                scroll={{ x: 1500 }}
              />
            </Card>
          ) : viewMode === 'cards' ? (
            <Row gutter={[16, 16]}>
              {sortedContent.slice(0, 12).map((content, index) => (
                <Col xs={24} sm={12} md={8} lg={6} key={content.contentId}>
                  <Card hoverable>
                    <Card.Meta
                      avatar={
                        <Badge count={`#${index + 1}`} style={{ backgroundColor: index < 3 ? '#f5222d' : '#8c8c8c' }}>
                          <Avatar size={48} style={{ backgroundColor: '#1890ff' }}>
                            {content.type === 'post' ? <FileTextOutlined /> : <FolderOutlined />}
                          </Avatar>
                        </Badge>
                      }
                      title={
                        <Tooltip title={content.title}>
                          <Text ellipsis>{content.title}</Text>
                        </Tooltip>
                      }
                      description={content.author}
                    />
                    <div style={{ marginTop: 16 }}>
                      <Row gutter={[8, 8]}>
                        <Col span={12}>
                          <Statistic
                            title="浏览量"
                            value={content.views}
                            valueStyle={{ fontSize: 14 }}
                          />
                        </Col>
                        <Col span={12}>
                          <Statistic
                            title="互动率"
                            value={content.engagementScore}
                            suffix="%"
                            valueStyle={{ fontSize: 14 }}
                          />
                        </Col>
                      </Row>
                      <Progress
                        percent={content.engagementScore}
                        
                        strokeColor={{
                          '0%': '#108ee9',
                          '100%': '#87d068'
                        }}
                        style={{ marginTop: 8 }}
                      />
                    </div>
                  </Card>
                </Col>
              ))}
            </Row>
          ) : (
            <Card>
              <AnalyticsChart
                type="heatmap"
                title="内容表现热力图"
                data={{
                  xAxis: ['周一', '周二', '周三', '周四', '周五', '周六', '周日'],
                  yAxis: sortedContent.slice(0, 10).map(c => c.title.substring(0, 20) + '...'),
                  values: sortedContent.slice(0, 10).flatMap((content, y) =>
                    [0, 1, 2, 3, 4, 5, 6].map((x) => [x, y, Math.random() * 100])
                  ),
                  min: 0,
                  max: 100
                }}
                height={400}
                loading={metricsLoading}
              />
            </Card>
          )}
        </TabPane>

        <TabPane tab="作者分析" key="authors">
          <Row gutter={[16, 16]}>
            <Col span={24}>
              <Card title="作者表现排行">
                <Table
                  columns={authorColumns}
                  dataSource={authorPerformance}
                  rowKey="authorId"
                  loading={authorLoading}
                  pagination={{
                    showSizeChanger: true,
                    showTotal: (total) => `共 ${total} 位作者`
                  }}
                />
              </Card>
            </Col>
            <Col xs={24} lg={12}>
              <AnalyticsChart
                type="bar"
                title="作者发文量对比"
                data={{
                  categories: authorPerformance?.slice(0, 10).map(a => a.authorName) || [],
                  series: [{
                    name: '发文量',
                    data: authorPerformance?.slice(0, 10).map(a => a.posts) || []
                  }]
                }}
                height={350}
                loading={authorLoading}
              />
            </Col>
            <Col xs={24} lg={12}>
              <AnalyticsChart
                type="pie"
                title="作者贡献占比"
                data={{
                  items: authorPerformance?.slice(0, 10).map(a => ({
                    name: a.authorName,
                    value: a.totalViews
                  })) || []
                }}
                height={350}
                loading={authorLoading}
              />
            </Col>
          </Row>
        </TabPane>

        <TabPane tab="分类标签" key="categories">
          <Row gutter={[16, 16]}>
            <Col xs={24} lg={12}>
              <Card title="分类表现">
                <Table
                  dataSource={contentPerformance?.categories}
                  rowKey="category"
                  loading={performanceLoading}
                  pagination={false}
                  columns={[
                    {
                      title: '分类',
                      dataIndex: 'category',
                      key: 'category',
                      render: (cat: string) => <Tag color="blue">{cat}</Tag>
                    },
                    {
                      title: '文章数',
                      dataIndex: 'posts',
                      key: 'posts',
                      align: 'right' as const
                    },
                    {
                      title: '总浏览',
                      dataIndex: 'totalViews',
                      key: 'totalViews',
                      align: 'right' as const,
                      render: (views: number) => views.toLocaleString()
                    },
                    {
                      title: '平均浏览',
                      dataIndex: 'avgViews',
                      key: 'avgViews',
                      align: 'right' as const,
                      render: (views: number) => views.toLocaleString()
                    },
                    {
                      title: '互动率',
                      dataIndex: 'engagementRate',
                      key: 'engagementRate',
                      align: 'center' as const,
                      render: (rate: number) => (
                        <Progress percent={rate}  strokeColor="#52c41a" />
                      )
                    }
                  ]}
                />
              </Card>
            </Col>
            <Col xs={24} lg={12}>
              <Card title="标签云">
                <div style={{ minHeight: 300, display: 'flex', flexWrap: 'wrap', gap: 8, alignItems: 'center', justifyContent: 'center' }}>
                  {contentPerformance?.tags?.map((tag, index) => (
                    <Tag
                      key={tag.tag}
                      color={['magenta', 'red', 'volcano', 'orange', 'gold', 'lime', 'green', 'cyan', 'blue', 'geekblue', 'purple'][index % 11]}
                      style={{
                        fontSize: Math.min(24, Math.max(12, tag.views / 1000)),
                        padding: '4px 12px',
                        cursor: 'pointer'
                      }}
                    >
                      {tag.tag} ({tag.posts})
                    </Tag>
                  ))}
                </div>
              </Card>
            </Col>
            <Col span={24}>
              <AnalyticsChart
                type="bar"
                title="分类内容分布"
                data={categoryChartData}
                height={350}
                loading={performanceLoading}
              />
            </Col>
          </Row>
        </TabPane>

        <TabPane tab="互动分析" key="engagement">
          <Row gutter={[16, 16]}>
            <Col xs={24} lg={12}>
              <AnalyticsChart
                type="radar"
                title="互动类型分布"
                data={engagementChartData}
                height={400}
                loading={engagementLoading}
              />
            </Col>
            <Col xs={24} lg={12}>
              <Card title="互动趋势">
                <Row gutter={[16, 16]}>
                  <Col span={12}>
                    <Statistic
                      title="总点赞数"
                      value={engagementData?.likes || 0}
                      prefix={<HeartOutlined />}
                      valueStyle={{ color: '#eb2f96' }}
                    />
                  </Col>
                  <Col span={12}>
                    <Statistic
                      title="总评论数"
                      value={engagementData?.comments || 0}
                      prefix={<CommentOutlined />}
                      valueStyle={{ color: '#1890ff' }}
                    />
                  </Col>
                  <Col span={12}>
                    <Statistic
                      title="总分享数"
                      value={engagementData?.shares || 0}
                      prefix={<ShareAltOutlined />}
                      valueStyle={{ color: '#52c41a' }}
                    />
                  </Col>
                  <Col span={12}>
                    <Statistic
                      title="总收藏数"
                      value={engagementData?.saves || 0}
                      prefix={<TagOutlined />}
                      valueStyle={{ color: '#faad14' }}
                    />
                  </Col>
                </Row>
                <Divider />
                <div>
                  <Text type="secondary">互动率</Text>
                  <Progress
                    percent={engagementData?.engagementRate ? engagementData.engagementRate * 100 : 0}
                    strokeColor={{
                      '0%': '#108ee9',
                      '100%': '#87d068'
                    }}
                    style={{ marginTop: 8 }}
                  />
                </div>
                <div style={{ marginTop: 16 }}>
                  <Text type="secondary">病毒传播指数</Text>
                  <Progress
                    percent={Math.min(100, (engagementData?.viralityScore || 0) * 10)}
                    strokeColor={{
                      '0%': '#fa8c16',
                      '100%': '#f5222d'
                    }}
                    style={{ marginTop: 8 }}
                    format={() => engagementData?.viralityScore?.toFixed(2) || '0'}
                  />
                </div>
              </Card>
            </Col>
            <Col span={24}>
              <Card title="热门内容互动排行">
                <div style={{ overflowX: 'auto' }}>
                  <table style={{ width: '100%', minWidth: 800 }}>
                    <thead>
                      <tr style={{ borderBottom: '1px solid #f0f0f0' }}>
                        <th style={{ padding: '12px', textAlign: 'left' }}>内容</th>
                        <th style={{ padding: '12px', textAlign: 'center' }}>
                          <HeartOutlined /> 点赞
                        </th>
                        <th style={{ padding: '12px', textAlign: 'center' }}>
                          <CommentOutlined /> 评论
                        </th>
                        <th style={{ padding: '12px', textAlign: 'center' }}>
                          <ShareAltOutlined /> 分享
                        </th>
                        <th style={{ padding: '12px', textAlign: 'center' }}>互动总分</th>
                      </tr>
                    </thead>
                    <tbody>
                      {sortedContent.slice(0, 10).map((content, index) => (
                        <tr key={content.contentId} style={{ borderBottom: '1px solid #f0f0f0' }}>
                          <td style={{ padding: '12px' }}>
                            <Space>
                              {index < 3 && <TrophyOutlined style={{ color: ['#ffd700', '#c0c0c0', '#cd7f32'][index] }} />}
                              <Text>{content.title}</Text>
                            </Space>
                          </td>
                          <td style={{ padding: '12px', textAlign: 'center' }}>
                            <Text>{content.likes.toLocaleString()}</Text>
                          </td>
                          <td style={{ padding: '12px', textAlign: 'center' }}>
                            <Text>{content.comments.toLocaleString()}</Text>
                          </td>
                          <td style={{ padding: '12px', textAlign: 'center' }}>
                            <Text>{content.shares.toLocaleString()}</Text>
                          </td>
                          <td style={{ padding: '12px', textAlign: 'center' }}>
                            <Badge
                              count={content.engagementScore}
                              style={{
                                backgroundColor: content.engagementScore > 80 ? '#52c41a' :
                                  content.engagementScore > 50 ? '#1890ff' : '#faad14'
                              }}
                            />
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </Card>
            </Col>
          </Row>
        </TabPane>
      </Tabs>
    </div>
  );
};

export default ContentAnalytics;
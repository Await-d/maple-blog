// @ts-nocheck
import React, { useState, useCallback, useMemo } from 'react';
import {
  Card,
  Table,
  Button,
  Space,
  Tag,
  Typography,
  Input,
  Modal,
  Form,
  message,
  Dropdown,
  Popconfirm,
  Tooltip,
  Badge,
  Select,
  Tree,
  Row,
  Col,
  Statistic,
  Drawer,
  Switch,
  ColorPicker,
  Upload,
  Avatar,
  Divider
} from 'antd';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  MoreOutlined,
  SearchOutlined,
  DragOutlined,
  FileTextOutlined,
  FolderOutlined,
  FolderOpenOutlined,
  PictureOutlined,
  BgColorsOutlined,
  SortAscendingOutlined,
  ReloadOutlined,
  EyeOutlined,
  EyeInvisibleOutlined,
  InfoCircleOutlined,
  ExportOutlined,
  ImportOutlined,
  CopyOutlined,
  ArrowUpOutlined,
  ArrowDownOutlined
} from '@ant-design/icons';
import type { DataNode } from 'antd/es/tree';
import type { Color } from 'antd/es/color-picker';
import dayjs from 'dayjs';

const { Title, Text, Paragraph } = Typography;
const { Search } = Input;
const { Option } = Select;

// Category interface
interface Category {
  id: number;
  name: string;
  slug: string;
  description: string;
  parentId: number | null;
  level: number;
  sort: number;
  isActive: boolean;
  isVisible: boolean;
  color: string;
  icon: string | null;
  image: string | null;
  postCount: number;
  childrenCount: number;
  createdAt: string;
  updatedAt: string;
  path: string;
  children?: Category[];
}

// Mock categories data with hierarchical structure
const mockCategories: Category[] = [
  {
    id: 1,
    name: '前端技术',
    slug: 'frontend',
    description: '前端开发相关技术文章',
    parentId: null,
    level: 0,
    sort: 1,
    isActive: true,
    isVisible: true,
    color: '#1677ff',
    icon: 'CodeOutlined',
    image: 'https://via.placeholder.com/100x60',
    postCount: 25,
    childrenCount: 3,
    createdAt: '2024-01-01T10:00:00Z',
    updatedAt: '2024-01-15T14:30:00Z',
    path: '前端技术',
    children: [
      {
        id: 2,
        name: 'React',
        slug: 'react',
        description: 'React 框架相关文章',
        parentId: 1,
        level: 1,
        sort: 1,
        isActive: true,
        isVisible: true,
        color: '#61dafb',
        icon: null,
        image: null,
        postCount: 12,
        childrenCount: 0,
        createdAt: '2024-01-02T10:00:00Z',
        updatedAt: '2024-01-12T16:20:00Z',
        path: '前端技术 > React'
      },
      {
        id: 3,
        name: 'Vue.js',
        slug: 'vuejs',
        description: 'Vue.js 框架相关文章',
        parentId: 1,
        level: 1,
        sort: 2,
        isActive: true,
        isVisible: true,
        color: '#4fc08d',
        icon: null,
        image: null,
        postCount: 8,
        childrenCount: 0,
        createdAt: '2024-01-03T10:00:00Z',
        updatedAt: '2024-01-10T12:15:00Z',
        path: '前端技术 > Vue.js'
      },
      {
        id: 4,
        name: 'TypeScript',
        slug: 'typescript',
        description: 'TypeScript 开发相关文章',
        parentId: 1,
        level: 1,
        sort: 3,
        isActive: true,
        isVisible: true,
        color: '#3178c6',
        icon: null,
        image: null,
        postCount: 5,
        childrenCount: 0,
        createdAt: '2024-01-04T10:00:00Z',
        updatedAt: '2024-01-08T09:45:00Z',
        path: '前端技术 > TypeScript'
      }
    ]
  },
  {
    id: 5,
    name: '后端技术',
    slug: 'backend',
    description: '后端开发相关技术文章',
    parentId: null,
    level: 0,
    sort: 2,
    isActive: true,
    isVisible: true,
    color: '#52c41a',
    icon: 'DatabaseOutlined',
    image: null,
    postCount: 18,
    childrenCount: 2,
    createdAt: '2024-01-05T10:00:00Z',
    updatedAt: '2024-01-20T11:30:00Z',
    path: '后端技术',
    children: [
      {
        id: 6,
        name: '.NET',
        slug: 'dotnet',
        description: '.NET 开发相关文章',
        parentId: 5,
        level: 1,
        sort: 1,
        isActive: true,
        isVisible: true,
        color: '#512bd4',
        icon: null,
        image: null,
        postCount: 10,
        childrenCount: 0,
        createdAt: '2024-01-06T10:00:00Z',
        updatedAt: '2024-01-18T15:20:00Z',
        path: '后端技术 > .NET'
      },
      {
        id: 7,
        name: 'Node.js',
        slug: 'nodejs',
        description: 'Node.js 开发相关文章',
        parentId: 5,
        level: 1,
        sort: 2,
        isActive: true,
        isVisible: true,
        color: '#68a063',
        icon: null,
        image: null,
        postCount: 8,
        childrenCount: 0,
        createdAt: '2024-01-07T10:00:00Z',
        updatedAt: '2024-01-16T13:40:00Z',
        path: '后端技术 > Node.js'
      }
    ]
  },
  {
    id: 8,
    name: '数据库',
    slug: 'database',
    description: '数据库技术相关文章',
    parentId: null,
    level: 0,
    sort: 3,
    isActive: true,
    isVisible: true,
    color: '#722ed1',
    icon: 'ConsoleSqlOutlined',
    image: null,
    postCount: 12,
    childrenCount: 0,
    createdAt: '2024-01-08T10:00:00Z',
    updatedAt: '2024-01-22T16:15:00Z',
    path: '数据库'
  },
  {
    id: 9,
    name: '系统架构',
    slug: 'architecture',
    description: '系统架构设计相关文章',
    parentId: null,
    level: 0,
    sort: 4,
    isActive: false,
    isVisible: false,
    color: '#fa8c16',
    icon: 'ClusterOutlined',
    image: null,
    postCount: 3,
    childrenCount: 0,
    createdAt: '2024-01-09T10:00:00Z',
    updatedAt: '2024-01-09T10:00:00Z',
    path: '系统架构'
  }
];

interface CategoryFilters {
  search: string;
  status: 'all' | 'active' | 'inactive';
  visibility: 'all' | 'visible' | 'hidden';
  parentId: number | null;
}

const CategoryManagement: React.FC = () => {
  const [categories, setCategories] = useState<Category[]>(mockCategories);
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [filters, setFilters] = useState<CategoryFilters>({
    search: '',
    status: 'all',
    visibility: 'all',
    parentId: null
  });
  const [editModalVisible, setEditModalVisible] = useState(false);
  const [editingCategory, setEditingCategory] = useState<Category | null>(null);
  const [detailDrawerVisible, setDetailDrawerVisible] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState<Category | null>(null);
  const [loading, setLoading] = useState(false);
  const [treeView, setTreeView] = useState(false);
  const [form] = Form.useForm();

  // Build flat category list from hierarchical data
  const flatCategories = useMemo(() => {
    const flatten = (cats: Category[], result: Category[] = []): Category[] => {
      cats.forEach(cat => {
        result.push(cat);
        if (cat.children && cat.children.length > 0) {
          flatten(cat.children, result);
        }
      });
      return result;
    };
    return flatten(categories);
  }, [categories]);

  // Filter categories
  const filteredCategories = useMemo(() => {
    let filtered = [...flatCategories];

    // Text search
    if (filters.search) {
      const searchLower = filters.search.toLowerCase();
      filtered = filtered.filter(cat =>
        cat.name.toLowerCase().includes(searchLower) ||
        cat.description.toLowerCase().includes(searchLower) ||
        cat.slug.toLowerCase().includes(searchLower)
      );
    }

    // Status filter
    if (filters.status !== 'all') {
      filtered = filtered.filter(cat =>
        filters.status === 'active' ? cat.isActive : !cat.isActive
      );
    }

    // Visibility filter
    if (filters.visibility !== 'all') {
      filtered = filtered.filter(cat =>
        filters.visibility === 'visible' ? cat.isVisible : !cat.isVisible
      );
    }

    // Parent filter
    if (filters.parentId !== null) {
      filtered = filtered.filter(cat => cat.parentId === filters.parentId);
    }

    return filtered;
  }, [flatCategories, filters]);

  // Statistics
  const statistics = useMemo(() => {
    const total = flatCategories.length;
    const active = flatCategories.filter(c => c.isActive).length;
    const visible = flatCategories.filter(c => c.isVisible).length;
    const totalPosts = flatCategories.reduce((sum, c) => sum + c.postCount, 0);
    const rootCategories = flatCategories.filter(c => c.parentId === null).length;

    return {
      total,
      active,
      inactive: total - active,
      visible,
      hidden: total - visible,
      totalPosts,
      rootCategories
    };
  }, [flatCategories]);

  // Convert categories to tree data
  const treeData: DataNode[] = useMemo(() => {
    const convertToTree = (cats: Category[]): DataNode[] => {
      return cats.map(cat => ({
        key: cat.id,
        title: (
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              {cat.color && (
                <div
                  className="w-3 h-3 rounded-full"
                  style={{ backgroundColor: cat.color }}
                />
              )}
              <span className={!cat.isActive ? 'opacity-50' : ''}>{cat.name}</span>
              <Badge count={cat.postCount} showZero={false}  />
              {!cat.isActive && <Tag  color="red">停用</Tag>}
              {!cat.isVisible && <Tag  color="orange">隐藏</Tag>}
            </div>
            <Space >
              <Button
                type="text"
                
                icon={<EditOutlined />}
                onClick={(e) => {
                  e.stopPropagation();
                  handleEdit(cat);
                }}
              />
              <Button
                type="text"
                
                icon={<EyeOutlined />}
                onClick={(e) => {
                  e.stopPropagation();
                  handleViewDetail(cat);
                }}
              />
            </Space>
          </div>
        ),
        children: cat.children ? convertToTree(cat.children) : undefined
      }));
    };
    return convertToTree(categories);
  }, [categories]);

  // Handle category actions
  const handleEdit = useCallback((category: Category | null = null) => {
    setEditingCategory(category);
    setEditModalVisible(true);
    if (category) {
      form.setFieldsValue({
        ...category,
        parentId: category.parentId || undefined
      });
    } else {
      form.resetFields();
    }
  }, [form]);

  const handleViewDetail = useCallback((category: Category) => {
    setSelectedCategory(category);
    setDetailDrawerVisible(true);
  }, []);

  const handleSave = useCallback(async (values: any) => {
    setLoading(true);

    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));

      if (editingCategory) {
        // Update existing category
        const updateCategory = (cats: Category[]): Category[] => {
          return cats.map(cat => {
            if (cat.id === editingCategory.id) {
              return {
                ...cat,
                ...values,
                updatedAt: new Date().toISOString(),
                path: values.parentId
                  ? `${flatCategories.find(p => p.id === values.parentId)?.path} > ${values.name}`
                  : values.name
              };
            }
            if (cat.children) {
              return { ...cat, children: updateCategory(cat.children) };
            }
            return cat;
          });
        };
        setCategories(updateCategory(categories));
        message.success('分类已更新');
      } else {
        // Create new category
        const newCategory: Category = {
          id: Date.now(),
          ...values,
          level: values.parentId ? 1 : 0,
          sort: flatCategories.length + 1,
          postCount: 0,
          childrenCount: 0,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          path: values.parentId
            ? `${flatCategories.find(p => p.id === values.parentId)?.path} > ${values.name}`
            : values.name
        };

        if (values.parentId) {
          // Add to parent's children
          const addToParent = (cats: Category[]): Category[] => {
            return cats.map(cat => {
              if (cat.id === values.parentId) {
                return {
                  ...cat,
                  children: [...(cat.children || []), newCategory],
                  childrenCount: (cat.childrenCount || 0) + 1
                };
              }
              if (cat.children) {
                return { ...cat, children: addToParent(cat.children) };
              }
              return cat;
            });
          };
          setCategories(addToParent(categories));
        } else {
          // Add as root category
          setCategories([...categories, newCategory]);
        }
        message.success('分类已创建');
      }

      setEditModalVisible(false);
      setEditingCategory(null);
    } catch (error) {
      message.error('操作失败');
    } finally {
      setLoading(false);
    }
  }, [editingCategory, categories, flatCategories]);

  const handleDelete = useCallback((categoryId: number) => {
    const deleteCategory = (cats: Category[]): Category[] => {
      return cats.filter(cat => {
        if (cat.id === categoryId) {
          return false;
        }
        if (cat.children) {
          cat.children = deleteCategory(cat.children);
          cat.childrenCount = cat.children.length;
        }
        return true;
      });
    };

    setCategories(deleteCategory(categories));
    message.success('分类已删除');
  }, [categories]);

  const handleStatusToggle = useCallback((categoryId: number, field: 'isActive' | 'isVisible') => {
    const updateCategory = (cats: Category[]): Category[] => {
      return cats.map(cat => {
        if (cat.id === categoryId) {
          return {
            ...cat,
            [field]: !cat[field],
            updatedAt: new Date().toISOString()
          };
        }
        if (cat.children) {
          return { ...cat, children: updateCategory(cat.children) };
        }
        return cat;
      });
    };

    setCategories(updateCategory(categories));
    message.success(`分类${field === 'isActive' ? '状态' : '可见性'}已更新`);
  }, [categories]);

  // Table columns
  const columns = [
    {
      title: '分类信息',
      key: 'info',
      width: 300,
      render: (record: Category) => (
        <div className="flex items-center gap-3">
          {record.image ? (
            <Avatar src={record.image} size={40} shape="square" />
          ) : (
            <Avatar
              size={40}
              shape="square"
              style={{ backgroundColor: record.color }}
              icon={<FolderOutlined />}
            />
          )}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <span className="font-medium">{record.name}</span>
              <Tag  color="blue">{record.slug}</Tag>
              {record.level > 0 && (
                <Tag  color="green">L{record.level}</Tag>
              )}
            </div>
            <div className="text-sm text-gray-500 truncate">{record.description}</div>
            <div className="text-xs text-gray-400 mt-1">{record.path}</div>
          </div>
        </div>
      )
    },
    {
      title: '状态',
      key: 'status',
      width: 120,
      render: (record: Category) => (
        <div className="space-y-1">
          <div>
            <Switch
              
              checked={record.isActive}
              onChange={() => handleStatusToggle(record.id, 'isActive')}
              checkedChildren="启用"
              unCheckedChildren="停用"
            />
          </div>
          <div>
            <Switch
              
              checked={record.isVisible}
              onChange={() => handleStatusToggle(record.id, 'isVisible')}
              checkedChildren="显示"
              unCheckedChildren="隐藏"
            />
          </div>
        </div>
      )
    },
    {
      title: '统计',
      key: 'stats',
      width: 100,
      render: (record: Category) => (
        <div className="text-sm">
          <div className="flex justify-between">
            <span className="text-gray-500">文章:</span>
            <span>{record.postCount}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-500">子分类:</span>
            <span>{record.childrenCount}</span>
          </div>
        </div>
      )
    },
    {
      title: '排序',
      key: 'sort',
      width: 80,
      render: (record: Category) => (
        <div className="text-center">
          <Space direction="vertical" >
            <Button type="text"  icon={<ArrowUpOutlined />} />
            <span className="text-xs">{record.sort}</span>
            <Button type="text"  icon={<ArrowDownOutlined />} />
          </Space>
        </div>
      )
    },
    {
      title: '操作',
      key: 'actions',
      width: 120,
      render: (record: Category) => (
        <Space >
          <Tooltip title="查看详情">
            <Button
              type="text"
              
              icon={<EyeOutlined />}
              onClick={() => handleViewDetail(record)}
            />
          </Tooltip>
          <Tooltip title="编辑">
            <Button
              type="text"
              
              icon={<EditOutlined />}
              onClick={() => handleEdit(record)}
            />
          </Tooltip>
          <Dropdown
            menu={{
              items: [
                {
                  key: 'addChild',
                  icon: <PlusOutlined />,
                  label: '添加子分类',
                  onClick: () => {
                    form.setFieldsValue({ parentId: record.id });
                    handleEdit(null);
                  }
                },
                {
                  key: 'copy',
                  icon: <CopyOutlined />,
                  label: '复制分类',
                  onClick: () => message.info('复制分类功能')
                },
                {
                  type: 'divider'
                },
                {
                  key: 'delete',
                  icon: <DeleteOutlined />,
                  label: '删除分类',
                  danger: true,
                  disabled: record.postCount > 0 || record.childrenCount > 0,
                  onClick: () => {
                    Modal.confirm({
                      title: '确认删除',
                      content: '确定要删除这个分类吗？此操作不可恢复。',
                      okType: 'danger',
                      onOk: () => handleDelete(record.id)
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

  return (
    <div className="space-y-6">
      {/* Statistics Cards */}
      <Row gutter={16}>
        <Col span={4}>
          <Card>
            <Statistic
              title="总分类数"
              value={statistics.total}
              prefix={<FolderOutlined />}
              valueStyle={{ color: '#1677ff' }}
            />
          </Card>
        </Col>
        <Col span={4}>
          <Card>
            <Statistic
              title="启用分类"
              value={statistics.active}
              prefix={<FolderOpenOutlined />}
              valueStyle={{ color: '#52c41a' }}
            />
          </Card>
        </Col>
        <Col span={4}>
          <Card>
            <Statistic
              title="停用分类"
              value={statistics.inactive}
              prefix={<FolderOutlined />}
              valueStyle={{ color: '#ff4d4f' }}
            />
          </Card>
        </Col>
        <Col span={4}>
          <Card>
            <Statistic
              title="根分类"
              value={statistics.rootCategories}
              prefix={<FileTextOutlined />}
              valueStyle={{ color: '#722ed1' }}
            />
          </Card>
        </Col>
        <Col span={4}>
          <Card>
            <Statistic
              title="总文章数"
              value={statistics.totalPosts}
              prefix={<FileTextOutlined />}
              valueStyle={{ color: '#fa8c16' }}
            />
          </Card>
        </Col>
        <Col span={4}>
          <Card>
            <Statistic
              title="可见分类"
              value={statistics.visible}
              prefix={<EyeOutlined />}
              valueStyle={{ color: '#13c2c2' }}
            />
          </Card>
        </Col>
      </Row>

      <Card>
        <div className="flex justify-between items-center mb-4">
          <Title level={3} className="mb-0">分类管理</Title>
          <Space>
            <Button
              icon={treeView ? <SortAscendingOutlined /> : <DragOutlined />}
              onClick={() => setTreeView(!treeView)}
            >
              {treeView ? '列表视图' : '树形视图'}
            </Button>
            <Button icon={<PlusOutlined />} type="primary" onClick={() => handleEdit()}>
              新建分类
            </Button>
            <Button icon={<ImportOutlined />}>
              导入分类
            </Button>
            <Button icon={<ExportOutlined />}>
              导出分类
            </Button>
          </Space>
        </div>

        {/* Filters */}
        <div className="flex gap-4 mb-4 flex-wrap">
          <Search
            placeholder="搜索分类名称、描述或别名"
            style={{ width: 300 }}
            value={filters.search}
            onChange={e => setFilters(prev => ({ ...prev, search: e.target.value }))}
            allowClear
          />
          <Select
            placeholder="状态筛选"
            style={{ width: 120 }}
            value={filters.status}
            onChange={value => setFilters(prev => ({ ...prev, status: value }))}
          >
            <Option value="all">全部状态</Option>
            <Option value="active">已启用</Option>
            <Option value="inactive">已停用</Option>
          </Select>
          <Select
            placeholder="可见性筛选"
            style={{ width: 120 }}
            value={filters.visibility}
            onChange={value => setFilters(prev => ({ ...prev, visibility: value }))}
          >
            <Option value="all">全部</Option>
            <Option value="visible">可见</Option>
            <Option value="hidden">隐藏</Option>
          </Select>
          <Select
            placeholder="父分类筛选"
            style={{ width: 150 }}
            value={filters.parentId}
            onChange={value => setFilters(prev => ({ ...prev, parentId: value }))}
            allowClear
          >
            <Option value={null}>根分类</Option>
            {flatCategories.filter(cat => cat.parentId === null).map(cat => (
              <Option key={cat.id} value={cat.id}>{cat.name}</Option>
            ))}
          </Select>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => setFilters({
              search: '',
              status: 'all',
              visibility: 'all',
              parentId: null
            })}
          >
            重置筛选
          </Button>
        </div>

        {/* Content */}
        {treeView ? (
          <Tree
            treeData={treeData}
            defaultExpandAll
            showLine
            showIcon
            blockNode
            className="category-tree"
          />
        ) : (
          <Table
            rowSelection={{
              selectedRowKeys,
              onChange: setSelectedRowKeys
            }}
            columns={columns}
            dataSource={filteredCategories}
            rowKey="id"
            pagination={{
              total: filteredCategories.length,
              pageSize: 10,
              showSizeChanger: true,
              showQuickJumper: true,
              showTotal: (total, range) => `第 ${range[0]}-${range[1]} 条，共 ${total} 条`
            }}
            loading={loading}
          />
        )}
      </Card>

      {/* Edit Modal */}
      <Modal
        title={editingCategory ? '编辑分类' : '新建分类'}
        open={editModalVisible}
        onCancel={() => {
          setEditModalVisible(false);
          setEditingCategory(null);
        }}
        footer={null}
        width={600}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSave}
          initialValues={{
            isActive: true,
            isVisible: true,
            color: '#1677ff',
            sort: flatCategories.length + 1
          }}
        >
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                label="分类名称"
                name="name"
                rules={[{ required: true, message: '请输入分类名称' }]}
              >
                <Input placeholder="请输入分类名称" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                label="URL别名"
                name="slug"
                rules={[{ required: true, message: '请输入URL别名' }]}
              >
                <Input placeholder="请输入URL别名" />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item
            label="分类描述"
            name="description"
          >
            <Input.TextArea rows={3} placeholder="请输入分类描述" />
          </Form.Item>

          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                label="父分类"
                name="parentId"
              >
                <Select placeholder="选择父分类（可选）" allowClear>
                  {flatCategories.filter(cat =>
                    cat.parentId === null &&
                    (!editingCategory || cat.id !== editingCategory.id)
                  ).map(cat => (
                    <Option key={cat.id} value={cat.id}>{cat.name}</Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                label="排序权重"
                name="sort"
                rules={[{ required: true, message: '请输入排序权重' }]}
              >
                <Input type="number" placeholder="数字越小排序越靠前" />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col span={8}>
              <Form.Item
                label="主题色"
                name="color"
              >
                <ColorPicker showText />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item
                label="启用状态"
                name="isActive"
                valuePropName="checked"
              >
                <Switch checkedChildren="启用" unCheckedChildren="停用" />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item
                label="前台可见"
                name="isVisible"
                valuePropName="checked"
              >
                <Switch checkedChildren="可见" unCheckedChildren="隐藏" />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item
            label="分类图标"
            name="image"
          >
            <Upload
              listType="picture-card"
              maxCount={1}
              beforeUpload={() => false}
            >
              <div>
                <PictureOutlined />
                <div style={{ marginTop: 8 }}>上传图标</div>
              </div>
            </Upload>
          </Form.Item>

          <div className="flex justify-end gap-2">
            <Button onClick={() => setEditModalVisible(false)}>
              取消
            </Button>
            <Button type="primary" htmlType="submit" loading={loading}>
              {editingCategory ? '更新' : '创建'}
            </Button>
          </div>
        </Form>
      </Modal>

      {/* Detail Drawer */}
      <Drawer
        title="分类详情"
        placement="right"
        width={500}
        onClose={() => setDetailDrawerVisible(false)}
        open={detailDrawerVisible}
      >
        {selectedCategory && (
          <div className="space-y-6">
            <div className="text-center">
              {selectedCategory.image ? (
                <Avatar src={selectedCategory.image} size={80} shape="square" />
              ) : (
                <Avatar
                  size={80}
                  shape="square"
                  style={{ backgroundColor: selectedCategory.color }}
                  icon={<FolderOutlined />}
                />
              )}
              <Title level={4} className="mt-2 mb-1">{selectedCategory.name}</Title>
              <Text type="secondary">{selectedCategory.slug}</Text>
            </div>

            <Divider />

            <div>
              <Text strong>描述</Text>
              <Paragraph className="mt-1">{selectedCategory.description || '暂无描述'}</Paragraph>
            </div>

            <div>
              <Text strong>分类路径</Text>
              <div className="mt-1">
                <Tag color="blue">{selectedCategory.path}</Tag>
              </div>
            </div>

            <Row gutter={16}>
              <Col span={12}>
                <Statistic title="文章数量" value={selectedCategory.postCount} />
              </Col>
              <Col span={12}>
                <Statistic title="子分类数" value={selectedCategory.childrenCount} />
              </Col>
            </Row>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <Text strong>状态</Text>
                <div className="mt-1">
                  <Tag color={selectedCategory.isActive ? 'green' : 'red'}>
                    {selectedCategory.isActive ? '启用' : '停用'}
                  </Tag>
                </div>
              </div>
              <div>
                <Text strong>可见性</Text>
                <div className="mt-1">
                  <Tag color={selectedCategory.isVisible ? 'blue' : 'orange'}>
                    {selectedCategory.isVisible ? '可见' : '隐藏'}
                  </Tag>
                </div>
              </div>
            </div>

            <div>
              <Text strong>主题色</Text>
              <div className="mt-1 flex items-center gap-2">
                <div
                  className="w-6 h-6 rounded border"
                  style={{ backgroundColor: selectedCategory.color }}
                />
                <Text code>{selectedCategory.color}</Text>
              </div>
            </div>

            <Divider />

            <div className="space-y-2">
              <div>
                <Text strong>创建时间</Text>
                <div>{dayjs(selectedCategory.createdAt).format('YYYY-MM-DD HH:mm:ss')}</div>
              </div>
              <div>
                <Text strong>更新时间</Text>
                <div>{dayjs(selectedCategory.updatedAt).format('YYYY-MM-DD HH:mm:ss')}</div>
              </div>
            </div>

            <div className="flex gap-2">
              <Button type="primary" icon={<EditOutlined />} onClick={() => {
                setDetailDrawerVisible(false);
                handleEdit(selectedCategory);
              }}>
                编辑分类
              </Button>
              <Button icon={<PlusOutlined />} onClick={() => {
                setDetailDrawerVisible(false);
                form.setFieldsValue({ parentId: selectedCategory.id });
                handleEdit(null);
              }}>
                添加子分类
              </Button>
            </div>
          </div>
        )}
      </Drawer>
    </div>
  );
};

export default CategoryManagement;
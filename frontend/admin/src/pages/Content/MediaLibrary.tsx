import React, { useState, useCallback, useMemo } from 'react';
import {
  Card,
  Button,
  Space,
  Typography,
  Input,
  Modal,
  message,
  Dropdown,
  Upload,
  Image,
  Select,
  Tag,
  Progress,
  Tooltip,
  Drawer,
  Form,
  Row,
  Col,
  Statistic,
  Divider,
  Table,
  Badge,
  Tree,
  Radio,
  Slider,
  Switch,
  Alert
} from 'antd';
import {
  PlusOutlined,
  UploadOutlined,
  DeleteOutlined,
  DownloadOutlined,
  EyeOutlined,
  EditOutlined,
  CopyOutlined,
  FolderOutlined,
  FolderOpenOutlined,
  FileImageOutlined,
  PlayCircleOutlined,
  FileOutlined,
  FilePdfOutlined,
  FileWordOutlined,
  FileExcelOutlined,
  FilePptOutlined,
  AudioOutlined,
  CloudUploadOutlined,
  ShareAltOutlined,
  MoreOutlined,
  AppstoreOutlined,
  BarsOutlined,
  FilterOutlined,
  StarOutlined,
  StarFilled,
  LinkOutlined,
  SafetyOutlined} from '@ant-design/icons';
import type { DataNode } from 'antd/es/tree';
import dayjs from 'dayjs';

const { Title, Text, Paragraph } = Typography;
const { Search } = Input;
const { Option } = Select;

// Media file interface
interface MediaFile {
  id: number;
  name: string;
  originalName: string;
  type: 'image' | 'video' | 'audio' | 'document' | 'archive' | 'other';
  mimeType: string;
  extension: string;
  size: number;
  url: string;
  thumbnailUrl?: string;
  folderId: number | null;
  isStarred: boolean;
  isPublic: boolean;
  downloadCount: number;
  viewCount: number;
  tags: string[];
  alt: string;
  description: string;
  metadata: {
    width?: number;
    height?: number;
    duration?: number;
    bitrate?: number;
    fps?: number;
    pages?: number;
  };
  uploadedBy: {
    id: number;
    name: string;
    avatar: string;
  };
  createdAt: string;
  updatedAt: string;
  lastAccessedAt: string;
}

// Folder interface
interface MediaFolder {
  id: number;
  name: string;
  parentId: number | null;
  path: string;
  fileCount: number;
  totalSize: number;
  isPublic: boolean;
  createdAt: string;
  updatedAt: string;
  children?: MediaFolder[];
}

// Mock data
const mockFolders: MediaFolder[] = [
  {
    id: 1,
    name: '文章图片',
    parentId: null,
    path: '文章图片',
    fileCount: 25,
    totalSize: 52428800, // 50MB
    isPublic: true,
    createdAt: '2024-01-01T10:00:00Z',
    updatedAt: '2024-01-15T14:30:00Z',
    children: [
      {
        id: 2,
        name: '2024年',
        parentId: 1,
        path: '文章图片/2024年',
        fileCount: 15,
        totalSize: 31457280, // 30MB
        isPublic: true,
        createdAt: '2024-01-01T10:00:00Z',
        updatedAt: '2024-01-15T14:30:00Z'
      }
    ]
  },
  {
    id: 3,
    name: '用户头像',
    parentId: null,
    path: '用户头像',
    fileCount: 8,
    totalSize: 2097152, // 2MB
    isPublic: false,
    createdAt: '2024-01-02T10:00:00Z',
    updatedAt: '2024-01-10T16:20:00Z'
  },
  {
    id: 4,
    name: '系统资源',
    parentId: null,
    path: '系统资源',
    fileCount: 12,
    totalSize: 10485760, // 10MB
    isPublic: true,
    createdAt: '2024-01-03T10:00:00Z',
    updatedAt: '2024-01-20T11:45:00Z'
  }
];

const mockFiles: MediaFile[] = [
  {
    id: 1,
    name: 'react-19-features.jpg',
    originalName: 'React 19 新特性封面图.jpg',
    type: 'image',
    mimeType: 'image/jpeg',
    extension: 'jpg',
    size: 1572864, // 1.5MB
    url: 'https://via.placeholder.com/800x600/1677ff/ffffff?text=React+19',
    thumbnailUrl: 'https://via.placeholder.com/200x150/1677ff/ffffff?text=React+19',
    folderId: 2,
    isStarred: true,
    isPublic: true,
    downloadCount: 23,
    viewCount: 156,
    tags: ['React', '前端', '技术'],
    alt: 'React 19 新特性介绍',
    description: 'React 19 版本新特性的封面图片',
    metadata: {
      width: 800,
      height: 600
    },
    uploadedBy: {
      id: 1,
      name: '张三',
      avatar: 'https://via.placeholder.com/32'
    },
    createdAt: '2024-01-15T10:30:00Z',
    updatedAt: '2024-01-15T10:30:00Z',
    lastAccessedAt: '2024-01-20T14:22:00Z'
  },
  {
    id: 2,
    name: 'architecture-diagram.png',
    originalName: '系统架构图.png',
    type: 'image',
    mimeType: 'image/png',
    extension: 'png',
    size: 2097152, // 2MB
    url: 'https://via.placeholder.com/1200x800/52c41a/ffffff?text=Architecture',
    thumbnailUrl: 'https://via.placeholder.com/200x150/52c41a/ffffff?text=Architecture',
    folderId: 4,
    isStarred: false,
    isPublic: true,
    downloadCount: 8,
    viewCount: 45,
    tags: ['架构', '系统设计', '技术'],
    alt: '系统架构设计图',
    description: '博客系统的整体架构设计图',
    metadata: {
      width: 1200,
      height: 800
    },
    uploadedBy: {
      id: 2,
      name: '李四',
      avatar: 'https://via.placeholder.com/32'
    },
    createdAt: '2024-01-10T14:20:00Z',
    updatedAt: '2024-01-10T14:20:00Z',
    lastAccessedAt: '2024-01-18T09:15:00Z'
  },
  {
    id: 3,
    name: 'demo-video.mp4',
    originalName: '功能演示视频.mp4',
    type: 'video',
    mimeType: 'video/mp4',
    extension: 'mp4',
    size: 20971520, // 20MB
    url: '/videos/demo-video.mp4',
    thumbnailUrl: 'https://via.placeholder.com/200x150/722ed1/ffffff?text=Video',
    folderId: 1,
    isStarred: true,
    isPublic: false,
    downloadCount: 5,
    viewCount: 28,
    tags: ['演示', '视频', '教程'],
    alt: '功能演示视频',
    description: '系统功能操作演示视频',
    metadata: {
      width: 1920,
      height: 1080,
      duration: 300, // 5 minutes
      bitrate: 2000,
      fps: 30
    },
    uploadedBy: {
      id: 1,
      name: '张三',
      avatar: 'https://via.placeholder.com/32'
    },
    createdAt: '2024-01-12T16:45:00Z',
    updatedAt: '2024-01-12T16:45:00Z',
    lastAccessedAt: '2024-01-19T11:30:00Z'
  },
  {
    id: 4,
    name: 'api-docs.pdf',
    originalName: 'API接口文档.pdf',
    type: 'document',
    mimeType: 'application/pdf',
    extension: 'pdf',
    size: 5242880, // 5MB
    url: '/documents/api-docs.pdf',
    folderId: 4,
    isStarred: false,
    isPublic: true,
    downloadCount: 42,
    viewCount: 89,
    tags: ['文档', 'API', '开发'],
    alt: 'API接口文档',
    description: '系统API接口的详细文档',
    metadata: {
      pages: 156
    },
    uploadedBy: {
      id: 3,
      name: '王五',
      avatar: 'https://via.placeholder.com/32'
    },
    createdAt: '2024-01-08T11:15:00Z',
    updatedAt: '2024-01-16T15:20:00Z',
    lastAccessedAt: '2024-01-21T08:45:00Z'
  }
];

interface MediaFilters {
  search: string;
  type: string;
  folderId: number | null;
  isStarred: boolean | null;
  isPublic: boolean | null;
  uploadedBy: number | null;
  sizeRange: [number, number] | null;
  dateRange: [string, string] | null;
  tags: string[];
}

const MediaLibrary: React.FC = () => {
  const [files, setFiles] = useState<MediaFile[]>(mockFiles);
  const [folders] = useState<MediaFolder[]>(mockFolders);
  const [selectedFileKeys, setSelectedFileKeys] = useState<React.Key[]>([]);
  const [currentFolderId, setCurrentFolderId] = useState<number | null>(null);
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
  const [filters, setFilters] = useState<MediaFilters>({
    search: '',
    type: 'all',
    folderId: null,
    isStarred: null,
    isPublic: null,
    uploadedBy: null,
    sizeRange: null,
    dateRange: null,
    tags: []
  });
  const [uploadModalVisible, setUploadModalVisible] = useState(false);
  const [detailDrawerVisible, setDetailDrawerVisible] = useState(false);
  const [selectedFile, setSelectedFile] = useState<MediaFile | null>(null);
  const [folderModalVisible, setFolderModalVisible] = useState(false);
  const [editingFolder, setEditingFolder] = useState<MediaFolder | null>(null);
  const [previewVisible, setPreviewVisible] = useState(false);
  const [previewFile, setPreviewFile] = useState<MediaFile | null>(null);
  const [filterDrawerVisible, setFilterDrawerVisible] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [uploading, setUploading] = useState(false);
  const [form] = Form.useForm();

  // Get file type icon
  const getFileIcon = useCallback((file: MediaFile) => {
    switch (file.type) {
      case 'image':
        return <FileImageOutlined style={{ color: '#52c41a' }} />;
      case 'video':
        return <PlayCircleOutlined style={{ color: '#722ed1' }} />;
      case 'audio':
        return <AudioOutlined style={{ color: '#fa8c16' }} />;
      case 'document':
        if (file.mimeType.includes('pdf')) return <FilePdfOutlined style={{ color: '#ff4d4f' }} />;
        if (file.mimeType.includes('word')) return <FileWordOutlined style={{ color: '#1677ff' }} />;
        if (file.mimeType.includes('excel')) return <FileExcelOutlined style={{ color: '#52c41a' }} />;
        if (file.mimeType.includes('powerpoint')) return <FilePptOutlined style={{ color: '#fa8c16' }} />;
        return <FileOutlined style={{ color: '#8c8c8c' }} />;
      default:
        return <FileOutlined style={{ color: '#8c8c8c' }} />;
    }
  }, []);

  // Format file size
  const formatFileSize = useCallback((bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }, []);

  // Build flat folder list
  const flatFolders = useMemo(() => {
    const flatten = (folders: MediaFolder[], result: MediaFolder[] = []): MediaFolder[] => {
      folders.forEach(folder => {
        result.push(folder);
        if (folder.children && folder.children.length > 0) {
          flatten(folder.children, result);
        }
      });
      return result;
    };
    return flatten(folders);
  }, [folders]);

  // Filter files
  const filteredFiles = useMemo(() => {
    let filtered = [...files];

    // Folder filter
    if (currentFolderId !== null) {
      filtered = filtered.filter(file => file.folderId === currentFolderId);
    }

    // Search filter
    if (filters.search) {
      const searchLower = filters.search.toLowerCase();
      filtered = filtered.filter(file =>
        file.name.toLowerCase().includes(searchLower) ||
        file.originalName.toLowerCase().includes(searchLower) ||
        file.alt.toLowerCase().includes(searchLower) ||
        file.description.toLowerCase().includes(searchLower) ||
        file.tags.some(tag => tag.toLowerCase().includes(searchLower))
      );
    }

    // Type filter
    if (filters.type !== 'all') {
      filtered = filtered.filter(file => file.type === filters.type);
    }

    // Starred filter
    if (filters.isStarred !== null) {
      filtered = filtered.filter(file => file.isStarred === filters.isStarred);
    }

    // Public filter
    if (filters.isPublic !== null) {
      filtered = filtered.filter(file => file.isPublic === filters.isPublic);
    }

    // Size range filter
    if (filters.sizeRange) {
      const [min, max] = filters.sizeRange;
      filtered = filtered.filter(file => file.size >= min && file.size <= max);
    }

    // Tags filter
    if (filters.tags.length > 0) {
      filtered = filtered.filter(file =>
        filters.tags.every(tag => file.tags.includes(tag))
      );
    }

    return filtered;
  }, [files, currentFolderId, filters]);

  // Statistics
  const statistics = useMemo(() => {
    const totalFiles = files.length;
    const totalSize = files.reduce((sum, file) => sum + file.size, 0);
    const publicFiles = files.filter(f => f.isPublic).length;
    const starredFiles = files.filter(f => f.isStarred).length;
    const totalDownloads = files.reduce((sum, file) => sum + file.downloadCount, 0);
    const totalViews = files.reduce((sum, file) => sum + file.viewCount, 0);

    const typeStats = {
      image: files.filter(f => f.type === 'image').length,
      video: files.filter(f => f.type === 'video').length,
      audio: files.filter(f => f.type === 'audio').length,
      document: files.filter(f => f.type === 'document').length,
      other: files.filter(f => !['image', 'video', 'audio', 'document'].includes(f.type)).length
    };

    return {
      totalFiles,
      totalSize,
      publicFiles,
      starredFiles,
      totalDownloads,
      totalViews,
      typeStats
    };
  }, [files]);

  // Convert folders to tree data
  const folderTreeData: DataNode[] = useMemo(() => {
    const convertToTree = (folders: MediaFolder[]): DataNode[] => {
      return folders.map(folder => ({
        key: folder.id,
        title: (
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <FolderOutlined style={{ color: '#1677ff' }} />
              <span>{folder.name}</span>
              <Badge count={folder.fileCount} showZero  />
            </div>
            <Text type="secondary" className="text-xs">
              {formatFileSize(folder.totalSize)}
            </Text>
          </div>
        ),
        children: folder.children ? convertToTree(folder.children) : undefined
      }));
    };
    return [
      {
        key: 'root',
        title: (
          <div className="flex items-center gap-2">
            <FolderOpenOutlined style={{ color: '#52c41a' }} />
            <span>全部文件</span>
            <Badge count={statistics.totalFiles} showZero  />
          </div>
        ),
        children: convertToTree(folders)
      }
    ];
  }, [folders, statistics.totalFiles, formatFileSize]);

  // Handle file operations
  const handleFileUpload: UploadProps['customRequest'] = useCallback((options) => {
    const { onProgress, onSuccess, onError, file } = options;
    setUploading(true);
    setUploadProgress(0);

    // Simulate upload progress
    const interval = setInterval(() => {
      setUploadProgress(prev => {
        if (prev >= 100) {
          clearInterval(interval);
          setUploading(false);

          // Add new file to list
          const newFile: MediaFile = {
            id: Date.now(),
            name: (file as File).name,
            originalName: (file as File).name,
            type: (file as File).type.startsWith('image/') ? 'image' :
                  (file as File).type.startsWith('video/') ? 'video' :
                  (file as File).type.startsWith('audio/') ? 'audio' :
                  (file as File).type.includes('pdf') ||
                  (file as File).type.includes('document') ||
                  (file as File).type.includes('word') ||
                  (file as File).type.includes('excel') ||
                  (file as File).type.includes('powerpoint') ? 'document' : 'other',
            mimeType: (file as File).type,
            extension: (file as File).name.split('.').pop() || '',
            size: (file as File).size,
            url: URL.createObjectURL(file as File),
            folderId: currentFolderId,
            isStarred: false,
            isPublic: true,
            downloadCount: 0,
            viewCount: 0,
            tags: [],
            alt: '',
            description: '',
            metadata: {},
            uploadedBy: {
              id: 1,
              name: '当前用户',
              avatar: 'https://via.placeholder.com/32'
            },
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
            lastAccessedAt: new Date().toISOString()
          };

          setFiles(prev => [...prev, newFile]);
          message.success(`文件 ${(file as File).name} 上传成功`);
          onSuccess?.(newFile);
          return 100;
        }
        onProgress?.({ percent: prev + 10 });
        return prev + 10;
      });
    }, 200);

    return {
      abort() {
        clearInterval(interval);
        setUploading(false);
        onError?.(new Error('Upload aborted'));
      }
    };
  }, [currentFolderId]);

  const handleStarToggle = useCallback((fileId: number) => {
    setFiles(prev => prev.map(file =>
      file.id === fileId
        ? { ...file, isStarred: !file.isStarred }
        : file
    ));
    message.success('收藏状态已更新');
  }, []);

  const handleFileDelete = useCallback((fileId: number) => {
    setFiles(prev => prev.filter(file => file.id !== fileId));
    message.success('文件已删除');
  }, []);

  const handleFileView = useCallback((file: MediaFile) => {
    setSelectedFile(file);
    setDetailDrawerVisible(true);

    // Update view count
    setFiles(prev => prev.map(f =>
      f.id === file.id
        ? { ...f, viewCount: f.viewCount + 1, lastAccessedAt: new Date().toISOString() }
        : f
    ));
  }, []);

  const handleFilePreview = useCallback((file: MediaFile) => {
    if (file.type === 'image') {
      setPreviewFile(file);
      setPreviewVisible(true);
    } else {
      // For non-image files, open in new tab
      window.open(file.url, '_blank');
    }
  }, []);

  const handleFolderSelect = useCallback((selectedKeys: React.Key[]) => {
    const folderId = selectedKeys[0];
    setCurrentFolderId(folderId === 'root' ? null : folderId as number);
  }, []);

  // Grid view file card
  const FileCard: React.FC<{ file: MediaFile }> = ({ file }) => (
    <Card
      hoverable
      className="file-card"
      cover={
        <div className="relative h-32 bg-gray-50 flex items-center justify-center">
          {file.type === 'image' ? (
            <Image
              src={file.thumbnailUrl || file.url}
              alt={file.alt}
              className="max-h-full max-w-full object-cover"
              preview={false}
              onClick={() => handleFilePreview(file)}
            />
          ) : (
            <div className="text-4xl">
              {getFileIcon(file)}
            </div>
          )}
          <div className="absolute top-2 right-2 flex gap-1">
            {file.isStarred && (
              <StarFilled className="text-yellow-500" />
            )}
            {!file.isPublic && (
              <SafetyOutlined className="text-red-500" />
            )}
          </div>
        </div>
      }
      actions={[
        <Tooltip key="view" title="查看详情">
          <EyeOutlined onClick={() => handleFileView(file)} />
        </Tooltip>,
        <Tooltip key="star" title={file.isStarred ? '取消收藏' : '收藏'}>
          {file.isStarred ? (
            <StarFilled className="text-yellow-500" onClick={() => handleStarToggle(file.id)} />
          ) : (
            <StarOutlined onClick={() => handleStarToggle(file.id)} />
          )}
        </Tooltip>,
        <Tooltip key="download" title="下载">
          <DownloadOutlined onClick={() => {
            // Simulate download
            setFiles(prev => prev.map(f =>
              f.id === file.id
                ? { ...f, downloadCount: f.downloadCount + 1 }
                : f
            ));
            message.success('开始下载');
          }} />
        </Tooltip>,
        <Dropdown
          key="actions"
          menu={{
            items: [
              {
                key: 'copy',
                icon: <CopyOutlined />,
                label: '复制链接',
                onClick: () => {
                  navigator.clipboard.writeText(file.url);
                  message.success('链接已复制');
                }
              },
              {
                key: 'edit',
                icon: <EditOutlined />,
                label: '编辑信息',
                onClick: () => handleFileView(file)
              },
              {
                key: 'share',
                icon: <ShareAltOutlined />,
                label: '分享文件',
                onClick: () => message.info('分享功能')
              },
              {
                type: 'divider'
              },
              {
                key: 'delete',
                icon: <DeleteOutlined />,
                label: '删除文件',
                danger: true,
                onClick: () => {
                  Modal.confirm({
                    title: '确认删除',
                    content: '确定要删除这个文件吗？此操作不可恢复。',
                    okType: 'danger',
                    onOk: () => handleFileDelete(file.id)
                  });
                }
              }
            ]
          }}
          trigger={['click']}
        >
          <MoreOutlined />
        </Dropdown>
      ]}
    >
      <Card.Meta
        title={
          <Tooltip title={file.originalName}>
            <div className="truncate">{file.name}</div>
          </Tooltip>
        }
        description={
          <div className="space-y-1">
            <div className="flex justify-between text-xs">
              <span>{formatFileSize(file.size)}</span>
              <span>{file.extension.toUpperCase()}</span>
            </div>
            <div className="flex gap-1 flex-wrap">
              {file.tags.slice(0, 2).map(tag => (
                <Tag key={tag} >{tag}</Tag>
              ))}
              {file.tags.length > 2 && (
                <Tag >+{file.tags.length - 2}</Tag>
              )}
            </div>
          </div>
        }
      />
    </Card>
  );

  // List view columns
  const listColumns = [
    {
      title: '文件',
      key: 'file',
      render: (record: MediaFile) => (
        <div className="flex items-center gap-3">
          {record.type === 'image' ? (
            <Image
              src={record.thumbnailUrl || record.url}
              alt={record.alt}
              width={40}
              height={30}
              className="rounded object-cover"
              preview={false}
            />
          ) : (
            <div className="w-10 h-8 flex items-center justify-center bg-gray-50 rounded">
              {getFileIcon(record)}
            </div>
          )}
          <div className="flex-1 min-w-0">
            <div className="font-medium truncate">{record.name}</div>
            <div className="text-sm text-gray-500">{record.originalName}</div>
          </div>
        </div>
      )
    },
    {
      title: '类型',
      key: 'type',
      width: 80,
      render: (record: MediaFile) => (
        <Tag>{record.extension.toUpperCase()}</Tag>
      )
    },
    {
      title: '大小',
      key: 'size',
      width: 100,
      render: (record: MediaFile) => formatFileSize(record.size)
    },
    {
      title: '状态',
      key: 'status',
      width: 120,
      render: (record: MediaFile) => (
        <div className="flex flex-col gap-1">
          {record.isStarred && <Tag color="gold" >收藏</Tag>}
          <Tag color={record.isPublic ? 'green' : 'red'} >
            {record.isPublic ? '公开' : '私有'}
          </Tag>
        </div>
      )
    },
    {
      title: '下载/查看',
      key: 'stats',
      width: 100,
      render: (record: MediaFile) => (
        <div className="text-xs">
          <div>{record.downloadCount} 下载</div>
          <div>{record.viewCount} 查看</div>
        </div>
      )
    },
    {
      title: '上传时间',
      key: 'createdAt',
      width: 120,
      render: (record: MediaFile) => dayjs(record.createdAt).format('MM-DD HH:mm')
    },
    {
      title: '操作',
      key: 'actions',
      width: 120,
      render: (record: MediaFile) => (
        <Space >
          <Button type="text"  icon={<EyeOutlined />} onClick={() => handleFileView(record)} />
          <Button
            type="text"
            
            icon={record.isStarred ? <StarFilled className="text-yellow-500" /> : <StarOutlined />}
            onClick={() => handleStarToggle(record.id)}
          />
          <Button type="text"  icon={<DownloadOutlined />} />
        </Space>
      )
    }
  ];

  return (
    <div className="flex h-full">
      {/* Folder Tree Sidebar */}
      <div className="w-64 border-r bg-white p-4">
        <div className="flex justify-between items-center mb-4">
          <Title level={5} className="mb-0">文件夹</Title>
          <Button
            type="text"
            
            icon={<PlusOutlined />}
            onClick={() => {
              setEditingFolder(null);
              setFolderModalVisible(true);
            }}
          />
        </div>
        <Tree
          treeData={folderTreeData}
          defaultExpandAll
          onSelect={handleFolderSelect}
          selectedKeys={currentFolderId ? [currentFolderId] : ['root']}
        />
      </div>

      {/* Main Content */}
      <div className="flex-1 flex flex-col">
        {/* Statistics Bar */}
        <div className="bg-white border-b p-4">
          <Row gutter={16}>
            <Col span={4}>
              <Statistic
                title="总文件"
                value={statistics.totalFiles}
                prefix={<FileOutlined />}
                valueStyle={{ fontSize: '16px' }}
              />
            </Col>
            <Col span={4}>
              <Statistic
                title="总大小"
                value={formatFileSize(statistics.totalSize)}
                prefix={<CloudUploadOutlined />}
                valueStyle={{ fontSize: '16px' }}
              />
            </Col>
            <Col span={4}>
              <Statistic
                title="图片"
                value={statistics.typeStats.image}
                prefix={<FileImageOutlined />}
                valueStyle={{ fontSize: '16px', color: '#52c41a' }}
              />
            </Col>
            <Col span={4}>
              <Statistic
                title="视频"
                value={statistics.typeStats.video}
                prefix={<PlayCircleOutlined />}
                valueStyle={{ fontSize: '16px', color: '#722ed1' }}
              />
            </Col>
            <Col span={4}>
              <Statistic
                title="文档"
                value={statistics.typeStats.document}
                prefix={<FilePdfOutlined />}
                valueStyle={{ fontSize: '16px', color: '#ff4d4f' }}
              />
            </Col>
            <Col span={4}>
              <Statistic
                title="收藏"
                value={statistics.starredFiles}
                prefix={<StarOutlined />}
                valueStyle={{ fontSize: '16px', color: '#faad14' }}
              />
            </Col>
          </Row>
        </div>

        {/* Toolbar */}
        <div className="bg-white border-b p-4">
          <div className="flex justify-between items-center mb-4">
            <div className="flex items-center gap-4">
              <Search
                placeholder="搜索文件名、标签或描述"
                style={{ width: 300 }}
                value={filters.search}
                onChange={e => setFilters(prev => ({ ...prev, search: e.target.value }))}
                allowClear
              />
              <Select
                placeholder="文件类型"
                style={{ width: 120 }}
                value={filters.type}
                onChange={value => setFilters(prev => ({ ...prev, type: value }))}
              >
                <Option value="all">全部类型</Option>
                <Option value="image">图片</Option>
                <Option value="video">视频</Option>
                <Option value="audio">音频</Option>
                <Option value="document">文档</Option>
                <Option value="other">其他</Option>
              </Select>
              <Button
                icon={<FilterOutlined />}
                onClick={() => setFilterDrawerVisible(true)}
              >
                高级筛选
              </Button>
            </div>
            <Space>
              <Radio.Group
                value={viewMode}
                onChange={e => setViewMode(e.target.value)}
              >
                <Radio.Button value="grid" icon={<AppstoreOutlined />}>
                  <AppstoreOutlined />
                </Radio.Button>
                <Radio.Button value="list" icon={<BarsOutlined />}>
                  <BarsOutlined />
                </Radio.Button>
              </Radio.Group>
              <Button
                icon={<UploadOutlined />}
                type="primary"
                onClick={() => setUploadModalVisible(true)}
              >
                上传文件
              </Button>
            </Space>
          </div>

          {/* Current folder breadcrumb */}
          {currentFolderId && (
            <div className="text-sm text-gray-500">
              当前位置: {flatFolders.find(f => f.id === currentFolderId)?.path}
            </div>
          )}
        </div>

        {/* File List */}
        <div className="flex-1 p-4 bg-gray-50">
          {viewMode === 'grid' ? (
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6 gap-4">
              {filteredFiles.map(file => (
                <FileCard key={file.id} file={file} />
              ))}
            </div>
          ) : (
            <Table
              rowSelection={{
                selectedRowKeys: selectedFileKeys,
                onChange: setSelectedFileKeys
              }}
              columns={listColumns}
              dataSource={filteredFiles}
              rowKey="id"
              pagination={{
                total: filteredFiles.length,
                pageSize: 20,
                showSizeChanger: true,
                showQuickJumper: true,
                showTotal: (total, range) => `第 ${range[0]}-${range[1]} 条，共 ${total} 条`
              }}
            />
          )}
        </div>
      </div>

      {/* Upload Modal */}
      <Modal
        title="上传文件"
        open={uploadModalVisible}
        onCancel={() => setUploadModalVisible(false)}
        footer={null}
        width={600}
      >
        <div className="space-y-4">
          <Upload.Dragger
            multiple
            customRequest={handleFileUpload}
            showUploadList={false}
            className="mb-4"
          >
            <p className="ant-upload-drag-icon">
              <CloudUploadOutlined />
            </p>
            <p className="ant-upload-text">点击或拖拽文件到此区域上传</p>
            <p className="ant-upload-hint">
              支持单个或批量上传。支持图片、视频、音频、文档等格式
            </p>
          </Upload.Dragger>

          {uploading && (
            <div>
              <Progress percent={uploadProgress} status="active" />
              <div className="text-center text-sm text-gray-500 mt-2">
                上传中... {uploadProgress}%
              </div>
            </div>
          )}

          <Alert
            message="上传提示"
            description="单个文件最大支持 100MB，支持的格式包括：图片(jpg, png, gif, webp)、视频(mp4, avi, mov)、音频(mp3, wav, aac)、文档(pdf, doc, docx, xls, xlsx, ppt, pptx)等。"
            type="info"
            showIcon
          />
        </div>
      </Modal>

      {/* File Detail Drawer */}
      <Drawer
        title="文件详情"
        placement="right"
        width={500}
        onClose={() => setDetailDrawerVisible(false)}
        open={detailDrawerVisible}
      >
        {selectedFile && (
          <div className="space-y-6">
            {/* File Preview */}
            <div className="text-center">
              {selectedFile.type === 'image' ? (
                <Image
                  src={selectedFile.url}
                  alt={selectedFile.alt}
                  className="max-w-full"
                  style={{ maxHeight: '200px' }}
                />
              ) : (
                <div className="h-32 bg-gray-50 flex items-center justify-center text-6xl">
                  {getFileIcon(selectedFile)}
                </div>
              )}
            </div>

            <Divider />

            {/* Basic Info */}
            <div>
              <Title level={5}>基本信息</Title>
              <div className="space-y-2">
                <div className="flex justify-between">
                  <Text strong>文件名:</Text>
                  <Text copyable>{selectedFile.name}</Text>
                </div>
                <div className="flex justify-between">
                  <Text strong>原始名称:</Text>
                  <Text>{selectedFile.originalName}</Text>
                </div>
                <div className="flex justify-between">
                  <Text strong>文件大小:</Text>
                  <Text>{formatFileSize(selectedFile.size)}</Text>
                </div>
                <div className="flex justify-between">
                  <Text strong>文件类型:</Text>
                  <Tag>{selectedFile.extension.toUpperCase()}</Tag>
                </div>
                <div className="flex justify-between">
                  <Text strong>MIME类型:</Text>
                  <Text code>{selectedFile.mimeType}</Text>
                </div>
              </div>
            </div>

            {/* Metadata */}
            {Object.keys(selectedFile.metadata).length > 0 && (
              <div>
                <Title level={5}>文件属性</Title>
                <div className="space-y-2">
                  {selectedFile.metadata.width && selectedFile.metadata.height && (
                    <div className="flex justify-between">
                      <Text strong>尺寸:</Text>
                      <Text>{selectedFile.metadata.width} × {selectedFile.metadata.height}</Text>
                    </div>
                  )}
                  {selectedFile.metadata.duration && (
                    <div className="flex justify-between">
                      <Text strong>时长:</Text>
                      <Text>{Math.floor(selectedFile.metadata.duration / 60)}:{(selectedFile.metadata.duration % 60).toString().padStart(2, '0')}</Text>
                    </div>
                  )}
                  {selectedFile.metadata.pages && (
                    <div className="flex justify-between">
                      <Text strong>页数:</Text>
                      <Text>{selectedFile.metadata.pages}</Text>
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* Statistics */}
            <div>
              <Title level={5}>使用统计</Title>
              <Row gutter={16}>
                <Col span={8}>
                  <Statistic title="下载次数" value={selectedFile.downloadCount} />
                </Col>
                <Col span={8}>
                  <Statistic title="查看次数" value={selectedFile.viewCount} />
                </Col>
                <Col span={8}>
                  <Statistic
                    title="收藏状态"
                    value={selectedFile.isStarred ? "已收藏" : "未收藏"}
                    valueStyle={{ color: selectedFile.isStarred ? '#faad14' : '#8c8c8c' }}
                  />
                </Col>
              </Row>
            </div>

            {/* Tags and Description */}
            <div>
              <Title level={5}>标签和描述</Title>
              <div className="space-y-3">
                <div>
                  <Text strong>标签:</Text>
                  <div className="mt-1">
                    {selectedFile.tags.length > 0 ? (
                      selectedFile.tags.map(tag => (
                        <Tag key={tag}>{tag}</Tag>
                      ))
                    ) : (
                      <Text type="secondary">暂无标签</Text>
                    )}
                  </div>
                </div>
                <div>
                  <Text strong>ALT文本:</Text>
                  <Paragraph className="mt-1">{selectedFile.alt || '暂无ALT文本'}</Paragraph>
                </div>
                <div>
                  <Text strong>描述:</Text>
                  <Paragraph className="mt-1">{selectedFile.description || '暂无描述'}</Paragraph>
                </div>
              </div>
            </div>

            {/* Time Info */}
            <div>
              <Title level={5}>时间信息</Title>
              <div className="space-y-2">
                <div>
                  <Text strong>上传时间:</Text>
                  <div>{dayjs(selectedFile.createdAt).format('YYYY-MM-DD HH:mm:ss')}</div>
                </div>
                <div>
                  <Text strong>最后修改:</Text>
                  <div>{dayjs(selectedFile.updatedAt).format('YYYY-MM-DD HH:mm:ss')}</div>
                </div>
                <div>
                  <Text strong>最后访问:</Text>
                  <div>{dayjs(selectedFile.lastAccessedAt).format('YYYY-MM-DD HH:mm:ss')}</div>
                </div>
              </div>
            </div>

            {/* Actions */}
            <div className="flex gap-2">
              <Button type="primary" icon={<DownloadOutlined />}>
                下载文件
              </Button>
              <Button icon={<LinkOutlined />}>
                复制链接
              </Button>
              <Button icon={<ShareAltOutlined />}>
                分享文件
              </Button>
            </div>
          </div>
        )}
      </Drawer>

      {/* Advanced Filter Drawer */}
      <Drawer
        title="高级筛选"
        placement="right"
        width={400}
        onClose={() => setFilterDrawerVisible(false)}
        open={filterDrawerVisible}
      >
        <Form layout="vertical">
          <Form.Item label="收藏状态">
            <Select
              placeholder="选择收藏状态"
              value={filters.isStarred}
              onChange={value => setFilters(prev => ({ ...prev, isStarred: value }))}
              allowClear
            >
              <Option value={true}>已收藏</Option>
              <Option value={false}>未收藏</Option>
            </Select>
          </Form.Item>

          <Form.Item label="公开状态">
            <Select
              placeholder="选择公开状态"
              value={filters.isPublic}
              onChange={value => setFilters(prev => ({ ...prev, isPublic: value }))}
              allowClear
            >
              <Option value={true}>公开</Option>
              <Option value={false}>私有</Option>
            </Select>
          </Form.Item>

          <Form.Item label="文件大小范围 (MB)">
            <Slider
              range
              min={0}
              max={100}
              onChange={(value) => setFilters(prev => ({
                ...prev,
                sizeRange: [value[0] * 1024 * 1024, value[1] * 1024 * 1024]
              }))}
            />
          </Form.Item>

          <Form.Item label="标签筛选">
            <Select
              mode="multiple"
              placeholder="选择标签"
              value={filters.tags}
              onChange={value => setFilters(prev => ({ ...prev, tags: value }))}
            >
              <Option value="React">React</Option>
              <Option value="前端">前端</Option>
              <Option value="技术">技术</Option>
              <Option value="架构">架构</Option>
              <Option value="文档">文档</Option>
              <Option value="演示">演示</Option>
              <Option value="视频">视频</Option>
              <Option value="教程">教程</Option>
            </Select>
          </Form.Item>

          <div className="flex gap-2">
            <Button
              type="primary"
              onClick={() => setFilterDrawerVisible(false)}
            >
              应用筛选
            </Button>
            <Button
              onClick={() => {
                setFilters({
                  search: '',
                  type: 'all',
                  folderId: null,
                  isStarred: null,
                  isPublic: null,
                  uploadedBy: null,
                  sizeRange: null,
                  dateRange: null,
                  tags: []
                });
                setFilterDrawerVisible(false);
              }}
            >
              重置筛选
            </Button>
          </div>
        </Form>
      </Drawer>

      {/* Image Preview Modal */}
      <Image.PreviewGroup
        preview={{
          visible: previewVisible,
          onVisibleChange: setPreviewVisible,
          current: previewFile?.url
        }}
      >
        {previewFile && (
          <Image src={previewFile.url} style={{ display: 'none' }} />
        )}
      </Image.PreviewGroup>

      {/* Folder Management Modal */}
      <Modal
        title={editingFolder ? '编辑文件夹' : '新建文件夹'}
        open={folderModalVisible}
        onCancel={() => setFolderModalVisible(false)}
        footer={null}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={(_values) => {
            // Handle folder creation/editing
            message.success(editingFolder ? '文件夹已更新' : '文件夹已创建');
            setFolderModalVisible(false);
          }}
        >
          <Form.Item
            label="文件夹名称"
            name="name"
            rules={[{ required: true, message: '请输入文件夹名称' }]}
          >
            <Input placeholder="请输入文件夹名称" />
          </Form.Item>

          <Form.Item
            label="父文件夹"
            name="parentId"
          >
            <Select placeholder="选择父文件夹（可选）" allowClear>
              {flatFolders.map(folder => (
                <Option key={folder.id} value={folder.id}>{folder.path}</Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item
            label="公开访问"
            name="isPublic"
            valuePropName="checked"
          >
            <Switch checkedChildren="公开" unCheckedChildren="私有" />
          </Form.Item>

          <div className="flex justify-end gap-2">
            <Button onClick={() => setFolderModalVisible(false)}>
              取消
            </Button>
            <Button type="primary" htmlType="submit">
              {editingFolder ? '更新' : '创建'}
            </Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

export default MediaLibrary;
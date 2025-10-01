import React, { useState } from 'react';
import {
  Button,
  Card,
  Form,
  Input,
  Modal,
  Popconfirm,
  Space,
  Switch,
  Table,
  Tag as AntTag,
  Typography,
  message,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import tagService from '@/services/tag.service';
import type { Tag, CreateTagInput, UpdateTagInput } from '@/types';

const { Title, Text } = Typography;

const generateSlug = (value: string) =>
  value
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-');

const TagManagement: React.FC = () => {
  const queryClient = useQueryClient();
  const [form] = Form.useForm();
  const [modalVisible, setModalVisible] = useState(false);
  const [editingTag, setEditingTag] = useState<Tag | null>(null);

  const { data: tags = [], isLoading } = useQuery({
    queryKey: ['admin-tags'],
    queryFn: () => tagService.getTags(),
    staleTime: 300_000,
  });

  const createMutation = useMutation({
    mutationFn: (payload: CreateTagInput) => tagService.createTag(payload),
    onSuccess: () => {
      message.success('标签创建成功');
      queryClient.invalidateQueries({ queryKey: ['admin-tags'] });
      setModalVisible(false);
      form.resetFields();
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '创建标签失败，请稍后重试';
      message.error(errMsg);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateTagInput }) => tagService.updateTag(id, payload),
    onSuccess: () => {
      message.success('标签更新成功');
      queryClient.invalidateQueries({ queryKey: ['admin-tags'] });
      setModalVisible(false);
      setEditingTag(null);
      form.resetFields();
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '更新标签失败，请稍后重试';
      message.error(errMsg);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => tagService.deleteTag(id),
    onSuccess: () => {
      message.success('标签已删除');
      queryClient.invalidateQueries({ queryKey: ['admin-tags'] });
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '删除标签失败，请稍后重试';
      message.error(errMsg);
    },
  });

  const openCreateModal = () => {
    setEditingTag(null);
    form.resetFields();
    form.setFieldsValue({ isActive: true });
    setModalVisible(true);
  };

  const openEditModal = (tag: Tag) => {
    setEditingTag(tag);
    form.setFieldsValue({
      name: tag.name,
      slug: tag.slug,
      description: tag.description,
      color: tag.color,
      isActive: tag.isActive,
    });
    setModalVisible(true);
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      const payload: CreateTagInput | UpdateTagInput = {
        ...values,
        slug: values.slug || generateSlug(values.name),
      };

      if (editingTag) {
        updateMutation.mutate({ id: editingTag.id, payload });
      } else {
        createMutation.mutate(payload as CreateTagInput);
      }
    } catch (error) {
      if (error instanceof Error) {
        message.error(error.message);
      }
    }
  };

  const handleModalCancel = () => {
    setModalVisible(false);
    setEditingTag(null);
    form.resetFields();
  };

  const columns = [
    {
      title: '标签名称',
      dataIndex: 'name',
      key: 'name',
      render: (_: unknown, record: Tag) => (
        <Space direction="vertical" size={0}>
          <Space>
            <AntTag color={record.color || 'default'}>{record.name}</AntTag>
            {!record.isActive && <AntTag color="default">禁用</AntTag>}
          </Space>
          {record.description && <Text type="secondary">{record.description}</Text>}
        </Space>
      ),
    },
    {
      title: 'Slug',
      dataIndex: 'slug',
      key: 'slug',
      render: (value: string) => <Text code>{value}</Text>,
    },
    {
      title: '文章数',
      dataIndex: 'postCount',
      key: 'postCount',
      width: 120,
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 200,
      render: (value: string) => dayjs(value).format('YYYY-MM-DD HH:mm'),
    },
    {
      title: '操作',
      key: 'actions',
      width: 160,
      render: (_: unknown, record: Tag) => (
        <Space size="small">
          <Button type="link" icon={<EditOutlined />} onClick={() => openEditModal(record)}>
            编辑
          </Button>
          <Popconfirm
            title="确认删除该标签?"
            okText="删除"
            cancelText="取消"
            onConfirm={() => deleteMutation.mutate(record.id)}
          >
            <Button type="link" danger icon={<DeleteOutlined />}>
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div className="page-header">
        <div>
          <Title level={2} className="page-title">
            标签管理
          </Title>
          <Text className="page-description">维护文章标签，帮助内容分类与检索</Text>
        </div>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreateModal}>
          新建标签
        </Button>
      </div>

      <Card className="page-container" bordered={false}>
        <Table<Tag>
          rowKey="id"
          loading={isLoading}
          columns={columns}
          dataSource={tags}
          pagination={{ pageSize: 10 }}
        />
      </Card>

      <Modal
        title={editingTag ? '编辑标签' : '新建标签'}
        open={modalVisible}
        onCancel={handleModalCancel}
        onOk={handleSubmit}
        confirmLoading={createMutation.isLoading || updateMutation.isLoading}
        destroyOnClose
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="name"
            label="标签名称"
            rules={[{ required: true, message: '请输入标签名称' }]}
          >
            <Input placeholder="例如：前端" onBlur={(event) => {
              const value = event.target.value;
              if (!form.getFieldValue('slug') && value) {
                form.setFieldsValue({ slug: generateSlug(value) });
              }
            }} />
          </Form.Item>

          <Form.Item
            name="slug"
            label="Slug"
            rules={[{ required: true, message: '请输入标签 Slug' }]}
          >
            <Input placeholder="例如：frontend" />
          </Form.Item>

          <Form.Item name="description" label="描述">
            <Input.TextArea rows={3} placeholder="可选，描述标签用途" />
          </Form.Item>

          <Form.Item name="color" label="颜色">
            <Input placeholder="可选，支持 CSS 颜色值，例如 #722ed1" />
          </Form.Item>

          <Form.Item
            name="isActive"
            label="是否启用"
            valuePropName="checked"
          >
            <Switch checkedChildren="启用" unCheckedChildren="停用" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default TagManagement;

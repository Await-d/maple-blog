import React, { useEffect } from 'react';
import {
  Button,
  Card,
  Form,
  Input,
  Select,
  Space,
  Switch,
  Typography,
  message,
} from 'antd';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import postService from '@/services/post.service';
import tagService from '@/services/tag.service';
import categoryService from '@/services/category.service';
import type {
  Tag,
  Category,
  CreatePostInput,
  UpdatePostInput,
  PostStatus,
} from '@/types';
import { PostStatus as PostStatusEnum } from '@/types';

const { Title, Text } = Typography;
const { Option } = Select;

const slugify = (value: string) =>
  value
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-');

const statusOptions: Array<{ label: string; value: PostStatus }> = [
  { label: '草稿', value: PostStatusEnum.Draft },
  { label: '已发布', value: PostStatusEnum.Published },
  { label: '计划发布', value: PostStatusEnum.Scheduled },
  { label: '已归档', value: PostStatusEnum.Archived },
];

const PostEditor: React.FC = () => {
  const { id: postId } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form] = Form.useForm();
  const isEdit = Boolean(postId);

  const { data: categories = [], isLoading: categoriesLoading } = useQuery({
    queryKey: ['admin-categories'],
    queryFn: () => categoryService.getCategories(),
    staleTime: 300_000,
  });

  const { data: tags = [], isLoading: tagsLoading } = useQuery({
    queryKey: ['admin-tags'],
    queryFn: () => tagService.getTags(),
    staleTime: 300_000,
  });

  const postQuery = useQuery({
    queryKey: ['admin-post', postId],
    queryFn: () => postService.getPostById(postId as string),
    enabled: isEdit,
    onSuccess: (post) => {
      form.setFieldsValue({
        title: post.title,
        slug: post.slug,
        excerpt: post.excerpt,
        content: post.content,
        status: post.status,
        categoryId: post.category?.id,
        tagIds: post.tags.map((tag) => tag.id),
        featuredImage: post.featuredImage,
        featured: post.featured ?? false,
      });
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '加载文章信息失败';
      message.error(errMsg);
    },
  });

  useEffect(() => {
    if (!isEdit) {
      form.setFieldsValue({ status: PostStatusEnum.Draft, tagIds: [], featured: false });
    }
  }, [form, isEdit]);

  const createMutation = useMutation({
    mutationFn: (payload: CreatePostInput) => postService.createPost(payload),
    onSuccess: () => {
      message.success('文章创建成功');
      queryClient.invalidateQueries({ queryKey: ['admin-posts'] });
      navigate('/content/posts');
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '创建文章失败，请稍后重试';
      message.error(errMsg);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdatePostInput }) => postService.updatePost(id, payload),
    onSuccess: () => {
      message.success('文章更新成功');
      queryClient.invalidateQueries({ queryKey: ['admin-posts'] });
      queryClient.invalidateQueries({ queryKey: ['admin-post', postId] });
      navigate('/content/posts');
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '更新文章失败，请稍后重试';
      message.error(errMsg);
    },
  });

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      const payload: CreatePostInput | UpdatePostInput = {
        title: values.title,
        slug: values.slug || slugify(values.title),
        excerpt: values.excerpt,
        content: values.content,
        status: values.status,
        categoryId: values.categoryId,
        tagIds: values.tagIds || [],
        featuredImage: values.featuredImage,
        featured: values.featured,
        seoTitle: values.seoTitle,
        seoDescription: values.seoDescription,
      };

      if (isEdit && postId) {
        updateMutation.mutate({ id: postId, payload });
      } else {
        createMutation.mutate(payload as CreatePostInput);
      }
    } catch (error) {
      if (error instanceof Error) {
        message.error(error.message);
      }
    }
  };

  const submitting = createMutation.isLoading || updateMutation.isLoading;

  return (
    <Space direction="vertical" size={24} className="w-full">
      <div className="page-header">
        <div>
          <Title level={2} className="page-title">{isEdit ? '编辑文章' : '新建文章'}</Title>
          <Text className="page-description">
            {isEdit ? '更新现有文章内容、元数据与发布状态' : '撰写并发布新的博客文章'}
          </Text>
        </div>
        <Space>
          <Button onClick={() => navigate(-1)}>返回</Button>
          <Button type="primary" loading={submitting} onClick={handleSubmit}>
            {isEdit ? '保存修改' : '创建文章'}
          </Button>
        </Space>
      </div>

      <Card className="page-container" bordered={false} loading={postQuery.isLoading && isEdit}>
        <Form form={form} layout="vertical">
          <Form.Item
            name="title"
            label="文章标题"
            rules={[{ required: true, message: '请输入文章标题' }]}
          >
            <Input
              placeholder="请输入文章标题"
              onBlur={(event) => {
                const value = event.target.value;
                if (!form.getFieldValue('slug') && value) {
                  form.setFieldsValue({ slug: slugify(value) });
                }
              }}
            />
          </Form.Item>

          <Form.Item
            name="slug"
            label="Slug"
            rules={[{ required: true, message: '请输入文章 Slug' }]}
          >
            <Input placeholder="例如：awesome-post" />
          </Form.Item>

          <Form.Item name="excerpt" label="摘要">
            <Input.TextArea rows={3} placeholder="简要描述文章内容" />
          </Form.Item>

          <Form.Item
            name="content"
            label="正文内容"
            rules={[{ required: true, message: '请输入正文内容' }]}
          >
            <Input.TextArea rows={10} placeholder="支持 Markdown 或 HTML" />
          </Form.Item>

          <Form.Item
            name="status"
            label="发布状态"
            rules={[{ required: true, message: '请选择文章状态' }]}
          >
            <Select placeholder="选择文章状态">
              {statusOptions.map((option) => (
                <Option key={option.value} value={option.value}>
                  {option.label}
                </Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item
            name="categoryId"
            label="文章分类"
            rules={[{ required: true, message: '请选择文章分类' }]}
          >
            <Select placeholder="选择分类" loading={categoriesLoading} allowClear>
              {(categories as Category[]).map((category) => (
                <Option key={category.id} value={category.id}>
                  {category.name}
                </Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item name="tagIds" label="标签">
            <Select
              mode="multiple"
              placeholder="选择关联标签"
              loading={tagsLoading}
              allowClear
            >
              {(tags as Tag[]).map((tag) => (
                <Option key={tag.id} value={tag.id}>
                  {tag.name}
                </Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item name="featuredImage" label="封面图片">
            <Input placeholder="可选，填写封面图片 URL" />
          </Form.Item>

          <Form.Item name="featured" label="首页推荐" valuePropName="checked">
            <Switch checkedChildren="是" unCheckedChildren="否" />
          </Form.Item>

          <Form.Item name="seoTitle" label="SEO 标题">
            <Input placeholder="可选，自定义 SEO 标题" />
          </Form.Item>

          <Form.Item name="seoDescription" label="SEO 描述">
            <Input.TextArea rows={3} placeholder="可选，自定义 SEO 描述" />
          </Form.Item>

          {isEdit && postQuery.data && (
            <Text type="secondary">
              最后更新时间：{dayjs(postQuery.data.updatedAt).format('YYYY-MM-DD HH:mm:ss')}
            </Text>
          )}
        </Form>
      </Card>
    </Space>
  );
};

export default PostEditor;

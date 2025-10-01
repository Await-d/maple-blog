import React, { useState } from 'react';
import {
  Button,
  Card,
  Form,
  Input,
  Modal,
  Popconfirm,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  message,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, SafetyOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import roleService from '@/services/role.service';
import type { Role, Permission, CreateRoleInput, UpdateRoleInput } from '@/types';

const { Title, Text } = Typography;
const { Option } = Select;

const RoleManagement: React.FC = () => {
  const queryClient = useQueryClient();
  const [form] = Form.useForm();
  const [modalVisible, setModalVisible] = useState(false);
  const [editingRole, setEditingRole] = useState<Role | null>(null);

  const { data: roles = [], isLoading } = useQuery({
    queryKey: ['admin-roles'],
    queryFn: () => roleService.getRoles(),
    staleTime: 300_000,
  });

  const { data: permissions = [], isLoading: permissionsLoading } = useQuery({
    queryKey: ['admin-permissions'],
    queryFn: () => roleService.getPermissions(),
    staleTime: 300_000,
  });

  const createRoleMutation = useMutation({
    mutationFn: (payload: CreateRoleInput) => roleService.createRole(payload),
    onSuccess: () => {
      message.success('角色创建成功');
      queryClient.invalidateQueries({ queryKey: ['admin-roles'] });
      setModalVisible(false);
      form.resetFields();
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '创建角色失败';
      message.error(errMsg);
    },
  });

  const updateRoleMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateRoleInput }) => roleService.updateRole(id, payload),
    onSuccess: () => {
      message.success('角色更新成功');
      queryClient.invalidateQueries({ queryKey: ['admin-roles'] });
      setModalVisible(false);
      setEditingRole(null);
      form.resetFields();
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '更新角色失败';
      message.error(errMsg);
    },
  });

  const deleteRoleMutation = useMutation({
    mutationFn: (id: string) => roleService.deleteRole(id),
    onSuccess: () => {
      message.success('角色已删除');
      queryClient.invalidateQueries({ queryKey: ['admin-roles'] });
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '删除角色失败';
      message.error(errMsg);
    },
  });

  const openCreateModal = () => {
    setEditingRole(null);
    form.resetFields();
    setModalVisible(true);
  };

  const openEditModal = (role: Role) => {
    setEditingRole(role);
    form.setFieldsValue({
      name: role.name,
      description: role.description,
      level: role.level,
      permissionIds: role.permissions.map((permission) => permission.id),
    });
    setModalVisible(true);
  };

  const handleModalOk = async () => {
    try {
      const values = await form.validateFields();
      if (editingRole) {
        const payload: UpdateRoleInput = {
          name: values.name,
          description: values.description,
          level: values.level,
          permissionIds: values.permissionIds,
        };
        updateRoleMutation.mutate({ id: editingRole.id, payload });
      } else {
        const payload: CreateRoleInput = {
          name: values.name,
          description: values.description,
          level: values.level,
          permissionIds: values.permissionIds,
        };
        createRoleMutation.mutate(payload);
      }
    } catch (error) {
      if (error instanceof Error) {
        message.error(error.message);
      }
    }
  };

  const handleModalCancel = () => {
    setModalVisible(false);
    setEditingRole(null);
    form.resetFields();
  };

  const columns = [
    {
      title: '角色名称',
      dataIndex: 'name',
      key: 'name',
      render: (_: unknown, record: Role) => (
        <Space direction="vertical" size={2}>
          <Text strong>{record.name}</Text>
          {record.description && (
            <Text type="secondary">{record.description}</Text>
          )}
        </Space>
      ),
    },
    {
      title: '等级',
      dataIndex: 'level',
      key: 'level',
      width: 100,
    },
    {
      title: '权限数量',
      dataIndex: 'permissions',
      key: 'permissions',
      render: (permissions: Permission[]) => (
        <Tag color="blue">{permissions.length}</Tag>
      ),
    },
    {
      title: '类型',
      dataIndex: 'isBuiltIn',
      key: 'isBuiltIn',
      width: 120,
      render: (isBuiltIn: boolean) => (
        <Tag color={isBuiltIn ? 'geekblue' : 'green'}>
          {isBuiltIn ? '系统角色' : '自定义角色'}
        </Tag>
      ),
    },
    {
      title: '操作',
      key: 'actions',
      width: 200,
      render: (_: unknown, record: Role) => (
        <Space size="small">
          <Button
            type="link"
            icon={<EditOutlined />}
            onClick={() => openEditModal(record)}
            disabled={record.isBuiltIn}
          >
            编辑
          </Button>
          <Popconfirm
            title="确定删除该角色?"
            okText="删除"
            cancelText="取消"
            onConfirm={() => deleteRoleMutation.mutate(record.id)}
            disabled={record.isBuiltIn}
          >
            <Button type="link" danger icon={<DeleteOutlined />} disabled={record.isBuiltIn}>
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
            角色管理
          </Title>
          <Text className="page-description">
            配置后台角色及权限集合，确保最小权限原则
          </Text>
        </div>
        <Space>
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreateModal}>
            新增角色
          </Button>
        </Space>
      </div>

      <Card className="page-container" bordered={false}>
        <Table<Role>
          rowKey="id"
          loading={isLoading}
          dataSource={roles}
          columns={columns}
          pagination={false}
        />
      </Card>

      <Modal
        title={editingRole ? '编辑角色' : '新增角色'}
        open={modalVisible}
        onCancel={handleModalCancel}
        onOk={handleModalOk}
        confirmLoading={createRoleMutation.isLoading || updateRoleMutation.isLoading}
        destroyOnClose
      >
        <Form form={form} layout="vertical" initialValues={{ level: 3, permissionIds: [] }}>
          <Form.Item
            name="name"
            label="角色名称"
            rules={[{ required: true, message: '请输入角色名称' }]}
          >
            <Input placeholder="例如：内容管理员" />
          </Form.Item>

          <Form.Item
            name="description"
            label="角色说明"
            rules={[{ max: 120, message: '说明不超过 120 字' }]}
          >
            <Input.TextArea rows={3} placeholder="简要描述角色职责" />
          </Form.Item>

          <Form.Item
            name="level"
            label="角色等级"
            rules={[{ required: true, message: '请选择角色等级' }]}
          >
            <Select placeholder="选择等级 (1-5)" suffixIcon={<SafetyOutlined />}>
              {[1, 2, 3, 4, 5].map((level) => (
                <Option key={level} value={level}>
                  等级 {level}
                </Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item
            name="permissionIds"
            label="权限"
            rules={[{ required: true, message: '请至少选择一个权限' }]}
          >
            <Select
              mode="multiple"
              placeholder="请选择权限"
              loading={permissionsLoading}
              optionFilterProp="children"
            >
              {permissions.map((permission: Permission) => (
                <Option key={permission.id} value={permission.id}>
                  {permission.name}（{permission.code}）
                </Option>
              ))}
            </Select>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default RoleManagement;

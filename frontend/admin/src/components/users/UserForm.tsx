import React, { useEffect } from 'react';
import { Modal, Form, Input, Select, Typography, message } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useUserForm, useUserManagementStore } from '@/stores/userManagementStore';
import roleService from '@/services/role.service';
import userService from '@/services/user.service';
import type { CreateUserInput, Role, UpdateUserInput } from '@/types';
import { UserStatus } from '@/types';

const { Option } = Select;
const { Text } = Typography;
const statusOptions: Array<{ label: string; value: UserStatus }> = [
  { label: '活跃', value: UserStatus.Active },
  { label: '未激活', value: UserStatus.Inactive },
  { label: '禁用', value: UserStatus.Banned },
  { label: '待审核', value: UserStatus.Pending },
];

const UserForm: React.FC = () => {
  const queryClient = useQueryClient();
  const [form] = Form.useForm();
  const { visible, mode, data } = useUserForm();
  const { closeUserForm, addUser, updateUser } = useUserManagementStore();

  const { data: roles = [], isLoading: rolesLoading } = useQuery({
    queryKey: ['admin-roles'],
    queryFn: () => roleService.getRoles(),
    staleTime: 300_000,
  });

  const createMutation = useMutation({
    mutationFn: (payload: CreateUserInput) => userService.createUser(payload),
    onSuccess: (user) => {
      addUser(user);
      message.success('用户创建成功');
      queryClient.invalidateQueries({ queryKey: ['admin-users'] });
      closeUserForm();
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '创建用户失败，请稍后重试';
      message.error(errMsg);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateUserInput }) => userService.updateUser(id, payload),
    onSuccess: (user) => {
      updateUser(user.id, user);
      message.success('用户更新成功');
      queryClient.invalidateQueries({ queryKey: ['admin-users'] });
      closeUserForm();
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '更新用户失败，请稍后重试';
      message.error(errMsg);
    },
  });

  useEffect(() => {
    if (visible) {
      if (mode === 'edit' && data) {
        form.setFieldsValue({
          username: data.username,
          email: data.email,
          displayName: data.displayName,
          status: data.status,
          avatar: data.avatar,
          roleIds: data.roles?.map((role) => role.id),
        });
      } else {
        form.resetFields();
        form.setFieldsValue({ status: 'active' });
      }
    }
  }, [visible, mode, data, form]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();

      if (mode === 'create') {
        const payload: CreateUserInput = {
          username: values.username,
          email: values.email,
          password: values.password,
          displayName: values.displayName,
          avatar: values.avatar,
          status: values.status,
          roleIds: values.roleIds,
        };
        createMutation.mutate(payload);
      }

      if (mode === 'edit' && data) {
        const payload: UpdateUserInput = {
          username: values.username,
          email: values.email,
          displayName: values.displayName,
          avatar: values.avatar,
          status: values.status,
          roleIds: values.roleIds,
          password: values.password,
        };
        updateMutation.mutate({ id: data.id, payload });
      }
    } catch (error) {
      if (error instanceof Error) {
        message.error(error.message);
      }
    }
  };

  const handleCancel = () => {
    closeUserForm();
    form.resetFields();
  };

  const confirmLoading = createMutation.isLoading || updateMutation.isLoading;

  return (
    <Modal
      title={mode === 'create' ? '新增用户' : '编辑用户'}
      open={visible}
      onCancel={handleCancel}
      onOk={handleSubmit}
      confirmLoading={confirmLoading}
      okText={mode === 'create' ? '创建' : '保存'}
      destroyOnClose
    >
      <Form
        form={form}
        layout="vertical"
        initialValues={{ status: UserStatus.Active, roleIds: [] }}
      >
        <Form.Item
          name="username"
          label="用户名"
          rules={[{ required: true, message: '请输入用户名' }]}
        >
          <Input placeholder="请输入用户名" autoComplete="off" />
        </Form.Item>

        <Form.Item
          name="email"
          label="邮箱"
          rules={[
            { required: true, message: '请输入邮箱地址' },
            { type: 'email', message: '邮箱格式不正确' },
          ]}
        >
          <Input placeholder="name@example.com" autoComplete="off" />
        </Form.Item>

        <Form.Item
          name="displayName"
          label="显示名称"
          rules={[{ required: true, message: '请输入显示名称' }]}
        >
          <Input placeholder="请输入显示名称" />
        </Form.Item>

        <Form.Item
          name="status"
          label="账号状态"
          rules={[{ required: true, message: '请选择账号状态' }]}
        >
          <Select placeholder="请选择状态">
            {statusOptions.map((option) => (
              <Option key={option.value} value={option.value}>
                {option.label}
              </Option>
            ))}
          </Select>
        </Form.Item>

        <Form.Item
          name="roleIds"
          label="角色"
          rules={[{ required: true, message: '请至少选择一个角色' }]}
        >
          <Select
            mode="multiple"
            placeholder="请选择角色"
            loading={rolesLoading}
            optionFilterProp="children"
            allowClear
            disabled={roles.length === 0}
          >
            {roles.map((role: Role) => (
              <Option key={role.id} value={role.id}>
                {role.name}
              </Option>
            ))}
          </Select>
          {roles.length === 0 && (
            <Text type="secondary">暂无可选角色，请先前往角色管理创建。</Text>
          )}
        </Form.Item>

        <Form.Item
          name="avatar"
          label="头像地址"
        >
          <Input placeholder="可选，填写头像图片 URL" />
        </Form.Item>

        <Form.Item
          name="password"
          label={mode === 'create' ? '初始密码' : '重置密码'}
          rules={mode === 'create' ? [{ required: true, message: '请输入登录密码' }] : []}
        >
          <Input.Password autoComplete="new-password" placeholder={mode === 'create' ? '请输入初始密码' : '留空则不修改密码'} />
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default UserForm;

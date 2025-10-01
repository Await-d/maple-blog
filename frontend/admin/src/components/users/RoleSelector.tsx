import React, { useEffect, useMemo, useState } from 'react';
import { Modal, Checkbox, Space, Typography, List, Tag, Empty, message, Spin } from 'antd';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useRoleAssignment, useUserManagementStore } from '@/stores/userManagementStore';
import roleService from '@/services/role.service';
import userService from '@/services/user.service';

const { Text } = Typography;

const RoleSelector: React.FC = () => {
  const queryClient = useQueryClient();
  const { visible, user } = useRoleAssignment();
  const { closeRoleAssignment, updateUser } = useUserManagementStore();
  const [selectedRoleIds, setSelectedRoleIds] = useState<string[]>([]);

  const { data: roles = [], isLoading } = useQuery({
    queryKey: ['admin-roles'],
    queryFn: () => roleService.getRoles(),
    staleTime: 300_000,
  });

  useEffect(() => {
    if (user) {
      setSelectedRoleIds(user.roles.map((role) => role.id));
    } else {
      setSelectedRoleIds([]);
    }
  }, [user]);

  const assignRolesMutation = useMutation({
    mutationFn: (roleIds: string[]) => {
      if (!user) {
        return Promise.reject(new Error('无效的用户信息'));
      }
      return userService.assignRoles(user.id, roleIds);
    },
    onSuccess: (updatedUser) => {
      updateUser(updatedUser.id, updatedUser);
      message.success('角色已更新');
      queryClient.invalidateQueries({ queryKey: ['admin-users'] });
      closeRoleAssignment();
    },
    onError: (error: unknown) => {
      const errMsg = error instanceof Error ? error.message : '角色更新失败，请稍后重试';
      message.error(errMsg);
    },
  });

  const handleSubmit = () => {
    assignRolesMutation.mutate(selectedRoleIds);
  };

  const roleOptions = useMemo(() => roles.map((role) => ({
    label: role.name,
    value: role.id,
    role,
  })), [roles]);

  return (
    <Modal
      title="分配角色"
      open={visible}
      onCancel={closeRoleAssignment}
      onOk={handleSubmit}
      confirmLoading={assignRolesMutation.isLoading}
      okText="保存"
      cancelText="取消"
      destroyOnClose
    >
      {!user ? (
        <Empty description="未选择用户" />
      ) : (
        <Space direction="vertical" size={16} className="w-full">
          <div>
            <Text type="secondary">为用户</Text>{' '}
            <Text strong>{user.displayName || user.username}</Text>{' '}
            <Text type="secondary">选择角色</Text>
          </div>

          {isLoading ? (
            <Spin />
          ) : roles.length === 0 ? (
            <Empty description="暂无可用角色" />
          ) : (
            <Checkbox.Group
              value={selectedRoleIds}
              onChange={(values) => setSelectedRoleIds(values as string[])}
              className="w-full"
            >
              <List
                dataSource={roleOptions}
                renderItem={({ role }) => (
                  <List.Item key={role.id}>
                    <Space direction="vertical" size={4} className="w-full">
                      <Checkbox value={role.id}>
                        <Space>
                          <Text strong>{role.name}</Text>
                          <Tag color={role.isBuiltIn ? 'blue' : 'geekblue'}>
                            {role.isBuiltIn ? '系统角色' : '自定义角色'}
                          </Tag>
                        </Space>
                      </Checkbox>
                      {role.description && (
                        <Text type="secondary">{role.description}</Text>
                      )}
                    </Space>
                  </List.Item>
                )}
              />
            </Checkbox.Group>
          )}
        </Space>
      )}
    </Modal>
  );
};

export default RoleSelector;

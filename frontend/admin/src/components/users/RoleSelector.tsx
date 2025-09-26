import React, { useState, useEffect, useMemo } from 'react';
import {
  Modal,
  Card,
  Checkbox,
  Tree,
  Space,
  Button,
  Input,
  Typography,
  Tag,
  Alert,
  Collapse,
  Table,
  Badge,
  Row,
  Col,
  Tabs,
  Transfer,
  List,
  Avatar,
  message,
  Empty,
  Statistic,
} from 'antd';
import {
  UserOutlined,
  TeamOutlined,
  CrownOutlined,
  WarningOutlined,
  ApartmentOutlined,
  ShieldOutlined,
} from '@ant-design/icons';
import { useRoleAssignment, useUserManagementStore } from '@/stores/userManagementStore';
import type { Role, Permission } from '@/types';

const { Text } = Typography;
const { Search } = Input;
const { Panel } = Collapse;
const { TabPane } = Tabs;

interface RoleSelectorProps {}

const RoleSelector: React.FC<RoleSelectorProps> = () => {
  const { visible, user } = useRoleAssignment();
  const { closeRoleAssignment, updateUser } = useUserManagementStore();

  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState('roles');
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  const [searchValue, setSearchValue] = useState('');
  const [expandedKeys, setExpandedKeys] = useState<string[]>([]);

  // Mock data for roles and permissions
  const mockRoles: Role[] = useMemo(() => [
    {
      id: '1',
      name: 'Administrator',
      description: 'System administrator with full access to all system features and settings',
      permissions: [
        {
          id: 'perm_1',
          name: '系统管理',
          code: 'system.admin',
          description: 'Full system administration access',
          category: 'system',
          isActive: true,
        },
        {
          id: 'perm_2',
          name: '用户管理',
          code: 'user.manage',
          description: 'User management permissions',
          category: 'user',
          isActive: true,
        },
        {
          id: 'perm_3',
          name: '内容管理',
          code: 'content.manage',
          description: 'Content management permissions',
          category: 'content',
          isActive: true,
        },
      ],
      level: 1,
      isBuiltIn: true,
    },
    {
      id: '2',
      name: 'Editor',
      description: 'Content editor with publishing and content management rights',
      permissions: [
        {
          id: 'perm_3',
          name: '内容管理',
          code: 'content.manage',
          description: 'Content management permissions',
          category: 'content',
          isActive: true,
        },
        {
          id: 'perm_4',
          name: '文章发布',
          code: 'post.publish',
          description: 'Post publishing permissions',
          category: 'content',
          isActive: true,
        },
        {
          id: 'perm_5',
          name: '媒体管理',
          code: 'media.manage',
          description: 'Media management permissions',
          category: 'content',
          isActive: true,
        },
      ],
      level: 2,
      isBuiltIn: true,
    },
    {
      id: '3',
      name: 'Author',
      description: 'Content author with writing and draft management permissions',
      permissions: [
        {
          id: 'perm_6',
          name: '文章创建',
          code: 'post.create',
          description: 'Post creation permissions',
          category: 'content',
          isActive: true,
        },
        {
          id: 'perm_7',
          name: '文章编辑',
          code: 'post.edit',
          description: 'Post editing permissions',
          category: 'content',
          isActive: true,
        },
      ],
      level: 3,
      isBuiltIn: true,
    },
    {
      id: '4',
      name: 'User',
      description: 'Regular user with basic access permissions',
      permissions: [
        {
          id: 'perm_8',
          name: '基本访问',
          code: 'basic.access',
          description: 'Basic system access',
          category: 'basic',
          isActive: true,
        },
        {
          id: 'perm_9',
          name: '个人资料',
          code: 'profile.edit',
          description: 'Personal profile editing',
          category: 'profile',
          isActive: true,
        },
      ],
      level: 4,
      isBuiltIn: true,
    },
    {
      id: '5',
      name: 'Moderator',
      description: 'Community moderator with content moderation permissions',
      permissions: [
        {
          id: 'perm_10',
          name: '内容审核',
          code: 'content.moderate',
          description: 'Content moderation permissions',
          category: 'moderation',
          isActive: true,
        },
        {
          id: 'perm_11',
          name: '评论管理',
          code: 'comment.manage',
          description: 'Comment management permissions',
          category: 'moderation',
          isActive: true,
        },
      ],
      level: 3,
      isBuiltIn: false,
    },
  ], []);

  // Initialize selected roles when modal opens
  useEffect(() => {
    if (visible && user) {
      setSelectedRoles(user.roles.map(role => role.id));
    }
  }, [visible, user]);


  // Get role hierarchy display
  const getRoleHierarchy = () => {
    const hierarchyData = mockRoles
      .sort((a, b) => a.level - b.level)
      .map(role => ({
        title: (
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              {role.level === 1 ? <CrownOutlined /> : <TeamOutlined />}
              <span className="font-medium">{role.name}</span>
              <Tag color={getRoleColor(role.level)}>
                级别 {role.level}
              </Tag>
              {role.isBuiltIn && (
                <Tag color="blue" >
                  内置
                </Tag>
              )}
            </div>
            <Checkbox
              checked={selectedRoles.includes(role.id)}
              onChange={(e) => handleRoleToggle(role.id, e.target.checked)}
            />
          </div>
        ),
        key: role.id,
        children: [
          {
            title: (
              <div className="text-sm text-gray-600">
                {role.description}
              </div>
            ),
            key: `${role.id}-desc`,
            selectable: false,
          },
          {
            title: (
              <div className="flex flex-wrap gap-1 mt-2">
                {role.permissions.map(permission => (
                  <Tag key={permission.id} >
                    {permission.name}
                  </Tag>
                ))}
              </div>
            ),
            key: `${role.id}-perms`,
            selectable: false,
          },
        ],
      }));

    return hierarchyData;
  };

  // Get role color based on level
  const getRoleColor = (level: number) => {
    const colors = {
      1: 'red',      // Administrator
      2: 'orange',   // Editor
      3: 'blue',     // Author/Moderator
      4: 'green',    // User
    };
    return colors[level as keyof typeof colors] || 'default';
  };

  // Handle role toggle
  const handleRoleToggle = (roleId: string, checked: boolean) => {
    setSelectedRoles(prev => {
      if (checked) {
        return [...prev, roleId];
      } else {
        return prev.filter(id => id !== roleId);
      }
    });
  };

  // Handle save changes
  const handleSave = async () => {
    if (!user) return;

    try {
      setLoading(true);
      
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1500));

      const updatedRoles = mockRoles.filter(role => selectedRoles.includes(role.id));
      
      updateUser(user.id, {
        roles: updatedRoles,
        updatedAt: new Date().toISOString(),
      });

      message.success('角色分配更新成功');
      closeRoleAssignment();
    } catch (error) {
      message.error('更新失败，请重试');
    } finally {
      setLoading(false);
    }
  };

  // Get selected roles info
  const selectedRolesList = mockRoles.filter(role => selectedRoles.includes(role.id));
  const totalPermissions = new Set(
    selectedRolesList.flatMap(role => role.permissions.map(p => p.id))
  ).size;

  // Permission analysis
  const permissionCategories = useMemo(() => {
    const categories = new Map<string, Permission[]>();
    
    selectedRolesList.forEach(role => {
      role.permissions.forEach(permission => {
        if (!categories.has(permission.category)) {
          categories.set(permission.category, []);
        }
        const existing = categories.get(permission.category)!;
        if (!existing.find(p => p.id === permission.id)) {
          existing.push(permission);
        }
      });
    });

    return Array.from(categories.entries()).map(([category, permissions]) => ({
      category,
      permissions,
      count: permissions.length,
    }));
  }, [selectedRolesList]);

  // Transfer component for role assignment
  const transferData = mockRoles.map(role => ({
    key: role.id,
    title: role.name,
    description: role.description,
    level: role.level,
    isBuiltIn: role.isBuiltIn,
    permissionCount: role.permissions.length,
  }));

  const renderTransferItem = (item: typeof transferData[0]) => (
    <div className="flex items-center justify-between w-full">
      <div className="flex items-center gap-2">
        {item.level === 1 ? <CrownOutlined /> : <TeamOutlined />}
        <div>
          <div className="font-medium">{item.title}</div>
          <div className="text-xs text-gray-500 truncate max-w-48">
            {item.description}
          </div>
        </div>
      </div>
      <div className="flex flex-col items-end gap-1">
        <Tag color={getRoleColor(item.level)} >
          L{item.level}
        </Tag>
        <Text type="secondary" style={{ fontSize: '11px' }}>
          {item.permissionCount}个权限
        </Text>
      </div>
    </div>
  );

  const handleTransferChange = (targetKeys: string[]) => {
    setSelectedRoles(targetKeys);
  };

  if (!user) return null;

  return (
    <Modal
      title={
        <div className="flex items-center gap-3">
          <Avatar src={user.avatar} icon={<UserOutlined />} />
          <div>
            <div className="font-semibold">
              为 {user.displayName || user.username} 分配角色
            </div>
            <div className="text-sm text-gray-500 font-normal">
              当前角色: {user.roles.map(r => r.name).join(', ')}
            </div>
          </div>
        </div>
      }
      open={visible}
      onCancel={closeRoleAssignment}
      width={900}
      footer={
        <Space>
          <Button onClick={closeRoleAssignment}>
            取消
          </Button>
          <Button
            type="primary"
            loading={loading}
            onClick={handleSave}
            disabled={selectedRoles.length === 0}
          >
            保存更改
          </Button>
        </Space>
      }
    >
      <div className="space-y-4">
        {/* Role Assignment Summary */}
        <Card  className="bg-blue-50 border-blue-200">
          <Row gutter={16}>
            <Col span={8}>
              <Statistic
                title="已选角色"
                value={selectedRoles.length}
                prefix={<TeamOutlined />}
                valueStyle={{ color: '#1890ff' }}
              />
            </Col>
            <Col span={8}>
              <Statistic
                title="总权限数"
                value={totalPermissions}
                prefix={<ShieldOutlined />}
                valueStyle={{ color: '#52c41a' }}
              />
            </Col>
            <Col span={8}>
              <Statistic
                title="权限分类"
                value={permissionCategories.length}
                prefix={<ApartmentOutlined />}
                valueStyle={{ color: '#722ed1' }}
              />
            </Col>
          </Row>
        </Card>

        {/* Warning for sensitive roles */}
        {selectedRoles.some(id => mockRoles.find(r => r.id === id)?.level === 1) && (
          <Alert
            type="warning"
            icon={<WarningOutlined />}
            message="管理员权限警告"
            description="您正在分配管理员权限，该角色具有系统的完全访问权限。请确认用户需要这些权限。"
            showIcon
          />
        )}

        <Tabs activeKey={activeTab} onChange={setActiveTab}>
          {/* Transfer Mode */}
          <TabPane tab="角色选择" key="transfer">
            <div className="mb-4">
              <Search
                placeholder="搜索角色..."
                value={searchValue}
                onChange={(e) => setSearchValue(e.target.value)}
                style={{ width: 300 }}
                allowClear
              />
            </div>

            <Transfer
              dataSource={transferData.filter(item =>
                item.title.toLowerCase().includes(searchValue.toLowerCase()) ||
                item.description.toLowerCase().includes(searchValue.toLowerCase())
              )}
              targetKeys={selectedRoles}
              onChange={handleTransferChange}
              render={renderTransferItem}
              titles={['可选角色', '已分配角色']}
              listStyle={{
                width: 400,
                height: 400,
              }}
              operations={['分配', '移除']}
              showSearch
              searchPlaceholder="搜索角色"
            />
          </TabPane>

          {/* Hierarchy View */}
          <TabPane tab="层级视图" key="hierarchy">
            <div className="mb-4">
              <Search
                placeholder="搜索角色..."
                value={searchValue}
                onChange={(e) => setSearchValue(e.target.value)}
                style={{ width: 300 }}
                allowClear
              />
            </div>

            <Tree
              treeData={getRoleHierarchy()}
              expandedKeys={expandedKeys}
              onExpand={setExpandedKeys}
              selectable={false}
              defaultExpandAll
              className="role-hierarchy-tree"
            />
          </TabPane>

          {/* Permission Analysis */}
          <TabPane tab="权限分析" key="permissions">
            {permissionCategories.length > 0 ? (
              <Collapse defaultActiveKey={permissionCategories.map(c => c.category)}>
                {permissionCategories.map(({ category, permissions, count }) => (
                  <Panel
                    header={
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <ShieldOutlined />
                          <span className="font-medium">
                            {category.charAt(0).toUpperCase() + category.slice(1)}
                          </span>
                        </div>
                        <Badge count={count} />
                      </div>
                    }
                    key={category}
                  >
                    <List
                      
                      dataSource={permissions}
                      renderItem={(permission) => (
                        <List.Item>
                          <List.Item.Meta
                            avatar={
                              <div className="w-2 h-2 bg-green-500 rounded-full mt-2"></div>
                            }
                            title={permission.name}
                            description={permission.description}
                          />
                          <Tag >{permission.code}</Tag>
                        </List.Item>
                      )}
                    />
                  </Panel>
                ))}
              </Collapse>
            ) : (
              <Empty
                description="请先选择角色以查看权限分析"
                image={Empty.PRESENTED_IMAGE_SIMPLE}
              />
            )}
          </TabPane>

          {/* Role Comparison */}
          <TabPane tab="角色对比" key="comparison">
            {selectedRolesList.length > 0 ? (
              <Table
                dataSource={selectedRolesList}
                rowKey="id"
                
                pagination={false}
                columns={[
                  {
                    title: '角色',
                    dataIndex: 'name',
                    key: 'name',
                    render: (name, role) => (
                      <div className="flex items-center gap-2">
                        {role.level === 1 ? <CrownOutlined /> : <TeamOutlined />}
                        <span className="font-medium">{name}</span>
                        <Tag color={getRoleColor(role.level)} >
                          L{role.level}
                        </Tag>
                      </div>
                    ),
                  },
                  {
                    title: '描述',
                    dataIndex: 'description',
                    key: 'description',
                    ellipsis: true,
                  },
                  {
                    title: '权限数量',
                    key: 'permissionCount',
                    width: 100,
                    render: (_, role) => (
                      <Badge count={role.permissions.length} />
                    ),
                  },
                  {
                    title: '类型',
                    dataIndex: 'isBuiltIn',
                    key: 'isBuiltIn',
                    width: 80,
                    render: (isBuiltIn) => (
                      <Tag color={isBuiltIn ? 'blue' : 'green'} >
                        {isBuiltIn ? '内置' : '自定义'}
                      </Tag>
                    ),
                  },
                ]}
                expandable={{
                  expandedRowRender: (role) => (
                    <div className="p-4 bg-gray-50">
                      <div className="font-medium mb-2">权限详情:</div>
                      <div className="flex flex-wrap gap-1">
                        {role.permissions.map(permission => (
                          <Tag key={permission.id} >
                            {permission.name}
                          </Tag>
                        ))}
                      </div>
                    </div>
                  ),
                }}
              />
            ) : (
              <Empty
                description="请先选择角色以查看对比信息"
                image={Empty.PRESENTED_IMAGE_SIMPLE}
              />
            )}
          </TabPane>
        </Tabs>
      </div>
    </Modal>
  );
};

export default RoleSelector;
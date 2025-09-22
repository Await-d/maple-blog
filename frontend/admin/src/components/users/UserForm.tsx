// @ts-nocheck
import React, { useEffect, useState } from 'react';
import {
  Modal,
  Form,
  Input,
  Select,
  Switch,
  Upload,
  Avatar,
  Button,
  Space,
  Row,
  Col,
  Card,
  Divider,
  Alert,
  Tooltip,
  message,
  Progress,
} from 'antd';
import {
  UserOutlined,
  MailOutlined,
  LockOutlined,
  PlusOutlined,
  CameraOutlined,
  EyeInvisibleOutlined,
  EyeTwoTone,
  InfoCircleOutlined,
  SafetyOutlined,
  TeamOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import type { UploadProps, UploadFile } from 'antd/es/upload';
import type { RcFile } from 'antd/es/upload/interface';
import { useUserForm, useUserManagementStore } from '@/stores/userManagementStore';
import type { User, UserStatus, Role } from '@/types';

const { Option } = Select;
const { TextArea } = Input;

interface UserFormProps {}

const UserForm: React.FC<UserFormProps> = () => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [avatarUrl, setAvatarUrl] = useState<string>('');
  const [fileList, setFileList] = useState<UploadFile[]>([]);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [passwordStrength, setPasswordStrength] = useState(0);

  const { visible, mode, data } = useUserForm();
  const { closeUserForm, addUser, updateUser } = useUserManagementStore();

  // Mock roles data
  const mockRoles: Role[] = [
    {
      id: '1',
      name: 'Administrator',
      description: 'System administrator with full access',
      permissions: [],
      level: 1,
      isBuiltIn: true,
    },
    {
      id: '2',
      name: 'Editor',
      description: 'Content editor with publishing rights',
      permissions: [],
      level: 2,
      isBuiltIn: true,
    },
    {
      id: '3',
      name: 'Author',
      description: 'Content author with writing permissions',
      permissions: [],
      level: 3,
      isBuiltIn: true,
    },
    {
      id: '4',
      name: 'User',
      description: 'Regular user with basic permissions',
      permissions: [],
      level: 4,
      isBuiltIn: true,
    },
  ];

  // Reset form when modal opens/closes
  useEffect(() => {
    if (visible) {
      if (mode === 'edit' && data) {
        // Populate form with existing user data
        form.setFieldsValue({
          username: data.username,
          email: data.email,
          displayName: data.displayName,
          status: data.status,
          roleIds: data.roles?.map(role => role.id) || [],
        });
        setAvatarUrl(data.avatar || '');
      } else {
        // Reset form for create mode
        form.resetFields();
        setAvatarUrl('');
        setFileList([]);
        setPasswordStrength(0);
      }
    }
  }, [visible, mode, data, form]);

  // Handle form submission
  const handleSubmit = async (values: any) => {
    try {
      setLoading(true);

      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1500));

      const userData: Partial<User> = {
        username: values.username,
        email: values.email,
        displayName: values.displayName,
        status: values.status || 'active',
        avatar: avatarUrl,
        roles: mockRoles.filter(role => values.roleIds?.includes(role.id)) || [],
      };

      if (mode === 'create') {
        const newUser: User = {
          id: `user_${Date.now()}`,
          ...userData,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        } as User;
        
        addUser(newUser);
        message.success('用户创建成功');
      } else if (mode === 'edit' && data) {
        updateUser(data.id, {
          ...userData,
          updatedAt: new Date().toISOString(),
        });
        message.success('用户更新成功');
      }

      closeUserForm();
    } catch (error) {
      message.error('操作失败，请重试');
    } finally {
      setLoading(false);
    }
  };

  // Handle avatar upload
  const handleAvatarChange: UploadProps['onChange'] = (info) => {
    setFileList(info.fileList);

    if (info.file.status === 'uploading') {
      setUploadProgress(info.file.percent || 0);
    }

    if (info.file.status === 'done') {
      // Get this url from response in real world
      setAvatarUrl(info.file.response?.url || URL.createObjectURL(info.file.originFileObj as RcFile));
      setUploadProgress(0);
      message.success('头像上传成功');
    } else if (info.file.status === 'error') {
      setUploadProgress(0);
      message.error('头像上传失败');
    }
  };

  // Custom upload function (simulate upload)
  const customUpload = ({ file, onProgress, onSuccess, onError }: any) => {
    let progress = 0;
    const timer = setInterval(() => {
      progress += 10;
      onProgress({ percent: progress });
      
      if (progress >= 100) {
        clearInterval(timer);
        onSuccess({
          url: URL.createObjectURL(file),
        });
      }
    }, 100);

    return {
      abort() {
        clearInterval(timer);
      },
    };
  };

  // Upload props
  const uploadProps: UploadProps = {
    name: 'avatar',
    listType: 'picture-card',
    fileList,
    showUploadList: false,
    beforeUpload: (file) => {
      const isJpgOrPng = file.type === 'image/jpeg' || file.type === 'image/png';
      if (!isJpgOrPng) {
        message.error('只支持 JPG/PNG 格式的图片');
        return false;
      }
      const isLt2M = file.size / 1024 / 1024 < 2;
      if (!isLt2M) {
        message.error('图片大小不能超过 2MB');
        return false;
      }
      return true;
    },
    customRequest: customUpload,
    onChange: handleAvatarChange,
  };

  // Password strength checker
  const checkPasswordStrength = (password: string) => {
    let strength = 0;
    if (password.length >= 8) strength += 25;
    if (/[a-z]/.test(password)) strength += 25;
    if (/[A-Z]/.test(password)) strength += 25;
    if (/[0-9]/.test(password) && /[^A-Za-z0-9]/.test(password)) strength += 25;
    
    setPasswordStrength(strength);
    return strength;
  };

  // Get password strength color
  const getPasswordStrengthColor = (strength: number) => {
    if (strength < 25) return '#ff4d4f';
    if (strength < 50) return '#faad14';
    if (strength < 75) return '#1890ff';
    return '#52c41a';
  };

  // Get password strength text
  const getPasswordStrengthText = (strength: number) => {
    if (strength < 25) return '弱';
    if (strength < 50) return '一般';
    if (strength < 75) return '良好';
    return '强';
  };

  const title = mode === 'create' ? '新增用户' : mode === 'edit' ? '编辑用户' : '查看用户';
  const isViewMode = mode === 'view';

  return (
    <Modal
      title={title}
      open={visible}
      onCancel={closeUserForm}
      width={800}
      footer={
        isViewMode ? (
          <Button onClick={closeUserForm}>关闭</Button>
        ) : (
          <Space>
            <Button onClick={closeUserForm}>取消</Button>
            <Button
              type="primary"
              loading={loading}
              onClick={() => form.submit()}
            >
              {mode === 'create' ? '创建用户' : '保存更改'}
            </Button>
          </Space>
        )
      }
      destroyOnClose
    >
      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        disabled={isViewMode}
      >
        <Row gutter={24}>
          {/* Left Column - Avatar */}
          <Col xs={24} md={8}>
            <Card  className="text-center">
              <div className="mb-4">
                <Upload {...uploadProps}>
                  <div className="relative">
                    <Avatar
                      size={120}
                      src={avatarUrl}
                      icon={<UserOutlined />}
                      className="border-2 border-dashed border-gray-300 hover:border-blue-400 transition-colors cursor-pointer"
                    />
                    {uploadProgress > 0 && uploadProgress < 100 && (
                      <div className="absolute inset-0 flex items-center justify-center bg-black bg-opacity-50 rounded-full">
                        <Progress
                          type="circle"
                          percent={uploadProgress}
                          size={60}
                          strokeColor="#1890ff"
                        />
                      </div>
                    )}
                    {!isViewMode && (
                      <div className="absolute bottom-0 right-0 bg-blue-500 text-white rounded-full p-2 border-2 border-white">
                        <CameraOutlined />
                      </div>
                    )}
                  </div>
                </Upload>
              </div>
              {!isViewMode && (
                <div className="text-sm text-gray-500">
                  <div>支持 JPG、PNG 格式</div>
                  <div>文件大小不超过 2MB</div>
                </div>
              )}
            </Card>
          </Col>

          {/* Right Column - Form Fields */}
          <Col xs={24} md={16}>
            <div className="space-y-4">
              {/* Basic Information */}
              <Card  title="基本信息">
                <Row gutter={16}>
                  <Col xs={24} md={12}>
                    <Form.Item
                      name="username"
                      label="用户名"
                      rules={[
                        { required: true, message: '请输入用户名' },
                        { min: 3, message: '用户名至少3个字符' },
                        { max: 20, message: '用户名最多20个字符' },
                        { pattern: /^[a-zA-Z0-9_]+$/, message: '只能包含字母、数字和下划线' },
                      ]}
                    >
                      <Input
                        prefix={<UserOutlined />}
                        placeholder="请输入用户名"
                        disabled={mode === 'edit'} // Username cannot be changed
                      />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={12}>
                    <Form.Item
                      name="email"
                      label="邮箱地址"
                      rules={[
                        { required: true, message: '请输入邮箱地址' },
                        { type: 'email', message: '请输入有效的邮箱地址' },
                      ]}
                    >
                      <Input
                        prefix={<MailOutlined />}
                        placeholder="请输入邮箱地址"
                      />
                    </Form.Item>
                  </Col>
                </Row>

                <Form.Item
                  name="displayName"
                  label="显示名称"
                  rules={[
                    { max: 50, message: '显示名称最多50个字符' },
                  ]}
                >
                  <Input
                    placeholder="请输入显示名称（可选）"
                  />
                </Form.Item>

                {mode === 'create' && (
                  <>
                    <Form.Item
                      name="password"
                      label="密码"
                      rules={[
                        { required: true, message: '请输入密码' },
                        { min: 8, message: '密码至少8个字符' },
                      ]}
                    >
                      <Input.Password
                        prefix={<LockOutlined />}
                        placeholder="请输入密码"
                        iconRender={(visible) => (visible ? <EyeTwoTone /> : <EyeInvisibleOutlined />)}
                        onChange={(e) => checkPasswordStrength(e.target.value)}
                      />
                    </Form.Item>

                    {passwordStrength > 0 && (
                      <div className="mb-4">
                        <div className="flex items-center gap-2 mb-1">
                          <span className="text-sm">密码强度:</span>
                          <span
                            className="text-sm font-medium"
                            style={{ color: getPasswordStrengthColor(passwordStrength) }}
                          >
                            {getPasswordStrengthText(passwordStrength)}
                          </span>
                        </div>
                        <Progress
                          percent={passwordStrength}
                          
                          strokeColor={getPasswordStrengthColor(passwordStrength)}
                          showInfo={false}
                        />
                      </div>
                    )}

                    <Form.Item
                      name="confirmPassword"
                      label="确认密码"
                      dependencies={['password']}
                      rules={[
                        { required: true, message: '请确认密码' },
                        ({ getFieldValue }) => ({
                          validator(_, value) {
                            if (!value || getFieldValue('password') === value) {
                              return Promise.resolve();
                            }
                            return Promise.reject(new Error('两次密码输入不一致'));
                          },
                        }),
                      ]}
                    >
                      <Input.Password
                        prefix={<LockOutlined />}
                        placeholder="请再次输入密码"
                        iconRender={(visible) => (visible ? <EyeTwoTone /> : <EyeInvisibleOutlined />)}
                      />
                    </Form.Item>
                  </>
                )}
              </Card>

              {/* Account Settings */}
              <Card  title="账户设置">
                <Row gutter={16}>
                  <Col xs={24} md={12}>
                    <Form.Item
                      name="status"
                      label="账户状态"
                      initialValue="active"
                    >
                      <Select>
                        <Option value="active">
                          <div className="flex items-center gap-2">
                            <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                            活跃
                          </div>
                        </Option>
                        <Option value="inactive">
                          <div className="flex items-center gap-2">
                            <div className="w-2 h-2 bg-gray-400 rounded-full"></div>
                            非活跃
                          </div>
                        </Option>
                        <Option value="banned">
                          <div className="flex items-center gap-2">
                            <div className="w-2 h-2 bg-red-500 rounded-full"></div>
                            已封禁
                          </div>
                        </Option>
                        <Option value="pending">
                          <div className="flex items-center gap-2">
                            <div className="w-2 h-2 bg-yellow-500 rounded-full"></div>
                            待激活
                          </div>
                        </Option>
                      </Select>
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={12}>
                    <Form.Item
                      name="roleIds"
                      label={
                        <span>
                          用户角色
                          <Tooltip title="用户可以拥有多个角色，权限将累加">
                            <InfoCircleOutlined className="ml-1 text-gray-400" />
                          </Tooltip>
                        </span>
                      }
                      rules={[
                        { required: true, message: '请选择至少一个角色' },
                      ]}
                    >
                      <Select
                        mode="multiple"
                        placeholder="请选择用户角色"
                        optionLabelProp="label"
                      >
                        {mockRoles.map(role => (
                          <Option
                            key={role.id}
                            value={role.id}
                            label={role.name}
                          >
                            <div className="flex items-center justify-between">
                              <div className="flex items-center gap-2">
                                <TeamOutlined />
                                <span>{role.name}</span>
                              </div>
                              <span className="text-xs text-gray-500">
                                级别 {role.level}
                              </span>
                            </div>
                            <div className="text-xs text-gray-500 mt-1">
                              {role.description}
                            </div>
                          </Option>
                        ))}
                      </Select>
                    </Form.Item>
                  </Col>
                </Row>
              </Card>

              {/* Security Notice */}
              {mode === 'create' && (
                <Alert
                  type="info"
                  icon={<SafetyOutlined />}
                  message="安全提示"
                  description="新用户创建后，系统将发送激活邮件到指定邮箱。用户需要点击邮件中的链接完成账户激活。"
                  showIcon
                />
              )}
            </div>
          </Col>
        </Row>
      </Form>
    </Modal>
  );
};

export default UserForm;